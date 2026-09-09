"""Build deployment phase PNGs from the durable, approved imagegen RGBA masters.

No native art pixels are imported. The primary colored brush is intact; only spatially
detached flecks are split off, into a disjoint layer, so no color is double composited.
Every frame has authored spatial coverage/deformation and disintegration, not a global fade.
"""
from pathlib import Path
import hashlib,json,math
from functools import lru_cache
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
SHIP=ROOT/'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
SRC=HERE/'source_assets';MASTERS=HERE/'imagegen_masters'
TIMES=[i/60 for i in [0,1,2,4,7,11,17,25]]

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

@lru_cache(maxsize=16)
def noise(size,seed):
    rng=np.random.default_rng(seed);w,h=size;fields=[]
    for n in [8,26,85]:
        field=Image.fromarray(rng.integers(0,256,(max(2,h//n),max(2,w//n)),dtype=np.uint8))
        fields.append(np.asarray(field.resize(size,Image.Resampling.BICUBIC),float)/255)
    return fields[0]*.18+fields[1]*.37+fields[2]*.45

def fit_master(path,name,splash=False):
    master=Image.open(path).convert('RGBA');bbox=master.getchannel('A').point(lambda a:255 if a>10 else 0).getbbox()
    im=master.crop(bbox)
    # Match native attack sprite scale while retaining antialiased brush filaments.
    # All six decoded profiles, including independent splash, remain below 48 MiB.
    limit=(512,400) if name not in ['MemoryHit','MemoryGuard'] else (300,360) if name=='MemoryGuard' else (336,336)
    if splash:limit=(280,280)
    ratio=min(limit[0]/im.width,limit[1]/im.height)
    im=im.resize((round(im.width*ratio),round(im.height*ratio)),Image.Resampling.LANCZOS)
    pad=24;canvas=Image.new('RGBA',(im.width+pad*2,im.height+pad*2));canvas.alpha_composite(im,(pad,pad))
    return canvas,bbox

def split_detached(im):
    a=np.asarray(im).copy();mask=a[:,:,3]>14;h,w=mask.shape
    seen=np.zeros((h,w),bool);parts=[]
    for y,x in zip(*np.nonzero(mask)):
        if seen[y,x]:continue
        stack=[(y,x)];seen[y,x]=True;part=[]
        while stack:
            yy,xx=stack.pop();part.append((yy,xx))
            for dy,dx in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(-1,1),(1,-1),(1,1)]:
                ny,nx=yy+dy,xx+dx
                if 0<=ny<h and 0<=nx<w and mask[ny,nx] and not seen[ny,nx]:seen[ny,nx]=True;stack.append((ny,nx))
        parts.append(part)
    largest=max(map(len,parts));separate=np.zeros((h,w),bool)
    for part in parts:
        if len(part)<max(18,largest*.008):
            for y,x in part:separate[y,x]=True
    # Include each detached fleck's antialiased fringe, while remaining disjoint.
    expanded=np.array(Image.fromarray(separate.astype('uint8')*255).filter(ImageFilter.MaxFilter(3)))>0
    body=a.copy();accent=np.zeros_like(a);accent[expanded]=a[expanded];body[expanded]=0
    return Image.fromarray(body),Image.fromarray(accent)

def phase(im,name,index,is_accent=False,splash=False):
    a=np.asarray(im).copy();h,w=a.shape[:2];y,x=np.indices((h,w));u=np.clip((x-24)/(w-48),0,1);v=np.clip((y-24)/(h-48),0,1)
    n=noise((w,h),816);r=noise((w,h),1021)
    if name=='MemoryGuard':progress=v
    elif name=='MemoryHit':progress=np.clip(np.hypot((u-.74)*1.65,(v-.48)*1.55),0,1)
    elif splash:progress=np.clip(np.hypot((u-.25)*1.65,(v-.8)*1.55),0,1)
    else:progress=u
    if index<2:
        extent=[.42,.74][index]
        if name=='MemoryGuard':visible=np.clip((extent-progress+(n-.5)*.08)*35,0,1)
        elif name=='MemoryHit' or splash:visible=np.clip((extent-progress+(n-.5)*.10)*26,0,1)
        else:visible=np.clip((progress-(1-extent)+(n-.5)*.1)*32,0,1)
    elif index==2:visible=np.ones((h,w))
    elif index==3:visible=np.clip((n-(.29 if name=='SeaFarHit' else .14))*12,0,1)
    else:
        retreat=[0,0,0,0,.17,.40,.66,.91][index]
        if name=='MemoryGuard':
            visible=np.clip((1-retreat-np.abs(progress-.46)*1.9+(n-.5)*.3)*16,0,1)
        elif name=='MemoryHit' or splash:
            visible=np.clip((progress-retreat*.9+(n-.5)*.27)*20,0,1)
        else:visible=np.clip((progress-retreat+(n-.5)*.23)*23,0,1)
        visible*=np.clip((r-[0,0,0,0,.13,.23,.35,.55][index])*11,0,1)
    # Luminescent contact loses heat before the remaining ink; dark alpha is intact.
    if name.startswith('Memory') and index>=4:
        hot=(a[:,:,0].astype(float)>a[:,:,2]*1.4)&(a[:,:,0]>160)&(a[:,:,1]>65)
        a[hot,:3]=(a[hot,:3].astype(float)*[1,1,1,1,.88,.58,.35,.18][index]).astype('uint8')
    a[:,:,3]=(a[:,:,3]*visible).astype('uint8')
    # Local flow/recoil deformation has zero displacement at the piercing contact
    # point. It is not a root-scale animation or identical complete texture swap.
    if index!=2:
        amount=[4,2,0,1.5,3,5,8,11][index]
        if name=='MemoryGuard':dx=amount*np.sin(v*10+index)*np.sin(v*math.pi);dy=amount*.25*np.sin(u*9)
        elif name=='MemoryHit' or splash:dx=(u-.5)*amount*2;dy=(v-.5)*amount*2
        else:dx=np.zeros_like(u);dy=amount*(1-u)*np.sin(u*11+index*.7)
        sx=np.clip(np.rint(x-dx).astype(int),0,w-1);sy=np.clip(np.rint(y-dy).astype(int),0,h-1)
        a=a[sy,sx]
    if is_accent and index>=4:a[:,:,3]=(a[:,:,3]*[1,1,1,1,.95,.85,.6,.2][index]).astype('uint8')
    a[a[:,:,3]==0]=0
    return Image.fromarray(a)

def main():
    inputs=json.loads((MASTERS/'inputs.json').read_text(encoding='utf-8'))
    manifest={'version':2,'provenance':{'kind':'imagegen-reference-guided-phase-authored','method':'reference-guided independent imagegen masters; chroma-key and despill by source artists; spatially disjoint primary/detached-fleck layers; authored local-flow phase deformation and tail erosion','inputManifest':'imagegen_masters/inputs.json','phaseSource':'import_masters.py','status':'candidate pending independent visual acceptance'},'profiles':{},'assets':{},'masters':{}}
    for name,entry in inputs.items():
        path=MASTERS/entry['master'];im,bbox=fit_master(path,name);body,accent=split_detached(im)
        w,h=im.size
        span=entry['worldSpan'];ppu=(h-48 if name=='MemoryGuard' else w-48)/span
        px=entry.get('pivotX',.15);py=entry.get('pivotY',.5)
        times=[i/60 for i in [0,1,2,3,5,8,13,20]] if name=='SeaFarHit' else TIMES
        cfg={'name':name,'duration':.40 if name=='SeaFarHit' else .45 if name=='MemoryHit' else .5,'pixelsPerUnit':ppu,'pivotX':px,'pivotY':py,'actorScale':1,'frameTimes':times,'body':[],'accent':[],'splash':[]}
        splashim=None
        if entry.get('splash'):splashim,_=fit_master(MASTERS/entry['splash'],name,True)
        manifest['masters'][name]={'file':'imagegen_masters/'+entry['master'],'sha256':digest(path),'alphaContentBbox':bbox,'fitCanvas':[w,h],'pixelsPerUnit':ppu,'worldSpan':span,'pivot':[px,py]}
        for i in range(8):
            layers={'body':phase(body,name,i),'accent':phase(accent,name,i,True)}
            if splashim is not None:layers['splash']=phase(splashim,name,i,True,True)
            for role,frame in layers.items():
                file=f'{name}_{role}_{i:02}.png';frame.save(SRC/file,optimize=True);(SHIP/file).write_bytes((SRC/file).read_bytes());cfg[role].append(file)
                manifest['assets'][file]={'sha256':digest(SRC/file),'width':frame.width,'height':frame.height,'role':role,'phase':i}
        if splashim is not None:
            cfg['splashPixelsPerUnit']=(splashim.width-48)/2.8
            cfg['splashPivotX']=entry.get('splashPivotX',.42);cfg['splashPivotY']=entry.get('splashPivotY',.5)
        (SHIP/(name+'.json')).write_text(json.dumps(cfg,indent=2)+'\n',encoding='utf-8');manifest['profiles'][name]=cfg
    manifest['decodedRgbaBytes']=sum(x['width']*x['height']*4 for x in manifest['assets'].values())
    manifest['cachePolicy']='lazy per-profile shared GPU textures/sprites; no retained CPU pixels; one shared alpha material; no per-instance material allocation'
    (HERE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print('IMAGEGEN_PHASES_READY',len(manifest['assets']),'frames,',round(manifest['decodedRgbaBytes']/1024**2,2),'MiB decoded RGBA')

if __name__=='__main__':main()
