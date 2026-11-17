"""Freeze local preview evidence after the single-agent visual review; never deploy."""
from pathlib import Path
import hashlib,json,re,shutil,subprocess
from datetime import datetime,timezone
from PIL import Image,ImageDraw,ImageFont
import numpy as np
import imageio_ffmpeg

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
VISUAL='candidate-05-convex-to-camera';NATIVE='native-09';DLL_CANDIDATE='build-candidate-07'
visual=HERE/'evidence'/VISUAL
native=ROOT/'output/antie-ice-indigo'/NATIVE
target=HERE/'evidence'/NATIVE
target.mkdir(parents=True,exist_ok=True)
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
inputs=json.loads((native/'inputs.json').read_text())
assert digest(Path(inputs['dll']))==inputs['dll_sha256']
bundle=visual/'bundle/steria_antie_ice_indigo_combat'
assert digest(bundle)==inputs['bundle_sha256']
checks=(native/'checks.txt').read_text()
match=re.match(r'ANTIE_NATIVE_PASS (\d+) checks;',checks)
assert match and int(match.group(1))>=99
baseline=HERE/'evidence/candidate-01'
hit_frames=list((visual/'HitVertical/motion').glob('*.png'))+list((visual/'HitVertical/stages').glob('*.png'))
assert len(hit_frames)==77
assert all(digest(p)==digest(baseline/p.relative_to(visual)) for p in hit_frames),'Vertical slash must stay byte-identical across all stages and motion frames'
previous=HERE/'evidence/candidate-04-horizontal-mirror'
dual_frames=list((visual/'PierceDual/motion').glob('*.png'))+list((visual/'PierceDual/stages').glob('*.png'))
assert len(dual_frames)==77
assert all(digest(p)==digest(previous/p.relative_to(visual)) for p in dual_frames),'Dual slash must remain byte-identical to the approved orientation'
pixels={}
for path in sorted(native.glob('native-*.png')):
    shutil.copy2(path,target/path.name)
    if path.stem.endswith('-clear'):continue
    rgb=np.array(Image.open(path)).astype(int)
    clear=np.array(Image.open(path.with_name(path.stem+'-clear.png'))).astype(int)
    pixels[path.name]=int((np.abs(rgb-clear).max(axis=2)>12).sum())
    assert pixels[path.name]>2500,(path,pixels[path.name])
for name in ['checks.txt','inputs.json','assembly-location.txt']:
    shutil.copy2(native/name,target/name)
(target/'pixel-evidence.json').write_text(json.dumps(pixels,indent=2)+'\n')
sheet=Image.new('RGB',(1920,900),'#080f1b')
draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',23)
for row,(name,label) in enumerate([('Hit','打击：竖斩'),('Slash','斩击：横斩'),('Pierce','突刺：双刀')]):
    for col,side in enumerate(['left','right']):
        # Preserve image aspect ratio: fit the actual native camera frame inside each cell.
        pic=Image.open(target/f'native-{name}-{side}.png');pic.thumbnail((960,270),Image.Resampling.LANCZOS)
        sheet.paste(pic,(col*960+(960-pic.width)//2,row*300+30))
        draw.text((col*960+20,row*300+2),label+' · '+('朝左' if side=='left' else '朝右'),font=font,fill='#bdeaff')
sheet.save(target/'native-contexts.png')
comparison=Image.new('RGB',(1920,600),'#080f1b');draw=ImageDraw.Draw(comparison)
for col,label in enumerate(['上一版：误解为左右镜像','本版：凸刃朝屏幕，凹侧略微抬起']):draw.text((col*960+20,8),label,font=font,fill='#bdeaff')
for row,(name,label) in enumerate([('SlashHorizontal','横斩')]):
    for col,folder in enumerate([previous,visual]):
        comparison.paste(Image.open(folder/name/'stages/04.png'),(col*960,40+row*560))
        draw.text((col*960+20,48+row*560),label,font=font,fill='#bdeaff')
comparison.save(visual/'angle-before-after.png')
direction=Image.new('RGB',(1920,600),'#080f1b');draw=ImageDraw.Draw(direction)
for col,(filename,label) in enumerate([('peak-right-grey.png','固定侧视：凹侧朝上抬起'),('top-view.png','俯视：下方为屏幕，凸刃朝下方')]):
    direction.paste(Image.open(visual/'SlashHorizontal'/filename),(col*960,40))
    draw.text((col*960+20,8),label,font=font,fill='#bdeaff')
direction.save(visual/'convex-screen-direction.png')
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(visual/'review-60fps.mp4'),'-f','null','-'],check=True,capture_output=True)
candidate={
    'created_utc':datetime.now(timezone.utc).isoformat(),
    'status':'preview-ready; not deployed; not promoted to shared production library',
    'visual_candidate':VISUAL,'native_candidate':NATIVE,'dll_candidate':DLL_CANDIDATE,
    'unity':'2019.3.15f1','renderer':'D3D11 / Built-in / Gamma',
    'unit_cores':['SteriaBuilding:99000011','SteriaBuilding:13'],
    'mapping':{'Hit':'HitVertical','Slash':'SlashHorizontal','Penetrate':'PierceDual'},
    'native_checks':int(match.group(1)),'native_pixels':pixels,
    'vertical_slash_unchanged_frames':len(hit_frames),
    'dual_slash_unchanged_frames':len(dual_frames),'horizontal_screen_mirror':False,
    'horizontal_orientation':'convex cutting edge toward screen (-Z); concave inner edge rises upward (+Y) and away (+Z); preserved in both native actor facings',
    'plane_euler_degrees':{'HitVertical':[0,0,0],'SlashHorizontal':[-106,0,0],'PierceDual':[[0,246,0],[0,250,0]]},
    'bundle_sha256':digest(bundle),'dll_sha256':inputs['dll_sha256'],
    'preview_video':'evidence/'+VISUAL+'/review-60fps.mp4',
    'inventory':'evidence/'+VISUAL+'/asset-inventory.json',
    'source_integration_verified':True,'full_live_battle_verified':False,'deployed':False,
    'visual_review':'Single-agent separate review pass, as explicitly requested; no independent-agent review claimed.',
    'limitations':['Native fixture uses synthetic actor transforms and placeholder bodies, not Antie skin or a full reception.','Visual palette and texture licensing provenance remain user-supplied; no third-party artwork added.'],
    'integration_source_hashes':{str(p.relative_to(ROOT)).replace('\\','/'):digest(p) for p in [ROOT/'SteriaBuild'/name for name in ['AntieIceIndigoCombat.cs','HarmonyPatches.cs','ModInitializer.cs','Steria.csproj']]},
}
(HERE/'preview-candidate.json').write_text(json.dumps(candidate,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('ANTIE_REVIEW_READY',pixels)
