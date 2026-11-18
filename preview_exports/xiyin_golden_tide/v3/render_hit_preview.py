"""Xiyin battle preview with contact-only, target-registered water and shared front-wave depth."""
import argparse
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path
import subprocess
import sys

import cv2
import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(OUT.parent/"v2"))
import render_tide_v2 as tide

stage=tide.stage
game=stage.game
W,H,FPS=tide.W,tide.H,tide.FPS
IMPACT=.75
END=stage.END
BOTH=(1,2)  # The representative native manager callback contains these two victims.
SHOT_TIMES=(("battle",-.33,1.,146),("details",.468,.36,166),("beauty",-.27,1.,137))


@dataclass(frozen=True)
class Unit:
    key:int
    folder:Path
    wx:float
    wy:float
    flip:bool

    def root(self,cam):
        return np.array((cam.cx+self.wx*H/34,cam.cy-self.wy*H/34))


UNITS=tuple(Unit(i,*data) for i,data in enumerate(stage.actors()))


@lru_cache(None)
def body(unit,motion):
    """Find the visible trunk in the loaded game pose, not the forward attack pivot."""
    art,pivot=game.sprite(unit.folder,motion,unit.flip)
    y,x=np.nonzero(art[:,:,3]>90)
    top,sole=np.quantile(y,[.005,.995]); height=sole-top
    widths=[]; centers=[]
    for level in (.34,.45,.55,.64):
        strip=x[np.abs(y-(top+level*height))<.026*height]
        if len(strip):
            left,right=np.quantile(strip,[.06,.94])
            centers.append(np.median(strip));widths.append(right-left)
    return (float(np.median(centers)-pivot[0]),float(sole-pivot[1]),
            float(np.median(widths)),float(height))


@dataclass(frozen=True)
class Contact:
    key:int
    seed:int
    cx:float
    foot:float
    width:float
    height:float
    sign:int


def results(cam,t,victims):
    """Map the manager's unique successful victims to local visuals, one per die/target."""
    if not IMPACT<=t<END:return {}
    valid=set(victims)
    records={}
    for unit in UNITS[1:]:
        if unit.key not in valid:continue
        root=unit.root(cam)
        offset,foot,b,h=body(unit,"Damaged")
        recoil=4.7*stage.window(IMPACT,.80,.84,1.14,t)
        sign=-1 if root[0]+offset>=cam.cx else 1
        records[unit.key]=Contact(unit.key,84+unit.key*17,root[0]+offset+sign*recoil,
                                 root[1]+foot,b,h,sign)
    return records


def draw_subject(scene,cam,t,unit,contact):
    root=unit.root(cam)
    feedback=stage.window(IMPACT,.80,.84,1.14,t) if contact else 0.
    if unit.key==0:feedback=.55*stage.window(.22,.64,.83,1.04,t)
    move=root+np.array(((contact.sign*4.7*feedback if contact else 0.),0.))
    lit=(np.array([contact.cx,contact.foot-.56*contact.height]) if contact else
         root+np.array(([1.92 if unit.flip else -1.92,-2.52]))*3.*H/34)
    pose="Damaged" if contact and t<1.16 else "Default"
    game.shadow(scene,root,66 if unit.key==0 else 88)
    game.draw_sprite(scene,unit.folder,pose,move,unit.flip,lit,feedback)


@lru_cache(None)
def outer_stream(unit):
    """Seed material on the actual initial shoal and trace its lane through the enemy formation."""
    import center_flow
    cam=tide.BATTLE; R=cam.radius
    root=unit.root(cam); offset,foot,b,h=body(unit,"Default")
    center=root[0]+offset
    side=1 if center>cam.cx else -1
    touch=np.array(((center+side*.411*b-cam.cx)/R,.444*h/R,
                    (root[1]+foot-cam.cy)/(cam.flatten*R)+.063*b/R))
    outward=touch[[0,2]]/np.linalg.norm(touch[[0,2]])
    birth=.155 if unit.key==2 else .206
    wet=np.concatenate([m[0] for m in center_flow.sample(birth)])
    anchor=outward*1.035
    distance=(wet[:,0]-anchor[0])**2+(wet[:,2]-anchor[1])**2+.9*wet[:,1]**2
    start=wet[np.argmin(distance)].astype(float)
    return start,touch,outward,birth,b/R,h/R


