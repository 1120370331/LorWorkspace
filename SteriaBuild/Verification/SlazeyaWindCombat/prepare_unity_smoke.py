"""Main acceptance only: extend an IDLE existing visual fixture, never a running one.
Usage: python prepare_unity_smoke.py <visual UnityProject> <accepted bundle>
No Unity process is started. No game or mod deployment is performed.
"""
import json
import shutil
import sys
from pathlib import Path
from prepare_contract import ROOT, HERE, GEN

project=Path(sys.argv[1]).resolve()
bundle=Path(sys.argv[2]).resolve()
assert (project/'Assets/Runtime/SlazeyaWindVisual.cs').exists(), 'expected existing visual fixture'
assert bundle.is_file(), bundle
runtime=project/'Assets/Runtime';editor=project/'Assets/Editor';plugins=project/'Assets/Plugins'
plugins.mkdir(exist_ok=True)
for name in ('DiceAttackEffect_Steria_SlazeyaWind.cs','SlazeyaWindParticleAssets.cs','SlazeyaWindVisual.cs','SlazeyaWindImpact.cs'):
    shutil.copyfile(ROOT/'SteriaBuild'/name,runtime/name)
shutil.copyfile(GEN/'ProductionFactory.cs',runtime/'ProductionFactory.cs')
shutil.copyfile(HERE/'FixtureLogger.cs.txt',runtime/'FixtureLogger.cs')
shutil.copyfile(HERE/'UnityFactorySmoke.cs.txt',runtime/'SlazeyaWindFactoryFrameSmoke.cs')
shutil.copyfile(HERE/'UnityFactorySmokeEditor.cs.txt',editor/'SlazeyaWindFactorySmoke.cs')
game=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed')
for name in ('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','Coffee.SoftMaskForUGUI.dll','Facepunch.Steamworks.Win64.dll','spine-unity.dll','Unity.Postprocessing.Runtime.dll','Unity.TextMeshPro.dll','Unity.Timeline.dll','UnityEngine.UI.dll','com.unity.multiplayer-hlapi.Runtime.dll','UnityEngine.SpatialTracking.dll','UnityEngine.XR.LegacyInputHelpers.dll'):
    if (game/name).exists():shutil.copyfile(game/name,plugins/name)
for name in ('XGamingRuntime.dll','Ookii.Dialogs.dll','Unity.Mathematics.dll'):
    if (game/name).exists():shutil.copyfile(game/name,plugins/name)
for name in ('0Harmony.dll','Mono.Cecil.dll','Mono.Cecil.Rocks.dll','Mono.Cecil.Mdb.dll','Mono.Cecil.Pdb.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll'):
    shutil.copyfile(ROOT/'SteriaBuild/Libs'/name,plugins/name)
tag=ROOT/'SteriaBuild/VFXSource/AnhierTextureCombat/UnityProject/ProjectSettings/TagManager.asset'
shutil.copyfile(tag,project/'ProjectSettings/TagManager.asset')
cache=project/'Library/Assemblies/AB';cache.mkdir(parents=True,exist_ok=True)
shutil.copyfile(bundle,cache/'steria_slazeya_wind_combat')
print('UNITY_SMOKE_PREPARED; actual production files and native APIs; synthetic Bada transforms; run SlazeyaWindFactorySmoke.Run with -windSmokeOutput <fresh output>')
