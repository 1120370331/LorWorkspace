"""Package native rendered frames without retiming or adding effect imagery."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw
import imageio_ffmpeg


def main():
    output = Path(sys.argv[1]).resolve()
    stages = [6, 18, 33, 54, 75, 90]
    media = []
    for facing in ["dark-canonical", "dark-mirrored", "mid-canonical", "mid-mirrored"]:
        frames = sorted((output / facing).glob("frame_*.png"))
        assert len(frames) == 91, (facing, len(frames))
        contact = Image.new("RGB", (1280, 1152), "#10151d")
        draw = ImageDraw.Draw(contact)
        for index, stage in enumerate(stages):
            x, y = (index % 2) * 640, (index // 2) * 384
            with Image.open(output / f"{facing}_{stage:03}.png") as image:
                contact.paste(image.convert("RGB").resize((640, 360), Image.Resampling.LANCZOS), (x, y + 24))
            draw.text((x + 12, y + 6), f"{facing} | {stage / 60:.2f}s | exact native frame", fill="white")
        sheet = output / f"{facing}-stages.jpg"
        contact.save(sheet, quality=94)
        video = output / f"{facing}-60fps.mp4"
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-y",
            "-framerate", "60", "-i", str(output / facing / "frame_%03d.png"),
            "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(video)], check=True)
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-i", str(video), "-f", "null", "-"], check=True)
        media.extend([sheet, video])
    light_contact = Image.new("RGB", (1280, 1152), "#10151d")
    light_draw = ImageDraw.Draw(light_contact)
    for index, stage in enumerate(stages):
        x, y = (index % 2) * 640, (index // 2) * 384
        with Image.open(output / f"light-only_{stage:03}.png") as image:
            light_contact.paste(image.convert("RGB").resize((640, 360), Image.Resampling.LANCZOS), (x, y + 24))
        light_draw.text((x + 12, y + 6), f"Light only | {stage / 60:.2f}s | native GPU", fill="white")
    light_sheet = output / "light-only-stages.jpg"
    light_contact.save(light_sheet, quality=94)
    media.append(light_sheet)
    record = {str(p.relative_to(output)): hashlib.sha256(p.read_bytes()).hexdigest() for p in media}
    (output / "media-hashes.json").write_text(json.dumps(record, indent=2) + "\n")
    print("Native media encoded and decoded: dark/mid gray x two facings, 91 frames at 60 fps; plus native light-only stages. No visual acceptance implied.")


if __name__ == "__main__":
    main()
