#!/bin/bash
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
root_dir="$(cd "$script_dir/.." && pwd)"

dotnet run --project "$root_dir/src/PowerDown.UI/PowerDown.UI.csproj"
