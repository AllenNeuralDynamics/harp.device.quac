#ifndef SOURCE_PLAYER_H
#define SOURCE_PLAYER_H
#include "waveform_settings.h"
#include "dma_double_buffer.h"
#include <algorithm>
#include <atomic>

/**
 * \brief Abstract base class for streaming a source waveform to a
 *  DMADoubleBuffer. Ultimately, the DMADoubleBuffer may be connected to an
 *  output like a DAC.
 * \details Derived classes need only implement rewind_source(),
 *  transfer_source_chunk(), source_finished(), and setup() functions and
 *  populate \ref settings_ptr_ in the constructor.
 */
template <typename T, size_t BUF_SIZE>
class SourcePlayer
{
public:
/**
 * \brief constructor.
 */
    SourcePlayer()
    : idle_buf_ptr_{nullptr}, buf_ptr_{nullptr}, curr_cycles_{0},
    settings_ptr_{nullptr}, samples_emitted_{0}, total_samples_emitted_{0},
    chunk_bytes_read_{0}, manage_buffer_timing_{false}, is_updating{false}
    {}

/**
 * \brief destructor
 */
        ~SourcePlayer()
        {cleanup();}

/**
 * \brief claim the TimerPacedDMADoubleBuffer specified. true if successful.
 * \details when apply_settings() is called, the underlying timer of this buffer
 *  is adjusted to match.
 */
    bool claim_buffer(TimerPacedDMADoubleBuffer<T, BUF_SIZE>* buf)
    {
        manage_buffer_timing_ = true; // Flag that we need to deal with timer.
        // Upcast to parent and call the other overload option.
        DMADoubleBuffer<T, BUF_SIZE>* parent_buf =
            static_cast<DMADoubleBuffer<T, BUF_SIZE>*>(buf);
        return claim_buffer(parent_buf);
    }

/**
 * \brief claim the DMADoubleBuffer specified. true if successful.
 */
    bool claim_buffer(DMADoubleBuffer<T, BUF_SIZE>* buf)
    {
        // FIXME: make it so buffer is claimable from the buffer's perspective.
        //if (!buf->claim(this))
        //    return false;
        buf_ptr_ = buf;
        if (settings_ptr_) // shouldn't be nullptr if child was derived correctly.
            return apply_settings(*settings_ptr_);
        return true;
    }

/**
 * \brief apply settings passed in.
 * \details if the claimed buffer manages its own timing via Timer, update the
 *  Timer settings to match.
 * \note child classes should call this function within their own override
 *  implementation.
 * \warning settings can only be applied if the player is not busy playing.
 */
    virtual bool apply_settings(WaveformSettings& settings)
    {
        if ((buf_ptr_ == nullptr) || is_busy())
            return false;
        reset(); // calls rewind_source().
        // Check if buffer manages its own pacing via Timer. If yes, update the
        // Timer to match current settings.
        if (!manage_buffer_timing_)
            return true;
        // reinterpret_cast is safe here bc we know explicitly what buffer class
        // was passed in using the overloaded claim_buffer().
        TimerPacedDMADoubleBuffer<T, BUF_SIZE>* timer_paced_buf_ptr =
            reinterpret_cast<TimerPacedDMADoubleBuffer<T, BUF_SIZE>*>(buf_ptr_);
        timer_paced_buf_ptr->set_frequency_hz(settings.update_frequency_hz);
        return true;
    }

/**
 * \brief unclaim the previously-claimed DMADoubleBuffer
 */
    bool unclaim_buffer()
    {
        if (buf_ptr_ == nullptr)
            return false;
        buf_ptr_ = nullptr;
        idle_buf_ptr_ = nullptr;
        manage_buffer_timing_ = false;
        return true;
    }