@lru_cache(None)
def stream_topology():
    rings,sides=43,14
    u,phi=np.meshgrid(np.linspace(0,1,rings),np.arange(sides)*2*np.pi/sides,indexing="ij")
    ix=np.arange((rings-1)*sides,dtype=np.int32); nx=ix//sides*sides+(ix+1)%sides
    faces=np.stack((ix,nx,ix+sides,nx,nx+sides,ix+sides),axis=1).reshape(-1,3)
    a=np.arange(sides,dtype=np.int32);z=a+(rings-1)*sides; zz=((a+1)%sides)+(rings-1)*sides
    ends=np.stack((a*0+rings*sides,(a+1)%sides,a,a*0+rings*sides+1,z,zz),axis=1).reshape(-1,3)
    return u,phi,np.concatenate((faces,ends))


@lru_cache(maxsize=12)
def stream_mesh(which,t):
    """A finite cohort of shallow water advances at all times, including the result frame."""
    unit=UNITS[which];start,touch,out,birth,b,h=outer_stream(unit)
    head=(t-birth)/(IMPACT-birth)
    tail=max(0.,head-.43)
    u,phi,faces=stream_topology()
    s=tail+(head-tail)*u
    right=np.array([-out[1],out[0]])
    # Tangential lead follows the original vortex. The heel and head have the same velocity
    # field; fluid does not appear all at once when the combat result is published.
    leading=np.minimum(1.,s);released=np.maximum(s-1.,0.)
    xz=(start[[0,2]][None,None,:]+leading[:,:,None]*(touch[[0,2]]-start[[0,2]])
        +right[None,None,:]*(.20*np.sin(np.pi*leading)*(1-leading))[:,:,None])
    # The outer lane curves around the shield's projecting flank before spilling inward.
    # Its heel still originates on the parent water mesh; no detached downstream tube is spawned.
    xz[:,:,0]+=out[0]*(.105 if which==2 else .036)*np.sin(np.pi*leading)*(1-leading)**.38
    radial_speed=max(.275,float((start[[0,2]]-touch[[0,2]]) @ out))
    xz-=out[None,None,:]*(released*radial_speed+.13*released**2)[:,:,None]
    # Passing flow travels behind the armor before falling into the central ebb.
    xz[:,:,1]-=b*.55*stage.ease(1.0,1.43,s)
    y=(.012+start[1]*.62)*(1-leading)+.012*leading+(.444*h-.012)*(leading**2.15)
    y*=1-.978*stage.ease(1.04,1.68,s)
    cap=np.maximum(0,np.sin(np.pi*u))**.70
    channel=.68+.19*np.cos(7.4*s-9*t+which)+.10*np.cos(15*s+.41*which)
    az=phi+(.21*np.sin(6*s+2*t))
    breadth=b*(.061 if which==2 else .079)*cap*(.66+.34*np.sin(np.pi*np.minimum(1.,s))**2)
    height=h*(.016+.032*leading**2)*cap*channel
    # Use the changing tangent of this channel: as it rises obliquely at the armor,
    # the two banks turn broadside to the camera, rather than collapsing to a vertical blade.
    tangent=np.gradient(xz,axis=0)
    normal2=np.stack([-tangent[:,:,1],tangent[:,:,0]],axis=-1)
    normal2/=np.maximum(1e-7,np.linalg.norm(normal2,axis=-1,keepdims=True))
    xz+=normal2*(np.sin(az)*breadth)[:,:,None]
    y+=height*np.cos(az)*(.66+.36*np.maximum(0.,np.sin(2*az+9*s-13*t))**2)
    verts=np.stack((xz[:,:,0],np.maximum(.003,y),xz[:,:,1]),axis=-1)
    # Color and UV drift with the moving packet. Neither uses a hit flag.
    foam=cap*np.maximum(0,np.cos(az))**1.6*(.17+.30*leading+.17*np.sin(9*s-3*t+phi)**2)
    uv=np.stack([u,.5+.46*np.cos(az)],axis=-1)
    return (np.ascontiguousarray(np.vstack((verts.reshape(-1,3),verts[[0,-1],0])),np.float32),faces,
            np.ascontiguousarray(np.vstack((uv.reshape(-1,2),uv[[0,-1],0])),np.float32),
            np.ascontiguousarray(np.r_[foam.ravel(),0.,0.],np.float32))


