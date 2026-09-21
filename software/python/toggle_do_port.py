#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Toggle each digital output line on the Quac in turn."""
import os
from time import sleep

from harp import serial

import device as quac

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

with serial.open_device(quac, port=COM_PORT) as device:

    for i in range(4):
        value = int(1) << i
        print(f"Writing: 0x{value:02x}", end = " ")
        reply = device.write(quac.DOPortState, value)
        print(f" Read back: 0x{int(reply.payload):02x}")
        sleep(0.5)
    print("Setting all Digital outputs to 0.")
    reply = device.write(quac.DOPortState, 0)
    print("Disconnecting.")
