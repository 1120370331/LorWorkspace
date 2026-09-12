from pathlib import Path
import shutil,json

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'output/memory-flame-delivery'
PROJECT=OUT/'UnityProject'
ASSEMBLIES=OUT/'Runtime/Assemblies'
for p in [PROJECT/'Assets/Editor',PROJECT/'Packages',PROJECT/'ProjectSettings',ASSEMBLIES]:p.mkdir(parents=True,exist_ok=True)
source=Path(__file__).resolve().parent/'bin/Release/net472'
for p in source.iterdir():
    if p.suffix in ['.exe','.dll','.config'] and not p.name.startswith(('UnityEngine','Unity.TextMeshPro')):
        shutil.copyfile(p,ASSEMBLIES/p.name)
ship=ROOT/'SteriaBuild/SteriaModFolder/Resource/CustomAudio/MemoryFlame'
dest=OUT/'Runtime/Resource/CustomAudio/MemoryFlame';dest.mkdir(parents=True,exist_ok=True)
for p in ship.glob('*.wav'):shutil.copyfile(p,dest/p.name)
deps={'com.unity.ugui':'1.0.0','com.unity.textmeshpro':'2.0.1'}
deps.update({'com.unity.modules.'+m:'1.0.0' for m in ['audio','animation','physics','physics2d','ui','particlesystem','imageconversion','assetbundle','jsonserialize']})
(PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}),encoding='utf8')
(PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.3.15f1\n',encoding='utf8')
(PROJECT/'Assets/Editor/Fixture.asmdef').write_text(json.dumps({'name':'Steria.MemoryAudio.Editor','includePlatforms':['Editor']}),encoding='utf8')
shutil.copyfile(Path(__file__).with_name('UnityFixture.cs.txt'),PROJECT/'Assets/Editor/MemoryFlameFixture.cs')
(OUT/'repository-root.txt').write_text(str(ROOT),encoding='utf8')
print('UNITY_AUDIO_FIXTURE_PREPARED: isolated project and exact candidate mod/PCM')
