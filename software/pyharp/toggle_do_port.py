#!/usr/bin/env python3
import os
from time import sleep

from harp.serial import open_device

from app_registers import AppRegs

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"

# ----END OF CUSTOM SETTINGS-----------------------------------------

device = open_device(AppRegs, port=COM_PORT)

for i in range(4):
    value = int(1) << i
    print(f"Writing: 0x{value:02x}", end = " ")
    reply = device.write(AppRegs.DOPortState, value)
    print(f" Read back: 0x{int(reply.payload):02x}")
    sleep(0.5)
print("Setting all Digital outputs to 0.")
reply = device.write(AppRegs.DOPortState, 0)
device.close()
