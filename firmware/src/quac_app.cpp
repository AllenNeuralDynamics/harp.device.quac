#include <quac_app.h>
#include <sd_write_buffer.h>
#include <hardware/sha256.h>
#include <cstring>

app_regs_t app_regs;
queue_t ext_trigger_event_queue;

// File-scoped state for the active SD write session (one channel at a time).
static SdWriteBuffer sd_writer;
static FIL write_file;
static int active_channel = -1;
static size_t bytes_written = 0;

RegSpecs app_reg_specs[APP_REG_COUNT]
{
    {(uint8_t*)&app_regs.digital_output_port_state, sizeof(app_regs.digital_output_port_state), U8},
    {(uint8_t*)&app_regs.digital_output_port_set, sizeof(app_regs.digital_output_port_set), U8},
    {(uint8_t*)&app_regs.digital_output_port_clear, sizeof(app_regs.digital_output_port_clear), U8},

    {(uint8_t*)&app_regs.ext_trigger_state, sizeof(app_regs.ext_trigger_state), U8},

    {(uint8_t*)&app_regs.analog_output_port_state, sizeof(app_regs.analog_output_port_state), U16},
    {(uint8_t*)&app_regs.analog_output_channel_0, sizeof(T), U16},
    {(uint8_t*)&app_regs.analog_output_channel_1, sizeof(T), U16},
    {(uint8_t*)&app_regs.analog_output_channel_2, sizeof(T), U16},
    {(uint8_t*)&app_regs.analog_output_channel_3, sizeof(T), U16},

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

    // RegSpecs::num_bytes is uint8_t so WAVEFORM_MAX_BYTES cannot be expressed
    // here. The write handler streams the full payload directly; this spec
    // only needs to identify the register and its element type.
    {(uint8_t*)&app_regs.waveform_data[0], sizeof(T), U16},
    {(uint8_t*)&app_regs.waveform_data[1], sizeof(T), U16},
    {(uint8_t*)&app_regs.waveform_data[2], sizeof(T), U16},
    {(uint8_t*)&app_regs.waveform_data[3], sizeof(T), U16},
};


RegFnPair reg_handler_fns[APP_REG_COUNT]
{
    {read_digital_output_port_state, write_digital_output_port_state},
    {HarpCore::read_from_write_only_reg_error, write_digital_output_port_set},
    {HarpCore::read_from_write_only_reg_error, write_digital_output_port_clear},

    {read_ext_trigger_state, HarpCore::write_to_read_only_reg_error},

    {read_analog_output_port_state, write_analog_output_port_state},
    {read_any_analog_output_channel, write_any_analog_output_channel},
    {read_any_analog_output_channel, write_any_analog_output_channel},
    {read_any_analog_output_channel, write_any_analog_output_channel},
    {read_any_analog_output_channel, write_any_analog_output_channel},

    {read_dac_ready, HarpCore::write_to_read_only_reg_error},
    {HarpCore::read_from_write_only_reg_error, write_dac_start},
    {read_dac_pause, write_dac_pause},
    {read_dac_abort, write_dac_abort},
    {HarpCore::read_from_write_only_reg_error, HarpCore::write_to_read_only_reg_error},

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
    app_regs.digital_output_port_state =
        uint8_t((DO_PORT_MASK & gpio_get_all64()) >> DO_PORT_BASE);
    HarpCore::read_reg_generic(address);
}


