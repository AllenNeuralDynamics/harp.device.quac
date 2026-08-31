#include "quac_app.h"
#include "pio_ltc264x.h"
#include "waveform_settings.h"

app_regs_t app_regs;
queue_t ext_trigger_event_queue;


RegSpec app_reg_specs[]
{
    RegSpec::U8(&app_regs.digital_output_port_state,
        read_digital_output_port_state, write_digital_output_port_state),
    RegSpec::U8(&app_regs.digital_output_port_set,
        HarpCore::read_reg_error, write_digital_output_port_set),
    RegSpec::U8(&app_regs.digital_output_port_clear,
        HarpCore::read_reg_error, write_digital_output_port_clear),
    RegSpec::U8(&app_regs.ext_trigger_state,
        read_ext_trigger_state, HarpCore::write_reg_error),

    RegSpec::FloatArray(&app_regs.analog_output_port_state,
        std::size(app_regs.analog_output_port_state),
        read_analog_output_port_state, write_analog_output_port_state),
    RegSpec::Float(&app_regs.analog_output_channel_0,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::Float(&app_regs.analog_output_channel_1,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::Float(&app_regs.analog_output_channel_2,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::Float(&app_regs.analog_output_channel_3,
        read_any_analog_output_channel, write_any_analog_output_channel),

    RegSpec::U8(&app_regs.dac_ready,
        read_dac_ready, HarpCore::write_reg_error),
    RegSpec::U8(&app_regs.dac_start,
        HarpCore::read_reg_error, write_dac_start),
    RegSpec::U8(&app_regs.dac_pause,
        HarpCore::read_reg_generic, write_dac_pause),
    RegSpec::U8(&app_regs.dac_abort,
        HarpCore::read_reg_error, write_dac_abort),
    RegSpec::U8(&app_regs.dac_finished,
        HarpCore::read_reg_error, HarpCore::write_reg_error),

    RegSpec::U8(&app_regs.channel_external_triggers[0],
        HarpCore::read_reg_generic, write_any_channel_external_triggers),
    RegSpec::U8(&app_regs.channel_external_triggers[1],
        HarpCore::read_reg_generic, write_any_channel_external_triggers),
    RegSpec::U8(&app_regs.channel_external_triggers[2],
        HarpCore::read_reg_generic, write_any_channel_external_triggers),
    RegSpec::U8(&app_regs.channel_external_triggers[3],
        HarpCore::read_reg_generic, write_any_channel_external_triggers),

    RegSpec::U8(&app_regs.active_players[0],
        HarpCore::read_reg_generic, write_any_channel_active_player),
    RegSpec::U8(&app_regs.active_players[1],
        HarpCore::read_reg_generic, write_any_channel_active_player),
    RegSpec::U8(&app_regs.active_players[2],
        HarpCore::read_reg_generic, write_any_channel_active_player),
    RegSpec::U8(&app_regs.active_players[3],
        HarpCore::read_reg_generic, write_any_channel_active_player),

    RegSpec::U8Array(&app_regs.file_settings[0], sizeof(FileSettings),
        HarpCore::read_reg_generic, write_any_file_settings),
    RegSpec::U8Array(&app_regs.file_settings[1], sizeof(FileSettings),
        HarpCore::read_reg_generic, write_any_file_settings),
    RegSpec::U8Array(&app_regs.file_settings[2], sizeof(FileSettings),
        HarpCore::read_reg_generic, write_any_file_settings),
    RegSpec::U8Array(&app_regs.file_settings[3], sizeof(FileSettings),
        HarpCore::read_reg_generic, write_any_file_settings),

    RegSpec::U8Array(&app_regs.sine_settings[0], sizeof(FunctionSettings),
        HarpCore::read_reg_generic, write_any_sine_settings),
    RegSpec::U8Array(&app_regs.sine_settings[1], sizeof(FunctionSettings),
        HarpCore::read_reg_generic, write_any_sine_settings),
    RegSpec::U8Array(&app_regs.sine_settings[2], sizeof(FunctionSettings),
        HarpCore::read_reg_generic, write_any_sine_settings),
    RegSpec::U8Array(&app_regs.sine_settings[3], sizeof(FunctionSettings),
        HarpCore::read_reg_generic, write_any_sine_settings),

    RegSpec::U8Array(&app_regs.trapezoid_settings[0], sizeof(TrapezoidSettings),
        HarpCore::read_reg_generic, write_any_trapezoid_settings),
    RegSpec::U8Array(&app_regs.trapezoid_settings[1], sizeof(TrapezoidSettings),
        HarpCore::read_reg_generic, write_any_trapezoid_settings),
    RegSpec::U8Array(&app_regs.trapezoid_settings[2], sizeof(TrapezoidSettings),
        HarpCore::read_reg_generic, write_any_trapezoid_settings),
    RegSpec::U8Array(&app_regs.trapezoid_settings[3], sizeof(TrapezoidSettings),
        HarpCore::read_reg_generic, write_any_trapezoid_settings),

    // TODO: File Waveform blobs.
};

const size_t APP_REG_COUNT = std::size(app_reg_specs);

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
        if (bufs[i].is_transferring())
        {
            HarpCore::send_harp_reply(READ_ERROR, address);
            return;
        }
    }
    // Update register contents.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        app_regs.analog_output_port_state[i] =
            FunctionSettings::volts_16bit_to_float(dacs[i].get_last_value());
    }
    HarpCore::send_harp_reply(READ, address);
}


