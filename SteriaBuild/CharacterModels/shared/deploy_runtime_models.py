"""Deploy verified Ailierel/Sivier runtime packages; default is a read-only dry run.

The workspace mod is inferred as CharacterModels/../SteriaModFolder. Repeat
--model and --game-mod as needed. Only --apply writes mod files; reports and
verified backups live in --report-dir (default: CharacterModels/deployments).
This tool never builds art, writes enemy books, deploys DLLs, or starts the game.
"""
import argparse
import copy
import hashlib
import json
import os
import re
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

from PIL import Image

MODELS = Path(__file__).resolve().parents[1]
WORKSPACE_MOD = MODELS.parent / 'SteriaModFolder'
LIBRARY = 'Data/EquipPage_Librarian.xml'
ENEMY = 'Data/EquipPage_Enemy.xml'
SUPPORTED = {
    'Ailierel_Layered_v1': ('Ailierel', '8', '99000007'),
    'Sivier_Layered_v1': ('Sivier', '9', '99000008'),
}
ACTIONS = {'Default', 'Guard', 'Evade', 'Damaged', 'Slash', 'Penetrate',
           'Hit', 'Move', 'Special', 'S1', 'Fire', 'Aim'}


def ensure(condition, message):
    if not condition:
        raise RuntimeError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    ensure(path.resolve() == path, f'File path changed resolution: {path}')
    ensure(path.is_file(), f'Expected regular file: {path}')
    return digest(path.read_bytes())


def relative_path(relative):
    ensure(isinstance(relative, str) and '\\' not in relative and ':' not in relative,
           f'Invalid relative path: {relative!r}')
    parts = PurePosixPath(relative)
    ensure(not parts.is_absolute() and relative and all(p not in ('', '.', '..') for p in relative.split('/')),
           f'Unsafe relative path: {relative!r}')
    return parts


def confined(root, relative):
    root = root.resolve()
    path = root.joinpath(*relative_path(relative).parts).resolve()
    ensure(path != root and path.is_relative_to(root), f'Path escapes root {root}: {relative}')
    ensure(not path.exists() or path.is_file(), f'Expected file destination: {path}')
    return path


def tree_bytes(root):
    root = copy.deepcopy(root)
    for node in root.iter():
        if node.text is not None and not node.text.strip():
            node.text = None
        if node.tail is not None and not node.tail.strip():
            node.tail = None
    return ET.tostring(root)


def book(root, book_id):
    matches = root.findall(f"Book[@ID='{book_id}']")
    ensure(len(matches) == 1, f'Expected exactly one Book ID={book_id}')
    node = matches[0]
    ensure(len(node.findall('CharacterSkin')) == 1, f'Expected one skin field: Book {book_id}')
    ensure(node.findtext('CharacterSkinType') == 'Custom', f'Book {book_id} is not Custom')
    return node


def patched_books(data, bindings):
    """Patch only authorized skin text; preserve UTF-8 BOM and every other byte."""
    text = data.decode('utf-8-sig')
    before = ET.fromstring(text)
    output = text
    for book_id, (guest, projection) in bindings.items():
        node = book(before, book_id)
        ensure(node.findtext('CharacterSkin') in (guest, projection),
               f'Unexpected skin for librarian {book_id}: {node.findtext("CharacterSkin")}')
        pattern = rf'(<Book\b(?=[^>]*\bID\s*=\s*[\"\x27]{re.escape(book_id)}[\"\x27])[^>]*>)(.*?)(</Book\s*>)'
        matches = list(re.finditer(pattern, output, re.S))
        ensure(len(matches) == 1, f'Cannot uniquely patch Book {book_id}')
        match = matches[0]
        skins = list(re.finditer(r'(<CharacterSkin\s*>)([^<]*)(</CharacterSkin\s*>)', match.group(2)))
        ensure(len(skins) == 1, f'Cannot uniquely patch CharacterSkin in Book {book_id}')
        skin = skins[0]
        ensure(skin.group(2) in (guest, projection), f'Unexpected skin text in Book {book_id}')
        start, end = match.start(2) + skin.start(2), match.start(2) + skin.end(2)
        output = output[:start] + projection + output[end:]
        node.find('CharacterSkin').text = projection
    ensure(tree_bytes(before) == tree_bytes(ET.fromstring(output)), 'Unexpected librarian XML change')
    encoded = output.encode('utf-8')
    return (b'\xef\xbb\xbf' + encoded) if data.startswith(b'\xef\xbb\xbf') else encoded


