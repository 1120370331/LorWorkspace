"""Package existing independent CPU frames at real speed and fingerprint the candidate.

The supported end-to-end entry is run_managed_preview.ps1. This script can also
re-encode already verified independent frames without regenerating art or references.
"""
from pathlib import Path
import hashlib
import json
import subprocess
import tempfile

import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = HERE / 'previews'
SHIP = ROOT / 'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
FIX = HERE / 'ManagedFixture'
NAMES = ['SeaPierce', 'SeaFarHit', 'MemorySlash', 'MemoryPierce', 'MemoryHit', 'MemoryGuard']
SIZES = ['large', 'small']
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
SMALL = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()  # This bundled build includes libx264.
BACKGROUND = (17, 22, 30)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path):
    return path.relative_to(ROOT).as_posix()


def encode(arguments, destination):
    result = subprocess.run([FFMPEG, '-hide_banner', '-loglevel', 'error', '-y'] + arguments + [
        '-c:v', 'libx264', '-crf', '19', '-preset', 'fast', '-pix_fmt', 'yuv420p',
        '-movflags', '+faststart', '-r', '60', '-an', str(destination),
    ], capture_output=True, text=True)
    if result.returncode:
        raise RuntimeError(result.stderr)


def frame_path(name, size, frame):
    return OUT / 'independent' / (name + '_' + size) / f'{frame:02}.png'


