#!/usr/bin/env bash
set -euo pipefail

package_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

has_xcb_shape=0
ldconfig_bin="$(command -v ldconfig || true)"
if [[ -z "$ldconfig_bin" && -x /sbin/ldconfig ]]; then
  ldconfig_bin=/sbin/ldconfig
fi
if [[ -n "$ldconfig_bin" ]]; then
  ldconfig_output="$($ldconfig_bin -p 2>/dev/null || true)"
  if grep -Fq 'libxcb-shape.so.0' <<<"$ldconfig_output"; then
    has_xcb_shape=1
  fi
fi
if [[ $has_xcb_shape -eq 0 ]]; then
  for libdir in /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu /lib64 /usr/lib64; do
    if [[ -e "$libdir/libxcb-shape.so.0" ]]; then
      has_xcb_shape=1
      break
    fi
  done
fi

if [[ $has_xcb_shape -eq 0 ]]; then
  cat >&2 <<'EOF'
EdgePilot requires libxcb-shape.so.0 to keep transparent desktop areas click-through on Linux.
Debian / Ubuntu / Parrot: sudo apt install libxcb-shape0
Install that package, then run install.sh again.
EOF
  exit 1
fi

"$package_dir/EdgePilot" --install
printf '%s\n' 'EdgePilot installed. Open it from the Applications menu.'
