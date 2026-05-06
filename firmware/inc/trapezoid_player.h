#ifndef TRAPEZOID_PLAYER_H
#define TRAPEZOID_PLAYER_H
#include "function_player.h"
#include "waveform_settings.h"
#include <cstdint>

/**
 * \brief class for reading a trapezoid waveform into a buffer
 */
template <typename T, size_t BUF_SIZE>
class TrapezoidPlayer: public FunctionPlayer<T, BUF_SIZE>
{
public:
    TrapezoidPlayer()
    : FunctionPlayer<T, BUF_SIZE>{}, settings_{}
    {
        this->settings_ptr_ = &settings_;
    }

/**
 * \brief apply trapezoid-specific settings.
 */
    bool apply_settings(TrapezoidSettings& settings)
    {
        if ((!this->buf_ptr_) || this->is_busy())
            return false;
        settings_ = settings; // copy settings so rewind_source() works.
        return SourcePlayer<T, BUF_SIZE>::apply_settings(settings_);
    }

/**
* \brief return a read-only reference to the current settings
*/
    const TrapezoidSettings& get_settings() const
    {return settings_;}

protected:
/**
 * \brief rewind file so it's ready to be played again from the beginning.
 */
    inline void rewind_source() override
    {
        period_counter_ = 0;
        interval_samples_ = settings_.period_sample_count();
        width_samples_ = settings_.plateau_sample_count();
        ramp_on_samples_ = settings_.ramp_on_sample_count();
        ramp_off_samples_ = settings_.ramp_off_sample_count();
    }

/**
 * \brief
 */
 void generate_function_chunk(T* dest, size_t num_samples) override
 {
     uint32_t t              = period_counter_;
     const uint32_t interval = interval_samples_;
     const uint32_t width    = width_samples_;
     const uint32_t ramp_on  = ramp_on_samples_;
     const uint32_t ramp_off = ramp_off_samples_;
     const uint32_t amp      = settings_.amplitude;

     const uint32_t plateau_end  = ramp_on + width;
     const uint32_t ramp_down_end = plateau_end + ramp_off;

     for (size_t i = 0; i < num_samples; ++i)
     {
         uint32_t shape = 0;
         if (t < ramp_on)
         {
             shape = (ramp_on == 0)
                 ? 65535
                 : static_cast<uint32_t>((uint64_t{65535} * t) / ramp_on);
         }
         else if (t < plateau_end)
         {
             shape = 65535;
         }
         else if (t < ramp_down_end)
         {
             const uint32_t dt = t - plateau_end;
             shape = (ramp_off == 0)
                 ? 0
                 : static_cast<uint32_t>(uint64_t{65535} - (uint64_t{65535} * dt)
                                         / ramp_off);
         }
         else
         {
             shape = 0;
         }
         // Scale to desired frequency and amplitude settings.
         uint32_t result = float(int32_t(shape ) - 32768) * (float(amp)/65535.0f)
                          + 32768.0f // nominal offset.
                          + settings_.vertical_shift;
         dest[i] = static_cast<T>(result); // TODO: clamp instead.
         if (++t >= interval)
             t = 0;
     }
     period_counter_ = t;
 }

private:
    uint32_t period_counter_;

    uint32_t interval_samples_;
    uint32_t width_samples_;
    uint32_t ramp_on_samples_;
    uint32_t ramp_off_samples_;

    TrapezoidSettings settings_;
};
#endif // TRAPEZOID_PLAYER_H
