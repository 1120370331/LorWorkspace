from pathlib import Path
import shutil,json,tarfile,hashlib
repo=Path(__file__).resolve().parents[3]
out=repo/'output/flow-rework'; project=out/'UnityProject'; assets=project/'Assets'
for p in [assets/'Editor',assets/'FixtureFonts',project/'Packages',project/'ProjectSettings',out/'checks/bin/Debug/net472']:p.mkdir(parents=True,exist_ok=True)
source=repo/'SteriaBuild/Verification/FlowChecks'
for p in (source/'bin/Debug/net472').iterdir():
 if p.is_file():shutil.copy2(p,out/'checks/bin/Debug/net472'/p.name)
shutil.copy2(source/'FlowFixture.cs.txt',assets/'Editor/FlowFixture.cs')
(assets/'Editor/FlowFixture.asmdef').write_text(json.dumps({'name':'Steria.FlowFixture.Editor','includePlatforms':['Editor']}))
modules=['imageconversion','ui','audio','animation','physics','particlesystem','assetbundle']
deps={'com.unity.textmeshpro':'2.0.1','com.unity.ugui':'1.0.0',**{'com.unity.modules.'+m:'1.0.0' for m in modules}}
(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}))
(project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\n')
shutil.copy2('C:/Windows/Fonts/simhei.ttf',assets/'FixtureFonts/simhei.ttf')
# TextMeshPro package resources are fixtures only, never shipped with the mod.
candidates=[project/'Library/PackageCache/com.unity.textmeshpro@2.0.1/Package Resources/TMP Essential Resources.unitypackage',repo/'output/music-dice-ui/implementation/UnityProject/Library/PackageCache/com.unity.textmeshpro@2.0.1/Package Resources/TMP Essential Resources.unitypackage']
pkg=next((p for p in candidates if p.exists()),None)
if pkg:
 with tarfile.open(pkg) as archive:
  members={m.name:m for m in archive.getmembers()}
  for name,member in members.items():
   if not name.endswith('/pathname'):continue
   target=(project/archive.extractfile(member).read().decode().strip()).resolve()
   if not target.is_relative_to(project.resolve()):raise ValueError(target)
   prefix=name.rsplit('/',1)[0]
   asset=members.get(prefix+'/asset')
   if not asset or not asset.isfile():continue
   target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.extractfile(asset).read())
   if prefix+'/asset.meta' in members:Path(str(target)+'.meta').write_bytes(archive.extractfile(members[prefix+'/asset.meta']).read())
files=['FlowPlanning.cs','FlowHandUI.cs','CardAbilities.cs','HarmonyPatches.cs','DreamBuffs.cs','SlazeyaAbilities.cs','Steria.csproj','bin/Release/net472/Steria.dll']
(out/'candidate-hashes.json').write_text(json.dumps({f:hashlib.sha256((repo/'SteriaBuild'/f).read_bytes()).hexdigest() for f in files},indent=2))
print('Prepared exact-candidate DLL fixture; missing TMP essentials are imported asynchronously by the editor entry.')
