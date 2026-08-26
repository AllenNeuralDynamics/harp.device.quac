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
import threading

from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import device_module

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

def print_event(msg: HarpMessage) -> None:
    print(msg)
    print()


print("Waiting for events.")
with open_device(device_module, port=COM_PORT) as device:
    with device.subscribe_all(print_event):
        try:
            threading.Event().wait()
        except KeyboardInterrupt:
            print("Disconnecting.")