    inline bool output_buffer_unspecified()
    {return buf_ptr_ == nullptr;}

/**
 * \brief Clear internal state but do not release claimed resources
 */
    virtual void reset()
    {
        abort_transfer(); // Will deassert is_busy()
        while (is_updating.load()) // wait for core1 update to finish any update.
            __asm__ __volatile__ ("nop");
        total_samples_emitted_ = 0;
        curr_cycles_ = 0;
        samples_emitted_ = 0;
        chunk_bytes_read_ = 0;
        sample_count_ = this->settings_ptr_->sample_count(); // Recompute.
        rewind_source();
    }

/**
 * \brief abort any in-flight transfers and cleanup any internal variables
 *  such that internal state tracking works from update(). Idempotent.
 * \warninng you must call a reset before you can
 */
    void abort_transfer()
    {
        if (!buf_ptr_)
            return;
        buf_ptr_->abort_transfer();
        idle_buf_ptr_ = nullptr;
    }

/**
 * \brief
 */
    virtual void setup()
    {
        reset();
        // pre-read buffers (if buffer is claimed).
        if (!buf_ptr_)
            return;
        // Continue calling update until the buffer is fully stuffed.
        while (!SourcePlayer<T, BUF_SIZE>::is_armed())
            SourcePlayer<T, BUF_SIZE>::update();
    }

/**
 * \brief release claimed resources.
 * \details Derived classes should release any other resources by extending
 * this function in the child class.
 */
    virtual void cleanup()
    {
        abort_transfer();
        unclaim_buffer();
    }

/**
 * \brief true if the channel's buffer has been pre-filled and the underlying
 *  DMA channel can start draining it immediately.
 */
    inline bool is_armed()
    {
        // Edge Cases:
        // -> Endless source  & unbounded duration ("play forever").
        //    sample_count_ is 0, and cycles never increment.
        // -> Finite source and & unbounded duration ("play to completion.").
        //    sample_count is 0, and cycles increments when source is fully read.
        // -> Number of total samples to emit is less than the full buffer.
        //    Here, calling update() once calls setup_last_dma_transfer().
        // -> Source requires multiple calls to transfer_source_chunk() to fill
        //    up the buffer (i.e: a short source that needs to be rewind()ed
        //    multiple times).
        // -> Player has played at least once and needs to be reset() before
        //    we can rerun it.

        // Bail-early case: buffer is fully stuffed.
        if (total_samples_emitted_ >= BUF_SIZE)
            return true;
        // Play-forever or play-to-completion case.
        if (sample_count_ == 0)
        {
            // Short finite source that's smaller than the buffer.
            // i.e: play-to-completion.
            if (curr_cycles_ == settings_ptr_->cycles) // i.e: source finished.
                return true;
        }
        // Bounded sample count and finite source that is also short.
        else if (sample_count_ < BUF_SIZE)
            return total_samples_emitted_ == sample_count_;
        return false; // shouldn't happen unless there are cases we haven't found.
    }

/**
 * \brief true if channel is transferring data to its respective output (DAC,
 *  etc). False otherwise (paused or aborted).
 */
    inline bool is_active()
    {
        if (!buf_ptr_)
            return false;
        return buf_ptr_->is_transferring();
    }

/**
 * \brief true if the specified channel needs to be handled with periodic calls
 *  to update(). Alias for is_active().
 */
    inline bool is_busy()
    {return is_active();}

/**
 * \brief true if the player is ready to start transferring immediately.
 */
    inline bool is_ready()
    {
        // FIXME: validate that this will be multicore safe.
        return (!is_active()) && is_armed();
    }

/**
 * \brief tick the buffer-stuffing process.
 * \details if active, read the next chunk of the source (file, equation, etc.)
 *  into the buffer. If not active but the source is known, read only the
 *  first source chunk into the buffer so that the buffer output is ready to
 *  start immediately, i.e: "arm the buffer." Idempotent.
 */
    void update()
    {
        is_updating.store(true);
        _update();
        is_updating.store(false);
    }

