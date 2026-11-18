"""Render the spiralling star-lance stream and its two world-space meteor wakes."""
import argparse
import math
from pathlib import Path
import subprocess
import sys

import cv2
import imageio_ffmpeg
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUT.parent / "v2"))
import render_v2 as gold

stage = gold.stage
ease, smooth, pulse = gold.ease, gold.smooth, gold.pulse


def basis(delta):
    forward = delta / np.linalg.norm(delta)
    normal = stage.point(-forward[1], forward[0])
    if normal[1] > 0:
        normal *= -1
    return forward, normal


def ribbons(light, origin, tip, t, scale, strength, exit_beam):
    length = np.linalg.norm(tip-origin)
    if strength < .004 or length < 3*scale:
        return
    axis, normal = basis(tip-origin)
    fringe = (10.5 if exit_beam else 23.)*scale
    dest, x, y = gold.patch(light, origin, tip, fringe+9*scale, axis, normal)
    u = np.clip(x/length, 0., 1.)
    # The blade socket and tip stay tight. Older material fans out in the rear third.
    flare = ease(0., .18, u) * (1-ease(.31, .99, u))**.84
    radius = fringe * flare * min(1., length/(88*scale))
    base_mask = ease(0., .065, u) * (1-ease(.84, 1., u)) * strength
    if not exit_beam:
        dissolved = -.14 + 1.19*smooth(.272,.458,t)
        base_mask *= ease(dissolved-.09, dissolved+.10, u)
    time = t-(.164 if exit_beam else .048)
    # The traveling light packets modulate radiance alone: there is always a connected spine.
    wave = (np.cos(2*math.pi*(u*1.04-time*10.4))*.5+.5)**7
    afterwave = (np.cos(2*math.pi*(u*.83-time*6.5+.22))*.5+.5)**11
    spine = np.exp(-(y/(.68*scale+.34))**2)
    dest += (spine*base_mask*(.23+.66*wave+.21*afterwave))[...,None] * gold.IVORY
    brush = gold.brushes()[1]

    # Unequal azimuths, lengths and winding rates keep the outer material open on one side.
    strands = ((.35, .69, .99, .97, gold.GOLD),
               (2.94, .87, .85, .80, gold.CHAMPAGNE),
               (4.92, .61, .63, .59, gold.PLATINUM))
    for i,(azimuth,turns,extent,weight,tint) in enumerate(strands):
        phase = azimuth + 2*math.pi*turns*(.68*u+.32*u*u) - time*(16.4+1.0*i)
        phase += .21*np.sin(4.3*u+.63*i)
        depth = np.cos(phase)
        front = ease(-.88, .9, depth)
        flank = radius * np.sin(phase) * (1-.07*i)
        if i==2:
            flank += .15*radius*ease(.1,.5,u)
        distance = y-flank
        face = .67 + .77*front + .62*wave
        width = (.40+1.02*front + .32*wave)*scale
        # Foreground strokes broaden on turn. Behind the luminous core they lose contrast.
        back_occlusion = (1 - .88*(1-front)*np.exp(-(y/(3.3*scale))**2))*(.53+.47*front)
        reach = 1-ease(extent-.17,extent+.01,u)
        if i==1:
            # One flank sheds its rear edge before it catches the next moving light packet.
            notch = (.24+.56*(1-front))*np.exp(-((u-.27+.06*np.sin(time*9.))/.105)**2)
            reach *= ease(.035,.205,u)*(1-notch)
        elif i==2:
            reach *= ease(.11,.36,u)*(.33+.67*afterwave)
        coverage = base_mask*weight*reach*back_occlusion
        band = np.exp(-(distance/(width+.27))**2)
        # Project the existing ImageGen filament's longitudinal texture in each ribbon's chart.
        # Its shifting highlights ride along the ribbon rather than oscillating its silhouette.
        tu = np.clip(.12+.72*u+.09*np.sin(4.8*u-15*time+azimuth), 0, 1)
        grain = brush.sample(tu, distance/(2.8*width+.65), max(2.,brush.sx/length))
        color = gold.mix(gold.BRONZE*.72, tint, .42+.57*front)
        color = gold.mix(color, gold.IVORY, .56*wave*front)
        dest += (coverage*face)[...,None]*(band[...,None]*color+.92*grain)
        bloom = np.exp(-(distance/(width+3.1*scale))**2)
        dest += (coverage*bloom*(.065+.12*front))[...,None]*tint
        # One strand peels into two finer trails before it rejoins the beam.
        if i == 0:
            # Older fibers separate towards the rear, away from the bright ridge. Their
            # free ends die independently; only the front of each tributary joins the ribbon.
            for short_end, join, side in ((.035,.53,1.),(.18,.76,-.73)):
                f = np.clip((u-short_end)/(join-short_end),0,1)
                taper = ease(0,.19,f)*(1-ease(.72,1.,f))
                split = side*fringe*(1-f)**1.2*(.47+.40*np.sin(phase+.89))
                width_shed = (.44+.35*np.sin(np.pi*f)**2)*scale
                fine = np.exp(-((distance-split)/(width_shed+.28))**2)
                dest += (fine*base_mask*taper*(.36+.55*wave)*(.65+.35*front))[...,None]*gold.ROSE


