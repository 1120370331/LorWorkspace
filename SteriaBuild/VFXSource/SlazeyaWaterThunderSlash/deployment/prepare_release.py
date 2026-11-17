"""Prepare a narrow release on the exact PDB-verified installed build, not the dirty worktree."""
from pathlib import Path
import hashlib,json,shutil,sys

ROOT=Path(__file__).resolve().parents[4]
VFX=ROOT/'SteriaBuild/VFXSource/SlazeyaWaterThunderSlash'
BASE=ROOT/'output/slazeya-water-thunder'/(sys.argv[3] if len(sys.argv)>3 else 'release-03')
OUT=ROOT/'output/slazeya-water-thunder'/sys.argv[1]
GAME=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder').resolve()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
metadata=json.loads((BASE/'built-metadata.json').read_text())
assert metadata['pdb_matches'] and metadata['dll_sha256']==sha(GAME/'Assemblies/Steria.dll')
assert not OUT.exists(),OUT
source=OUT/'SteriaBuild';source.mkdir(parents=True)
compiled_root=Path(metadata['dll']).parents[3]
for row in metadata['documents']:
    p=Path(row['path'])
    if 'obj' in p.parts:continue
    rel=p.relative_to(compiled_root);assert sha(p)==row['checksum'],p
    dest=source/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
for name in ['Steria.csproj','Libs','VisualAssets']:
    p=BASE/'SteriaBuild'/name
    if p.is_dir():shutil.copytree(p,source/name)
    else:shutil.copy2(p,source/name)
runtime=Path('VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime')
additions=[]
asset_only=len(sys.argv)>4 and sys.argv[4]=='asset-only'
overlays=[] if asset_only else ['SlazeyaWaterThunderCombat.cs',str(runtime/'WaterThunderMotion.cs').replace('\\','/'),str(runtime/'WaterThunderSlash.cs').replace('\\','/'),str(runtime/'WaterThunderImpact.cs').replace('\\','/')]
for rel in overlays:shutil.copy2(ROOT/'SteriaBuild'/rel,source/rel)
changed=[rel for rel in overlays if sha(source/rel)!=sha(compiled_root/rel)]
if asset_only:
    # Reuse the exact installed DLL/PDB and their build metadata. Only the art bundle changes.
    dest=source/'bin/Release/net472';dest.mkdir(parents=True)
    for name in ['Steria.dll','Steria.pdb']:shutil.copy2(BASE/'SteriaBuild/bin/Release/net472'/name,dest/name)
    shutil.copy2(BASE/'built-metadata.json',OUT/'built-metadata.json')
payload=OUT/'payload';(payload/'Assemblies/AB').mkdir(parents=True)
art_name=sys.argv[2] if len(sys.argv)>2 else json.loads((VFX/'accepted-candidate.json').read_text())['candidate']
art_dir=VFX/'evidence'/art_name
assert json.loads((art_dir/'complete.json').read_text())['status']=='technical-pass'
assert (art_dir/'visual-review.md').exists(),'Main visual review must precede release preparation'
art=art_dir/'bundle/steria_slazeya_water_thunder_assets'
shutil.copy2(art,payload/'Assemblies/AB'/art.name)
notice=payload/'Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt';notice.parent.mkdir(parents=True)
notice.write_text('Slazeya Water / Thunder melee VFX\nProject-authored meshes, slash/flow/depth textures, masks, particle motion and UV shaders.\nOne user-provided water normal is included unchanged: water normal 425641.png\nUser asset SHA256: 09ae9b1415003871a019cdaee2d49bb0964fa2d9af2161f25857883f5737afae\nProvided by the user for this mod; no public open-source license is claimed for that image.\nBlade shader: compatible derivative of local unity-sword-slash-vfx Contract V1 infrastructure.\nTechnique references only: https://www.cyanilux.com/tutorials/sword-slash-shader-breakdown/ and https://github.com/innogames/IG-VFX-Unity-Mobile-Toolkit\nNo game-extracted images/meshes or third-party source pixels/code are copied.\nProvenance: SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/provenance.json\n',encoding='utf-8')
preserved={rel:sha(GAME/rel) for rel in ['Data/CardInfo.xml','Assemblies/AB/steria_slazeya_wind_combat','Resource/ThirdPartyNotices/SlazeyaWindCombat.txt','Data/EquipPage_Librarian.xml','Data/EquipPage_Enemy.xml']}
record={'status':'PREPARED','art_candidate':art_name,'baseline_dll':metadata['dll_sha256'],'baseline_metadata':str(BASE/'built-metadata.json'),'game_root':str(GAME),'additions':additions,'changed_existing':changed,'source_overlays':overlays,'before_payload':{rel:sha(GAME/rel) for rel in ['Assemblies/Steria.dll','Assemblies/AB/steria_slazeya_water_thunder_assets','Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt']},
        'sources':{p.relative_to(source).as_posix():sha(p) for p in source.rglob('*.cs')},'preserved':preserved,
        'art_bundle_sha256':sha(art),'assembly_reused':asset_only,'scope':'final white highlight reaches mid-arc; existing core melee routing and actor-front binding preserved' if asset_only else 'game anchor lowered by 0.18 character-local units; exact candidate40 art/runtime and core/ranged routing preserved'}
(OUT/'prepared.json').write_text(json.dumps(record,indent=2)+'\n')
print('WATER_THUNDER_SCOPED_PREPARED',OUT,len(record['sources']),'source files;',len(changed),'bounded updates; exact installed assembly reused' if asset_only else 'bounded updates')
