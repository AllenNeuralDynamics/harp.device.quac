# This file was automatically generated and should not be edited directly.
# To make changes, edit the device metadata and regenerate the interface.

import enum
from typing import Any, ClassVar

import numpy as np
from harp.protocol import (
    AnonymousPayload,
    BitMask,
    Field,
    GroupMask,
    IdentityConverter,
    PayloadType,
    RegisterBase,
    RegisterFloat,
    StringConverter,
    StructPayload,
)
from harp.device.core import REGISTER_MAP as _CORE_REGISTER_MAP


__all__ = [
    "DEVICE_NAME",
    "WHO_AM_I",
    "DigitalInputs",
    "DigitalOutputs",
    "AnalogOutputs",
    "PlayerType",
    "DOPortStatePayload",
    "DOPortSetPayload",
    "DOPortClearPayload",
    "ExternalTriggerStatePayload",
    "AOPortStatePayload",
    "DacReadyPayload",
    "DacStartPayload",
    "DacPausePayload",
    "DacAbortPayload",
    "DacFinishedPayload",
    "ChannelExternalTriggers0Payload",
    "ChannelExternalTriggers1Payload",
    "ChannelExternalTriggers2Payload",
    "ChannelExternalTriggers3Payload",
    "ActivePlayer0Payload",
    "ActivePlayer1Payload",
    "ActivePlayer2Payload",
    "ActivePlayer3Payload",
    "FileSettings0Payload",
    "FileSettings1Payload",
    "FileSettings2Payload",
    "FileSettings3Payload",
    "SineSettings0Payload",
    "SineSettings1Payload",
    "SineSettings2Payload",
    "SineSettings3Payload",
    "TrapezoidSettings0Payload",
    "TrapezoidSettings1Payload",
    "TrapezoidSettings2Payload",
    "TrapezoidSettings3Payload",
    "DOPortState",
    "DOPortSet",
    "DOPortClear",
    "ExternalTriggerState",
    "AOPortState",
    "AOChannel0",
    "AOChannel1",
    "AOChannel2",
    "AOChannel3",
    "DacReady",
    "DacStart",
    "DacPause",
    "DacAbort",
    "DacFinished",
    "ChannelExternalTriggers0",
    "ChannelExternalTriggers1",
    "ChannelExternalTriggers2",
    "ChannelExternalTriggers3",
    "ActivePlayer0",
    "ActivePlayer1",
    "ActivePlayer2",
    "ActivePlayer3",
    "FileSettings0",
    "FileSettings1",
    "FileSettings2",
    "FileSettings3",
    "SineSettings0",
    "SineSettings1",
    "SineSettings2",
    "SineSettings3",
    "TrapezoidSettings0",
    "TrapezoidSettings1",
    "TrapezoidSettings2",
    "TrapezoidSettings3",
    "REGISTER_MAP",
]

DEVICE_NAME: str = "Quac"
WHO_AM_I: int = 1411


class DigitalInputs(enum.IntFlag):
    """Specifies the external trigger input lines available on the device."""

    DI0 = 0x1
    """External trigger input line 0."""

    DI1 = 0x2
    """External trigger input line 1."""

    DI2 = 0x4
    """External trigger input line 2."""

    DI3 = 0x8
    """External trigger input line 3."""


class DigitalOutputs(enum.IntFlag):
    """Specifies the digital output lines available on the device."""

    DO0 = 0x1
    """Digital output line 0."""

    DO1 = 0x2
    """Digital output line 1."""

    DO2 = 0x4
    """Digital output line 2."""

    DO3 = 0x8
    """Digital output line 3."""


class AnalogOutputs(enum.IntFlag):
    """Specifies the ten volt bipolar analog output channels available on the device."""

    AO0 = 0x1
    """Analog output channel 0."""

    AO1 = 0x2
    """Analog output channel 1."""

    AO2 = 0x4
    """Analog output channel 2."""

    AO3 = 0x8
    """Analog output channel 3."""


class PlayerType(enum.IntEnum):
    """Specifies the waveform player driving an analog output channel."""

    FILE = 0
    """Plays samples streamed from a file on the SD card."""

    SINE = 1
    """Plays a synthesized sine waveform."""

    TRAPEZOID = 2
    """Plays a synthesized trapezoid waveform."""


class DOPortStatePayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DOPortState register."""

    __value__: DigitalOutputs = BitMask(enum=DigitalOutputs)


class DOPortSetPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DOPortSet register."""

    __value__: DigitalOutputs = BitMask(enum=DigitalOutputs)


class DOPortClearPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DOPortClear register."""

    __value__: DigitalOutputs = BitMask(enum=DigitalOutputs)


class ExternalTriggerStatePayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ExternalTriggerState register."""

    __value__: DigitalInputs = BitMask(enum=DigitalInputs)


