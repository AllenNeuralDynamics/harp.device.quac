#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Trigger a sine waveform on one analog output channel, repeatedly on Enter."""

import os
import random
import threading
from time import sleep

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
#update_frequency_hz = 10_000
update_frequency_hz = 500_000
#frequency_hz = 1
frequency_hz = 10
#duration_us = 3_000_000
duration_us = 100_000
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 1.25
normalized_phase_shift = 0.5

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
    ## Apply settings.
    #reply = device.write(SETTINGS_REG, settings)
    #print(f"SineSettings[{CHANNEL}] -> {settings}, ({reply.message_type.name})")
    #print(f"reply: {reply.payload}")
    #print()

    channel_mask = 1 << CHANNEL

    # Subscribe to the end-of-waveform event *before* triggering, so a fast
    # finish can't be missed between starting the waveform and waiting for it.
    waveform_finished = threading.Event()

    def on_dac_finished(msg: HarpMessage) -> None:
        print(msg)
        waveform_finished.set()


    with device.subscribe(quac.DacFinished, on_dac_finished):
        for i in range(1000):
            print(f"Trial {i+1}/1000")
            waveform_finished.clear()

            # Apply settings.
            print("Applying settings.")
            reply = device.write(SETTINGS_REG, settings)
            print(f"SineSettings[{CHANNEL}] -> {settings}, ({reply.message_type.name})")
            print(f"reply: {reply.payload}")
            print()

            # Wait until device is ready
            print("Waiting for device to be ready. ")
            ready = 0
            for i in range(1000):
                print(f"  Checking... try {i}/1000", end="")
                ready = int(device.read(quac.DacReady).payload)
                if ready & channel_mask:
                    print()
                    break
                sleep(0.001)
            if not ready:
                raise RuntimeError(f"AO{CHANNEL} never became ready")
            # Trigger waveform.
            print("Starting waveform.")
            reply = device.write(quac.DacStart, channel_mask)
            print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), "
                f"time: {reply.timestamp}")

            # Wait for waveform-finished event.
            print("Waiting for end-of-waveform event.")
            waveform_finished.wait(timeout=(duration_us/1.0e6 + 0.1))
            if not waveform_finished:
                raise RuntimeError("Waveform not finished")
            sleep(random.uniform(0, 0.1))
            print()

    print("Disconnecting.")
