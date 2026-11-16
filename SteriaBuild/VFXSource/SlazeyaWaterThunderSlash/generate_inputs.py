"""Original UV-space technical textures. No old slash or game pixels are used."""
from pathlib import Path
import hashlib
import json
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ASSETS = HERE / 'UnityProject/Assets/WaterThunder'
TEX = ASSETS / 'Textures'
SHADERS = ASSETS / 'Shaders'
SKILL = Path('C:/Users/rog/.codex/skills/unity-sword-slash-vfx')
for directory in (TEX, SHADERS, HERE / 'source_assets'):
    directory.mkdir(parents=True, exist_ok=True)

def save(name, a):
    Image.fromarray(np.uint8(np.clip(a, 0, 1) * 255 + .5)).save(TEX / name)

def noise(u, v, seed=17):
    rng = np.random.default_rng(seed)
    result = np.zeros_like(u + v)
    weight = 0
    for freq, amp in ((1, .26), (2, .18), (4, .1), (8, .06), (16, .025)):
        for _ in range(3):
            kx, ky = rng.integers(1, 4, size=2) * freq
            result += np.sin(2 * np.pi * (u * kx + v * ky) + rng.uniform(0, 6.283)) * amp
            weight += amp
    return np.clip(.5 + result / weight * .7, 0, 1)

w, h = 1024, 256
u, v = np.meshgrid(np.linspace(0, 1, w), np.linspace(1, 0, h))
tau = np.pi * 2
grain = noise(u, v, 91)
filaments = np.zeros_like(u)
broad = np.zeros_like(u)
for k in range(7):
    center = .05 + k * .135 + .115 * np.sin(tau * (u * (1.05 + k * .045)) + k * 2.13)
    center += .045 * np.sin(tau * (u * 2.7) + k * 1.41)
    width = .010 + .012 * (.5 + .5 * np.sin(tau * u * 2 + k))
    along = .3 + .7 * np.clip(np.sin(u * 13 + k * 3.7) + .6, 0, 1)
    filaments = np.maximum(filaments, np.exp(-((v - center) / width) ** 2) * along)
    broad += np.exp(-((v - center) / (.055 + width)) ** 2) * along * .24
edge = np.clip(v / .045, 0, 1) * np.clip((1 - v) / .11, 0, 1)
alpha = np.clip((.30 + .32 * grain + broad * .45 + filaments * .54) * edge, 0, .96)
body = np.zeros((h, w, 4))
shine = np.clip(filaments * .78 + broad * .46, 0, 1)
body[..., 0] = .035 + shine * .47
body[..., 1] = .37 + shine * .55
body[..., 2] = .62 + shine * .38
body[..., 3] = alpha
save('L1_WaterBody.png', body)
mask = np.ones_like(u)
# Torn inner edge and elongated negative spaces; not uniform sine stripes.
inner = .84 + .06 * np.sin(u * 39 + 1.7 * np.sin(u * 11)) + .07 * grain
mask *= np.clip((inner - v) / .045, 0, 1)
for a, b, ru, rv in ((.18,.65,.11,.07),(.41,.77,.13,.065),(.58,.53,.12,.043),(.77,.7,.085,.07),(.32,.28,.11,.032)):
    hole = ((u-a)/ru)**2 + ((v-b-.025*np.sin(u*30))/rv)**2
    mask *= np.clip((hole-.65)/.5,0,1)
tail_weight=np.clip((.37-u)/.25,0,1)
mask=mask*(1-tail_weight)+edge*tail_weight
save('L1_WaterMask.png', np.repeat(mask[...,None],3,axis=2))
# Five distinct, broken water filaments open into a fan at the trailing end.
tail_fan=np.zeros_like(u)
fan=np.clip((.38-u)/.38,0,1)
for k,branch in enumerate([-2.1,-1.0,-.12,1.05,2.0]):
    c=.5+branch*(.035+.18*fan)+.035*np.sin(u*35+k*1.6)
    width=.014+.006*(k%2)+.030*(1-fan)
    strand=np.exp(-((v-c)/width)**2)
    breakup=.2+.8*np.clip(np.sin(u*48+k*2.1)+.6,0,1)
    rear_fade=np.clip((u-[0,.025,.07,.015,.04][k])/.03,0,1)
    tail_fan=np.maximum(tail_fan,strand*breakup*rear_fade)
