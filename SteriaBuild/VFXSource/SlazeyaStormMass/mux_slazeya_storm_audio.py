"""Verify the frozen source mix and mux skill SFX onto the candidate video without re-encoding it."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path(__file__).resolve().parent / "source_audio"
VISUAL = ROOT / "preview_exports/slazeya_storm_mass/round5"
OUTPUT = VISUAL / "audio"
ACCEPTED_BUNDLE = None
ACCEPTED_DRIVER = None


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def run(command):
    return subprocess.run(command, capture_output=True, check=True)


def ffmpeg_path():
    candidates = []
    try:
        import imageio_ffmpeg
        candidates.append(imageio_ffmpeg.get_ffmpeg_exe())
    except (ImportError, RuntimeError):
        pass
    candidates.append(shutil.which("ffmpeg"))
    for candidate in dict.fromkeys(x for x in candidates if x):
        encoders = run([candidate, "-hide_banner", "-encoders"]).stdout.decode(errors="replace")
        if any(len(parts := line.split()) > 1 and parts[1] == "aac" for line in encoders.splitlines()):
            return candidate
    raise RuntimeError("No available FFmpeg AAC encoder; no visual fallback/rebuild is permitted")


def read_pcm(path, expected_frames):
    with wave.open(str(path), "rb") as stream:
        assert stream.getnchannels() == 2 and stream.getsampwidth() == 2 and stream.getframerate() == 44100
        assert stream.getcomptype() == "NONE" and stream.getnframes() == expected_frames
        raw = stream.readframes(expected_frames)
    pcm = np.frombuffer(raw, dtype="<i2").reshape(-1, 2)
    return pcm, raw


def encoded_stream_hash(ffmpeg, path, stream):
    result = run([ffmpeg, "-v", "error", "-i", str(path), "-map", stream,
                  "-c", "copy", "-f", "streamhash", "-hash", "sha256", "pipe:1"])
    return result.stdout.decode().strip().split("=")[-1].upper()


def main():
    global VISUAL, OUTPUT, ACCEPTED_BUNDLE, ACCEPTED_DRIVER
    parser = argparse.ArgumentParser()
    parser.add_argument('--candidate-dir', type=Path, required=True)
    parser.add_argument('--expected-bundle', required=True)
    parser.add_argument('--expected-driver', required=True)
    args = parser.parse_args()
    VISUAL = args.candidate_dir.resolve()
    OUTPUT = VISUAL / 'audio'
    ACCEPTED_BUNDLE = args.expected_bundle.upper()
    ACCEPTED_DRIVER = args.expected_driver.upper()
    assert len(ACCEPTED_BUNDLE) == 64 and len(ACCEPTED_DRIVER) == 64
    OUTPUT.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((SOURCE / "manifest.json").read_text(encoding="utf-8-sig"))
    visual = json.loads((VISUAL / "manifest.json").read_text(encoding="utf-8-sig"))
    media = json.loads((VISUAL / "media-manifest.json").read_text(encoding="utf-8-sig"))
    video = VISUAL / "battlefield_motion.mp4"
    video_file_hash = digest(video)
    accepted_video = next(item for item in media["media"] if Path(item["path"]).name == video.name)
    assert video_file_hash == accepted_video["sha256"]
    assert visual["bundleSha256"] == ACCEPTED_BUNDLE == digest(visual["bundle"])
    assert digest(ROOT / "SteriaBuild/SlazeyaStormVisualController.cs") == ACCEPTED_DRIVER
    contract, preview = manifest["contract"], manifest["preview"]
    assert manifest["version"] == "2026-09-07.storm-dawn.audio.1"
    assert contract["gather_duration_s"] == 1.35 and contract["gather_max_gain"] == 0.55
    assert contract["gather_ramp"] == ".32*smoothstep(t/.03)+.23*smoothstep(t/1.35)"
    assert contract["burst_gain"] == 0.78 and contract["gather_fade_after_b_s"] == 0.04
    assert contract["option_gain_in_preview"] == 1 and not preview["normalization_after_mix"]
    assert round(visual["callbackTime"] * 44100) == preview["burst_start_sample"] == 68355
    source_hashes = {name: digest(SOURCE / name) for name in ("gather_loop.wav", "burst_tail.wav", "preview_mix.wav")}
    for name, value in source_hashes.items():
        assert value == manifest["files"][name]["sha256"], f"Unfrozen source WAV: {name}"
    gather, _ = read_pcm(SOURCE / "gather_loop.wav", 44100)
    burst, _ = read_pcm(SOURCE / "burst_tail.wav", 52920)
    mix, mix_raw = read_pcm(SOURCE / "preview_mix.wav", 133770)
    # Independently reconstruct only for verification; the asset worker's WAV remains the output.
    index = np.arange(len(mix), dtype=np.float64)
    ramp = np.clip(index / (44100 * 1.35), 0, 1)
    early_ramp = np.clip(index / (44100 * 0.03), 0, 1)
    envelope = 0.32 * early_ramp * early_ramp * (3 - 2 * early_ramp) + 0.23 * ramp * ramp * (3 - 2 * ramp)
    envelope *= np.clip(1 - np.maximum(0, index - 68355) / (44100 * 0.04), 0, 1)
    expected = gather[np.arange(len(mix)) % 44100].astype(np.float64) * envelope[:, None]
    expected[68355:68355 + len(burst)] += burst.astype(np.float64) * 0.78
    max_lsb_error = int(np.max(np.abs(np.rint(expected).astype(np.int32) - mix.astype(np.int32))))
    assert max_lsb_error <= 1, f"Preview gain/loop/callback mismatch: {max_lsb_error} LSB"
    assert np.max(np.abs(mix.astype(np.int32))) < 32767
    copied_mix = OUTPUT / "preview_mix.wav"
    shutil.copyfile(SOURCE / "preview_mix.wav", copied_mix)
    assert digest(copied_mix) == source_hashes["preview_mix.wav"]
    ffmpeg = ffmpeg_path()
    output_video = OUTPUT / "battlefield_with_audio.mp4"
    run([ffmpeg, "-hide_banner", "-v", "error", "-y", "-i", str(video), "-i", str(copied_mix),
         "-map", "0:v:0", "-map", "1:a:0", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k",
         "-ar", "44100", "-ac", "2", "-shortest", "-movflags", "+faststart", str(output_video)])
    before_stream = encoded_stream_hash(ffmpeg, video, "0:v:0")
    after_stream = encoded_stream_hash(ffmpeg, output_video, "0:v:0")
    assert before_stream == after_stream, "Candidate encoded video stream changed"
    assert digest(video) == video_file_hash, "Original candidate MP4 was modified"
    decoded = run([ffmpeg, "-v", "error", "-xerror", "-i", str(output_video), "-map", "0:v:0",
                   "-progress", "pipe:1", "-f", "null", "-"]).stdout.decode()
    frames = [int(line.split("=")[1]) for line in decoded.splitlines() if line.startswith("frame=")]
    assert frames[-1] == 91
    decoded_audio = run([ffmpeg, "-v", "error", "-xerror", "-i", str(output_video), "-map", "0:a:0",
                         "-c:a", "pcm_s16le", "-f", "s16le", "pipe:1"]).stdout
    assert len(decoded_audio) % 4 == 0 and len(decoded_audio) // 4 >= len(mix)
    decoded_pcm = np.frombuffer(decoded_audio, dtype="<i2").reshape(-1, 2)
    assert np.max(np.abs(decoded_pcm.astype(np.int32))) < 32767
    probe = shutil.which("ffprobe")
    streams = None
    if probe:
        streams = json.loads(run([probe, "-v", "error", "-show_streams", "-of", "json", str(output_video)]).stdout)["streams"]
        v = next(s for s in streams if s["codec_type"] == "video")
        a = next(s for s in streams if s["codec_type"] == "audio")
        assert v["width"] == 1280 and v["height"] == 720 and int(v["nb_frames"]) == 91
        assert abs(float(v["start_time"])) < 1 / 44100 and abs(float(a["start_time"])) < 1 / 44100
        assert abs(float(v["duration"]) - 91 / 30) < 0.0001 and a["sample_rate"] == "44100" and a["channels"] == 2
        keep = {"codec_name", "codec_type", "sample_rate", "channels", "start_time", "duration", "nb_frames", "width", "height", "r_frame_rate"}
        streams = [{k: value for k, value in s.items() if k in keep} for s in streams]
    evidence = {
        "status": "MUX_AND_NUMERIC_VERIFICATION_PASS", "listening": "NOT_PERFORMED; no audio-perception tool",
        "ffmpeg": ffmpeg, "sourceWavSha256": source_hashes, "mixGainAndTimingMaxErrorPcm16Lsb": max_lsb_error,
        "callbackSample": 68355, "callbackSeconds": 1.55, "gatherGain": 0.55, "burstGain": 0.78,
        "originalVideoFileSha256": video_file_hash, "encodedVideoStreamSha256Before": before_stream,
        "encodedVideoStreamSha256After": after_stream, "videoCopyVerified": True, "decodedVideoFrames": frames[-1],
        "previewPcmSha256": hashlib.sha256(mix_raw).hexdigest().upper(),
        "encodedAacStreamSha256": encoded_stream_hash(ffmpeg, output_video, "0:a:0"),
        "decodedAacPcmSha256": hashlib.sha256(decoded_audio).hexdigest().upper(),
        "decodedAudioFrames": len(decoded_audio) // 4,
        "audioEncoding": "AAC 192kbps is lossy; source PCM and encoded/decoded audio hashes are deliberately distinct",
        "streams": streams, "output": str(output_video), "outputSha256": digest(output_video),
        "candidateVisualBundleSha256": ACCEPTED_BUNDLE, "candidateVisualControllerSha256": ACCEPTED_DRIVER,
    }
    (OUTPUT / "av-verification.json").write_text(json.dumps(evidence, indent=2), encoding="utf-8")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    main()
