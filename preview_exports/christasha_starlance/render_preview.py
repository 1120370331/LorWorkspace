"""Render a battle-camera animatic. Run with Python, Pillow, NumPy, OpenCV and FFmpeg."""
import argparse
from dataclasses import dataclass
from functools import lru_cache
import math
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
CASTER = ROOT / "SteriaBuild/SteriaModFolder/Resource/CharacterSkin/Christasha"
TARGET = ROOT / "AceTaffy/AceTaffyMod/Resource/CharacterSkin/AceTaffy_Invention_0_3"
W, H, FPS = 1280, 720, 60
MAP_SCALE = 3
WHITE = np.array([255, 251, 235])
GOLD = np.array([255, 219, 155])


def smooth(a, b, t):
    t = min(1, max(0, (t - a) / (b - a)))
    return t * t * (3 - 2 * t)


def pulse(t, start, peak, end):
    return smooth(start, peak, t) * (1 - smooth(peak, end, t))


def point(x, y):
    return np.array((x, y), dtype=float)


@dataclass(frozen=True)
class Shot:
    label: str
    sign: int
    height: float
    speed: float
    length: float
    lead: float
    isolate: bool = False


SHOTS = (
    Shot("左向右   /   常速", 1, 0, 1, 1.85, .49),
    Shot("右向左   /   常速", -1, 0, 1, 1.85, .49),
    Shot("高低差   /   0.4× 慢放", 1, 1.85, .4, 2.95, .44),
    Shot("光效细节   /   0.4× 慢放", 1, 1.85, .4, 2.70, .37, True),
)


def pixels_per_unit(shot):
    return H / (23 if shot.isolate else 34)


def project(shot, xy):
    # Orthographic side view. Depth sorts sprites, while world X/Y set their image location.
    center = point(1.25, 9.0) if shot.isolate else point(0, 9)
    return point(W / 2, H / 2) + (xy - center) * point(1, -1) * pixels_per_unit(shot)


def pose(shot, t):
    sign = shot.sign
    advance = smooth(-.06, .14, t) * (1 - smooth(.29, .61, t))
    actor = point((-14.5 + 1.6 * advance) * sign, 0)
    target = point(10.7 * sign, shot.height)
    # World positions of native source and target after the single facing transform.
    source = actor + point(sign * 1.92, 2.52) * MAP_SCALE
    contact = target + point(-sign * 1.92, 2.52) * MAP_SCALE
    delta = contact - source
    distance = np.linalg.norm(delta)
    axis = delta / distance
    center_distance = np.dot(point(target[0], contact[1]) - source, axis)
    exit_pos = source + max(distance + 2 * MAP_SCALE, center_distance + 3.2 * MAP_SCALE) * axis
    return actor, target, source, contact, exit_pos


@lru_cache(None)
def font(size):
    return ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", size)


