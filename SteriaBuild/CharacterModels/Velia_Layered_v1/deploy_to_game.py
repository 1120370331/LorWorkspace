"""Package the accepted art for guest/projection and deploy only its assets/XML.

No DLL, combat data, saves, or VFX are changed. Every overwritten file is backed up.
"""
import argparse
import copy
import hashlib
import json
import re
import shutil
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
MOD = ROOT.parents[1] / "SteriaModFolder"
PROJECTION = "Velia_Layered_v1_Projection"
LIBRARY_XML = Path("Data/EquipPage_Librarian.xml")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def ensure(condition, message):
    if not condition:
        raise RuntimeError(message)


def confined(root, relative):
    path = (root / relative).resolve()
    ensure(path.is_relative_to(root.resolve()), f"Path outside mod root: {path}")
    return path


def patched_book(data):
    text = data.decode("utf-8-sig")
    pattern = r'(<Book\s+ID="99000004"\s*>)(.*?)(</Book>)'
    matches = list(re.finditer(pattern, text, flags=re.S))
    ensure(len(matches) == 1, "Expected one Velia librarian book 99000004")
    match = matches[0]
    block = match.group(2)
    skins = re.findall(r'<CharacterSkin>([^<]+)</CharacterSkin>', block)
    ensure(skins in [["Velia"], [PROJECTION]], f"Unexpected Velia skin mapping: {skins}")
    ensure("<CharacterSkinType>Custom</CharacterSkinType>" in block, "Velia book is not Custom")
    updated = re.sub(r'<CharacterSkin>[^<]+</CharacterSkin>',
                     f'<CharacterSkin>{PROJECTION}</CharacterSkin>', block, count=1)
    output = text[:match.start(2)] + updated + text[match.end(2):]
    # Compare XML after normalizing exactly the single authorized field.
    before, after = ET.fromstring(text), ET.fromstring(output)
    before.find("Book[@ID='99000004']/CharacterSkin").text = PROJECTION
    ensure(ET.tostring(before) == ET.tostring(after), "Unexpected book-data change")
    encoded = output.encode("utf-8")
    return (b'\xef\xbb\xbf' + encoded) if data.startswith(b'\xef\xbb\xbf') else encoded


def package():
    build = ROOT / "build"
    manifest = json.loads((build / "build_manifest.json").read_text(encoding="utf-8"))
    verification = json.loads((build / "verification.json").read_text(encoding="utf-8"))
    ensure(verification["result"] == "PASS" and verification["candidate"] == manifest["candidate"],
           "Art candidate has no matching verification")
    for relative, digest in manifest["sources"].items():
        ensure(sha(ROOT / relative) == digest, f"Stale art build: {relative}")
    actions = manifest["actions"] + list(manifest["aliases"])
    package_root = build / "runtime"
    guest = package_root / "Resource/CharacterSkin/Velia"
    project = package_root / "Resource/CharacterSkin" / PROJECTION
    for path in [guest / "ClothCustom", project / "ClothCustom"]:
        path.mkdir(parents=True, exist_ok=True)
    generated = []
    for action in actions:
        src = build / "assembled" / (action + ".png")
        with Image.open(src) as image:
            ensure(image.mode == "RGBA" and image.size == (512,512), f"Bad guest PNG: {action}")
        target = guest / "ClothCustom" / (action + ".png")
        shutil.copy2(src,target)
        generated.append(target)
        # Clear any prior foreground image under the guest name to avoid a
        # duplicate arm over the already assembled guest frame.
        target = guest / "ClothCustom" / (action + "_front.png")
        Image.new("RGBA",(512,512)).save(target)
        generated.append(target)
        for suffix in ["", "_front"]:
            src = build / PROJECTION / "ClothCustom" / (action + suffix + ".png")
            target = project / "ClothCustom" / src.name
            shutil.copy2(src,target)
            generated.append(target)
    projection_xml = ET.parse(build / PROJECTION / "ModInfo.Xml").getroot()
    ensure(projection_xml.findtext("ClothInfo/Name") == PROJECTION, "Wrong projection name")
    guest_xml = copy.deepcopy(projection_xml)
    guest_xml.find("ClothInfo/Name").text = "Velia"
    for action in actions:
        node = guest_xml.find(f"ClothInfo/{action}/Head")
        node.attrib.update(head_x="0",head_y="0",rotation="0",head_enable="False")
        ensure(projection_xml.find(f"ClothInfo/{action}/Head").get("head_enable") == "True",
               f"Projection head disabled: {action}")
    for path, xml in [(guest,guest_xml),(project,projection_xml)]:
        ET.indent(xml)
        ET.ElementTree(xml).write(path / "ModInfo.Xml",encoding="utf-8",xml_declaration=True)
        generated.append(path / "ModInfo.Xml")
        shutil.copy2(build / "assembled/Default.png",path / "Thumb.png")
        generated.append(path / "Thumb.png")
    xml_target = package_root / LIBRARY_XML
    xml_target.parent.mkdir(parents=True,exist_ok=True)
    xml_target.write_bytes(patched_book((MOD / LIBRARY_XML).read_bytes()))
    generated.append(xml_target)
    files = {str(p.relative_to(package_root)).replace("\\","/"):sha(p) for p in generated}
    runtime = {"art_candidate":manifest["candidate"],"files":files,
        "guest_skin":"Velia", "projection_skin":PROJECTION,
        "enemy_books":[4,5,6], "librarian_book":99000004,
        "guest_head":"baked from layered authoring; native head disabled",
        "projection_head":"wearer's native head enabled",
        "actions":actions,"dll_changed":False}
    (package_root / "runtime_manifest.json").write_text(json.dumps(runtime,indent=2)+"\n",encoding="utf-8")
    return package_root, runtime


