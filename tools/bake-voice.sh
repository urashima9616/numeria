#!/bin/zsh
# 预烘焙全部英文语音台词 → unity/Assets/Resources/Voice/*.wav
# key 规则必须与 unity/Assets/Scripts/Game/VoiceKeys.cs 一致:
# 小写、非字母数字折叠为 '-'、去首尾 '-'
set -e
cd "$(dirname "$0")/.."
OUT=unity/Assets/Resources/Voice
TMP=$(mktemp -d)
mkdir -p "$OUT"

VOICE=Samantha
RATE=150

bake() {
  local text="$1"
  local key
  key=$(printf '%s' "$text" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//')
  # macOS 语音服务不可用时，say/afconvert 仍可能留下只有 4096 bytes WAV 头的 0 秒空壳。
  # 只缓存真正含有音频正文的文件；下次获准访问系统语音服务时会自动修复空壳。
  if [[ -f "$OUT/$key.wav" ]] && [[ $(stat -f %z "$OUT/$key.wav") -gt 4096 ]]; then return; fi
  say -v "$VOICE" -r "$RATE" -o "$TMP/$key.aiff" "$text"
  afconvert -f WAVE -d LEI16@22050 -c 1 "$TMP/$key.aiff" "$OUT/$key.wav"
  echo "baked: $key.wav"
}

# ---- 固定台词 ----
bake "A wild Duplirock appeared! It has a number shield!"
bake "Pick two crystals that make ten!"
bake "Great job!"
bake "Shield break!"
bake "Hmm, not quite."
bake "Try again!"
bake "Nice try! Your move still works!"
bake "Nice try! The shield holds for now."
bake "You win! Addmander got five experience points!"
bake "Oh no! Let's try again!"
bake "You win!"

# ---- P2 森林地图台词 ----
bake "A wild Countipillar appeared!"
bake "Gotcha! Countipillar joined your team!"
bake "Duplirock guards the portal!"
bake "The portal is open! A new world awaits!"
bake "Level up! Addmander is getting stronger!"
bake "A math chest! Solve the lock!"
bake "Attack goes up by one!"
bake "Defense goes up by one!"
bake "Let's rest and try again!"
bake "Portal trial! Solve three magic puzzles!"
bake "How many fireflies do you see?"
bake "Which side has more mushrooms?"

# ---- 咒语算式读题(与 PuzzleGenerator 一致) ----
lower=(zero one two three four five six seven eight nine ten
  eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty
  twenty-one twenty-two twenty-three twenty-four twenty-five twenty-six twenty-seven
  twenty-eight twenty-nine thirty)
lower+=(thirty-one thirty-two thirty-three thirty-four thirty-five thirty-six thirty-seven
  thirty-eight thirty-nine forty)

# 加法填空:a + ? = sum,sum 3..40
for sum in {3..40}; do
  for a in {1..$((sum - 1))}; do
    bake "${lower[$((a + 1))]} plus what makes ${lower[$((sum + 1))]}?"
  done
done

# 减法填空:a - ? = c,a 3..40
for a in {3..40}; do
  for c in {1..$((a - 1))}; do
    bake "${lower[$((a + 1))]} take away what leaves ${lower[$((c + 1))]}?"
  done
done

# 翻倍:n 2..10
for n in {2..10}; do
  bake "What is double ${lower[$((n + 1))]}?"
done

# 三关护盾 / 凑数题
bake "Pick two crystals that make twelve!"
bake "Pick two crystals that make twenty!"
bake "Pick two crystals that make thirty!"
bake "Pick two crystals that make forty!"

# ---- P3 山脉与进化台词 ----
bake "A wild Doublit appeared!"
bake "Gotcha! Doublit joined your team!"
bake "A wild Duplirock Elder appeared! It has a number shield!"
bake "Welcome to Silent Peaks!"
bake "Welcome to Mystic Forest!"
bake "You found the Evolution Stone!"
bake "Reach level eight to evolve!"
bake "Evolution trial! Solve three puzzles!"
bake "Addmander is evolving!"
bake "Amazing! Addmander evolved into Sumdrake!"
bake "The gate is cleared! Azure Sky City awaits!"
bake "You win! Sumdrake got five experience points!"
bake "Level up! Sumdrake is getting stronger!"
bake "You win! Countipillar got five experience points!"
bake "Level up! Countipillar is getting stronger!"
bake "You win! Doublit got five experience points!"
bake "Level up! Doublit is getting stronger!"
bake "Already best friends! Bonus experience!"
bake "It broke free! Lower its health and try again!"
bake "Shield stun! The enemy misses a turn!"
bake "You found an attack charm! Equip it to one Mathmon."
bake "You found a defense charm! Equip it to one Mathmon."

# ---- P3 天空城与图形规律 ----
bake "What comes next in the pattern?"
bake "Find the matching wing!"
bake "Which one is it after a turn?"
bake "What number comes next?"
bake "Add them all up!"
bake "Find the circle!"
bake "Find the triangle!"
bake "Find the square!"
bake "Find the diamond!"
bake "Which shape has three straight sides?"
bake "Which shape has no straight sides?"
bake "Find the four-sided shape with a flat top!"
bake "Find the four-sided shape standing on a point!"
bake "Welcome to Azure Sky City!"
bake "A wild Mirrowl appeared!"
bake "Gotcha! Mirrowl joined your team!"
bake "A wild Symmetrix appeared! It has a pattern shield!"
bake "Symmetrix guards the sky gate!"
bake "The sky gate shines! More adventures await!"
bake "You win! Mirrowl got five experience points!"
bake "Level up! Mirrowl is getting stronger!"

# ---- 首发 15 只图鉴与通用进化链 ----
bake "A wild Numberfly appeared! It has a number shield!"
bake "Numberfly guards the portal!"
bake "Reach level 15 to evolve!"

bake "Sumdrake is evolving!"
bake "Amazing! Sumdrake evolved into Equadragon!"
bake "Tenfin is evolving!"
bake "Amazing! Tenfin evolved into Decaqua!"
bake "Decaqua is evolving!"
bake "Amazing! Decaqua evolved into Tidalten!"
bake "Shapling is evolving!"
bake "Amazing! Shapling evolved into Pattervine!"
bake "Pattervine is evolving!"
bake "Amazing! Pattervine evolved into Geoflora!"
bake "Countipillar is evolving!"
bake "Amazing! Countipillar evolved into Numberfly!"
bake "Doublit is evolving!"
bake "Amazing! Doublit evolved into Duplirock!"
bake "Mirrowl is evolving!"
bake "Amazing! Mirrowl evolved into Symmetrix!"

for mon in Equadragon Tenfin Decaqua Tidalten Shapling Pattervine Geoflora Numberfly Duplirock Symmetrix; do
  bake "You win! $mon got five experience points!"
  bake "Level up! $mon is getting stronger!"
done

# ---- 30 只图鉴、动态生态与战斗消耗品 ----
mathmons=(Addmander Sumdrake Equadragon Tenfin Decaqua Tidalten Shapling Pattervine Geoflora
  Countipillar Numberfly Doublit Duplirock Mirrowl Symmetrix
  Paircub Matchbear Equilibear Subunny Differhare Minuelope Pebblit Stackstone Tallytitan
  Prismouse Polygoncat Geometiger Seqkit Patternlynx Ordinalion
  Glimlet Twinkelle Luminara Moonmote Lunafae Selenequin Charmite Pairabelle Harmonique
  Wishwink Starwhisp Constellara Pixipip Prismfae Radianta
  Addling Sumscale Totalisk Dracount Tallywyrm Enumeragon Loopling Spirake Rotaragon
  Twinsting Doublescale Multisaur Shardrake Prismwyrm Geodragon
  Voltlet Sumvolt Totalstorm Sparkit Patternzap Sequencera Chargecub Doublebolt Thunderbear
  Flickerfin Neonray Luminamp Switchick Mirrorvolt Voltalance
  Budsum Vineplus Totalbloom Clovercub Fourleaf Cloverlord Sprouturn Spiralfern Symmetroak
  Mossbit Doublmoss Grovemult Seedseq Patternpod Orderchid
  Numblet Tallywing Totalon)
for mon in $mathmons; do
  bake "A wild $mon appeared!"
  bake "Gotcha! $mon joined your team!"
  bake "Gotcha! $mon wants to travel with you!"
  bake "$mon is getting stronger!"
done

bake "Paircub is evolving!"
bake "Amazing! Paircub evolved into Matchbear!"
bake "Matchbear is evolving!"
bake "Amazing! Matchbear evolved into Equilibear!"
bake "Subunny is evolving!"
bake "Amazing! Subunny evolved into Differhare!"
bake "Differhare is evolving!"
bake "Amazing! Differhare evolved into Minuelope!"
bake "Pebblit is evolving!"
bake "Amazing! Pebblit evolved into Stackstone!"
bake "Stackstone is evolving!"
bake "Amazing! Stackstone evolved into Tallytitan!"
bake "Prismouse is evolving!"
bake "Amazing! Prismouse evolved into Polygoncat!"
bake "Polygoncat is evolving!"
bake "Amazing! Polygoncat evolved into Geometiger!"
bake "Seqkit is evolving!"
bake "Amazing! Seqkit evolved into Patternlynx!"
bake "Patternlynx is evolving!"
bake "Amazing! Patternlynx evolved into Ordinalion!"

new_evolution_from=(Glimlet Twinkelle Moonmote Lunafae Charmite Pairabelle Wishwink Starwhisp Pixipip Prismfae
  Addling Sumscale Dracount Tallywyrm Loopling Spirake Twinsting Doublescale Shardrake Prismwyrm
  Voltlet Sumvolt Sparkit Patternzap Chargecub Doublebolt Flickerfin Neonray Switchick Mirrorvolt
  Budsum Vineplus Clovercub Fourleaf Sprouturn Spiralfern Mossbit Doublmoss Seedseq Patternpod
  Numblet Tallywing)
new_evolution_to=(Twinkelle Luminara Lunafae Selenequin Pairabelle Harmonique Starwhisp Constellara Prismfae Radianta
  Sumscale Totalisk Tallywyrm Enumeragon Spirake Rotaragon Doublescale Multisaur Prismwyrm Geodragon
  Sumvolt Totalstorm Patternzap Sequencera Doublebolt Thunderbear Neonray Luminamp Mirrorvolt Voltalance
  Vineplus Totalbloom Fourleaf Cloverlord Spiralfern Symmetroak Doublmoss Grovemult Patternpod Orderchid
  Tallywing Totalon)
for i in {1..42}; do
  bake "${new_evolution_from[$i]} is evolving!"
  bake "Amazing! ${new_evolution_from[$i]} evolved into ${new_evolution_to[$i]}!"
done

bake "Reach level five to evolve!"
bake "Reach level seven to evolve!"
bake "Reach level fourteen to evolve!"
bake "Reach level ten to evolve!"
bake "Reach level twelve to evolve!"
bake "Reach level twenty to evolve!"
bake "Reach level twenty-four to evolve!"
bake "Reach level twenty-eight to evolve!"
bake "The enemy dropped an HP Potion!"
bake "The enemy dropped a Gem Snack!"
bake "Health is already full!"
bake "Gems are already full!"
bake "Your team is full. Choose a friend to release, or let the new friend go."
bake "Your new friend joined the team!"
bake "The new friend returned to the wild."
bake "You caught a stronger friend. Keep it, or turn the catch into experience."
bake "Your stronger friend is ready for adventure!"
bake "A number rune is glowing! Solve its math magic!"
bake "Tessa smiles. Beat my Paircub and my shop is yours to browse!"
bake "Bram nods. Show my Stackstone your strongest math magic, then we can trade!"
bake "Ari opens a star map. Outsmart my Polygoncat and the sky market opens!"
bake "Nia raises her sun goggles. Match my Mirrorvolt and the oasis market is yours!"
bake "Shop unlocked!"
bake "Great choice!"
bake "Not enough coins."
bake "That item is sold out."
bake "Lucas wakes beneath a sky full of glowing numbers."
bake "Where am I? This isn't home."
bake "Welcome to Numeria, Lucas. The gate home has lost its power."
bake "Four Digit Crystals can wake it. Seek the Crystal Guardians."
bake "Let's be brave, make Mathmon friends, and solve this together!"
bake "Lucas, the Forest Crystal answers only to a kind and clever heart."
bake "Show Numberfly what you have learned. Mistakes are steps, not failures."
bake "You have earned the Forest Digit Crystal. Carry its light wisely."
bake "The mountain remembers every brave attempt."
bake "Match your strength with Duplirock Elder, and the Peaks Crystal will shine."
bake "The Peaks Digit Crystal is yours. Your courage gave it light."
bake "Patterns guide every star in Numeria."
bake "Read Symmetrix's sky pattern, and the third crystal will be yours."
bake "The Sky Digit Crystal is yours. One last light burns beyond the clouds."
bake "Welcome to Fever Desert!"
bake "The desert sun hides patterns in every dune."
bake "Join the four kinds of magic, and show Solar Totalisk how brightly you can think."
bake "Solar Totalisk guards the final crystal!"
bake "The Desert Digit Crystal is yours. All four lights now sing together."
bake "The desert crystal blazes! The gate home is awake!"
bake "The four Digit Crystals sing together. The gate home is awake!"
bake "A wild Solar Totalisk appeared! It has a number shield!"
bake "I can go home when I am ready—and Numeria will always be waiting."
for coins in {1..50}; do
  bake "You found $coins Numeria coins!"
done
for amount in 1 2 3 4; do
  bake "You found $amount HP Potions! Use them only in battle."
  bake "You found $amount Gem Snacks! Use them only in battle."
done
for remaining in 1 2 3 4 5; do
  if [[ $remaining -eq 1 ]]; then
    bake "The portal is quiet. Find 1 more treasure chest!"
  else
    bake "The portal is quiet. Find $remaining more treasure chests!"
  fi
done

rm -rf "$TMP"
echo "done: $(find "$OUT" -type f -name '*.wav' | wc -l | tr -d ' ') clips in $OUT"