def load_json(path, guards):
    data = path.read_bytes()
    guards[path] = digest(data)
    return json.loads(data.decode('utf-8'))


def read_package(requested, guards):
    root = requested.resolve()
    ensure(root.parent == MODELS and root.name in SUPPORTED, f'Unsupported model root: {root}')
    ensure(root.is_dir(), f'Missing model directory: {root}')
    guest, enemy_id, librarian_id = SUPPORTED[root.name]
    projection = root.name + '_Projection'
    manifest = load_json(confined(root, 'build/build_manifest.json'), guards)
    verified = load_json(confined(root, 'build/verification.json'), guards)
    runtime = load_json(confined(root, 'build/runtime/runtime_manifest.json'), guards)
    model = load_json(confined(root, 'model.json'), guards)
    candidate = manifest['candidate']
    ensure(re.fullmatch('[0-9a-f]{64}', candidate), f'Invalid candidate: {root}')
    ensure(digest(json.dumps(manifest['sources'], sort_keys=True).encode()) == candidate,
           f'Candidate identity does not match source inventory: {root}')
    ensure(verified['result'] == 'PASS' and verified['candidate'] == candidate
           and verified['actions_checked'] == 12 and runtime['art_candidate'] == candidate,
           f'Build/verification/runtime candidate mismatch: {root}')
    ensure(model['id'] == root.name and model['character'] == guest, f'Model identity mismatch: {root}')
    actions = manifest['actions'] + list(manifest['aliases'])
    ensure(len(actions) == 12 and set(actions) == ACTIONS, f'Expected all 12 actions: {root}')
    ensure(manifest['aliases'] == {'S1': 'Special', 'Fire': 'Penetrate', 'Aim': 'Guard'},
           f'Unexpected action aliases: {root}')
    for relative, expected in manifest['sources'].items():
        if relative.startswith('../shared/'):
            source = confined(MODELS / 'shared', relative[len('../shared/'):])
        else:
            source = confined(root, relative)
        ensure(sha(source) == expected, f'Stale art source: {source}')
        guards[source] = expected
    ensure(runtime['skin_bindings'] == {
        'EquipPage_Enemy.xml': {enemy_id: guest},
        'EquipPage_Librarian.xml': {librarian_id: projection}}, f'Unexpected runtime bindings: {root}')
    expected_files = set()
    for skin in (guest, projection):
        prefix = f'Resource/CharacterSkin/{skin}/'
        expected_files.update(prefix + name for name in ('ModInfo.Xml', 'Thumb.png'))
        expected_files.update(prefix + f'ClothCustom/{action}{suffix}.png'
                              for action in actions for suffix in ('', '_front'))
    ensure(set(runtime['files']) == expected_files, f'Runtime file inventory must contain exactly 52 skin files: {root}')
    package_root = confined(root, 'build/runtime/runtime_manifest.json').parent
    files = {}
    for relative, expected in runtime['files'].items():
        source = confined(package_root, relative)
        ensure(sha(source) == expected, f'Runtime file hash mismatch: {source}')
        guards[source] = expected
        files[relative] = (source, expected)
        if source.suffix.lower() == '.png':
            with Image.open(source) as image:
                ensure(image.mode == 'RGBA' and image.size == (512, 512), f'Invalid runtime PNG: {source}')
                if f'/{guest}/ClothCustom/' in relative and source.stem.endswith('_front'):
                    ensure(image.getchannel('A').getextrema()[1] == 0, f'Guest foreground is not empty: {source}')
    for action in actions:
        full = confined(root, f'build/assembled/{action}.png')
        ensure(sha(full) == files[f'Resource/CharacterSkin/{guest}/ClothCustom/{action}.png'][1],
               f'Guest package differs from assembled build: {action}')
        guards[full] = sha(full)
        for suffix in ('', '_front'):
            cloth = confined(root, f'build/{projection}/ClothCustom/{action}{suffix}.png')
            ensure(sha(cloth) == files[f'Resource/CharacterSkin/{projection}/ClothCustom/{action}{suffix}.png'][1],
                   f'Projection package differs from build: {action}{suffix}')
            guards[cloth] = sha(cloth)
    projection_path = confined(root, f'build/{projection}/ModInfo.Xml')
    projection_xml = ET.fromstring(projection_path.read_bytes())
    guards[projection_path] = sha(projection_path)
    ensure(sha(projection_path) == files[f'Resource/CharacterSkin/{projection}/ModInfo.Xml'][1],
           f'Projection XML differs from build: {root}')
    ensure(projection_xml.findtext('ClothInfo/Name') == projection, f'Projection name mismatch: {root}')
    guest_xml = copy.deepcopy(projection_xml)
    guest_xml.find('ClothInfo/Name').text = guest
    for action in actions:
        head = projection_xml.find(f'ClothInfo/{action}/Head')
        ensure(head is not None and head.get('head_enable') == 'True', f'Projection head disabled: {action}')
        guest_xml.find(f'ClothInfo/{action}/Head').attrib.update(head_x='0', head_y='0', rotation='0', head_enable='False')
    actual_guest = ET.fromstring(files[f'Resource/CharacterSkin/{guest}/ModInfo.Xml'][0].read_bytes())
    ensure(tree_bytes(guest_xml) == tree_bytes(actual_guest), f'Guest XML is not the expected head-disabled projection: {root}')
    for skin in (guest, projection):
        ensure(files[f'Resource/CharacterSkin/{skin}/Thumb.png'][1] == sha(confined(root, 'build/assembled/Default.png')),
               f'Wrong runtime thumbnail: {skin}')
    return {'model': root.name, 'root': str(root), 'candidate': candidate, 'files': files,
            'enemy': {enemy_id: guest}, 'librarian': {librarian_id: (guest, projection)}}


