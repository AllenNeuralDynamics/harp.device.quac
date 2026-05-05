#ifndef DMA_DOUBLE_BUFFER
#define DMA_DOUBLE_BUFFER
#include "hardware/dma.h"
#include <type_traits>
#include <concepts>
#include <bit>
#include <cmath>
#include <functional>


// Restrict template class to 8-bit, 16-bit and 32-bit integral types.
template<typename T>
concept TransferType = std::integral<T> && (sizeof(T) <= 4);

/**
 * \brief DMA-based double-buffer.
 *
 * The main use case is to keep the destination (usually a peripheral) topped
 * off with a constant stream of data provided at a regular time interval.
 *
*/
template <TransferType T, size_t BUF_SIZE>
class DMADoubleBuffer
{
protected:
/**
 * \brief private constructor to setup the bare minimum data members. All other
 * class constructors (including those of derived classes) must still call
 * setup_transfer() in their constructor body.
 */
    DMADoubleBuffer()
    :ctrl_chan_{-1}, data_chan_{-1}, end_of_transfer_irq_num_{-1},
    trigger_isr_{false}{}

public:
/**
 * \brief constructor. Setup 2 DMA channels in chained configuration.
 * \param pacing_signal the data-request pacing signal of the DMA transfer.
 * Examples include:
 * - `DREQ_DMA_TIMER0` DMA timer that fires on a configurable interval.
 * - `DREQ_PIO0_TX0` as soon as the outgoing buffer of PIO0 State Machine 0 can
 *  accept a new value.
 * - `DREQ_PIO0_RX0` as soona as the receive buffer of PIO0 State Machine 0
 *   receives a new value.
 * \param target_address target address for the output of the buffer
 * (likely a peripheral).
 */
    DMADoubleBuffer(dreq_num_t pacing_signal, volatile void* target_address)
    :DMADoubleBuffer()
    {
        setup_transfer(pacing_signal, target_address);
    }

    void setup_transfer(dreq_num_t pacing_signal, volatile void* target_address)
    {
        ctrl_chan_ = dma_claim_unused_channel(true);
        data_chan_ = dma_claim_unused_channel(true);
        // Setup the control channel.
        // Cycle between writing 2 buffer address each time the ctrl channel
        // is invoked using the ring feature.
        // Write to update the data channel's read address.
        // Use alias3 to start the transfer in one write.
        ctrl_chan_data_[0] = &buffers_[0];
        ctrl_chan_data_[1] = &buffers_[1];
        dma_channel_config ctrl_chan_cfg = dma_channel_get_default_config(ctrl_chan_);
        channel_config_set_dreq(&ctrl_chan_cfg, DREQ_FORCE); // Go as fast as possible.
        channel_config_set_transfer_data_size(&ctrl_chan_cfg, DMA_SIZE_32); // system address size.
        channel_config_set_read_increment(&ctrl_chan_cfg, true);
        channel_config_set_write_increment(&ctrl_chan_cfg, false);
        channel_config_set_irq_quiet(&ctrl_chan_cfg, true);
        channel_config_set_chain_to(&ctrl_chan_cfg, ctrl_chan_); // disable chaining.
        // Set ctrl_chan_data_ to reset after transferring 2 words.
        channel_config_set_ring(&ctrl_chan_cfg, false, // wrap read ptr.
                                3); // 8-byte (i.e: 1 << 3) boundary
                                    // creates a ring-size = 2 words.
                                    // Note: addresses are 4 bytes.
        // Apply the configuration.
        dma_channel_configure(ctrl_chan_, &ctrl_chan_cfg,
                              &dma_hw->ch[data_chan_].al3_read_addr_trig, // write address
                              &ctrl_chan_data_[0],      // read address.
                              1,
                              false);  // Don't start.
        ctrl_chan_default_cfg_ = ctrl_chan_cfg;

        // Setup the data channel
        // By chaining-to the ctrl channel, completing a transfer will retrigger
        // the control channel.
        dma_channel_config data_chan_cfg = dma_channel_get_default_config(data_chan_);
        channel_config_set_dreq(&data_chan_cfg, pacing_signal);
        channel_config_set_transfer_data_size(&data_chan_cfg,
                                              dma_channel_transfer_size(sizeof(T)>>1));
        channel_config_set_read_increment(&data_chan_cfg, true);
        channel_config_set_write_increment(&data_chan_cfg, false);
        channel_config_set_irq_quiet(&data_chan_cfg, true);
        channel_config_set_chain_to(&data_chan_cfg, ctrl_chan_);
        // Apply the configuration.
        dma_channel_configure(data_chan_, &data_chan_cfg,
                              target_address,   // write address
                              nullptr,          // read address. Will be populated by ctrl_chan_
                              BUF_SIZE,
                              false);  // Don't start.
        data_chan_default_cfg_ = data_chan_cfg;
    }

