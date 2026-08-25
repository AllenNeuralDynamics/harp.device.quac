#!/usr/bin/env python3
"""Trigger four file-based waveforms across all analog output channels."""
import os
import threading
from time import sleep

from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import (device_module, 
                           ACTIVE_PLAYERS, 
                           FILE_SETTINGS, 
                           WaveformType)

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

WAVEFORM_TYPE = WaveformType.FILE
NUM_CHANNELS = 4

duration_us = 0 # play the whole file
cycles = 1 # play once
update_frequency_hz = 500_000

# ----END OF CUSTOM SETTINGS-----------------------------------------

# Open the device (validates WhoAmI against device.yml) and print the info on
# screen. There is no built-in raw-traffic dump file in the new package (the
# old "ibl.bin" argument); use device.subscribe_all() if that's needed again.
with open_device(device_module, port=COM_PORT) as device:

    settings = [
        FILE_SETTINGS[i].payload_class(
            cycles=cycles, duration=duration_us,
            update_frequency=update_frequency_hz, path=f"channel_{i}.bin",
        )
        for i in range(NUM_CHANNELS)
    ]

    print(f"Setting all active players to {WAVEFORM_TYPE.name}")
    for i in range(NUM_CHANNELS):
        reply = device.write(ACTIVE_PLAYERS[i], WAVEFORM_TYPE)
        print(f" Read back: {reply.payload.name} ({reply.message_type.name}), "
            f"time: {reply.timestamp}")
    print()

    # Apply settings.
    for i in range(NUM_CHANNELS):
        print(f"Applying FileSettings for waveform {i}.")
        reply = device.write(FILE_SETTINGS[i], settings[i])
        print(f"Read back: ({reply.message_type.name}) {reply.payload}")

    # Ensure waveform is ready.
    channels_ready = False
    while not channels_ready:
        reply = device.read(device_module.DacReady)
        channels_ready = int(reply.payload) == 0b1111
        if not channels_ready:
            print(f"Not all channels are ready.... Current state: {int(reply.payload):02x}")
            sleep(0.1)
    print("Channels are all ready.")

    # Wait for one end-of-waveform event per channel; subscribe *before*
    # triggering so a fast finish can't be missed.
    waveform_replies_left = NUM_CHANNELS
    all_finished = threading.Event()


    def on_dac_finished(msg: HarpMessage) -> None:
        global waveform_replies_left
        print(msg)
        print()
        waveform_replies_left -= 1
        if waveform_replies_left <= 0:
            all_finished.set()


    with device.subscribe(device_module.DacFinished, on_dac_finished):
        # Start each channel offset by 0.5 sec.
        for i in range(NUM_CHANNELS):
            value = int(1) << i
            print(f"Writing: 0x{value:02x}", end = " ")
            reply = device.write(device_module.DacStart, value)
            print(f" Read back: 0x{int(reply.payload):02x} ({reply.message_type.name}), time: {reply.timestamp}")
            sleep(0.5)

        print("Waiting for end-of-message replies")
        all_finished.wait()

    print("Done.")
    device.close()
