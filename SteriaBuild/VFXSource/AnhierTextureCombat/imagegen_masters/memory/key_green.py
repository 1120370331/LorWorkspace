"""Deterministic color-dominance chroma matting; does not brightness-key dark pigment."""
from pathlib import Path
import argparse,json,hashlib,shutil
import numpy as np
from PIL import Image,ImageDraw
from scipy.ndimage import distance_transform_edt

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("name"); ap.add_argument("source")
    args=ap.parse_args()
    root=Path(__file__).resolve().parent
    for sub in ["raw","keyed","qa","metadata"]: (root/sub).mkdir(exist_ok=True)
    raw=root/"raw"/(args.name+"_green.png")
    shutil.copy2(args.source,raw)
    rgb=np.asarray(Image.open(raw).convert("RGB")).astype(np.float32)/255
    r,g,b=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
    dominance=g-np.maximum(r,b)
    # Solid warm and purple pigment is preserved completely. Only pixels with
    # genuinely green-dominant color enter the chroma separation.
    mixed=dominance>0.025
    # Generated chroma canvases contain slight codec/noise variation around green.
    # Remove this entire backdrop family, including non-exact corner pixels.
    background=(dominance>0.65)&(g>0.75)&(np.maximum(r,b)<0.30)
    opaque=~mixed
    _,idx=distance_transform_edt(mixed,return_indices=True)
    foreground=rgb[tuple(idx)]
    bg=np.array([0.,1.,0.],dtype=np.float32)
    delta=foreground-bg
    a=np.sum((rgb-bg)*delta,axis=2)/np.maximum(np.sum(delta*delta,axis=2),1e-8)
    a=np.clip(a,0,1)
    a[opaque]=1
    a[background]=0
    a[a<0.018]=0
    out=(rgb-(1-a[:,:,None])*bg)/np.maximum(a[:,:,None],1e-6)
    out=np.clip(out,0,1)
    # Recover mixed edge RGB using nearby authentic foreground, removing
    # chroma bleed without suppressing deep purple/black or warm fire.
    residual=(out[:,:,1]>np.maximum(out[:,:,0],out[:,:,2]))&mixed
    out[residual]=foreground[residual]
    out[a==0]=0
    rgba=np.dstack([out,a])
    result=Image.fromarray(np.uint8(np.round(rgba*255)))
    # Added clean transparent margin keeps extreme fragments safely inside UVs.
    pad=max(24,round(max(result.size)*0.05))
    padded=Image.new("RGBA",(result.width+pad*2,result.height+pad*2))
    padded.alpha_composite(result,(pad,pad))
    keyed=root/"keyed"/(args.name+"_master.png"); padded.save(keyed)
    qa=Image.new("RGB",(1200,850),(55,55,59))
    draw=ImageDraw.Draw(qa)
    draw.rectangle((0,425,1200,850),fill=(237,235,229))
    for y in [0,425]:
        thumb=padded.copy(); thumb.thumbnail((1140,355),Image.Resampling.LANCZOS)
        qa.paste(thumb,((1200-thumb.width)//2,y+45+(355-thumb.height)//2),thumb)
    draw.text((18,16),args.name+" | chroma-key RGBA / dark background",fill="white")
    draw.text((18,442),args.name+" | same RGBA / near-white background",fill=(30,30,30))
    qa_path=root/"qa"/(args.name+"_dark_light.jpg"); qa.save(qa_path,quality=95,subsampling=0)
    pix=np.asarray(padded)
    mask=pix[:,:,3]>0
    ys,xs=np.where(mask)
    opaque_dark=(a>0.99)&(np.max(rgb,axis=2)<0.30)
    meta={"asset":args.name,"built_in_source":str(Path(args.source).resolve()),
      "raw":str(raw),"raw_sha256":sha(raw),"keyed":str(keyed),"keyed_sha256":sha(keyed),
      "source_dimensions":list(Image.open(raw).size),"keyed_dimensions":list(padded.size),
      "alpha_range":[int(pix[:,:,3].min()),int(pix[:,:,3].max())],
      "nonzero_alpha_bbox":[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)],
      "partially_transparent_pixels":int(np.count_nonzero((a>0)&(a<1))),
      "opaque_dark_pixels_preserved":int(np.count_nonzero(opaque_dark)),
      "green_dominant_pixels_after_key_above_10pct_alpha":int(np.count_nonzero((out[:,:,1]>np.maximum(out[:,:,0],out[:,:,2])+0.03)&(a>0.1))),
      "qa":str(qa_path),"matte_method":"green color dominance; nearest authentic foreground least-squares unmix; residual despill; no brightness key",
      "transparent_padding_each_edge_px":pad}
    (root/"metadata"/(args.name+".json")).write_text(json.dumps(meta,indent=2),encoding="utf-8")
    print(json.dumps(meta,indent=2))

if __name__=="__main__": main()
