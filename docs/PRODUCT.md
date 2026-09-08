# Product scope

EdgePilot keeps useful local machine state one screen edge away. Its first preview focuses on a compact desktop monitor that opens when needed.

## Included in v0.1

Windows and Linux desktop support; CPU, physical memory, disk capacity, network throughput and interface details; uptime and machine identity; four-edge placement; hover expansion, delayed fold, pinning and tooltips; persistent settings; disk selection; tray integration; optional start at login; per-user installation.

Refresh intervals are 0.5, 1, 2 or 5 seconds. The interface ships in English and Italian and follows the operating system until the user chooses otherwise. Repository documentation uses English.

## Interaction contract

1. The collapsed pill occupies a narrow strip at the selected edge of the usable working area.
2. Pointer entry expands it; pointer exit folds after a 450 ms grace period.
3. Pinning retains the expanded view; moving into the tooltip must not fold the notch.
4. Always-open and hidden modes are available independently of pointer interaction.
5. Reopening the executable recovers settings through the running instance.
6. Changing the interface language takes effect at once, without a restart.
7. Missing readings and missing selected volumes degrade visibly without inventing replacement data.

## Future scope

Network interface selection, richer metrics, accessibility, further languages and broader desktop validation come before additional modules. Remote servers, Docker, clipboard tools and quick actions are ideas, not shipped features. GPU, temperature and fan sensors are not part of this preview.
