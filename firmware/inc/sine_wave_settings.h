#ifndef SINE_WAVE_SETTINGS_H
#define SINE_WAVE_SETTINGS_H
#include <cstdint>

/**
 * \brief settings for the raised-cosine sine generator.
 *
 * Output on the bipolar +/-10V DAC swings from 0V (DAC midscale) upward to
 * `amplitude` DAC codes above midscale, following
 *      sample = midscale + amplitude * (1 - cos(phase)) / 2.
 */
#pragma pack(push, 1)
struct SineWaveSettings
{
    uint32_t frequency_hz;          // signal frequency (distinct from sample rate).
    uint32_t duration_us;           // 0 = run forever.
    uint16_t amplitude;             // peak offset above midscale in DAC codes.
    uint8_t  external_trigger_mask; // DI pin(s) that arm this channel.
};
#pragma pack(pop)

#endif // SINE_WAVE_SETTINGS_H
