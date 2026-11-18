"""Offline, time-seekable battle VFX preview. Run python render_tide.py [--stills]."""
import argparse
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from functools import lru_cache
import math
from pathlib import Path
import subprocess
import sys

import cv2
import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUT.parent / "christasha_starlance"))
import render_preview as game

W, H, FPS = 1280, 720, 60
Y, X = np.indices((H, W), dtype=np.float32)
TAU = 2*math.pi
END = 1.67


def ease(a, b, t):
    q = np.clip((t-a)/(b-a), 0., 1.)
    return q*q*(3.-2.*q)


def window(a, b, c, d, t):
    return ease(a,b,t)*(1-ease(c,d,t))


@dataclass(frozen=True)
class Camera:
    battle: bool
    cx: int
    cy: int
    radius: int
    flatten: float

    @property
    def ry(self):
        return self.radius*self.flatten


BEAUTY = Camera(False,638,420,425,.405)
BATTLE = Camera(True,646,508,453,.265)
STAGES = ((.085,"起潮"),(.235,"旋流"),(.395,"乐章展开"),(.585,"潮涡加速"),
          (.755,"群潮爆发"),(.984,"碎浪席卷"),(1.28,"星光余波"),(1.51,"消散"))
SHOTS = ((BEAUTY,1.,.30,2.26),(BATTLE,1.,.38,2.47),(BEAUTY,.45,.29,4.35))


@lru_cache(None)
def set_plate(cam):
    """Use one fixed stone material for both the stage and its broken wet reflections."""
    floor=Image.open(OUT/"art/stone-floor.png").convert("RGB")
    floor=np.asarray(floor.resize((W,H),Image.Resampling.LANCZOS)).astype(np.float32)
    luma=floor @ np.float32([.27,.58,.15])
    local=cv2.GaussianBlur(luma,(0,0),4.8)
    coarse=cv2.GaussianBlur(luma,(0,0),20.)
    rough=np.clip(.48+(luma-local)/28+(local-coarse)/48,0.,1.)
    vignette=.72+.21*np.exp(-((X-cam.cx)/555.)**2-((Y-cam.cy)/360.)**2)
    shade=(.76+.24*ease(25,570,Y))*vignette
    return np.uint8(np.clip(2.+floor*shade[:,:,None],0,255)),rough


@lru_cache(None)
def crest_art(name):
    im=Image.open(OUT/"art"/(name+"-crest.png")).convert("RGBA")
    box=im.getchannel("A").point(lambda x:255 if x>6 else 0).getbbox()
    im=im.crop(box); im.thumbnail((1280,850),Image.Resampling.LANCZOS)
    a=np.asarray(im).astype(np.float32)/255.
    alpha=a[:,:,3]
    # Keep concentrated foam opaque while transmitting the stage through the bronze belly.
    brightness=a[:,:,:3] @ np.float32([.30,.57,.13])
    alpha=alpha*(.49+.47*ease(.16,.87,brightness))
    a[:,:,:3]*=alpha[:,:,None]
    a[:,:,3]=alpha
    silhouette=np.argmax(alpha>.20,axis=0)/max(1,len(a)-1)
    return np.uint8(np.rint(a*255)), silhouette


@lru_cache(None)
def pool_art():
    im=Image.open(OUT/"art/pool.png").convert("RGB")
    im.thumbnail((1152,1152),Image.Resampling.LANCZOS)
    return np.asarray(im)


@lru_cache(None)
def breakup_noise():
    """Bounded, continuous multiscale fields for breakup in material space."""
    rng=np.random.default_rng(5348)
    field=np.zeros((384,640),np.float32)
    for res,gain in ((8,.47),(21,.31),(54,.16),(119,.06)):
        tile=rng.random((res,round(res*1.53)),dtype=np.float32)
        field+=gain*cv2.resize(tile,(640,384),interpolation=cv2.INTER_CUBIC)
    return field


@lru_cache(None)
def breaker_lip():
    """Measure the foam-bearing lip of the actual wave, ignoring its isolated tiny drops."""
    material,_=crest_art("front")
    solid=cv2.morphologyEx(np.uint8(material[:,:,3]>44),cv2.MORPH_OPEN,
                          cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(7,7)))
    top=np.argmax(solid,axis=0).astype(np.float32)/len(material)
    top=cv2.GaussianBlur(top[None,:],(15,1),3.1)[0]
    return material,top


