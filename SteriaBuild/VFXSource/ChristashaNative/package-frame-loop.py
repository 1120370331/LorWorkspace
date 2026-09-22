"""Assemble unchanged GPU readbacks into stage sheets and a 60 fps strike reel."""
from pathlib import Path
import argparse
import subprocess
from PIL import Image, ImageDraw
import imageio_ffmpeg

parser = argparse.ArgumentParser()
parser.add_argument("candidate", type=Path)
args = parser.parse_args()
root = args.candidate.resolve()
if (root / "complete.txt").read_text().strip() != "PASS":
    raise SystemExit("Frame-loop acceptance did not pass")
recipes = ["Slash", "Hit", "Pierce", "Projectile", "Cast", "Area", "Point"]
for background in ("dark", "mid"):
    sheet = Image.new("RGB", (1440, len(recipes) * 184), (25, 28, 34))
    draw = ImageDraw.Draw(sheet)
    for row, recipe in enumerate(recipes):
        for col, frame in enumerate((1, 6, 10, 17, 30)):
            path = root / recipe / f"{background}_{frame:04d}.png"
            if path.exists():
                with Image.open(path) as im:
                    sheet.paste(im.resize((288, 162)), (col * 288, row * 184 + 22))
            draw.text((col * 288 + 6, row * 184 + 4), f"{recipe} | {frame / 60:.3f}s", fill="white")
    sheet.save(root / f"stages-{background}.jpg", quality=93)

reel = root / "reel"
reel.mkdir(exist_ok=True)
index = 0
for recipe in ("Pierce", "Projectile", "PierceContext", "ProjectileContext"):
    frames = sorted((root / recipe).glob("dark_*.png"))
    for source in frames:
        with Image.open(source) as im:
            frame = im.convert("RGB")
        draw = ImageDraw.Draw(frame)
        draw.rectangle((0, 0, 960, 24), fill=(20, 22, 26))
        draw.text((12, 6), f"{recipe} | Unity 2019 Play Mode | 60 fps | {int(source.stem.split('_')[-1])/60:.3f}s", fill="white")
        frame.save(reel / f"{index:04d}.png")
        index += 1
    for _ in range(24):
        frame.save(reel / f"{index:04d}.png")
        index += 1
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error", "-framerate", "60", "-i", str(reel / "%04d.png"),
                "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(root / "strikes-60fps.mp4")], check=True)
print(root / "strikes-60fps.mp4")
