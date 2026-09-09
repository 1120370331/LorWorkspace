from pathlib import Path
import json, hashlib, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
name = sys.argv[1]
src = ROOT / "source" / (name + "-master-green-v1.png")
im = Image.open(src).convert("RGB")
rgb = np.asarray(im, dtype=np.float32) / 255.0
r,g,b = rgb[...,0],rgb[...,1],rgb[...,2]
edges = np.concatenate([rgb[:16].reshape(-1,3),rgb[-16:].reshape(-1,3),rgb[:,:16].reshape(-1,3),rgb[:,-16:].reshape(-1,3)])
key = np.median(edges,axis=0)
dominance = g - np.maximum(r,b)
key_diff = float(key[1]-max(key[0],key[2]))
# Color difference matte; blue/cyan/white and dark paint are retained.
# Tiny background codec variations are fully cleared without brightness-keying.
alpha = 1.0 - np.clip((dominance - 0.025) / max(0.01,key_diff-0.025),0,1)
bg = (dominance > 0.50) & (np.maximum(r,b) < 0.12)
alpha[bg] = 0.0
alpha[alpha < 0.015] = 0.0
alpha[alpha > 0.99] = 1.0
safe = np.maximum(alpha,1/255.0)
foreground = (rgb - (1-alpha)[...,None]*key[None,None,:])/safe[...,None]
foreground = np.clip(foreground,0,1)
# Despill any residual green excess while preserving cyan-white highlight values.
foreground[...,1] = np.minimum(foreground[...,1], np.maximum(foreground[...,0],foreground[...,2]))
foreground[alpha==0] = 0
rgba = np.dstack((foreground,alpha))
out = Image.fromarray(np.round(rgba*255).astype(np.uint8))
outpath = ROOT / "keyed" / (name+"-master-rgba-v1.png")
out.save(outpath)
bbox = out.getbbox()
crop_box = (max(0,bbox[0]-12),max(0,bbox[1]-12),min(im.width,bbox[2]+12),min(im.height,bbox[3]+12))
crop = out.crop(crop_box)
crop_path = ROOT / "keyed" / (name+"-crop-rgba-v1.png")
crop.save(crop_path)
w=1400
scaled = crop.copy()
scaled.thumbnail((w-60,520),Image.Resampling.LANCZOS)
h=scaled.height+80
qa=Image.new("RGB",(w,h*2),(0,0,0))
d=ImageDraw.Draw(qa)
for i,(label,color) in enumerate([("DARK / keyed RGBA",(24,27,36)),("LIGHT / keyed RGBA",(223,221,213))]):
    d.rectangle((0,h*i,w,h*(i+1)),fill=color)
    qa.paste(scaled,((w-scaled.width)//2,h*i+50),scaled)
    d.text((18,h*i+16),name+" | "+label,fill=(170,176,188) if i==0 else (54,57,65))
qpath=ROOT/"qa"/(name+"-light-dark-v1.png")
qa.save(qpath)
a8=np.asarray(out)[...,3]
stats={
  "asset":name,"source":str(src),"source_sha256":hashlib.sha256(src.read_bytes()).hexdigest(),
  "keyed":str(outpath),"keyed_sha256":hashlib.sha256(outpath.read_bytes()).hexdigest(),
  "dimensions":list(im.size),"crop":str(crop_path),"crop_sha256":hashlib.sha256(crop_path.read_bytes()).hexdigest(),
  "crop_box":list(crop_box),"crop_dimensions":list(crop.size),"alpha_bbox":list(bbox),
  "alpha_zero":int(np.sum(a8==0)),"alpha_partial":int(np.sum((a8>0)&(a8<255))),
  "alpha_opaque":int(np.sum(a8==255)),"key_median_rgb":(key*255).round().astype(int).tolist(),
  "green_dominant_visible_pixels_after_despill":int(np.sum((foreground[...,1]>np.maximum(foreground[...,0],foreground[...,2])+1/255)&(alpha>0))),
  "qa":str(qpath),"method":"green color-difference key, soft edge unmix, residual-green despill; never brightness key",
  "limitation":"Generated visual master and deterministic matte only; runtime timing/game acceptance belong to integration review."
}
(ROOT/"qa"/(name+"-stats-v1.json")).write_text(json.dumps(stats,indent=2),encoding="utf-8")
print(json.dumps(stats,indent=2))
