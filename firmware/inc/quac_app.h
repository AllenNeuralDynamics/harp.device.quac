#ifndef QUAC_H
#define QUAC_H
#include <array>
#include <cstdint>
#include <cstring> // for memcpy
#include "pico/util/queue.h"
#include "pico/multicore.h"
#include "config.h"
#include "core1_file_player.h"
#include "harp_core.h"
#include "harp_c_app.h"
#include "harp_message.h"
#include "reg_spec.h"
#include "dma_double_buffer.h"
#include "file_player.h"
#include "sine_wave_player.h"
#include "trapezoid_player.h"
#include "multi_transfer_manager.h"
#include "waveform_settings.h"
#include "pio_ltc264x.h"

using enum reg_type_t;

extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;
extern queue_t ext_trigger_event_queue;

extern std::array<TimerPacedDMADoubleBuffer<T, READ_BUF_SIZE>, NUM_CHANNELS> bufs;
extern std::array<DMADoubleBuffer<T, READ_BUF_SIZE>*, NUM_CHANNELS> buf_ptrs;
extern std::array<FilePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> file_players;
extern std::array<SinePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> sine_players;
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
};

enum di_pin_mode_t: uint8_t
{
    NONE = 0,
    EXTERNAL_TRIGGER = 1,
    BLANKING = 2
};

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

    float analog_output_port_state[NUM_CHANNELS];
    float& analog_output_channel_0  = analog_output_port_state[0];
    float& analog_output_channel_1  = analog_output_port_state[1];
    float& analog_output_channel_2  = analog_output_port_state[2];
    float& analog_output_channel_3  = analog_output_port_state[3];

    // DAC state masks.
    uint8_t dac_ready;
    uint8_t dac_start;
    uint8_t dac_pause; // 1-bit: pause active channel, 0-bit: unpause paused channel
    uint8_t dac_abort;
    uint8_t dac_finished;

    // DAC DI Pin Modes
    uint8_t di_pin_modes[NUM_CHANNELS];
    uint8_t& di0_pin_mode = di_pin_modes[0];
    uint8_t& di1_pin_mode = di_pin_modes[1];
    uint8_t& di2_pin_mode = di_pin_modes[2];
    uint8_t& di3_pin_mode = di_pin_modes[3];

    // DI0-3 pin mapping to one or more analog output channels.
    // i.e: Which analog outputs are assigned to digital inputs 0-3.
    uint8_t input_to_ao_channel_mapping[NUM_CHANNELS];
    uint8_t& di0_to_ao_channel_mapping = input_to_ao_channel_mapping[0];
    uint8_t& di1_to_ao_channel_mapping = input_to_ao_channel_mapping[1];
    uint8_t& di2_to_ao_channel_mapping = input_to_ao_channel_mapping[2];
    uint8_t& di3_to_ao_channel_mapping = input_to_ao_channel_mapping[3];

    // External blanking masks per-channel. Assign channel[i] to one or more blanking pins.
    uint8_t channel_blanking_pins[NUM_CHANNELS];

    // Which player is active per channel.
    uint8_t active_players[NUM_CHANNELS];

    // Settings for each distinct player.
    FileSettings file_settings[NUM_CHANNELS];
    FunctionSettings sine_settings[NUM_CHANNELS];
    TrapezoidSettings trapezoid_settings[NUM_CHANNELS];
    //TrapezoidSettings (&sawtooth_settings)[NUM_CHANNELS] = trapezoid_settings;
    //TrapezoidSettings (&triangle_settings)[NUM_CHANNELS] = trapezoid_settings;

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

void write_dac_start(msg_t& msg);

void write_dac_pause(msg_t& msg);

void write_dac_abort(msg_t& msg);

void write_any_di_pin_mode(msg_t& msg);

void write_map_any_di_pin_to_ao_channels(msg_t& msg);

void write_any_channel_active_player(msg_t& msg);

void select_player(size_t channel, player_t player);



/**
 * \brief handler function to write any settings to a specific player type.
 * \details templated function such that it can be specialized to a specific
 *  player type.
 */
template <typename SETTINGS, SETTINGS* settings_arr, player_t PLAYER_ENUM>
void write_settings(msg_t& msg)
{
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& spec = HarpCore::reg_address_to_spec(msg.header.address);
    size_t i = ((SETTINGS*)spec.base_ptr - settings_arr);
    // Error if we try to change the specified channel while it's busy.
    if (bufs[i].is_transferring())
    {
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    HarpCore::copy_msg_payload_to_register(msg);
    // Re-setup the player if it is active. (i.e: apply_settings() and setup())
    if (player_t(app_regs.active_players[i]) == PLAYER_ENUM)
        select_player(i, PLAYER_ENUM);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}

// These are generated from the above function template
void write_any_file_settings(msg_t& msg);
void write_any_sine_settings(msg_t& msg);
void write_any_trapezoid_settings(msg_t& msg);

void read_any_waveform_hash(uint8_t address);

void write_any_waveform_data(msg_t& msg);

void apply_blanking_settings_to_ao_channel(uint32_t ao_channel);

void reset_app();


bool player_is_ready(size_t channel, player_t player_type);

void update_app();


/// Callbacks
void handle_digital_input();


template <typename DI_SETTING_TYPE>
void write_di_setting(msg_t& msg, DI_SETTING_TYPE* base_address)
{
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& spec = HarpCore::reg_address_to_spec(msg.header.address);
    size_t di_index = ((DI_SETTING_TYPE*)spec.base_ptr - base_address);
    // Error if we try to change the DI setting while any of its
    // assigned analog outputs are busy playing.
    for (size_t ao_chan = 0; ao_chan < NUM_CHANNELS; ++ao_chan)
    {
        // Skip checking unassigned analog output channels.
        if (!((app_regs.input_to_ao_channel_mapping[di_index] >> ao_chan) & 1u))
            continue;
        // Check if this analog output channel is busy.
        if (bufs[ao_chan].is_transferring())
        {
            if (!HarpCore::is_muted())
                HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    // Check if resulting blanking settings are valid.
    // i.e: 2 DI pins cannot blank the same AO pin.
    // i.e: do pairwise combination of all ao blanking masks to make sure any AO
    // channel is being blanked by at most 1 DI pin. (oof.)
    // Temporarily write the settings.
    // Save current settings.
    DI_SETTING_TYPE old_setting = base_address[di_index];
    HarpCore::copy_msg_payload_to_register(msg);
    bool valid_settings = true;
    for (size_t i = 0; i < NUM_DIS; ++i)
    {
        // Only check overlapping BLANKING settings.
        if (app_regs.di_pin_modes[i] != BLANKING)
            continue;
        for (size_t j = i+1; j < NUM_DIS; ++j)
        {
            if (app_regs.di_pin_modes[j] != BLANKING)
                continue;
            if (app_regs.input_to_ao_channel_mapping[i] &
                app_regs.input_to_ao_channel_mapping[j])
                valid_settings = false;
        }
    }
    if (!valid_settings)
    {
        // restore settings. Harp error.
        base_address[di_index] = old_setting;
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    // Apply any new blanking settings to any channels that have already been setup.
    for (size_t ao_chan = 0; ao_chan < NUM_CHANNELS; ++ao_chan)
    {
        player_t active_player = player_t(app_regs.active_players[ao_chan]);
        if (player_is_ready(ao_chan, active_player))
            apply_blanking_settings_to_ao_channel(ao_chan);
    }

    // Write the setting
    HarpCore::write_reg_generic(msg);

}


#endif // QUAC_H
