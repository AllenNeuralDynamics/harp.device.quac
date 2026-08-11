"""App registers for the quad-DAC device."""
import git
import yaml
from enum import IntEnum
from pathlib import Path


repo = git.Repo(".", search_parent_directories=True)
device_yaml_path = Path(repo.working_tree_dir) / Path("device.yml")
yml = None
with open(device_yaml_path, "r") as yaml_file:
    yml = yaml.safe_load(yaml_file)
regs = {reg: data["address"] for reg, data in yml["registers"].items()}
AppRegs = IntEnum("AppRegs", regs)


# Values for the WaveformTypeN registers.
class WaveformType(IntEnum):
    File = 0
    Sine = 1
    Trapezoid = 2


# struct format strings matching the packed C++ structs in firmware/inc.
FILE_SETTINGS_FMT = "<III33s"
SINE_SETTINGS_FMT = "<IIIIff"  # 24 bytes
TRAPEZOID_SETTINGS_FMT = "<IIIIffII"  # 36 bytes

if __name__ == "__main__":
    for reg in AppRegs:
        print(f"{reg.name}: {reg.value}")
