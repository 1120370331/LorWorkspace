"""Render the ImageGen-textured gold thrust against the original battle-camera staging."""
import argparse
import math
from functools import lru_cache
from pathlib import Path
import subprocess
import sys

import cv2
import imageio_ffmpeg
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUT.parent))
import render_preview as stage

W, H, FPS = stage.W, stage.H, stage.FPS
P = stage.point
smooth, pulse = stage.smooth, stage.pulse

BRONZE = np.array([.65, .30, .11], np.float32)
GOLD = np.array([1., .72, .32], np.float32)
CHAMPAGNE = np.array([1., .85, .60], np.float32)
IVORY = np.array([1., .975, .90], np.float32)
ROSE = np.array([1., .61, .56], np.float32)
PLATINUM = np.array([.68, .79, 1.], np.float32)


def ease(low, high, values):
    u = np.clip((values-low)/(high-low), 0., 1.)
    return u*u*(3.-2.*u)


def mix(a, b, weight):
    return a + (b-a)*np.asarray(weight)[..., None]


class Brush:
    def __init__(self, path, star):
        img = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32) / 255.
        # Remove the black floor before interpolation so the texture has no visible rectangle.
        floor = np.median(np.concatenate((img[0],img[-1],img[:,0],img[:,-1])),axis=0)
        img = np.maximum(0, (img-floor-1/255)/(1-floor+1e-5))
        luma = img @ np.float32([.24,.64,.12])
        ys, xs = np.nonzero(luma > (.31 if star else .07))
        if not len(xs):
            raise ValueError(f"Brush has no luminous body: {path}")
        if star:
            centers_y, centers_x = np.nonzero(luma > np.quantile(luma, .999))
            self.cx=float(np.median(centers_x))
            self.cy=float(np.median(centers_y))
            self.sx=max(1., (xs.max()-xs.min())*.5)
            self.sy=max(1., (ys.max()-ys.min())*.5)
        else:
            self.cx=float(xs.min())
            self.cy=float(np.median(ys))
            self.sx=float(xs.max()-xs.min())
            self.sy=max(1.,np.quantile(abs(ys-self.cy),.98))
        self.img=img

    def sample(self, u, v, pixel_scale=1.):
        # Prefilter to the projected pixel footprint before sampling hairline artwork.
        level = min(5, max(0, int(math.log2(max(1., pixel_scale)))))
        divisor=float(2**level)
        return cv2.remap(self.mip(level), np.float32((self.cx+u*self.sx)/divisor),
                         np.float32((self.cy+v*self.sy)/divisor), cv2.INTER_LINEAR,
                         borderMode=cv2.BORDER_CONSTANT)

    @lru_cache(None)
    def mip(self, level):
        return self.img if level == 0 else cv2.pyrDown(self.mip(level-1))


@lru_cache(None)
def brushes():
    return Brush(OUT/"art/star.png", True), Brush(OUT/"art/filament.png", False)


def patch(frame, a, b, margin, axis, normal):
    lo=np.maximum([0,0], np.floor(np.minimum(a,b)-margin).astype(int))
    hi=np.minimum([W,H], np.ceil(np.maximum(a,b)+margin+1).astype(int))
    if np.any(hi<=lo):
        raise ValueError("Visible effect lies outside the view")
    y,x=np.ogrid[lo[1]:hi[1],lo[0]:hi[0]]
    dx,dy=np.float32(x-a[0]),np.float32(y-a[1])
    return frame[lo[1]:hi[1],lo[0]:hi[0]], dx*axis[0]+dy*axis[1], dx*normal[0]+dy*normal[1]


