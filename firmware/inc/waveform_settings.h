#ifndef WAVEFORM_SETTINGS_H
#define WAVEFORM_SETTINGS_H
#include <algorithm>
#include <cstdint>


#pragma pack(push, 1)
struct WaveformSettings
{
    uint32_t cycles; /// number of iterations or 0 for loops-forever.
    uint32_t duration_us; /// 0 for read-everything or keep-going forever.
    uint32_t frequency_hz;

    WaveformSettings()
    : cycles{1}, duration_us{0}, frequency_hz{500000}
    {}

    uint32_t sample_count()
    {return static_cast<uint32_t>(float(duration_us) * frequency_hz / 1.0E6);}
};
#pragma pack(pop)


#pragma pack(push, 1)
/**
 * \brief base waveform settings and additional settings for Harp interface.
 */
struct SineWaveSettings: WaveformSettings
{
    uint16_t amplitude;             // peak offset above midscale in DAC codes.
};
#pragma pack(pop)


#pragma pack(push, 1)
/**
 * \brief base waveform settings and additional settings for Harp interface.
 */
struct WaveformInterfaceSettings: WaveformSettings
{
    uint8_t external_trigger_mask;
};
#pragma pack(pop)


#endif // WAVEFORM_SETTINGS_H
