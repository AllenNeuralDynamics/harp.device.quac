#!/usr/bin/env python3
import os
import random
from time import sleep

from harp.serial import open_device

from app_registers import AO_CHANNELS, device_module

# ----CUSTOM SETTINGS------------------------------------------------
COM_PORT = "/dev/ttyACM0" if os.name == "posix" else "COM3"
NUM_CHANNELS = 4

# ----END OF CUSTOM SETTINGS-----------------------------------------

with open_device(device_module, port=COM_PORT) as device:

    for i in range(NUM_CHANNELS):
        ao_value = random.uniform(-10, 10)
        reply = device.read(AO_CHANNELS[i])
        print(f" AO[{i}] initial value: {reply.payload}")
        print(f"Writing: {ao_value:.3f}[V]", end = " ")
        reply = device.write(AO_CHANNELS[i], ao_value)
        print(f" | result: {reply.payload} ({reply.message_type.name})")
        print()
        sleep(0.5)
    # Reset everything to midscale
    print("Resetting all Analog Output Waveforms to midscale.")
    for i in range(NUM_CHANNELS):
        reply = device.write(AO_CHANNELS[i], 0)
        print(f"  result: {reply.payload} ({reply.message_type.name})")
    device.close()
