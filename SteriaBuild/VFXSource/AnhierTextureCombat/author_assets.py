"""Original phase-first raster painting. No game sprites or generated reference pixels are sampled.

The manually placed pressure paths define the six gestures; layered pigment, carved dry
bristles, individually placed fire tongues and foaming water ribs supply the material.
Frames alter the extent, pressure, tip posture and fracture mask instead of scaling a PNG.
"""
from pathlib import Path
import hashlib, json, math, shutil
from functools import lru_cache
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
# Superseded prototype is quarantined. Running it cannot overwrite the accepted
# ImageGen master inputs, final manifest, source frames or shipping resources.
EXPERIMENT=HERE/'experiments/procedural_v0'
SHIP=EXPERIMENT/'shipping_preview'
SRC=EXPERIMENT/'source_assets'
TIMES=[0,.0167,.0333,.0667,.1167,.1833,.2833,.4167]
PI=math.pi

def spline(points,n=220):
    a=np.array(points,dtype=float); out=[]
    for t in np.linspace(0,len(a)-1,n):
        i=min(int(t),len(a)-2);f=t-i
        p0,p1,p2,p3=a[max(0,i-1)],a[i],a[i+1],a[min(len(a)-1,i+2)]
        out.append(.5*((2*p1)+(-p0+p2)*f+(2*p0-5*p1+4*p2-p3)*f*f+(-p0+3*p1-3*p2+p3)*f*f*f))
    return np.array(out)

def stroke_mask(size,points,seed=1):
    a=spline(points);xy=a[:,:2];w=np.maximum(a[:,2],0)
    d=np.gradient(xy,axis=0);d/=np.maximum(np.linalg.norm(d,axis=1)[:,None],.001)
    normal=np.column_stack((-d[:,1],d[:,0])); t=np.linspace(0,1,len(a))
    # Unequal upper/lower bristle fringe, not a parallel offset outline.
    rough=1+.085*np.sin(t*95+seed)+.035*np.sin(t*241+seed*.2)
    left=xy+normal*(w*rough)[:,None]
    right=xy-normal*(w*(1+.10*np.sin(t*117+seed*.7)))[:,None]
    m=Image.new('L',size);ImageDraw.Draw(m).polygon([tuple(x) for x in np.vstack((left,right[::-1]))],fill=255)
    return m

