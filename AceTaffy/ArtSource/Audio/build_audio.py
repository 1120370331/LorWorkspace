"""Rebuild the original AceTaffy mechanical cues; stdlib only, deterministic."""
from __future__ import annotations

import hashlib
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "AceTaffyMod" / "Resource" / "CustomAudio" / "AceTaffy"
LENGTHS = {"SteamBlunt": .28, "SteamPierce": .22, "SteamGuard": .30, "SteamDetonation": .65}


def pulse(t: float, start: float, decay: float) -> float:
    x = t - start
    return 0.0 if x < 0 else (1 - math.exp(-x / .0014)) * math.exp(-x / decay)


def make(name: str, length: float) -> tuple[bytes, float]:
    rng = random.Random(0xACE7AFF + sum(ord(c) for c in name))
    n = round(length * RATE)
    samples: list[float] = []
    slow = fast = 0.0
    for i in range(n):
        t = i / RATE
        white = rng.uniform(-1, 1)
        slow += .018 * (white - slow)
        fast += .23 * (white - fast)
        hiss = fast - slow
        if name == "SteamBlunt":
            click = .59 * pulse(t, .004, .012) * math.sin(2 * math.pi * (880 * t - 1250 * t * t))
            body = .38 * pulse(t, .008, .042) * math.sin(2 * math.pi * 220 * t)
            vent = .24 * pulse(t, .025, .065) * hiss
            value = click + body + vent
        elif name == "SteamPierce":
            click = .49 * pulse(t, .002, .008) * math.sin(2 * math.pi * (1750 * t - 1800 * t * t))
            ring = .26 * pulse(t, .004, .035) * math.sin(2 * math.pi * 720 * t)
            vent = .32 * pulse(t, .015, .045) * hiss
            value = click + ring + vent
        elif name == "SteamGuard":
            strike = .44 * pulse(t, .007, .012) * math.sin(2 * math.pi * 1040 * t)
            ring = .35 * pulse(t, .009, .095) * (
                .65 * math.sin(2 * math.pi * 410 * t) + .35 * math.sin(2 * math.pi * 685 * t))
            breath = .08 * pulse(t, .025, .080) * slow
            value = strike + ring + breath
        else:
            # A local pressure release: low weight and two uneven valve breaths.
            thump = .56 * pulse(t, .004, .085) * math.sin(2 * math.pi * (104 * t - 25 * t * t))
            brass = .24 * pulse(t, .009, .025) * math.sin(2 * math.pi * 390 * t)
            vents = (.23 * pulse(t, .040, .12) + .18 * pulse(t, .19, .085)
                     + .12 * pulse(t, .36, .070)) * hiss
            value = thump + brass + vents
        # Keep endpoints silent even after PCM quantization.
        fade = min(1., t / .003, (length - t) / .018)
        samples.append(value * max(0., fade))

    peak = max(abs(s) for s in samples)
    scale = min(1., .63 / peak)  # cap safely below -3 dBFS; no upward normalization.
    pcm = bytearray()
    for value in samples:
        mono = value * scale
        # Very narrow stereo presence without a delayed second transient.
        pcm += struct.pack('<hh', round(mono * 32767), round(mono * .965 * 32767))
    return bytes(pcm), peak * scale


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, length in LENGTHS.items():
        pcm, peak = make(name, length)
        path = OUT / f"{name}.wav"
        with wave.open(str(path), 'wb') as wav:
            wav.setnchannels(2)
            wav.setsampwidth(2)
            wav.setframerate(RATE)
            wav.writeframes(pcm)
        print(f"{path.relative_to(ROOT)}  frames={len(pcm)//4}  peak={peak:.4f}  sha256={hashlib.sha256(path.read_bytes()).hexdigest()}")


if __name__ == '__main__':
    main()
