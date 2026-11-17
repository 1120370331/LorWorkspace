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
# Hand-authored UV paint: a connected asymmetric water wash, not repeated parallel ridges.
def smooth_controls(x,knots,values):
    index=np.clip(np.searchsorted(knots,x,side='right')-1,0,len(knots)-2)
    q=np.clip((x-np.take(knots,index))/(np.take(knots,index+1)-np.take(knots,index)),0,1);q=q*q*(3-2*q)
    return np.take(values,index)*(1-q)+np.take(values,index+1)*q

edge=np.clip(v/.028,0,1)*np.clip((1-v)/.085,0,1)
face=np.clip(1-v,0,1)**.7
wash_center=smooth_controls(u,[0,.18,.45,.72,1],[.61,.54,.46,.29,.23])
wash_width=smooth_controls(u,[0,.25,.58,.82,1],[.22,.28,.20,.26,.16])
wash=np.exp(-((v-wash_center)/wash_width)**2)
patch=np.exp(-((u-.55)/.20)**2-((v-.72)/.22)**2)
density=.59+.12*face+.055*noise(u*.7,v*.6,440)
body=np.zeros((h,w,4))
for channel,(deep,lit) in enumerate([(.025,.09),(.22,.49),(.44,.72)]):
    body[...,channel]=deep+(lit-deep)*face+wash*[.035,.12,.14][channel]+patch*[.008,.025,.03][channel]
body[...,3]=np.clip(density,0,.88)
save('L1_WaterBody.png',body)
# A spatially shaded water volume, paired with height/thickness and a forward flow vector.
crest_center=smooth_controls(u,[0,.24,.50,.78,1],[.46,.37,.56,.33,.41])
water_height=.18+.38*np.exp(-((v-crest_center)/.31)**2)
for px,py,rx,ry,amount in [(.19,.67,.20,.25,.18),(.55,.28,.23,.22,.20),(.82,.61,.18,.27,.16)]:
    water_height+=amount*np.exp(-((u-px)/rx)**2-((v-py)/ry)**2)
for px,py,rx,ry in [(.36,.72,.13,.18),(.72,.21,.15,.16)]:water_height-=.10*np.exp(-((u-px)/rx)**2-((v-py)/ry)**2)
water_height=np.clip(water_height+.012*np.sin(tau*(u*1.3+v*.7))+.008*np.sin(tau*(u*3.1-v*1.2)),.08,.92)
dv,du=np.gradient(water_height,1/(h-1),1/(w-1));nx=-du*.30;ny=-dv*.85;nz=np.ones_like(nx)
norm=np.sqrt(nx*nx+ny*ny+nz*nz);nx/=norm;ny/=norm;nz/=norm
light=np.array([-.25,-.48,.84]);light/=np.linalg.norm(light)
diffuse=np.clip(nx*light[0]+ny*light[1]+nz*light[2],0,1)
thickness=np.clip(.22+.86*water_height,0,1)
wet=np.clip((water_height-.20)/.60,0,1)
spec=np.clip(nx*(-.12)-ny*.28+nz*.952,0,1)**14*wet*.23
volume=np.zeros((h,w,4))
for c,(deep,scatter) in enumerate([(.018,.15),(.12,.50),(.34,.52)]):
    volume[...,c]=deep+scatter*wet*(.16+.84*diffuse**1.4)+spec*[.82,.95,1.][c]
volume[...,3]=body[...,3]
save('L1_WaterVolume.png',volume)
flow_y=np.clip(-du*.16/(1+np.abs(dv)),-.22,.22);flow_length=np.sqrt(1+flow_y*flow_y)
depth_data=np.stack([.5+.5/flow_length,.5+.5*flow_y/flow_length,water_height,thickness],axis=2)
save('L1_WaterDepthFlow.png',depth_data)

