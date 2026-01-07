#ifndef DMA_DOUBLE_BUFFER
#define DMA_DOUBLE_BUFFER
#include <hardware/dma.h>
#include <type_traits>
#include <concepts>
#include <bit>
#include <functional>


// Restrict template class to 8-bit, 16-bit and 32-bit integral types.
template<typename T>
concept TransferType = std::integral<T> && (sizeof(T) <= 4);

// Restrict Buffer to power of two due to limitations of the DMA hardware as
// currently implemented.
template<typename T>
concept BufSize = std::integral<T>;// && std::has_single_bit<T>;// && requires(T t){ t <= 32768;};




/**
 * \brief DMA-based double-buffer.
 *
 * The main use case is to keep the destination (usually a peripheral) topped
 * off with a constant stream of data provided at a regular time interval.
*/
template <TransferType T, size_t BUF_SIZE>
class DMADoubleBuffer
{
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
    DMADoubleBuffer(dreq_num_t pacing_signal, T* target_address,
                    bool increment = false)
    {
        for (size_t i = 0; i < 2; ++i)
            dma_channels_[i] = dma_claim_unused_channel(true);
        for (size_t i = 0; i < 2; ++i)
        {
            dma_channel_cfgs_[i] = dma_channel_get_default_config(dma_channels_[i]);
            auto& cfg = dma_channel_cfgs_[i];
            channel_config_set_dreq(&cfg, pacing_signal);
            channel_config_set_transfer_data_size(&cfg, dma_channel_transfer_size(sizeof(T)>>1));
            channel_config_set_read_increment(&cfg, true);
            channel_config_set_write_increment(&cfg, increment);
            channel_config_set_chain_to(&cfg, dma_channels_[(i+1)%2]);
            // By default, the DMA's write address will not reset after the
            // transfer completes without reconfiguration unless we set the
            // ring buffer setting. Note that this will only reset on a power
            // of two.
            channel_config_set_ring(&cfg, true, 15);
            // Apply the configuration.
            dma_channel_configure(dma_channels_[i], &cfg,
                                  target_address,   // write address
                                  buffers_[i],      // read address.
                                  BUF_SIZE,
                                  false);  // Don't start.
        }
    }

    ~DMADoubleBuffer()
    {dma_unclaim_mask((1u << dma_channels_[0]) | (1u << dma_channels_[1]));}

/**
 * \brief load the buffer with \p num_words words of data from \p word_source.
 * \note alternatively, you can write to the idle buffer directly with
 *  \ref get_idle_buffer
 */
    void load_buffer(T* word_source, size_t num_words)
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
    T (*get_buffer(size_t buffer_id))[BUF_SIZE]
    {return &(buffers_[buffer_id]);}

    T (*get_idle_buffer())[BUF_SIZE]
    {return get_buffer(get_idle_buffer_id());}

/**
 * \brief Exit the Ping-Pong Buffer endless chaining loop
 */
    void setup_last_dma_transfer(size_t word_count)
    {
        //uint32_t encoded_transfer_count = dma_encode_transfer_count(word_count);
        uint channel = dma_channels_[get_idle_buffer_id()];
        //dma_channel_hw_addr(channel)->transfer_count = encoded_transfer_count;
        dma_channel_hw_addr(channel)->transfer_count = word_count;
    }

    void start_transfer()
    {dma_start_channel_mask(1u << dma_channels_[0]);}

/**
 * \brief true if either channel is transferring (or paused mid-transfer).
 */
    bool is_transferring()
    {return dma_channel_is_busy(dma_channels_[0]) ||
            dma_channel_is_busy(dma_channels_[1]);}

    //void pause_transfer();
    //void resume_transfer();

    void abort_transfer()
    {dma_channel_abort(dma_channels_[get_busy_buffer_id()]);}

    //void set_internal_transfer_rate(uint32_t words_per_sec);


/**
 * \brief True if neither dma is actively transferring.
 */
    bool transfer_complete()
    {return !(dma_channel_is_busy(dma_channels_[0]) ||
              dma_channel_is_busy(dma_channels_[1]));}

private:
    alignas(8*sizeof(T)) T buffers_[2][BUF_SIZE];
    int dma_channels_[2];
    dma_channel_config dma_channel_cfgs_[2];
};
#endif // DMA_DOUBLE_BUFFER

