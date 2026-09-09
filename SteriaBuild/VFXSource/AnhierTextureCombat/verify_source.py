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
    assert manifest['provenance']['kind'] == 'imagegen-reference-guided-phase-authored'
    assert set(manifest['profiles']) == set(PROFILES)
    assert set(manifest['masters']) == set(PROFILES)
    for entry in manifest['masters'].values():
        assert hashlib.sha256((HERE/entry['file']).read_bytes()).hexdigest()==entry['sha256']
    paths=set()
    for name in PROFILES:
        cfg = json.loads((SHIP/(name+'.json')).read_text(encoding='utf-8'))
        assert .35 <= cfg['duration'] <= .60
        assert cfg['frameTimes'][0] == 0 and .016 <= cfg['frameTimes'][2] <= .05
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
                assert np.count_nonzero(alpha==0) > alpha.size*.45
                assert p.read_bytes()==(HERE/'source_assets'/file).read_bytes()
                digest=hashlib.sha256(p.read_bytes()).hexdigest()
                assert manifest['assets'][file]['sha256']==digest
                if role=='body':
                    areas.append(np.count_nonzero(alpha>40)); digests.append(digest)
                    if name.startswith('Memory') and file==cfg['body'][2]:
                        dark=(np.max(a[:,:,:3],axis=2)<105)&(alpha>160)
                        assert np.count_nonzero(dark)>200, 'lost dark pigment: '+file
        assert len(set(digests))==len(digests), name+' uses duplicate frames'
        assert areas[0]<areas[2] and areas[-1]<areas[2]*.35, (name,areas)
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
    baseline=ROOT/'output/anhier-texture-vfx/baseline/CardInfo.xml'
    if baseline.exists():
        before=ET.parse(baseline)
        for document in [before,tree]:
            for card in document.findall('Card'):
                cid=int(card.attrib['ID'])
                if cid not in expected: continue
                for die,profile in zip(card.findall('BehaviourList/Behaviour'),expected[cid]):
                    if profile: die.set('EffectRes','<authorized-profile-change>')
        assert ET.tostring(before.getroot())==ET.tostring(tree.getroot()),'CardInfo changed beyond the authorized 18 EffectRes bindings'
    adapter=(ROOT/'SteriaBuild/DiceAttackEffect_Steria_AnhierTextureCombat.cs').read_text(encoding='utf-8')
    core=(ROOT/'SteriaBuild/AnhierTextureTimeline.cs').read_text(encoding='utf-8')
    registry=(ROOT/'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')
    for name in PROFILES:
        assert registry.count('SteriaCustomEffects["Steria_AnhierTexture'+name+'"]')==1
        assert 'class DiceAttackEffect_Steria_AnhierTexture'+name in adapter
    assert 'GetTexture(' not in core and 'Sprites/Default' in core
    assert '.material =' not in core and '.material.' not in core
    assert 'PlaySound' not in adapter and 'ScreenShake' not in adapter
    assert manifest['decodedRgbaBytes'] <= 48*1024*1024
    print('SOURCE_PASS: 6 profiles, RGBA source/shipping hashes, independent frames, timing, dark pigment, exact 18 dice bindings, shared-material contract')

if __name__=='__main__': verify()
