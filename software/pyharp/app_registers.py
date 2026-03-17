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
    AOChannel0 = 40

    DACReady = 41
    DACStart = 42
    DACPause = 43
    DACFinished = 44

    DACSettings0 = 45
    DACSettings1 = 46
    DACSettings2 = 47
    DACSettings3 = 48

    WaveformHashes0 = 49
    WaveformHashes1 = 50
    WaveformHashes2 = 51
    WaveformHashes3 = 52

    WaveformData0 = 53
    WaveformData1 = 52
    WaveformData2 = 54
    WaveformData3 = 55

