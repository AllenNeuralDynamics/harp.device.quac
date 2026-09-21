#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Read and print the active waveform player for each analog output channel."""
import os
from time import sleep

from harp import serial

import device as quac

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

ACTIVE_PLAYER_REGISTERS = [
    quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3,
]

# Open the device. Passing the ``quac`` module validates the WHO_AM_I on open.
with serial.open_device(quac, port=COM_PORT) as device:
    for i, register in enumerate(ACTIVE_PLAYER_REGISTERS):
        print(f"Reading channel[{i}] waveform type.")
        reply = device.read(register)
        print(f"  Read back: {reply.payload.name} ({reply.message_type.name}), "
              f"time: {reply.timestamp}")
        sleep(0.5)
