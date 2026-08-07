#!/bin/zsh
# Remove ImageGen's chroma key and split one three-stage family sheet into Unity icons.
set -e

if [[ $# -lt 5 || $# -gt 6 ]]; then
  echo "usage: $0 <sheet.png> <tag> <stage1-id> <stage2-id> <stage3-id> [output-suffix]" >&2
  exit 2
fi

cd "$(dirname "$0")/.."
SHEET="$1"
TAG="$2"
ALPHA="/private/tmp/numeria-${TAG}-family-alpha.png"
KEY_HELPER="${CODEX_HOME:-$HOME/.codex}/skills/.system/imagegen/scripts/remove_chroma_key.py"

python3 "$KEY_HELPER" \
  --input "$SHEET" \
  --out "$ALPHA" \
  --auto-key border \
  --soft-matte \
  --transparent-threshold 12 \
  --opaque-threshold 220 \
  --edge-contract 1

SUFFIX="${6:-_large_icon}"
python3 tools/split-evolution-sheet.py "$ALPHA" unity/Assets/Resources/generated \
  --suffix "$SUFFIX" "$3" "$4" "$5"
