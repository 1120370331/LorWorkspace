"""Prepare registered head layers and coherent body/weapon pose drawings."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps, ImageStat


def key_magenta(image):
    pixels=[]
    for r,g,b in image.convert('RGB').getdata():
        excess=min(r,b)-g
        if excess>=235: pixels.append((0,0,0,0))
        elif excess>35:
            a=1-excess/255
            pixels.append((max(0,min(255,round((r-(1-a)*255)/a))),
                           max(0,min(255,round(g/a))),
                           max(0,min(255,round((b-(1-a)*255)/a))),round(a*255)))
        else: pixels.append((r,g,b,255))
    out=Image.new('RGBA',image.size);out.putdata(pixels)
    return out


def polygon_mask(size,polygons,origin=(0,0)):
    result=Image.new('L',size);draw=ImageDraw.Draw(result)
    for poly in polygons:
        draw.polygon([(x-origin[0],y-origin[1]) for x,y in poly],fill=255)
    return result


def masked(image,mask):
    out=image.copy();out.putalpha(ImageChops.multiply(image.getchannel('A'),mask));return out


def register_head(reference,other):
    """Translate the whole head by silhouette fit; never fit individual features."""
    a=reference.getchannel('A').point(lambda x:255 if x>32 else 0)
    b=other.getchannel('A').point(lambda x:255 if x>32 else 0)
    box_a,box_b=a.getbbox(),b.getbbox()
    dx0=round((box_a[0]+box_a[2]-box_b[0]-box_b[2])/2)
    dy0=box_a[1]-box_b[1]
    best=None
    for dy in range(dy0-3,dy0+4):
        for dx in range(dx0-5,dx0+6):
            aligned=Image.new('L',b.size);aligned.paste(b,(dx,dy))
            score=ImageStat.Stat(ImageChops.difference(a,aligned)).sum[0]
            if best is None or score<best[0]:best=(score,dx,dy)
    _,dx,dy=best
    result=Image.new('RGBA',other.size);result.alpha_composite(other,(dx,dy))
    return result,[dx,dy]


def prepare(project):
    project=Path(project).resolve()
    config_path=project/'source_layout.json'
    config=json.loads(config_path.read_text(encoding='utf-8'))
    sheets={p.stem:key_magenta(Image.open(p)) for p in (project/'sources/raw').glob('*.png')}
    provenance={'generated_raw':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (project/'sources/raw').glob('*.png')},
                'head_registration':{},'poses':config['poses']}
    h=config['head'];crop=h['crop'];size=(crop[2]-crop[0],crop[3]-crop[1])
    master=sheets['head'].crop(crop)
    skin=polygon_mask(size,h['skin'],crop[:2])
    feature_masks={name:ImageChops.multiply(polygon_mask(size,polys,crop[:2]),skin) for name,polys in h['features'].items()}
    union=Image.new('L',size)
    for mask in feature_masks.values():union=ImageChops.lighter(union,mask)
    face=masked(master,skin)
    face.paste(Image.new('RGBA',size,tuple(h['skin_fill'])+(255,)),(0,0),union)
    alpha=face.getchannel('A');ImageDraw.Draw(alpha).rectangle((0,h['skin_cut_y']-crop[1],size[0],size[1]),fill=0);face.putalpha(alpha)
    hair=masked(master,ImageOps.invert(skin));back=polygon_mask(size,h['back_hair'],crop[:2])
    directory=project/'sources/head';directory.mkdir(parents=True,exist_ok=True)
    for name,im in [('facemodel',face),('hair_back',masked(hair,back)),('hair_front',masked(hair,ImageOps.invert(back))),('master_normal',master)]:im.save(directory/(name+'.png'))
    for expression,col in [('normal',0),('attack',1),('damaged',2)]:
        other=sheets['head'].crop((crop[0]+512*col,crop[1],crop[2]+512*col,crop[3]))
        registered,offset=(master,[0,0]) if col==0 else register_head(master,other)
        provenance['head_registration'][expression]=offset
        for name,mask in feature_masks.items():masked(registered,mask).save(directory/(name+'_'+expression+'.png'))
    body_dir=project/'sources/body';body_dir.mkdir(exist_ok=True)
    for action,p in config['poses'].items():
        rect=p['crop'];im=sheets[p['sheet']].crop(rect)
        weapon=polygon_mask(im.size,p.get('weapon',[]),rect[:2])
        front=ImageChops.multiply(polygon_mask(im.size,p.get('front',[]),rect[:2]),ImageOps.invert(weapon))
        main=ImageOps.invert(ImageChops.lighter(weapon,front))
        for name,mask in [('body',main),('front',front),('weapon',weapon)]:masked(im,mask).save(body_dir/(action+'_'+name+'.png'))
        im.save(body_dir/(action+'_master.png'))
    (project/'sources/provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    write_model(project,config)
    print('Prepared '+config['character']+': registered head, 9 body/weapon poses.')


def write_model(project,config):
    h=config['head'];crop=h['crop'];w,ch=crop[2]-crop[0],crop[3]-crop[1]
    pin=[h['neck'][0]-crop[0],h['neck'][1]-crop[1]];scale=h['scale']
    parts=[]
    for id,z,slot in [('hair_back',0,'hair'),('facemodel',30,'facemodel'),('eyes',41,'face'),('brows',40,'face'),('nose',42,'face'),('mouth',43,'face'),('hair_front',50,'hair')]:
        fn=id+'_normal' if slot=='face' else id
        parts.append({'id':id,'label':id,'domain':'head','slot':slot,'path':'sources/head/'+fn+'.png',
            'size':[w*scale,ch*scale],'pivot':[pin[0]/w,pin[1]/ch],'at':[0,0],'z':z})
    for id,z,plane,suffix,slot in [('body',20,'main','body','body'),('body_front',60,'front','front','body'),('weapon',65,'front','weapon','weapon')]:
        parts.append({'id':id,'domain':'wearing','slot':slot,'path':'sources/body/Default_'+suffix+'.png',
                      'size':[1,1],'pivot':[0,0],'at':[256,390],'z':z,'plane':plane})
    poses={}
    labels={'Default':'待机','Guard':'防御','Evade':'闪避','Damaged':'受击','Slash':'斩击','Penetrate':'突刺','Hit':'打击','Move':'移动','Special':'特殊'}
    for action in labels:
        p=config['poses'][action];rect=p['crop'];s=p['scale'];image=Image.open(project/('sources/body/'+action+'_master.png'))
        # Calibrate from body/boot pixels, excluding detached blade tips.
        main=Image.open(project/('sources/body/'+action+'_body.png')).getchannel('A')
        front=Image.open(project/('sources/body/'+action+'_front.png')).getchannel('A')
        bounds=ImageChops.lighter(main,front).point(lambda a:255 if a>32 else 0).getbbox()
        ground=[p['ground'][0],rect[1]+bounds[3]-1];neck=p['neck']
        def world(point):return [round(256+(point[0]-ground[0])*s,5),round(390+(point[1]-ground[1])*s,5)]
        overrides={}
        for id,suffix in [('body','body'),('body_front','front'),('weapon','weapon')]:
            overrides[id]={'path':'sources/body/'+action+'_'+suffix+'.png', 'size':[image.width*s,image.height*s],
                'pivot':[(ground[0]-rect[0])/image.width,(ground[1]-rect[1])/image.height],'at':[256,390]}
        expression='damaged' if action=='Damaged' else 'normal' if action in ['Default','Move','Special'] else 'attack'
        poses[action]={'label':labels[action],'expression':expression,'body_angle':0,'head_angle':p.get('head_angle',0),
            'neck_at':world(neck),'joints':{key:world(value) for key,value in p['joints'].items()},
            'feet':[world(v) for v in p['feet']],'overrides':overrides}
    native={
        'facemodel':{'path':'sources/native/head.png','size':[70,78],'pivot':[.5,.5],'at':[0,-37]},
        'hair_front':{'path':'sources/native/hair.png','size':[83,70],'pivot':[.5,0],'at':[0,-86]},
        'eyes':{'path':'sources/native/eyes.png','size':[49,13],'pivot':[.5,.5],'at':[-7,-20]},
        'brows':{'path':'sources/native/brows.png','size':[37,5],'pivot':[.5,.5],'at':[-8,-29]},
        'mouth':{'path':'sources/native/mouth.png','size':[8,3],'pivot':[.5,.5],'at':[-8,-7]}}
    model={'schema_version':1,'id':config['character']+'_Layered_v1','character':config['character'],
        'prepared_from':{'path':'source_layout.json','sha256':hashlib.sha256((project/'source_layout.json').read_bytes()).hexdigest()},
        'display_name':config['display_name'],'status':'visual_candidate','default_head':'character',
        'canvas':{'width':512,'height':512,'origin':'top_left','facing':'left','pixels_per_unit':50,'root':[256,390]},
        'head':{'hair':['hair_back','hair_front'],'facemodel':'facemodel','face':{x:x for x in ['eyes','brows','nose','mouth']},
            'color':{'mode':'baked_palette'},'anchor':[0,0],'runtime_origin_from_neck':[0,-37],
            'registration':{'canvas':[w,ch],'neck':pin,'uniform_scale':scale},
            'expressions':{e:({} if e=='normal' else {x:{'path':'sources/head/'+x+'_'+e+'.png'} for x in ['eyes','brows','nose','mouth']}) for e in ['normal','attack','damaged']},
            'projection_policy':'keep_target_head'},
        'wearing':{'body':['body','body_front'],'hat':[],'accessories':[],
            'weapon':{'mode':config['weapon_mode'],'parts':['weapon'],'binding':'pose drawing and hand/grip anchors'},
            'ego_specials':{'baked':False,'references':config.get('effect_references',[])},'ornament_policy':'fixed details drawn into owning part'},
        'parts':parts,'poses':poses,'aliases':{'S1':'Special','Fire':'Penetrate','Aim':'Guard'},
        'head_presets':{'character':{'label':config['display_name'],'overrides':{}},'librarian':{'label':'原版司书换头对照','hide':['hair_back','nose'],'overrides':native}},
        'grounding':{'reference':'sources/grounding/bada_reference.json','target_sole_root_y':.06,'alpha_threshold':32,'sole_band_y':[346,416],'exceptions':{}},
        'limitations':['Front-view artwork only; native game testing not performed.','No effects baked into the body/weapon images.']}
    (project/'model.json').write_text(json.dumps(model,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
