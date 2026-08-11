#!/usr/bin/env python3
from pyharp.device import Device
from pyharp.messages import ReadU8HarpMessage, WriteU8ArrayMessage, WriteU8HarpMessage
import logging
from struct import unpack
from time import sleep
from app_registers import AppRegs, WaveformType, FILE_SETTINGS_FMT
import sys

#logging.basicConfig(level=logging.DEBUG)
COM_PORT = "/dev/ttyACM0"

WAVEFORM_TYPE =  WaveformType.File
WAVEFORM_FMT = FILE_SETTINGS_FMT
BASE_SETTINGS_REG = AppRegs.FileSettings0
NUM_CHANNELS = 4

# Open the device and print the info on screen
# Open serial connection and save communication to a file
device = Device(COM_PORT, "ibl.bin")

cycles = 5
duration_us = 1_000_000
sample_rate_hz = 500_000

settings = [
    (cycles, duration_us, sample_rate_hz, b"channel_0.bin"),
    (cycles, duration_us, sample_rate_hz, b"channel_1.bin"),
    (cycles, duration_us, sample_rate_hz, b"channel_2.bin"),
    (cycles, duration_us, sample_rate_hz, b"channel_3.bin"),
]

print(f"Setting all active players to {WAVEFORM_TYPE.name}")
for i in range(NUM_CHANNELS):
    reply = device.send(WriteU8HarpMessage(AppRegs.ActivePlayers0 + i,
                                           WAVEFORM_TYPE.value).frame)
    print(f" Read back: 0x{reply.payload[0]:02x} ({reply.message_type.name}), "
          f"time: {reply.timestamp}")
print()

# Apply settings.
for i in range(NUM_CHANNELS):
    print(f"Applying FileSettings for waveform {i}.")
    reply = device.send(WriteU8ArrayMessage(BASE_SETTINGS_REG + i,
                                            WAVEFORM_FMT, settings[i]).frame)
    print(f"Read back: ({reply.message_type.name}) "
          f"{unpack(WAVEFORM_FMT, bytes(reply.payload))}")

# Ensure waveform is ready.
channels_ready = False
while not channels_ready:
    reply = device.send(ReadU8HarpMessage(AppRegs.DACReady).frame)
    channels_ready = reply.payload[0] == 0b1111
    if not channels_ready:
        print(f"Not all channels are ready.... Current state: {reply.payload[0]:02x}")
        sleep(0.1)
print(f"Channels are all ready.")

# Start each channel offset by 0.5 sec.
for i in range(NUM_CHANNELS):
    value = int(1) << i
    print(f"Writing: 0x{value:02x}", end = " ")
    reply = device.send(WriteU8HarpMessage(AppRegs.DACStart, value).frame)
    print(f" Read back: 0x{reply.payload[0]:02x} ({reply.message_type.name}), time: {reply.timestamp}")
    sleep(0.5)

print("Waiting for end-of-message replies")
waveform_replies_left = NUM_CHANNELS
while (waveform_replies_left):
    events = device.get_events()
    for msg in events:
        print(msg)
        print()
        if msg.address == AppRegs.DACFinished:
            waveform_replies_left -= 1