@lru_cache(maxsize=160)
def noise(size,seed):
    rng=np.random.default_rng(seed);w,h=size
    fields=[]
    for n in [8,26,85]:
        f=Image.fromarray(rng.integers(0,256,(max(2,h//n),max(2,w//n)),dtype=np.uint8))
        fields.append(np.asarray(f.resize(size,Image.Resampling.BICUBIC),float)/255)
    return fields[0]*.18+fields[1]*.37+fields[2]*.45

def paint(mask,low,high,seed=1,grain=.12):
    size=mask.size;n=noise(size,seed); y,x=np.indices((size[1],size[0]))
    flow=.12*np.sin(x*.028+np.sin(y*.018)*3)+.08*np.sin(y*.10+x*.016)
    f=np.clip(n*.95+flow,0,1)[...,None]
    rgb=np.array(low)*(1-f)+np.array(high)*f
    a=np.asarray(mask,float)*(1-grain+grain*n)
    return Image.fromarray(np.dstack([rgb,a]).clip(0,255).astype('uint8'),'RGBA')

def over(base,im): base.alpha_composite(im)

def brush(base,points,lo,hi,seed=1):
    m=stroke_mask(base.size,points,seed);over(base,paint(m,lo,hi,seed));return m

def erase(base,points,seed=1,strength=1):
    m=np.asarray(stroke_mask(base.size,points,seed),float)/255
    a=np.asarray(base).copy();a[:,:,3]=(a[:,:,3]*(1-m*strength)).astype('uint8');base.paste(Image.fromarray(a))

def flame(base,x,y,length,angle,seed,hot=True):
    rng=np.random.default_rng(seed);d=np.array([math.cos(angle),math.sin(angle)]);n=np.array([-d[1],d[0]])
    pts=[]
    for t,w in [(0,.065),(.23,.12),(.49,.067),(.68,.079),(.85,.029),(1,0)]:
        pos=np.array([x,y])+d*length*t+n*length*(math.sin(t*8+seed)*.065*t)
        pts.append((*pos,length*w))
    brush(base,pts,(167,24,41),(251,79,24),seed)
    core=[(a,b,w*.42) for a,b,w in pts[:4]]+[(pts[4][0],pts[4][1],0)]
    brush(base,core,(255,106,27),(255,210,105),seed+2)
    if hot: brush(base,[(x,y,length*.025),(x+d[0]*length*.23,y+d[1]*length*.23,length*.020),(x+d[0]*length*.44,y+d[1]*length*.44,0)],(255,208,110),(255,242,210),seed+3)

def drop(base,x,y,rx,ry,sea,seed):
    m=Image.new('L',base.size);d=ImageDraw.Draw(m)
    d.ellipse((x-rx,y-ry,x+rx,y+ry),fill=230)
    over(base,paint(m,(12,70,123) if sea else (25,12,38),(88,193,226) if sea else (97,43,135),seed))
    if sea:
        d=ImageDraw.Draw(base);d.arc((x-rx,y-ry,x+rx,y+ry),205,320,fill=(200,244,250,230),width=max(1,int(rx*.3)))

def base_art(profile,frame):
    sea=profile.startswith('Sea');rng=np.random.default_rng(901)
    size=(1024,432) if profile in ['SeaPierce','SeaFarHit','MemoryPierce'] else (1024,512) if profile=='MemorySlash' else (640,768) if profile=='MemoryGuard' else (640,640)
    body=Image.new('RGBA',size);accent=Image.new('RGBA',size)
    wobble=math.sin(frame*1.7)*5
    ink=((13,8,25),(73,29,111));water=((8,55,110),(64,158,193))
    lo,hi=water if sea else ink
    if profile in ['SeaPierce','SeaFarHit','MemoryPierce']:
        if sea:
            gestures=[[(60,259,0),(210,215,20),(367,251,29),(550,207,33),(749,212,22),(952,198,0)],
                      [(134,314,0),(314,274,12),(488,274,19),(680,235,15),(918,199,0)],
                      [(250,154,0),(390,195,12),(508,176,22),(708,196,10),(941,198,0)]]
        else:
            gestures=[[(70,262,0),(260,247,14),(480,227,38),(690,214,28),(958,197,0)],
                      [(233,158,0),(443,188,19),(620,173,12),(811,192,11),(947,198,0)],
                      [(165,325,0),(367,289,13),(618,266,19),(849,210,10),(936,199,0)]]
        for j,p in enumerate(gestures): brush(body,[(x,y+wobble*(1-x/1024),w) for x,y,w in p],lo,hi,18+j)
        # Long interior cavities separate the three drawn currents / carbon tongues.
        for p in [[(250,245,0),(392,244,5),(551,227,8),(746,218,0)],[(378,276,0),(535,266,5),(700,239,0)]]: erase(body,p,strength=.98)
        if sea:
            for j in range(17):
                x=180+j*40;y=220+math.sin(j*1.2)*24;length=100+(j%4)*20
                brush(accent,[(x,y,0),(x+length*.33,y-12,2.5),(x+length*.68,y+5,3.2),(x+length,y-4,0)],(80,167,203),(207,242,249),j+2)
            brush(accent,[(650,207,0),(790,197,3),(935,199,1.4),(954,198,0)],(181,236,244),(250,251,243),14)
            # Water surface rolls have gaps and a restrained reflective edge.
            for j in range(7):
                x=200+j*87;y=254+math.sin(j)*25
                brush(accent,[(x,y,0),(x-14,y-12,2),(x+2,y-23,3.7),(x+26,y-11,1),(x+20,y+3,0)],(46,133,182),(168,226,239),42+j)
        else:
            for j,(x,y,length,angle) in enumerate([(460,214,87,3.55),(568,197,56,3.11),(602,221,106,3.4),(704,207,79,3.1),(768,200,144,3.35),(827,208,57,2.9),(887,197,105,3.24),(924,198,75,3.13)]):
                flame(accent,x,y,length*(1+.06*math.sin(frame+j)),angle,42+j,hot=x>820)
            flame(accent,948,197,113,PI+.05,77)
            for j in range(11):
                x=180+j*59;y=242+math.sin(j)*17
                brush(body,[(x,y,0),(x+55,y-14,2),(x+155,y-8,0)],(84,30,129),(160,70,186),j+9)
    elif profile=='MemorySlash':
        gestures=[[(74,213,0),(159,299,18),(320,347,44),(497,339,59),(686,283,42),(917,160,0)],
                  [(111,303,0),(240,373,13),(440,383,20),(662,318,15),(826,241,0)],
                  [(187,240,0),(344,308,21),(507,300,14),(693,253,7),(904,167,0)]]
        for j,p in enumerate(gestures):brush(body,[(x,y+wobble*(1-x/1024),w) for x,y,w in p],lo,hi,20+j)
        for p in [[(128,281,0),(262,340,8),(417,352,5),(544,324,0)],[(316,311,0),(483,328,6),(653,286,0)],[(444,374,0),(617,329,5),(773,264,0)]]:erase(body,p)
        for j in range(15):
            x=290+j*38;y=356-((x-350)/620)**2*170
            brush(body,[(x,y,0),(x+42,y+3,3),(x+106,y-20,0)],(63,23,105),(162,66,179),j+20)
        # Irregular merged burning patches, separated by charred gaps. The short
        # pale tip is dominant; no repeated tooth/flame train along the edge.
        brush(accent,[(555,309,0),(647,284,6),(735,258,10),(829,208,7),(909,169,0)],(155,24,45),(238,70,32),92)
        for j,(x,y,length,angle) in enumerate([(556,314,69,3.62),(636,291,126,3.18),(676,279,56,3.85),(760,244,148,3.43),(793,228,77,3.02),(853,201,110,3.70),(898,175,92,3.49)]):
            flame(accent,x,y,length*(1+.07*math.sin(frame+j)),angle,35+j,hot=x>830)
        for p in [[(560,300,0),(618,303,3),(661,283,0)],[(701,276,0),(749,265,3),(787,235,0)],[(784,217,0),(824,214,2),(865,188,0)]]:erase(accent,p,strength=.9)
        brush(accent,[(690,280,0),(804,226,2.1),(903,168,1.6),(919,159,0)],(252,112,58),(255,229,167),61)
    elif profile=='MemoryGuard':
        paths=[[(237,75,0),(376,142,19),(456,292,36),(455,432,34),(366,592,24),(226,685,0)],
               [(328,94,0),(443,214,10),(480,392,12),(420,542,9),(301,667,0)]]
        for j,p in enumerate(paths):brush(body,[(x+math.sin(frame)*2*math.sin(y/100),y,w) for x,y,w in p],lo,hi,15+j)
        erase(body,[(373,177,0),(434,300,6),(431,457,5),(363,574,0)])
        for j in range(10):
            y=159+j*43;x=462-((y-355)/330)**2*127
            brush(body,[(x-10,y-30,0),(x+2,y,3),(x-5,y+41,0)],(85,35,123),(151,67,166),j+5)
        for j in range(8):
            y=279+j*15;x=462+math.sin(j*.5)*4
            flame(accent,x,y,42+(j%3)*21,-.4-(j-3)*.18,31+j,hot=j in [3,4])
        brush(accent,[(459,260,0),(466,316,2),(466,363,3),(451,401,0)],(224,62,43),(255,203,133),33)
    else:
        # Off-centre impact: broad downward carbon smear plus six unequal recoils.
        paths=[[(172,160,0),(243,257,23),(321,329,60),(393,366,41),(526,429,0)],
               [(135,349,0),(249,347,24),(341,329,41),(493,289,0)],
               [(259,516,0),(311,415,21),(332,322,39),(386,150,0)],
               [(229,92,0),(293,209,16),(339,323,25),(448,469,0)],
               [(465,190,0),(412,273,19),(349,342,30),(506,515,0)]]
        for j,p in enumerate(paths):brush(body,[(x+math.sin(frame)*3*math.sin(y/70),y,w) for x,y,w in p],lo,hi,40+j)
        for p in [[(167,356,0),(259,365,8),(322,342,0)],[(355,367,0),(401,410,9),(474,463,0)],[(283,218,0),(312,284,5),(314,327,0)]]:erase(body,p)
        for j in range(15):
            angle=j*2.4;radius=20+(j%3)*12;x=343+math.cos(angle)*radius;y=324+math.sin(angle)*radius
            flame(accent,x,y,45+(j%6)*13,angle,40+j,hot=j%3==0)
        for j in range(6):
            a=j*1.11;x=348+math.cos(a)*126;y=329+math.sin(a)*131
            brush(body,[(x,y,0),(x+math.cos(a)*34,y+math.sin(a)*34,7),(x+math.cos(a)*76,y+math.sin(a)*76,0)],(27,12,40),(94,40,119),j)
    # Texture with many pressure-varied, clipped bristle cuts; not a uniform noise veil.
    rng=np.random.default_rng(735)
    for j in range(105 if profile!='MemoryGuard' else 72):
        x=rng.uniform(90,size[0]-90);y=rng.uniform(65,size[1]-65);ln=rng.uniform(9,58)
        erase(body,[(x,y,0),(x+ln*.4,y-3,rng.uniform(.4,1.4)),(x+ln,y-8,0)],j,.6)
    # Sparse original droplets/coal, biased towards the trailing side.
    for j in range(19):
        x=rng.uniform(84,size[0]-80);y=rng.uniform(70,size[1]-70)
        if profile=='MemoryGuard' and x<350:continue
        drop(body,x,y,rng.uniform(1.7,4.2),rng.uniform(2.5,6),sea,j)
    return body,accent

def phase(image,profile,frame,accent=False):
    a=np.asarray(image).copy().astype(float);h,w=a.shape[:2];y,x=np.indices((h,w));n=noise((w,h),665)
    if profile=='MemoryGuard':progress=np.clip((y-60)/(h-130),0,1)
    elif profile=='MemoryHit':progress=np.clip(np.hypot((x-342)*.9,y-329)/270,0,1)
    else:progress=np.clip((x-70)/(w-145),0,1)
    # Initial contact gesture is immediately visible, with complete peak on frame 2.
    if frame<2:
        limit=[.46,.79][frame]
        visible=np.clip((limit-progress+n*.055)*40,0,1) if profile in ['MemoryHit','MemoryGuard'] else np.clip((progress-(1-limit)+n*.055)*40,0,1)
    elif frame<=3: visible=np.ones((h,w))
    else:
        # Tail-to-tip erosion. Guard breaks first at ends, leaving the pressure scar;
        # impact loses the bright centre before the dark recoil fragments.
        q={4:.23,5:.49,6:.76,7:.95}[frame]
        if profile=='MemoryGuard':
            age=np.abs(progress-.45)*1.85
            visible=np.clip(((1-q)-age+(n-.5)*.27)*14,0,1)
        elif profile=='MemoryHit':
            visible=np.clip((progress-q*.84+(n-.5)*.26)*18,0,1)
            visible*=np.clip((1-q+.20-n*.35)*3,0,1)
        else:visible=np.clip((progress-q+(n-.5)*.20)*17,0,1)
        if frame>=5:
            # Dissolve channels open into separate fragments, not uniform opacity.
            visible*=np.clip((n-({5:.20,6:.36,7:.53}[frame]))*15,0,1)
    a[:,:,3]*=visible
    if accent and frame>=4:a[:,:,3]*=[1,1,1,1,.74,.43,.17,.06][frame]
    a[a[:,:,3]<.7]=0
    return Image.fromarray(a.clip(0,255).astype('uint8'),'RGBA')

def splash(frame):
    im=Image.new('RGBA',(512,512));rng=np.random.default_rng(345)
    q=[.36,.71,1,1.05,1.1,1.2,1.3,1.35][frame]
    for j in range(12):
        a=-1.5+j*.26;ln=(76+(j%4)*27)*q
        x,y=244,263
        pts=[(x,y,3),(x+math.cos(a)*ln*.45,y+math.sin(a)*ln*.45,9),(x+math.cos(a)*ln,y+math.sin(a)*ln,0)]
        brush(im,pts,(16,88,146),(129,220,240),j)
        brush(im,[(p[0],p[1],p[2]*.22) for p in pts],(129,221,236),(230,250,246),j+8)
    for j in range(24):
        a=rng.uniform(-2.1,1.7);r=rng.uniform(52,172)*q
        if frame>3 and j%3<frame-4:continue
        drop(im,246+math.cos(a)*r,263+math.sin(a)*r,2+(j%3),3+(j%5),True,j)
    if frame>=4:
        a=np.asarray(im).copy();n=noise(im.size,97);a[:,:,3]=(a[:,:,3]*np.clip((n-(frame-4)*.16)*3,0,1)*[1,1,1,1,.9,.7,.4,.1][frame]).astype('uint8');im=Image.fromarray(a)
    return im

def main():
    SRC.mkdir(parents=True,exist_ok=True);SHIP.mkdir(parents=True,exist_ok=True)
    specs={
        'SeaPierce':(140,.13,.5,1.00),'SeaFarHit':(180,.90,.5,.95),
        'MemorySlash':(145,.19,.49,1.00),'MemoryPierce':(153,.14,.5,1.00),
        'MemoryHit':(136,.51,.50,.92),'MemoryGuard':(166,.53,.5,.98)}
    manifest={'version':1,'provenance':{'kind':'original-direct-painting','tool':'Python/Pillow raster pressure brush + authored paths','seed':901,'source':'author_assets.py','reference_use':'native contact sheets and actual card artwork inspected; no image pixels imported'},'profiles':{},'assets':{}}
    for name,(ppu,px,py,scale) in specs.items():
        cfg={'name':name,'duration':.5 if name!='MemoryHit' else .45,'pixelsPerUnit':ppu*.5,'pivotX':px,'pivotY':py,'actorScale':scale,'frameTimes':TIMES,'body':[],'accent':[],'splash':[]}
        for frame in range(8):
            body,accent=base_art(name,frame)
            layers={'body':phase(body,name,frame),'accent':phase(accent,name,frame,True)}
            if name=='SeaFarHit':layers['splash']=splash(frame)
            for role,im in layers.items():
                im=im.resize((im.width//2,im.height//2),Image.Resampling.LANCZOS)
                file=f'{name}_{role}_{frame:02}.png';im.save(SRC/file,optimize=True);shutil.copyfile(SRC/file,SHIP/file);cfg[role].append(file)
                manifest['assets'][file]={'sha256':hashlib.sha256((SRC/file).read_bytes()).hexdigest(),'width':im.width,'height':im.height,'role':role,'phase':frame}
        (SHIP/(name+'.json')).write_text(json.dumps(cfg,indent=2)+'\n',encoding='utf-8')
        manifest['profiles'][name]=cfg
    manifest['decodedRgbaBytes']=sum(a['width']*a['height']*4 for a in manifest['assets'].values())
    manifest['cachePolicy']='lazy per-profile GPU texture/sprite cache; markNonReadable releases CPU pixels; one shared RGBA material; maximum all-six uncompressed RGBA bytes recorded above'
    (EXPERIMENT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    out=EXPERIMENT/'previews';out.mkdir(exist_ok=True)
    names=list(specs);sheet=Image.new('RGB',(1500,1100),(26,29,36));d=ImageDraw.Draw(sheet)
    for i,name in enumerate(names):
        x=(i%2)*750;y=(i//2)*365;d.text((x+18,y+12),name,fill=(224,227,237))
        b=Image.open(SRC/f'{name}_body_02.png');b.alpha_composite(Image.open(SRC/f'{name}_accent_02.png'));b.thumbnail((715,316))
        sheet.paste(b,(x+(750-b.width)//2,y+40+(316-b.height)//2),b)
    sheet.save(out/'authored-peak-contact.png')
    print(f'AUTHOR_PASS: {len(manifest["assets"])} original RGBA frames; source and shipping copies written')

if __name__=='__main__':main()
