"""App registers for the cuttlefish."""
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

