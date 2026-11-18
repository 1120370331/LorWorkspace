"""Same-root, same-world-scale Default comparison for native and AceTaffy skins."""
from pathlib import Path
from xml.etree import ElementTree as ET

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / "ArtSource/Character/native-reference"
SKIN = ROOT / "AceTaffyMod/Resource/CharacterSkin/AceTaffy"
WORLD_CANVAS = (800, 650)
WORLD_ROOT = (400, 560)  # native reference renderer uses 100 px/world unit
PANEL = (400, 650)

def taffy_at_native_scale():
    xml = ET.parse(SKIN / "ModInfo.xml").getroot().find("ClothInfo/Default")
    pivot = xml.find("Pivot")
    px = 256 + float(pivot.attrib["pivot_x"]) / 2
    py = 256 - float(pivot.attrib["pivot_y"]) / 2
    sprite = Image.open(SKIN / "ClothCustom/Default.png").convert("RGBA")
    if sprite.size != (512, 512):
        raise ValueError("AceTaffy Default must have a 512x512 sprite canvas")
    scaled = sprite.resize((1024, 1024), Image.Resampling.BICUBIC)
    canvas = Image.new("RGBA", WORLD_CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(scaled, (round(WORLD_ROOT[0] - px * 2),
                                    round(WORLD_ROOT[1] - py * 2)))
    return canvas

def main():
    output = Image.new("RGB", (PANEL[0] * 3, PANEL[1]), "#30343b")
    draw = ImageDraw.Draw(output)
    for index, (name, image) in enumerate((
        ("Malkuth (native)", Image.open(NATIVE / "Malkuth_Default_full.png").convert("RGBA")),
        ("Bada (native)", Image.open(NATIVE / "Bada_Default_full.png").convert("RGBA")),
        ("AceTaffy (50 PPU)", taffy_at_native_scale()),
    )):
        crop = image.crop((200, 0, 600, 650))
        x = index * PANEL[0]
        output.paste(crop, (x, 0), crop)
        draw.rectangle((x + 3, 3, x + 210, 24), fill="#1a1d22")
        draw.text((x + 8, 7), name, fill="white")
        draw.line((x, WORLD_ROOT[1], x + PANEL[0] - 1, WORLD_ROOT[1]), fill="#6a6d72", width=1)
    path = ROOT / "Verification/native-vs-taffy-default.png"
    output.save(path)
    print(path)

if __name__ == "__main__":
    main()
