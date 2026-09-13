"""Package actual offscreen renders; never generates or retouches VFX artwork."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from PIL import Image, ImageDraw, ImageFont, ImageOps
import imageio_ffmpeg


def font(size):
    return ImageFont.truetype('C:/Windows/Fonts/arial.ttf', size)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('candidate', type=Path)
    parser.add_argument('--startup', action='store_true', help='Include 0/17/33 ms as the first three comparison rows.')
    parser.add_argument('--recipe', choices=['Slash', 'Hit'], default='Slash')
    parser.add_argument('--timing', action='store_true', help='Select the requested speed/duration milestone frames.')
    args = parser.parse_args()
    output = args.candidate.resolve()
    frames = [0, 1, 2, 6, 17, 26] if args.startup else [1, 3, 6, 10, 17, 26]
    headings = ['ORIGINAL DATA / EDITOR REPLAY', 'CHRISTASHA / NATIVE THEME CANDIDATE']
    if args.recipe == 'Hit':
        frames = [1, 4, 8, 10, 17, 30]
        headings = ['GREATSWORD SUBSET / EDITOR REPLAY', 'CHRISTASHA / VERTICAL SWORD SLAM']
    if args.timing:
        frames = [1, 3, 6, 10, 17, 24, 45, 65] if args.recipe == 'Hit' else [1, 3, 6, 10, 17, 30, 45, 78]
    for bg in ['dark', 'mid']:
        board = Image.new('RGB', (1920, 88 + 540 * len(frames)), '#141619')
        draw = ImageDraw.Draw(board)
        for column, name in enumerate(headings):
            draw.text((column * 960 + 28, 24), name, font=font(26), fill='#ead8b7')
        for row, frame in enumerate(frames):
            for col, side in enumerate(['original', 'candidate']):
                image = Image.open(output / side / f'{bg}_{frame:04d}.png').convert('RGB')
                board.paste(image, (col * 960, 88 + row * 540))
                draw.text((col * 960 + 20, 100 + row * 540), f'{frame / 60:.3f} s', font=font(22), fill='#e5e2de')
        board.save(output / f'comparison-{bg}.png')
        gray = ImageOps.grayscale(board).convert('RGB')
        gray.save(output / f'comparison-{bg}-grayscale.png')

    # Two synchronized replays separated by a blank beat, both at normal 60 fps.
    command = [imageio_ffmpeg.get_ffmpeg_exe(), '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pixel_format', 'rgb24',
               '-video_size', '1920x624', '-framerate', '60', '-i', 'pipe:0', '-c:v', 'libx264',
               '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(output / 'comparison-normal-speed.mp4')]
    process = subprocess.Popen(command, stdin=subprocess.PIPE)
    for bg in ['dark', 'mid']:
        for repeat in range(2):
            for frame in range(85):
                canvas = Image.new('RGB', (1920, 624), '#141619')
                draw = ImageDraw.Draw(canvas)
                for col, side in enumerate(['original', 'candidate']):
                    draw.text((col * 960 + 24, 17), headings[col], font=font(24), fill='#ead8b7')
                    draw.text((col * 960 + 24, 50), f'{frame / 60:.3f} s | native render path / fixed camera / 60 fps', font=font(18), fill='#aaaeb4')
                    canvas.paste(Image.open(output / side / f'{bg}_{frame:04d}.png').convert('RGB'), (col * 960, 84))
                process.stdin.write(canvas.tobytes())
            blank = Image.new('RGB', (1920, 624), '#141619').tobytes()
            for _ in range(18):
                process.stdin.write(blank)
    process.stdin.close()
    if process.wait() != 0:
        raise RuntimeError('ffmpeg encoding failed')

    source = Path(__file__).resolve().parents[2] / 'ChristashaNativeParticleLibrary.cs'
    editor_name = 'ChristashaNativeHitPreview.cs' if args.recipe == 'Hit' else 'ChristashaNativePreview.cs'
    paths = [source, Path(__file__).with_name('Editor') / editor_name]
    if args.recipe == 'Hit':
        paths += [source.with_name('ChristashaNativeMassRecipes.cs'), Path(__file__).with_name('render-hit.ps1')]
    paths += [Path(__file__).with_name('Assets') / 'tide-brush-imagegen-v1.png',
              Path(__file__).with_name('Assets') / 'tide-brush-imagegen-v1-prompt.txt',
              Path(__file__).with_name('render-slash.ps1'), Path(__file__)]
    paths += [output / 'original' / 'metadata.txt', output / 'candidate' / 'metadata.txt']
    paths += sorted(output.glob('*.png')) + [output / 'comparison-normal-speed.mp4', output / 'complete.txt', output / 'focused-checks.txt']
    receipt = {'state': 'candidate_pending_independent_visual_review', 'fps': 60,
               'renderer': 'Unity 2019.3.15f1 D3D11 original native compiled shaders',
               'boundary': 'Original data Editor replay, not game combat. No desktop interaction.',
               'files': [{'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths]}
    (output / 'candidate-manifest.json').write_text(json.dumps(receipt, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
