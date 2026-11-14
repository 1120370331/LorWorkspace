"""Create isolated Unity 2019 fixture; Bada references never enter the bundle."""
from pathlib import Path
import json,shutil,hashlib,math
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];PROJECT=HERE/'UnityProject'
for d in ['Assets/Runtime','Assets/Editor','Assets/Reference','Assets/SlazeyaWindInputs','Assets/SkillSwordSlash/ContractV1','Packages','ProjectSettings']:(PROJECT/d).mkdir(parents=True,exist_ok=True)
source=ROOT/'SteriaBuild/SlazeyaWindVisual.cs';shutil.copyfile(source,PROJECT/'Assets/Runtime/SlazeyaWindVisual.cs')
skill=Path('C:/Users/rog/.codex/skills/unity-sword-slash-vfx')
shader=skill/'assets/unity/SwordSlashFlowBuiltInContractV1.shader'
shutil.copyfile(shader,PROJECT/'Assets/SkillSwordSlash/ContractV1'/shader.name)
swept=skill/'assets/library/unity2019/Candidate/Shaders/SwordSlashSweptWedgeBuiltInV1.shader'
shutil.copyfile(swept,PROJECT/'Assets/SlazeyaWindInputs'/swept.name)
for name in ('wind-wedge-v3.png','wind-flow-cc0.png','wind-mote-cc0.png','wind-impact-ring-mit.png'):
 shutil.copyfile(HERE/'source_assets'/name,PROJECT/'Assets/SlazeyaWindInputs'/name)
(PROJECT/'Assets/Runtime/SlazeyaWind.Runtime.asmdef').write_text(json.dumps({'name':'SlazeyaWind.Runtime'}))
(PROJECT/'Assets/Editor/SlazeyaWind.Editor.asmdef').write_text(json.dumps({'name':'SlazeyaWind.Editor','references':['SlazeyaWind.Runtime'],'includePlatforms':['Editor']}))
modules=['animation','assetbundle','audio','imageconversion','jsonserialize','particlesystem','physics','physics2d','ui','uielements','imgui','terrain','terrainphysics','tilemap','video','director','unitywebrequest','unitywebrequesttexture','unitywebrequestassetbundle','xr','vr','ai','cloth','wind','vehicles','screencapture']
(PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+m:'1.0.0' for m in modules}},indent=2))
(PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\nm_EditorVersionWithRevision: 2019.3.15f1 (59ff3e03856d)\n')
for f in ['ProjectSettings.asset','QualitySettings.asset','GraphicsSettings.asset']:
 if not (PROJECT/'ProjectSettings'/f).exists():shutil.copyfile(HERE.parent/'VeliaPotentialObservation/UnityProject/ProjectSettings'/f,PROJECT/'ProjectSettings'/f)
shutil.copyfile(HERE.parent/'AnhierTextureCombat/UnityProject/ProjectSettings/TagManager.asset',PROJECT/'ProjectSettings/TagManager.asset')
ref=HERE.parent/'AnhierTextureCombat/UnityProject/Assets/Reference';skin=json.loads((ref/'native-skins.json').read_text())['skins'][0]
motions=[]
for m in skin['motions']:
 parts=[]
 for part in m['parts']:
  if not part['enabled'] or part['spriteType']==9:continue
  shutil.copyfile(ref/part['file'],PROJECT/'Assets/Reference'/part['file'])
  mat=part['matrix'];parts.append(dict(file=part['file'],ppu=part['ppu'],pivot=part['pivot'],order=part['order'],position=[mat[0][3],mat[1][3],0],scale=[mat[0][0],mat[1][1],1]))
 motions.append(dict(action=m['action'],parts=parts))
(PROJECT/'Assets/Reference/bada-fixture.json').write_text(json.dumps({'motions':motions},indent=2))
brush=HERE/'source_assets/wind-wedge-v3.png'
record={'status':'candidate-not-promoted-to-skill-library','art':'Original mesh silhouettes and native Shuriken. Slash uses SweptWedge; pressure, thrust, guard and projectile use FormedArc. Inspector-authored materials are preserved.',
 'brush':{'path':str(brush.relative_to(HERE)),'sha256':hashlib.sha256(brush.read_bytes()).hexdigest(),'origin':'Built-in ImageGen edit','prompt_record':'source_assets/wind-wedge-v3.json',
 'role':'Local alpha wedge, not a complete slash image; original bytes preserved, linear/Clamp/512 import/mipmaps'},
 'shaders':[{'source':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'license':'Local skill fixed infrastructure, unchanged'} for p in (shader,swept)],
 'licensed_textures':[
  {'path':'source_assets/wind-flow-cc0.png','sha256':hashlib.sha256((HERE/'source_assets/wind-flow-cc0.png').read_bytes()).hexdigest(),'author':'Screaming Brain Studios','source':'https://opengameart.org/content/700-noise-textures','license':'CC0-1.0','proof':'source_assets/WindFlow-CC0-License.txt','role':'linear Repeat highlight; border delta max 3/255'},
  {'path':'source_assets/wind-mote-cc0.png','sha256':hashlib.sha256((HERE/'source_assets/wind-mote-cc0.png').read_bytes()).hexdigest(),'author':'Kenney','source':'https://kenney.nl/assets/particle-pack','license':'CC0-1.0','proof':'source_assets/Kenney-CC0-License.txt','role':'Unchanged transparent light_01 auxiliary mote'},
  {'path':'source_assets/wind-impact-ring-mit.png','sha256':hashlib.sha256((HERE/'source_assets/wind-impact-ring-mit.png').read_bytes()).hexdigest(),'author':'haw2fregel','source':'https://github.com/haw2fregel/VFXTextureMaker/tree/eced62d6f71520afc12cf23f239e04b85c4838cf','license':'MIT','proof':'source_assets/VFXTextureMaker-MIT-License.txt','catalog_id':'incoming.vfx-texture-maker.texture-candidate.circle-01.2bb8978990','role':'Unchanged circle_01.png; Clamp, sRGB, no mipmaps, uncompressed; expanding impact halo; sample PNG only, no Unity 2022 code imported','status':'candidate; local Unity 2019 import and visual gates required; no library promotion'}],
 'reference':{'source':str(ref/'native-skins.json'),'sha256':hashlib.sha256((ref/'native-skins.json').read_bytes()).hexdigest(),'usage':'Bada original matrices/PPU/pivot and local images: preview only, excluded from AssetBundle'}}
(HERE/'provenance.json').write_text(json.dumps(record,indent=2)+'\n')
print('SLAZEYA project prepared; fixed shaders and four current texture inputs copied')