def add_star(frame, center, axis, normal, rx, ry, gain, rim, halo_radius, halo_gain):
    if max(gain,halo_gain)<.003 or min(rx,ry)<.3:
        return
    r=max(halo_radius*1.85,2.3*max(rx,ry))
    dest,x,y=patch(frame,center,center,r,axis,normal)
    u,v=x/rx,-y/ry
    brush=brushes()[0]
    sample=brush.sample(u,v, min(brush.sx/rx,brush.sy/ry))
    original_luma=sample @ np.float32([.24,.64,.12])
    # Keep the painted near-field color. A small neutral hot core is exposed separately.
    field=sample**1.55
    tint=mix(rim,IVORY,ease(.30,.92,original_luma))
    dest += gain*(.59*field+.53*original_luma[...,None]**1.55*tint)
    # Curved shoulders control the optical waist across screen sizes; the painted body
    # carries the chromatic aberration and textured edge into those shoulders.
    contour=np.power(np.abs(u),.62)+np.power(np.abs(v),.62)
    inner=1-ease(.72,1.03,contour)
    dest += gain*1.10*inner[...,None]*mix(rim, IVORY, 1-ease(.40,.84,contour))
    # Two differently colored local lobes echo the hot pulse without broad full-frame bloom.
    rr=(x*x+y*y)/max(.2,halo_radius**2)
    halo=halo_gain*(.43*np.exp(-rr*11)+.49*np.exp(-rr*1.70))
    side=ease(-.60,.9,(x*.53+y*.64)/max(1.,halo_radius))
    tint_far=mix(np.float32([.84,.38,.13]), rim, .25)
    tint_near=mix(tint_far,rim,ease(.18,.89,np.exp(-rr*5)))
    gradient=mix(tint_near,PLATINUM,.30*side)
    dest += halo[...,None]*gradient


def thread(frame, a, b, axis, normal, spread, width, energy, root_fade, through, scale):
    length=float(np.linalg.norm(b-a))
    if energy<.002 or length<1.:
        return
    dest,x,y=patch(frame,a,b,spread+13*scale,axis,normal)
    u=x/length
    clipped=np.clip(u,0,1)
    endmask=ease(-1.,1. if through else min(24.*scale,length*.12),x)*(1-ease(length-1,length+.4,x))
    tip_fade=1-ease(.89,1.,clipped) if through else np.ones_like(clipped)
    arch=np.maximum(0.,np.sin(np.pi*clipped))
    start=ease(root_fade-.07,root_fade+.10,clipped) if root_fade > -.08 else 1.
    env=endmask*start
    if through:
        shape=(.06+arch**.80)*(1-.42*clipped)
        ridge=width*shape
        line=np.exp(-((y+.35*scale*tip_fade)/(ridge*tip_fade+.34))**2)
        # Narrow luminous blade face widens behind the travelling tip, with a single exit.
        wedge=env*line*tip_fade**.58
        ridge_color=mix(CHAMPAGNE,IVORY,ease(.10,.90,clipped))
        dest+=wedge[...,None]*ridge_color*energy*1.32
    else:
        # Long filament edges narrow into a unique travelling point.
        finger=spread*arch*(1-.62*clipped)
        for dy, weight, tint in ((.68,.68,ROSE),(-.58,.67,PLATINUM),(0.,1.05,CHAMPAGNE)):
            edge=np.exp(-((y-dy*finger)/(.45+width*(.38+.43*clipped)))**2)
            ramp=mix(BRONZE,tint,.32+.66*ease(-.12,.77,clipped))
            ramp=mix(ramp,IVORY,.70*ease(.54,1.,clipped))
            dest+=(edge*env*(.47+.60*clipped)*energy*weight)[...,None]*ramp
    brush=brushes()[1]
    band=(spread*(.18+.82*arch**.7)+.55*scale)*(1. if through else .66)
    band=.24+(band-.24)*tip_fade
    film=brush.sample(clipped, y/band, min(brush.sx/length,brush.sy/max(spread,1.)))
    detail=.8*film+.2*(film @ np.float32([.24,.64,.12]))[...,None]*CHAMPAGNE
    dest+=detail*(env*tip_fade*energy*(.81 if through else .76))[...,None]
    warm=np.exp(-((y/((2.8*scale+width)*tip_fade+1.2*scale))**2))*env*tip_fade
    dest+=(warm*(.15 if through else .075)*energy)[...,None]*GOLD
    if through:
        # Light scattered from the extremity extends continuously past the geometric blade.
        tip_light=np.exp(-((x-length+2*scale)/(5.4*scale))**2-(y/(1.9*scale))**2)
        dest+=(tip_light*.12*energy)[...,None]*CHAMPAGNE


