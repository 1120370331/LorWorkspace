"""Focused source/asset binding checks, complemented by native runtime tests."""
import json
import hashlib
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]

def main():
    cards=(ROOT/'VeliaCards.cs').read_text(encoding='utf-8-sig')
    passive=(ROOT/'VeliaAbilities.cs').read_text(encoding='utf-8-sig')
    visual=(ROOT/'VeliaPotentialObservationVisual.cs').read_text(encoding='utf-8-sig')
    host=(ROOT/'VeliaPotentialObservationEffect.cs').read_text(encoding='utf-8-sig')
    assert 'VeliaPotentialObservationEffect.Play(unit, targetUnit)' in cards
    assert 'DelayedResetMotion' not in cards
    assert 'override void OnAfterRollSpeedDice()' in passive
    assert '_pendingObservationTarget = target' in passive
    assert 'WaitForSeconds(0.22f)' in host
    assert 'UnityEngine.Random' not in visual
    assert 'Instantiate(particlePrefab' in visual and 'system.Simulate(Step,false,false,false)' in visual
    assert 'ParticleSystemStopBehavior.StopEmittingAndClear' in visual
    assert 'DrawFountain' not in visual and 'DrawSpray' not in visual
    assert 'GoldWisps' not in visual and '"CoreFlow","SoftLift","Glints"' in visual
    assert '_lightProperties.SetFloat("_Age",Age)' in visual
    assert 'LoadAsset<GameObject>(VeliaPotentialObservationVisual.PrefabName)' in host
    assert 'ActionDetail.S2' in host and 'GetCurrentMotionDetail()' in host
    model=json.loads((ROOT/'CharacterModels/Velia_Layered_v1/model.json').read_text(encoding='utf-8'))
    assert model['poses']['S2']['expression']=='observation_smile'
    assert model['poses']['Special']['expression']=='cast'
    assert model['aliases']['S1']=='Special'
    assert model['poses']['S2']['anatomical_anchors']['rightPalm'] == json.loads((ROOT/'CharacterModels/Velia_Layered_v1/sources/observation/anchors.json').read_text())['rightPalm']
    art=Path(__file__).resolve().parent/'source_assets/light-masks'
    provenance=json.loads((art/'manifest.json').read_text())
    for path,record in provenance['files'].items():
        assert hashlib.sha256((art/path).read_bytes()).hexdigest()==record['sha256'],path
        image=Image.open(art/path)
        assert image.size==tuple(record['size']),path
        assert record['channel']=='R' and record['sRGB'] is False,path
        lo,hi=image.convert('RGB').getchannel('R').getextrema()
        assert lo==0 and hi>=200,path
    for package in ['particle-pack','light-masks']:
        assert 'Creative Commons Zero' in (art/'licenses'/package/'License.txt').read_text(encoding='utf-8-sig')
    shader=(Path(__file__).resolve().parent/'UnityProject/Assets/Shaders/VeliaObservationParticle.shader').read_text()
    assert 'Blend One One' in shader and 'tex2D(_MainTex,warped).r' in shader
    assert 'return half4(light*coverage,0)' in shader and '_Time' not in shader
    print('VELIA_OBSERVATION_SOURCE_PASS')

if __name__=='__main__':main()
