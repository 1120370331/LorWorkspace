"""Deterministically size the three story portraits for StoryStanding.

The source images are left intact. All variants use the Default image's single
scale factor; each subject is bottom aligned on one shared canvas.
"""

from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / "ArtSource" / "Story" / "portraits"
DESTINATION = ROOT / "AceTaffyMod" / "Resource" / "Story" / "Characters"
REPORT = Path(__file__).resolve().parent
NAMES = ("Taffy_Default", "Taffy_Happy", "Taffy_Confused")
PERSON_HEIGHT = 820
CANVAS_HEIGHT = 896
FOOT_MARGIN = 5
SIDE_MARGIN = 20


def visible_box(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    # A small threshold excludes isolated near-transparent antialiasing noise.
    bounds = alpha.point(lambda value: 255 if value >= 8 else 0).getbbox()
    if bounds is None:
        raise ValueError("Portrait has no visible subject")
    return bounds


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    images = {}
    boxes = {}
    for name in NAMES:
        path = SOURCES / f"{name}.png"
        if not path.is_file():
            raise FileNotFoundError(path)
        with Image.open(path) as opened:
            opened.load()
            if opened.format != "PNG" or opened.mode != "RGBA":
                raise ValueError(f"{name} must be an RGBA PNG")
            images[name] = opened.copy()
        boxes[name] = visible_box(images[name])

    default = boxes["Taffy_Default"]
    scale = PERSON_HEIGHT / (default[3] - default[1])
    scaled = {}
    scaled_boxes = {}
    for name in NAMES:
        image = images[name]
        size = (round(image.width * scale), round(image.height * scale))
        scaled[name] = image.resize(size, Image.Resampling.LANCZOS)
        scaled_boxes[name] = visible_box(scaled[name])

    largest_width = max(box[2] - box[0] for box in scaled_boxes.values())
    canvas_width = max(512, math.ceil((largest_width + SIDE_MARGIN * 2) / 16) * 16)
    DESTINATION.mkdir(parents=True, exist_ok=True)
    manifest = {
        "scale_from_default": scale,
        "default_source_visible_height": default[3] - default[1],
        "target_visible_height": PERSON_HEIGHT,
        "canvas": [canvas_width, CANVAS_HEIGHT],
        "foot_margin": FOOT_MARGIN,
        "portraits": {},
    }
    previews = []
    for name in NAMES:
        box = scaled_boxes[name]
        subject = scaled[name].crop(box)
        canvas = Image.new("RGBA", (canvas_width, CANVAS_HEIGHT), (0, 0, 0, 0))
        left = (canvas_width - subject.width) // 2
        top = CANVAS_HEIGHT - FOOT_MARGIN - subject.height
        if left < 0 or top < 0:
            raise ValueError(f"{name} exceeds the fixed canvas")
        canvas.alpha_composite(subject, (left, top))
        output = DESTINATION / f"{name}.png"
        canvas.save(output, optimize=True)
        result_box = visible_box(canvas)
        if result_box[3] != CANVAS_HEIGHT - FOOT_MARGIN:
            raise AssertionError(f"Foot alignment failed for {name}: {result_box}")
        manifest["portraits"][name] = {
            "source": str(SOURCES / f"{name}.png"),
            "source_sha256": sha256(SOURCES / f"{name}.png"),
            "source_alpha_box": boxes[name],
            "derived": str(output),
            "derived_sha256": sha256(output),
            "derived_alpha_box": result_box,
        }
        previews.append(canvas)

    (REPORT / "portrait-import.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    sheet = Image.new("RGBA", (canvas_width * 3, CANVAS_HEIGHT), (30, 33, 39, 255))
    draw = ImageDraw.Draw(sheet)
    for index, (name, portrait) in enumerate(zip(NAMES, previews)):
        x = index * canvas_width
        sheet.alpha_composite(portrait, (x, 0))
        draw.line((x, CANVAS_HEIGHT - FOOT_MARGIN, x + canvas_width, CANVAS_HEIGHT - FOOT_MARGIN), fill=(242, 191, 77, 255), width=2)
        draw.text((x + 12, 12), name, fill=(255, 255, 255, 255))
    sheet.convert("RGB").save(REPORT / "portrait-import-preview.png")
    print(json.dumps({"scale": scale, "canvas": [canvas_width, CANVAS_HEIGHT], "boxes": {key: value["derived_alpha_box"] for key, value in manifest["portraits"].items()}}, ensure_ascii=False))


if __name__ == "__main__":
    main()
