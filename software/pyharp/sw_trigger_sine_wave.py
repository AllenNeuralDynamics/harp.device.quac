#!/usr/bin/env python3
import logging
from time import sleep
from pyharp.device import Device
from pyharp.messages import (WriteU8HarpMessage, WriteU8ArrayMessage,
    ReadU8HarpMessage)
from app_registers import AppRegs, WaveformType, SINE_SETTINGS_FMT

# logging.basicConfig(level=logging.DEBUG)
COM_PORT = "/dev/ttyACM0"

CHANNEL = 0
WAVEFORM_TYPE =  WaveformType.Sine

cycles = 1
duration_us = 2_000_000
sample_rate_hz = 10_000
frequency_hz = 10
amplitude_volts = 2.5 # center-to-peak, not peak-to-peak
vertical_shift_volts = 0

# Open the device and print the info on screen
# Open serial connection and save communication to a file
device = Device(COM_PORT, "ibl.bin")

# Specify Sine Player
print(f"Setting channel {CHANNEL} to {WAVEFORM_TYPE.name} Player.")
reply = device.send(WriteU8HarpMessage(AppRegs.ActivePlayers0 + CHANNEL,
                                       WAVEFORM_TYPE.value).frame)
print(f"  Read back: {WaveformType(reply.payload[0]).name}, time: {reply.timestamp}")

sine_settings = (
    cycles,
    duration_us,
    sample_rate_hz,
    frequency_hz,
    amplitude_volts,
    vertical_shift_volts
)
# Apply settings.
reply = device.send(WriteU8ArrayMessage(AppRegs.SineSettings0 + CHANNEL,
                                        SINE_SETTINGS_FMT, sine_settings,).frame)
print(f"SineSettings[{CHANNEL}] -> {sine_settings}, ({reply.message_type.name})")

# Ensure waveform is ready.
channel_is_ready = False
while not channel_is_ready:
    reply = device.send(ReadU8HarpMessage(AppRegs.DACReady).frame)
    channel_is_ready = bool(reply.payload[0] >> CHANNEL)
    if not channel_is_ready:
        print(f"Channel[{CHANNEL}] is not yet ready...")
        sleep(0.1)
print(f"Channel[{CHANNEL}] is ready.")

# Trigger waveform.
print("Starting waveform.")
start_mask = 1 << CHANNEL
reply = device.send(WriteU8HarpMessage(AppRegs.DACStart, start_mask).frame)
print(f" Read back: 0x{reply.payload[0]:02x} ({reply.message_type.name}), time: {reply.timestamp}")

# Wait for waveform-finished event.
print("Waiting for end-of-waveform event.")
waveform_playing = True
while waveform_playing:
    events = device.get_events()
    for msg in events:
        print(msg)
        print()
        if msg.address == AppRegs.DACFinished:
            waveform_playing = False

print("Done.")
device.disconnect()
