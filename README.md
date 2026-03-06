# harp.device.quad-dac
This repository contains the hardware, firmware, and software related to the Harp quad DAC


## Compatible SD Cards
During normal operation, up-to-four waveforms stored on the SD card are read (interleaved) at 4MB per second.
With the extra overhead of switching between files, the SD card must be able to support reading at no less than 8MB per second.
In theory, any _Class 10 SD Card_ should be compatible with the _quac_ board.

But since card performacne can vary, here's a list of tested cards:
| Vendor    | Model                   |
|-----------|-------------------------|
| Samsung   | Pro Plus 8GB Smart Card |
| GIGASTONE | Industrial 8GB MLC      |
|           |                         |

## Generating Waveforms

The _quac_ board reads files in 16-bit little-endian _Pulse-Code Modulation_ (PCM) format.
There are a few options for generating waveforms in this format.

### Upscaling Existing Files 💽
It's possible to convert existing audio files to a format compatible with the _quac_ board using `ffmpeg`.
To upscale an existing _\*.wav_ file to a compatible format, use:
```bash
ffmpeg -i example.wav -f u16le -ar 500000 output.raw
```

### With numpy 💻
For more complicated waveforms that do not derive from an existing audio file, we recommend using numpy.

Here's an example to generate the [North American Ringing Tone](https://en.wikipedia.org/wiki/Ringing_tone#Bell_System_tones), which is the sum of a 440Hz and 480Hz sine wave.

```python
import numpy as np


```
