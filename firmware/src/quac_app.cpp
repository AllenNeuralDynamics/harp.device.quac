#include <quac_app.h>

app_regs_t app_regs;
queue_t ext_trigger_event_queue;

RegSpecs app_reg_specs[APP_REG_COUNT]
{
    {(uint8_t*)&app_regs.digital_output_port_state, sizeof(app_regs.digital_output_port_state), U8},
    {(uint8_t*)&app_regs.digital_output_port_set, sizeof(app_regs.digital_output_port_set), U8},
    {(uint8_t*)&app_regs.digital_output_port_clear, sizeof(app_regs.digital_output_port_clear), U8},

    {(uint8_t*)&app_regs.dac_external_triggers, sizeof(app_regs.dac_external_triggers), U8},

    {(uint8_t*)&app_regs.analog_output_port_state, sizeof(app_regs.analog_output_port_state), U16},
    {(uint8_t*)&app_regs.analog_output_channel_0, sizeof(app_regs.analog_output_channel_0), U16},
    {(uint8_t*)&app_regs.analog_output_channel_1, sizeof(app_regs.analog_output_channel_1), U16},
    {(uint8_t*)&app_regs.analog_output_channel_2, sizeof(app_regs.analog_output_channel_2), U16},
    {(uint8_t*)&app_regs.analog_output_channel_3, sizeof(app_regs.analog_output_channel_3), U16},

    {(uint8_t*)&app_regs.dac_ready, sizeof(app_regs.dac_ready), U8},
    {(uint8_t*)&app_regs.dac_start, sizeof(app_regs.dac_start), U8},
    {(uint8_t*)&app_regs.dac_pause, sizeof(app_regs.dac_pause), U8},
    {(uint8_t*)&app_regs.dac_abort, sizeof(app_regs.dac_abort), U8},
    {(uint8_t*)&app_regs.dac_finished, sizeof(app_regs.dac_finished), U8},

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
    {read_digital_output_port_state, write_digital_output_port_state},
    {HarpCore::read_from_write_only_reg_error, write_digital_output_port_set},
    {HarpCore::read_from_write_only_reg_error, write_digital_output_port_clear},

    {read_dac_external_triggers, write_dac_external_triggers},

    {read_analog_output_port_state; write_analog_output_port_state},
    {read_any_analog_output_channel; write_any_analog_output_channel},
    {read_any_analog_output_channel; write_any_analog_output_channel},
    {read_any_analog_output_channel; write_any_analog_output_channel},
    {read_any_analog_output_channel; write_any_analog_output_channel},

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


void read_digital_output_port_state(uint8_t address)
{
    app_regs.digital_output_port_state = uint8_t((DO_PORT_MASK & gpio_get_all64())
                                      >> DO_PORT_BASE);
    HarpCore::read_reg_generic(address);
}


void write_digital_output_port_state(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.digital_output_port_state = app_regs.digital_output_port_dir & app_regs.digital_output_port_state;
    uint64_t digital_output_mask = uint64_t(app_regs.digital_output_port_dir) << DO_PORT_BASE;
    uint64_t digital_output_state_mask = uint64_t(app_regs.digital_output_port_state) << DO_PORT_BASE;
    gpio_put_masked64(digital_output_mask, digital_output_state_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_digital_output_port_set(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.digital_output_port_set = app_regs.digital_output_port_dir & app_regs.digital_output_port_set;
    uint64_t digital_output_set_mask = uint64_t(app_regs.digital_output_port_set) << DO_PORT_BASE;
    gpio_put_masked64(digital_output_set_mask, digital_output_set_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_digital_output_port_clear(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    // Filter out pins specified as inputs.
    app_regs.digital_output_port_clear = app_regs.digital_output_port_dir & app_regs.digital_output_port_clear;
    uint64_t digital_output_clr_mask = uint64_t(app_regs.digital_output_port_clear) << DO_PORT_BASE;
    gpio_put_masked64(digital_output_clr_mask, 0);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dac_external_triggers(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_dac_external_triggers(msg_t& msg)
{
    // TODO: implement this.
}


void read_analog_output_port_state(uint8_t address)
{
    if (HarpCore::is_muted())
        return;
    // Ensure no channels are busy.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (!((app_regs.dac_start >> i) & 1u))
            continue;
        if (player.channel_is_busy(i))
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    // Update register contents.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] = dacs[i].get_last_value();
    HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_analog_output_port_state(msg_t& msg)
{
    // Ensure no channels are busy.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (player.channel_is_busy(i))
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    HarpCore::copy_msg_payload_to_register(msg);
    // Apply the write.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        dacs[i].write_value(app_regs.analog_output_port_state[i]);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_any_analog_output_channel(uint8_t address)
{
    if (HarpCore::is_muted())
        return;
    // Convert address to output channel.
    uint32_t channel = address - AO_CHANNEL_BASE_ADDRESS;
    if (player.channel_is_busy(channel))
    {
        HarpCore::send_harp_reply(READ_ERROR, address);
        return;
    }
    app_regs.analog_output_port_state[channel] = dacs[channel].get_last_value();
}


void write_any_analog_output_channel(msg_t& msg)
{
    // Convert address to output channel.
    uint32_t channel = msg.header.address - AO_CHANNEL_BASE_ADDRESS;
    if (player.channel_is_busy(channel))
    {
        if (!HarpCore::is_muted());
            HarCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    HarpCore::copy_msg_payload_to_register(msg);
    // FYI: also updates the individual register representation bc it's a ref.
    dacs[channel].write_value(app_regs.analog_output_port_state[channel]);
    if (!HarpCore::is_muted())
        HarCore::send_harp_reply(WRITE, address);
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
    // Ensure specified channels are ready.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (!((app_regs.dac_start >> i) & 1u))
            continue;
        if (!player.channel_is_ready(i))
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    player.start(uint32_t(app_regs.dac_start)); // Can be started from core1.
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
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
    // WriteError if we try to change the specified channel while it's busy.
    // Convert address to output channel.
    uint32_t channel = address - AO_CHANNEL_BASE_ADDRESS;
    if (player.channel_is_busy(channel)
    {
        HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    HarpCore::copy_msg_payload_to_register();
    // TODO: Send waveform settings to core1.
    // ...
    // ...
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(msg.header.address);
}


void read_any_waveform_hash(uint8_t address)
{
    // Assumes we have connected to the SD card on reset and keep this hash
    // updated each time we write a new waveform to the card via Harp interface.
    HarpCore::read_reg_generic(address);
}


void write_any_waveform_data(msg_t& msg)
{
    irq_set_enabled(IO_IRQ_BANK0, false); // disable external triggers.
    // TODO: implement this. Will need to be pseudo-Harp spec.
    // ...
    // also update waveform hash.
    // ...
    irq_set_enabled(IO_IRQ_BANK0, true); // reenable external triggers.
}

void update_app()
{
    // TODO: poll external triggers.
    // Start any externally triggered DACs if they are armed.

    // Send Harp replies for externally-triggered events.
    ext_trigger_event_t trigger_event;
    if (HarpCore::is_muted()) // Drain the queue and exit.
    {
        while (queue_try_remove(&ext_trigger_event_queue, &trigger_event)){}
        return;
    }
    while (queue_try_remove(&ext_trigger_event_queue, &trigger_event))
    {
        app_regs.dac_start = trigger_event.start_mask;
        HarpCore::send_harp_reply(EVENT, DAC_START_ADDRESS,
                                  system_to_harp_us_64(trigger_event.timestamp));
    }
}

void reset_app()
{
    for (size_t i = DI_PORT_BASE; i < DI_PORT_BASE + NUM_DIS; ++i)
        gpio_init(i);
    for (size_t i = DO_PORT_BASE; i < DO_PORT_BASE + NUM_DOS; ++i)
        gpio_init(i);

    // Reset External Triggers to all-inputs.
    gpio_set_dir_masked64(DI_PORT_BASE, 0); // 0-bit: input.

    // Reset Digital Outputs.
    gpio_set_dir_masked64(DO_PORT_MASK, DO_PORT_MASK); // 1-bit: output.
    gpio_set_put_masked64(digital_output_mask, 0); // Set all outputs LOW.

    memset(&app_regs.waveform_hashes[0], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[1], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[2], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[3], 0, SHA256_NUM_BYTES);
    // TODO: open SD card, find hash files, update Harp reg hashes as needed.

    // FYI: PIO_LTC264x instances manage GPIO pin function.
    for (const auto& dac: dacs)
        dacs.write_value(DAC_MIDSCALE);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] = DAC_MIDSCALE;
    // Reset Waveform trigger settings.
    for (size_t i = 0; I < NUM_CHANNELS)
    {
        auto& settings = dac_settings[i];
        settings.cycles = 1; // play once: "single-shot."
        settings.sample_count = 0; // Play everything.
        settings.frequency_hz = MultiFilePlayer::DEFAULT_FREQUENCY_HZ; // 500KHz
        settings.external_trigger_mask = (1u << i); // DI[i] triggers AO[i].
    }

    // TODO: reset file player.

    // Setup External Trigger Callback
    // Enable all External Trigger GPIOS to trigger the callback.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        gpio_set_irq_enabled(i + DI_PORT_BASE, GPIO_IRQ_EDGE_RISE, true);
    irq_add_shared_handler(IO_IRQ_BANK0, handle_external_trigger,
                           GPIO_IRQ_CALLBACK_ORDER_PRIORITY);
    irq_set_enabled(IO_IRQ_BANK0, true);
}

void handle_external_trigger()
{
    // Start the waveform per the 1s in the channel.
    // Filter out the busy channels first.
    // TODO: consider an app_regs.IgnoredTriggers register?
    uint32_t trigger_mask = ((gpio_get_all64() & DI_PORT_MASK) >> DI_PORT_BASE);
    // Combine waveform settings external triggers to the final mask.
    start_mask = 0;
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (player.channel_is_busy(i))
            continue;
        // Check if any currently HIGH pins would trigger this channel.
        if (trigger_mask & app_regs.dac_settings[i].external_trigger_mask)
            start_mask |= (1u << i);
    }
    start_mask &= composite_mask;
    ext_trigger_event_t trigger_event;
    player.start(start_mask); // Can be started from core1.
    trigger_event.channel_mask = start_mask;
    trigger_event.timestamp = time_us_64();
    queue_try_add(&ext_trigger_event_queue, &trigger_event);
    // Push harp message
}
