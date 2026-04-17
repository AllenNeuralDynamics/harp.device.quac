#ifndef QUAC_H
#define QUAC_H
#include <harp_core.h>
#include <harp_c_app.h>
#include <waveform_settings.h>
#include <sine_wave_settings.h>
#include <pulse_train_settings.h>
#include <array>
#include <config.h>
#include <multi_file_player.h>
#include <multi_waveform_player.h>
#include <pico/util/queue.h>
#include <pico/multicore.h>
#include <core1_file_player.h>
#include <reg_spec.h>

using enum reg_type_t;

extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;
extern queue_t ext_trigger_event_queue;
extern MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE> player;
extern MultiWaveformPlayer<T, NUM_CHANNELS, READ_BUF_SIZE> waveform_player;
extern RegSpec app_reg_specs[];

extern const size_t APP_REG_COUNT;
inline constexpr size_t DAC_START_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 10;
inline constexpr size_t DAC_FINISHED_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 13;
// Offsets into the app-register table below DAC_FINISHED_ADDRESS's entry.
// See app_reg_specs[] layout in quac_app.cpp for the authoritative order.
inline constexpr size_t WAVEFORM_TYPE_BASE_ADDRESS    = HarpCore::APP_REG_START_ADDRESS + 26;
inline constexpr size_t SINE_SETTINGS_BASE_ADDRESS    = HarpCore::APP_REG_START_ADDRESS + 30;
inline constexpr size_t PULSE_SETTINGS_BASE_ADDRESS   = HarpCore::APP_REG_START_ADDRESS + 34;
inline constexpr size_t WAVEFORM_START_ADDRESS        = HarpCore::APP_REG_START_ADDRESS + 38;
inline constexpr size_t WAVEFORM_ABORT_ADDRESS        = HarpCore::APP_REG_START_ADDRESS + 39;
inline constexpr size_t WAVEFORM_FINISHED_ADDRESS     = HarpCore::APP_REG_START_ADDRESS + 40;
inline constexpr size_t SAMPLE_RATE_HZ_ADDRESS        = HarpCore::APP_REG_START_ADDRESS + 41;


struct ext_trigger_event_t
{
    uint32_t channel_start_mask;
    uint64_t timestamp;
};

#pragma pack(push, 1)
struct app_regs_t
{
    // Digital Output
    uint8_t digital_output_port_state;
    uint8_t digital_output_port_set;
    uint8_t digital_output_port_clear;

    // Triggers.
    uint8_t ext_trigger_state;

    uint16_t analog_output_port_state[NUM_CHANNELS];
    uint16_t& analog_output_channel_0  = analog_output_port_state[0];
    uint16_t& analog_output_channel_1  = analog_output_port_state[1];
    uint16_t& analog_output_channel_2  = analog_output_port_state[2];
    uint16_t& analog_output_channel_3  = analog_output_port_state[3];

    uint8_t dac_ready;
    uint8_t dac_start;
    uint8_t dac_pause; // 1-bit: pause active channel, 0-bit: unpause paused channel
    uint8_t dac_abort;
    uint8_t dac_finished;

    // WaveformSettings are only exposed for read/write as individual registers.
    WaveformSettings dac_settings[NUM_CHANNELS];

    // waveform_hashes are only exposed for read as individual registers.
    uint8_t waveform_hashes[NUM_CHANNELS][SHA256_NUM_BYTES];
    // waveform_data are only exposed for write as individual registers.
    T waveform_data[NUM_CHANNELS]; // treat like a pointer. Data is stored on SD card.

    // Waveform generator registers (MultiWaveformPlayer).
    uint8_t waveform_type[NUM_CHANNELS];             // 0=Sine, 1=PulseTrain
    SineWaveSettings sine_settings[NUM_CHANNELS];
    PulseTrainSettings pulse_settings[NUM_CHANNELS];
    uint8_t waveform_start;                          // write-only bitmask
    uint8_t waveform_abort;                          // write-only bitmask
    uint8_t waveform_finished;                       // EVENT payload
    uint32_t sample_rate_hz;                         // shared DMA pacing rate
};
#pragma pack(pop)

extern app_regs_t app_regs;

void read_digital_output_port_state(uint8_t address);
void write_digital_output_port_state(msg_t& msg);

void write_digital_output_port_set(msg_t& msg);

void write_digital_output_port_clear(msg_t& msg);

void read_ext_trigger_state(uint8_t address);

void read_analog_output_port_state(uint8_t address);
void write_analog_output_port_state(msg_t& msg);

void read_any_analog_output_channel(uint8_t address);
void write_any_analog_output_channel(msg_t& msg);

void read_dac_ready(uint8_t address);

void read_dac_start(uint8_t address);
void write_dac_start(msg_t& msg);

void read_dac_pause(uint8_t address);
void write_dac_pause(msg_t& msg);

void read_dac_abort(uint8_t address);
void write_dac_abort(msg_t& msg);

void read_any_dac_settings(uint8_t address);
void write_any_dac_settings(msg_t& msg);

void read_any_waveform_hash(uint8_t address);

void write_any_waveform_data(msg_t& msg);

void read_any_waveform_type(uint8_t address);
void write_any_waveform_type(msg_t& msg);

void read_any_sine_settings(uint8_t address);
void write_any_sine_settings(msg_t& msg);

void read_any_pulse_settings(uint8_t address);
void write_any_pulse_settings(msg_t& msg);

void write_waveform_start(msg_t& msg);
void write_waveform_abort(msg_t& msg);

void read_sample_rate_hz(uint8_t address);
void write_sample_rate_hz(msg_t& msg);

void reset_app();

void update_app();


/// Callbacks
void handle_external_trigger();



#endif // QUAC_H

