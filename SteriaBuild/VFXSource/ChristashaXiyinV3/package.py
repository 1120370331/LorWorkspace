"""Build Xiyin from the exact installed Starlance DLL's portable-PDB inputs."""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET


HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / 'ChristashaStarlance'))
import deploy as starlance_deploy
import release as starlance


ROOT = starlance.ROOT
GAME = starlance.GAME
WORK = ROOT / 'output/christasha-xiyin'
BASE = ROOT / 'output/christasha-starlance/release-02'
ART = 'Resource/Effects/ChristashaXiyinV3'
DLL = starlance.DLL
PDB = 'Assemblies/Steria.pdb'
CHANGED = {'BehaviourAction_Steria_ChristashaXiyinV2.cs', 'FarAreaEffect_Steria_ChristashaXiyinV2.cs'}
ADDED = {'ChristashaXiyinTide.cs'}
MEDIA = 'd022ff6ffd305b5775db3fbacf0ceec5177a503ba5926b1d953aebf2806b7521'
PRESERVE = starlance.PRESERVE + starlance.ORDER[:-1] + ['Assemblies/Steria.dll.bak']
sha, read, write = starlance.hashfile, starlance.read, starlance.write


def binding(root):
    xml = ET.parse(root / 'Data/CardInfo.xml')
    cards = [card for card in xml.iter('Card') if any(die.get('ActionScript') == 'Steria_ChristashaXiyinV2'
             for die in card.findall('./BehaviourList/Behaviour'))]
    assert len(cards) == 1 and cards[0].get('ID') == '9009006', root
    spec = cards[0].find('Spec')
    assert spec.get('Range') == 'FarAreaEach', ET.tostring(cards[0])


def prepare(release):
    baseline = read(BASE / 'built-metadata.json')
    assert sha(Path(baseline['pdb'])) == baseline['pdb_sha256'] and baseline['pdb_matches']
    assert sha(ROOT / 'preview_exports/xiyin_golden_tide/v3/xiyin-golden-tide-v3.mp4') == MEDIA
    for _, root in starlance.ROOTS:
        assert sha(root / DLL) == baseline['dll_sha256'], root
        binding(root)

    dest = release / 'SteriaBuild'
    dest.mkdir(parents=True)
    for rel, value in starlance.documents(baseline).items():
        source = BASE / 'SteriaBuild' / rel
        assert sha(source) == value, source
        (dest / rel).parent.mkdir(exist_ok=True, parents=True)
        shutil.copy2(source, dest / rel)
    for folder in ('Libs', 'VisualAssets'):
        shutil.copytree(BASE / 'SteriaBuild' / folder, dest / folder)
    shutil.copy2(BASE / 'SteriaBuild/Steria.csproj', dest / 'Steria.csproj')
    for rel in CHANGED | ADDED:
        shutil.copy2(ROOT / 'SteriaBuild' / rel, dest / rel)

    assets = read(release / 'payload' / ART / 'manifest.json')['atlases']
    order = [f'{ART}/{name}' for name in assets] + [f'{ART}/manifest.json', PDB, DLL]
    write(release / 'prepared.json', dict(status='PREPARED', game_root=str(GAME),
        baseline_metadata=str(BASE / 'built-metadata.json'), baseline_dll=baseline['dll_sha256'],
        card_id=9009006, art_candidate='Christasha Xiyin Golden Tide V3', approved_media_sha256=MEDIA,
        project_hash=sha(dest / 'Steria.csproj'),
        sources={path.relative_to(dest).as_posix(): sha(path) for path in dest.rglob('*.cs')}, payload_order=order,
        before_payload={label: {rel: sha(root / rel) for rel in order} for label, root in starlance.ROOTS},
        preserved={label: {rel: sha(root / rel) for rel in PRESERVE} for label, root in starlance.ROOTS}))


def build(release):
    project = release / 'SteriaBuild/Steria.csproj'
    with (release / 'build.log').open('w', encoding='utf-8') as log:
        subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '--nologo', '-v:q'],
                       check=True, stdout=log, stderr=subprocess.STDOUT)
    build_output = project.parent / 'bin/Release/net472'
    probe = ROOT / 'output/slazeya-wind-combat/release-tools/MetadataProbe'
    subprocess.run(['dotnet', 'run', '--project', str(probe / 'MetadataProbe.csproj'), '--',
                    str(build_output / 'Steria.dll'), str(build_output / 'Steria.pdb'),
                    str(release / 'built-metadata.json')], check=True)


def verify(release, evidence):
    old, new = (read(p / 'built-metadata.json') for p in (BASE, release))
    before, after = starlance.documents(old), starlance.documents(new)
    prepared = read(release / 'prepared.json')
    assert {name for name in before if before.get(name) != after.get(name)} == CHANGED
    assert after.keys() - before.keys() == ADDED and after == prepared['sources']
    assert old['pdb_matches'] and new['pdb_matches'] and old['resources'] == new['resources']
    assert sorted(old['references'], key=lambda n: n['name']) == sorted(new['references'], key=lambda n: n['name'])
    assert {name for name in old['types'] if '<' not in name}.issubset(new['types'])
    assert sha(release / 'SteriaBuild/Steria.csproj') == prepared['project_hash']
    payload = release / 'payload'
    for kind, target in (('dll', DLL), ('pdb', PDB)):
        assert sha(Path(new[kind])) == new[kind + '_sha256']
        (payload / target).parent.mkdir(exist_ok=True, parents=True)
        shutil.copy2(new[kind], payload / target)
    result = (evidence / 'checks.txt').read_text(encoding='utf-8')
    assert 'CHRISTASHA_XIYIN_NATIVE_PASS' in result
    inputs = read(evidence / 'inputs.json')
    assert inputs['dll_sha256'] == new['dll_sha256']
    assert inputs['atlases'] == {rel: sha(payload / rel) for rel in prepared['payload_order'] if rel.startswith(ART)}
    write(release / 'release.json', dict(prepared, status='RELEASE_VERIFIED', native_evidence=str(evidence),
        native_result=result, native_inputs=inputs, preserved_baseline_sources=len(before)-len(CHANGED),
        payload={rel: sha(payload / rel) for rel in prepared['payload_order']}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('prepare', 'build', 'verify', 'deploy'))
    parser.add_argument('release', type=Path)
    parser.add_argument('--evidence', type=Path)
    args = parser.parse_args()
    release = args.release.resolve()
    assert release.is_relative_to(WORK)
    if args.operation == 'prepare':
        prepare(release)
    elif args.operation == 'build':
        build(release)
    elif args.operation == 'verify':
        verify(release, args.evidence.resolve(strict=True))
    else:
        starlance_deploy.WORK = WORK
        starlance_deploy.deploy(release)
