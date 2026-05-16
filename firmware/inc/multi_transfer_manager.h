#ifndef MULTI_TRANSFER_MANAGER
#define MULTI_TRANSFER_MANAGER
#include <array>
#include <cstdint>
#include "pico/stdlib.h"
#include "pico/util/queue.h"
#include "hardware/dma.h"
#include "hardware/timer.h"
#include "pio_ltc264x.h"
#include "dma_double_buffer.h"

/**
 * \brief represents an event when one or more channels finished transferring
 *  and when they finished.
 */
struct end_of_transfer_event_t
{
    uint32_t finished_channels_mask;
    uint64_t timestamp_us;
};

/**
 * \brief Class to catch DMADoubleBuffer events and stuff them into a queue.
 * \warning Because the RP2xx0 has limited DMA_IRQ resources, only 2 instances
 * of this class can be instantiated maximum. (In most cases, you probably only
 * need one.)
 */
template <typename T, size_t BUF_SIZE, size_t NUM_CHANNELS>
class MultiTransferManager
{
public:
    static inline constexpr size_t DEFAULT_QUEUE_SIZE = 32;

/**
 * \brief constructor.
 * \param buf_ptrs reference to an array of buffer ptrs. (Cannot be reference to
 *  array of buffers because we may use a child class implementation.)
 * \param dacs reference to an array of dacs
 */
    MultiTransferManager(
        std::array<DMADoubleBuffer<T, BUF_SIZE>*, NUM_CHANNELS>& buf_ptrs,
        std::array<PIO_LTC264x, NUM_CHANNELS>& dacs)
    : buf_ptrs_{buf_ptrs}, dacs_{dacs}, irq_{-1}
    {
        queue_init(&end_of_transfer_event_queue_,
                   sizeof(end_of_transfer_event_t), DEFAULT_QUEUE_SIZE);
    }

    ~MultiTransferManager()
    {
        reset();
        queue_free(&end_of_transfer_event_queue_);
    }

/**
 *  \brief undo object state changes since instantiation.
 */
    void reset()
    {
        // Drain queue.
        end_of_transfer_event_t transfer_done_event;
        while (queue_try_remove(&end_of_transfer_event_queue_,
                                &transfer_done_event)){}
        // Detach interrupt.
        disable_end_of_transfer_interrupt();
    }


/**
 * \brief start one or more buffer transfers simultaneously.
 */
    void start(uint32_t channel_mask)
    {
        // Create a trigger mask to start all Double Buffer DMA channels at once.
        uint32_t trigger_mask = 0;
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if ((channel_mask & (1u << i)))
                trigger_mask |= (1u << buf_ptrs_[i]->get_ctrl_channel());
        }
        dma_start_channel_mask(trigger_mask);
    }

/**
 * \brief abort multiple buffer transfers simultaneously
 */
    void abort(uint32_t channel_mask)
    {
        // Create a mask to stop all Double Buffer DMA channels at once.
        uint32_t abort_mask = 0;
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if ((channel_mask & (1u << i)))
                abort_mask |= (1u << buf_ptrs_[i]->get_dma_channel_mask());
        }
        dma_hw->abort = abort_mask;
        // RP2350 only: poll set bits until they clear (i.e abort took effect).
        while (dma_hw->abort & abort_mask)
            tight_loop_contents();
        // Additionally, call each channel's abort to clear DMA config.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if ((channel_mask & (1u << i)))
                buf_ptrs_[i]->abort_transfer();
        }
    }

/**
 * \brief Enable a finished transfer to trigger an interrupt.
 *  Interrupt can be specified explicitly. Otherwise, the default will be used.
 *  For more details on the default interrupt behavior, see
 *  handle_end_of_transfer()
 * \warning Per hardware limits, only two `MultiTransferManager` instances max
 *  can use the two separate DMA irq interrupt lines (DMA_IRQ_0 and DMA_IRQ_1).
 * \param dma_irq_index 0 or 1 corresponding to DMA_IRQ_0 or DMA_IRQ_1
 * \param fn_ptr optional callback function to call instead of the default.
 */
    void enable_end_of_transfer_interrupt(size_t dma_irq_index,
                                          void (*fn_ptr)(void) = nullptr)
    {
        // Limit one DMA IRQ line per class instance.
        if (irq_ >= 0) // Bail early if already specified
            return;
        irq_ = DMA_IRQ_0 + dma_irq_index; // get associated IRQ number
        // For the default interrupt callback fn,
        // use the wrapper ("trampoline") function so that we can pass a
        // pointer-to-function to the IRQ (cannot be pointer-to-member).
        // Connect IRQ to handler function.
        if (fn_ptr == nullptr)
        {
            fn_ptr = static_end_of_transfer_callbacks[dma_irq_index];
            insts_[dma_irq_index] = this;
        }
        irq_set_exclusive_handler(irq_, fn_ptr);
        // Enable underlying dma channels to trigger IRQ.
        for (auto& buf_ptr: buf_ptrs_)
            buf_ptr->enable_end_of_transfer_irq(dma_irq_index); // from 0.
        // Enable the interrupt.
        irq_set_enabled(irq_, true);
    }

