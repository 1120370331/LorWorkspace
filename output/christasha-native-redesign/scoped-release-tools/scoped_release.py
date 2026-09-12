"""Scoped Christasha release preparation. Every mutating command requires --execute."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

HERE = Path(__file__).resolve().parent
REPO = next(p for p in HERE.parents if (p / 'SteriaBuild').is_dir())
OWNED_CS = {'ChristashaNativeParticleLibrary.cs', 'DiceAttackEffect_Steria_ChristashaParticleV2.cs',
            'BehaviourAction_Steria_ChristashaXiyinV2.cs', 'FarAreaEffect_Steria_ChristashaXiyinV2.cs',
            'ChristashaNativeMassRecipes.cs', 'ChristashaNativePierceRecipes.cs'}
BASE_SUPPLEMENTS = {'HarmonyPatches.cs', 'DiceAttackEffect_Steria_AnhierTextureCombat.cs', 'BehaviourAction_Steria_MemoryConcerto.cs'}
EXCLUDED = {'Assembly-CSharp', 'BaseMod', 'VFXSource', '_analysis', 'Verification', 'obj', 'bin'}
OLD_AB = {'Assemblies/AB/steria_christasha_particle_v2', 'Assemblies/AB/steria_christasha_particle_v2.ab',
          'Resource/AssetBundle/steria_christasha_particle_v2', 'Resource/AssetBundle/steria_christasha_particle_v2.ab'}
XML_PATH = 'SteriaBuild/SteriaModFolder/Data/CardInfo.xml'
HASH = re.compile(r'^[a-f0-9]{64}$')

def sha(data): return hashlib.sha256(data).hexdigest()
def hash_file(path): return sha(path.read_bytes()) if path.is_file() else None
def git(*args): return subprocess.check_output(['git', *args], cwd=REPO)
def blob(commit, path): return git('show', commit + ':' + path)
def save_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    atomic_write(path, (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
def load_json(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def relative(value):
    path = PurePosixPath(value.replace('\\', '/'))
    if path.is_absolute() or '..' in path.parts or ':' in str(path): raise ValueError('Unsafe relative path: ' + value)
    return str(path)
def beneath(root, value):
    result = (root / relative(value)).resolve()
    if not result.is_relative_to(root.resolve()): raise ValueError('Path escapes root: ' + str(result))
    return result
def canonical(node): return (node.tag, dict(node.attrib), (node.text or '').strip(), tuple(canonical(c) for c in node))
def xml_equal(a, b): return canonical(ET.fromstring(a)) == canonical(ET.fromstring(b))

def xml_patch(data, edits, allowed_cards=None):
    text = data.decode('utf-8-sig')
    for edit in edits:
        card = str(edit['card']); attribute = edit['attribute']
        if allowed_cards is not None and card not in allowed_cards: raise ValueError('XML edit outside Christasha cards: ' + card)
        if attribute not in ('EffectRes', 'ActionScript'): raise ValueError('Only visual XML attributes may change')
        if not re.fullmatch(r'[A-Za-z0-9_]*', edit['after']): raise ValueError('Unexpected effect/action identifier')
        matches = list(re.finditer(r'<Card\b[^>]*\bID="' + re.escape(card) + r'"[^>]*>[\s\S]*?</Card>', text))
        if len(matches) != 1: raise ValueError('Missing/duplicate card: ' + card)
        block = matches[0]; tags = list(re.finditer(r'<Behaviour\s[^>]*?/?>', block[0]))
        die = tags[edit['die']]
        pattern = r'(\b' + attribute + r'=")([^"]*)(")'
        found = re.search(pattern, die[0])
        if not found or found[2] != edit['before']: raise ValueError('XML before-value differs: ' + str(edit))
        changed = die[0][:found.start(2)] + edit['after'] + die[0][found.end(2):]
        local = block[0][:die.start()] + changed + block[0][die.end():]
        text = text[:block.start()] + local + text[block.end():]
    return (b'\xef\xbb\xbf' if data.startswith(b'\xef\xbb\xbf') else b'') + text.encode('utf-8')

def xml_diff(a, b):
    rows = []
    def visit(x, y, path):
        for key in sorted(x.attrib.keys() | y.attrib.keys()):
            if x.get(key) != y.get(key): rows.append({'path': path, 'attribute': key, 'before': x.get(key), 'after': y.get(key)})
        if (x.text or '').strip() != (y.text or '').strip(): rows.append({'path': path, 'text_changed': True})
        if len(x) != len(y): rows.append({'path': path, 'child_counts': [len(x), len(y)]})
        for index, (u, v) in enumerate(zip(x, y)):
            visit(u, v, path + '/' + u.tag + ("[@ID='" + u.get('ID') + "']" if u.get('ID') else '[' + str(index) + ']'))
    visit(ET.fromstring(a), ET.fromstring(b), 'root')
    return rows

def snapshot(config, item):
    path = beneath(config.parent, item['snapshot'])
    data = path.read_bytes()
    if sha(data) != item['sha256']: raise ValueError('Frozen installed-baseline snapshot drift: ' + str(path))
    return data

def config_context(config):
    cfg = load_json(config)
    commit = git('rev-parse', cfg['baseline_commit'] + '^{commit}').decode().strip()
    game = Path(cfg['game_mod']).resolve()
    candidate = beneath(REPO, cfg['candidate_directory'])
    if not candidate.is_relative_to(REPO / 'output/christasha-native-redesign'):
        raise ValueError('Candidate must stay under output/christasha-native-redesign')
    for item in cfg['overlays']:
        if relative(item['source']) != relative(item['target']) or item['target'] not in {'SteriaBuild/' + name for name in OWNED_CS}:
            raise ValueError('Source overlay outside explicit Christasha ownership')
    for item in cfg['baseline_source_supplements']:
        if item['target'] not in {'SteriaBuild/' + name for name in BASE_SUPPLEMENTS}:
            raise ValueError('Unapproved baseline source supplement')
    if set(cfg['remove_sources']) - {'SteriaBuild/ChristashaParticleVisual.cs'}: raise ValueError('Unapproved source removal')
    if set(cfg['retire_files']) - OLD_AB: raise ValueError('Unapproved installed-file removal')
    for item in cfg['assets']:
        source = relative(item['source']); dest = relative(item['destination'])
        brush_asset = source.startswith('SteriaBuild/VFXSource/ChristashaNative/Assets/') and dest.startswith('Resource/Effects/ChristashaNative/')
        skin_asset = source.startswith('SteriaBuild/SteriaModFolder/Resource/CharacterSkin/Christasha/ClothCustom/') and dest.startswith('Resource/CharacterSkin/Christasha/ClothCustom/')
        if not (brush_asset or skin_asset):
            raise ValueError('Asset must be an explicit Christasha brush or approved ClothCustom source')
        if Path(source).suffix.lower() not in ('.png', '.json') or Path(dest).suffix.lower() not in ('.png', '.json'):
            raise ValueError('Only explicitly named original brush PNG/JSON assets can be shipped; no reference/old AB')
        if 'native_reference' in source.lower() + dest.lower(): raise ValueError('Reference extraction bundle cannot ship')
    return cfg, commit, game, candidate

def make_plan(config):
    cfg, commit, game, candidate = config_context(config)
    issues = []; observations = {}
    for name, expected in cfg['expected_installed'].items():
        actual = hash_file(beneath(game, name))
        observations[name] = {'expected': expected, 'actual': actual, 'matches': expected == actual}
        if actual != expected: issues.append('Installed hash drift; preserve/reconcile: ' + name)
    committed_dll = sha(blob(commit, 'SteriaBuild/SteriaModFolder/Assemblies/Steria.dll'))
    if committed_dll != cfg['expected_installed']['Assemblies/Steria.dll']: issues.append('Committed baseline DLL does not match expected installed DLL')
    baseline_xml = snapshot(config, cfg['baseline_xml_snapshot'])
    if sha(baseline_xml) != cfg['expected_installed']['Data/CardInfo.xml']: issues.append('Frozen XML is not the expected installed XML')
    amended_xml = xml_patch(blob(commit, XML_PATH), cfg['baseline_xml_preserved_edits'])
    if not xml_equal(amended_xml, baseline_xml): issues.append('Installed XML differs beyond explicitly recorded baseline-preservation edits')
    for item in cfg['baseline_source_supplements']: snapshot(config, item)
    for item in cfg['overlays'] + cfg['assets']:
        actual = hash_file(beneath(REPO, item['source']))
        item = dict(item, actual_sha256=actual)
        if not item.get('sha256') or not HASH.fullmatch(item['sha256']): issues.append('Input not frozen: ' + item['source'])
        elif actual != item['sha256']: issues.append('Frozen input drift: ' + item['source'])
    if not cfg['frozen']: issues.append('Manifest frozen=false; adapters/artwork await owner acceptance')
    issues.extend(cfg.get('pending_inputs', []))
    cards = {c.get('ID') for c in ET.fromstring(baseline_xml).findall('Card') if any(d.get('EffectRes', '').startswith('Steria_ChristashaV2') for d in c.findall('./BehaviourList/Behaviour'))}
    edited_xml = xml_patch(baseline_xml, cfg['xml_edits'], cards)
    workspace_xml = (REPO / XML_PATH).read_bytes()
    source_paths = [p for p in git('ls-tree', '-rz', '--name-only', commit, 'SteriaBuild').decode().split('\0')
                    if p.endswith('.cs') and not any(part in EXCLUDED for part in Path(p).relative_to('SteriaBuild').parts) and p not in cfg['remove_sources']]
    source_paths = sorted(set(source_paths) | {i['target'] for i in cfg['baseline_source_supplements'] + cfg['overlays']})
    destinations = ['Assemblies/Steria.dll', 'Data/CardInfo.xml'] + [i['destination'] for i in cfg['assets']] + cfg['retire_files']
    before_files = {name: hash_file(beneath(game, name)) for name in destinations}
    plan = {'status': 'BLOCKED' if issues else 'READY', 'utc': datetime.now(timezone.utc).isoformat(), 'baseline_commit': commit,
            'config_sha256': sha(config.read_bytes()), 'game_mod': str(game), 'candidate_directory': str(candidate),
            'installed': observations, 'committed_dll_sha256': committed_dll,
            'baseline_source_supplements': cfg['baseline_source_supplements'], 'baseline_xml_preserved_edits': cfg['baseline_xml_preserved_edits'],
            'working_xml_changes_excluded': xml_diff(edited_xml, workspace_xml), 'task_xml_edits': cfg['xml_edits'],
            'working_harmony_policy': 'Never copy current Harmony. Start from frozen installed/PDB-matching snapshot; apply only explicit Christasha registry edits.',
            'source_paths': source_paths, 'overlay_inputs': [dict(i, actual_sha256=hash_file(beneath(REPO, i['source']))) for i in cfg['overlays']],
            'assets': cfg['assets'], 'before_files': before_files, 'issues': issues,
            'staging_policy': 'No git index/commit mutation. Never stage baseline supplements or shared worktree files recursively.'}
    return cfg, commit, game, candidate, plan, edited_xml

def pin_inputs(candidate):
    return {str(p.relative_to(candidate)): hash_file(p) for p in sorted(candidate.rglob('*')) if p.is_file() and not any(k in ('bin', 'obj') for k in p.relative_to(candidate).parts)}

def validate_prepared(root, config):
    receipt = load_json(root / 'prepared.json')
    if receipt['config_sha256'] != sha(config.read_bytes()): raise ValueError('Config changed after prepare')
    if pin_inputs(root / 'candidate') != receipt['inputs']: raise ValueError('Candidate source/assets drift or unlisted extra files')
    for path, expected in receipt['external_reference_hashes'].items():
        if hash_file(Path(path)) != expected: raise ValueError('Native reference DLL drift: ' + path)
    return receipt

def prepare(config):
    cfg, commit, game, root, plan, edited_xml = make_plan(config)
    save_json(HERE / 'dry-run-plan.json', plan)
    if plan['issues']: raise ValueError('Preparation blocked; see dry-run-plan.json')
    root.mkdir(parents=True, exist_ok=True)
    if (root / 'prepared.json').exists():
        validate_prepared(root, config); print('PREPARED_CANDIDATE_REUSED'); return
    candidate = root / 'candidate'
    if candidate.exists(): raise ValueError('Unreceipted candidate directory exists; choose a fresh candidate_directory')
    project = candidate / 'SteriaBuild'; project.mkdir(parents=True)
    supplements = {i['target']: i for i in cfg['baseline_source_supplements']}
    overlays = {i['target']: i for i in cfg['overlays']}
    for path in plan['source_paths']:
        dest = beneath(candidate, path); dest.parent.mkdir(parents=True, exist_ok=True)
        data = beneath(REPO, overlays[path]['source']).read_bytes() if path in overlays else snapshot(config, supplements[path]) if path in supplements else blob(commit, path)
        dest.write_bytes(data)
    harmony = project / 'HarmonyPatches.cs'; code = harmony.read_text(encoding='utf-8-sig')
    for edit in cfg['registry_edits']:
        if not edit['before'].strip().startswith('SteriaCustomEffects["Steria_ChristashaV2') or not edit['after'].strip().startswith('SteriaCustomEffects["Steria_ChristashaV2'):
            raise ValueError('Registry patch outside Christasha')
        if code.count(edit['before']) != 1: raise ValueError('Registry before-text is not unique')
        code = code.replace(edit['before'], edit['after'])
    if cfg['registry_edits']: harmony.write_text(code, encoding='utf-8')
    project_file = project / 'Steria.csproj'; project_file.write_bytes(blob(commit, 'SteriaBuild/Steria.csproj'))
    tree = ET.parse(project_file); external = {}
    for ref in cfg['project_references']:
        if not re.fullmatch(r'UnityEngine(?:\.[A-Za-z0-9]+)*', ref['include']): raise ValueError('Only explicit UnityEngine project references are supported')
        hint = Path(ref['hint_path']).resolve()
        if hash_file(hint) != ref['sha256']: raise ValueError('Project reference hash drift')
        node = tree.getroot().find(".//Reference[@Include='" + ref['include'] + "']")
        if node is None: node = ET.SubElement(ET.SubElement(tree.getroot(), 'ItemGroup'), 'Reference', Include=ref['include'])
        child = node.find('HintPath')
        if child is None: child = ET.SubElement(node, 'HintPath')
        child.text = str(hint)
    if cfg['project_references']: tree.write(project_file, encoding='utf-8', xml_declaration=False)
    for hint in tree.findall('.//HintPath'):
        path = Path(hint.text)
        if path.is_absolute():
            if not path.is_file(): raise ValueError('Missing baseline native reference: ' + str(path))
            external[str(path.resolve())] = hash_file(path)
        else:
            rel = relative(hint.text); dest = beneath(project, rel); dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(blob(commit, 'SteriaBuild/' + rel))
    for resource in tree.findall('.//EmbeddedResource'):
        if resource.get('Include'):
            rel = relative(resource.get('Include')); dest = beneath(project, rel); dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(blob(commit, 'SteriaBuild/' + rel))
    for p in project.rglob('*.cs'):
        if any(term in p.read_text(encoding='utf-8-sig') for term in ('steria_christasha_particle_v2', 'ChristashaParticleVisual')):
            raise ValueError('Active source still depends on retired V2 runtime/bundle: ' + str(p))
    package = candidate / 'package'; (package / 'Data').mkdir(parents=True)
    (package / 'Data/CardInfo.xml').write_bytes(edited_xml)
    for asset in cfg['assets']:
        dest = beneath(package, asset['destination']); dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(beneath(REPO, asset['source']).read_bytes())
    receipt = dict(plan, status='PREPARED', inputs=pin_inputs(candidate), external_reference_hashes=external)
    save_json(root / 'prepared.json', receipt)
    print('SCOPED_PREPARE_PASS', root)

def build(config):
    cfg, _, _, root = config_context(config)
    receipt = validate_prepared(root, config)
    if (root / 'build.json').exists():
        saved = load_json(root / 'build.json')
        if saved['config_sha256'] != receipt['config_sha256'] or hash_file(Path(saved['dll'])) != saved['dll_sha256']: raise ValueError('Existing build artifact/receipt drift')
        print('SCOPED_BUILD_REUSED'); return
    project = root / 'candidate/SteriaBuild/Steria.csproj'
    with (root / 'build.log').open('wb') as log:
        subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '--nologo'], cwd=project.parent, stdout=log, stderr=subprocess.STDOUT, check=True)
    validate_prepared(root, config)
    dll = project.parent / 'bin/Release/net472/Steria.dll'
    if not dll.is_file(): raise ValueError('Build did not produce Steria.dll')
    save_json(root / 'build.json', {'status': 'BUILT', 'config_sha256': receipt['config_sha256'], 'dll': str(dll), 'dll_sha256': hash_file(dll)})
    print('SCOPED_BUILD_PASS', hash_file(dll))

def atomic_write(target, data):
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(target.name + '.christasha-' + uuid.uuid4().hex + '.tmp')
    try:
        with temporary.open('xb') as stream: stream.write(data); stream.flush(); os.fsync(stream.fileno())
        os.replace(temporary, target) # same-directory file-level atomic replacement
    finally:
        if temporary.exists(): temporary.unlink()

def restore(receipt, game):
    backup_root = Path(receipt['backup_directory']).resolve()
    failures = []
    for op in reversed(receipt['operations']):
        if not op.get('attempted'): continue
        target = beneath(game, op['relative']); current = hash_file(target)
        if current not in (op['after_sha256'], op['before_sha256']):
            failures.append('Later/concurrent target change preserved: ' + op['relative']); continue
        try:
            if op['before_sha256'] is None:
                if target.exists(): target.unlink()
            else:
                backup = beneath(backup_root, op['relative'])
                if hash_file(backup) != op['before_sha256']: failures.append('Backup drift: ' + op['relative']); continue
                atomic_write(target, backup.read_bytes())
            if hash_file(target) != op['before_sha256']: failures.append('Rollback verification failed: ' + op['relative'])
        except OSError as error:
            failures.append('Rollback filesystem error for ' + op['relative'] + ': ' + str(error))
    return failures

def deploy(config):
    cfg, _, game, root = config_context(config)
    prepared = validate_prepared(root, config); built = load_json(root / 'build.json')
    if built['config_sha256'] != sha(config.read_bytes()) or hash_file(Path(built['dll'])) != built['dll_sha256']: raise ValueError('Build receipt/artifact drift')
    if (root / 'deployment.json').exists():
        previous = load_json(root / 'deployment.json')
        if previous['status'] == 'DEPLOYED_VERIFIED' and previous['config_sha256'] == sha(config.read_bytes()) and all(hash_file(beneath(game, op['relative'])) == op['after_sha256'] for op in previous['operations']):
            print('SCOPED_DEPLOY_ALREADY_MATCHES'); return
    for name, expected in prepared['before_files'].items():
        if hash_file(beneath(game, name)) != expected: raise ValueError('Installation changed after prepare: ' + name)
    package = root / 'candidate/package'
    writes = [(a['destination'], beneath(package, a['destination'])) for a in cfg['assets']]
    writes += [('Data/CardInfo.xml', package / 'Data/CardInfo.xml'), ('Assemblies/Steria.dll', Path(built['dll']))]
    writes += [(name, None) for name in cfg['retire_files'] if prepared['before_files'][name] is not None]
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S') + '-' + uuid.uuid4().hex[:8]
    backup = root / 'deployments' / stamp; backup.mkdir(parents=True)
    operations = []
    for name, source in writes:
        target = beneath(game, name); before = hash_file(target)
        if before != prepared['before_files'][name]: raise ValueError('Installation changed during backup preflight: ' + name)
        if before is not None:
            saved = beneath(backup, name); saved.parent.mkdir(parents=True, exist_ok=True); saved.write_bytes(target.read_bytes())
            if hash_file(saved) != before: raise ValueError('Backup verification failed')
        operations.append({'relative': name, 'source': str(source) if source else None, 'before_sha256': before, 'after_sha256': hash_file(source) if source else None, 'attempted': False})
    receipt = {'status': 'BACKED_UP', 'game_mod': str(game), 'backup_directory': str(backup), 'config_sha256': sha(config.read_bytes()), 'operations': operations}
    path = backup / 'receipt.json'; save_json(path, receipt)
    try:
        for op in operations:
            target = beneath(game, op['relative'])
            if hash_file(target) != op['before_sha256']: raise ValueError('Concurrent target change before replace: ' + op['relative'])
            data = Path(op['source']).read_bytes() if op['source'] else None
            if data is not None and sha(data) != op['after_sha256']: raise ValueError('Payload drift')
            op['attempted'] = True; save_json(path, receipt)
            if data is None: target.unlink()
            else: atomic_write(target, data)
            if hash_file(target) != op['after_sha256']: raise ValueError('Installed verification failed')
        if any(hash_file(beneath(game, op['relative'])) != op['after_sha256'] for op in operations): raise ValueError('Post-deployment concurrent drift')
        receipt['status'] = 'DEPLOYED_VERIFIED'; save_json(path, receipt); save_json(root / 'deployment.json', receipt)
    except Exception as error:
        receipt['error'] = str(error); receipt['rollback_failures'] = restore(receipt, game)
        receipt['status'] = 'ROLLBACK_BLOCKED' if receipt['rollback_failures'] else 'ROLLED_BACK_AFTER_FAILURE'
        save_json(path, receipt); raise
    print('SCOPED_DEPLOY_PASS', path)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('plan', 'prepare', 'build', 'deploy', 'rollback'))
    parser.add_argument('--config', type=Path, default=HERE / 'release-config.json')
    parser.add_argument('--execute', action='store_true', help='Required to extract/build/deploy/rollback; absent means read-only plan')
    parser.add_argument('--receipt', type=Path, help='Rollback deployment receipt')
    args = parser.parse_args(); config = args.config.resolve()
    if args.command == 'plan' or not args.execute:
        plan = make_plan(config)[4]; save_json(HERE / 'dry-run-plan.json', plan)
        print(json.dumps({'status': plan['status'], 'baseline_commit': plan['baseline_commit'], 'issues': plan['issues'],
                          'excluded_workspace_xml_changes': len(plan['working_xml_changes_excluded']), 'report': str(HERE / 'dry-run-plan.json')}, ensure_ascii=False, indent=2)); return
    if args.command == 'prepare': prepare(config)
    elif args.command == 'build': build(config)
    elif args.command == 'deploy': deploy(config)
    else:
        if not args.receipt: parser.error('rollback requires --receipt')
        _, _, game, _ = config_context(config); receipt = load_json(args.receipt)
        if Path(receipt['game_mod']).resolve() != game: raise ValueError('Rollback game root mismatch')
        failures = restore(receipt, game)
        receipt['status'] = 'ROLLBACK_BLOCKED' if failures else 'ROLLED_BACK_VERIFIED'; receipt['rollback_failures'] = failures
        save_json(args.receipt, receipt)
        if failures: raise ValueError('; '.join(failures))
        print('SCOPED_ROLLBACK_PASS')

if __name__ == '__main__':
    try: main()
    except (ValueError, FileNotFoundError, subprocess.CalledProcessError) as error:
        print('SCOPED_RELEASE_BLOCKED: ' + str(error), file=sys.stderr); sys.exit(2)