def ring(frame,c,axis,normal,t,scale):
    arrival=smooth(.158,.181,t)
    if not arrival:
        return
    radius=(17+79*smooth(.157,.224,t)+11*smooth(.224,.49,t))*scale
    narrow=radius*.206
    dest,x,y=patch(frame,c,c,radius+5*scale,axis,normal)
    phi=np.arctan2(y/radius, x/narrow)
    ellipse=np.sqrt((x/narrow)**2+(y/radius)**2)
    distance=np.abs(ellipse-1)*ellipse/np.maximum(1e-6,np.sqrt((x/narrow**2)**2+(y/radius**2)**2))
    near=.5+.5*np.cos(phi-.89)
    decay=1-ease(.246+.06*near, .393+.146*near, t)
    alive=arrival*decay
    thread_width=scale*(.36+.50*near)
    band=np.exp(-((distance/(thread_width+.25))**2))
    bloom=np.exp(-((distance/(thread_width+1.9*scale))**2))
    color=mix(ROSE,CHAMPAGNE,near)
    dest+=((band+.12*bloom)*(.33+.68*near)*alive)[...,None]*color


def splinter(frame,c,axis,normal,angle,reach,width,birth,life,t,scale,cool):
    age=t-birth
    if not 0<age<life:
        return
    theta=math.radians(angle)
    forward=axis*math.cos(theta)+normal*math.sin(theta)
    side=-axis*math.sin(theta)+normal*math.cos(theta)
    progress=1-math.exp(-age*(16 if birth>.17 else 24))
    head=c+forward*reach*scale*progress
    tail_distance=(1-math.exp(-age*68))*(1-age/life)**1.1*reach*.71*scale
    a=head-forward*tail_distance
    if tail_distance<1.:
        return
    dest,x,y=patch(frame,a,head,width*scale+4,forward,side)
    u=np.clip(x/tail_distance,0,1)
    silhouette=np.interp(u,[0,.49,.67,.79,1.],[0,.10,.21,.86,0.])*width*scale
    kink=.32*scale*np.sin(u*np.pi*2+.22)
    coverage=np.clip((silhouette+.75-np.abs(y-kink))/.95,0,1)
    mask=coverage*ease(-1,1,x)*(1-ease(tail_distance-.5,tail_distance+.45,x))
    across=(y-kink)/(silhouette+.85)
    face=np.clip(.56+.44*across,0,1)
    color=mix(ROSE if cool else GOLD,PLATINUM if cool else IVORY,face*ease(.45,.81,u))
    age_light=pulse(age,0,.022,life)
    hot=np.exp(-2.2*across**2)*ease(.70,.83,u)
    veiled=np.exp(-np.square((y-kink)/(silhouette+2.*scale)))*ease(.30,.80,u)*ease(.02,.26,1-u)
    dest+=(mask*age_light*(.79+.40*hot))[...,None]*color
    dest+=(.15*age_light*veiled)[...,None]*(PLATINUM if cool else GOLD)


