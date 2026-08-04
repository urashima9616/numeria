#!/bin/zsh
# 将本地 8-bit Jukebox Lite 选曲同步到 Numeria 的七个运行时音乐 mood。
# 默认安装 Jukebox；传 --restore-dynamic 可恢复此前的 Dynamic Music 选曲。
set -e

cd "$(dirname "$0")/.."
DEST="unity/Assets/Resources/Music/Jukebox"
JUKEBOX="unity/Assets/Cyberleaf Music - The 8-bit Jukebox Lite"
DYNAMIC="unity/Assets/Dynamic Music/Audio Files"
mkdir -p "$DEST"

copy_track() {
  local source="$1"
  local target="$2"
  if [[ ! -f "$source" ]]; then
    echo "missing licensed source: $source" >&2
    exit 1
  fi
  cp -f "$source" "$DEST/$target.wav"
  cmp -s "$source" "$DEST/$target.wav"
  echo "$target <- $(basename "$source")"
}

if [[ "$1" == "--restore-dynamic" ]]; then
  copy_track "$DYNAMIC/Stealth/Parts/Stealth Menu Loop.wav" forest
  copy_track "$DYNAMIC/Tibet/Parts/Tibet Menu Loop.wav" mountains
  copy_track "$DYNAMIC/Centurion/Parts/Centurion Menu Loop.wav" sky
  copy_track "$DYNAMIC/Tibet/Parts/Tibet Part 2 Loop.wav" desert
  copy_track "$DYNAMIC/Battlefield/Parts/Battlefield Part 1.wav" battle
  copy_track "$DYNAMIC/Battlefield/Parts/Battlefield Part 3.wav" boss
  copy_track "$DYNAMIC/Tension/Parts/Tension Part 1 Loop.wav" evolution
  echo "installed previous Dynamic Music mapping into the Jukebox runtime slots"
  exit 0
fi

copy_track "$JUKEBOX/CaptChipPants.wav" forest
copy_track "$JUKEBOX/LittleHauntedMansion.wav" mountains
copy_track "$JUKEBOX/Don'tFallOffTheClouds.wav" sky
copy_track "$JUKEBOX/PyramidsPyramids.wav" desert
copy_track "$JUKEBOX/OfGodsAndPhilosophers.wav" battle
copy_track "$JUKEBOX/WakingTheDemons.wav" boss
copy_track "$JUKEBOX/VictoryAtLast.wav" evolution
echo "installed 8-bit Jukebox Lite mapping"