def protected_files(root):
    files = {'Assemblies/Steria.dll', ENEMY, 'Data/CardInfo.xml', 'StageModInfo.xml'}
    files.update(p.relative_to(root).as_posix() for p in root.rglob('*.dll') if p.is_file())
    return {relative: sha(confined(root, relative)) for relative in sorted(files)}


def verify_guards(guards):
    for path, expected in guards.items():
        ensure(sha(path) == expected, f'Source changed since preflight: {path}')


def verify_aliases(aliases):
    for alias in aliases:
        ensure(str(Path(alias['requested']).resolve()) == alias['resolved'],
               f'Target junction changed: {alias["requested"]}')


def target_digest(path):
    ensure(not path.exists() or path.is_file(), f'Target is not a file: {path}')
    return sha(path) if path.exists() else None


def atomic_write(root, relative, data):
    ensure(root.resolve() == root, f'Target root changed resolution: {root}')
    dest = confined(root, relative)
    dest.parent.mkdir(parents=True, exist_ok=True)
    ensure(confined(root, relative) == dest, f'Destination changed while creating parent: {dest}')
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(prefix='.' + dest.name + '.', suffix='.deploy-tmp', dir=dest.parent, delete=False) as handle:
            temporary = Path(handle.name)
            handle.write(data)
            handle.flush()
            os.fsync(handle.fileno())
        ensure(temporary.resolve().is_relative_to(root.resolve()), f'Temporary file escapes target: {temporary}')
        ensure(confined(root, relative) == dest, f'Destination changed before replace: {dest}')
        os.replace(temporary, dest)
    finally:
        if temporary is not None and temporary.exists():
            ensure(temporary.resolve().is_relative_to(root.resolve()), 'Unsafe temporary cleanup path')
            temporary.unlink()


