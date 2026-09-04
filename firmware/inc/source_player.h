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
    chunk_bytes_read_{0}, manage_buffer_timing_{false}, is_updating{false},
    is_armed_{false}
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
        manage_buffer_timing_ = true;
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
 * \brief apply settings passed in and update settings to reflect what is
 *  realistically achieveable with hardware limitations.
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
        // Check if buffer manages its own pacing via Timer. If yes, update the
        // Timer to match current settings.
        if (!manage_buffer_timing_)
        {
            reset(); // calls rewind_source().
            return true;
        }
        // reinterpret_cast is safe here bc we know explicitly what buffer class
        // was passed in using the overloaded claim_buffer().
        TimerPacedDMADoubleBuffer<T, BUF_SIZE>* timer_paced_buf_ptr =
            reinterpret_cast<TimerPacedDMADoubleBuffer<T, BUF_SIZE>*>(buf_ptr_);
        // Update settings passed in to reflect the actual frequency achievable.
        settings_ptr_->update_frequency_hz =
            timer_paced_buf_ptr->set_frequency_hz(settings.update_frequency_hz);
        // Update input reference.
        settings.update_frequency_hz = settings_ptr_->update_frequency_hz;
        reset(); // calls rewind_source().
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
        if (buf_ptr_ != nullptr)
            buf_ptr_->reset_transfer_config(false); // Don't reset buffer tracking.
        while (is_updating.load()) // wait for core1 update to finish any update.
            __asm__ __volatile__ ("nop");
        is_armed_ = false;
        idle_buf_ptr_ = nullptr;
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

        // Once armed, always armed unless reset.
        return is_armed_;
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
        gpio_put(33, 1);
        _update();
        gpio_put(33, 0);
        is_updating.store(false);
    }

    inline void _update()
    {
        // TODO: deadlne check between chunk transfers to ensure buffers are topped off.
        if (output_buffer_unspecified())
            return;
        // Skip if channel is ready but not transferring.
        if (!is_active() && is_armed())
            return;
        if (buf_ptr_->last_transfer_configured())
        {
            gpio_put(34, 1);
            return;
        }
        gpio_put(34, 0);
        T* curr_idle_buf_ptr_ = buf_ptr_->get_idle_buffer();
        // Reset chunk index tracking as soon as we switch buffers.
        if (idle_buf_ptr_ != curr_idle_buf_ptr_)
            chunk_bytes_read_ = 0;
        // Skip if channel is active but buffer hasn't switched yet & the idle
        // buffer is full.
        if (idle_buf_ptr_ == curr_idle_buf_ptr_ && !remaining_buf_size())
            return;
        // Save buffer ptr to track when buffers switch next.
        idle_buf_ptr_ = curr_idle_buf_ptr_;
        // Transfer data from source to idle buffer.
        // Continue reading up to a full chunk or up to the subset specified.
        // if sample_count_ is 0, read forever or up to the end of the source.
        size_t bytes_to_read = (sample_count_ == 0)?
            remaining_buf_size_bytes():
            std::min(size_t((sample_count_ - samples_emitted_)*sizeof(T)),
                     remaining_buf_size_bytes());
        if (bytes_to_read == 0)
            return;
        size_t bytes_transferred;
        transfer_source_chunk(idle_buf_ptr_ + chunk_index(), bytes_to_read,
                              bytes_transferred);
        chunk_bytes_read_ += bytes_transferred;
        size_t samples_transferred = bytes_transferred / sizeof(T);
        samples_emitted_ += samples_transferred;
        total_samples_emitted_ += samples_transferred;
        bool source_subset_finished = ((samples_emitted_ == sample_count_)
                                       && (sample_count_ != 0));
        // armed long-waveform (>1 buffer) case:
        //   armed as soon as we've fully stuffed the first buffer
        if (!remaining_buf_size())
            is_armed_ = true;
        if (!source_finished() && !source_subset_finished)
            return;
        // Handle end-of-source (or end of subset of source).
         ++curr_cycles_; // increment full source read iterations.
         samples_emitted_ = 0; // Reset tracking of samples within a cycle.
        // Handle last transfer condition.
        if ((curr_cycles_ == settings_ptr_->cycles) && (settings_ptr_->cycles != 0))
        {
            // armed short-waveform (<=1 buffer) case:
            //   armed as soon as we've stuffed the first buffer as much as
            //   as possible for the given settings.
            is_armed_ = true;
            // Next transfer will be the last transfer.
            // The next update() tick will reload waveform from the beginning.
            // At that point, user will be able to retrigger the waveform once
            // is_busy() is false.
            gpio_put(35, 1);
            buf_ptr_->setup_last_dma_transfer(chunk_index(), idle_buf_ptr_);
            curr_cycles_ = 0;
            gpio_put(35, 0);
            return;
        }
        // Handle endless/many-iteration transfer condition.
        // i.e: rewind at the completion of a cycle.
        rewind_source();
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

    bool is_armed_;
    uint32_t total_samples_emitted_; // Never resets unless we call reset()
    size_t curr_cycles_; /// elapsed cycles of the specified settings
                         /// (NOT cycles of a periodic waveform).
    uint32_t samples_emitted_; /// samples emitted within a cycle. resets to 0.
                               /// upon reset() or at the beginning of each
                               /// cycle.
    uint32_t sample_count_; // Cached value. Recomputed on reset().
                            // Number of samples within a cycle.
    size_t chunk_bytes_read_; /// bytes read after the most recent call to
                              /// transfer_source_chunk().
    DMADoubleBuffer<T, BUF_SIZE>* buf_ptr_;
    WaveformSettings* settings_ptr_;
    std::atomic<bool> is_updating;  // should be in RAM

    static inline constexpr size_t CHUNK_SIZE_BYTES = BUF_SIZE * sizeof(T); // bytes

private:
    inline size_t remaining_buf_size_bytes()
    {return (CHUNK_SIZE_BYTES - chunk_bytes_read_);}

    inline size_t remaining_buf_size()
    {return remaining_buf_size_bytes() / sizeof(T);}

    inline size_t chunk_index()
    {return chunk_bytes_read_ / sizeof(T);}

    bool manage_buffer_timing_; // Flag that we need to deal with timer.
    T* idle_buf_ptr_;
};
#endif // SOURCE_PLAYER_H
