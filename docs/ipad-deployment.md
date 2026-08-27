# Numeria iPad deployment and save migration

This runbook keeps Lucas's progress recoverable throughout development. Never uninstall the iPad app or change
the bundle identifier without exporting another backup first.

## Current verified state

- Unity: `6000.5.6f1`, iOS Build Support installed
- Xcode: `26.6` (`17F113`)
- Bundle identifier: `com.yuankunxue.numeria` — treat this as permanent
- Minimum OS: iOS/iPadOS 15.0
- Device family: universal iPhone + iPad
- Orientation: landscape left and landscape right only
- Finder Files access: `UIFileSharingEnabled` and `LSSupportsOpeningDocumentsInPlace` are enabled after export
- Unity EditMode: 131/131 passing; Node prototype: 15/15 passing
- Unity Xcode export: passing
- Unsigned arm64 iOS device compilation: passing with `CODE_SIGNING_ALLOWED=NO`
- Current Debug `.app`: about 959 MB; optimize generated texture compression before TestFlight

The local Xcode build directory is ignored by Git. Use the newest `unity/Builds/iOS/Numeria-*` directory.

## Lucas's protected migration files

The original macOS directory was copied without deleting it. A raw directory backup, checksums, and the importable
single-file package are stored under:

```text
~/Documents/Numeria Save Backups/2026-08-26-before-ipad-port/
```

The current Finder-transfer file is:

```text
ipad-transfer/numeria-backup-ipad-transfer-20260827-003228-033.json
```

SHA-256:

```text
7d990315764f720812fdd5e6b58fe1e94aa8cf40ae34ac15d79b2c5703b8ba4e
```

The package contains save schema v9, active Slot 1, and was verified against the original slot file. Keep the
entire parent backup directory; the raw save is an independent recovery route if the package format ever changes.

## Build the Xcode project

1. Close any older generated Numeria project in Xcode.
2. Open the Unity project at `unity/`.
3. Choose **Numeria → iOS → Configure Project** if settings need to be refreshed.
4. Choose **Numeria → iOS → Build Xcode Project**.
5. Unity writes to a new versioned directory instead of overwriting an existing Xcode project.

The same export can be run headlessly:

```bash
DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer \
/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/unity" \
  -executeMethod Numeria.Editor.NumeriaIosBuild.BuildXcodeProject \
  -quit -logFile /tmp/numeria-ios-build.log
```

## Sign and install on Lucas's iPad

1. Connect and unlock the iPad with a data-capable USB cable; tap **Trust** if prompted.
2. Open the newest `Unity-iPhone.xcodeproj` from `unity/Builds/iOS/Numeria-*`.
3. In Xcode, add the Apple Account under **Settings → Accounts**.
4. Select the **Unity-iPhone** target, open **Signing & Capabilities**, enable automatic signing, and choose the
   correct Team. Do not edit the bundle identifier.
5. Select Lucas's iPad as the run destination and press **Run**.
6. If iPadOS asks for Developer Mode or developer trust, follow the device prompt and run again.

A free Personal Team works for local testing but must be reprovisioned periodically. TestFlight requires an Apple
Developer Program membership.

## Import Lucas's progress

1. Launch Numeria once on the iPad so iPadOS creates its Documents container, then close the game.
2. In Finder, select the iPad, open **Files**, expand **Numeria**, and drag in the single-file migration JSON above.
3. Relaunch Numeria. It is safe if the title initially shows a blank slot.
4. Open **MENU → SAVES → IMPORT LATEST**.
5. Confirm the filename. Numeria automatically exports the iPad's current slots as a `pre-import` backup before
   replacing anything; invalid or newer-schema files are rejected before writes begin.
6. After the game reloads, verify at minimum:
   - Slot 1 is active.
   - Current map is Dark Mines.
   - Battle buddy is Shaleling.
   - Coins show 311.
   - Team, levels, inventory, crystals, opened chests, merchants, and records look correct.
7. Play briefly, return to the menu with **SAVE & RETURN**, relaunch, and confirm the imported state persists.

## Ongoing backups

Use **MENU → SAVES → EXPORT BACKUP** before installing a new build. Finder exposes the generated file inside
Numeria's `Numeria Backups` folder. Copy it back to the Mac and keep it outside the repository.

App updates keep the same iOS Documents container when the bundle identifier stays unchanged. Uninstalling the app
removes that container, so export first even when a code update is expected to be safe.
