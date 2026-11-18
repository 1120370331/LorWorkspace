"""Compile imagegen's device and qualifier layers into BaseMod status icons."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
MASTERS = ROOT / 'ArtSource/Icons/inventions-v1'
OUTPUT = ROOT / 'AceTaffyMod/Resource/ArtWork'
KINDS = ('mechanism', 'chain', 'defense', 'destruction', 'energy')
QUALIFIERS = ('odd', 'weird', 'fragile', 'mysterious', 'super')


def layer(name, size):
    image = Image.open(MASTERS / f'{name}.png').convert('RGBA')
    alpha = image.getchannel('A')
    if alpha.getextrema()[0] != 0:
        raise ValueError(f'{name}: source lacks transparency')
    visible = alpha.point(lambda a: 255 if a > 16 else 0).getbbox()
    if visible is None:
        raise ValueError(f'{name}: source is empty')
    image = image.crop(visible)
    image.thumbnail((size, size), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (size, size))
    canvas.alpha_composite(image, ((size-image.width)//2, (size-image.height)//2))
    return canvas


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    icons = {}
    progress = Image.new('RGBA', (128, 128))
    progress.alpha_composite(layer('progress', 112), (8, 8))
    icons['AceTaffy_InventionProgress'] = progress
    for q, qualifier in enumerate(QUALIFIERS):
        for k, kind in enumerate(KINDS):
            icon = Image.new('RGBA', (128, 128))
            icon.alpha_composite(layer(kind, 108), (4, 16))
            icon.alpha_composite(layer(qualifier, 48), (78, 2))
            icons[f'AceTaffy_Invention_{q}_{k}'] = icon
    for key, icon in icons.items():
        icon.save(OUTPUT / f'{key}.png', optimize=True)
    # Contact sheet is a build preview, not a screenshot of the game.
    sheet = Image.new('RGB', (1000, 720), '#26292f')
    draw = ImageDraw.Draw(sheet)
    for k, kind in enumerate(KINDS):
        draw.text((150+k*170, 12), kind, fill='white')
    for q, qualifier in enumerate(QUALIFIERS):
        draw.text((10, 64+q*128), qualifier, fill='white')
        for k in range(5):
            icon = icons[f'AceTaffy_Invention_{q}_{k}']
            x,y = 130+k*170,36+q*128
            sheet.paste(icon.resize((80,80),Image.Resampling.LANCZOS),(x,y),icon.resize((80,80),Image.Resampling.LANCZOS))
            for offset,size in ((92,32),(132,24)):
                small=icon.resize((size,size),Image.Resampling.LANCZOS)
                sheet.paste(small,(x+offset,y+40),small)
    preview=ROOT/'Verification/Reception/invention-icons.png'
    sheet.save(preview)
    manifest={'layout':{'canvas':128,'device_size':108,'device_xy':[4,16],'qualifier_size':48,'qualifier_xy':[78,2]},
              'kinds':KINDS,'qualifiers':QUALIFIERS,
              'outputs':{key:hashlib.sha256((OUTPUT/f'{key}.png').read_bytes()).hexdigest() for key in icons}}
    (MASTERS/'build-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print(f'Compiled {len(icons)} transparent 128x128 status icons; {preview}')


if __name__ == '__main__':
    main()
