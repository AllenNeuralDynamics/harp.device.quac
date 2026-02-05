#ifndef MULTI_FILE_PLAYER_H
#define MULTI_FILE_PLAYER_H
#include <waveform_settings.h>
#include <pio_ltc264x.h>
#include <dma_double_buffer.h>
#include <ff.h>
#include <f_util.h>
#include <pico/stdlib.h>
#include <etl/vector.h>

template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class MultiFilePlayer
{
public:

/**
 * \brief constructor.
 */
    MultiFilePlayer(std::array<PIO_LTC264x, NUM_CHANNELS>& dacs,
                    std::array<const char*, NUM_CHANNELS>& filenames)
    : dacs_{dacs}, filenames_{filenames}
    {
        // TODO: setup pacing.

        // Setup the file system.
        FRESULT fr = f_mount(&filesystem_, "", 1);
        if (fr != FR_OK)
        {panic("f_mount error: %s (%d)\r\n", FRESULT_str(fr), fr);}

        // Setup timer.
        // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
        int dma_timer_chan = dma_claim_unused_timer(true);
        //printf("Claimed DMA Timer %d.\r\n", dma_timer_chan);
        dma_timer_set_fraction(dma_timer_chan, 1, 300); // numerator=1, denominator=300
        dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan));

        // Setup Double buffers.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            PIO& pio = dacs[i].get_pio();
            int32_t sm = dacs[i].get_sm();
            file_bufs_.emplace_back(pacing_signal, &pio->txf[sm]);
        }
        // Open 4 files. Note: max number is set by FF_FS_LOCK in ffconf.h
        for (const auto& id: active_channels_)
        {
            fr = f_open(&fil[id], filenames[id], FA_READ);
            if (fr != FR_OK)
            {panic("Could not open: %s\r\n", filename[id]);}
        }
        // Fill idle buffers with nullptr
        std::ranges::fill(idle_buffers_, nullptr);
    }

/**
 * \brief destructor
 */
    ~MultiFilePlayer()
    {
        // Close all files.
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            fr = f_close(fils_[i]);
            if (fr != FR_OK)
                panic("Could not close: fils_[%s].", i);
        }
        // unmount the file system.
        f_unmount("");
    }

/**
 * \brief start one or more channels specified as bitfields.
 * \details multiple channels started this way will be started concurrently.
 */
    void start(uint32_t channel_mask)
    {
        // FIXME: maybe make this fn return a bool in case we're not ready (armed)?

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
        // FIXME.
    }

    void abort(uint32_t channel_mask);
    {
        // FIXME.
    }

/**
 * \brief entire SD->DAC setup. Additionally set the DAC outputs to 0[V].
 */
    void reset()
    {
        // FIXME.
    }

/**
 * \brief attach an interrupt
 */
    void attach_waveform_finished_interrupt()
    {
        // TODO
    }

/**
 * \brief arm channel or channels by pre-reading SD Card into the
 * first DMA buffer.
 * \warning not multicore safe.
 */
    bool is_armed(uint32_t channel_mask)
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            if (channel_mask & (1u << i))  // Check specific channels
            {
                // FIXME: edge case if file is exactly 1 CHUNK_SIZE long and
                // has been pre-read but was reset to 0 for the next file
                // iteration.
                if (f_tell(fils_[i]) == 0)
                    return false;
            }
        }
        return true;
    }


    void channel_is_active(size_t channel_index)
    {return file_bufs_[channel_index].is_transferring();}


/**
 * \brief iterate through multichannel read loop.
 * \details if the channel is active, read the next chunk of the file into
 *  the DAC buffers. If not, read only the first chunk of the file
 *  into the DAC buffers so that the DAC is ready to start immediately.
 */
    void update()
    {
        // TODO: deadlne check between SD read iterations to ensure buffers are topped off.
        for (size_t id = 0; id < NUM_CHANNELS; ++id)
        {
            // Skip if channel is not reading and doesn't need to be armed.
            // FIXME: edge case if file is exactly 1 CHUNK_SIZE long.
            if (!channel_is_active(id) && f_tell(fils_[id]) != 0)
                continue;
            // Skip if buffer hasn't switched yet.
            if (idle_buffers_[id] == file_bufs_[id].get_idle_buffer())
                continue;

            idle_buffers_[id] = file_bufs_[id].get_idle_buffer();
            // Transfer data from card to double-buffer.
            fr = f_read(&fils_[id], idle_buffers_[id], SD_CHUNK_SIZE, &bytes_read);
            if (fr != FR_OK)
                {panic("Could not read the data: %s", filenames[id]);}
            if (!f_eof(&fils_[id])) // TODO: also handle reading subset of file.
                continue;
            // Handle end-of-file.
             ++curr_iterations_[i]; // increment full file read iterations.
            // Handle last transfer condition.
            if ((curr_iterations_[id] == iterations_[id]) && (iterations_[id] != 0)
            {
                //printf("Setting up last transfer for file %d!\r\n", id);
                // Next transfer will be the last transfer.
                file_bufs_[id].setup_last_dma_transfer(bytes_read);
                idle_buffers_[id] = nullptr;
                f_lseek(&fils_[id], 0);
                continue;
            }
            // Seek to the start of the file.
            f_lseek(&fils_[id], 0);
            // Pad out the rest of the chunk if we didn't finish reading.
            // FIXME: this will be slow if it's not a multiple of 512.
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
    FATFS filesystem_;
    etl::vector<DMADoubleBuffer<T, BUF_SIZE>, NUM_CHANNELS> file_bufs_;
    std::array<FIL, NUM_CHANNELS> __not_in_flash("file_handlers") fils_;
    std::array<WaveformSettings, NUM_CHANNELS> settings_;

    inline constexpr size_t SD_CHUNK_SIZE = BUF_SIZE * sizeof(T); // in bytes.
};
#endif // MULTI_FILE_PLAYER_H
