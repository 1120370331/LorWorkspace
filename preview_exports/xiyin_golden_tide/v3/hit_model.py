"""Finite-duration impact, boundary flow and peeling water at one target.

Coordinates are (body widths right, body heights up from feet, body widths
toward the camera).  The incoming jet starts on the negative-X side. A
normal impulse splits it into two unequal free-surface sheets that travel
around a convex body, detach, and fall onto its own ground contact.
"""
from functools import lru_cache
import math

import numpy as np


STEP = 1.0 / 240
KEY_RATE = 120
END = .635
ROWS, LANES = 65, 17
AXES = np.array([.46,.32,.25], np.float32)
CENTER = np.array([0.,.54,0.], np.float32)
_TRI = np.empty((0,3),np.int32)
_VERT = np.empty((0,3),np.float32)
_UV = np.empty((0,2),np.float32)
_FOAM = np.empty(0,np.float32)
_DROPS = np.empty((0,5),np.float32)
_EMPTY = (_VERT,_TRI,_UV,_FOAM)


def ease(a,b,x):
    u=np.clip((x-a)/(b-a),0.,1.)
    return u*u*(3.-2.*u)


def unit(x):
    return x/np.maximum(1e-7,np.linalg.norm(x,axis=-1,keepdims=True))


def surface(theta,lat):
    cs=np.cos(lat)
    n=np.stack([cs*np.cos(theta),np.sin(lat),cs*np.sin(theta)],axis=-1)
    return CENTER+n*AXES,unit(n/AXES)


@lru_cache(None)
def shell_topology(rows):
    grid=np.arange(rows*LANES,dtype=np.int32).reshape(rows,LANES)
    a,b,c,d=grid[:-1,:-1],grid[1:,:-1],grid[1:,1:],grid[:-1,1:]
    skin=np.stack([a,b,c,a,c,d],axis=-1).reshape(-1,3)
    mass=rows*LANES
    boundary=np.concatenate([grid[:,0],grid[-1,1:],grid[-2::-1,-1],grid[0,-2:0:-1]])
    q=np.roll(boundary,-1)
    ends=np.stack([q,boundary,boundary+mass,q,boundary+mass,q+mass],axis=-1).reshape(-1,3)
    return np.ascontiguousarray(np.vstack([skin,skin[:,::-1]+mass,ends]))


