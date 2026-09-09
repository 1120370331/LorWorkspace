"""Build managed native-contract fixture and exact native sprite/pose references.
Only the adapter/framework API is simulated. No Unity engine render is claimed.
"""
from pathlib import Path
import json,hashlib,shutil,sys
import numpy as np
from PIL import Image
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];FIX=HERE/'ManagedFixture';GEN=FIX/'Generated';GEN.mkdir(exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
source_hashes={}
def record(skins):
 (HERE/'previews').mkdir(exist_ok=True)
 (HERE/'previews/managed-source-hashes.json').write_text(json.dumps({'kind':'Managed native-contract simulation; not Unity GPU engine','sourceFiles':source_hashes,'originalNativeMethodBodiesCopied':True,'unityContractShimSha256':sha(FIX/'UnityContract.cs'),'fixtureSources':{str(p.relative_to(ROOT)).replace('\\','/'):sha(p) for p in [FIX/'RunContract.cs',FIX/'UnityContract.cs',FIX/'NativeContract.csproj']},'runtimeResources':{p.name:sha(p) for p in sorted((FIX/'Runtime/Resource/Effects/AnhierTextureCombat').glob('*'))},'skins':[{k:s[k] for k in ['name','bundle','sha256']} for s in skins]},indent=2),encoding='utf8')
for name in ['AnhierTextureTimeline.cs','DiceAttackEffect_Steria_AnhierTextureCombat.cs']:
 p=ROOT/'SteriaBuild'/name;shutil.copyfile(p,GEN/name);source_hashes[str(p.relative_to(ROOT))]=sha(p)
for name,path in [('NativeDiceAttackEffect.cs','Battle/DiceAttackEffect/DiceAttackEffect.cs'),('NativeRedHoodGun.cs','DiceAttackEffect_RedHoodMercenary_Gun.cs')]:
 p=ROOT/'SteriaBuild/Assembly-CSharp'/path;(GEN/name).write_text('using Object = UnityEngine.Object;\n'+p.read_text(encoding='utf8'),encoding='utf8');source_hashes[str(p.relative_to(ROOT))]=sha(p)
# Preserve exact native pivot routing body instead of maintaining another switch.
p=ROOT/'SteriaBuild/Assembly-CSharp/CharacterAppearance.cs';s=p.read_text(encoding='utf8');start=s.index('\tpublic Transform GetAtkEffectPivot(');end=s.index('\n\t// Token:',start)
base=(FIX/'UnityContract.cs').read_text(encoding='utf-8-sig');a=base.index('public '+('UnityEngine.Transform' if 'public UnityEngine.Transform GetAtkEffectPivot(' in base else 'Transform')+' GetAtkEffectPivot(');b=base.index('\n}\npublic class BattleSceneRoot',a);base=base[:a]+s[start:end]+base[b:];base=base if 'using UnityEngine;' in base else base.replace('using N=System.Numerics;','using N=System.Numerics;\nusing UnityEngine;');(GEN/'NativePivotRouting.txt').write_text(s[start:end],encoding='utf8');(FIX/'UnityContract.cs').write_text(base,encoding='utf8')
ship=ROOT/'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat';dest=FIX/'Runtime/Resource/Effects/AnhierTextureCombat';dest.mkdir(parents=True,exist_ok=True)
for p in ship.glob('*'):shutil.copyfile(p,dest/p.name)
# The prepared Unity reference extraction is shared by both runners, not new art.
reference=HERE/'UnityProject/Assets/Reference'
if '--reuse-references' in sys.argv:
 skins=json.loads((reference/'native-skins.json').read_text())['skins']
 assert (reference/'native-effects.json').exists(), 'Missing native reference cache; run prepare_preview.py once'
 record(skins)
 print('MANAGED_PREPARED: exact production/native methods, cached reference data reused; no game or Unity launch')
 sys.exit(0)
config=json.loads((reference/'fixture-config.json').read_text());sys.path.insert(0,str(ROOT/'output/music-dice-ui/python-deps'));import UnityPy
skins=[]
for skin,path in [('Bada',config['badaBundle']),('Fire',config['fireBundle'])]:
 env=UnityPy.load(path);obs={o.path_id:o for o in env.objects};trees={i:o.read_typetree() for i,o in obs.items() if o.type.name in ['GameObject','Transform','SpriteRenderer','MonoBehaviour']};trs={i:t for i,t in trees.items() if obs[i].type.name=='Transform'};gos={i:t for i,t in trees.items() if obs[i].type.name=='GameObject'};gotr={t['m_GameObject']['m_PathID']:i for i,t in trs.items()}
 def mat(i):
  if i==0:return np.eye(4)
  t=trs[i];x,y,z,w=[t['m_LocalRotation'][k] for k in 'xyzw'];r=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])
  m=np.eye(4);m[:3,:3]=r@np.diag([t['m_LocalScale'][k] for k in 'xyz']);m[:3,3]=[t['m_LocalPosition'][k] for k in 'xyz'];return mat(t['m_Father']['m_PathID'])@m
 appearance=next(t for t in trees.values() if 'atkEffectRoot' in t);motions=[]
 for i,t in trees.items():
  if 'actionDetail' not in t:continue
  action=t['actionDetail'];parts=[]
  for ss in t['motionSpriteSet']:
   rid=ss['sprRenderer']['m_PathID'];rd=trees[rid];sid=rd['m_Sprite']['m_PathID']
   if not sid:continue
   so=obs[sid].read();sp=obs[sid].read_typetree();im=so.image.convert('RGBA');name=f'{skin}-{action}-{rid}.png';im.save(reference/name);rect=sp['m_Rect'];pivot=sp['m_Pivot'];off=sp['m_RD']['textureRectOffset'];ppu=sp['m_PixelsToUnits']
   parts.append({'name':gos[rd['m_GameObject']['m_PathID']]['m_Name'],'file':name,'spriteType':ss['sprType'],'enabled':rd['m_Enabled'],'order':rd['m_SortingOrder'],'layer':rd['m_SortingLayerID'],'ppu':ppu,'pivot':[(pivot['x']*rect['width']-off['x'])/im.width,(pivot['y']*rect['height']-off['y'])/im.height],'matrix':mat(gotr[rd['m_GameObject']['m_PathID']]).tolist(),'sha256':sha(reference/name)})
  motions.append({'action':action,'parts':parts})
 skins.append({'name':skin,'bundle':path,'sha256':sha(Path(path)),'appearanceTransform':gotr[appearance['m_GameObject']['m_PathID']],'root':appearance['atkEffectRoot']['m_PathID'],'pivots':{k[-1]:v['m_PathID'] for k,v in appearance.items() if k.startswith('atkEffectPivot_')},'transforms':[{'id':i,'name':gos[t['m_GameObject']['m_PathID']]['m_Name'],'parent':t['m_Father']['m_PathID'],'position':t['m_LocalPosition'],'rotation':t['m_LocalRotation'],'scale':t['m_LocalScale']} for i,t in trs.items()],'motions':motions})
(reference/'native-skins.json').write_text(json.dumps({'skins':skins},indent=2),encoding='utf8')
record(skins)
print('MANAGED_PREPARED: exact production and native base methods, serialized actual Bada/Fire action graphs')
