"""Prepare an isolated Unity 2019 fixture from the exact production VFX source.

Uses read-only game references already used by Steria's native preview project.
Never opens Library of Ruina or touches its saves/mod installation.
"""
from pathlib import Path
import hashlib
import json
import shutil

HERE = Path(__file__).resolve().parent
ACE = HERE.parents[1]
ROOT = ACE.parent
PROJECT = HERE / "UnityFixture"
ASSETS = PROJECT / "Assets"
NATIVE = ROOT / "SteriaBuild/VFXSource/AnhierTextureCombat/UnityProject"


def copy(source: Path, target: Path):
    if not source.is_file():
        raise FileNotFoundError(source)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)


for folder in ("Runtime", "Editor", "Plugins", "Reference"):
    (ASSETS / folder).mkdir(parents=True, exist_ok=True)
copy(ACE / "src/TaffyVfx.cs", ASSETS / "Runtime/TaffyVfx.cs")
manifest = json.loads((NATIVE / "Packages/manifest.json").read_text(encoding="utf-8"))
manifest["dependencies"].pop("com.unity.modules.unet", None)  # Removed from this installed 2019.4 editor.
(PROJECT / "Packages/manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
copy(NATIVE / "ProjectSettings/TagManager.asset", PROJECT / "ProjectSettings/TagManager.asset")
(PROJECT / "ProjectSettings/ProjectVersion.txt").write_text(
    "m_EditorVersion: 2019.3.15f1\n", encoding="utf-8")
(ASSETS / "Runtime/TaffyVfx.Runtime.asmdef").write_text(json.dumps({"name": "TaffyVfx.Runtime"}), encoding="utf-8")
(ASSETS / "Editor/TaffyVfx.Editor.asmdef").write_text(json.dumps({
    "name": "TaffyVfx.Editor", "references": ["TaffyVfx.Runtime"], "includePlatforms": ["Editor"]
}), encoding="utf-8")
(ASSETS / "Runtime/FixtureMod.cs").write_text(
    "namespace AceTaffy { public static class AceTaffyMod { "
    "public static string RootPath { get; set; } "
    "public static void Log(string s) { UnityEngine.Debug.Log(\"[AceTaffy fixture] \"+s); } "
    "} }\n", encoding="utf-8")

plugins = [
    "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll", "Coffee.SoftMaskForUGUI.dll",
    "Facepunch.Steamworks.Win64.dll", "spine-unity.dll", "Unity.Postprocessing.Runtime.dll",
    "Unity.TextMeshPro.dll", "Unity.Timeline.dll", "UnityEngine.UI.dll",
    "com.unity.multiplayer-hlapi.Runtime.dll", "UnityEngine.SpatialTracking.dll",
    "UnityEngine.XR.LegacyInputHelpers.dll",
    "Mono.Cecil.dll", "MonoMod.Utils.dll", "MonoMod.RuntimeDetour.dll",
    "XGamingRuntime.dll", "Ookii.Dialogs.dll", "Unity.Mathematics.dll", "NAudio.dll",
]
managed = Path("D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed")
for name in plugins:
    source = NATIVE / "Assets/Plugins" / name
    if not source.is_file():
        source = ROOT / "SteriaBuild/Libs" / name
    if not source.is_file():
        source = managed / name
    copy(source, ASSETS / "Plugins" / name)
copy(ROOT / "SteriaBuild/Libs/0Harmony.dll", ASSETS / "Plugins/0Harmony.dll")
copy(ROOT / "SteriaBuild/Libs/BaseMod.dll", ASSETS / "Plugins/BaseMod.dll")
base_meta = ASSETS / "Plugins/BaseMod.dll.meta"
if base_meta.exists():
    base_meta.write_text(base_meta.read_text(encoding="utf-8").replace("validateReferences: 1", "validateReferences: 0"), encoding="utf-8")
for name in ("LorIdExtensions.dll", "MyJsonTool.dll"):
    copy(ROOT / "SteriaBuild/SteriaModFolder/Assemblies" / name, ASSETS / "Plugins" / name)
game_meta = ASSETS / "Plugins/Assembly-CSharp.dll.meta"
if game_meta.exists():
    game_meta.write_text(game_meta.read_text(encoding="utf-8").replace(
        "Editor: Editor\n    second:\n      enabled: 0",
        "Editor: Editor\n    second:\n      enabled: 1"), encoding="utf-8")
fingerprint = {
    "candidate": "Unity 2019 exact-source VFX factory fixture",
    "vfx_source": hashlib.sha256((ACE / "src/TaffyVfx.cs").read_bytes()).hexdigest(),
    "game_assembly": hashlib.sha256((ASSETS / "Plugins/Assembly-CSharp.dll").read_bytes()).hexdigest(),
    "base_mod": hashlib.sha256((ASSETS / "Plugins/BaseMod.dll").read_bytes()).hexdigest(),
    "production_copy_matches": (ACE / "src/TaffyVfx.cs").read_bytes() == (ASSETS / "Runtime/TaffyVfx.cs").read_bytes(),
    "actor_inputs": {
        name: hashlib.sha256((ACE / "AceTaffyMod/Resource/CharacterSkin/AceTaffy/ClothCustom" / (name + ".png")).read_bytes()).hexdigest()
        for name in ("Default", "Hit", "Penetrate", "Guard")
    },
    "modinfo": hashlib.sha256((ACE / "AceTaffyMod/Resource/CharacterSkin/AceTaffy/ModInfo.xml").read_bytes()).hexdigest(),
    "cardinfo": hashlib.sha256((ACE / "AceTaffyMod/Data/CardInfo.xml").read_bytes()).hexdigest(),
    "new_effect_inputs": {
        name: hashlib.sha256((ACE / "AceTaffyMod/Resource/Effects/AceTaffy" / (name + ".png")).read_bytes()).hexdigest()
        for name in ("SteamDetonation", "SteamPressureArc", "SteamBlunt", "SteamPierce", "SteamGuard", "SteamWisp")
    },
    "native_pivot_data": hashlib.sha256((HERE / "native-hit-pivot.json").read_bytes()).hexdigest(),
}
run_id = __import__("os").environ.get("TAFFY_ULTIMATE_RUN_ID")
candidate_path = HERE / "ultimate-v1" / run_id / "candidate.json" if run_id else HERE / "unity-fixture-candidate.json"
candidate_path.parent.mkdir(parents=True, exist_ok=True)
candidate_path.write_text(json.dumps(fingerprint, indent=2), encoding="utf-8")
print(json.dumps(fingerprint, indent=2))
