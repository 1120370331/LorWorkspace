import hashlib
import json
import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
ACE = HERE.parents[1]
PROJECT = HERE / 'UnityFixture'
REFERENCE = ACE / 'Verification/Vfx/UnityFixture'
for folder in ('ProjectSettings', 'Packages', 'Assets/Plugins'):
    shutil.copytree(REFERENCE / folder, PROJECT / folder, dirs_exist_ok=True)
plugins = PROJECT / 'Assets/Plugins'
dll = ACE / 'bin/Release/net472/AceTaffy.dll'
shutil.copyfile(dll, plugins / dll.name)
editor = PROJECT / 'Assets/Editor'
editor.mkdir(parents=True, exist_ok=True)
shutil.copyfile(HERE / 'ReceptionFixture.cs', editor / 'ReceptionFixture.cs')
shutil.copyfile(HERE / 'InventionIconFixture.cs', editor / 'InventionIconFixture.cs')
(editor / 'Reception.Editor.asmdef').write_text(json.dumps({'name':'Reception.Editor','includePlatforms':['Editor']}))
(HERE / 'candidate.json').write_text(json.dumps({'dll_sha256':hashlib.sha256(dll.read_bytes()).hexdigest(),
    'sources':{str(p.relative_to(ACE)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (ACE/'src').glob('*.cs')},
    'data':{str(p.relative_to(ACE)):hashlib.sha256(p.read_bytes()).hexdigest()
        for folder in ('Data','Localize') for p in (ACE/'AceTaffyMod'/folder).rglob('*.xml')},
    'fixture_sha256':hashlib.sha256((HERE/'ReceptionFixture.cs').read_bytes()).hexdigest()},indent=2))
print(PROJECT)
