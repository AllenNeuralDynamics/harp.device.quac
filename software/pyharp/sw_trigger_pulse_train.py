#!/usr/bin/env python3
"""Trigger a trapezoid waveform on one analog output channel."""
import threading
from time import sleep

import os
from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import (device_module, 
                           ACTIVE_PLAYERS, 
                           TRAPEZOID_SETTINGS, 
                           WaveformType)

# -------------------------------------------------------------------
# CUSTOM SETTINGS
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"
CHANNEL = 0
WAVEFORM_TYPE = WaveformType.TRAPEZOID
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = TRAPEZOID_SETTINGS[CHANNEL]

cycles = 1
duration_us = 1_000_000
update_frequency_hz = 10_000
frequency_hz = 3
amplitude_volts = 5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 2.5
ramp_on_us = 100_000
ramp_off_us = 100_000

# -------------------------------------------------------------------

# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
device = open_device(device_module, port=COM_PORT)

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
    ramp_on_duration=ramp_on_us,
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
    reply = device.read(device_module.DacReady)
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


with device.subscribe(device_module.DacFinished, on_dac_finished):
    # Trigger waveform.
    print("Starting waveform.")
    reply = device.write(device_module.DacStart, channel_mask)
    print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
          f"time: {reply.timestamp}")

    # Wait for waveform-finished event.
    print("Waiting for end-of-waveform event.")
    waveform_finished.wait()

print("Done.")
device.close()
