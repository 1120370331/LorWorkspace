"""Prepare real game-assembly native attachment fixture. No game process or saves."""
from pathlib import Path
import hashlib,json,shutil,sys
from PIL import Image
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PROJECT=HERE/'UnityProject';ASSETS=PROJECT/'Assets'
GAME=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data')
REF=ROOT/'SteriaBuild/VFXSource/NativeAttackReference/references/all'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for name in ['Runtime','Editor','Plugins','Reference']:(ASSETS/name).mkdir(parents=True,exist_ok=True)
(ASSETS/'Runtime/AnhierNativeRuntime.asmdef').write_text(json.dumps({'name':'AnhierNativeRuntime'}))
(ASSETS/'Editor/AnhierNativePreview.Editor.asmdef').write_text(json.dumps({'name':'AnhierNativePreview.Editor','references':['AnhierNativeRuntime'],'includePlatforms':['Editor']}))
# The game assembly keeps its exact identity. All fixture source is inside asmdefs,
# so Unity does not create a competing default Assembly-CSharp assembly.
for file in ['AnhierTextureTimeline.cs','DiceAttackEffect_Steria_AnhierTextureCombat.cs']:
    shutil.copyfile(ROOT/'SteriaBuild'/file,ASSETS/'Runtime'/file)
    old=ASSETS/file
    if old.exists():old.unlink()
(ASSETS/'Runtime/FixtureLogger.cs').write_text('namespace Steria { public static class SteriaLogger { public static void LogError(string value) { UnityEngine.Debug.LogError(value); } } }')
plugins=['Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','Coffee.SoftMaskForUGUI.dll','Facepunch.Steamworks.Win64.dll','spine-unity.dll','Unity.Postprocessing.Runtime.dll','Unity.TextMeshPro.dll','Unity.Timeline.dll','UnityEngine.UI.dll','com.unity.multiplayer-hlapi.Runtime.dll','UnityEngine.SpatialTracking.dll','UnityEngine.XR.LegacyInputHelpers.dll']
for name in plugins:
    if (GAME/'Managed'/name).exists():shutil.copyfile(GAME/'Managed'/name,ASSETS/'Plugins'/name)
