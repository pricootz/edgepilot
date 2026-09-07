#!/usr/bin/env bash
set -euo pipefail
source_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
"$source_dir/EdgePilot" --install
printf '%s\n' "Puoi aprire EdgePilot dal menu Applicazioni."
