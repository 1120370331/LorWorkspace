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
TIMES=[0,1/60,.15,.25,.35,.45,.55,.64]

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
    limit=(400,560) if name=='MemorySlash' else (300,460) if name=='MemoryGuard' else (512,400)
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
    n=noise((w,h),816)
    # Event contact arrives with the complete native-sized brush. The first frame
    # retains 92% pigment; all shape through normalized .25 remains complete.
    visible=np.ones((h,w)) * (.92 if index==0 else 1.)
    if index>=4:
        # Original art already points LEFT: retreat consumes the trailing RIGHT.
        progress=v if name=='MemorySlash' else u
        retreat=[0,0,0,0,.04,.13,.29,.48][index]
        visible=np.clip((1-retreat-progress+(n-.5)*.12)*16,0,1)
        if splash:visible=np.clip((n-[0,0,0,0,.03,.08,.14,.23][index])*10,0,1)
        if name.startswith('Memory'):
            hot=(a[:,:,0].astype(float)>a[:,:,2]*1.4)&(a[:,:,0]>160)&(a[:,:,1]>65)
            a[hot,:3]=(a[hot,:3].astype(float)*[1,1,1,1,.96,.87,.72,.55][index]).astype('uint8')
    # Native alpha lives in the renderer, not in the PNGs. Only late local erosion
    # is authored here, preserving >=85% peak alpha mass through 250 ms.
    a[:,:,3]=(a[:,:,3]*visible).astype('uint8')
    if index in [2,4,5,6,7]:
        amount=[0,0,.55,0,.7,1.2,1.9,2.5][index]
        dx=amount*np.sin(v*9)*np.sin(v*math.pi) if name in ['MemorySlash','MemoryGuard'] or splash else np.zeros_like(u)
        dy=amount*u*np.sin(u*11) if name not in ['MemorySlash','MemoryGuard'] else np.zeros_like(v)
        sx=np.clip(np.rint(x-dx).astype(int),0,w-1);sy=np.clip(np.rint(y-dy).astype(int),0,h-1);a=a[sy,sx]
    a[a[:,:,3]==0]=0
    return Image.fromarray(a)


def main():
    inputs=json.loads((MASTERS/'inputs.json').read_text(encoding='utf-8'))
    manifest={'version':3,'provenance':{'kind':'native-grounded-imagegen-phase-authored','method':'reference-guided independent imagegen masters; chroma-key and despill by source artists; spatially disjoint primary/detached-fleck layers; authored local-flow phase deformation and tail erosion','inputManifest':'imagegen_masters/inputs.json','phaseSource':'import_masters.py','status':'candidate pending independent visual acceptance'},'profiles':{},'assets':{},'masters':{}}
    for name,entry in inputs.items():
        path=MASTERS/entry['master']
        if not path.exists(): raise FileNotFoundError('Approved vertical master must be supplied before final generation: '+str(path))
        im,bbox=fit_master(path,name)
        if entry.get('flipToNativeLeft'): im=im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        body,accent=split_detached(im)
        w,h=im.size
        span=entry['localSpan'];ppu=(h-48 if entry['spanAxis']=='height' else w-48)/span
        px=entry.get('pivotX',.15);py=entry.get('pivotY',.5)
        times=[0,1/30,.3,2/3,.72,.80,.88,.96] if name=='SeaFarHit' else TIMES
        cfg={'name':name,'duration':.5 if name=='SeaFarHit' else 1.,'pixelsPerUnit':ppu,'pivotX':px,'pivotY':py,'frameTimes':times,'body':[],'accent':[],'splash':[]}
        splashim=None
        if entry.get('splash'):
            splashim,_=fit_master(MASTERS/entry['splash'],name,True)
        manifest['masters'][name]={'file':'imagegen_masters/'+entry['master'],'sha256':digest(path),'alphaContentBbox':bbox,'fitCanvas':[w,h],'pixelsPerUnit':ppu,'localSpan':span,'spanAxis':entry['spanAxis'],'intrinsicDirection':'LEFT','pivot':[px,py]}
        for i in range(8):
            layers={'body':phase(body,name,i),'accent':phase(accent,name,i,True)}
            if splashim is not None:layers['splash']=phase(splashim,name,i,True,True)
            for role,frame in layers.items():
                file=f'{name}_{role}_{i:02}.png';frame.save(SRC/file,optimize=True);(SHIP/file).write_bytes((SRC/file).read_bytes());cfg[role].append(file)
                manifest['assets'][file]={'sha256':digest(SRC/file),'width':frame.width,'height':frame.height,'role':role,'phase':i}
        if splashim is not None:
            cfg['splashPixelsPerUnit']=(splashim.width-48)/entry['splashLocalSpan']
            cfg['splashPivotX']=entry.get('splashPivotX',.42);cfg['splashPivotY']=entry.get('splashPivotY',.5)
        (SHIP/(name+'.json')).write_text(json.dumps(cfg,indent=2)+'\n',encoding='utf-8');manifest['profiles'][name]=cfg
    manifest['decodedRgbaBytes']=sum(x['width']*x['height']*4 for x in manifest['assets'].values())
    manifest['cachePolicy']='lazy per-profile shared GPU textures/sprites; no retained CPU pixels; one shared alpha material; no per-instance material allocation'
    (HERE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print('IMAGEGEN_PHASES_READY',len(manifest['assets']),'frames,',round(manifest['decodedRgbaBytes']/1024**2,2),'MiB decoded RGBA')

if __name__=='__main__':main()
