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

# 加法填空:a + ? = sum,sum 3..30
for sum in {3..30}; do
  for a in {1..$((sum - 1))}; do
    bake "${lower[$((a + 1))]} plus what makes ${lower[$((sum + 1))]}?"
  done
done

# 减法填空:a - ? = c,a 3..30
for a in {3..30}; do
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
  Prismouse Polygoncat Geometiger Seqkit Patternlynx Ordinalion)
for mon in $mathmons; do
  bake "A wild $mon appeared!"
  bake "Gotcha! $mon joined your team!"
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

bake "Reach level five to evolve!"
bake "Reach level seven to evolve!"
bake "Reach level fourteen to evolve!"
bake "The enemy dropped an HP Potion!"
bake "The enemy dropped a Gem Snack!"
bake "Health is already full!"
bake "Gems are already full!"
for amount in 1 2 3; do
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
echo "done: $(ls "$OUT" | wc -l | tr -d ' ') clips in $OUT"
