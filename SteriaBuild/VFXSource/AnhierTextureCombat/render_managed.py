"""Composite measured native pose sprites using matrices emitted by exact C# adapters.
This is a CPU native-contract simulation, not a Unity GPU render or game screenshot.
"""
from pathlib import Path
import json,hashlib
from functools import lru_cache
import numpy as np
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;OUT=HERE/'previews';REF=HERE/'UnityProject/Assets/Reference';SHIP=HERE.parents[1]/'SteriaModFolder/Resource/Effects/AnhierTextureCombat'
data=json.loads((OUT/'managed-frames.json').read_text());skins={x['name']:x for x in json.loads((REF/'native-skins.json').read_text())['skins']}
W,H=720,405;P=H/(23*3);world_to_screen=np.array([[P,0,W/2],[0,-P,H/2+2.8*3*P],[0,0,1.]])
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
@lru_cache(maxsize=512)
def image(file):return Image.open(file).convert('RGBA')
cache={}
def transformed(file,ppu,pivot,matrix):
 key=(str(file),ppu,tuple(pivot),tuple(np.asarray(matrix).flat))
 if key in cache:return cache[key]
 im=image(str(file));m=np.array(matrix).reshape(4,4);local=np.array([[1/ppu,0,-pivot[0]*im.width/ppu],[0,-1/ppu,(1-pivot[1])*im.height/ppu],[0,0,1]])
 affine=world_to_screen@m[np.ix_([0,1,3],[0,1,3])]@local;inv=np.linalg.inv(affine)
 rendered=im.transform((W,H),Image.Transform.AFFINE,tuple(inv[:2].flat),Image.Resampling.BICUBIC)
 cache[key]=rendered;return rendered

def actor(items,skin,action,left,x,map_scale=3):
 s=skins[skin];motion=next((x for x in s['motions'] if x['action']==action),next(x for x in s['motions'] if x['action']==0))
 root=np.diag([map_scale*(1 if left else -1),map_scale,map_scale,1.]);root[0,3]=x*map_scale
 # Type9 effect_atk is preserved, identically, in native and candidate panels.
 for r in sorted(motion['parts'],key=lambda r:r['order']):
  if r['enabled']:items.append((r['layer'],r['order'],transformed(REF/r['file'],r['ppu'],r['pivot'],root@np.array(r['matrix']))))

def scene(case):
 canvas=Image.new('RGBA',(W,H),(40,43,51,255));d=ImageDraw.Draw(canvas)
 for y in np.arange(-6,14,2):
  yy=world_to_screen[1,1]*(y*3)+world_to_screen[1,2];d.line((0,yy,W,yy),fill=(47,50,58))
 y=world_to_screen[1,2];d.line((0,y,W,y),fill=(80,85,96))
 items=[]
 actor(items,case['skin'],case['action'],case['left'],5 if case['left'] else -5)
 actor(items,'Bada',1,not case['left'],-6.5 if case['left'] else 6.5)
 return canvas,items

def layers(canvas,items,native=False,actor_items=()):
 composite=list(actor_items)
 # Native Bada/Philip measured sorting rules are preserved in the recorded metadata;
 # actor-part order here is equivalent for supplied non-overlapping source poses.
 for sr in sorted(items,key=lambda r:(r['sortingLayer']==-1396713383,r['order'])):
  if not sr['enabled'] or sr['alpha']<=0:continue
  p=(REF if native else SHIP)/sr['file'];im=transformed(p,sr['ppu'],sr['pivot'],np.array(sr['matrix']).reshape(4,4).T)
  if sr['alpha']<.9999:im=im.copy();im.putalpha(im.getchannel('A').point(lambda a:round(a*sr['alpha'])))
  composite.append((sr['sortingLayer'],sr['order'],im))
 for layer,order,im in sorted(composite,key=lambda r:(3 if r[0] in [-1396713383,2898253913] else 0,r[1])):canvas.alpha_composite(im)
 return canvas

def heading(im,title,subtitle):
 d=ImageDraw.Draw(im);d.rectangle((0,0,W,45),fill=(17,21,29));d.text((10,5),title,font=font,fill=(235,239,245));d.text((10,27),subtitle,font=small,fill=(170,187,205));return im

