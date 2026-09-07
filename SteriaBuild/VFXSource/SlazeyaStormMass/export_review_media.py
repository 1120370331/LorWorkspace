"""Assemble only the native bundle-rendered frames. Never synthesizes effect imagery."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[3]
OUTPUT = REPO / "preview_exports" / "slazeya_storm_mass" / "round5"


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


def export_burst_comparison(output, baseline, previous_candidate=None):
    """Presentation only: retain both fixed-camera source sets and label the separate camera pass."""
    digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
    candidate = json.loads((output / "manifest.json").read_text(encoding="utf-8-sig"))
    original = json.loads((baseline / "manifest.json").read_text(encoding="utf-8-sig"))
    assert original["bundleSha256"] == "918B3213A65BA542FAEC7F83CE6926A57CE464A81A8990DD65272B6E3AACB8C5"
    assert original["controllerSha256"] == "3DA8605425D84085F1F62704A5BC69BC01629941C188813E17D4C0961EF025C9"
    for key in ("width", "height", "referenceBodyHeight", "callbackTime", "orthographicSize", "cameraPosition", "cameraLookAt", "recipientFeet", "simulationHz"):
        assert candidate[key] == original[key], f"A/B camera or timing mismatch: {key}"
    presentation = output / "comparison-presentation"
    presentation.mkdir(exist_ok=False)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 24)
    rows = []
    evidence = {"baseline": str(baseline), "candidate": str(output), "baselineBundleSha256": original["bundleSha256"],
                "candidateBundleSha256": candidate["bundleSha256"], "candidateControllerSha256": candidate["controllerSha256"], "fixedFrames": [], "unchangedGatherAndB0": []}
    def fixed_frame(directory, step):
        path = directory / "fixed-comparison" / f"battlefield_B{step:02d}.png"
        if not path.exists() and step == 21:
            # Original fixed 30fps frame57 is global1.90s: callback1.55 + .35, no new baseline render.
            assert abs(json.loads((directory / "manifest.json").read_text(encoding="utf-8-sig"))["callbackTime"] - 1.55) < 1e-6
            path = directory / "battlefield_sequence/frame_0057.png"
        return path
    for step, requested in ((3, .05), (7, .12), (15, .25), (21, .35), (27, .45), (48, .8)):
        name = f"battlefield_B{step:02d}.png"
        paths = [fixed_frame(directory, step) for directory in (baseline, output)]
        sheet = Image.new("RGB", (2560, 780), "#14191e")
        draw = ImageDraw.Draw(sheet)
        for column, (path, label) in enumerate(zip(paths, ("A / original round5", "B / " + candidate["version"]))):
            with Image.open(path) as source:
                assert source.size == (1280, 720)
                sheet.paste(source.convert("RGB"), (column * 1280, 60))
            draw.text((column * 1280 + 18, 15), f"{label} | requested B+{requested:.2f} | native B+{step/60:.6f}s", font=font, fill="#e0eaf2")
        sheet.save(presentation / f"AB_B{step:02d}.png")
        rows.append(sheet.resize((1280, 390), Image.Resampling.LANCZOS))
        evidence["fixedFrames"].append({"requestedBurstAge": requested, "sampledBurstAge": step / 60,
                                       "sourceFiles": [str(p) for p in paths], "sha256": [digest(p) for p in paths]})
    overview = Image.new("RGB", (1280, 390 * len(rows)), "#14191e")
    for i, row in enumerate(rows):
        overview.paste(row, (0, i * 390))
    overview.save(presentation / "fixed-AB-overview.png")
    if previous_candidate:
        evidence["previousCandidate"] = str(previous_candidate)
        crops = ((365, 405, 580, 525), (910, 475, 1170, 565))
        for step in (15, 21, 27):
            paths = [fixed_frame(directory, step) for directory in (previous_candidate, output)]
            pair = Image.new("RGB", (2560, 780), "#14191e")
            for column, path in enumerate(paths):
                with Image.open(path) as source:
                    pair.paste(source.convert("RGB"), (column * 1280, 60))
                ImageDraw.Draw(pair).text((column * 1280 + 16, 16), f"{'Rejected first candidate' if column == 0 else 'Revision2: 32 long / 80 short'} | native B+{step/60:.2f}", font=font, fill="#e0eaf2")
            pair.save(presentation / f"revision1-vs-revision2_B{step:02d}.png")
            if step == 27:
                crop_sheet = Image.new("RGB", (1280, 560), "#14191e")
                for row, box in enumerate(crops):
                    for column, path in enumerate(paths):
                        with Image.open(path) as source:
                            crop = source.convert("RGB").crop(box)
                        crop = crop.resize((crop.width*2, crop.height*2), Image.Resampling.NEAREST)
                        crop_sheet.paste(crop, (column*640, 40+row*280))
                        ImageDraw.Draw(crop_sheet).text((column*640+8, row*280+8), f"{'First' if column == 0 else 'Revision2'} / {'front-left' if row == 0 else 'front-right'} / 2x", font=font, fill="#e0eaf2")
                crop_sheet.save(presentation / "reviewer-comb-crops-B27.png")
        evidence["previousCandidateBundleSha256"] = json.loads((previous_candidate / "manifest.json").read_text(encoding="utf-8-sig"))["bundleSha256"]
    for name in candidate["representativeFiles"][:11]:
        a, b = (directory / f"battlefield_{name}.png" for directory in (baseline, output))
        evidence["unchangedGatherAndB0"].append({"name": name, "baselineSha256": digest(a), "candidateSha256": digest(b), "byteIdentical": a.read_bytes() == b.read_bytes()})
    assert all(item["byteIdentical"] for item in evidence["unchangedGatherAndB0"]), "Gather/B0 differs from original native capture"
    shake = output / "camera-approximation_sequence"
    if shake.exists():
        labelled = presentation / "camera-approximation-labelled"
        labelled.mkdir()
        label = "camera approximation; not native game EarthQuake shader"
        for path in sorted(shake.glob("frame_*.png")):
            with Image.open(path) as source:
                frame = source.convert("RGB")
            draw = ImageDraw.Draw(frame)
            draw.rectangle((0, 0, 1280, 76), fill="#14191e")
            draw.text((16, 8), label, font=font, fill="#f5c57b")
            draw.text((16, 40), ".25 scaled seconds | native preview-camera offset only | bounded 20px X / 10px Y", font=font, fill="#e0eaf2")
            frame.save(labelled / path.name)
        ffmpeg, codec, quality = select_encoder()
        video = presentation / "camera-approximation_with_current_SFX.mp4"
        audio = Path(__file__).resolve().parent / "source_audio/preview_mix.wav"
        subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-framerate", "30", "-i", str(labelled / "frame_%04d.png"),
                        "-i", str(audio), "-map", "0:v:0", "-map", "1:a:0", "-c:v", codec, *quality, "-pix_fmt", "yuv420p",
                        "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", str(video)], check=True)
        evidence["cameraApproximation"] = {"label": label, "parameters": (output / "camera-approximation.txt").read_text(),
                                           "offsetsSha256": digest(output / "camera-approximation-offsets.csv"), "sourceAudioSha256": digest(audio),
                                           "audioNormalization": False, "media": verify_media(ffmpeg, video)}
    evidence["exporterSha256"] = digest(Path(__file__))
    (presentation / "comparison-manifest.json").write_text(json.dumps(evidence, indent=2), encoding="utf-8")
    print(f"PASS fixed A/B and separately labelled native-camera illustration: {presentation}")


def main():
    global OUTPUT
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=OUTPUT)
    parser.add_argument("--comparison-baseline", type=Path)
    parser.add_argument("--previous-candidate", type=Path)
    args = parser.parse_args()
    OUTPUT = args.output.resolve()
    if args.comparison_baseline:
        export_burst_comparison(OUTPUT, args.comparison_baseline.resolve(), args.previous_candidate.resolve() if args.previous_candidate else None)
        return
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
