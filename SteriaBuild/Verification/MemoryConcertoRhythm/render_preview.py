"""Queue-driven timing/pose diagram. This is NOT a game or Unity recording.

No rhythm constants: phases, events and holds come from queues.json emitted by
the linked production classes. Neutral actors and movement distances are schematic.
Existing effect PNGs are displayed as illustrative overlays, not a renderer test.
"""
from pathlib import Path
import bisect
import hashlib
import json
import math
import subprocess
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'output/memory-concerto-rhythm'
SHIP = ROOT / 'SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat'
FONT = 'C:/Windows/Fonts/msyh.ttc'
W, H, FPS = 1100, 660, 60
fonts = {s: ImageFont.truetype(FONT, s) for s in [14, 16, 20, 25, 32]}
data = json.loads((OUT / 'queues.json').read_text(encoding='utf8'))
segments, attacks, spans = [], [], []
clock = 0.
for die in data['sequence']:
    spans.append((clock, clock + die['minimumDuration'], f"{die['die']}  " + {'Long': '长', 'Medium': '中', 'Final': '更长'}[die['label']]))
    for s in die['segments']:
        seg = dict(s, start=clock+s['start'], end=clock+s['end'], die=die['die'])
        segments.append(seg)
        if s['self']['attackEvent'] == 'PRE':
            attacks.append((seg['start'], die['effectProfile']))
    clock += die['minimumDuration']
evasion_start = clock
for index, (a, b) in enumerate(zip(data['nativeFourthEvasion']['self'], data['nativeFourthEvasion']['opponent'])):
    duration = max(a['delay'], b['delay'])
    segments.append(dict(start=clock, end=clock+duration, self=a, opponent=b, die=4, phase='evasion', index=index))
    clock += duration
spans.append((evasion_start, clock, '4  原生闪避'))
total = clock
effect_frames = {}
for _, profile in attacks:
    cfg = json.loads((SHIP / (profile + '.json')).read_text(encoding='utf8'))
    ims = []
    for body, accent in zip(cfg['body'], cfg['accent']):
        im = Image.alpha_composite(Image.open(SHIP / body).convert('RGBA'), Image.open(SHIP / accent).convert('RGBA'))
        # Authored effect facing is left; the neutral attacker faces right.
        im = im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        bbox = im.getbbox()
        scale = 250 / (bbox[3]-bbox[1]) if profile == 'MemorySlash' else 305 / (bbox[2]-bbox[0])
        im = im.resize((round(im.width*scale), round(im.height*scale)), Image.Resampling.LANCZOS)
        ims.append(im)
    effect_frames[profile] = cfg, ims


def text(d, xy, value, size=20, color='#ddd8e7'):
    d.text(xy, value, font=fonts[size], fill=color)


def actor(d, x, motion, facing, color):
    ground = 410
    lean = {'Guard': -8, 'Slash': 14, 'Hit': 24, 'Penetrate': 26, 'Damaged': -25, 'Evade': -35}.get(motion, 0)
    head = (x + lean*facing, ground-160)
    shoulder = (head[0], ground-119)
    hip = (x, ground-64)
    d.ellipse((x-58, ground-7, x+58, ground+9), fill='#14141b')
    d.line([shoulder, hip], fill=color, width=25)
    d.ellipse((head[0]-18, head[1]-18, head[0]+18, head[1]+18), fill=color)
    d.line([(hip[0], hip[1]), (x-30, ground)], fill=color, width=16)
    d.line([(hip[0], hip[1]), (x+38, ground)], fill=color, width=16)
    hx, hy, tx, ty = {
        'Guard': (20,-95,37,-190), 'Slash': (62,-86,134,-22),
        'Hit': (62,-105,115,-170), 'Penetrate': (68,-119,154,-119),
        'Damaged': (-43,-98,-84,-127), 'Evade': (-10,-91,-58,-150),
    }.get(motion, (18,-82,62,-36))
    hand = (x+hx*facing, ground+hy)
    d.line([shoulder, hand], fill=color, width=13)
    d.line([hand, (x+tx*facing, ground+ty)], fill='#ede3d4', width=5)
    d.line([(hand[0]-9*facing, hand[1]-5), (hand[0]+9*facing, hand[1]+5)], fill='#a88ac4', width=5)
    text(d, (x-52, ground+22), motion, 16, color)


def position_at(t):
    # A schematic movement indicator. Durations are real queue minima; map
    # interpolation, actual knockback power and engine move completion are not.
    x, y = 325., 515.
    for s in segments:
        progress = max(0., min(1., (t-s['start']) / (s['end']-s['start'])))
        for key in ['self', 'opponent']:
            q = s[key]
            delta = {'MoveForward': 13*q['distanceRatio'], 'Knockback': -38, 'MoveBack': -22}.get(q['movement'], 0)
            if key == 'self': x += delta*progress
            else: y -= delta*progress
    return x, y


