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

    ChannelExternalTriggers0 = 46
    ChannelExternalTriggers1 = 47
    ChannelExternalTriggers2 = 48
    ChannelExternalTriggers3 = 49

    ActivePlayers0 = 50
    ActivePlayers1 = 51
    ActivePlayers2 = 52
    ActivePlayers3 = 53

    FileSettings0 = 57
    FileSettings1 = 55
    FileSettings2 = 56
    FileSettings3 = 57

    SineSettings0 = 58
    SineSettings1 = 59
    SineSettings2 = 60
    SineSettings3 = 61

    TrapezoidSettings0 = 62
    TrapezoidSettings1 = 63
    TrapezoidSettings2 = 64
    TrapezoidSettings3 = 65

    WaveformHashes0 = 66
    WaveformHashes1 = 67
    WaveformHashes2 = 68
    WaveformHashes3 = 69


# Values for the WaveformTypeN registers.
class WaveformType(IntEnum):
    File = 0
    Sine = 1
    Trapezoid = 2


# struct format strings matching the packed C++ structs in firmware/inc.
SINE_SETTINGS_FMT = "<IIHB"  # 11 bytes
PULSE_SETTINGS_FMT = "<IIHIIIB"  # 23 bytes