    inline void _update()
    {
        // TODO: deadlne check between chunk transfers to ensure buffers are topped off.
        size_t chunk_samples_read;
        size_t bytes_to_read;
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
        // Read up to a chunk or up to the subset specified.
        bytes_to_read = (sample_count_ == 0) ?
            CHUNK_SIZE_BYTES:
            std::min(size_t((sample_count_ - samples_emitted_) * sizeof(T)),
                     CHUNK_SIZE_BYTES);
        transfer_source_chunk(idle_buf_ptr_, bytes_to_read, chunk_bytes_read_);
        chunk_samples_read = chunk_bytes_read_ / sizeof(T);
        samples_emitted_ += chunk_samples_read;
        total_samples_emitted_ += chunk_samples_read;
        bool source_subset_finished = ((samples_emitted_ == sample_count_)
                                       && (sample_count_ != 0));
        if (!source_finished() && !source_subset_finished)
            return;
        // Handle end-of-source (or end of subset of source).
         ++curr_cycles_; // increment full source read iterations.
        // Handle last transfer condition.
        if ((curr_cycles_ == settings_ptr_->cycles) && (settings_ptr_->cycles != 0))
        {
            // Next transfer will be the last transfer.
            //printf("EOF at %llu. Setting up last transfer\r\n", fil_.fptr);
            // Current idle buffer is the last active buffer
            buf_ptr_->setup_last_dma_transfer(chunk_bytes_read_ / sizeof(T));
            idle_buf_ptr_ = nullptr; // Trigger a re-arm on next update.
            curr_cycles_ = 0; // reset counter for next round.
            samples_emitted_ = 0;
            return;
        }
        // Handle endless/many-iteration transfer condition.
        rewind_source();
        // Pad out the rest of the chunk if we didn't read a full chunk.
        if (chunk_bytes_read_ == CHUNK_SIZE_BYTES)
            return;
        transfer_source_chunk(idle_buf_ptr_ + chunk_bytes_read_/sizeof(T),
                              (CHUNK_SIZE_BYTES - chunk_bytes_read_),
                              chunk_bytes_read_);
        chunk_samples_read = chunk_bytes_read_ / sizeof(T);
        samples_emitted_ += chunk_samples_read;
        total_samples_emitted_ += chunk_samples_read;
    }

protected:
/**
 * \brief rewind the source (move to start-of-file, set t=0 on an equation,
 *  etc.) such that it is ready to be played again from the beginning.
 * \details should be idempotent even if the source is unavailable.
 */
    inline virtual void rewind_source() = 0;

/**
 * \brief transfer bytes from source to the address specified in \p dest.
 */
    inline virtual void transfer_source_chunk(T* dest, size_t num_bytes,
                                              size_t& bytes_transferred) = 0;

/**
 * \brief true if the source (file, waveform, etc) can or should no longer
 *  produce samples under the current settings without a call to rewind() or
 *  reset() first.
 * \details examples of how this could be true include:
 * - if the source is a file, the file has been read to completion.
 * - if the current \ref WaveformSettings specify a fixed `duration_us`, and
 *   producing more samples would violate the settings.
 */
    inline virtual bool source_finished() = 0;

    uint32_t total_samples_emitted_;
    size_t curr_cycles_; /// elapsed cycles of the specified settings
                         /// (NOT cycles of a periodic waveform).
    uint32_t samples_emitted_; /// samples emitted within a cycle. resets to 0.
                               /// upon reset() or at the beginning of each
                               /// cycle.
    uint32_t sample_count_; // Cached value. Recomputed on reset().
    size_t chunk_bytes_read_; /// bytes read after the most recent call to
                              /// transfer_source_chunk().
    DMADoubleBuffer<T, BUF_SIZE>* buf_ptr_;
    WaveformSettings* settings_ptr_;
    std::atomic<bool> is_updating;  // should be in RAM

    static inline constexpr size_t CHUNK_SIZE_BYTES = BUF_SIZE * sizeof(T); // bytes

private:
    bool manage_buffer_timing_;
    T* idle_buf_ptr_;
};
#endif // SOURCE_PLAYER_H