@lru_cache(None)
def background(shot):
    y, x = np.indices((H, W), dtype=np.float32)
    if shot.isolate:
        base = np.empty((H, W, 3), np.uint8)
        base[:] = [7, 8, 12]
    else:
        # A low-key painted arcade leaves a dark value behind the weapon and the contact.
        light = np.exp(-((x-650)/730)**2 - ((y-330)/350)**2)
        base = (np.array([8, 12, 21]) + light[..., None] * [20, 28, 36]).astype(np.uint8)
        canvas = Image.fromarray(base)
        d = ImageDraw.Draw(canvas)
        for cx in range(-140, W+170, 235):
            # Recessed lancet windows in the distant wall.
            for half, top, color in [(74, -10, (27, 36, 49)), (69, -3, (15, 23, 34)), (61, 9, (32, 44, 58))]:
                pts = [(cx-half, 309), (cx-half, top+89), (cx-half*.74,top+45),
                       (cx,top), (cx+half*.74,top+45), (cx+half,top+89),(cx+half,309)]
                d.polygon(pts, fill=color)
            d.line((cx, 15, cx, 309), fill=(21,30,43), width=3)
            for height in [112,204,278]:
                d.line((cx-60,height,cx+60,height),fill=(23,32,45),width=3)
            d.rectangle((cx-80,308,cx+81,315),fill=(28,38,49))
            for xx in (cx-116, cx-106):
                d.rectangle((xx,0,xx+3,425),fill=(23,31,43))
        back = cv2.GaussianBlur(np.asarray(canvas), (0,0), 3.0)
        base = cv2.addWeighted(base,.47,back,.53,0)
        # Floor plates follow the side-view horizon, never the thrust slope.
        horizon = 454
        for iy in range(horizon,H):
            depth = (iy-horizon)/(H-horizon)
            base[iy] = np.array([25,32,39]) + (1-depth)*np.array([4,5,5])
        for xx in np.arange(-2*W,4*W,300):
            cv2.line(base, (640+int((xx-640)*.08),horizon), (int(xx),H), (24,30,37), 1, cv2.LINE_AA)
        for iy in (465,489,538,619,712):
            cv2.line(base,(0,iy),(W,iy),(22,28,34),1,cv2.LINE_AA)
        vignette = .97 - .31*((x-W*.50)/(W*.66))**2 - .16*((y-H*.49)/(H*.7))**2
        noise = np.random.default_rng(100).normal(0,.55,(H,W,1))
        base = np.uint8(np.clip(base*vignette[...,None]+noise,0,255))
    canvas = Image.fromarray(base)
    d = ImageDraw.Draw(canvas)
    d.text((48,38), "克里丝塔夏  ·  金白星芒突刺", font=font(23), fill=(213,202,179))
    d.text((49,79), shot.label, font=font(15), fill=(142,150,157))
    return np.array(canvas)


@lru_cache(None)
def sprite(folder, motion, flip):
    xml = ET.parse(next(folder.glob("ModInfo.*")))
    node = xml.find(f".//{motion}/Pivot")
    path = folder / "ClothCustom" / f"{motion}.png"
    img = Image.open(path).convert("RGBA")
    px = (512 + float(node.attrib["pivot_x"])) / 1024 * img.width
    py = (512 - float(node.attrib["pivot_y"])) / 1024 * img.height
    if flip:
        img = img.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        px = img.width-px
    bounds = img.getbbox()
    art = img.crop(bounds)
    scale = H/34 * MAP_SCALE / 50
    size = tuple(max(1,round(n*scale)) for n in art.size)
    art = np.asarray(art.resize(size, Image.Resampling.LANCZOS))
    return art, point(px-bounds[0], py-bounds[1])*scale


def draw_sprite(frame, folder, motion, root_px, flip, light=None, strength=0, tilt=0):
    art, pivot = sprite(folder, motion, flip)
    x, y = np.rint(root_px-pivot).astype(int)
    h, w = art.shape[:2]
    if tilt:
        turn = cv2.getRotationMatrix2D(tuple(light-point(x,y)), tilt, 1)
        art = cv2.warpAffine(art, turn, (w,h), flags=cv2.INTER_LINEAR)
    color = art[:,:,:3].astype(np.float32)*[.91,.91,.94]
    if light is not None and strength > 0:
        grid_y, grid_x = np.ogrid[:h,:w]
        influence = np.exp(-((grid_x+x-light[0])**2 + (grid_y+y-light[1])**2)/(2*76**2))
        color += influence[:,:,None]*np.array([61,43,17])*strength
    alpha = art[:,:,3:4].astype(np.float32)/255
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x+w), min(H, y+h)
    if x1 <= x0 or y1 <= y0:
        return
    roi = frame[y0:y1,x0:x1]
    visible = np.s_[y0-y:y1-y,x0-x:x1-x]
    alpha = alpha[visible]
    roi[:] = np.uint8(np.clip(color[visible]*alpha+roi*(1-alpha),0,255))


