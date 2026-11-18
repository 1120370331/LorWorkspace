"""Converging, finite-thickness breakers. Units are field radii and seconds.

The two shoals travel inward while pumping the water column. Released
parcels retain their velocity, roll over and enter the faster, shallower
return current. A small hydraulic jump at the vortex throat converts some
of that incoming momentum into whitewater before the column drains.
"""
from functools import lru_cache
from pathlib import Path
from threading import Lock
import sys

import numpy as np
from scipy.interpolate import PchipInterpolator

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "v2"))
import wave_sim as water


# Signed displacement of the travelling pressure front along the bed.
_RUN = PchipInterpolator(
    [0., .17, .40, .58, .75, .96, 1.15, 1.34, 1.70],
    [-.095, -.070, .001, .071, .197, .402, .585, .725, .790])


class InwardBreaker(water.Breaker):
    def pressure_path(self, t):
        p = super().pressure_path(t)
        if not hasattr(self, "current_weight"):
            # The low heel is supplied from farther out. The overhanging
            # lip and the foam on it travel with the pressure front.
            self.current_weight = .32 + .68*water.ramp(.05, .64, self.v)
            self.jumped = np.zeros(self.u.shape, dtype=bool)
        r = np.hypot(p[:,:,0], p[:,:,2])
        distance = float(_RUN(t)) * self.current_weight
        # Translate the thick sections along local flow directions rather
        # than scaling vertex height, thickness and cross-section width.
        new_r = np.maximum(.04, r-distance)
        p[:,:,0] *= new_r/r
        p[:,:,2] *= new_r/r
        return p

    def step(self, t, frame):
        p, vel = self.pos, self.vel
        dt = water.DT
        target = self.pressure_path(t)
        transport = (target-self.previous_drive)/dt
        self.previous_drive = target
        release_time = (.72 if self.front else .69) + .036*self.lobes + .055*(1-self.v)
        free = water.ramp(release_time,release_time+.095,t)
        stiffness = (200.+87.*(1-self.v))*(1-free)
        acc = stiffness[...,None]*(target-p) + (1.65*np.sqrt(stiffness))[...,None]*(transport-vel)
        acc += 29.*water.laplace(p) + 4.8*water.laplace(vel)
        acc[:,:,1] -= .12 + 7.45*free

        r = np.maximum(.001, np.hypot(p[:,:,0],p[:,:,2]))
        nx, nz = p[:,:,0]/r, p[:,:,2]/r
        vr = vel[:,:,0]*nx + vel[:,:,2]*nz
        vt = -vel[:,:,0]*nz + vel[:,:,2]*nx
        throat = water.ramp(.035,.215,r)
        through = -(.38 + .61*self.current_weight)*throat
        turn = (.15*throat + (1-throat)*1.2*r)
        # Hydrostatic slope and bed drag act along the INCOMING current.
        # In the throat radial flow converges and turns into circulation.
        ar = (through-vr)*free*(7.8+5*(1-throat))
        at = (turn-vt)*free*(3.7+6*(1-throat))
        acc[:,:,0] += nx*ar-nz*at
        acc[:,:,2] += nz*ar+nx*at

        floor = .004+.002*self.v
        jump = (~self.jumped) & (r<.185) & (vr<-.32) & (p[:,:,1]<.042) & (self.v>.55)
        jump_speed = np.minimum(.91, -vr)*(.48+.50*self.end_taper)*jump
        vel[:,:,1] += jump_speed
        self.jumped |= jump
        arriving_y = vel[:,:,1].copy()
        vel += dt*acc
        old = p.copy()
        p += dt*vel
        for a,b,limit in ((p[:-1],p[1:],self.u_limit),
                          (p[:,:-1],p[:,1:],self.v_limit)):
            d = b-a
            segment = water.length(d)
            correction = (.41*np.maximum(0,segment-limit)/np.maximum(segment,1e-8))[...,None]*d
            a += correction
            b -= correction
        impact = (p[:,:,1]<floor) & (vel[:,:,1]<0)
        p[:,:,1] = np.maximum(p[:,:,1],floor)
        vel[:] = (p-old)/dt
        # The bed cancels vertical momentum only. A fraction of the impact
        # feeds the inward-running bore instead of kicking it back to shore.
        impact_speed = np.minimum(.83,np.maximum(0.,-arriving_y-.15))*.13*impact*throat
        friction = np.where(impact,.990,.9985)
        vel[:,:,0] = (vel[:,:,0]-nx*impact_speed)*friction
        vel[:,:,2] = (vel[:,:,2]-nz*impact_speed)*friction

        normal,area,tangent = water.jacobian(p)
        turn_rate = water.length(np.gradient(tangent,axis=1))
        squeeze = np.clip((self.area0/(area+1e-8)-.78)*1.35,0,1)
        lip = water.ramp(.71,.97,self.v)
        rough = np.clip(.59+.31*self.lobes+.26*self.slow_fingers,0,1)
        crest = water.ramp(.12,.37,p[:,:,1])*(.60*water.ramp(.11,.46,turn_rate)+lip*.78)
        white = (crest*(.47+.68*rough)+.24*squeeze*lip)*self.end_taper
        white += water.ramp(.08,.32,p[:,:,1])*(.49*self.pore_rim+.21*self.tongues*lip)
        white += np.clip(-arriving_y*.8,0,1)*impact*rough + jump_speed*.92
        self.prev_foam = np.maximum(self.prev_foam*np.exp(-dt*5.1),np.clip(white,0,1))
        jets = self.jet_index
        self.history.append((p[jets,-1].copy(),vel[jets,-1].copy(),water.unit(p[jets,-1]-p[jets,-3])))
        if len(self.history)>32:
            self.history.pop(0)
        if frame%3==0 and .22<t<1.46:
            self.release_drops(t,impact,arriving_y)
        if np.any(jump):
            self.eye_splash(t,jump,jump_speed)
        return normal,area

    def release_drops(self, t, impact, arriving_y):
        start = len(self.spray_births)
        super().release_drops(t,impact,arriving_y)
        for i in range(start,len(self.spray_births)):
            time,x,y,z,vx,vy,vz,radius,gain = self.spray_births[i]
            length = max(.035,np.hypot(x,z))
            nx,nz = x/length,z/length
            # Droplets inherit both the upstream speed and the lip shear.
            # Remove the shorewards impulse of the old release nozzle.
            correction = .28 + .64*max(0.,nx*vx+nz*vz+.20)
            self.spray_births[i] = time,x,y,z,vx-nx*correction,vy,vz-nz*correction,radius,gain

    def eye_splash(self,t,jump,jump_speed):
        # A finite group of marked columns impinges on the vortex throat.
        # Eject only a sparse subset so the center stays open and legible.
        i,j = np.where(jump)
        chosen = self.rng.random(len(i))<.023
        for a,b in zip(i[chosen],j[chosen]):
            p = self.pos[a,b]
            v = .72*self.vel[a,b]
            v[1] = self.rng.uniform(.52,.95)*jump_speed[a,b]+.20
            self.spray_births.append((t,*p,*v,self.rng.uniform(.0011,.0032),self.rng.uniform(.48,.86)))


