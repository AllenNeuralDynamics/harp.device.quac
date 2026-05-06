#ifndef SINE_WAVE_PLAYER_H
#define SINE_WAVE_PLAYER_H
#include "raised_cosine_lut.h"
#include "function_player.h"
#include "source_player.h"
#include "waveform_settings.h"
#include <cstdint>

/**
 * \brief generates raised-cosine sinusoids and streams them to the specified
 *  buffer.
 * \details Sample output is baselined at DAC midscale (0 V on the bipolar
 * +/-10 V board). Raised-cosine sine output swings from midscale up to
 * `midscale + amplitude`.
 */
template <typename T, size_t BUF_SIZE>
class SineWavePlayer: public FunctionPlayer<T, BUF_SIZE>
{
public:
    SineWavePlayer()
    : FunctionPlayer<T, BUF_SIZE>{}, settings_{}
    {
        this->settings_ptr_ = &settings_; // for base class.
    }

/**
 * \brief apply sinusoid-specific settings.
 */
    bool apply_settings(FunctionSettings& settings)
    {
        if ((this->buf_ptr_ == nullptr) || this->is_busy())
            return false;
        settings_ = settings; // copy settings so rewind_source() works.
        return SourcePlayer<T, BUF_SIZE>::apply_settings(settings);
    }

/**
* \brief return a read-only reference to the current settings
*/
    const FunctionSettings& get_settings() const
    {return settings_;}

protected:
/**
 * \brief reset sine wave so it's ready to be played again from the beginning.
 */
    inline void rewind_source() override
    {
        phase_q32_ = 0;
        phase_inc_q32_ =
            static_cast<uint32_t>((uint64_t{settings_.frequency_hz} << 32)
                                  / settings_.update_frequency_hz);
    }

    inline void generate_function_chunk(T* dest, size_t num_samples) override
    {
        uint32_t phase = phase_q32_;
        const uint32_t inc = phase_inc_q32_;
        const uint32_t amp = settings_.amplitude;
        for (size_t i = 0; i < num_samples; ++i)
        {
            const uint32_t idx  = phase >> 22; // top 10 bits from uint32
            const uint32_t next = (idx + 1) & RAISED_COSINE_LUT_MASK;
            const uint32_t frac = phase & 0x003FFFFFu; // low 22 bits from uint32
            const int64_t a = static_cast<int64_t>(RAISED_COSINE_LUT[idx]);
            const int64_t b = static_cast<int64_t>(RAISED_COSINE_LUT[next]);
            // Linear interpolation in [0, 65535].
            const int64_t interp =
                a + ((b - a) * static_cast<int64_t>(frac)) / (int64_t(1) << 22);
            const uint32_t shape = static_cast<uint32_t>(
                interp < 0 ? 0 : (interp > 65535 ? 65535 : interp));
            //const uint32_t result = (shape * amp) >> 16;
            //const uint32_t result = (uint64_t(int32_t(shape) - 32768)*amp)/65535 + 32768; // ??
            const uint32_t result = float(int32_t(shape) - 32768) *
                                    (float(amp)/65535.0f)
                                    + 32768.0f // nominal offset.
                                    + settings_.vertical_shift;
            dest[i] = static_cast<T>(result );
            phase += inc;
        }
        phase_q32_ = phase;
    }

private:
    FunctionSettings settings_;
    // Sine DDS state.
    uint32_t phase_q32_;
    uint32_t phase_inc_q32_; /// phase increment
};
#endif // SINE_WAVE_PLAYER_H
