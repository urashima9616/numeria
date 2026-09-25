# iPad release 1.4.1 (7) — family DEX correction

## Status

**Installed in place and launched on September 25, 2026.** The iPad became connected after the user
asked to retry. Fresh initial/final Documents backups matched; all four device save files were then
verified byte-for-byte unchanged after installation and before first launch. Device inventory confirms
1.4.1 (7). User confirmed saved progress and family/form navigation are normal on the physical iPad.

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
Verified backup directory: `~/Documents/Numeria Save Backups/ipad-before-1.4.1-6xOWDSVL/`.
Contains `initial-Documents`, `final-Documents` and `post-install-Documents`, with identical file inventories/content.
No uninstall, save import, or Mac progress replacement occurred.
Logs: `/tmp/numeria-family-ios-export.log`, `/tmp/numeria-family-xcode-release.log`.
Bundle ID/team must remain `com.yuankunxue.numeria` / `7B74WP6AR2`.

## Save preservation evidence

Slot 2 immediately before installation: forest, **3,278 coins**, Addmander family stage 1 / Lv.8 / XP 23,
50 caught entries plus starter, 52 recorded forms, save schema v13. Active slot remains 2; all other saves retained.
The post-install copy was taken before launching the app. File inventory and bytes matched the final backup:

| File | SHA-256 |
| --- | --- |
| numeria-active-slot.txt | `d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35` |
| numeria-save-slot-1.json | `657d723d8e8475e95b8dcb64fc612ac33f56e4c0e3c078e8ed5271d7691ceef5` |
| numeria-save-slot-2.json | `4df7b52abad18f5bd33ae78f9c06210d6dcb2eb777eb3f7f7ef8828e019845bd` |
| numeria-save.json | `1be24b0ee0e177fa956606c6dfbb1395fcde1e03964316d8bbf0134dfe9dd7d7` |

Device install/inventory/launch results: `/tmp/numeria-family-install.json`,
`/tmp/numeria-family-installed-after.json`, `/tmp/numeria-family-launch.json`.
The initial sandboxed signature recheck could not reach trust services; the escalated strict/deep check passed.

## Ongoing safety and acceptance

Before replacing the app, back up the device's latest Documents; never restore a dated Mac snapshot
or uninstall the app. After installing in place, retrieve Documents again **before first launch** and
verify recursive file inventory and all bytes match. Then launch and confirm the device app version.
Physical-device acceptance: LOAD GAME → saved slot, progress/coins intact, DEX denominator 142,
and Addmander/Sumdrake/Equadragon displayed through internal FORM pages rather than separate cards.
Confirm sound and visuals. Do not equate successful process launch with complete gameplay verification.