@lru_cache(None)
def breaking_tufts():
    rng=np.random.default_rng(1643)
    tufts=[]
    for sector in ((-.45,.51,17),(.82,1.73,29),(2.05,2.68,12)):
        for j in range(sector[2]):
            angle=rng.uniform(*sector[:2])
            tufts.append((angle,.84+rng.uniform(0,.16)+.026*np.sin(angle),rng.uniform(.17,.33),
                          rng.uniform(.44,.80),rng.uniform(.04,.12),rng.uniform(.023,.086),rng.uniform(.52,1.)))
    return tufts


def breaker_splash(im,cam,t):
    if not .84<t<1.37:return
    material,top=breaker_lip(); th,tw=material.shape[:2]
    for angle,birth,life,u,width,height,power in breaking_tufts():
        age=(t-birth)/life
        if not 0<age<1:continue
        a=angle+.14*age+.04*age*age
        radius=.815+.085*ease(.84,1.03,birth)+.17*age+.032*np.sin(3*a-.5)
        fall=np.sin(np.pi*age)
        center_x=cam.cx+cam.radius*radius*np.cos(a)
        bottom=cam.cy+cam.ry*radius*np.sin(a)-cam.radius*.033*fall
        rw=max(3.,cam.radius*width*(.60+.73*age))
        rh=max(2.,cam.radius*height*(.12+.88*fall))
        x0=max(0,int(center_x-rw));x1=min(W,int(center_x+rw+1))
        y0=max(0,int(bottom-rh));y1=min(H,int(bottom+2))
        if x1<=x0 or y1<=y0:continue
        dx=(X[y0:y1,x0:x1]-center_x)/rw
        dy=(bottom-Y[y0:y1,x0:x1])/rh
        # Each tuft is the real perforated top edge of a breaking lip. Local air
        # rises first, and then the broad remaining foam settles into the wet skirt.
        utex=u+dx*.068+.025*age
        top_y=top[np.clip(np.int32(utex*(tw-1)),0,tw-1)]
        vtex=top_y+(.59-dy+.15*dx*dx)*.136
        pix=cv2.remap(material,np.float32(utex*(tw-1)),np.float32(vtex*(th-1)),cv2.INTER_LINEAR,
                      borderMode=cv2.BORDER_CONSTANT)
        softness=(1-ease(.32,.98,np.abs(dx)))*ease(-.01,.46,dy)
        mask=power*fall**.76*softness
        roi=im[y0:y1,x0:x1]
        roi[:]=np.uint8(np.minimum(255,roi*(1-(pix[:,:,3]/255*mask*.65)[:,:,None])+pix[:,:,:3]*mask[:,:,None]))


