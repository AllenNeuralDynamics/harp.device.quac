#ifndef SINE_WAVE_PLAYER_H
#define SINE_WAVE_PLAYER_H
#include "raised_cosine_lut.h"
#include "function_player.h"
#include <cstdint>



// FIXME FIXME FIXME: where is the period set?

/**
 * \brief generates raised-cosine sinusoids and streams them to the specified
 *  buffer.
 * \details Sample output is baselined at DAC midscale (0 V on the bipolar
 * +/-10 V board). Raised-cosine sine output swings from midscale up to
 * `midscale + amplitude`.
 *  trapezoidal pulses share the same convention for their flat-top value.
 */
template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class SineWavePlayer: public FunctionPlayer<T, BUF_SIZE>
{
public:
    SineWavePlayer(DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : FunctionPlayer<T, BUF_SIZE>{buf_ptr}, settings_{}
    {
        this->settings_ptr_ = &settings_; // for base class.
    }

/**
 * \brief Clear state but do not release claimed resources.
 * \note does not clear settings.
 */
    void reset() override
    {
        FunctionPlayer<T, BUF_SIZE>::reset();
    }

/**
 * \brief reset sine wave so it's ready to be played again from the beginning.
 */
    inline void rewind_source() override
    {
        phase_q32_          = 0;
        phase_inc_q32_      = 0;
        FunctionPlayer<T, BUF_SIZE>::rewind_source();
    }

protected:
    inline void generate_function_chunk(T* dest, size_t num_samples) override
    {
        generate_sine(dest, num_samples);
        FunctionPlayer<T, BUF_SIZE>::generate_function_chunk(dest, num_samples);
    }

private:
    void generate_sine(T* dest, size_t num_samples)
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
            const uint32_t offset = (shape * amp) >> 16;
            dest[i] = this->saturating_offset(offset);
            phase += inc;
        }
        phase_q32_ = phase;
    }

private:
    FunctionSettings settings_{};
    // Sine DDS state.
    uint32_t phase_q32_;
    uint32_t phase_inc_q32_;
};

#endif // SINE_WAVE_PLAYER_H
