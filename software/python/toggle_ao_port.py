#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Write a random voltage to each analog output channel, then reset to midscale."""
import os
import random
from time import sleep

from harp import serial

import device as quac

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"
NUM_CHANNELS = 4

# ----END OF CUSTOM SETTINGS-----------------------------------------

AO_CHANNELS = [quac.AOChannel0, quac.AOChannel1, quac.AOChannel2, quac.AOChannel3]

with serial.open_device(quac, port=COM_PORT) as device:

    for i in range(NUM_CHANNELS):
        ao_value = random.uniform(-10, 10)
        reply = device.read(AO_CHANNELS[i])
        print(f" AO[{i}] initial value: {reply.payload}")
        print(f"Writing: {ao_value:.3f}[V]", end = " ")
        reply = device.write(AO_CHANNELS[i], ao_value)
        print(f" | result: {reply.payload} ({reply.message_type.name})")
        print()
        sleep(0.5)
    # Reset everything to midscale
    print("Resetting all Analog Output Waveforms to midscale.")
    for i in range(NUM_CHANNELS):
        reply = device.write(AO_CHANNELS[i], 0)
        print(f"  result: {reply.payload} ({reply.message_type.name})")
    print("Disconnecting.")
