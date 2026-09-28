"""Focused source/assets contract for the first die's exclusive VFX."""
from pathlib import Path
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET

ACE = Path(__file__).resolve().parents[2]
BASE = ACE / "Verification/Vfx/ultimate-v1/baseline"
PKG = ACE / "AceTaffyMod"
SOURCE = ACE / "src/TaffyVfx.cs"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check(ok, label):
    if not ok:
        raise AssertionError(label)
    print("PASS", label)


cards = ET.parse(PKG / "Data/CardInfo.xml").getroot()
pages = cards.findall(".//Card")
det = [p for p in pages if p.get("ID") == "1883009"]
check(len(det) == 1, "one card 1883009")
check(det[0].find("Spec") is not None and det[0].find("Spec").get("Cost") == "4", "ultimate cost 4")
dice = det[0].findall(".//Behaviour")
check(len(dice) == 3, "ultimate retains three dice")
check(dice[0].get("EffectRes") == "AceTaffy_SteamDetonation" and dice[0].get("Motion") == "H"
      and dice[0].get("Min") == "6" and dice[0].get("Dice") == "18", "first 6-18 H alone owns detonation")
check(dice[1].get("EffectRes") == "AceTaffy_SteamGuard" and dice[2].get("EffectRes") == "AceTaffy_SteamBlunt",
      "second guard and third ordinary blunt preserved")
check(sum(d.get("EffectRes") == "AceTaffy_SteamDetonation" for p in pages for d in p.findall(".//Behaviour")) == 1,
      "detonation resource occurs on exactly one die")

asset_manifest = json.loads((ACE / "ArtSource/Effects/ultimate-v1/asset-manifest.json").read_text(encoding="utf-8"))
for row in asset_manifest["files"]:
    if "production" in row:
        path = ACE.parent / row["production"]
        check(sha(path) == row["sha256"], path.name + " matches approved original RGBA")

check(sha(ACE / "src/TaffyActions.cs") == json.loads((BASE / "manifest.json").read_text(encoding="utf-8"))["src/TaffyActions.cs"],
      "finisher ActionScript source unchanged")
ordinary_baseline = json.loads((ACE / "Verification/Vfx/preview/manifest.json").read_text(encoding="utf-8"))["inputs_sha256"]
for name in ("SteamBlunt", "SteamPierce", "SteamGuard", "SteamWisp"):
    key = next(k for k in ordinary_baseline if k.endswith(name + ".png"))
    check(sha(PKG / "Resource/Effects/AceTaffy" / (name + ".png")) == ordinary_baseline[key],
          name + " ordinary mother unchanged")
baseline_card = ET.parse(BASE / "AceTaffyMod/Data/CardInfo.xml").getroot()
check([tuple(d.attrib.items()) for d in baseline_card.findall(".//Behaviour")] ==
      [tuple(d.attrib.items()) for d in cards.findall(".//Behaviour")], "dice attributes unchanged from approved baseline")

if "--after" in sys.argv:
    source = SOURCE.read_text(encoding="utf-8")
    check('"SteamPressureArc"' in source and 'case "SteamPressureArc"' in source, "arc prewarm and PPU route")
    check("_pressureArc" in source and "SampleDetonation" in source, "separate per-instance arc sampler")
    check("BrassFleck" not in source, "six fake wisp sparks removed")
    check("private void OnDisable()" in source and "_pressureArc.enabled = false" in source,
          "cancel path clears pressure arc")
    check("DiceAttackEffect_AceTaffy_SteamDetonation" in source and "CreateBehaviourEffectPrefix" in source,
          "real factory name and typed dispatch retained")
