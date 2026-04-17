#ifndef MULTI_FILE_PLAYER_H
#define MULTI_FILE_PLAYER_H
#include <waveform_settings.h>
#include <pio_ltc264x.h>
#include <dma_double_buffer.h>
#include <ff.h>
#include <f_util.h>
#include <pico/stdlib.h>
#include <pico/util/queue.h>
#include <etl/vector.h>
#include <cmath>
#include <limits>

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
 * \brief class for playing multiple files to multiple DACs concurrently.
 */
template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class MultiFilePlayer
{
public:
    static inline constexpr size_t DEFAULT_FREQUENCY_HZ = 500000;
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max()/2;
    static inline constexpr size_t DEFAULT_QUEUE_SIZE = 32;


/**
 * \brief constructor.
 * \param dacs
 * \param filenames
 */
    MultiFilePlayer(std::array<PIO_LTC264x, NUM_CHANNELS>& dacs,
                    const std::array<const char*, NUM_CHANNELS>& filenames)
    : dacs_{dacs}, filenames_{filenames}, dma_timer_chan_{-1}, irq_{-1}
    {
        // Timer setup. Also initializes `timer_pacing_signal_`.
        dma_timer_chan_ = dma_claim_unused_timer(true);
        timer_pacing_signal_ = dreq_num_t(dma_get_timer_dreq(dma_timer_chan_));
        set_frequency_hz(DEFAULT_FREQUENCY_HZ);
        // Connect buffer output to dacs; connect timer to buffers.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            PIO& pio = dacs_[i].get_pio();
            int32_t sm = dacs_[i].get_sm();
            file_bufs_.emplace_back(timer_pacing_signal_, &pio->txf[sm]);
        }
        queue_init(&end_of_transfer_event_queue_,
                   sizeof(end_of_transfer_event_t), DEFAULT_QUEUE_SIZE);
        reset();
    }

/**
 * \brief setup the file reading loop
 * \details opens each expected file on the SD card. Missing files are
 *  tolerated: the corresponding channel stays in a "not file-backed" state
 *  and `channel_is_busy()`/`channel_is_ready()` report false for it, so the
 *  file player is a no-op for that channel. This lets the waveform player
 *  drive channels even with no SD card inserted.
 */
    void setup()
    {
        // TODO: could use some extra protection to make this func idempotent.
        // Open 4 files. Note: max number is set by FF_FS_LOCK in ffconf.h
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if (!file_is_open(id))
                (void)open_file(id);
            is_file_mode_[id] = file_is_open(id);
        }
        // pre-read buffers.
        update();
    }

/**
 * \brief Reset internal variables and close all files.
 * \note must call setup() before the class is ready for general usage.
 * \warning not multicore safe.
 */
    void reset()
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
            file_bufs_[i].abort_transfer(); // Do this first across all channels.
        // TODO: maybe validate that the abort took place?
        std::ranges::fill(idle_buffers_, nullptr); // Clear local buffer value.
        std::ranges::fill(filptrs_, nullptr); // mark files as starting closed.
        std::ranges::fill(curr_iterations_, 0);
        std::ranges::fill(iterations_, 1);
        std::ranges::fill(is_file_mode_, false); // no channel is file-backed yet.
        for (auto& dac: dacs_)
            dac.write_value(OUTPUT_MIDSCALE);
        cleanup(); // close all files.
        // Drain queue.
        end_of_transfer_event_t dummy_event;
        while (queue_try_remove(&end_of_transfer_event_queue_, &dummy_event)){}
    }

/**
 * \brief destructor
 */
    ~MultiFilePlayer()
    {
        cleanup();
        queue_free(&end_of_transfer_event_queue_);
    }

/**
 * \brief
 */
    inline bool file_is_open(size_t file_index)
    {return filptrs_[file_index] != nullptr;}

/**
 * \brief attempt to open the given channel's file for reading.
 * \return true on success, false if the file is missing or the SD card is
 *  unavailable. Non-fatal so the device remains usable with no SD card.
 */
    inline bool open_file(size_t file_index)
    {
        FRESULT fr = f_open(&fils_[file_index], filenames_[file_index], FA_READ);
        if (fr != FR_OK)
            return false;
        filptrs_[file_index] = &fils_[file_index]; // set ptr to indicate open file.
        return true;
    }

    inline void close_file(size_t file_index)
    {
        if (!file_is_open(file_index))
            return;
        (void)f_close(&fils_[file_index]);
        filptrs_[file_index] = nullptr; // clear ptr to indicate closed file.
    }

