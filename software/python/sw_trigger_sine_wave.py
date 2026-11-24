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

CHANNEL = 0
WAVEFORM_TYPE = quac.PlayerType.SINE
ACTIVE_PLAYERS = [quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3]
SINE_SETTINGS = [quac.SineSettings0, quac.SineSettings1, quac.SineSettings2, quac.SineSettings3]
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = SINE_SETTINGS[CHANNEL]

cycles = 1
update_frequency_hz = 10_000
frequency_hz = 1
duration_us = 3_000_000
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 2.5
normalized_phase_shift = 0

# ----END OF CUSTOM SETTINGS-----------------------------------------


# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
with serial.open_device(quac, port=COM_PORT) as device:

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

    def on_dac_error(msg: HarpMessage) -> None:
        print(msg)
        print("Scheduler Error!")


    with (
        device.subscribe(quac.DacFinished, on_dac_finished),
        device.subscribe(quac.DacSchedulerError, on_dac_error),
        ):
        # Trigger waveform.
        print("Starting waveform.")
        reply = device.write(quac.DacStart, channel_mask)
        print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
            f"time: {reply.timestamp}")

        # Wait for waveform-finished event.
        print("Waiting for end-of-waveform event.")
        waveform_finished.wait()

    print("Disconnecting.")