void write_analog_output_port_state(msg_t& msg)
{
    const float& MIN_VOLTS = FunctionSettings::MIN_VOLTS;
    const float& MAX_VOLTS = FunctionSettings::MAX_VOLTS;
    // Ensure no channels are busy.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (bufs[i].is_transferring())
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    // Limits check:
    float old_outputs[NUM_CHANNELS];
    memcpy(old_outputs, app_regs.analog_output_port_state, sizeof(old_outputs));
    HarpCore::copy_msg_payload_to_register(msg);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        float& value = app_regs.analog_output_port_state[i];
        if (value < MIN_VOLTS || value > MAX_VOLTS)
        {
            // Restore old value.
            memcpy(app_regs.analog_output_port_state, old_outputs, sizeof(old_outputs));
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    // Apply the write.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        float& volts = app_regs.analog_output_port_state[i];
        uint16_t volts_16bit = FunctionSettings::volts_float_to_16bit(volts);
        dacs[i].write_value(volts_16bit);
    }
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_any_analog_output_channel(uint8_t address)
{
    if (HarpCore::is_muted())
        return;
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& specs = HarpCore::reg_address_to_spec(address);
    size_t channel = ((float*)specs.base_ptr - app_regs.analog_output_port_state);
    if (bufs[channel].is_transferring())
    {
        HarpCore::send_harp_reply(READ_ERROR, address);
        return;
    }
    app_regs.analog_output_port_state[channel] =
        FunctionSettings::volts_16bit_to_float(dacs[channel].get_last_value());
    HarpCore::send_harp_reply(READ, address);
}


void write_any_analog_output_channel(msg_t& msg)
{
    const float& MIN_VOLTS = FunctionSettings::MIN_VOLTS;
    const float& MAX_VOLTS = FunctionSettings::MAX_VOLTS;
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& specs = HarpCore::reg_address_to_spec(msg.header.address);
    size_t channel = ((float*)specs.base_ptr - app_regs.analog_output_port_state);
    if (bufs[channel].is_transferring())
    {
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    // Limits Check
    float old_value = app_regs.analog_output_port_state[channel];
    HarpCore::copy_msg_payload_to_register(msg);
    float& volts = app_regs.analog_output_port_state[channel];
    if (volts < MIN_VOLTS || volts > MAX_VOLTS)
    {
        // Restore old value.
        app_regs.analog_output_port_state[channel] = old_value;
        HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    // FYI: also updates the individual register representation bc it's a ref.
    uint16_t volts_16bit = FunctionSettings::volts_float_to_16bit(volts);
    dacs[channel].write_value(volts_16bit);
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void read_dac_ready(uint8_t address)
{
    // Aggregate is_ready commands for active channels.
    uint32_t dac_ready = 0;
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        player_t active_player = player_t(app_regs.active_players[i]);
        if (player_is_ready(i, active_player))
            dac_ready |= (1u << i);
        else
            dac_ready &= ~(1u << i);
    }
    app_regs.dac_ready = static_cast<uint8_t>(dac_ready);
    HarpCore::read_reg_generic(address);
}


void write_dac_start(msg_t& msg)
{
    // TODO: handle paused logic.
    // Ensure specified channels are ready.
    uint32_t started_dacs = app_regs.dac_start;
    HarpCore::copy_msg_payload_to_register(msg); // update dac_start
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        bool error = false;
        if (!((app_regs.dac_start >> i) & 1u)) // Skip untriggered channels.
            continue;
        // Error if the player has already been started but hasn't finished.
        if ((started_dacs >> i) & 1u)
            error = true;
        // Error if any specified channel is not ready.
        if (!player_is_ready(i, player_t(app_regs.active_players[i])))
            error = true;
        if (error)
        {
            if (!HarpCore::is_muted())
                HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            // Restore original register value.
            app_regs.dac_start = started_dacs;
            return;
        }
    }
    transfer_manager.start(uint32_t(app_regs.dac_start)); // Can start from core0.
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}


void write_dac_pause(msg_t& msg)
{
    // TODO: implement this.
}


void write_dac_abort(msg_t& msg)
{
    HarpCore::copy_msg_payload_to_register(msg); // update dac_start
    transfer_manager.abort(app_regs.dac_abort);
    // Update DacStart Register state to reflect aborted state.
    app_regs.dac_start &= ~app_regs.dac_abort;
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}

void write_any_channel_external_triggers(msg_t &msg)
{
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& spec = HarpCore::reg_address_to_spec(msg.header.address);
    size_t i = ((uint8_t*)spec.base_ptr - app_regs.channel_external_triggers);
    // Error if we try to change the specified channel settings while it's busy.
    if (bufs[i].is_transferring())
    {
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
   HarpCore::write_reg_generic(msg);
}


void write_any_channel_active_player(msg_t &msg)
{
    // Convert address to output channel with pointer arithmetic.
    const RegSpec& spec = HarpCore::reg_address_to_spec(msg.header.address);
    size_t i = ((uint8_t*)spec.base_ptr - app_regs.active_players);
    // Error if we try to change the specified channel while it's busy.
    if (bufs[i].is_transferring())
    {
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    // Error if active player enum does not exist.
    uint8_t active_player_old = app_regs.active_players[i];
    HarpCore::copy_msg_payload_to_register(msg);
    if (app_regs.active_players[i] > size_t(player_t::trapezoid))
    {
        app_regs.active_players[i] = active_player_old; // Restore old value.
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
        return;
    }
    select_player(i, (player_t)app_regs.active_players[i]); // also updates reg.
    if (!HarpCore::is_muted())
        HarpCore::send_harp_reply(WRITE, msg.header.address);
}

void write_any_file_settings(msg_t& msg)
{write_settings<FileSettings, app_regs.file_settings, player_t::file>(msg);}


void write_any_sine_settings(msg_t& msg)
{write_settings<FunctionSettings, app_regs.sine_settings, player_t::sine>(msg);}


void write_any_trapezoid_settings(msg_t& msg)
{
    write_settings<TrapezoidSettings, app_regs.trapezoid_settings,
                   player_t::trapezoid>(msg);
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
    // FYI: external triggers are handled via interrupts on this core,
    // But events capturing when they started (or if they errored out)
    // are collected and dispatched over USB in this loop.

    // Send Harp replies for externally-triggered events.
    ext_trigger_event_t trigger_event;
    end_of_transfer_event_t transfer_done_event;
    // Dispatch any externally-triggered transfer-started events.
    while (queue_try_remove(&ext_trigger_event_queue, &trigger_event))
    {
        app_regs.dac_start = trigger_event.channel_start_mask;
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(EVENT, DAC_START_ADDRESS,
                HarpCore::system_to_harp_us_64(trigger_event.timestamp));
    }
    // Dispatch any transfer-finished events.
    uint8_t finished_transfers = 0;
    while (transfer_manager.get_finished_transfers(&transfer_done_event))
    {
        app_regs.dac_finished = uint8_t(transfer_done_event.finished_channels_mask);
        finished_transfers |= app_regs.dac_finished; // Collect for batch reset.
        if (!HarpCore::is_muted())
            HarpCore::send_harp_reply(EVENT, DAC_FINISHED_ADDRESS,
                HarpCore::system_to_harp_us_64(transfer_done_event.timestamp_us));
        app_regs.dac_finished = 0; // Clear it since it's event-only.
    }
    // TODO: mixed inputs.
    // Re-arm any finished waveforms.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (!((finished_transfers >> i) & 1u)) // Skip unfinished/untriggered channels.
            continue;
        // Do the reset. (select player is the lazy way.)
        select_player(i, player_t(app_regs.active_players[i]));
    }
    // Clear all started dacs that have finished.
    app_regs.dac_start &= ~finished_transfers;
}


void select_player(size_t channel, player_t player_type)
{
    size_t& i = channel;
    // Collect all players for the corresponding channel.
    SourcePlayer<T, READ_BUF_SIZE>* players[] =
        {&file_players[i], &sine_players[i], &trapezoid_players[i]};
    // Unclaim the shared buffer first.
    for (auto& player: players)
    {
        player->reset();
        player->unclaim_buffer();
    }
    players[player_type]->claim_buffer(&bufs[i]);
    switch (player_type) // Call child class method on specific settings type.
    {
        case file:
            file_players[i].apply_settings(app_regs.file_settings[i]);
            break;
        case sine:
            sine_players[i].apply_settings(app_regs.sine_settings[i]);
            break;
        case trapezoid:
            trapezoid_players[i].apply_settings(app_regs.trapezoid_settings[i]);
            break;
        default:
            break;
    }
    players[player_type]->setup();
    app_regs.active_players[i] = player_type; // Update Harp register.
}

bool player_is_ready(size_t channel, player_t player_type)
{
    switch (player_type)
    {
        case file:
            return file_players[channel].is_ready();
        case sine:
            return sine_players[channel].is_ready();
        case trapezoid:
            return trapezoid_players[channel].is_ready();
    }
    return false;
}

void reset_app()
{
    // Init all digital inputs and outputs.
    for (size_t i = DI_PORT_BASE; i < DI_PORT_BASE + NUM_DIS; ++i)
        gpio_init(i);
    for (size_t i = DO_PORT_BASE; i < DO_PORT_BASE + NUM_DOS; ++i)
        gpio_init(i);

    // Reset External Triggers to all-inputs.
    gpio_set_dir_masked64(DI_PORT_MASK, 0); // 0-bit: input.

    // Reset Digital Outputs.
    gpio_set_dir_masked64(DO_PORT_MASK, DO_PORT_MASK); // 1-bit: output.
    gpio_put_masked64(DO_PORT_MASK, 0); // Set all outputs LOW.

    // Reset app reg values that are not write-only and are not updated
    // inside their handlers.
    app_regs.dac_pause = 0;
    // Reset Waveform trigger settings. Default: DI[i] triggers AO[i].
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.channel_external_triggers[i] = (1u << i);
    // Reset all Player settings to defaults.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        app_regs.file_settings[i] = FileSettings();
        strcpy(app_regs.file_settings[i].path, DEFAULT_FILENAMES[i]);
        app_regs.sine_settings[i] = FunctionSettings();
        app_regs.trapezoid_settings[i] = TrapezoidSettings();
    }
    transfer_manager.reset();
    // FIXME: hardcoded reference to DMA_IRQ_1.
    transfer_manager.enable_end_of_transfer_interrupt(1); // corresponds to DMA_IRQ_1
    // Reset DoubleBuffers before DACs, since they send data to DACs.
    for (auto& buf: bufs)
        buf.reset();
    // FYI: PIO_LTC264x instances manage GPIO pin function.
    for (auto& dac: dacs)
        dac.write_value(PIO_LTC264x::OUTPUT_MIDSCALE);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] =
            FunctionSettings::volts_16bit_to_float(PIO_LTC264x::OUTPUT_MIDSCALE);
    // Select default player (also does a reset).
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        select_player(i, player_t::sine); // claims buffer & applies settings.
    // Launch core1.
    multicore_reset_core1();
    (void)multicore_fifo_pop_blocking(); // Wait until core1 is ready.
    multicore_launch_core1(core1main);
    // Setup External Trigger Callback
    // Enable all External Trigger GPIOS to trigger the callback.
    irq_set_exclusive_handler(IO_IRQ_BANK0, handle_external_trigger);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        gpio_set_irq_enabled(i + DI_PORT_BASE, GPIO_IRQ_EDGE_RISE, true);
    irq_set_enabled(IO_IRQ_BANK0, true);
}

// FYI: this ISR will fire for every rising edge event on said pins--even if
// all channels are busy.
void __not_in_flash_func(handle_external_trigger)()
{
    // Note: we must read gpios here directly because we are not on a clean
    // multiple of 8-boundary, so it's cumbersome to assemble the interrupt
    // state from multiple reads.
    // Start the waveform per the 1s in the channel.
    // Filter out the busy channels first.
    uint32_t trigger_mask = ((gpio_get_all64() & DI_PORT_MASK) >> DI_PORT_BASE);
    // Combine per-channel external trigger settings into the final trigger mask
    // to trigger waveforms simultaneously.
    uint32_t start_mask = 0; // aggregate multi-channel trigger mask.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (bufs[i].is_transferring()) // Skip re-triggering busy channels.
            continue;
        // Check if any currently HIGH pins would trigger this channel.
        if (trigger_mask & app_regs.channel_external_triggers[i])
            start_mask |= (1u << i);
    }
    if (start_mask)
    {
        ext_trigger_event_t trigger_event;
        transfer_manager.start(start_mask); // Can be started from core1.
        trigger_event.channel_start_mask = start_mask;
        trigger_event.timestamp = time_us_64();
        // Push harp message
        queue_try_add(&ext_trigger_event_queue, &trigger_event);
        // Update which dacs have started.
        app_regs.dac_start |= start_mask;
    }
    // Acknowledge the interrupt. Assume nothing else is setting these pins.
    // Clear the INTR[n] state since we dealt with all pin changes.
    // Clear by "writing a 1" to the set bits.
    io_bank0_hw->intr[DI_PORT_BASE >> 3] = 0xFFFFFFFF; // >> 3: floor-divide by 8
    // Do it twice since we are not on a clean multiple of 8 boundary.
    io_bank0_hw->intr[(DI_PORT_BASE + NUM_CHANNELS) >> 3] = 0xFFFFFFFF;
}