/**
 * \brief accessor for the underlying DMA double-buffer for a given channel.
 * \note only intended to be called by a cooperating waveform generator that
 *  wants to borrow the same DMA transport while the file player is idle on
 *  that channel. See `abandon_files` / `reclaim_files`.
 */
    inline DMADoubleBuffer<T, BUF_SIZE>& get_file_buf(size_t channel_id)
    {return file_bufs_[channel_id];}

/**
 * \brief release ownership of the specified channels so a cooperating
 *  waveform generator can drive the DAC via the same DMA transport.
 * \details aborts any in-flight transfer, closes the file (if open), and
 *  marks the channel as not file-backed so `update()` skips it and the
 *  busy checks return false for it.
 */
    void abandon_files(uint32_t mask)
    {
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)))
                continue;
            if (file_bufs_[ch].is_transferring())
                file_bufs_[ch].abort_transfer();
            if (file_is_open(ch))
                close_file(ch);
            is_file_mode_[ch] = false;
            idle_buffers_[ch] = nullptr;
            curr_iterations_[ch] = 0;
        }
    }

/**
 * \brief reclaim ownership of the specified channels, reopen their files
 *  (if available), and re-prime their buffers.
 * \note if a file cannot be opened, the channel stays in the not-file-backed
 *  state. This is the symmetric operation for `abandon_files`.
 */
    void reclaim_files(uint32_t mask)
    {
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)))
                continue;
            file_bufs_[ch].abort_transfer();
            file_bufs_[ch].reset_transfer_config();
            if (!file_is_open(ch))
                (void)open_file(ch);
            is_file_mode_[ch] = file_is_open(ch);
            idle_buffers_[ch] = nullptr;
            curr_iterations_[ch] = 0;
            if (is_file_mode_[ch])
                f_rewind(&fils_[ch]);
        }
        // Prime buffers for the reclaimed channels.
        update();
    }

/**
 * \brief true if the specified channel is currently backed by an open file.
 */
    inline bool is_file_mode(size_t channel_id) const
    {return is_file_mode_[channel_id];}

/**
 * \brief close all open files.
 */
    void cleanup()
    {
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if (file_is_open(id))
                close_file(id);
        }
    }

/**
 * \brief Enable a finished transfer to trigger an interrupt.
 *  Interrupt can be specified explicitly. Otherwise, the default will be used.
 *  For more details on the default interrupt behavior, see
 *  handle_end_of_transfer()
 * \warning Per current implementation, only one `MultiFilePlayer` instance can
 *  use the default interrupt handler, because its functionality is tied to a
 *  static class member.
 */
    void enable_end_of_transfer_interrupt(size_t dma_irq_index,
                                          void (*fn_ptr)(void) = nullptr)
    {
        irq_ = DMA_IRQ_0 + dma_irq_index; // get associated IRQ number
        // For the default interrupt callback fn,
        // Create a wrapper ("trampoline") function so that we can pass a
        // pointer-to-function to the IRQ (cannot be pointer-to-member).
        // Connect IRQ to handler function.
        if (fn_ptr == nullptr)
        {
            fn_ptr = static_handle_end_of_transfer;
            isr_instance_ = this;
        }
        irq_set_exclusive_handler(irq_, fn_ptr);
        // Enable underlying dma channels to trigger IRQ.
        for (auto& file_buf: file_bufs_)
            file_buf.enable_end_of_transfer_irq(dma_irq_index); // from 0.
        // Enable the interrupt.
        irq_set_enabled(irq_, true);
    }

/**
 * \brief 
 */
    void disable_end_of_transfer_interrupt()
    {
        // Disconnect irq handler function.
        irq_set_exclusive_handler(irq_, nullptr);
        irq_ = -1;
        // Disable the interrupt.
        for (auto& file_buf: file_bufs_)
            file_buf.disable_end_of_transfer_irq();
        // Enable the interrupt.
        irq_set_enabled(irq_, false);
        // TODO: clear queue?
    }

/**
 * \brief static trampoline function to pass to the ISR. ISRs cannot invoke
 *  a pointer-to-member function, so we use this wrapper function instead.
 */
    static void __not_in_flash_func(static_handle_end_of_transfer)()
    {isr_instance_->handle_end_of_transfer();}