def rays(frame,s,c,e,t,scale):
    axis=(c-s)/np.linalg.norm(c-s)
    normal=P(-axis[1],axis[0])
    if normal[1]>0:
        normal*=-1
    charge=pulse(t,-.008,.045,.109)
    for i,(dx,dy,size) in enumerate([(31,17,6.5),(55,1,5),(39,-22,3.5)]):
        pull=(1-smooth(-.004,.092,t))
        pos=s+(axis*dx+normal*dy)*pull*scale
        add_star(frame,pos,axis,normal,size*scale,size*1.12*scale,charge*pull,CHAMPAGNE,9*scale,.38*charge*pull)
    add_star(frame,s,axis,normal,14*scale,18*scale,charge*1.24,GOLD,21*scale,.80*charge)
    advance=smooth(.048,.163,t)
    front=s+(c-s)*advance
    travel=smooth(.040,.093,t)*(1-smooth(.285,.473,t))
    thread(frame,s,front,axis,normal,8.5*scale,1.12*scale,travel,
           -.10+1.15*smooth(.285,.489,t),False,scale)
    ramp=pulse(t,.05,.122,.168)
    add_star(frame,front,axis,normal,10.7*scale,13.7*scale,ramp*1.37,CHAMPAGNE,15*scale,.62*ramp)
    if t<.156:
        return
    ring(frame,c,axis,normal,t,scale)
    thrust=smooth(.164,.234,t)
    tip=c+(e-c)*thrust
    body=smooth(.163,.203,t)*(1-smooth(.25,.411,t))
    thread(frame,c,tip,axis,normal,4.6*scale,3.1*scale,body,-.16,True,scale)
    # Rare small backscatters leave the forward fan open.
    parts=[(-71,85,3.0),(-50,107,3.2),(-39,133,2.3),(-20,145,2.9),(-9,103,3.5),
           (12,134,1.7),(25,149,4.1),(38,128,2.4),(54,110,3.2),(75,90,1.8),
           (110,67,1.9),(139,81,2.6),(-136,70,2.1),(-157,98,1.8),(160,68,1.5)]
    for i,(angle,reach,width) in enumerate(parts):
        splinter(frame,c,axis,normal,angle,reach,width,.157+(i%5)*.0047,.168+(i%5)*.039,t,scale,i in (1,8,13))
    # Larger stars launch on separate beats; the late pair becomes visible after the white peak.
    stars=[(89,60,19.,.173,.615,-18.,CHAMPAGNE),(94,-51,17.5,.194,.677,17.,ROSE),
           (-70,63,8.4,.257,.508,24.,PLATINUM),(35,99,6.9,.283,.704,-13.,CHAMPAGNE)]
    for i,(dx,dy,radius,birth,end,tilt,color) in enumerate(stars):
        age=t-birth
        if not 0<age<end-birth:
            continue
        u=1-math.exp(-age*(33 if i==0 else 23))
        center=c+(axis*dx+normal*dy)*scale*(.33+.87*u)
        phase=math.radians(tilt)
        sa=axis*math.cos(phase)+normal*math.sin(phase)
        sn=-axis*math.sin(phase)+normal*math.cos(phase)
        strength=smooth(0,.027+i*.004,age)*(1-smooth(birth+.078,end,t))
        size=scale*radius*(.48+.53*strength)
        add_star(frame,center,sa,sn,size,size*1.10,strength*1.29,color,1.54*size,.89*strength)
    peak=pulse(t,.158,.194,.352)
    after=smooth(.158,.182,t)*(1-smooth(.278,.639,t))
    growth=smooth(.158,.195,t)*(1-smooth(.23,.352,t))
    radius=(9*after+31.5*growth)*scale
    height=(10*after+36*growth)*scale
    halation=pulse(t,.157,.227,.468)
    halo_size=scale*(26+31*pulse(t,.157,.258,.495))
    add_star(frame,c,axis,normal,radius,height, .80*after+1.00*peak,GOLD,halo_size,1.30*halation)


