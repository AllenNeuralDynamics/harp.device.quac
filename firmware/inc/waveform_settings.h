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
    uint32_t update_frequency_hz; /// update rate that the waveform produces a
                                  /// new sample.

/// \brief default constructor: play the full duration once at 500KHz (max rate).
    WaveformSettings()
    : cycles{1}, duration_us{0}, update_frequency_hz{500000}{}

/// \brief full constructor.
    WaveformSettings(uint32_t cycles, uint32_t duration_us,
                     uint32_t update_frequency_hz)
    : cycles{cycles}, duration_us{duration_us},
      update_frequency_hz{update_frequency_hz}{}

/// \brief get number of samples associated with the \ref duration_us setting.
    uint32_t sample_count()
    {return uint32_t((float(duration_us) * update_frequency_hz / 1.0E6));}
};
#pragma pack(pop)

/**
 * \brief waveform settings for files.
 */
#pragma pack(push, 1)
struct FileSettings: WaveformSettings
{
    static inline constexpr size_t MAX_FILE_NAME_LENGTH = 32;
    char path[MAX_FILE_NAME_LENGTH + 1];
};
#pragma pack(pop)

/**
 * \brief periodic waveform base settings.
 */
#pragma pack(push, 1)
struct FunctionSettings: WaveformSettings
{
    static inline constexpr float MAX_AMPLITUDE_VOLTS = 10.0f;
    uint32_t frequency_hz;
    float amplitude_volts; // peak offset from center position in volts.
    float vertical_shift_volts; // vertical shift in samples.

/// Default Constructor
    FunctionSettings()
    : WaveformSettings{1, 1000000, 10000}, amplitude_volts{10}, frequency_hz{10},
      vertical_shift_volts{0}{}

/// Full Constructor
    FunctionSettings(float amplitude_volts, uint32_t frequency_hz,
                     float vertical_shift_volts, uint32_t cycles,
                     uint32_t duration_us, uint32_t update_frequency_hz)
    : amplitude_volts{amplitude_volts}, frequency_hz{frequency_hz},
      vertical_shift_volts{vertical_shift_volts},
      WaveformSettings{cycles, duration_us, update_frequency_hz}{}

/// \brief compute samples in one period.
    uint32_t period_sample_count()
    {return uint32_t(1.0f/float(frequency_hz) * update_frequency_hz);}

/// \brief compute period in microseconds
    uint32_t period_us()
    {return uint32_t(1.0e6f/float(frequency_hz));}

    uint32_t amplitude_16bit()
    {return peak_to_peak_amplitude_16bit()/2;}

    inline uint32_t peak_to_peak_amplitude_16bit()
    {return uint32_t(amplitude_volts/MAX_AMPLITUDE_VOLTS * 65535u);}

    uint32_t vertical_shift_16bit()
    {return uint32_t(vertical_shift_volts/MAX_AMPLITUDE_VOLTS * 65535u);}

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
    // TODO: a duty cycle? so we can reuse this class for pulse trains?
    // TODO: a phase offset? so we can start slightly offset?

/// \brief Default Constructor
    TrapezoidSettings()
    : FunctionSettings{},
      ramp_on_us{uint32_t(period_us() / 4.0f)},
      ramp_off_us{uint32_t(period_us() / 4.0f)} {}

/// \brief Full Constructor
    TrapezoidSettings(uint16_t amplitude, uint32_t frequency_hz,
                      uint32_t vertical_shift, uint32_t cycles,
                      uint32_t duration_us, uint32_t update_frequency_hz,
                      uint32_t ramp_on_us, uint32_t ramp_off_us)
    : FunctionSettings(amplitude, frequency_hz, vertical_shift, cycles,
                       duration_us, update_frequency_hz),
      ramp_on_us{ramp_on_us}, ramp_off_us{ramp_off_us}{}

// Computed values
    uint32_t plateau_sample_count()
    {return period_sample_count() - ramp_on_sample_count() - ramp_off_sample_count();}

    uint32_t ramp_on_sample_count()
    {return uint32_t(float(update_frequency_hz) * float(ramp_on_us) / 1'000'000.0f);}

    uint32_t ramp_off_sample_count()
    {return uint32_t(float(update_frequency_hz) * float(ramp_off_us) / 1'000'000.0f);}

    // TODO: Triangle constructor
    // TODO: Pulse Train constructor
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
