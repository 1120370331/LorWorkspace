"""Package actual Unity-rendered frames; this script never paints the effect."""
import argparse
import hashlib
import json
import subprocess
import imageio_ffmpeg
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--revision', default='candidate-r1')
parser.add_argument('--showcase-only', action='store_true', help='Repackage the accepted caller showcase without re-exporting Unity or replacing source snapshots')
args = parser.parse_args()
root = Path(__file__).resolve().parent
out = root / 'evidence' / args.revision
font_path = 'C:/Windows/Fonts/consola.ttf'
font = ImageFont.truetype(font_path, 20)

for folder in ([] if args.showcase_only else sorted(out.iterdir())):
    if not folder.is_dir() or not list(folder.glob('*.png')):
        continue
    frame_numbers = [0, 1, 3, 6, 11, 21, 36, 45]
    if folder.name.startswith('Mass'):
        frame_numbers = [0, 9, 18, 30, 35, 45, 66, 107]
    elif folder.name.startswith('Area'):
        frame_numbers = [0, 3, 6, 11, 21, 36, 66, 73]
    canvas = Image.new('RGB', (1920, 634), '#151d22')
    draw = ImageDraw.Draw(canvas)
    for i, number in enumerate(frame_numbers):
        path = folder / f'{number:04d}.png'
        if not path.exists():
            continue
        x, y = (i % 4) * 480, (i // 4) * 312
        image = Image.open(path).resize((480, 270), Image.Resampling.LANCZOS)
        canvas.paste(image, (x, y + 32))
        draw.text((x + 10, y + 5), f'{folder.name} {number / 60:.3f}s', font=font, fill='#efe8cd')
    canvas.save(out / f'{folder.name}-contact.png')
    if not folder.name.endswith('-mid'):
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-loglevel', 'error', '-framerate', '60', '-i', str(folder / '%04d.png'), '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(out / (folder.name + '-60fps.mp4'))], check=True)

showcase = ['Slash-dark', 'Hit-dark', 'Pierce-dark', 'ProjectilePath-left-dark', 'Mass-dark']
if all((out / name).is_dir() for name in showcase):
    command = [imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pixel_format', 'rgb24', '-video_size', '1280x720', '-framerate', '60', '-i', 'pipe:0', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(out / 'showcase-60fps.mp4')]
    process = subprocess.Popen(command, stdin=subprocess.PIPE)
    try:
        for name in showcase:
            frames = sorted((out / name).glob('*.png'))
            for repeat in range(2):
                for frame in frames:
                    image = Image.open(frame).convert('RGB')
                    ImageDraw.Draw(image).text((22, 22), f'{name.replace("-dark", "")} | 1x / 60fps | Unity proxy', font=font, fill='#e6dfc5')
                    process.stdin.write(image.tobytes())
    finally:
        process.stdin.close()
    assert process.wait() == 0, 'Showcase encoding failed'
if args.showcase_only:
    print(f'Accepted caller showcase repackaged without Unity: {out / "showcase-60fps.mp4"}')
    raise SystemExit(0)

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

source = root.parents[1] / 'ChristashaParticleVisual.cs'
mirror = root / 'UnityProject/Assets/Scripts/ChristashaParticleVisual.cs'
assert sha(source) == sha(mirror), 'Canonical driver differs from Unity preview source'
files = [source]
files += sorted(p for p in (root / 'UnityProject/Assets').rglob('*') if p.is_file())
files += sorted(p for p in (root / 'UnityProject/ProjectSettings').rglob('*') if p.is_file())
files += sorted(p for p in (root / 'UnityProject/Packages').rglob('*') if p.is_file())
files += [root / 'verify_source.py', root / 'build_preview.ps1', root / 'package_evidence.py', root / 'README.md']
bundle = root / 'UnityProject/AssetBundles/steria_christasha_particle_v2'
files.append(bundle)
manifest = {
    'state': 'candidate-awaiting-independent-visual-acceptance',
    'unity': '2019.3.15f1', 'pipeline': 'Built-in Gamma',
    'renderer': 'D3D11 Camera.Render actual GPU ParticleSystem',
    'preview': 'Opaque neutral proxy battle; not a game capture or proof of actual game pivots',
    'source': 'Original mathematical ribbon meshes and basic brush/leaf/mist masks. No third-party or original-game assets copied.',
    'license': 'Repository-authored; no external asset license dependencies',
    'seed': [194, 781], 'fps': 60, 'simulation_step': '1/120',
    'mass_timeline': {'cast': 0, 'area': 0.3, 'earliest_manager_release_and_success_points': 0.5, 'area_x': 0.3, 'area_scale_x': 13.4 / 13, 'point_count': 3, 'unsuccessful_target_point_count': 0},
    'guard': 'Runnable editor example only; not one of the seven shipping bundle roots, no game-page binding',
    'camera': {'resolution': [1280, 720], 'orthographicSize': 4.6, 'position': [0, 0.3, -20], 'HDR': False, 'bloom': False},
    'scale': {'actor_reference_height': 3, 'map_scales': [3, 2.25], 'native_attack_multiplier': 2, 'recipe_melee_unit': '1/3'},
    'assets': {str(p.relative_to(root)) if p.is_relative_to(root) else '../../ChristashaParticleVisual.cs': sha(p) for p in files},
    'media': {p.name: sha(p) for p in sorted(out.iterdir()) if p.suffix in ['.png', '.mp4', '.csv', '.txt']}
}
(out / 'candidate-manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
snapshot = out / 'source-snapshot'
snapshot.mkdir(exist_ok=True)
for path in [source, root / 'UnityProject/Assets/Editor/ChristashaParticleBuilder.cs', root / 'UnityProject/Assets/ChristashaV2/Shaders/ParticleAlpha.shader', bundle]:
    shutil.copy2(path, snapshot / path.name)
print(f'V2 media and candidate manifest: {out}')
