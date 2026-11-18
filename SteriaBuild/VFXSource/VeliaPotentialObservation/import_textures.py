"""Normalize saved original ImageGen brush tiles, preserve alpha, make review masks."""
import hashlib
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps

ROOT=Path(__file__).resolve().parent
ART=ROOT/'source_assets'

def fit(image,size,fill=.74):
    alpha=image.getchannel('A')
    bounds=alpha.point(lambda a:255 if a>4 else 0).getbbox()
    cut=image.crop(bounds)
    cut.thumbnail((round(size*fill),round(size*fill)),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(size,size))
    out.alpha_composite(cut,((size-cut.width)//2,(size-cut.height)//2))
    return out

def main():
    raw=Image.open(ART/'raw/flow-brush-raw.png').convert('RGBA')
    atlas=Image.new('RGBA',(1024,1024))
    for y in range(2):
        for x in range(2):
            tile=raw.crop((x*raw.width//2,y*raw.height//2,(x+1)*raw.width//2,(y+1)*raw.height//2))
            atlas.alpha_composite(fit(tile,512),(x*512,y*512))
    atlas.save(ART/'flow-brush-atlas.png')
    fit(Image.open(ART/'raw/mist-raw.png').convert('RGBA'),512).save(ART/'mist-breakup.png')
    # A mathematical four-ray point: this is a tiny light-mask primitive, not
    # a replacement for the authored organic brush/mist artwork.
    glint=Image.new('RGBA',(256,256));pixels=[]
    for y in range(256):
        for x in range(256):
            u=(x-127.5)/128;v=(y-127.5)/128
            vertical=math.exp(-abs(u)/.017-abs(v)/(.25 if v<0 else .19))
            horizontal=math.exp(-abs(v)/.017-abs(u)/(.18 if u<0 else .22))
            halo=math.exp(-(u*u+v*v)/.012)*.18
            a=min(1,(vertical+horizontal)*.65+halo)
            if abs(u)>.76 or abs(v)>.76:a=0
            warm=min(1,math.sqrt(u*u+v*v)*3)
            pixels.append((255,round(250-24*warm),round(232-89*warm),round(255*a)))
    glint.putdata(pixels);glint.save(ART/'glint.png')
    tiles=[('Flow '+str(i+1),atlas.crop((i%2*512,i//2*512,i%2*512+512,i//2*512+512))) for i in range(4)]
    tiles += [('Mist',Image.open(ART/'mist-breakup.png')),('Glint',glint)]
    sheet=Image.new('RGB',(1020,1170),(37,40,47));draw=ImageDraw.Draw(sheet)
    headings=['Original / black','Original / mid-gray','Alpha mask','Light x alpha','64 px sample']
    for c,title in enumerate(headings):draw.text((c*204+8,8),title,fill='white')
    for r,(name,im) in enumerate(tiles):
        thumb=im.resize((168,168),Image.Resampling.LANCZOS)
        for c in range(5):
            x=c*204+18;y=r*188+38
            if c<2:
                tile=Image.new('RGBA',(168,168),(0,0,0,255) if c==0 else (100,100,100,255));tile.alpha_composite(thumb)
            elif c==2:tile=Image.merge('RGB',(thumb.getchannel('A'),)*3)
            elif c==3:
                rgb=thumb.convert('RGB');luma=ImageOps.grayscale(rgb)
                from PIL import ImageChops
                luma=ImageChops.multiply(luma,thumb.getchannel('A'));tile=Image.merge('RGB',(luma,)*3)
            else:
                tile=Image.new('RGBA',(168,168),(35,38,43,255));tile.alpha_composite(im.resize((64,64),Image.Resampling.LANCZOS),(52,52))
            sheet.paste(tile.convert('RGB'),(x,y))
        draw.text((8,r*188+22),name,fill='white')
    sheet.save(ART/'texture-contact-sheet.png')
    paths=['raw/flow-brush-raw.png','raw/mist-raw.png','flow-brush-atlas.png','mist-breakup.png','glint.png']
    report={'generator':'built-in image_gen for brush and mist; deterministic four-ray light-mask for glint',
        'processing':'per-tile alpha-bound crop, uniform downsample into centered 74% square with transparent padding; alpha preserved',
        'files':{p:{'sha256':hashlib.sha256((ART/p).read_bytes()).hexdigest(),'size':Image.open(ART/p).size} for p in paths}}
    (ART/'provenance.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
