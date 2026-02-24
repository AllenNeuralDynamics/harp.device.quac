#ifndef QUAC_H
#define QUAC_H
#include <harp_core.h>
#include <harp_c_app.h>
#include <waveform_settings.h>
#include <array>
#include <config.h>
#include <multi_file_player.h>

using enum reg_type_t;

inline constexpr size_t APP_REG_COUNT = 22;

#pragma pack(push, 1)
struct app_regs_t
{
    // Digital IO
    uint8_t dio_port_dir;
    uint8_t dio_port_state;
    uint8_t dio_port_set;
    uint8_t dio_port_clear;

    // Triggers.
    uint8_t dac_external_triggers;   // Attach Digital Input to trigger DAC

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


void read_dio_port_dir(uint8_t address);
void write_dio_port_dir(msg_t& msg);

/**
 * \brief read the state of the DIO pins.
 */
void read_dio_port_state(uint8_t address);
void write_dio_port_state(msg_t& msg);

/**
 * \brief read the last set value.
 */
void read_dio_port_set(uint8_t address);
void write_dio_port_set(msg_t& msg);

/**
 * \brief read the last set value.
 */
void read_dio_port_clear(uint8_t address);
void write_dio_port_clear(msg_t& msg);

void read_dac_external_triggers(uint8_t address);
void write_dac_external_triggers(msg_t& msg);

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


#endif // QUAC_H

