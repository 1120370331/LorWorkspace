"""Compile the generated stationary workshop artwork into BaseMod's 512px skin."""
from pathlib import Path
from xml.etree import ElementTree as ET
from PIL import Image

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'ArtSource/Character/steam-workshop/workshop.png'
ACTIONS = ('Default', 'Move', 'Slash', 'Penetrate', 'Hit', 'Guard', 'Evade', 'Damaged', 'Special')
image = Image.open(SOURCE).convert('RGBA')
assert image.getchannel('A').getextrema()[0] == 0, 'Workshop must have real transparency'
# Generated antialiasing can leave nearly invisible pixels beyond the object.
# Ground against the visible silhouette rather than those transparent fringes.
visible = image.getchannel('A').point(lambda a: 255 if a > 16 else 0).getbbox()
image = image.crop(visible)
image.thumbnail((320, 255), Image.Resampling.LANCZOS)
canvas = Image.new('RGBA', (512, 512))
canvas.alpha_composite(image, ((512 - image.width) // 2, 393 - image.height))
skin = ROOT / 'AceTaffyMod/Resource/CharacterSkin/AceTaffy_Workshop'
(skin / 'ClothCustom').mkdir(parents=True, exist_ok=True)
info = ET.Element('ModInfo', Extended='true')
cloth = ET.SubElement(info, 'ClothInfo')
ET.SubElement(cloth, 'Name').text = 'AceTaffy_Workshop'
for action in ACTIONS:
    canvas.save(skin / f'ClothCustom/{action}.png')
    node = ET.SubElement(cloth, action, size_x='512', size_y='512', quality='50')
    ET.SubElement(node, 'Direction').text = 'Front'
    ET.SubElement(node, 'Pivot', pivot_x='0', pivot_y='-280')
    ET.SubElement(node, 'Head', head_x='0', head_y='0', rotation='0', head_enable='False')
ET.indent(info)
ET.ElementTree(info).write(skin / 'ModInfo.xml', encoding='utf-8', xml_declaration=True)
print(skin)