class Jet:
    def __init__(self,lower,rng):
        self.lower=lower
        self.rng=rng
        self.turn=-1. if lower else 1.
        self.phase=rng.uniform(-1.1,1.1)
        self.delay=.036 if lower else 0.
        self.duration=.111 if lower else .123
        u,q=np.meshgrid(np.linspace(0,1,ROWS),np.linspace(-1,1,LANES),indexing='ij')
        self.u,self.q=u,q
        self.birth=self.delay+self.duration*u
        # The first material hits as a broad, compressed bolus. Both the last
        # supplied water and the shoulders of the leading lip are thinner.
        pulse=.38+.62*np.sin(np.pi*(.12+.88*u))**.56
        self.halfwidth=(.082 if lower else .122)*pulse*(.27+.73*ease(-.015,.17,u))
        core_y=(.375 if lower else .463)+.028*np.sin(5*u+self.phase)
        y=core_y+q*self.halfwidth*(.76+.24*np.cos(2.8*u+self.phase))
        z=(.058 if lower else .048)+.021*np.sin(np.pi*u)+.034*q
        x=-AXES[0]*np.sqrt(np.maximum(.02,1-((y-CENTER[1])/AXES[1])**2-(z/AXES[2])**2))
        x-=.009+.052*u +.010*q*q
        self.pos=np.stack([x,y,z],axis=-1)
        self.initial=self.pos.copy()
        self.velocity=np.zeros_like(self.pos)
        self.velocity[:,:,0]=3.9+.6*(1-u)
        self.velocity[:,:,1]=.25 if not lower else -.11
        self.velocity[:,:,2]=-.38 if not lower else .51
        self.status=np.zeros(u.shape,np.uint8) # supplied, attached, peeled, grounded
        self.angle=np.full_like(u,np.pi)
        self.turns=np.zeros_like(u)
        self.lat=np.arcsin(np.clip((y-CENTER[1])/AXES[1],-.98,.98))
        self.lat0=self.lat.copy()
        self.lat_velocity=np.zeros_like(u)
        self.speed=np.zeros_like(u)
        self.peeled=np.full_like(u,END+1)
        self.hit_time=np.full_like(u,END+1)
        self.frost=np.zeros_like(u)
        self.drops=[]
        self.strip_uv=np.stack([u,(q+1)*.5],axis=-1).astype(np.float32)
        # Shoulders of a moving tongue cut the flow at slightly staggered
        # azimuths, avoiding an even bracelet on the silhouette.
        self.detach=(3.00 if lower else 4.93)+.21*np.sin(5*u+self.phase)+.23*q
        self.detach-=.19*np.sin(4.1*q+2*u+self.phase)

    def sprinkle(self,t,mask,mode):
        ids=np.argwhere(mask)
        if not len(ids):
            return
        # Decimate the event surface, then emit families at wetted vertices.
        prob={'hit':.013,'exit':.029,'floor':.012}[mode]
        for i,j in ids[self.rng.random(len(ids))<prob]:
            p=self.pos[i,j]
            v=self.velocity[i,j]
            if mode=='hit':
                base=np.array([-.42,.67,-.48*self.turn])
            elif mode=='floor':
                base=np.array([.4*np.sign(v[0]),.82,.4*np.sign(v[2])])
            else:
                base=v*.33+np.array([.09,.57,.11])
            for _ in range(self.rng.integers(2,5)):
                speed=base+self.rng.normal(0,(.37,.35,.34))
                radius=self.rng.uniform(.0031,.0125)
                self.drops.append((t,*p,*speed,radius,self.rng.uniform(.48,1.)))

    def step(self,t):
        q,u=self.q,self.u
        on=t>=self.birth
        incoming=on&(self.status==0)
        self.pos[incoming]+=STEP*self.velocity[incoming]
        local=(self.pos-CENTER)/AXES
        coll=incoming&(np.linalg.norm(local,axis=-1)<1.025)
        if np.any(coll):
            direction=unit(local[coll])
            normal=unit(direction/AXES)
            vn=np.maximum(0.,-np.sum(self.velocity[coll]*normal,axis=-1))
            # A continuously fed jet drives an impulse into the curved wall.
            # The pressure redirects the normal flux along one of two tangent
            # families. From here its material speed evolves independently.
            self.angle[coll]=np.arctan2(direction[:,2],direction[:,0])
            self.lat[coll]=np.arcsin(direction[:,1])
            self.lat0[coll]=self.lat[coll]
            self.speed[coll]=(vn*(2.46 if not self.lower else 2.00)+.62+.29*np.cos(2.5*q[coll])
                              +.28*np.sin(4.8*q[coll]+self.phase)*ease(.15,.78,1-u[coll]))
            self.hit_time[coll]=t
            self.status[coll]=1
            self.frost[coll]=.88
            self.sprinkle(t,coll,'hit')
        attached=self.status==1
        if np.any(attached):
            travel=self.turns
            # Under the shoulder the jet banks upward, then turns naturally
            # into the foreground across the chest. The lower stream passes
            # around the belly from the opposite depth side.
            if self.lower:
                track=.328+.127*ease(.25,2.22,travel)-.058*ease(2.1,3.24,travel)
            else:
                track=.463+.222*ease(.18,2.69,travel)-.227*ease(2.67,4.96,travel)
            span=self.halfwidth*(.81+.19*np.sin(1.62*travel+3*u+self.phase))
            level=track+q*span+.032*np.sin(2.34*travel+3.5*u+self.phase)
            req=np.arcsin(np.clip((level-CENTER[1])/AXES[1],-.91,.91))
            self.lat_velocity[attached]+=STEP*(1020*(req-self.lat)-56*self.lat_velocity)[attached]
            self.lat[attached]+=STEP*self.lat_velocity[attached]
            friction=.76+3.5*ease(.64*self.detach,self.detach,travel)
            self.speed[attached]*=np.exp(-friction[attached]*STEP)
            self.speed[attached]+=STEP*(.75*np.exp(-7*(t-self.hit_time[attached])))
            metric=np.cos(self.lat)*np.hypot(AXES[0]*np.sin(self.angle),AXES[2]*np.cos(self.angle))
            dangle=STEP*self.speed/np.maximum(.12,metric)
            self.turns[attached]+=dangle[attached]
            self.angle[attached]+=self.turn*dangle[attached]
            skin,normal=surface(self.angle,self.lat)
            compress=np.exp(-np.clip(t-self.hit_time,0,1)*28)
            lip=(.016+.012*np.sin(np.pi*u)**2+.014*compress
                 +.014*q*q*ease(.35,1.7,travel)
                 +.009*np.sin(2.8*travel+4*q+self.phase)*ease(0,.9,travel))
            target=skin+normal*lip[:,:,None]
            self.velocity[attached]=(target[attached]-self.pos[attached])/STEP
            self.pos[attached]=target[attached]
            ledge=np.clip(np.abs(q)**1.5*(.50+.35*np.cos(8*u+3*q+self.phase)),0,1)
            foam=.18+.76*ledge+.56*compress
            self.frost[attached]=np.maximum(.978*self.frost[attached],foam[attached])
            release=attached&(travel>=self.detach)
            if np.any(release):
                incident=self.velocity[release]
                speed=np.clip(.75*np.linalg.norm(incident,axis=-1),1.6,3.7)
                self.velocity[release]=unit(incident)*speed[:,None]+normal[release]*.63
                self.velocity[:,:,1][release]-=.52+.19*u[release]
                self.status[release]=2
                self.peeled[release]=t
                self.frost[release]=np.maximum(self.frost[release],.57+.29*np.abs(q[release]))
                self.sprinkle(t,release,'exit')
        loose=self.status==2
        if np.any(loose):
            # The deflected liquid rejoins the incoming +X current as it
            # leaves the flank. This bends its path toward the field centre
            # after parting around the obstacle rather than stopping there.
            entrain=ease(.006,.101,t-self.peeled)*(1-ease(.36,.56,t))
            self.velocity[:,:,0][loose]+=STEP*(7.1*entrain*(2.45-self.velocity[:,:,0]))[loose]
            self.velocity[:,:,1][loose]-=STEP*13.8
            self.velocity[loose]*=math.exp(-2.23*STEP)
            self.pos[loose]+=STEP*self.velocity[loose]
            # Exiting streams can briefly meet the curved target again;
            # remove inward motion but preserve the flow heading down it.
            delta=(self.pos-CENTER)/AXES
            touch=loose&(np.linalg.norm(delta,axis=-1)<1.013)
            if np.any(touch):
                n=unit(delta[touch]/AXES)
                self.pos[touch]=CENTER+unit(delta[touch])*AXES+n*.009
                self.velocity[touch]-=np.minimum(0,np.sum(n*self.velocity[touch],axis=-1))[:,None]*n
            grounded=loose&(self.pos[:,:,1]<=.013)
            if np.any(grounded):
                self.pos[:,:,1][grounded]=.012
                self.status[grounded]=3
                self.velocity[:,:,1][grounded]=0
                self.velocity[grounded]*=.71
                self.frost[grounded]=.83
                self.sprinkle(t,grounded,'floor')
        floor=self.status==3
        self.pos[floor]+=STEP*self.velocity[floor]
        self.velocity[floor]*=math.exp(-10*STEP)
        self.frost[self.status>=2]*=math.exp(-5.7*STEP)

    def snapshot(self):
        # Extrude normal to the local liquid sheet. Orient the material's
        # positive face consistently for each wrapping direction.
        du=np.gradient(self.pos,axis=0)
        dv=np.gradient(self.pos,axis=1)
        normal=unit(np.cross(du,dv))
        thick=(.012+.037*(1-self.q*self.q))*(.35+.65*ease(.003,.12,1-self.u))
        thick*=.71+.29*np.sin(np.pi*self.u)
        thick*=np.where(self.status>=2,.55,1.)
        outer=self.pos+normal*thick[:,:,None]*.5
        inner=self.pos-normal*thick[:,:,None]*.5
        outer[:,:,1]=np.maximum(.0018,outer[:,:,1])
        inner[:,:,1]=np.maximum(.0012,inner[:,:,1])
        return (np.stack([outer,inner]).astype(np.float32),
                np.clip(self.frost,0,1).astype(np.float32))


