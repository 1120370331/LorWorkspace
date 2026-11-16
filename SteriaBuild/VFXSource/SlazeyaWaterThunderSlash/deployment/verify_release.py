from pathlib import Path
import hashlib,json,shutil,sys
ROOT=Path(__file__).resolve().parents[4];out=ROOT/'output/slazeya-water-thunder'/sys.argv[1];native=ROOT/'output/slazeya-water-thunder'/sys.argv[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prepared=read(out/'prepared.json');old=read(Path(prepared['baseline_metadata']));new=read(out/'built-metadata.json')
def authored(m):return {Path(d['path']).name:d['checksum'] for d in m['documents'] if 'obj' not in Path(d['path']).parts}
a,b=authored(old),authored(new)
assert {k for k,v in a.items() if b.get(k)!=v}==set(prepared['changed_existing']),'Unexpected existing source changed'
assert set(b)-set(a)=={Path(s).name for s in prepared['additions']}
baseline=Path(prepared['baseline_metadata']).parent/'SteriaBuild'
expected=(baseline/'HarmonyPatches.cs').read_text(encoding='utf-8-sig')
for suffix in ['Slash','Hit','Pierce']:
    expected=expected.replace('SteriaCustomEffects["Steria_SlazeyaWind'+suffix+'"] = typeof(DiceAttackEffect_Steria_SlazeyaWind'+suffix+');','SteriaCustomEffects["Steria_SlazeyaWind'+suffix+'"] = typeof(DiceAttackEffect_Steria_SlazeyaWaterThunder'+suffix+');')
assert (out/'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')==expected,'Registry changed beyond three keys'
impact=(out/'SteriaBuild/SlazeyaWindImpact.cs').read_text(encoding='utf-8-sig')
for extra in ['    private static DiceAttackEffect_Steria_SlazeyaWaterThunderBase waterPending;\n','        waterPending=effect as DiceAttackEffect_Steria_SlazeyaWaterThunderBase;\n','        if(waterPending!=null&&victim==target&&frame==Time.frameCount)\n            return waterPending.Confirm(type,target);\n','        waterPending=null;\n','        if(waterPending==effect){waterPending=null;victim=null;}\n']:
    assert extra in impact;impact=impact.replace(extra,'',1)
assert impact==(baseline/'SlazeyaWindImpact.cs').read_text(encoding='utf-8-sig'),'Hit route changed beyond water pairing'
assert new['resources']==old['resources'] and new['references']==old['references']
assert set(old['types']).issubset(new['types'])
for rel,value in prepared['sources'].items():assert sha(out/'SteriaBuild'/rel)==value,rel
for rel in prepared['additions']:assert sha(out/'SteriaBuild'/rel)==sha(ROOT/'SteriaBuild'/rel),rel+' not current'
assert sha(native/'UnityProject/Assets/Plugins/Steria.dll')==new['dll_sha256']
checks=(native/'checks.txt').read_text();assert checks.startswith('WATER_THUNDER_NATIVE_PASS'),checks
assert sha(native/'bundles/steria_slazeya_water_thunder_assets')==prepared['art_bundle_sha256']
payload=out/'payload';shutil.copy2(out/'SteriaBuild/bin/Release/net472/Steria.dll',payload/'Assemblies/Steria.dll')
record={**prepared,'status':'RELEASE_VERIFIED','baseline_authored_sources_preserved':len(a)-len(prepared['changed_existing']),'baseline_authored_sources_total':len(a),'new_source_files':len(b)-len(a),'embedded_resources_preserved':len(new['resources']),
        'native_log':str(native/'acceptance.log'),'native_result':checks,'dll_sha256':new['dll_sha256'],
        'payload':{p.relative_to(payload).as_posix():sha(p) for p in payload.rglob('*') if p.is_file()}}
(out/'release.json').write_text(json.dumps(record,indent=2)+'\n')
print('SCOPED_RELEASE_VERIFIED',record['baseline_authored_sources_preserved'],'old authored sources and',len(new['resources']),'resources unchanged;',len(b)-len(a),'new sources; two bounded existing changes')
print(json.dumps(record['payload'],indent=2))
