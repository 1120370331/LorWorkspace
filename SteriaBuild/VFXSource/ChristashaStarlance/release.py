"""Stage and verify the narrowly scoped release on the installed Antie baseline."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import re
import shutil

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
WORK = ROOT / 'output/christasha-starlance'
BASE = ROOT / 'output/antie-ice-indigo/release-02'
GAME = Path('C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder').resolve(strict=True)
REPO = ROOT / 'SteriaBuild/SteriaModFolder'
ADAPTER = 'DiceAttackEffect_Steria_ChristashaParticleV2.cs'
VISUAL = 'ChristashaStarlance.cs'
DLL = 'Assemblies/Steria.dll'
ART = 'Resource/Effects/ChristashaStarlanceV3'
ORDER = [f'{ART}/{name}.png' for name in ('Charge', 'Lead', 'Travel', 'Exit', 'Impact')] + [DLL]
PRESERVE = ['Data/CardInfo.xml', 'Data/EquipPage_Librarian.xml', 'Data/EquipPage_Enemy.xml',
            'Assemblies/AB/steria_christasha_light_strike', 'Assemblies/AB/steria_slazeya_water_thunder_assets',
            'Assemblies/AB/steria_antie_ice_indigo_combat',
            'Resource/Effects/ChristashaNative/tide-brush-imagegen-v1.png']
ROOTS = [('game', GAME), ('repo', REPO)]


def hashfile(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def documents(metadata):
    root = Path(metadata['dll']).parents[3]
    return {Path(d['path']).relative_to(root).as_posix(): d['checksum'] for d in metadata['documents']
            if 'obj' not in Path(d['path']).relative_to(root).parts}


def prepare(output):
    old = read(BASE / 'built-metadata.json')
    assert old['pdb_matches'] and hashfile(Path(old['pdb'])) == old['pdb_sha256']
    for _, root in ROOTS:
        assert hashfile(root / DLL) == old['dll_sha256'], root
    sources = documents(old)
    target = output / 'SteriaBuild'
    target.mkdir(parents=True)
    for rel, expected in sources.items():
        path = BASE / 'SteriaBuild' / rel
        assert hashfile(path) == expected, path
        dest = target / rel
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, dest)
    for folder in ['Libs', 'VisualAssets']:
        shutil.copytree(BASE / 'SteriaBuild' / folder, target / folder)
    shutil.copy2(BASE / 'SteriaBuild/Steria.csproj', target / 'Steria.csproj')
    for changed in [ADAPTER, VISUAL]:
        shutil.copy2(ROOT / 'SteriaBuild' / changed, target / changed)
    write(output / 'prepared.json', dict(status='PREPARED', game_root=str(GAME),
        baseline_metadata=str(BASE / 'built-metadata.json'), baseline_dll=old['dll_sha256'],
        project_hash=hashfile(target / 'Steria.csproj'), art_candidate='Christasha Starlance V3',
        sources={p.relative_to(target).as_posix(): hashfile(p) for p in target.rglob('*.cs')},
        payload_order=ORDER, before_payload={label: {rel: hashfile(root / rel) for rel in ORDER} for label, root in ROOTS},
        preserved={label: {rel: hashfile(root / rel) for rel in PRESERVE} for label, root in ROOTS}))
    print('CHRISTASHA_SCOPED_RELEASE_PREPARED', output)


def verify(output, evidence):
    prepared = read(output / 'prepared.json')
    old, new = read(BASE / 'built-metadata.json'), read(output / 'built-metadata.json')
    baseline, candidate = documents(old), documents(new)
    assert {p for p, h in baseline.items() if candidate.get(p) != h} == {ADAPTER}
    assert candidate.keys() - baseline.keys() == {VISUAL}
    assert candidate == prepared['sources']
    assert old['pdb_matches'] and new['pdb_matches'] and old['resources'] == new['resources']
    assert sorted(old['references'], key=lambda n: n['name']) == sorted(new['references'], key=lambda n: n['name'])
    assert set(old['types']).issubset(new['types'])
    assert hashfile(output / 'SteriaBuild/Steria.csproj') == prepared['project_hash']
    assert hashfile(Path(new['dll'])) == new['dll_sha256']
    assert hashfile(Path(new['pdb'])) == new['pdb_sha256']
    inputs = read(evidence / 'inputs.json')
    assert inputs['dll_sha256'] == new['dll_sha256']
    result = (evidence / 'checks.txt').read_text(encoding='utf-8')
    assert 'CHRISTASHA_STARLANCE_NATIVE_PASS' in result
    payload = output / 'payload'
    assert inputs['atlases'] == {rel: hashfile(payload / rel) for rel in ORDER[:-1]}
    (payload / 'Assemblies').mkdir(parents=True, exist_ok=True)
    shutil.copy2(Path(new['dll']), payload / DLL)
    proof = {**prepared, 'status': 'RELEASE_VERIFIED', 'native_result': result,
             'native_evidence': str(evidence), 'native_inputs': inputs,
             'verified_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
             'baseline_authored_sources_preserved': len(baseline)-1,
             'embedded_resources_preserved': len(new['resources']),
             'payload': {rel: hashfile(payload / rel) for rel in ORDER}}
    write(output / 'release.json', proof)
    print('CHRISTASHA_SCOPED_RELEASE_VERIFIED', len(baseline)-1, 'baseline sources preserved')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=['prepare', 'verify'])
    parser.add_argument('release')
    parser.add_argument('--evidence', type=Path)
    args = parser.parse_args()
    assert re.fullmatch(r'[A-Za-z0-9_-]+', args.release)
    output = WORK / args.release
    if args.operation == 'prepare':
        prepare(output)
    else:
        verify(output, args.evidence.resolve(strict=True))
