# iPad release 1.4.2 (8) — question replay

## Status (September 25, 2026)

**Installed in place and launched on September 25, 2026.** After the user confirmed saved/main-menu
readiness, a fresh final Documents backup was taken and validated. After installation and before first
launch, all four save files and their complete recursive inventory matched that backup byte-for-byte.
Device inventory confirms 1.4.2 (8); launch succeeded. Physical-device replay/progress acceptance is pending.

Includes `e43565d`: small speaker / LISTEN on all math UI and eight depth mechanisms, offline statement
replay without resetting selections, seven new voice clips, and the puzzle-design audit. The proposed
mechanism redesigns are not included. Unity 223/223 and Node 15/15 passed before export.
Save schema remains v13. Bundle/team remain `com.yuankunxue.numeria` / `7B74WP6AR2`.

## Prepared build

- Unity export: `unity/Builds/iOS/Numeria-12`.
- Signed Release app: `/tmp/numeria-replay-ios-derived/Build/Products/Release-iphoneos/Numeria.app`.
- Unity log: `/tmp/numeria-replay-ios-export.log`.
- Xcode log: `/tmp/numeria-replay-xcode-release.log` (`BUILD SUCCEEDED`).
- Strict/deep codesign validation passed; Info.plist confirms 1.4.2 (8) and the original bundle ID.
- Existing provisioning expires **September 27, 2026, 19:12 PDT** (September 28 02:12:33 UTC).
  This build did not extend the signing period.

## Device backup and preservation evidence

First read failed due to a connection reset, with no backup directory produced. Read-only retry succeeded:

`/Users/yuankunxue/Documents/Numeria Save Backups/ipad-before-1.4.2-Z5QxIE58/initial-retry-Documents`

Four files copied. Slot 2: **Verdant Vault, 3,664 coins, active Mushmellow**, v13, 61 recorded forms,
59 caught entries plus starter, Lunabyss defeated. After user confirmation, `final-Documents` and then
`post-install-Documents` were copied into the same parent directory. All three copies have the identical
four files/hashes below. The post-install copy was taken before the first launch. No uninstall, save import,
or Mac progress replacement occurred. Future progress on the iPad remains authoritative.

| File | SHA-256 |
| --- | --- |
| numeria-active-slot.txt | `d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35` |
| numeria-save-slot-1.json | `657d723d8e8475e95b8dcb64fc612ac33f56e4c0e3c078e8ed5271d7691ceef5` |
| numeria-save-slot-2.json | `dcc96af6007347e372855dcd3a84c12688a83d4bea34c0fad68b4a8cf215cf95` |
| numeria-save.json | `1be24b0ee0e177fa956606c6dfbb1395fcde1e03964316d8bbf0134dfe9dd7d7` |

## Deployment results and remaining acceptance

- Installation: `/tmp/numeria-replay-install.json` (succeeded).
- Installed version: `/tmp/numeria-replay-installed-after.json` (1.4.2 / 8).
- Backup results: `/tmp/numeria-replay-final-backup.json`, `/tmp/numeria-replay-post-backup.json`.
- Launch: `/tmp/numeria-replay-launch.json` (succeeded).
- User asked to load SLOT 2, confirm Verdant Vault / 3,664 coins, then tap LISTEN on a math or mechanism
  question and verify the complete statement is replayed. Acceptance remains pending; launch is not audio/touch QA.
- Future updates must take another fresh device backup; never restore this older snapshot over later play.
