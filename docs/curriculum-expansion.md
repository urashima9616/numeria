# Grade-specific puzzle expansion — 2026-09-22

Release target: **1.3.0 (4)**. Save format remains **v12**; no progress or learning-setting migration.
Also includes the separately committed TEAM scroll-position fix (`4b40c10`).

## Actual coverage

The mixed pool now has explicit per-grade inventories. Counts below are task families, not a claim
of that many independent standards or complete curricula. Earlier skills remain as review where useful.

| Grade | Mixed families | Distinct practice |
|---|---:|---|
| K | 17 | Within-10 arithmetic including zero, unknown parts, comparison, numeric patterns, shapes and sides, teen ten-frames, lengths, picture data, two-tile number bonds, balanced equations, ordered number paths, repeating shapes, mirror completion, object counting, classification counts, composed shapes, join/take-away/missing-part stories |
| 1 | 19 | Within-20 operations and the concrete K tasks; tens/ones, whole/half-hour clocks, halves/quarters, length differences and three-category data |
| 2 | 18 | Within-100 operations, hundreds, shape properties, five-minute clocks, halves/thirds/quarters, equal-row arrays and row counts, nickel/dime totals and change; classification/counting/composition families give way to these tasks |
| 3 | 20 | Within-1,000 operations, multiplication/division facts, equal sharing and grouping, two-step stories, area/perimeter/missing dimensions, minute clocks and earlier/later time, fraction equivalence/comparison/number lines, rounding and scaled bar data |
| 4 | 22 | Multi-digit arithmetic including two-digit multiplication and one-digit-divisor division; grouping with leftovers, like-denominator fraction addition/subtraction and fraction × whole number, unit conversions, factors/multiples, expanded place-value forms |
| 5 | 24 | Two-digit divisors, decimal arithmetic and place value, decimal multiplication/division, unlike-denominator fraction addition/subtraction, fraction multiplication and limited unit-fraction division, volume/missing dimensions, first-quadrant coordinates and moves |

Doubling, subtraction and chain addition are three additional **theme-only** tasks, not extra entries
padding the mixed-pool counts. Their skill routes retain the requested operation. The existing 25% mixed
skill-puzzle chance is retained; themed questions outside that chance still practice their own concepts.

## Interaction and selection

- Number bonds require selecting two different tile identities, then CHECK. Every mathematically valid
  pair is accepted, including equal-valued tiles when two copies exist. CLEAR allows correcting a selection.
- Number paths require tapping all four stones in order and CHECK; it is no longer a single missing-number choice.
- Balance displays an actual two-sided equation and scale, rather than routing to generic missing numbers.
- Visual patterns and mirror completion remain visual in every grade. Shape answer buttons include pictures,
  not just English words. Shape composition is a pictured join-and-identify task, **not a freeform drag puzzle**.
- Graphs and counting diagrams do not print or narrate the values being tested. Clock faces now have all
  60 minute ticks. Squares and composed square pieces use equal side lengths; rectangle diagrams use their dimensions.
- The per-adventure, in-memory shuffle bag covers every mixed family before refilling. It avoids the last
  three families when alternatives remain, retries recent exact questions and tries another template of the
  same family. Forced skill families may repeat intentionally. History survives battle/menu changes while
  the same learning settings object is in use, but is not persisted across app launches.

## Grade, mode and chapter bounds

The original grade/mode arithmetic limits and chapter 1–8 linear interpolation remain. Other dimensions
(array factors, sequence length, elapsed-time bounds, denominator pool and unit-conversion values) use
their own grade-appropriate bounded ranges. Discrete skills naturally progress in steps, not continuously.

Feel Good keeps smaller ranges and immediate hints. Normal emphasizes direct tasks. Challenging favors
unknown parts, reverse order, earlier time, inverse measurements, multi-step stories and more complex
fraction operations. Random resolves **one mode per question**, even when duplicate avoidance retries it.
It cannot randomly choose another grade. A later chapter does not make every sampled question harder.

Grade 1 clock answers and distractors are whole hours or half hours; Grade 2 uses five-minute intervals.
Early fraction choices stay within one whole, and Grade 3 fraction denominators are restricted to
2, 3, 4, 6 and 8. Fraction answers are reduced, with mathematically equivalent distractors excluded.

Reference guidance: [K number/shape foundations](https://www.thecorestandards.org/Math/Content/K/introduction/),
[Grade 1](https://www.thecorestandards.org/Math/Content/1/introduction/),
[Grade 2](https://www.thecorestandards.org/Math/Content/2/introduction/),
[Grade 3 fractions](https://www.thecorestandards.org/Math/Content/3/NF/),
[Grade 4 whole-number operations](https://www.thecorestandards.org/Math/Content/4/NBT/),
[Grade 5](https://www.thecorestandards.org/Math/Content/5/introduction/).
This is a curated practice game, **not complete standards coverage or a certified curriculum**.
Angles, line plots, fully manipulative geometry, adaptive mastery scheduling and broad story banks remain future work.

## Validation and assets

- **188/188 Unity EditMode**, **15/15 Node** tests passed. New tests include 154,560 generated expanded
  cases across six grades, four modes and eight chapters; they verify pool boundaries, solvable pair/order
  tasks, fraction equivalence, independently evaluated equations, clock choices and no-repeat scheduling.
- UI tests check two-tile selection/reset, non-leaking graph labels/narration, visible diagram meshes and
  resource/audio availability. Tests use temporary saves, not Lucas's data.
- Generated 120 grade/family 4:3 previews plus 41 K/Grade-5 widescreen previews; representative interactions,
  shape choices, story text, fractions, clocks and coordinate diagrams visually inspected.
- Exact teaching diagrams are native UI geometry; no AI art is used to represent numerical facts.
  Existing art remains unchanged. Offline Samantha narration is generated from the finite manifest in
  `tools/learning-voice-lines.txt`; spoken numbers/operators are composed from reusable tokens.
- Full physical-device touch/performance/audio acceptance is pending after installation.

## Deployment safety

User authorized installation after development and confirmed Lucas had saved. Initial device Documents
backup is `~/Documents/Numeria Save Backups/ipad-before-1.3.0-yMjQUYkJ/initial-Documents`.
Active Slot 2: schema v12, Silent Peaks, Paircub family Lv.8, 2,564 coins, 50 caught identities,
58 opened chests, six crystals, Grade 1 / Challenging. This supersedes the older Lv.7 snapshot.
Initial Slot 2 SHA-256: `3dcc9346dbbddd338d23e0bebd9c4abbd3783ff9295acfc1eb7e9df342664fcf`.

Installation still pending: take a final device backup, use the same bundle ID and signing team for an
in-place update, and compare every Documents file before first launch. Never uninstall or import old Mac saves.
