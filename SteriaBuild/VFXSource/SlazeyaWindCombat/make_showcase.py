"""Package unaltered native frames with labels into a normal-speed, five-effect preview."""
from pathlib import Path
import subprocess
import sys
from PIL import Image, ImageDraw, ImageFont
import imageio_ffmpeg

root=Path(sys.argv[1]).resolve()
font_path=Path('C:/Windows/Fonts/msyh.ttc')
font=ImageFont.truetype(str(font_path),22) if font_path.exists() else ImageFont.load_default()
small=ImageFont.truetype(str(font_path),13) if font_path.exists() else font
labels=[('Slash','斩击'),('Hit','打击'),('Pierce','突刺'),('Guard','防御'),('Projectile','远程')]
output=root/'SlazeyaWind-showcase-60fps.mp4'
args=[imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
      '-f','rawvideo','-pixel_format','rgb24','-video_size','960x600','-framerate','60','-i','-',
      '-c:v','libx264','-crf','16','-pix_fmt','yuv420p','-movflags','+faststart',str(output)]
process=subprocess.Popen(args,stdin=subprocess.PIPE)
try:
    for kind,label in labels:
        frames=[]
        for i in range(49 if kind=='Pierce' else 37):
            canvas=Image.new('RGB',(960,600),'#101b20')
            with Image.open(root/f'{kind}-dark-left'/f'frame_{i:03}.png') as native:
                canvas.paste(native.convert('RGB'),(0,40))
            draw=ImageDraw.Draw(canvas)
            draw.text((18,5),'斯拉泽雅 · 风系粒子特效' if font_path.exists() else 'Slazeya Wind VFX',font=font,fill='#d7f9f1')
            draw.text((824,5),label if font_path.exists() else kind,font=font,fill='#70dfc7')
            draw.text((18,582),'Unity 2019 原生渲染 · 演示场景' if font_path.exists() else 'Unity 2019 native rendering / preview scene',font=small,fill='#9db5b6')
            frames.append(canvas.tobytes())
        for cycle in range(3):
            for frame in frames:process.stdin.write(frame)
            for _ in range(13):process.stdin.write(frames[-1])
finally:
    process.stdin.close()
assert process.wait()==0,'ffmpeg failed'
print(output)
