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
    bool apply_settings(TrapezoidSettings& settings) override
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
        width_samples_ = settings_.width_sample_count();
        ramp_on_samples_ = settings_.ramp_on_sample_count();
        ramp_off_samples_ = settings_.ramp_off_sample_count();
        SourcePlayer<T, BUF_SIZE>::rewind_source();
    }

/**
 * \brief
 */
    void generate_function_chunk(T* dest, size_t num_samples) override
    {
        generate_pulse(dest, num_samples);
        SourcePlayer<T, BUF_SIZE>::generate_function_chunk(dest, num_samples);
    }

private:
/**
 * \brief generate a pulse
 */
 void generate_pulse(T* dest, size_t num_samples)
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
         uint32_t level = 0;
         if (t < ramp_on)
         {
             level = (ramp_on == 0)
                 ? amp
                 : uint32_t{(uint64_t{amp} * t) / ramp_on};
         }
         else if (t < plateau_end)
         {
             level = amp;
         }
         else if (t < ramp_down_end)
         {
             const uint32_t dt = t - plateau_end;
             level = (ramp_off == 0)
                 ? 0
                 : uint32_t{uint64_t{amp} - (uint64_t{amp} * dt) / ramp_off};
         }
         else
         {
             level = 0;
         }
         dest[i] = this->saturating_offset(level);
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
