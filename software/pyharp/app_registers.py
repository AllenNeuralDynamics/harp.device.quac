"""App registers for the quad-DAC device.

Registers are generated at import time from the shared ``device.yml`` schema
via ``harp.device.schema.create_device_module``. Addresses, struct field
layouts (offsets/types), and enum values all come straight from device.yml,
so there is nothing here to hand-maintain or let drift out of sync with the
firmware (the old address-only ``AppRegs`` IntEnum and the manual struct
format strings are gone).
"""
import git
from pathlib import Path

from harp.device.schema import create_device_module

repo = git.Repo(".", search_parent_directories=True)
device_yaml_path = Path(repo.working_tree_dir) / Path("device.yml")

AppRegs = create_device_module(device_yaml_path.read_bytes())

# Waveform player selection for the ActivePlayersN registers, generated from
# the `PlayerType` groupMask in device.yml. Members are WaveformType.FILE,
# .SINE and .TRAPEZOID.
WaveformType = AppRegs.PlayerType

# Per-channel register lookups, so scripts can index by channel number the
# same way the old code added CHANNEL to a base register address.
ACTIVE_PLAYERS = [
    AppRegs.ActivePlayer0, AppRegs.ActivePlayer1,
    AppRegs.ActivePlayer2, AppRegs.ActivePlayer3,
]
FILE_SETTINGS = [
    AppRegs.FileSettings0, AppRegs.FileSettings1,
    AppRegs.FileSettings2, AppRegs.FileSettings3,
]
SINE_SETTINGS = [
    AppRegs.SineSettings0, AppRegs.SineSettings1,
    AppRegs.SineSettings2, AppRegs.SineSettings3,
]
TRAPEZOID_SETTINGS = [
    AppRegs.TrapezoidSettings0, AppRegs.TrapezoidSettings1,
    AppRegs.TrapezoidSettings2, AppRegs.TrapezoidSettings3,
]
AO_CHANNELS = [
    AppRegs.AOChannel0, AppRegs.AOChannel1,
    AppRegs.AOChannel2, AppRegs.AOChannel3,
]
CHANNEL_EXTERNAL_TRIGGERS = [
    AppRegs.ChannelExternalTriggers0, AppRegs.ChannelExternalTriggers1,
    AppRegs.ChannelExternalTriggers2, AppRegs.ChannelExternalTriggers3,
]

if __name__ == "__main__":
    for address, reg in sorted(AppRegs.REGISTER_MAP.items()):
        print(f"{reg.__name__}: {address}")
