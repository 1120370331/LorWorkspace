"""Import pose garments and split a registered head master without per-feature fitting."""
import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps

from prepare_face_sources import key_magenta

ROOT = Path(__file__).resolve().parent
ART = ROOT / "sources/r2"
GENERATED = Path("C:/Users/rog/.codex/generated_images/01a08150-8393-7641-a84b-efb2d86c7e3e")
INPUTS = {
    "support": "exec-ccbd7971-abf2-44f2-8de4-44d3745ec122.png",
    "attack": "exec-a0a2739e-83c8-4e8a-8b21-fcfb265b9cca.png",
    "motion": "exec-f925e812-528b-4686-a3b6-fc94ed0c08a3.png",
    "head_master": "exec-5d176f6e-918c-4310-94cd-ac577c723d4b.png",
}
# Landmarks refer to the full source sheet. Each pose's scale is chosen from
# anatomical size, not by forcing crouching/kneeling to have standing height.
POSES = {
    "Default":{"sheet":"support","crop":[76,225,423,887],"scale":.27,"ground":[244,874],"neck":[245,295],"head_angle":0,
        "joints":{"shoulder_l":[205,310],"elbow_l":[160,435],"wrist_l":[141,547],"shoulder_r":[301,310],"elbow_r":[325,434],"wrist_r":[324,549]},"feet":[[163,866],[259,872]]},
    "Guard":{"sheet":"support","crop":[504,290,987,883],"scale":.285,"ground":[736,872],"neck":[788,378],"head_angle":-4,
        "joints":{"shoulder_l":[713,391],"elbow_l":[644,429],"wrist_l":[570,370],"shoulder_r":[845,399],"elbow_r":[872,479],"wrist_r":[804,462]},"feet":[[560,864],[942,835]]},
    "Special":{"sheet":"support","crop":[1083,422,1468,879],"scale":.27,"ground":[1276,864],"neck":[1277,489],"head_angle":7,
        "joints":{"shoulder_l":[1223,506],"elbow_l":[1186,610],"wrist_l":[1243,562],"shoulder_r":[1340,517],"elbow_r":[1370,614],"wrist_r":[1260,571]},"feet":[[1176,856],[1425,838]]},
    "Slash":{"sheet":"attack","crop":[36,310,480,758],"scale":.35,"ground":[253,744],"neck":[263,391],"head_angle":10,
        "joints":{"shoulder_l":[208,406],"elbow_l":[153,470],"wrist_l":[103,536],"shoulder_r":[327,376],"elbow_r":[367,407],"wrist_r":[306,409]},"feet":[[97,741],[445,718]]},
    "Penetrate":{"sheet":"attack","crop":[505,315,1055,762],"scale":.35,"ground":[811,736],"neck":[765,391],"head_angle":5,
        "joints":{"shoulder_l":[716,396],"elbow_l":[645,394],"wrist_l":[562,379],"shoulder_r":[835,395],"elbow_r":[871,447],"wrist_r":[817,448]},"feet":[[634,733],[1022,725]]},
    "Hit":{"sheet":"attack","crop":[1082,356,1520,758],"scale":.35,"ground":[1306,746],"neck":[1291,435],"head_angle":6,
        "joints":{"shoulder_l":[1230,443],"elbow_l":[1178,468],"wrist_l":[1125,429],"shoulder_r":[1366,440],"elbow_r":[1397,491],"wrist_r":[1321,514]},"feet":[[1135,741],[1479,728]]},
    "Evade":{"sheet":"motion","crop":[16,371,488,764],"scale":.35,"ground":[255,754],"neck":[179,434],"head_angle":-4,
        "joints":{"shoulder_l":[124,446],"elbow_l":[94,515],"wrist_l":[76,575],"shoulder_r":[238,441],"elbow_r":[276,497],"wrist_r":[196,481]},"feet":[[108,750],[451,733]]},
    "Damaged":{"sheet":"motion","crop":[541,303,966,769],"scale":.35,"ground":[761,755],"neck":[762,358],"head_angle":-16,
        "joints":{"shoulder_l":[702,381],"elbow_l":[652,429],"wrist_l":[605,417],"shoulder_r":[827,369],"elbow_r":[876,437],"wrist_r":[884,398]},"feet":[[615,748],[921,729]]},
    "Move":{"sheet":"motion","crop":[1032,318,1507,763],"scale":.35,"ground":[1269,745],"neck":[1166,389],"head_angle":12,
        "joints":{"shoulder_l":[1137,411],"elbow_l":[1101,474],"wrist_l":[1077,443],"shoulder_r":[1227,373],"elbow_r":[1322,378],"wrist_r":[1381,454]},"feet":[[1094,733],[1457,742]]}
}

