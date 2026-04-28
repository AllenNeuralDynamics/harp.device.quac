#!/usr/bin/env python3
"""
Configure and software-trigger a trapezoidal pulse train on AO channel 0.

Harp message sequence:
  1. (optional) write SampleRateHz to change the shared DMA pacing rate.
  2. write WaveformType0 = PulseTrain.
  3. write PulseSettings0 (packed struct, 23 bytes) with the parameters.
  4. write WaveformStart = 0b0001 to fire channel 0.
  5. wait for the WaveformFinished event.
"""
import logging
import os
from time import sleep

from pyharp.device import Device
from pyharp.messages import (
    WriteU8HarpMessage,
    WriteU8ArrayMessage,
    WriteU32HarpMessage,
)

from app_registers import AppRegs, WaveformType, PULSE_SETTINGS_FMT

# logging.basicConfig(level=logging.DEBUG)

CHANNEL = 0


def main():
    if os.name == "posix":  # Linux / macOS
        device = Device("/dev/ttyACM0", "ibl.bin")
    else:  # Windows
        device = Device("COM13", "ibl.bin")

    # ----- 1) Sample rate. -----
    # 10 kHz is the firmware default; writing explicitly makes the script
    # self-contained.
    sample_rate_hz = 10_000
    reply = device.send(
        WriteU32HarpMessage(AppRegs.SampleRateHz, sample_rate_hz).frame
    )
    print(f"SampleRateHz -> {sample_rate_hz} ({reply.message_type.name})")

    # ----- 2) Select PulseTrain on channel 0. -----
    reply = device.send(
        WriteU8HarpMessage(
            AppRegs.WaveformType0 + CHANNEL, int(WaveformType.PulseTrain)
        ).frame
    )
    print(
        f"WaveformType[{CHANNEL}] -> PulseTrain "
        f"({reply.message_type.name})"
    )

    # ----- 3) Configure the pulse train. -----
    #   1 ms trapezoidal pulse every 10 ms, peak at +16384 DAC codes above
    #   midscale (~+5 V on the bipolar +/-10 V board), 100 us rise,
    #   300 us fall, run for 2 seconds total, no external trigger.
    pulse_width_us       = 100_000      # flat-top duration
    pulse_interval_us    = 200_000     # pulse-to-pulse period
    pulse_amplitude      = 16_384     # DAC codes above midscale
    ramp_on_duration_us  = 10_000
    ramp_off_duration_us = 10_000
    total_duration_us    = 1_100_000  # 0 = run forever
    external_trigger_mask = 0         # no DI-based trigger

    pulse_settings = (
        pulse_width_us,
        pulse_interval_us,
        pulse_amplitude,
        ramp_on_duration_us,
        ramp_off_duration_us,
        total_duration_us,
        external_trigger_mask,
    )
    reply = device.send(
        WriteU8ArrayMessage(
            AppRegs.PulseSettings0 + CHANNEL,
            PULSE_SETTINGS_FMT,
            pulse_settings,
        ).frame
    )
    print(f"PulseSettings[{CHANNEL}] -> {pulse_settings} "
          f"({reply.message_type.name})")

    # Small delay so core1 has definitely finished the previous reset_app
    # cycle before we trigger.
    sleep(0.05)

    # ----- 4) Trigger the waveform. -----
    start_mask = 1 << CHANNEL
    reply = device.send(
        WriteU8HarpMessage(AppRegs.WaveformStart, start_mask).frame
    )
    print(
        f"WaveformStart = 0x{start_mask:02x} "
        f"({reply.message_type.name}, t={reply.timestamp})"
    )

    # ----- 5) Wait for the WaveformFinished event. -----
    print("Waiting for WaveformFinished event...")
    remaining_channels = start_mask
    while remaining_channels:
        for msg in device.get_events():
            if msg.address == AppRegs.WaveformFinished:
                finished = msg.payload[0]
                print(
                    f"WaveformFinished: 0x{finished:02x} at t={msg.timestamp}"
                )
                remaining_channels &= ~finished
        sleep(0.01)

    print("Done.")
    device.disconnect()


if __name__ == "__main__":
    main()
