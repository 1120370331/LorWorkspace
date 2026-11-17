"""Verify the exact scoped DLL/PDB, unchanged baseline and native results before shipping."""
from pathlib import Path
import hashlib,json,re,shutil,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
out=ROOT/'output/antie-ice-indigo'/sys.argv[1];native=ROOT/'output/antie-ice-indigo'/sys.argv[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prepared=read(out/'prepared.json');old=read(Path(prepared['baseline_metadata']));new=read(out/'built-metadata.json')
def authored(m):
    root=Path(m['dll']).parents[3]
    return {Path(d['path']).relative_to(root).as_posix():d['checksum'] for d in m['documents'] if 'obj' not in Path(d['path']).relative_to(root).parts}
a,b=authored(old),authored(new)
assert {k for k,v in a.items() if b.get(k)!=v}==set(prepared['changed_existing'])
assert set(b)-set(a)==set(prepared['additions'])
assert old['pdb_matches'] and new['pdb_matches']
assert new['resources']==old['resources']
assert sorted(new['references'],key=lambda x:x['name'])==sorted(old['references'],key=lambda x:x['name'])
assert set(old['types']).issubset(new['types'])
for rel,value in prepared['sources'].items():assert sha(out/'SteriaBuild'/rel)==value==b[rel],rel
for rel in prepared['additions']:assert sha(out/'SteriaBuild'/rel)==sha(ROOT/'SteriaBuild'/rel),rel+' not current'
assert sha(out/'SteriaBuild/Steria.csproj')==prepared['project_after_sha256']
assert sha(Path(new['dll']))==new['dll_sha256']
assert sha(Path(new['pdb']))==new['pdb_sha256']
inputs=read(native/'inputs.json')
assert inputs['dll_sha256']==new['dll_sha256'] and inputs['bundle_sha256']==prepared['bundle_sha256']
checks=(native/'checks.txt').read_text()
assert 'ANTIE_RELEASE_NATIVE_PASS' in checks
assert re.search(r'ANTIE_NATIVE_PASS 111 checks;',checks)
assert re.search(r'WATER_THUNDER_NATIVE_PASS 189 checks;',checks)
reference=HERE.parent/'evidence/native-09';pixel_proof={}
for suffix in ['Hit','Slash','Pierce']:
    for facing in ['left','right']:
        name=f'native-{suffix}-{facing}.png'
        assert sha(native/name)==sha(reference/name),'Release differs from approved preview: '+name
        pixel_proof[name]=sha(native/name)
payload=out/'payload';shutil.copy2(Path(new['dll']),payload/'Assemblies/Steria.dll')
assert sha(payload/'Assemblies/AB/steria_antie_ice_indigo_combat')==prepared['approved_preview']['bundle_sha256']
files={p.relative_to(payload).as_posix():sha(p) for p in payload.rglob('*') if p.is_file()}
assert set(files)==set(prepared['payload_order'])
proof={**prepared,'status':'RELEASE_VERIFIED','native_log':str(native/'acceptance.log'),'native_result':checks,
       'baseline_authored_sources_total':len(a),'baseline_authored_sources_preserved':len(a)-len(prepared['changed_existing']),
       'embedded_resources_preserved':len(new['resources']),'dll_sha256':new['dll_sha256'],'payload':files,
       'approved_native_frames_pixel_equal':pixel_proof,'native_inputs_sha256':sha(native/'inputs.json')}
(out/'release.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('ANTIE_RELEASE_VERIFIED',len(a)-2,'baseline sources and',len(new['resources']),'resources preserved; 111 Antie + 189 Slazeya native checks; six approved GPU frames identical')
