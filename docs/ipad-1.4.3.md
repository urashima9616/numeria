# iPad 1.4.3 (9) — 300-member team

## Status — September 28, 2026

**Installed in place; first launch blocked by iPad security/trust.** User reauthenticated the original Apple account.
A fresh final backup succeeded; after installation, all four files and the full inventory matched it byte-for-byte
before the attempted first launch. Device inventory confirms 1.4.3 (9). Unity 229/229 and Node 15/15 passed. See
[capacity behavior and safeguards](team-capacity-300.md).
Original bundle/team: `com.yuankunxue.numeria` / `7B74WP6AR2`. Schema remains v13.

## Device backups

`/Users/yuankunxue/Documents/Numeria Save Backups/ipad-before-1.4.3-T1Qebuj8/initial-Documents`

Read successfully from the connected iPad. Slot 2: **Rime Gallery, 5,492 coins, active Paircub family,
99 total team members, 106 recorded forms, v13**. This is newer than the 1.4.2 deployment snapshot; never
replace it with older Mac progress. `final-Documents` and `post-install-Documents` in the same parent directory
both match the initial copy. Every file and byte was compared before the first launch attempt. No uninstall,
save import, restoration or Mac progress replacement occurred.

| File | Initial / final / post-install SHA-256 |
| --- | --- |
| numeria-active-slot.txt | `d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35` |
| numeria-save-slot-1.json | `657d723d8e8475e95b8dcb64fc612ac33f56e4c0e3c078e8ed5271d7691ceef5` |
| numeria-save-slot-2.json | `5f29f209034cbc561a7db04d7065ecb03ae21d3a924da15e273d562f98eb4618` |
| numeria-save.json | `1be24b0ee0e177fa956606c6dfbb1395fcde1e03964316d8bbf0134dfe9dd7d7` |

The previous profile expired September 27. The new embedded profile was verified to expire
**October 5, 2026 at 07:39 PDT** (14:39:43 UTC); bundle/team remain unchanged.

## Build / signing recovery

- Unity successfully exported `unity/Builds/iOS/Numeria-13`; log `/tmp/numeria-team300-ios-export.log`.
- Signed Release build with `-allowProvisioningUpdates DEVELOPMENT_TEAM=7B74WP6AR2 CODE_SIGN_STYLE=Automatic`
  failed: Xcode keychain account credentials are invalid (`missing Xcode-Username`), followed by
  `No Accounts` and `No profiles for com.yuankunxue.numeria`.
- Signed build log: `/tmp/numeria-team300-xcode-release.log`.
- User asked to reauthenticate the original Apple account in Xcode → Settings → Accounts. Do not change the
  bundle/team, inspect passwords, or uninstall the iPad app to work around this.
- Unsigned native compile passed separately with `CODE_SIGNING_ALLOWED=NO`; that product was **not installed**.
  Derived data: `/tmp/numeria-team300-ios-derived`; log `/tmp/numeria-team300-xcode-unsigned.log`.
- After user reauthentication, signed Release retry succeeded; log `/tmp/numeria-team300-xcode-signed-retry.log`.
- Ready app: `/tmp/numeria-team300-ios-derived/Build/Products/Release-iphoneos/Numeria.app`.
- Strict/deep codesign validation passed, Info.plist confirms 1.4.3 (9), and the renewed profile confirms original team.
- Final read-only iPad process check found no running Numeria process; no app needed to be terminated.

## Installation and pending device acceptance

- Installation succeeded: `/tmp/numeria-team300-install.json`.
- Installed inventory confirms 1.4.3 (9): `/tmp/numeria-team300-installed-after.json`.
- Final/post-install copy results: `/tmp/numeria-team300-final-backup.json`, `/tmp/numeria-team300-post-backup.json`.
- First launch denied by SBMainWorkspace Security, naming invalid signature, entitlements or profile trust as
  possible causes: `/tmp/numeria-team300-launch.json`. **Do not report the app as launched or playable yet.**
- Local strict/deep signature passes. App/profile application identifiers and team match; development entitlement
  is compatible, and the profile contains this iPad UDID. Device-side trust/verification is the next check,
  not a conclusively proven cause from the generic error alone.
- User should open Numeria manually and report the exact alert. If untrusted, use Settings → General →
  VPN & Device Management → the original Apple Development identity to trust/verify while online.
  Do not delete the app, reset progress, or trust an unrelated identity.
- After successful launch: LOAD GAME → SLOT 2, confirm Rime Gallery / 5,492 coins, TEAM **99/300** and ability
  to retain a newly caught family. Touch/performance and saved-progress acceptance still pending.

Never restore this snapshot over subsequent iPad play; the device is authoritative.
