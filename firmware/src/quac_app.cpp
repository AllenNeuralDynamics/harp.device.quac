#include <quac_app.h>

app_regs_t app_regs;

RegSpecs app_reg_specs[APP_REG_COUNT]
{
    {(uint8_t*)&app_regs.dio_port_dir, sizeof(app_regs.dio_port_dir), U8},
    {(uint8_t*)&app_regs.dio_port_state, sizeof(app_regs.dio_port_state), U8},
    {(uint8_t*)&app_regs.dio_port_set, sizeof(app_regs.dio_port_set), U8},
    {(uint8_t*)&app_regs.dio_port_clear, sizeof(app_regs.dio_port_clear), U8},

    {(uint8_t*)&app_regs.dac_external_triggers, sizeof(app_regs.dac_external_triggers), U8},

    {(uint8_t*)&app_regs.dac_ready, sizeof(app_regs.dac_ready), U8},
    {(uint8_t*)&app_regs.dac_start, sizeof(app_regs.dac_start), U8},
    {(uint8_t*)&app_regs.dac_pause, sizeof(app_regs.dac_pause), U8},
    {(uint8_t*)&app_regs.dac_abort, sizeof(app_regs.dac_abort), U8},

    {(uint8_t*)&app_regs.dac_settings[0], sizeof(WaveformSettings), U8},
    {(uint8_t*)&app_regs.dac_settings[1], sizeof(WaveformSettings), U8},
    {(uint8_t*)&app_regs.dac_settings[2], sizeof(WaveformSettings), U8},
    {(uint8_t*)&app_regs.dac_settings[3], sizeof(WaveformSettings), U8},

    {(uint8_t*)&app_regs.waveform_hashes[0], SHA256_NUM_BYTES, U8},
    {(uint8_t*)&app_regs.waveform_hashes[1], SHA256_NUM_BYTES, U8},
    {(uint8_t*)&app_regs.waveform_hashes[2], SHA256_NUM_BYTES, U8},
    {(uint8_t*)&app_regs.waveform_hashes[3], SHA256_NUM_BYTES, U8},

    // Note: WAVEFORM_MAX_BYTES cannot be coerced into RegSpecs type.
    // num_bytes should be WAVEFORM_MAX_BYTES
    {(uint8_t*)&app_regs.waveform_hashes[0], 1, U8},
    {(uint8_t*)&app_regs.waveform_hashes[0], 1, U8},
    {(uint8_t*)&app_regs.waveform_hashes[0], 1, U8},
    {(uint8_t*)&app_regs.waveform_hashes[0], 1, U8},
};


RegFnPair reg_handler_fns[APP_REG_COUNT]
{
    {read_dio_port_dir, write_dio_port_dir},
    {read_dio_port_state, write_dio_port_state},
    {read_dio_port_set, write_dio_port_set},
    {read_dio_port_clear, write_dio_port_clear},

    {read_dac_external_triggers, write_dac_external_triggers},

    {read_dac_ready, HarpCore::write_to_read_only_reg_error},
    {read_dac_start, write_dac_start},
    {read_dac_pause, write_dac_pause},
    {read_dac_abort, write_dac_abort},
    {read_dac_finished, HarpCore::write_to_read_only_reg_error},

    {read_any_dac_settings, write_any_dac_settings},
    {read_any_dac_settings, write_any_dac_settings},
    {read_any_dac_settings, write_any_dac_settings},
    {read_any_dac_settings, write_any_dac_settings},

    {read_any_waveform_hash, HarpCore::write_to_read_only_reg_error},
    {read_any_waveform_hash, HarpCore::write_to_read_only_reg_error},
    {read_any_waveform_hash, HarpCore::write_to_read_only_reg_error},
    {read_any_waveform_hash, HarpCore::write_to_read_only_reg_error},

    {HarpCore::read_from_write_only_reg_error, write_any_waveform_data},
    {HarpCore::read_from_write_only_reg_error, write_any_waveform_data},
    {HarpCore::read_from_write_only_reg_error, write_any_waveform_data},
    {HarpCore::read_from_write_only_reg_error, write_any_waveform_data},
};


void read_dio_port_dir(uint8_t address)
{
    // Assume we reset to all-inputs.
    HarpCore::read_reg_generic(address);
}


void write_dio_port_dir(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    uint64_t dio_mask = uint64_t(app_regs.dio_port_dir) << DIO_PORT_BASE;
    gpio_set_dir_masked64(dio_mask, dio_mask); // 0-bits: input; 1-bits: output
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dio_port_state(uint8_t address)
{
    app_regs.dio_port_state = uint8_t((DIO_PORT_MASK & gpio_get_all64())
                                      >> DIO_PORT_BASE);
    HarpCore::read_reg_generic(address);
}


void write_dio_port_state(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.dio_port_state = app_regs.dio_port_dir & app_regs.dio_port_state;
    uint64_t dio_mask = uint64_t(app_regs.dio_port_dir) << DIO_PORT_BASE;
    uint64_t dio_state_mask = uint64_t(app_regs.dio_port_state) << DIO_PORT_BASE;
    gpio_put_masked64(dio_mask, dio_state_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dio_port_set(uint8_t address)
{HarpCore::read_reg_generic(address);} // should really be a READ ERROR


void write_dio_port_set(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.dio_port_set = app_regs.dio_port_dir & app_regs.dio_port_set;
    uint64_t dio_set_mask = uint64_t(app_regs.dio_port_set) << DIO_PORT_BASE;
    gpio_put_masked64(dio_set_mask, dio_set_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}

void read_dio_port_clear(uint8_t address)
{HarpCore::read_reg_generic(address);} // should really be a READ ERROR


void write_dio_port_clear(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.dio_port_clear = app_regs.dio_port_dir & app_regs.dio_port_clear;
    uint64_t dio_clr_mask = uint64_t(app_regs.dio_port_clear) << DIO_PORT_BASE;
    gpio_put_masked64(dio_clr_mask, 0);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dac_external_triggers(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_dac_external_triggers(msg_t& msg)
{
    // TODO: implement this.
}

void read_dac_ready(uint8_t address)
{
    // TODO: implement this.
}


void read_dac_start(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_dac_start(msg_t& msg)
{
    // TODO: handle paused logic.
    HarpCore::copy_msg_payload_to_register(msg);
    player.start(uint32_t(app_regs.dac_start));
}


void read_dac_pause(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_dac_pause(msg_t& msg)
{
    // TODO: implement this.
}


void read_dac_abort(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_dac_abort(msg_t& msg)
{
    // TODO: implement this.
}


void read_dac_finished(uint8_t address)
{
    // TODO: implement this.
}


void read_any_dac_settings(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_any_dac_settings(msg_t& msg)
{
    // TODO: implement this.
}


void read_any_waveform_hash(uint8_t address)
{
    // TODO: First ensure hash is up-to-date from the card.
    HarpCore::read_reg_generic(address);
}


void write_any_waveform_data(msg_t& msg)
{
    // TODO: will need a custom implementation.
}

void update_app()
{
    // TODO: poll external triggers.
    // Start any externally triggered DACs if they are armed.
}

void reset_app()
{
    // Reset DIO pins all-inputs.
    for (size_t i = DIO_PORT_BASE; i < DIO_PORT_BASE + 4; ++i)
        gpio_init(i);
    gpio_set_dir_masked64(DIO_PORT_MASK, 0); // 0-bits: input; 1-bits: output

    // TODO: Reset External Triggers to all-inputs.

    // FYI: PIO_LTC264x instances manage GPIO pin function.

    // TODO: reset file player.
}
