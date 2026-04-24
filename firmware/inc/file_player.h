#ifndef FILE_PLAYER_H
#define FILE_PLAYER_H
#include <waveform_settings.h>
#include <pio_ltc264x.h>
#include <dma_double_buffer.h>
#include <ff.h>
#include <f_util.h>
#include <pico/stdlib.h>
#include <pico/util/queue.h>
#include <etl/vector.h>
#include <limits>

/**
 * \brief class for streaming a single waveform file a DAC via DMADoubleBuffer.
 */
template <typename T, size_t BUF_SIZE>
class FilePlayer
{
public:
    static inline constexpr size_t DEFAULT_FREQUENCY_HZ = 500000;
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max()/2;
    static inline constexpr size_t DEFAULT_QUEUE_SIZE = 32;

/**
 * \brief constructor.
 * \param dac
 */
    FilePlayer(PIO_LTC264x& dac, DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : dac_{dac}, dma_timer_chan_{-1}, filptr_{nullptr},
    idle_buffer_{nullptr}, curr_iterations_{0}, iterations_{1}, buf_ptr_{buf_ptr}
    {
        // FIXME: Connect buffer address.
        //PIO& pio = dac_.get_pio();
        //int32_t sm = dac_.get_sm();
        reset();
        if (buf_ptr != nullptr)
            claim_buffer(buf_ptr);
    }

    bool claim_buffer(DMADoubleBuffer<T, BUF_SIZE>* buf)
    {
        // FIXME: make is so buffer is claimable.
        //if (!buf->claim(this))
        //    return false;
        buf_ptr_ = buf;
        return true;
    }

    bool unclaim_buffer()
    {
        if (buf_ptr_ == nullptr)
            return false;
        //buf_ptr_->unclaim();
        buf_ptr_ = nullptr;
        return true;
    }

/**
 * \brief Reset internal variables and close file if opened.
 * \warning not multicore safe.
 */
    void reset()
    {
        cleanup(); // close all files.
        dac_.write_value(OUTPUT_MIDSCALE);
    }

/**
 * \brief destructor
 */
    ~FilePlayer()
    {cleanup();}

/**
 *  \brief open the previously specified file.
 *  \details idempotent.
 */
    inline void open_file(const char* filename)
    {
        if (file_is_open())
        {close_file();}
        if (f_open(&fil_, filename, FA_READ) != FR_OK)
        {panic("Could not open: %s\r\n", filename);}
        filptr_ = &fil_; // set ptr to indicate open file.
        // pre-read buffers.
        update();
    }

/**
*  \brief close any previously opened file.
*/
    inline void close_file()
    {
        if (filptr_ != nullptr)
            f_close(&fil_);
        filptr_ = nullptr; // clear ptr to indicate closed file.
        idle_buffer_ = nullptr; // Clear local buffer value.
        curr_iterations_ = 0;
        iterations_ = 1;
    }

/**
 * \brief true if this class currently has any file open.
 */
    inline bool file_is_open()
    {return filptr_ != nullptr;}

/**
 * \brief release claimed resources.
 */
    void cleanup()
    {
        if (buf_ptr_ != nullptr)
            buf_ptr_->abort_transfer();
        if (file_is_open())
            close_file();
        unclaim_buffer();
    }

/**
 * \brief
 * \warning will not work if file streaming has already started.
 */
    // FIXME: consider making this return a bool and check if channel is active.
    inline void set_channel_iterations(size_t iterations)
    {iterations_ = iterations;}

/**
 * \brief set waveform_settings
 */
    bool apply_settings(WaveformSettings& settings)
    {
        // FIXME: implement this.
        return false;
    }

/**
 * \brief true if the channel's buffer has been filled and the underlying
 *  DMA channel can start draining it immediately.
 */
    inline bool is_armed()
    {
        //  we can't strictly rely on an nonzero file read pointer
        //  (i.e: `f_tell(fil_ != 0`) because the overall file size may be
        //  less than the buffer size, so it would be constantly reset to 0.
        return (idle_buffer_ != nullptr) && (!buf_ptr_->is_aborted());
    }

/**
 * \brief true if channel is transferring data to its respective DAC.
 *  False otherwise (paused or aborted).
 */
    inline bool is_active()
    {return buf_ptr_->is_transferring();}

/**
 * \brief true if the specified channel needs to be handled with periodic calls
 *  to update().
 */
    inline bool is_busy()
    {
        if (is_active())
            return true;
        else if (!is_armed())
            return true;
        return false;
    }

/**
 * \brief true if the specified channel is ready.
 */
    inline bool is_ready(size_t channel_id)
    {
        // FIXME: validate that this will be multicore safe.
        return (!is_active()) && is_armed();
    }

/**
 * \brief tick the file reading process.
 * \details if active, read the next chunk of the file into
 *  the DAC buffer. If not but the file is known, read only the first
 *  chunk of the file
 *  into the DAC buffer so that the DAC is ready to start immediately.
 *  Idempotent.
 */
    void update()
    {
        // TODO: deadlne check between SD read iterations to ensure buffers are topped off.
        FRESULT fr;
        UINT bytes_read;
        // Skip if buffer is not specified.
        if (buf_ptr_ == nullptr)
            return;
        // Skip if channel is ready but not transferring.
        bool active = is_active();
        bool armed = is_armed();
        if (!active && armed)
            return;
        // Handle playback finished or aborted condition (i.e: needs rewind).
        if (!active && !armed)
        {
            buf_ptr_->reset_transfer_config();
            f_rewind(&fil_);
        } // Keep going.
        // Skip if we setup last DMA transfer, but it hasn't finished yet.
        if (active && buf_ptr_->dma_chain_loop_disconnected())
            return;
        // Skip if channel is active but buffer hasn't switched yet.
        if (idle_buffer_ == buf_ptr_->get_idle_buffer())
            return;
        idle_buffer_ = buf_ptr_->get_idle_buffer();
        // Transfer data from card to double-buffer.
        fr = f_read(&fil_, idle_buffer_, SD_CHUNK_SIZE, &bytes_read);
        //printf("fptr: %llu\r\n", fil_.fptr);
        if (fr != FR_OK)
            {panic("Could not read data from file!\r\n");}
        if (!f_eof(&fil_)) // TODO: also handle reading subset of file.
            return;
        // Handle end-of-file.
         ++curr_iterations_; // increment full file read iterations.
        // Handle last transfer condition.
        if ((curr_iterations_ == iterations_) && (iterations_ != 0))
        {
            // Next transfer will be the last transfer.
            //printf("EOF at %llu. Setting up last transfer\r\n", fil_.fptr);
            buf_ptr_->setup_last_dma_transfer(bytes_read);
            idle_buffer_ = nullptr; // Trigger a re-arm on next update.
            curr_iterations_ = 0; // reset counter for next round.
            return;
        }
        // Handle endless/many-iteration transfer condition.
        f_rewind(&fil_);
        // Pad out the rest of the chunk if we didn't read a full chunk.
        // NOTE: this will be SLOW if remaining chunk is not a multiple of 512.
        //  Figure out how to buffer this so we always read SD card in
        //  multiples of 512.
        if (bytes_read == SD_CHUNK_SIZE)
            return;
        fr = f_read(&fil_, idle_buffer_ + bytes_read/sizeof(T),
                    (SD_CHUNK_SIZE - bytes_read), &bytes_read);
    }

private:
    PIO_LTC264x& dac_;
    T* idle_buffer_;
    FIL fil_;
    FIL* filptr_;
    size_t iterations_;
    size_t curr_iterations_;
    DMADoubleBuffer<T, BUF_SIZE>* buf_ptr_;
    WaveformSettings settings_;
    int dma_timer_chan_;
    dreq_num_t timer_pacing_signal_;

    static inline constexpr size_t SD_CHUNK_SIZE = BUF_SIZE * sizeof(T); // bytes
};
#endif // FILE_PLAYER_H
