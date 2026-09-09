"""Verify the actual authoring/export contracts, especially head isolation."""
import hashlib
import json
import xml.etree.ElementTree as ET

from PIL import Image

from build_model import ROOT, load_model, render, rotate_point


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    model = load_model()
    out = ROOT / "build"
    manifest = json.loads((out / "build_manifest.json").read_text())
    for path, expected in manifest["sources"].items():
        require(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected,
                f"Stale build: {path}")
    source_identity = hashlib.sha256(json.dumps(manifest["sources"], sort_keys=True).encode()).hexdigest()
    require(source_identity == manifest["candidate"], "Candidate identity mismatch")
    provenance = json.loads((ROOT / "sources/provenance.json").read_text())
    for source in provenance["native"]:
        require(hashlib.sha256((ROOT / source["path"]).read_bytes()).hexdigest() == source["sha256"],
                f"Native source changed: {source['path']}")
    actions = list(model["poses"]) + list(model["aliases"])
    xml = ET.parse(out / "Velia_Layered_v1_Projection/ModInfo.Xml").getroot()
    changed_heads = 0
    for action in actions:
        clothing = render(model, action, domain="wearing")
        alternate = render(model, action, "librarian", domain="wearing")
        require(clothing.tobytes() == alternate.tobytes(), f"Head leaked into projection: {action}")
        first, second = render(model, action), render(model, action, "librarian")
        require(first.tobytes() != second.tobytes(), f"Head preset does not change preview: {action}")
        changed_heads += 1
        for folder, suffix, expected in [("assembled", "", first),
                ("Velia_Layered_v1_Projection/ClothCustom", "",render(model,action,domain='wearing',plane='main')),
                ("Velia_Layered_v1_Projection/ClothCustom", "_front",render(model,action,domain='wearing',plane='front'))]:
            with Image.open(out / folder / (action + suffix + ".png")) as actual:
                require(actual.mode == "RGBA" and actual.size == (512, 512), f"Invalid PNG: {action}")
                require(actual.tobytes() == expected.tobytes(), f"Stale PNG: {action}")
                bounds = actual.getchannel("A").point(lambda a: 255 if a > 16 else 0).getbbox()
                require((suffix=='_front' and bounds is None) or (bounds and bounds[0] > 0 and bounds[1] > 0 and bounds[2] < 512 and bounds[3] < 512),
                        f"Clipped/empty sprite: {action}")
        node = xml.find(f"ClothInfo/{action}")
        require(node is not None and node.find("Head").get("head_enable") == "True", f"Head disabled: {action}")
        require(node.findtext("Direction") == "Front", f"Unexpected view: {action}")
        pivot, head = node.find("Pivot"), node.find("Head")
        root_x = (float(pivot.get("pivot_x")) + 512) / 1024 * 512
        root_y = 512 - (float(pivot.get("pivot_y")) + 512) / 1024 * 512
        require(abs(root_x - model['canvas']['root'][0]) < .001, f"Horizontal root conversion: {action}")
        pose = model["poses"][model["aliases"].get(action, action)]
        offset=rotate_point(model['head']['runtime_origin_from_neck'],[0,0],pose['head_angle'])
        anchor=[pose['neck_at'][0]+offset[0],pose['neck_at'][1]+offset[1]]
        factor=model['canvas']['pixels_per_unit']/100
        actual_anchor = [root_x + float(head.get("head_x"))*factor, root_y - float(head.get("head_y"))*factor]
        require(max(abs(a-b) for a,b in zip(anchor,actual_anchor)) < .001, f"Head conversion: {action}")
        require(pose['body_angle']==0, f"Rigid full-body tilt returned: {action}")
        grounded=clothing.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox()
        require(abs(grounded[3]-391)<=2,f"Feet no longer meet the ground: {action}")
        config=model['grounding']
        reference=json.loads((ROOT/config['reference']).read_text(encoding='utf-8'))
        require(abs(config['target_sole_root_y']-reference['reference_sole_root_y'])<1e-6,
                'Configured ground height differs from the measured native reference')
        special=config['exceptions'].get(model['aliases'].get(action,action))
        require(not special or bool(special.get('reason')),'Undocumented ground offset')
        expected=reference['reference_sole_root_y']+(special['offset_world_y'] if special else 0)
        sole_world=(root_y-grounded[3])/model['canvas']['pixels_per_unit']
        require(abs(sole_world-expected)<1e-6,f"Sole baseline differs from native Default: {action}")
    require('pendant' not in {p['id'] for p in model['parts']} and model['wearing']['accessories']==[],
            'The pendant must be baked into the garment')
    reg=model['head']['registration']
    for p in [p for p in model['parts'] if p['domain']=='head']:
        require(Image.open(ROOT/p['path']).size==tuple(reg['canvas']),f"Head layer lost common canvas: {p['id']}")
        require(p['at']==[0,0] and p['pivot']==[reg['neck'][0]/512,reg['neck'][1]/610],
                f"Independent feature translation/pivot returned: {p['id']}")
        require(p['size']==[117.76,140.3],f"Independent feature scaling returned: {p['id']}")
    face_provenance = json.loads((ROOT / "sources/face_provenance.json").read_text())
    for part in face_provenance["parts"].values():
        require(hashlib.sha256((ROOT / part["path"]).read_bytes()).hexdigest() == part["sha256"],
                f"Custom face source changed: {part['path']}")
        image = Image.open(ROOT / part["path"])
        require(not any(min(r,b)-g > 100 and a > 220 for r,g,b,a in image.getdata()),
                f"Opaque magenta spill: {part['path']}")
    # Same pose, different expression: genuine artwork changes, not body motion.
    faces = []
    for expression in ["normal", "attack", "damaged"]:
        model["poses"]["Default"]["expression"] = expression
        faces.append(render(model, "Default", domain="head").tobytes())
    require(len(set(faces)) == 3, "Expression sprites are not distinct")
    model["poses"]["Default"]["expression"] = "normal"
    # Alpha is preserved in the light robe; there is no white-color deletion.
    robe = Image.open(ROOT / "sources/r2/body/Default.png")
    require(sum(1 for r,g,b,a in robe.getdata() if min(r,g,b) > 230 and a > 240) > 1000,
            "White robe highlights lost alpha")
    baseline = render(model, "Default", domain="wearing").tobytes()
    model["head"]["color"]["skin"] = "#794b3c"
    require(render(model, "Default", domain="wearing").tobytes() == baseline,
            "Head color changes projection")
    report = {"result":"PASS", "candidate":manifest["candidate"], "actions_checked":len(actions),
        "head_swap_isolation":changed_heads, "png_dimensions_alpha_bounds":"PASS",
        "source_hashes":"PASS", "native_provenance":"PASS", "xml_anchor_roundtrip":"PASS",
        "white_highlights":"PASS", "custom_face_color_key":"PASS", "expressions":"PASS",
        "head_shared_registration":"PASS", "ground_contacts":"PASS", "native_sole_baseline":"PASS", "pendant_baked":"PASS",
        "wearing_front_export":"PASS", "runtime_game_acceptance":"NOT_RUN",
        "scope":"Authoring/export verification; no deployment or game runtime certification."}
    (out / "verification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
