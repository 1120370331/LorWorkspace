"""Focused delivery contract; authored before the production implementation.
Run from any cwd: python SteriaBuild/VFXSource/AnhierTextureCombat/verify_source.py
"""
from pathlib import Path
import hashlib, json, xml.etree.ElementTree as ET
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
SHIP = ROOT/'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
PROFILES = ['SeaPierce','SeaFarHit','MemorySlash','MemoryPierce','MemoryHit','MemoryGuard']

def verify():
    manifest = json.loads((HERE/'manifest.json').read_text(encoding='utf-8'))
    assert manifest['provenance']['kind'] == 'native-grounded-imagegen-phase-authored'
    assert set(manifest['profiles']) == set(PROFILES)
    assert set(manifest['masters']) == set(PROFILES)
    for entry in manifest['masters'].values():
        assert hashlib.sha256((HERE/entry['file']).read_bytes()).hexdigest()==entry['sha256']
    paths=set()
    for name in PROFILES:
        cfg = json.loads((SHIP/(name+'.json')).read_text(encoding='utf-8'))
        assert cfg['duration']==(.5 if name=='SeaFarHit' else 1.)
        assert cfg['frameTimes'][0] == 0 and cfg['frameTimes'][1] <= 1/30
        assert len(cfg['frameTimes']) == len(cfg['body']) == len(cfg['accent']) >= 7
        assert all(a < b for a,b in zip(cfg['frameTimes'],cfg['frameTimes'][1:]))
        areas=[]; digests=[]
        for role in ['body','accent']+(['splash'] if cfg.get('splash') else []):
            for file in cfg[role]:
                p=SHIP/file; paths.add(file)
                im=Image.open(p); assert im.mode=='RGBA', file
                a=np.asarray(im); alpha=a[:,:,3]
                assert np.max(alpha[:3])==0 and np.max(alpha[-3:])==0
                assert np.max(alpha[:,:3])==0 and np.max(alpha[:,-3:])==0
                assert np.count_nonzero(alpha==0) > alpha.size*.30
                assert p.read_bytes()==(HERE/'source_assets'/file).read_bytes()
                digest=hashlib.sha256(p.read_bytes()).hexdigest()
                assert manifest['assets'][file]['sha256']==digest
                if role=='body':
                    areas.append(float(alpha.sum())); digests.append(digest)
                    if name.startswith('Memory') and file==cfg['body'][2]:
                        dark=(np.max(a[:,:,:3],axis=2)<105)&(alpha>160)
                        assert np.count_nonzero(dark)>200, 'lost dark pigment: '+file
        assert areas[0]>=areas[1]*.85 and all(a>=areas[1]*.85 for a in areas[1:4]), (name,'native hold lost',areas)
        assert areas[-1]<areas[1], (name,'no late erosion')
        im=np.asarray(Image.open(SHIP/cfg['body'][1]));bb=Image.fromarray(im[:,:,3]).point(lambda a:255 if a>10 else 0).getbbox()
        multiplier=2 if name=='MemoryGuard' else 4
        span=((bb[2]-bb[0])/cfg['pixelsPerUnit']*multiplier,(bb[3]-bb[1])/cfg['pixelsPerUnit']*multiplier)
        wanted={'SeaPierce':(16.8,3.94),'SeaFarHit':(13.6,4.56),'MemorySlash':(10,14),'MemoryPierce':(16.8,4.53),'MemoryHit':(17.6,4.68),'MemoryGuard':(5.87,8.8)}[name]
        assert all(abs(a/b-1)<.15 for a,b in zip(span,wanted)), (name,span,wanted)
        assert cfg['pixelsPerUnit']>0 and 0<=cfg['pivotX']<=1 and 0<=cfg['pivotY']<=1
    assert paths == set(manifest['assets'])
    tree=ET.parse(ROOT/'SteriaBuild/SteriaModFolder/Data/CardInfo.xml')
    expected={9001001:['SeaFarHit'],9001002:['MemorySlash']*2,9001003:['MemoryGuard','MemorySlash'],
              9001004:['MemorySlash','MemoryHit','MemoryPierce',None],9001005:['MemorySlash']*2,
              9001006:['MemoryGuard']*2,9001007:['SeaPierce',None],9001008:['SeaFarHit',None],
              9001009:['MemorySlash','MemoryHit','MemoryGuard'],9001010:[None,'MemorySlash']}
    for card in tree.findall('Card'):
        cid=int(card.attrib['ID'])
        if cid not in expected: continue
        dice=card.findall('BehaviourList/Behaviour'); assert len(dice)==len(expected[cid])
        for die,profile in zip(dice,expected[cid]):
            assert die.get('EffectRes')==('Steria_AnhierTexture'+profile if profile else ''),(cid,die.attrib)
    adapter=(ROOT/'SteriaBuild/DiceAttackEffect_Steria_AnhierTextureCombat.cs').read_text(encoding='utf-8')
    core=(ROOT/'SteriaBuild/AnhierTextureTimeline.cs').read_text(encoding='utf-8')
    registry=(ROOT/'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')
    for name in PROFILES:
        assert registry.count('SteriaCustomEffects["Steria_AnhierTexture'+name+'"]')==1
        assert 'class DiceAttackEffect_Steria_AnhierTexture'+name in adapter
    assert 'GetTexture(' not in core and 'Sprites/Default' in core
    assert '.material =' not in core and '.material.' not in core
    assert 'PlaySound' not in adapter and 'ScreenShake' not in adapter
    assert manifest['decodedRgbaBytes'] <= 64*1024*1024
    assert 'base.Initialize(self, target' in adapter and 'base.SetScale(scaleFactor)' in adapter and 'base.SetDestroyTime(' in adapter
    assert 'sortingLayerName = "Particle"' in core and 'SetParent(parent, false)' in core
    for rejected in ['Mathf.Clamp(actorScale', '1f + .12f', 'Atan2', 'Vector3.Lerp', 'transform.position = target']:
        assert rejected not in core+adapter, rejected
    assert 'base.Update();' in adapter and '_bHasDamagedEffect = false' in adapter
    assert manifest['masters']['MemoryHit']['file'].endswith('memory/keyed/MemorySlash_master.png')
    assert 'native_vertical' in manifest['masters']['MemorySlash']['file']
    print('SOURCE_PASS: 6 profiles, RGBA source/shipping hashes, native content footprint, 250ms shape hold, dark pigment, exact 18 dice bindings, shared-material contract')

if __name__=='__main__': verify()
