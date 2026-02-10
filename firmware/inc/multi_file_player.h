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
                    std::array<const char*, NUM_CHANNELS>& filenames)
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
        reset(); // Open files; reset the buffer loop.
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
        // reset all file pointers.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
            idle_buffers_[i] = nullptr; // Clear local buffer value.
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
            panic("Could not close: fils_[%s].", i);
        filptrs_[file_index] = nullptr; // clear ptr to indicate closed file.
    }

/**
 * \brief close all open files.
 */
    void cleanup()
    {
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            if !file_is_open(id)
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
        uint32_t SYS_CLOCK_HZ = SYS_CLOCK_MHZ * 1'000'000;
        float divisor = float(SYS_CLOCK_HZ) / frequency_hz;
        if (round(divisor) != divisor)
        {panic("Update frequency (%f [Hz]) must be a multiple of sys clock: %d",
                SYS_CLOCK_MHZ);}
        // TODO: enable more flexible pacing options by allocating timers
        //  on-demand and sharing timers for matching frequencies, and
        //  respecting max number of used timers.
        //  Requires re-attaching timers to buffers.
        dma_timer_set_fraction(dma_timer_chan_, 1, divisor);
    }

/**
 * \brief start one or more channels specified as bitfields.
 * \details multiple channels started this way will be started concurrently.
 */
    void start(uint32_t channel_mask)
    {
        // TODO: maybe make this fn return a bool in case we're not ready (armed)?
        // Create a trigger mask to start all Double Buffer DMA channels at once.
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
    inline bool channel_is_armed(size_t channel_index)
    {
        //  we can't strictly rely on an nonzero file read pointer
        //  (i.e: `f_tell(fils_[id] != 0`) because the overall file size may be
        //  less than the buffer size, so it would be constantly reset to 0.
        return (idle_buffers_[channel_index] != nullptr) &&
               (!file_bufs_[channel_index].is_aborted());
    }

/**
 * \brief true if channel is transferring data to its respective DAC.
 *  False otherwise (paused or aborted).
 */
    void channel_is_active(size_t channel_index)
    {return file_bufs_[channel_index].is_transferring();}

/**
 * \brief true if any channels are active.
 */
    bool is_busy()
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if (channel_is_active(i))
                return true;
        }
        return false;
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
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            FRESULT fr;
            // Skip if channel is ready but not transferring.
            if (!channel_is_active(id) && channel_is_armed(id))
                continue;
            // Handle playback-finished or playback-aborted condition
            if (!channel_is_active(id) && !channel_is_armed(id))
            {
                file_bufs_[id].reset_transfer_config();
                f_rewind(&fils_[id]);
            } // Keep going.
            // Skip if channel is active but buffer hasn't switched yet.
            if (idle_buffers_[id] == file_bufs_[id].get_idle_buffer())
                continue;

            idle_buffers_[id] = file_bufs_[id].get_idle_buffer();
            // Transfer data from card to double-buffer.
            fr = f_read(&fils_[id], idle_buffers_[id], SD_CHUNK_SIZE, &bytes_read);
            if (fr != FR_OK)
                {panic("Could not read the data: %s", filenames_[id]);}
            if (!f_eof(&fils_[id])) // TODO: also handle reading subset of file.
                continue;
            // Handle end-of-file.
             ++curr_iterations_[i]; // increment full file read iterations.
            // Handle last transfer condition.
            if ((curr_iterations_[id] == iterations_[id]) && (iterations_[id] != 0)
            {
                // Next transfer will be the last transfer.
                file_bufs_[id].setup_last_dma_transfer(bytes_read);
                idle_buffers_[id] = nullptr; // Trigger a re-arm on next update.
                //f_rewind(&fils_[id]); // gets handled on next iteration.
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
    std::array<const char*, NUM_CHANNELS>& filenames_;
    std::array<T*, NUM_CHANNELS> idle_buffers_;
    std::array<FIL, NUM_CHANNELS> fils_;
    std::array<FIL*, NUM_CHANNELS> filptrs_;
    etl::vector<DMADoubleBuffer<T, BUF_SIZE>, NUM_CHANNELS> file_bufs_;
    std::array<WaveformSettings, NUM_CHANNELS> settings_;
    int dma_timer_chan_;
    dreq_num_t timer_pacing_signal_;

    static inline constexpr size_t SD_CHUNK_SIZE = BUF_SIZE * sizeof(T); // in bytes.
    static inline constexpr size_t DEFAULT_FREQUENCY_HZ = 500000;
    static inline constexpr uint16_t OUTPUT_MIDSCALE = 32768;
};
#endif // MULTI_FILE_PLAYER_H
