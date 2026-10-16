# Halloween implementation — work in progress

2026-10-16. Local 1.5.0 (10), save schema v14. Not yet installed; no real iPad/Mac player slots accessed during development.

## Implemented and checked

- Spooky Pumpkin Patch and Haunted House: separate walkable layouts, five local encounter habitats each, 15 families each, fixed Lv.76–82 wild ranges and Lv.84 Bosses. Both unlock after Veilmoon Lake. WORLD travel and permanent return doors are available.
- 30 new three-stage families, plus two appended forms for each of the 88 depth families, including giants. 172 family DEX entries / 513 form pages. All old IDs and base species statistics retained.
- 266 new portraits, two battle panoramas, terrain materials, independent farm/room props. All 30 seasonal families now have distinct signatures (34 motion kinds including Pipkin/clock stage variants), with 32 generated transparent motifs and 30 individual command icons. Bell/clock effects use short pitched chimes.
- All 88 depth families now have explicit second/third-stage presentation pairs, using 88 family motifs and eight accents from eight ImageGen atlases. These route by species/stage before the old elemental renderer, including the three legacy beam kinds. Base forms and Tackle retain their original renderer; animation remains visual-only with one shared impact clock. Particle textures are bounded to 256px at runtime.
- Six playable, untimed mechanisms: water connectivity, equal sharing, pattern completion, mirrored cells, portrait/candle ordering, audible melody replay. Each has LISTEN, undo, reset and exit. Completion, coins and guardian readiness persist.
- New offline narration bank. Bell clips are owned by the overlay and released on close; LISTEN does not reset partial answers.
- Portrait puzzles use count ordering for K and spoken relational clues for higher grades. Sharing supports tap or drag. Channel complexity varies by difficulty; water inlet/outlet markers and non-overlapping candle graphics improve readability. Six completion shortcuts and restoration lights persist. Tall map props fade when they actually obscure Lucas.
- Grade-specific mechanisms retain the same interaction instead of adding another quiz: K picture patterns and countable sharing; G1–3 whole-number sequences and equal groups; G4 fraction sharing and fraction sequences; G5 mass targets, decimal sequences and coordinate reflections. G1/G2 distractors stay within the selected curriculum cap. All higher-grade instructions and units have offline narration.
- Six restoration sequences show water/root-bridge growth, harvest-cart delivery, sequential orchard lanterns, mirror guests exiting, portraits greeting while a shelf slides, and music-door bells lighting. Camera briefly focuses on the unlocked shortcut. Completion is saved before animation; re-entering reconstructs the completed pose rather than replaying or losing progress.
- Append-only v14 migration; no automatic evolution. Existing captured offsets, history and original 88 base curves unchanged; existing 159 Mega forms unchanged.
- Music reuses the locally licensed jukebox with crossfaded exploration/battle/Boss tracks and narration ducking. It is not stem-based layering; no claim of new adaptive stems is made.

## Requirement-to-evidence audit

| Requirement | Implementation and verification | Remaining acceptance |
| --- | --- | --- |
| Two distinct themed worlds, late-game peers, permanent way home | `HalloweenWorlds` / `HalloweenScene`; `NewWorldsUnlockTogetherAndEveryObjectiveChestHabitatAndExitIsReachable` checks both layouts and each completion stage | Walk both worlds on iPad |
| 15 pumpkin + 15 ghost families, three stages each | `ExpansionCatalog`, all-family evolution/resource tests; existing `CollectionDepthTests` verifies 172 family entries / 513 form pages | Inspect TEAM/DEX at device scale |
| Existing non-evolving families receive three stages | 88 original IDs retained, 176 appended forms; exact legacy curves and captured offsets tested, including Lv.99 voluntary evolution | Load Lucas's latest v13 slot after backup |
| Comparable enemy level, trainable Boss challenge | Wild 76–82, fixed Boss 84; seeded ecology checks and 11,520 actual battle simulations across families/gear/levels; base DEF revised after simulation | Play one ordinary and one Boss battle per world |
| Distinct art and skills, no placeholder release | 266 imported portraits, two panoramas, per-world props; all 30 seasonal family signatures and all 176 depth evolution effects tested and rendered | Observe frame pacing and small-icon readability |
| Six meaningful, grade-aware mechanisms with replay | `HalloweenMechanism` / `MapHalloweenUi`; solvability, range, replay state, offline resource checks and 108 grade previews | Touch/drag, LISTEN and melody audio on device |
| Progress/collection/equipment preservation | Append-only v14; migration/300-team/equipment/DEX regression suite; old species curves and Mega roster unchanged | Fresh device backup, byte-for-byte prelaunch comparison, user load confirmation |
| Commit, push and safe iPad delivery | Unity export is ready; final native signing build and Git delivery tracked below | Do not install without confirmed saved state and verified backup |

Editor snapshots and synthetic tests do not prove device audio, touch, memory or performance. These remain release-acceptance gates, not implicit passes.

## Evidence as of this checkpoint

