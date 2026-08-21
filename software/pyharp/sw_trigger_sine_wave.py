#!/usr/bin/env python3
"""Trigger a sine waveform on one analog output channel, repeatedly on Enter."""
import os
import threading
from time import sleep

from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import ACTIVE_PLAYERS, AppRegs, SINE_SETTINGS, WaveformType

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

CHANNEL = 0
WAVEFORM_TYPE = WaveformType.SINE
ACTIVE_PLAYER_REG = ACTIVE_PLAYERS[CHANNEL]
SETTINGS_REG = SINE_SETTINGS[CHANNEL]

cycles = 2
update_frequency_hz = 10_000
frequency_hz = 1
duration_us = 2_000_000
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 0

# ----END OF CUSTOM SETTINGS-----------------------------------------


# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
device = open_device(AppRegs, port=COM_PORT)

# Specify Player
print(f"Setting channel {CHANNEL} to {WAVEFORM_TYPE.name} Player.")
reply = device.write(ACTIVE_PLAYER_REG, WAVEFORM_TYPE)
print(f"  Read back: {reply.payload.player.name}, time: {reply.timestamp}")

settings = SETTINGS_REG.payload_class(
    cycles=cycles,
    duration_us=duration_us,
    update_frequency_hz=update_frequency_hz,
    frequency_hz=frequency_hz,
    amplitude_volts=amplitude_volts,
    vertical_shift_volts=vertical_shift_volts,
)
# Apply settings.
reply = device.write(SETTINGS_REG, settings)
print(f"SineSettings[{CHANNEL}] -> {settings}, ({reply.message_type.name})")
print(f"reply: {reply.payload}")
print()

channel_mask = 1 << CHANNEL


def on_dac_finished(msg: HarpMessage) -> None:
    print(msg)
    print()
    waveform_finished.set()


try:
    # Ensure waveform is ready.
    channel_is_ready = False
    while not channel_is_ready:
        reply = device.read(AppRegs.DacReady)
        channel_is_ready = bool(int(reply.payload) & channel_mask)
        if not channel_is_ready:
            print(f"Channel[{CHANNEL}] is not yet ready...")
            sleep(0.1)
    print(f"Channel[{CHANNEL}] is ready.")
    input("press Enter to start.")
    print("Starting waveform.")

    # Subscribe to the end-of-waveform event *before* triggering, so a
    # fast finish can't be missed.
    waveform_finished = threading.Event()
    with device.subscribe(AppRegs.DacFinished, on_dac_finished):
        reply = device.write(AppRegs.DacStart, channel_mask)
        print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
            f"time: {reply.timestamp}")
        # Wait for waveform-finished event.
        print("Waiting for end-of-waveform event.")
        waveform_finished.wait()
    # Re-trigger waveform.
except KeyboardInterrupt:
    print("Keyboard interrupt received. Exiting loop.")
    pass
    
print("Disconnecting.")
device.close()
