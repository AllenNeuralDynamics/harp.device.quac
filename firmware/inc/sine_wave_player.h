#ifndef SINE_WAVE_PLAYER_H
#define SINE_WAVE_PLAYER_H
#include <array>
#include <cstdint>
#include <limits>
#include "sine_wave_settings.h"
#include "raised_cosine_lut.h"
#include "source_player.h"

/**
 * \brief generates raised-cosine sinusoids and streams them to the specified
 *  buffer.
 * \details Sample output is baselined at DAC midscale (0 V on the bipolar
 * +/-10 V board). Raised-cosine sine output swings from midscale up to
 * `midscale + amplitude`.
 *  trapezoidal pulses share the same convention for their flat-top value.
 */
template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class SineWavePlayer: public SourcePlayer<T, BUF_SIZE>
{
public:
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max() / 2;
    static inline constexpr T OUTPUT_MAX      = std::numeric_limits<T>::max();
    static inline constexpr uint32_t DEFAULT_SAMPLE_RATE_HZ = 10'000;

    SineWavePlayer(DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : SourcePlayer<T, BUF_SIZE>{buf_ptr}
    {reset();}

/**
 * \brief clear state and relinquish claimed resources.
 * \warning not multicore safe.
 */
// FIXME: does reset need to be virtual...
    void reset()
    {
        rewind_source();
        sine_settings_ = SineWaveSettings{};
        sample_count_ = this->settings_.sample_count(); // Recompute.
        SourcePlayer<T, BUF_SIZE>::reset();
    }

/**
 * \brief
 */
    void set_sine_settings(const SineWaveSettings& s)
    {
        sine_settings_ = s;
        sample_count_ = this->settings_.sample_count(); // Recompute.
    }

    const SineWaveSettings& get_sine_settings() const
    {return sine_settings_;}

/**
 * \brief setup waveform playback on the specified channels.
 */
    void setup()
    {
        // Reset generator state.
        rewind_source();
        sample_count_ = this->settings_.sample_count(); // Recompute.
        // Pre-fill the first ping-pong buffer for each requested channel.
        this->update();
    }

/**
 * \brief reset sine wave so it's ready to be played again from the beginning.
 */
    inline void rewind_source()
    {
        phase_q32_          = 0;
        phase_inc_q32_      = 0;
        samples_emitted_ = 0;
    }

/**
 * \brief transfer bytes from file to the address specified in \p dest.
 * \details if a finite number of samples is specified, only transfer up to that
 *  limit specified until calling rewind_source().
 */
    inline void transfer_source_chunk(T* dest, size_t num_bytes,
                                      size_t& bytes_transferred)
    {
        uint32_t num_samples = num_bytes/sizeof(T);
        uint32_t& sample_count = this->settings_.sample_count();
        samples_emitted_ += num_samples;
        // Only generate up to sample_count total samples.
        if ((sample_count > 0) && (samples_emitted_ > sample_count))
        {
            uint32_t delta = samples_emitted_ - sample_count;
            num_samples -= delta;
            samples_emitted_ = sample_count;
        }
        generate_sine(dest, num_samples);
        bytes_transferred = sizeof(T) * num_samples;
    }

/**
 * \brief true if the file has been fully read to the end.
 */
    inline bool source_finished()
    {
        if (this->settings_.duration_us == 0) // never finished in this case.
            return false;
        return sample_count_ == samples_emitted_;
    }

private:
    static inline T saturating_offset(uint32_t offset)
    {
        uint32_t sample = static_cast<uint32_t>(OUTPUT_MIDSCALE) + offset;
        if (sample > OUTPUT_MAX)
            sample = OUTPUT_MAX;
        return static_cast<T>(sample);
    }

    void generate_sine(T* dst, size_t num_samples)
    {
        uint32_t phase = phase_q32_;
        const uint32_t inc = phase_inc_q32_;
        const uint32_t amp = sine_settings_.amplitude;
        for (size_t i = 0; i < num_samples; ++i)
        {
            const uint32_t idx  = phase >> 22;            // top 10 bits from 32-bit integer
            const uint32_t next = (idx + 1) & RAISED_COSINE_LUT_MASK;
            const uint32_t frac = phase & 0x003FFFFFu;    // low 22 bits from 32-bit integer
            const int64_t a = static_cast<int64_t>(RAISED_COSINE_LUT[idx]);
            const int64_t b = static_cast<int64_t>(RAISED_COSINE_LUT[next]);
            // Linear interpolation in [0, 65535].
            const int64_t interp =
                a + ((b - a) * static_cast<int64_t>(frac)) / (int64_t(1) << 22);
            const uint32_t shape = static_cast<uint32_t>(
                interp < 0 ? 0 : (interp > 65535 ? 65535 : interp));
            const uint32_t offset = (shape * amp) >> 16;
            dst[i] = saturating_offset(offset);
            phase += inc;
        }
        phase_q32_ = phase;
        samples_emitted_ += num_samples;
    }

private:
    SineWaveSettings sine_settings_{};
    // Sine DDS state.
    uint32_t phase_q32_;
    uint32_t phase_inc_q32_;
    // Internal playback tracking
    uint32_t samples_emitted_;
    uint32_t sample_count_; // cached value computed from settings_;
};

#endif // SINE_WAVE_PLAYER_H
