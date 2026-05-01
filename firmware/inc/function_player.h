#ifndef FUNCTION_PLAYER_H
#define FUNCTION_PLAYER_H
#include <cstdint>
#include <limits>
#include "source_player.h"

/**
 * \brief base class for creating waveforms from time-series functions and
 *  streaming them to the specified buffer.
 */
template <typename T, size_t BUF_SIZE>
class FunctionPlayer: public SourcePlayer<T, BUF_SIZE>
{
public:
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max() / 2;
    static inline constexpr T OUTPUT_MAX      = std::numeric_limits<T>::max();
    static inline constexpr uint32_t DEFAULT_SAMPLE_RATE_HZ = 10'000;

    FunctionPlayer(DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : SourcePlayer<T, BUF_SIZE>{buf_ptr}{}

/**
 * \brief Clear internal state but do not release claimed resources.
 */
    virtual void reset() override
    {
        SourcePlayer<T, BUF_SIZE>::reset(); // calls rewind
        sample_count_ = this->settings_ptr_->sample_count(); // Recompute.
    }

/**
 * \brief setup waveform playback
 */
    void setup()
    {
        reset(); // calls rewind
        // Pre-fill the first ping-pong buffer for each requested channel.
        this->update();
    }

/**
 * \brief reset sine wave so it's ready to be played again from the beginning.
 */
    inline virtual void rewind_source()
    {
        samples_emitted_ = 0;
    }

/**
 * \brief true if the file has been fully read to the end.
 */
    inline virtual bool source_finished()
    {
        if (this->settings_ptr_.duration_us == 0) // never finished in this case.
            return false;
        return sample_count_ == samples_emitted_;
    }

protected:
/**
 * \brief transfer bytes from file to the address specified in \p dest.
 * \details if a finite number of samples is specified, only transfer up to that
 *  limit specified until calling rewind_source().
 */
    inline void transfer_source_chunk(T* dest, size_t num_bytes,
                                      size_t& bytes_transferred)
    {
        uint32_t num_samples = num_bytes/sizeof(T);
        uint32_t& sample_count = this->settings_ptr_.sample_count();
        samples_emitted_ += num_samples;
        // Only generate up to sample_count total samples.
        if ((sample_count > 0) && (samples_emitted_ > sample_count))
        {
            uint32_t delta = samples_emitted_ - sample_count;
            num_samples -= delta;
            samples_emitted_ = sample_count;
        }
        generate_function_chunk(dest, num_samples);
        bytes_transferred = sizeof(T) * num_samples;
    }

/**
 * \brief
 */
    inline virtual void generate_function_chunk(T* dest, size_t num_samples)
    {
        samples_emitted_ += num_samples;
    }

/**
 * \brief
 */
    static inline T saturating_offset(uint32_t offset)
    {
        uint32_t sample = static_cast<uint32_t>(OUTPUT_MIDSCALE) + offset;
        if (sample > OUTPUT_MAX)
            sample = OUTPUT_MAX;
        return static_cast<T>(sample);
    }

private:
    // Internal playback tracking
    uint32_t samples_emitted_;
    uint32_t sample_count_; // cached value computed from settings_ptr_;
};

#endif // FUNCTION_PLAYER_H