def deploy(package_root, runtime, game_mods):
    requested = [("workspace",MOD)] + [(f"game_{i+1}",p) for i,p in enumerate(game_mods)]
    aliases = [{"label":label,"requested":str(path.absolute()),"resolved":str(path.resolve())}
               for label,path in requested]
    # Steam paths can be junction aliases of the same directory. Back up and
    # write each real target once, then verify every requested alias.
    unique = {}
    for label,path in requested:
        unique.setdefault(str(path.resolve()).lower(),(label,path.resolve()))
    roots = list(unique.values())
    # Preflight all destinations before any writes.
    source_before = (MOD / LIBRARY_XML).read_bytes()
    for label,root in roots:
        ensure(root.is_dir(),f"Missing mod directory: {root}")
        stage = ET.parse(root / "StageModInfo.xml").getroot()
        ensure(stage.findtext("Workshop/ID") == "SteriaBuilding", f"Wrong mod identity: {root}")
        ensure((root / LIBRARY_XML).read_bytes() in [source_before,(package_root / LIBRARY_XML).read_bytes()],
               f"Librarian XML differs from the inspected candidate: {root}")
        enemy = ET.parse(root / "Data/EquipPage_Enemy.xml").getroot()
        for book in [4,5,6]:
            ensure(enemy.findtext(f"Book[@ID='{book}']/CharacterSkin") == "Velia",f"Unexpected guest skin: {root}, {book}")
        for relative in runtime["files"]:
            confined(root,relative)
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    evidence = ROOT / "deployments" / timestamp
    evidence.mkdir(parents=True)
    report = {"art_candidate":runtime["art_candidate"], "timestamp_utc":timestamp,
        "targets":[], "path_aliases":aliases, "backup_root":str(evidence / "backup"), "status":"preparing",
        "runtime_battle_smoke":"not run; game was closed during deployment"}
    operations = []
    for label,root in roots:
        baseline = {r:sha(root/r) for r in ["Assemblies/Steria.dll","Data/EquipPage_Enemy.xml","Data/CardInfo.xml"]}
        entry = {"label":label,"root":str(root),"unchanged_baseline":baseline,"files":[]}
        report["targets"].append(entry)
        for relative,digest in runtime["files"].items():
            dest = confined(root,relative)
            old = sha(dest) if dest.is_file() else None
            backup = evidence / "backup" / label / relative
            if old is not None:
                backup.parent.mkdir(parents=True,exist_ok=True)
                shutil.copy2(dest,backup)
                ensure(sha(backup)==old,f"Backup mismatch: {dest}")
            item={"path":relative,"before":old,"after":digest,"backup":str(backup) if old else None}
            entry["files"].append(item)
            operations.append((root,dest,relative,backup,old,digest))
    report_path=evidence / "deployment.json"
    report_path.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    written=[]
    try:
        for root,dest,relative,backup,old,digest in operations:
            ensure((sha(dest) if dest.is_file() else None)==old,f"Target changed after backup: {dest}")
            dest.parent.mkdir(parents=True,exist_ok=True)
            written.append((root,dest,backup,old))
            shutil.copy2(package_root / relative,dest)
            ensure(sha(dest)==digest,f"Deployment mismatch: {dest}")
        for entry in report["targets"]:
            root=Path(entry["root"])
            for relative,digest in entry["unchanged_baseline"].items():
                ensure(sha(root/relative)==digest,f"Unrelated file changed: {root/relative}")
            entry["sha256_verification"]="PASS"
        for alias in aliases:
            path=Path(alias["requested"])
            ensure(str(path.resolve())==alias["resolved"],f"Target junction changed: {path}")
            for relative,digest in runtime["files"].items():
                ensure(sha(confined(path,relative))==digest,f"Alias verification mismatch: {path/relative}")
            alias["sha256_verification"]="PASS"
        report["status"]="deployed_verified"
    except Exception as error:
        for root,dest,backup,old in reversed(written):
            ensure(dest.resolve().is_relative_to(root.resolve()),"Unsafe rollback path")
            if old is not None:shutil.copy2(backup,dest)
            elif dest.is_file():dest.unlink()
        report["status"]="rolled_back"
        report["error"]=str(error)
        raise
    finally:
        report_path.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"status":report["status"],"files_per_target":len(runtime["files"]),
        "targets":[str(p) for _,p in roots],"report":str(report_path)},indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-mod",type=Path,action="append",default=[])
    args=parser.parse_args()
    root,manifest=package()
    if args.game_mod:deploy(root,manifest,args.game_mod)
    else:print(f"Packaged {len(manifest['files'])} files at {root}; nothing deployed.")
