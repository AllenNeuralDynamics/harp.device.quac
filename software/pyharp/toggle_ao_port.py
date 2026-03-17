#!/usr/bin/env python3
from pyharp.device import Device, DeviceMode
from pyharp.messages import WriteU8HarpMessage, WriteU8ArrayMessage
from pyharp.messages import MessageType
from pyharp.messages import CommonRegisters as Regs
from struct import pack, unpack
import logging
import random
import os
from time import sleep
from app_registers import AppRegs

#logging.basicConfig(level=logging.DEBUG)


# Open the device and print the info on screen
# Open serial connection and save communication to a file
if os.name == 'posix': # check for Linux.
    device = Device("/dev/ttyACM0", "ibl.bin")
else: # assume Windows.
    device = Device("COM95", "ibl.bin")


ao_value = random.randint(0, 65535)
for i in range(4):
    print("Writing: 0x{value:02x}", end = " ")
    reply = device.send(WriteU8HarpMessage(AppRegs.AOChannel0 + i, ao_value).frame)
    print(f" Read back: 0x{reply.payload[0]:02x}")
    sleep(1.0)
# Reset everything to midscale
print(f"Resetting all Analog Output Waveforms to midscale.")
midscale_settings = [32768, 32768, 32768, 32768]
data_fmt = "<HHHH" # 4 unsigned 16-bit numbers.
reply = device.send(WriteU8ArrayMessage(AppRegs.AOPortState, data_fmt, midscale_settings).frame)