@lru_cache(maxsize=12)
def stream_surface(which,cam,t):
    import water_render as material
    co=np.sqrt(1-cam.flatten**2)
    verts,faces,uv,foam=stream_mesh(which,t)
    x,y,z=verts.T
    proj=np.float32(np.stack((cam.cx+cam.radius*x,cam.cy+cam.radius*(cam.flatten*z-co*y),
                              cam.flatten*y+co*z),axis=1))
    lo=np.maximum([0,0],np.floor(proj[:,:2].min(axis=0)-2).astype(int))
    hi=np.minimum([W,H],np.ceil(proj[:,:2].max(axis=0)+3).astype(int))
    if np.any(hi<=lo):return None
    proj[:,:2]-=lo
    normal,tangent=material._attributes(verts,faces,uv)
    traced=material._trace(proj,faces,normal,tangent,uv,foam,int(hi[0]-lo[0]),int(hi[1]-lo[1]))
    return lo,hi,traced


def draw_inflow(scene,cam,t,which,part):
    import water_render as material
    if not .155<t<1.17:return
    unit=UNITS[which]
    _,_,_,birth,_,_=outer_stream(unit)
    alpha=.62*stage.window(birth,birth+.125,.789,1.02,t)
    if alpha<.002:return
    traced=stream_surface(which,cam,t)
    if traced is None:return
    lo,hi,(near,far,depth,attributes)=traced
    original=scene.copy()
    material._shade(original,scene,int(lo[0]),int(lo[1]),cam.cx,cam.cy,cam.radius,cam.flatten,t,near,far,depth,
                    attributes,material._maps(),*material._color_tables())
    region=np.s_[lo[1]:hi[1],lo[0]:hi[0]]
    draw=near>-10.
    if part!="all":
        # The part nearer than the actor plane runs in front of the lower armor;
        # its rear half and the unconfirmed ground water are unconditionally world effects.
        _,sole,_,_=body(unit,"Default")
        z_plane=(unit.root(cam)[1]+sole-cam.cy)/(cam.radius*cam.flatten)
        ordinate=(np.arange(lo[1],hi[1])[:,None]-cam.cy)/cam.radius
        sprite_depth=(z_plane-cam.flatten*ordinate)/np.sqrt(1-cam.flatten**2)
        is_near=near>sprite_depth
        draw&=is_near if part=="near" else ~is_near
    u=attributes[:,:,6];v=attributes[:,:,7]
    cap=np.maximum(0.,np.sin(np.pi*np.clip(u,0,1)))**.68
    streak=(.61+.39*np.cos(u*14+v*12-4.7*t))**3
    surf=np.clip(attributes[:,:,8],0,1)*cap*(.35+.65*streak)
    # A thin patchy rim makes its moving front legible against the dark shield.
    crest=np.exp(-((v-.86-.06*np.sin(u*13-t*5))/.10)**2)*(.29+.71*streak)
    head=np.exp(-((u-.74)/.22)**2)
    freshet=(surf*(.61+.39*head)+crest*cap*(.35+.40*head))*alpha
    lit=scene[region].astype(np.float32)+freshet[:,:,None]*np.float32([156,133,93])
    blend=draw[:,:,None]*alpha
    scene[region]=np.uint8(np.clip(original[region]*(1-blend)+lit*blend,0,255))