tail_mask=1-tail_weight+tail_weight*tail_fan
save('L1_TailFanMask.png',np.repeat(tail_mask[...,None],3,axis=2))

# Clamp brush wedge; this travels over fixed pathT instead of unveiling the mesh.
su,sv=np.meshgrid(np.linspace(0,1,512),np.linspace(1,0,256))
rise=np.clip(su/.24,0,1);fall=np.clip((1-su)/.055,0,1)
# Leading end u=1 is a point at across=0; the water sheet widens behind it.
wedge=(su**1.0)*rise*fall*np.clip(((1-su)-.74*sv)/.065,0,1)*(.7+.3*noise(su,sv,89))
wedge/=wedge.max()
save('Shared_SweepBrushMask.png',np.repeat(wedge[...,None],3,axis=2))

# Four curved fluid sides, tapered and slightly broken; kept inside the water by the shader.
wx,wy=np.meshgrid(np.linspace(-1,1,256),np.linspace(1,-1,256))
angle=np.arctan2(wy,wx);radius=np.sqrt(wx*wx+wy*wy)
square_radius=.56/(np.abs(np.cos(angle))**8+np.abs(np.sin(angle))**8)**.125
square_radius+=.022*np.sin(angle*5+.7)
stroke_width=.018+.010*(.5+.5*np.sin(angle*3+1.2))
quad=np.exp(-((radius-square_radius)/stroke_width)**2)*(.40+.60*np.clip(np.sin(angle*2.5+.8)+.65,0,1))
quad+=.24*np.exp(-((radius-square_radius+.045)/.014)**2)*np.clip(np.cos(angle*2+.4),0,1)
save('L1_WhiteFluidQuadMask.png',np.repeat(np.clip(quad,0,1)[...,None],3,axis=2))

# Periodic data has identical opposite borders, and no one-shot silhouette baked in.
p, q = np.meshgrid(np.linspace(0,1,256), np.linspace(0,1,256))
flow = noise(p,q,48)
fy = noise(p,q,71)
caustic = np.exp(-((np.sin(tau*(p*3+.17*np.sin(tau*q*2))) + .55*np.sin(tau*(q*4+p*2)))/.17)**2)
save('Shared_FlowRepeat.png', np.stack([flow,fy,.5+0*flow],axis=2))
save('Shared_CausticsRepeat.png', np.repeat((.17+.83*caustic)[...,None],3,axis=2))
save('Shared_ErosionRepeat.png', np.repeat(noise(p,q,207)[...,None],3,axis=2))

# Water body and true linear normal data; moving specular and deforming contours are shader-driven.
x, y = np.meshgrid(np.linspace(-1,1,128), np.linspace(1,-1,128))
radius = np.sqrt(x*x+y*y)
a = np.clip((1-radius)/.045,0,1)
thickness=np.sqrt(np.clip(1-radius*radius,0,1))
splash = np.zeros((128,128,4))
splash[...,0] = .035+.13*thickness
splash[...,1] = .27+.37*thickness
splash[...,2] = .52+.42*thickness
splash[...,3] = a
save('L2_Splash.png',splash)
save('L2_SplashMask.png',np.repeat(a[...,None],3,axis=2))
nx=x*.92;ny=y*.92;nz=np.sqrt(np.clip(1-nx*nx-ny*ny,0,1))
save('L2_WaterNormal.png',np.stack([nx*.5+.5,ny*.5+.5,nz],axis=2))

# Narrow white center with violet falloff. Shape changes come from the branch mesh.
for layer, sigma, rgb in [('L3_Lightning',.15,(.42,.18,1)),('L4_Core',.12,(.16,.63,1))]:
    pu,pv=np.meshgrid(np.linspace(0,1,256),np.linspace(1,0,64))
    d=np.abs(pv-.5)
    bright=np.exp(-(d/sigma)**2)
    halo=np.exp(-(d/(sigma*3))**2)
    rgba=np.zeros((64,256,4))
    for c in range(3):rgba[...,c]=rgb[c]*halo+(1-rgb[c])*bright
    rgba[...,3]=np.clip(bright*.85+halo*.15,0,1)
    save(layer+'.png',rgba)
    start_soft=.003 if layer=='L4_Core' else .035
    save(layer+'Mask.png',np.repeat((np.clip(pu/start_soft,0,1)*np.clip((1-pu)/.012,0,1))[...,None],3,axis=2))

