#ifndef DMA_DOUBLE_BUFFER
#define DMA_DOUBLE_BUFFER
#include <hardware/dma.h>
#include <type_traits>
#include <concepts>


// Restrict template class to 8-bit, 16-bit and 32-bit integral types.
template<typename T>
concept TransferType = std::is_integral<T> && sizeof(T) <= 4;


/**
 * \brief DMA-based double-buffer.
 *
 * The main use case is to keep the destination (usually a peripheral) topped
 * off with a constant stream of data provided at a regular time interval.
*/
template <TransferType T, size_t BUF_SIZE>
class DMADoubleBuffer
{

/**
 * \brief constructor. Setup 2 DMA channels in chained configuration.
 */
    DMADoubleBuffer()
    {
        for (size_t i = 0; i < 2; ++i)
            dma_channels_[i] = dma_claim_unused_channel(true);
        for (size_t i = 0; i < 2; ++i)
        {
            dma_channel_cfgs_[i] = dma_channel_get_default_config(dma_channels_[i]);
            auto& cfg = dma_channel_cfgs_[i];
            channel_config_set_transfer_data_size(&cfg, sizeof(T)>>1);
            channel_config_read_increment(&cfg, true);
            channel_config_write_increment(&cfg, false);
            channel_config_set_chain_to(&cfg, dma_channels[(i+1)%2]);
            // Note: we must still specify source/destination addresses to
            // finish the setup.
        }
    }

    ~DMADoubleBuffer()
    {dma_unclaim_mask((1u << dma_channels_[i]) | (1u << dma_channels_[i]));}

/**
 * \brief Setup the data-request pacing of the DMA transfers.
 * Examples include:
 * - `DREQ_DMA_TIMER0` DMA timer that fires on a configurable interval.
 * - `DREQ_PIO0_TX0` as soon as the outgoing buffer of PIO0 State Machine 0 can
 *  accept a new value.
 * - `DREQ_PIO0_RX0` as soona as the receive buffer of PIO0 State Machine 0
 *   receives a new value.
 */
    void connect_external_pacing_signal(dreq_num_t pacing_signal)
    {
        for (auto& dma_channel_cfg: dma_channel_cfgs_)
            channel_config_set_dreq(pacing_signal);
    }

/**
 * \brief specify the destination address (likely a peripheral).
 */
    void set_target_address(void* target_address)
    {
        for (size_t i = 0; i < 2; ++i)
        {
            auto& cfg = dma_channel_cfgs_[i];
            dma_channel_configure(dma_channels_[i], &cfg,
                                  target_address,   // read address
                                  buffers_[i],      // write address.
                                  BUF_SIZE,
                                  false);  // Don't start.
        }
    }

/**
 * \brief load the buffer with \p num_words words of data from \p word_source.
 * \note alternatively, you can write to the idle buffer directly with
 *  \ref get_idle_buffer
 */
    load_buffer(T* word_source, size_t num_words)
    {
        size_t idle_buffer_id = get_idle_buffer_id();
        memcpy(&buffers_[idle_buffer_id], word_source, num_words*sizeof(T));
    }


/**
 * \brief return index of the buffer that is not currently being used
 *  for DMA transfer. If no active transfer is taking place, return the
 *  next buffer that would be used when the transfer is started.
*/
    size_t get_idle_buffer_id()
    {
        if (dma_channel_is_busy(dma_channels_[0]))
            return 1;
        else if (dma_channel_is_busy(dma_channels_[1]))
            return 0;
        else
            return 0; // Both are free. Pick an arbitrary default.
    }

    size_t get_busy_buffer_id()
    {
        if (dma_channel_is_busy(dma_channels_[0]))
            return 0;
        else if (dma_channel_is_busy(dma_channels_[1]))
            return 1;
        else
            return 1; // Both are free. Pick an arbitrary default.
    }

/**
 * \brief return pointer to the specified buffer.
*/
    T(*)[SIZE] get_buffer(size_t buffer_id)
    {return &(buffers_[buffer_id]);}

    T(*)[SIZE] get_idle_buffer()
    {return get_buffer(get_idle_buffer_id());}

/**
 * \brief Exit the Ping-Pong Buffer endless chaining loop
 */
    void setup_last_dma_transfer(size_t dma_channel, size_t word_count);
    {}

    void start_transfer()
    {
        dma_start_channel_mask((1u << dma_channels_[i]) |
                               (1u << dma_channels_[i]));
    }

/**
 * \brief true if either channel is transferring (or paused mid-transfer).
 */
    bool is_transferring()
    {return dma_channel_is_busy(dma_channels_[0]) ||
            dma_channel_is_busy(dma_channels_[1]);}

    //void pause_transfer();
    //void resume_transfer();

    void abort_transfer()
    {dma_channel_abort(dma_channels_[get_busy_buffer_id()]);

    //void set_internal_transfer_rate(uint32_t words_per_sec);


/**
 * \brief True if neither dma is actively transferring.
 */
    bool transfer_complete()
    {return !(dma_channel_is_busy(dma_channels_[0]) &&
              dma_channel_is_busy(dma_channels_[1]));}

private:
    alignas(16) T buffers_[2][SIZE];
    int dma_channels_[2];
    dma_channel_config_t dma_channel_cfgs_[2];
};
#endif // DMA_DOUBLE_BUFFER

