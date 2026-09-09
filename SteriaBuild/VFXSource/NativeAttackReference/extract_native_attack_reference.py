"""Export a reference-only VFX library from a locally installed Library of Ruina.

Selection uses the player ResourceManager and serialized dependencies, never only
texture filename guesses. Stripped MonoBehaviour fields and dynamic loads remain
explicit coverage limitations. No output belongs in a shipping mod directory.
"""
from __future__ import annotations

import argparse
import base64
import collections
import hashlib
import importlib.metadata
import json
import math
import re
import struct
import sys
import time
import zlib
from pathlib import Path
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
PREFIXES = (
    "prefabs/battle/diceattackeffects/",
    "prefabs/battle/dicedamagedeffects/",
    "prefabs/battle/creatureeffect/",
    "prefabs/battle/specialeffect/",
)
LEAF_TYPES = {"Shader", "MonoScript", "AudioClip", "Mesh", "Font", "TextAsset", "Avatar", "VideoClip", "RenderTexture", "Cubemap"}
SKIP_FIELDS = {"m_Father", "m_Script", "m_Shader"}


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as f:
        for chunk in iter(lambda: f.read(4 * 1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def safe(name):
    return re.sub(r"[^a-zA-Z0-9_.-]+", "_", name)[:115] or "unnamed"


def json_default(value):
    if isinstance(value, bytes):
        return {"encoding": "base64", "data": base64.b64encode(value).decode("ascii")}
    raise TypeError(type(value).__name__)


def json_finite(value):
    """Keep Unity's non-finite floats explicit without emitting invalid JSON."""
    if isinstance(value, float) and not math.isfinite(value):
        return {"unity_float": "NaN" if math.isnan(value) else "Infinity" if value > 0 else "-Infinity"}
    if isinstance(value, dict):
        return {key: json_finite(child) for key, child in value.items()}
    if isinstance(value, (list, tuple)):
        return [json_finite(child) for child in value]
    return value


def save_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(json_finite(value), ensure_ascii=False, indent=2, default=json_default, allow_nan=False) + "\n", encoding="utf-8")


def object_key(obj):
    return f"{Path(obj.assets_file.name).name}:{obj.path_id}"


def pointers(value, field=""):
    if isinstance(value, dict):
        if "m_FileID" in value and "m_PathID" in value:
            if value["m_PathID"]:
                yield field, value
        else:
            for key, child in value.items():
                if key not in SKIP_FIELDS:
                    yield from pointers(child, field + "/" + key)
    elif isinstance(value, (list, tuple)):
        for index, child in enumerate(value):
            yield from pointers(child, field + f"/{index}")


def category(resource):
    tail = resource.rsplit("/", 1)[-1]
    suffix = tail.rsplit("_", 1)[-1]
    if suffix == "j" or "slash" in tail:
        return "slash"
    if suffix == "z" or "penetrate" in tail or "pierce" in tail:
        return "thrust"
    if suffix == "h" or "hit" in tail:
        return "hit"
    if suffix == "g":
        return "guard"
    return "special"


def animation_timing(tree):
    muscle = tree.get("m_MuscleClip", {})
    clip = muscle.get("m_Clip", {})
    clip = clip.get("data", clip)
    dense, streamed = clip.get("m_DenseClip", {}), clip.get("m_StreamedClip", {})
    attributes = {zlib.crc32(name.encode()): name for name in ("m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a", "m_Enabled")}
    bindings = []
    for binding in tree.get("m_ClipBindingConstant", {}).get("genericBindings", []):
        bindings.append({"path_hash": binding.get("path"), "type_id": binding.get("typeID"),
                         "attribute_hash": binding.get("attribute"),
                         "known_attribute": attributes.get(binding.get("attribute")), "is_pointer": binding.get("isPPtrCurve")})
    result = {"sample_rate": tree.get("m_SampleRate"), "start_time": muscle.get("m_StartTime"),
              "stop_time": muscle.get("m_StopTime"), "dense_frame_count": dense.get("m_FrameCount"),
              "dense_sample_rate": dense.get("m_SampleRate"), "streamed_curve_count": streamed.get("curveCount"),
              "bindings": bindings, "events": tree.get("m_Events", [])}
    # A small readable excerpt, never a replacement for the complete raw clip.
    words = streamed.get("data", [])
    if words and len(words) <= 2000:
        raw, pos, frames = struct.pack("<" + "I" * len(words), *words), 0, []
        while pos + 8 <= len(raw):
            timestamp, count = struct.unpack_from("<fi", raw, pos)
            pos += 8
            if count < 0 or pos + count * 20 > len(raw):
                break
            keys = []
            for _ in range(count):
                index, *coefficients = struct.unpack_from("<i4f", raw, pos)
                pos += 20
                keys.append({"curve_index": index, "cubic_coefficients": coefficients, "value": coefficients[3]})
            if math.isfinite(timestamp) and timestamp >= 0:
                frames.append({"time": timestamp, "keys": keys})
        result["streamed_keyframes"] = frames
    return result


def contact_sheets(out, images, roots):
    from PIL import Image, ImageDraw, ImageFont
    font_path = Path("C:/Windows/Fonts/arial.ttf")
    font = ImageFont.truetype(str(font_path), 15) if font_path.exists() else ImageFont.load_default()
    title_font = ImageFont.truetype(str(font_path), 21) if font_path.exists() else font
    ownership = collections.defaultdict(set)
    root_labels = collections.defaultdict(set)
    for root in roots:
        for key in root["images"]:
            ownership[key].add(category(root["resource_path"]))
            root_labels[key].add(root["resource_path"].rsplit("/", 1)[-1])
    pages = []
    groups = {"all_sprites": [i for i in images if i["type"] == "Sprite"],
              "all_textures": [i for i in images if i["type"] == "Texture2D"]}
    for group in ("slash", "thrust", "hit", "guard", "special"):
        groups[group] = [i for i in images if group in ownership[i["id"]] and i["type"] == "Sprite"]
    for group, pattern in {"theme_fire": r"/(philipego|xiaoego|liu[12])_[hjz]$",
                           "theme_ink_purple": r"/(kurokumo|thepurpletear[jhz]?)_[hjz]$",
                           "theme_blue": r"/(argalia[2]?|twistedargalia)_[hjsz][1-5]?$"}.items():
        keys = {key for root in roots if re.search(pattern, root["resource_path"]) for key in root["images"]}
        groups[group] = [i for i in images if i["id"] in keys and i["type"] == "Sprite"]
    for group, selected in groups.items():
        selected.sort(key=lambda i: (i["name"].lower(), i["id"]))
        for start in range(0, len(selected), 30):
            page_items = selected[start:start + 30]
            canvas = Image.new("RGB", (1500, 70 + 230 * math.ceil(len(page_items) / 5)), (22, 25, 33))
            draw = ImageDraw.Draw(canvas)
            draw.text((16, 15), f"Native LoR | {group} | {start + 1}-{start + len(page_items)} / {len(selected)} | REFERENCE ONLY", font=title_font, fill=(230, 234, 240))
            for n, item in enumerate(page_items):
                x, y = (n % 5) * 300, 65 + (n // 5) * 230
                # Neutral dark/light checks make alpha visible; images are only
                # resized for this index. Exported originals remain unmodified.
                for yy in range(y, y + 182, 16):
                    for xx in range(x + 5, x + 295, 16):
                        c = 45 if ((xx - x) // 16 + (yy - y) // 16) % 2 else 61
                        draw.rectangle((xx, yy, min(xx + 15, x + 294), min(yy + 15, y + 181)), fill=(c, c, c))
                with Image.open(out / item["png"]) as source:
                    thumb = source.convert("RGBA")
                    thumb.thumbnail((280, 170))
                    canvas.paste(thumb, (x + (300 - thumb.width) // 2, y + (182 - thumb.height) // 2), thumb)
                label = ", ".join(sorted(root_labels[item["id"]])) or item["name"]
                draw.text((x + 8, y + 184), label[:33], font=font, fill=(245, 245, 247))
                draw.text((x + 8, y + 205), f"{item['id']} | {item['width']}x{item['height']}"[:43], font=font, fill=(159, 174, 193))
            rel = f"contact_sheets/{group}_{start // 30 + 1:03d}.png"
            (out / rel).parent.mkdir(exist_ok=True)
            canvas.save(out / rel)
            pages.append({"group": group, "png": rel, "images": [i["id"] for i in page_items], "sha256": sha(out / rel)})
    return pages


class Extractor:
    def __init__(self, game_data, out):
        import UnityPy
        from UnityPy.classes import PPtr
        self.PPtr = PPtr
        self.env = UnityPy.load(str(game_data / "globalgamemanagers"))
        self.out = out
        self.nodes = {}
        self.images = {}
        self.errors = []
        self.opaque = []
        self.asset_inputs = set()

    def resolve(self, source, pointer):
        return self.PPtr(assetsfile=source.assets_file, **pointer).deref()

    def inspect(self, obj):
        key = object_key(obj)
        if key in self.nodes:
            return self.nodes[key]
        typ = obj.type.name
        node = {"id": key, "type": typ, "name": "", "edges": []}
        self.nodes[key] = node
        self.asset_inputs.add(Path(obj.assets_file.name))
        if typ in LEAF_TYPES:
            node["not_traversed"] = "non-image/non-animation terminal dependency"
            return node
        try:
            if typ in ("Sprite", "Texture2D"):
                data = obj.read()
                node["name"] = data.m_Name
                rel = f"images/{typ}/{safe(data.m_Name)}__{safe(key)}.png"
                path = self.out / rel
                path.parent.mkdir(parents=True, exist_ok=True)
                im = data.image.convert("RGBA")
                im.save(path)
                info = {"id": key, "type": typ, "name": data.m_Name, "png": rel, "sha256": sha(path),
                        "width": im.width, "height": im.height, "alpha_extrema": list(im.getchannel("A").getextrema())}
                if typ == "Sprite":
                    info.update({"ppu": data.m_PixelsToUnits,
                                 "pivot": [data.m_Pivot.x, data.m_Pivot.y],
                                 "rect": [data.m_Rect.x, data.m_Rect.y, data.m_Rect.width, data.m_Rect.height],
                                 "texture_rect_offset": [data.m_RD.textureRectOffset.x, data.m_RD.textureRectOffset.y]})
                    for label, ptr in (("texture", data.m_RD.texture), ("alphaTexture", data.m_RD.alphaTexture)):
                        if ptr:
                            linked = ptr.deref()
                            node["edges"].append({"field": label, "target": object_key(linked)})
                            self.inspect(linked)
                    # UnityPy caches whole decoded atlases. Bound those caches.
                    cache = obj.assets_file._cache
                    while len(cache) > 4:
                        cache.pop(next(iter(cache)))
                else:
                    info["unity_texture_format"] = int(data.m_TextureFormat)
                    stream = getattr(data, "m_StreamData", None)
                    if stream and stream.path:
                        info["stream"] = {"path": stream.path, "offset": stream.offset, "size": stream.size}
                self.images[key] = info
                return node
            try:
                tree = obj.read_typetree()
            except ValueError as exc:
                if typ != "MonoBehaviour":
                    raise
                tree = obj.read_typetree(check_read=False)
                node["opaque_custom_fields"] = True
                node["custom_field_parse_error"] = str(exc)
            node["name"] = tree.get("m_Name", "")
            if typ == "MonoBehaviour" and tree.get("m_Script", {}).get("m_PathID"):
                script_obj = self.resolve(obj, tree["m_Script"])
                self.asset_inputs.add(Path(script_obj.assets_file.name))
                node["script_class"] = script_obj.read().m_ClassName
                # Explicitly supported vanilla base-class layout, verified in
                # Assembly-CSharp/Battle/DiceAttackEffect/DiceAttackEffect.cs.
                # Do not guess derived classes or layouts of a different size.
                raw = obj.get_raw_data()
                if node["script_class"] == "DiceAttackEffect" and len(raw) == 80:
                    fields = struct.unpack("<5fiqiqf", raw[32:])
                    tree.update({"offset": dict(zip(("x", "y"), fields[:2])),
                                 "additionalScale": dict(zip(("x", "y", "z"), fields[2:5])),
                                 "spr": {"m_FileID": fields[5], "m_PathID": fields[6]},
                                 "animator": {"m_FileID": fields[7], "m_PathID": fields[8]},
                                 "addedDestroyTime": fields[9]})
                    node.pop("opaque_custom_fields", None)
                    node.pop("custom_field_parse_error", None)
                    node["custom_field_decoder"] = "vanilla DiceAttackEffect 80-byte Unity 2019 layout"
                    rel = f"metadata/MonoBehaviour/{safe(key)}.json"
                    save_json(self.out / rel, tree)
                    node["metadata"] = rel
                elif node.get("opaque_custom_fields"):
                    self.opaque.append({"id": key, "script_class": node["script_class"],
                                        "reason": node["custom_field_parse_error"],
                                        "note": "Only built-in MonoBehaviour header decoded; custom serialized fields unavailable."})
            for field, pointer in pointers(tree):
                try:
                    linked = self.resolve(obj, pointer)
                    node["edges"].append({"field": field, "target": object_key(linked)})
                    self.inspect(linked)
                except Exception as exc:
                    self.errors.append({"id": key, "field": field, "pointer": pointer, "error": str(exc)})
            if typ in {"AnimationClip", "AnimatorController", "AnimatorOverrideController", "Transform", "RectTransform", "SpriteRenderer", "Material", "Animator", "ParticleSystemRenderer", "LineRenderer", "TrailRenderer", "MeshRenderer", "SortingGroup"}:
                rel = f"metadata/{typ}/{safe(key)}.json"
                save_json(self.out / rel, tree)
                node["metadata"] = rel
                if typ == "AnimationClip":
                    node["timing"] = animation_timing(tree)
            elif typ == "ParticleSystem":
                # Keep all enabled module curves, UV frame sequence, timing,
                # emission and initial properties without disabled-module bulk.
                selected = {k: v for k, v in tree.items() if not isinstance(v, dict) or v.get("enabled", True)}
                rel = f"metadata/{typ}/{safe(key)}.json"
                save_json(self.out / rel, selected)
                node["metadata"] = rel
        except Exception as exc:
            node["error"] = str(exc)
            self.errors.append({"id": key, "type": typ, "error": str(exc)})
        return node

    def closure(self, start):
        visited, pending = set(), [start]
        while pending:
            key = pending.pop()
            if key in visited:
                continue
            visited.add(key)
            pending.extend(e["target"] for e in self.nodes.get(key, {}).get("edges", []))
        return visited


def verify(out):
    manifest = json.loads((out / "manifest.json").read_text(encoding="utf-8"))
    for entry in manifest["images"] + manifest["contact_sheets"]:
        path = out / entry["png"]
        if sha(path) != entry["sha256"]:
            raise ValueError(f"Hash mismatch: {path}")
    for entry in manifest.get("metadata_artifacts", []):
        if sha(out / entry["path"]) != entry["sha256"]:
            raise ValueError(f"Metadata hash mismatch: {entry['path']}")
    known = {entry["id"] for entry in manifest["images"]}
    for root in manifest["roots"]:
        if not set(root["images"]).issubset(known):
            raise ValueError(f"Missing image in {root['resource_path']}")
    print(f"VERIFIED {len(known)} original PNGs, {len(manifest['contact_sheets'])} contact sheets, {len(manifest['roots'])} root image indexes.", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-data", type=Path)
    parser.add_argument("--output", type=Path, default=HERE / "references" / "all")
    parser.add_argument("--python-deps", type=Path, help="Isolated UnityPy/Pillow install directory")
    parser.add_argument("--root-regex", help="Optional subset, explicitly reported in coverage")
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    if args.python_deps:
        sys.path.insert(0, str(args.python_deps.resolve()))
    if args.verify_only:
        verify(args.output)
        return
    if not args.game_data:
        parser.error("--game-data is required for extraction")
    out = args.output.resolve()
    game = args.game_data.resolve()
    if game in out.parents or out == game:
        parser.error("Reference output must be outside the game installation")
    out.mkdir(parents=True, exist_ok=True)
    started = time.time()
    ex = Extractor(game, out)
    manager_obj = next(o for o in ex.env.objects if o.type.name == "ResourceManager")
    manager = manager_obj.read()
    ex.asset_inputs.add(Path(manager_obj.assets_file.name))
    container = sorted(manager.m_Container, key=lambda item: item[0])
    all_roots = [(p, v) for p, v in container if p.startswith(PREFIXES)]
    selected = [(p, v) for p, v in all_roots if not args.root_regex or re.search(args.root_regex, p, re.I)]
    mapping = []
    for path, ptr in container:
        if path == "xml/attackeffectpathinfo":
            text = ptr.read().m_Script
            (out / "AttackEffectPathInfo.xml").write_text(text, encoding="utf-8")
            for info in ET.fromstring(text).findall("Info"):
                mapping.append({"effect_res": info.findtext("Name"), "resource_path": PREFIXES[0] + "new/" + info.findtext("FilePath", "").lower()})
    roots = []
    for index, (resource, ptr) in enumerate(selected):
        try:
            obj = ptr.deref()
            ex.inspect(obj)
            closure = ex.closure(object_key(obj))
            roots.append({"resource_path": resource, "root": object_key(obj), "category": category(resource),
                          "objects": sorted(closure), "images": sorted(closure & ex.images.keys()),
                          "animations": sorted(k for k in closure if ex.nodes.get(k, {}).get("type") == "AnimationClip"),
                          "opaque_components": sorted(k for k in closure if ex.nodes.get(k, {}).get("opaque_custom_fields"))})
        except Exception as exc:
            ex.errors.append({"resource_path": resource, "error": str(exc)})
        if (index + 1) % 50 == 0:
            print(f"Roots {index + 1}/{len(selected)}; images {len(ex.images)}; objects {len(ex.nodes)}", flush=True)
    images = list(ex.images.values())
    sheets = contact_sheets(out, images, roots)
    # Hash the exact serialized inputs and resource streams that actually loaded.
    inputs = []
    loaded = {Path(name) for name in ex.env.files}
    resolved_inputs = {(path if path.is_absolute() else game / path).resolve() for path in ex.asset_inputs | loaded}
    for path in sorted(resolved_inputs, key=str):
        if path.is_file():
            inputs.append({"path": str(path), "bytes": path.stat().st_size, "sha256": sha(path)})
    limitations = [
        "Coverage is all selected ResourceManager roots plus traversable serialized dependencies, not proof of every runtime-created effect.",
        "Stripped MonoBehaviour custom fields are opaque; references only reachable through those fields or dynamically constructed Resources.Load paths may be absent.",
        "CreatureEffect and SpecialEffect roots are supplemental context and include non-attack visuals; labels are not proof of gameplay ownership.",
        "Raw Unity animation curves and Sprite pivots/PPU are exported. Clip time is not final wall-clock timing: DiceAttackEffect.Initialize changes Animator.speed to 1/(destroyTime+addedDestroyTime).",
        "Shaders, audio, meshes, fonts and scripts are terminal dependencies; no geometry, sound or compiled shader code is exported.",
        "PNG previews show native alpha; additive material appearance requires reviewing material metadata and the real game.",
        "Extracted original game assets are local reference only and must never be packaged with the mod.",
    ]
    coverage = {"resource_manager_entries": len(container), "available_root_counts": dict(collections.Counter(p.split('/')[2] for p, _ in all_roots)),
                "root_filter": args.root_regex, "selected_roots": len(selected), "exported_roots": len(roots),
                "roots_with_images": sum(bool(r["images"]) for r in roots),
                "discovered_object_types": dict(collections.Counter(n["type"] for n in ex.nodes.values())),
                "exported_image_types": dict(collections.Counter(i["type"] for i in images)),
                "opaque_components": len(ex.opaque), "errors": len(ex.errors),
                "attack_effect_mappings": len(mapping),
                "mapping_roots_missing_from_resource_manager": [m for m in mapping if m["resource_path"] not in {p for p, _ in all_roots}],
                "elapsed_seconds": round(time.time() - started, 2), "limitations": limitations}
    metadata_paths = sorted({n["metadata"] for n in ex.nodes.values() if "metadata" in n})
    artifacts = [{"path": rel, "sha256": sha(out / rel)} for rel in metadata_paths + ["AttackEffectPathInfo.xml"]]
    generator = {"script_sha256": sha(Path(__file__)), "python": sys.version.split()[0],
                 "UnityPy": importlib.metadata.version("UnityPy"), "Pillow": importlib.metadata.version("Pillow")}
    manifest = {"schema_version": 1, "generator": generator, "usage": "reference-only; never ship", "coverage": coverage, "inputs": inputs, "metadata_artifacts": artifacts,
                "effect_res_mapping": mapping, "roots": roots, "images": images, "objects": list(ex.nodes.values()),
                "opaque_components": ex.opaque, "errors": ex.errors, "contact_sheets": sheets}
    save_json(out / "manifest.json", manifest)
    save_json(out / "coverage.json", coverage)
    print(json.dumps(coverage, ensure_ascii=False, indent=2), flush=True)
    verify(out)


if __name__ == "__main__":
    main()
