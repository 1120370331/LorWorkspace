from pathlib import Path
import hashlib,json,sys
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parent
version=sys.argv[1] if len(sys.argv)>1 else "v1"
im=Image.open(ROOT/"keyed"/("SeaFarHit-master-rgba-"+version+".png")).convert("RGBA")
a=np.array(im)[...,3]
gap=(535,625) if version=="v1" else (540,580)
assert not np.any(a[gap[0]:gap[1]]), "FarHit and splash are not independently separable"
manifest={"composition":"SeaFarHit-master-rgba-"+version+".png","transparent_separation_source_y":list(gap),"elements":[]}
for name,region in [("SeaFarHit-body",(0,0,im.width,580)),("SeaFarHit-splash",(0,580,im.width,im.height))]:
    part=im.crop(region)
    box=part.getbbox()
    padded=(max(0,box[0]-12),max(0,box[1]-12),min(part.width,box[2]+12),min(part.height,box[3]+12))
    crop=part.crop(padded)
    out=ROOT/"keyed"/(name+"-rgba-"+version+".png")
    crop.save(out)
    q=Image.new("RGB",(1200,800))
    d=ImageDraw.Draw(q)
    sc=crop.copy()
    sc.thumbnail((1100,320),Image.Resampling.LANCZOS)
    for i,color in enumerate([(24,27,36),(223,221,213)]):
        d.rectangle((0,400*i,1200,400*(i+1)),fill=color)
        q.paste(sc,((1200-sc.width)//2,400*i+60),sc)
        d.text((20,400*i+20),name+" | "+("DARK" if i==0 else "LIGHT"),fill=(170,176,188) if i==0 else (54,57,65))
    qa=ROOT/"qa"/(name+"-light-dark-"+version+".png")
    q.save(qa)
    source_box=[region[0]+padded[0],region[1]+padded[1],region[0]+padded[2],region[1]+padded[3]]
    manifest["elements"].append({"name":name,"path":str(out),"dimensions":list(crop.size),"crop_source_box":source_box,"sha256":hashlib.sha256(out.read_bytes()).hexdigest(),"qa":str(qa)})
(ROOT/"qa"/("SeaFarHit-elements-"+version+".json")).write_text(json.dumps(manifest,indent=2),encoding="utf-8")
print(json.dumps(manifest,indent=2))