def scene_frame(cam,t,victims=(),label=False):
    import hit_render
    scene=stage.set_plate(cam)[0].copy()
    active=0<t<END
    if active:
        tide.floor_water(scene,cam,t)
        hit_render.draw_field_floor(scene,cam,t)
        for i in (1,2):draw_inflow(scene,cam,t,i,"far" if cam.battle else "all")
        tide.score_back(scene,cam,t)
        hit_render.draw_field(scene,cam,t,"rear")
    contacts=results(cam,t,victims) if cam.battle else {}
    age=t-IMPACT
    show_hit=bool(contacts) and 0<=age<.66
    depth=hit_render.global_front_depth(cam,t,scene.shape) if show_hit else None
    def hit(c,layer,after=False):
        hit_render.draw_hit(scene,cam,age,c.seed,c.cx,c.foot,c.width,c.height,c.sign,
                            layer,front_depth=depth,after_world=after)
    # Each back arc can disappear behind its own sprite; units farther from the camera render first.
    subjects=sorted(UNITS,key=lambda u:u.root(cam)[1]) if cam.battle else ()
    for unit in subjects:
        c=contacts.get(unit.key)
        if c and show_hit:hit(c,"back")
        draw_subject(scene,cam,t,unit,c)
        if unit.key and active:draw_inflow(scene,cam,t,unit.key,"near")
        if c and show_hit:hit(c,"front")
    if active:
        hit_render.draw_field(scene,cam,t,"front")
        if show_hit:
            for unit in subjects:
                if unit.key in contacts:hit(contacts[unit.key],"front",True)
        stage.score(scene,cam,t,True);stage.stars(scene,cam,t)
    if label:apply_caption(scene,"battle" if cam.battle else "beauty")
    return scene


def render_battle(t,victims=BOTH,label=False):
    return scene_frame(tide.BATTLE,t,victims,label)


