from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from rembg import new_session, remove


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source_assets" / "christasha_vertical_crescent"
OUT_SOURCE = SOURCE / "processed_cropped"
UNITY_OUT = (
    ROOT.parent
    / "AnhierBlueWhiteSlash"
    / "UnityProject"
    / "Assets"
    / "Textures"
    / "ChristashaProvidedVerticalCrescent"
    / "Cropped"
)
MODEL_NAME = "isnet-general-use"
_MODEL_SESSION = None


def smoothstep(edge0: float, edge1: float, x: np.ndarray) -> np.ndarray:
    t = np.clip((x - edge0) / max(0.0001, edge1 - edge0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def alpha_bbox(alpha: np.ndarray, threshold: int = 8):
    ys, xs = np.where(alpha > threshold)
    if len(xs) == 0:
        return 0, 0, alpha.shape[1], alpha.shape[0]
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def model_session():
    global _MODEL_SESSION
    if _MODEL_SESSION is None:
        _MODEL_SESSION = new_session(MODEL_NAME)
    return _MODEL_SESSION


def model_cut_alpha(rgba: np.ndarray) -> np.ndarray:
    img = Image.fromarray(rgba, "RGBA")
    cut = remove(
        img,
        session=model_session(),
        alpha_matting=True,
        alpha_matting_foreground_threshold=235,
        alpha_matting_background_threshold=10,
        alpha_matting_erode_size=2,
    ).convert("RGBA")
    alpha = np.array(cut)[:, :, 3]
    alpha = cv2.medianBlur(alpha, 3)
    alpha = cv2.GaussianBlur(alpha, (0, 0), 0.42)
    alpha[alpha < 8] = 0
    return alpha


def connected_border_mask(candidate: np.ndarray) -> np.ndarray:
    count, labels = cv2.connectedComponents(candidate.astype(np.uint8), 8)
    if count <= 1:
        return np.zeros_like(candidate, dtype=bool)

    edge_labels = np.concatenate(
        [
            labels[0, :],
            labels[-1, :],
            labels[:, 0],
            labels[:, -1],
        ]
    )
    keep = np.zeros(count, dtype=bool)
    keep[np.unique(edge_labels)] = True
    keep[0] = False
    return keep[labels]


def estimate_checker_palette(border: np.ndarray) -> np.ndarray:
    grayish = border[
        (np.max(border, axis=1) - np.min(border, axis=1) < 18)
        & (np.mean(border, axis=1) > 150)
    ].astype(np.float32)
    if len(grayish) < 64:
        grayish = border.astype(np.float32)

    # Opaque PNG exports from image tools often contain a baked white/gray
    # checkerboard. K-means on the border captures those square colors, then
    # connected-component removal treats only border-reachable checker pixels as
    # background so bright blade material is not erased wholesale.
    k = min(6, max(2, len(grayish) // 4096))
    criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 30, 0.3)
    _, _, centers = cv2.kmeans(
        grayish,
        k,
        None,
        criteria,
        3,
        cv2.KMEANS_PP_CENTERS,
    )
    return centers.astype(np.float32)


def cleanup_alpha(alpha: np.ndarray, min_area: int) -> np.ndarray:
    labels, comps, stats, _ = cv2.connectedComponentsWithStats((alpha > 8).astype(np.uint8), 8)
    cleaned = np.zeros_like(alpha)
    for comp_id in range(1, labels):
        area = stats[comp_id, cv2.CC_STAT_AREA]
        if area >= min_area:
            cleaned[comps == comp_id] = alpha[comps == comp_id]
    cleaned = cv2.GaussianBlur(cleaned, (0, 0), 0.32)
    cleaned[cleaned < 10] = 0
    return cleaned


def remove_background(src: Image.Image, layer_index: int) -> Image.Image:
    # Work at Unity's max texture size first; the source files are opaque exports
    # over a pale background, so every alpha channel begins as a full rectangle.
    rgba = np.array(src.convert("RGBA").resize((1024, 1024), Image.Resampling.LANCZOS))
    model_alpha = model_cut_alpha(rgba)
    rgb = rgba[:, :, :3].astype(np.float32)
    h, w, _ = rgb.shape

    border = np.concatenate(
        [
            rgb[:32].reshape(-1, 3),
            rgb[-32:].reshape(-1, 3),
            rgb[:, :32].reshape(-1, 3),
            rgb[:, -32:].reshape(-1, 3),
        ],
        axis=0,
    )
    bg = np.median(border, axis=0)
    palette = estimate_checker_palette(border)

    diff = np.linalg.norm(rgb - bg, axis=2)
    palette_diff = np.min(
        np.linalg.norm(rgb[:, :, None, :] - palette[None, None, :, :], axis=3),
        axis=2,
    )
    hsv = cv2.cvtColor(rgb.astype(np.uint8), cv2.COLOR_RGB2HSV).astype(np.float32)
    sat = hsv[:, :, 1]
    value = hsv[:, :, 2]
    lum = 0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]
    bg_lum = 0.2126 * bg[0] + 0.7152 * bg[1] + 0.0722 * bg[2]
    darker_than_bg = np.maximum(0.0, bg_lum - lum)
    grayness = np.max(rgb, axis=2) - np.min(rgb, axis=2)
    warmth = np.maximum(0.0, rgb[:, :, 0] * 0.55 + rgb[:, :, 1] * 0.36 - rgb[:, :, 2] * 0.78 - 30.0)

    checker_candidate = (
        (grayness < 18.0)
        & (sat < 22.0)
        & (lum > 150.0)
        & (palette_diff < 34.0)
    )
    pale_checker = (grayness < 12.0) & (sat < 16.0) & (value > 214.0)
    border_background = connected_border_mask(checker_candidate | pale_checker)

    # ISNet supplies the main silhouette. The color matte below is only allowed
    # near that silhouette, so white blade material survives while the baked
    # white/gray checkerboard does not leak back in.
    score = np.maximum.reduce(
        [
            palette_diff * 0.92,
            diff * 0.58,
            sat * 0.90,
            darker_than_bg * 1.38,
            warmth * 1.10,
        ]
    )
    matte = np.maximum(
        smoothstep(42.0, 118.0, score),
        smoothstep(72.0, 150.0, sat),
    )
    matte[checker_candidate & (matte < 0.34)] = 0.0
    matte[pale_checker & (matte < 0.48)] = 0.0
    matte[border_background] = 0.0

    color_alpha = np.clip(matte * 255.0, 0, 255).astype(np.uint8)
    color_alpha = cv2.medianBlur(color_alpha, 3)
    color_alpha = cv2.GaussianBlur(color_alpha, (0, 0), 0.42)
    color_alpha[color_alpha < 30] = 0

    min_area = max(180, int(w * h * 0.00020))
    model_alpha = cleanup_alpha(model_alpha, min_area)

    model_near = cv2.dilate((model_alpha > 4).astype(np.uint8), np.ones((17, 17), np.uint8), iterations=1) > 0
    model_core = model_alpha > 48
    material_near_model = model_near & ~border_background
    color_alpha = np.where(material_near_model, color_alpha, 0).astype(np.uint8)

    alpha = np.maximum(model_alpha, (color_alpha.astype(np.float32) * 0.72).astype(np.uint8))
    alpha = np.where(border_background & ~model_core, 0, alpha).astype(np.uint8)
    alpha = cv2.bilateralFilter(alpha, 5, 36, 36)
    alpha = cv2.GaussianBlur(alpha, (0, 0), 0.28)
    alpha[alpha < 12] = 0
    alpha[:8, :] = 0
    alpha[-8:, :] = 0
    alpha[:, :8] = 0
    alpha[:, -8:] = 0

    # Estimate original RGB before it was composited over the pale background.
    a = np.maximum(alpha.astype(np.float32) / 255.0, 0.001)
    unpremultiplied = (rgb - bg * (1.0 - a[:, :, None])) / a[:, :, None]
    unpremultiplied = np.clip(unpremultiplied, 0, 255)

    # Keep the provided material color; only clean the matte and decontaminate bg.
    out = np.dstack([unpremultiplied.astype(np.uint8), alpha])
    out[alpha == 0, :3] = 0
    x0, y0, x1, y1 = alpha_bbox(alpha, 10)
    pad = 22
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(w, x1 + pad)
    y1 = min(h, y1 + pad)
    cropped = out[y0:y1, x0:x1]
    return Image.fromarray(cropped, "RGBA")


def contact_sheet(paths):
    thumbs = []
    for p in paths:
        im = Image.open(p).convert("RGBA")
        im.thumbnail((200, 200), Image.LANCZOS)
        canvas = Image.new("RGBA", (220, 220), (20, 20, 20, 255))
        checker = Image.new("RGBA", canvas.size, (28, 28, 28, 255))
        draw_checker = ImageDraw.Draw(checker)
        for y in range(0, checker.height, 20):
            for x in range(0, checker.width, 20):
                if ((x // 20) + (y // 20)) % 2 == 0:
                    draw_checker.rectangle((x, y, x + 19, y + 19), fill=(70, 70, 70, 255))
        canvas.alpha_composite(checker)
        canvas.alpha_composite(im, ((220 - im.width) // 2, (200 - im.height) // 2))
        draw = ImageDraw.Draw(canvas)
        draw.text((8, 202), p.stem[-2:], fill=(240, 220, 160, 255))
        thumbs.append(canvas)
    sheet = Image.new("RGBA", (660, 440), (12, 12, 12, 255))
    for i, thumb in enumerate(thumbs):
        sheet.alpha_composite(thumb, ((i % 3) * 220, (i // 3) * 220))
    return sheet


def main():
    OUT_SOURCE.mkdir(parents=True, exist_ok=True)
    UNITY_OUT.mkdir(parents=True, exist_ok=True)

    outputs = []
    for i, src_path in enumerate(sorted(SOURCE.glob("christasha_vertical_crescent_*.png")), start=1):
        cleaned = remove_background(Image.open(src_path), i)
        out_name = f"christasha_vertical_crescent_layer_{i:02d}.png"
        src_out = OUT_SOURCE / out_name
        unity_out = UNITY_OUT / out_name
        cleaned.save(src_out)
        cleaned.save(unity_out)
        outputs.append(src_out)
        print(f"{out_name}: {cleaned.size}")

    sheet = contact_sheet(outputs)
    sheet.save(OUT_SOURCE / "cropped_layer_contact_sheet.png")
    print(f"contact_sheet: {OUT_SOURCE / 'cropped_layer_contact_sheet.png'}")


if __name__ == "__main__":
    main()