def composite(scene, light):
    ys,xs=np.nonzero(np.max(light,axis=2)>.002)
    if not len(xs):
        return scene
    rect=np.s_[ys.min():ys.max()+1,xs.min():xs.max()+1]
    layer=light[rect]
    # White has a short shoulder, retaining saturated gold around sub-white material.
    k=.75
    shoulder=np.minimum(layer,k)+(1-k)*(-np.expm1(-np.maximum(0,layer-k)/(1-k)))
    base=scene[rect].astype(np.float32)/255.
    scene[rect]=np.rint(255*(base+(1-base)*shoulder)).astype(np.uint8)
    return scene


def render(shot,t):
    scene=stage.background(shot).copy()
    actor,target,s,c,e=stage.pose(shot,t)
    sp,cp,ep=[stage.project(shot,p) for p in (s,c,e)]
    impact=pulse(t,.157,.19,.362)
    if not shot.isolate:
        a=stage.project(shot,actor)
        b=stage.project(shot,target)
        stage.shadow(scene,a+P(0,5),95)
        stage.shadow(scene,b+P(0,2),110)
        active=-.06<t<.54
        if active:
            a+=P(0,12*stage.MAP_SCALE/50*stage.pixels_per_unit(shot))
        tilt=math.degrees(math.atan2(sp[1]-cp[1],abs(cp[0]-sp[0])))*shot.sign if active else 0.
        stage.draw_sprite(scene,stage.TARGET,"Damaged" if .16<=t<.5 else "Default",b+P(shot.sign*3.5*impact,0),shot.sign<0,cp,impact)
        stage.draw_sprite(scene,stage.CASTER,"Penetrate" if active else "Default",a,shot.sign>0,sp,pulse(t,-.02,.07,.23)*.58,tilt)
    if -.008<t<.72:
        light=np.zeros((H,W,3),np.float32)
        rays(light,sp,cp,ep,t,stage.pixels_per_unit(shot)/(H/34))
        composite(scene,light)
    return scene


def stills():
    moments=(.046,.125,.179,.222,.299,.389,.505,.641)
    sheet=Image.new("RGB",(1920,540))
    for i,t in enumerate(moments):
        img=Image.fromarray(render(stage.SHOTS[-1],t))
        sheet.paste(img.resize((480,270),Image.Resampling.LANCZOS),((i%4)*480,(i//4)*270))
    sheet.save(OUT/"stages.jpg",quality=96)
    Image.fromarray(render(stage.SHOTS[-1],.222)).save(OUT/"peak.png")
    gallery=Image.new("RGB",(1280,720))
    for i,shot in enumerate(stage.SHOTS):
        img=Image.fromarray(render(shot,.222)).resize((640,360),Image.Resampling.LANCZOS)
        gallery.paste(img,((i%2)*640,(i//2)*360))
    gallery.save(OUT/"camera-review.jpg",quality=95)


def movie():
    dest=OUT/"christasha-starlance-v2.mp4"
    temp=OUT/"christasha-starlance-v2.part.mp4"
    cmd=[imageio_ffmpeg.get_ffmpeg_exe(),"-nostdin","-hide_banner","-loglevel","error","-y",
         "-f","rawvideo","-pix_fmt","rgb24","-s",f"{W}x{H}","-r",str(FPS),"-i","-",
         "-an","-c:v","libx264","-crf","15","-preset","fast","-pix_fmt","yuv420p",
         "-movflags","+faststart",str(temp)]
    with subprocess.Popen(cmd,stdin=subprocess.PIPE) as process:
        for shot in stage.SHOTS:
            neutral=render(shot,-1)
            for i in range(round(shot.length*FPS)):
                t=(i/FPS-shot.lead)*shot.speed
                image=neutral if t<=-.061 or t>.72 else render(shot,t)
                process.stdin.write(image.tobytes())
        process.stdin.close()
        if process.wait()!=0:
            raise RuntimeError("Failed to encode preview")
    temp.replace(dest)


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stills",action="store_true")
    args=parser.parse_args()
    cv2.setNumThreads(4)
    stills()
    if not args.stills:
        movie()