def render(t):
    t = min(t, total-1e-7)
    s = next(s for s in segments if s['start'] <= t < s['end'])
    im = Image.new('RGBA', (W,H), '#20212b')
    d = ImageDraw.Draw(im)
    text(d, (38,24), '回忆协奏之刃 · 长 — 中 — 更长', 32)
    text(d, (38,76), '正式动作队列驱动 · 正常速度 / 标称游戏时间 · CPU 演示，非实机录制', 16, '#b8afc9')
    phase = {'preparation':'调整 / 蓄势', 'attack':'出手 / 唯一 PRE', 'recovery':'收势 / 无新增事件', 'evasion':'第 4 骰原生闪避样例'}[s['phase']]
    text(d, (38,117), f"第 {s['die']} 骰   {phase}", 25, '#e6bb7c')
    text(d, (843,119), f'{t:0.2f} / {total:0.2f}s', 20)
    d.line((50,418,W-50,418), fill='#555061', width=2)
    ax,bx = position_at(t)
    actor(d, ax, s['self']['motion'], 1, '#d1c6e1')
    actor(d, bx, s['opponent']['motion'], -1, '#8c9bad')
    for start, profile in attacks:
        age = t-start
        if not 0 <= age < 2/3: continue
        cfg, ims = effect_frames[profile]
        j = max(0, bisect.bisect_right(cfg['frameTimes'],age)-1)
        fx = ims[j].copy()
        # Existing native fade: illustrative only; production renderer is unchanged.
        opacity = 1 if age <= .25 else max(0, (2/3-age)/(2/3-.25))
        fx.putalpha(fx.getchannel('A').point(lambda a: round(a*opacity)))
        im.alpha_composite(fx, (round(ax+100-fx.width/2), round(303-fx.height/2)))
    d = ImageDraw.Draw(im)
    event_count = sum(t >= start for start,_ in attacks)
    event_label = '准备段：无本骰攻击、恢复或伤害打印' if s['phase']=='preparation' else f'三击 PRE 已触发：{event_count} / 3'
    text(d, (38,467), event_label, 20, '#e6bb7c')
    # Three differently colored phases, driven by the same queue JSON.
    colors = {'preparation':'#6b5a86','attack':'#eea563','recovery':'#424b61','evasion':'#647b85'}
    left,right,top = 38,W-38,537
    def xx(v): return left+(right-left)*v/total
    for seg in segments:
        d.rectangle((xx(seg['start']),top,xx(seg['end'])-1,top+20),fill=colors[seg['phase']])
    for start,end,label in spans:
        caption = f'{label} · {end-start:.2f}s'
        width = d.textlength(caption, font=fonts[16])
        text(d, (min(xx(start)+4, right-width),508), caption, 16)
    for start,_ in attacks:
        d.line((xx(start),top-3,xx(start),top+27),fill='#ffd0a0',width=2)
    d.line((xx(t),top-8,xx(t),top+28),fill='white',width=3)
    text(d,(38,583),'时长为双方队列的最低值；真实位移完成、帧间 yield、骰子 UI 和游戏速度会延长或缩放。',14,'#aca6ba')
    text(d,(38,611),'角色为中性姿势示意；位移/特效挂点为示意。仅验证准备、出手、收势与闪避边界。',14,'#aca6ba')
    return im.convert('RGB')


def main():
    frames = [render(n/FPS) for n in range(math.ceil(total*FPS))]
    preview = OUT/'rhythm-60fps.mp4'
    pipe = subprocess.Popen([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-loglevel','error','-f','rawvideo','-vcodec','rawvideo','-pix_fmt','rgb24',
        '-s',f'{W}x{H}','-r',str(FPS),'-i','-','-an','-c:v','libx264','-pix_fmt','yuv420p','-crf','18',str(preview)],stdin=subprocess.PIPE)
    for im in frames: pipe.stdin.write(im.tobytes())
    pipe.stdin.close()
    if pipe.wait(): raise RuntimeError('ffmpeg failed')
    gifs = [im.resize((825,495),Image.Resampling.LANCZOS) for im in frames[::2]]
    gifs[0].save(OUT/'rhythm-30fps.gif',save_all=True,append_images=gifs[1:],duration=[30,40,30]*((len(gifs)+2)//3),loop=0)
    samples = []
    for s in segments:
        # Every attack and each preparation/recovery midpoint; all times from JSON.
        t = s['start'] if s['phase']=='attack' else (s['start']+s['end'])/2
        shot = render(t)
        file = OUT/f"phase-{len(samples):02d}-die{s['die']}-{s['phase']}.png"
        shot.save(file)
        samples.append((file,t,shot))
    sheet = Image.new('RGB',(W*2,H*math.ceil(len(samples)/2)), '#20212b')
    for i,(_,_,shot) in enumerate(samples): sheet.paste(shot,((i%2)*W,(i//2)*H))
    sheet.save(OUT/'phase-contact.png')
    (OUT/'preview-index.json').write_text(json.dumps({
        'queueSha256':hashlib.sha256((OUT/'queues.json').read_bytes()).hexdigest(),
        'normalSpeedDuration':len(frames)/FPS,'fps':FPS,'attackTimes':[start for start,_ in attacks],
        'fourthEvasionStarts':evasion_start,'samples':[{'file':p.name,'time':t} for p,t,_ in samples],
        'boundary':data['boundary'], 'rendering':'Neutral pose diagram; illustrative native movement and existing art overlays; not gameplay or Unity renderer QA.'
    },ensure_ascii=False,indent=2),encoding='utf8')
    print(f'PREVIEW_READY: {len(frames)} frames at {FPS}fps, {total:.2f}s; queue-derived phases; {preview}')


if __name__ == '__main__': main()
