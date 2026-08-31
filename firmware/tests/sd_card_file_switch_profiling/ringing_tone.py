# /// script
# requires-python = ">=3.12"
# dependencies = [
#   "numpy",
#   "matplotlib",
# ]
# ///


import numpy as np
import matplotlib
matplotlib.use("TkAgg") # or 'Qt5Agg', 'Qt6Agg', 'WXAgg', 'GTK3Agg', etc.
import matplotlib.pyplot as plt

NUM_SAMPLES = int(5e6)
SAMPLES_PER_SECOND = 500000.
FULL_SCALE_RANGE = (1 << 16) - 1 # 16 bit resolution
SECONDS = NUM_SAMPLES/SAMPLES_PER_SECOND

filename = f"channel_0.bin"

t = np.linspace(0, SECONDS, NUM_SAMPLES)
x = np.zeros(NUM_SAMPLES)

# make sine wave. offset it to uint16 range: 0-65535
for freq in [440, 480]:
    x += (np.sin(2 * np.pi * freq * t)+1)/2 * FULL_SCALE_RANGE/2

plt.plot(t, x, label=filename)
print(f"Writing result to file: {filename}")
with open(filename, "wb") as file:
    x.astype("<u2").tofile(file)

plt.show()
#plt.savefig('ringing_tone.png')

