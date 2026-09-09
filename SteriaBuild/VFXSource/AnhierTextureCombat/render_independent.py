"""Independent mannequin presentation of original effect assets.
Opaque figures and arm poses are authored here from geometric shapes; no game skin,
body sprite or skin-baked attack layer is used. All effect transforms come from the
compiled production adapter's managed contract fixture.
"""
from pathlib import Path
from functools import lru_cache
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;OUT=HERE/'previews';SHIP=HERE.parents[1]/'SteriaModFolder/Resource/Effects/AnhierTextureCombat'
DATA=json.loads((OUT/'independent-frames.json').read_text());W,H=720,405;P=H/69
SCREEN=np.array([[P,0,W/2],[0,-P,H/2+8.4*P],[0,0,1.]])
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
cache={}
@lru_cache(maxsize=128)
def load(file):return Image.open(SHIP/file).convert('RGBA')
def sprite(sr):
 key=(sr['file'],sr['ppu'],tuple(sr['pivot']),tuple(sr['matrix']))
 if key in cache:return cache[key]
 im=load(sr['file']);m=np.array(sr['matrix']).reshape(4,4).T
 local=np.array([[1/sr['ppu'],0,-sr['pivot'][0]*im.width/sr['ppu']],[0,-1/sr['ppu'],(1-sr['pivot'][1])*im.height/sr['ppu']],[0,0,1]])
 inv=np.linalg.inv(SCREEN@m[np.ix_([0,1,3],[0,1,3])]@local)
 out=im.transform((W,H),Image.Transform.AFFINE,tuple(inv[:2].flat),Image.Resampling.BICUBIC);cache[key]=out;return out

def mannequin(canvas,matrix,action,target=False):
 # Draw at 3x rasterization resolution for clean opaque capsule limbs.
 factor=3;im=Image.new('RGBA',(W*factor,H*factor));d=ImageDraw.Draw(im);m=np.array(matrix).reshape(4,4).T
 def pt(x,y):
  p=SCREEN@(m@np.array([x,y,0,1]))[[0,1,3]];return tuple(np.round(p[:2]*factor).astype(int))
 scale=np.linalg.norm(m[:3,1])*P*factor
 body=(122,143,162,255) if not target else (159,151,141,255);edge=(24,34,46,255);light=(197,209,216,255)
 def disk(p,r,color):
  x,y=pt(*p);r=r*scale;d.ellipse((x-r,y-r,x+r,y+r),fill=color)
 def limb(points,r,color):
  points=[pt(*p) for p in points];d.line(points,fill=edge,width=max(1,round((r*2+.10)*scale)),joint='curve');d.line(points,fill=color,width=max(1,round(r*2*scale)),joint='curve')
  for p in points[1:-1]:
   rr=r*scale;d.ellipse((p[0]-rr,p[1]-rr,p[0]+rr,p[1]+rr),fill=color)
 # Common original neutral humanoid: no hair, outfit, logo or game character pixels.
 limb([(-.27,1.9),(-.62,.92),(-.85,.16)],.19,body);limb([(.30,1.9),(.56,.93),(.98,.16)],.2,body)
 limb([(-1.1,.13),(-.56,.13)],.13,light);limb([(.74,.13),(1.25,.13)],.13,light)
 torso=[(-.46,1.7),(.50,1.7),(.62,2.8),(.36,3.25),(-.39,3.22),(-.62,2.78)]
 d.polygon([pt(*p) for p in torso],fill=body,outline=edge)
 limb([(0,3.05),(0,3.53)],.18,body);disk((0,3.97),.46,light)
 # A thin front face direction mark keeps the independent silhouettes legible.
 limb([(-.34,4.0),(-.23,4.0)],.04,edge)
 if action==1:
  limb([(.4,3.0),(-.5,2.6),(-1.4,2.25)],.18,body);limb([(-.35,3.0),(-1.0,3.15),(-1.42,2.85)],.17,light);hand=(-1.4,2.25)
 elif action==4:
  limb([(.4,3.0),(-.50,3.12),(-1.6,2.45)],.18,body);limb([(-.32,3.0),(-.6,2.4),(-1.25,2.3)],.16,light);hand=(-1.6,2.45)
 elif action==6:
  limb([(.4,3.0),(-.45,2.6),(-1.5,2.3)],.18,body);limb([(-.4,3.0),(.1,2.5),(.9,2.7)],.16,light);hand=(-1.5,2.3)
 else:
  limb([(.4,3.0),(-.6,2.72),(-1.65,2.55)],.18,body);limb([(-.35,3.0),(-.65,2.1),(-1.05,2.52)],.16,light);hand=(-1.65,2.55)
 disk(hand,.19,light)
 canvas.alpha_composite(im.resize((W,H),Image.Resampling.LANCZOS))

