"""Register saved ImageGen observation assets; never calls generation or deploys."""
import copy
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'sources/observation'


def main():
    model = json.loads((ROOT / 'model.json').read_text(encoding='utf-8'))
    body = Image.open(SOURCE / 'body_overhead_raw.png').convert('RGBA')
    body.save(SOURCE / 'body.png')
    Image.new('RGBA', body.size).save(SOURCE / 'body_front.png')
    # Feature is registered on the same head canvas and transformed with the
    # entire head. No per-feature runtime transform or independent head fitting.
    mouth = Image.open(SOURCE / 'smile_raw.png').convert('RGBA')
    mouth = mouth.crop(mouth.getchannel('A').point(lambda a:255 if a>24 else 0).getbbox())
    mouth = mouth.resize((38, 20), Image.Resampling.LANCZOS)
    registered = Image.new('RGBA', (512,610))
    registered.alpha_composite(mouth, (179,328))
    registered.save(SOURCE / 'mouth_smile.png')
    model['head']['expressions']['observation_smile'] = {'mouth':{'path':'sources/observation/mouth_smile.png'}}
    scale = (390-233.13)/(1003-620)
    neck = (647,620)
    ground = (647,1003)
    palm = (544,400)
    def output(point):
        return [256+(point[0]-ground[0])*scale, 390+(point[1]-ground[1])*scale]
    pose = copy.deepcopy(model['poses']['Default'])
    pose.update(label='潜力观测 · 举右手微笑', expression='observation_smile', neck_at=output(neck))
    for part in ['body','body_front']:
        pose['overrides'][part] = {'path':'sources/observation/'+part+'.png',
            'size':[body.width*scale,body.height*scale],
            'pivot':[ground[0]/body.width,ground[1]/body.height], 'at':[256,390]}
    pose['joints'] = {'shoulder_l':output((617,595)), 'elbow_l':output((521,491)),
        'wrist_l':output((543,421)), 'shoulder_r':output((681,622)),
        'elbow_r':output((699,708)), 'wrist_r':output((695,780))}
    pose['feet'] = [output((590,990)),output((664,998))]
    pose['anatomical_anchors'] = {'rightPalm':output(palm)}
    model['poses']['S2'] = pose
    (ROOT / 'model.json').write_text(json.dumps(model,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    manifest = {'action':'S2','canvas':[512,512], 'pixelsPerUnit':50,
        'anatomicalSide':'right', 'canonicalFacing':'left', 'rightPalm':output(palm),
        'sourceNeck':neck, 'sourceGround':ground, 'sourcePalm':palm, 'sourceScale':scale,
        'source':'sources/observation/body_overhead_raw.png',
        'bodySha256':hashlib.sha256((SOURCE/'body_overhead_raw.png').read_bytes()).hexdigest(),
        'generator':'built-in image_gen', 'noVfxBaked':True}
    (SOURCE/'anchors.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(manifest,indent=2))


if __name__=='__main__':main()