void write_digital_output_port_state(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    uint64_t digital_output_state_mask =
        uint64_t(app_regs.digital_output_port_state) << DO_PORT_BASE;
    gpio_put_masked64(DO_PORT_MASK, digital_output_state_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_digital_output_port_set(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    uint64_t digital_output_set_mask =
        uint64_t(app_regs.digital_output_port_set) << DO_PORT_BASE;
    gpio_put_masked64(digital_output_set_mask, digital_output_set_mask);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_digital_output_port_clear(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg);
    uint64_t digital_output_clr_mask = uint64_t(app_regs.digital_output_port_clear) << DO_PORT_BASE;
    gpio_put_masked64(digital_output_clr_mask, 0);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_ext_trigger_state(uint8_t address)
{
    app_regs.ext_trigger_state =
        uint8_t((EXT_TRIGGER_MASK & gpio_get_all64()) >> EXT_TRIGGER_BASE);
    HarpCore::read_reg_generic(address);
}


void read_analog_output_port_state(uint8_t address)
{
    if (HarpCore::is_muted())
        return;
    // Ensure no channels are busy.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (player.channel_is_busy(i))
        {
            HarpCore::send_harp_reply(READ_ERROR, address);
            return;
        }
    }
    // Update register contents.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] = dacs[i].get_last_value();
    HarpCore::send_harp_reply(READ, address);
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
    // Convert address to output channel with pointer arithmetic.
    const RegSpecs& specs = HarpCore::reg_address_to_specs(address);
    size_t channel = ((uint16_t*)specs.base_ptr - app_regs.analog_output_port_state);
    if (player.channel_is_busy(channel))
    {
        HarpCore::send_harp_reply(READ_ERROR, address);
        return;
    }
    app_regs.analog_output_port_state[channel] = dacs[channel].get_last_value();
    HarpCore::send_harp_reply(READ, address);
}


void write_any_analog_output_channel(msg_t& msg)
{
    // Convert address to output channel with pointer arithmetic.
    const RegSpecs& specs = HarpCore::reg_address_to_specs(msg.header.address);
    size_t channel = ((uint16_t*)specs.base_ptr - app_regs.analog_output_port_state);
    if (player.channel_is_busy(channel)) // FIXME: launch core1
    {
        if (!HarpCore::is_muted());
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    HarpCore::copy_msg_payload_to_register(msg);
    // FYI: also updates the individual register representation bc it's a ref.
    dacs[channel].write_value(app_regs.analog_output_port_state[channel]);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dac_ready(uint8_t address)
{
    // TODO: implement this.
}


void write_dac_start(msg_t& msg)
{
    // TODO: handle paused logic.
    HarpCore::copy_msg_payload_to_register(msg);
    // Ensure specified channels are ready.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (!((app_regs.dac_start >> i) & 1u)) // Skip untriggered channels.
            continue;
        // Error if any specified channel is not ready.
        if (!player.channel_is_ready(i))
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    player.start(uint32_t(app_regs.dac_start)); // Can be started from core0.
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


void read_any_dac_settings(uint8_t address)
{HarpCore::read_reg_generic(address);}


void write_any_dac_settings(msg_t& msg)
{
    // WriteError if we try to change the specified channel while it's busy.
    // Convert address to output channel with pointer arithmetic.
    const RegSpecs& specs = HarpCore::reg_address_to_specs(msg.header.address);
    size_t channel = ((uint16_t*)specs.base_ptr - app_regs.analog_output_port_state);
    if (player.channel_is_busy(channel))
    {
        HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    HarpCore::copy_msg_payload_to_register(msg);
    // TODO: Send waveform settings to core1.
    // ...
    // ...
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
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

    // waveform_data registers occupy app reg indices 22-25 (one per channel).
    constexpr uint8_t WAVEFORM_DATA_BASE_ADDR = APP_REG_START_ADDRESS + 22;
    uint8_t channel = msg.header.address - WAVEFORM_DATA_BASE_ADDR;
    if (channel >= NUM_CHANNELS)
    {
        irq_set_enabled(IO_IRQ_BANK0, true);
        return;
    }

    const uint8_t* data = static_cast<const uint8_t*>(msg.payload);
    size_t len = msg.payload_length();
    size_t expected_bytes =
        static_cast<size_t>(app_regs.dac_settings[channel].sample_count) * sizeof(T);

    // Cannot write without a valid sample count.
    if (expected_bytes == 0)
    {
        irq_set_enabled(IO_IRQ_BANK0, true);
        return;
    }

    // Begin a new write session when none is active.
    if (!sd_writer.is_active())
    {
        FRESULT fr = f_open(&write_file, filenames[channel],
                            FA_WRITE | FA_CREATE_ALWAYS);
        if (fr != FR_OK)
        {
            irq_set_enabled(IO_IRQ_BANK0, true);
            return;
        }
        sd_writer.begin(&write_file);
        active_channel = static_cast<int>(channel);
        bytes_written = 0;
    }
    else if (active_channel != static_cast<int>(channel))
    {
        // A different channel is already being written; reject.
        irq_set_enabled(IO_IRQ_BANK0, true);
        return;
    }

    // Clamp to remaining expected bytes so we never over-write.
    size_t remaining = expected_bytes - bytes_written;
    if (len > remaining)
        len = remaining;

    if (!sd_writer.write(data, len))
    {
        sd_writer.abort();
        f_close(&write_file);
        active_channel = -1;
        irq_set_enabled(IO_IRQ_BANK0, true);
        return;
    }
    bytes_written += len;

    // Finalize once all expected samples have arrived.
    if (bytes_written >= expected_bytes)
    {
        sha256_result_t result;
        if (sd_writer.finalize(result))
        {
            // Store the first SHA256_NUM_BYTES bytes in the app register.
            memcpy(app_regs.waveform_hashes[channel], result.bytes, SHA256_NUM_BYTES);

            // Persist the full 32-byte hash to its own file on the SD card.
            FIL hash_file;
            FRESULT fr = f_open(&hash_file, sha256_filenames[channel],
                                FA_WRITE | FA_CREATE_ALWAYS);
            if (fr == FR_OK)
            {
                UINT written_bytes;
                f_write(&hash_file, result.bytes, SHA256_RESULT_BYTES, &written_bytes);
                f_close(&hash_file);
            }
        }
        f_close(&write_file);
        active_channel = -1;
    }

    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);

    irq_set_enabled(IO_IRQ_BANK0, true); // reenable external triggers.
}


void update_app()
{
    // FYI: external triggers are handled via interrupts on this core,
    // But events capturing when they started (or if they errored out)
    // are collected and dispatched over USB in this loop.

    // Send Harp replies for externally-triggered events.
    ext_trigger_event_t trigger_event;
    end_of_transfer_event_t transfer_done_event;
    // Bail early if we're muted. Drain all queues and exit.
    if (HarpCore::is_muted())
    {
        while (queue_try_remove(&ext_trigger_event_queue, &trigger_event)){}
        while (player.get_finished_transfers(&transfer_done_event)){}
        return;
    }
    // Dispatch any externally-triggered transfer-started events.
    while (queue_try_remove(&ext_trigger_event_queue, &trigger_event))
    {
        app_regs.dac_start = trigger_event.channel_start_mask;
        HarpCore::send_harp_reply(EVENT, DAC_START_ADDRESS,
            HarpCore::system_to_harp_us_64(trigger_event.timestamp));
    }
    // Dispatch any transfer-finished events.
    while (player.get_finished_transfers(&transfer_done_event))
    {
        app_regs.dac_finished = uint8_t(transfer_done_event.finished_channels_mask);
        HarpCore::send_harp_reply(EVENT, DAC_FINISHED_ADDRESS,
            HarpCore::system_to_harp_us_64(transfer_done_event.timestamp_us));
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
    gpio_put_masked64(DO_PORT_MASK, 0); // Set all outputs LOW.

    memset(&app_regs.waveform_hashes[0], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[1], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[2], 0, SHA256_NUM_BYTES);
    memset(&app_regs.waveform_hashes[3], 0, SHA256_NUM_BYTES);
    // TODO: open SD card, find hash files, update Harp reg hashes as needed.

    // FYI: PIO_LTC264x instances manage GPIO pin function.
    const T& DAC_MIDSCALE =
        MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE>::OUTPUT_MIDSCALE;
    const size_t DEFAULT_FREQUENCY_HZ =
        MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE>::DEFAULT_FREQUENCY_HZ;

    for (auto& dac: dacs)
        dac.write_value(DAC_MIDSCALE);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] = DAC_MIDSCALE;
    // Reset Waveform trigger settings.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        auto& settings = app_regs.dac_settings[i];
        settings.cycles = 1; // play once: "single-shot."
        settings.sample_count = 0; // Play everything.
        settings.frequency_hz = DEFAULT_FREQUENCY_HZ;
        settings.external_trigger_mask = (1u << i); // DI[i] triggers AO[i].
    }
    multicore_reset_core1(); // Ensure core1 is not updating the player first.
    player.reset();
    player.set_frequency_hz(500'000);
    player.setup(); // FIXME: Locks up if the files don't exist.
    // Launch core1.
    (void)multicore_fifo_pop_blocking(); // Wait until core1 is ready.
    multicore_launch_core1(core1main);

    // Setup External Trigger Callback
    // Enable all External Trigger GPIOS to trigger the callback.
    irq_set_exclusive_handler(IO_IRQ_BANK0, handle_external_trigger);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        gpio_set_irq_enabled(i + DI_PORT_BASE, GPIO_IRQ_EDGE_RISE, true);
    irq_set_enabled(IO_IRQ_BANK0, true);

    uint32_t LED_MASK = (1u << DEBUG_LEDS[0]) | (1u << DEBUG_LEDS[1]);
    gpio_init_mask(LED_MASK);
    gpio_set_dir_masked(LED_MASK, 0xFFFFFFFF); // 1: output.
    gpio_put_masked(LED_MASK, 0);
}

// FIXME: this ISR will fire for every rising edge event on said pins--event if
// all channels are busy.
void __not_in_flash_func(handle_external_trigger)()
{
    uint32_t LED_MASK = (1u << DEBUG_LEDS[0]) | (1u << DEBUG_LEDS[1]);
    gpio_put_masked(LED_MASK, ~gpio_get_all()); // Toggle LEDs.

    // Note: we must read gpios here directly because we are not on a clean
    // multiple of 8-boundary, so it's cumbersome to assemble the interrupt
    // state from multiple reads.
    // Start the waveform per the 1s in the channel.
    // Filter out the busy channels first.
    uint32_t trigger_mask = ((gpio_get_all64() & DI_PORT_MASK) >> DI_PORT_BASE);
    // Combine waveform settings external triggers to the final mask.
    uint32_t start_mask = 0;
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (player.channel_is_busy(i)) // Skip re-triggering busy channels.
            continue;
        // Check if any currently HIGH pins would trigger this channel.
        if (trigger_mask & app_regs.dac_settings[i].external_trigger_mask)
            start_mask |= (1u << i);
    }
    if (start_mask != 0)
    {
        ext_trigger_event_t trigger_event;
        player.start(start_mask); // Can be started from core1.
        trigger_event.channel_start_mask = start_mask;
        trigger_event.timestamp = time_us_64();
        // Push harp message
        queue_try_add(&ext_trigger_event_queue, &trigger_event);
    }
    // Acknowledge the interrupt. Assume nothing else is setting these pins.
    // Clear the INTR[n] state since we dealt with all pin changes.
    // Clear by "writing a 1" to the set bits.
    io_bank0_hw->intr[DI_PORT_BASE >> 3] = 0xFFFFFFFF; // >> 3: floor-divide by 8
    // Do it twice since we are not on a clean multiple of 8 boundary.
    io_bank0_hw->intr[(DI_PORT_BASE + NUM_CHANNELS) >> 3] = 0xFFFFFFFF;
}
