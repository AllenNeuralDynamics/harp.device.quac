#ifndef WAVEFORM_SETTINGS_H
#define WAVEFORM_SETTINGS_H
#include <algorithm>
#include <cstdint>


/**
 * \brief base waveform base settings common to all waveforms.
 */
#pragma pack(push, 1)
struct WaveformSettings
{
    uint32_t cycles; /// number of iterations or 0 for loops-forever.
    uint32_t duration_us; /// 0 for read-everything or keep-going forever.
    uint32_t update_frequency_hz; /// update frequency that the waveform produces a new sample.

/**
 * \brief default settings constructor: play-once (single-shot), play the entire
 *  duration, play at the max update rate (500KHz).
 */
    WaveformSettings()
    : cycles{1}, duration_us{0}, update_frequency_hz{500000}
    {}

    WaveformSettings(uint32_t cycles, uint32_t duration_us,
                     uint32_t update_frequency_hz)
    : cycles{cycles}, duration_us{duration_us},
      update_frequency_hz{update_frequency_hz}
    {}

/**
 * \brief get number of samples associated with the \ref duration_us setting.
 */
    uint32_t sample_count()
    {return uint32_t((float(duration_us) * update_frequency_hz / 1.0E6));}
};
#pragma pack(pop)

#pragma pack(push, 1)
struct FileSettings: WaveformSettings
{
    static inline constexpr size_t MAX_FILE_NAME_LENGTH = 127;
    char file_name[MAX_FILE_NAME_LENGTH + 1]; // must be last element.
};
#pragma pack(pop)

/**
 * \brief periodic waveform base settings.
 */
#pragma pack(push, 1)
struct FunctionSettings: WaveformSettings
{
    uint16_t amplitude; // peak offset above midscale in DAC codes.
    uint32_t frequency_hz;

    FunctionSettings()
    : WaveformSettings{1, 1000000, 10000}, amplitude{32768}, frequency_hz{10}
    {}

/**
 * \brief compute samples in one period.
 */
    uint32_t period_sample_count()
    {return uint32_t(1.0f/float(frequency_hz) * update_frequency_hz);}
};
#pragma pack(pop)

/**
 * \brief triangle waveform settings
 */
#pragma pack(push, 1)
struct TrapezoidSettings: FunctionSettings
{
    uint32_t ramp_on_us;
    uint32_t ramp_off_us;

    uint32_t width_sample_count()
    {return period_sample_count() - ramp_on_sample_count() - ramp_off_sample_count();}

    uint32_t ramp_on_sample_count()
    {return update_frequency_hz * ramp_on_us;}

    uint32_t ramp_off_sample_count()
    {return update_frequency_hz * ramp_off_us;}

    // TODO: Triangle constructor
    // TODO: Square Wave constructor
    // TODO: Sawtooth constructor
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
