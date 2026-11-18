"""Fixed-camera preparation portrait simulation from the native UI render path.

UICharacterRenderer uses Standing, a per-index camera render texture, and
height*0.005 scaling. This visual uses the same root and height 170 for each
actor; it is a simulation, not a screenshot from the user's game.
"""
import json
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw
from compare_native_character import taffy_at_native_scale

ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / "ArtSource/Character/native-reference"
MANIFEST = ROOT / "ArtSource/Character/slender-v2/layered-runtime-manifest.json"
VIEWPORT = (400, 500)
SOURCE_ROOT = (400, 560)
VIEW_ROOT = (200, 430)
HEIGHT = 170
UI_SCALE = HEIGHT * 0.005

def center(values):
    return (values[0] + values[1]) / 2

def actor_slot(source, shift_world_px=0):
    source = source.convert("RGBA")
    scaled = source.resize((round(source.width * UI_SCALE), round(source.height * UI_SCALE)),
                           Image.Resampling.BICUBIC)
    bg = Image.new("RGB", VIEWPORT, "#30343b")
    x = round(VIEW_ROOT[0] - SOURCE_ROOT[0] * UI_SCALE + shift_world_px)
    y = round(VIEW_ROOT[1] - SOURCE_ROOT[1] * UI_SCALE)
    bg.paste(scaled, (x, y), scaled)
    return bg

def main():
    native = json.loads((NATIVE / "measurements.json").read_text(encoding="utf-8"))["references"]
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    old_target = (center(native["Bada"]["landmarks"]["head_core_x"]) - 400 +
              center(native["Malkuth"]["landmarks"]["head_core_x"]) - 400) / 2
    target = 0  # UICharacterRenderer's native x=10*index slot anchor.
    taffy_head_center = (manifest["motions"]["Default"]["neck_canvas_xy"][0] +
                         (center(manifest["head_core_x"]) -
                          manifest["head_anchor_source_xy"][0]) * manifest["head_scale"])
    taffy_world_offset = (taffy_head_center - 256) * 2
    shape_delta = target - taffy_world_offset
    screen_delta = shape_delta * UI_SCALE
    old_screen_delta = (old_target - taffy_world_offset) * UI_SCALE
    frames = (
        ("Bada native", Image.open(NATIVE / "Bada_Default_full.png"), 0),
        ("Malkuth native", Image.open(NATIVE / "Malkuth_Default_full.png"), 0),
        ("Taffy before (old shift)", taffy_at_native_scale(), old_screen_delta),
        ("Taffy after", taffy_at_native_scale(), screen_delta),
    )
    output = Image.new("RGB", (VIEWPORT[0] * len(frames), VIEWPORT[1]), "#30343b")
    draw = ImageDraw.Draw(output)
    for i, (label, source, delta) in enumerate(frames):
        x = i * VIEWPORT[0]
        output.paste(actor_slot(source, delta), (x, 0))
        draw.line((x + VIEW_ROOT[0], 28, x + VIEW_ROOT[0], 480), fill="#666b73", width=1)
        draw.rectangle((x + 3, 3, x + 170, 23), fill="#17191d")
        draw.text((x + 8, 7), label, fill="white")
    preview = ROOT / "Verification/preparation-before-after.png"
    output.save(preview)
    cases = []
    for index, height in ((0, 170), (4, 200)):
        native_x = 10 * index
        aligned_x = native_x + shape_delta / 100 * (height * 0.005)
        repeated_x = native_x + shape_delta / 100 * (height * 0.005)
        restored_x = native_x
        if aligned_x != repeated_x or restored_x != native_x:
            raise AssertionError("alignment is not idempotent")
        cases.append((index, height, native_x, aligned_x, repeated_x, restored_x))
    skin_root = ROOT / "AceTaffyMod/Resource/CharacterSkin"
    hashes = {
        str(path.relative_to(ROOT)).replace("\\", "/"): hashlib.sha256(path.read_bytes()).hexdigest()
        for skin in ("AceTaffy", "AceTaffy_Projection")
        for path in [skin_root / skin / "ClothCustom/Default.png",
                     skin_root / skin / "ModInfo.xml"]
    }
    log = ROOT / "Verification/preparation-alignment.log"
    log.write_text(
        "UICharacterRenderer.SetCharacter -> ChangeMotion(Standing) -> cameraList[index].Render() "
        "-> UICharacterSlot / UIBattleSettingLibrarianInfoPanel RawImage\n"
        f"Native head center offsets at 100 PPU: Bada {center(native['Bada']['landmarks']['head_core_x'])-400:.2f}px; "
        f"Malkuth {center(native['Malkuth']['landmarks']['head_core_x'])-400:.2f}px; mean {old_target:.2f}px\n"
        f"Centering target: native slot root {target:.2f}px, not the reference actors' pose lean\n"
        f"AceTaffy baked Default head center offset: {taffy_world_offset:.2f}px at equivalent 100 PPU\n"
        f"Shape correction: {shape_delta:.2f}px = {shape_delta/100:.4f} Unity units before UI height scale\n"
        f"At height {HEIGHT}, UI scale={UI_SCALE:.3f}, actor local x shift={screen_delta:.2f}px = "
        f"{screen_delta/100:.4f} Unity units\n"
        "Cases (slot index, height, native x, aligned x, repeated SetCharacter x, switched-to-other-skin x):\n" +
        "".join(f"  {index}, {height}, {native_x:.4f}, {aligned_x:.4f}, {repeated_x:.4f}, {restored_x:.4f}\n"
                for index, height, native_x, aligned_x, repeated_x, restored_x in cases) +
        "Scope: exact AceTaffy package skin Default.png paths in UICharacterRenderer only; "
        "game battle roots and PNG/XML remain unchanged.\n" +
        "Frozen skin source SHA256:\n" +
        "".join(f"  {path}: {digest}\n" for path, digest in sorted(hashes.items())),
        encoding="utf-8",
    )
    print(preview)
    print(log)

if __name__ == "__main__":
    main()
