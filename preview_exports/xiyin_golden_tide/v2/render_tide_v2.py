"""Render the simulated mass-attack preview. Use --stills for contact sheets or --verify-spin for an optical measurement."""
import argparse
from concurrent.futures import ThreadPoolExecutor
from functools import lru_cache
from pathlib import Path
import subprocess
import sys

import cv2
import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
sys.path.insert(0,str(OUT.parent))
import render_tide as stage

W,H,FPS=stage.W,stage.H,stage.FPS
BEAUTY,BATTLE,SHOTS=stage.BEAUTY,stage.BATTLE,stage.SHOTS
ease,window=stage.ease,stage.window
SPIN=1.2  # rad/s, positive in projected world XZ


@lru_cache(None)
def pool_material():
    """A UV tile for submerged horizontal caustics. No upright water geometry is stored here."""
    size=1024
    yy,xx=np.indices((size,size),dtype=np.float32)
    x=(xx-size/2)/(size*.46); z=(yy-size/2)/(size*.46)
    r=np.hypot(x,z); theta=np.arctan2(z,x)
    plate=stage.pool_art()
    u=(.5+.46*x)*(plate.shape[1]-1)
    v=(.5+.46*z)*(plate.shape[0]-1)
    tex=cv2.remap(plate,u,v,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    loose=ease(.17,.73,r)
    phi=(theta+8.6*np.log1p(3.15*r)+loose*(.20*np.sin(5*theta+r*22)
         +.11*np.cos(11*theta-r*32)+.042*np.sin(25*theta+r*31)))
    fibers=np.zeros_like(r)
    for offset,extent,power in ((0.,.97,1.),(1.97,.85,.62),(4.31,.91,.40)):
        a=phi+offset
        dist=np.arctan2(np.sin(a),np.cos(a))
        thick=.043+.14*np.exp(-r*14)
        glint=.5+.5*np.cos(theta*3+offset*1.4+16*r)
        breakup=(.5+.5*np.sin(8*theta+32*r+offset))*(.61+.39*np.cos(13*theta-45*r+offset)**2)
        openness=(1-ease(.14,.58,r))+.63*ease(.14,.58,r)*ease(.38,.83,glint*breakup)
        fibers+=np.exp(-(dist/thick)**2)*openness*power*(1-ease(extent-.22,extent,r))
    # One slightly stronger short segment makes the exact angular velocity visible in playback.
    seed=np.exp(-((r-.120)/.050)**2)*np.exp(-((np.arctan2(np.sin(phi-.28),np.cos(phi-.28)))/.38)**2)
    hot=np.exp(-(r/.096)**2)*(.61+.32*np.cos(phi))+ .64*np.exp(-(r/.037)**2)
    c=(tex.astype(np.float32)*.90+fibers[:,:,None]*np.float32([173,116,44])
       +hot[:,:,None]*np.float32([84,52,19])+seed[:,:,None]*np.float32([95,74,40]))
    c*= (1-ease(.96,1.095,r))[:,:,None]
    return np.float32(c)


def pool_sample(rad,angle,t):
    outside=ease(.25,.68,rad)
    # The inverse material lookup applies exactly one rigid spin in the eye. Outer tides can
    # change pitch and radius without transferring acceleration or shear into the inner 25%.
    material_angle=angle-SPIN*t
    material_angle-=outside*(.48*ease(.22,.81,t)+.14*ease(.84,1.32,t)+.070*np.sin(8*rad-3.7*t))
    scale=1+outside*(-.135*(1-ease(0,.48,t))+.045*ease(.84,1.28,t))
    lookup_r=rad/scale
    tile=pool_material()
    u=(.5+.46*lookup_r*np.cos(material_angle))*tile.shape[1]
    v=(.5+.46*lookup_r*np.sin(material_angle))*tile.shape[0]
    return cv2.remap(tile,u.astype(np.float32),v.astype(np.float32),cv2.INTER_LINEAR,
                     borderMode=cv2.BORDER_CONSTANT)


@lru_cache(None)
def footprint(cam):
    x0=max(0,int(cam.cx-1.23*cam.radius)); x1=min(W,int(cam.cx+1.23*cam.radius)+1)
    y0=max(0,int(cam.cy-1.23*cam.ry)); y1=min(H,int(cam.cy+1.30*cam.ry)+1)
    xx=(stage.X[y0:y1,x0:x1]-cam.cx)/cam.radius
    zz=(stage.Y[y0:y1,x0:x1]-cam.cy)/cam.ry
    return (np.s_[y0:y1,x0:x1],np.hypot(xx,zz),np.arctan2(zz,xx),xx,zz)


def floor_water(im,cam,t):
    rect,r,a,x,z=footprint(cam)
    env=window(0,.115,1.37,stage.END,t)
    if env<.001:return
    peak=window(.14,.73,.86,1.45,t)
    film=pool_sample(r,a,t)
    # The glaze is confined to the wet exterior. The central UV field remains rigid.
    stone=stage.set_plate(cam)[1][rect]
    wet=(.12+.88*ease(.43,.80,stone))*(.45+.55*np.cos(a*7+r*10+t*1.5)**2)
    wet*=np.exp(-((r-.96)/.175)**2)
    glint=wet*peak*(.22+.65*np.exp(-((z-.83)/.52)**2))*(.42+.58*np.sin(3.6*a+.2)**2)
    field=env*((.67+.39*peak)*film+glint[:,:,None]*np.float32([72,47,18]))
    dest=im[rect]
    dest[:]=np.rint(np.clip(dest+field,0,255)).astype(np.uint8)


def verify_spin():
    """Recover the signed rotation optically, independently at three radii and four times."""
    angles=np.arange(4096,dtype=np.float32)*np.float32(2*np.pi/4096)
    radii=np.array([.105,.155,.216],np.float32)
    r,a=np.meshgrid(radii,angles,indexing="ij")
    times=(.18,.43,.68,.93)
    previous=None
    for t in times:
        field=pool_sample(r,a,t) @ np.float32([.27,.59,.14])
        if previous is not None:
            shifts=[]
            for old,new in zip(previous,field):
                corr=np.fft.irfft(np.fft.rfft(new-new.mean())*np.conjugate(np.fft.rfft(old-old.mean())))
                idx=int(np.argmax(corr)); signed=idx if idx<len(angles)//2 else idx-len(angles)
                shifts.append(signed*(2*np.pi/len(angles)))
            if np.max(np.abs(np.asarray(shifts)-.25*SPIN))>.004:
                raise RuntimeError(f"Eye rotation deviated at {t}: {shifts}")
            print(f"{t-.25:.2f} -> {t:.2f} s  ",", ".join(f"{v:.5f} rad" for v in shifts))
        previous=field


def normalize(v):
    return v/np.maximum(np.linalg.norm(v,axis=-1,keepdims=True),1e-7)


def band_points(cam,angles,t,line=0):
    """Projected rear ribbon and its angle-dependent light response in three dimensions."""
    theta=np.asarray(angles)
    roll=.24+.35*np.sin(theta*1.08+.46)+.17*np.cos(theta*2.38-1.23*t)
    spacing=(.0138 if not cam.battle else .0125)
    h=.160+.045*np.sin(1.86*theta-1.95*t)+.018*np.sin(2.9*theta+.55*t)
    r=.963+.014*np.sin(1.17*theta+.72)
    sn=np.sin(theta);cs=np.cos(theta)
    inside=r+line*spacing*np.sin(roll)
    yy=h+line*spacing*np.cos(roll)
    s=np.sqrt(1-cam.flatten**2)
    px=cam.cx+cam.radius*inside*cs
    py=cam.cy+cam.radius*(cam.flatten*inside*sn-s*yy)
    tan=normalize(np.stack((-sn,.079*np.cos(1.86*theta-1.95*t),cs),axis=-1))
    cross=normalize(np.stack((cs*np.sin(roll),np.cos(roll),sn*np.sin(roll)),axis=-1))
    n=normalize(np.cross(tan,cross))
    view=np.array([0.,cam.flatten,s])
    light=normalize(np.array([-.65,.74,.18]))
    halfway=normalize(light+view)
    lambert=np.abs(n @ light)
    ndv=np.abs(n @ view)
    sheen=np.clip(np.abs(n @ halfway),0,1)**13
    rim=(1-ndv)**2.1
    distance=ease(-1.,-.03,sn)
    # Thin, partially transmissive lines: a broad diffuse value and a short sun-facing glint.
    response=(.19+.28*lambert+.18*rim+.72*sheen)*(.67+.33*distance)
    return px,py,response,sheen,distance


def back_note(layer,x,y,kind,size,body,specular):
    """Local embossed rim of a score glyph catches the same directional light as its ribbon."""
    sz=max(32,int(size*3.7)); mask=np.zeros((2*sz+1,2*sz+1,3),np.uint8)
    stage.note(mask,sz,sz,kind,size,(255,255,255))
    a=mask[:,:,0].astype(np.float32)/255
    eps=cv2.GaussianBlur(a,(0,0),.92)
    dx=cv2.Sobel(eps,cv2.CV_32F,1,0,ksize=3,scale=.25)
    dy=cv2.Sobel(eps,cv2.CV_32F,0,1,ksize=3,scale=.25)
    edge=np.clip(-dx*.64-dy*.74,-.65,.78)
    lit=a[:,:,None]*(body*(.84+.69*edge[:,:,None])
            +specular*np.maximum(edge,0)[:,:,None]*np.float32([64,80,92]))
    x0=round(x)-sz; y0=round(y)-sz
    xa=max(0,x0); xb=min(W,x0+len(mask)); ya=max(0,y0);yb=min(H,y0+len(mask))
    if xa<xb and ya<yb:
        roi=layer[ya:yb,xa:xb]
        np.maximum(roi,np.uint8(np.clip(lit[ya-y0:yb-y0,xa-x0:xb-x0],0,255)),out=roi)


def score_back(im,cam,t):
    fade=window(.157,.31,.92,1.20,t)
    if t<=.157 or t>=1.48:return
    start,stop=3.70,6.00
    spin=.22*ease(.18,.83,t)+.16*ease(.87,1.32,t)
    reveal=ease(.157,.508,t)
    lay=np.zeros_like(im)
    for line in range(-2,3):
        u=np.linspace(0,reveal,100)
        theta=start+(stop-start)*u+spin
        x,y,light,spec,depth=band_points(cam,theta,t,line)
        p=np.int32(np.rint(np.stack((x,y),axis=1)*16))
        edge=.12+.88*np.sin(np.pi*(.025+.95*u))**.63
        tint=(np.float32([173,117,61])+spec[:,None]*[43,62,79]+depth[:,None]*[13,9,5])
        colors=np.clip(fade*edge[:,None]*light[:,None]*tint,0,255).astype(np.uint8)
        for i in range(len(p)-1):
            cv2.line(lay,tuple(p[i]),tuple(p[i+1]),tuple(int(c) for c in colors[i]),1,cv2.LINE_AA,4)
    for u,kind,pitch in ((.095,"clef",0),(.265,"single",1),(.475,"pair",-1),(.69,"quarter",1),(.843,"single",-1)):
        gain=window(.16+u*.22,.24+u*.22,.90+u*.18,1.30+u*.16,t)
        if gain<.003:continue
        drift=ease(.92+u*.18,1.30+u*.16,t)
        a=np.array([start+(stop-start)*u+spin+.20*drift])
        x,y,light,spec,depth=band_points(cam,a,t,pitch)
        y=y[0]-cam.radius*.09*drift
        size=10.5*(cam.radius/425)*(.87+.16*depth[0])
        color=(np.float32([192.,147.,85.])+spec[0]*np.float32([28,42,60]))*gain*(.41+.70*light[0])
        back_note(lay,x[0],y,kind,size,color,gain*(.30+.70*spec[0]))
    # Modest local radiance; no uniform halo at the far side of the arc.
    cv2.add(im,cv2.addWeighted(lay,.96,cv2.GaussianBlur(lay,(0,0),1.54),.28,0),dst=im)


def render(cam,t,slow=False,label=True):
    import water_render
    im=stage.set_plate(cam)[0].copy()
    active=0<t<stage.END
    if active:
        floor_water(im,cam,t)
        water_render.draw_floor_response(im,cam,t)
        score_back(im,cam,t)
        water_render.draw_water(im,cam,t,"rear")
    if cam.battle:stage.battle_units(im,cam,t)
    if active:
        water_render.draw_water(im,cam,t,"front")
        stage.score(im,cam,t,True)
        stage.stars(im,cam,t)
    if label:
        cap=stage.caption(cam,slow);roi=im[:cap.shape[0],:cap.shape[1]]
        a=cap[:,:,3:4].astype(np.float32)/255.
        roi[:]=roi*(1-a)+cap[:,:,:3]*a
    return im


def geometry_study(t=.75):
    """Neutral-light clay pass and final shading from exactly the same simulated vertices."""
    import water_render
    import wave_sim
    cam=BEAUTY
    clay=np.full((H,W,3),(23,25,30),np.uint8)
    zbuffer=np.full((H,W),-20.,np.float32)
    slope=np.sqrt(1-cam.flatten**2)
    for pos,tri,uv,foam in wave_sim.sample(t):
        x,y,z=pos.T
        projection=np.stack((cam.cx+cam.radius*x,cam.cy+cam.radius*(cam.flatten*z-slope*y),
                             cam.flatten*y+slope*z),axis=1)
        normals,tangents=water_render._attributes(pos,tri,uv)
        near,_,_,attributes=water_render._trace(projection,tri,normals,tangents,uv,foam,W,H)
        visible=(near>zbuffer)&(near>-10)
        zbuffer[visible]=near[visible]
        n=normalize(attributes[:,:,:3])
        light=.55+.43*(n @ normalize(np.float32([-.51,.78,.36])))
        grid_uv=attributes[:,:,6:8]*np.float32([25.,10.])
        cell=np.min(np.abs(grid_uv-np.rint(grid_uv)),axis=2)
        wire=.81+.19*ease(.006,.044,cell)
        mat=light[:,:,None]*wire[:,:,None]*np.float32([205,209,211])
        clay[visible]=mat[visible]
    mat=Image.fromarray(render(BEAUTY,t,label=False))
    other=Image.fromarray(clay)
    crop=(176,36,1100,622)
    side=(616,390); output=Image.new("RGB",(side[0]*2,side[1]+37),(24,25,29))
    for i,frame in enumerate((other,mat)):
        output.paste(frame.crop(crop).resize(side,Image.Resampling.LANCZOS),(side[0]*i,0))
    draw=ImageDraw.Draw(output)
    f=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",15)
    draw.text((14,398),"三维水体表面 · 出手后 0.75 秒",font=f,fill=(211,209,205))
    draw.text((630,398),"同一网格的材质渲染 · 视频 1.05 秒",font=f,fill=(226,214,183))
    output.save(OUT/"geometry-study.jpg",quality=95)


def stills():
    timeline=Image.new("RGB",(1920,540))
    text=ImageDraw.Draw(timeline); f=ImageFont.truetype("C:/Windows/Fonts/simkai.ttf",20)
    for i,(t,caption) in enumerate(stage.STAGES):
        im=Image.fromarray(render(BEAUTY,t,label=False))
        if i==4:im.save(OUT/"peak.png")
        x,y=(i%4)*480,(i//4)*270
        timeline.paste(im.resize((480,270),Image.Resampling.LANCZOS),(x,y))
        text.text((x+10,y+9),f"{i+1:02}  {caption}",font=f,fill=(244,224,173))
    timeline.save(OUT/"stages.jpg",quality=96)
    Image.fromarray(render(BATTLE,.78)).save(OUT/"battle-peak.jpg",quality=95)
    geometry_study()


def movie():
    src=OUT/"xiyin-golden-tide-v2.part.mp4"; dest=OUT/"xiyin-golden-tide-v2.mp4"
    cmd=[imageio_ffmpeg.get_ffmpeg_exe(),"-hide_banner","-loglevel","error","-nostdin","-y",
         "-f","rawvideo","-pix_fmt","rgb24","-s",f"{W}x{H}","-r",str(FPS),"-i","-","-an","-c:v","libx264",
         "-preset","fast","-crf","15","-pix_fmt","yuv420p","-movflags","+faststart",str(src)]
    render(BEAUTY,.755,label=False)  # Prime JIT and the simulation on a single thread.
    with subprocess.Popen(cmd,stdin=subprocess.PIPE) as proc, ThreadPoolExecutor(max_workers=3) as workers:
        for i,(cam,speed,lead,dur) in enumerate(SHOTS):
            neutral=render(cam,-1.,speed<1)
            def sample(n):
                t=(n/FPS-lead)*speed
                return neutral if t<=0 or t>=stage.END else render(cam,t,speed<1)
            count=round(dur*FPS)
            for k in range(0,count,6):
                for frame in workers.map(sample,range(k,min(k+6,count))):
                    proc.stdin.write(frame.tobytes())
            print(f"Encoded shot {i+1}",flush=True)
        proc.stdin.close()
        if proc.wait()!=0:raise RuntimeError("FFmpeg encoder failed")
    src.replace(dest)


if __name__=="__main__":
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--stills",action="store_true")
    ap.add_argument("--movie-only",action="store_true")
    ap.add_argument("--verify-spin",action="store_true")
    args=ap.parse_args()
    cv2.setNumThreads(2)
    if args.verify_spin:
        verify_spin()
    else:
        if not args.movie_only:stills()
        if not args.stills:movie()
