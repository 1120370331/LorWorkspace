from pathlib import Path
import hashlib,json,sys,subprocess
import imageio_ffmpeg
import numpy as np
from PIL import Image,ImageDraw,ImageFont
out=Path(sys.argv[1]).resolve();here=Path(__file__).resolve().parent
project=Path(sys.argv[2]).resolve() if len(sys.argv)>2 else here/'UnityProject'
font_path='C:/Windows/Fonts/msyh.ttc'
font=lambda size:ImageFont.truetype(font_path,size)
labels=['01 高处起刀 · 45ms','02 亮部扫过 · 155ms','03 命中减速 · 215ms','04 再加速扫落 · 330ms','05 水纹散开 · 535ms','06 蒸散余波 · 790ms']
sheet=Image.new('RGB',(1560,1480),'#080f1b');d=ImageDraw.Draw(sheet)
d.text((28,16),'斯拉泽雅 · 水 / 雷斩击',font=font(30),fill='#bdeaff')
d.text((29,60),'Unity实际GPU渲染 · 移动刷形蒙版扫光 · 发散水尾 · 蒸发水纹散点图',font=font(16),fill='#63869d')
for i,label in enumerate(labels):
    image=Image.open(out/'composite'/f'{i+1:02}.png').resize((752,423),Image.Resampling.LANCZOS)
    x=20+(i%2)*775;y=95+(i//2)*458
    sheet.paste(image,(x,y+26));d.text((x+9,y+2),label,font=font(19),fill='#b3d9f0');d.rectangle((x,y,x+752,y+449),outline='#29445a')
sheet.save(out/'six-stages.png')
layer_names=['L1_WaterBlade','L2_SplashWake','L3_Lightning','L4_BladeCore']
titles=['L1 水刃主体','L2 水花尾流','L3 雷电弧光','L4 刀光亮芯']
layers=Image.new('RGB',(1600,860),'#080f1b');d=ImageDraw.Draw(layers)
d.text((24,15),'四层拆解 / 同一时间与相机',font=font(26),fill='#c4e9ff')
for row,(name,title) in enumerate(zip(layer_names,titles)):
    y=64+row*195;d.text((22,y),title,font=font(18),fill='#a5d6ec')
    for col in range(6):
        pic=Image.open(out/name/f'{col+1:02}.png').resize((256,144),Image.Resampling.LANCZOS);x=20+col*262
        layers.paste(pic,(x,y+29));d.text((x+5,y+31),str(col+1).zfill(2),font=font(13),fill='#91b3c8')
layers.save(out/'four-layers.png')
# Technical texture contact sheet; PNG source pixels remain unchanged.
texdir=project/'Assets/WaterThunder/Textures'
texsheet=Image.new('RGB',(1200,((len(list(texdir.glob('*.png')))+2)//3)*225),'#192332');d=ImageDraw.Draw(texsheet)
for i,p in enumerate(sorted(texdir.glob('*.png'))):
    x=(i%3)*400;y=(i//3)*225
    pic=Image.open(p).convert('RGBA');pic.thumbnail((380,180));texsheet.paste(pic,(x+10,y+30),pic)
    d.text((x+8,y+5),p.name,font=font(15),fill='#c4dceb')
texsheet.save(out/'texture-and-mask-atlas.png')
baseline=np.asarray(Image.open(out/'end-clear.png'),dtype=int)
counts={}
for name in ['composite']+layer_names:
    counts[name]=[]
    for i in range(1,7):
        a=np.asarray(Image.open(out/name/f'{i:02}.png'),dtype=int)
        count=int((np.abs(a-baseline).max(axis=2)>5).sum());counts[name].append(count)
    assert max(counts[name])>35,(name,counts[name])
assert counts['composite'][3]>counts['composite'][0]*2
energy=[]
for i in range(1,7):
    a=np.asarray(Image.open(out/'composite'/f'{i:02}.png'),dtype=int)
    energy.append(int(np.maximum(a-baseline,0).sum()))
# Dispersing flecks can cover more pixels while fading; compare energy, not painted area.
assert energy[5]<energy[3]*.35,energy
hit_before=np.asarray(Image.open(out/'impact-only/00.png'),dtype=int)
hit_peak=np.asarray(Image.open(out/'impact-only/02.png'),dtype=int)
assert np.array_equal(hit_before,baseline),'Impact leaked before contact'
assert (np.abs(hit_peak-baseline).max(2)>5).sum()>100,'No visible impact particles'
(out/'pixel-evidence.json').write_text(json.dumps(counts,indent=2))
for label,source_folder,times in [
    ('命中水雷爆发','impact-only',[180,190,215,250,300,380,480,650]),
    ('水纹蒙版溶解 / 散点扩散','evaporation-only',[360,440,520,600,720,880]),
    ('固定Alpha=1 · 仅看蒙版侵蚀','dissolve-mask-only',[360,440,520,600,720,880]),
    ('树状电枝 · 先经过与低储能区段先衰减','lightning-discharge',[160,215,270,350,450,600]),
    ('命中前后连续帧','contact-frames',[round(i/60*1000) for i in range(8,23)])]:
    files=sorted((out/source_folder).glob('*.png'));cols=4 if len(files)>6 else 3
    strip=Image.new('RGB',(cols*400,55+((len(files)+cols-1)//cols)*250),'#080f1b');draw=ImageDraw.Draw(strip)
    draw.text((16,10),label,font=font(23),fill='#c5eaff')
    for i,file in enumerate(files):
        x=(i%cols)*400;y=55+(i//cols)*250
        strip.paste(Image.open(file).resize((400,225),Image.Resampling.LANCZOS),(x,y+22));draw.text((x+12,y),f'{times[i]}ms',font=font(16),fill='#90c7e7')
    strip.save(out/(source_folder+'-stages.png'))
files=sorted((out/'early-head').glob('*.png'))
if files:
    early=Image.new('RGB',(1600,555),'#080f1b');draw=ImageDraw.Draw(early);draw.text((16,10),'尖头先入场，前锋经过后展开水流',font=font(23),fill='#c5eaff')
    for i,file in enumerate(files):
        x=i%4*400;y=55+i//4*250;early.paste(Image.open(file).resize((400,225),Image.Resampling.LANCZOS),(x,y+22));draw.text((x+12,y),f'{(i+1)/60*1000:.0f}ms',font=font(16),fill='#90c7e7')
    early.save(out/'early-head-stages.png')
if (out/'water-drop-dynamics').exists():
    dynamic=Image.new('RGB',(1440,950),'#080f1b');draw=ImageDraw.Draw(dynamic)
    draw.text((18,12),'水滴拉伸回圆 / 水丝收颈断开 · Unity材质放大',font=font(24),fill='#c5eaff')
    for row,(folder,title) in enumerate([('water-drop-dynamics','水滴'),('water-thread-dynamics','水丝')]):
        for j,frame in enumerate([0,3,6,11,17,23]):
            x=j%3*480;y=58+row*440+j//3*214
            dynamic.paste(Image.open(out/folder/f'{frame:03}.png').resize((480,270),Image.Resampling.LANCZOS).crop((0,35,480,225)),(x,y+22))
            draw.text((x+12,y),f'{title}  {frame/60*1000:.0f}ms',font=font(17),fill='#90c7e7')
    dynamic.save(out/'water-dynamics-stages.png')
# Plot the actual C# sampling output rather than a separate approximation of its curve.
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
trace=np.genfromtxt(out/'motion-profile.csv',delimiter=',',names=True)
fig,axes=plt.subplots(2,1,figsize=(10,6),sharex=True,layout='constrained')
axes[0].plot(trace['time'],trace['progress'],color='#008ac1',linewidth=2);axes[0].set_ylabel('Blade path (0-1)')
axes[1].plot(trace['time'],trace['speed'],color='#7352c7',linewidth=2);axes[1].set_ylabel('Path speed / second');axes[1].set_xlabel('Time (seconds)')
for ax in axes:
    ax.axvline(.19,color='#b94839',linestyle='--',label='Contact 190ms');ax.axvspan(.19,.25,alpha=.12,color='#f8ba40',label='Brief contact slowdown');ax.set_xlim(0,.36);ax.grid(alpha=.2)
axes[0].legend(loc='upper left');fig.suptitle('Accelerate - brief contact slowdown - accelerate again');fig.savefig(out/'impact-speed-curve.png',dpi=160);plt.close(fig)
(out/'pixel-evidence.json').write_text(json.dumps(counts,indent=2))
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
if ffmpeg:
    subprocess.run([ffmpeg,'-y','-v','error','-framerate','60','-i',str(out/'motion/%03d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p',str(out/'slash-60fps.mp4')],check=True)
imgs=[Image.open(file).resize((640,360)) for file in sorted((out/'motion').glob('*.png'))]
imgs[0].save(out/'slash-loop.gif',save_all=True,append_images=imgs[1:],duration=17,loop=0)
# Review reel: one attack at true speed, then the same attack explicitly labelled 0.5x.
review_cmd=[ffmpeg,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','960x608','-r','60','-i','-','-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'review-60fps.mp4')]
proc=subprocess.Popen(review_cmd,stdin=subprocess.PIPE)
try:
    files=sorted((out/'motion').glob('*.png'))
    for cycle,repeats in [(1,1),(2,2)]:
        for i,file in enumerate(files):
            canvas=Image.new('RGB',(960,608),'#080f1b');canvas.paste(Image.open(file),(0,0));draw=ImageDraw.Draw(canvas)
            t=i/60;phase='亮部加速扫过' if t<.19 else '命中 · 短暂减速' if t<.25 else '再次加速' if t<.34 else '蒙版溶解 / 余纹淡出' if t<1.04 else '循环间隔'
            title='正常速度 · 单次斩击 / 移动蒙版扫光' if repeats==1 else '0.5x慢放 · 同一次斩击'
            draw.text((22,549),title,font=font(18),fill='#badff4');draw.text((500,549),f'{t:.3f}s  {phase}',font=font(18),fill='#91d6f3')
            draw.rectangle((22,589,938,592),fill='#233747');draw.rectangle((22,589,22+int(916*min(t/1.04,1)),592),fill='#55c6ee')
            data=canvas.tobytes()
            for _ in range(repeats):proc.stdin.write(data)
    proc.stdin.close()
    assert proc.wait()==0,'Review video encode failed'
except BaseException:
    proc.kill();raise
if (out/'water-drop-dynamics').exists():
    cmd=[ffmpeg,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1280x460','-r','60','-i','-','-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'water-dynamics-60fps.mp4')]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    try:
        for loop in range(3):
            for i in range(32):
                canvas=Image.new('RGB',(1280,460),'#080f1b');draw=ImageDraw.Draw(canvas)
                for column,folder in enumerate(['water-drop-dynamics','water-thread-dynamics']):
                    canvas.paste(Image.open(out/folder/f'{i:03}.png').resize((640,360),Image.Resampling.LANCZOS),(column*640,55))
                draw.text((20,12),'水滴：拉伸 / 回圆 / 表面波动',font=font(23),fill='#bdeaff');draw.text((660,12),'水丝：拉长 / 收颈 / 分离',font=font(23),fill='#bdeaff')
                draw.text((20,424),f'材质放大 · 0.5x · 固定中心观察形变   {i/60:.3f}s',font=font(18),fill='#89b9d0')
                data=canvas.tobytes();proc.stdin.write(data);proc.stdin.write(data)
        proc.stdin.close();assert proc.wait()==0,'Water dynamics video encode failed'
    except BaseException:
        proc.kill();raise
if (out/'dissolve-exit').exists():
    cmd=[ffmpeg,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','960x608','-r','60','-i','-','-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'dissolve-fade-60fps.mp4')]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    try:
        files=sorted((out/'dissolve-exit').glob('*.png'))
        for repeat_count in [1,2]:
            for i,file in enumerate(files):
                canvas=Image.new('RGB',(960,608),'#080f1b');canvas.paste(Image.open(file),(0,0));draw=ImageDraw.Draw(canvas)
                title='溶解退场 · 正常速度' if repeat_count==1 else '溶解退场 · 同段0.5x慢放'
                draw.text((20,550),title,font=font(20),fill='#bdeaff');draw.text((520,550),f'{.32+i/60:.3f}s · 先侵蚀，再淡出',font=font(18),fill='#91c7dd')
                data=canvas.tobytes()
                for _ in range(repeat_count):proc.stdin.write(data)
        proc.stdin.close();assert proc.wait()==0,'Dissolve exit video encode failed'
    except BaseException:
        proc.kill();raise
if (out/'white-fluid-spin').exists():
    cmd=[ffmpeg,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','960x608','-r','60','-i','-','-an','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'white-fluid-spin-60fps.mp4')]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    try:
        files=sorted((out/'white-fluid-spin').glob('*.png'))
        for repeats in [1,2]:
            for i,file in enumerate(files):
                canvas=Image.new('RGB',(960,608),'#080f1b');canvas.paste(Image.open(file),(0,0));draw=ImageDraw.Draw(canvas)
                draw.text((20,551),'白色流体四方纹 · 固定月牙观察旋流',font=font(20),fill='#bdeaff');draw.text((600,551),'正常速度' if repeats==1 else '0.5x慢放',font=font(19),fill='#91c7dd')
                for _ in range(repeats):proc.stdin.write(canvas.tobytes())
        proc.stdin.close();assert proc.wait()==0,'White fluid spin video encode failed'
    except BaseException:
        proc.kill();raise
paths=[p for p in (project/'Assets').rglob('*') if p.is_file()]
paths += list((project/'ProjectSettings').glob('*'))+list((project/'Packages').glob('*.json'))
manifest=[{'path':'UnityProject/'+p.relative_to(project).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(paths)]
manifest += [{'path':p.relative_to(here).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(list(here.glob('*.py'))+list(here.glob('*.ps1'))+[here/'provenance.json'])]
(out/'source-hashes.json').write_text(json.dumps(manifest,indent=2))
(out/'bundle-hashes.json').write_text(json.dumps([{'file':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in (out/'bundle').glob('*') if p.is_file()],indent=2))
print('PREVIEW_PACKAGE_PASS',counts)
