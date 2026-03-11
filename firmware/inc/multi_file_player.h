#ifndef MULTI_FILE_PLAYER_H
#define MULTI_FILE_PLAYER_H
#include <waveform_settings.h>
#include <pio_ltc264x.h>
#include <dma_double_buffer.h>
#include <ff.h>
#include <f_util.h>
#include <pico/stdlib.h>
#include <etl/vector.h>
#include <cmath>

template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class MultiFilePlayer
{
public:

/**
 * \brief constructor.
 * \param dacs
 * \param filenames
 */
    MultiFilePlayer(std::array<PIO_LTC264x, NUM_CHANNELS>& dacs,
                    const std::array<const char*, NUM_CHANNELS>& filenames)
    : dacs_{dacs}, filenames_{filenames}, dma_timer_chan_{-1}
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
        std::ranges::fill(filptrs_, nullptr); // mark files as starting closed.
        std::ranges::fill(curr_iterations_, 0);
        std::ranges::fill(iterations_, 1);
        reset();
    }

/**
 * \brief setup the file reading loop
 */
    void setup()
    {
        // Open 4 files. Note: max number is set by FF_FS_LOCK in ffconf.h
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if (file_is_open(id))
                continue;
            open_file(id);
        }
        // pre-read buffers.
        update();
    }

/**
 * \brief
 * \warning not multicore safe.
 */
    void reset()
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
            file_bufs_[i].abort_transfer(); // Do this first across all channels.
        // TODO: maybe validate that the abort took place?
        for (auto& dac: dacs_)
            dac.write_value(OUTPUT_MIDSCALE);
        std::ranges::fill(idle_buffers_, nullptr); // Clear local buffer value.
        std::ranges::fill(curr_iterations_, 0);
        cleanup(); // close all files.
    }

/**
 * \brief destructor
 */
    ~MultiFilePlayer()
    {cleanup();}

/**
 * \brief
 */
    inline bool file_is_open(size_t file_index)
    {return filptrs_[file_index] != nullptr;}

    inline void open_file(size_t file_index)
    {
        FRESULT fr = f_open(&fils_[file_index], filenames_[file_index], FA_READ);
        if (fr != FR_OK)
        {panic("Could not open: %s\r\n", filenames_[file_index]);}
        filptrs_[file_index] = &fils_[file_index]; // set ptr to indicate open file.
    }

    inline void close_file(size_t file_index)
    {
        FRESULT fr = f_close(&fils_[file_index]);
        if (fr != FR_OK)
            panic("Could not close: %s\r\n.", filenames_[file_index]);
        filptrs_[file_index] = nullptr; // clear ptr to indicate closed file.
    }

/**
 * \brief close all open files.
 */
    void cleanup()
    {
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if (!file_is_open(id))
                continue;
            close_file(id);
        }
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
            if (channel_is_active(id))
                return true;
            else if (!channel_is_armed(id))
                return true;
        }
        return false;
    }

/**
 * \brief true if the specified channel is ready.
 */
    inline bool channel_is_ready(size_t channel_id)
    {
        // FIXME: validate that this will be multicore safe.
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
    etl::vector<DMADoubleBuffer<T, BUF_SIZE>, NUM_CHANNELS> file_bufs_;
    std::array<WaveformSettings, NUM_CHANNELS> settings_;
    int dma_timer_chan_;
    dreq_num_t timer_pacing_signal_;

    static inline constexpr size_t SD_CHUNK_SIZE = BUF_SIZE * sizeof(T); // in bytes.
    static inline constexpr size_t DEFAULT_FREQUENCY_HZ = 500000;
    static inline constexpr uint16_t OUTPUT_MIDSCALE = 32768;
};
#endif // MULTI_FILE_PLAYER_H
