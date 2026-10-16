# iPad 1.5.0 (10) — Halloween expansion

## Current state — October 16, 2026

**Signed build ready; feature commit `936ba52` pushed to `origin/codex/numeria`. Not installed.**
An initial read-only iPad save backup succeeded; no device save has been modified.
Implementation/evidence: [Halloween checklist](halloween-implementation.md).
The connected device is an iPad (10th generation); connectivity alone does not confirm Lucas has saved.

## Initial backup — not the final pre-install snapshot

- Initial copy: a new, uniquely named local backup; all JSON files parse successfully.
- Copy evidence: `/tmp/numeria-halloween-initial-backup.json`, successful.
- Private progress details, file inventory and hashes are retained only in local backup records,
  not published in this repository. The device snapshot is newer than the historical backup.
- No Numeria process was running at the check (`/tmp/numeria-halloween-before-processes-full.json`).
  The first connection attempt reset; the later tunnel/read succeeded. An intermediate filter attempt
  failed because the process executable is a URL, not a string; the unfiltered JSON inventory succeeded.
- Still waiting for the user's fresh saved/menu/unlocked confirmation. Do not install on the strength
  of this initial snapshot or a prior process check; repeat the final checks immediately before installation.

## Build evidence

- Unity export: `unity/Builds/iOS/Numeria-15`.
- Export log: `/tmp/numeria-halloween-final-ios-export.log`, successful.
- Native log: `/tmp/numeria-halloween-final-xcode.log`, `BUILD SUCCEEDED`, process exit 0.
- App: `/tmp/numeria-halloween-final-derived/Build/Products/Release-iphoneos/Numeria.app`.
- Bundle/team unchanged: `com.yuankunxue.numeria` / `7B74WP6AR2`.
- App Info.plist: 1.5.0 (10). Strict/deep codesign verification passed; profile and app entitlements match.
- Xcode reused the existing valid development profile, expiring **October 19, 2026 at 18:23 PDT**
  (October 20 01:23:47 UTC). Rebuilding did not renew its lifetime. Do not promise another seven days.
- Unity 248/248 and Node 15/15 passed. This does not prove device performance/audio/touch behavior.

Native build command (uses existing Xcode account; no account/bundle substitution):

```sh
DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer xcodebuild \
  -project unity/Builds/iOS/Numeria-15/Unity-iPhone.xcodeproj \
  -scheme Unity-iPhone -configuration Release -sdk iphoneos \
  -destination 'generic/platform=iOS' \
  -derivedDataPath /tmp/numeria-halloween-final-derived \
  -allowProvisioningUpdates DEVELOPMENT_TEAM=7B74WP6AR2 CODE_SIGN_STYLE=Automatic
```

## Required safe installation gates

1. Fresh confirmation: Lucas has saved, returned to the menu, and the connected iPad is unlocked.
   If the app has expired, do not uninstall it. Confirm it cannot be running instead.
2. Copy the app's complete Documents directory into a new timestamped backup beneath
   `~/Documents/Numeria Save Backups/`; verify file inventory, JSON readability and SHA-256 hashes.
   Inspect current slots; never use the old October 12 coin/partner totals as current expectations.
3. Check that the game is not running and take a final fresh copy immediately before installation.
   A prior snapshot is not a substitute for this final copy.
4. Install this signed app over the same bundle/team. Never uninstall, seed a starter save,
   import an old Mac save or restore a historical iPad snapshot.
5. Before first launch, copy Documents again and compare every filename and byte with the final backup.
   Stop if anything differs; do not launch and autosave over an unexplained discrepancy.
6. Launch, load Lucas's actual active slot, and ask the user to confirm the freshly recorded progress.
   Then verify TEAM/DEX, sound, touch controls and existing map behavior.
7. Visit both new WORLD destinations after Veilmoon Lake, verify six puzzles/LISTEN, ordinary and Boss
   battles, voluntary evolution, save/reload and responsive performance on the iPad. If Lucas's slot has
   not unlocked the worlds, do not alter his progress for testing; use a separate unused slot with permission.

## Rollback boundary

The source change is a normal revertible Git commit. Saves migrate from v13 to v14 only through normal
game loading/saving; existing IDs/stats/history remain intact and evolution is never automatic.
**Git revert is not save downgrade.** Do not install an old v13 app over a subsequently played v14 slot.
Keep the pre-update backup, but never restore it over newer play without explicit informed permission.

Final-backup, installation and user-acceptance evidence will be appended after those gates actually pass.
