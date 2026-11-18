"""Nine fixed-zoom neck/collar closeups from the current layered skin candidate."""
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "ArtSource/Character/slender-v2/layered-runtime-manifest.json"
SKINS = ROOT / "AceTaffyMod/Resource/CharacterSkin"
OUTPUT = ROOT / "Verification/neck-closeups"
ACTIONS = ("Default", "Move", "Slash", "Penetrate", "Hit", "Guard", "Evade", "Damaged", "Special")
SIDE = 320

def crop_on_gray(path, center):
    image = Image.open(path).convert("RGBA")
    x, y = center
    crop = image.crop((x - 90, y - 105, x + 90, y + 75))
    crop = crop.resize((SIDE, SIDE), Image.Resampling.NEAREST)
    background = Image.new("RGB", (SIDE, SIDE), "#30343b")
    background.paste(crop, (0, 0), crop)
    return background

def main():
    info = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if info.get("variant") != "layered-native":
        raise ValueError("Need current layered-native manifest")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    montage = Image.new("RGB", (SIDE * 6, SIDE * 3), "#30343b")
    draw = ImageDraw.Draw(montage)
    for index, action in enumerate(ACTIONS):
        center = info["motions"][action]["neck_canvas_xy"]
        guest = crop_on_gray(SKINS / "AceTaffy/ClothCustom" / f"{action}.png", center)
        projection = crop_on_gray(SKINS / "AceTaffy_Projection/ClothCustom" / f"{action}.png", center)
        pair = Image.new("RGB", (SIDE * 2, SIDE), "#30343b")
        pair.paste(guest, (0, 0))
        pair.paste(projection, (SIDE, 0))
        pair_draw = ImageDraw.Draw(pair)
        pair_draw.rectangle((3, 3, 290, 23), fill="#17191d")
        pair_draw.text((8, 7), f"{action} guest / headless projection", fill="white")
        pair.save(OUTPUT / f"{action}.png")
        ox, oy = (index % 3) * SIDE * 2, (index // 3) * SIDE
        montage.paste(pair, (ox, oy))
    result = ROOT / "Verification/neck-closeups.png"
    montage.save(result)
    print(result)

if __name__ == "__main__":
    main()
