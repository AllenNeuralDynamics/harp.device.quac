#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Trigger a sine waveform on one analog output channel, repeatedly on Enter."""

import os
import threading

from harp import serial
from harp.protocol import HarpMessage

import device as quac

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

CHANNELS = [0, 1]
ACTIVE_PLAYERS = [quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3]
SINE_SETTINGS = [quac.SineSettings0, quac.SineSettings1, quac.SineSettings2, quac.SineSettings3]
#ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
#SETTINGS_REG = SINE_SETTINGS[CHANNEL]

cycles = 1
update_frequency_hz = 10_000
frequency_hz = 1
duration_us = 1_000_000
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 1.25
normalized_phase_shift = 0

# ----END OF CUSTOM SETTINGS-----------------------------------------


# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
with serial.open_device(quac, port=COM_PORT) as device:

    # Setup DO Tracking
    print(f"Setting up DO0 to track AO0 state.")
    reply = device.write(quac.DO0PinMode, quac.DOPinMode.TRACKING)
    print(f"  Read back: {reply}, {reply.payload}")
    reply = device.write(quac.AOChannelsAssignedToDO0, 0b0001)
    print(f"  Read back: {reply}, {reply.payload}")

    print(f"Setting up DO1 to track AO0 and AO1 state.")
    reply = device.write(quac.DO1PinMode, quac.DOPinMode.TRACKING)
    print(f"  Read back: {reply}, {reply.payload}")
    reply = device.write(quac.AOChannelsAssignedToDO1, 0b0011)
    print(f"  Read back: {reply}, {reply.payload}")

    channel_mask = 0
    for channel in CHANNELS:
        # Specify Player
        print(f"Setting channel {channel} to {quac.PlayerType.SINE.name} Player.")
        reply = device.write(ACTIVE_PLAYERS[channel], quac.PlayerType.SINE)
        print(f"  Read back: {reply.payload.name}, time: {reply.timestamp}")

        # Make subsequent channels .25seconds longer.
        settings = SINE_SETTINGS[channel].payload_class(
            cycles=cycles,
            duration=int(duration_us * (1 + (channel * 0.25))),
            update_frequency=update_frequency_hz,
            frequency=frequency_hz,
            amplitude=amplitude_volts,
            vertical_shift=vertical_shift_volts,
            normalized_phase_shift=normalized_phase_shift + (0.5)
        )
        # Apply settings.
        reply = device.write(SINE_SETTINGS[channel], settings)
        print(f"SineSettings[{channel}] -> {settings}, ({reply.message_type.name})")
        print(f"reply: {reply.payload}")
        print()

        channel_mask |= 1 << channel

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

        # Wait for waveform-finished event.
        print("Waiting for end-of-waveform event.")
        waveform_finished.wait()

    print("Disconnecting.")
