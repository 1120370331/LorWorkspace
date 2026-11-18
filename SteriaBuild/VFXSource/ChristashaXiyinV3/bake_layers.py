"""Project the V3 finite-volume simulation into transparent Unity flipbooks."""
import argparse
from concurrent.futures import ThreadPoolExecutor
from dataclasses import replace
import hashlib
import json
from pathlib import Path
import sys

import cv2
import numpy as np
from PIL import Image


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SIM = ROOT / 'preview_exports/xiyin_golden_tide'
sys.path[:0] = [str(SIM / 'v3'), str(SIM / 'v2'), str(SIM)]
import hit_render
import render_tide_v2 as tide
import water_render

RATE = 30
FIELD_BOX = (40, 112, 1320, 848)
FIELD_SIZE = (500, 288)
FIELD_FRAMES = 51
HIT_SIZE = (192, 160)
HIT_FRAMES = 20
HIT_FRAME = (288, 240, 126, 178, 120, 150)  # canvas, body origin and body size
LAYERS = ('Behind', 'Breaker', 'Score', 'HitBack0', 'HitFront0', 'HitBack1', 'HitFront1')
LEVELS = (0, 87)
cv2.setNumThreads(1)


# Extend the filmed projection so off-frame foam and star tails are never
# clipped in the texture when the game camera zooms out.
tide.W = tide.stage.W = 1360
tide.H = tide.stage.H = 900
tide.stage.Y, tide.stage.X = np.indices((tide.H, tide.W), dtype=np.float32)
FIELD_CAMERA = replace(tide.BATTLE, cx=686, cy=548)

# For the floor sheen, use even neutral wetness. No pixels from the filmed arena
# are read by the atlas pipeline.
tide.stage.set_plate = lambda cam: (None, np.full((tide.H, tide.W), .58, np.float32))


def resolve(draw, shape, size, box=None):
    samples = []
    for level in LEVELS:
        plate = np.full(shape, level, np.uint8)
        draw(plate)
        if box:
            x0, y0, x1, y1 = box
            plate = plate[y0:y1, x0:x1]
        samples.append(cv2.resize(plate.astype(np.float32), size, interpolation=cv2.INTER_AREA) / 255)
    black, light = samples
    average_transmission = np.median((light - black) * (255 / LEVELS[1]), axis=2)
    alpha = np.clip(np.maximum(black.max(axis=2), 1 - average_transmission), 0, 1)
    rgb = black / np.maximum(alpha[:, :, None], 1e-4)
    # Push nearest surface color into transparent texels to avoid a dark bilinear rim.
    invisible = alpha <= 1 / 255
    inset = (~invisible).astype(np.uint8)
    if inset.any():
        _, labels = cv2.distanceTransformWithLabels(1-inset, cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL)
        rgb[invisible] = rgb[~invisible][np.maximum(0, labels[invisible]-1)]
    else:
        rgb[:] = 0
    alpha[invisible] = 0
    return np.rint(np.dstack((rgb, alpha)) * 255).clip(0, 255).astype(np.uint8)


def field_frame(index):
    cam = FIELD_CAMERA
    t = max(.018, index / RATE)
    shape = (tide.H, tide.W, 3)

    def behind(plate):
        tide.floor_water(plate, cam, t)
        hit_render.draw_field_floor(plate, cam, t)
        tide.score_back(plate, cam, t)
        hit_render.draw_field(plate, cam, t, 'rear')

    def breaker(plate):
        hit_render.draw_field(plate, cam, t, 'front')

    def score(plate):
        tide.stage.score(plate, cam, t, True)
        tide.stage.stars(plate, cam, t)

    return tuple(resolve(draw, shape, FIELD_SIZE, FIELD_BOX) for draw in (behind, breaker, score))


def impact_frame(index):
    cam = tide.BATTLE
    w, h, cx, feet, width, height = HIT_FRAME
    age = max(.0085, index / RATE)
    return tuple(resolve(lambda plate: hit_render.draw_hit(plate, cam, age, seed, cx, feet, width, height, 1, layer),
                         (h, w, 3), HIT_SIZE) for seed in (101, 118) for layer in ('back', 'front'))


def write_pages(output, layer, frames, cols, rows):
    height, width = frames[0].shape[:2]
    capacity = cols * rows
    for page, start in enumerate(range(0, len(frames), capacity)):
        cells = frames[start:start+capacity]
        atlas = np.zeros(((height+4)*((len(cells)+cols-1)//cols), (width+4)*cols, 4), np.uint8)
        for i, cell in enumerate(cells):
            x, y = (i % cols)*(width+4), (i // cols)*(height+4)
            atlas[y:y+height+4, x:x+width+4] = np.pad(cell, ((2, 2), (2, 2), (0, 0)), mode='edge')
        path = output / f'{layer}-{page}.png'
        Image.fromarray(atlas).save(path, compress_level=6)
    return sum(int(np.count_nonzero(frame[:, :, 3])) for frame in frames)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bake(output):
    output.mkdir(parents=True, exist_ok=True)
    # Compile the shared rasterizers before the worker pool hits them.
    water_render._maps()
    warm_field, warm_hit = field_frame(15), impact_frame(3)
    assert all(np.count_nonzero(frame[:, :, 3]) > 150 for frame in (*warm_field, *warm_hit))
    coverage = {}
    for capture, count, labels, cols, rows in ((field_frame, FIELD_FRAMES, LAYERS[:3], 4, 7),
                                              (impact_frame, HIT_FRAMES, LAYERS[3:], 6, 4)):
        with ThreadPoolExecutor(max_workers=5) as executor:
            frames = list(executor.map(capture, range(count)))
        for name, track in zip(labels, zip(*frames)):
            coverage[name] = write_pages(output, name, track, cols, rows)
            print(name, coverage[name], flush=True)
    manifest = dict(reference='Xiyin Golden Tide V3', fps=RATE,
                    world_box=list(FIELD_BOX), world_frame=list(FIELD_SIZE), world_count=FIELD_FRAMES,
                    impact_box=list(HIT_FRAME), impact_frame=list(HIT_SIZE), impact_count=HIT_FRAMES,
                    alpha_pixels=coverage, atlases={p.name: sha(p) for p in sorted(output.glob('*.png'))})
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('out', type=Path)
    bake(parser.parse_args().out)
