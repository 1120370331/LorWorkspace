"""Extract the original Appearance_Custom attack pivot from local game data."""
from pathlib import Path
import hashlib
import json
import struct

import UnityPy

HERE = Path(__file__).resolve().parent
SOURCE = Path("D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/resources.assets")
assert SOURCE.is_file(), SOURCE
EXPECTED = "ee635cd419ce02027664abf9ebe8a83836a1c5ae6d0d41a73f30a0092dcdd794"
sha = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
assert sha == EXPECTED, (sha, EXPECTED)
objects = {o.path_id: o for o in UnityPy.load(str(SOURCE)).objects}
prefab = objects[33898].read_typetree()
assert prefab["m_Name"] == "[Prefab]Appearance_Custom"


def transform(path_id):
    data = objects[path_id].read_typetree()
    go = objects[data["m_GameObject"]["m_PathID"]].read_typetree()
    return {"name": go["m_Name"], "path_id": path_id,
            "position": data["m_LocalPosition"], "scale": data["m_LocalScale"],
            "rotation": data["m_LocalRotation"],
            "children": [x["m_PathID"] for x in data["m_Children"]]}


root_id = next(x["component"]["m_PathID"] for x in prefab["m_Component"]
               if objects[x["component"]["m_PathID"]].type.name == "Transform")
root = transform(root_id)
children = [transform(x) for x in root["children"]]
hit = next(x for x in children if x["name"] == "Custom_Hit")
attack = next(x for x in children if x["name"] == "AtkEffectRoot")
# The 320-byte Appearance MonoBehaviour holds a PPtr to this root at byte 60.
# Immediately following are the serialized H/J/Z/G/E/S/F pivot PPtrs. All
# seven are null in the original custom prefab, so GetAtkEffectPivot(H) falls
# back to AtkEffectRoot exactly as CharacterAppearance.cs specifies.
appearance_id = next(x["component"]["m_PathID"] for x in prefab["m_Component"]
                     if objects[x["component"]["m_PathID"]].type.name == "MonoBehaviour"
                     and len(objects[x["component"]["m_PathID"]].get_raw_data()) == 320)
raw = objects[appearance_id].get_raw_data()
pointers = {name: {"file_id": struct.unpack_from("<i", raw, 60 + i * 12)[0],
                   "path_id": struct.unpack_from("<q", raw, 64 + i * 12)[0]}
            for i, name in enumerate(("atkEffectRoot", "H", "J", "Z", "G", "E", "S", "F"))}
assert pointers["atkEffectRoot"] == {"file_id": 0, "path_id": attack["path_id"]}
assert all(pointers[k] == {"file_id": 0, "path_id": 0} for k in ("H", "J", "Z", "G", "E", "S", "F"))
result = {
    "source": str(SOURCE), "source_sha256": sha, "prefab_path_id": 33898,
    "prefab": prefab["m_Name"], "root": root,
    "attack_root": attack, "hit_motion": hit,
    "hit_children": [transform(x) for x in hit["children"]],
    "appearance_monobehaviour_path_id": appearance_id,
    "appearance_pointers": pointers,
    "note": "Original custom prefab has null action-specific pivot fields; native GetAtkEffectPivot(H) falls back to AtkEffectRoot (-1.92, 2.52, 0).",
}
(HERE / "native-hit-pivot.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps({"attack_root": attack, "hit_motion": hit, "hit_children": result["hit_children"]}, indent=2))
