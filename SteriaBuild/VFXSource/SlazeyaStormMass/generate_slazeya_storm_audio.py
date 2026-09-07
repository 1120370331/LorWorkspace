#!/usr/bin/env python3
"""Original storm SFX; frozen 2026-09-07 source contract. No deployment writes.

Run to author source_audio; --verify-repro is read-only and compares full RIFF
bytes against independent FFmpeg decoding and deterministic in-memory synthesis.
Shared synthesis/measurement primitives also serve the separate Velia generator.
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
from scipy import signal, ndimage
from PIL import Image, ImageDraw, ImageFont, __version__ as PIL_VERSION

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[2]
OUT = ROOT / "source_audio"
SR = 44100
SEED = 2026090707
TAU = 2 * np.pi
GATHER = 1.35
GATHER_GAIN = .55
BURST_GAIN = .78
GATHER_FADE = .04
SUBJECTIVE = "UNVERIFIED (audio input unsupported)"
CONTRACTS = {
    "gather_loop.wav": {"frames": 44100, "peak_dbfs": -8., "true_peak_dbfs": -7.5, "rms_dbfs": -16.},
    "burst_tail.wav": {"frames": 52920, "peak_dbfs": -5., "true_peak_dbfs": -4.5, "rms_dbfs": -16., "front_dbfs": -12.},
}

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
    side = noise(n, offset + 1, max(lo, 320), hi, slope)
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


def finish_transient(x, highpass=35):
    x = signal.sosfilt(signal.butter(2, highpass, "highpass", fs=SR, output="sos"), x, axis=0)
    t = np.arange(len(x)) / SR
    end = (len(x) - 1) / SR
    gate = smooth(t / .0005) * (1 - smooth((t - (end - .03)) / .03))
    return dc_project(x * gate[:, None], gate)


def dc_project(x, gate):
    x = x.copy()
    x -= (x.sum(axis=0) / gate.sum())[None, :] * gate[:, None]
    x[[0, -1]] = 0
    return x


def to_pcm(x, normalize=False):
    if normalize:
        raise ValueError("Peak normalization removed; explicit source mastering required")
    if not np.all(np.isfinite(x)) or np.max(np.abs(x)) >= 1:
        raise ValueError("Nonfinite or clipping float candidate")
    return np.rint(x * 32768).astype("<i2")


def encoded(pcm):
    buf = io.BytesIO()
    with wave.open(buf, "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    return buf.getvalue()


def master(x, target_rms, peak_cap, loop=False, front_target=None):
    """Stereo-linked offline smooth peak-envelope gain; no sample clipping.

    A +/-1ms maximum envelope, Gaussian-smoothed with exactly 1ms support,
    bounds each sample. Soft sixth-order gain approaches the ceiling smoothly.
    This is gain modulation (no per-sample tanh or clip); source output is solved
    to RMS, never normalized after preview mixing. M/S linkage preserves phase.
    """
    target = 10 ** (target_rms / 20)
    ceiling = 10 ** ((peak_cap - .20) / 20)
    t = np.arange(len(x)) / SR
    gate = np.sin(np.linspace(0, np.pi, len(x))) ** 2 if loop else (
        smooth(t / .0005) * (1 - smooth((t - ((len(x)-1)/SR - .03)) / .03)))
    # Front weight smoothly relaxes by .18s; no discontinuous RMS block gain.
    front_shape = 1 - smooth((t - .075) / .105)
    radius = round(.001 * SR)
    mode = "wrap" if loop else "constant"

    def solve(head):
        z = x * np.exp(np.log(head) * front_shape[:, None])
        env = ndimage.maximum_filter1d(np.max(np.abs(z), axis=1), 2*radius+1, mode=mode)
        env = ndimage.gaussian_filter1d(env, radius/4, radius=radius, mode=mode)
        base = target / rms(z)
        def process(drive):
            gain = (1 + (drive * env / ceiling) ** 6) ** (-1/6)
            y = dc_project(z * (drive * gain)[:, None], gate)
            return y, gain
        lo, hi = 0., 16 * base
        for _ in range(42):
            mid = (lo + hi) / 2
            if rms(process(mid)[0]) < target:
                lo = mid
            else:
                hi = mid
        y, gain = process(hi)
        if abs(db(rms(y)) - target_rms) > .001:
            raise ValueError("RMS target cannot be reached with bounded smooth source dynamics")
        return y, gain, hi

    head = 1.
    if front_target is not None:
        lo, hi = .15, 12.
        for _ in range(24):
            head = math.sqrt(lo * hi)
            y, _, _ = solve(head)
            if db(rms(y[:5292])) < front_target:
                lo = head
            else:
                hi = head
        head = math.sqrt(lo * hi)
    y, gain, drive = solve(head)
    if np.max(np.abs(y)) > 10 ** (peak_cap / 20):
        raise ValueError("DC-projected master exceeds frozen source peak")
    # Reject excessive dynamics; report a blocker instead of hiding damage.
    active = np.max(np.abs(x), axis=1) > np.max(np.abs(x)) * .01
    max_reduction = -db(np.min(gain[active]))
    if max_reduction > 12:
        raise ValueError(f"Excessive source gain reduction {max_reduction:.2f}dB; redesign layers")
    return to_pcm(y), {
        "method": "Stereo-linked smooth lookaround peak-envelope gain, sixth-order soft knee; RMS solve",
        "rms_target_dbfs": target_rms, "source_peak_ceiling_dbfs": peak_cap-.20,
        "front_120ms_target_dbfs": front_target, "front_weight": head,
        "front_weight_hold_s": .075, "front_weight_end_s": .18,
        "drive_linear": drive, "maximum_gain_reduction_db": max_reduction,
        "median_active_gain_reduction_db": float(np.median(-20*np.log10(gain[active]))),
        "lookaround_radius_samples": radius, "gaussian_sigma_samples": radius/4,
        "gaussian_radius_samples": radius, "gain_exponent": 6, "envelope_boundary": mode,
        "hard_clipping": False, "waveshaping": False,
        "final_dc_projection": True, "post_mix_normalization": False,
        "quantization": "Round nearest PCM16, no dither; exact silence retained"}


def synthesize_sources():
    n = SR
    t = np.arange(n) / SR
    pressure = stereo_noise(n, 101, 65, 215, -.35, .015)
    pressure *= (1 + .035*np.sin(TAU*2*t+.4))[:, None]
    water = stereo_noise(n, 201, 260, 1760, -.25, .19)
    water *= (.90+.10*modulation(n, 203, 3, 12))[:, None]
    vortex = stereo_noise(n, 211, 720, 2400, -.25, .21)
    vortex *= (.88+.12*modulation(n, 213, 4, 15))[:, None]
    air = stereo_noise(n, 301, 2300, 5700, -.7, .25)
    air *= (.92+.08*modulation(n, 303, 6, 21))[:, None]
    bubbles, events = droplets(n, 401, 76, 0, 1, periodic=True)
    gather, seam = close_loop(.41*pressure + .39*water + .18*vortex + .15*air + .36*bubbles)
    gather_pcm, gm = master(gather, -16, -8, loop=True)

    n = 52920
    t = np.arange(n) / SR
    # One broad front, not a chain of separate low-frequency impacts.
    attack = smooth(t / .006)
    body_env = attack * np.interp(t, [0,.035,.075,.12,.28,.45,.70,.90,1.17,1.20],
                                [1,1,.86,.70,.57,.46,.30,.10,0,0])
    pressure = stereo_noise(n, 501, 85, 265, -.2, .012) * body_env[:, None]
    slap = stereo_noise(n, 541, 160, 1080, -.10, .13) * body_env[:, None]
    shear = stereo_noise(n, 551, 720, 3800, -.45, .23)
    shear *= (body_env * (.86+.14*modulation(n, 553, 8, 23)))[:, None]
    crack = stereo_noise(n, 511, 1400, 9300, -.2, .16)
    crack *= event_envelope(n, 0, .0012, .008, .035)[:, None]
    second = stereo_noise(n, 531, 2200, 8200, -.4, .18)
    second *= event_envelope(n, .10, .001, .006, .135)[:, None]
    # Exact isolated -10dB relation before shared source gain.
    second *= .316227766 * np.max(np.abs(crack)) / np.max(np.abs(second))
    wash_env = smooth(t/.065) * np.interp(t, [0,.12,.45,.7,.9,1.17,1.2], [0,.52,.5,.32,.12,0,0])
    wash = stereo_noise(n, 561, 280, 1950, -.20, .19) * wash_env[:, None]
    mist = stereo_noise(n, 571, 1800, 4800, -.5, .28)
    mist *= (smooth((t-.12)/.12) * (1-smooth((t-.55)/.5)) * .20)[:, None]
    drops, burst_events = droplets(n, 601, 145, .025, 1.155)
    # Lower tail masks no late fresh hit; only scattered contacts beyond .90s.
    drops *= np.where(t < .90, 1., .42)[:, None]
    burst = finish_transient(.29*pressure + .85*slap + .24*shear + .19*crack
                             + .19*second + .46*wash + .12*mist + .39*drops)
    burst_pcm, bm = master(burst, -16, -5, front_target=-12.4)
    design = {
        "gather_loop.wav": {
            "layers": [
                {"name":"central_wind_pressure","band_hz":[65,215],"weight":.41,"width":.015,"seed_offsets":[101,102]},
                {"name":"dense_water_friction","band_hz":[260,1760],"weight":.39,"width":.19,"seed_offsets":[201,202,203]},
                {"name":"inward_vortex_texture","band_hz":[720,2400],"weight":.18,"width":.21,"seed_offsets":[211,212,213]},
                {"name":"air_detail","band_hz":[2300,5700],"weight":.15,"width":.25,"seed_offsets":[301,302,303]},
                {"name":"circular_modal_water_contacts","count":76,"weight":.36,"seed_offset":401}],
            "droplet_events_before_phase_rotation":events, "loop_construction":seam, "mastering":gm,
            "prospective_timbre":"Continuous weighty wind/water with dense audible midrange; no baked recurring cast accent."},
        "burst_tail.wav": {
            "layers": [
                {"name":"single_pressure_body","band_hz":[85,265],"weight":.29,"seed_offsets":[501,502]},
                {"name":"thick_water_slap","band_hz":[160,1080],"weight":.85,"seed_offsets":[541,542]},
                {"name":"water_sheet_shear","band_hz":[720,3800],"weight":.24,"seed_offsets":[551,552,553]},
                {"name":"primary_water_electric_crack","onset_s":0,"end_s":.035,"weight":.19,"seed_offsets":[511,512]},
                {"name":"lighter_secondary_crack","onset_s":.10,"end_s":.135,"isolated_relative_peak_db":-10,"seed_offsets":[531,532]},
                {"name":"returning_water","band_hz":[280,1950],"weight":.46,"seed_offsets":[561,562]},
                {"name":"foam_air","band_hz":[1800,4800],"weight":.12,"seed_offsets":[571,572]},
                {"name":"irregular_drops","count":145,"last_onset_before_s":1.155,"weight":.39,"seed_offset":601}],
            "body_envelope_knots_s":[0,.035,.075,.12,.28,.45,.70,.90,1.17,1.20],
            "body_envelope_values":[1,1,.86,.70,.57,.46,.30,.10,0,0], "body_attack_s":.006,
            "droplet_events":burst_events, "final_fade_s":[(n-1)/SR-.03,(n-1)/SR],
            "mastering":bm, "secondary_primary_isolated_peak_ratio":.316227766,
            "prospective_timbre":"One forceful water-pressure strike; short electrical edge then substantial rolling water, foam and sparse drops."}}
    # Compare both cracks through the actual common time-varying master gain.
    raw = burst
    # Common gain is known analytically; recompute from the recorded parameters.
    head = np.exp(np.log(bm["front_weight"]) * (1-smooth((t-.075)/.105)))
    radius = bm["lookaround_radius_samples"]
    env = ndimage.maximum_filter1d(np.max(np.abs(raw*head[:,None]),axis=1),2*radius+1,mode="constant")
    env = ndimage.gaussian_filter1d(env,radius/4,radius=radius,mode="constant")
    gain = bm["drive_linear"] * (1+(bm["drive_linear"]*env/10**(-5.2/20))**6)**(-1/6) * head
    ratio = np.max(np.abs(second*gain[:,None])) / np.max(np.abs(crack*gain[:,None]))
    design["burst_tail.wav"]["secondary_primary_after_common_gain_peak_db"] = db(ratio)
    if db(ratio) > -8:
        raise ValueError("Secondary crack after shared mastering exceeds -8dB relative primary")
    return {"gather_loop.wav":gather_pcm, "burst_tail.wav":burst_pcm}, design


def preview_context():
    path = REPO / "preview_exports/slazeya_storm_mass/round5/manifest.json"
    media = REPO / "preview_exports/slazeya_storm_mass/round5/media-manifest.json"
    m = json.loads(path.read_text(encoding="utf-8-sig"))
    mm = json.loads(media.read_text(encoding="utf-8-sig"))
    video = next(v for v in mm["media"] if Path(v["path"]).name == "battlefield_motion.mp4")
    video_path = Path(video["path"])
    if not video_path.exists():
        video_path = path.parent / video_path.name
    assert sha(video_path.read_bytes()) == video["sha256"]
    return {
        "source_manifest":str(path.relative_to(REPO)).replace("\\","/"),
        "source_manifest_sha256":sha(path.read_bytes()),
        "media_manifest_sha256":sha(media.read_bytes()),
        "video":str(video_path.relative_to(REPO)).replace("\\","/"), "video_sha256":video["sha256"],
        "video_frames":video["decodedFrames"], "video_fps":m["sequenceFps"],
        "frames":round(video["decodedFrames"]*SR/m["sequenceFps"]),
        "burst_start_s":m["callbackTime"], "burst_start_sample":round(m["callbackTime"]*SR),
        "cast_start_s":0, "option_gain":1., "normalization_after_mix":False}


def preview(pcm, context):
    t = np.arange(context["frames"]) / SR
    b = context["burst_start_sample"] / SR
    gain = .32*smooth(np.minimum(t,b)/.03) + .23*smooth(np.minimum(t,b)/GATHER)
    gain *= 1-np.clip((t-b)/GATHER_FADE,0,1)
    mix = pcm["gather_loop.wav"][np.arange(len(t))%SR].astype(float)/32768*gain[:,None]
    start = context["burst_start_sample"]
    tail = pcm["burst_tail.wav"].astype(float)/32768
    length = min(len(tail), len(mix)-start)
    mix[start:start+length] += tail[:length]*BURST_GAIN
    return to_pcm(mix)


def metrics(pcm, loop=False):
    x = pcm.astype(float) / 32768
    peak = np.max(np.abs(x))
    mono = x.mean(axis=1)
    f,p = signal.welch(x,SR,nperseg=2048,axis=0)
    bands = {f"{lo}-{hi}Hz":float(p[(f>=lo)&(f<hi)].sum()/max(float(p.sum()),1e-30))
             for lo,hi in ((20,250),(250,2000),(2000,12000))}
    low = signal.sosfilt(signal.butter(4,200,"lowpass",fs=SR,output="sos"),x,axis=0)
    window = round(.02*SR)
    energy = np.convolve(np.mean(x*x,axis=1),np.ones(window)/window,"valid")
    result = {
        "frames":len(pcm),"duration_s":len(pcm)/SR,
        "sample_peak_dbfs":db(peak),"sample_peak_linear":float(peak),
        "sample_peak_dbfs_per_channel":[db(v) for v in np.max(np.abs(x),axis=0)],
        "true_peak_8x_dbfs":db(np.max(np.abs(signal.resample_poly(x,8,1,axis=0)))),
        "true_peak_method":"scipy.signal.resample_poly 8/1, default Kaiser beta5 FIR, constant boundary",
        "rms_dbfs":db(rms(x)),"front_120ms_rms_dbfs":db(rms(x[:5292])),
        "max_20ms_rms_dbfs":db(np.sqrt(np.max(energy))),
        "max_20ms_rms_window_start_s":int(np.argmax(energy))/SR,
        "dc_per_channel":x.mean(axis=0).tolist(),
        "clipped_samples":int(np.count_nonzero((pcm==-32768)|(pcm==32767))),
        "first_pcm16":pcm[0].tolist(),"last_pcm16":pcm[-1].tolist(),
        "stereo_correlation":float(np.corrcoef(x.T)[0,1]),
        "low_band_correlation":float(np.corrcoef(low.T)[0,1]),
        "mono_fold_rms_loss_db":db(rms(mono)/rms(x)),
        "band_power_fractions":bands,
        "edge_first_1ms_peak_dbfs":db(np.max(np.abs(x[:44]))),
        "edge_last_1ms_peak_dbfs":db(np.max(np.abs(x[-44:]))),
        "rms_20ms_blocks_dbfs":[db(rms(x[i:i+window])) for i in range(0,len(x),window)]}
    if loop:
        delta = np.diff(x,axis=0)
        curvature = np.diff(np.vstack((x[-2:],x[:2])),n=2,axis=0)
        seam_energy = rms(np.vstack((x[-441:],x[:441])))
        result["loop_seam"] = {
            "step_pcm16":(pcm[0].astype(int)-pcm[-1]).tolist(),
            "slope_in_linear":(x[-1]-x[-2]).tolist(),"slope_out_linear":(x[1]-x[0]).tolist(),
            "seam_curvature_peak":np.max(np.abs(curvature),axis=0).tolist(),
            "interior_step_p99_9":np.percentile(np.abs(delta),99.9,axis=0).tolist(),
            "seam_20ms_rms_ratio":seam_energy/rms(x),
            "seam_vs_adjacent_20ms_rms_db":db(seam_energy/rms(np.vstack((x[-1323:-441],x[441:1323])))),
            "three_repeat_boundary_steps_pcm16":[(np.tile(pcm,(3,1))[k*len(pcm)].astype(int)-np.tile(pcm,(3,1))[k*len(pcm)-1]).tolist() for k in (1,2)]}
    return result


def decode(path, frames=None):
    with wave.open(str(path),"rb") as f:
        assert (f.getnchannels(),f.getsampwidth(),f.getframerate(),f.getcomptype()) == (2,2,SR,"NONE"), path
        n = f.getnframes()
        raw = f.readframes(n)
        assert len(raw)==n*4 and (frames is None or n==frames), path
    return np.frombuffer(raw,dtype="<i2").reshape(-1,2).copy()


def validate(pcm, spec, name):
    m = metrics(pcm,name=="gather_loop.wav")
    assert len(pcm)==spec["frames"], (name,"frames")
    assert m["sample_peak_dbfs"] <= spec["peak_dbfs"], (name,"peak",m["sample_peak_dbfs"])
    assert m["true_peak_8x_dbfs"] <= spec["true_peak_dbfs"], (name,"true peak")
    if "rms_dbfs" in spec:
        assert abs(m["rms_dbfs"]-spec["rms_dbfs"])<=1, (name,"RMS",m["rms_dbfs"])
    if "front_dbfs" in spec:
        assert abs(m["front_120ms_rms_dbfs"]-spec["front_dbfs"])<=1, (name,"front RMS",m["front_120ms_rms_dbfs"])
        assert m["max_20ms_rms_window_start_s"] < .080, (name,"late main body",m["max_20ms_rms_window_start_s"])
    assert m["clipped_samples"]==0 and max(abs(v) for v in m["dc_per_channel"])<1e-4, (name,"clip/DC")
    assert m["first_pcm16"]==[0,0] and m["last_pcm16"]==[0,0], (name,"endpoints")
    assert m["mono_fold_rms_loss_db"]>=-1.5 and m["low_band_correlation"]>=.85, (name,"phase")
    if name=="gather_loop.wav":
        s=m["loop_seam"]
        assert s["step_pcm16"]==[0,0]
        assert np.all(np.array(s["seam_curvature_peak"]) < np.array(s["interior_step_p99_9"]))
        assert .65<s["seam_20ms_rms_ratio"]<1.5 and abs(s["seam_vs_adjacent_20ms_rms_db"])<3, ("seam energy",s)
    return m


def waveform_board(out, decoded, results, title):
    img=Image.new("RGB",(1600,1180),"#0d1721")
    d=ImageDraw.Draw(img)
    font_path=Path("C:/Windows/Fonts/consola.ttf")
    font=ImageFont.truetype(str(font_path),18) if font_path.exists() else ImageFont.load_default()
    small=ImageFont.truetype(str(font_path),14) if font_path.exists() else ImageFont.load_default()
    d.text((25,15),title+" | PCM16 stereo 44100 Hz",font=font,fill="#eaf4ff")
    d.text((25,47),"OBJECTIVE DIAGNOSTICS ONLY | subjective_listening: "+SUBJECTIVE,font=small,fill="#ebc087")
    for row,(name,pcm) in enumerate(decoded.items()):
        x=pcm.astype(float)/32768
        m=results[name]
        top=125+row*315
        d.text((25,top-38),f"{name} | peak {m['sample_peak_dbfs']:.2f} | RMS {m['rms_dbfs']:.2f} | TP8x {m['true_peak_8x_dbfs']:.2f} dBFS",font=font,fill="#eaf4ff")
        left,width,height=80,1490,160
        d.line((left,top+height/2,left+width,top+height/2),fill="#426073")
        for c,color in enumerate(("#58dbed","#efb360")):
            for i in range(width):
                a,b=int(i*len(x)/width),max(int((i+1)*len(x)/width),int(i*len(x)/width)+1)
                v=x[a:b,c]
                d.line((left+i,top+height/2-float(v.max())*height,left+i,top+height/2-float(v.min())*height),fill=color)
        for j in range(7):
            d.text((left+width*j/6-15,top+height+6),f"{len(x)/SR*j/6:.3f}s",font=small,fill="#aabccc")
        vals=m["rms_20ms_blocks_dbfs"]
        points=[(left+width*i/max(1,len(vals)-1),top+height+75-np.clip((v+60)/60,0,1)*43) for i,v in enumerate(vals)]
        d.line(points,fill="#9ae8ad",width=2)
        d.text((25,top+height+95),f"20ms RMS contour | mono {m['mono_fold_rms_loss_db']:.3f} dB | low corr {m['low_band_correlation']:.4f} | DC {max(abs(v) for v in m['dc_per_channel']):.2e}",font=small,fill="#aabccc")
    d.text((25,1105),"Source mastering uses linked smooth envelope gain. Preview uses decoded sources, frozen gains, and no final normalization.",font=small,fill="#c3dfcf")
    d.text((25,1140),"Playable source/preview WAVs accompany these diagnostics. Game DSP and perceived timbre require independent acceptance.",font=small,fill="#c3dfcf")
    img.save(out/"waveform_diagnostics.png")


def run_generator(out, generator, contracts, synth, mix, context, label, contract, dependencies=()):
    import imageio_ffmpeg
    parser=argparse.ArgumentParser(description=label+" original source audio")
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--verify-repro",action="store_true")
    args=parser.parse_args()
    verify=args.verify or args.verify_repro
    pcm,design=synth()
    pcm["preview_mix.wav"]=mix(pcm,context)
    specs={**contracts,"preview_mix.wav":{"frames":context["frames"],"peak_dbfs":-3.,"true_peak_dbfs":-3.}}
    # All contract gates precede replacing production sources.
    for name, data in pcm.items():
        validate(data,specs[name],name)
    if not verify:
        out.mkdir(parents=True,exist_ok=True)
        for name,data in pcm.items():
            (out/name).write_bytes(encoded(data))
    ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    results,decoded={},{}
    for name,spec in specs.items():
        path=out/name
        data=decode(path,spec["frames"])
        decoded[name]=data
        run=subprocess.run([ffmpeg,"-v","error","-xerror","-i",str(path),"-map","0:a:0","-c:a","pcm_s16le","-f","s16le","pipe:1"],capture_output=True,check=True)
        assert run.stdout==data.tobytes(), (name,"FFmpeg decode")
        assert encoded(pcm[name])==path.read_bytes(), (name,"deterministic full RIFF")
        results[name]={**validate(data,spec,name),"sha256":sha(path.read_bytes()),"bytes":path.stat().st_size,
                       "independent_ffmpeg_decode":"PASS: every PCM16 sample identical",
                       "deterministic_regeneration":"PASS: identical full RIFF bytes"}
    assert np.array_equal(mix(decoded,context),decoded["preview_mix.wav"]), "Exact preview source/gain/timing mismatch"
    generator_hashes={str(p.relative_to(REPO)).replace("\\","/"):sha(p.read_bytes()) for p in [generator,*dependencies]}
    if verify:
        manifest=json.loads((out/"manifest.json").read_text(encoding="utf-8"))
        assert manifest["generator_hashes"]==generator_hashes
        assert manifest["preview"]==context
        for name in specs:
            assert manifest["files"][name]["sha256"]==results[name]["sha256"]
    else:
        waveform_board(out,decoded,results,label)
        manifest={
            "version":"2026-09-07.storm-dawn.audio.1","status":"SOURCE_NUMERIC_PASS",
            "subjective_listening":SUBJECTIVE,
            "format":{"container":"RIFF/WAVE","codec":"PCM","format_tag":1,"sample_rate_hz":SR,
                      "bits_per_sample":16,"channels":2,"channel_order":["L","R"],"block_align":4,"byte_rate":176400},
            "seed":SEED,"rng":"NumPy PCG64, base seed plus per-layer offsets",
            "generator_sha256":sha(generator.read_bytes()),"generator_hashes":generator_hashes,
            "originality":"Original offline FFT noise and low-Q irregular modal synthesis; no sampled recordings, VO, speech, music or external licensed assets.",
            "dependencies":{"python":platform.python_version(),"numpy":np.__version__,"scipy":scipy.__version__,"Pillow":PIL_VERSION},
            "independent_decoder":ffmpeg,"independent_decoder_sha256":sha(Path(ffmpeg).read_bytes()),
            "contract":contract,"source_contracts":contracts,"preview":context,
            "files":{name:{**results[name],**design.get(name,{})} for name in specs},
            "waveform_diagnostics":{"file":"waveform_diagnostics.png","sha256":sha((out/"waveform_diagnostics.png").read_bytes())},
            "acceptance":{"numeric":"PASS","subjective_listening":SUBJECTIVE,
                          "limitations":"Independent game/native-DSP lifecycle acceptance is owned by the main thread. Numeric gates do not establish timbre, absence of perceived harshness/ringing or actual game mix audibility."}}
        (out/"manifest.json").write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps({"status":"PASS","verify":verify,"files":{k:{p:v[p] for p in
                      ("sha256","frames","sample_peak_dbfs","true_peak_8x_dbfs","rms_dbfs","front_120ms_rms_dbfs","band_power_fractions")} for k,v in results.items()},
                      "subjective_listening":SUBJECTIVE},indent=2))


def main():
    run_generator(OUT,Path(__file__),CONTRACTS,synthesize_sources,preview,preview_context(),
                  "SLAZEYA / STORM",{
                      "gather_duration_s":1.35,"gather_max_gain":.55,
                      "gather_ramp":".32*smoothstep(t/.03)+.23*smoothstep(t/1.35)",
                      "burst_gain":.78,"gather_fade_after_b_s":.04,
                      "gather_fade_curve":"linear, snapshot current gain at callback",
                      "runtime_path":"Resource/CustomAudio/SlazeyaStorm/",
                      "option_gain_in_preview":1.,"burst_trigger":"actual callback only"})


if __name__=="__main__":
    main()
