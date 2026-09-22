#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///
"""Subscribe to and print all Harp events from the device."""
import os
import threading

from harp import serial
from harp.protocol import HarpMessage

import device as quac


COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"


def print_event(msg: HarpMessage) -> None:
    print(msg)
    print()


print("Waiting for events.")
with serial.open_device(quac, port=COM_PORT) as device:
    with device.subscribe_all(print_event):
        try:
            threading.Event().wait()
        except KeyboardInterrupt:
            print("Disconnecting.")