@lru_cache(None)
def text_panel(kind):
    layer=Image.new("RGBA",(W,105))
    d=ImageDraw.Draw(layer)
    font=ImageFont.truetype("C:/Windows/Fonts/simkai.ttf",29)
    face=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",16)
    if kind=="details":
        d.text((32,21),"机兵 · 浪击慢放",font=font,fill=(245,230,198,255))
        d.text((W//2+32,21),"盾兵 · 浪击慢放",font=font,fill=(245,230,198,255))
        for x in (32, W//2+32):
            d.text((x+1,62),"0.36×",font=face,fill=(197,190,176,240))
    else:
        d.text((39,23),"汐音 · 光耀万海",font=font,fill=(235,221,194,245))
        d.text((42,65),"双目标命中 · 常速" if kind=="battle" else "潮涡全景 · 常速",
               font=face,fill=(191,185,171,244))
    return np.asarray(layer)


def apply_caption(scene,kind):
    cap=text_panel(kind);a=cap[:,:,3:4].astype(np.float32)/255
    dst=scene[:cap.shape[0]]
    dst[:]=dst*(1-a)+cap[:,:,:3]*a


@lru_cache(None)
def close_rect(unit):
    cam=tide.BATTLE
    offset,foot,b,h=body(unit,"Damaged")
    center,ground=unit.root(cam)+[offset,foot]
    # Camera framing is locked for the whole shot. Both panels share the battle projection.
    # Keep at least half a torso width on the incoming (outer-field) side so the
    # viewer can follow the wave into the body before its forward branch turns in.
    native_width=max(366.,b*1.92)
    native_height=native_width*H/(W/2)
    outside=1. if center>=cam.cx else -1.
    x=max(0,min(W-native_width,center+outside*.13*b-native_width*.50))
    y=max(0,min(H-native_height,ground-native_height*.76))
    return (round(x),round(y),round(native_width),round(native_height))


def closeup(scene,label=True):
    panels=[]
    for unit in UNITS[1:]:
        x,y,w,h=close_rect(unit)
        panels.append(cv2.resize(scene[y:y+h,x:x+w],(W//2,H),interpolation=cv2.INTER_LANCZOS4))
    out=np.concatenate(panels,axis=1)
    out[:,W//2-1:W//2+1]=np.uint8([34,31,29])
    if label:apply_caption(out,"details")
    return out


def stills():
    at=(.739,.792,.874,.984,1.14,1.267)
    sheet=Image.new("RGB",(1440,540))
    d=ImageDraw.Draw(sheet);font=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",15)
    for i,t in enumerate(at):
        original=render_battle(t)
        detail=closeup(original,False)
        if i==3:
            Image.fromarray(original).save(OUT/"battle-impact.jpg",quality=96)
            Image.fromarray(detail).save(OUT/"hit-closeup.jpg",quality=96)
        x,y=i%3*480,i//3*270
        sheet.paste(Image.fromarray(detail).resize((480,270),Image.Resampling.LANCZOS),(x,y))
        d.text((x+10,y+9),f"{t:.3f} s",font=font,fill=(237,223,196))
    sheet.save(OUT/"target-hit-stages.jpg",quality=96)
    inward=Image.new("RGB",(1280,720));ink=ImageDraw.Draw(inward)
    for i,t in enumerate((.50,.75,1.00,1.25)):
        x,y=i%2*640,i//2*360
        image=Image.fromarray(scene_frame(tide.BEAUTY,t)).resize((640,360),Image.Resampling.LANCZOS)
        inward.paste(image,(x,y))
        ink.text((x+13,y+12),f"{t:.2f} s",font=font,fill=(237,223,196))
    inward.save(OUT/"inward-stages.jpg",quality=95)


def contact_strip():
    out=Image.new("RGB",(1440,760))
    d=ImageDraw.Draw(out);font=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",13)
    for k,n in enumerate(range(58,74)):
        t=SHOT_TIMES[0][1]+n/FPS
        frame=render_battle(t)
        detail=Image.fromarray(frame[323:553,802:1238]).resize((360,190),Image.Resampling.LANCZOS)
        x,y=k%4*360,k//4*190
        out.paste(detail,(x,y));d.text((x+6,y+5),f"F{n}  {t:.3f} s",font=font,fill=(245,233,207))
    out.save(OUT/"shield-contact-sequence.jpg",quality=95)


def verify_targets():
    cam=tide.BATTLE
    assert results(cam,.749,BOTH)=={}
    assert results(cam,.874,[0,1,1,18])==results(cam,.874,[1])
    assert tuple(results(cam,.874,BOTH))==(1,2)
    assert results(cam,END,BOTH)=={}
    none=render_battle(.874,());one=render_battle(.874,(1,));two=render_battle(.874,BOTH)
    for unit in UNITS[1:]:
        x,y,w,h=close_rect(unit)
        x+=w//4;w//=2
        around=np.s_[y:y+h,x:x+w]
        delta01=np.abs(none[around].astype(np.int16)-one[around].astype(np.int16)).max(-1)
        delta12=np.abs(two[around].astype(np.int16)-one[around].astype(np.int16)).max(-1)
        first=int((delta01>8).sum());second=int((delta12>8).sum())
        print("victim",unit.key,"left-only vs miss:",first,"both vs left-only:",second)
        if unit.key==1:assert first>250 and second<first*.07
        if unit.key==2:assert second>250 and first<second*.07


def frame_of(kind,t,add_label=True):
    if kind=="beauty":im=scene_frame(tide.BEAUTY,t)
    else:im=render_battle(t)
    if kind=="details":return closeup(im,add_label)
    if add_label:apply_caption(im,kind)
    return im


def movie():
    tmp=OUT/"xiyin-golden-tide-v3.part.mp4";dest=OUT/"xiyin-golden-tide-v3.mp4"
    run=[imageio_ffmpeg.get_ffmpeg_exe(),"-nostdin","-hide_banner","-loglevel","error","-y","-f","rawvideo",
         "-pix_fmt","rgb24","-s",f"{W}x{H}","-r",str(FPS),"-i","-","-an","-c:v","libx264",
         "-crf","15","-preset","fast","-pix_fmt","yuv420p","-movflags","+faststart",str(tmp)]
    render_battle(.87)
    with subprocess.Popen(run,stdin=subprocess.PIPE) as proc,ThreadPoolExecutor(max_workers=3) as workers:
        for kind,time0,speed,count in SHOT_TIMES:
            neutral=frame_of(kind,-1.)
            def sample(i):
                t=time0+i*speed/FPS
                return neutral if t<=0 or t>=END else frame_of(kind,t)
            for first in range(0,count,6):
                for im in workers.map(sample,range(first,min(count,first+6))):proc.stdin.write(im.tobytes())
            print(kind,"encoded",flush=True)
        proc.stdin.close()
        if proc.wait()!=0:raise RuntimeError("FFmpeg failed")
    tmp.replace(dest)


if __name__=="__main__":
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--stills",action="store_true")
    ap.add_argument("--movie-only",action="store_true")
    ap.add_argument("--verify-targets",action="store_true")
    ap.add_argument("--contact-strip",action="store_true")
    args=ap.parse_args();cv2.setNumThreads(2)
    if args.contact_strip:contact_strip()
    elif args.verify_targets:verify_targets()
    else:
        if not args.movie_only:stills()
        if not args.stills:movie()
