# Collection-aware wild encounters

Original form-based rule installed in **1.4.0 (6)** on 2026-09-25; see [deployment verification](ipad-1.4.0.md).
The family-based correction below is local, **not yet installed**; see [family DEX](family-dex.md).

## Rule

All 16 playable maps use the permanent DEX collection record to favor locally available,
not-yet-collected **evolution families**, not individual forms or current party membership.
Any recorded form marks its entire family collected for encounter-assistance purposes.
For catchable wild encounters, mix:

- 60%: draw from positive-weight entries belonging to uncollected families in this map's existing table.
- 40%: draw from the full original positive-weight table.

Both draws retain the authored relative form rarity. The implementation combines these pools into
one weighted draw. If `T` is total original weight and `U` is the weight of missing families' entries,
an entry in a missing family of weight `w` receives effective weight `w × (0.4 + 0.6 × T/U)`; a known-family entry gets
`w × 0.4`. Total weight remains `T`.

| Original combined probability of missing families | Assisted probability |
| --- | --- |
| 50% | 80% |
| 20% | 68% |
| 10% | 64% |
| 1% | 60.4% |

If all local families are collected, or all are missing, retain the original distribution.
Capture/evolution updates apply on the next encounter. Releasing a partner never renews its
bonus. Merely seeing or defeating a missing family's form does not remove assistance. This is a
probability improvement, not a guaranteed encounter after a fixed number of battles.

## Boundaries

- Only existing positive-weight wild entries are eligible: no off-map species, disabled
  entries, giant guardians or later-world unlocks are added to the pool.
- Master classification happens first using original ecology and the existing 30% base
  chance / fifth-encounter drought protection. Master partners remain uncatchable and
  receive no collection-based selection boost. Only ordinary wilds are redrawn with assistance.
- Bush encounter frequency, level ranges, stat variation, catch success, Boss/Merchant
  encounters and fixed-level giant challenges are unchanged.
- No new save fields or schema migration: this uses the existing v13 `CollectedForms`.
  Old-save history limitations still apply: a previously released form absent from historical
  data cannot establish collection unless another form in the same family is recorded.
- Feature development did not access real player saves. Subsequent authorized installation backed up
  device saves and verified them byte-for-byte before first launch; see the deployment record above.

## Verification

Unity EditMode **215/215**, Node prototype **15/15** passed on 2026-09-25 (including family correction).

`CollectionEncounterTests` covers exact weights, empty/complete collections, immediate
capture and release behavior, family identity across forms, relative rarity and disabled entries,
all sixteen maps' ecology/levels/Bosses, and the actual runtime wild/Master dispatch path
over 8,000 seeded encounters. Existing save and progression tests remain in the full suite.

Rollback this update's commit to restore authored encounter probabilities; this update
does not add or remove save data. The earlier v13 expansion remains installed in the codebase.
