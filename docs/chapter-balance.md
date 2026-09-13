# Story encounter balance

Updated 2026-09-12 (Pacific time). Applies to the Unity game, not the historical Web prototype.

## Training must move the player forward

Previously wild levels followed the active partner, story Boss level was
`max(BossMinLevel, playerLevel + tier + 1)`, and merchants followed `playerLevel + 1`.
Returning after training therefore raised the opponent too. Story encounters now use
authored chapter levels, independent of the active partner, party size, or strongest teammate.

| Chapter | Wild levels (overall) | Merchant level | Boss level | Boss HP | ATK | DEF |
|---|---|---|---|---|---|---|
| Mystic Forest | 1–6 | 4 | 5 | 24 | 4 | 2 |
| Silent Peaks | 7–14 | 11 | 12 | 39 | 6 | 4 |
| Azure Sky City | 14–22 | 19 | 20 | 56 | 9 | 7 |
| Fever Desert | 24–33 | 29 | 32 | 115 | 21 | 12 |
| Dark Mines | 34–43 | 40 | 42 | 132 | 29 | 15 |
| Underground Tunnels | 41–51 | 47 | 50 | 190 | 37 | 23 |

Each wild species has its own narrower minimum/maximum inside the chapter cap;
species selection weights remain unchanged. Wild HP still varies from 92% to 108%.
Merchant level is fixed, while its existing small HP variation remains. Story Boss
HP/ATK/DEF are deterministic, including repeated attempts after a defeat.
Existing `playerLevel` arguments remain for caller compatibility, but no longer scale encounters.

## Boss-specific tuning

Fixing levels alone left high-defense late Bosses excessively hard for starter families.
Because shields already halve ordinary hits, stacking the old health multiplier and
defense bonus created lengthy minimum-damage fights. For chapter `c` clamped to 1–6,
starting from that species' stats at the fixed Boss level:

- HP = max(base HP + 8, round(base HP × (1.40 + 0.05 × c))).
- ATK = max(1, round-half-up(base ATK × 0.65)) + max(1, floor(c / 2)).
- DEF = max(1, round-half-up(base DEF × 0.50)).

HP uses the existing C# `Math.Round` convention. Damage variance, shield puzzles,
break stun and 2× next hit, Mega, rewards and player/captured stat growth are unchanged.
This is story-mode tuning; an optional scaling/rematch mode is **not implemented**.

## Verification and limitations

`ChapterBalanceTests` verifies fixed Boss/merchant stats with active levels from 1 to 99,
all chapter/species wild ranges and variation, and improved damage after ten levels of
training against the same Boss. Core tests retain shield, damage and HP invariants.

The simulation runs 6,144 synthetic fights: six chapters × four families (Addmander,
Tenfin, Shapling, Runelet) × four level offsets (−5, 0, +5, +10) × 64 seeds.
The policy solves every shield, banks gems using basic attacks and activates Mega/Nova
when available. It assumes level-eligible evolutions have been completed, uses no items,
equipment or captured stat offsets, and caps a fight at 160 player actions.

At Boss level +10 all four tested families win all 64 seeds in each chapter. Against
the final Lv.50 Boss, Addmander's evolved line wins 0/64 at Lv.50, 56/64 at Lv.55,
and 64/64 at Lv.60. Runelet's evolved line is substantially stronger. These are
regression-policy results, **not predictions of Lucas's win rate or proof that all
48 families are equally balanced**. Manual playtesting should next evaluate mistake
tolerance, starter-to-rare-species gaps, and chapter XP pacing.

## Save safety and release

Save schema remains v10. No saved creature levels/stats, coins, inventory, crystals,
opened chests or chapter unlocks are recalculated. New encounters use the new rules.
No real save was read or written by the balance tests, and this change does not install
anything on the iPad. Lucas's newest progress is now on the iPad: back it up before a
future update, preserve the same bundle ID, and never restore the stale Mac save over it.

Rollback: revert the balance commit. No save migration or save rollback is required.
