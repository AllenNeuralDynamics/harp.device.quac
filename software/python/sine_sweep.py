#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "harp",
# ]
# ///

import argparse
import threading
from time import sleep

from harp import serial

import device as quac


def logspace(start_hz, stop_hz, count):
    values = [
        int(round(start_hz * (stop_hz / start_hz) ** (i / (count - 1))))
        for i in range(count)
    ]

    # Frequency is an integer in the QUAC register, so remove duplicates.
    return list(dict.fromkeys(values))


parser = argparse.ArgumentParser(
    description="Run a stepped logarithmic sine sweep on a Harp QUAC."
)

parser.add_argument("--port", required=True)
parser.add_argument("--channel", type=int, default=0)
parser.add_argument("--start-hz", type=float, default=20)
parser.add_argument("--stop-hz", type=float, default=20_000)
parser.add_argument("--steps", type=int, default=60)
parser.add_argument("--dwell-ms", type=float, default=100)
parser.add_argument("--amplitude", type=float, default=0.5)
parser.add_argument("--offset", type=float, default=0.0)
parser.add_argument("--update-rate", type=int, default=500_000)

args = parser.parse_args()


if not 0 <= args.channel <= 3:
    parser.error("channel must be 0..3")

if args.start_hz <= 0:
    parser.error("start-hz must be > 0")

if args.stop_hz <= args.start_hz:
    parser.error("stop-hz must be greater than start-hz")

if args.steps < 2:
    parser.error("steps must be >= 2")

if args.amplitude < 0 or args.amplitude > 10:
    parser.error("amplitude must be between 0 and 10 V")

if abs(args.offset) + args.amplitude > 10:
    parser.error("offset ± amplitude must remain inside the QUAC ±10 V range")

if args.stop_hz >= args.update_rate / 2:
    parser.error("stop-hz must be below half the update rate")


frequencies = logspace(args.start_hz, args.stop_hz, args.steps)

channel = args.channel
channel_mask = 1 << channel

ACTIVE_PLAYERS = [quac.ActivePlayer0, quac.ActivePlayer1, quac.ActivePlayer2, quac.ActivePlayer3]
SINE_SETTINGS = [quac.SineSettings0, quac.SineSettings1, quac.SineSettings2, quac.SineSettings3]
AO_CHANNELS = [quac.AOChannel0, quac.AOChannel1, quac.AOChannel2, quac.AOChannel3]

active_player = ACTIVE_PLAYERS[channel]
settings_reg = SINE_SETTINGS[channel]
ao_reg = AO_CHANNELS[channel]

print(f"Opening {args.port}")
print(
    f"AO{channel}: {frequencies[0]} Hz -> {frequencies[-1]} Hz, "
    f"{len(frequencies)} steps"
)
print(
    f"Amplitude: {args.amplitude:.3f} V peak, "
    f"offset: {args.offset:.3f} V"
)
print()


with serial.open_device(quac, port=args.port) as device:

    # Select QUAC's internal synthesized sine player.
    device.write(active_player, quac.PlayerType.SINE)

    waveform_finished = threading.Event()

    def on_dac_finished(msg):
        if msg.payload & channel_mask:
            waveform_finished.set()
            print("Waveform finished.")

    try:
        with device.subscribe(quac.DacFinished, on_dac_finished):

            for index, frequency in enumerate(frequencies, start=1):

                waveform_finished.clear()

                print(f"Applying settings for {frequency} [Hz] waveform.")
                settings = settings_reg.payload_class(
                    cycles=1,
                    duration=int(args.dwell_ms * 1000),
                    update_frequency=args.update_rate,
                    frequency=frequency,
                    amplitude=args.amplitude,
                    vertical_shift=args.offset,
                    normalized_phase_shift=0.0,
                )

                device.write(settings_reg, settings)

                # Wait until QUAC says this channel is ready.
                print("Waiting for device to become ready.")
                for i in range(1000):
                    print(f"Checking... try {i}/1000", end="")
                    ready = int(device.read(quac.DacReady).payload)
                    if ready & channel_mask:
                        break
                    sleep(0.001)
                else:
                    raise RuntimeError(
                        f"AO{channel} did not become ready"
                    )
                print()

                print(
                    f"{index:3d}/{len(frequencies):3d}   "
                    f"{frequency:6d} Hz"
                )

                print("Starting channel output.")
                device.write(quac.DacStart, channel_mask)


                sleep(args.dwell_ms / 1000 + 0.1)
                #timeout = max(2.0, args.dwell_ms / 1000 + 1.0)

                #if not waveform_finished.wait(timeout=timeout):
                #    raise TimeoutError(
                #        f"Timed out waiting for {frequency} Hz waveform"
                #    )

    except KeyboardInterrupt:
        print("\nAborting sweep...")

        try:
            device.write(quac.DacAbort, channel_mask)
            # Explicitly return the DAC to 0 [V].
            device.write(ao_reg, 0.0)
        except Exception:
            pass

    #finally:
    #    # Once playback has stopped, explicitly return the DAC to 0 V.
    #    try:
    #        device.write(ao_reg, 0.0)
    #    except Exception:
    #        pass


print()
print("Sweep complete; output returned to 0 V.")
