# 300-member team — September 28, 2026

## Implemented

- `Progress.TeamCapacity` is now **300**, including the permanent starter. TEAM and full-roster UI use this constant.
- Capturing, replacement, giant-reward deferral and DEX claims share the same capacity rule.
- Existing one-companion-per-evolution-family behavior is unchanged. All 142 current families fit simultaneously;
  300 is a capacity ceiling, not 300 newly created species or duplicate-family slots.
- Save schema stays **v13**. Existing 99-member teams immediately have free space after loading. No migration,
  truncation, progress reset, balance change or level-cap change (still Lv.99).

## Validation

Unity **229/229** EditMode tests and Node **15/15** passed. Coverage includes:

- 300 total members accepted; the next new family rejected; duplicate/upgrade behavior remains separate.
- Full-roster replacement preserves captured stats and releases previous growth/equipment correctly.
- Full-roster giant rewards stay pending until space is available; claims are idempotent.
- All 142 real families fit without releasing anyone.
- Isolated save/load round-trip of 300 members preserves order, levels, active partner, equipment and world progress.
- An old 99-member saved team can catch its 100th partner after loading.
- 30/300-row TEAM menus preserve top/middle/bottom scroll offsets when selecting their final entries;
  all rows render and the header shows the new denominator. Synthetic future families test the 300 boundary.

EditMode coverage is not a physical-iPad performance benchmark; device menu responsiveness and capture acceptance
must still be checked. Actual installed state is tracked in [iPad 1.4.3](ipad-1.4.3.md).

## Safety / rollback

No real player save is modified during development. The iPad is the authoritative source; deployment requires
a fresh device backup, same-bundle in-place installation, and a byte comparison before first launch.
Code can be reverted independently, but an older 99-capacity build would again block captures above its limit.
Never restore an older player save to roll back code.
