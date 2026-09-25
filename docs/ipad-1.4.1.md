# iPad release 1.4.1 (7) — family DEX correction

## Status

Unity export and signed Xcode Release build succeeded. **Not yet installed.** The paired iPad was
unavailable at both connection checks; installation waits for a connected/unlocked device and user-confirmed save.
The authoritative installed release remains [1.4.0 (6)](ipad-1.4.0.md).

## Scope

Includes `4fbddb8`: one DEX entry per evolution family (**142 entries**, containing **247 form pages**),
family-based collection/filtering and encounter assistance, private unknown-form pages, and stable outer
pagination. Preserves exact-form history and schema v13 without resetting or rewriting progress.

Verification already completed: Unity EditMode **215/215**, Node **15/15**, and synthetic 4:3 layout
inspection of known and unknown internal form pages. See [family-dex.md](family-dex.md).

Unity export: `unity/Builds/iOS/Numeria-11`.
Native build directory: `/tmp/numeria-family-ios-derived`.
Ready app: `/tmp/numeria-family-ios-derived/Build/Products/Release-iphoneos/Numeria.app`.
Deep/strict signature verification passed; app identity/version are `com.yuankunxue.numeria` / 1.4.1 (7).
Existing provisioning profile still expires September 27, 2026 at 19:12 PDT; it was not renewed.
Prepared backup directory: `~/Documents/Numeria Save Backups/ipad-before-1.4.1-6xOWDSVL/`.
This directory is **empty**, not a successful backup: no device save transfer or installation has occurred yet.
Logs: `/tmp/numeria-family-ios-export.log`, `/tmp/numeria-family-xcode-release.log`.
Bundle ID/team must remain `com.yuankunxue.numeria` / `7B74WP6AR2`.

## Required installation checks

Before replacing the app, back up the device's latest Documents; never restore a dated Mac snapshot
or uninstall the app. After installing in place, retrieve Documents again **before first launch** and
verify recursive file inventory and all bytes match. Then launch and confirm the device app version.
Physical-device acceptance: LOAD GAME → saved slot, progress/coins intact, DEX denominator 142,
and Addmander/Sumdrake/Equadragon displayed through internal FORM pages rather than separate cards.
Confirm sound and visuals. Do not equate successful process launch with complete gameplay verification.
