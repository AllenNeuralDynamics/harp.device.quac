# /// script
# requires-python = ">=3.12"
# dependencies = [
#   "numpy",
#   "matplotlib",
# ]
# ///
import numpy as np
import matplotlib
matplotlib.use("Agg") # or 'QtAgg', 'Qt5Agg', 'Qt6Agg', 'WXAgg', 'GTK3Agg', etc.
import matplotlib.pyplot as plt

NUM_SAMPLES = 20_000#int(2.5e6)
SAMPLES_PER_SECOND = 10_000.
FULL_SCALE_RANGE = (1 << 16) - 1 # 16 bit resolution
SECONDS = NUM_SAMPLES/SAMPLES_PER_SECOND


filenames = ["channel_0.bin", "channel_1.bin", "channel_2.bin", "channel_3.bin"]
NUM_CHANNELS = len(filenames)

print(f"Generating {SECONDS} seconds of data for {NUM_CHANNELS} channels...")

t = np.linspace(0, SECONDS, NUM_SAMPLES)
x = np.zeros((NUM_CHANNELS, NUM_SAMPLES))

# make ringtone. offset it to uint16 range: 0-65535
for freq in [440, 480]:
    x[0] += (np.sin(2 * np.pi * freq * t)+1)/2 * FULL_SCALE_RANGE/2

# make 1Hz sine wave. offset it to uint16 range
x[1] = (np.sin(2 * np.pi * 1 * t)+1)/2 * FULL_SCALE_RANGE/2

# make ramp. offset it to uint16 range
x[2] = np.linspace(0, FULL_SCALE_RANGE, NUM_SAMPLES, dtype=int)

# make reverse ramp. offset it to uint16 range
x[3] = np.linspace(FULL_SCALE_RANGE, 0, NUM_SAMPLES, dtype=int)


# Plot
for index, filename in enumerate(filenames):
    plt.plot(t, x[index], label=filename)

# Write to file
for index, filename in enumerate(filenames):
    print(f"Writing result to file: {filename}")
    with open(filename, "wb") as file:
        x[index].astype("<u2").tofile(file)

figure_filename = "test_waveforms.png"
print(f"Saving figure as {figure_filename}")
plt.savefig(figure_filename, bbox_inches='tight', dpi=300,)