# Continuous silhouette only. Internal shading belongs to RGB and the supplied surface normal.
save('L1_WaterMask.png',np.repeat(edge[...,None],3,axis=2))
# Independently painted ribbon routes: three broad ribbons and two quieter wisps.
tail_fan=np.zeros_like(u)
ribbons=[([.15,.10,.22,.31,.34],.065,.015),([.47,.40,.48,.50,.50],.079,.075),([.86,.92,.79,.71,.66],.070,.045),([.30,.25,.33,.39,.42],.026,.110),([.69,.75,.67,.61,.58],.030,.140)]
for k,(route,width,end) in enumerate(ribbons):
    center=smooth_controls(u,[0,.10,.22,.36,.50],route)
    brush_width=width*smooth_controls(u,[0,.15,.32,.50],[.45,.83,1.13,1.0])
    stroke=np.exp(-((v-center)/(brush_width+.0001))**4)
    tip=np.clip((u-end)/.09,0,1);tail_fan=np.maximum(tail_fan,stroke*tip)
tail_fan*=np.clip(v/.018,0,1)*np.clip((1-v)/.018,0,1)
save('L1_TailFanMask.png',np.repeat(tail_fan[...,None],3,axis=2))

# Clamp brush wedge; this travels over fixed pathT instead of unveiling the mesh.
su,sv=np.meshgrid(np.linspace(0,1,512),np.linspace(1,0,256))
rise=np.clip(su/.20,0,1);fall=np.clip((1-su)/.045,0,1)
# A single brush-shaped pointed front; soft rear and a continuous curved diagonal.
brush_width=np.clip((1-su)**.72,0,1)
wedge=(su**.75)*rise*fall*np.clip((brush_width-.82*sv)/.075,0,1)
wedge*=.91+.09*noise(su*.65,sv*.5,89);wedge/=wedge.max()
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
# Kept as a compatibility input; the live water material uses the supplied full RGB normal.
height=np.sin(tau*(p*2+q+.16*np.sin(tau*q*2)))+.46*np.sin(tau*(p*5-q*3+.12*np.sin(tau*p)))
dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*7
dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*7
ripple=np.stack([np.tanh(dx)*.5+.5,np.tanh(dy)*.5+.5,.5+.5*np.tanh(height*1.3)],axis=2)
ripple[0]=ripple[-1]=(ripple[0]+ripple[-1])*.5
ripple[:,0]=ripple[:,-1]=(ripple[:,0]+ripple[:,-1])*.5
save('L1_RippleNormalRepeat.png',ripple)
shutil.copyfile(HERE/'source_assets/UserWaterNormal_original.png',TEX/'L1_UserWaterNormal.png')

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

# One tileable noise field drives rotated/stretched water flow and cone UV shadow motion.
def periodic_value_field(size,cells,seed):
    rng=np.random.default_rng(seed);grid=rng.random((cells,cells))
    xy=np.linspace(0,cells,size)%cells;xx,yy=np.meshgrid(xy,xy)
    ix=xx.astype(int);iy=yy.astype(int);fx=xx-ix;fy=yy-iy
    fx=fx*fx*fx*(fx*(fx*6-15)+10);fy=fy*fy*fy*(fy*(fy*6-15)+10)
    return grid[iy,ix]*(1-fx)*(1-fy)+grid[iy,(ix+1)%cells]*fx*(1-fy)+grid[(iy+1)%cells,ix]*(1-fx)*fy+grid[(iy+1)%cells,(ix+1)%cells]*fx*fy
