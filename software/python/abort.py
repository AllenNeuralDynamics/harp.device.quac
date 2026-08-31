#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
#     "gitpython",
# ]
# ///
"""Trigger a sine waveform on one analog output channel and abort it shortly after."""

import os
import threading
from time import sleep

from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import (device_module,
                           ACTIVE_PLAYERS,
                           SINE_SETTINGS,
                           WaveformType)

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

CHANNEL = 0
CHANNEL_MASK = 1 << CHANNEL
WAVEFORM_TYPE = WaveformType.SINE
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = SINE_SETTINGS[CHANNEL]

cycles = 1
update_frequency_hz = 10_000
frequency_hz = 1
duration_us = 0  # Play forever
amplitude_volts = 1
vertical_shift_volts = 0.5
normalized_phase_shift = 0

# ----END OF CUSTOM SETTINGS-----------------------------------------


# Open the device (validates WhoAmI against device.yml) and print the info on
# screen.
with open_device(device_module, port=COM_PORT) as device:

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

    # Ensure waveform is ready.
    channels_ready = False
    print("Checking if channel is ready.")
    while not channels_ready:
        reply = device.read(device_module.DacReady)
        print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
            f"time: {reply.timestamp}")
        channels_ready = int(reply.payload) & CHANNEL_MASK
        if not channels_ready:
            print(f"Channel is not ready.... Current state: {int(reply.payload):02x}")
            sleep(0.1)
    print("Channel is ready.")

    # Trigger waveform.
    print("Starting waveform.")
    reply = device.write(device_module.DacStart, CHANNEL_MASK)
    print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
        f"time: {reply.timestamp}")

    # Abort waveform
    sleep(1.25)
    print("Aborting waveform.")
    reply = device.write(device_module.DacAbort, CHANNEL_MASK)
    print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
        f"time: {reply.timestamp}")

    print("Disconnecting.")
