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

# ---- 咒语算式读题:a + ? = sum,sum 3..10,a 1..sum-1(与 PuzzleGenerator 一致) ----
words=(zero One Two Three Four Five Six Seven Eight Nine Ten)
lower=(zero one two three four five six seven eight nine ten)
for sum in {3..10}; do
  for a in {1..$((sum - 1))}; do
    bake "${words[$((a + 1))]} plus what makes ${lower[$((sum + 1))]}?"
  done
done

rm -rf "$TMP"
echo "done: $(ls "$OUT" | wc -l | tr -d ' ') clips in $OUT"
