# /// script
# requires-python = ">=3.12"
# dependencies = [
#   "numpy",
#   "matplotlib",
# ]
# ///


import numpy as np
from math import ceil
import matplotlib.pyplot as plt

NUM_SAMPLES = int(5e6)
SAMPLES_PER_SECOND = 500000.
FREQ = 1
FULL_SCALE_RANGE = (1 << 16) - 1 # 16 bit resolution

WAVEFORM_COUNT = 4

SECONDS = NUM_SAMPLES/SAMPLES_PER_SECOND

t = np.linspace(0, SECONDS, NUM_SAMPLES)
x = []

for i, phi in enumerate(np.linspace(0, 2*np.pi, WAVEFORM_COUNT, endpoint=False)):
    filename = f"channel_{i}.bin"
    print(f"Making waveform for sin(2 * pi * {FREQ} * t + {phi:.3f})")
    # make sine wave. offset it to uint16 range: 0-65535
    x.append((np.sin(2 * np.pi * FREQ * t + phi)+1)/2 * FULL_SCALE_RANGE)
    x[i] = x[i].astype("<u2")  # little-endian uint16
    plt.plot(t, x[i], label=filename)
    print(f"Writing result to file: {filename}")
    with open(filename, "wb") as file:
        x[i].tofile(file)

plt.show()