def meteor_center(shot,t,index):
    _, _, s, c, _ = stage.pose(shot,t)
    source, contact = stage.project(shot,s), stage.project(shot,c)
    axis, normal = basis(contact-source)
    x, y, birth, damping = ((89,60,.173,33.), (94,-51,.194,23.))[index]
    age = max(0.,t-birth)
    travel = .33 + .87*(1-math.exp(-age*damping))
    scale = stage.pixels_per_unit(shot)/(stage.H/34)
    return contact+(axis*x+normal*y)*scale*travel


def meteor_wake(light,shot,t,index,scale):
    birth = (.173,.194)[index]
    age = t-birth
    gain = smooth(.004,.032,age)*(1-smooth(.21,.29,age))
    if gain < .002:
        return
    history = min(age,.053 if index==0 else .048)
    lags = np.linspace(0.,history,9)
    history_points = np.array([meteor_center(shot,t-dt,index) for dt in lags])
    head = history_points[0]
    # Released filaments drift outward from their stored positions as the star slows down.
    axis, side = basis(head-meteor_center(shot,birth,index))
    age_fraction = lags/max(.001,history)
    drift = 13*scale*smooth(.015,.093,age)*(1-smooth(.16,.23,age))
    history_points -= (age_fraction**1.5 * drift)[:,None]*axis
    offsets = history_points-head
    distances = np.hypot(offsets[:,0],offsets[:,1])
    length = min(float(distances[-1]),54.*scale)
    if length < 2.:
        return
    dest, x, y = gold.patch(light, head, head-axis*length, 16*scale, -axis, side)
    u = x/length
    inside = np.clip(u,0,1)
    spine_y = np.interp(inside*length,distances,offsets@side)
    fork = (4.6 if index==0 else 6.6)*scale*(1. if index==0 else -1.)
    color = gold.CHAMPAGNE if index==0 else gold.ROSE
    for fork_sign, reach, power in ((1.,1.,1.),(-.65,.82,.68)):
        bend = fork*fork_sign*inside**1.2*(.75+.25*np.cos(6*inside-age*21))
        width = scale*(.45+1.9*np.sin(np.pi*inside)**2+.60*(1-inside))
        dist = y-spine_y-bend
        cover = ease(-.06,.14,u)*(1-ease(.32*reach,reach,u))
        core = np.exp(-(dist/(width+.42))**2)
        tint = gold.mix(color,gold.IVORY, (.8-.64*inside))
        dest += (cover*core*gain*power*1.34)[...,None]*tint
        mist = np.exp(-(dist/(width+3.*scale))**2)
        dest += (cover*mist*gain*power*.23)[...,None]*color


def render(shot,t):
    # v2 owns scene projection, authored star heads, hit ring and the slim continuous core.
    scene = gold.render(shot,t)
    if not .048 < t < .495:
        return scene
    light = np.zeros((stage.H,stage.W,3),np.float32)
    _,_,s,c,e = stage.pose(shot,t)
    source, contact, end = [stage.project(shot,p) for p in (s,c,e)]
    scale = stage.pixels_per_unit(shot)/(stage.H/34)
    front = source + (contact-source)*smooth(.048,.163,t)
    travel = smooth(.055,.098,t)*(1-smooth(.28,.46,t))
    ribbons(light,source,front,t,scale,travel,False)
    through = contact+(end-contact)*smooth(.164,.234,t)
    exit_light = smooth(.164,.207,t)*(1-smooth(.248,.403,t))
    ribbons(light,contact,through,t,scale,exit_light,True)
    for index in range(2):
        meteor_wake(light,shot,t,index,scale)
    return gold.composite(scene,light)


def stills():
    moments=(.104,.128,.155,.180,.204,.227,.275,.325)
    sheet=Image.new("RGB",(1640,640))
    for i,t in enumerate(moments):
        frame=Image.fromarray(render(stage.SHOTS[-1],t))
        cell=frame.crop((355,160,1220,498)).resize((410,160),Image.Resampling.LANCZOS)
        sheet.paste(cell,((i%2)*820,(i//2)*160))
        full=Image.fromarray(render(stage.SHOTS[0],t))
        cell=full.crop((350,253,1110,550)).resize((410,160),Image.Resampling.LANCZOS)
        sheet.paste(cell,((i%2)*820+410,(i//2)*160))
    sheet.save(OUT/"flow-stages.jpg",quality=96)
    Image.fromarray(render(stage.SHOTS[-1],.217)).save(OUT/"peak.png")


def movie():
    dest=OUT/"christasha-starlance-v3.mp4"
    temp=dest.with_suffix(".part.mp4")
    cmd=[imageio_ffmpeg.get_ffmpeg_exe(),"-nostdin","-hide_banner","-loglevel","error","-y",
         "-f","rawvideo","-pix_fmt","rgb24","-s",f"{stage.W}x{stage.H}","-r",str(stage.FPS),
         "-i","-","-an","-c:v","libx264","-crf","15","-preset","fast","-pix_fmt","yuv420p",
         "-movflags","+faststart",str(temp)]
    with subprocess.Popen(cmd,stdin=subprocess.PIPE) as proc:
        for shot in stage.SHOTS:
            neutral=render(shot,-1)
            for i in range(round(shot.length*stage.FPS)):
                t=(i/stage.FPS-shot.lead)*shot.speed
                frame=neutral if t<=-.061 or t>.72 else render(shot,t)
                proc.stdin.write(frame.tobytes())
        proc.stdin.close()
        if proc.wait():
            raise RuntimeError("Unable to encode the energy-beam preview")
    temp.replace(dest)


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stills",action="store_true")
    args=parser.parse_args()
    cv2.setNumThreads(4)
    stills()
    if not args.stills:
        movie()