    ~DMADoubleBuffer()
    {
        abort_transfer();
        dma_unclaim_mask((1u << ctrl_chan_) | (1u << data_chan_));
    }

    virtual void reset()
    {
        abort_transfer();
        reset_transfer_config();
    }

/**
 * \brief load the buffer with \p num_words words of data from \p word_source.
 * \note alternatively, you can write to the idle buffer directly with
 *  \ref get_idle_buffer
 */
    inline void load_buffer(T* word_source, size_t num_words)
    {memcpy(get_idle_buffer(), word_source, num_words*sizeof(T));}

    //T (*get_idle_buffer())[BUF_SIZE]
    T* get_idle_buffer()
    {return *((T**)(dma_channel_hw_addr(ctrl_chan_)->read_addr));}

/**
 * \brief Enable the last completed dma transfer to trigger an interrupt
 *  request, i.e: connect DMA channel to IRQ.
 * \note an interrupt handler function must be attached separately, and the IRQ
 *  itself must be enabled separately.
 */
    void enable_end_of_transfer_irq(uint32_t irq_index)
    {
        end_of_transfer_irq_num_ = irq_index;
        trigger_isr_ = true; // only gets applied in setup_last_dma_transfer()
    }

/**
 * \brief Disable the last completed dma transfer to trigger an interrupt.
 */
    void disable_end_of_transfer_irq()
    {
        trigger_isr_ = false;
        if (end_of_transfer_irq_num_ < 0) // unspecified. Bail early.
            return;
        // Detach it at the hardware level, so the effect is immediate.
        dma_irqn_set_channel_enabled(end_of_transfer_irq_num_, data_chan_, false);
    }

/**
 * \brief Exit the Ping-Pong Buffer endless chaining loop by specifying that the
 *  next buffer switch to the buffer that is currently idle will be the last
 *  buffer transfer. Adjust transfer count if we aren't transferring a full
 *  buffer's worth.
 */
    void setup_last_dma_transfer(size_t word_count)
    {
        // setting the transfer count while the dma channel is running sets the
        // *next* transfer count; it doesn't affect the active transfer.
        // ref: RP2350 pg 1126
        //TODO: uint32_t encoded_transfer_count = dma_encode_transfer_count(word_count);
        dma_channel_hw_addr(data_chan_)->transfer_count = word_count;
        // Attach channel to IRQ if configured to do so:
        dma_irqn_set_channel_enabled(end_of_transfer_irq_num_, data_chan_,
                                     trigger_isr_);
        // Disable chaining on the next transfer.
        // Modifying the CTRL register updates settings for the *next* transfer.
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        channel_config_set_chain_to(&cfg, data_chan_); // chain-to-self disables chaining.
        channel_config_set_irq_quiet(&cfg, false); // Enable end of transfer irq
        dma_channel_set_config(data_chan_, &cfg, false); // trigger = false
    }

/**
 * \brief start dma transfer. Note that setup_transfer() must be called first.
 */
    void start_transfer()
    {dma_start_channel_mask(1u << ctrl_chan_);}

/**
 * \brief true if either channel is transferring (or paused mid-transfer).
 */
    bool is_transferring()
    {
        // Handle edge case where data_chan is momentarily not-busy while
        // ctrl_chan reconfigures data_chan. Note: we can't just check if
        // ctrl_chan is busy bc it stays busy after data_chan finishes its
        // last transfer.
        // Edge cases:
        //  armed but not transferring -> false
        //    -> data chan idle, ctrl chan idle, dma chain loop connected.
        //  finished transferring -> false
        //    -> data chan idle, ctrl chan busy
        //  aborted -> false
        //    -> data chan idle, ctrl chan idle, dma chain loop disconnected.
        //  paused -> true
        //    -> data chan busy, ctrl chan busy, dma chain loop connected.
        //  transferring but mid-data-channel reconfiguration -> true
        //    -> data chan idle, ctrl chan busy, dma chain loop connected.

        // Note: we check *twice* because it's possible to read on the cycle
        //  where both channels are idle and one is reconfiguring the other.
        bool data_chan_is_busy = dma_channel_is_busy(data_chan_);
        bool ctrl_chan_is_busy = dma_channel_is_busy(ctrl_chan_);
        if (data_chan_is_busy || ctrl_chan_is_busy)
            return true;
        data_chan_is_busy = dma_channel_is_busy(data_chan_);
        ctrl_chan_is_busy = dma_channel_is_busy(ctrl_chan_);
        return data_chan_is_busy || ctrl_chan_is_busy;
    }

/**
 * \brief pause an active transfer
 */
    void pause_transfer()
    {
        // Clear EN bit on an active transfer.
        // Apply the pause, then validate and reapply if needed (in case the
        // data channel was in the middle of being reconfigured).
        if (!is_transferring())
            return;
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        while (true)
        {
            if ((cfg.ctrl & DMA_CH0_CTRL_TRIG_EN_BITS) == 0) // is-paused
                return; // bail when channel actually pauses.
            channel_config_set_enable(&cfg, false); // clear enable bit.
            dma_channel_set_config(data_chan_, &cfg, false); // trigger = false
            // re apply if needed.
            dma_channel_config cfg = dma_get_channel_config(data_chan_);
        }
    }

/**
 * \brief resume a paused transfer
 */
    void resume_transfer()
    {
        // FYI a paused channel appears to be transferring.
        if (!is_transferring())
            return;
        // Set EN bit on data channel to resume transfer.
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        channel_config_set_enable(&cfg, true); // set enable bit.
        dma_channel_set_config(data_chan_, &cfg, false); // trigger = false
    }


/**
 * \brief true if the transfer sequence is paused.
 */
    bool is_paused()
    {
        // is-paused: BUSY == 1 while EN == 0 for data channel.
        if (!is_transferring()) // Check (BUSY == 1) case.
            return false;
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        return (cfg.ctrl & DMA_CH0_CTRL_TRIG_EN_BITS) == 0;
    }

/**
 * \brief stop an active transfer.
 * \note we will need to call setup_transfer() or reset_transfer_config()
 *  before we can start a new transfer.
 * \details See RP2350 Datashset pg 1108 for the correct abort procedure.
 */
    void abort_transfer()
    {
        // Clear EN bit and CHAIN_TO across all channels.
        // Clear ctrl_chan_ first, so we don't race to reconfigure data_chan_?
        int dma_channels[] = {ctrl_chan_, data_chan_};
        for (auto& chan: dma_channels)
        {
            dma_channel_config cfg = dma_get_channel_config(chan);
            channel_config_set_chain_to(&cfg, chan); // chain-to-self disables chaining.
            channel_config_set_enable(&cfg, false); // clear enable bit.
            dma_channel_set_config(chan, &cfg, false); // trigger = false
        }
        uint32_t abort_mask = (1u << ctrl_chan_) | (1u << data_chan_);
        dma_hw->abort = abort_mask;
        // RP2350 only: poll set bits until they clear (i.e abort took effect).
        while (dma_hw->abort & abort_mask)
            tight_loop_contents();
    }

