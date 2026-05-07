#ifndef QUAC_H
#define QUAC_H
#include "pico/util/queue.h"
#include "pico/multicore.h"
#include <core1_file_player.h>
#include <reg_spec.h>
#include <harp_core.h>
#include <harp_c_app.h>
#include "waveform_settings.h"
#include "multi_transfer_manager.h"
#include "file_player.h"
#include "sine_wave_player.h"
#include "trapezoid_player.h"
#include <array>
#include <cstdint>
#include <config.h>

using enum reg_type_t;

extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;
extern queue_t ext_trigger_event_queue;

extern std::array<TimerPacedDMADoubleBuffer<T, READ_BUF_SIZE>, NUM_CHANNELS> bufs;
extern std::array<DMADoubleBuffer<T, READ_BUF_SIZE>*, NUM_CHANNELS> buf_ptrs;
extern std::array<FilePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> file_players;
extern std::array<SineWavePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> sine_players;
extern std::array<TrapezoidPlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> trapezoid_players;
extern MultiTransferManager<T, READ_BUF_SIZE, NUM_CHANNELS> transfer_manager;
extern RegSpec app_reg_specs[];

extern const size_t APP_REG_COUNT;
inline constexpr size_t DAC_START_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 10;
inline constexpr size_t DAC_FINISHED_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 13;

enum player_t: uint8_t
{
    file = 0,
    sine = 1,
    trapezoid = 2
}

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

    // DAC state masks.
    uint8_t dac_ready;
    uint8_t dac_start;
    uint8_t dac_pause; // 1-bit: pause active channel, 0-bit: unpause paused channel
    uint8_t dac_abort;
    uint8_t dac_finished;

    // External trigger masks per-channeel. Assign a channel to one or more triggers.
    uint8_t channel_external_triggers[NUM_CHANNELS];

    // Which player is active per channel.
    uint8_t active_players[NUM_CHANNELS];

    // Settings for each distinct player.
    FileSettings file_settings[NUM_CHANNELS];
    FunctionSettings sine_settings[NUM_CHANNELS];
    TrapezoidSettings trapezoid_settings[NUM_CHANNELS];
    //TrapezoidSettings (&sawtooth_settings)[NUM_CHANNELS] = trapezoid_settings;
    //TrapezoidSettings (&triangle_settings)[NUM_CHANNELS] = trapezoid_settings;

    // waveform_hashes are only exposed for read as individual registers.
    uint8_t waveform_hashes[NUM_CHANNELS][SHA256_NUM_BYTES];
    // waveform_data are only exposed for write as individual registers.
    T waveform_data[NUM_CHANNELS]; // treat like a pointer. Data is stored on SD card.

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

void select_player(size_t channel, player_t player);

void update_app();


/// Callbacks
void handle_external_trigger();



#endif // QUAC_H