/**
 * \brief The ISR callback function to handle the end of any (or multiple)
 *  file buffer(s) finishing a transfer. Specifically,
 *  - record which channels finished and when (bitmask, timestamp). Push the
 *    result to a queue for later collection in a superloop, etc.
 *  - set the corresponding DAC in \ref dacs_ to midscale, i.e: the "idle"
 *    value.
 * \note implemented as `inline` such that the contents of this function are
 *  (ideally) splatted into the static wrapper function.
 */
    inline void __not_in_flash_func(handle_end_of_transfer)()
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
            if (!((1u << file_bufs_[i].get_data_channel()) & int_status))
                continue; // Skip channels that did not trigger the interrupt.
            dacs_[i].write_value(OUTPUT_MIDSCALE);
            end_of_transfer_event.finished_channels_mask |= 1u << i;
        }
        // Push a timestamp bitmask to a queue.
        queue_try_add(&end_of_transfer_event_queue_, &end_of_transfer_event);
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
 * \brief set the update frequency for the specified channel.
 * \details Assume soure clock of 150MHz.
 */
    void set_frequency_hz(uint32_t frequency_hz)
    {
        float divisor = float(SYS_CLK_HZ) / frequency_hz;
        if (round(divisor) != divisor)
        {panic("Update frequency (%f [Hz]) must be a multiple of sys clock: %d",
                SYS_CLK_HZ);}
        // TODO: enable more flexible pacing options by allocating timers
        //  on-demand and sharing timers for matching frequencies, and
        //  respecting max number of used timers.
        //  Requires re-attaching timers to buffers.
        dma_timer_set_fraction(dma_timer_chan_, 1, divisor);
    }

/**
 * \brief
 * \warning not multicore safe.
 */
    void set_channel_iterations(size_t channel_index, size_t iterations)
    {iterations_[channel_index]= iterations;}

/**
 * \brief start one or more channels specified as bitfields.
 * \details multiple channels started this way will be started concurrently.
 * \note can be called from either core.
 */
    void start(uint32_t channel_mask)
    {
        // TODO: figure out resume-logic.
        // TODO: maybe make this fn return a bool in case we're not ready (armed)?
        // Create a trigger mask to start all Double Buffer DMA channels at once.
        uint32_t multi_channel_trigger_mask = 0;
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if ((channel_mask & (1u << i)) == 0)
                continue;
            multi_channel_trigger_mask |= (1u << file_bufs_[i].get_ctrl_channel());
        }
        dma_start_channel_mask(multi_channel_trigger_mask);
    }

    void pause(uint32_t channel_mask)
    {
        // TODO.
    }

    void resume(uint32_t channel_mask)
    {
        // TODO.
    }

    void abort(uint32_t channel_mask)
    {
        // TODO.
    }

    void abort_channel(size_t channel_index)
    {
        file_bufs_[channel_index].abort_transfer();
        // interacting with the buffer object is multicore safe, but everything
        // else is not, so cleanup must go through the update loop.
        // FIXME: how do we delegate cleanup to the update loop?
    }

/**
 * \brief attach an interrupt
 */
    void attach_waveform_finished_interrupt()
    {
        // TODO
    }

/**
 * \brief True if all channels specified in the mask are armed.
 */
    bool is_armed(uint32_t channel_mask)
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if (channel_mask & (1u << i))  // Check specific channels
            {
                if (!channel_is_armed(i))
                    return false;
            }
        }
        return true;
    }

/**
 * \brief true if the channel's buffer has been filled and the underlying
 *  DMA channel can start draining it immediately.
 */
    inline bool channel_is_armed(size_t channel_id)
    {
        //  we can't strictly rely on an nonzero file read pointer
        //  (i.e: `f_tell(fils_[id] != 0`) because the overall file size may be
        //  less than the buffer size, so it would be constantly reset to 0.
        return (idle_buffers_[channel_id] != nullptr) &&
               (!file_bufs_[channel_id].is_aborted());
    }

/**
 * \brief true if channel is transferring data to its respective DAC.
 *  False otherwise (paused or aborted).
 */
    inline bool channel_is_active(size_t channel_id)
    {return file_bufs_[channel_id].is_transferring();}

/**
 * \brief true if any channel needs to be handled with periodic calls to update().
 */
    bool is_busy()
    {
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if (channel_is_busy(id))
                return true;
        }
        return false;
    }

/**
 * \brief true if the specified channel needs to be handled with periodic calls
 *  to update().
 * \note a channel that has been abandoned (not file-backed) is not busy from
 *  this player's perspective — some other cooperating player may own it.
 */
    inline bool channel_is_busy(size_t channel_id)
    {
        if (!is_file_mode_[channel_id])
            return false;
        if (channel_is_active(channel_id))
            return true;
        else if (!channel_is_armed(channel_id))
            return true;
        return false;
    }

/**
 * \brief true if the specified channel is ready.
 */
    inline bool channel_is_ready(size_t channel_id)
    {
        // FIXME: validate that this will be multicore safe.
        if (!is_file_mode_[channel_id])
            return false;
        return (!channel_is_active(channel_id)) && channel_is_armed(channel_id);
    }


