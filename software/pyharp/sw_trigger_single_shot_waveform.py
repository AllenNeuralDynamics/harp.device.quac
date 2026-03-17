#!/usr/bin/env python3
from pyharp.device import Device, DeviceMode
from pyharp.messages import WriteU8HarpMessage, WriteU8ArrayMessage
from pyharp.messages import MessageType
from pyharp.messages import CommonRegisters as Regs
from struct import pack, unpack
import logging
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
    value = int(1) << i
    print("Writing: 0x{value:02x}", end = " ")
    reply = device.send(WriteU8HarpMessage(AppRegs.DACStart, i).frame)
    print(f" Read back: 0x{reply.payload[0]:02x}")
    sleep(1.0)

printf("Waiting for end-of-message replies")
waveform_replies_left = 4
while (waveform_replies_left):
    events = device.get_events()
    for msg in events:
        print(msg)
        print()
        if msg.address == AppRegs.DACFinished:
            waveform_replies_left -= 1

