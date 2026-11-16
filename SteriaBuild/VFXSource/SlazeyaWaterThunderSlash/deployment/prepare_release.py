"""Prepare a narrow release on the exact PDB-verified installed build, not the dirty worktree."""
from pathlib import Path
import hashlib,json,shutil,sys

ROOT=Path(__file__).resolve().parents[4]
VFX=ROOT/'SteriaBuild/VFXSource/SlazeyaWaterThunderSlash'
BASE=ROOT/'output/slazeya-wind-combat/scoped-pierce-release'
OUT=ROOT/'output/slazeya-water-thunder'/sys.argv[1]
GAME=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder').resolve()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
metadata=json.loads((BASE/'built-metadata.json').read_text())
assert metadata['dll_sha256']==sha(GAME/'Assemblies/Steria.dll')=='e3d7385aa0b475e6cc1e09d543003848837dd3602163ba0c6608971cedf1528a'
assert not OUT.exists(),OUT
source=OUT/'SteriaBuild';source.mkdir(parents=True)
for row in metadata['documents']:
    p=Path(row['path'])
    if 'obj' in p.parts:continue
    assert p.parent==BASE/'SteriaBuild' and sha(p)==row['checksum'],p
    shutil.copy2(p,source/p.name)
for name in ['Steria.csproj','Libs','VisualAssets']:
    p=BASE/'SteriaBuild'/name
    if p.is_dir():shutil.copytree(p,source/name)
    else:shutil.copy2(p,source/name)
runtime=Path('VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime')
additions=['SlazeyaWaterThunderCombat.cs']+[str(runtime/n).replace('\\','/') for n in ['WaterThunderSlash.cs','WaterThunderMotion.cs','WaterThunderImpact.cs','WaterDropletMotion.cs']]
for rel in additions:
    dest=source/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/'SteriaBuild'/rel,dest)
shutil.copy2(ROOT/'SteriaBuild/SlazeyaWindImpact.cs',source/'SlazeyaWindImpact.cs')
harmony=source/'HarmonyPatches.cs';text=harmony.read_text(encoding='utf-8-sig')
for suffix in ['Slash','Hit','Pierce']:
    before='SteriaCustomEffects["Steria_SlazeyaWind'+suffix+'"] = typeof(DiceAttackEffect_Steria_SlazeyaWind'+suffix+');'
    after='SteriaCustomEffects["Steria_SlazeyaWind'+suffix+'"] = typeof(DiceAttackEffect_Steria_SlazeyaWaterThunder'+suffix+');'
    assert text.count(before)==1; text=text.replace(before,after)
harmony.write_text(text,encoding='utf-8')
project=source/'Steria.csproj';xml=project.read_text(encoding='utf-8-sig')
includes='\n  <ItemGroup>\n'+''.join('    <Compile Include="'+rel.replace('/','\\')+'" />\n' for rel in additions[1:])+'  </ItemGroup>\n'
project.write_text(xml.replace('</Project>',includes+'</Project>'),encoding='utf-8')
payload=OUT/'payload';(payload/'Assemblies/AB').mkdir(parents=True)
art_name=sys.argv[2] if len(sys.argv)>2 else json.loads((VFX/'accepted-candidate.json').read_text())['candidate']
art_dir=VFX/'evidence'/art_name
assert json.loads((art_dir/'complete.json').read_text())['status']=='technical-pass'
assert (art_dir/'visual-review.md').exists(),'Main visual review must precede release preparation'
art=art_dir/'bundle/steria_slazeya_water_thunder_assets'
shutil.copy2(art,payload/'Assemblies/AB'/art.name)
notice=payload/'Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt';notice.parent.mkdir(parents=True)
notice.write_text('Slazeya Water / Thunder melee VFX\nOriginal project-authored meshes, textures, masks and water droplet motion.\nShader: compatible derivative of the local unity-sword-slash-vfx Contract V1 infrastructure.\nTechnique reference only: https://www.cyanilux.com/tutorials/sword-slash-shader-breakdown/\nNo game-extracted or third-party image/mesh files are redistributed.\nAuthoring provenance: SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/provenance.json\n',encoding='utf-8')
preserved={rel:sha(GAME/rel) for rel in ['Data/CardInfo.xml','Assemblies/AB/steria_slazeya_wind_combat','Resource/ThirdPartyNotices/SlazeyaWindCombat.txt']}
record={'status':'PREPARED','art_candidate':art_name,'baseline_dll':metadata['dll_sha256'],'baseline_metadata':str(BASE/'built-metadata.json'),'game_root':str(GAME),'additions':additions,'changed_existing':['HarmonyPatches.cs','SlazeyaWindImpact.cs'],
        'sources':{p.relative_to(source).as_posix():sha(p) for p in source.rglob('*.cs')},'preserved':preserved,
        'art_bundle_sha256':sha(art),'scope':'three existing melee keys only; FarHit, Guard, FarArea and XML unchanged'}
(OUT/'prepared.json').write_text(json.dumps(record,indent=2)+'\n')
print('WATER_THUNDER_SCOPED_PREPARED',OUT,len(record['sources']),'source files; five additions; no existing source overwritten')
