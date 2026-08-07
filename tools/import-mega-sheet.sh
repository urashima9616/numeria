#!/bin/zsh
# Remove an ImageGen chroma key and split a two- or three-form Mega family sheet.
set -e

if [[ $# -lt 4 ]]; then
  echo "usage: $0 <sheet.png> <tag> <stage1-id> <stage2-id> [stage3-id ...]" >&2
  exit 2
fi

cd "$(dirname "$0")/.."
SHEET="$1"
TAG="$2"
shift 2
ALPHA="/private/tmp/numeria-${TAG}-mega-alpha.png"
KEY_HELPER="${CODEX_HOME:-$HOME/.codex}/skills/.system/imagegen/scripts/remove_chroma_key.py"

python3 "$KEY_HELPER" \
  --input "$SHEET" \
  --out "$ALPHA" \
  --auto-key border \
  --soft-matte \
  --transparent-threshold 12 \
  --opaque-threshold 220 \
  --edge-contract 1

python3 tools/split-evolution-sheet.py "$ALPHA" unity/Assets/Resources/generated \
  --suffix _mega_icon "$@"
