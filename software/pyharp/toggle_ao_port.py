#!/usr/bin/env python3
from pyharp.device import Device, DeviceMode
from pyharp.messages import ReadFloatHarpMessage, WriteFloatHarpMessage
from pyharp.messages import MessageType
from pyharp.messages import CommonRegisters as Regs
from struct import pack, unpack
import logging
import random
import os
from time import sleep
from app_registers import AppRegs

#logging.basicConfig(level=logging.DEBUG)

NUM_CHANNELS = 4

# Open the device and print the info on screen
# Open serial connection and save communication to a file
if os.name == 'posix': # check for Linux.
    device = Device("/dev/ttyACM0", "ibl.bin")
else: # assume Windows.
    device = Device("COM95", "ibl.bin")


for i in range(NUM_CHANNELS):
    ao_value = random.uniform(-10, 10)
    reply = device.send(ReadFloatHarpMessage(AppRegs.AOChannel0 + i).frame)
    print(f" AO[{i}] initial value: {reply.payload}")
    print(f"Writing: {ao_value:.3f}[V]", end = " ")
    reply = device.send(WriteFloatHarpMessage(AppRegs.AOChannel0 + i, ao_value).frame)
    print(f" | result: {reply.payload} ({reply.message_type.name})")
    print()
    sleep(0.5)
# Reset everything to midscale
print("Resetting all Analog Output Waveforms to midscale.")
for i in range(NUM_CHANNELS):
    reply = device.send(WriteFloatHarpMessage(AppRegs.AOChannel0 + i, 0).frame)
    print(f"  result: {reply.payload} ({reply.message_type.name})")
