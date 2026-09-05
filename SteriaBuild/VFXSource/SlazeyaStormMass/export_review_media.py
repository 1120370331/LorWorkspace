"""Assemble only the native bundle-rendered frames. Never synthesizes effect imagery."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[3]
OUTPUT = REPO / "preview_exports" / "slazeya_storm_mass" / "round4"


def select_encoder():
    """Prefer software H.264; PATH builds such as conda may omit libx264."""
    candidates = []
    try:
        import imageio_ffmpeg
        candidates.append(imageio_ffmpeg.get_ffmpeg_exe())
    except (ImportError, RuntimeError):
        pass
    candidates.append(shutil.which("ffmpeg"))
    available = []
    for executable in dict.fromkeys(path for path in candidates if path):
        try:
            probe = subprocess.run([executable, "-hide_banner", "-encoders"],
                                   capture_output=True, text=True, check=True)
        except (OSError, subprocess.CalledProcessError):
            continue
        encoders = {parts[1] for line in probe.stdout.splitlines()
                    if len(parts := line.split()) >= 2 and len(parts[0]) == 6 and parts[0].startswith("V")}
        if "gif" in encoders:
            available.append((executable, encoders))
    for codec, options in (("libx264", ["-crf", "18"]), ("mpeg4", ["-q:v", "3"])):
        for executable, encoders in available:
            if codec in encoders:
                return executable, codec, options
    raise RuntimeError("No available ffmpeg provides GIF and libx264 or mpeg4 encoding")


def verify_media(ffmpeg, path):
    """Decode the whole output, so successful encoding alone is not the gate."""
    result = subprocess.run([ffmpeg, "-hide_banner", "-v", "error", "-xerror", "-i", str(path),
                             "-map", "0:v:0", "-progress", "pipe:1", "-f", "null", "-"],
                            capture_output=True, text=True, check=True)
    frames = [int(line.split("=", 1)[1]) for line in result.stdout.splitlines() if line.startswith("frame=")]
    if not frames or frames[-1] == 0 or path.stat().st_size == 0:
        raise RuntimeError(f"No decodable frames in {path}")
    return {"path": str(path), "bytes": path.stat().st_size, "decodedFrames": frames[-1],
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper()}


def main():
    global OUTPUT
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=OUTPUT)
    OUTPUT = parser.parse_args().output.resolve()
    manifest = json.loads((OUTPUT / "manifest.json").read_text(encoding="utf-8-sig"))
    names = manifest["representativeFiles"]
    times = [f"G={t:.3f}s" if t < manifest["callbackTime"] else f"B+{t-manifest['callbackTime']:.3f}s"
             for t in manifest["representativeTimes"]]
    tile_w, tile_h, label_h = 640, 360, 40
    columns = 4
    rows_per_background = (len(names) + columns - 1) // columns
    sheet = Image.new("RGB", (tile_w * columns, 96 + (tile_h + label_h) * rows_per_background * 2), "#13191f")
    draw = ImageDraw.Draw(sheet)
    font_path = Path("C:/Windows/Fonts/segoeui.ttf")
    font = ImageFont.truetype(str(font_path), 20) if font_path.exists() else ImageFont.load_default()
    draw.text((18, 12), f"SLAZEYA / STORM MASS / {manifest.get('version', 'round1')}    |    Native Unity bundle readback", font=font, fill="#e4edf3")
    draw.text((18, 46), f"H=6 | XZ ground / Y up | 5 recipients + caster left | Actual callback B={manifest['callbackTime']:.3f}s | Shared controller", font=font, fill="#8ca3b4")
    for row, background in enumerate(("black", "battlefield")):
        for index, name in enumerate(names):
            path = OUTPUT / f"{background}_{name}.png"
            with Image.open(path) as original:
                if original.size != (manifest["width"], manifest["height"]):
                    raise ValueError(f"Unexpected native frame size: {path}")
                tile = original.convert("RGB").resize((tile_w, tile_h), Image.Resampling.LANCZOS)
            x = (index % columns) * tile_w
            y = 96 + (row * rows_per_background + index // columns) * (tile_h + label_h)
            draw.text((x + 12, y + 8), f"{background} / {name[3:]} / {times[index]}", font=font, fill="#c4d5df")
            sheet.paste(tile, (x, y + label_h))
            tile.resize((320, 180), Image.Resampling.LANCZOS).save(OUTPUT / f"{background}_{name}_thumbnail.png")
    sheet.save(OUTPUT / "contact_sheet.png")
    ffmpeg, codec, quality = select_encoder()
    print(f"Using ffmpeg={ffmpeg}; MP4 encoder={codec}", flush=True)
    media = []
    for background in ("black", "battlefield"):
        sequence = OUTPUT / f"{background}_sequence" / "frame_%04d.png"
        video = OUTPUT / f"{background}_motion.mp4"
        subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-framerate", "30",
                        "-i", str(sequence), "-c:v", codec, *quality, "-pix_fmt", "yuv420p",
                        "-movflags", "+faststart", str(video)], check=True)
        media.append(verify_media(ffmpeg, video))
    gif = OUTPUT / "battlefield_motion.gif"
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-framerate", "30",
                    "-i", str(OUTPUT / "battlefield_sequence" / "frame_%04d.png"),
                    "-filter_complex", "fps=20,scale=960:-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse",
                    "-loop", "0", str(gif)], check=True)
    media.append(verify_media(ffmpeg, gif))
    (OUTPUT / "media-manifest.json").write_text(json.dumps({
        "ffmpeg": ffmpeg, "mp4Encoder": codec, "source": "Unmodified native Unity PNG sequences",
        "bundleSha256": manifest["bundleSha256"], "media": media,
        "exporterSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest().upper(),
    }, indent=2), encoding="utf-8")
    print(f"PASS contact sheet, two MP4s and GIF; full decode verified: {OUTPUT}")


if __name__ == "__main__":
    main()
