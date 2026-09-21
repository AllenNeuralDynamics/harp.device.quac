#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Trigger a trapezoid waveform on one analog output channel."""

import os
import threading
from time import sleep

from harp import serial
from harp.protocol import HarpMessage

import device as quac

# -------------------------------------------------------------------
# CUSTOM SETTINGS
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"
CHANNEL = 0
WAVEFORM_TYPE = quac.PlayerType.TRAPEZOID
ACTIVE_PLAYERS = [quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3]
TRAPEZOID_SETTINGS = [
    quac.TrapezoidSettings0, quac.TrapezoidSettings1,
    quac.TrapezoidSettings2, quac.TrapezoidSettings3,
]
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = TRAPEZOID_SETTINGS[CHANNEL]

cycles = 1
duration_us = 1_000_000
update_frequency_hz = 10_000
frequency_hz = 2
amplitude_volts = 5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 2.5
normalized_phase_shift = 0
ramp_on_us = 50_000
ramp_off_us = 50_000
pulse_width_us = 150_000

# -------------------------------------------------------------------

# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
device = serial.open_device(quac, port=COM_PORT)

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
    normalized_phase_shift=normalized_phase_shift,
    ramp_on_duration=ramp_on_us,
    pulse_width_duration=pulse_width_us,
    ramp_off_duration=ramp_off_us,
)
# Apply settings.
reply = device.write(SETTINGS_REG, settings)
print(f"TrapezoidSettings[{CHANNEL}] -> {settings}, "
      f"({reply.message_type.name})")

# Ensure waveform is ready.
channel_mask = 1 << CHANNEL
channel_is_ready = False
while not channel_is_ready:
    reply = device.read(quac.DacReady)
    channel_is_ready = (reply.payload) & channel_mask > 0
    if not channel_is_ready:
        print(f"Channel[{CHANNEL}] is not yet ready...")
        sleep(0.1)
print(f"Channel[{CHANNEL}] is ready.")

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
    print("Disconnecting")
