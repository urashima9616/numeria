# iPad 1.4.3 (9) — 300-member team

## Status — September 28, 2026

**Not installed: signing blocked by expired Xcode account credentials.** User requested development and installation;
device save/main-menu readiness confirmation requested. Unity 229/229 and Node 15/15 passed. See
[capacity behavior and safeguards](team-capacity-300.md).
Original bundle/team: `com.yuankunxue.numeria` / `7B74WP6AR2`. Schema remains v13.

## Initial backup

`/Users/yuankunxue/Documents/Numeria Save Backups/ipad-before-1.4.3-T1Qebuj8/initial-Documents`

Read successfully from the connected iPad. Slot 2: **Rime Gallery, 5,492 coins, active Paircub family,
99 total team members, 106 recorded forms, v13**. This is newer than the 1.4.2 deployment snapshot; never
replace it with older Mac progress. Final backup and post-install byte comparison are still required.

| File | Initial SHA-256 |
| --- | --- |
| numeria-active-slot.txt | `d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35` |
| numeria-save-slot-1.json | `657d723d8e8475e95b8dcb64fc612ac33f56e4c0e3c078e8ed5271d7691ceef5` |
| numeria-save-slot-2.json | `5f29f209034cbc561a7db04d7065ecb03ae21d3a924da15e273d562f98eb4618` |
| numeria-save.json | `1be24b0ee0e177fa956606c6dfbb1395fcde1e03964316d8bbf0134dfe9dd7d7` |

The previously installed provisioning profile expired September 27. A newly valid profile must be verified
before installing; compiling without renewed signing is not a completed update.

## Build / current blocker

- Unity successfully exported `unity/Builds/iOS/Numeria-13`; log `/tmp/numeria-team300-ios-export.log`.
- Signed Release build with `-allowProvisioningUpdates DEVELOPMENT_TEAM=7B74WP6AR2 CODE_SIGN_STYLE=Automatic`
  failed: Xcode keychain account credentials are invalid (`missing Xcode-Username`), followed by
  `No Accounts` and `No profiles for com.yuankunxue.numeria`.
- Signed build log: `/tmp/numeria-team300-xcode-release.log`.
- User asked to reauthenticate the original Apple account in Xcode → Settings → Accounts. Do not change the
  bundle/team, inspect passwords, or uninstall the iPad app to work around this.
- Unsigned native compile started separately with `CODE_SIGNING_ALLOWED=NO`; that product is **not installable**.
  Derived data: `/tmp/numeria-team300-ios-derived`; log `/tmp/numeria-team300-xcode-unsigned.log`.
- Read-only iPad process check found no running Numeria process. No app was terminated or replaced.

After login, rerun signed build, verify codesign, version and **new** provisioning expiration, take another fresh
final device backup, install in place and compare post-install files before launching. The initial backup is not
a substitute for a fresh final backup if Lucas has played again.
