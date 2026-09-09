"""Import the approved magenta-background face kit, preserving pale artwork.

The user explicitly chose a simple color-key workflow for generated components.
This keyed source is for a pale/gray/mint face kit; it is not a general image matting model.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
CROPS = {
    "facemodel": [118,74,564,625],
    "eyes_normal": [736,370,1140,480],
    "brows_normal": [750,309,1110,355],
    "nose_normal": [896,482,918,505],
    "mouth_normal": [907,532,958,548],
    "eyes_attack": [131,926,537,1023],
    "brows_attack": [148,877,488,932],
    "nose_attack": [291,1028,315,1059],
    "mouth_attack": [308,1075,361,1105],
    "eyes_damaged": [756,947,1134,1023],
    "brows_damaged": [777,897,1112,940],
    "nose_damaged": [895,1027,919,1059],
    "mouth_damaged": [908,1066,984,1107]
}


def key_magenta(image):
    result = []
    for r,g,b in image.convert("RGB").getdata():
        excess = min(r,b) - g
        if excess >= 235:
            result.append((0,0,0,0))
        elif excess > 35:
            # Estimate magenta coverage on a low-chroma foreground and unspill edges.
            alpha = 1 - excess/255
            result.append((max(0,min(255,round((r-(1-alpha)*255)/alpha))),
                           max(0,min(255,round(g/alpha))),
                           max(0,min(255,round((b-(1-alpha)*255)/alpha))),round(alpha*255)))
        else:
            result.append((r,g,b,255))
    out=Image.new("RGBA",image.size)
    out.putdata(result)
    return out


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input",type=Path,default=ROOT/"sources/face_kit_magenta.png")
    args=parser.parse_args()
    image=Image.open(args.input)
    if image.size != (1254,1254):
        raise ValueError("This version's explicit crop layout requires 1254 x 1254.")
    original=ROOT/"sources/face_kit_magenta.png"
    if args.input.resolve()!=original.resolve():original.write_bytes(args.input.read_bytes())
    rgba=key_magenta(image)
    rgba.save(ROOT/"sources/face_kit_rgba.png")
    provenance={"mode":"built-in image_gen + deterministic magenta color key", "background":"#FF00FF",
                "source":"sources/face_kit_magenta.png", "source_sha256":hashlib.sha256(original.read_bytes()).hexdigest(),
                "processing":"prepare_face_sources.py:key_magenta", "parts":{}}
    out=ROOT/"sources/head/custom"
    out.mkdir(parents=True,exist_ok=True)
    for name,rect in CROPS.items():
        target=out/(name+".png")
        rgba.crop(rect).save(target)
        provenance["parts"][name]={"path":str(target.relative_to(ROOT)).replace("\\","/"),"crop":rect,
                                  "sha256":hashlib.sha256(target.read_bytes()).hexdigest()}
    (ROOT/"sources/face_provenance.json").write_text(json.dumps(provenance,indent=2)+"\n",encoding="utf-8")
    print("Imported custom facemodel and normal/attack/damaged feature parts from a flat magenta background.")


if __name__=="__main__":main()