# One original transparent water-vapour scatter map: open micro-ripples, wisps and specks.
# No closed circles. Marks originate near the water arc, then the runtime sheet disperses.
scale=2;size=512;canvas=Image.new('L',(size*scale,size*scale),0);draw=ImageDraw.Draw(canvas)
rng=np.random.default_rng(3910)
for k in range(205):
    t=rng.uniform(.035,.99);s=1-t
    c=s**3*np.array([-.85,3.65])+3*s*s*t*np.array([2.65,3.65])+3*s*t*t*np.array([3.0,.9])+t**3*np.array([.8,.18])
    c+=rng.normal(0,[.14,.16])
    px=((c[0]-.65)/3.7+.5)*size;py=(.5-(c[1]-1.95)/3.85)*size
    radius=rng.uniform(1.7,6.0);angle=rng.uniform(0,tau);sweep=rng.uniform(.5,2.0)
    points=[]
    for q in np.linspace(0,1,16):
        a=angle+(q-.5)*sweep
        points.append(((px+np.cos(a)*radius*1.7)*scale,(py+np.sin(a)*radius*.55)*scale))
    draw.line(points,fill=int(rng.uniform(80,210)),width=max(1,int(rng.uniform(.65,1.35)*scale)))
    if k%3==0:draw.ellipse((px*scale-1.2,py*scale-1.2,px*scale+1.2,py*scale+1.2),fill=170)
alpha=np.asarray(canvas.filter(ImageFilter.GaussianBlur(.6)).resize((size,size),Image.Resampling.LANCZOS),dtype=float)/255
border=np.minimum(np.arange(size),np.arange(size)[::-1])/9
alpha*=np.clip(border[None,:],0,1)*np.clip(border[:,None],0,1)
field_u,field_v=np.meshgrid(np.linspace(0,1,size),np.linspace(0,1,size))
ripple_field=noise(field_u+.028*np.sin(field_v*17),field_v+.021*np.sin(field_u*21),717)
blurred=np.asarray(Image.fromarray(np.uint8(alpha*255)).filter(ImageFilter.GaussianBlur(6)),dtype=float)/255
grain_source=Image.fromarray(np.uint8(np.random.default_rng(927).random((size,size))*255))
def field_at(radius):
    f=np.asarray(grain_source.filter(ImageFilter.GaussianBlur(radius)),dtype=float)
    lo,hi=np.quantile(f,[.015,.985]);return np.clip((f-lo)/max(hi-lo,1),0,1)
height=.64*field_at(3.5)+.24*field_at(9)+.07*ripple_field+.12*blurred
low,high=np.quantile(height,[.015,.985]);height=np.clip((height-low)/(high-low),0,1)
drop_height=.70*field_at(9)+.23*field_at(16)+.07*ripple_field
low,high=np.quantile(drop_height,[.015,.985]);drop_height=np.clip((drop_height-low)/(high-low),0,1)
# Packed data: R=fine body erosion, G=softer drop erosion, A=visible ripple scatter.
scatter=np.zeros((size,size,4));scatter[...,0]=height;scatter[...,1]=drop_height;scatter[...,2]=.85+.15*alpha;scatter[...,3]=alpha
save('Evap_WaterScatter.png',scatter)

# Compact contact flash.
fx,fy=np.meshgrid(np.linspace(-1,1,128),np.linspace(1,-1,128))
rad=np.sqrt(fx*fx+fy*fy);bright=np.exp(-(rad/.19)**2);halo=np.exp(-(rad/.47)**2)
flash=np.zeros((128,128,4));flash[...,0]=.15+.85*bright;flash[...,1]=.55+.45*bright;flash[...,2]=1
flash[...,3]=np.clip(bright*.9+halo*.26,0,1)*np.clip((1-rad)/.13,0,1)
save('Impact_Flash.png',flash)