def breaker(im,cam,t):
    """An outward-moving low sheet carries the collapsing wall into a turbulent front."""
    gain=window(.824,.966,1.145,1.414,t)
    wet=window(.87,1.10,1.25,1.52,t)
    if max(gain,wet)<.003:return
    x,y,rad,ang=plane_coords(cam)
    arc=np.where(ang<-.84,ang+TAU,ang)
    sector=window(-.89,-.32,2.58,3.33,arc)
    current=.5+.5*np.sin(3.1*arc-.61+1.8*(t-.85))
    advance=ease(.828+.035*(1-current),1.19+.027*current,t)
    # The leading break starts on the right, rolls into the foreground and fragments asymmetrically.
    radius=.824+.239*advance+.024*ease(1.15,1.37,t)
    radius+=.042*np.sin(3*arc-.50-2.4*(t-.85))+.021*np.sin(7*arc+.77-3.5*(t-.85))
    radius+=.006*np.sin(18*arc+3.3*np.sin(5*arc)-6*t)
    width=.063+.054*current+.032*advance
    depth=(radius-rad)/width
    water,top=breaker_lip(); sh,sw=water.shape[:2]
    scan=.61+.26*np.sin(2.3*arc+.5*np.sin(5*arc)-3.9*t)+.032*np.sin(11*arc-3.8*t)
    sample_u=np.clip(scan+.029*depth+.011*np.sin(arc*16+depth*3),.30,.97)
    ridge_y=np.interp(sample_u.ravel(),np.arange(len(top),dtype=np.float32)/(len(top)-1),top).reshape(sample_u.shape)
    sample_v=ridge_y+.026+depth*.186+.028*np.sin(6.8*arc+3.7*depth-5*t)
    foam=cv2.remap(water,np.float32(sample_u*(sw-1)),np.float32(sample_v*(sh-1)),
                   cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    density=cv2.remap(breakup_noise(),np.float32((arc*67-depth*35-63*t)),np.float32(depth*97-arc*32+t*61+243),
                      cv2.INTER_LINEAR,borderMode=cv2.BORDER_REFLECT_101)
    clumps=ease(.33+.16*ease(1.12,1.37,t),.63,density)
    layer=window(-.70,-.28,1.36,2.25,depth)*sector
    # Advected foam clusters have a ragged leading edge, dark liquid across the shoulder
    # and fine isolated trailing bubbles. The patch occupies only the near/right sector.
    surf=foam[:,:,:3].astype(np.float32)
    mass=foam[:,:,3].astype(np.float32)/255
    fade=gain*layer*(.33+.67*clumps)
    edgefoam=fade*(1-.48*ease(.8,1.95,depth))
    leading=np.exp(-((depth-.08)/.45)**2)
    amber=np.exp(-((depth-.80)/.82)**2)*fade*(.2+.8*clumps)
    _,rough=set_plate(cam)
    sheen=wet*sector*(.14+.86*ease(.45,.84,rough))*np.exp(-((depth+.12)/.88)**2)
    light=(edgefoam*(.44+.48*leading+.25*clumps))[:,:,None]*surf
    light+=amber[:,:,None]*np.float32([31,17,5])+sheen[:,:,None]*np.float32([44,29,10])
    im[:]=np.clip(im*(1-(edgefoam*mass*.47)[:,:,None])+light,0,255).astype(np.uint8)


@lru_cache(None)
def plane_coords(cam):
    x=(X-cam.cx)/cam.radius; y=(Y-cam.cy)/cam.ry
    r=np.hypot(x,y); a=np.arctan2(y,x)
    return x,y,r,a


def paint_pool(im,cam,t):
    if t<=0 or t>=END:
        return
    x,y,rad,angle=plane_coords(cam)
    reach=.82+.20*ease(0,.46,t)+.07*ease(.77,1.24,t)
    env=window(0,.106,1.36,END,t)
    peak=window(.15,.77,.85,1.49,t)
    r=rad/reach
    spin=(.63*t+1.05*ease(.22,.83,t)+.32*ease(.8,1.4,t))
    theta=angle+spin*(.42+.88*np.exp(-r*r*3))+.13*np.sin(r*8-t*5)*ease(.06,.8,r)
    rip=.013*np.sin(5*theta+16*r-5*t)+.007*np.sin(9*theta-26*r+t*9)
    ripple_r=r+rip*ease(.055,.6,r)
    tex=pool_art()
    tx=(.50+.48*ripple_r*np.cos(theta))*(tex.shape[1]-1)
    ty=(.50+.48*ripple_r*np.sin(theta))*(tex.shape[0]-1)
    caustics=cv2.remap(tex,tx,ty,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    luma=caustics @ np.float32([.28,.58,.14])/255
    color=(np.float32(.95)*caustics + 94*luma[:,:,None]*np.float32([1.,.69,.30]))
    rim=1-ease(.94,1.12,r)
    gain=(.63+.44*peak)*(.79+.21*np.sin(3*theta+r*17-t*3.4)**2)*env*rim
    eye_energy=env*(.55+.45*peak)
    eye=np.exp(-(rad/.100)**2)*eye_energy
    # A few points of wet specular reflection survive between the large water silhouettes.
    _,rough=set_plate(cam)
    wet=(.2+.8*ease(.46,.85,rough))*(.35+.65*np.sin(16*angle+3*np.sin(rad*16)+t*2.2)**2)
    wet*=np.exp(-((rad-.91)/.19)**2)*peak
    # Thin foam spirals are advected with the undercurrent, tightening in pitch toward the eye.
    swirl=(angle+9.9*np.log1p(rad*2.95)+spin*(1.35-.60*rad)+.085*np.sin(7*angle+rad*27))
    bands=np.zeros_like(rad)
    for phase,extent,weight in ((0.,.94,1.),(2.05,.85,.65),(-2.15,.99,.58)):
        phase_curve=swirl+phase
        angular_distance=np.abs(np.arctan2(np.sin(phase_curve),np.cos(phase_curve)))
        width=.044+.103*np.exp(-13*rad)
        # Preserve a continuous curled lip near the eye, then break it into irregular caustic slivers.
        flicker=np.clip(.35+.82*np.sin(angle*3.1+28*rad+phase+.5*t)+.38*np.sin(11*angle-44*rad-phase),0,1)
        visibility=(.50+.50*np.exp(-7*rad)) + (.78*flicker-.66)*ease(.08,.27,rad)
        bands+=weight*np.exp(-(angular_distance/width)**2)*visibility*(1-ease(extent-.22,extent,rad))
    bands*=env*(.61+.52*peak)*(.75+.25*rough)
    goldwell=(np.exp(-((rad-.10)/.116)**2)*(.35+.65*np.cos(swirl/2)**4)+np.exp(-(rad/.035)**2))*eye_energy
    reflection_pool=(.32*np.exp(-((x+.55)/.35)**2-((y-.88)/.39)**2)
                    +.65*np.exp(-((x-.42)/.21)**2-((y-.99)/.40)**2))
    reflecting=(.15+1.35*ease(.47,.85,rough)**2)*reflection_pool*env*(.36+.64*peak)
    # Surface foam bends around the outer shoal, joining the feet of the two curling walls.
    # Sample real local water texture on the sloping shoulder of that shoal. Holes travel
    # with the current and open as each wave recedes, exposing the darker floor below.
    low=window(.075,.46,1.12,1.53,t)
    if low>.001:
        path=.89+.058*np.sin(3*angle-2.6*t)+.029*np.sin(7*angle-3.8*t+rad*3)
        rr=rad-path
        margin=(.071+.049*(.5+.5*np.sin(3*angle-5*t)))*(.60+.53*peak)
        strip=np.mod((angle+np.pi)*.59+.09*np.sin(4*angle)+rr*.94-.31*t,1.)
        u=.09+.78*strip
        v=.68+.165*rr/np.maximum(.04,margin)+.13*np.sin(5*angle-2*t)+.038*np.sin(22*angle+t*4)
        wave,_=crest_art("rear")
        surf=cv2.remap(wave,np.float32(u*(wave.shape[1]-1)),np.float32(v*(wave.shape[0]-1)),
                       cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
        brightness=surf[:,:,:3] @ np.float32([.27,.59,.14])/255.
        width=np.exp(-1.1*(rr/margin)**2)
        side=.065+.92*ease(.42,.94,np.abs(np.cos(angle-.13)))
        brk=.40+.60*ease(.13,.86,.5+.5*np.sin(angle*8.8-spin+rr*38))
        gain_surface=low*width*side*brk*window(0,.13,.84,1,strip)
        foam=surf[:,:,:3]*gain_surface[:,:,None]*(.22+.76*ease(.14,.70,brightness))[:,:,None]
    else:
        foam=0.
    out=(im.astype(np.float32)+color*gain[:,:,None]+foam+wet[:,:,None]*np.float32([30,18,7])
         +eye[:,:,None]*np.float32([74,42,12])+bands[:,:,None]*np.float32([214,157,72])
         +goldwell[:,:,None]*np.float32([94,62,22])+reflecting[:,:,None]*np.float32([117,83,32]))
    im[:]=np.clip(out,0,255).astype(np.uint8)


def wave_shape(cam,name,t):
    front = name=="front"
    birth=.11 if front else .075
    rise=ease(birth,.70 if front else .63,t)**.84
    hold=1-ease(.79 if front else .76,1.235 if front else 1.19,t)
    wall=rise*hold
    surge=ease(.32,.83,t)-.62*ease(.88,1.36,t)
    R=cam.radius
    if front:
        outflow=ease(.81,1.19,t)
        lo=cam.cx+R*(-.79-.13*surge+.12*(1-rise)-.046*outflow)
        hi=cam.cx+R*(.98+.055*surge+.069*outflow)
        max_h=R*(.64 if not cam.battle else .43)
    else:
        lo=cam.cx+R*(-.98+.032*surge)
        hi=cam.cx+R*(.27+.21*surge)
        max_h=R*(.48 if not cam.battle else .39)
    h=max_h*(.044+.956*wall)
    opacity=window(birth,birth+.115,1.10 if front else 1.07,1.47 if front else 1.42,t)
    return lo,hi,h,opacity,wall


def wave_base(x,cam,name,t):
    nx=(x-cam.cx)/cam.radius
    z=np.sqrt(np.maximum(0,1-np.minimum(.998,np.abs(nx))**2))
    if name=="front":
        depth=z*.83-.045+.075*np.sin(nx*3.1+t*3.3)+(.085+.085*z)*ease(.81,1.18,t)
    else:
        depth=-z*.68+.018+.075*np.sin(nx*3.5+t*3.1)
    return cam.cy+cam.ry*depth


def paint_wave(im,cam,name,t):
    lo,hi,h,amp,wall=wave_shape(cam,name,t)
    if amp<.003: return
    x0=max(0,int(lo-45)); x1=min(W,int(hi+45))
    y0=max(0,int(cam.cy-cam.ry-h-28)); y1=min(H,int(cam.cy+cam.ry+h*.18+15))
    x=X[y0:y1,x0:x1]; y=Y[y0:y1,x0:x1]
    tex,_=crest_art(name); th,tw=tex.shape[:2]
    q=(x-lo)/(hi-lo)
    ground=wave_base(x,cam,name,t)
    height=(ground-y)/h
    above=np.maximum(height,0)
    sign=1 if name=="front" else -1
    roll=ease(.32,.78,t)-1.68*ease(.8,1.23,t)
    # Invert the moving wave surface: the base follows the elliptical shoreline while the
    # crest curls farther than the mid-wall. A separate, slower phase carries the foam lip.
    uv=q+sign*(.11*roll*above**1.8+.037*np.sin(q*9.1-t*8.3)*above**1.23
        +.022*np.sin(q*20-height*5.8-t*10)*np.minimum(above,1))
    bob=(.035*np.sin(q*8.2+t*6.2)+.024*np.sin(q*17.4-t*8.7))*ease(.03,.8,above)
    uv+=.006*np.sin(19*q+height*4-t*9)
    v=1-height+bob+.016*np.sin(12*uv+height*9-6*t)
    sampled=cv2.remap(tex,np.float32(uv*(tw-1)),np.float32(v*(th-1)),cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    a=sampled[:,:,3].astype(np.float32)/255
    dissolve=ease(.895,1.285,t)
    if dissolve>.001:
        clusters=cv2.remap(breakup_noise(),np.float32(uv*662+31*t),np.float32(v*362-23*t),
                           cv2.INTER_LINEAR,borderMode=cv2.BORDER_REFLECT_101)
        foam_retention=sampled[:,:,3].astype(np.float32)/255
        exposed=clusters+.12*foam_retention
        broken=(1-.70*dissolve)*ease(-.22+.84*dissolve,.11+.81*dissolve,exposed)
    else:
        broken=1.
    foot_grain=(.5+.5*np.sin(31*uv+7*height+1.7*t))*(.7+.3*np.sin(64*uv+6*t)**2)
    blend_foot=1-ease(.81+.11*foot_grain,.95+.05*foot_grain,v)*(.67+.24*(1-foot_grain))
    gain=amp*broken*blend_foot
    rgb=sampled[:,:,:3].astype(np.float32)
    spec=(.60+.40*np.sin(uv*17+height*4-t*8))**8
    belly=(np.exp(-((uv-(.19 if name=="rear" else .72))/.13)**2-((height-.45)/.32)**2)
           *(.29+.71*np.cos(uv*27-height*9-t*5)**4)*wall)
    lit=(1+.21*wall*spec)[:,:,None]*np.float32([1.03,.995,.965])+belly[:,:,None]*np.float32([.43,.30,.09])
    roi=im[y0:y1,x0:x1]
    out=roi*(1-(gain*a)[:,:,None])+rgb*lit*gain[:,:,None]
    if name=="front" and wall>.03:
        # The specular light on the wet floor is stretched and broken by the paving.
        ref_height=(y-ground)/max(6.,h*.29)
        phase=.009*np.sin(y*.36+x*.028+4*t)+.007*np.sin(y*.67-t*5)
        ref=cv2.remap(tex,np.float32((q+phase)*(tw-1)),np.float32((1-ref_height)*(th-1)),cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
        rough=set_plate(cam)[1][y0:y1,x0:x1]
        reflect=amp*wall*.23*ease(.35,.69,rough)*(1-ease(.1,.93,ref_height))*ease(0,.035,ref_height)
        out+=ref[:,:,:3]*reflect[:,:,None]*np.float32([1.,.78,.42])
    roi[:]=np.clip(out,0,255).astype(np.uint8)


@lru_cache(None)
def flecks():
    rng=np.random.default_rng(52119)
    return [(name,float(rng.uniform(.17,1.06)),float(rng.uniform(.18,.45)),
             float(rng.uniform(.09,.76) if name=="rear" else rng.uniform(.38,.93)),
             float(rng.uniform(-.13,.16)),float(rng.uniform(.13,.38)),float(rng.uniform(.6,2.4)))
            for name in ("rear","front") for _ in range(108)]


def drops(im,cam,t,name):
    tex,top=crest_art(name)
    spray=np.zeros_like(im)
    for which,birth,life,u,vx,vy,r in flecks():
        if which!=name or not birth<t<birth+life: continue
        lo,hi,h,_,_=wave_shape(cam,name,birth)
        x=lo+(hi-lo)*u
        yy=wave_base(x,cam,name,birth)-h*(1-top[min(len(top)-1,int(u*len(top)))])
        age=(t-birth)/life
        xx=x+cam.radius*(vx*age-.013*age*age)
        yy+=cam.radius*(-vy*age+.40*age*age)
        light=(1-age)**.67*ease(0,.10,age)
        px=int(xx); py=int(yy)
        tone=tuple(int(ch) for ch in np.float32([229,199,146])*light)
        if r>1.7 and age<.72:
            cv2.line(spray,(px,py),(int(px+vx*7),int(py+(-vy+.8*age)*6)),tone,1,cv2.LINE_AA)
        else:
            cv2.circle(spray,(px,py),1,tone,-1,cv2.LINE_AA)
    overlay(im,spray)


def overlay(im,patch):
    cv2.add(im,patch,dst=im)


@lru_cache(None)
def clef(size):
    font=ImageFont.truetype("C:/Windows/Fonts/seguisym.ttf",size)
    bbox=font.getbbox("𝄞")
    im=Image.new("L",(bbox[2]-bbox[0]+6,bbox[3]-bbox[1]+6))
    ImageDraw.Draw(im).text((3-bbox[0],3-bbox[1]),"𝄞",font=font,fill=255)
    return np.asarray(im)


def staff_point(cam,angle,t,depth,track=0):
    return (cam.cx+cam.radius*.966*np.cos(angle),
            cam.cy+cam.ry*1.01*np.sin(angle)-cam.radius*((.223 if depth else .146)+.042*np.sin(angle*2.1-t*3)
            +.045*np.sin(angle*1.05+t*2.9))+track)


def note(im,x,y,kind,size,color):
    sz=max(1.,size); fg=tuple(int(v) for v in color)
    if kind=="clef":
        alpha=clef(max(20,int(sz*5.1)))
        ht,wd=alpha.shape
        xx,yy=int(x-wd/2),int(y-ht*.55)
        if xx<0 or yy<0 or xx+wd>W or yy+ht>H:return
        roi=im[yy:yy+ht,xx:xx+wd]
        np.maximum(roi,(alpha[:,:,None]*(np.asarray(color)/255.)).astype(np.uint8),out=roi)
        return
    for dx,dy in [(0,0)]+([(sz*1.30,-sz*.20)] if kind=="pair" else []):
        xx=x+dx;yy=y+dy
        cv2.ellipse(im,(round(xx),round(yy)),(max(2,round(sz*.41)),max(1,round(sz*.25))),-26,0,360,fg,-1,cv2.LINE_AA)
        cv2.line(im,(round(xx+.33*sz),round(yy-.08*sz)),(round(xx+.33*sz),round(yy-1.65*sz)),fg,max(1,round(sz*.105)),cv2.LINE_AA)
    if kind=="pair":
        cv2.line(im,(round(x+.33*sz),round(y-1.62*sz)),(round(x+1.64*sz),round(y-1.82*sz)),fg,max(2,round(sz*.32)),cv2.LINE_AA)
    if kind=="single":
        pts=np.int32([(x+.33*sz,y-1.65*sz),(x+.64*sz,y-1.39*sz),(x+.82*sz,y-1.07*sz),(x+.72*sz,y-.83*sz)])
        cv2.polylines(im,[pts],False,fg,max(1,round(sz*.16)),cv2.LINE_AA)


def score(im,cam,t,front):
    lay=np.zeros_like(im)
    time=ease(.16,.83,t)
    phase=(-.11+ .34*time + .20*ease(.87,1.38,t))
    if front:
        start,stop,appear,last=.19,2.80,.285,1.22
    else:
        start,stop,appear,last=3.76,5.98,.155,1.15
    reveal=ease(appear,appear+.30,t)
    if not reveal:return
    arc_alpha=(1-ease(.84,last,t))*ease(appear,appear+.10,t)
    pix=cam.radius/425
    spacing=(6.2 if front else 3.6)*pix
    for j in range(4*13):
        u=j/52
        if u>=reveal:break
        v=min((j+1)/52,reveal)
        af=start+(stop-start)*np.linspace(u,v,6)+phase
        taper=(.10+.90*np.sin(np.pi*(.02+.96*u))**.56)
        intensity=arc_alpha*taper*(1 if front else .71)
        color=tuple(int(k*intensity) for k in (190,141,76))
        for l in range(5):
            px,py=staff_point(cam,af,t,front,(l-2)*spacing)
            cv2.polylines(lay,[np.int32(np.rint(np.stack((px,py),axis=1)*16))],False,color,1,cv2.LINE_AA,4)
    notes=((.075,"single",0),(.256,"pair",-1),(.46,"quarter",1),(.635,"pair",-1),(.80,"single",1),(.905,"clef",0)) if front else ((.07,"clef",0),(.24,"single",1),(.46,"pair",-1),(.64,"single",1),(.80,"pair",-1))
    for u,kind,pitch in notes:
        reveal_note=ease(appear+u*.24,appear+u*.24+.10,t)
        if not reveal_note: continue
        life=1-ease(.88+u*.24,1.26+u*.23,t)
        if life<.002: continue
        tilt=(1-life)*.28
        a=start+(stop-start)*u+phase+tilt
        px,py=staff_point(cam,a,t,front,pitch*spacing)
        drift=(1-life)*cam.radius*(.10+.07*u)
        strength=life*reveal_note*(1 if front else .82)
        col=np.float32([254,220,151])*strength
        note(lay,float(px),float(py)-drift,kind,(13 if front else 10)*pix,col)
    base=cv2.GaussianBlur(lay,(0,0),2.8)
    overlay(im,cv2.addWeighted(lay,.86,base,.54,0))


def fourstar(im,x,y,rad,energy):
    if energy<.015 or rad<.6:return
    r=rad; xx=int(x); yy=int(y)
    m=max(3,int(2.8*r)+1)
    l=max(0,xx-m); top=max(0,yy-m); ri=min(W,xx+m+1); bot=min(H,yy+m+1)
    if l>=ri or top>=bot: return
    yq,xq=np.ogrid[top:bot,l:ri]; ax=np.abs(xq-x)/r; ay=np.abs(yq-y)/r
    q=(ax**.55+ay**.55)
    core=np.clip((1.-q)*4.2+.18,0,1)
    halo=np.exp(-1.65*(ax**2+ay**2))
    lit=core[:,:,None]*np.float32([255,246,205])+halo[:,:,None]*np.float32([91,58,19])
    roi=im[top:bot,l:ri]
    roi[:]=np.uint8(np.minimum(255,roi+lit*energy))


def stars(im,cam,t):
    pattern=[(-.57,-.02,11,.05,.29),(.28,-.20,7,.07,.36),(-.83,-.55,11,.25,.87),(.73,-.44,7,.39,1.1),
             (.80,.21,10,.58,.97),(-.60,.25,12,.63,1.18),(-.39,-.46,14,.89,1.51),(.34,-.83,10,1.02,1.62),
             (.06,.44,6,1.13,1.45),(-.69,.58,8,1.16,1.65)]
    for i,(x,y,rad,birth,end) in enumerate(pattern):
        u=(t-birth)/(end-birth)
        if not 0<u<1: continue
        amt=np.sin(np.pi*u)**1.1
        fourstar(im,cam.cx+cam.radius*(x+.085*u),cam.cy+cam.radius*(y-.12*u),
                 rad*(.56+.6*amt),float(amt))


@lru_cache(None)
def actors():
    return ((game.CASTER,-18.2,0.,True),(game.TARGET,5.1,1.,False),
            (game.ROOT/"AceTaffy/AceTaffyMod/Resource/CharacterSkin/AceTaffy_Invention_1_2",17,-1.15,False))


def battle_units(im,cam,t):
    for i,(asset,gx,gy,flip) in enumerate(actors()):
        root=np.array((cam.cx+gx*H/34,cam.cy-gy*H/34))
        a=window(.75,.80,.84,1.14,t) if i else window(.22,.64,.83,1.04,t)*.55
        grip=root+np.array((1 if flip else -1, -1))*np.array((1.92,2.52))*3*(H/34)
        move=root+np.array((4.7*a if i else 0,0))
        game.shadow(im,root,66 if not i else 88)
        game.draw_sprite(im,asset,"Damaged" if i and .75<=t<1.16 else "Default",move,flip,grip,a)
        if i and .75<t<1.05:
            u=(t-.75)/.30
            fourstar(im,grip[0],grip[1],14*(1-u)+4,window(.75,.775,.80,1.05,t))


@lru_cache(None)
def caption(cam,slow):
    im=Image.new("RGBA",(480,103)); d=ImageDraw.Draw(im)
    font=ImageFont.truetype("C:/Windows/Fonts/simkai.ttf",29)
    d.text((39,23),"汐音 · 光耀万海",font=font,fill=(235,221,194,245))
    f2=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",14)
    name="战斗镜头 · 常速" if cam.battle else ("潮涡细节 · 0.45× 慢放" if slow else "潮涡全景 · 常速")
    d.text((42,65),name,font=f2,fill=(175,167,147,235))
    return np.asarray(im)


def render(cam,t,slow=False,label=True):
    im=set_plate(cam)[0].copy()
    paint_pool(im,cam,t)
    if 0<t<END:
        score(im,cam,t,False); paint_wave(im,cam,"rear",t); drops(im,cam,t,"rear")
    if cam.battle: battle_units(im,cam,t)
    if 0<t<END:
        breaker(im,cam,t); paint_wave(im,cam,"front",t); breaker_splash(im,cam,t)
        drops(im,cam,t,"front"); score(im,cam,t,True); stars(im,cam,t)
    if label:
        top=caption(cam,slow); roi=im[:top.shape[0],:top.shape[1]]; a=top[:,:,3:4].astype(np.float32)/255
        roi[:]=roi*(1-a)+top[:,:,:3]*a
    return im


def stills():
    Image.fromarray(render(BEAUTY,.755,label=False)).save(OUT/"peak.png")
    Image.fromarray(render(BATTLE,.755)).save(OUT/"battle-peak.jpg",quality=94)
    sheet=Image.new("RGB",(1920,540))
    draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype("C:/Windows/Fonts/simkai.ttf",20)
    for i,(t,label) in enumerate(STAGES):
        dest=((i%4)*480,(i//4)*270)
        sheet.paste(Image.fromarray(render(BEAUTY,t,label=False)).resize((480,270),Image.Resampling.LANCZOS),dest)
        draw.text((dest[0]+10,dest[1]+9),f"{i+1:02}  {label}",font=font,fill=(244,224,173))
    sheet.save(OUT/"stages.jpg",quality=96)
    roll=Image.new("RGB",(1280,720))
    for i,t in enumerate((.859,.986,1.105,1.216)):
        roll.paste(Image.fromarray(render(BEAUTY,t,label=False)).resize((640,360),Image.Resampling.LANCZOS),
                   ((i%2)*640,(i//2)*360))
    roll.save(OUT/"06-breaker.jpg",quality=95)


def movie():
    final=OUT/"xiyin-golden-tide.mp4"; part=OUT/"xiyin-golden-tide.part.mp4"
    cmd=[imageio_ffmpeg.get_ffmpeg_exe(),"-nostdin","-hide_banner","-loglevel","error","-y","-f","rawvideo",
         "-pix_fmt","rgb24","-s",f"{W}x{H}","-r",str(FPS),"-i","-","-an","-c:v","libx264","-crf","15",
         "-preset","fast","-pix_fmt","yuv420p","-movflags","+faststart",str(part)]
    with subprocess.Popen(cmd,stdin=subprocess.PIPE) as proc,ThreadPoolExecutor(max_workers=3) as workers:
        for cam,speed,lead,duration in SHOTS:
            base=render(cam,-1,speed<1)
            def sample(i):
                t=(i/FPS-lead)*speed
                return base if t<0 or t>=END else render(cam,t,speed<1)
            count=round(duration*FPS)
            for k in range(0,count,9):
                for image in workers.map(sample,range(k,min(k+9,count))):
                    proc.stdin.write(image.tobytes())
        proc.stdin.close()
        if proc.wait()!=0: raise RuntimeError("FFmpeg failed")
    part.replace(final)


if __name__=="__main__":
    ap=argparse.ArgumentParser(description=__doc__); ap.add_argument("--stills",action="store_true")
    args=ap.parse_args(); cv2.setNumThreads(2); stills()
    if not args.stills:movie()