class AOPortStatePayload(StructPayload[np.float32], length=4):
    """Represents the payload of the AOPortState register."""

    ao_channel0: np.float32 = Field(IdentityConverter(np.float32))
    """The output voltage of analog output channel 0 in volts."""

    ao_channel1: np.float32 = Field(IdentityConverter(np.float32), offset=1)
    """The output voltage of analog output channel 1 in volts."""

    ao_channel2: np.float32 = Field(IdentityConverter(np.float32), offset=2)
    """The output voltage of analog output channel 2 in volts."""

    ao_channel3: np.float32 = Field(IdentityConverter(np.float32), offset=3)
    """The output voltage of analog output channel 3 in volts."""


class DacReadyPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DacReady register."""

    __value__: AnalogOutputs = BitMask(enum=AnalogOutputs)


class DacStartPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DacStart register."""

    __value__: AnalogOutputs = BitMask(enum=AnalogOutputs)


class DacPausePayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DacPause register."""

    __value__: AnalogOutputs = BitMask(enum=AnalogOutputs)


class DacAbortPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DacAbort register."""

    __value__: AnalogOutputs = BitMask(enum=AnalogOutputs)


class DacFinishedPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the DacFinished register."""

    __value__: AnalogOutputs = BitMask(enum=AnalogOutputs)


class ChannelExternalTriggers0Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ChannelExternalTriggers0 register."""

    __value__: DigitalInputs = BitMask(enum=DigitalInputs)


class ChannelExternalTriggers1Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ChannelExternalTriggers1 register."""

    __value__: DigitalInputs = BitMask(enum=DigitalInputs)


class ChannelExternalTriggers2Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ChannelExternalTriggers2 register."""

    __value__: DigitalInputs = BitMask(enum=DigitalInputs)


class ChannelExternalTriggers3Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ChannelExternalTriggers3 register."""

    __value__: DigitalInputs = BitMask(enum=DigitalInputs)


class ActivePlayer0Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ActivePlayer0 register."""

    __value__: PlayerType = GroupMask(enum=PlayerType, mask=0xFF)


class ActivePlayer1Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ActivePlayer1 register."""

    __value__: PlayerType = GroupMask(enum=PlayerType, mask=0xFF)


class ActivePlayer2Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ActivePlayer2 register."""

    __value__: PlayerType = GroupMask(enum=PlayerType, mask=0xFF)


class ActivePlayer3Payload(AnonymousPayload[np.uint8]):
    """Represents the payload of the ActivePlayer3 register."""

    __value__: PlayerType = GroupMask(enum=PlayerType, mask=0xFF)


class FileSettings0Payload(StructPayload[np.uint8], length=45):
    """Represents the payload of the FileSettings0 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    path: str = Field(StringConverter(33), offset=12)
    """Specifies the null-terminated path of the waveform file on the SD card."""


class FileSettings1Payload(StructPayload[np.uint8], length=45):
    """Represents the payload of the FileSettings1 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    path: str = Field(StringConverter(33), offset=12)
    """Specifies the null-terminated path of the waveform file on the SD card."""


class FileSettings2Payload(StructPayload[np.uint8], length=45):
    """Represents the payload of the FileSettings2 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    path: str = Field(StringConverter(33), offset=12)
    """Specifies the null-terminated path of the waveform file on the SD card."""


class FileSettings3Payload(StructPayload[np.uint8], length=45):
    """Represents the payload of the FileSettings3 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    path: str = Field(StringConverter(33), offset=12)
    """Specifies the null-terminated path of the waveform file on the SD card."""


class SineSettings0Payload(StructPayload[np.uint8], length=28):
    """Represents the payload of the SineSettings0 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""


class SineSettings1Payload(StructPayload[np.uint8], length=28):
    """Represents the payload of the SineSettings1 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""


class SineSettings2Payload(StructPayload[np.uint8], length=28):
    """Represents the payload of the SineSettings2 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""


class SineSettings3Payload(StructPayload[np.uint8], length=28):
    """Represents the payload of the SineSettings3 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""


class TrapezoidSettings0Payload(StructPayload[np.uint8], length=40):
    """Represents the payload of the TrapezoidSettings0 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""

    ramp_on_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=28)
    """Specifies the duration of the rising ramp in microseconds."""

    pulse_width_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=32)
    """Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds."""

    ramp_off_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=36)
    """Specifies the duration of the falling ramp in microseconds."""


class TrapezoidSettings1Payload(StructPayload[np.uint8], length=40):
    """Represents the payload of the TrapezoidSettings1 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""

    ramp_on_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=28)
    """Specifies the duration of the rising ramp in microseconds."""

    pulse_width_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=32)
    """Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds."""

    ramp_off_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=36)
    """Specifies the duration of the falling ramp in microseconds."""


