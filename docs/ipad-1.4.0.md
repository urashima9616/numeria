# iPad release 1.4.0 (6) — September 25, 2026

Installed in place on the paired iPad (10th generation), using unchanged bundle identifier
`com.yuankunxue.numeria` and signing team `7B74WP6AR2`. Never uninstalled the app or imported a Mac save.

Includes the lifetime DEX, eight portal expansion layers, 80 ordinary species and eight collectible giants
(`7e4e506`), plus collection-aware wild encounters (`9f8b030`). Roster: 247 forms / 142 families.
Save schema advances from v12 to v13 on normal loading/saving; retain the pre-upgrade backup before
any rollback. Do not run an older v12 application against a later v13 save.

## Save preservation

The user confirmed Lucas had saved and the iPad was connected/unlocked. Separate initial, final and
post-install copies of the entire Documents directory are stored under:

`/Users/yuankunxue/Documents/Numeria Save Backups/ipad-before-1.4.0-PRd9wUPJ/`

`final-Documents` and `post-install-Documents` have identical recursive file inventories and all four
files are byte-for-byte identical, verified **before the first launch**. Initial and final backups also match.
Active Slot 2: forest, 3,264 coins, Addmander family stage 1 / Lv.8 / XP 8, 50 caught entries plus starter,
Grade 1 / Random, schema v12. Slot 1 and legacy saves were preserved too.

| File | SHA-256 |
| --- | --- |
| numeria-active-slot.txt | `d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35` |
| numeria-save-slot-1.json | `657d723d8e8475e95b8dcb64fc612ac33f56e4c0e3c078e8ed5271d7691ceef5` |
| numeria-save-slot-2.json | `996f778b3f18cc8b0b0e7608b7d9f7308ca04fb68bfbf837178fddc82b09f406` |
| numeria-save.json | `1be24b0ee0e177fa956606c6dfbb1395fcde1e03964316d8bbf0134dfe9dd7d7` |

## Build and validation

- Feature regression: Unity EditMode 212/212; Node 15/15.
- Unity export succeeded: `unity/Builds/iOS/Numeria-10`.
- Xcode Release arm64 build succeeded; deep/strict code-signature verification passed.
- Signed app: `/tmp/numeria-depth-ios-derived/Build/Products/Release-iphoneos/Numeria.app`.
- Device inventory reports 1.4.0 (6); process launch succeeded after save verification.
- Export log: `/tmp/numeria-depth-ios-export.log`; successful native build log:
  `/tmp/numeria-depth-xcode-release.log`.
- Xcode reused the existing provisioning profile. It expires **September 27, 2026 at 19:12 PDT**
  (`2026-09-28T02:12:33Z`); this installation did not extend its lifetime.
- User confirmed LOAD GAME → SLOT 2 progress, DEX, visuals and sound are all normal on the physical iPad.
  Extended touch/performance and expansion gameplay-balance testing remain outstanding.

The iPad remains authoritative after this installation. Never restore this dated snapshot over newer play.
