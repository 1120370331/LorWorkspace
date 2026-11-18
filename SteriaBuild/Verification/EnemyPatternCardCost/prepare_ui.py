"""Prepare an isolated Unity project for the real-game cost binding smoke."""
from pathlib import Path
import hashlib
import json
import shutil

repo = Path(__file__).resolve().parents[3]
out = repo / "output/enemy-pattern-cost"
project = out / "UnityProject"
editor = project / "Assets/Editor"
checks = out / "checks"
for path in (editor, project / "Packages", project / "ProjectSettings", checks):
    path.mkdir(parents=True, exist_ok=True)
for path in (repo / "SteriaBuild/Verification/EnemyPatternCardCost/bin/Release/net472").iterdir():
    if path.is_file():
        shutil.copy2(path, checks / path.name)
(project / "Packages/manifest.json").write_text(json.dumps({"dependencies": {
    "com.unity.textmeshpro": "2.0.1", "com.unity.ugui": "1.0.0",
    "com.unity.modules.ui": "1.0.0", "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.animation": "1.0.0", "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0", "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0"
}}), encoding="utf-8")
(project / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 2019.3.15f1\n")
(editor / "EnemyPatternCostEntry.cs").write_text('''
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class EnemyPatternCostEntry {
    public static void Run() {
        try {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../checks"));
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string file = Path.Combine(dir, new AssemblyName(a.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.LoadFrom(file) : null;
            };
            var assembly = Assembly.LoadFrom(Path.Combine(dir, "EnemyPatternCostChecks.exe"));
            assembly.GetType("NativeUiChecks").GetMethod("Run").Invoke(null, null);
            EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
''', encoding="utf-8")
paths = ["EnemyPatternCardCost.cs", "SlazeyaAbilities.cs", "ChristashaAbilities.cs",
         "VeliaEgoBossAbilities.cs", "ChristashaEgoBoss.cs", "bin/Release/net472/Steria.dll"]
(out / "candidate.json").write_text(json.dumps({name: hashlib.sha256(
    (repo / "SteriaBuild" / name).read_bytes()).hexdigest() for name in paths}, indent=2))
print(project)