class TrapezoidSettings2Payload(StructPayload[np.uint8], length=40):
    """Represents the payload of the TrapezoidSettings2 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""

    ramp_on_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=28)
    """Specifies the duration of the rising ramp in microseconds."""

    pulse_width_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=32)
    """Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds."""

    ramp_off_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=36)
    """Specifies the duration of the falling ramp in microseconds."""


class TrapezoidSettings3Payload(StructPayload[np.uint8], length=40):
    """Represents the payload of the TrapezoidSettings3 register."""

    cycles: np.uint32 = Field(IdentityConverter(np.uint32))
    """Specifies how many times the waveform is played, or zero to play it indefinitely."""

    duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=4)
    """Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform."""

    update_frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=8)
    """Specifies the sample update rate in hertz."""

    frequency: np.uint32 = Field(IdentityConverter(np.uint32), offset=12)
    """Specifies the frequency of the waveform in hertz."""

    amplitude: np.float32 = Field(IdentityConverter(np.float32), offset=16)
    """Specifies the peak amplitude of the waveform in volts, measured from its vertical centre."""

    vertical_shift: np.float32 = Field(IdentityConverter(np.float32), offset=20)
    """Specifies the vertical offset applied to the waveform in volts."""

    normalized_phase_shift: np.float32 = Field(IdentityConverter(np.float32), offset=24)
    """Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift)."""

    ramp_on_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=28)
    """Specifies the duration of the rising ramp in microseconds."""

    pulse_width_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=32)
    """Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds."""

    ramp_off_duration: np.uint32 = Field(IdentityConverter(np.uint32), offset=36)
    """Specifies the duration of the falling ramp in microseconds."""


class DOPortState(RegisterBase[DigitalOutputs]):
    """Reflects and specifies the state of the digital output lines."""

    address: ClassVar[int] = 32
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DOPortStatePayload


class DOPortSet(RegisterBase[DigitalOutputs]):
    """Sets the digital output lines specified in the mask to logic HIGH."""

    address: ClassVar[int] = 33
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DOPortSetPayload


class DOPortClear(RegisterBase[DigitalOutputs]):
    """Clears the digital output lines specified in the mask to logic LOW."""

    address: ClassVar[int] = 34
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DOPortClearPayload


class ExternalTriggerState(RegisterBase[DigitalInputs]):
    """Reflects the raw state of the external trigger input lines."""

    address: ClassVar[int] = 35
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ExternalTriggerStatePayload


class AOPortState(RegisterBase[AOPortStatePayload]):
    """Reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value."""

    address: ClassVar[int] = 36
    payload_type: ClassVar[PayloadType] = PayloadType.Float
    payload_class = AOPortStatePayload


class AOChannel0(RegisterFloat):
    """Reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform."""

    address: ClassVar[int] = 37


class AOChannel1(RegisterFloat):
    """Reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform."""

    address: ClassVar[int] = 38


class AOChannel2(RegisterFloat):
    """Reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform."""

    address: ClassVar[int] = 39


class AOChannel3(RegisterFloat):
    """Reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform."""

    address: ClassVar[int] = 40


class DacReady(RegisterBase[AnalogOutputs]):
    """Reflects which analog output channels are configured and ready to start."""

    address: ClassVar[int] = 41
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DacReadyPayload


class DacStart(RegisterBase[AnalogOutputs]):
    """Starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger."""

    address: ClassVar[int] = 42
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DacStartPayload


class DacPause(RegisterBase[AnalogOutputs]):
    """Pauses the channels set in the mask and resumes those cleared in it."""

    address: ClassVar[int] = 43
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DacPausePayload


class DacAbort(RegisterBase[AnalogOutputs]):
    """Aborts waveform playback on the analog output channels specified in the mask."""

    address: ClassVar[int] = 44
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DacAbortPayload


class DacFinished(RegisterBase[AnalogOutputs]):
    """Reports which analog output channels have finished playing their waveform."""

    address: ClassVar[int] = 45
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = DacFinishedPayload


class ChannelExternalTriggers0(RegisterBase[DigitalInputs]):
    """Specifies which external trigger lines can start channel 0."""

    address: ClassVar[int] = 46
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ChannelExternalTriggers0Payload


class ChannelExternalTriggers1(RegisterBase[DigitalInputs]):
    """Specifies which external trigger lines can start channel 1."""

    address: ClassVar[int] = 47
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ChannelExternalTriggers1Payload


class ChannelExternalTriggers2(RegisterBase[DigitalInputs]):
    """Specifies which external trigger lines can start channel 2."""

    address: ClassVar[int] = 48
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ChannelExternalTriggers2Payload


class ChannelExternalTriggers3(RegisterBase[DigitalInputs]):
    """Specifies which external trigger lines can start channel 3."""

    address: ClassVar[int] = 49
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ChannelExternalTriggers3Payload


class ActivePlayer0(RegisterBase[PlayerType]):
    """Specifies which waveform player drives analog output channel 0."""

    address: ClassVar[int] = 50
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ActivePlayer0Payload


class ActivePlayer1(RegisterBase[PlayerType]):
    """Specifies which waveform player drives analog output channel 1."""

    address: ClassVar[int] = 51
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ActivePlayer1Payload


class ActivePlayer2(RegisterBase[PlayerType]):
    """Specifies which waveform player drives analog output channel 2."""

    address: ClassVar[int] = 52
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ActivePlayer2Payload


class ActivePlayer3(RegisterBase[PlayerType]):
    """Specifies which waveform player drives analog output channel 3."""

    address: ClassVar[int] = 53
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ActivePlayer3Payload


class FileSettings0(RegisterBase[FileSettings0Payload]):
    """Specifies the file player settings for analog output channel 0."""

    address: ClassVar[int] = 54
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = FileSettings0Payload


class FileSettings1(RegisterBase[FileSettings1Payload]):
    """Specifies the file player settings for analog output channel 1."""

    address: ClassVar[int] = 55
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = FileSettings1Payload


class FileSettings2(RegisterBase[FileSettings2Payload]):
    """Specifies the file player settings for analog output channel 2."""

    address: ClassVar[int] = 56
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = FileSettings2Payload


class FileSettings3(RegisterBase[FileSettings3Payload]):
    """Specifies the file player settings for analog output channel 3."""

    address: ClassVar[int] = 57
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = FileSettings3Payload


class SineSettings0(RegisterBase[SineSettings0Payload]):
    """Specifies the sine player settings for analog output channel 0."""

    address: ClassVar[int] = 58
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = SineSettings0Payload


class SineSettings1(RegisterBase[SineSettings1Payload]):
    """Specifies the sine player settings for analog output channel 1."""

    address: ClassVar[int] = 59
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = SineSettings1Payload


class SineSettings2(RegisterBase[SineSettings2Payload]):
    """Specifies the sine player settings for analog output channel 2."""

    address: ClassVar[int] = 60
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = SineSettings2Payload


class SineSettings3(RegisterBase[SineSettings3Payload]):
    """Specifies the sine player settings for analog output channel 3."""

    address: ClassVar[int] = 61
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = SineSettings3Payload


class TrapezoidSettings0(RegisterBase[TrapezoidSettings0Payload]):
    """Specifies the trapezoid player settings for analog output channel 0."""

    address: ClassVar[int] = 62
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = TrapezoidSettings0Payload


class TrapezoidSettings1(RegisterBase[TrapezoidSettings1Payload]):
    """Specifies the trapezoid player settings for analog output channel 1."""

    address: ClassVar[int] = 63
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = TrapezoidSettings1Payload


class TrapezoidSettings2(RegisterBase[TrapezoidSettings2Payload]):
    """Specifies the trapezoid player settings for analog output channel 2."""

    address: ClassVar[int] = 64
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = TrapezoidSettings2Payload


class TrapezoidSettings3(RegisterBase[TrapezoidSettings3Payload]):
    """Specifies the trapezoid player settings for analog output channel 3."""

    address: ClassVar[int] = 65
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = TrapezoidSettings3Payload


REGISTER_MAP: dict[int, type[RegisterBase[Any]]] = {
    **_CORE_REGISTER_MAP,
    32: DOPortState,
    33: DOPortSet,
    34: DOPortClear,
    35: ExternalTriggerState,
    36: AOPortState,
    37: AOChannel0,
    38: AOChannel1,
    39: AOChannel2,
    40: AOChannel3,
    41: DacReady,
    42: DacStart,
    43: DacPause,
    44: DacAbort,
    45: DacFinished,
    46: ChannelExternalTriggers0,
    47: ChannelExternalTriggers1,
    48: ChannelExternalTriggers2,
    49: ChannelExternalTriggers3,
    50: ActivePlayer0,
    51: ActivePlayer1,
    52: ActivePlayer2,
    53: ActivePlayer3,
    54: FileSettings0,
    55: FileSettings1,
    56: FileSettings2,
    57: FileSettings3,
    58: SineSettings0,
    59: SineSettings1,
    60: SineSettings2,
    61: SineSettings3,
    62: TrapezoidSettings0,
    63: TrapezoidSettings1,
    64: TrapezoidSettings2,
    65: TrapezoidSettings3,
}