@lru_cache(maxsize=32)
def _bake(seed):
    rng=np.random.default_rng(int(seed)&0xffffffff)
    keys=[]
    droplets=[]
    for lower in (False,True):
        jet=Jet(lower,rng)
        k0=jet.snapshot()
        count=int(np.ceil(END*KEY_RATE))+1
        pos=np.empty((count,)+k0[0].shape,np.float32)
        foam=np.empty((count,)+k0[1].shape,np.float32)
        pos[0],foam[0]=k0
        for tick in range(1,2*(count-1)+1):
            jet.step(tick*STEP)
            if tick%2==0:pos[tick//2],foam[tick//2]=jet.snapshot()
        keys.append((jet.delay,jet.duration,pos,foam,jet.strip_uv))
        droplets.extend(jet.drops)
    return keys,np.asarray(droplets,np.float32).reshape(-1,9)


@lru_cache(None)
def _impact_grid():
    rings,angles=9,43
    rho,phi=np.meshgrid(np.linspace(.045,1,rings),2*np.pi*np.arange(angles)/angles,indexing='ij')
    indices=np.arange((rings-1)*angles,dtype=np.int32)
    right=(indices//angles)*angles+(indices%angles+1)%angles
    cap=rings*angles
    a=np.arange(angles,dtype=np.int32)
    front=np.vstack([np.stack([indices,indices+angles,right],axis=1),
                     np.stack([right,indices+angles,right+angles],axis=1),
                     np.stack([a*0+cap,a,(a+1)%angles],axis=1)])
    back=front[:,::-1]+cap+1
    out=a+(rings-1)*angles
    nxt=(a+1)%angles+(rings-1)*angles
    side=np.stack([nxt,out,out+cap+1,nxt,out+cap+1,nxt+cap+1],axis=1).reshape(-1,3)
    triangles=np.ascontiguousarray(np.vstack([front,back,side])[:,::-1],dtype=np.int32)
    return np.r_[rho.ravel(),0.],np.r_[phi.ravel(),0.],triangles


def _contact_bolus(t,seed):
    # Incoming water builds pressure on the near-facing quarter of the impact
    # flank, flattens, and drains along the same streamlines supplying both
    # sheets. The compressed footprint is on the curved collision surface.
    rho,phi,faces=_impact_grid()
    pressure=(1-math.exp(-t/.013))*(1-ease(.030,.119,t))
    spread=.15+.85*(1-math.exp(-t/.017))
    tail=1-ease(.053,.120,t)
    grain=np.sin(5*phi+.43*seed)+.51*np.sin(9*phi-.34*seed)
    r=rho*(1+.093*grain)
    theta=2.84-.58*ease(.008,.114,t)+(.70+.09*np.cos(phi))*np.cos(phi)*r*spread*tail
    lat=-.215+.097*ease(.025,.108,t)+.37*np.sin(phi)*r*spread*tail
    skin,normal=surface(theta,lat)
    # Impact displaces/squeezes a pocket, spreads its leading hem, then sends
    # its material around the body instead of preserving a circular patch.
    thick=tail*(.006+.071*(1-rho*rho)*(.67+.33*np.sin(2.1*phi+2*rho+t*25)**2))
    skin+=normal*(.011+.027*pressure)
    p=np.concatenate([skin+normal*thick[:,None],skin+normal*(thick*.07)[:,None]])
    uv=np.column_stack([.50+.48*np.cos(phi)*rho,.50+.48*np.sin(phi)*rho])
    fo=pressure*(.40+.45*ease(.35,.87,rho)+.10*np.sin(phi*4+seed)**2)
    return p.astype(np.float32),faces,np.tile(uv,(2,1)).astype(np.float32),np.r_[fo,.78*fo].astype(np.float32)


def sample_hit(age,seed):
    """Return one closed volumetric mesh and its airborne droplets for this victim."""
    if age<0 or age>=END:
        return _EMPTY,_DROPS
    jets,marks=_bake(seed)
    clock=age*KEY_RATE
    k=int(clock)
    blend=np.float32(clock-k)
    positions=[]; faces=[]; coords=[]; whites=[]
    base=0
    if .0001<age<.119:
        bolus,index,material,frost=_contact_bolus(age,seed)
        positions.append(bolus);faces.append(index+base);coords.append(material);whites.append(frost)
        base+=len(bolus)
    for delay,duration,pos,foam,uv in jets:
        n=min(ROWS,int((age-delay)/duration*(ROWS-1))+1)
        if n<2:continue
        # Once water hits the ground it drains in material order: the last
        # incoming rows remain for a moment after the first ones wash out.
        first=int(ease(.447+delay,.557+delay,age)*(ROWS-1))
        if n-first<2:continue
        a=(1-blend)*pos[k,:,first:n]+blend*pos[k+1,:,first:n]
        f=(1-blend)*foam[k,first:n]+blend*foam[k+1,first:n]
        mapped=uv[first:n].reshape(-1,2)
        positions.append(a.reshape(-1,3));faces.append(shell_topology(n-first)+base)
        coords.extend((mapped,mapped));whites.extend((f.ravel(),.82*f.ravel()))
        base+=a.shape[0]*a.shape[1]*a.shape[2]
    shell=_EMPTY if not positions else tuple(np.ascontiguousarray(a) for a in
        (np.vstack(positions),np.vstack(faces),np.vstack(coords),np.concatenate(whites)))
    tau=age-marks[:,0]
    valid=(tau>=0)&(tau<.31)
    drops=marks[valid]
    tau=tau[valid]
    drag=2.7
    advance=-np.expm1(-drag*tau)/drag
    p=drops[:,1:4]+drops[:,4:7]*advance[:,None]
    p[:,1]-=10.7*(tau-advance)/drag
    intensity=drops[:,8]*ease(0,.009,tau)*(1-ease(.13,.30,tau))*(1-ease(.49,END,age))
    keep=(p[:,1]>.007)&(intensity>.013)
    return shell,np.ascontiguousarray(np.column_stack([p[keep],drops[keep,7],intensity[keep]]),dtype=np.float32)
