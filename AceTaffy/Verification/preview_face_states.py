"""Compare the three authored faces at one frozen Default body/neck transform."""
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/Character/slender-v2"
MANIFEST = SOURCE / "layered-runtime-manifest.json"
BODY = ROOT / "AceTaffyMod/Resource/CharacterSkin/AceTaffy_Projection/ClothCustom/Default.png"
HEADS = (("Normal", "head.png"), ("Attack", "head_attack.png"), ("Hurt", "head_hurt.png"))

def main():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if manifest.get("variant") != "layered-native":
        raise ValueError("Need layered-native candidate")
    source_records = manifest["sources"]["heads"]
    for state, path in HEADS:
        if state not in source_records or source_records[state]["path"] != f"ArtSource/Character/slender-v2/{path}":
            raise ValueError(f"Manifest head source mismatch for {state}")
    body = Image.open(BODY).convert("RGBA")
    nx, ny = manifest["motions"]["Default"]["neck_canvas_xy"]
    head_scale = manifest["head_scale"]
    ax, ay = (round(v * head_scale) for v in manifest["head_anchor_source_xy"])
    output = Image.new("RGB", (960, 320), "#30343b")
    draw = ImageDraw.Draw(output)
    for index, (state, filename) in enumerate(HEADS):
        head = Image.open(SOURCE / filename).convert("RGBA")
        head = head.resize((round(head.width * head_scale), round(head.height * head_scale)),
                           Image.Resampling.LANCZOS)
        frame = body.copy()
        frame.alpha_composite(head, (nx - ax, ny - ay))
        closeup = frame.crop((nx - 80, ny - 120, nx + 80, ny + 40))
        closeup = closeup.resize((320, 320), Image.Resampling.NEAREST)
        output.paste(closeup, (index * 320, 0), closeup)
        draw.rectangle((index * 320 + 3, 3, index * 320 + 100, 24), fill="#17191d")
        draw.text((index * 320 + 8, 7), state, fill="white")
    result = ROOT / "Verification/face-states.png"
    output.save(result)
    print(result)

if __name__ == "__main__":
    main()
