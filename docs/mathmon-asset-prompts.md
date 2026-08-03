# Numeria 30-species expansion — ImageGen prompt manifest

Generated with the built-in ImageGen tool on 2026-08-02. Each asset used the common production prompt below plus one species brief. Sources were generated on a flat `#ff00ff` chroma background, then converted to hard-edge alpha PNGs with the installed `remove_chroma_key.py` helper (`--auto-key border --tolerance 36`).

## Common production prompt

> Use case: stylized-concept. Asset type: square Numeria game character icon. Polished enlarged 32-bit pixel art with crisp block pixels, a strong dark navy-brown outline, readable handheld-RPG monster silhouette, two-step pixel shading, kindergarten-friendly expression, and a completely original design. Exactly one centered full-body front three-quarter pose with all defining parts visible and generous padding. Perfectly flat uniform `#ff00ff` chroma-key background; no gradient, texture, shadow, floor, reflection, text, written numbers, frame, extra objects, trademark, or watermark. Do not use magenta in the subject.

## Species briefs and final files

| Family | Species brief | Final asset |
|---|---|---|
| Equality bears | Paircub — honey-brown bear cub, twin ear marks and gold equals-sign paw badges | `paircub_large_icon.png` |
| | Matchbear — sturdier bear, matching cream chest halves and forearm equality emblems | `matchbear_large_icon.png` |
| | Equilibear — large guardian, symmetrical markings and balance-scale shoulder motifs | `equilibear_large_icon.png` |
| Subtraction runners | Subunny — small sandy rabbit, ear dashes and coral minus belly badge | `subunny_large_icon.png` |
| | Differhare — lean athletic hare, repeated dash markings and subtraction badge | `differhare_large_icon.png` |
| | Minuelope — rabbit-antelope guardian, short horns and symmetrical minus markings | `minuelope_large_icon.png` |
| Tally stones | Pebblit — squat living pebble, three back plates ordered smallest to largest | `pebblit_large_icon.png` |
| | Stackstone — medium golem built from ordered stone tiers with four tally notches | `stackstone_large_icon.png` |
| | Tallytitan — mountain guardian with ascending plates and five chest notches | `tallytitan_large_icon.png` |
| Geometry cats | Prismouse — cream mouse with segmented geometric ears and faceted tail tip | `prismouse_large_icon.png` |
| | Polygoncat — agile cat with polygon patchwork, pentagon gem and prism tail | `polygoncat_large_icon.png` |
| | Geometiger — heroic tiger with triangle/diamond stripes and hexagon forehead gem | `geometiger_large_icon.png` |
| Sequence cats | Seqkit — leafy kitten with a gold-dot/cream-stripe AB tail pattern | `seqkit_large_icon.png` |
| | Patternlynx — leafy lynx carrying the repeated pattern across tail and legs | `patternlynx_large_icon.png` |
| | Ordinalion — leaf lion with ordered mane bands and repeating tail/leg pattern | `ordinalion_large_icon.png` |

All final assets are 1254×1254 RGBA PNGs under `unity/Assets/Resources/generated/`. Unity imports them as point-filtered sprites through the existing `PixelArtImporter` pipeline.
