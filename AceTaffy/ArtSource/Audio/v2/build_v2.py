"""Mix short AceTaffy cues from the archived CC0 source packs. Requires numpy/scipy/ffmpeg."""
from __future__ import annotations

import hashlib
import io
import json
import subprocess
import wave
import zipfile
from pathlib import Path

import numpy as np
from scipy import signal
from scipy.io import wavfile

HERE = Path(__file__).resolve().parent
SOURCES = HERE / 'sources'
OUT = HERE / 'candidate'
RATE = 44100


def metal(name: str) -> np.ndarray:
    with zipfile.ZipFile(SOURCES / 'Kenney_impact-sounds.zip') as pack:
        original = pack.read('Audio/' + name + '.ogg')
    decoded = subprocess.check_output([
        'ffmpeg', '-v', 'error', '-i', 'pipe:0', '-f', 'f32le', '-ac', '2',
        '-ar', str(RATE), 'pipe:1'], input=original)
    x = np.frombuffer(decoded, '<f4').reshape(-1, 2).astype(np.float64)
    # Discard only leading encoder/silence padding, retaining the recorded transient.
    active = np.flatnonzero(np.max(abs(x), axis=1) > .005 * np.max(abs(x)))
    return x[max(0, int(active[0]) - 80):] if len(active) else x


def steam(marker: int) -> np.ndarray:
    with zipfile.ZipFile(SOURCES / 'bart_steam_hisses.zip') as pack:
        filename = f'steam hisses - Marker #{marker}.wav'
        rate, pcm = wavfile.read(io.BytesIO(pack.read(filename)))
    if rate != RATE or pcm.ndim != 2 or pcm.shape[1] != 2:
        raise ValueError('Unexpected steam source format')
    return pcm.astype(np.float64) / 32768


def filter_low(x: np.ndarray, cutoff: float) -> np.ndarray:
    sos = signal.butter(3, cutoff, fs=RATE, output='sos')
    return signal.sosfilt(sos, x, axis=0)


def layer(dest: np.ndarray, source: np.ndarray, when: float, gain: float,
          cutoff: float, life: float, attack: float = .002, release: float = .018) -> None:
    start = round(when * RATE)
    count = min(round(life * RATE), len(source), len(dest) - start)
    if count <= 0:
        return
    clip = filter_low(source[:count], cutoff)
    t = np.arange(count) / RATE
    envelope = np.minimum(1, t / attack) * np.minimum(1, (count - 1 - np.arange(count)) / (release * RATE))
    dest[start:start + count] += clip * envelope[:, None] * gain


def make(name: str, seconds: float, master: float, recipe: list[tuple]) -> dict:
    buffer = np.zeros((round(seconds * RATE), 2), np.float64)
    for kind, source, at, gain, lowpass, life in recipe:
        sample = metal(source) if kind == 'metal' else steam(source)
        layer(buffer, sample, at, gain, lowpass, life)
    buffer *= master
    # One shared ceiling prevents clipping; no upward peak normalization.
    maximum = float(np.max(abs(buffer)))
    if maximum > .64:
        buffer *= .64 / maximum
    buffer = np.clip(buffer, -.64, .64)
    pcm = np.rint(buffer * 32767).astype('<i2')
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / (name + '.wav')
    with wave.open(str(path), 'wb') as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())
    return dict(name=name, frames=len(pcm), seconds=seconds,
                peak=float(np.max(abs(pcm)) / 32768),
                rms=float(np.sqrt(np.mean((pcm.astype(np.float64) / 32768) ** 2))),
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main() -> None:
    recipes = {
        'SteamBlunt': (.28, 1.5, [
            ('metal', 'impactMetal_heavy_002', .003, .43, 5200, .16),
            ('metal', 'impactPunch_heavy_002', .003, .16, 1100, .13),
            ('steam', 2, .025, .48, 4300, .17),
        ]),
        'SteamPierce': (.22, 2.4, [
            ('metal', 'impactMetal_medium_002', .002, .41, 5800, .15),
            ('steam', 5, .013, .36, 4900, .13),
        ]),
        'SteamGuard': (.27, 1.5, [
            ('metal', 'impactMetal_light_004', .004, .35, 4100, .21),
            ('metal', 'impactTin_medium_001', .008, .13, 3200, .14),
        ]),
        'SteamDetonation': (.58, 1.4, [
            ('metal', 'impactPlate_heavy_003', .003, .28, 3200, .31),
            ('metal', 'impactPunch_heavy_002', .004, .27, 850, .23),
            ('steam', 3, .045, .78, 3400, .24),
            ('steam', 1, .19, .45, 2800, .18),
            ('steam', 5, .32, .24, 2700, .16),
        ]),
    }
    manifest = [make(name, length, master, recipe) for name, (length, master, recipe) in recipes.items()]
    (HERE / 'candidate-measures.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    for row in manifest:
        print(f"{row['name']}: {row['seconds']:.2f}s peak={row['peak']:.3f} rms={row['rms']:.3f} {row['sha256']}")


if __name__ == '__main__':
    main()
