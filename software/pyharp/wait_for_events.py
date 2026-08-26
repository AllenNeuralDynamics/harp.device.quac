#!/usr/bin/env python3
import os
import threading

from harp.protocol import HarpMessage
from harp.serial import open_device

from app_registers import device_module

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

device = open_device(device_module, port=COM_PORT)


def print_event(msg: HarpMessage) -> None:
    print(msg)
    print()


print("Waiting for events.")
with device.subscribe_all(print_event):
    try:
        threading.Event().wait()
    except KeyboardInterrupt:
        pass
print("Disconnecting.")
device.close()
