from pathlib import Path
import json,hashlib,subprocess,shutil
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;OUT=HERE/'previews'
names=['SeaPierce','SeaFarHit','MemorySlash','MemoryPierce','MemoryHit','MemoryGuard']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
def label(im,text,sub=''):
    im=im.convert('RGB');d=ImageDraw.Draw(im);d.rectangle((0,0,im.width,48),fill=(17,21,30));d.text((12,5),text,font=font,fill=(235,237,244));d.text((12,29),sub,font=small,fill=(166,182,204));return im
def tile_frame(i):
    page=Image.new('RGB',(1280,1128),(17,21,30))
    for n,name in enumerate(names):
        frame=Image.open(OUT/name/f'{i:02}.png').resize((640,360),Image.Resampling.LANCZOS)
        page.paste(label(frame,name+f'   {i/60:.3f} s','Unity 2019.3.15 | production renderer | native Bada scale reference'),((n%2)*640,(n//2)*376))
    return page
tiles=[tile_frame(i) for i in range(31)]
# MP4 encodes the actual 60fps source sequence, then a 17-frame empty pause.
grid=OUT/'video-tiles';grid.mkdir(exist_ok=True)
for i,im in enumerate(tiles+[tiles[-1]]*17):im.save(grid/f'{i:03}.png')
try:
    import imageio_ffmpeg
    ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()  # bundled build includes libx264
except ImportError:
    ffmpeg=shutil.which('ffmpeg') or 'C:/Users/rog/anaconda3/Library/bin/ffmpeg.exe'
result=subprocess.run([ffmpeg,'-y','-framerate','60','-i',str(grid/'%03d.png'),'-vf','loop=loop=4:size=48:start=0,setpts=N/(60*TB),format=yuv420p','-c:v','libx264','-crf','19','-preset','fast','-movflags','+faststart','-r','60','-an',str(OUT/'all-six-60fps.mp4')],capture_output=True,text=True)
if result.returncode:raise RuntimeError(result.stderr[-6000:])
# Browser-safe GIF: every other rendered frame at 30fps. Delays are all >=30ms,
# avoiding the common viewer clamp of 10ms frames that falsely slows an attack.
gif=tiles[::2]+[tiles[-1]]*8
gif[0].save(OUT/'all-six-30fps.gif',save_all=True,append_images=gif[1:],duration=[30,40,30]*8,loop=0,optimize=False)
times=[0,1,2,4,11,25];labels=['EARLY','REVEAL','CONTACT','FULL','TAIL RETREAT','END FRAGMENTS']
contact=Image.new('RGB',(1440,6*230),(17,21,30))
for row,name in enumerate(names):
    for col,(i,lbl) in enumerate(zip(times,labels)):
        im=Image.open(OUT/name/f'{i:02}.png').resize((240,135),Image.Resampling.LANCZOS)
        contact.paste(im,(col*240,row*230+63))
        d=ImageDraw.Draw(contact);d.text((col*240+8,row*230+10),name if col==0 else lbl,font=small,fill=(225,232,239));d.text((col*240+8,row*230+35),f't = {i/60:.3f} s',font=small,fill=(149,175,200))
contact.save(OUT/'phase-contact.png')
compare=Image.new('RGB',(1920,6*360),(17,21,30))
for row,name in enumerate(names):
    for col,(file,title) in enumerate([(OUT/name/'02.png','RIGHT / SCENE'),(OUT/(name+'_left_light.png'),'LEFT / LIGHT'),(OUT/(name+'_body_only.png'),'BODY ONLY / NO ACCENTS')]):
        im=Image.open(file).resize((640,360),Image.Resampling.LANCZOS);compare.paste(label(im,name+'  '+title), (col*640,row*360))
compare.save(OUT/'facing-and-body-contact.png')
record=json.loads((OUT/'candidate-hashes.json').read_text())
record['previewHashes']={str(p.relative_to(OUT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [OUT/'all-six-60fps.mp4',OUT/'all-six-30fps.gif',OUT/'phase-contact.png',OUT/'facing-and-body-contact.png',OUT/'checks.txt']}
for name in ['verify_source.py','assemble_previews.py','prepare_preview.py','run_preview.ps1']:
    path=HERE/name;record['files'][str(path.relative_to(HERE.parents[2])).replace('\\','/')]=hashlib.sha256(path.read_bytes()).hexdigest()
record['candidateId']=hashlib.sha256(json.dumps(record['files'],sort_keys=True).encode()).hexdigest()
record['mediaTiming']={'authoritative':'all-six-60fps.mp4','mp4Fps':60,'mp4FramesPerCycle':48,'mp4Cycles':5,'gifFps':30,'gifDelaysMs':[30,40,30],'gameplaySpeed':1}
(OUT/'candidate-hashes.json').write_text(json.dumps(record,indent=2)+'\n')
print('PREVIEWS_READY: authoritative 60fps MP4, browser-safe 30fps GIF, phase contact, two facings + body-only contact')