noise_field=sum(weight*periodic_value_field(256,cells,881+cells) for cells,weight in [(2,.60),(4,.25),(8,.10),(16,.05)])
lo,hi=np.quantile(noise_field,[.005,.995]);noise_field=np.clip((noise_field-lo)/(hi-lo),0,1)
save('Shared_WaterNoiseRepeat.png',np.repeat(noise_field[...,None],3,axis=2))
# A dedicated high-contrast streaming UV map shared by the blade highlight and light needles.
lu,lv=np.meshgrid(np.linspace(0,1,512),np.linspace(1,0,128))
cloud=np.asarray(Image.fromarray(np.uint8(noise_field*255)).resize((512,128),Image.Resampling.BILINEAR),dtype=float)/255
ribbon_phase=tau*(lu*2+.10*np.sin(tau*lv)+.045*np.sin(tau*(lu*3-lv)))
flow_bright=(.5+.5*np.cos(ribbon_phase))**2
flow_shadow=np.clip(.58*(1-flow_bright)+.42*cloud,0,1)
flow_image=np.stack([flow_bright,flow_shadow,cloud],axis=2)
flow_image[0]=flow_image[-1]=(flow_image[0]+flow_image[-1])*.5;flow_image[:,0]=flow_image[:,-1]=(flow_image[:,0]+flow_image[:,-1])*.5
save('Shared_LightFlowRepeat.png',flow_image)
# Cone light is white. A separate UV mask alone determines the dark-blue side.
nu,nv=np.meshgrid(np.linspace(0,1,256),np.linspace(1,0,128))
shadow=np.exp(-((nv-.62)/.105)**2)*(1-.45*nu**3)
save('Impact_ConeShadowMask.png',np.repeat(shadow[...,None],3,axis=2))
# Legacy texture slots retained for source compatibility; the live burst is an unshaped billboard line.
needle=np.ones((128,256,4));needle[...,3]=np.clip((1-np.abs(nv-.5)*2-nu)/.12,0,1)*np.clip(nu/.04,0,1)
save('Impact_Needle.png',needle)
save('Impact_NeedleFlowRepeat.png',np.repeat(noise_field[...,None],3,axis=2))

fx,fy=np.meshgrid(np.linspace(-1,1,96),np.linspace(1,-1,96));fr=np.sqrt(fx*fx+fy*fy);fa=np.arctan2(fy,fx)
foam=np.zeros((96,96,4));foam[...,:3]=[.65,.90,1.]
foam[...,3]=np.exp(-((fr-.61)/.12)**2)*np.clip(.45+.5*np.sin(fa*3+.5)+.28*np.cos(fa*5),0,1)+.12*np.exp(-(fr/.5)**2)
save('Impact_Foam.png',foam)

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
    if layer=='L4_Core':
        center=.50+.009*np.sin(pu*8+.4)
        variation=smooth_controls(pu,[0,.20,.43,.64,.82,1],[.68,.85,1.08,.88,1.13,.66])
        offset=pv-center
        sigma=np.where(offset<0,.055,.105)*variation
        core=np.exp(-(offset/(sigma+.001))**2)
        fringe=np.exp(-(offset/(sigma*2.8+.001))**2)
        for channel,blue in enumerate([.07,.57,.90]):rgba[...,channel]=blue+(1-blue)*core
        rgba[...,3]=np.clip(core*.86+fringe*.14,0,1)
        save(layer+'.png',rgba)
    start_soft=.003 if layer=='L4_Core' else .035
    save(layer+'Mask.png',np.repeat((np.clip(pu/start_soft,0,1)*np.clip((1-pu)/.012,0,1))[...,None],3,axis=2))

# One original transparent water-vapour scatter map: open micro-ripples, wisps and specks.
# No closed circles. Marks originate near the water arc, then the runtime sheet disperses.
scale=2;size=512;canvas=Image.new('L',(size*scale,size*scale),0);draw=ImageDraw.Draw(canvas)
rng=np.random.default_rng(3910)
for k in range(205):
    t=rng.uniform(.035,.99);angle=np.pi*.5-np.pi*t
    c=np.array([.65+1.95*np.cos(angle),1.95+1.95*np.sin(angle)])
    c+=rng.normal(0,[.14,.16])
    px=((c[0]-.65)/4.6+.5)*size;py=(.5-(c[1]-1.95)/4.8)*size
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
def field_at(radius,seed=927):
    source=Image.fromarray(np.uint8(np.random.default_rng(seed).random((size,size))*255))
    f=np.asarray(source.filter(ImageFilter.GaussianBlur(radius)),dtype=float)
    lo,hi=np.quantile(f,[.01,.99]);return np.clip((f-lo)/max(hi-lo,1),0,1)

