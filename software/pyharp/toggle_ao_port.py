#!/usr/bin/env python3
from pyharp.device import Device, DeviceMode
from pyharp.messages import WriteU16HarpMessage, WriteU16ArrayMessage, ReadU16HarpMessage
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


for i in range(4):
    ao_value = random.randint(0, 65535)
    reply = device.send(ReadU16HarpMessage(AppRegs.AOChannel0 + i).frame)
    print(f" AO[{i}] initial value: 0x{reply.payload[0]:04x}")
    print(f"Writing: 0x{ao_value:04x}", end = " ")
    reply = device.send(WriteU16HarpMessage(AppRegs.AOChannel0 + i, ao_value).frame)
    print(f" | result: 0x{reply.payload[0]:04x}")
    print()
    sleep(1.0)
# Reset everything to midscale
print(f"Resetting all Analog Output Waveforms to midscale.")
midscale_settings = [32768, 32768, 32768, 32768]
data_fmt = "<HHHH" # 4 unsigned 16-bit numbers.
reply = device.send(WriteU16ArrayMessage(AppRegs.AOPortState, data_fmt, midscale_settings).frame)