def scene(case):
 im=Image.new('RGBA',(W,H),(36,41,50,255));d=ImageDraw.Draw(im)
 ground=SCREEN[1,2];d.line((0,ground,W,ground),fill=(76,85,97),width=1)
 # Opaque original figures are rendered before the candidate's Particle layer.
 mannequin(im,case['selfMatrix'],case['action']);mannequin(im,case['targetMatrix'],1,True)
 return im

measure=[]
for case in DATA['cases']:
 cache.clear();name=case['profile'];size='large' if case['actorScale']==3 else 'small';folder=OUT/'independent'/(name+'_'+size);folder.mkdir(parents=True,exist_ok=True);base=scene(case)
 for i,frame in enumerate(case['frames']):
  im=base.copy()
  for sr in sorted(frame['layers'],key=lambda s:(s['sortingLayer']==-1396713383,s['order'])):
   if not sr['enabled'] or sr['alpha']<=0:continue
   assert sr['sortingLayer']==-1396713383,'Presentation must respect production front sorting'
   layer=sprite(sr)
   if sr['alpha']<.9999:layer=layer.copy();layer.putalpha(layer.getchannel('A').point(lambda a:round(a*sr['alpha'])))
   im.alpha_composite(layer)
   if i in [0,1,3,9,15,24,40]:
    a=np.array(load(sr['file']))[:,:,3];bb=Image.fromarray(a).point(lambda a:255 if a>10 else 0).getbbox();m=np.array(sr['matrix']).reshape(4,4).T;w,h=sr['size'];pts=[]
    for x,y in [(bb[0],bb[1]),(bb[0],bb[3]),(bb[2],bb[1]),(bb[2],bb[3])]:pts.append(m@np.array([(x-sr['pivot'][0]*w)/sr['ppu'],(h-y-sr['pivot'][1]*h)/sr['ppu'],0,1]))
    pts=np.array(pts);measure.append({'profile':name,'size':size,'frame':i,'time':frame['time'],'layer':sr['name'],'sortingLayer':sr['sortingLayer'],'alpha':sr['alpha'],'alphaMass':float(a.sum()/255)*sr['alpha'],'worldBounds':[float(pts[:,0].min()),float(pts[:,1].min()),float(pts[:,0].max()),float(pts[:,1].max())]})
  d=ImageDraw.Draw(im);d.rectangle((0,0,W,46),fill=(17,22,30));d.text((10,5),name+'  |  original independent effect',font=font,fill=(238,242,247));d.text((10,29),('LEFT  /  actor scale 3' if size=='large' else 'RIGHT  /  actor scale 2.25')+f'  |  t={frame["time"]:.3f}s  |  neutral mannequin simulation',font=small,fill=(167,186,204));im.convert('RGB').save(folder/f'{i:02}.png')
 print('INDEPENDENT_PREVIEW',name,size,flush=True)
(OUT/'independent-content-bounds.json').write_text(json.dumps({'kind':'original neutral rig fit; no game character pixels','camera':{'width':W,'height':H,'orthoSize':34.5,'position':[0,8.4],'sameForAll':True},'measurements':measure},indent=2))