def domain_warp(field):
    xx=np.clip(field_u*(size-1)+(field_at(13,71)-.5)*size*.14,0,size-1.001)
    yy=np.clip(field_v*(size-1)+(field_at(19,93)-.5)*size*.12,0,size-1.001)
    x0=xx.astype(int);y0=yy.astype(int);dx=xx-x0;dy=yy-y0
    return field[y0,x0]*(1-dx)*(1-dy)+field[y0,x0+1]*dx*(1-dy)+field[y0+1,x0]*(1-dx)*dy+field[y0+1,x0+1]*dx*dy

macro=field_at(22,941);meso=field_at(6,315);micro=field_at(1.6,638)
curl=.5+.5*np.sin(tau*(field_u*11+field_v*8+field_at(12,876)*2))
height=domain_warp(.40*macro+.28*meso+.18*micro+.14*curl)
# Equalized rank spreads erosion through the lifetime rather than all clumps vanishing together.
order=np.argsort(height,axis=None);rank=np.empty(height.size,dtype=float);rank[order]=np.linspace(0,1,height.size);height=rank.reshape(height.shape)
drop_height=.70*field_at(9)+.23*field_at(16)+.07*ripple_field
low,high=np.quantile(drop_height,[.015,.985]);drop_height=np.clip((drop_height-low)/(high-low),0,1)
# R=warped multiscale erosion, G=soft droplet erosion, B=independent micro detail, A=visible scatter.
scatter=np.zeros((size,size,4));scatter[...,0]=height;scatter[...,1]=drop_height;scatter[...,2]=micro;scatter[...,3]=alpha
save('Evap_WaterScatter.png',scatter)

# Sparse foamy crest noise for the inner/lower wake; independent of water-body coverage.
ex,ey=np.meshgrid(np.linspace(-1,1,256),np.linspace(1,-1,128))
foam_grain=noise((ex+1)*1.5,(ey+1)*1.2,714)
foam_envelope=np.clip(1-ex*ex,0,1)*np.exp(-(ey/.65)**4)
foam_alpha=np.clip((foam_grain-.43)*5,0,1)*foam_envelope
foam_image=np.zeros((128,256,4));foam_image[...,:3]=[.80,.95,1.];foam_image[...,3]=foam_alpha
save('EdgeFoam_WaveNoise.png',foam_image)

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
shader=shader.replace('        _UseShapeRGB (', '''        _BladeBoundary ("Shared Blade Center Radius Inset", Vector) = (0.65,1.95,1.95,0.028)
        _WaterDepthTex ("Height B / Flow RG / Thickness A (Linear)", 2D) = "gray" {}
        _UVFlowDistance ("UV Flow Distance", Range(0,0.3)) = 0
        _DepthOffset ("Height Guided Offset", Range(0,0.06)) = 0
        _LightFlowTex ("Streaming Light RG (Repeat, Linear)", 2D) = "gray" {}
        _CoreFlowStrength ("Highlight Internal Flow", Range(0,1)) = 0
        _WaterNoiseTex ("Water Noise (Repeat, Linear)", 2D) = "gray" {}
        _WaterNoiseStrength ("Noise Flow Strength", Range(0,1)) = 0
        _NoiseStretch ("Noise UV Stretch", Vector) = (1.7,0.62,0,0)
        _NoiseSpin ("Noise UV Rotation Speed", Float) = 0.6
        _RippleTex ("User Water Surface Normal (Clamp, Linear)", 2D) = "gray" {}
        _RippleStrength ("Internal Water Ripples", Range(0,0.1)) = 0
        _RippleShine ("Ripple Moving Highlights", Range(0,1)) = 0
        _Liquid ("Liquid Droplet Surface", Range(0,1)) = 0
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
        _EvapFrame ("Scatter Local XY Frame", Vector) = (0.65,1.95,4.6,4.8)
        _HeadWeight ("Legacy Front Emphasis", Range(0,1)) = 0
        _HeadSpan ("Front Length", Range(0.03,1)) = 0.32
        _Evaporation ("Water Scatter Evaporation", Range(0,1)) = 0
        _UseShapeRGB (''',1)
