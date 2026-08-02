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
  if [[ -f "$OUT/$key.wav" ]]; then return; fi
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

# ---- P2 森林地图台词 ----
bake "A wild Countipillar appeared!"
bake "Gotcha! Countipillar joined your team!"
bake "Duplirock guards the portal!"
bake "The portal is open! A new world awaits!"
bake "Level up! Addmander is getting stronger!"
bake "A math chest! Solve the lock!"
bake "Attack goes up by one!"
bake "Let's rest and try again!"

# ---- 咒语算式读题(与 PuzzleGenerator 一致) ----
lower=(zero one two three four five six seven eight nine ten
  eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty)

# 加法填空:a + ? = sum,sum 3..20
for sum in {3..20}; do
  for a in {1..$((sum - 1))}; do
    bake "${lower[$((a + 1))]} plus what makes ${lower[$((sum + 1))]}?"
  done
done

# 减法填空:a - ? = c,a 3..20
for a in {3..20}; do
  for c in {1..$((a - 1))}; do
    bake "${lower[$((a + 1))]} take away what leaves ${lower[$((c + 1))]}?"
  done
done

# 翻倍:n 2..10
for n in {2..10}; do
  bake "What is double ${lower[$((n + 1))]}?"
done

# 凑十二(山脉 Boss 护盾)
bake "Pick two crystals that make twelve!"

# ---- P3 山脉与进化台词 ----
bake "A wild Doublit appeared!"
bake "Gotcha! Doublit joined your team!"
bake "A wild Duplirock Elder appeared! It has a number shield!"
bake "Welcome to Silent Peaks!"
bake "Welcome to Mystic Forest!"
bake "You found the Evolution Stone!"
bake "Reach level five to evolve!"
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

# ---- P3 天空城与图形规律 ----
bake "What comes next in the pattern?"
bake "Welcome to Azure Sky City!"
bake "A wild Mirrowl appeared!"
bake "Gotcha! Mirrowl joined your team!"
bake "A wild Symmetrix appeared! It has a pattern shield!"
bake "Symmetrix guards the sky gate!"
bake "The sky gate shines! More adventures await!"
bake "You win! Mirrowl got five experience points!"
bake "Level up! Mirrowl is getting stronger!"

rm -rf "$TMP"
echo "done: $(ls "$OUT" | wc -l | tr -d ' ') clips in $OUT"
