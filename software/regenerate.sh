#!/usr/bin/env bash
# Regenerates the C# (Bonsai) and Python interfaces from device.yml using the Harp Toolkit.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

dotnet tool restore
dotnet restore software/bonsai/Interface/AllenNeuralDynamics.Quac.sln

dotnet harp.toolkit generate interface csharp device.yml \
    --namespace AllenNeuralDynamics.Quac \
    --output software/bonsai/Interface/AllenNeuralDynamics.Quac

dotnet harp.toolkit generate interface python device.yml \
    --output software/python
