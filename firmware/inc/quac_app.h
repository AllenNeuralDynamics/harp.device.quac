#ifndef QUAC_H
#define QUAC_H
#include <harp_core.h>
#include <harp_c_app.h>
#include <waveform_settings.h>
#include <array>
#include <config.h>
#include <multi_file_player.h>
#include <pico/multicore.h>

using enum reg_type_t;

extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;
queue_t ext_trigger_event_queue;


inline constexpr size_t APP_REG_COUNT = 26;
inline constexpr size_t AO_CHANNEL_BASE_ADDRESS = APP_REG_START_ADDRESS + 5;
inline constexpr size_t DAC_START_ADDRESS = APP_REG_START_ADDRESS + 10;

#pragma pack(push, 1)
struct app_regs_t
{
    // Digital Output
    uint8_t digital_output_port_state;
    uint8_t digital_output_port_set;
    uint8_t digital_output_port_clear;

    // Triggers.
    uint8_t waveform_triggered;

    uint16_t analog_output_port_state[NUM_CHANNELS]; // group register view
    uint16_t& analog_output_channel_0 = analog_output_port_state[0];
    uint16_t& analog_output_channel_1 = analog_output_port_state[1];
    uint16_t& analog_output_channel_2 = analog_output_port_state[2];
    uint16_t& analog_output_channel_3 = analog_output_port_state[3];

    uint8_t dac_ready;
    uint8_t dac_start;
    uint8_t dac_pause; // 1-bit: pause active channel, 0-bit: unpause paused channel
    uint8_t dac_abort;
    uint8_t dac_finished;

    WaveformSettings dac_settings[NUM_CHANNELS];

    uint8_t waveform_hashes[NUM_CHANNELS][SHA256_NUM_BYTES];
    T waveform_data[NUM_CHANNELS]; // treat like a pointer. Data is stored on SD card.
};
#pragma pack(pop)

extern MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE> player;
extern app_regs_t app_regs;
extern RegSpecs app_reg_specs[APP_REG_COUNT];
extern RegFnPair reg_handler_fns[APP_REG_COUNT];


void read_digital_output_port_dir(uint8_t address);
void write_digital_output_port_dir(msg_t& msg);

/**
 * \brief read the state of the DIO pins.
 */
void read_digital_output_port_state(uint8_t address);
void write_digital_output_port_state(msg_t& msg);

void write_dio_port_set(msg_t& msg);

void write_dio_port_clear(msg_t& msg);

void read_analog_output_port_state(uint8_t address);
void write_analog_output_port_state(msg_t& msg);

void read_any_ao_channel(uint8_t address);
void write_any_ao_channel(msg_t& msg);

void read_dac_ready(uint8_t address);

void read_dac_start(uint8_t address);
void write_dac_start(msg_t& msg);

void read_dac_pause(uint8_t address);
void write_dac_pause(msg_t& msg);

void read_dac_abort(uint8_t address);
void write_dac_abort(msg_t& msg);

void read_dac_finished(uint8_t address);

void read_any_dac_settings(uint8_t address);
void write_any_dac_settings(msg_t& msg);

void read_any_waveform_hash(uint8_t address);

void write_any_waveform_data(msg_t& msg);

void reset_app();

void update_app();


/// Callbacks
void handle_external_trigger();



#endif // QUAC_H

