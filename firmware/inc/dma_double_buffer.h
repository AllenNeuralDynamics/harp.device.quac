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
 *
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
    DMADoubleBuffer(dreq_num_t pacing_signal, T* target_address)
    :ctrl_chan_{-1}, data_chan_{-1}
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
        ctrl_chan_cfg_ = dma_channel_get_default_config(ctrl_chan_);
        auto& cfg = ctrl_chan_cfg_;
        channel_config_set_dreq(&cfg, DREQ_FORCE); // Go as fast as possible.
        channel_config_set_transfer_data_size(&cfg, DMA_SIZE_32);
        channel_config_set_read_increment(&cfg, true);
        channel_config_set_write_increment(&cfg, false);
        channel_config_set_irq_quiet(&cfg, true);
        channel_config_set_ring(&cfg, false, // wrap read ptr.
                                3); // 8-byte (i.e: 1 << 3) boundary
                                    // creates a ring-size = 2 words.
                                    // Note: addresses are 4 bytes.
        // Apply the configuration.
        dma_channel_configure(ctrl_chan_, &cfg,
                              &dma_hw->ch[data_chan_].al3_read_addr_trig, // write address
                              &ctrl_chan_data_[0],      // read address.
                              1,
                              false);  // Don't start.

        // Setup the data channel
        // By chaining-to the ctrl channel, completing a transfer will retrigger
        // the control channel.
        data_chan_cfg_ = dma_channel_get_default_config(data_chan_);
        cfg = data_chan_cfg_;
        channel_config_set_dreq(&cfg, pacing_signal);
        channel_config_set_transfer_data_size(&cfg, DMA_SIZE_16);//FIXME: dma_channel_transfer_size(sizeof(T)>>1));
        channel_config_set_read_increment(&cfg, true);
        channel_config_set_write_increment(&cfg, false);
        channel_config_set_irq_quiet(&cfg, true);
        channel_config_set_chain_to(&cfg, ctrl_chan_);
        // Apply the configuration.
        dma_channel_configure(data_chan_, &cfg,
                              target_address,   // write address
                              nullptr,          // read address. Will be populated by ctrl_chan_
                              BUF_SIZE,
                              false);  // Don't start.
    }

    ~DMADoubleBuffer()
    {dma_unclaim_mask((1u << ctrl_chan_) | (1u << data_chan_));}

/**
 * \brief load the buffer with \p num_words words of data from \p word_source.
 * \note alternatively, you can write to the idle buffer directly with
 *  \ref get_idle_buffer
 */
    void load_buffer(T* word_source, size_t num_words)
    {
        memcpy(get_idle_buffer(), word_source, num_words*sizeof(T));
    }


    //T (*get_idle_buffer())[BUF_SIZE]
    T* get_idle_buffer()
    {return *((T**)(dma_channel_hw_addr(ctrl_chan_)->read_addr));}

/**
 * \brief Exit the Ping-Pong Buffer endless chaining loop
 */
    void setup_last_dma_transfer(size_t word_count)
    {
        // setting the transfer count while the dma channel is running sets the
        // *next* transfer count; it doesn't affect the active transfer.
        // ref: RP2350 pg 1126
        //TODO: uint32_t encoded_transfer_count = dma_encode_transfer_count(word_count);
        dma_channel_hw_addr(data_chan_)->transfer_count = word_count;

        // Disable chaining on the next transfer.
        // Modifying the CTRL register updates the *next*
        dma_channel_config cfg = dma_get_channel_config(data_chan_);
        channel_config_set_chain_to(&cfg, data_chan_); // chain-to-self disables chaining.
        dma_channel_set_config(data_chan_, &cfg, false); // trigger = false
    }

    void start_transfer()
    {dma_start_channel_mask(1u << ctrl_chan_);}

/**
 * \brief true if either channel is transferring (or paused mid-transfer).
 */
    bool is_transferring()
    {return dma_channel_is_busy(ctrl_chan_) || dma_channel_is_busy(data_chan_);}

    //void pause_transfer();
    //void resume_transfer();

    void abort_transfer()
    {dma_hw->abort = (1u << ctrl_chan_) | (1u << data_chan_);
    // FIXME: we need to setup the transfer all over again.
    }

/**
 * \brief True if neither dma is actively transferring.
 */
    bool transfer_complete()
    {return !(dma_channel_is_busy(ctrl_chan_) || dma_channel_is_busy(data_chan_));}

//private:
    alignas(8*sizeof(T)) T buffers_[2][BUF_SIZE]; // FIXME: huge alignment isn't needed.
    int ctrl_chan_;
    dma_channel_config ctrl_chan_cfg_;
    T (*ctrl_chan_data_[2])[BUF_SIZE];
    int data_chan_;
    dma_channel_config data_chan_cfg_;

    //int dma_channels_[2];
    dma_channel_config dma_channel_cfgs_[2];
};
#endif // DMA_DOUBLE_BUFFER

