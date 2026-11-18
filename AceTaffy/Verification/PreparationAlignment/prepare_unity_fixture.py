"""Prepare an isolated Unity 2019 project with the exact production alignment file."""
import hashlib
import json
import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
ACE = HERE.parents[1]
REFERENCE = ACE / "Verification/Vfx/UnityFixture"
PROJECT = HERE / "UnityFixture"

for folder in ("ProjectSettings", "Packages", "Assets/Plugins"):
    source = REFERENCE / folder
    if not source.is_dir():
        raise FileNotFoundError(source)
    shutil.copytree(source, PROJECT / folder, dirs_exist_ok=True)
runtime = PROJECT / "Assets/Runtime"
editor = PROJECT / "Assets/Editor"
runtime.mkdir(parents=True, exist_ok=True)
editor.mkdir(parents=True, exist_ok=True)
shutil.copyfile(HERE / "TaffyPreparationFixture.cs", editor / "TaffyPreparationFixture.cs")
source = ACE / "src/TaffyPreparationAlignment.cs"
shutil.copyfile(source, runtime / source.name)
(runtime / "FixtureMod.cs").write_text(
    "namespace AceTaffy { public sealed class AceTaffyMod { "
    "public static string RootPath { get; set; } } }\n", encoding="utf-8")
(runtime / "TaffyAlignment.Runtime.asmdef").write_text(
    json.dumps({"name": "TaffyAlignment.Runtime"}), encoding="utf-8")
(editor / "TaffyAlignment.Editor.asmdef").write_text(
    json.dumps({"name": "TaffyAlignment.Editor",
                "references": ["TaffyAlignment.Runtime"], "includePlatforms": ["Editor"]}),
    encoding="utf-8")
fingerprint = {
    "production_source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
    "fixture_source_sha256": hashlib.sha256((runtime / source.name).read_bytes()).hexdigest(),
    "game_assembly_sha256": hashlib.sha256(
        (PROJECT / "Assets/Plugins/Assembly-CSharp.dll").read_bytes()).hexdigest(),
}
if fingerprint["production_source_sha256"] != fingerprint["fixture_source_sha256"]:
    raise ValueError("Fixture source does not match production")
(HERE / "candidate.json").write_text(
    json.dumps(fingerprint, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(fingerprint, ensure_ascii=False))
