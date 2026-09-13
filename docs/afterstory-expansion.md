# Ancient Ruins and Veilmoon Lake

Local expansion, September 2026. The physical iPad still has Lucas's authoritative progress; this work has not read, replaced or reset it.

![New exploration regions, runtime 4:3 samples](images/afterstory-worlds.png)

![Veilmoon Lake Boss battle, runtime 4:3 preview](images/afterstory-lake-battle.png)

## Progression

| Chapter | Unlock | Wild levels | Fixed Boss | HP / ATK / DEF |
|---|---|---|---|---|
| Ancient Ruins | Clear Underground Tunnels | 50–58 | Grand Abacoracle, Lv.58 | 201 / 31 / 22 |
| Veilmoon Lake (幽冥湖畔) | Clear Ancient Ruins | 58–66 | Lady Moonveil, Lv.66 | 206 / 39 / 21 |

Use WORLD or SETTINGS travel to enter the ruins after the original ending. The six-crystal ending and original underground exit are unchanged. Expansion victories award Mosaic and Moonlight badges, derived from cleared-gate IDs, not seventh/eighth Digit Crystals. New bosses never follow the active companion's level. The existing Boss stat formula caps its chapter modifier at six; higher-level species provide the afterstory's progression.

Each 32×18 map has four discoveries, five optional chests, a merchant challenge/shop, weighted encounters, a guardian and a portal trial. Three discoveries restore crossings and unlock the guardian. Ruins use shapes, gear patterns and equality balance; the lake uses number bonds, symmetry and lotus patterns. These reuse the existing child-readable puzzle interfaces, not new freeform construction minigames. Arithmetic remains within 20. Solved passages are reconstructed from discovery IDs on reload.

## New families

| Family and evolution | Element | Evolution levels | Skill choreography |
|---|---|---|---|
| Tiletot → Mosaicub → Tessellion | Rock | 52 / 60 | Mosaic March: tiles assemble and a mosaic lion charges |
| Abacub → Beadwarden → Abacoracle | Mind | 52 / 60 | Balance Beam: paired beads settle and converge into light |
| Coglet → Gearling → Clockondor | Sky | 52 / 60 | Pattern Flight: a clockwork bird follows illuminated nodes |
| Gliminnow → Luminfin → Lanternray | Water | 60 / 68 | Lantern Current: a ray follows an arcing lantern trail |
| Wisplet → Veilkit → Moonveil | Fairy | 60 / 68 | Mirror Waltz: two mirrored fox silhouettes converge |
| Lilypadlet → Bloomboat → Lotustide | Grass | 60 / 68 | Lotus Sequence: blooms open along the attack path |

The roster is now 159 forms / 54 families, with normal and redesigned Mega art for all 18 new forms. New entries are appended, preserving old roster/family indices. Each family has one new visual kind; stages and Mega share its choreography. Existing skill button icons are reused. Bosses use the existing pattern shield/stun/Mega-capable battle loop, not bespoke multi-phase AI.

ImageGen produced the two battle paintings, two ground textures, two environment atlases and twelve creature sheets. Mechanical atlas extraction produces 36 individual creature PNGs. See [asset prompts and provenance](afterstory-assets.md). The two regions reuse licensed Sky/Mountains Jukebox moods with existing crossfade/ducking, rather than introducing unlicensed or newly generated music. New dialogue, encounters and evolution narration are baked offline by `NUMERIA_AFTERSTORY_ONLY=1 zsh tools/bake-voice.sh`.

## Save and deployment contract

- Schema v11 adds identities, not destructive field migration. Regression tests upgrade synthetic v10 progress and compare every serialized field except the version; capture offsets, inventory, chest/discovery IDs and completed story remain intact.
- Never use the Mac save to replace Lucas's newer iPad progress. No New Game, reset, uninstall or bundle-ID change is needed.
- Before installation, Lucas must save and connect/unlock the iPad. Copy its current Documents to a fresh external backup and verify it before a same-ID in-place update (`com.yuankunxue.numeria`). Inspect actual device logs, load the latest slot, and verify team, level, coins, chest state and audio.
- Code can be reverted with `git revert`, but an older v10 binary intentionally rejects v11 saves. Do not downgrade over newer play. Keep a pre-update v10 backup for deliberate rollback, or ship a forward-compatible fix. Restoring the old backup would lose subsequent play and requires an explicit user choice.

## Verification and remaining acceptance

Unity exported version **1.1.0 (2)** to local ignored `unity/Builds/iOS/Numeria-6/Unity-iPhone.xcodeproj`.
No older build folder was overwritten. Signing and device installation remain separate from this export.
Xcode Release arm64 compilation/linking succeeded with `CODE_SIGNING_ALLOWED=NO`; the unsigned local app is
`/tmp/numeria-afterstory-ios-derived/Build/Products/Release-iphoneos/Numeria.app`. Its plist confirms version
1.1.0 and bundle ID `com.yuankunxue.numeria`. Use `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer`
when invoking xcodebuild on this Mac; the system default points to CommandLineTools. Do not change that global setting.

Unity EditMode 163/163 and Node prototype 15/15 passed. Chapter balance simulation now runs 8,192 synthetic battles across eight chapters, four existing families and four level offsets. All +10-level cases win under the tested policy (correct answers, appropriate evolution and Mega; no potions/equipment). This is not a claim about every species or a child's success rate.

`Numeria → Preview Afterstory` renders both maps, Boss battles, all six new spell kinds and the eight-region atlas at 4:3 using synthetic progress only. A sprite slicing defect involving overlapping bounding rectangles was found and fixed during visual inspection. The physical iPad remains pending: touch targets, performance, music, the real saved slot and a full playthrough must still be checked after connection.
