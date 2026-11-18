"""Compact source-grounded 2D simulation of the production steam timeline.

Uses the shipped action PNG, ModInfo quality/pivot, authored effect mothers and
the TaffyVfx.Sample constants. It is not Unity or gameplay capture.
"""
from __future__ import annotations

import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / "AceTaffyMod/Resource"
OUT = Path(__file__).resolve().parent / "preview"
TIMES = (0, 50, 150, 250, 400, 650, 850)
PPU = {"SteamBlunt": 1170, "SteamPierce": 1050, "SteamGuard": 1000, "SteamDetonation": 790, "SteamWisp": 900}
FORWARD = {"SteamBlunt": 1.2, "SteamPierce": 1.4, "SteamGuard": .3, "SteamDetonation": 2.0}
SPEC = {
    "SteamBlunt": ("Hit", 0.65, 0.55, 0.65, 4, 1.0),
    "SteamPierce": ("Penetrate", 0.55, 0.45, 0.55, 3, 1.0),
    "SteamGuard": ("Guard", 0.55, 0.45, 0.55, 0, 2.0),
    "SteamDetonation": ("Hit", 0.85, 0.60, 0.85, 8, 1.25),
}


def fade(t: float, hold: float, end: float) -> float:
    u = max(0.0, min(1.0, (t - hold) / (end - hold)))
    return 1 - u * u * (3 - 2 * u)


def tinted(source: Image.Image, alpha: float, rgb=(255, 255, 255)) -> Image.Image:
    image = source.copy()
    if rgb != (255, 255, 255):
        colored = Image.new("RGBA", image.size, (*rgb, 0))
        colored.putalpha(image.getchannel("A"))
        image = colored
    image.putalpha(image.getchannel("A").point(lambda a: int(a * max(0, min(1, alpha)))))
    return image