baseline=SKILL/'assets/unity/SwordSlashFlowBuiltInContractV1.shader'
source=baseline.read_text(encoding='utf-8-sig')
shader=source.replace('Shader "Skill/SwordSlashFlowBuiltInContractV1"','Shader "Steria/WaterThunder/FormedArc"')
shader=shader.replace('        _Reveal (', '        _UseShapeRGB ("Use Texture Color", Range(0,1)) = 1\n        _Dissolve ("Fracture", Range(0,1)) = 0\n        _DissolveTex ("Fracture Noise (Repeat, Linear)", 2D) = "white" {}\n        _Reveal (',1)
shader=shader.replace('            float _Reveal;', '            float _UseShapeRGB;\n            float _Dissolve;\n            sampler2D _DissolveTex;\n            float _Reveal;',1)
shader=shader.replace('float shape = tex2D(_ShapeTex, i.uv).a;', 'float4 shapeSample = tex2D(_ShapeTex, i.uv);\n                float shape = shapeSample.a;')
shader=shader.replace('float coverage = saturate(shape * mask * lifetime);','float fractureNoise = tex2D(_DissolveTex, i.uv * float2(3, 1) + float2(_Phase * 0.07, 0)).r;\n                float fracture = 1 - smoothstep(fractureNoise - 0.10, fractureNoise + 0.10, _Dissolve);\n                float coverage = saturate(shape * mask * lifetime * fracture);')
shader=shader.replace('float3 color = _BaseColor.rgb * max(_BodyStrength, 0.0001);','float3 color = _BaseColor.rgb * lerp(float3(1,1,1), shapeSample.rgb, _UseShapeRGB) * max(_BodyStrength, 0.0001);')
shader=shader.replace('        _UseShapeRGB (', '''        _Liquid ("Liquid Droplet Surface", Range(0,1)) = 0
        _WaterNormalTex ("Water Normal RG (Clamp, Linear)", 2D) = "gray" {}
        _LiquidWobble ("Capillary Wobble", Range(0,1)) = 0.8
        _LiquidShine ("Water Specular", Range(0,2)) = 1.1
        _WhiteFluidTex ("Curved Four-Sided Fluid Mask", 2D) = "black" {}
        _WhiteFluid ("White Fluid Pattern", Range(0,1)) = 0
        _WhiteFluidStrength ("White Fluid Strength", Range(0,2)) = 0.55
        _WhiteFluidSpin ("Fluid Spin Rate", Float) = 4.5
        _Voltage ("Electrical Potential Envelope", Range(0,1)) = 0
        _VoltageParticle ("Particle Mesh Energy In Blue", Range(0,1)) = 0
        _ElectricalTime ("Local Electrical Clock (-1 uses Phase)", Float) = -1
        _VoltageDecay ("Voltage Discharge", Range(1,10)) = 3.2
        _LiquidDissolve ("Drop Dissolve Before Fade", Range(0,1)) = 1
        _EvapDissolveSoftness ("Water Dissolve Edge Softness", Range(0.01,0.15)) = 0.055
        _TailGrowth ("Tail Expansion After Head", Range(0,1)) = 1
        _SweepAcross ("Sweep Across Width", Range(0,1)) = 1
        _SweepCore ("Centered Pointed Core", Range(0,1)) = 0
        _TailMaskTex ("Trailing Water Fan Mask (Clamp, Linear)", 2D) = "white" {}
        _TailMaskStrength ("Tail Mask Strength", Range(0,1)) = 1
        _TailSpread ("Tail Fan Spread", Range(0,2)) = 1
        _SweepMaskTex ("Moving Brush Wedge (Clamp, Linear)", 2D) = "white" {}
        _SweepEnabled ("Swept Brightness", Range(0,1)) = 0
        _SweepProgress ("Sweep Progress", Range(0,1)) = 0
        _SweepWidth ("Bright Sweep Width", Range(0.08,1)) = 0.38
        _SweepWake ("Passed Water Afterglow", Range(0,1)) = 0.60
        _EvapScatterTex ("Water Vapour Scatter (Clamp)", 2D) = "black" {}
        _EvapProgress ("Scatter Evaporation Progress", Range(0,1)) = 0
        _EvapSpread ("Scatter Spread", Float) = 1
        _EvapLift ("Scatter Rise", Float) = 0
        _EvapFrame ("Scatter Local XY Frame", Vector) = (0.65,1.95,3.7,3.85)
        _HeadWeight ("Legacy Front Emphasis", Range(0,1)) = 0
        _HeadSpan ("Front Length", Range(0.03,1)) = 0.32
        _Evaporation ("Water Scatter Evaporation", Range(0,1)) = 0
        _UseShapeRGB (''',1)