all_measures=[]
for case in data['cases']:
 cache.clear();name=case['profile'];face='LEFT' if case['left'] else 'RIGHT';folder=OUT/(name+('_left' if case['left'] else '_right'));folder.mkdir(exist_ok=True);base,actor_items=scene(case)
 for i in range(61):
  panels=[]
  for variant in ['native','candidate']:
   frame=case['variants'][variant][i];im=layers(base.copy(),frame['layers'],variant=='native',actor_items);label='NATIVE measured art + exact native class' if variant=='native' else 'CANDIDATE exact production adapter'
   heading(im,name+' | '+label,face+' | actual '+case['skin']+' action | map 3 | roll 1 | '+str(round(frame['time']*1000))+' ms | CPU simulation')
   panels.append(im)
  joined=Image.new('RGB',(W*2,H));joined.paste(panels[0],(0,0));joined.paste(panels[1],(W,0));joined.save(folder/f'ab_{i:02}.png')
 # Exact alpha footprint under emitted Transform, independent from full PNG bounds.
 for variant in ['native','candidate']:
  for i in [0,1,3,9,15,24,40]:
   record=case['variants'][variant][i]
   for sr in record['layers']:
    if not sr['enabled']:continue
    im=image(str((REF if variant=='native' else SHIP)/sr['file']));a=np.array(im)[:,:,3];points=np.argwhere(a>10);bb=[points[:,1].min(),points[:,0].min(),points[:,1].max()+1,points[:,0].max()+1];w,h=im.size;corners=[]
    m=np.array(sr['matrix']).reshape(4,4).T
    for x,y in [(bb[0],bb[1]),(bb[0],bb[3]),(bb[2],bb[1]),(bb[2],bb[3])]:corners.append(m@np.array([(x-sr['pivot'][0]*w)/sr['ppu'],(h-y-sr['pivot'][1]*h)/sr['ppu'],0,1]))
    corners=np.array(corners);all_measures.append({'profile':name,'facing':face,'variant':variant,'time':record['time'],'layer':sr['name'],'alpha':sr['alpha'],'alphaMass':float(a.sum())*sr['alpha']/255,'worldBounds':[corners[:,0].min(),corners[:,1].min(),corners[:,0].max(),corners[:,1].max()],'spriteHash':hashlib.sha256(((REF if variant=='native' else SHIP)/sr['file']).read_bytes()).hexdigest(),'matrix':sr['matrix']})
 # Baseline v1 H exact old source layer and its measured old placement, same scene/camera.
 if name=='MemoryHit' and case['left']:
  oldroot=OUT/'previous-v1/resources';p=json.loads((oldroot/'MemoryHit.json').read_text());im=layers(base.copy(),[],False,actor_items);target_matrix=np.array(case['variants']['candidate'][0]['layers'][0]['matrix']).reshape(4,4).T
  # Bada stable target root for mirrored target, world x=-6.5*3 + 2.2*3, y=2.34*3.
  m=np.diag([-1.75*1.12,1.75*1.12,1,1]);m[0,3]=(-6.5+2.2)*3;m[1,3]=2.34*3
  for role in ['body','accent']:im.alpha_composite(transformed(oldroot/p[role][2],p['pixelsPerUnit'],[p['pivotX'],p['pivotY']],m))
  heading(im,'PREVIOUS v1 | detached target starburst','Same Bada Hit pose, camera, map 3, roll 1 | frozen v1 source snapshot')
  old=Image.new('RGB',(W*3,H));old.paste(im,(0,0));ab=Image.open(folder/'ab_03.png');old.paste(ab,(W,0));old.save(OUT/'previous-native-candidate-hit.png')
 print('SIMULATION_FRAMES',name,face,flush=True)
(OUT/'content-bounds.json').write_text(json.dumps({'units':'world units; near inherits actor map3, far neutral SceneRoot no mapScale','camera':{'width':W,'height':H,'orthoSize':34.5,'center':[0,8.4],'sharedForAll':True},'measurements':all_measures},indent=2))
print('MANAGED_FRAMES_RENDERED: native/candidate shared camera + true poses, matrices from compiled production adapter')