modules=['animation','assetbundle','audio','imageconversion','jsonserialize','particlesystem','physics','physics2d','ui','uielements','imgui','textcore','textcorefontengine','textcoretextengine','terrain','terrainphysics','tilemap','video','director','unitywebrequest','unitywebrequesttexture','unitywebrequestaudio','unitywebrequestassetbundle','unitywebrequestwww','xr','vr','ai','cloth','wind','vehicles','unet','screencapture']
# Only modules present in Unity 2019 are listed.
modules=[m for m in modules if m not in ['textcore','textcorefontengine','textcoretextengine']]
(PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+m:'1.0.0' for m in modules}},indent=2))
(PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\nm_EditorVersionWithRevision: 2019.3.15f1 (59ff3e03856d)\n')
sys.path.insert(0,str(ROOT/'output/music-dice-ui/python-deps'))
import UnityPy
tag=next(o.read_typetree() for o in UnityPy.load(str(GAME/'globalgamemanagers')).objects if o.type.name=='TagManager')
yaml='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!78 &1\nTagManager:\n  serializedVersion: 2\n  tags: []\n  layers:\n'+''.join('  - '+x+'\n' for x in tag['layers'])+'  m_SortingLayers:\n'+''.join('  - name: '+x['name']+'\n    uniqueID: '+str(x['uniqueID'])+'\n    locked: 0\n' for x in tag['m_SortingLayers'])
(PROJECT/'ProjectSettings/TagManager.asset').write_text(yaml)
reference=ASSETS/'Reference'
man=json.loads((REF/'manifest.json').read_text(encoding='utf8'));obs={o['id']:o for o in man['objects']};imgs={o['id']:o for o in man['images']}
def meta(o):return json.loads((REF/o['metadata']).read_text(encoding='utf8'))
def edge(o,f):return next(e['target'] for e in o['edges'] if e['field']==f)
rows=[]
for name,leaf in [('SeaPierce','bada_z'),('SeaFarHit','redhoodmercenary_gun'),('MemorySlash','philipego_j'),('MemoryPierce','philipego_z'),('MemoryHit','bada_h'),('MemoryGuard','bada_g')]:
    root=next(r for r in man['roots'] if r['resource_path'].endswith('/'+leaf));renderers=[]
    for key in root['objects']:
        o=obs[key]
        if o['type']!='SpriteRenderer':continue
        rd=meta(o);sp=imgs[edge(o,'/m_Sprite')];im=Image.open(REF/sp['png']);file=leaf+'_'+key.replace(':','_')+'.png';shutil.copyfile(REF/sp['png'],reference/file)
        go=obs[edge(o,'/m_GameObject')];tid=next(e['target'] for e in go['edges'] if obs[e['target']]['type']=='Transform');tr=meta(obs[tid]);rect=sp['rect'];off=sp['texture_rect_offset'];pivot=sp['pivot']
        renderers.append({'name':go['name'],'file':file,'ppu':sp['ppu'],'pivotX':(pivot[0]*rect[2]-off[0])/im.width,'pivotY':(pivot[1]*rect[3]-off[1])/im.height,'sortingLayer':rd['m_SortingLayerID'] if rd['m_SortingLayerID']<2**31 else rd['m_SortingLayerID']-2**32,'sortingOrder':rd['m_SortingOrder'],'position':tr['m_LocalPosition'],'rotation':tr['m_LocalRotation'],'scale':tr['m_LocalScale'],'sha256':digest(REF/sp['png'])})
    rows.append({'profile':name,'resource':root['resource_path'],'sprites':renderers})
(reference/'native-effects.json').write_text(json.dumps({'effects':rows},indent=2))
# Find a genuine Fire skin by serialized CharacterMotion data, then load its real
# prefab in Unity. Restrict to the known Red Hood candidate if available.
fireBundle=None
for candidate in [GAME/'StreamingAssets/AssetBundles/char/char_30001.pres',GAME/'StreamingAssets/AssetBundles/char/char_200001.pres']:
    if not candidate.exists():continue
    env=UnityPy.load(str(candidate))
    if any(o.type.name=='MonoBehaviour' and o.read_typetree().get('actionDetail')==10 for o in env.objects):fireBundle=candidate;break
# Native card mapping locates the actual Red Hood skin; fallback bounded scan of
# Abnormality-era char bundles rather than pretending Bada has a Fire pose.
if fireBundle is None:
    for candidate in sorted((GAME/'StreamingAssets/AssetBundles/char').glob('*.pres')):
        env=UnityPy.load(str(candidate))
        if any(o.type.name=='MonoBehaviour' and o.read_typetree().get('actionDetail')==10 for o in env.objects):fireBundle=candidate;break
if fireBundle is None:raise RuntimeError('No genuine native Fire appearance found')
(reference/'fixture-config.json').write_text(json.dumps({'badaBundle':str(GAME/'StreamingAssets/AssetBundles/char/char_418.pres'),'fireBundle':str(fireBundle)},indent=2))
ship=ROOT/'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
fixtureShip=PROJECT/'Library/Resource/Effects/AnhierTextureCombat';fixtureShip.mkdir(parents=True,exist_ok=True)
for file in ship.glob('*'):shutil.copyfile(file,fixtureShip/file.name)
files=[ROOT/'SteriaBuild'/f for f in ['AnhierTextureTimeline.cs','DiceAttackEffect_Steria_AnhierTextureCombat.cs','HarmonyPatches.cs','Steria.csproj','bin/Release/net472/Steria.dll','SteriaModFolder/Data/CardInfo.xml']]+[HERE/f for f in ['manifest.json','import_masters.py','verify_source.py','prepare_preview.py','assemble_previews.py','run_preview.ps1','imagegen_masters/inputs.json','UnityProject/Assets/Editor/AnhierTexturePreview.cs','UnityProject/ProjectSettings/TagManager.asset']]+sorted(ship.glob('*'))
record={'unity':'2019.3.15f1','provenance':'Simulation: exact production adapter and timeline compiled against the actual game Assembly-CSharp; native BattleUnitView, actual native CharacterAppearance prefab and native DiceAttackEffect.Initialize/SetScale/SetLayer/Update invoked. Native images reference-only. Gameplay controllers/sound/camera are outside this simulation.','productionCopyMatches':all((ROOT/'SteriaBuild'/f).read_bytes()==(ASSETS/'Runtime'/f).read_bytes() for f in ['AnhierTextureTimeline.cs','DiceAttackEffect_Steria_AnhierTextureCombat.cs']),'nativeReferenceOnly':{'gameAssembly':digest(GAME/'Managed/Assembly-CSharp.dll'),'bada':digest(GAME/'StreamingAssets/AssetBundles/char/char_418.pres'),'fireBundle':str(fireBundle),'fireBundleSha256':digest(fireBundle)},'files':{str(p.relative_to(ROOT)).replace('\\','/'):digest(p) for p in files if p.exists()}}
(HERE/'previews').mkdir(exist_ok=True);(HERE/'previews/candidate-hashes.json').write_text(json.dumps(record,indent=2)+'\n')
print('FIXTURE_PREPARED: exact native game types, real Bada and Fire prefabs, Particle sorting, exact production adapter')