def _bake():
    states=[]
    births=[]
    keys=round(water.END*water.STORAGE_RATE)+1
    stride=water.RATE//water.STORAGE_RATE
    for front in (False,True):
        breaker=InwardBreaker(front)
        normal,area,_=water.jacobian(breaker.pos)
        v0,f0=breaker.mesh(normal,area,0.)
        verts=np.empty((keys,)+v0.shape,np.float32)
        foam=np.empty((keys,)+f0.shape,np.float32)
        verts[0],foam[0]=v0,f0
        for frame in range(1,(keys-1)*stride+1):
            t=frame/water.RATE
            normal,area=breaker.step(t,frame)
            if frame%stride==0:
                verts[frame//stride],foam[frame//stride]=breaker.mesh(normal,area,t)
        states.append((verts,foam))
        births.extend(breaker.spray_births)
    return states,np.asarray(births,np.float32).reshape(-1,9)


_saved=None
_lock=Lock()


def state():
    global _saved
    if _saved is None:
        with _lock:
            if _saved is None:
                _saved=_bake()
    return _saved


def sample(t):
    states,_=state()
    key=np.clip(float(t),0,water.END)*water.STORAGE_RATE
    i=min(int(key),len(states[0][0])-2)
    w=np.float32(key-i)
    return tuple((np.ascontiguousarray(v[i]+w*(v[i+1]-v[i])),*water.topology(bool(j)),
                  np.ascontiguousarray(f[i]+w*(f[i+1]-f[i])))
                 for j,(v,f) in enumerate(states))


@lru_cache(maxsize=12)
def _air(t):
    _,all_drops=state()
    ages=t-all_drops[:,0]
    active=(ages>=0)&(ages<.49)
    drops,age=all_drops[active],ages[active]
    drag=2.3
    q=-np.expm1(-drag*age)/drag
    pos=drops[:,1:4]+drops[:,4:7]*q[:,None]
    pos[:,1]-=5.8*(age-q)/drag
    energy=drops[:,8]*water.ramp(0,.014,age)*(1-water.ramp(.18,.47,age))*(1-water.ramp(1.42,1.66,t))
    visible=(pos[:,1]>.004)&(energy>.012)
    velocity=drops[:,4:7]*np.exp(-drag*age[:,None])
    velocity[:,1]-=5.8*q
    return (np.ascontiguousarray(np.column_stack((pos[visible],drops[visible,7],energy[visible])),np.float32),
            np.ascontiguousarray(velocity[visible],np.float32))


def spray(t):
    return _air(float(t))[0]


def spray_motion(t):
    return _air(float(t))[1]
