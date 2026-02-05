#ifndef WAVEFORM_SETTINGS_H
#define WAVEFORM_SETTINGS_H


struct WaveformSettings
{
    uint32_t iterations; // 0 for loops-forever.
    uint32_t sample_count;
    uint32_t frequency_hz;
    uint8_t external_triggers;
    //uint8_t sha256[8];

    // TODO: default constructor should set iterations=1
};

// TODO: should commands be enums, and the actual command is a struct of
//  {cmd, mask}?
struct BulkWaveformCommands
{
    uint8_t start;
    uint8_t pause;
    uint8_t abort;
    uint8_t arm;
};


/// Each bit represents a waveform.
//struct BulkWaveformStates
//{
//    uint8_t is_armed;   // waveform[i] is ready (buffered) to be started.
//    uint8_t is_playing; // waveform[i] is playing .
//    uint8_t is_short_circuited; // waveform[i] short-circuit detection triggered.
//};



#endif // WAVEFORM_SETTINGS_H
