"""Isolated real-DLL/native-Unity smoke; never writes the game or authoring project."""
from pathlib import Path
import hashlib,json,shutil,sys

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
out=ROOT/'output/antie-ice-indigo'/sys.argv[1]
project=out/'UnityProject'
assert not out.exists(),out
for folder in ['Assets/Plugins','Assets/Runtime','Assets/Editor','Packages','ProjectSettings','bundles']:
    (project/folder).mkdir(parents=True,exist_ok=True)
game=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed')
for name in ['Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','Coffee.SoftMaskForUGUI.dll','Facepunch.Steamworks.Win64.dll','spine-unity.dll','Unity.Postprocessing.Runtime.dll','Unity.TextMeshPro.dll','Unity.Timeline.dll','UnityEngine.UI.dll','com.unity.multiplayer-hlapi.Runtime.dll','UnityEngine.SpatialTracking.dll','UnityEngine.XR.LegacyInputHelpers.dll','XGamingRuntime.dll','Ookii.Dialogs.dll','Unity.Mathematics.dll']:
    if (game/name).exists():shutil.copy2(game/name,project/'Assets/Plugins'/name)
for name in ['0Harmony.dll','BaseMod.dll','Mono.Cecil.dll','Mono.Cecil.Rocks.dll','Mono.Cecil.Mdb.dll','Mono.Cecil.Pdb.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll','MonoMod.Common.dll']:
    source=ROOT/'SteriaBuild/Libs'/name
    if source.exists():shutil.copy2(source,project/'Assets/Plugins'/name)
framework=Path('D:/Game Center/Steam/steamapps/workshop/content/1256670/2603522001/Assemblies')
for name in ['LorIdExtensions.dll','NAudio.dll','BaseBridge.dll','1ExtendedLoader.dll','10EnumExtender.dll','MyJsonTool.dll','UnitRenderUtils.dll']:
    source=framework/'1FrameworkAssemblies'/name
    assert source.exists(),source
    shutil.copy2(source,project/'Assets/Plugins'/name)
for name in ['1FrameworkLoader.dll','00HarmonyPreloader.dll']:
    if (framework/name).exists():shutil.copy2(framework/name,project/'Assets/Plugins'/name)
dll=Path(sys.argv[2]).resolve(strict=True) if len(sys.argv)>2 else ROOT/'output/antie-ice-indigo/build-candidate-07/Steria.dll'
shutil.copy2(dll,project/'Assets/Plugins/Steria.dll')
base=HERE.parent/'UnityProject'
for name in ['Packages','ProjectSettings']:
    for source in (base/name).glob('*'):
        if source.is_file():shutil.copy2(source,project/name/source.name)
shutil.copy2(ROOT/'SteriaBuild/VFXSource/SlazeyaWindCombat/UnityProject/Packages/manifest.json',project/'Packages/manifest.json')
shutil.copy2(ROOT/'SteriaBuild/VFXSource/SlazeyaWindCombat/UnityProject/ProjectSettings/TagManager.asset',project/'ProjectSettings/TagManager.asset')
# A native fixture uses the player's UI/TMP/Timeline DLLs. Editor UPM versions
# would duplicate those assemblies and do not belong in this test environment.
(project/'Packages/packages-lock.json').unlink(missing_ok=True)
# This is the established isolated native entry mechanism, parameterized for Antie.
entry=(ROOT/'SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/deployment/NativeEntry.cs.txt').read_text(encoding='utf-8-sig')
entry=entry.replace('WaterThunderNativeEntry','AntieNativeEntry').replace('DiceAttackEffect_Steria_SlazeyaWaterThunderBase','DiceAttackEffect_Steria_AntieIceIndigoBase').replace('WaterThunderNativeSmoke','AntieNativeSmoke').replace('WaterThunder.Native.Output','Antie.Native.Output')
(project/'Assets/Editor/AntieNativeEntry.cs').write_text(entry,encoding='utf-8')
shutil.copy2(HERE/'NativeSmoke.cs.txt',project/'Assets/Runtime/AntieNativeSmoke.cs')
(project/'Assets/Runtime/Antie.NativeFixture.Runtime.asmdef').write_text(json.dumps({'name':'Antie.NativeFixture.Runtime'}))
(project/'Assets/Editor/Antie.NativeFixture.Editor.asmdef').write_text(json.dumps({'name':'Antie.NativeFixture.Editor','references':['Antie.NativeFixture.Runtime'],'includePlatforms':['Editor']}))
(out/'bundles').mkdir()
bundle=HERE.parent/'evidence/candidate-05-convex-to-camera/bundle/steria_antie_ice_indigo_combat'
shutil.copy2(bundle,out/'bundles'/bundle.name)
(out/'inputs.json').write_text(json.dumps({'project':str(project),'dll':str(dll),'dll_sha256':hashlib.sha256(dll.read_bytes()).hexdigest(),'bundle_sha256':hashlib.sha256(bundle.read_bytes()).hexdigest()},indent=2))
print('ANTIE_NATIVE_PREPARED',project)
