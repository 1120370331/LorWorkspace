"""Stage an isolated Unity 2019.3 fixture; production DLL must be frozen first."""
from pathlib import Path
import hashlib
import json
import shutil
import tarfile

repo = Path(__file__).resolve().parents[3]
source = Path(__file__).resolve().parent
out = repo / 'output/ocean-current-vfx/ui'
project = out / 'UnityProject'
assets = project / 'Assets'
for path in [assets / 'Editor', assets / 'FixtureFonts', project / 'Packages',
             project / 'ProjectSettings', out / 'checks']:
    path.mkdir(parents=True, exist_ok=True)
for path in (source / 'bin/Debug/net472').iterdir():
    if path.is_file():
        shutil.copy2(path, out / 'checks' / path.name)
shutil.copy2(source / 'FlowBorderFixture.cs.txt', assets / 'Editor/FlowBorderFixture.cs')
(assets / 'Editor/FlowBorderFixture.asmdef').write_text(json.dumps({
    'name': 'Steria.FlowBorderFixture.Editor', 'includePlatforms': ['Editor']}))
modules = ['imageconversion', 'ui', 'audio', 'animation', 'physics', 'particlesystem', 'assetbundle']
(project / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
    'com.unity.textmeshpro': '2.0.1', 'com.unity.ugui': '1.0.0',
    **{'com.unity.modules.' + module: '1.0.0' for module in modules}}}))
(project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\n')
shutil.copy2('C:/Windows/Fonts/simhei.ttf', assets / 'FixtureFonts/simhei.ttf')
package = repo / 'output/flow-rework/UnityProject/Library/PackageCache/com.unity.textmeshpro@2.0.1/Package Resources/TMP Essential Resources.unitypackage'
if package.exists():
    with tarfile.open(package) as archive:
        members = {item.name: item for item in archive.getmembers()}
        for name, member in members.items():
            if not name.endswith('/pathname'):
                continue
            target = (project / archive.extractfile(member).read().decode().strip()).resolve()
            if not target.is_relative_to(project.resolve()):
                raise ValueError(target)
            prefix = name.rsplit('/', 1)[0]
            asset = members.get(prefix + '/asset')
            if not asset or not asset.isfile():
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.extractfile(asset).read())
            if prefix + '/asset.meta' in members:
                Path(str(target) + '.meta').write_bytes(archive.extractfile(members[prefix + '/asset.meta']).read())
paths = ['SteriaBuild/FlowCardCurrentBorder.cs', 'SteriaBuild/FlowHandUI.cs',
         'SteriaBuild/FlowPlanning.cs', 'SteriaBuild/HarmonyPatches.cs',
         'output/ocean-current-vfx/ui/candidate/Steria.dll']
(out / 'candidate-hashes.json').write_text(json.dumps({
    path: hashlib.sha256((repo / path).read_bytes()).hexdigest() for path in paths}, indent=2))
print('Prepared isolated actual-Unity UI fixture, frozen candidate DLL, no deployment.')
