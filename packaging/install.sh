#!/usr/bin/env bash
set -euo pipefail
package_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
"$package_dir/EdgePilot" --install
printf '%s\n' 'EdgePilot installed. Open it from the Applications menu.'
