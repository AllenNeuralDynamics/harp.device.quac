#!/usr/bin/env python3
"""
Configure and software-trigger a raised-cosine sine wave on AO channel 0.

Harp message sequence:
  1. (optional) write SampleRateHz to change the shared DMA pacing rate.
  2. write WaveformType0 = Sine.
  3. write SineSettings0 (packed struct, 11 bytes) with the parameters.
  4. write WaveformStart = 0b0001 to fire channel 0.
  5. wait for the WaveformFinished event.

Output shape: sample = midscale + amplitude * (1 - cos(phase)) / 2
  * starts and ends at 0 V (DAC midscale on the bipolar +/-10 V board).
  * peaks at `midscale + amplitude`.
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

from app_registers import AppRegs, WaveformType, SINE_SETTINGS_FMT

# logging.basicConfig(level=logging.DEBUG)

CHANNEL = 0


def main():
    if os.name == "posix":  # Linux / macOS
        device = Device("/dev/ttyACM0", "ibl.bin")
    else:  # Windows
        device = Device("COM13", "ibl.bin")

    # ----- 1) Sample rate. -----
    # 10 kHz is the firmware default; writing explicitly makes the script
    # self-contained. Higher rates let you generate higher-frequency sines
    # (Nyquist = sample_rate / 2), but require more DMA/core1 headroom.
    sample_rate_hz = 10_000
    reply = device.send(
        WriteU32HarpMessage(AppRegs.SampleRateHz, sample_rate_hz).frame
    )
    print(f"SampleRateHz -> {sample_rate_hz} ({reply.message_type.name})")

    # ----- 2) Select Sine on channel 0. -----
    reply = device.send(
        WriteU8HarpMessage(
            AppRegs.WaveformType0 + CHANNEL, int(WaveformType.Sine)
        ).frame
    )
    print(
        f"WaveformType[{CHANNEL}] -> Sine "
        f"({reply.message_type.name})"
    )

    # ----- 3) Configure the sine wave. -----
    #   100 Hz raised-cosine sine for 2 s, peaking at +16384 DAC codes above
    #   midscale (~+5 V on the bipolar +/-10 V board), no external trigger.
    frequency_hz          = 10
    duration_us           = 2_000_000  # 0 = run forever
    amplitude             = 16_384     # DAC codes above midscale
    external_trigger_mask = 0          # no DI-based trigger

    sine_settings = (
        frequency_hz,
        duration_us,
        amplitude,
        external_trigger_mask,
    )
    reply = device.send(
        WriteU8ArrayMessage(
            AppRegs.SineSettings0 + CHANNEL,
            SINE_SETTINGS_FMT,
            sine_settings,
        ).frame
    )
    print(f"SineSettings[{CHANNEL}] -> {sine_settings} "
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
