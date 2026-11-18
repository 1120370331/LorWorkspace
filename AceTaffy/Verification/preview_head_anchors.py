"""Render head anchor dots from ModInfo without changing any character art."""
from pathlib import Path
from xml.etree import ElementTree as ET

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SKIN = ROOT / "AceTaffyMod/Resource/CharacterSkin/AceTaffy_Projection"
ACTIONS = ("Default", "Move", "Slash", "Penetrate", "Hit", "Guard", "Evade", "Damaged", "Special")
TILE = 384

def render():
    cloth = ET.parse(SKIN / "ModInfo.xml").getroot().find("ClothInfo")
    sheet = Image.new("RGB", (TILE * 3, TILE * 3), "#30343b")
    draw = ImageDraw.Draw(sheet)
    for index, action in enumerate(ACTIONS):
        node = cloth.find(action)
        head = node.find("Head")
        pivot = node.find("Pivot")
        # Loader divides XML pivot/head coordinates by 1024/100, then creates
        # a 512 px sprite at 50 PPU. Hence each XML head unit is half a pixel.
        x = 256 + (float(pivot.attrib["pivot_x"]) + float(head.attrib["head_x"])) / 2
        y = 256 - (float(pivot.attrib["pivot_y"]) + float(head.attrib["head_y"])) / 2
        source = Image.open(SKIN / "ClothCustom" / f"{action}.png").convert("RGBA")
        source = source.resize((TILE, TILE), Image.Resampling.LANCZOS)
        ox, oy = (index % 3) * TILE, (index // 3) * TILE
        sheet.paste(source, (ox, oy), source)
        px, py = ox + x * TILE / 512, oy + y * TILE / 512
        draw.ellipse((px - 8, py - 8, px + 8, py + 8), fill="#ff3333", outline="white", width=2)
        draw.line((px - 15, py, px + 15, py), fill="white", width=2)
        draw.line((px, py - 15, px, py + 15), fill="white", width=2)
        draw.rectangle((ox + 2, oy + 2, ox + 175, oy + 22), fill="#24272b")
        draw.text((ox + 7, oy + 5), f"{action} ({x:.0f},{y:.0f})", fill="white")
    output = ROOT / "Verification/head-anchor-preview.png"
    sheet.save(output)
    print(output)

    guest = ROOT / "AceTaffyMod/Resource/CharacterSkin/AceTaffy/ClothCustom"
    guest_sheet = Image.new("RGB", (TILE * 3, TILE * 3), "#30343b")
    guest_draw = ImageDraw.Draw(guest_sheet)
    for index, action in enumerate(ACTIONS):
        source = Image.open(guest / f"{action}.png").convert("RGBA")
        source = source.resize((TILE, TILE), Image.Resampling.LANCZOS)
        ox, oy = (index % 3) * TILE, (index // 3) * TILE
        guest_sheet.paste(source, (ox, oy), source)
        guest_draw.rectangle((ox + 2, oy + 2, ox + 125, oy + 22), fill="#191c21")
        guest_draw.text((ox + 7, oy + 5), action, fill="white")
    guest_output = ROOT / "Verification/guest-dark-preview.png"
    guest_sheet.save(guest_output)
    print(guest_output)

if __name__ == "__main__":
    render()