class Emission:
    """Subpixel emissive geometry with additive compositing in local masks."""
    def __init__(self):
        self.rgb = np.zeros((H,W,3),np.float32)

    def shape(self, pts, color, amount, line=False):
        if amount <= .002:
            return
        pts = np.array(pts, dtype=float)
        low = np.maximum(0, np.floor(pts.min(axis=0)-3).astype(int))
        high = np.minimum([W,H], np.ceil(pts.max(axis=0)+4).astype(int))
        size = high-low
        if min(size) <= 0:
            return
        mask = np.zeros((size[1],size[0]),np.uint8)
        fixed = np.rint((pts-low)*16).astype(np.int32)
        if line:
            cv2.polylines(mask,[fixed],False,255,1,cv2.LINE_AA,4)
        else:
            cv2.fillPoly(mask,[fixed],255,cv2.LINE_AA,4)
        roi = self.rgb[low[1]:high[1],low[0]:high[0]]
        roi += mask[:,:,None] * (np.asarray(color,np.float32) * (amount / 255.0))

    def glint(self, center, axis, length, height, amount):
        normal = np.array([-axis[1],axis[0]])
        theta = np.linspace(0,2*math.pi,60,endpoint=False)
        u, v = np.cos(theta), np.sin(theta)
        # An astroid gives the concave flanks of a diffraction glint.
        u = np.sign(u)*np.abs(u)**6.5
        v = np.sign(v)*np.abs(v)**6.5
        outline = u[:,None]*axis*length + v[:,None]*normal*height
        self.shape(center+outline*1.10, GOLD, amount*.56)
        self.shape(center+outline, WHITE, amount*1.15)

    def needle(self, a, b, half, color, amount, widest=.70, lean=0):
        delta = b-a
        dist = np.linalg.norm(delta)
        if dist < 1 or half < .04:
            return
        perp = point(-delta[1],delta[0])/dist
        ridge = a+delta*widest
        self.shape([a,ridge+perp*half,b,ridge-perp*half*.63+delta*lean],color,amount)

    def composite(self, frame):
        strongest = np.max(self.rgb,axis=2)
        ys, xs = np.nonzero(strongest>.7)
        if not len(xs):
            return frame
        x0,x1=max(0,xs.min()-66),min(W,xs.max()+67)
        y0,y1=max(0,ys.min()-66),min(H,ys.max()+67)
        region = self.rgb[y0:y1,x0:x1]
        near = cv2.GaussianBlur(region,(0,0),2.2)
        small = cv2.resize(region,None,fx=.25,fy=.25,interpolation=cv2.INTER_AREA)
        halo = cv2.resize(cv2.GaussianBlur(small,(0,0),2.7), (x1-x0,y1-y0))
        broad = cv2.resize(cv2.GaussianBlur(small,(0,0),6.0), (x1-x0,y1-y0))
        light = region*.89+near*.28+halo*np.array([.55,.50,.37])+broad*np.array([.64,.52,.36])
        roi = frame[y0:y1,x0:x1]
        roi[:] = np.uint8(np.minimum(255,roi+light))
        return frame


