from __future__ import annotations

import hashlib
import sys
from pathlib import Path

import numpy as np
from PIL import Image


EXPECTED_PREVIEWS = (
    "black_01_early_reveal.png",
    "black_02_contact_peak.png",
    "black_03_full_body.png",
    "black_04_fade_retreat.png",
    "stage_01_early_reveal.png",
    "stage_02_contact_peak.png",
    "stage_03_full_body.png",
    "stage_04_fade_retreat.png",
)


def fail(message: str) -> None:
    print(f"FAIL image contract: {message}")
    raise SystemExit(1)


def load_rgb(path: Path) -> np.ndarray:
    if not path.is_file():
        fail(f"missing preview {path}")
    image = Image.open(path).convert("RGB")
    if image.size != (1280, 720):
        fail(f"unexpected preview size {image.size} for {path.name}")
    return np.asarray(image, dtype=np.float32)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_native_evidence(path: Path) -> None:
    if not path.is_file():
        fail(f"missing native particle evidence {path}")

    lines = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    if len(lines) != len(EXPECTED_PREVIEWS):
        fail(f"expected 8 native particle evidence rows, got {len(lines)}")

    rows = {line.split("|", 1)[0]: line for line in lines}
    for name in EXPECTED_PREVIEWS:
        row = rows.get(name)
        if row is None:
            fail(f"missing native particle evidence row for {name}")
        if "nativeParticleRenderers=6" not in row or "enabled=6" not in row:
            fail(f"native renderers were not enabled for {name}")
        if "modes=Mesh,Mesh,Mesh,Mesh,Stretch,Mesh" not in row:
            fail(f"native renderer roles are incomplete for {name}")

    for name in (
        "black_02_contact_peak.png",
        "black_03_full_body.png",
        "stage_02_contact_peak.png",
        "stage_03_full_body.png",
    ):
        row = rows[name]
        alive_field = next((field for field in row.split("|") if field.startswith("alive=")), None)
        if alive_field is None or int(alive_field.split("=", 1)[1]) <= 0:
            fail(f"no live native particles at {name}")


def verify_banding(full_rgb: np.ndarray) -> tuple[float, float, float]:
    luminance = full_rgb[275:355, 130:1080].max(axis=2).mean(axis=0) / 255.0
    kernel_size = 31
    padded = np.pad(luminance, kernel_size // 2, mode="reflect")
    trend = np.convolve(padded, np.ones(kernel_size) / kernel_size, mode="valid")
    residual = luminance - trend
    power = np.abs(np.fft.rfft(residual)) ** 2
    frequencies = np.fft.rfftfreq(residual.size)
    high_frequency = (frequencies >= 1.0 / 8.0) & (frequencies <= 1.0 / 4.0)
    total_power = max(float(power[1:].sum()), 1e-12)
    selected = np.where(high_frequency)[0]
    peak_index = selected[np.argmax(power[selected])]
    peak_period = 1.0 / frequencies[peak_index]
    peak_share = float(power[peak_index] / total_power)
    band_share = float(power[high_frequency].sum() / total_power)

    if 4.0 <= peak_period <= 8.0 and (peak_share >= 0.035 or band_share >= 0.18):
        fail(
            "regular transverse banding remains "
            f"(period={peak_period:.2f}px, peakShare={peak_share:.3f}, bandShare={band_share:.3f})"
        )
    return peak_period, peak_share, band_share


def verify_contact(contact_rgb: np.ndarray, full_rgb: np.ndarray) -> tuple[float, float, float]:
    effect_mask = contact_rgb.max(axis=2) > 24.0
    if int(effect_mask.sum()) < 20000:
        fail("contact frame effect mask is unexpectedly small")

    contact_pixels = contact_rgb[effect_mask]
    clip_any = float(np.mean(contact_pixels.max(axis=1) >= 250.0))
    clip_all = float(np.mean(contact_pixels.min(axis=1) >= 240.0))
    if clip_any >= 0.45 or clip_all >= 0.12:
        fail(f"contact is overexposed (clipAny={clip_any:.3f}, clipAll={clip_all:.3f})")

    contact_tip_energy = float(contact_rgb[:, 1120:].max(axis=2).sum())
    full_tip_energy = float(full_rgb[:, 1120:].max(axis=2).sum())
    if contact_tip_energy <= 1.0:
        fail("contact needle is missing from its expected overlap region")
    full_ratio = full_tip_energy / contact_tip_energy
    if not 0.04 <= full_ratio <= 0.60:
        fail(f"full-frame contact support ratio is {full_ratio:.3f}, expected 0.04-0.60")
    return clip_any, clip_all, full_ratio


def main() -> None:
    if len(sys.argv) != 4:
        fail("usage: verify_plasma_lightning_slash_images.py PREVIEW_DIR ROUND1_DIR EVIDENCE_FILE")

    preview_dir = Path(sys.argv[1])
    round1_dir = Path(sys.argv[2])
    evidence_path = Path(sys.argv[3])

    images = {name: load_rgb(preview_dir / name) for name in EXPECTED_PREVIEWS}
    for name in EXPECTED_PREVIEWS:
        if not (round1_dir / name).is_file():
            fail(f"missing archived round-1 preview {round1_dir / name}")

    changed_count = sum(
        sha256(preview_dir / name) != sha256(round1_dir / name)
        for name in EXPECTED_PREVIEWS
    )
    if changed_count < 6:
        fail(f"only {changed_count}/8 previews differ from round 1")

    peak_period, peak_share, band_share = verify_banding(images["black_03_full_body.png"])
    clip_any, clip_all, full_ratio = verify_contact(
        images["black_02_contact_peak.png"],
        images["black_03_full_body.png"],
    )
    verify_native_evidence(evidence_path)

    print(
        "PASS plasma lightning slash image contract "
        f"changed={changed_count}/8 "
        f"bandPeriod={peak_period:.2f}px peakShare={peak_share:.3f} bandShare={band_share:.3f} "
        f"clipAny={clip_any:.3f} clipAll={clip_all:.3f} fullContactRatio={full_ratio:.3f}"
    )


if __name__ == "__main__":
    main()
