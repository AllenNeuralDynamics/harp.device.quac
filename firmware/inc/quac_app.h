#ifndef QUAC_H
#define QUAC_H
#include <harp_core.h>
#include <harp_c_app.h>
#include <waveform_settings.h>
#include <array>
#include <config.h>
#include "multi_transfer_manager.h"
#include <pico/util/queue.h>
#include <pico/multicore.h>
#include <core1_file_player.h>
#include <reg_spec.h>

using enum reg_type_t;

extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;
extern queue_t ext_trigger_event_queue;

extern std::array<TimerPacedDMADoubleBuffer<T, READ_BUF_SIZE>, NUM_CHANNELS> bufs;
extern std::array<DMADoubleBuffer<T, READ_BUF_SIZE>*, NUM_CHANNELS> buf_ptrs;
extern std::array<FilePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> file_players;
extern MultiTransferManager<T, READ_BUF_SIZE, NUM_CHANNELS> transfer_manager;
extern RegSpec app_reg_specs[];

extern const size_t APP_REG_COUNT;
inline constexpr size_t DAC_START_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 10;
inline constexpr size_t DAC_FINISHED_ADDRESS = HarpCore::APP_REG_START_ADDRESS + 13;


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

void reset_app();

void update_app();


/// Callbacks
void handle_external_trigger();



#endif // QUAC_H
