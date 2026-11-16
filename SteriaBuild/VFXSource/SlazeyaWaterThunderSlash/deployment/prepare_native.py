from pathlib import Path
import json,shutil,sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
release=ROOT/'output/slazeya-water-thunder'/sys.argv[1];out=ROOT/'output/slazeya-water-thunder'/sys.argv[2];project=out/'UnityProject'
assert not out.exists(),out
for rel in ['Assets/Plugins','Assets/Runtime','Assets/Editor','Packages','ProjectSettings','bundles']:(project/rel).mkdir(parents=True,exist_ok=True)
game=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed')
for name in ['Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','Coffee.SoftMaskForUGUI.dll','Facepunch.Steamworks.Win64.dll','spine-unity.dll','Unity.Postprocessing.Runtime.dll','Unity.TextMeshPro.dll','Unity.Timeline.dll','UnityEngine.UI.dll','com.unity.multiplayer-hlapi.Runtime.dll','UnityEngine.SpatialTracking.dll','UnityEngine.XR.LegacyInputHelpers.dll','XGamingRuntime.dll','Ookii.Dialogs.dll','Unity.Mathematics.dll']:
    if (game/name).exists():shutil.copy2(game/name,project/'Assets/Plugins'/name)
for name in ['0Harmony.dll','BaseMod.dll','Mono.Cecil.dll','Mono.Cecil.Rocks.dll','Mono.Cecil.Mdb.dll','Mono.Cecil.Pdb.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll','MonoMod.Common.dll']:
    p=ROOT/'SteriaBuild/Libs'/name
    if p.exists():shutil.copy2(p,project/'Assets/Plugins'/name)
framework=Path('D:/Game Center/Steam/steamapps/workshop/content/1256670/2603522001/Assemblies')
for name in ['LorIdExtensions.dll','NAudio.dll','BaseBridge.dll','1ExtendedLoader.dll','10EnumExtender.dll','MyJsonTool.dll','UnitRenderUtils.dll']:
    p=framework/'1FrameworkAssemblies'/name;assert p.exists(),p;shutil.copy2(p,project/'Assets/Plugins'/name)
for name in ['1FrameworkLoader.dll','00HarmonyPreloader.dll']:
    p=framework/name
    if p.exists():shutil.copy2(p,project/'Assets/Plugins'/name)
for name in ['Steria.dll','Steria.pdb']:shutil.copy2(release/'SteriaBuild/bin/Release/net472'/name,project/'Assets/Plugins'/name)
base=ROOT/'SteriaBuild/VFXSource/SlazeyaWindCombat/UnityProject'
shutil.copy2(base/'Packages/manifest.json',project/'Packages/manifest.json')
for p in (base/'ProjectSettings').glob('*'):
    if p.is_file():shutil.copy2(p,project/'ProjectSettings'/p.name)
shutil.copy2(HERE/'NativeEntry.cs.txt',project/'Assets/Editor/WaterThunderNativeEntry.cs')
shutil.copy2(HERE/'NativeSmoke.cs.txt',project/'Assets/Runtime/WaterThunderNativeSmoke.cs')
(project/'Assets/Runtime/WaterThunder.NativeFixture.Runtime.asmdef').write_text(json.dumps({'name':'WaterThunder.NativeFixture.Runtime'}))
(project/'Assets/Editor/WaterThunder.NativeFixture.Editor.asmdef').write_text(json.dumps({'name':'WaterThunder.NativeFixture.Editor','references':['WaterThunder.NativeFixture.Runtime'],'includePlatforms':['Editor']}))
(out/'bundles').mkdir()
for p in (release/'payload/Assemblies/AB').iterdir():shutil.copy2(p,out/'bundles'/p.name)
shutil.copy2(ROOT/'SteriaBuild/SteriaModFolder/Assemblies/AB/steria_slazeya_wind_combat',out/'bundles/steria_slazeya_wind_combat')
(out/'inputs.json').write_text(json.dumps({'release':str(release),'project':str(project),'dll':str(project/'Assets/Plugins/Steria.dll')},indent=2))
print('ACTUAL_DLL_NATIVE_FIXTURE_READY',project)
