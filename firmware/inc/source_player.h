#ifndef SOURCE_PLAYER_H
#define SOURCE_PLAYER_H
#include "waveform_settings.h"
#include "dma_double_buffer.h"

/**
 * \brief Abstract base class for streaming a source waveform to a
 *  DMADoubleBuffer. Ultimately, the DMADoubleBuffer may be connected to an
 *  output like a DAC.
 * \details Derived classes need only implement rewind_source(),
 *  transfer_source_chunk(), and source_finished(), functions and populate
 *  settings_ptr_ in the constructor.
 */
template <typename T, size_t BUF_SIZE>
class SourcePlayer
{
public:
/**
 * \brief constructor.
 */
    SourcePlayer(DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : idle_buf_ptr_{nullptr}, buf_ptr_{buf_ptr}, curr_cycles_{0},
      settings_ptr_{nullptr}
    {
        if (buf_ptr != nullptr)
            claim_buffer(buf_ptr);
    }

/**
 * \brief destructor
 */
        ~SourcePlayer()
        {cleanup();}

/**
 * \brief claim the DMADoubleBuffer specified. true if successful.
 */
    bool claim_buffer(DMADoubleBuffer<T, BUF_SIZE>* buf)
    {
        // FIXME: make it so buffer is claimable.
        //if (!buf->claim(this))
        //    return false;
        buf_ptr_ = buf;
        // TODO: apply the waveform frequency settings?
        return true;
    }

/**
 * \brief unclaim the previously-claimed DMADoubleBuffer
 */
    bool unclaim_buffer()
    {
        if (buf_ptr_ == nullptr)
            return false;
        //buf_ptr_->unclaim();
        buf_ptr_ = nullptr;
        idle_buf_ptr_ = nullptr;
        return true;
    }

    // FIXME: consider reading from dma double buffer directly.
    inline bool output_buffer_unspecified()
    {return buf_ptr_ == nullptr;}

/**
 * \brief Clear internal state but do not release claimed resources
 */
    virtual void reset()
    {
        curr_cycles_ = 0;
        idle_buf_ptr_ = nullptr;
        rewind_source();
    }

/**
 * \brief release claimed resources.
 * \details Derived classes should release any other resources by extending
 * this function in the child class.
 */
    virtual void cleanup()
    {
        if (buf_ptr_ != nullptr)
            buf_ptr_->abort_transfer();
        unclaim_buffer();
    }

/**
 * \brief set waveform settings.
 * \note settings can only be altered if the player is not active otherwise
 *  this function is not multicore safe.
 */
    bool apply_settings(WaveformSettings& settings)
    {
        // FIXME: implement this.
        // TODO: update the double buffer pacing settings?
        *settings_ptr_ = settings;
    }

/**
 * \brief return a read-only reference to the current settings
 */
    const WaveformSettings& get_settings() const
    {return *settings_ptr_;}

/**
 * \brief true if the channel's buffer has been pre-filled and the underlying
 *  DMA channel can start draining it immediately.
 */
    inline bool is_armed()
    {
        //  we can't strictly rely on an nonzero file read pointer
        //  (i.e: `f_tell(fil_ != 0`) because the overall file size may be
        //  less than the buffer size, so it would be constantly reset to 0.
        return (idle_buf_ptr_ != nullptr) && (!buf_ptr_->is_aborted());
    }

/**
 * \brief true if channel is transferring data to its respective output (DAC,
 *  etc). False otherwise (paused or aborted).
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
 * \brief true if the player is ready to start transferring immediately.
 */
    inline bool is_ready()
    {
        // FIXME: validate that this will be multicore safe.
        return (!is_active()) && is_armed();
    }

/**
 * \brief rewind the source (move to start-of-file, set t=0 on an equation,
 *  etc.) such that it is ready to be played again from the beginning.
 */
    inline virtual void rewind_source() = 0;

/**
 * \brief transfer bytes from source to the address specified in \p dest.
 */
    inline virtual void transfer_source_chunk(T* dest, size_t num_bytes,
                                              size_t& bytes_transferred) = 0;

/**
 * \brief
 */
    inline virtual bool source_finished() = 0;


/**
 * \brief tick the buffer-stuffing process.
 * \details if active, read the next chunk of the source (file, equation, etc.)
 *  into the buffer. If not active but the source is known, read only the
 *  first source chunk into the buffer so that the buffer output is ready to
 *  start immediately, i.e: "arm the buffer." Idempotent.
 */
    void update()
    {
        // TODO: deadlne check between chunk transfers to ensure buffers are topped off.
        size_t bytes_read;
        if (output_buffer_unspecified())
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
            rewind_source();
        } // Keep going.
        // Skip if we setup last DMA transfer, but it hasn't finished yet.
        if (active && buf_ptr_->dma_chain_loop_disconnected())
            return;
        // Skip if channel is active but buffer hasn't switched yet.
        if (idle_buf_ptr_ == buf_ptr_->get_idle_buffer())
            return;
        idle_buf_ptr_ = buf_ptr_->get_idle_buffer();
        // Transfer data from card to double-buffer.
        transfer_source_chunk(idle_buf_ptr_, CHUNK_SIZE_BYTES, bytes_read);
        if (!source_finished()) // TODO: also handle reading subset of file.
            return;
        // Handle end-of-file.
         ++curr_cycles_; // increment full file read iterations.
        // Handle last transfer condition.
        if ((curr_cycles_ == settings_ptr_->cycles) && (settings_ptr_->cycles != 0))
        {
            // Next transfer will be the last transfer.
            //printf("EOF at %llu. Setting up last transfer\r\n", fil_.fptr);
            buf_ptr_->setup_last_dma_transfer(bytes_read);
            idle_buf_ptr_ = nullptr; // Trigger a re-arm on next update.
            curr_cycles_ = 0; // reset counter for next round.
            return;
        }
        // Handle endless/many-iteration transfer condition.
        rewind_source();
        // Pad out the rest of the chunk if we didn't read a full chunk.
        if (bytes_read == CHUNK_SIZE_BYTES)
            return;
        transfer_source_chunk(idle_buf_ptr_ + bytes_read/sizeof(T),
                              (CHUNK_SIZE_BYTES - bytes_read), bytes_read);
    }

protected:
    T* idle_buf_ptr_;
    size_t curr_cycles_;
    DMADoubleBuffer<T, BUF_SIZE>* buf_ptr_;
    WaveformSettings* settings_ptr_;

    static inline constexpr size_t CHUNK_SIZE_BYTES = BUF_SIZE * sizeof(T); // bytes
};
#endif // SOURCE_PLAYER_H
