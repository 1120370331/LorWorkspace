"""Reuse the native fixture with the scoped release DLL, then run baseline regression."""
from pathlib import Path
import hashlib,json,runpy,shutil,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
release=ROOT/'output/antie-ice-indigo'/sys.argv[1];revision=sys.argv[2]
proof=json.loads((release/'prepared.json').read_text(encoding='utf-8'))
dll=release/'SteriaBuild/bin/Release/net472/Steria.dll'
sys.argv=[str(HERE.parent/'verification/prepare_native.py'),revision,str(dll)]
runpy.run_path(sys.argv[0],run_name='__main__')
out=ROOT/'output/antie-ice-indigo'/revision;project=out/'UnityProject'
shutil.copy2(HERE/'ReleaseNativeEntry.cs.txt',project/'Assets/Editor/AntieNativeEntry.cs')
shutil.copy2(ROOT/'SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/deployment/NativeSmoke.cs.txt',project/'Assets/Runtime/WaterThunderNativeSmoke.cs')
for rel in ['Assemblies/AB/steria_slazeya_water_thunder_assets','Assemblies/AB/steria_slazeya_wind_combat']:
    p=Path(proof['game_root'])/rel
    assert hashlib.sha256(p.read_bytes()).hexdigest()==proof['preserved']['game'][rel]
    shutil.copy2(p,out/'bundles'/p.name)
inputs=json.loads((out/'inputs.json').read_text());inputs['scoped_release']=str(release)
inputs['fixtures']={p.relative_to(project).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in project.glob('Assets/**/*.cs')}
(out/'inputs.json').write_text(json.dumps(inputs,indent=2)+'\n')
