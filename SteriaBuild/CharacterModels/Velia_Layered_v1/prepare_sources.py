"""Import explicit parts; never infer a character mask or remove a background.

Optional one-time import. The committed source PNGs make normal builds offline.
Native sprites require a locally owned Library of Ruina resources.assets.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
PARTS = {
    "wearing/body/robe": (75, 42, 475, 493),
    "wearing/body/sleeve_relaxed": (668, 80, 845, 465),
    "wearing/body/sleeve_cast": (1170, 77, 1445, 425),
    "head/hair/back": (66, 540, 460, 1007),
    "head/hair/front": (590, 555, 966, 995),
    "wearing/accessories/pendant": (1190, 671, 1340, 890),
}
NATIVE = {
    23412: "head/facemodel/front",
    20377: "head/face/eyes",
    20439: "head/face/brows_normal",
    24579: "head/face/mouth_normal",
    20452: "head/face/eyes_alternate",
    20152: "head/face/brows_alternate",
    24388: "references/alternate_front_hair",
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--atlas", type=Path, required=True)
    parser.add_argument("--resources", type=Path, required=True)
    args = parser.parse_args()
    import UnityPy

    atlas = Image.open(args.atlas)
    if atlas.mode != "RGBA" or atlas.getchannel("A").getextrema()[0] != 0:
        raise ValueError("Need real RGBA transparency. RGB/checkerboard is not accepted.")
    if atlas.size != (1536, 1024):
        raise ValueError("Crop contract requires the approved 1536 x 1024 atlas.")
    generated = ROOT / "sources/generated_parts_rgba.png"
    generated.parent.mkdir(parents=True, exist_ok=True)
    generated.write_bytes(args.atlas.read_bytes())
    provenance = {"generated": {"path": str(generated.relative_to(ROOT)),
                               "sha256": sha(generated), "mode": "built-in image_gen",
                               "crops": {}}, "native": []}
    for part, rect in PARTS.items():
        target = ROOT / "sources" / (part + ".png")
        target.parent.mkdir(parents=True, exist_ok=True)
        atlas.crop(rect).save(target)
        provenance["generated"]["crops"][part] = {"rect": rect, "sha256": sha(target)}

    env = UnityPy.load(str(args.resources))
    found = set()
    for obj in env.objects:
        if obj.type.name != "Sprite" or obj.path_id not in NATIVE:
            continue
        sprite = obj.read()
        part = NATIVE[obj.path_id]
        target = ROOT / "sources" / (part + ".png")
        target.parent.mkdir(parents=True, exist_ok=True)
        # UnityPy decodes the original sprite alpha/packing; no color key or rembg.
        sprite.image.save(target)
        texture = sprite.m_RD.texture.read()
        provenance["native"].append({"path": str(target.relative_to(ROOT)).replace("\\", "/"),
            "asset_file": args.resources.name, "path_id": obj.path_id,
            "sprite_name": sprite.m_Name, "texture_name": texture.m_Name,
            "sha256": sha(target), "ppu": sprite.m_PixelsToUnits,
            "pivot": [sprite.m_Pivot.x, sprite.m_Pivot.y],
            "rect": [sprite.m_Rect.x, sprite.m_Rect.y, sprite.m_Rect.width, sprite.m_Rect.height],
            "texture_rect_offset": [sprite.m_RD.textureRectOffset.x, sprite.m_RD.textureRectOffset.y],
            "usage": "local authoring/reference only; not included in projection exports"})
        found.add(obj.path_id)
    if found != set(NATIVE):
        raise ValueError(f"Native asset version mismatch: missing {set(NATIVE) - found}")
    (ROOT / "sources/provenance.json").write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("Imported 6 generated parts and 7 native sprites with provenance.")


if __name__ == "__main__":
    main()
