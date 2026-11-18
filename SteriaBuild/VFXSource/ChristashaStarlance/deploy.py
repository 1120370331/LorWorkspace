"""Back up and install a verified release into the game and repository package."""
import argparse
import datetime
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

from release import DLL, GAME, ROOTS, WORK, hashfile, read


def contained(root, rel):
    path = (root / rel).resolve()
    assert path.is_relative_to(root.resolve()), path
    return path


def atomic(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(prefix='.' + path.name, suffix='.tmp', dir=path.parent)
    try:
        with os.fdopen(fd, 'wb') as file:
            file.write(data)
            file.flush()
            os.fsync(file.fileno())
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


def game_closed():
    process = subprocess.run(['powershell', '-NoProfile', '-Command',
        "Get-Process | Where-Object {$_.ProcessName -match '^LibraryOfRuina$|^Library Of Ruina$'} | Select-Object -ExpandProperty ProcessName"],
        check=True, capture_output=True, text=True)
    if process.stdout.strip():
        raise RuntimeError('The game is running and locks its loaded mod assembly')


def deploy(release):
    proof = read(release / 'release.json')
    assert proof['status'] == 'RELEASE_VERIFIED' and Path(proof['game_root']) == GAME
    order = proof['payload_order']
    assert order[-1] == DLL
    alias = Path('C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder')
    assert alias.resolve(strict=True) == GAME
    game_closed()
    utc = datetime.datetime.now(datetime.timezone.utc)
    backup = WORK / 'deployments' / utc.strftime('%Y%m%dT%H%M%SZ')
    backup.mkdir(parents=True)
    receipt = {'status': 'PREPARING', 'release': str(release), 'art_candidate': proof['art_candidate'],
               'game': str(GAME), 'steam_alias': str(alias), 'native_evidence': proof['native_evidence'],
               'preserved': proof['preserved'], 'items': []}
    written = []

    def save():
        atomic(backup / 'receipt.json', (json.dumps(receipt, ensure_ascii=False, indent=2)+'\n').encode('utf-8'))

    def verify_preserved():
        for label, root in ROOTS:
            for rel, expected in proof['preserved'][label].items():
                assert hashfile(contained(root, rel)) == expected, (label, rel)

    try:
        verify_preserved()
        for rel in order:
            assert hashfile(release / 'payload' / rel) == proof['payload'][rel], rel
            for label, root in ROOTS:
                path = contained(root, rel)
                previous = hashfile(path)
                assert previous == proof['before_payload'][label][rel], (label, rel, 'target drifted')
                stored = None
                if previous is not None:
                    stored = contained(backup, Path(label) / rel)
                    stored.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(path, stored)
                    assert hashfile(stored) == previous, stored
                receipt['items'].append(dict(path=str(path), relative=rel, root=label, before=previous,
                    after=proof['payload'][rel], backup=str(stored) if stored else None))
        receipt['status'] = 'BACKED_UP'
        save()
        game_closed()
        for item in receipt['items']:
            if item['relative'] == DLL:
                game_closed()
            path = Path(item['path'])
            assert hashfile(path) == item['before'], (path, 'target drifted after backup')
            atomic(path, (release / 'payload' / item['relative']).read_bytes())
            written.append(item)
            assert hashfile(path) == item['after'], path
        verify_preserved()
        for _, root in (*ROOTS, ('steam alias', alias)):
            for rel, expected in proof['payload'].items():
                assert hashfile(contained(root, rel)) == expected, (root, rel)
        receipt['status'] = 'DEPLOYED_VERIFIED'
        receipt['finished_utc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        save()
    except BaseException as error:
        receipt['status'] = 'FAILED'
        receipt['error'] = str(error)
        for item in reversed(written):
            path = Path(item['path'])
            if hashfile(path) != item['after']:
                continue
            if item['backup']:
                atomic(path, Path(item['backup']).read_bytes())
            else:
                path.unlink()
        save()
        raise
    print('DEPLOYED_VERIFIED', backup / 'receipt.json')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('release')
    deploy(contained(WORK, parser.parse_args().release))