/**
 * \brief
 */
    void disable_end_of_transfer_interrupt()
    {
        // Disconnect irq handler function if it was set.
        if (irq_ < 0)
            return;
        irq_set_exclusive_handler(irq_, nullptr);
        irq_ = -1;
        // Disable the interrupt.
        for (auto& buf_ptr: buf_ptrs_)
            buf_ptr->disable_end_of_transfer_irq();
        // Enable the interrupt.
        irq_set_enabled(irq_, false);
    }

/**
 * \brief receive a record of any finished transfers from a queue and put
 *  the contents in \p event_ptr.
 * \return `true`, if a record was successfully remove from the queue;
 *  `false` otherwise.
 */
    inline bool get_finished_transfers(end_of_transfer_event_t* event_ptr)
    {
        return queue_try_remove(&end_of_transfer_event_queue_, event_ptr);
    }

/**
 * \brief static trampoline function to pass to the ISR for DMA_IRQ_0.
 * \details ISRs cannot invoke a pointer-to-member function, so we pass this
 * wrapper function instead.
 */
    static void static_handle_end_of_transfer0()
    {insts_[0]->handle_end_of_transfer();}

/**
 * \brief static trampoline function to pass to the ISR for DMA_IRQ_0.
 * \details ISRs cannot invoke a pointer-to-member function, so we pass this
 * wrapper function instead.
 */
    static void static_handle_end_of_transfer1()
    {insts_[1]->handle_end_of_transfer();}

/**
 * \brief The ISR callback function to handle the end of any (or multiple)
 *  file buffer(s) finishing a transfer. Specifically,
 *  - record which channels finished and when (bitmask, timestamp). Push the
 *    result to a queue for later collection in a superloop, etc.
 *  - set the corresponding DAC in \ref dacs_ to midscale, i.e: the "idle"
 *    value.
 * \note  this callback can handle multiple buffer transfers that finish
 *  simultaneously.
 * \note implemented as `inline` such that the contents of this function are
 *  (ideally) splatted into the static wrapper function.
 */
    inline void handle_end_of_transfer()
    {
        end_of_transfer_event_t end_of_transfer_event;
        end_of_transfer_event.timestamp_us = time_us_64(); // record time asap.
        end_of_transfer_event.finished_channels_mask = 0;

        // Identify which channel(s) triggered the handler.
        uint32_t irq_index = irq_ - DMA_IRQ_0;
        uint32_t int_status = dma_hw->irq_ctrl[irq_index].ints;
        // Clear the interrupt(s).
        dma_hw->irq_ctrl[irq_index].ints = int_status;
        // Disconnect already-fired DMA channels from interrupt
        // (since we only fire once at end of buffer transfer).
        // Figure out which DMA channels finished.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            // Identify which AO channel triggered the interrupt.
            if (!((1u << buf_ptrs_[i]->get_data_channel()) & int_status))
                continue; // Skip channels that did not trigger the interrupt.
            dacs_[i].write_value(PIO_LTC264x::OUTPUT_MIDSCALE);
            end_of_transfer_event.finished_channels_mask |= 1u << i;
        }
        // Push a timestamp bitmask to a queue.
        queue_try_add(&end_of_transfer_event_queue_, &end_of_transfer_event);
    }

private:
    std::array<DMADoubleBuffer<T, BUF_SIZE>*, NUM_CHANNELS>& buf_ptrs_;
    std::array<PIO_LTC264x, NUM_CHANNELS>& dacs_;
    queue_t end_of_transfer_event_queue_;
    int irq_;

    static inline constexpr size_t MAX_INSTANCES = 2;

    typedef void (*callback_fn_ptr)(void);
    static inline callback_fn_ptr static_end_of_transfer_callbacks[MAX_INSTANCES] =
        {static_handle_end_of_transfer0, static_handle_end_of_transfer1};
    static inline MultiTransferManager<T, BUF_SIZE, NUM_CHANNELS>* insts_[MAX_INSTANCES] =
        {nullptr, nullptr};
};
#endif // MULTI_TRANSFER_MANAGER
