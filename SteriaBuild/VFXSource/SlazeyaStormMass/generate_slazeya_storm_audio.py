#!/usr/bin/env python3
"""Original Slazeya skill SFX. No samples, network, voices, music or Unity writes.

Python 3.12 + numpy 2.2.6 + scipy 1.16.1 + Pillow 11.3.0; imageio_ffmpeg
provides the independent decoder. Run to regenerate only source_audio/.
--verify decodes existing WAVs and checks their manifest; --verify-repro also
resynthesizes in memory and compares all three complete RIFF byte streams.
The waveform board is numeric evidence, never a claim of listening approval.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import platform
import subprocess
import wave

import numpy as np
import scipy
from scipy import signal
from PIL import Image, ImageDraw, ImageFont, __version__ as PIL_VERSION

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "source_audio"
SR = 44100
SEED = 2026090507
TAU = 2 * np.pi
TARGET_DBFS = -3.5
GATHER = 1.35
BURST = 1.55
GATHER_GAIN = .30
BURST_GAIN = .78
GATHER_FADE = .04
# Accepted video has 91 frames at 30 Hz, exactly 133770 audio frames.
PREVIEW_SAMPLES = 91 * SR // 30
SPECS = {"gather_loop.wav": SR, "burst_tail.wav": 52920,
         "preview_mix.wav": PREVIEW_SAMPLES}


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def rms(x):
    return float(np.sqrt(np.mean(np.square(x))))


def db(x):
    return float(20 * np.log10(max(float(x), 1e-15)))


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def rng_for(offset):
    return np.random.Generator(np.random.PCG64(SEED + offset))


def noise(n, seed_offset, lo, hi, slope=0.):
    """Periodic random-phase FFT noise; smooth bands, no single oscillator.

    Integer FFT bins close phase at N, not at N-1. Each component has its own
    PCG64 stream. Transient callers subsequently apply causal envelopes.
    """
    rng = rng_for(seed_offset)
    f = np.fft.rfftfreq(n, 1 / SR)
    safe = np.maximum(f, 1.)
    band = (1 / np.sqrt(1 + (lo / safe) ** 10)
            / np.sqrt(1 + (safe / hi) ** 12)
            * (safe / max(lo, 40.)) ** (slope / 2))
    spectrum = (rng.normal(size=len(f)) + 1j * rng.normal(size=len(f))) * band
    spectrum[0] = spectrum[-1] = 0
    x = np.fft.irfft(spectrum, n=n)
    return x / max(rms(x), 1e-12)


def stereo_noise(n, offset, lo, hi, slope=0., width=.25):
    mid = noise(n, offset, lo, hi, slope)
    side = noise(n, offset + 1, max(lo, 180), hi, slope)
    return np.column_stack((mid + width * side, mid - width * side)) / math.sqrt(1 + width**2)


def modulation(n, offset, lo=3, hi=19):
    x = noise(n, offset, lo, hi)
    return .68 + .32 * np.tanh(.8 * x)


def event_envelope(n, start, attack, decay, end):
    t = np.arange(n) / SR
    age = np.maximum(t - start, 0)
    return (smooth(age / attack) * np.exp(-age / decay)
            * (1 - smooth((t - (end - .009)) / .009)) * (t >= start))


def droplets(n, offset, count, start, stop, periodic=False):
    """Damped, rising bubble resonances with irregular noisy wet contacts.

    Low Q, independent frequencies/times, and inharmonic secondary modes keep
    these from becoming sustained musical pings. Circular overlap-add closes
    gather's droplets exactly across its one-second period.
    """
    rng = rng_for(offset)
    result = np.zeros((n, 2))
    events = []
    for i in range(count):
        onset = float(rng.uniform(start, stop))
        f0 = float(np.exp(rng.uniform(np.log(320 if periodic else 550), np.log(1950 if periodic else 4200))))
        tau = float(rng.uniform(.0035, .013) * (800 / f0) ** .25)
        length = min(int(SR * tau * 7), int(.13 * SR))
        t = np.arange(length) / SR
        rise = float(rng.uniform(.08, .38))
        phase = TAU * f0 * (t + rise * (t - tau * (1 - np.exp(-t / tau))))
        ratio = float(rng.uniform(1.67, 2.43))
        modes = np.sin(phase) + .19 * np.sin(ratio * phase + .4) * np.exp(-t / (tau * .48))
        contact = rng.normal(size=length)
        contact = signal.lfilter([.22, .33, .27, .18], [1.], contact)
        x = (modes + .33 * contact * np.exp(-t / .0018)) * np.exp(-t / tau)
        x *= smooth(t / .0005) * (1 - smooth((t - max(t[-1] - .004, 0)) / .004))
        # Remove each grain's tiny area without changing its zero endpoints.
        w = np.sin(np.linspace(0, np.pi, length)) ** 2
        x -= np.sum(x) / np.sum(w) * w
        amplitude = float(rng.uniform(.35, 1.))
        if not periodic:
            amplitude *= .85 * np.exp(-max(onset - .15, 0) / .37) + .07
        pan = float(rng.uniform(-.67, .67))
        gains = np.array([math.sqrt((1 - pan) / 2), math.sqrt((1 + pan) / 2)])
        idx = int(round(onset * SR)) + np.arange(length)
        if periodic:
            np.add.at(result, idx % n, x[:, None] * amplitude * gains)
        else:
            valid = idx < n
            result[idx[valid]] += x[valid, None] * amplitude * gains
        events.append({"sample": int(round(onset * SR)), "time_s": round(onset, 6),
                       "f0_hz": round(f0, 3), "decay_s": round(tau, 6),
                       "amplitude": round(amplitude, 6), "pan": round(pan, 5)})
    return result, events


def close_loop(x):
    """Choose a quiet stereo phase; C2 local correction closes value and slope.

    The whole buffer is already periodic. This small seam-centered correction
    additionally permits a zero-valued isolated source start/end without a
    whole-loop fade or a repeated gap. DC correction preserves the endpoints.
    """
    dx = x - np.roll(x, 1, axis=0)
    score = np.sum(x*x + 8 * dx*dx, axis=1)
    index = int(np.argmin(score))
    x = np.roll(x, -index, axis=0).copy()
    before = x.copy()
    n = len(x)
    s = (np.arange(n) + .5) / SR
    s[s > n / SR / 2] -= n / SR
    radius = .008
    basis = np.maximum(1 - (s / radius)**2, 0)**3
    v = .5 * (x[0] + x[-1]) / basis[0]
    d = (x[0] - x[-1]) * SR / basis[0]
    x -= basis[:, None] * (v + s[:, None] * d)
    x[[0, -1]] = 0
    w = np.sin(np.linspace(0, np.pi, n))**2
    x -= (x.sum(axis=0) / w.sum())[None, :] * w[:, None]
    x[[0, -1]] = 0
    return x, {"phase_rotation_samples": index, "periodic_fft_samples": n,
               "seam_correction_radius_s": radius,
               "correction_rms_relative_db": db(rms(x - before) / rms(before)),
               "method": "Periodic FFT and circular grains; quiet phase rotation; local C2 value/slope correction; zero-DC projection."}


def finish_transient(x):
    x = signal.sosfilt(signal.butter(2, 28, "highpass", fs=SR, output="sos"), x, axis=0)
    t = np.arange(len(x)) / SR
    gate = smooth(t / .0005) * (1 - smooth((t - 1.17) / (.03 - 1 / SR)))
    x *= gate[:, None]
    x -= (x.sum(axis=0) / gate.sum())[None, :] * gate[:, None]
    x[[0, -1]] = 0
    return x


def to_pcm(x, normalize=False):
    if normalize:
        x = x * (10 ** (TARGET_DBFS / 20) / np.max(np.abs(x)))
    if not np.all(np.isfinite(x)) or np.max(np.abs(x)) >= 1:
        raise ValueError("Nonfinite or clipping float candidate")
    # Nearest PCM16 quantization; no random floor added to the loop or silence.
    return np.rint(x * 32768).astype("<i2")


def encoded(pcm):
    buf = io.BytesIO()
    with wave.open(buf, "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    return buf.getvalue()


def synthesize():
    metadata = {}
    n = SR
    t = np.arange(n) / SR
    low = stereo_noise(n, 101, 38, 190, -.9, .055)
    low *= (1 + .085*np.sin(TAU*t + .4) + .065*np.sin(TAU*3*t + 1.7))[:, None]
    water = stereo_noise(n, 201, 230, 1850, -.65, .28) * modulation(n, 203, 3, 12)[:, None]
    air = stereo_noise(n, 301, 2100, 6900, -.5, .40) * modulation(n, 303, 6, 25)[:, None]
    bubbles, gather_events = droplets(n, 401, 49, 0, 1, periodic=True)
    gather, loop_info = close_loop(.49*low + .28*water + .060*air + .41*bubbles)
    metadata["gather_loop.wav"] = {
        "seed_offsets": {"pressure_mid_side": [101, 102], "water_mid_side_mod": [201, 202, 203],
                         "air_mid_side_mod": [301, 302, 303], "bubble_grains": 401},
        "layers": [
            {"name": "pressure", "start_s": 0, "end_s": 1, "band_hz": [38, 190], "weight": .49},
            {"name": "water_friction", "start_s": 0, "end_s": 1, "band_hz": [230, 1850], "weight": .28},
            {"name": "air_shear", "start_s": 0, "end_s": 1, "band_hz": [2100, 6900], "weight": .06},
            {"name": "short_bubble_resonances", "start_s": 0, "end_s": 1, "count": 49, "weight": .41}],
        "droplet_events_before_phase_rotation": gather_events, "loop_construction": loop_info}

    n = SPECS["burst_tail.wav"]
    t = np.arange(n) / SR
    thunder = stereo_noise(n, 501, 34, 210, -.8, .045) * event_envelope(n, 0, .0015, .145, .70)[:, None]
    # Front cracks are short, independent streams; the second flash has .34 of
    # the first crack's pre-master weight and no repeated low-frequency slam.
    crack = np.zeros((n, 2))
    for j, (start, weight, decay, end) in enumerate(((0, 1., .010, .046), (.012, .29, .004, .034), (.028, .16, .003, .046))):
        crack += weight * stereo_noise(n, 511 + j*3, 1500, 11200, -.25, .17) * event_envelope(n, start, .00035, decay, end)[:, None]
    second = stereo_noise(n, 531, 2050, 10500, -.1, .22) * event_envelope(n, .10, .0004, .008, .146)[:, None]
    slap_env = (event_envelope(n, 0, .001, .078, .43)
                + .36*event_envelope(n, .024, .0025, .065, .41)
                + .20*event_envelope(n, .057, .004, .048, .35))
    slap = stereo_noise(n, 541, 115, 1900, -.4, .23) * slap_env[:, None]
    spray = stereo_noise(n, 551, 850, 8000, -.35, .39)
    spray *= (event_envelope(n, .009, .007, .18, .87) * modulation(n, 553, 8, 37))[:, None]
    wash = stereo_noise(n, 561, 260, 2550, -.8, .31)
    wash_env = (event_envelope(n, .075, .026, .27, 1.20)
                + .19*event_envelope(n, .32, .025, .12, .86))
    wash *= (wash_env * modulation(n, 563, 5, 23))[:, None]
    mist = stereo_noise(n, 571, 1600, 5600, -.5, .40) * event_envelope(n, .18, .10, .27, 1.20)[:, None]
    drops, burst_events = droplets(n, 601, 112, .017, 1.115)
    burst = finish_transient(.93*thunder + .48*crack + .48*.34*second + .86*slap
                             + .31*spray + .24*wash + .065*mist + .69*drops)
    metadata["burst_tail.wav"] = {
        "seed_offsets": {"thunder_mid_side": [501, 502], "primary_cracks": [511, 512, 514, 515, 517, 518],
                         "secondary_crack": [531, 532], "wave_slap": [541, 542],
                         "spray": [551, 552, 553], "wash": [561, 562, 563], "mist": [571, 572], "droplets": 601},
        "layers": [
            {"name": "low_thunder", "start_s": 0, "end_s": .70, "band_hz": [34, 210], "weight": .93},
            {"name": "primary_lightning_cracks", "start_s": 0, "end_s": .046, "subcracks_s": [0, .012, .028], "band_hz": [1500, 11200], "weight": .48},
            {"name": "wave_slap", "start_s": 0, "end_s": .43, "sheets_s": [0, .024, .057], "band_hz": [115, 1900], "weight": .86},
            {"name": "secondary_lightning", "start_s": .10, "end_s": .146, "band_hz": [2050, 10500], "relative_primary_weight": .34},
            {"name": "spray", "start_s": .009, "end_s": .87, "band_hz": [850, 8000], "weight": .31},
            {"name": "returning_wash", "start_s": .075, "end_s": 1.2, "band_hz": [260, 2550], "weight": .24},
            {"name": "mist_air", "start_s": .18, "end_s": 1.2, "band_hz": [1600, 5600], "weight": .065},
            {"name": "damped_wet_drops", "start_s": .017, "last_onset_before_s": 1.115, "end_s": 1.2, "count": 112, "weight": .69}],
        "droplet_events": burst_events, "final_fade_s": [1.17, (n - 1) / SR],
        "secondary_primary_isolated_peak_ratio": float(np.max(np.abs(.34*second)) / np.max(np.abs(crack)))}
    pcm = {"gather_loop.wav": to_pcm(gather, True), "burst_tail.wav": to_pcm(burst, True)}
    pcm["preview_mix.wav"] = preview(pcm)
    return pcm, metadata


def preview(pcm):
    t = np.arange(PREVIEW_SAMPLES) / SR
    gain = GATHER_GAIN * smooth(t / GATHER)
    gain *= 1 - np.clip((t - BURST) / GATHER_FADE, 0, 1)
    mix = pcm["gather_loop.wav"][np.arange(len(t)) % SR].astype(float) / 32768 * gain[:, None]
    start = int(round(BURST * SR))
    tail = pcm["burst_tail.wav"].astype(float) / 32768
    mix[start:start + len(tail)] += tail * BURST_GAIN
    return to_pcm(mix)


def decode(path):
    with wave.open(str(path), "rb") as f:
        if (f.getnchannels(), f.getsampwidth(), f.getframerate(), f.getcomptype()) != (2, 2, SR, "NONE"):
            raise ValueError(f"Format mismatch: {path}")
        n = f.getnframes()
        raw = f.readframes(n)
        if n != SPECS[path.name] or len(raw) != n * 4:
            raise ValueError(f"Duration/data mismatch: {path}")
    return np.frombuffer(raw, dtype="<i2").reshape(-1, 2).copy()


def metrics(pcm, loop=False):
    x = pcm.astype(float) / 32768
    delta = np.diff(x, axis=0)
    peak = np.max(np.abs(x))
    mono = x.mean(axis=1)
    f, p = signal.welch(x, SR, nperseg=2048, axis=0)
    total = float(p.sum())
    bands = {f"{lo}-{hi}Hz": float(p[(f >= lo) & (f < hi)].sum() / total)
             for lo, hi in ((20, 250), (250, 2000), (2000, 12000))}
    low = signal.sosfilt(signal.butter(4, 200, "lowpass", fs=SR, output="sos"), x, axis=0)
    result = {
        "frames": len(pcm), "duration_s": len(pcm) / SR,
        "sample_peak_dbfs": db(peak), "sample_peak_linear": float(peak),
        "sample_peak_dbfs_per_channel": [db(v) for v in np.max(np.abs(x), axis=0)],
        "true_peak_8x_dbfs": db(np.max(np.abs(signal.resample_poly(x, 8, 1, axis=0)))),
        "rms_dbfs": db(rms(x)), "dc_per_channel": x.mean(axis=0).tolist(),
        "dc_dbfs_per_channel": [db(abs(v)) for v in x.mean(axis=0)],
        "clipped_samples": int(np.count_nonzero((pcm == -32768) | (pcm == 32767))),
        "first_pcm16": pcm[0].tolist(), "last_pcm16": pcm[-1].tolist(),
        "stereo_correlation": float(np.corrcoef(x.T)[0, 1]),
        "low_band_correlation": float(np.corrcoef(low.T)[0, 1]),
        "mono_fold_rms_loss_db": db(rms(mono) / rms(x)),
        "band_power_fractions": bands,
        "edge_first_1ms_peak_dbfs": db(np.max(np.abs(x[:44]))),
        "edge_last_1ms_peak_dbfs": db(np.max(np.abs(x[-44:]))),
    }
    if loop:
        seam_step = x[0] - x[-1]
        curvature = np.diff(np.vstack((x[-2:], x[:2])), n=2, axis=0)
        interior_p99 = np.percentile(np.abs(delta), 99.9, axis=0)
        result["loop_seam"] = {
            "step_pcm16": (pcm[0].astype(int) - pcm[-1]).tolist(),
            "step_linear": seam_step.tolist(),
            "slope_in_linear": (x[-1] - x[-2]).tolist(),
            "slope_out_linear": (x[1] - x[0]).tolist(),
            "seam_curvature_peak": np.max(np.abs(curvature), axis=0).tolist(),
            "interior_step_p99_9": interior_p99.tolist(),
            "seam_20ms_rms_ratio": rms(np.vstack((x[-441:], x[:441]))) / rms(x),
            "three_repeat_boundary_steps_pcm16": [
                (np.tile(pcm, (3, 1))[k*len(pcm)].astype(int) - np.tile(pcm, (3, 1))[k*len(pcm)-1]).tolist()
                for k in (1, 2)],
        }
    return result


def inspect_files(repro=False):
    import imageio_ffmpeg
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    results, decoded = {}, {}
    for name in SPECS:
        path = OUT / name
        pcm = decode(path)
        decoded[name] = pcm
        # Independent real decode, bit-for-bit against Python wave, not just a header probe.
        run = subprocess.run([ffmpeg, "-v", "error", "-xerror", "-i", str(path), "-map", "0:a:0",
                              "-c:a", "pcm_s16le", "-f", "s16le", "pipe:1"], capture_output=True, check=True)
        if run.stdout != pcm.tobytes():
            raise ValueError(f"Independent decoded samples differ: {name}")
        m = metrics(pcm, name == "gather_loop.wav")
        assert m["clipped_samples"] == 0 and m["sample_peak_dbfs"] <= (-3 if name != "preview_mix.wav" else -.01), name
        assert max(abs(v) for v in m["dc_per_channel"]) < 1e-4, (name, "DC")
        assert m["first_pcm16"] == [0, 0] and m["last_pcm16"] == [0, 0], (name, "source edges")
        assert m["stereo_correlation"] > .5 and m["mono_fold_rms_loss_db"] > -1.5, (name, "phase cancellation")
        assert m["low_band_correlation"] > .85, (name, "low phase")
        if name == "gather_loop.wav":
            seam = m["loop_seam"]
            assert seam["step_pcm16"] == [0, 0]
            assert np.all(np.array(seam["seam_curvature_peak"]) < np.array(seam["interior_step_p99_9"]))
            assert .35 < seam["seam_20ms_rms_ratio"] < 2.0, "Loop seam gap/spike"
        results[name] = {**m, "sha256": sha(path.read_bytes()), "bytes": path.stat().st_size,
                         "independent_ffmpeg_decode": "PASS: every decoded PCM16 sample identical"}
    assert np.array_equal(preview(decoded), decoded["preview_mix.wav"]), "Preview gain/timing/source mismatch"
    assert np.count_nonzero(decoded["preview_mix.wav"][int(round((BURST + 1.2) * SR)):]) == 0
    if repro:
        regenerated, _ = synthesize()
        for name in SPECS:
            assert encoded(regenerated[name]) == (OUT / name).read_bytes(), (name, "Reproducibility mismatch")
            results[name]["deterministic_regeneration"] = "PASS: identical full WAV bytes"
    return results, decoded, ffmpeg


def waveform_board(decoded, results):
    """Draw decoded min/max bins, true stereo seam samples and RMS envelopes."""
    img = Image.new("RGB", (1600, 1290), "#0d1721")
    d = ImageDraw.Draw(img)
    font_path = Path("C:/Windows/Fonts/consola.ttf")
    font = ImageFont.truetype(str(font_path), 18) if font_path.exists() else ImageFont.load_default()
    small = ImageFont.truetype(str(font_path), 14) if font_path.exists() else ImageFont.load_default()
    d.text((28, 16), "SLAZEYA / ORIGINAL STORM SFX   |   decoded PCM16 / 44100 Hz / stereo", fill="#eaf4ff", font=font)
    d.text((28, 46), "Numeric diagnostics only. Listening approval is not implied. Cyan=L / amber=R", fill="#aabccc", font=small)

    def plot(x, y, title, seconds, ylim=.75, markers=()):
        d.text((28, y), title, fill="#eaf4ff", font=font)
        left, top, width, height = 95, y + 34, 1470, 166
        for value in (-ylim, 0, ylim):
            yy = top + height/2 - value/ylim*height/2
            d.line((left, yy, left+width, yy), fill="#263645")
            d.text((30, yy-8), f"{value:+.3f}", fill="#8c9fb0", font=small)
        for c, color in enumerate(("#58dbed", "#efb360")):
            for i in range(width):
                a, b = int(i*len(x)/width), max(int((i+1)*len(x)/width), int(i*len(x)/width)+1)
                vals = x[a:min(b, len(x)), c]
                if len(vals):
                    lo, hi = float(vals.min()), float(vals.max())
                    d.line((left+i, top+height/2-hi/ylim*height/2, left+i, top+height/2-lo/ylim*height/2), fill=color)
        for j in range(7):
            xpos = left+width*j/6
            d.text((xpos-15, top+height+6), f"{seconds*j/6:.3f}", fill="#8c9fb0", font=small)
        for when, label in markers:
            xx = left + width * when/seconds
            d.line((xx, top, xx, top+height), fill="#e57590", width=1)
            d.text((min(xx+4, left+width-170), top+3), label, fill="#ff9cb4", font=small)

    for index, name in enumerate(SPECS):
        x = decoded[name].astype(float)/32768
        m = results[name]
        markers = ((.1, "B+.10"), (1.17, "fade")) if name == "burst_tail.wav" else ()
        if name == "preview_mix.wav":
            markers = ((1., "loop"), (GATHER, "G"), (BURST, "B"), (BURST+GATHER_FADE, "+40ms"), (2.75, "silent"))
        plot(x, 90+index*248, f"{name}   peak {m['sample_peak_dbfs']:.2f} dBFS | RMS {m['rms_dbfs']:.2f} | corr {m['stereo_correlation']:.3f}", len(x)/SR, markers=markers)
    x = decoded["gather_loop.wav"].astype(float)/32768
    seam = np.vstack((x[-int(.004*SR):], x[:int(.004*SR)]))
    plot(seam, 845, "Loop wrap close-up: last 4 ms -> first 4 ms | no duplicated end frame", len(seam)/SR, ylim=max(.04, float(np.max(np.abs(seam)))*1.1), markers=((int(.004*SR)/SR, "wrap"),))
    d.text((28, 1090), "Phase + loop gate: exact zero endpoint samples; seam slope/curvature below normal noise steps; mono loss <1.5 dB.", font=small, fill="#c3dfcf")
    d.text((28, 1120), "Preview: smoothstep gather 0 -> .30 over 1.35 s, hold until B=1.55 s, linear 40 ms fade; burst .78 once.", font=small, fill="#c3dfcf")
    for i, name in enumerate(SPECS):
        m = results[name]
        d.text((28, 1160+i*31), f"{name:18s} DC={max(abs(v) for v in m['dc_per_channel']):.2e} | mono={m['mono_fold_rms_loss_db']:.3f} dB | true peak 8x={m['true_peak_8x_dbfs']:.2f} dBFS", font=small, fill="#aabccc")
    img.save(OUT / "waveform_diagnostics.png")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true")
    parser.add_argument("--verify-repro", action="store_true")
    args = parser.parse_args()
    if args.verify or args.verify_repro:
        results, _, _ = inspect_files(args.verify_repro)
        manifest = json.loads((OUT / "manifest.json").read_text(encoding="utf-8"))
        assert manifest["generator_sha256"] == sha(Path(__file__).read_bytes()), "Generator changed after freeze"
        for name in SPECS:
            assert manifest["files"][name]["sha256"] == results[name]["sha256"], name
        print(json.dumps({"status": "PASS", "repro": args.verify_repro, "files": results}, indent=2))
        return
    OUT.mkdir(parents=True, exist_ok=True)
    pcm, design = synthesize()
    for name, data in pcm.items():
        (OUT / name).write_bytes(encoded(data))
    results, decoded, ffmpeg = inspect_files(repro=True)
    waveform_board(decoded, results)
    manifest = {
        "version": "2026-09-05.slazeya-sfx.1", "status": "FROZEN_SOURCE_NUMERIC_PASS",
        "format": {"container": "RIFF/WAVE", "codec": "PCM", "format_tag": 1, "sample_rate_hz": SR,
                   "bits_per_sample": 16, "channels": 2, "channel_order": ["L", "R"], "block_align": 4, "byte_rate": 176400},
        "seed": SEED, "rng": "NumPy PCG64; independent offsets per layer", "originality": "Offline original synthesis; no source recordings, external assets, speech or music.",
        "generator": "../generate_slazeya_storm_audio.py", "generator_sha256": sha(Path(__file__).read_bytes()),
        "reproduce": "python SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_audio.py",
        "verify": "python SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_audio.py --verify-repro",
        "dependencies": {"python": platform.python_version(), "numpy": np.__version__, "scipy": scipy.__version__, "Pillow": PIL_VERSION},
        "independent_decoder": ffmpeg,
        "mastering": {"source_target_peak_dbfs": TARGET_DBFS, "hard_clipping_or_limiting": False, "quantization": "Nearest PCM16, no dither added to silent endpoints"},
        "contract": {"gather_duration_s": GATHER, "gather_max_gain": GATHER_GAIN, "gather_ramp": "smoothstep(elapsed/1.35)",
                     "burst_gain": BURST_GAIN, "gather_fade_after_b_s": GATHER_FADE, "gather_fade_curve": "linear", "option_gain_in_preview": 1.,
                     "runtime_path": "Resource/CustomAudio/SlazeyaStorm/", "burst_trigger": "actual manager callback only"},
        "preview": {"file": "preview_mix.wav", "duration_s": PREVIEW_SAMPLES/SR, "video_frames": 91, "video_fps": 30,
                    "gather_start_sample": 0, "gather_ready_sample": int(round(GATHER*SR)), "gather_wrap_sample": SR,
                    "burst_start_sample": int(round(BURST*SR)), "burst_start_s": BURST,
                    "secondary_lightning_sample": int(round((BURST+.1)*SR)), "gather_stop_sample": int(round((BURST+GATHER_FADE)*SR)),
                    "burst_end_exclusive_sample": int(round((BURST+1.2)*SR)), "normalization_after_mix": False,
                    "sources": {name: results[name]["sha256"] for name in ("gather_loop.wav", "burst_tail.wav")}},
        "files": {name: {**results[name], **design.get(name, {})} for name in SPECS},
        "waveform_diagnostics": {"file": "waveform_diagnostics.png", "sha256": sha((OUT / "waveform_diagnostics.png").read_bytes())},
        "acceptance": {"numeric": "PASS", "checks": ["Python wave and FFmpeg full decode/sample equality", "all three exact deterministic WAVs",
                      "format/duration/peak/DC/no clipping/zero endpoints", "loop value/slope/curvature and triple-repeat boundaries",
                      "stereo and low-band phase correlation/mono fold", "preview regenerated from decoded source WAVs, exact gains and sample times"],
                       "listening": "NOT_REVIEWED: no audio-perception tool available; playable WAVs supplied for actual listening.",
                       "limitations": "Numeric checks cannot establish timbre or perceived click-free quality; main-thread listening/game integration acceptance remains."}}
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"status": manifest["status"], "output": str(OUT), "files": {k: {"sha256": v["sha256"], "duration_s": v["duration_s"], "sample_peak_dbfs": v["sample_peak_dbfs"], "dc": v["dc_per_channel"]} for k, v in results.items()}}, indent=2))


if __name__ == "__main__":
    main()