def save_report(path, report):
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def deploy(args):
    guards = {}
    packages = [read_package(root, guards) for root in args.model]
    ensure(len({p['model'] for p in packages}) == len(packages), 'Duplicate --model')
    files, enemy, librarian = {}, {}, {}
    for package in packages:
        for relative, item in package['files'].items():
            ensure(relative.casefold() not in {p.casefold() for p in files}, f'Conflicting runtime destination: {relative}')
            files[relative] = item
        ensure(not set(enemy).intersection(package['enemy']) and not set(librarian).intersection(package['librarian']), 'Conflicting book binding')
        enemy.update(package['enemy'])
        librarian.update(package['librarian'])
    requested = [('workspace', WORKSPACE_MOD)] + [(f'game_{i + 1}', p) for i, p in enumerate(args.game_mod)]
    aliases, unique = [], {}
    for label, path in requested:
        root = path.resolve()
        aliases.append({'label': label, 'requested': str(path.absolute()), 'resolved': str(root)})
        unique.setdefault(str(root).casefold(), (label, root))
    timestamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    evidence = (args.report_dir or MODELS / 'deployments' / timestamp).resolve()
    for _, root in unique.values():
        ensure(not evidence.is_relative_to(root) and not root.is_relative_to(evidence), f'Report directory overlaps mod target: {evidence}')
    for package in packages:
        root = Path(package['root'])
        ensure(not evidence.is_relative_to(root) and not root.is_relative_to(evidence), f'Report directory overlaps art model: {evidence}')
    ensure(not args.apply or not (evidence / 'backup').exists(), f'Backup directory already exists: {evidence / "backup"}')
    ensure(not args.apply or not (evidence / 'deployment.json').exists(), f'Deployment report already exists: {evidence}')
    report = {'timestamp_utc': timestamp, 'status': 'preflight', 'apply': args.apply,
              'candidates': {p['model']: p['candidate'] for p in packages}, 'path_aliases': aliases,
              'book_bindings': {'enemy_unchanged': enemy, 'librarian_projection': {k: v[1] for k, v in librarian.items()}},
              'files_per_target': len(files) + 1, 'targets': [], 'runtime_battle_smoke': 'not run'}
    operations = []
    # All target preflights finish before evidence backups or any target writes.
    for label, root in unique.values():
        ensure(root.is_dir(), f'Missing mod target: {root}')
        ensure(not root.is_relative_to(MODELS), f'Target overlaps authoring models: {root}')
        stage = ET.fromstring(confined(root, 'StageModInfo.xml').read_bytes())
        ensure(stage.findtext('Workshop/ID') == 'SteriaBuilding', f'Wrong mod identity: {root}')
        enemy_xml = ET.fromstring(confined(root, ENEMY).read_bytes())
        for book_id, skin in enemy.items():
            ensure(book(enemy_xml, book_id).findtext('CharacterSkin') == skin, f'Unexpected enemy skin: {root}, Book {book_id}')
        library_path = confined(root, LIBRARY)
        before_library = library_path.read_bytes()
        library_data = patched_books(before_library, librarian)
        entry = {'label': label, 'root': str(root), 'unchanged_baseline': protected_files(root), 'files': []}
        report['targets'].append(entry)
        target_files = {**files, LIBRARY: (None, digest(library_data))}
        for relative, (source, expected) in sorted(target_files.items()):
            dest = confined(root, relative)
            old = target_digest(dest)
            if relative == LIBRARY:
                ensure(old == digest(before_library), f'Librarian XML changed during preflight: {root}')
            backup = confined(evidence, f'backup/{label}/{relative}')
            item = {'path': relative, 'source': str(source) if source else 'target-local librarian XML patch',
                    'before': old, 'after': expected, 'backup': str(backup) if old is not None else None,
                    'action': 'unchanged' if old == expected else 'create' if old is None else 'update'}
            entry['files'].append(item)
            operations.append((root, relative, source, library_data if source is None else None, item))
    verify_guards(guards)
    verify_aliases(aliases)
    evidence.mkdir(parents=True, exist_ok=True)
    report_path = confined(evidence, 'deployment.json' if args.apply else 'plan.json')
    if not args.apply:
        report['status'] = 'dry_run'
        save_report(report_path, report)
    else:
        written = []
        try:
            # Every overwritten file is backed up and verified before the first write.
            for root, relative, _, _, item in operations:
                dest = confined(root, relative)
                ensure(target_digest(dest) == item['before'], f'Target changed before backup: {dest}')
                if item['before'] is not None:
                    backup = confined(evidence, f'backup/{next(e["label"] for e in report["targets"] if e["root"] == str(root))}/{relative}')
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(dest, backup)
                    ensure(sha(backup) == item['before'], f'Backup hash mismatch: {backup}')
            report['status'] = 'backed_up'
            save_report(report_path, report)
            verify_guards(guards)
            verify_aliases(aliases)
            for entry in report['targets']:
                ensure(protected_files(Path(entry['root'])) == entry['unchanged_baseline'], f'Protected files changed before deployment: {entry["root"]}')
            for root, relative, source, inline, item in operations:
                verify_aliases(aliases)
                dest = confined(root, relative)
                ensure(target_digest(dest) == item['before'], f'Target changed before write: {dest}')
                ensure(source is None or source.resolve() == source, f'Source path changed resolution: {source}')
                data = source.read_bytes() if source is not None else inline
                ensure(digest(data) == item['after'], f'Source changed before write: {source or dest}')
                if item['action'] != 'unchanged':
                    written.append((root, relative, item))
                    atomic_write(root, relative, data)
                ensure(sha(confined(root, relative)) == item['after'], f'Deployment hash mismatch: {dest}')
            verify_guards(guards)
            verify_aliases(aliases)
            for entry in report['targets']:
                root = Path(entry['root'])
                ensure(protected_files(root) == entry['unchanged_baseline'], f'Unrelated DLL/enemy/card/mod data changed: {root}')
                for item in entry['files']:
                    ensure(sha(confined(root, item['path'])) == item['after'], f'Final file mismatch: {root / item["path"]}')
                entry['sha256_verification'] = 'PASS'
            for alias in aliases:
                entry = next(e for e in report['targets'] if e['root'] == alias['resolved'])
                for item in entry['files']:
                    ensure(sha(confined(Path(alias['requested']), item['path'])) == item['after'], f'Alias verification mismatch: {alias["requested"]}/{item["path"]}')
                alias['sha256_verification'] = 'PASS'
                alias['files_verified'] = len(entry['files'])
            report['status'] = 'deployed_verified'
        except Exception as error:
            report['error'] = str(error)
            failures = []
            for root, relative, item in reversed(written):
                try:
                    ensure(root.resolve() == root, f'Unsafe rollback root: {root}')
                    dest = confined(root, relative)
                    if item['before'] is not None:
                        backup = Path(item['backup'])
                        ensure(backup.resolve().is_relative_to(evidence), f'Unsafe backup path: {backup}')
                        ensure(sha(backup) == item['before'], f'Corrupt rollback backup: {backup}')
                        atomic_write(root, relative, backup.read_bytes())
                        ensure(sha(confined(root, relative)) == item['before'], f'Rollback hash mismatch: {dest}')
                    elif dest.exists():
                        ensure(sha(dest) == item['after'], f'New file changed externally; refusing deletion: {dest}')
                        confined(root, relative).unlink()
                    item['rollback'] = 'verified'
                except Exception as rollback_error:
                    failures.append(str(rollback_error))
            report['status'] = 'rollback_failed' if failures else 'rolled_back'
            report['rollback_errors'] = failures
            raise RuntimeError(f'{error}; {report["status"]}; report: {report_path}') from error
        finally:
            save_report(report_path, report)
    print(json.dumps({'status': report['status'], 'files_per_target': report['files_per_target'],
                      'actual_target_count': len(unique), 'targets': [e['root'] for e in report['targets']],
                      'aliases': aliases, 'candidates': report['candidates'],
                      'planned_changes': {e['label']: sum(i['action'] != 'unchanged' for i in e['files']) for e in report['targets']},
                      'report': str(report_path)}, ensure_ascii=False, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--model', type=Path, action='append', required=True, help='Repeat for verified Ailierel_Layered_v1 / Sivier_Layered_v1 roots')
    parser.add_argument('--game-mod', type=Path, action='append', default=[], help='Repeat for game mod paths; resolved aliases are written once')
    parser.add_argument('--report-dir', type=Path, help='Local report/backup directory outside model and mod roots')
    parser.add_argument('--apply', action='store_true', help='Back up and deploy; omitted means dry-run only')
    args = parser.parse_args()
    try:
        deploy(args)
    except Exception as error:
        print(f'ERROR: {error}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
