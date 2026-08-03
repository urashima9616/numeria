# Generated Visual Assets

Numeria's family skill icons and Lucas explorer sprite were generated with the built-in ImageGen workflow,
then converted from a flat magenta chroma-key background to alpha PNGs with the imagegen skill's local
`remove_chroma_key.py` helper. The final project assets live under:

- `unity/Assets/Resources/generated/Skills/`
- `unity/Assets/Resources/generated/Heroes/lucas_explorer.png`

## Shared skill-icon prompt

Each icon used this production prompt, with the subject brief replaced by the table below:

> Create one original Numeria game UI skill icon. Match the Equation Flame style anchor's crisp
> hand-authored 16-bit pixel scale, dark forest-green outline, compact silhouette, limited palette,
> chunky pixels and clean highlights. Center exactly one icon with generous padding, readable at
> 76×76 pixels. Use a perfectly flat solid `#FF00FF` chroma-key background with no shadows,
> gradients, texture, floor or reflection; do not use the key color in the subject. No frame,
> watermark, logos or trademarks; no sprite sheet.

| Family | Resource | Final subject brief |
|---|---|---|
| Addmander | `equation_flame` | Orange-red magical flame containing `2 + 3` number tiles |
| Tenfin | `make_ten_wave` | Turquoise wave wrapping `6` and `4` number tiles |
| Shapling | `pattern_leaf` | Three leaves in a repeating pattern joined by a golden path |
| Countipillar | `count_crunch` | Friendly bug mandibles around three counting beads |
| Doublit | `double_boulder` | Two matching boulders colliding around a `×2` spark |
| Mirrowl | `symmetry_beam` | Mirrored owl wings around a vertical cyan beam |
| Paircub | `matching_paws` | Two identical bear paws linked by an equals sign |
| Subunny | `subtraction_dash` | Golden rabbit dash crossing a subtraction sign |
| Pebblit | `tally_stone` | Ascending stone stack carved with glowing tally marks |
| Prismouse | `geometry_prism` | Cyan prism casting circle, triangle and square light shapes |
| Seqkit | `sequence_spark` | Four magical seeds increasing in size along a step path |

## Lucas explorer prompt

The user-provided photo was used only as an identity, age, proportion, hair, outfit, backpack and footwear
reference. Existing Addmander artwork was used only as a rendering-style reference.

> Transform Lucas into an original full-body Numeria pixel-art math-magic explorer. Preserve his short
> straight black hair, youthful face and proportions, navy short sleeves, pale teal front panel, black
> shorts, small black backpack, pale yellow socks and blue-green sport sandals. Give him a warm curious
> expression and confident neutral exploration pose while holding one small golden number crystal.
> Render polished hand-authored 16-bit pixel art in a slight top-down three-quarter view. Include only
> Lucas; omit all other people, objects, store scenery and branded characters. Do not adultify him; no
> hat, weapon, text, watermark, logo or trademark. Use the same flat `#FF00FF` removal background.

The original photo is intentionally not copied into the repository.

## Exploration economy assets

The coin and all three merchants were generated as one-character/one-object transparent gameplay sprites,
using the same dark forest-green outline, warm highlights, crisp pixel clusters and slightly top-down
three-quarter view as Lucas and the existing Numeria overworld art. Their final resources live under
`unity/Assets/Resources/generated/Economy/`.

- `numeria_coin.png`: "A golden Numeria coin with a raised ten-point number-crystal star and two tiny
  arithmetic sparks; clearly a spendable coin rather than the orange battle gems; compact circular silhouette."
- `merchant_tessa.png`: "Friendly adult forest trader Tessa in a moss-green coat and orange scarf, carrying
  a supply backpack with an abacus charm; approachable pose, original character, not based on a real identity."
- `merchant_bram.png`: "Friendly adult mountain trader Bram with a neat charcoal beard, slate quilted coat,
  crystal supply pack and small golden balance-scale charm; sturdy but welcoming pose."
- `merchant_ari.png`: "Friendly adult sky-city cartographer Ari in a pale-blue geometric coat, carrying rolled
  maps, compass and prism cases; clever welcoming pose."

Each used a perfectly flat `#FF00FF` removal background, no frame, text, watermark, logo, floor, scenery,
drop shadow, existing franchise character or trademark. Chroma removal used the same ImageGen helper as the
skill and Lucas assets.

## Digit Crystal guardians

The built-in ImageGen workflow also produced the main-story portraits under
`unity/Assets/Resources/generated/Story/`. Lucas and Tessa were supplied only as rendering-language references.
All four prompts asked for polished hand-authored 16-bit pixel clusters, a dark forest-green outline, readable
silhouette, centered subject and a perfectly flat `#FF00FF` removal background with no scenery, shadow, frame,
watermark, trademark or existing franchise character.

- `guardian_rowan.png`: Elder Rowan, a kind but formidable older woman with silver-green braided hair, leaf
  mantle, cream-and-moss number-rune robes and an emerald-gold crystal held in both hands.
- `guardian_orin.png`: Keeper Orin, a broad older man with a short white beard, slate-blue mountain robes,
  geometric stone clasps, an amber number sash and a sapphire-gold crystal.
- `guardian_lyra.png`: Astronomer Lyra, an adult woman with a dark-blue high braid, pale-cyan geometric coat,
  cloud-white cape, golden compass ornaments and a violet-cyan crystal.
- `digit_crystal.png`: one upright faceted story-key crystal with green, blue, violet and gold light bands, a
  glowing numeral 3 and three arithmetic sparks; explicitly distinct from the coin and orange battle gems.

These images are original fictional characters. The generated chroma-key sources remain in the local Codex
generation cache; the repository contains only the transparent production PNGs.
