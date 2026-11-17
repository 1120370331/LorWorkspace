"""Freeze only the Antie delta on the PDB-proven installed release; no deployment."""
from pathlib import Path
import datetime,difflib,hashlib,json,re,shutil,sys

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3];VFX=HERE.parent
name=sys.argv[1] if len(sys.argv)>1 else 'release-01'
assert re.fullmatch(r'[A-Za-z0-9_-]+',name)
OUT=ROOT/'output/antie-ice-indigo'/name
BASE=ROOT/'output/slazeya-water-thunder/release-07'
GAME=Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder').resolve(strict=True)
REPO=(ROOT/'SteriaBuild/SteriaModFolder').resolve(strict=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
approved=read(VFX/'preview-candidate.json');old=read(BASE/'built-metadata.json')
assert approved['visual_candidate']=='candidate-05-convex-to-camera' and approved['native_checks']==111
assert old['pdb_matches'] and sha(GAME/'Assemblies/Steria.dll')==old['dll_sha256']==sha(REPO/'Assemblies/Steria.dll')
for rel,value in approved['integration_source_hashes'].items():assert sha(ROOT/rel)==value,rel+' drifted since preview'
art=VFX/'evidence'/approved['visual_candidate'];bundle=art/'bundle/steria_antie_ice_indigo_combat'
assert sha(bundle)==approved['bundle_sha256'] and (art/'review.md').is_file()
assert read(art/'complete.json')['nativeBundleReloadPixelEqual']
runtime='VFXSource/AntieIceIndigoSlash/UnityProject/Assets/AntieIceIndigo/Runtime/AntieSlash.cs'
input_hashes={r['path']:r['sha256'] for r in read(art/'source-hashes.json')}
assert sha(ROOT/'SteriaBuild'/runtime)==input_hashes['UnityProject/Assets/AntieIceIndigo/Runtime/AntieSlash.cs']
assert not OUT.exists(),OUT
source=OUT/'SteriaBuild';source.mkdir(parents=True)
compiled=Path(old['dll']).parents[3]
for document in old['documents']:
    p=Path(document['path'])
    if 'obj' in p.relative_to(compiled).parts:continue
    assert sha(p)==document['checksum'],p
    target=source/p.relative_to(compiled);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
for rel in ['Steria.csproj','Libs','VisualAssets']:
    p=BASE/'SteriaBuild'/rel
    if p.is_dir():shutil.copytree(p,source/rel)
    else:shutil.copy2(p,source/rel)
for rel in ['AntieIceIndigoCombat.cs',runtime]:
    p=source/rel;p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/'SteriaBuild'/rel,p)

def insert(rel,anchor,addition):
    p=source/rel;data=p.read_bytes();nl=b'\r\n' if b'\r\n' in data else b'\n'
    a=anchor.replace('\n',nl.decode()).encode();b=addition.replace('\n',nl.decode()).encode()
    assert data.count(a)==1,(rel,'ambiguous anchor')
    p.write_bytes(data.replace(a,b+a))

registrations=''.join('            SteriaCustomEffects["Steria_AntieIceIndigo'+suffix+'"] = typeof(DiceAttackEffect_Steria_AntieIceIndigo'+suffix+');\n' for suffix in ['Hit','Slash','Pierce'])
for line in registrations.splitlines():assert line in (ROOT/'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')
insert('HarmonyPatches.cs','            SteriaCustomEffects["Steria_WindSlash"]',registrations)
direct='''                if (Steria.Vfx.AntieIceIndigoDefaults.TryCreateDirect(unit, destroyTime, out coreEffect))
                {
                    __result = coreEffect;
                    return false;
                }
'''
insert('ModInitializer.cs','                if (Steria.Vfx.SlazeyaWaterThunderDefaults.TryCreateDirect',direct)
assert (source/'ModInitializer.cs').read_text(encoding='utf-8-sig')==(ROOT/'SteriaBuild/ModInitializer.cs').read_text(encoding='utf-8-sig')
insert('Steria.csproj','</Project>', '  <ItemGroup>\n    <Compile Include="'+runtime.replace('/','\\')+'" />\n  </ItemGroup>\n')

payload=OUT/'payload';(payload/'Assemblies/AB').mkdir(parents=True)
shutil.copy2(bundle,payload/'Assemblies/AB'/bundle.name)
notice=payload/'Resource/ThirdPartyNotices/AntieIceIndigoSlash.txt';notice.parent.mkdir(parents=True)
lines=['Antie Ice / Indigo three-profile melee slash VFX',
       'Project-authored meshes, shaders, timing, integration and animation clips.',
       'Four user-provided textures/masks are included unchanged for this mod; no public open-source license is claimed.',
       'No game-extracted artwork, Slazeya art assets or third-party source pixels/code are included.',
       'Provenance: SteriaBuild/VFXSource/AntieIceIndigoSlash/provenance.json','']
lines += [item['original_filename']+' | SHA256 '+item['sha256'] for item in read(VFX/'provenance.json')['inputs']]
notice.write_text('\n'.join(lines)+'\n',encoding='utf-8')
order=['Assemblies/AB/steria_antie_ice_indigo_combat','Resource/ThirdPartyNotices/AntieIceIndigoSlash.txt','Assemblies/Steria.dll']
preserve=['Data/CardInfo.xml','Data/EquipPage_Librarian.xml','Data/EquipPage_Enemy.xml','Assemblies/AB/steria_slazeya_water_thunder_assets','Assemblies/AB/steria_slazeya_wind_combat','Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt','Resource/ThirdPartyNotices/SlazeyaWindCombat.txt']
diff=[]
for rel in ['HarmonyPatches.cs','ModInitializer.cs','Steria.csproj']:
    diff.extend(difflib.unified_diff((BASE/'SteriaBuild'/rel).read_text(encoding='utf-8-sig').splitlines(True),(source/rel).read_text(encoding='utf-8-sig').splitlines(True),fromfile='installed/'+rel,tofile='antie-release/'+rel))
(OUT/'source-changes.diff').write_text(''.join(diff),encoding='utf-8')
proof={'status':'PREPARED','created_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'art_candidate':approved['visual_candidate'],
       'game_root':str(GAME),'baseline_metadata':str(BASE/'built-metadata.json'),'baseline_dll':old['dll_sha256'],
       'changed_existing':['HarmonyPatches.cs','ModInitializer.cs'],'additions':['AntieIceIndigoCombat.cs',runtime],
       'project_before_sha256':sha(BASE/'SteriaBuild/Steria.csproj'),'project_after_sha256':sha(source/'Steria.csproj'),
       'sources':{p.relative_to(source).as_posix():sha(p) for p in source.rglob('*.cs')},
       'payload_order':order,'before_payload':{label:{rel:sha(root/rel) for rel in order} for label,root in [('game',GAME),('repo',REPO)]},
       'preserved':{label:{rel:sha(root/rel) for rel in preserve} for label,root in [('game',GAME),('repo',REPO)]},
       'bundle_sha256':sha(bundle),'approved_preview':approved,'scope':'Only Antie Hit/Slash/Penetrate core presentation; installed baseline code otherwise retained.'}
(OUT/'prepared.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('ANTIE_SCOPED_RELEASE_PREPARED',OUT,'only 2 existing source files patched, 2 source additions')