def tile_frame(size, frame):
    page = Image.new('RGB', (1280, 1132), BACKGROUND)
    draw = ImageDraw.Draw(page)
    draw.text((12, 4), 'Six original effects | independent neutral mannequins | real speed', font=FONT, fill=(235, 241, 247))
    draw.text((12, 29), 'Managed CPU simulation, not Unity/game capture | ' + ('LEFT / scale 3' if size == 'large' else 'RIGHT / scale 2.25'), font=SMALL, fill=(165, 185, 204))
    for index, name in enumerate(NAMES):
        with Image.open(frame_path(name, size, frame)) as source:
            page.paste(source.resize((640, 360), Image.Resampling.LANCZOS), ((index % 2) * 640, 52 + (index // 2) * 360))
    return page


def validate_candidate_inputs():
    snapshot = json.loads((OUT / 'managed-source-hashes.json').read_text(encoding='utf8'))
    for name, digest in snapshot['sourceFiles'].items():
        assert sha(ROOT / name) == digest, 'Simulation source changed: ' + name
    for name in ['AnhierTextureTimeline.cs', 'DiceAttackEffect_Steria_AnhierTextureCombat.cs']:
        assert (ROOT / 'SteriaBuild' / name).read_bytes() == (FIX / 'Generated' / name).read_bytes(), 'Production copy mismatch: ' + name
    assert sha(FIX / 'UnityContract.cs') == snapshot['unityContractShimSha256'], 'Contract shim changed'
    for name, digest in snapshot['fixtureSources'].items():
        assert sha(ROOT / name) == digest, 'Fixture source changed: ' + name
    for path in sorted(SHIP.glob('*')):
        assert sha(path) == snapshot['runtimeResources'][path.name], 'Simulation asset changed: ' + path.name
        assert path.read_bytes() == (FIX / 'Runtime/Resource/Effects/AnhierTextureCombat' / path.name).read_bytes(), 'Runtime resource mismatch'
    checks = (OUT / 'checks.txt').read_text(encoding='utf8').splitlines()
    assert checks and all(line.startswith('PASS ') for line in checks)
    source_log = (OUT / 'source-checks.txt').read_bytes()
    assert 'SOURCE_PASS:' in source_log.decode('utf16' if source_log.startswith(b'\xff\xfe') else 'utf-8-sig')
    data = json.loads((OUT / 'independent-frames.json').read_text(encoding='utf8'))
    assert data['fps'] == 60 and len(data['cases']) == 12
    for case in data['cases']:
        assert len(case['frames']) == 61
        assert all(abs(frame['time'] - index / 60) < .000001 for index, frame in enumerate(case['frames']))
    for name in NAMES:
        for size in SIZES:
            for index in range(61):
                assert frame_path(name, size, index).exists(), 'Missing independent frame'
    return checks


def assemble_media():
    video = OUT / 'independent-all-six-60fps.mp4'
    gif_path = OUT / 'independent-all-six-30fps.gif'
    gif_frames = []
    # Each size plays 61 actual 60fps samples, then holds the already empty end
    # for 29 frames. A size takes 1.5s; two sizes take 3s, with no time stretching.
    with tempfile.TemporaryDirectory(prefix='anhier-independent-') as temporary:
        grid = Path(temporary)
        for size_index, size in enumerate(SIZES):
            last_bytes = None
            last_gif = None
            for index in range(90):
                target = grid / f'{size_index * 90 + index:03}.png'
                if index <= 60:
                    page = tile_frame(size, index)
                    page.save(target)
                    last_bytes = target.read_bytes()
                    if index % 2 == 0:
                        last_gif = page.resize((960, 849), Image.Resampling.LANCZOS).quantize(colors=256)
                else:
                    target.write_bytes(last_bytes)
                if index % 2 == 0:
                    gif_frames.append(last_gif.copy())
        encode(['-framerate', '60', '-i', str(grid / '%03d.png'), '-vf',
                'loop=loop=2:size=180:start=0,setpts=N/(60*TB)', '-frames:v', '540'], video)
    gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:],
                       duration=[30, 40, 30] * 30, loop=0, optimize=False)
    files = [video, gif_path]
    for name in NAMES:
        destination = OUT / ('independent-' + name + '-two-sizes-60fps.mp4')
        encode(['-framerate', '60', '-i', str(frame_path(name, 'large', 0)).replace('00.png', '%02d.png'),
                '-framerate', '60', '-i', str(frame_path(name, 'small', 0)).replace('00.png', '%02d.png'),
                '-filter_complex', '[0:v][1:v]hstack=inputs=2,pad=1440:406,tpad=stop_mode=clone:stop_duration=0.4833333333,loop=loop=2:size=90:start=0,setpts=N/(60*TB)',
                '-frames:v', '270'], destination)
        files.append(destination)
    return files


def contacts():
    phases = [(0, 'EVENT'), (1, 'FULL'), (3, 'HOLD'), (9, 'HOLD'), (15, 'HOLD'), (24, 'FADE'), (40, 'VISUAL END')]
    page = Image.new('RGB', (1680, 990), BACKGROUND)
    draw = ImageDraw.Draw(page)
    for row, name in enumerate(NAMES):
        for column, (index, phase) in enumerate(phases):
            x, y = column * 240, row * 165
            draw.text((x + 5, y + 4), f'{name if column == 0 else phase} | {round(index / 60 * 1000)} ms', font=SMALL, fill=(224, 234, 244))
            with Image.open(frame_path(name, 'large', index)) as source:
                page.paste(source.resize((240, 135), Image.Resampling.LANCZOS), (x, y + 27))
    phase_path = OUT / 'independent-phase-contact.png'
    page.save(phase_path)
    page = Image.new('RGB', (1200, 2046), BACKGROUND)
    for row, name in enumerate(NAMES):
        for column, size in enumerate(SIZES):
            with Image.open(frame_path(name, size, 9)) as source:
                page.paste(source.resize((600, 338), Image.Resampling.LANCZOS), (column * 600, row * 341))
    size_path = OUT / 'independent-two-size-contact.png'
    page.save(size_path)
    return [phase_path, size_path]


def fingerprint(files, checks):
    video_reader = imageio_ffmpeg.read_frames(str(files[0]))
    video_info = next(video_reader)
    video_reader.close()
    count, seconds = imageio_ffmpeg.count_frames_and_secs(str(files[0]))
    assert abs(video_info['fps'] - 60) < .001 and count == 540 and abs(seconds - 9) < .02
    with Image.open(files[1]) as gif:
        delays = []
        for index in range(gif.n_frames):
            gif.seek(index)
            delays.append(gif.info['duration'])
    assert min(delays) >= 30 and sum(delays) == 3000, 'GIF speed or browser-safe delay mismatch'
    media_record = {'kind': 'managed CPU simulation; not Unity GPU/game capture',
                    'mp4Fps': video_info['fps'], 'mp4Frames': count, 'mp4Seconds': seconds,
                    'framesPerSizeCycle': 90, 'actualSourceFramesPerSize': 61, 'emptyHoldFrames': 29,
                    'sizesPerCycle': 2, 'cycles': 3, 'gifSourceSampleRate': 30,
                    'gifCycleMs': sum(delays), 'gifMinimumStoredDelayMs': min(delays),
                    'gifAuthoredDelaysMs': [30, 40, 30], 'gameplaySpeed': 1}
    media_checks = OUT / 'independent-media-checks.json'
    media_checks.write_text(json.dumps(media_record, indent=2) + '\n', encoding='utf8')
    production = [ROOT / 'SteriaBuild' / name for name in ['AnhierTextureTimeline.cs', 'DiceAttackEffect_Steria_AnhierTextureCombat.cs', 'HarmonyPatches.cs', 'Steria.csproj', 'SteriaModFolder/Data/CardInfo.xml', 'bin/Release/net472/Steria.dll']]
    assert all(path.exists() for path in production), 'Missing Release DLL; use run_managed_preview.ps1 -BuildRelease'
    sources = [HERE / name for name in ['manifest.json', 'README.md', 'import_masters.py', 'verify_source.py', 'prepare_managed.py', 'prepare_preview.py', 'render_independent.py', 'render_managed.py', 'run_managed_preview.ps1', 'assemble_independent_previews.py', 'imagegen_masters/inputs.json', 'ManagedFixture/RunContract.cs', 'ManagedFixture/UnityContract.cs', 'ManagedFixture/NativeContract.csproj', '.gitignore']]
    sources += [ROOT / 'ModGuideDocs/13_贴图攻击特效制作流程.md']
    sources += sorted((FIX / 'Generated').glob('*'))
    sources += sorted((FIX / 'Runtime/Assemblies').glob('NativeContract.*'))
    references = HERE / 'UnityProject/Assets/Reference'
    sources += [references / name for name in ['native-skins.json', 'native-effects.json', 'fixture-config.json']]
    evidence = [OUT / name for name in ['managed-source-hashes.json', 'managed-frames.json', 'independent-frames.json', 'runtime-evidence.json', 'independent-content-bounds.json', 'checks.txt', 'source-checks.txt', 'managed-contract.log']]
    if (OUT / 'release-build.log').exists():
        evidence.append(OUT / 'release-build.log')
    sources += sorted(SHIP.glob('*')) + sorted((HERE / 'source_assets').glob('*.png'))
    sources += sorted((OUT / 'independent').glob('*/*.png'))
    paths = production + sources + evidence + files + [media_checks]
    hashes = {relative(path): sha(path) for path in sorted(set(paths))}
    candidate_id = hashlib.sha256(json.dumps(hashes, sort_keys=True).encode('utf8')).hexdigest()
    record = {'candidateId': candidate_id,
              'kind': 'independent original effects; exact production/native managed simulation',
              'productionCopyMatches': True, 'nativeReferenceOnly': True,
              'shipping': 'Only original PNG/JSON and the mod DLL; no copied game skin or native code',
              'primaryPresentation': 'Opaque authored neutral mannequins; no Bada pixels or Effect(9) layer',
              'nearSpacingBeforeActorScale': {'self': 3, 'target': -3},
              'farSpacingBeforeActorScale': {'self': 5, 'target': -6.5},
              'sourceCheck': 'PASS', 'managedContractChecksPassed': len(checks),
              'visualAcceptance': 'Pending independent and main review',
              'evidenceLimits': 'No Unity GPU, actual game camera, audio or battle execution verified. Unity 2019.3/2019.4 license blocked; managed simulation is the authorized delivery path.',
              'mediaTiming': media_record, 'primaryMedia': [path.relative_to(OUT).as_posix() for path in files],
              'files': hashes}
    (OUT / 'candidate-hashes.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf8')
    print('INDEPENDENT_MEDIA_PASS: 60fps / 540 frames / 9 seconds; 30fps GIF / 3000ms / delays >=30ms', flush=True)
    print('CANDIDATE_ID: ' + candidate_id, flush=True)


if __name__ == '__main__':
    verified_checks = validate_candidate_inputs()
    media = assemble_media()
    media += contacts()
    fingerprint(media, verified_checks)
