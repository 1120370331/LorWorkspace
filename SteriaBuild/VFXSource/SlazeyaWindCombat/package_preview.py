from pathlib import Path
import sys,json,hashlib,subprocess
from PIL import Image,ImageDraw
import imageio_ffmpeg
p=Path(sys.argv[1]);media=[]
for d in sorted(p.iterdir()):
 if not d.is_dir():continue
 frames=sorted(d.glob('frame_*.png'))
 if not frames:continue
 assert len(frames) in (37,49)
 sheet=Image.new('RGB',(1440,588),'#10161b');draw=ImageDraw.Draw(sheet)
 for i,f in enumerate([2,6,8,13,22,45] if d.name.startswith('Pierce') else [1,3,5,10,17,30]):
  x=(i%3)*480;y=(i//3)*294
  with Image.open(d/f'frame_{f:03}.png') as im:sheet.paste(im.resize((480,270)),(x,y+24))
  draw.text((x+8,y+6),f'{d.name}  {f/60:.3f}s | native GPU',fill='white')
 file=p/(d.name+'-stages.jpg');sheet.save(file,quality=96);media.append(file)
 video=p/(d.name+'-60fps.mp4')
 subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(d/'frame_%03d.png'),'-c:v','libx264','-crf','16','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True);media.append(video)
(p/'media-hashes.json').write_text(json.dumps({f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in media},indent=2))
print('Native frame sheets and 60fps MP4 packaged',len(media))
