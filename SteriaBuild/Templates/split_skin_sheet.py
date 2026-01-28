#!/usr/bin/env python3
"""
Split a sprite sheet into LoR ClothCustom action images.

Features:
- Optional resize to a target size before splitting.
- Row-major slicing (left-to-right, top-to-bottom).
- Custom filename prefix.
"""

import argparse
from pathlib import Path

from PIL import Image


DEFAULT_NAMES = [
    "Default",
    "Guard",
    "Evade",
    "Damaged",
    "Slash",
    "Penetrate",
    "Hit",
    "Move",
    "S1",
]


def parse_size(value: str) -> tuple[int, int]:
    parts = value.lower().split("x")
    if len(parts) != 2:
        raise argparse.ArgumentTypeError("Size must be like 1536x1536")
    try:
        return int(parts[0]), int(parts[1])
    except ValueError as exc:
        raise argparse.ArgumentTypeError("Size must be like 1536x1536") from exc


def parse_names(value: str) -> list[str]:
    return [name.strip() for name in value.split(",") if name.strip()]


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Split a sheet into ClothCustom action PNGs.",
    )
    parser.add_argument("-i", "--input", required=True, help="Input sheet image.")
    parser.add_argument(
        "-o",
        "--output-dir",
        default=".",
        help="Output directory. Defaults to current directory.",
    )
    parser.add_argument(
        "--prefix",
        default="",
        help="Filename prefix for outputs (e.g. Ailierel_).",
    )
    parser.add_argument("--rows", type=int, default=3, help="Grid rows.")
    parser.add_argument("--cols", type=int, default=3, help="Grid columns.")
    parser.add_argument(
        "--cell",
        type=int,
        default=512,
        help="Cell size in pixels (square).",
    )
    parser.add_argument(
        "--target-size",
        type=parse_size,
        default=None,
        help="Resize sheet to WIDTHxHEIGHT before slicing (e.g. 1536x1536).",
    )
    parser.add_argument(
        "--names",
        type=parse_names,
        default=None,
        help="Comma-separated output names in row-major order.",
    )
    parser.add_argument(
        "--ext",
        default="png",
        help="Output extension (default: png).",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    input_path = Path(args.input)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    img = Image.open(input_path).convert("RGBA")

    if args.target_size:
        target_w, target_h = args.target_size
        img = img.resize((target_w, target_h), Image.LANCZOS)

    cols = args.cols
    rows = args.rows
    cell = args.cell

    expected_w = cols * cell
    expected_h = rows * cell
    if img.width != expected_w or img.height != expected_h:
        raise SystemExit(
            f"Image size {img.width}x{img.height} does not match "
            f"cols*cell x rows*cell ({expected_w}x{expected_h}). "
            "Use --target-size or adjust --rows/--cols/--cell."
        )

    if args.names:
        names = args.names
        if len(names) != rows * cols:
            raise SystemExit(
                f"--names count {len(names)} does not match rows*cols ({rows*cols})."
            )
    else:
        if rows * cols == len(DEFAULT_NAMES):
            names = DEFAULT_NAMES
        else:
            names = [f"cell_{i+1:02d}" for i in range(rows * cols)]

    for idx, name in enumerate(names):
        col = idx % cols
        row = idx // cols
        left = col * cell
        top = row * cell
        right = left + cell
        bottom = top + cell
        crop = img.crop((left, top, right, bottom))

        filename = f"{args.prefix}{name}.{args.ext}"
        crop.save(output_dir / filename)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