# Only the garment's foreground arms/hands. Painting these above hair also
# prevents the necklace from being drawn across clasped fingers.
FRONT = {
    "Guard":[(765,424),(769,415),(782,415),(797,426),(801,443),(825,454),(846,444),(878,449),(901,470),(906,489),(894,512),(863,510),(830,499),(802,479),(782,459)],
    "Special":[(1227,492),(1240,486),(1253,500),(1264,530),(1263,556),(1304,581),(1330,609),(1337,652),(1320,675),(1285,654),(1250,609),(1238,582),(1202,623),(1173,614),(1196,572),(1218,553)],
    "Slash":[(286,391),(300,383),(316,391),(329,406),(358,414),(374,442),(348,473),(319,451),(304,421),(287,415)],
    "Penetrate":[(801,428),(815,424),(831,437),(835,452),(863,460),(885,494),(864,510),(833,488),(814,466),(797,455)],
    "Hit":[(1311,499),(1323,488),(1340,496),(1352,514),(1378,523),(1391,548),(1369,564),(1332,543),(1310,520)],
    "Evade":[(150,424),(163,425),(181,447),(202,468),(231,471),(265,480),(289,502),(298,527),(280,548),(241,546),(216,520),(190,490),(166,470)],
    "Move":[(1053,414),(1065,402),(1084,407),(1090,427),(1116,436),(1142,447),(1152,480),(1124,505),(1072,516),(1058,482),(1067,447)],
}

def draw_pendant(image, crop, spec):
    """Draw the pose's authored curves/cross directly into its garment pixels."""
    samples=3
    layer=Image.new('RGBA',(image.width*samples,image.height*samples))
    draw=ImageDraw.Draw(layer)
    def point(p):return ((p[0]-crop[0])*samples,(p[1]-crop[1])*samples)
    def curve(a,c,b):
        return [point(((1-t)**2*a[0]+2*(1-t)*t*c[0]+t*t*b[0],
                       (1-t)**2*a[1]+2*(1-t)*t*c[1]+t*t*b[1])) for t in [i/24 for i in range(25)]]
    draw.line(curve(*spec['left']),fill='#596366',width=5)
    draw.line(curve(*spec['right']),fill='#596366',width=5)
    cx,cy=spec['left'][-1]
    draw.ellipse((*point((cx-2.5,cy-2)),*point((cx+2.5,cy+3))),outline='#535d60',width=4)
    w,h=spec['width'],spec['height']
    def charm(x,y):return (cx+x*w+spec['lean']*y*h,cy+2+y*h)
    outline=[(-.14,0),(.14,0),(.14,.27),(.5,.27),(.5,.44),(.14,.44),
             (.14,1),(-.14,1),(-.14,.44),(-.5,.44),(-.5,.27),(-.14,.27)]
    cross=[point(charm(x,y)) for x,y in outline]
    draw.polygon(cross,fill='#c6cecd')
    draw.line(cross+[cross[0]],fill='#556064',width=5,joint='curve')
    draw.line([point(charm(-.06,.08)),point(charm(-.06,.88))],fill='#e8ede9',width=2)
    paint=layer.resize(image.size,Image.Resampling.LANCZOS)
    paint.putalpha(ImageChops.multiply(paint.getchannel('A'),image.getchannel('A')))
    image.alpha_composite(paint)


def polygon(points):
    mask=Image.new('L',(512,610))
    ImageDraw.Draw(mask).polygon([(x,y-210) for x,y in points],fill=255)
    return mask


def masked(image, mask):
    result=image.copy()
    result.putalpha(ImageChops.multiply(image.getchannel('A'),mask))
    return result


