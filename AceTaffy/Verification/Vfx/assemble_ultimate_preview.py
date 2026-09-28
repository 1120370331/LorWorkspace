"""Package one real-Unity-frame run into normal-speed review media."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys

from PIL import Image, ImageDraw
import imageio_ffmpeg

HERE = Path(__file__).resolve().parent
ACE = HERE.parents[1]
run_id = sys.argv[1]
OUT = HERE / "ultimate-v1" / run_id
assert OUT.is_dir() and run_id != "baseline", OUT
marks = (0, 17, 50, 100, 220, 400, 600, 800)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def labeled(image: Image.Image, title: str, width=480, height=270):
    tile = image.convert("RGB").resize((width, height), Image.Resampling.LANCZOS)
    draw = ImageDraw.Draw(tile)
    draw.rectangle((0, 0, width, 24), fill=(28, 30, 33))
    draw.text((8, 6), title, fill=(245, 243, 237))
    return tile


contact = Image.new("RGB", (480 * len(marks), 270 * 2), (58, 61, 64))
for row, facing in enumerate(("left", "right")):
    for col, ms in enumerate(marks):
        frame = Image.open(OUT / f"{facing}_{ms:03d}.png")
        tile = labeled(frame, f"UNITY FACTORY SIM | {facing.upper()} | +{ms}ms | roll .5")
        contact.paste(tile, (col * 480, row * 270))
contact.save(OUT / "timeline-contact.png")

comparison = Image.new("RGB", (480 * 3, 270), (58, 61, 64))
for col, (name, label) in enumerate((
    ("ordinary_same_context.png", "Ordinary Blunt | real factory"),
    ("old_mother_asset_only.png", "Old mother only | same native pivot"),
    ("left_100.png", "New ultimate | real factory + arc | 100ms"),
)):
    comparison.paste(labeled(Image.open(OUT / name), label), (col * 480, 0))
comparison.save(OUT / "same-camera-comparison.png")

source_dir = ACE / "AceTaffyMod/Resource/Effects/AceTaffy"
asset_sheet = Image.new("RGB", (800, 440), (57, 60, 63))
for row, background in enumerate(((45, 48, 52), (112, 116, 118))):
    for col, name in enumerate(("SteamDetonation", "SteamPressureArc")):
        image = Image.open(source_dir / f"{name}.png").convert("RGBA")
        image.thumbnail((370, 190), Image.Resampling.LANCZOS)
        tile = Image.new("RGBA", (400, 220), (*background, 255))
        tile.alpha_composite(image, ((400 - image.width) // 2, (220 - image.height) // 2))
        d = ImageDraw.Draw(tile)
        d.text((8, 6), f"{name} | {'dark' if row == 0 else 'mid'} grey", fill="white")
        asset_sheet.paste(tile.convert("RGB"), (col * 400, row * 220))
asset_sheet.save(OUT / "asset-alpha-review.png")

frames = sorted((OUT / "frames").glob("*.png"))
assert len(frames) == 67, len(frames)
title = "Unity native factory isolated simulation | production queue posture: Guard .30s -> Hit event -> recovery .40s | MoveRoutine not replayed"


def annotated(index):
    image = Image.open(frames[index]).convert("RGB")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, image.width, 36), fill=(26, 28, 31))
    draw.text((9, 5), title, fill="white")
    draw.text((9, 20), f"Nominal 1x / 60fps | t={(index - 18) / 60:+.3f}s relative to attack effect event", fill=(226, 224, 218))
    return image


video = OUT / "ultimate-normal-60fps.mp4"
cmd = [imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-f", "rawvideo", "-pixel_format", "rgb24",
       "-video_size", "960x540", "-framerate", "60", "-i", "-", "-an",
       "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", str(video)]
with subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE) as ffmpeg:
    for index in range(len(frames)):
        ffmpeg.stdin.write(annotated(index).tobytes())
    ffmpeg.stdin.close()
    stderr = ffmpeg.stderr.read()
    if ffmpeg.wait() != 0:
        raise RuntimeError(stderr.decode(errors="replace")[-2000:])

gif_frames = [annotated(i).resize((640, 360), Image.Resampling.LANCZOS)
              for i in range(0, len(frames), 2)]
gif_durations = [30, 40, 30] * 11 + [20]
assert len(gif_frames) == len(gif_durations) == 34
gif_frames[0].save(OUT / "ultimate-normal-30fps.gif", save_all=True,
                   append_images=gif_frames[1:], duration=gif_durations, loop=0, optimize=False)
with Image.open(OUT / "ultimate-normal-30fps.gif") as encoded:
    encoded_durations = []
    for i in range(encoded.n_frames):
        encoded.seek(i)
        encoded_durations.append(encoded.info["duration"])
assert len(encoded_durations) == 34 and 30 in encoded_durations and 40 in encoded_durations
assert abs(sum(encoded_durations) - len(frames) * 1000 / 60) <= 10, encoded_durations

actor = Image.open(ACE / "AceTaffyMod/Resource/CharacterSkin/AceTaffy/ClothCustom/Hit.png").convert("RGBA")
def bbox8(image):
    return image.getchannel("A").point(lambda a: 255 if a >= 8 else 0).getbbox()
actor_bbox = bbox8(actor)
actor_h = (actor_bbox[3] - actor_bbox[1]) / 50
main = Image.open(source_dir / "SteamDetonation.png").convert("RGBA")
rotated = main.rotate(12, expand=True, resample=Image.Resampling.BICUBIC)
main_box = bbox8(rotated)
arc_box = bbox8(Image.open(source_dir / "SteamPressureArc.png").convert("RGBA"))
main_w_h = (main_box[2] - main_box[0]) * 3 * 1.06 / 600 / actor_h
main_h_h = (main_box[3] - main_box[1]) * 3 * 1.06 / 600 / actor_h
arc_h_h = (arc_box[3] - arc_box[1]) * 3 * (1 + .25 * (.05 / .17)) / 1200 / actor_h

metrics = json.loads((OUT / "metrics.json").read_text(encoding="utf-8"))
records = metrics["faceCoverage"]
face_summary = {}
for facing in ("left", "right"):
    for roll in (0.0, .5, 1.0):
        subset = sorted((r for r in records if r["facing"] == facing and r["rollFactor"] == roll),
                        key=lambda r: r["milliseconds"])
        assert subset, (facing, roll)
        max_owner = max(r["ownerFace"] for r in subset)
        max_target = max(r["targetFace"] for r in subset)
        current = longest = 0
        for record in subset:
            current = current + 33 if record["targetFace"] >= .25 else 0
            longest = max(longest, current)
        face_summary[f"{facing}_roll_{roll}"] = {
            "samples": len(subset), "max_owner_face_alpha_0_6": max_owner,
            "max_target_face_alpha_0_6": max_target,
            "target_coverage_ge_25_percent_longest_sampled_ms": longest,
        }

anchors = metrics["emitterAnchors"]
anchor_summary = {}
for facing in ("left", "right"):
    at100 = [r for r in anchors if r["facing"] == facing and r["milliseconds"] == 100]
    assert len(at100) == 3, at100
    across_roll = max(r["screenX"] for r in at100) - min(r["screenX"] for r in at100)
    assert across_roll < 1.0, (facing, across_roll)
    by_roll = {}
    for roll in (0.0, .5, 1.0):
        rows = [r for r in anchors if r["facing"] == facing and r["rollFactor"] == roll]
        origin = next(r for r in rows if r["milliseconds"] == 0)
        drift = max(abs(r["screenX"] - origin["screenX"]) for r in rows)
        assert drift <= 15.0, (facing, roll, drift)
        by_roll[str(roll)] = {"event_x": origin["screenX"],
                             "100ms_x": next(r["screenX"] for r in rows if r["milliseconds"] == 100),
                             "max_0_to_220ms_screen_drift_px": drift}
    anchor_summary[facing] = {"100ms_roll_spread_px": across_roll, "rolls": by_roll}

report = {
    "evidence": "Unity native factory isolated simulation; prefab Hit pivot data reconstructed, actual current actor PNG/ModInfo; MoveRoutine not replayed",
    "actor_visible_height_world": actor_h,
    "main_width_actor_heights_at_100ms_roll_0_5": main_w_h,
    "main_height_actor_heights_at_100ms_roll_0_5": main_h_h,
    "arc_height_actor_heights_at_100ms_roll_0_5": arc_h_h,
    "face": face_summary,
    "emitter_anchor": anchor_summary,
    "video_duration_ms": len(frames) * 1000 / 60,
    "gif_encoded_duration_ms": sum(encoded_durations),
    "gif_duration_quantization": "GIF stores centiseconds; final sampled frame is 20ms after encoding",
    "media_sha256": {p.name: digest(p) for p in (video, OUT / "ultimate-normal-30fps.gif",
                                            OUT / "timeline-contact.png", OUT / "same-camera-comparison.png",
                                            OUT / "asset-alpha-review.png")},
}
(OUT / "review-metrics.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2))
