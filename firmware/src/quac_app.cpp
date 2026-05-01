#include "config.h"
#include "dma_double_buffer.h"
#include "pio_ltc264x.h"
#include <quac_app.h>

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

    RegSpec::U16(&app_regs.analog_output_port_state,
        read_analog_output_port_state, write_analog_output_port_state),
    RegSpec::U16(&app_regs.analog_output_channel_0,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::U16(&app_regs.analog_output_channel_1,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::U16(&app_regs.analog_output_channel_2,
        read_any_analog_output_channel, write_any_analog_output_channel),
    RegSpec::U16(&app_regs.analog_output_channel_3,
        read_any_analog_output_channel, write_any_analog_output_channel),

    RegSpec::U8(&app_regs.dac_ready,
        read_dac_ready, HarpCore::write_reg_error),
    RegSpec::U8(&app_regs.dac_start,
        HarpCore::read_reg_error, write_dac_start),
    RegSpec::U8(&app_regs.dac_pause,
        read_dac_pause, write_dac_pause),
    RegSpec::U8(&app_regs.dac_abort,
        read_dac_abort, write_dac_abort),
    RegSpec::U8(&app_regs.dac_finished,
        HarpCore::read_reg_error, HarpCore::write_reg_error),

    RegSpec::U8Array(&app_regs.dac_settings[0], sizeof(WaveformSettings),
        read_any_dac_settings, write_any_dac_settings),
    RegSpec::U8Array(&app_regs.dac_settings[1], sizeof(WaveformSettings),
        read_any_dac_settings, write_any_dac_settings),
    RegSpec::U8Array(&app_regs.dac_settings[2], sizeof(WaveformSettings),
        read_any_dac_settings, write_any_dac_settings),
    RegSpec::U8Array(&app_regs.dac_settings[3], sizeof(WaveformSettings),
        read_any_dac_settings, write_any_dac_settings),

    RegSpec::U8Array(&app_regs.waveform_hashes[0], SHA256_NUM_BYTES,
        read_any_waveform_hash, HarpCore::write_reg_error),
    RegSpec::U8Array(&app_regs.waveform_hashes[1], SHA256_NUM_BYTES,
        read_any_waveform_hash, HarpCore::write_reg_error),
    RegSpec::U8Array(&app_regs.waveform_hashes[2], SHA256_NUM_BYTES,
        read_any_waveform_hash, HarpCore::write_reg_error),
    RegSpec::U8Array(&app_regs.waveform_hashes[3], SHA256_NUM_BYTES,
        read_any_waveform_hash, HarpCore::write_reg_error),
    // Note: WAVEFORM_MAX_BYTES cannot be coerced into RegSpec type.
    // num_bytes should be WAVEFORM_MAX_BYTES
    RegSpec::U8Array(&app_regs.waveform_hashes[0], 1,
        HarpCore::read_reg_error, write_any_waveform_data),
    RegSpec::U8Array(&app_regs.waveform_hashes[0], 1,
        HarpCore::read_reg_error, write_any_waveform_data),
    RegSpec::U8Array(&app_regs.waveform_hashes[0], 1,
        HarpCore::read_reg_error, write_any_waveform_data),
    RegSpec::U8Array(&app_regs.waveform_hashes[0], 1,
    HarpCore::read_reg_error, write_any_waveform_data)
};

const size_t APP_REG_COUNT = sizeof(app_reg_specs)/sizeof(RegSpec);

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
        app_regs.analog_output_port_state[i] = dacs[i].get_last_value();
    HarpCore::send_harp_reply(READ, address);
}


void write_analog_output_port_state(msg_t& msg)
{
    // Ensure no channels are busy.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        if (bufs[i].is_transferring())
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
    const RegSpec& specs = HarpCore::reg_address_to_spec(address);
    size_t channel = ((uint16_t*)specs.base_ptr - app_regs.analog_output_port_state);
    if (bufs[channel].is_transferring())
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
    const RegSpec& specs = HarpCore::reg_address_to_spec(msg.header.address);
    size_t channel = ((uint16_t*)specs.base_ptr - app_regs.analog_output_port_state);
    if (bufs[channel].is_transferring())
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
        if (!file_players[i].is_ready())
        {
            HarpCore::send_harp_reply(WRITE_ERROR, msg.header.address);
            return;
        }
    }
    transfer_manager.start(uint32_t(app_regs.dac_start)); // Can start from core0.
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
    const RegSpec& spec = HarpCore::reg_address_to_spec(msg.header.address);
    size_t channel = ((uint16_t*)spec.base_ptr - app_regs.analog_output_port_state);
    if (bufs[channel].is_transferring())
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
    // Bail early if we're muted. Drain all queues and exit.
    if (HarpCore::is_muted())
    {
        while (queue_try_remove(&ext_trigger_event_queue, &trigger_event)){}
        while (transfer_manager.get_finished_transfers(&transfer_done_event)){}
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
    while (transfer_manager.get_finished_transfers(&transfer_done_event))
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

    // Reset Waveform trigger settings.
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        auto& settings = app_regs.dac_settings[i];
        settings.cycles = 1; // play once: "single-shot."
        settings.sample_count = 0; // Play everything.
        settings.frequency_hz =
            TimerPacedDMADoubleBuffer<T, READ_BUF_SIZE>::DEFAULT_FREQUENCY_HZ;
        settings.external_trigger_mask = (1u << i); // DI[i] triggers AO[i].
    }
    transfer_manager.reset();
    // FIXME: hardcoded reference to DMA_IRQ_1.
    transfer_manager.enable_end_of_transfer_interrupt(1); // corresponds to DMA_IRQ_1
    // Reset DoubleBuffers before DACs, since they send data to DACs.
    for (auto& buf: bufs)
        buf.reset();
    // TODO: "apply settings from WaveformSettings to buffers and xfer managers"
    // FYI: PIO_LTC264x instances manage GPIO pin function.
    for (auto& dac: dacs)
        dac.write_value(PIO_LTC264x::OUTPUT_MIDSCALE);
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
        app_regs.analog_output_port_state[i] = PIO_LTC264x::OUTPUT_MIDSCALE;
    multicore_reset_core1(); // Ensure core1 is not updating the player first.
    for (auto& file_player: file_players)
        file_player.reset();
    // Open default files
    for (size_t i = 0; i < NUM_CHANNELS; ++i)
    {
        file_players[i].claim_buffer(buf_ptrs[i]);
        // Warning: might lock up if the file doesn't exist.
        file_players[i].open_file(default_filenames[i]);
    }
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
        if (bufs[i].is_transferring()) // Skip re-triggering busy channels.
            continue;
        // Check if any currently HIGH pins would trigger this channel.
        if (trigger_mask & app_regs.dac_settings[i].external_trigger_mask)
            start_mask |= (1u << i);
    }
    if (start_mask != 0)
    {
        ext_trigger_event_t trigger_event;
        transfer_manager.start(start_mask); // Can be started from core1.
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
