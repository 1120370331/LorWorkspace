from pathlib import Path
import json,hashlib,subprocess,sys,csv
from collections import Counter
from PIL import Image,ImageDraw,ImageFont
import imageio_ffmpeg
import numpy as np

HERE=Path(__file__).resolve().parent;out=Path(sys.argv[1]).resolve();project=HERE/'UnityProject'
font=lambda size:ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size)
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
profiles=[('HitVertical','打击骰 · 竖斩'),('SlashHorizontal','斩击骰 · 横斩'),('PierceDual','突刺骰 · 错时双刀斩')]
sheet=Image.new('RGB',(1920,768),'#080f1b');d=ImageDraw.Draw(sheet);d.text((25,15),'安蒂 · 冰蓝 / 靛紫 · 三种独立造型与动画',font=font(30),fill='#bdeaff')
counts={}
for row,(name,label) in enumerate(profiles):
    folder=out/name;d.text((20,75+row*225),label,font=font(22),fill='#bdeaff')
    for col,index in enumerate([2,3,4,5,6]):
        p=folder/'stages'/f'{index:02}.png';pic=Image.open(p).resize((360,203),Image.Resampling.LANCZOS);sheet.paste(pic,(100+col*364,100+row*225))
    peak=np.array(Image.open(folder/'stages/04.png'));clear=np.array(Image.open(folder/'end-clear.png'));visible=int((np.abs(peak.astype(int)-clear.astype(int)).max(axis=2)>12).sum())
    assert visible>1200,(name,visible);counts[name]=visible
    for group,video in [('motion','slash-60fps.mp4'),('flow','flow-60fps.mp4')]:
        subprocess.run([ffmpeg,'-y','-v','error','-framerate','60','-i',str(folder/group/'%03d.png'),'-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/video)],check=True)
sheet.save(out/'three-profile-stages.png')
# One review video with all three profiles at normal speed, followed by labelled half speed.
cmd=[ffmpeg,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','960x608','-r','60','-i','-','-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'review-60fps.mp4')]
proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
try:
    for name,label in profiles:
        for repeats in [1,2]:
            for p in sorted((out/name/'motion').glob('*.png')):
                canvas=Image.new('RGB',(960,608),'#080f1b');canvas.paste(Image.open(p),(0,0));draw=ImageDraw.Draw(canvas)
                draw.text((20,552),label,font=font(23),fill='#bdeaff');draw.text((640,552),'正常速度' if repeats==1 else '0.5x 慢放',font=font(22),fill='#9ac9de')
                for _ in range(repeats):proc.stdin.write(canvas.tobytes())
    proc.stdin.close();assert proc.wait()==0
except BaseException:
    proc.kill();raise
assets=[];types={'.png':'Texture / Mask','.asset':'Mesh','.mat':'Material','.prefab':'Prefab','.shader':'Shader','.cs':'C# source','.unity':'Scene','.anim':'AnimationClip','.controller':'AnimatorController'}
for p in sorted((project/'Assets/AntieIceIndigo').rglob('*')):
    if not p.is_file() or p.suffix=='.meta':continue
    assert Path(str(p)+'.meta').exists(),p
    assets.append({'type':types.get(p.suffix,p.suffix),'path':p.relative_to(project).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'size_bytes':p.stat().st_size})
integration=[{'type':'Integration source','path':str(p.resolve()),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'size_bytes':p.stat().st_size} for p in [HERE.parents[1]/name for name in ['AntieIceIndigoCombat.cs','HarmonyPatches.cs','ModInitializer.cs','Steria.csproj']]]
bundle=out/'bundle/steria_antie_ice_indigo_combat'
integration.append({'type':'Native AssetBundle','path':str(bundle),'sha256':hashlib.sha256(bundle.read_bytes()).hexdigest(),'size_bytes':bundle.stat().st_size})
totals=dict(Counter(x['type'] for x in assets));(out/'asset-inventory.json').write_text(json.dumps({'assets':assets,'counts':totals,'integration':integration},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (out/'asset-inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(assets[0]));writer.writeheader();writer.writerows(assets)
lines=['# 安蒂新斩击：资产清单','',f'{len(assets)}项素材/源码；三种独立轮廓，3套AnimationClip与AnimatorController。四张用户原图未修改。','', '| 类型 | 文件 |','| --- | --- |']
lines += [f"| {a['type']} | [{a['path']}]({(project/a['path']).as_posix()}) |" for a in assets]
lines += ['', '另附单位接入源文件与原生资源包（未部署）：', '', '| 类型 | 文件 |', '| --- | --- |']
lines += [f"| {a['type']} | [{Path(a['path']).name}]({Path(a['path']).as_posix()}) |" for a in integration]
(out/'asset-inventory.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
paths=[p for p in (project/'Assets').rglob('*') if p.is_file()]+[p for p in (project/'ProjectSettings').glob('*') if p.is_file()]+list((project/'Packages').glob('*.json'))
manifest=[{'path':'UnityProject/'+p.relative_to(project).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(paths)]
manifest += [{'path':p.relative_to(HERE).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(list(HERE.glob('*.py'))+list(HERE.glob('*.ps1'))+[HERE/'provenance.json'])]
manifest += [{'path':p.relative_to(HERE).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted((HERE/'verification').glob('*')) if p.is_file()]
(out/'source-hashes.json').write_text(json.dumps(manifest,indent=2)+'\n');(out/'pixel-evidence.json').write_text(json.dumps(counts,indent=2)+'\n')
print('ANTIE_PACKAGE_READY',counts,totals)
