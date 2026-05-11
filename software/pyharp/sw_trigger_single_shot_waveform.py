#!/usr/bin/env python3
from pyharp.device import Device
from pyharp.messages import ReadU8HarpMessage, WriteU8HarpMessage
from struct import pack, unpack
import logging
import os
from time import sleep
from app_registers import AppRegs, WaveformType
import sys

#logging.basicConfig(level=logging.DEBUG)

waveform_type =  WaveformType.File

# Open the device and print the info on screen
# Open serial connection and save communication to a file
if os.name == 'posix': # check for Linux.
    device = Device("/dev/ttyACM0", "ibl.bin")
else: # assume Windows.
    device = Device("COM95", "ibl.bin")

for i in range(4):
    print(f"Setting all active players to {waveform_type.name}")
    reply = device.send(WriteU8HarpMessage(AppRegs.ActivePlayers0 + i,
                                           waveform_type.value).frame)
    print(f" Read back: 0x{reply.payload[0]:02x} ({reply.message_type.name}), "
          f"time: {reply.timestamp}")
sleep(0.1)

print("Checking if players are ready.")
reply = device.send(ReadU8HarpMessage(AppRegs.DACReady).frame)
print(f" Read back: 0x{reply.payload[0]:02x}")
if reply.payload[0] != 0b1111:
    sys.exit("Error: players are not yet ready.")

for i in range(4):
    value = int(1) << i
    print(f"Writing: 0x{value:02x}", end = " ")
    reply = device.send(WriteU8HarpMessage(AppRegs.DACStart, value).frame)
    print(f" Read back: 0x{reply.payload[0]:02x} ({reply.message_type.name}), time: {reply.timestamp}")
    sleep(0.5)

print("Waiting for end-of-message replies")
waveform_replies_left = 4
while (waveform_replies_left):
    events = device.get_events()
    for msg in events:
        print(msg)
        print()
        if msg.address == AppRegs.DACFinished:
            waveform_replies_left -= 1