    bool is_aborted()
    {
        bool data_chan_is_busy = dma_channel_is_busy(data_chan_);
        bool ctrl_chan_is_busy = dma_channel_is_busy(ctrl_chan_);
        return dma_chain_loop_disconnected()
               && !data_chan_is_busy && !ctrl_chan_is_busy;
    }


/**
 * \brief reset both dma channels to their starting configuration (i.e:
 *  ready to kick off a transfer sequence) set by the most recent call to
 * setup_transfer().
 */
    void reset_transfer_config()
    {
        // For ctrl_chan_ we must additionally reset the idle buffer because
        // ping-ponging between buffers is specified relative to the starting
        // buffer address.
        dma_channel_set_read_addr(ctrl_chan_, &ctrl_chan_data_[0], false);
        dma_channel_set_config(ctrl_chan_, &ctrl_chan_default_cfg_, false);
        // For data_chan_ we must additionally reset the transfer count since
        // it was likely altered for the last buffer transfer.
        dma_channel_hw_addr(data_chan_)->transfer_count = BUF_SIZE;
        dma_channel_set_config(data_chan_, &data_chan_default_cfg_, false);
    }

/**
 * \brief get the DMA channel responsible for starting the DMA transfer
 *  (useful if we need to start multiple channels at once).
 */
    int get_ctrl_channel() const
    {return ctrl_chan_;}