shader=shader.replace('            float _UseShapeRGB;', '''            sampler2D _WaterNormalTex;
            float _Liquid, _LiquidWobble, _LiquidShine, _LiquidDissolve, _EvapDissolveSoftness, _TailGrowth, _SweepAcross, _SweepCore;
            sampler2D _WhiteFluidTex;
            float _WhiteFluid, _WhiteFluidStrength, _WhiteFluidSpin, _Voltage, _VoltageParticle, _ElectricalTime, _VoltageDecay;
            float _HeadWeight, _HeadSpan, _Evaporation;
            sampler2D _TailMaskTex, _SweepMaskTex, _EvapScatterTex;
            float _TailMaskStrength, _TailSpread, _SweepEnabled, _SweepProgress, _SweepWidth, _SweepWake;
            float _EvapProgress, _EvapSpread, _EvapLift;
            float4 _EvapFrame;
            float _UseShapeRGB;''',1)
shader=shader.replace('                fixed4 color : COLOR;\n            };\n\n            v2f vert', '                fixed4 color : COLOR;\n                float2 localPosition : TEXCOORD1;\n            };\n\n            v2f vert',1)
shader=shader.replace('                o.color = v.color;', '                o.color = v.color;\n                o.localPosition = v.vertex.xy;',1)
shader=shader.replace('                float2 tailOffset : TEXCOORD2;', '                float2 tailOffset : TEXCOORD2;\n                float2 chargeData : TEXCOORD1;',1) if 'float2 tailOffset : TEXCOORD2;' in shader else shader
shader=shader.replace('                float2 uv : TEXCOORD0;\n                fixed4 color : COLOR;\n            };\n\n            struct v2f','                float2 uv : TEXCOORD0;\n                float2 tailOffset : TEXCOORD2;\n                fixed4 color : COLOR;\n            };\n\n            struct v2f',1)
shader=shader.replace('o.vertex = UnityObjectToClipPos(v.vertex);','v.vertex.xy += v.tailOffset * (_TailSpread*_TailGrowth-1);\n                o.vertex = UnityObjectToClipPos(v.vertex);',1)
# UV2 is birth time / energy reserve for electricity (ignored by other material modes).
shader=shader.replace('                float2 tailOffset : TEXCOORD2;','                float2 tailOffset : TEXCOORD2;\n                float2 chargeData : TEXCOORD1;',1) if 'float2 chargeData : TEXCOORD1;' not in shader else shader
shader=shader.replace('                float2 localPosition : TEXCOORD1;','                float2 localPosition : TEXCOORD1;\n                float2 electrical : TEXCOORD2;',1)
shader=shader.replace('                o.localPosition = v.vertex.xy;','                o.localPosition = v.vertex.xy;\n                o.electrical = v.chargeData;',1)
shader=shader.replace('float coverage = saturate(shape * mask * lifetime * fracture);', '''float front = pow(saturate(1 - max(0, _Reveal-i.uv.x) / max(_HeadSpan, 0.03)), 0.7);
                float across=lerp(i.uv.y,abs(i.uv.y-0.5)*2,_SweepCore);
                across=lerp(0.025,across,_SweepAcross);
                float2 sweepUV=float2((i.uv.x-_SweepProgress)/max(_SweepWidth,0.08)+0.92,across);
                float sweep=tex2D(_SweepMaskTex,sweepUV).r * step(0,sweepUV.x)*step(sweepUV.x,1);
                float passed=max(0,_SweepProgress-i.uv.x);
                float spread=smoothstep(0.015,0.18,passed);
                float wakeWidth=1-smoothstep(0.04+spread*0.96,0.08+spread*0.96,across);
                float wake=(1-smoothstep(_SweepProgress-0.035,_SweepProgress+0.015,i.uv.x))*_SweepWake*smoothstep(0.012,0.10,passed)*wakeWidth;
                float sweepStrength=0.65+0.80*_SweepProgress;
                float movingMask=max(wake,sweep*sweepStrength);
                mask*=lerp(1,tex2D(_TailMaskTex,i.uv).r,_TailMaskStrength);
                float2 dissolveUV=(i.localPosition-_EvapFrame.xy)/_EvapFrame.zw+0.5;
                float dissolveHeight=tex2D(_EvapScatterTex,dissolveUV).r;
                float cutoff=lerp(-0.10,1.10,_EvapProgress),soft=max(0.01,_EvapDissolveSoftness);
                float evaporated=1-smoothstep(dissolveHeight-soft,dissolveHeight+soft,cutoff);
                float coverage = saturate(shape * mask * lifetime * fracture) * lerp(1, front, _HeadWeight) * lerp(1,movingMask,_SweepEnabled) * lerp(1,evaporated,_Evaporation);''')
