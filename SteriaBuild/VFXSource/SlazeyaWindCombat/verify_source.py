from pathlib import Path
import hashlib
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
canonical=Path('C:/Users/rog/.codex/skills/unity-sword-slash-vfx/assets/unity/SwordSlashFlowBuiltInContractV1.shader')
installed=HERE/'UnityProject/Assets/SkillSwordSlash/ContractV1'/canonical.name
assert canonical.read_bytes()==installed.read_bytes(),'Canonical shader drift'
swept=Path('C:/Users/rog/.codex/skills/unity-sword-slash-vfx/assets/library/unity2019/Candidate/Shaders/SwordSlashSweptWedgeBuiltInV1.shader')
assert swept.read_bytes()==(HERE/'UnityProject/Assets/SlazeyaWindInputs'/swept.name).read_bytes(),'SweptWedge shader drift'
assert hashlib.sha256((HERE/'source_assets/wind-flow-cc0.png').read_bytes()).hexdigest()=='faeb3200d072fd44b2b61252b2d8f94abfd56609cef39dd89ffacbac01962f34','Registered CC0 texture changed'
assert hashlib.sha256((HERE/'source_assets/wind-impact-ring-mit.png').read_bytes()).hexdigest()=='a7d0a60c4ffada75eb10a08d52ca972add2481a300e8911bddbf9db889d0f899','Registered MIT ring changed'
assert (HERE/'source_assets/wind-impact-ring-mit.png').read_bytes()==(HERE/'UnityProject/Assets/SlazeyaWindInputs/wind-impact-ring-mit.png').read_bytes()
source=(ROOT/'SteriaBuild/SlazeyaWindVisual.cs').read_text()
assert '.material' not in source,'Per-frame material allocation'
assert 'Simulate(0,false,false,false)' in source
assert 'const float step=1f/120f' in source
assert 'public void Arrive()' in source
assert 'Elapsed>=.26f)Arrive()' not in source
builder=(HERE/'UnityProject/Assets/Editor/SlazeyaWindParticleBuilder.cs').read_text()
assert 'new Texture2D' not in builder,'No raster silhouette construction'
assert 'ParticleSystemRenderMode.Mesh' in builder
assert not list((HERE/'UnityProject/Assets/SlazeyaWind').glob('*.png')),'Full slash bitmap forbidden'
assert 'mesh.colors32=packedColors' in builder,'Unity 2019 native packed color contract'
assert (ROOT/'SteriaBuild/SlazeyaWindVisual.cs').read_bytes()==(HERE/'UnityProject/Assets/Runtime/SlazeyaWindVisual.cs').read_bytes()
print('SLAZEYA_SOURCE_PASS',hashlib.sha256(source.encode()).hexdigest())
