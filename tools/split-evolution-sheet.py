#!/usr/bin/env python3
"""Split a transparent three-character ImageGen sheet into three square Unity icons."""

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def components(alpha, minimum_pixels=200):
    width, height = alpha.size
    pixels = alpha.load()
    seen = bytearray(width * height)
    result = []
    for y in range(height):
        for x in range(width):
            index = y * width + x
            if seen[index] or pixels[x, y] < 16:
                continue
            queue = deque([(x, y)])
            seen[index] = 1
            count = 0
            left = right = x
            top = bottom = y
            while queue:
                cx, cy = queue.popleft()
                count += 1
                left, right = min(left, cx), max(right, cx)
                top, bottom = min(top, cy), max(bottom, cy)
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if nx < 0 or nx >= width or ny < 0 or ny >= height:
                        continue
                    neighbor = ny * width + nx
                    if seen[neighbor] or pixels[nx, ny] < 16:
                        continue
                    seen[neighbor] = 1
                    queue.append((nx, ny))
            if count >= minimum_pixels:
                result.append((left, top, right + 1, bottom + 1, count))
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("input")
    parser.add_argument("output_dir")
    parser.add_argument("names", nargs="+")
    args = parser.parse_args()

    image = Image.open(args.input).convert("RGBA")
    parts = components(image.getchannel("A"))
    count = len(args.names)
    if len(parts) < count:
        raise SystemExit(f"expected at least {count} connected bodies, found {len(parts)}: {parts}")
    anchors = sorted(sorted(parts, key=lambda box: box[4], reverse=True)[:count], key=lambda box: box[0])
    groups = [[] for _ in anchors]
    for part in parts:
        center = (part[0] + part[2]) / 2
        nearest = min(range(count), key=lambda index: abs(center - (anchors[index][0] + anchors[index][2]) / 2))
        groups[nearest].append(part)
    boxes = []
    for group in groups:
        boxes.append((min(box[0] for box in group), min(box[1] for box in group),
                      max(box[2] for box in group), max(box[3] for box in group),
                      sum(box[4] for box in group)))

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    for name, (left, top, right, bottom, _) in zip(args.names, boxes):
        width, height = right - left, bottom - top
        padding = max(18, int(max(width, height) * 0.08))
        side = max(width, height) + padding * 2
        icon = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        crop = image.crop((left, top, right, bottom))
        icon.alpha_composite(crop, ((side - width) // 2, (side - height) // 2))
        icon = icon.resize((512, 512), Image.Resampling.NEAREST)
        suffix = "_large_icon" if count > 1 else ""
        icon.save(output_dir / f"{name}{suffix}.png", optimize=True)


if __name__ == "__main__":
    main()