shader=shader.replace('            float _UseShapeRGB;', '''            sampler2D _WaterDepthTex, _LightFlowTex;
            float _UVFlowDistance, _DepthOffset, _CoreFlowStrength;
            sampler2D _WaterNoiseTex;
            float _WaterNoiseStrength, _NoiseSpin;
            float4 _NoiseStretch, _BladeBoundary;
            sampler2D _RippleTex;
            float _RippleStrength, _RippleShine;
            sampler2D _WaterNormalTex;
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
shader=shader.replace('o.vertex = UnityObjectToClipPos(v.vertex);', """v.vertex.xy += v.tailOffset * (_TailSpread*_TailGrowth-1);
                float flutter=(sin(v.uv.x*23+v.uv.y*17-_Phase*9)+0.45*sin(v.uv.x*41-v.uv.y*9-_Phase*6))*0.16;
                float trailing=1-smoothstep(_Retreat+0.06,_Retreat+0.30,v.uv.x);
                v.vertex.xy += v.tailOffset*flutter*trailing*_TailMaskStrength*_TailGrowth;
                if(_TailMaskStrength>0.5)
                {
                    float2 radial=v.vertex.xy-_BladeBoundary.xy;
                    float limit=max(0.01,_BladeBoundary.z-_BladeBoundary.w);
                    v.vertex.xy=_BladeBoundary.xy+radial*min(1,limit/max(length(radial),0.0001));
                }
                o.vertex = UnityObjectToClipPos(v.vertex);""",1)
# UV2 is birth time / energy reserve for electricity (ignored by other material modes).
shader=shader.replace('                float2 tailOffset : TEXCOORD2;','                float2 tailOffset : TEXCOORD2;\n                float2 chargeData : TEXCOORD1;',1) if 'float2 chargeData : TEXCOORD1;' not in shader else shader
shader=shader.replace('                float2 localPosition : TEXCOORD1;','                float2 localPosition : TEXCOORD1;\n                float2 electrical : TEXCOORD2;',1)
shader=shader.replace('                o.localPosition = v.vertex.xy;','                o.localPosition = v.vertex.xy;\n                o.electrical = v.chargeData;',1)
shader=shader.replace('float coverage = saturate(shape * mask * lifetime * fracture);', '''float front = pow(saturate(1 - max(0, _Reveal-i.uv.x) / max(_HeadSpan, 0.03)), 0.7);
                float across=lerp(i.uv.y,abs(i.uv.y-0.5)*2,_SweepCore);
                across=lerp(0.025,across,_SweepAcross);
                // The bright layer has a fixed span. Its rear cutoff can only advance.
                float sweepWidth=_SweepWidth;
                float2 sweepUV=float2((i.uv.x-_SweepProgress)/max(sweepWidth,0.08)+0.92,across);
                float sweep=tex2D(_SweepMaskTex,sweepUV).r * step(0,sweepUV.x)*step(sweepUV.x,1);
                float passed=max(0,_SweepProgress-i.uv.x);
                float spread=smoothstep(0.015,0.18,passed);
                float wakeWidth=1-smoothstep(0.04+spread*0.96,0.08+spread*0.96,across);
                float wake=(1-smoothstep(_SweepProgress-0.035,_SweepProgress+0.015,i.uv.x))*_SweepWake*smoothstep(0.012,0.10,passed)*wakeWidth;
                float sweepStrength=0.65+0.80*_SweepProgress;
                float coreTail=max(_Retreat,max(0,_SweepProgress-_SweepWidth*0.70));
                float coreRear=smoothstep(coreTail,coreTail+0.035,i.uv.x);
                float corePassed=1-smoothstep(_SweepProgress-0.018,_SweepProgress+0.005,i.uv.x);
                float coreCoverage=coreRear*corePassed*wakeWidth;
                float movingMask=lerp(max(wake,sweep*sweepStrength),coreCoverage,_SweepCore);
                float tailRelative=(i.uv.x-_Retreat)/0.30;
                float ribbonWeight=1-smoothstep(0.10,1,tailRelative);
                float2 ribbonUV=float2(tailRelative*0.48,i.uv.y+0.055*sin(i.uv.x*23-_Phase*9)+0.025*sin(i.uv.x*41+_Phase*6));
                mask*=lerp(1,tex2D(_TailMaskTex,saturate(ribbonUV)).r,_TailMaskStrength*ribbonWeight);
                float2 dissolveUV=(i.localPosition-_EvapFrame.xy)/_EvapFrame.zw+0.5;
                float4 dissolveData=tex2D(_EvapScatterTex,dissolveUV);
                float dissolveHeight=dissolveData.r*0.78+dissolveData.b*0.22;
                float cutoff=lerp(-0.10,1.10,_EvapProgress),soft=max(0.01,_EvapDissolveSoftness);
                float evaporated=1-smoothstep(dissolveHeight-soft,dissolveHeight+soft,cutoff);
                float wetEdge=(1-smoothstep(soft*0.5,soft*2.5,abs(dissolveHeight-cutoff)))*step(0.01,_EvapProgress);
                float coverage = saturate(shape * mask * lifetime * fracture) * lerp(1, front, _HeadWeight) * lerp(1,movingMask,_SweepEnabled) * lerp(1,evaporated,_Evaporation);''')
shader=shader.replace('                float4 shapeSample = tex2D(_ShapeTex, i.uv);', '''                // Rotate then stretch a single noise map; never use it to punch holes in the body.
                float noiseAngle=0.28+_Phase*_NoiseSpin;
                float2 stretched=(i.uv-0.5)*_NoiseStretch.xy;
                float2 noiseUV=float2(stretched.x*cos(noiseAngle)-stretched.y*sin(noiseAngle),stretched.x*sin(noiseAngle)+stretched.y*cos(noiseAngle))+float2(-_Phase*.34,_Phase*.08);
                float flowNoise=tex2D(_WaterNoiseTex,noiseUV).r;
                float flowNoiseY=tex2D(_WaterNoiseTex,noiseUV+float2(.31,.43)).r;
                float2 noiseWarp=(float2(flowNoise,flowNoiseY)-.5)*.025*_WaterNoiseStrength;
                // Flow follows the blade tangent; height only adds a bounded lateral UV displacement.
                float phase0=frac(_Phase*0.85),phase1=frac(_Phase*0.85+0.5);
                float weight=1-abs(phase0*2-1);
                float4 depth=tex2D(_WaterDepthTex,i.uv);
                float2 direction=depth.rg*2-1;
                float2 lateral=float2(-direction.y,direction.x)*(depth.b-.5)*_DepthOffset;
                float2 offset0=-direction*(phase0-.5)*_UVFlowDistance+lateral+noiseWarp;
                float2 offset1=-direction*(phase1-.5)*_UVFlowDistance+lateral+noiseWarp;
                offset0=clamp(offset0,float2(-.10,-.035),float2(.10,.035));
                offset1=clamp(offset1,float2(-.10,-.035),float2(.10,.035));
                float2 normalUV=i.uv*float2(.76,.74)+float2(.12,.10);
                float3 normalA=tex2D(_RippleTex,normalUV+offset0*.45).rgb*2-1;
                float3 normalB=tex2D(_RippleTex,normalUV+offset1*.45).rgb*2-1;
                float3 rippleN=normalize(lerp(normalB,normalA,weight)+float3(0,0,.001));
                float4 shapeA=tex2D(_ShapeTex,saturate(i.uv+offset0));
                float4 shapeB=tex2D(_ShapeTex,saturate(i.uv+offset1));
                float4 shapeSample=lerp(shapeB,shapeA,weight);
                shapeSample.a=tex2D(_ShapeTex,i.uv).a; // Coverage never moves with the colour UVs.
''',1)
shader=shader.replace('                color *= i.color.rgb;', '                color *= lerp(1,0.80+sweep*sweepStrength*1.55,_SweepEnabled*(1-_SweepCore));\n                color *= i.color.rgb;',1)
shader=shader.replace('                color *= i.color.rgb;', '''                color+=float3(0.06,0.25,0.38)*wetEdge*_Evaporation;
                float rippleSpec=pow(saturate(dot(rippleN,normalize(float3(-0.35,0.55,1)))),22);
                color*=1+_RippleShine*(rippleN.z-0.70)*0.22;
                color+=float3(0.55,0.88,1)*rippleSpec*_RippleShine;
                color*=1+(flowNoise-.5)*.65*_WaterNoiseStrength;
                color+=float3(.04,.16,.24)*smoothstep(.56,.88,flowNoise)*_WaterNoiseStrength;
                float2 highlightFlow=tex2D(_LightFlowTex,float2(i.uv.x*1.4-_Phase*3.6,i.uv.y)).rg;
                color=lerp(color,color*float3(.45,.72,1),highlightFlow.g*_CoreFlowStrength);
                color+=float3(.10,.25,.36)*highlightFlow.r*_CoreFlowStrength;
                float2 pattern=i.uv*float2(3.25,1.1)+float2(-_Phase*0.8,_Phase*0.18);
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
(SHADERS/'WaterThunderLightCone.shader').write_text('Shader "Steria/WaterThunder/LightNeedle"\n{\n    Properties\n    {\n        _NoiseTex ("Rotated and Stretched Noise", 2D) = "gray" {}\n        _LightFlowTex ("High Contrast Streaming UV", 2D) = "gray" {}\n        _UVFlowStrength ("UV Flow Contrast", Range(0,2)) = 1\n        _ShadowTex ("UV Dark Blue Shadow", 2D) = "black" {}\n        _WhiteColor ("White Emission", Color) = (1,1,1,1)\n        _ShadowColor ("Dark Blue UV Shade", Color) = (0.025,0.15,0.55,1)\n        _Emission ("Emission", Float) = 1.8\n        _NeedleTime ("Local Flow Time", Float) = 0\n    }\n    SubShader\n    {\n        Tags { "Queue"="Transparent+60" "RenderType"="Transparent" }\n        Blend SrcAlpha OneMinusSrcAlpha\n        ZWrite Off\n        Cull Off\n        Pass\n        {\n            CGPROGRAM\n            #pragma vertex vert\n            #pragma fragment frag\n            #include "UnityCG.cginc"\n            sampler2D _NoiseTex, _ShadowTex, _LightFlowTex;\n            float4 _WhiteColor, _ShadowColor;\n            float _Emission, _NeedleTime, _UVFlowStrength;\n            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };\n            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };\n            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }\n            half4 frag(v2f i):SV_Target\n            {\n                float angle=.18+_NeedleTime*.65;\n                float2 p=(i.uv-.5)*float2(3.2,.85);\n                float2 uv=float2(p.x*cos(angle)-p.y*sin(angle),p.x*sin(angle)+p.y*cos(angle))+float2(-_NeedleTime*2.8,0);\n                float n=tex2D(_NoiseTex,uv).r;\n                float2 stream=tex2D(_LightFlowTex,float2(i.uv.x*1.2-_NeedleTime*3.8,i.uv.y)).rg;\n                float shade=saturate(tex2D(_ShadowTex,i.uv).r+(n-.5)*.22+(stream.g-.5)*.65*_UVFlowStrength);\n                float longitudinal=pow(saturate(sin(i.uv.x*3.14159265)),0.72);\n                float y=i.uv.y-.5;\n                float core=exp(-(y*y)/(.16*.16));\n                float halo=exp(-(y*y)/(.25*.25));\n                float3 light=lerp(_WhiteColor.rgb*_Emission*(.85+stream.r*.35*_UVFlowStrength),_ShadowColor.rgb,shade)*core*longitudinal;\n                float alpha=i.color.a*halo*longitudinal;\n                return half4(light*i.color.rgb,alpha);\n            }\n            ENDCG\n        }\n    }\n    Fallback Off\n}\n',encoding='utf-8')
record={'state':'candidate','editor':'2019.3.15f1','pipeline':'Built-in Gamma D3D11','mode':'SweptWedge main brightness / compatible FormedArc auxiliary','old_effect_assets_used':0,'game_assets_used':0,
 'textures':{'origin':'Original Python/numpy water filaments, caustics, periodic noise, translucent drop bodies, linear normal data and pointed brush masks','generator':'generate_inputs.py','seed':[91,48,71,207],'license':'Project-authored assets; no third-party image dependencies'},
 'shader':{'origin':'Local skill Contract V1 compatible derivative','source':str(baseline),'source_sha256':hashlib.sha256(baseline.read_bytes()).hexdigest(),'changes':['Pointed head before delayed wake/tail','Divergent tail mask and artist TailSpread','Single packed water-scatter RGBA: R=body erosion, G=drop erosion, A=scatter coverage','Monotonic fine water-mask erosion before late alpha fade','Damped capillary contours, necking and linear-normal specular'],'retained_contract':'UV pathT/acrossBlade, Reveal/Retreat/Phase, render state, all original property names'},
 'references':[{'url':'https://www.cyanilux.com/tutorials/sword-slash-shader-breakdown/','usage':'Technique only; no downloaded mesh/image/code copied','checked_utc':'2026-11-16'}],
 'inputs':[{'path':str(f.relative_to(HERE)).replace('\\','/'),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in sorted(TEX.glob('*.png')) if f.name!='Evap_Ring.png']+[{'path':'UnityProject/Assets/WaterThunder/Shaders/WaterThunderFormedArc.shader','sha256':hashlib.sha256((SHADERS/'WaterThunderFormedArc.shader').read_bytes()).hexdigest()}]}
record['textures']['origin']='Project-painted water blade / core / silhouette / brush masks and procedural auxiliaries; one unchanged user-supplied water normal'
record['user_provided_asset_count']=1
record['user_assets']=[{'source':'User attachment: 水波贴图-波纹-海浪-法线-425641.png','path':'UnityProject/Assets/WaterThunder/Textures/L1_UserWaterNormal.png','sha256':hashlib.sha256((TEX/'L1_UserWaterNormal.png').read_bytes()).hexdigest(),'modified':False,'usage':'Water surface normal; user-supplied for this project; not promoted to public asset library'}]
record['references'].append({'url':'https://github.com/innogames/IG-VFX-Unity-Mobile-Toolkit/tree/6e9e80ae08f94c77943420d48fc50e863ffce8c4','license':'MIT','usage':'Method reference: separate main density, swipe mask and distortion; no source pixels/code copied'})
record['inputs'].append({'path':'UnityProject/Assets/WaterThunder/Shaders/WaterThunderLightCone.shader','sha256':hashlib.sha256((SHADERS/'WaterThunderLightCone.shader').read_bytes()).hexdigest()})
(HERE/'provenance.json').write_text(json.dumps(record,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('INPUTS_READY',len(record['inputs']),'original inputs; four role textures/masks and shared periodic data')
