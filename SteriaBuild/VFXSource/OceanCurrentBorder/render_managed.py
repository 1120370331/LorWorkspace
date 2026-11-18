"""CPU-composite the exact triangles emitted by unmodified production C# mesh code."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

repo = Path(__file__).resolve().parents[3]
out = repo / 'output/ocean-current-vfx/ui/managed'
frames = out / 'frames'
frames.mkdir(exist_ok=True)
shots = json.loads((out / 'production-meshes.json').read_text())
S = 3
FONT = 'C:/Windows/Fonts/simhei.ttf'
def font(size): return ImageFont.truetype(FONT, round(size*S))
def xy(point): return ((640+point[0]*1.2)*S, (416-point[1]*1.2)*S)
def text(draw, pos, value, size, fill=(183,201,213), anchor='mm'):
    draw.text((pos[0]*S,pos[1]*S), value, font=font(size), fill=fill, anchor=anchor)
labels = {'plain':'未强化', 'twice':'再次强化 · ×2', 'scaled-native-frame':'独立卡框变换 · 72% 缩放',
          'split-frame-union':'原生卡框分段 · 并集定位', 'reused-plain':'复用为同名未强化实例', 'consumed':'消费强化后'}
def render(shot):
    image=Image.new('RGB',(1280*S,720*S),(12,21,29)); draw=ImageDraw.Draw(image)
    text(draw,(640,57),'洋流 · 流强化书页边框',30,(211,230,240))
    text(draw,(640,100),'未替写生产网格 / 实际状态轨迹 · CPU 模拟 · 非游戏或 Unity 截图',17,(134,164,183))
    title=labels.get(shot['state'],f"持续流动  {shot['time']:.2f}s  /  2.40s 每圈")
    text(draw,(640,139),title,20,(139,207,234))
    for i,card in enumerate(shot['cards']):
        cx,cy=card['position']; scale=card['scale']; left,top=xy((cx-104*scale,cy+180*scale));right,bottom=xy((cx+104*scale,cy-180*scale))
        draw.rounded_rectangle((left,top,right,bottom),radius=4*S,fill=(29,42,53),outline=(62,84,97),width=1*S)
        px=(left+right)/2/S; unit=1.2*scale
        text(draw,(px,416-132*unit),'川流不息',19*unit,(204,219,224))
        ar=(left+13*unit*S, (416-101*unit)*S, right-13*unit*S,(416-16*unit)*S)
        draw.rounded_rectangle(ar,radius=2*S,fill=(37,61,76))
        # Deliberately neutral card fixture, not a fake game artwork or screenshot.
        text(draw,(px,416-57*unit),'卡图示意区域',12*unit,(121,156,173))
        if card['levels']:
            bx=px-67*unit; by=416-159*unit
            draw.ellipse(((bx-6*unit)*S,(by-6*unit)*S,(bx+6*unit)*S,(by+6*unit)*S),fill=(129,207,229))
            text(draw,(bx+28*unit,by),f"×{card['levels']}",14*unit,(183,232,245))
        for row in range(3):
            yy=416+(20+43*row)*unit
            text(draw,(px,yy),f"{4+row+card['levels']}-{7+row+card['levels']}",18*unit,(199,216,226))
            if card['levels']: text(draw,(px,yy+17*unit),f"(+{card['levels']})",14*unit,(167,214,232))
        text(draw,(px,664),'流强化' if card['levels'] else '同名 · 未强化',16,(148,183,202))
        layer=Image.new('RGBA',image.size); d=ImageDraw.Draw(layer)
        vertices=card['vertices']; triangles=card['triangles']
        for j in range(0,len(triangles),3):
            vv=[vertices[triangles[j+k]] for k in range(3)]
            rgba=tuple(round(sum(v[c] for v in vv)/3*255) for c in range(2,6))
            d.polygon([xy(v) for v in vv],fill=rgba)
        image=Image.alpha_composite(image.convert('RGBA'),layer).convert('RGB');draw=ImageDraw.Draw(image)
    return image.resize((1280,720),Image.Resampling.LANCZOS)

animation=[]
for shot in shots:
    image=render(shot)
    if shot['state']=='animation':
        index=len(animation);image.save(frames/f'current-{index:03d}.png');animation.append(image)
        if index in [0,9,18,27,36,54]: image.save(out/f'current-t{index/30:.1f}.png')
    else: image.save(out/(shot['state']+'.png'))
animation[0].save(out/'normal-speed.gif',save_all=True,append_images=animation[1:],duration=[30,30,40]*48,loop=0,disposal=2)
contact=Image.new('RGB',(1248,1040),(12,21,29))
for n,index in enumerate([0,9,18,27,36,54]):
    crop=animation[index].crop((490,173,790,645)).resize((300,472))
    x=12+(n%3)*416; y=16+(n//3)*512
    contact.paste(crop,(x+52,y+28));ImageDraw.Draw(contact).text((x+152,y),f't = {index/30:.1f}s',font=ImageFont.truetype(FONT,18),fill=(187,219,234))
contact.save(out/'stage-contact.png')
print('Rendered production mesh triangles, 144 frames at 30 fps; GIF uses repeating 30/30/40 ms delays.')