def rays(emit, s, c, e, t, scale):
    d = c-s
    length = np.linalg.norm(d)
    axis = d/length
    normal = point(-axis[1],axis[0])
    # Decorative local "up" mirrors with the units; it does not invert vertically.
    if normal[1] > 0:
        normal *= -1
    charge = pulse(t,-.005,.052,.131)
    advance = smooth(.052,.166,t)
    travelling = pulse(t,.05,.105,.178)
    front = s+d*advance
    emit.glint(s,axis,13*scale,18*scale,charge*.96)
    for i,(a,h) in enumerate([(41,22),(55,-12),(29,30)]):
        pull = (1-smooth(.005,.10,t))
        start=s+scale*pull*(axis*a+normal*h)
        emit.needle(start,s,scale*.48,GOLD,charge*.61,.84)
        if i<2:
            emit.glint(start,axis,(3+i)*scale,5*scale,charge*.86*pull)
    emit.glint(front,axis,13*scale,11*scale,travelling)
    travel_light = smooth(.047,.095,t)*(1-smooth(.238,.49,t))
    beam_width = .37 + .53*pulse(t,.159,.214,.30)
    for offset, drift, bright, delay in ((0,0,1.,0),(3.3,11,.83,.009),(-4.2,-9,.65,.018)):
        tip=s+d*smooth(.052+delay,.166+delay*.12,t)
        root=s+normal*offset*scale
        mid=root+(tip-root)*.50+normal*drift*scale
        pts=[]
        for f in np.linspace(0,1,27):
            pos=(1-f)**2*root+2*f*(1-f)*mid+f*f*tip
            pts.append(pos)
        widths=np.array([(.15+f**1.4)*beam_width*(1 if not offset else .42)*scale for f in np.linspace(0,1,len(pts))])
        emit.shape(list(pts+widths[:,None]*normal)+list((pts-widths[:,None]*normal)[::-1]),GOLD if offset else WHITE,travel_light*bright)
    if t < .157:
        return
    hit = smooth(.157,.177,t)
    peak = pulse(t,.16,.216,.39)
    faint = hit*(1-smooth(.27,.66,t))
    travel = smooth(.166,.231,t)
    through = c+(e-c)*travel
    pierce_light = hit*(1-smooth(.235,.406,t))
    emit.needle(c,through,scale*(.7+2.2*peak),WHITE,pierce_light*1.19,.35)
    emit.needle(c+normal*3.8*scale,through+normal*.4*scale,scale*.60,GOLD,pierce_light*.84,.65)
    emit.needle(c-normal*4.3*scale,through-(e-c)*.16,scale*.72,GOLD,pierce_light*.74,.43)
    # A narrow ring in the plane normal to the aim; the near and far arcs differ in brightness.
    ring_progress = smooth(.152,.214,t)
    radius = (16+80*ring_progress+12*smooth(.24,.52,t))*scale
    ring_alpha = hit*(1-smooth(.225,.56,t))
    for a in np.linspace(0,2*math.pi,44,endpoint=False):
        arc=np.linspace(a,a+2*math.pi/44,6)
        u=np.cos(arc)
        v=np.sin(arc)
        points=c+(u[:,None]*axis*.207+v[:,None]*normal)*radius
        radial=u[:,None]*axis+v[:,None]*normal*.207
        profile = .46+.60*(.5+.5*math.cos(a-.55))**1.1
        emit.shape(list(points+radial*.65*scale)+list((points-radial*.65*scale)[::-1]),GOLD,ring_alpha*profile)
    # Uneven, predominantly forward fragments; each has a short trail and a luminous chipped head.
    pattern=[(-68,72,1.8),(-43,114,2.9),(-24,91,1.7),(-12,131,2.1),(8,104,1.7),
             (19,129,2.9),(32,89,1.6),(46,117,2.6),(67,79,1.6),(105,60,1.4),
             (143,79,1.7),(-135,75,1.5),(-158,51,1.1),(155,52,1.3),(-84,78,1.)]
    for i,(angle,reach,width) in enumerate(pattern):
        age=t-(.157+(i%4)*.0048)
        life=.205+(i%5)*.030
        u=min(1,max(0,age/life))
        radiant=math.radians(angle)
        direction=axis*math.cos(radiant)+normal*math.sin(radiant)
        dispersion=1-math.exp(-age*22) if age>0 else 0
        tip=c+direction*reach*scale*dispersion
        fade=pulse(age,0,.024+i%3*.01,life)
        tail=c+direction*reach*scale*max(0,dispersion-(1-u)*(.43+.20*(i%3)))
        emit.needle(tail,tip,width*.26*scale,GOLD,fade*.85,.78)
        shard=max(0,1-u*.85)*min(1,age/.026)
        start=tip-direction*(13+6*(i%3))*scale*shard
        emit.needle(start,tip,width*scale*shard,GOLD,fade*1.45,.46,.07)
    for i,(dx,dy,size) in enumerate([(83,62,8.4),(102,-44,9.9),(-58,53,6.7),(50,98,5.6)]):
        age=t-.174-.012*(i%3)
        move=1-math.exp(-max(0,age)*28)
        center=c+(axis*dx+normal*dy)*(1.20*scale)*move
        life=(.47,.54,.32,.44)[i]
        alpha=pulse(age,0,.045+i*.014,life)
        phase=math.radians((-12,15,7,-16)[i])
        star_axis=axis*math.cos(phase)+normal*math.sin(phase)
        emit.glint(center,star_axis,size*scale*(.53+.54*alpha),size*scale*(.68+.63*alpha),alpha)
    core=hit*(1-smooth(.228,.39,t))
    emit.glint(c,axis,(9+38*peak)*scale,(11+38*peak)*scale,core*1.12)
    emit.glint(c,axis,(2.0+6*faint)*scale,(3.2+8*faint)*scale,faint*.89)


