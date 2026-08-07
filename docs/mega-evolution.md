# Mega Evolution design

Mega Evolution is a temporary battle form available to every registered Mathmon. It is deliberately powered by
the existing Gem economy so a child can see and plan the whole transformation window without a second resource.

## Activation

- Battle-only; it is never stored in `Progress` and always ends with the battle.
- Requires at least **7 Gems** (`Gems > 6`) and a solved tier-appropriate math puzzle.
- A wrong answer has zero punishment: no Gem, HP, or turn is lost, and the player may try again.
- Successful activation does not consume the player's action. The player immediately chooses a normal skill,
  item, catch attempt, shield action, or the new Mega Nova skill.
- After Mega ends, normal +2-Gem turns resume. Reaching 7 Gems again permits another activation in the same battle.

## Form and stats

`MegaSystem` derives a stable profile from the current species ID, covering all 141 existing forms and future
roster additions without per-form wiring:

- base HP, ATK, and DEF receive a deterministic **25–35%** boost (rounded upward for integer combat stats);
- accessory bonuses are added after the base-stat multiplier;
- current HP gains only the temporary difference between normal and Mega max HP, and is capped back to normal max
  when Mega ends;
- the species' existing elemental/mathematical visual language determines its aura and Nova attack;
- a stable three-way appearance variant changes the number of energy-wing shards and crown/crest pieces.

The presentation keeps the original sprite recognizable, then adds a larger colored silhouette, twelve rotating
aura rays, animated elemental wings and crown shards, a scale/pulse change, and full-screen activation/reversion
beats. This works with every current battle sprite rather than requiring 141 separately painted transformation PNGs.

## Skills and Gem lifecycle

- Every profile receives a unique `<Species> Nova` skill with zero cost and power at least five points above that
  form's strongest regular skill.
- While Mega is active, **all skills cost zero Gems**, including the regular math skill.
- One Gem is consumed after every completed player action. Activation and cancelling a menu are not actions.
- Mega turns do not receive the normal automatic +2 Gems.
- Gem Snacks are disabled and visibly marked `SEALED DURING MEGA`; HP Potions remain legal.
- At zero Gems the form reverts before the enemy completes the turn, boosted stats are removed, and the original
  sprite/status presentation returns.

## Verified invariants

EditMode tests cover the exact 7-Gem gate, solved-puzzle gate, deterministic 25–35% range, free regular and Nova
skills, refill blocking, one-Gem drain, zero-Gem reversion, repeat activation, all 141 registered forms, and the
runtime Mega UI hierarchy (outline, twelve-ray aura, wings, crests, button states, and normal-form restoration).
