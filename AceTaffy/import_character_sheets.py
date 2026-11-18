"""Deterministically import aligned guest/projection character sheets.

The legacy 3x3 path is preserved. The slender-v2 path detects row gaps from the
guest alpha mask, uses three fixed columns and one pixel scale for all actions.
Neither path edits the subject or paints out artifacts.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
DEST = ROOT / "AceTaffyMod/Resource/CharacterSkin"
ACTIONS = ("Default", "Move", "Slash", "Penetrate", "Hit", "Guard", "Evade", "Damaged", "Special")
LEGACY_CONTENT_SIZE = 344
SLENDER_SCALE = 0.67
SLENDER_GROUND_Y = 393
SLENDER_X_OFFSET = -20
LAYERED_BODY_SCALE = 0.50
LAYERED_HEAD_SCALE = 0.13
LAYERED_GROUND_Y = 390
LAYERED_NATIVE_HEAD_OFFSET_Y = 37  # Velia native-head preview: XML marker 37 px above neck_at
# Manually inspected source coordinates. These are neck roots, not face centers.
LAYERED_NECKS = ((219, 91), (170, 101), (238, 117),
                 (238, 77), (214, 70), (221, 72),
                 (259, 82), (286, 75), (211, 75))
LAYERED_ANGLES = (0, 16, -8, 4, 4, 8, 12, -17, 3)
HEAD_ANCHOR = (625, 925)  # source chin point; no bare-neck connector
HEAD_CROWN_Y = 286         # cranial cap, excluding forelock/goggles
HEAD_CHIN_Y = 925
HEAD_CORE_X = (318, 975)  # excludes short side locks
HEAD_FILES = {"Normal": "head.png", "Attack": "head_attack.png", "Hurt": "head_hurt.png"}
HEAD_STATE_BY_ACTION = {
    "Default": "Normal", "Move": "Normal",
    "Slash": "Attack", "Penetrate": "Attack", "Hit": "Attack",
    "Guard": "Attack", "Evade": "Attack", "Special": "Attack",
    "Damaged": "Hurt",
}

def read_rgba(path: Path, expected_size=None):
    if not path.is_file():
        raise FileNotFoundError(path)
    with Image.open(path) as opened:
        if opened.mode != "RGBA":
            raise ValueError(f"{path}: expected RGBA PNG, got {opened.mode}")
        image = opened.copy()
    if expected_size and image.size != expected_size:
        raise ValueError(f"{path}: {image.size} differs from guest sheet {expected_size}")
    if image.width % 3:
        raise ValueError(f"{path}: width must be divisible by 3")
    return image

def write_pair(guest, projection):
    for skin, frames in (("AceTaffy", guest), ("AceTaffy_Projection", projection)):
        target = DEST / skin / "ClothCustom"
        target.mkdir(parents=True, exist_ok=True)
        for action, canvas in frames.items():
            canvas.save(target / f"{action}.png")

def legacy_frames(sheet):
    if sheet.height % 3:
        raise ValueError("legacy sheet height must be divisible by 3")
    cell_w, cell_h = sheet.width // 3, sheet.height // 3
    scale = min(LEGACY_CONTENT_SIZE / cell_w, LEGACY_CONTENT_SIZE / cell_h)
    scaled_size = (round(cell_w * scale), round(cell_h * scale))
    frames = {}
    for index, action in enumerate(ACTIONS):
        x, y = (index % 3) * cell_w, (index // 3) * cell_h
        tile = sheet.crop((x, y, x + cell_w, y + cell_h))
        if not tile.getchannel("A").getbbox():
            raise ValueError(f"{action} cell is transparent")
        tile = tile.resize(scaled_size, Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.alpha_composite(tile, ((512 - tile.width) // 2, (512 - tile.height) // 2))
        frames[action] = canvas
    return frames

def opaque_runs(sheet, threshold=64):
    alpha = sheet.getchannel("A")
    filled = [y for y in range(sheet.height) if alpha.crop((0, y, sheet.width, y + 1)).getextrema()[1] > threshold]
    runs = []
    for y in filled:
        if not runs or y > runs[-1][-1] + 1:
            runs.append([y, y])
        else:
            runs[-1][1] = y
    return [(lo, hi) for lo, hi in runs if hi - lo >= 100]

def slender_layout(guest):
    runs = opaque_runs(guest)
    if len(runs) != 3:
        raise ValueError(f"slender sheet needs 3 distinct opaque rows, found {runs}")
    boundaries = [0, (runs[0][1] + runs[1][0]) // 2, (runs[1][1] + runs[2][0]) // 2, guest.height]
    cell_w = guest.width // 3
    # Each row has its own placement, while all nine figures share one scale.
    y_offsets = [round(SLENDER_GROUND_Y - (hi + 1 - boundaries[i]) * SLENDER_SCALE)
                 for i, (_, hi) in enumerate(runs)]
    x_offset = round(256 - cell_w * SLENDER_SCALE / 2 + SLENDER_X_OFFSET)
    default_height = round((runs[0][1] - runs[0][0] + 1) * SLENDER_SCALE)
    if not 220 <= default_height <= 285:
        raise ValueError(f"Default opaque height {default_height}px is outside LoR-like 220..285px")
    return runs, boundaries, y_offsets, x_offset, default_height

def ensure_gutters(sheet, boundaries):
    alpha = sheet.getchannel("A")
    for y in boundaries[1:-1]:
        if alpha.crop((0, y - 5, sheet.width, y + 5)).getextrema()[1] > 64:
            raise ValueError(f"opaque art crosses row boundary y={y}")
    cell_w = sheet.width // 3
    for x in (cell_w, 2 * cell_w):
        if alpha.crop((x - 5, 0, x + 5, sheet.height)).getextrema()[1] > 64:
            raise ValueError(f"opaque art crosses column boundary x={x}")

def slender_frames(sheet, boundaries, y_offsets, x_offset):
    cell_w = sheet.width // 3
    frames = {}
    for index, action in enumerate(ACTIONS):
        row, col = divmod(index, 3)
        top, bottom = boundaries[row], boundaries[row + 1]
        tile = sheet.crop((col * cell_w, top, (col + 1) * cell_w, bottom))
        if not tile.getchannel("A").point(lambda a: 255 if a > 64 else 0).getbbox():
            raise ValueError(f"{action} cell has no opaque subject")
        tile = tile.resize((round(tile.width * SLENDER_SCALE), round(tile.height * SLENDER_SCALE)), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        if x_offset < 0 or y_offsets[row] < 0 or x_offset + tile.width > 512 or y_offsets[row] + tile.height > 512:
            raise ValueError(f"{action} scaled cell does not fit 512 canvas")
        canvas.alpha_composite(tile, (x_offset, y_offsets[row]))
        frames[action] = canvas
    return frames

def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def layered_frames(body, heads):
    runs = opaque_runs(body)
    if len(runs) != 3:
        raise ValueError(f"layered body sheet needs 3 distinct opaque rows, found {runs}")
    boundaries = [0, (runs[0][1] + runs[1][0]) // 2,
                  (runs[1][1] + runs[2][0]) // 2, body.height]
    ensure_gutters(body, boundaries)
    cell_w = body.width // 3
    x_offset = round(256 - cell_w * LAYERED_BODY_SCALE / 2)
    y_offsets = [round(LAYERED_GROUND_Y - (hi + 1 - boundaries[row]) * LAYERED_BODY_SCALE)
                 for row, (_, hi) in enumerate(runs)]
    normal = heads["Normal"]
    normal_bbox = normal.getchannel("A").point(lambda a: 255 if a > 64 else 0).getbbox()
    for state, head in heads.items():
        if head.size != normal.size:
            raise ValueError(f"{state} head canvas {head.size} differs from Normal {normal.size}")
        bbox = head.getchannel("A").point(lambda a: 255 if a > 64 else 0).getbbox()
        if not bbox or any(abs(a - b) > 25 for a, b in zip(normal_bbox, bbox)):
            raise ValueError(f"{state} head alpha envelope drifts from Normal registration: {bbox}")
    scaled_heads = {
        state: head.resize((round(head.width * LAYERED_HEAD_SCALE),
                            round(head.height * LAYERED_HEAD_SCALE)), Image.Resampling.LANCZOS)
        for state, head in heads.items()
    }
    anchor_scaled = (round(HEAD_ANCHOR[0] * LAYERED_HEAD_SCALE),
                     round(HEAD_ANCHOR[1] * LAYERED_HEAD_SCALE))
    head_h = (HEAD_CHIN_Y - HEAD_CROWN_Y) * LAYERED_HEAD_SCALE
    if not 0.85 <= (HEAD_CORE_X[1] - HEAD_CORE_X[0]) / (HEAD_CHIN_Y - HEAD_CROWN_Y) <= 1.05:
        raise ValueError("head source core width/height diverges from native reference")
    guest_frames, projection_frames, motion_info = {}, {}, {}
    for index, action in enumerate(ACTIONS):
        row, col = divmod(index, 3)
        top, bottom = boundaries[row], boundaries[row + 1]
        tile = body.crop((col * cell_w, top, (col + 1) * cell_w, bottom))
        if not tile.getchannel("A").point(lambda a: 255 if a > 64 else 0).getbbox():
            raise ValueError(f"{action} body cell is empty")
        tile = tile.resize((round(tile.width * LAYERED_BODY_SCALE),
                            round(tile.height * LAYERED_BODY_SCALE)), Image.Resampling.LANCZOS)
        projection = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        projection.alpha_composite(tile, (x_offset, y_offsets[row]))
        nx = round(x_offset + LAYERED_NECKS[index][0] * LAYERED_BODY_SCALE)
        ny = round(y_offsets[row] + LAYERED_NECKS[index][1] * LAYERED_BODY_SCALE)
        state = HEAD_STATE_BY_ACTION[action]
        head_layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        head_layer.alpha_composite(scaled_heads[state], (nx - anchor_scaled[0], ny - anchor_scaled[1]))
        if LAYERED_ANGLES[index]:
            head_layer = head_layer.rotate(LAYERED_ANGLES[index], Image.Resampling.BICUBIC,
                                           center=(nx, ny))
        # Native heads sit above the body renderer; this also hides the
        # headless artwork's short skin-colored neck stub behind the face.
        guest = Image.alpha_composite(projection, head_layer)
        projection_frames[action] = projection
        guest_frames[action] = guest
        guest_bbox = guest.getchannel("A").point(lambda a: 255 if a > 128 else 0).getbbox()
        body_bbox = projection.getchannel("A").point(lambda a: 255 if a > 128 else 0).getbbox()
        if not guest_bbox or not body_bbox:
            raise ValueError(f"{action} imported empty")
        foot_y = body_bbox[3]
        foot_bottom_margin = 512 - foot_y
        # Native Bada Default sole is +0.06 world units above the character root.
        pivot_y = 2 * (foot_bottom_margin - 3) - 512
        head_x = 2 * (nx - 256)
        head_y = 2 * (256 - ny) - pivot_y
        projection_head_y = head_y + 2 * LAYERED_NATIVE_HEAD_OFFSET_Y
        motion_info[action] = {
            "source_row": row, "source_column": col,
            "head_state": state,
            "neck_source_xy": list(LAYERED_NECKS[index]),
            "neck_canvas_xy": [nx, ny], "angle_degrees": LAYERED_ANGLES[index],
            "pivot_y": pivot_y, "head_xml_xy": [head_x, head_y],
            "projection_head_canvas_xy": [nx, ny - LAYERED_NATIVE_HEAD_OFFSET_Y],
            "projection_head_xml_xy": [head_x, projection_head_y],
            "guest_bbox_alpha128": list(guest_bbox), "projection_bbox_alpha128": list(body_bbox),
            "sole_root_y_units": round((foot_bottom_margin - (pivot_y + 512) / 2) / 50, 4),
        }
    default = motion_info["Default"]
    body_h = default["projection_bbox_alpha128"][3] - default["neck_canvas_xy"][1] + (
        HEAD_ANCHOR[1] - HEAD_CHIN_Y) * LAYERED_HEAD_SCALE
    total_in_heads = 1 + body_h / head_h
    if not 2.70 <= total_in_heads <= 3.10:
        raise ValueError(f"Default anatomy is {total_in_heads:.3f} heads, outside native range")
    return guest_frames, projection_frames, {
        "variant": "layered-native", "body_scale": LAYERED_BODY_SCALE,
        "head_state_by_action": HEAD_STATE_BY_ACTION,
        "head_scale": LAYERED_HEAD_SCALE, "head_anchor_source_xy": list(HEAD_ANCHOR),
        "native_head_xml_marker_above_collar_px": LAYERED_NATIVE_HEAD_OFFSET_Y,
        "head_crown_y": HEAD_CROWN_Y, "head_chin_y": HEAD_CHIN_Y,
        "head_core_x": list(HEAD_CORE_X), "head_core_width_to_height":
            round((HEAD_CORE_X[1] - HEAD_CORE_X[0]) / (HEAD_CHIN_Y - HEAD_CROWN_Y), 3),
        "default_head_height_px": round(head_h, 2),
        "default_chin_to_sole_px": round(body_h, 2),
        "default_total_in_head_heights": round(total_in_heads, 3),
        "row_runs": runs, "row_boundaries": boundaries,
        "body_x_offset": x_offset, "body_y_offsets": y_offsets,
        "motions": motion_info,
    }

def write_layered_xml(manifest):
    for skin, enabled in (("AceTaffy", "False"), ("AceTaffy_Projection", "True")):
        lines = ['<?xml version="1.0" encoding="utf-8"?>',
                 '<ModInfo Extended="true">', '  <ClothInfo>', f'    <Name>{skin}</Name>']
        for action in ACTIONS:
            info = manifest["motions"][action]
            x, y = info["projection_head_xml_xy" if enabled == "True" else "head_xml_xy"]
            lines.append(
                f'    <{action} size_x="512" size_y="512" quality="50">'
                f'<Direction>Front</Direction><Pivot pivot_x="0" pivot_y="{info["pivot_y"]}" />'
                f'<Head head_x="{x}" head_y="{y}" rotation="{info["angle_degrees"]}" '
                f'head_enable="{enabled}" /></{action}>')
        lines.extend(('  </ClothInfo>', '</ModInfo>', ''))
        (DEST / skin / "ModInfo.xml").write_text("\n".join(lines), encoding="utf-8")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--variant", choices=("legacy", "slender-v2", "layered-native"), default="legacy")
    parser.add_argument("--source-dir", type=Path, help="folder with guest-sheet.png and projection-sheet.png")
    parser.add_argument("--inspect-only", action="store_true", help="analyze guest geometry without importing")
    args = parser.parse_args()
    source = args.source_dir or ROOT / "ArtSource/Character" / (
        "slender-v2" if args.variant in ("slender-v2", "layered-native") else "")
    if args.variant == "layered-native":
        body_path = source / "body-sheet.png"
        body = read_rgba(body_path)
        head_paths = {state: source / filename for state, filename in HEAD_FILES.items()}
        heads = {state: read_rgba(path) for state, path in head_paths.items()}
        guest_frames, projection_frames, manifest = layered_frames(body, heads)
        manifest["sources"] = {
            "body_sheet": {"path": str(body_path.relative_to(ROOT)).replace("\\", "/"),
                           "sha256": sha256(body_path)},
            "heads": {state: {"path": str(path.relative_to(ROOT)).replace("\\", "/"),
                               "sha256": sha256(path)} for state, path in head_paths.items()},
        }
        print(f"layered-native Default={manifest['default_total_in_head_heights']:.3f} heads "
              f"head={manifest['default_head_height_px']:.1f}px body={manifest['default_chin_to_sole_px']:.1f}px")
        if args.inspect_only:
            return
        write_pair(guest_frames, projection_frames)
        write_layered_xml(manifest)
        manifest_path = ROOT / "ArtSource/Character/slender-v2/layered-runtime-manifest.json"
        for skin in ("AceTaffy", "AceTaffy_Projection"):
            manifest.setdefault("outputs", {})[skin] = {
                action: sha256(DEST / skin / "ClothCustom" / f"{action}.png") for action in ACTIONS}
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Imported layered skins and wrote {manifest_path.relative_to(ROOT)}")
        return
    guest = read_rgba(source / "guest-sheet.png")

    if args.variant == "slender-v2":
        runs, bounds, y_offsets, x_offset, height = slender_layout(guest)
        ensure_gutters(guest, bounds)
        print(f"rows={runs} boundaries={bounds} x={x_offset} y={y_offsets} scale={SLENDER_SCALE} DefaultHeight={height}px")
        if args.inspect_only:
            return
        projection = read_rgba(source / "projection-sheet.png", guest.size)
        ensure_gutters(projection, bounds)
        guest_frames = slender_frames(guest, bounds, y_offsets, x_offset)
        projection_frames = slender_frames(projection, bounds, y_offsets, x_offset)
    else:
        if args.inspect_only:
            print(f"legacy sheet={guest.size} cell={guest.width // 3}x{guest.height // 3}")
            return
        projection = read_rgba(source / "projection-sheet.png", guest.size)
        guest_frames, projection_frames = legacy_frames(guest), legacy_frames(projection)

    write_pair(guest_frames, projection_frames)
    print("Imported nine aligned guest and projection motion pairs at 512x512")

if __name__ == "__main__":
    main()
