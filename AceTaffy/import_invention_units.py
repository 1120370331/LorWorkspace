"""Compile the 25 imagegen invention skins, preserving source art and alpha."""
import hashlib
import json
from pathlib import Path
from xml.etree import ElementTree as ET
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT/'ArtSource/Character/invention-units'
RESOURCE=ROOT/'AceTaffyMod/Resource'
ACTIONS=('Default','Move','Slash','Penetrate','Hit','Guard','Evade','Damaged','Special')
HEIGHTS=(195,175,185,185,180)

def crop_visible(path):
    image=Image.open(path).convert('RGBA')
    if image.getchannel('A').getextrema()[0]!=0:raise ValueError(f'Opaque source: {path}')
    box=image.getchannel('A').point(lambda a:255 if a>16 else 0).getbbox()
    if box is None:raise ValueError(f'Empty source: {path}')
    return image.crop(box)

def main():
    units=json.loads((SOURCE/'unit-manifest.json').read_text(encoding='utf-8'))['units']
    records=[]
    sheet=Image.new('RGB',(1400,1350),'#25292f');draw=ImageDraw.Draw(sheet)
    for u in units:
        q,k=u['qualifier'],u['kind'];name=u['skin']
        raw=crop_visible(SOURCE/f'unit_{q}_{k}.png')
        image=raw.copy()
        scale=1.12 if q==4 else .93 if q==2 else 1
        image.thumbnail((round(270*scale),round(HEIGHTS[k]*scale)),Image.Resampling.LANCZOS)
        foot=image.getchannel('A').point(lambda a:255 if a>64 else 0).crop((0,round(image.height*.85),image.width,image.height)).getbbox()
        foot_x=(foot[0]+foot[2])/2 if foot else image.width/2
        xy=(round(256-foot_x),393-image.height)
        canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(image,xy)
        skin=RESOURCE/'CharacterSkin'/name;(skin/'ClothCustom').mkdir(parents=True,exist_ok=True)
        info=ET.Element('ModInfo',Extended='true');cloth=ET.SubElement(info,'ClothInfo');ET.SubElement(cloth,'Name').text=name
        for action in ACTIONS:
            canvas.save(skin/'ClothCustom'/f'{action}.png',optimize=True)
            node=ET.SubElement(cloth,action,size_x='512',size_y='512',quality='50')
            ET.SubElement(node,'Direction').text='Front'
            ET.SubElement(node,'Pivot',pivot_x='0',pivot_y='-280')
            ET.SubElement(node,'Head',head_x='0',head_y='0',rotation='0',head_enable='False')
        ET.indent(info);ET.ElementTree(info).write(skin/'ModInfo.xml',encoding='utf-8',xml_declaration=True)
        x,y=k*280,q*270
        sample=canvas.crop((76,133,436,413));sample.thumbnail((260,228),Image.Resampling.LANCZOS)
        sheet.paste(sample,(x+(280-sample.width)//2,y+20),sample)
        draw.text((x+10,y+246),f'{q}/{k}  HP {u["hp"]} / BP {u["break"]}',fill='white')
        records.append({'skin':name,'canvas_xy':xy,'visible_size':image.size,'ground_y':393,
                        'sha256':hashlib.sha256((skin/'ClothCustom/Default.png').read_bytes()).hexdigest()})
        if q==0:
            artwork=Image.new('RGBA',(512,384));raw.thumbnail((450,330),Image.Resampling.LANCZOS)
            artwork.alpha_composite(raw,((512-raw.width)//2,(384-raw.height)//2))
            artwork.save(RESOURCE/'CombatPageArtwork'/f'AceTaffy_InventionKind_{k}.png',optimize=True)
    for q,name in enumerate(('odd','weird','fragile','mysterious','super')):
        badge=crop_visible(ROOT/'ArtSource/Icons/inventions-v1'/f'{name}.png');badge.thumbnail((112,112),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(128,128));canvas.alpha_composite(badge,((128-badge.width)//2,(128-badge.height)//2))
        canvas.save(RESOURCE/'ArtWork'/f'AceTaffy_InventionAura_{q}.png',optimize=True)
    preview=ROOT/'Verification/Reception/invention-unit-sheet.png';sheet.save(preview)
    (SOURCE/'build-manifest.json').write_text(json.dumps({'skins':records},indent=2)+'\n',encoding='utf-8')
    print(f'Compiled {len(records)} independent skins, five card artworks and five aura icons: {preview}')

if __name__=='__main__':main()
