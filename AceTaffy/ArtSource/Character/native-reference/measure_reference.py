"""Record manual anatomical landmarks on reconstructed native Default sprites."""
import json
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
LANDMARKS = {
    "Bada": {
        "sex_reference": "male",
        "crown_y": 77, "chin_y": 250, "sole_y": 554,
        "head_core_x": [289, 436], "face_x": [292, 422],
        "shoulder": {"y": 270, "x": [320, 424]},
        "chest": {"y": 320, "x": [326, 420]},
        "waist": {"y": 365, "x": [333, 414]},
    },
    "Malkuth": {
        "sex_reference": "female",
        "crown_y": 115, "chin_y": 269, "sole_y": 568,
        "head_core_x": [294, 449], "face_x": [314, 417],
        "shoulder": {"y": 286, "x": [345, 435]},
        "chest": {"y": 325, "x": [350, 429]},
        "waist": {"y": 365, "x": [351, 422]},
    },
    "Roland": {
        "sex_reference": "male",
        "crown_y": 55, "chin_y": 229, "sole_y": 560,
        "head_core_x": [299, 462], "face_x": [316, 441],
        "shoulder": {"y": 270, "x": [339, 449]},
        "chest": {"y": 320, "x": [346, 449]},
        "waist": {"y": 365, "x": [352, 441]},
    },
}

def main():
    extraction = json.loads((HERE / "extraction.json").read_text(encoding="utf-8"))
    result = {
        "units": "pixels at 100 display pixels per Unity world unit; native Sprite PPU 100",
        "method": ("UnityPy reconstructs the original Appearance_* Default prefab's enabled "
                   "SpriteRenderers with parent local transforms and sorting orders. "
                   "Anatomical landmarks are manually placed on the fixed-scale composite "
                   "and are approximate (about +/-5 px), not body PNG bounds."),
        "definitions": {
            "crown_y": "top of cranial hair cap, excluding loose tufts, headband and long back hair",
            "chin_y": "bottom of face/jaw, excluding collar and long hair",
            "head_core_x": "left/right cranial cap silhouette excluding detached tufts or long back hair",
            "face_x": "visible cheek-to-cheek skin width at widest facial level",
            "sole_y": "lowest planted boot pixel in Default motion",
            "shoulder_chest_waist": "clothed torso cross sections; exclude arms, cape, long hair, hat and weapon",
            "alpha_envelope": "all visible prefabricated body and head pixels at alpha>=32; includes hair, cape and weapon baked into selected body sprites",
        },
        "references": {},
    }
    panel_w, panel_h = 400, 650
    plain = Image.new("RGB", (panel_w * 3, panel_h), "#51545a")
    annotated = Image.new("RGB", plain.size, "#51545a")
    draw = ImageDraw.Draw(annotated)
    for index, (name, mark) in enumerate(LANDMARKS.items()):
        record = extraction[name]
        full = Image.open(HERE / record["outputs"]["full"]["file"]).convert("RGBA")
        crop = full.crop((200, 0, 600, panel_h))
        x0 = index * panel_w
        plain.paste(crop, (x0, 0), crop)
        annotated.paste(crop, (x0, 0), crop)
        head_h = mark["chin_y"] - mark["crown_y"]
        body_h = mark["sole_y"] - mark["chin_y"]
        head_w = mark["head_core_x"][1] - mark["head_core_x"][0]
        face_w = mark["face_x"][1] - mark["face_x"][0]
        widths = {kind: pair["x"][1] - pair["x"][0]
                  for kind, pair in ((k, mark[k]) for k in ("shoulder", "chest", "waist"))}
        source = {
            "bundle": record["bundle"], "bundle_sha256": record["bundle_sha256"],
            "prefab": record["prefab"], "motion": record["motion"],
            "parent_offsets": record["parent_offsets"],
            "parent_transforms": record["parent_transforms"],
            "sprite_ppu": 100, "root_pixel": record["origin_pixel"],
            "body_root_y_units": round((record["origin_pixel"][1] - mark["sole_y"]) / 100, 3),
            "alpha_envelope": record["outputs"]["full"]["bbox_alpha32"],
            "body_layer_envelope": record["outputs"]["body"]["bbox_alpha32"],
            "head_layer_envelope": record["outputs"]["head"]["bbox_alpha32"],
            "landmarks": mark,
            "derived": {
                "head_height": head_h, "head_core_width": head_w,
                "face_width": face_w, "chin_to_sole_height": body_h,
                "anatomical_total_height": mark["sole_y"] - mark["crown_y"],
                "total_in_head_heights": round((mark["sole_y"] - mark["crown_y"]) / head_h, 3),
                "body_to_head_height": round(body_h / head_h, 3),
                "head_core_width_to_height": round(head_w / head_h, 3),
                "shoulder_width": widths["shoulder"],
                "chest_width": widths["chest"],
                "waist_width": widths["waist"],
                "shoulder_to_head_height": round(widths["shoulder"] / head_h, 3),
                "chest_to_head_height": round(widths["chest"] / head_h, 3),
                "waist_to_head_height": round(widths["waist"] / head_h, 3),
            },
            "images": record["outputs"],
        }
        result["references"][name] = source
        draw.rectangle((x0 + 3, 3, x0 + 380, 36), fill="#212328")
        draw.text((x0 + 9, 9), f"{name}: head {head_h} / body {body_h} = {source['derived']['total_in_head_heights']:.2f} heads", fill="white")
        for label, y, color in (("crown", mark["crown_y"], "#f8d46b"),
                                ("chin", mark["chin_y"], "#f47272"),
                                ("sole", mark["sole_y"], "#81cbff")):
            draw.line((x0 + 30, y, x0 + 355, y), fill=color, width=2)
            draw.text((x0 + 7, y - 12), label, fill=color)
        for label, pair, y, color in (
            ("head", mark["head_core_x"], mark["crown_y"] + head_h // 2, "#faecb7"),
            ("shoulder", mark["shoulder"]["x"], mark["shoulder"]["y"], "#b6eea8"),
            ("chest", mark["chest"]["x"], mark["chest"]["y"], "#b6eea8"),
            ("waist", mark["waist"]["x"], mark["waist"]["y"], "#b6eea8"),
        ):
            lo, hi = [x0 + x - 200 for x in pair]
            draw.line((lo, y, hi, y), fill=color, width=3)
            draw.ellipse((lo - 3, y - 3, lo + 3, y + 3), fill=color)
            draw.ellipse((hi - 3, y - 3, hi + 3, y + 3), fill=color)
            draw.text((hi + 5, y - 7), label, fill=color)
    plain.save(HERE / "native-defaults.png")
    annotated.save(HERE / "native-measurements.png")
    (HERE / "measurements.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("Wrote fixed-scale native previews and measurements.json")

if __name__ == "__main__":
    main()
