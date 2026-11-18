"""Bake the approved V3's isolated radiance layers into Unity texture atlases."""
import argparse
from pathlib import Path
import sys
from unittest.mock import patch

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'preview_exports/christasha_starlance/v3'))
import render_v3 as v3

gold = v3.gold
stage = v3.stage
P = stage.point
smooth = gold.smooth
pulse = gold.pulse
SOURCE, CONTACT, EXIT = P(270, 365), P(530, 365), P(850, 365)
AXIS, NORMAL = v3.basis(CONTACT - SOURCE)
FPS = 60
COLUMNS = 8
PADDING = 2
LAYERS = {
    # Start/end frame, then bounds in V3 reference pixels. A beam x range is normalized.
    'Charge': (0, 7, (-46, -52, 66, 52)),
    'Lead': (3, 11, (-40, -43, 40, 43)),
    'Travel': (2, 30, (0, -44, 256, 44)),
    'Exit': (9, 26, (0, -24, 256, 24)),
    'Impact': (9, 44, (-146, -160, 176, 160)),
}


def crop(field, anchor, bounds):
    x, y = map(int, anchor)
    l, t, r, b = bounds
    return field[y+t:y+b, x+l:x+r]


def meteor_center(_shot, time, index):
    dx, dy, birth, damping = ((89, 60, .173, 33.), (94, -51, .194, 23.))[index]
    travel = .33 + .87*(1-np.exp(-max(0, time-birth)*damping))
    return CONTACT + (AXIS*dx + NORMAL*dy)*travel


def radiance(layer, time, bounds):
    result = np.zeros((stage.H, stage.W, 3), np.float32)
    if layer == 'Charge':
        charge = pulse(time, -.008, .045, .109)
        pull = 1-smooth(-.004, .092, time)
        for dx, dy, size in ((31, 17, 6.5), (55, 1, 5), (39, -22, 3.5)):
            star = SOURCE + (AXIS*dx + NORMAL*dy)*pull
            gold.add_star(result, star, AXIS, NORMAL, size, size*1.12,
                          charge*pull, gold.CHAMPAGNE, 9, .38*charge*pull)
        gold.add_star(result, SOURCE, AXIS, NORMAL, 14, 18, charge*1.24,
                      gold.GOLD, 21, .80*charge)
        return crop(result, SOURCE, bounds)
    if layer == 'Lead':
        ramp = pulse(time, .05, .122, .168)
        gold.add_star(result, SOURCE, AXIS, NORMAL, 10.7, 13.7, ramp*1.37,
                      gold.CHAMPAGNE, 15, .62*ramp)
        return crop(result, SOURCE, bounds)
    if layer in ('Travel', 'Exit'):
        if layer == 'Travel':
            origin = SOURCE
            tip = SOURCE + (CONTACT-SOURCE)*smooth(.048, .163, time)
            strength = smooth(.040, .093, time)*(1-smooth(.285, .473, time))
            gold.thread(result, origin, tip, AXIS, NORMAL, 8.5, 1.12, strength,
                        -.10+1.15*smooth(.285, .489, time), False, 1.)
            flow = smooth(.055, .098, time)*(1-smooth(.28, .46, time))
        else:
            origin = CONTACT
            tip = CONTACT + (EXIT-CONTACT)*smooth(.164, .234, time)
            strength = smooth(.163, .203, time)*(1-smooth(.25, .411, time))
            gold.thread(result, origin, tip, AXIS, NORMAL, 4.6, 3.1, strength, -.16, True, 1.)
            flow = smooth(.164, .207, time)*(1-smooth(.248, .403, time))
        v3.ribbons(result, origin, tip, time, 1., flow, layer == 'Exit')
        length = round(tip[0]-origin[0])
        _, top, width, bottom = bounds
        if length < 1:
            return np.zeros((bottom-top, width, 3), np.float32)
        return cv2.resize(crop(result, origin, (0, top, length, bottom)),
                          (width, bottom-top), interpolation=cv2.INTER_LINEAR)

    # V2's rays emits four charge stars and a lead star before the impact group.
    # Reuse the original impact star/halo/splinter painter while excluding all travel marks.
    add_star = gold.add_star
    count = 0

    def impact_stars(*args):
        nonlocal count
        count += 1
        if count > 5:
            add_star(*args)

    with patch.object(gold, 'add_star', impact_stars), patch.object(gold, 'thread'):
        gold.rays(result, SOURCE, CONTACT, EXIT, time, 1.)
    with patch.object(v3, 'meteor_center', meteor_center):
        for index in range(2):
            v3.meteor_wake(result, None, time, index, 1.)
    return crop(result, CONTACT, bounds)


def bake(out):
    out.mkdir(parents=True, exist_ok=True)
    for layer, (start, end, bounds) in LAYERS.items():
        left, top, right, bottom = bounds
        width, height = right-left, bottom-top
        stride_x, stride_y = width+2*PADDING, height+2*PADDING
        cols = min(COLUMNS, end-start+1)
        rows = (end-start+cols)//cols
        atlas = Image.new('RGB', (cols*stride_x, rows*stride_y))
        for offset, frame in enumerate(range(start, end+1)):
            field = radiance(layer, frame/FPS, bounds)
            # Same white shoulder as the reference preview; black is zero contribution.
            shoulder = np.minimum(field, .75) - .25*np.expm1(-np.maximum(0, field-.75)/.25)
            pixels = np.rint(np.clip(shoulder, 0, 1)*255).astype(np.uint8)
            if layer in ('Travel', 'Exit'):
                pixels[:, 0] = 0
                pixels[:, -1] = 0
            atlas.paste(Image.fromarray(pixels), ((offset % cols)*stride_x+PADDING,
                                                 (offset // cols)*stride_y+PADDING))
        atlas.save(out / (layer + '.png'), optimize=True)
        print(layer, atlas.size, start, end)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    cv2.setNumThreads(2)
    bake(parser.parse_args().output)