    int get_data_channel() const
    {return data_chan_;}

/**
 * \brief get all dma channels used in this class as a single bitmask.
 */
    inline uint32_t get_dma_channel_mask() const
    {return (1u << ctrl_chan_) | (1u << data_chan_);}


/**
 * \brief True if neither dma is actively transferring.
 * \note that per-this-implementation, a transfer is considered complete
 *  even if it has been aborted.
 */
    bool transfer_complete()
    {return !(is_transferring());}

/**
 * \brief true if dma re-triggering loop has been disconnected.
 * \details this happens if the current transfer was aborted or the next
 *  transfer was set to be the final transfer.
 */
    inline bool dma_chain_loop_disconnected()
    {
        // True if data_chan_ is chained-to-self.
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        uint32_t chained_channel = get_chained_channel(cfg);
        return chained_channel == data_chan_;
    }

private:
/**
 * \brief get the chained channel encoded in the given dma channel config.
 */
    inline uint32_t get_chained_channel(dma_channel_config cfg)
    {
        return ((cfg.ctrl & DMA_CH0_CTRL_TRIG_CHAIN_TO_BITS)
                >> DMA_CH0_CTRL_TRIG_CHAIN_TO_LSB);
    }


    alignas(8*sizeof(T)) T buffers_[2][BUF_SIZE];
    int ctrl_chan_;
    dma_channel_config ctrl_chan_default_cfg_;
    T (*ctrl_chan_data_[2])[BUF_SIZE];
    int data_chan_;
    dma_channel_config data_chan_default_cfg_;

    int end_of_transfer_irq_num_;
    bool trigger_isr_;
};


/**
 * \brief Identical to DMADoubleBuffer except this child class assumes
 * timer-based pacing where the hardware timer is either claimed internally
 * (new timer every time) or externally (shared timer) and passed in.
 */
template <TransferType T, size_t BUF_SIZE>
class TimerPacedDMADoubleBuffer: public DMADoubleBuffer<T, BUF_SIZE>
{
public:
    static inline constexpr size_t DEFAULT_FREQUENCY_HZ = 500000;

/**
 * \brief constructor. Auto-claim a hardware timer.
 * \param target_address destination address for the buffer to output to.
 */
    TimerPacedDMADoubleBuffer(volatile void* target_address)
    : DMADoubleBuffer<T, BUF_SIZE>(), dma_timer_chan_{-1},
    claimed_timer_chan_{true}
    {
        dma_timer_chan_ = dma_claim_unused_timer(true);
        // Get associated pacing signal for this timer.
        dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan_));
        DMADoubleBuffer<T, BUF_SIZE>::setup_transfer(pacing_signal, target_address);
    }

/**
 * \brief constructor. Pass in an an already-claimed timer.
 * \param dma_timer_chan
 * \param target_address destination address for the buffer to output to.
 */
    TimerPacedDMADoubleBuffer(int dma_timer_chan, volatile void* target_address)
    : DMADoubleBuffer<T, BUF_SIZE>(), dma_timer_chan_{dma_timer_chan},
    claimed_timer_chan_{false}
    {
        // Get associated pacing signal for this timer.
        dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan_));
        DMADoubleBuffer<T, BUF_SIZE>::setup_transfer(pacing_signal, target_address);
    }

    ~TimerPacedDMADoubleBuffer()
    {
        if (claimed_timer_chan_)
            dma_timer_unclaim(dma_timer_chan_);
    }

/**
 * \brief reset the buffer and additionally reset the pacing timer to the
 *  default frequency
 */
    void reset() override
    {
        DMADoubleBuffer<T, BUF_SIZE>::reset();
        set_frequency_hz(DEFAULT_FREQUENCY_HZ);
    }

/**
 *  \brief set frequency (in Hz) at which the buffer sends new data to the
 * target address specified in the constructor.
 * \warning frequency must be a multiple of sys clock (150MHz).
 * \warning if timer is shared, this value will also apply to other resources
 * using the timer.
 */
    void set_frequency_hz(uint32_t hz)
    {
        float divisor = float(SYS_CLK_HZ) / hz;
        if (round(divisor) != divisor)
        {panic("Update frequency (%f [Hz]) must be a multiple of sys clock: %d",
                SYS_CLK_HZ);}
        dma_timer_set_fraction(dma_timer_chan_, 1, divisor);
        // TODO: enable more flexible pacing options by allocating timers
        //  on-demand and sharing timers for matching frequencies, and
        //  respecting max number of used timers.
        //  Requires re-attaching timers to buffers.
    }

private:
    bool claimed_timer_chan_;
    int dma_timer_chan_;
};
#endif // DMA_DOUBLE_BUFFER
