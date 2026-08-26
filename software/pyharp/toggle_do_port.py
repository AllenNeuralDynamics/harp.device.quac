#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
#     "gitpython",
# ]
# [tool.uv.sources]
# harp = { git = "https://github.com/harp-tech/python", branch = "main" }
# ///

import os
from time import sleep

from harp.serial import open_device

from app_registers import device_module

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

with open_device(device_module, port=COM_PORT) as device:

    for i in range(4):
        value = int(1) << i
        print(f"Writing: 0x{value:02x}", end = " ")
        reply = device.write(device_module.DOPortState, value)
        print(f" Read back: 0x{int(reply.payload):02x}")
        sleep(0.5)
    print("Setting all Digital outputs to 0.")
    reply = device.write(device_module.DOPortState, 0)
    print("Disconnecting.")