/**
 * \brief iterate through all channels and update underlying resources.
 * \details if the channel is active, read the next chunk of the file into
 *  the DAC buffer. If not, read only the first chunk of the file
 *  into the DAC buffers so that the DAC is ready to start immediately.
 */
    void update()
    {
        // TODO: deadlne check between SD read iterations to ensure buffers are topped off.
        FRESULT fr;
        UINT bytes_read;
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            // Skip channels not owned by the file player (either never
            // opened or explicitly abandoned to another source).
            if (!is_file_mode_[id])
                continue;
            // Skip if channel is ready but not transferring.
            bool channel_active = channel_is_active(id);
            bool channel_armed = channel_is_armed(id);
            if (!channel_active && channel_armed)
            {
                //printf("not active and armed!\r\n");
                continue;
            }
            // Handle playback-finished or playback-aborted condition
            if (!channel_active && !channel_armed)
            {
                //printf("not active but not armed! needs rewind\r\n");
                file_bufs_[id].reset_transfer_config();
/*
                PIO& pio = dacs_[id].get_pio();
                int32_t sm = dacs_[id].get_sm();
                file_bufs_[id].setup_transfer(timer_pacing_signal_, &pio->txf[sm]);
*/

                f_rewind(&fils_[id]);
            } // Keep going.

            // Skip if we setup last DMA transfer, but it hasn't finished yet.
            if (channel_active && file_bufs_[id].dma_chain_loop_disconnected())
                continue;

            // Skip if channel is active but buffer hasn't switched yet.
            if (idle_buffers_[id] == file_bufs_[id].get_idle_buffer())
                continue;

            idle_buffers_[id] = file_bufs_[id].get_idle_buffer();
            // Transfer data from card to double-buffer.
            fr = f_read(&fils_[id], idle_buffers_[id], SD_CHUNK_SIZE, &bytes_read);
            //printf("fptr: %llu\r\n", fils_[id].fptr);
            if (fr != FR_OK)
                {panic("Could not read data from: %s", filenames_[id]);}
            if (!f_eof(&fils_[id])) // TODO: also handle reading subset of file.
                continue;
            // Handle end-of-file.
             ++curr_iterations_[id]; // increment full file read iterations.
            // Handle last transfer condition.
            if ((curr_iterations_[id] == iterations_[id]) && (iterations_[id] != 0))
            {
                // Next transfer will be the last transfer.
                //printf("EOF at %llu. Setting up last transfer\r\n", fils_[id].fptr);
                file_bufs_[id].setup_last_dma_transfer(bytes_read);
                idle_buffers_[id] = nullptr; // Trigger a re-arm on next update.
                curr_iterations_[id] = 0; // reset counter for next round.
                continue;
            }
            // Handle endless/many-iteration transfer condition.
            f_rewind(&fils_[id]);
            // Pad out the rest of the chunk if we didn't read a full chunk.
            // NOTE: this will be SLOW if remaining chunk is not a multiple of 512.
            //  Figure out how to buffer this so we always read SD card in
            //  multiples of 512.
            if (bytes_read == SD_CHUNK_SIZE);
                continue;
            fr = f_read(&fils_[id], idle_buffers_[id] + bytes_read/sizeof(T),
                        (SD_CHUNK_SIZE - bytes_read), &bytes_read);
        }
    }

private:
    // TODO: mark all data structures as __not_in_flash
    std::array<PIO_LTC264x, NUM_CHANNELS>& dacs_;
    const std::array<const char*, NUM_CHANNELS>& filenames_;
    std::array<T*, NUM_CHANNELS> idle_buffers_;
    std::array<FIL, NUM_CHANNELS> fils_;
    std::array<FIL*, NUM_CHANNELS> filptrs_;
    std::array<size_t, NUM_CHANNELS> iterations_;
    std::array<size_t, NUM_CHANNELS> curr_iterations_;
    std::array<bool, NUM_CHANNELS> is_file_mode_{};
    etl::vector<DMADoubleBuffer<T, BUF_SIZE>, NUM_CHANNELS> file_bufs_;
    std::array<WaveformSettings, NUM_CHANNELS> settings_;
    int dma_timer_chan_;
    dreq_num_t timer_pacing_signal_;

    queue_t end_of_transfer_event_queue_;

    int irq_;

    // TODO: annotate as __not_in_flash_func("mfp_static_members")
    static inline MultiFilePlayer<T, NUM_CHANNELS, BUF_SIZE>* isr_instance_ = nullptr;

    static inline constexpr size_t SD_CHUNK_SIZE = BUF_SIZE * sizeof(T); // in bytes.
};
#endif // MULTI_FILE_PLAYER_H
