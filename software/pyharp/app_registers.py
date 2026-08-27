"""App registers for the quad-DAC device.

Registers are generated at import time from the shared ``device.yml`` schema
via ``harp.device.schema.create_device_module``. Addresses, struct field
layouts (offsets/types), and enum values all come straight from device.yml,
so there is nothing here to hand-maintain or let drift out of sync with the
firmware (the old address-only ``device_module`` IntEnum and the manual struct
format strings are gone).
"""
import git
from pathlib import Path

from harp.device.schema import create_device_module

repo = git.Repo(".", search_parent_directories=True)
device_yaml_path = Path(repo.working_tree_dir) / Path("device.yml")

device_module = create_device_module(device_yaml_path.read_bytes())

# Waveform player selection for the ActivePlayersN registers, generated from
# the `PlayerType` groupMask in device.yml. Members are WaveformType.FILE,
# .SINE and .TRAPEZOID.
WaveformType = device_module.PlayerType

# Per-channel register lookups, so scripts can index by channel number the
# same way the old code added CHANNEL to a base register address.
ACTIVE_PLAYERS = [
    device_module.ActivePlayer0, device_module.ActivePlayer1,
    device_module.ActivePlayer2, device_module.ActivePlayer3,
]
FILE_SETTINGS = [
    device_module.FileSettings0, device_module.FileSettings1,
    device_module.FileSettings2, device_module.FileSettings3,
]
SINE_SETTINGS = [
    device_module.SineSettings0, device_module.SineSettings1,
    device_module.SineSettings2, device_module.SineSettings3,
]
TRAPEZOID_SETTINGS = [
    device_module.TrapezoidSettings0, device_module.TrapezoidSettings1,
    device_module.TrapezoidSettings2, device_module.TrapezoidSettings3,
]
AO_CHANNELS = [
    device_module.AOChannel0, device_module.AOChannel1,
    device_module.AOChannel2, device_module.AOChannel3,
]
CHANNEL_EXTERNAL_TRIGGERS = [
    device_module.ChannelExternalTriggers0, device_module.ChannelExternalTriggers1,
    device_module.ChannelExternalTriggers2, device_module.ChannelExternalTriggers3,
]

if __name__ == "__main__":
    for address, reg in sorted(device_module.REGISTER_MAP.items()):
        print(f"{reg.__name__}: {address}")