def shadow(frame, center, radius):
    x,y=np.rint(center).astype(int)
    xs=np.arange(-radius,radius+1)
    ys=np.arange(-13,14)[:,None]
    alpha=(.41*np.exp(-(xs/(radius*.62))**2-(ys/4.8)**2))[:,:,None]
    roi=frame[y-13:y+14,x-radius:x+radius+1]
    roi[:]=np.uint8(roi*(1-alpha))


def render(shot, t):
    frame=background(shot).copy()
    actor,target,s,c,e=pose(shot,t)
    sp,cp,ep=(project(shot,p) for p in (s,c,e))
    impact=pulse(t,.156,.188,.35)
    if not shot.isolate:
        caster_pose="Penetrate" if -.06 < t < .54 else "Default"
        dst_pose="Damaged" if .16 <= t < .50 else "Default"
        base_a,base_b=project(shot,actor),project(shot,target)
        # The native strike pose is airborne and the support shadow stays on the floor.
        shadow(frame,base_a+point(0,5),95)
        shadow(frame,base_b+point(0,2),110)
        if caster_pose=="Penetrate":
            # In the installed 512px art the sword spine is 12px above the fallback pivot.
            # Seat the airborne lunge lower so the canonical world-space origin meets the blade.
            base_a=base_a+point(0,12*MAP_SCALE/50*pixels_per_unit(shot))
        recoil=shot.sign*3.5*impact
        draw_sprite(frame,TARGET,dst_pose,base_b+point(recoil,0),shot.sign<0,cp,impact)
        tilt=math.degrees(math.atan2(sp[1]-cp[1], abs(cp[0]-sp[0])))*shot.sign if caster_pose=="Penetrate" else 0
        draw_sprite(frame,CASTER,caster_pose,base_a,shot.sign>0,sp,pulse(t,-.02,.071,.23)*.58,tilt)
    if -.008<t<.74:
        emit=Emission()
        rays(emit,sp,cp,ep,t,pixels_per_unit(shot)/(H/34))
        frame=emit.composite(frame)
    return frame


def stills():
    moments=(.052,.129,.177,.218,.292,.38,.5,.652)
    strip=Image.new("RGB",(4*320,2*180))
    for i,t in enumerate(moments):
        img=Image.fromarray(render(SHOTS[-1],t))
        strip.paste(img.resize((320,180),Image.Resampling.LANCZOS),((i%4)*320,(i//4)*180))
    strip.save(OUT/"stages.jpg",quality=94)
    gallery=Image.new("RGB",(2*640,2*360))
    for i,shot in enumerate(SHOTS):
        img=Image.fromarray(render(shot,.218))
        gallery.paste(img.resize((640,360),Image.Resampling.LANCZOS),((i%2)*640,(i//2)*360))
    gallery.save(OUT/"camera-review.jpg",quality=94)


def movie():
    try:
        import imageio_ffmpeg
        ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        ffmpeg=shutil.which("ffmpeg")
    dest=OUT/"christasha-starlance.mp4"
    tmp=dest.with_suffix(".part.mp4")
    cmd=[ffmpeg,"-nostdin","-hide_banner","-loglevel","error","-y","-f","rawvideo","-pix_fmt","rgb24",
         "-s",f"{W}x{H}","-r",str(FPS),"-i","-","-an","-c:v","libx264","-crf","15",
         "-preset","fast","-pix_fmt","yuv420p","-movflags","+faststart",str(tmp)]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    for shot in SHOTS:
        neutral=render(shot,-1)
        for i in range(round(shot.length*FPS)):
            t=(i/FPS-shot.lead)*shot.speed
            frame=neutral if t<=-.061 or t>.74 else render(shot,t)
            proc.stdin.write(frame.tobytes())
    proc.stdin.close()
    if proc.wait():
        raise RuntimeError("FFmpeg failed to encode the preview")
    tmp.replace(dest)


if __name__=="__main__":
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--stills",action="store_true")
    args=ap.parse_args()
    cv2.setNumThreads(4)
    stills()
    if not args.stills:
        movie()
