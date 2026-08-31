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

FREQUENCY_HZ = 250000

filename = f"resolution_test.bin"

t = np.linspace(0, SECONDS, NUM_SAMPLES)

x = np.arange(10) + FULL_SCALE_RANGE/2
x = np.repeat(x, NUM_SAMPLES/len(x))

plt.plot(t, x, label=filename,)
print(f"Writing result to file: {filename}")
with open(filename, "wb") as file:
    x.astype("<u2").tofile(file)

plt.show()
#plt.savefig('ringing_tone.png')