shader=shader.replace('                color *= i.color.rgb;', '                color *= lerp(1,0.80+sweep*sweepStrength*1.55,_SweepEnabled);\n                color *= i.color.rgb;',1)
shader=shader.replace('                color *= i.color.rgb;', '''                float2 pattern=i.uv*float2(3.25,1.1)+float2(-_Phase*0.8,_Phase*0.18);
                float cell=floor(pattern.x),spin=_Phase*_WhiteFluidSpin+cell*0.73;
                float2 local=frac(pattern)-0.5;
                float2 spun=float2(local.x*cos(spin)-local.y*sin(spin),local.x*sin(spin)+local.y*cos(spin))+0.5;
                float patternMask=tex2D(_WhiteFluidTex,spun).r;
                color+=float3(0.72,0.95,1)*patternMask*_WhiteFluid*_WhiteFluidStrength;
                float electricalClock=_ElectricalTime<0?_Phase:_ElectricalTime;
                float chargeStart=lerp(i.electrical.x,0,_VoltageParticle);
                float chargeAge=max(0,electricalClock-chargeStart);
                float reserve=saturate(lerp(i.electrical.y,i.color.b,_VoltageParticle));
                float charged=1-exp(-chargeAge*95);
                float discharge=exp(-chargeAge*(_VoltageDecay+6*(1-reserve)));
                float pulse=0.55+0.45*abs(cos(chargeAge*(52+reserve*17)+i.uv.x*9+i.localPosition.x*3));
                float potential=pow(max(reserve,0.001),0.70)*charged*discharge*pulse*step(chargeStart,electricalClock);
                float current=smoothstep(0.025,0.16,potential)*sqrt(max(potential,0));
                alpha*=lerp(1,current,_Voltage);
                color*=lerp(1,0.6+potential*1.4,_Voltage);
                color *= float3(i.color.rg,lerp(i.color.b,1,_VoltageParticle));''',1)
