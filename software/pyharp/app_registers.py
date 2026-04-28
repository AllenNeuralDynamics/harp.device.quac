"""App registers for the quad-DAC device."""
from enum import IntEnum


class AppRegs(IntEnum):
    DOPortState = 32
    DOPortSet = 33
    DOPortClear = 34

    ExtTriggerState = 35

    AOPortState = 36
    AOChannel0 = 37
    AOChannel1 = 38
    AOChannel2 = 39
    AOChannel3 = 40

    DACReady = 41
    DACStart = 42
    DACPause = 43
    DACAbort = 44
    DACFinished = 45

    DACSettings0 = 46
    DACSettings1 = 47
    DACSettings2 = 48
    DACSettings3 = 49

    WaveformHashes0 = 50
    WaveformHashes1 = 51
    WaveformHashes2 = 52
    WaveformHashes3 = 53

    WaveformData0 = 54
    WaveformData1 = 55
    WaveformData2 = 56
    WaveformData3 = 57

    # --- MultiWaveformPlayer registers (sine + pulse train generator). ---
    WaveformType0 = 58
    WaveformType1 = 59
    WaveformType2 = 60
    WaveformType3 = 61

    # SineWaveSettings (11 bytes, U8 array):
    #   <I frequency_hz>  <I duration_us>  <H amplitude>  <B external_trigger_mask>
    # struct format: "<IIHB"
    SineSettings0 = 62
    SineSettings1 = 63
    SineSettings2 = 64
    SineSettings3 = 65

    # PulseTrainSettings (23 bytes, U8 array):
    #   <I pulse_width_us>
    #   <I pulse_interval_us>
    #   <H pulse_amplitude>
    #   <I ramp_on_duration_us>
    #   <I ramp_off_duration_us>
    #   <I total_duration_us>
    #   <B external_trigger_mask>
    # struct format: "<IIHIIIB"
    PulseSettings0 = 66
    PulseSettings1 = 67
    PulseSettings2 = 68
    PulseSettings3 = 69

    WaveformStart = 70      # U8 bitmask (write-only)
    WaveformAbort = 71      # U8 bitmask (write-only)
    WaveformFinished = 72   # U8 bitmask (event-only)

    SampleRateHz = 73       # U32; shared DMA pacing rate, default 10_000


# Values for the WaveformTypeN registers.
class WaveformType(IntEnum):
    Sine = 0
    PulseTrain = 1


# struct format strings matching the packed C++ structs in firmware/inc.
SINE_SETTINGS_FMT = "<IIHB"       # 11 bytes
PULSE_SETTINGS_FMT = "<IIHIIIB"   # 23 bytes

