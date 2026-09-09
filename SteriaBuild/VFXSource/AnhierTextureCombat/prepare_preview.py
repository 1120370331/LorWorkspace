from pathlib import Path
import hashlib,json,shutil,sys
from PIL import Image,ImageDraw
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PROJECT=HERE/'UnityProject';ASSETS=PROJECT/'Assets'
(ASSETS/'Editor').mkdir(parents=True,exist_ok=True)
(PROJECT/'Packages').mkdir(exist_ok=True);(PROJECT/'ProjectSettings').mkdir(exist_ok=True)
(PROJECT/'Packages/manifest.json').write_text('{"dependencies":{"com.unity.modules.imageconversion":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}\n')
(PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\nm_EditorVersionWithRevision: 2019.3.15f1 (59ff3e03856d)\n')
production=ROOT/'SteriaBuild/AnhierTextureTimeline.cs'
shutil.copyfile(production,ASSETS/'AnhierTextureTimeline.cs')
reference=ASSETS/'Reference';reference.mkdir(exist_ok=True)
sys.path.insert(0,str(ROOT/'output/music-dice-ui/python-deps'))
import UnityPy
bundle=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/StreamingAssets/AssetBundles/char/char_418.pres')
env=UnityPy.load(str(bundle));sprites={obj.path_id:obj for obj in env.objects if obj.type.name=='Sprite'}
native_pivots={}
for obj in env.objects:
    if obj.type.name=='MonoBehaviour':
        try:
            tree=obj.read_typetree()
            if 'atkEffectRoot' in tree:native_pivots.update({k:v for k,v in tree.items() if 'atkEffect' in k})
        except Exception:pass
    elif obj.type.name=='Transform':
        transform=obj.read()
        if transform.m_GameObject.read().m_Name=='AtkEffectRoot':
            native_pivots['localPosition']=[transform.m_LocalPosition.x,transform.m_LocalPosition.y,transform.m_LocalPosition.z]
(reference/'bada-native-effect-pivots.json').write_text(json.dumps(native_pivots,indent=2)+'\n')
parts=json.loads((ROOT/'output/velia-layered-v1/baseline/bada_418_ground.json').read_text())
canvas=Image.new('RGBA',(600,650))
for name in ['body 1','head','face','front hair']:
    item=next(x for x in parts if x['sprite']==name)
    sprite=sprites[item['sprite_id']].read();im=sprite.image.convert('RGBA')
    x=300+item['position'][0]*100-im.width*.5
    y=600-item['position'][1]*100-im.height*.5
    canvas.alpha_composite(im,(round(x),round(y)))
canvas.save(reference/'bada-default-reference.png')
# Painted fixture background is only a scale/contrast environment, never shipped.
bg=Image.new('RGB',(960,540));d=ImageDraw.Draw(bg)
for y in range(540):
    t=y/540;d.line((0,y,959,y),fill=(round(31+20*t),round(39+18*t),round(54+10*t)))
for x in [20,175,335,495,655,815]:
    d.rectangle((x,35,x+105,400),fill=(24,31,43));d.rectangle((x+10,50,x+95,390),fill=(44,49,59));d.rectangle((x+35,70,x+67,380),fill=(25,32,43))
d.polygon([(0,402),(959,402),(959,539),(0,539)],fill=(54,52,59))
for y in [413,445,489,530]:d.line((0,y,960,y),fill=(72,66,68),width=2)
for x in range(-300,1400,160):d.line((480,402,x,540),fill=(38,39,48),width=2)
bg.save(reference/'fixture-library-background.png')
ship=ROOT/'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
files=[production,ROOT/'SteriaBuild/DiceAttackEffect_Steria_AnhierTextureCombat.cs',ROOT/'SteriaBuild/HarmonyPatches.cs',ROOT/'SteriaBuild/Steria.csproj',ROOT/'SteriaBuild/bin/Release/net472/Steria.dll',ROOT/'SteriaBuild/SteriaModFolder/Data/CardInfo.xml',HERE/'manifest.json',HERE/'import_masters.py',HERE/'verify_source.py',ASSETS/'Editor/AnhierTexturePreview.cs',HERE/'imagegen_masters/inputs.json']+sorted(ship.glob('*'))
record={'unity':'2019.3.15f1','provenance':'Exact complete production AnhierTextureTimeline.cs copied byte-for-byte. Game adapter separately compiled against actual game assemblies. Fixture supplies world pivots and native Bada default reference; this is not a game battle capture.','productionCopyMatches':production.read_bytes()==(ASSETS/'AnhierTextureTimeline.cs').read_bytes(),'nativeReferenceOnly':{'bundle':str(bundle),'sha256':hashlib.sha256(bundle.read_bytes()).hexdigest()},'files':{str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.exists()}}
(HERE/'previews').mkdir(exist_ok=True)
(HERE/'previews/candidate-hashes.json').write_text(json.dumps(record,indent=2)+'\n')
print('FIXTURE_PREPARED: exact production source, native Bada default reference and candidate hashes')
