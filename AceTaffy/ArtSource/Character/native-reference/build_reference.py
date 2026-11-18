"""Reconstruct two native Default prefab sprites without launching the game.

Requires UnityPy and the local game's original char bundles. Measurements use
100 display pixels per Unity world unit, matching the sprites' 100 PPU.
"""
import hashlib
import json
from pathlib import Path

import UnityPy
from PIL import Image

HERE = Path(__file__).resolve().parent
GAME = Path(r"D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data")
BUNDLES = GAME / "StreamingAssets/AssetBundles/char"
SPECS = (("Bada", 418), ("Malkuth", 512), ("Roland", 571))
CANVAS = (800, 650)
ORIGIN = (400, 560)
DISPLAY_PPU = 100

def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def hierarchy_for(bundle, character):
    environment = UnityPy.load(str(bundle))
    nodes = {}
    for obj in environment.objects:
        if obj.type.name != "GameObject":
            continue
        go = obj.read()
        transform = next((c.component for c in go.m_Component
                          if c.component and c.component.type.name == "Transform"), None)
        if transform:
            nodes[transform.path_id] = (go, transform.read())
    root = next(k for k, (go, _) in nodes.items() if go.m_Name == f"[Prefab]Appearance_{character}")
    outer = next(p.path_id for p in nodes[root][1].m_Children
                 if nodes[p.path_id][0].m_Name == f"{character}_Default")
    inner = next(p.path_id for p in nodes[outer][1].m_Children
                 if nodes[p.path_id][0].m_Name == f"{character}_Default")
    return nodes, (root, outer, inner)

def render(character, bundle):
    nodes, (root, outer, inner) = hierarchy_for(bundle, character)
    offsets = []
    parent_transforms = []
    for node_id in (root, outer, inner):
        position = nodes[node_id][1].m_LocalPosition
        scale = nodes[node_id][1].m_LocalScale
        rotation = nodes[node_id][1].m_LocalRotation
        if abs(scale.x - 1) > 1e-6 or abs(scale.y - 1) > 1e-6 or abs(rotation.z) > 1e-6:
            raise ValueError(f"{character} Default parent has unsupported transform")
        offsets.append((position.x, position.y))
        parent_transforms.append({"name": nodes[node_id][0].m_Name,
                                  "position": [position.x, position.y, position.z],
                                  "scale": [scale.x, scale.y, scale.z],
                                  "rotation_xyzw": [rotation.x, rotation.y, rotation.z, rotation.w]})
    parts = []
    for pointer in nodes[inner][1].m_Children:
        go, transform = nodes[pointer.path_id]
        name = go.m_Name
        if name not in ("head", "face", "front hair", "back hair") and not name.startswith("body"):
            continue  # Roland's P voo is an accessory, not anatomical width.
        renderer = next((c.component.read() for c in go.m_Component
                         if c.component and c.component.type.name == "SpriteRenderer"), None)
        if not renderer or not renderer.m_Enabled or not renderer.m_Sprite:
            continue
        sprite = renderer.m_Sprite.read()
        image = sprite.image.convert("RGBA")
        if sprite.m_PixelsToUnits != 100:
            raise ValueError(f"{character} {name} PPU changed: {sprite.m_PixelsToUnits}")
        world_x = sum(x for x, _ in offsets) + transform.m_LocalPosition.x
        world_y = sum(y for _, y in offsets) + transform.m_LocalPosition.y
        x = round(ORIGIN[0] + DISPLAY_PPU * world_x - image.width * sprite.m_Pivot.x)
        y = round(ORIGIN[1] - DISPLAY_PPU * world_y - image.height * (1 - sprite.m_Pivot.y))
        parts.append({"name": name, "sprite_id": renderer.m_Sprite.path_id,
                      "sorting_order": renderer.m_SortingOrder, "image": image,
                      "world_position": [world_x, world_y], "canvas_xy": [x, y],
                      "sprite_size": list(image.size), "ppu": sprite.m_PixelsToUnits})
    parts.sort(key=lambda part: part["sorting_order"])
    outputs = {}
    for layer in ("body", "head", "full"):
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        for part in parts:
            is_body = part["name"].startswith("body")
            if layer == "body" and not is_body:
                continue
            if layer == "head" and is_body:
                continue
            canvas.alpha_composite(part["image"], tuple(part["canvas_xy"]))
        output = HERE / f"{character}_Default_{layer}.png"
        canvas.save(output)
        outputs[layer] = {"file": output.name, "sha256": sha256(output),
                          "bbox_alpha32": list(canvas.getchannel("A").point(
                              lambda alpha: 255 if alpha >= 32 else 0).getbbox())}
    return {"bundle": str(bundle.relative_to(GAME)).replace("\\", "/"),
            "bundle_sha256": sha256(bundle),
            "prefab": f"[Prefab]Appearance_{character}",
            "motion": f"{character}_Default", "canvas": list(CANVAS),
            "origin_pixel": list(ORIGIN), "display_ppu": DISPLAY_PPU,
            "parent_offsets": offsets, "parent_transforms": parent_transforms,
            "parts": [{k: v for k, v in part.items() if k != "image"} for part in parts],
            "outputs": outputs}

if __name__ == "__main__":
    result = {name: render(name, BUNDLES / f"char_{bundle_id}.pres")
              for name, bundle_id in SPECS}
    (HERE / "extraction.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("Reconstructed native Bada, Malkuth and Roland Default sprites")