- Unity `/tmp/numeria-halloween-full-v12.xml`: **248/248 passed**, including all-family evolution, full-curve stat ratios, v13 preservation, puzzle solvability, map connectivity, resource coverage, replay controls, all 90 seasonal forms' effect cleanup in both directions, all new-family wild matchups, 11,520 Boss simulations, Defend damage, occlusion and all-grade offline narration checks. Covers all 176 depth evolutions in both directions, powered/unpowered, 41 sampled frames, single impact/cleanup/input checks, unchanged base/Tackle routing, memory bounds, and six restoration/re-entry pose checks. Grade-specific concepts, targets, unique choices and numeric caps checked over six grades, four modes and 100 seeds.
- `npm test`: **15/15 passed**.
- `node tools/validate-halloween-design.mjs`: catalog consistency passed (not a runtime test).
- Actual Unity previews: `/tmp/numeria-training-preview/halloween-*`. Reviewed all added portrait pages; corrected cell-boundary clipping with whole-silhouette slicing. Bellbit regenerated as a bell, not a pocket watch.
- `/tmp/numeria-world-presentation/seasonal-*`: actual gameplay-scale exploration and all 34 seasonal skill timelines. Corrected a separate spell-atlas slicing bug: multi-contour rings must stay in their grid cell, not be ranked as independent objects. Latest puzzle and known/unknown DEX pages reviewed at 4:3/16:9. Initialize TMP with the bundled font before enabling the component, eliminating absent-default-font warning floods.
- Unity iOS export succeeded: `unity/Builds/iOS/Numeria-14`, log `/tmp/numeria-halloween-ios-build.log`.
  Unsigned Xcode Release compile **succeeded** (`/tmp/numeria-halloween-xcode.log`). This checkpoint build predates subsequent puzzle, skill and restoration refinements;
  regenerate before release. No signing, installation or device validation performed.
- `/tmp/numeria-depth-restoration-preview-v2.log` completed successfully: 528 actual runtime depth frames (176 forms, charge/travel/impact), eight family/stage contact sheets, and 18 restoration frames. Contact sheets reviewed; corrected bell sound-ring imagery and crater flame-petal expansion. `WorldPresentationPreview.EvolutionAndRestoration` reproduces these without player slots.
- Earlier preview compile failed on deprecated `GetInstanceID` in the new test only; replaced that test's dictionary key with the renderer reference. Subsequent preview and full test runs completed successfully.
- `/tmp/numeria-halloween-grade-preview.log`: 108 actual UI previews spanning six grades, three explicit modes, sharing/pattern/mirror and both screen ratios. Reviewed G4 fraction baskets/patterns and G5 mass/coordinate views. Reproduce with `HalloweenPreview.GradeMechanisms`; preview data are synthetic, not player saves. The final bounded-distractor correction is covered by full-v12.
- Final Unity iOS export: `unity/Builds/iOS/Numeria-15`, `/tmp/numeria-halloween-final-ios-export.log`, succeeded. Signed Xcode Release compilation succeeded (`/tmp/numeria-halloween-final-xcode.log`, exit 0). Strict/deep codesign verification passed; app/profile/entitlements retain `com.yuankunxue.numeria` and team `7B74WP6AR2`, version 1.5.0 (10). Xcode reused the valid profile expiring October 19, 2026 at 18:23 PDT; this build does not extend that date. See [iPad release checklist](ipad-1.5.0.md). Not installed yet.

## Balance adjustment

Initial DEF growth of 4–5 produced very long defensive mirror matches under the existing flat subtraction damage rule.
Only the new 30 families were tuned: DEF grows by 3 per ten levels, defensive families retain higher HP and base DEF.
At Lv.80, base seasonal forms have HP 110–137, ATK 44–54 and DEF 27–29. This supersedes the preliminary DEF target
in the design document. At equal levels, the tested new-family ordinary matchups need about 2–10 successful formula
hits depending on evolution/training; Boss simulations use shield breaking, actual gems and enemy turns, not raw HP division.

Depth evolution percentages now apply to the entire original species-at-level curve, rounded once (minimum +1), not just Lv.1 base stats.
Normal HP 112/125%, ATK/DEF 110/120%; giant HP 106/112%, ATK/DEF 104/108%. These are relative to stage zero,
not compounded. Captured offsets, training and gear are added afterward once. Integer growth-only fitting could not retain the
intended small increases consistently across Lv.12–99; explicit species-only percentages supersede that preliminary implementation detail.

## Open acceptance items — do not label the expansion finished yet

- Final audio listening and device-scale interaction checks remain. On-device frame pacing, readability and touch feedback are not proven by Editor render snapshots.
- Commit/push the complete feature and keep this release record synchronized with delivery evidence.
- Device performance and touch/audio validation require the iPad. Before any update, take a fresh backup of its latest saves, verify the backup, and update the same bundle/team in place. Never uninstall or restore an old Mac snapshot.

Asset prompts/provenance: `halloween-production-assets.json`, `halloween-atlas-assets.json`,
`halloween-bellbit-asset.json`, `halloween-environment-detail-assets.json`, `halloween-spell-assets.json`, `depth-spell-assets.json`. All raster art was generated with the built-in image tool;
production copies live under `unity/Assets/Resources/generated/`, master atlases under `art/halloween-atlases/`.