def paste_center(canvas: Image.Image, source: Image.Image, center: tuple[int, int], width: int, facing: int, alpha=1.0, tint=(255, 255, 255)):
    height = max(1, round(source.height * width / source.width))
    image = tinted(source.resize((max(1, width), height), Image.Resampling.LANCZOS), alpha, tint)
    if facing == 1:
        image = image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    canvas.alpha_composite(image, (center[0] - image.width // 2, center[1] - image.height // 2))


def render(name: str, t_ms: int, facing: int, roll: float, actor: Image.Image, mother: Image.Image, wisp: Image.Image, pivot: tuple[float, float], ppu: float) -> Image.Image:
    width, height = 1180, 680
    scene = Image.new("RGBA", (width, height), (91, 96, 100, 255))
    draw = ImageDraw.Draw(scene)
    draw.rectangle((0, 515, width, height), fill=(63, 63, 65, 255))
    draw.line((0, 515, width, 515), fill=(121, 113, 103, 255), width=2)
    # ModInfo quality is the authored pixels per unit; camera calibration keeps
    # 512 authored pixels at 470 display pixels and applies its signed pivot.
    actor_size = 512
    actor_left, actor_top = (width - actor_size) // 2, 90
    actor_draw = actor.resize((actor_size, actor_size), Image.Resampling.LANCZOS)
    if facing == 1:
        actor_draw = actor_draw.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    scene.alpha_composite(actor_draw, (actor_left, actor_top))
    # ModInfo positions the cloth relative to its root. Approximate the native
    # hand pivot above that root; its exact Unity transform remains unverified.
    pivot_x = width // 2 + facing * int(pivot[0] * actor_size / 512)
    root_y = actor_top + actor_size // 2 - int(pivot[1] * actor_size / 512)
    pivot_y = root_y - round(actor_size * .59)
    # Negative actor-space X is screen LEFT; flip once for RIGHT.
    pivot_x += facing * 110
    pivot_x += facing * round(FORWARD[name] * 50)
    pivot_y -= 9
    t = t_ms / 1000
    _, nominal, body_end, wisp_end, count, body_scale = SPEC[name]
    # Native base scale 2; local PNG PPU 200, actor PPU 50. The map/camera
    # projection is held constant and the native roll multiplier is applied.
    base_width = round(mother.width / PPU[name] * 2 * (actor_size / (512 / ppu)) * body_scale * roll)
    if name == "SteamGuard":
        # Guard has native additionalScale=-.5, canceling its 2x child scale.
        base_width //= 2
    expand = 1 + .15 * min(1, t / .25) if name == "SteamDetonation" else 1 - .04 * min(1, t / .22) if name == "SteamGuard" else 1
    body_alpha = fade(t, .16, body_end) * (.90 if name == "SteamGuard" else .96)
    paste_center(scene, mother, (pivot_x - facing * round(.06 * min(1, t / .05) * 45), pivot_y), max(1, round(base_width * expand)), facing, body_alpha)
    for i in range(count):
        start = .025 + i * .018
        a = max(0, min(1, (t - start) / .045)) * fade(t, .19, wisp_end) * (.22 + .04 * (i % 3))
        x = pivot_x + facing * round((.18 + i * .095 + t * .28) * 45)
        y = pivot_y - round((((1 if i % 2 == 0 else -1) * (.07 + i * .045)) + t * .22) * 45)
        paste_center(scene, wisp, (x, y), round(wisp.width / PPU["SteamWisp"] * 2 * 50 * (.16 + .035 * (i % 3)) * roll), facing, a)
    if name == "SteamDetonation":
        for i in range(6):
            a = max(0, min(1, (t - .025) / .025)) * fade(t, .08, .22) * .75
            x = pivot_x - facing * round((.19 + i * .055 + t * .26) * 45)
            y = pivot_y - round(((i - 2.5) * .09 + t * (.12 if i % 2 == 0 else -.08)) * 45)
            draw.line((x - facing * 5, y, x + facing * 5, y - 2), fill=(222, 156, 75, round(255 * a)), width=2)
    draw.text((20, 18), f"SIMULATION | {name} | {'LEFT' if facing == -1 else 'RIGHT'} | {t_ms} ms | native roll x{roll}", fill="white")
    return scene.convert("RGB")


def main():
    skin = RES / "CharacterSkin/AceTaffy"
    effects = RES / "Effects/AceTaffy"
    info = ET.parse(skin / "ModInfo.xml")
    required = [effects / f"{name}.png" for name in (*SPEC, "SteamWisp")]
    required += [skin / "ClothCustom" / f"{action}.png" for action in ("Hit", "Penetrate", "Guard")]
    missing = [str(p) for p in required if not p.is_file()]
    if missing:
        raise SystemExit("Missing production inputs:\n" + "\n".join(missing))
    OUT.mkdir(parents=True, exist_ok=True)
    wisp = Image.open(effects / "SteamWisp.png").convert("RGBA")
    candidate = required + [skin / "ModInfo.xml", ROOT / "AceTaffyMod/Data/CardInfo.xml",
                            ROOT / "src/TaffyVfx.cs", ROOT / "src/TaffyActions.cs",
                            Path(__file__).resolve()]
    inputs = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in candidate}
    rows = []
    for name, (action, *_rest) in SPEC.items():
        node = info.find(f"./ClothInfo/{action}")
        assert node is not None
        pivot = node.find("Pivot")
        assert pivot is not None
        p = float(pivot.get("pivot_x")), float(pivot.get("pivot_y"))
        ppu = float(node.get("quality"))
        assert ppu > 0
        actor = Image.open(skin / "ClothCustom" / f"{action}.png").convert("RGBA")
        mother = Image.open(effects / f"{name}.png").convert("RGBA")
        tile_w, tile_h = 520, 300
        sheet = Image.new("RGB", (tile_w * len(TIMES), tile_h * 2), (61, 64, 67))
        for row, facing in enumerate((-1, 1)):
            for col, t in enumerate(TIMES):
                frame = render(name, t, facing, 1.5, actor, mother, wisp, p, ppu)
                sheet.paste(frame.resize((tile_w, tile_h), Image.Resampling.LANCZOS), (col * tile_w, row * tile_h))
        filename = f"{name}_timeline.png"
        sheet.save(OUT / filename)
        rows.append(filename)
        # Native roll-factor range is 1..2. A single contact frame makes the
        # low/high size contract inspectable without duplicating 7 timestamps.
        rolls = Image.new("RGB", (tile_w * 2, tile_h * 2), (61, 64, 67))
        for row, facing in enumerate((-1, 1)):
            for col, roll in enumerate((1.0, 2.0)):
                frame = render(name, 150, facing, roll, actor, mother, wisp, p, ppu)
                rolls.paste(frame.resize((tile_w, tile_h), Image.Resampling.LANCZOS), (col * tile_w, row * tile_h))
        filename = f"{name}_roll_bounds.png"
        rolls.save(OUT / filename)
        rows.append(filename)
    (OUT / "manifest.json").write_text(json.dumps({"kind": "source-grounded simulation, not Unity/gameplay capture", "times_ms": TIMES, "roll_bounds": [1.0, 2.0], "calibrated_ppu": PPU, "forward_offset_world": FORWARD, "inputs_sha256": inputs, "sheets": rows}, indent=2), encoding="utf-8")
    print(f"Rendered {len(rows)} compact simulation sheets at {OUT}")


if __name__ == "__main__":
    main()
