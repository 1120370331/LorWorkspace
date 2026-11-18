"""Replay the staged card through its real Unity 2019 mass manager in an isolated player."""
import argparse
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

from package import ART, DLL, GAME, HERE, ROOT, WORK, read, sha, write
import native_smoke as starlance


def video_encoder():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except (ImportError, RuntimeError):
        return 'ffmpeg'


def stage_actor(folder, label, output, motions):
    info = ET.parse(next(folder.glob('ModInfo.*')))
    for motion in motions:
        pivot = info.find(f'.//{motion}/Pivot')
        x, y = [(512 + float(pivot.attrib['pivot_' + axis])) / 1024 for axis in ('x', 'y')]
        shutil.copy2(folder / 'ClothCustom' / f'{motion}.png', output / f'{label}-{motion}.png')
        (output / f'{label}-{motion}.txt').write_text(f'{x},{y}', encoding='ascii')


def run(release, evidence):
    evidence.mkdir(parents=True)
    built = read(release / 'built-metadata.json')
    candidate = Path(built['dll'])
    staged = evidence / 'StagedMod'
    (staged / 'Assemblies').mkdir(parents=True)
    (staged / 'Data').mkdir()
    shutil.copy2(candidate, staged / DLL)
    shutil.copy2(GAME / 'Data/CardInfo.xml', staged / 'Data/CardInfo.xml')
    shutil.copytree(release / 'payload' / ART, staged / ART)
    characters = ROOT / 'AceTaffy/AceTaffyMod/Resource/CharacterSkin'
    stage_actor(GAME / 'Resource/CharacterSkin/Christasha', 'caster', evidence, ('Default',))
    stage_actor(characters / 'AceTaffy_Invention_0_3', 'knight', evidence, ('Default', 'Damaged'))
    stage_actor(characters / 'AceTaffy_Invention_1_2', 'shield', evidence, ('Default', 'Damaged'))

    starlance.WORK = WORK
    player, data = starlance.copy_player()
    managed = data / 'Managed'
    original = starlance.GAME_DATA / 'Managed/Assembly-CSharp.dll'
    refs = [original, candidate, starlance.MONO_LIBS / '0Harmony.dll']
    refs += [starlance.GAME_DATA / 'Managed' / ('UnityEngine.' + name + 'Module.dll')
             for name in ('Core', 'ImageConversion', 'Animation')]
    fixture = starlance.compile_fixture(evidence / 'fixture', HERE / 'NativeSmoke.cs.txt', 'XiyinNativeFixture', 'net472', refs)
    patcher = starlance.compile_fixture(evidence / 'bootstrap', 'BootstrapFixture.cs.txt', 'XiyinBootTool', 'net8.0',
                                        [starlance.MONO_LIBS / 'Mono.Cecil.dll'])
    shutil.copy2(fixture, managed / fixture.name)
    subprocess.run(['dotnet', str(patcher), str(original), str(fixture), str(managed / original.name)], check=True)
    deps = managed / 'SmokeDependencies'
    if not deps.exists():
        source = ROOT / 'output/christasha-starlance/isolated-vfx-player/StarlancePlayer_Data/Managed/SmokeDependencies'
        shutil.copytree(source, deps)
    write(evidence / 'inputs.json', dict(dll_sha256=sha(staged / DLL),
          card_xml_sha256=sha(staged / 'Data/CardInfo.xml'),
          atlases={p.relative_to(staged).as_posix(): sha(p) for p in (staged / ART).iterdir()},
          unity_player_sha256=sha(player.parent / 'UnityPlayer.dll'), fixture_sha256=sha(fixture),
          original_game_assembly_sha256=sha(original), staged_assembly_sha256=sha(managed / original.name)))
    log = evidence / 'acceptance.log'
    args = [str(player), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '720', '-screen-height', '405',
            '-job-worker-count', '4', '-logFile', str(log), '-xiyinOutput', str(evidence)]
    startup = subprocess.STARTUPINFO(dwFlags=subprocess.STARTF_USESHOWWINDOW, wShowWindow=subprocess.SW_HIDE)
    result = subprocess.run(args, cwd=player.parent, startupinfo=startup, timeout=95)
    if result.returncode or not (evidence / 'checks.txt').is_file():
        if log.is_file():
            print(log.read_text(encoding='utf-8', errors='replace')[-8000:])
        raise RuntimeError(f'Isolated player failed ({result.returncode}): {evidence}')
    subprocess.run([video_encoder(), '-v', 'error', '-framerate', '60', '-i', str(evidence / 'frames/%03d.png'),
                    '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                    str(evidence / 'xiyin-native-60fps.mp4')], check=True)
    print((evidence / 'checks.txt').read_text(encoding='utf-8').strip())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('release', type=Path)
    parser.add_argument('evidence', type=Path)
    args = parser.parse_args()
    run(args.release.resolve(strict=True), args.evidence.resolve())
