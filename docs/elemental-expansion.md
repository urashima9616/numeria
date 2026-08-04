# Elemental Roster and Fever Desert Expansion

This document records the production rules for the four-element roster expansion and chapter four. Runtime
balance remains authoritative in `GameData.cs`, `Progress.cs`, and `MapDefs.cs`.

## Catch and duplicate rules

- Every successful catch preserves the wild Mathmon's level and current evolution stage.
- Owning any form in a family means later catches are compared against that family's saved growth.
- A higher level or later evolution stage is a **stronger catch**. The player chooses either:
  - **Keep stronger friend:** adopt its higher level/form, reset progress within that level, and preserve the
    family's permanent training bonuses and equipped accessories.
  - **Turn into +XP:** keep the current companion and award 125% of normal catch XP.
- A weaker or equal duplicate automatically becomes normal catch XP.
- When the 15-family team is full, replacing a companion also preserves the newcomer's wild level and form.
- Addmander's family remains protected from release.

## New three-stage families

| Element | Stage 1 | Stage 2 | Stage 3 | Math theme |
|---|---|---|---|---|
| Fairy | Glimlet | Twinkelle | Luminara | Number bonds |
| Fairy | Moonmote | Lunafae | Selenequin | Moon phases |
| Fairy | Charmite | Pairabelle | Harmonique | Matching and equality |
| Fairy | Wishwink | Starwhisp | Constellara | Constellation counting |
| Fairy | Pixipip | Prismfae | Radianta | Prism symmetry |
| Dragon | Addling | Sumscale | Totalisk | Addition |
| Dragon | Dracount | Tallywyrm | Enumeragon | Tallies and counting |
| Dragon | Loopling | Spirake | Rotaragon | Spirals and rotation |
| Dragon | Twinsting | Doublescale | Multisaur | Doubling |
| Dragon | Shardrake | Prismwyrm | Geodragon | Geometry |
| Electric | Voltlet | Sumvolt | Totalstorm | Addition |
| Electric | Sparkit | Patternzap | Sequencera | Sequences |
| Electric | Chargecub | Doublebolt | Thunderbear | Doubling |
| Electric | Flickerfin | Neonray | Luminamp | Luminous geometry |
| Electric | Switchick | Mirrorvolt | Voltalance | Mirror symmetry |
| Grass | Budsum | Vineplus | Totalbloom | Addition |
| Grass | Clovercub | Fourleaf | Cloverlord | Four-leaf counting |
| Grass | Sprouturn | Spiralfern | Symmetroak | Natural symmetry |
| Grass | Mossbit | Doublmoss | Grovemult | Doubling |
| Grass | Seedseq | Patternpod | Orderchid | Ordered patterns |

Each elemental group has a dedicated generated skill icon and presentation language: Fairy Glimmer, Dragon
Spiral, Electric Bolt, and Grass Bloom. The complete project roster is now 90 forms across 31 families.

## Chapter four: Fever Desert

- Number range: 0–40, with mixed arithmetic, sequence, geometry, symmetry, and rotation challenges.
- Ecosystem: evolved forms from all 20 new families.
- Merchant: Nia, partnered with Mirrorvolt.
- Guardian: Sage Solara.
- Boss: Solar Totalisk, minimum level 32, with tier-scaled HP and defense.
- Rewards: five chests, four discovery runes, desert merchant stock, and the fourth Digit Crystal.
- Progression: Azure Sky City's cleared gate unlocks Fever Desert; all four crystals wake the gate home.
- Presentation: generated desert battlefield, Nia and Solara portraits, dedicated Jukebox desert music, and fully
  offline narration.
