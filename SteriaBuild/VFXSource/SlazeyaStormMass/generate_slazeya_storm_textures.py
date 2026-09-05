#!/usr/bin/env python3
"""Original Round 2 linear VFX data. Requires Python 3.12, numpy and Pillow.

Run without arguments to regenerate PNGs, inspection boards and manifest.
--verify checks the existing deliverable; --verify-repro regenerates in memory
and compares all six encoded PNG hashes. No Unity or external assets are used.
PNG row zero is V=1 for water, and atlas frame zero is at the TOP LEFT.
Inspection boards are offline data visualizations, never native Unity previews.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "source_assets" / "round2"
SEED = 20260905
GUTTER = 12
TAU = 2 * np.pi
SPECS = {
    "water_body_rgba.png": ((1024, 512), ["wide water density", "clustered lip/shoulder foam", "independent directional rupture threshold", "coverage / thickness windows"]),
    "water_normal_roughness.png": ((1024, 512), ["tangent normal X * .5 + .5", "tangent normal Y * .5 + .5 (+V toward lip)", "tangent normal Z * .5 + .5", "roughness"]),
    "water_flow_rg.png": ((512, 512), ["slow flow perturbation X * .5 + .5", "slow flow perturbation Y * .5 + .5", "shared medium height", "one"]),
    "foam_spray_4x4.png": ((1024, 1024), ["membrane density", "foam / heavy drops", "local lit face", "coverage"]),
    "mist_density_lighting_4x4.png": ((1024, 1024), ["integrated density", "upper/front light", "bottom/interior occlusion", "soft coverage"]),
    "droplet_spindrift_4x2.png": ((1024, 512), ["grayscale light", "grayscale light", "grayscale light", "shape coverage"]),
}


def sat(a):
    return np.clip(a, 0, 1)


def smooth(lo, hi, a):
    t = sat((a - lo) / (hi - lo))
    return t * t * (3 - 2 * t)


def quintic(t):
    return t * t * t * (t * (t * 6 - 15) + 10)


def grid(w, h, water=False):
    # Duplicated periodic endpoints give exact U edge equality after quantizing.
    u, y = np.meshgrid(np.linspace(0, 1, w), np.linspace(0, 1, h))
    return u, 1 - y if water else y


def noise2(u, v, nx, ny, seed):
    """C2 value noise; U repeats at unit period, V lattice extends periodically.

    V is sampled away from its period and imported clamped, never repeated.
    Integer nx is essential: arbitrary domain warps retain exact U periodicity.
    """
    rng = np.random.default_rng(seed)
    lattice = rng.random((ny + 5, nx))
    x, y = u * nx, v * ny + 2
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = quintic(x - ix), quintic(y - iy)
    ix0, ix1 = ix % nx, (ix + 1) % nx
    iy0, iy1 = iy % (ny + 5), (iy + 1) % (ny + 5)
    a = lattice[iy0, ix0] * (1 - fx) + lattice[iy0, ix1] * fx
    b = lattice[iy1, ix0] * (1 - fx) + lattice[iy1, ix1] * fx
    return a * (1 - fy) + b * fy


def fbm(u, v, nx, ny, seed, weights=(.56, .29, .115, .035)):
    result = np.zeros_like(u)
    for i, weight in enumerate(weights):
        result += weight * noise2(u, v, nx * 2**i, ny * 2**i, seed + i * 101)
    return result / sum(weights)


def stack(*channels):
    a = np.stack(np.broadcast_arrays(*channels), -1)
    if not np.all(np.isfinite(a)):
        raise ValueError("non-finite source field")
    return np.uint8(np.rint(sat(a) * 255))


def encode(a):
    stream = io.BytesIO()
    Image.fromarray(a).save(stream, format="PNG", compress_level=9)
    return stream.getvalue()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def water_fields(w, h):
    u, v = grid(w, h, water=True)
    # Shared physical coordinates for density, tangent normals and flow map.
    dx = fbm(u, v, 4, 3, SEED + 10) - .5
    dy = fbm(u, v, 3, 5, SEED + 20) - .5
    x = u + .095 * dx + .028 * np.sin(TAU * u + 3 * v)
    y = v + .21 * dy
    macro = fbm(x, y, 4, 4, SEED + 30, (.7, .3))
    medium = fbm(x + .04 * macro, y + .12 * macro, 9, 10, SEED + 40)
    fine = fbm(x, y, 34, 44, SEED + 50, (.78, .22))
    folds = np.zeros_like(u)
    shoulder = np.zeros_like(u)
    # Five unequal broad folds with local openings; three offshoots merge into
    # their parent, with no global stripe oscillator or equally spaced lanes.
    controls = [(.12, .086, .18, .20, .40), (.33, .126, .42, .74, .56),
                (.58, .080, .81, .33, .67), (.77, .115, .11, .17, .46),
                (.90, .067, .65, .62, .34)]
    for i, (base, width, phase, center, extent) in enumerate(controls):
        curve = base + (.055 + .016 * i) * np.sin(TAU * x + phase * TAU)
        curve += .027 * np.sin(TAU * 2 * x + i * 2.17) + .095 * (macro - .5)
        width_field = width * (.62 + .76 * noise2(u, v * .2, 5, 3, SEED + 60 + i))
        along = np.exp(-((np.sin(np.pi * (x - center))) / extent)**2)
        along = .12 + .88 * along
        d = (y - curve) / width_field
        fold = np.exp(-d * d * 1.4) * along
        folds = np.maximum(folds, fold)
        shoulder += np.exp(-((d - .60) / .48)**2) * along * (.8 if i > 2 else .27)
        if i in (1, 2, 3):
            join = np.exp(-((np.sin(np.pi * (x - center - .17))) / .55)**2)
            branch_y = curve + (.10 if i != 2 else -.14) * join
            branch = np.exp(-((y - branch_y) / (width_field * .50))**2) * join * along
            folds = np.maximum(folds, branch * .78)
    height = sat(.10 + .45 * folds + .27 * macro + .17 * medium)
    density = sat(.10 + .47 * folds + .36 * macro + .19 * (medium - .35))
    foam_mass = fbm(x + .06 * dy, y + .10 * dx, 13, 19, SEED + 80)
    foam_islands = smooth(.39, .68, foam_mass + .15 * shoulder)
    lip_zone = smooth(.48, .83, v + .08 * (macro - .5))
    foam = foam_islands * sat(shoulder * 1.65 + .24 * lip_zone)
    pores = fbm(x, y, 55, 69, SEED + 90, (.76, .24))
    foam *= .36 + .64 * smooth(.25, .62, pores)
    foam *= .7 + .3 * fine
    # Separate noise seed, broad oblique tears, independent from density/foam.
    tear = fbm(u + .08 * dx, v + .27 * dy + .22 * np.sin(TAU * u), 7, 12, SEED + 200)
    rupture = sat(.10 + .73 * tear + .17 * noise2(x, y, 17, 23, SEED + 201))
    lower = .013 + .050 * noise2(u, v * 0, 6, 3, SEED + 210)
    upper = .981 - .047 * noise2(u, v * 0, 7, 3, SEED + 211)
    envelope = smooth(lower, lower + .095, v) * (1 - smooth(upper - .12, upper, v))
    windows = 1 - .55 * smooth(.64, .85, fbm(x, y, 5, 6, SEED + 220))
    coverage = envelope * (.72 + .28 * density) * windows
    # Normal differences below are in UV units and explicitly invert PNG row Y.
    return dict(u=u, v=v, height=height, density=density, foam=foam,
                rupture=rupture, coverage=coverage, dx=dx, dy=dy, fine=fine)


def water_assets():
    f = water_fields(1024, 512)
    height = f["height"]
    # Exclude duplicate U endpoint while applying central periodic differences.
    core = height[:, :-1]
    du_core = (np.roll(core, -1, 1) - np.roll(core, 1, 1)) * (1023 / 2)
    du = np.concatenate((du_core, du_core[:, :1]), axis=1)
    dv = -np.gradient(height, 1 / 511, axis=0)
    # Small slopes suppress resin-like harsh normals; flatten where foam covers.
    strength = .022 * (1 - .60 * f["foam"])
    nx, ny = -du * strength, -dv * strength
    length = np.sqrt(1 + nx * nx + ny * ny)
    rough = sat(.38 + .38 * f["foam"] + .16 * f["fine"] + .06 * (1 - height))
    body = stack(f["density"], f["foam"], f["rupture"], f["coverage"])
    normal = stack(nx / length * .5 + .5, ny / length * .5 + .5, .5 / length + .5, rough)
    ff = water_fields(512, 512)
    flow = stack(.5 + .33 * ff["dx"], .5 + .27 * ff["dy"], ff["height"], np.ones_like(ff["height"]))
    return body, normal, flow


def tile_grid():
    # 256 native pixels per tile; all structures leave additional room before
    # the exactly zero 12px gutter. No visual board labels enter source atlases.
    return grid(256, 256)


def protect_tile(a):
    # Straight data channels: do not premultiply source RGB. Zero RGBA at gutter
    # and fade alpha over another 4px as a final support boundary, not a silhouette.
    border = np.minimum.reduce(np.meshgrid(np.arange(256), np.arange(256)) +
                               np.meshgrid(np.arange(255, -1, -1), np.arange(255, -1, -1)))
    a = a.copy()
    a[..., 3] = np.rint(a[..., 3].astype(float) * smooth(GUTTER - 1, GUTTER + 4, border)).astype(np.uint8)
    a[border < GUTTER] = 0
    return a


def stroke(x, y, points, radii):
    """Analytic tapered capsules, not isolated overlapping round brush stamps."""
    shape = np.zeros_like(x)
    depth = np.zeros_like(x)
    for i in range(len(points) - 1):
        p, q = points[i], points[i + 1]
        vx, vy = q[0] - p[0], q[1] - p[1]
        t = sat(((x - p[0]) * vx + (y - p[1]) * vy) / max(vx * vx + vy * vy, 1e-10))
        radius = radii[i] * (1 - t) + radii[i + 1] * t
        dist = np.sqrt((x - p[0] - vx * t)**2 + (y - p[1] - vy * t)**2)
        shape = np.maximum(shape, smooth(-.003, .003, radius - dist))
        depth = np.maximum(depth, np.sqrt(sat(1 - (dist / np.maximum(.0001, radius))**2)))
    return shape, depth


def bezier(p0, p1, p2, p3, count=22):
    t = np.linspace(0, 1, count)[:, None]
    return (1-t)**3*np.array(p0) + 3*(1-t)**2*t*np.array(p1) + 3*(1-t)*t*t*np.array(p2) + t**3*np.array(p3)


def foam_frame(t):
    x, y = tile_grid()
    # One local water tongue pulled lower-left -> upper-right along a bent
    # material axis. There is no horizontal bowl, crown arc or row of emitters.
    def width_at(s):
        return .010 + .106*np.exp(-((s-.55)/.245)**2)

    def point(s, q, age):
        stretch = smooth(0, .58, age)
        px = .145 + .020*age + (.49+.13*stretch)*s
        py = .755 + .18*max(0, age-.48)**2 - (.36+.12*stretch)*s
        py += .102*np.sin(np.pi*s)*(1-.18*age)
        return np.array([px, py + q*width_at(s)*(1-.25*age)])

    stretch = smooth(0, .58, t)
    s = (x-.145-.020*t)/(.49+.13*stretch)
    cy = .755+.18*max(0,t-.48)**2-(.36+.12*stretch)*s
    cy += .102*np.sin(np.pi*s)*(1-.18*t)
    q = (y-cy)/(width_at(s)*(1-.25*t))
    # Fixed material coordinates for all ages: pores stretch with the tongue.
    warp = fbm(.14+s*.63, .50+q*.14, 5, 6, SEED+300)-.5
    material = fbm(.14+s*.63+.035*warp, .50+q*.14+.035*warp, 8, 9, SEED+301)
    detail = fbm(.14+s*.63+.025*warp, .50+q*.14, 22, 27, SEED+302, (.80,.20))
    upper = -.73-.17*np.sin(s*5.3)-.24*warp
    lower = 1.05+.24*np.sin(s*8.6+.3)+.28*warp
    # Soft irregular taper at both ends and a torn, lean trailing side.
    support = smooth(upper-.065, upper+.065, q)*(1-smooth(lower-.07, lower+.07, q))
    support *= smooth(.015,.105,s)*(1-smooth(.945,1.035,s))
    trailing = smooth(.25,.43,material+.18*s)
    support *= 1-smooth(.18,.90,q)*(1-trailing)*.85
    threshold = .25+.49*smooth(.13,.90,t)
    pore_field = material+.055*(detail-.5)
    holes = smooth(threshold-.030, threshold+.036, pore_field)
    life = 1-smooth(.51,.92,t)
    membrane = support*holes*life
    pressure = np.exp(-((q-upper-.13)/.24)**2)*smooth(.22,.65,s)
    pore_rim = np.exp(-((pore_field-threshold-.02)/.05)**2)
    foam = membrane*sat((.69*pressure+.52*pore_rim+.16)*(.31+.69*smooth(.29,.66,detail)))
    density = membrane*sat(.28+.38*material+.19*pressure+.12*detail)
    alpha = membrane*(.60+.32*material)
    light = (.25+.34*detail+.22*pressure)*(alpha>.003)

    # Exactly two short offshoots; their diagonal tips belong to the same tongue.
    # The long leading edge is the tongue itself, not a third upright spike.
    offshoots = [(.64,-.61,.735,-1.14,.020),(.39,.77,.467,1.45,.013)]
    for root_s, root_q, tip_s, tip_q, radius in offshoots:
        root=point(root_s,root_q,t)
        tip=point(tip_s,tip_q,t)
        p=bezier(root,root+np.array([.026,-.008]),tip+np.array([-.012,.014]),tip)
        pq=np.linspace(0,1,len(p))
        radii=radius*(1-pq)**1.15+.0021
        shape,thick=stroke(x,y,p,radii)
        # Fade the root cap into the membrane, avoiding circular 'ears'.
        shape*=smooth(root[0]-.015,root[0]+.024,x)*life
        shape*=.75+.25*detail
        alpha=np.maximum(alpha,shape*.83)
        density=np.maximum(density,shape*(.42+.26*thick))
        foam=np.maximum(foam,shape*(.31+.29*detail))
        light=np.maximum(light,shape*(.35+.26*thick))

    # Three unequal fragments, separated in space and release time. Each starts
    # inside its own material tip and inherits that tip's velocity at release.
    drops=[(.977,-.06,.56,.026),(.735,-1.14,.66,.014),(.467,1.45,.72,.0085)]
    for i,(tip_s,tip_q,detach,radius) in enumerate(drops):
        origin=point(tip_s,tip_q,detach)
        velocity=(point(tip_s,tip_q,detach+.0001)-point(tip_s,tip_q,detach-.0001))/.0002
        dt=max(0,t-detach)
        head=point(tip_s,tip_q,t) if t<=detach else origin+velocity*dt+np.array([.025*dt*dt,1.45*dt*dt])
        tangent=point(tip_s+.003,tip_q,min(t,detach))-point(tip_s-.003,tip_q,min(t,detach))
        tangent/=np.linalg.norm(tangent)
        moving=velocity+np.array([.05*dt,2.90*dt])
        moving/=max(.001,np.linalg.norm(moving))
        blend=smooth(detach,detach+.16,t)
        axis=tangent*(1-blend)+moving*blend
        axis/=max(.001,np.linalg.norm(axis))
        side=np.array([-axis[1],axis[0]])
        grow=smooth(detach-.16,detach+.04,t)
        rx=radius*(.22+.78*grow)
        ry=rx*(1.13+.76*smooth(detach,.94,t))
        dx,dy=x-head[0],y-head[1]
        along=dx*axis[0]+dy*axis[1]
        cross=dx*side[0]+dy*side[1]
        d=np.sqrt((cross/rx)**2+(along/ry)**2)
        fade=1-smooth(.82,1,t)
        headmask=(1-smooth(.85,1.12,d))*fade
        alpha=np.maximum(alpha,headmask*.92)
        density=np.maximum(density,headmask*(.47+.30*np.sqrt(sat(1-d*d))))
        foam=np.maximum(foam,headmask*(.60+.17*detail))
        lit=sat(.48+.18*np.sqrt(sat(1-d*d))-.12*cross/rx)
        light=np.maximum(light,headmask*lit)
        # Curved thin tails remain attached to their heavy heads during descent.
        length=(.033+.070*smooth(detach,.92,t))*(1-i*.18)
        tail=head-axis*length+side*length*.22
        p=bezier(tail,tail+axis*length*.33,head-axis*length*.23,head)
        radii=np.linspace(.0008,rx*.43,len(p))
        tailmask,thick=stroke(x,y,p,radii)
        tailmask*=smooth(detach-.035,detach+.11,t)*fade
        alpha=np.maximum(alpha,tailmask*.73)
        density=np.maximum(density,tailmask*(.36+.27*thick))
        foam=np.maximum(foam,tailmask*.43)
        light=np.maximum(light,tailmask*.57)
    return protect_tile(stack(density,foam,light*(alpha>.003),alpha))


def noise3(x, y, z, scale, seed):
    rng=np.random.default_rng(seed)
    lattice=rng.random((31,31,31)).astype(np.float32)
    xx,yy,zz=(x*scale+14,y*scale+14,z*scale+14)
    ix,iy,iz=(np.floor(xx).astype(int),np.floor(yy).astype(int),np.floor(zz).astype(int))
    fx,fy,fz=(quintic(xx-ix),quintic(yy-iy),quintic(zz-iz))
    result=np.zeros_like(x)
    for dz in range(2):
        for dy in range(2):
            for dx in range(2):
                weight=(fx if dx else 1-fx)*(fy if dy else 1-fy)*(fz if dz else 1-fz)
                result += weight*lattice[(iz+dz)%31,(iy+dy)%31,(ix+dx)%31]
    return result


def mist_frame(t):
    # A single advected 3D density volume, integrated through 28 depth samples.
    # Three joined unequal folds form one bent plume, not alpha circle stamps.
    u,v=tile_grid()
    z=np.linspace(-.72,.72,28,dtype=np.float32)[:,None,None]
    xx=np.broadcast_to((u-.48)[None,:,:],(28,256,256)).astype(np.float32)
    yy=np.broadcast_to((.53-v)[None,:,:],xx.shape).astype(np.float32)
    zz=np.broadcast_to(z,xx.shape)
    scale=1+.35*t
    xx=(xx-.045*t)/scale
    yy=(yy-.025*t)/(1+.22*t)
    theta=-.72*t*np.exp(-((xx+.03)**2+(yy-.01)**2)*4)
    x=xx*np.cos(theta)-yy*np.sin(theta)
    y=xx*np.sin(theta)+yy*np.cos(theta)
    zz=zz/(1+.16*t)
    # Fixed coordinate warps: rotation/expansion moves the same field.
    warp=noise3(x,y,zz,4,SEED+400)-.5
    wx=x+.15*warp
    wy=y+.12*(noise3(x,y,zz,4,SEED+401)-.5)
    wz=zz+.14*warp
    support=np.zeros_like(x)
    for cx,cy,cz,rx,ry,rz,weight in [(-.13,-.08,.02,.21,.20,.30,1.0),
                                   (.02,.12,-.02,.20,.16,.26,.97),
                                   (.20,.09,.04,.145,.145,.23,.8),
                                   (.20,-.07,.03,.09,.14,.19,.52),
                                   (-.25,-.16,-.02,.11,.095,.17,.38)]:
        rr=((wx-cx)/rx)**2+((wy-cy)/ry)**2+((wz-cz)/rz)**2
        support += weight*np.exp(-rr*1.7)
    middle=noise3(wx,wy,wz,11,SEED+410)
    small=noise3(wx,wy,wz,24,SEED+411)
    detail=noise3(wx,wy,wz,42,SEED+412)
    # Diffusion removes internal wisps in material coordinates before final fade.
    rho=sat((support-.12-.13*t)+.32*(middle-.5)+.085*(small-.5)+.016*(detail-.5))
    rho*=smooth(.035,.18,support)
    rho*=1-.52*t
    gz,gy,gx=np.gradient(rho,.0533,1/255,1/255)
    length=np.sqrt(gx*gx+gy*gy+gz*gz+.8)
    # Light vector to screen upper-left and camera; front is negative depth.
    diffuse=sat((.43*gx+.63*gy+.65*gz)/length*.5+.5)
    # Directional optical occlusion: upstream upper-left/front density samples.
    occluder=np.zeros_like(rho)
    for step in (2,5,9):
        shifted=np.roll(rho,(step//2,step,step),axis=(0,1,2))
        # Support has already vanished at all volume edges; wrap is therefore zero.
        occluder+=shifted*.42
    shade=1-np.exp(-occluder*1.4)
    trans=np.exp(-np.cumsum(rho*.43,axis=0))
    weight=rho*trans
    denom=weight.sum(axis=0)+1e-7
    density=1-np.exp(-rho.sum(axis=0)*.40)
    lit=(weight*(.20+.80*diffuse)*(1-.58*shade)).sum(axis=0)/denom
    occlusion=(weight*(.68*shade+.32*sat(-yy*1.3+.35))).sum(axis=0)/denom
    life=1-smooth(.50,1.0,t)
    alpha=density**.88*life
    return protect_tile(stack(density,lit*(density>.001),occlusion,alpha))


def droplet_frame(index):
    x,y=tile_grid()
    rng=np.random.default_rng(SEED+500+index)
    if index < 5:
        bend=[-.13,.12,-.19,.26,-.28][index]
        head=np.array([.52+bend*.25,.73 if index<3 else .70])
        tail=np.array([.50-bend*.45,.17 if index!=1 else .30])
        points=bezier(tail,(tail[0]+bend*.55,.30),(head[0]-bend,.60),head,34)
        q=np.linspace(0,1,len(points))
        r=(.002+.017*q*q) if index<3 else (.002+.010*q**1.4)
        r*=1+.15*np.sin(q*11+index)
        shape,thick=stroke(x,y,points,r)
        rx=[.041,.051,.027,.021,.016][index]
        ry=[.069,.055,.080,.042,.035][index]
        dist=np.sqrt(((x-head[0])/rx)**2+((y-head[1])/ry)**2)
        shape=np.maximum(shape,1-smooth(.94,1.03,dist))
        thick=np.maximum(thick,np.sqrt(sat(1-dist**2)))
        # Tiny satellite at the thinned neck is an attached-family variation.
        if index in (2,4):
            d=np.sqrt(((x-tail[0]-.035)/.009)**2+((y-tail[1]+.026)/.015)**2)
            shape=np.maximum(shape,1-smooth(.80,1.10,d))
    else:
        wx=x+.030*(fbm(x,y,5,6,SEED+530)-.5)
        wy=y+.045*(fbm(x,y,6,5,SEED+531)-.5)
        shape=np.zeros_like(x)
        for j in range(6 if index==5 else 4):
            cx=.34+j*.053+rng.uniform(-.020,.02)
            cy=.45+.10*np.sin(j*.92+index)+rng.uniform(-.035,.035)
            rx=rng.uniform(.035,.070)
            ry=rng.uniform(.050,.110)
            dist=np.sqrt(((wx-cx)/rx)**2+((wy-cy)/ry)**2)
            shape=np.maximum(shape,1-smooth(.75,1.06,dist))
        pores=fbm(wx,wy,21,24,SEED+540+index)
        shape*=smooth(.27,.44,pores)
        thick=sat(shape*(.45+.55*pores))
    dy,dx=np.gradient(thick)
    gx,gy=dx*255,dy*255
    directional=(-.70*gx-.50*gy)/np.sqrt(1+gx*gx+gy*gy)
    light=sat(.36+.32*thick+.23*directional)
    light*=shape>.003
    return protect_tile(stack(light,light,light,shape))


def atlas(frames,columns=4):
    rows=len(frames)//columns
    return np.concatenate([np.concatenate(frames[row*columns:(row+1)*columns],axis=1) for row in range(rows)],axis=0)


def generate():
    print("Generating shared water fields / tangent normals...",flush=True)
    body,normal,flow=water_assets()
    print("Generating continuous membrane -> fingers -> falling drops...",flush=True)
    spray=[foam_frame(i/15) for i in range(16)]
    print("Integrating advected volumetric mist (16 x 28 slices)...",flush=True)
    mist=[]
    for i in range(16):
        mist.append(mist_frame(i/15))
        if i%4==3:
            print(f"  mist {i+1}/16",flush=True)
    droplets=[droplet_frame(i) for i in range(8)]
    return dict(zip(SPECS,(body,normal,flow,atlas(spray),atlas(mist),atlas(droplets))))


def font(size):
    try:
        return ImageFont.truetype("DejaVuSans.ttf",size)
    except OSError:
        return ImageFont.load_default(size=size)


def board_base(w,h,title):
    im=Image.new("RGB",(w,h),(16,23,29))
    ImageDraw.Draw(im).text((18,14),title,fill=(220,238,240),font=font(18))
    ImageDraw.Draw(im).text((18,39),"OFFLINE DATA INSPECTION | linear masks, diagnostic tint only | NOT A UNITY RENDER",fill=(145,168,180),font=font(12))
    return im


def tint(a,kind):
    f=a.astype(float)/255
    if kind=="water":
        low=np.array([.020,.090,.15])
        mid=np.array([.14,.54,.65])
        rgb=low+(mid-low)*f[...,:1]
        rgb=rgb*(1-f[...,1:2]*.83)+np.array([.78,.97,.99])*f[...,1:2]*.83
    elif kind=="mist":
        rgb=(.26+.72*f[...,1:2]-.26*f[...,2:3])*np.array([.62,.85,.91])
    elif kind=="spray":
        rgb=(.22+.42*f[...,:1]+.28*f[...,2:3])*np.array([.32,.73,.87])
        rgb=rgb*(1-f[...,1:2]*.62)+np.array([.86,.98,.99])*f[...,1:2]*.62
    else:
        rgb=f[...,:3]*np.array([.72,.91,.98])
    alpha=f[...,3:4]
    yy,xx=np.indices(a.shape[:2])
    bg=(.07+.025*((xx//16+yy//16)%2))[...,None]
    return Image.fromarray(np.uint8(sat(rgb*alpha+bg*(1-alpha))*255))


def boards(data):
    dest=OUT/"inspection"
    dest.mkdir(exist_ok=True)
    for name,kind in [("foam_spray_4x4.png","spray"),("mist_density_lighting_4x4.png","mist"),("droplet_spindrift_4x2.png","drop")]:
        if name not in data:
            continue
        a=data[name]
        rows=a.shape[0]//256
        im=board_base(1080,76+rows*274,name+" | top-left row-major / 256px native tiles")
        d=ImageDraw.Draw(im)
        channels=board_base(1080,76+rows*290,name+" | per-frame R / G / B / A at 112px")
        dc=ImageDraw.Draw(channels)
        for row in range(rows):
            for col in range(4):
                n=row*4+col
                tile=a[row*256:(row+1)*256,col*256:(col+1)*256]
                xx=18+col*266; yy=84+row*274
                im.paste(tint(tile,kind),(xx,yy))
                d.text((xx+4,yy+3),f"{n:02d}"+(f" / {n/15:.2f}" if rows==4 else ""),fill=(245,222,145),font=font(13))
                cy=84+row*290
                dc.text((xx,cy-18),f"{n:02d}  R / G  |  B / A",fill="white",font=font(12))
                for channel in range(4):
                    cimg=Image.fromarray(tile[...,channel]).convert("RGB").resize((112,112),Image.Resampling.LANCZOS)
                    channels.paste(cimg,(xx+(channel%2)*118,cy+(channel//2)*118))
        im.save(dest/(Path(name).stem+"_atlas.png"))
        channels.save(dest/(Path(name).stem+"_channels.png"))
    if "water_body_rgba.png" not in data:
        return
    water=board_base(1080,1070,"ROUND 2 / WATER CHANNELS | PNG TOP = WAVE LIP (+V)")
    draw=ImageDraw.Draw(water)
    body=data["water_body_rgba.png"]
    for i,label in enumerate(["R / wide density", "G / clustered pressure foam", "B / independent rupture", "A / coverage windows"]):
        xx=18+(i%2)*534; yy=90+(i//2)*282
        draw.text((xx,yy-21),label,fill="white",font=font(14))
        water.paste(Image.fromarray(body[...,i]).convert("RGB").resize((512,256)),(xx,yy))
    normal=data["water_normal_roughness.png"]
    flow=data["water_flow_rg.png"]
    water.paste(Image.fromarray(normal[...,:3]).resize((512,256)),(18,656))
    water.paste(Image.fromarray(flow[...,:3]).resize((256,256)),(552,656))
    water.paste(Image.fromarray(normal[...,3]).convert("RGB").resize((256,256)),(814,656))
    draw.text((18,634),"Normal RGB (+Y points UP) | same medium height",fill="white",font=font(14))
    draw.text((552,634),"Flow RG / height B",fill="white",font=font(14))
    draw.text((814,634),"Roughness A",fill="white",font=font(14))
    water.paste(tint(body,"water").resize((512,128)),(18,931))
    for c in range(4):
        small=Image.fromarray(body[...,c]).convert("RGB").resize((128,64),Image.Resampling.LANCZOS)
        water.paste(small,(552+(c%2)*262,946+(c//2)*0)) if c<2 else None
    draw.text((552,923),"Small footprint: R, G at 128 x 64",fill="white",font=font(13))
    water.save(dest/"water_channels.png")
    # Explicit two-tile inspection centered at the U join, not an averaged seam.
    seam=board_base(1080,662,"U REPEAT / TWO COPIES | join at center / V CLAMP")
    for i,(name,c) in enumerate([("water_body_rgba.png",0),("water_body_rgba.png",1),("water_body_rgba.png",3),("water_normal_roughness.png",None)]):
        a=data[name]
        raw=Image.fromarray(a[...,:3] if c is None else a[...,c]).convert("RGB").resize((512,128))
        yy=92+i*140
        seam.paste(raw,(18,yy));seam.paste(raw,(530,yy))
    seam.save(dest/"water_u_repeat.png")


def split_tiles(a):
    return [a[y:y+256,x:x+256] for y in range(0,a.shape[0],256) for x in range(0,a.shape[1],256)]


def validate(data):
    report={"status":"PASS", "assets":{},"limits":"Offline data checks only; native material, particle UV orientation, interpolation, compression and mip behavior remain unverified."}
    for name,(size,semantics) in SPECS.items():
        a=data[name]
        assert a.dtype==np.uint8 and a.shape==(size[1],size[0],4),(name,a.shape)
        stats={"size":list(size),"channel_ranges":[[int(a[...,c].min()),int(a[...,c].max())] for c in range(4)]}
        if name.startswith("water_"):
            delta=np.abs(a[:,0].astype(int)-a[:,-1].astype(int))
            stats["u_edge_max_lsb"]=int(delta.max())
            assert delta.max()<=1,(name,"U seam",delta.max())
            if name=="water_body_rgba.png":
                stats["v_alpha_edge_max_lsb"]=int(a[[0,-1],:,3].max())
                assert stats["v_alpha_edge_max_lsb"]==0
                for c in (0,1,2):
                    assert np.std(a[...,c])>8,(name,"flat channel",c)
                stats["density_rupture_correlation"]=round(float(np.corrcoef(a[...,0].ravel(),a[...,2].ravel())[0,1]),4)
                stats["foam_white_fraction"]=round(float(np.mean(a[...,1]>245)),5)
            if name=="water_normal_roughness.png":
                length=np.linalg.norm(a[...,:3].astype(float)/127.5-1,axis=-1)
                stats["normal_length_max_error"]=round(float(np.max(abs(length-1))),6)
                assert stats["normal_length_max_error"]<.012
            if name=="water_flow_rg.png":
                assert np.all(a[...,3]==255)
        else:
            tiles=split_tiles(a)
            gutter_max=max(int(max(tile[:GUTTER].max(),tile[-GUTTER:].max(),tile[:,:GUTTER].max(),tile[:,-GUTTER:].max())) for tile in tiles)
            stats["gutter_rgba_max_lsb"]=gutter_max
            assert gutter_max==0,(name,"gutter")
            stats["alpha_mass"]= [round(float(tile[...,3].sum()/255),2) for tile in tiles]
            stats["adjacent_alpha_mae"]=[round(float(np.mean(abs(b[...,3].astype(float)-aa[...,3]))/255),5) for aa,b in zip(tiles,tiles[1:])]
            stats["adjacent_alpha_cosine"]=[]
            for aa,b in zip(tiles,tiles[1:]):
                av,bv=aa[...,3].ravel().astype(float),b[...,3].ravel().astype(float)
                den=np.linalg.norm(av)*np.linalg.norm(bv)
                stats["adjacent_alpha_cosine"].append(round(float(np.dot(av,bv)/den),4) if den>0 else None)
            if "4x4" in name:
                assert max(stats["adjacent_alpha_mae"])<.10,(name,"temporal jump")
            if name=="droplet_spindrift_4x2.png":
                assert np.array_equal(a[...,0],a[...,1]) and np.array_equal(a[...,1],a[...,2])
        report["assets"][name]=stats
    return report


def manifest(data,checks):
    assets={}
    for name,(size,semantics) in SPECS.items():
        tiled="4x" in name
        entry=dict(path=name,size=list(size),mode="RGBA8",channels=dict(zip("RGBA",semantics)),
                   sha256=sha(encode(data[name])),sRGB=False,alpha="straight data, not premultiplied",
                   wrapU="Clamp" if tiled else "Repeat",wrapV="Clamp",filter="Bilinear",
                   recommended_mipmaps=False,recommended_compression="Uncompressed")
        if tiled:
            rows=4 if "4x4" in name else 2
            entry.update(atlas=dict(columns=4,rows=rows,tile_size=[256,256],gutter_px=12,
                                    frame_order="PNG top-left; row-major; first row frames 0,1,2,3",
                                    unity_tile_origin="((frame % 4)/4, 1 - (floor(frame/4)+1)/rows)",
                                    time="t=frame/15, continuous nonlooping" if rows==4 else "8 independent shape variants; do not animate as a lifecycle"))
        else:
            entry["uv_convention"]="U along flow; +V from foot to lip; PNG row 0 is lip; repeated endpoint samples at U=0/1"
        assets[name]=entry
    return dict(schema_version=2,round=2,seed=SEED,authoring="Original deterministic numpy/Pillow fields; no external image inputs",
                generator="../../generate_slazeya_storm_textures.py",generator_sha256=sha(Path(__file__).read_bytes()),
                command="py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py",
                seed_families={"water":f"{SEED}+10..220", "spray":f"{SEED}+300..301", "mist":f"{SEED}+400..412", "droplets":f"{SEED}+500..547"},
                semantics={"height_normal":"same medium field; Nx=-dH/dU, Ny=-dH/dV; strength .022 with foam flattening",
                           "flow":"decode RG * 2 - 1; perturbation, not full velocity; base direction stays +U",
                           "spray":"one diagonal curved water tongue, one long pressure edge and two short offshoots; material-coordinate pores; three staggered heavy fragments with continuous tip release",
                           "mist":"28-slice integration of fixed 3D field, inverse curl/expansion advection, front transmittance plus directional gradient/occlusion",
                           "atlas_lifetime":"nonlooping; use frame blending as needed and native confirmation of top-left order"},
                assets=assets,checks=checks,inspection_boards="inspection/*.png; OFFLINE data only, not Unity output",
                unverified=["Unity import/readback and atlas playback direction", "Material color/lighting and tangent handedness on actual deformed mesh", "Particle blend/overdraw and perceptual continuity at gameplay size", "Mip/compression sampling (recommended disabled/uncompressed)", "Final AAA visual acceptance, native bundle and game deployment"])


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify",action="store_true")
    parser.add_argument("--verify-repro",action="store_true")
    args=parser.parse_args()
    if args.verify:
        data={name:np.array(Image.open(OUT/name).convert("RGBA")) for name in SPECS}
        checks=validate(data)
        saved=json.loads((OUT/"manifest.json").read_text(encoding="utf-8"))
        assert sha(Path(__file__).read_bytes())==saved["generator_sha256"],"generator changed since generation"
        for name in SPECS:
            assert sha((OUT/name).read_bytes())==saved["assets"][name]["sha256"],(name,"file hash")
        print(json.dumps(checks,indent=2))
        return
    data=generate()
    checks=validate(data)
    if args.verify_repro:
        for name in SPECS:
            assert sha(encode(data[name]))==sha((OUT/name).read_bytes()),(name,"repro mismatch")
        print("PASS: all six regenerated PNGs are byte-identical.",flush=True)
        return
    OUT.mkdir(parents=True,exist_ok=True)
    for name,a in data.items():
        (OUT/name).write_bytes(encode(a))
    boards(data)
    (OUT/"manifest.json").write_text(json.dumps(manifest(data,checks),indent=2)+"\n",encoding="utf-8")
    print("PASS: six source PNGs, eight inspection boards and manifest: "+str(OUT),flush=True)


if __name__=="__main__":
    main()