def main():
    (ART/'raw').mkdir(parents=True,exist_ok=True)
    drawings=json.loads((ART/'pendant_strokes.json').read_text(encoding='utf-8'))
    if set(drawings)!=set(POSES):raise ValueError('Each pose needs its own garment ornament drawing.')
    provenance={"inputs":{},"poses":POSES,"head":{"crop_size":[512,610],"flip_left":True,
        "source_neck":[248,395],"scale":.23,"neck_skin_cut_y":402,
        "registration":"one common canvas; no independent crop-to-fit of features"},
        "pendant":"baked into each garment; no independent accessory slot",
        "pendant_pose_drawings":drawings, "foreground":FRONT}
    sheets={}
    for name,filename in INPUTS.items():
        target=ART/'raw'/(name+'.png')
        if not target.exists():shutil.copy2(GENERATED/filename,target)
        source=Image.open(target)
        if source.size != (1536,1024):raise ValueError(f"Unexpected source size: {name}")
        sheets[name]=key_magenta(source)
        provenance['inputs'][name]={"path":str(target.relative_to(ROOT)).replace('\\','/'),"sha256":hashlib.sha256(target.read_bytes()).hexdigest()}
    body_dir=ART/'body';body_dir.mkdir(exist_ok=True)
    for action,p in POSES.items():
        im=sheets[p['sheet']].crop(p['crop'])
        original=im.copy()
        front_mask=Image.new('L',im.size)
        if action in FRONT:
            ImageDraw.Draw(front_mask).polygon([(x-p['crop'][0],y-p['crop'][1]) for x,y in FRONT[action]],fill=255)
        draw_pendant(im,p['crop'],drawings[action])
        im.alpha_composite(masked(original,front_mask))
        im.save(body_dir/(action+'_wearing.png'))
        masked(im,ImageOps.invert(front_mask)).save(body_dir/(action+'.png'))
        masked(im,front_mask).save(body_dir/(action+'_front.png'))
    # One registered coordinate canvas. Face/skin masks follow the approved
    # master silhouette, not separately fitted tight sprite bounds.
    head_dir=ART/'head';head_dir.mkdir(exist_ok=True)
    master=sheets['head_master'].crop((0,210,512,820))
    skin=polygon([(206,465),(225,444),(250,420),(273,388),(290,496),(318,463),(339,415),
        (348,459),(359,491),(374,469),(389,471),(390,497),(372,522),(366,546),
        (336,565),(297,581),(292,612),(313,640),(321,659),(281,665),(242,655),
        (230,620),(218,575),(215,545),(214,529),(230,532),(218,520),(207,493)])
    ear=polygon([(141,445),(149,450),(150,485),(143,493),(134,479),(132,461)])
    skin=ImageChops.lighter(skin,ear)
    face_masks={
        'eyes':polygon([(211,456),(293,450),(292,512),(209,514)]),
        'eyes_right':polygon([(337,452),(395,449),(396,512),(335,513)]),
        'brows':polygon([(214,416),(286,415),(287,449),(215,449)]),
        'brows_right':polygon([(334,418),(393,426),(393,451),(334,447)]),
        'nose':polygon([(313,504),(339,504),(339,531),(313,531)]),
        'mouth':polygon([(287,531),(341,531),(341,561),(287,561)])
    }
    face_masks['eyes']=ImageChops.lighter(face_masks.pop('eyes_right'),face_masks['eyes'])
    face_masks['brows']=ImageChops.lighter(face_masks.pop('brows_right'),face_masks['brows'])
    face_masks={k:ImageChops.multiply(v,skin) for k,v in face_masks.items()}
    union=Image.new('L',master.size)
    for mask in face_masks.values():union=ImageChops.lighter(union,mask)
    base=masked(master,skin)
    # Fill only the hidden feature beds for editability; full-opacity feature
    # overlays recover the exact source master in the normal expression.
    fill=Image.new('RGBA',master.size,(225,228,219,255))
    base.paste(fill,(0,0),union)
    # Shorten only the visible neck. The whole registered head is placed using
    # the new neck cut, keeping the eye/nose/mouth/chin geometry unchanged.
    a=base.getchannel('A')
    ImageDraw.Draw(a).rectangle((0,402,512,610),fill=0)
    base.putalpha(a)
    hair=masked(master,ImageOps.invert(skin))
    backmask=ImageChops.lighter(polygon([(0,490),(143,478),(197,559),(207,608),(212,681),(236,820),(0,820)]),
        polygon([(432,494),(512,478),(512,820),(416,820),(432,765),(452,705),(442,643)]))
    hair_back=masked(hair,backmask)
    hair_front=masked(hair,ImageOps.invert(backmask))
    for name,im in [('facemodel',base),('hair_back',hair_back),('hair_front',hair_front),('master_normal',master)]:
        ImageOps.mirror(im).save(head_dir/(name+'.png'))
    # Corresponding head masters are registered by their crown/chin/neck,
    # translating the whole head together. Expression motion is retained.
    for expression,col,dx in [('normal',0,0),('attack',1,7),('damaged',2,11)]:
        source=sheets['head_master'].crop((col*512,210,(col+1)*512,820))
        aligned=Image.new('RGBA',source.size);aligned.alpha_composite(source,(dx,0))
        for name,mask in face_masks.items():
            ImageOps.mirror(masked(aligned,mask)).save(head_dir/(name+'_'+expression+'.png'))
    # Prayer uses closed eyelids with neutral eyebrows/mouth, not the complete
    # damaged face (whose open mouth would read as pain).
    shutil.copy2(head_dir/'eyes_damaged.png',head_dir/'eyes_cast.png')
    (ART/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')
    print('Prepared nine pose garments and shared-coordinate head layers.')


if __name__=='__main__':main()