shader=shader.replace('                // UV0.x is pathT', '''                if(_Liquid>0.5)
                {
                    // Color RGB carries stable seed, normalized age and ligament type for water only.
                    float seed=i.color.r, age=i.color.g, ligament=step(0.5,i.color.b);
                    float2 p=(i.uv-0.5)*2;
                    float angle=atan2(p.y,p.x);
                    float wave=age*(11+seed*7)+seed*19;
                    float damping=exp(-age*2.8);
                    float wobble=_LiquidWobble*damping*(0.075*cos(angle*2+wave)+0.032*sin(angle*3-wave*0.7));
                    float contour=1-smoothstep(0.84+wobble,0.94+wobble,length(p));
                    if(ligament>0.5)
                    {
                        float neck=smoothstep(0.18,0.65,age)*0.87*exp(-p.x*p.x*7);
                        float halfWidth=max(0,0.72*sqrt(saturate(1-p.x*p.x))-neck+0.035*sin(p.x*8+wave)*damping);
                        contour=(1-smoothstep(max(0,halfWidth-0.07),halfWidth+0.07,abs(p.y)))*smoothstep(0.01,0.06,halfWidth);
                        contour*=1-smoothstep(0.90,1,abs(p.x));
                        float split=smoothstep(0.45,0.82,age),separation=0.30+split*0.17;
                        float2 lobe=(p-float2(p.x<0?-separation:separation,0))/float2(0.32,0.65);
                        float threadDistance=abs(p.y)-halfWidth;
                        float daughterDistance=(length(lobe)-0.94)*0.45;
                        contour=(1-smoothstep(-0.055,0.055,lerp(threadDistance,daughterDistance,split)))*(1-smoothstep(0.94,1,abs(p.x)));
                    }
                    float2 waveUV=i.uv+float2(sin(wave+p.y*3),cos(wave*0.83+p.x*4))*0.019*_LiquidWobble*damping;
                    float3 normal=tex2D(_WaterNormalTex,saturate(waveUV)).rgb;
                    normal=normalize(float3(normal.xy*2-1,max(0.08,normal.z)));
                    float spec=pow(saturate(dot(normal,normalize(float3(-0.38,0.58,1)))),64)*_LiquidShine;
                    float fresnel=pow(1-saturate(normal.z),2.5)*saturate(normal.y*0.4-normal.x*0.2+0.35);
                    float thick=sqrt(saturate(1-dot(p,p)));
                    float caustic=exp(-abs(p.y+0.24*sin(p.x*3+wave))*12)*0.12*damping;
                    float3 water=tex2D(_ShapeTex,i.uv).rgb*(0.48+0.52*thick);
                    water+=float3(0.85,0.97,1)*spec+float3(0.12,0.52,0.8)*(fresnel*0.4+caustic);
                    float2 dropMaskUV=i.uv*0.60+float2(seed*0.30,(1-seed)*0.30);
                    float dropHeight=tex2D(_EvapScatterTex,dropMaskUV).g;
                    float dropCut=lerp(-0.10,1.10,smoothstep(0.62,0.98,age));
                    float dropKeep=1-smoothstep(dropHeight-0.065,dropHeight+0.065,dropCut);
                    float liquidAlpha=contour*tex2D(_MaskTex,i.uv).r*(0.10+thick*0.15+spec*0.48+fresnel*0.12)*_Alpha*i.color.a*lerp(1,dropKeep,_LiquidDissolve);
                    return half4(water*_BaseColor.rgb,liquidAlpha*_BaseColor.a);
                }
                // UV0.x is pathT''',1)
(SHADERS/'WaterThunderFormedArc.shader').write_text('// Compatible derivative; original contract and source hash in provenance.json.\n'+shader,encoding='utf-8')
shutil.copyfile(baseline,HERE/'source_assets/SwordSlashFlowBuiltInContractV1.shader.txt')
reference=Path('d:/Users/rog/Downloads/水雷刀光六帧分解.png')
if reference.exists():shutil.copyfile(reference,HERE/'source_assets/design-reference.png')
record={'state':'candidate','editor':'2019.3.15f1','pipeline':'Built-in Gamma D3D11','mode':'SweptWedge main brightness / compatible FormedArc auxiliary','old_effect_assets_used':0,'game_assets_used':0,
 'textures':{'origin':'Original Python/numpy water filaments, caustics, periodic noise, translucent drop bodies, linear normal data and pointed brush masks','generator':'generate_inputs.py','seed':[91,48,71,207],'license':'Project-authored assets; no third-party image dependencies'},
 'shader':{'origin':'Local skill Contract V1 compatible derivative','source':str(baseline),'source_sha256':hashlib.sha256(baseline.read_bytes()).hexdigest(),'changes':['Pointed head before delayed wake/tail','Divergent tail mask and artist TailSpread','Single packed water-scatter RGBA: R=body erosion, G=drop erosion, A=scatter coverage','Monotonic fine water-mask erosion before late alpha fade','Damped capillary contours, necking and linear-normal specular'],'retained_contract':'UV pathT/acrossBlade, Reveal/Retreat/Phase, render state, all original property names'},
 'references':[{'url':'https://www.cyanilux.com/tutorials/sword-slash-shader-breakdown/','usage':'Technique only; no downloaded mesh/image/code copied','checked_utc':'2026-11-16'}],
 'inputs':[{'path':str(f.relative_to(HERE)).replace('\\','/'),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in sorted(TEX.glob('*.png')) if f.name!='Evap_Ring.png']+[{'path':'UnityProject/Assets/WaterThunder/Shaders/WaterThunderFormedArc.shader','sha256':hashlib.sha256((SHADERS/'WaterThunderFormedArc.shader').read_bytes()).hexdigest()}]}
(HERE/'provenance.json').write_text(json.dumps(record,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('INPUTS_READY',len(record['inputs']),'original inputs; four role textures/masks and shared periodic data')
