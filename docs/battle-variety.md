# Battle question variety and Master encounters — 2026-09-22

Local follow-up to installed 1.3.0 (4). Release **1.3.1 (5)** is exported to `unity/Builds/iOS/Numeria-9`;
signed Xcode Release build and strict deep signature verification passed, **not yet installed on iPad**. Save schema stays v12;
no real save was loaded, modified or imported during development.

## Changes

- With learning settings enabled, every normal skill asks exactly one question from the grade's shared
  shuffle bag. Removed the 75% forced skill-family / 25% mixed split. Choreography still follows the
  Mathmon's move; selected grade, mode and chapter still bound the question. Theme-only generator APIs
  remain for compatibility and explicit practice, but do not override the battle pool.
- The same settings object retains question history across battles/partners. Mixed questions exclude
  the last three families, even when a forced exploration task leaves only recent entries in the bag:
  those entries are deferred while another eligible family is asked. Without forced interruptions the
  bag covers every family before repeating. History is in memory, not persisted across app restarts.
- Balance questions use positive parts by default. Normal and Feel Good deliberately allocate 8% of
  raw draws to zero-identity review; retry/deduplication can slightly alter the experienced proportion.
  Challenging uses no zero operands or zero answers. Six distinct equation templates replace the two
  nominal Challenging variants that previously produced the same subtraction template. Unknowns can
  be addends, minuends or subtrahends; all numbers/results remain inside the grade/mode/chapter cap.
  Identical or swapped decompositions are avoided where the number range permits. Distractor choices
  may still include zero; these are not operands or correct answers in Challenging.
- Master base probability is 30% per eligible random grass encounter (formerly 20%). Four consecutive
  ordinary encounters force the next encounter to be a Master. A Master resets the counter; Bosses,
  merchants and other scripted/non-catchable opponents do not consume/reset it. The counter is keyed
  to the adventure Progress object, so it survives map changes but resets on loading/restarting.
  Long-run Master share with this guarantee is approximately 36.1%, not a flat 30%. Grass encounter
  probability remains 35% per entered grass tile. The +20%–40% stats and no-capture rule are unchanged.
- Normal skill activation now rejects a queued second activation while battle actions are disabled.
  No definite cause for the user's report of two different questions on one cast was reproduced.
  Dispatch tests confirm each skill starts one question, not two; wrong-answer retry intentionally
  stays on the same question. Physical-device reproduction is still needed if it recurs.

## Verification and release safety

- Unity EditMode **193/193**, Node **15/15** passed.
- Added 144,000 balance samples across six grades, three resolved modes and eight chapters; checked
  independently evaluated equations, positive/bounded parts, template variety and controlled zero rates.
- Tests cover every skill visual's single-question dispatch, shared grade-pool coverage across newly
  constructed battle puzzle instances, busy-action rejection, forced/mixed recent-family exclusions,
  30% base encounter sampling and 10,000 encounter simulations verifying the four-ordinary maximum.
- Regenerated curriculum previews and visually checked Grade 1's mixed-operation balance layout.
  The offline narration manifest is unchanged; existing voice tokens cover the new equations.
- Does not change progress, learning preferences, monster stats, saved inventory or save schema.
- The currently installed iPad remains 1.3.0 (4); these changes require a future save-backed-up in-place
  update. Do not restore September 22's backup over any progress Lucas makes afterward.

The user has now authorized installation. The device is connected; final backup and installation wait
for confirmation that the latest progress has been saved and play has paused. Never uninstall the app.
The ready app is `/tmp/numeria-variety-ios-derived/Build/Products/Release-iphoneos/Numeria.app`;
bundle ID/team remain `com.yuankunxue.numeria` / `7B74WP6AR2`. Profile expires September 27 at 19:12 PDT.
Initial backup: `~/Documents/Numeria Save Backups/ipad-before-1.3.1-N1VmyQEv/initial-Documents`.
Slot 2 now has Silent Peaks, Paircub family Lv.10, 2,614 coins and Grade 1 / Random (v12).
This is newer than the 1.3.0 deployment backup. Take another fresh final backup after user confirmation,
install in place, copy Documents again and compare all files before launching. No installation has occurred yet.

Revert this feature commit to restore the old local behavior. This is a source-code rollback, not a
device installation or a save restoration; keep the latest iPad save authoritative.
