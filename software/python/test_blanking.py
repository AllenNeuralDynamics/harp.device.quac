#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Test mute/unmute functionality on DI0."""

from asyncio import sleep
import os
import threading

from harp import serial
from harp.protocol import HarpMessage
from time import sleep

import device as quac


## Hardware Setup:
# DI0 should be connected to DO0, such that the DigitalOutput pin can act as
# the mute/unmute in this setup.

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

CHANNEL = 0
WAVEFORM_TYPE = quac.PlayerType.SINE
ACTIVE_PLAYERS = [quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3]
SINE_SETTINGS = [quac.SineSettings0, quac.SineSettings1, quac.SineSettings2, quac.SineSettings3]
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = SINE_SETTINGS[CHANNEL]

cycles = 1
update_frequency_hz = 10_000
frequency_hz = 1
duration_us = 5_000_000
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 1.25
normalized_phase_shift = 0

# ----END OF CUSTOM SETTINGS-----------------------------------------


# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
with serial.open_device(quac, port=COM_PORT) as device:

    # Assert DO0 to control the blanking output of the pins.
    reply = device.write(quac.DOPortState, 0b0001) # start muted

    print("Setting DI0 to BLANKING and setting DIO to BLANK AO0.")
    # Set DI0 to BLANKING function.
    device.write(quac.PinModeDI0, quac.DIPinMode.BLANKING)
    # Setup DI0 to mute AO0.
    device.write(quac.AnalogOutputChannelAssignmentDI0, 0b0001)

    # Specify Player
    print(f"Setting channel {CHANNEL} to {WAVEFORM_TYPE.name} Player.")
    reply = device.write(ACTIVE_PLAYER_REG, WAVEFORM_TYPE)
    print(f"  Read back: {reply.payload.name}, time: {reply.timestamp}")

    settings = SETTINGS_REG.payload_class(
        cycles=cycles,
        duration=duration_us,
        update_frequency=update_frequency_hz,
        frequency=frequency_hz,
        amplitude=amplitude_volts,
        vertical_shift=vertical_shift_volts,
        normalized_phase_shift=normalized_phase_shift
    )
    # Apply settings.
    reply = device.write(SETTINGS_REG, settings)
    print(f"SineSettings[{CHANNEL}] -> {settings}, ({reply.message_type.name})")
    print(f"reply: {reply.payload}")
    print()

    channel_mask = 1 << CHANNEL

    # Subscribe to the end-of-waveform event *before* triggering, so a fast
    # finish can't be missed between starting the waveform and waiting for it.
    waveform_finished = threading.Event()

    def on_dac_finished(msg: HarpMessage) -> None:
        print(msg)
        print()
        waveform_finished.set()


    with device.subscribe(quac.DacFinished, on_dac_finished):
        # Trigger waveform.
        print("Starting waveform.")
        reply = device.write(quac.DacStart, channel_mask)
        print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
            f"time: {reply.timestamp}")

        sleep(0.5)
        for i in range(3):
            device.write(quac.DOPortState, 0b0000) # unmute
            sleep(0.25)
            device.write(quac.DOPortState, 0b0001) # mute
            sleep(0.25)
        device.write(quac.DOPortState, 0b0000) # unmute

        # Wait for waveform-finished event.
        print("Waiting for end-of-waveform event.")
        waveform_finished.wait()

    print("Disconnecting.")
