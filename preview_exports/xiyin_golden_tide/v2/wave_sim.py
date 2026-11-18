"""Fixed-step, finite-thickness Lagrangian breakers for the Xiyin preview.

The shoaling pressure field drives a sheet up and around a submerged turning
reef.  Past the crest the pressure support is released.  Parcels then fall,
collide with the bed and carry their momentum into the outgoing swash.  Each
material column stores its water volume, which determines its thickness as
the surface stretches.  Lengths here are fractions of the whirlpool radius.
"""
from functools import lru_cache
from threading import Lock

import numpy as np
from scipy.interpolate import CubicSpline

NU, NV = 169, 43
RATE, STORAGE_RATE = 180, 60
END = 1.70
DT = 1.0 / RATE
FILAMENTS=((.165,.064,.017,1.),(.246,.085,.020,1.),(.256,.058,.011,-.85),(.336,.073,.014,.87))
RINGS,SIDES=13,9


def ramp(a, b, x):
    q = np.clip((x-a)/(b-a), 0., 1.)
    return q*q*(3.-2.*q)


def vec(x, y, z):
    return np.stack(np.broadcast_arrays(x, y, z), axis=-1).astype(np.float32)


def length(a):
    return np.sqrt(np.sum(a*a, axis=-1))


def unit(a):
    return a / np.maximum(length(a)[..., None], 1.e-8)


def jacobian(p):
    du = np.gradient(p, axis=0)
    dv = np.gradient(p, axis=1)
    cross = np.cross(du, dv)
    return unit(cross), length(cross), unit(dv)


def laplace(p):
    out = np.zeros_like(p)
    out[1:-1] += p[2:] + p[:-2] - 2*p[1:-1]
    out[:,1:-1] += p[:,2:] + p[:,:-2] - 2*p[:,1:-1]
    return out


@lru_cache(None)
def grid(front):
    u = np.linspace(0., 1., NU, dtype=np.float32)
    v = np.linspace(0., 1., NV, dtype=np.float32)
    u,v=np.meshgrid(u,v,indexing='ij')
    cells=np.ones((NU-1,NV-1),bool)
    pore_rim=np.zeros((NU,NV),np.float32)
    collars=[]
    # Small air tears in the vulnerable lip grow with the material Jacobian
    # as the sheet stretches over the reef. Each pore has a rounded liquid
    # rim running through the thickness; the opening is present in depth.
    pores=((.142,.905,1,1),(.198,.935,2,2),(.231,.858,1,1),
           (.282,.918,2,1),(.346,.952,1,1),(.377,.882,1,2))
    for k,(cu,cv,rx,ry) in enumerate(pores):
        i=round((cu+(.016 if front else 0.))*(NU-1))
        j=min(NV-ry-2,round(cv*(NV-1)))
        cells[i-rx:i+rx,j-ry:j+ry]=False
        a,b=np.mgrid[-rx:rx+1,-ry:ry+1]
        boundary=(np.abs(a)==rx)|(np.abs(b)==ry)
        da,db=a[boundary]/rx,b[boundary]/ry
        hyp=np.hypot(da,db)
        ii,jj=i+a[boundary],j+b[boundary]
        u[ii,jj]=(i+rx*(da/hyp+.22*db/hyp))/(NU-1)
        v[ii,jj]=(j+ry*db/hyp*(.87+.13*da/hyp))/(NV-1)
        pore_rim[ii,jj]=.6+.35*np.cos(k+np.arctan2(db,da))
        y0,y1=max(0,i-rx-3),min(NU,i+rx+4)
        x0,x1=max(0,j-ry-3),min(NV,j+ry+4)
        aa,bb=np.mgrid[y0:y1,x0:x1]
        # Surface tension pulls the edge in. Continue that pull over its
        # neighbors so no thick polygonal collar is left round a pinhole.
        dist=np.maximum(np.abs(aa-i)/rx,np.abs(bb-j)/ry)
        pull=.71*np.exp(-4*np.maximum(0,dist-1.)**2)
        collars.append((i,j,np.s_[y0:y1,x0:x1],pull[...,None].astype(np.float32)))
    return u,v,cells,pore_rim,collars


@lru_cache(None)
def topology(front):
    u,v,cells,_,_ = grid(front)
    uv = np.stack([u,v], axis=-1).reshape(-1,2)
    uv = np.ascontiguousarray(np.concatenate([uv, uv]))
    ids = np.arange(NU*NV).reshape(NU,NV)
    a,b,c,d = ids[:-1,:-1],ids[1:,:-1],ids[1:,1:],ids[:-1,1:]
    upper = np.stack([a,b,c,a,c,d],axis=2)[cells].reshape(-1,3)
    count = NU*NV
    lower = upper[:,::-1] + count
    edges=np.vstack([upper[:,[0,1]],upper[:,[1,2]],upper[:,[2,0]]])
    keys=np.min(edges,axis=1)*count+np.max(edges,axis=1)
    _,first,uses=np.unique(keys,return_index=True,return_counts=True)
    edge,nxt=edges[first[uses==1]].T
    sides = np.stack([nxt,edge,edge+count,nxt,edge+count,nxt+count],axis=1).reshape(-1,3)
    faces=[upper,lower,sides]
    uvs=[uv]
    first=2*count
    k=np.arange(RINGS-1)[:,None]*SIDES + np.arange(SIDES)[None,:]
    k2=(k//SIDES)*SIDES+(k%SIDES+1)%SIDES
    barrel=np.stack([k,k2,k+SIDES,k2,k2+SIDES,k+SIDES],axis=2).reshape(-1,3)
    k=np.arange(SIDES)
    ends=np.vstack([np.stack([RINGS*SIDES+k*0,(k+1)%SIDES,k],axis=1),
                    np.stack([RINGS*SIDES+1+k*0,(RINGS-1)*SIDES+k,
                              (RINGS-1)*SIDES+(k+1)%SIDES],axis=1)])
    for cu,_,_,_ in FILAMENTS:
        faces.extend([barrel+first,ends+first])
        uu,vv=np.meshgrid(cu+.010*np.sin(2*np.pi*k/SIDES),np.linspace(.89,.999,RINGS))
        uvs.append(np.vstack([np.stack([uu,vv],axis=-1).reshape(-1,2),[cu,.89],[cu,.999]]))
        first+=RINGS*SIDES+2
    return (np.ascontiguousarray(np.vstack(faces),dtype=np.int32),
            np.ascontiguousarray(np.vstack(uvs),dtype=np.float32))


class Breaker:
    def __init__(self, front):
        self.u,self.v,_,self.pore_rim,self.collars = grid(front)
        u,v=self.u,self.v
        self.front=front
        self.seed=.83 if front else 3.91
        self.span=(2.90 if front else 2.72)
        self.start=(-.07 if front else -3.20)
        self.height=.468 if front else .535
        self.center=.27 if front else .29
        self.sigma=.208 if front else .151
        self.end_taper=ramp(0,.094,u)*(1-ramp(.89,1.,u))
        self.r0=.56+.43*v
        # The free boundary is a scalloped, concave planform. Open bays
        # reach in from that boundary between differently sized tongues.
        # Every column still runs continuously from its submerged bed to
        # its real free edge; there is no opacity cut or missing back face.
        slots=(((.189,.034,.15),(.287,.029,.19),(.391,.030,.135)) if front
               else ((.162,.036,.12),(.232,.030,.11),(.367,.031,.14)))
        edge=np.ones_like(u)
        for cu,width,depth in slots:
            edge-=depth*np.exp(-.5*((u-cu)/width)**2)
        self.material_v=v-(1-edge)*np.maximum(0,(v-.75)/.25)
        m=self.material_v
        # A spilling reef has a concave section in its plane of advance.
        # The stem climbs slightly *backward*, its shoulder turns forward
        # over the cavity, and the very end of the lip folds down. The
        # cross section has an independent extent in the third dimension;
        # neighboring sections form the deep, sloping body of the breaker.
        knots=[0,.10,.20,.31,.43,.55,.66,.76,.84,.90,.95,1.]
        section=np.array([
            [.27,.012,.19], [.16,.040,.13], [.044,.114,.056], [-.054,.245,.005],
            [-.092,.426,-.036],[-.070,.630,-.060],[.015,.814,-.050],
            [.147,.944,-.010],[.272,1.,.047],[.375,.984,.093],
            [.420,.895,.111],[.405,.752,.108],
        ])
        section[:,0]*=.87 if front else 1.12
        self.section=CubicSpline(knots,section,bc_type='natural')(m).astype(np.float32)
        # Aperiodic variations in the water supply turn the lip into long
        # muscular lobes with smaller fingers instead of a smooth ribbon.
        self.lobes=(.48*np.sin(u*42.2+self.seed+1.1*np.sin(u*16.2))
                    +.26*np.sin(u*84.7-self.seed)+.16*np.sin(169.8*u+.93)
                    +.10*np.sin(251.1*u+self.seed))
        self.slow_fingers=(.53*np.sin(73.4*u+7.6*v+self.seed)
                           +.29*np.sin(127*u-10.5*v)+.18*np.sin(191*u+3*v))
        shift=.017 if front else 0.
        # Three coherent streams overrun the falling rim. The second forks
        # into two unequal tips. Their lengths follow local water supply and
        # dynamically carry pressure, strain and spray into the foreground.
        self.tongues=np.zeros_like(u)
        for centre,breadth,amount in ((.178,.016,.78),(.246,.017,1.),
                                     (.274,.008,.61),(.322,.014,.83)):
            self.tongues+=amount*np.exp(-((u-centre-shift)/breadth)**2)
        self.tongues=np.clip(self.tongues,0,1)
        self.jet_index=[round((where+shift)*(NU-1)) for where,_,_,_ in FILAMENTS]
        self.history=[]
        self.edge_gauge=1-ramp(.66,.996,v)
        ii,jj=np.mgrid[:NU,:NV]
        for i,j in zip(*np.where(self.pore_rim>0)):
            distance=np.sqrt(((ii-i)*.013)**2+((jj-j)*.020)**2)
            self.edge_gauge=np.minimum(self.edge_gauge,ramp(0,.047,distance))
        wet=self.end_taper*(.25+.75*np.maximum(0,np.sin(np.pi*v))**.7)
        self.base_width=(.004+.038*wet).astype(np.float32)
        self.pos=self.pressure_path(0.)
        self.vel=np.zeros_like(self.pos)
        self.previous_drive=self.pos.copy()
        self.prev_foam=np.zeros_like(u)
        _,self.area0,_=jacobian(self.pos)
        self.material_volume=self.area0*self.base_width
        # Liquid stretching limit, from the actual pumped-out crest.
        raised=self.pressure_path(.715)
        self.u_limit=np.maximum(length(raised[1:]-raised[:-1]),
                                length(self.pos[1:]-self.pos[:-1]))*1.32+.002
        self.v_limit=np.maximum(length(raised[:,1:]-raised[:,:-1]),
                                length(self.pos[:,1:]-self.pos[:,:-1]))*1.36+.001
        self.spray_births=[]
        self.rng=np.random.default_rng(32156+int(front))

    def pressure_path(self,t):
        u,v=self.u,self.v
        start=.075 if self.front else .045
        drive=ramp(start,.704 if self.front else .670,t)
        swell_center=self.center+.034*np.sin(3.2*t+self.seed)+.018*t
        upwind=np.where(u<swell_center,.19 if self.front else .163,.142 if self.front else .088)
        shape=np.exp(-.5*((u-swell_center)/upwind)**2)
        skirt=.20*np.exp(-.5*((u-.47)/.204)**2) if self.front else .025
        height=self.height*self.end_taper*(.012+.98*shape+skirt*(1-shape))
        phase=2.9*t+self.seed
        cap=ramp(.63,.99,v)
        curl=ramp(start+.10,.72 if self.front else .69,t)
        foot=.825+.028*np.sin(u*14.2+self.seed)+.018*np.sin(u*37.3)
        sheet_width=self.end_taper*(.055+.945*shape**.41)
        fray=(.030*np.sin(u*64+phase+.6*np.sin(u*39))+.018*np.sin(u*137-self.seed))
        waterline=fray*self.end_taper*(1-ramp(.08,.53,v))
        r=foot+(self.r0-foot)*sheet_width+waterline
        # Off-bank ends return to a narrow, irregular run of ground water.
        phase_u=u-(.225 if self.front else .14)*np.sin(np.pi*u)
        phase_u+=(.060 if self.front else .026)*np.tanh((u-self.center)/.090)*self.end_taper
        theta=self.start+self.span*phase_u+.123*ramp(0,.73,t)
        nx,nz=np.cos(theta),np.sin(theta)
        lateral=-1. if self.front else 1.
        reach=self.section[:,:,0]
        h=self.section[:,:,1]
        depth=self.section[:,:,2]
        base_radius=.892+.013*np.sin(13*u+self.seed)
        water_x=base_radius*nx+lateral*reach*(.86+.14*self.tongues*cap)
        water_z=base_radius*nz+lateral*depth
        # The front feeder goes around the pool for a longer section than
        # the steeper rear one. At the crest it moves inward by a smaller
        # distance, which keeps the nearer wall coherent at the game angle.
        spring=shape**.75*curl*self.end_taper
        x=(1-spring)*r*nx+spring*water_x
        z=(1-spring)*r*nz+spring*water_z
        y=.008+drive*height*((1-curl)*v**1.42+curl*h)
        y+=drive*height*(.029*self.slow_fingers+.077*self.lobes*cap)*v**2.25
        x+=lateral*spring*(.032*self.lobes*cap+.021*self.slow_fingers*cap**2)
        z+=spring*(.021*self.lobes*cap+.013*self.slow_fingers*cap**2)
        return vec(x,y,z)

    def step(self,t,frame):
        u,v=self.u,self.v
        p,vel=self.pos,self.vel
        target=self.pressure_path(t)
        transport=(target-self.previous_drive)/DT
        self.previous_drive=target
        # Offshore pressure and orbital velocity only support the rising
        # water.  The free lip loses support first and falls independently.
        launch_time=(.744 if self.front else .703)+.032*self.lobes+.044*(1-v)
        free=ramp(launch_time,launch_time+.10,t)
        stiffness=(185.+95.*(1-v))*(1-free)
        acceleration=(target-p)*stiffness[...,None]+(transport-vel)*(1.64*np.sqrt(stiffness))[...,None]
        acceleration[:,:,1]-=.15+7.9*free
        acceleration+=31.*laplace(p)+5.2*laplace(vel)
        radial=unit(p[:,:,[0,2]])
        # Swash released by the collapsing wall spreads over the stone bed.
        push=(.23+1.6*v)*free*(1-ramp(.93,1.29,t))
        acceleration[:,:,0]+=radial[:,:,0]*push
        acceleration[:,:,2]+=radial[:,:,1]*push
        acceleration[:,:,0]-=radial[:,:,1]*.68*free
        acceleration[:,:,2]+=radial[:,:,0]*.68*free
        incoming=vel[:,:,1].copy()
        vel+=DT*acceleration
        old=p.copy()
        p+=DT*vel
        # One PBD tension projection on overloaded longitudinal and lateral
        # neighbors prevents a plume that is leaving the reef from tearing.
        for first,second,limit in ((p[:-1],p[1:],self.u_limit),
                                   (p[:,:-1],p[:,1:],self.v_limit)):
            delta=second-first
            span=length(delta)
            corr=delta*(.42*np.maximum(0.,span-limit)/np.maximum(span,1e-8))[...,None]
            first+=corr
            second-=corr
        floor=.004+.003*v
        impact=(p[:,:,1]<floor)&(vel[:,:,1]<0)
        p[:,:,1]=np.maximum(floor,p[:,:,1])
        vel[:]=(p-old)/DT
        # Convert downward impact momentum to outward spill, then damp bed
        # drag. Only the crossing frame receives the large impact impulse.
        kick=np.minimum(.8,np.maximum(0.,-incoming-.14))*.36*impact
        damp=np.where(impact,.935,.998)
        vel[:,:,0]=(vel[:,:,0]+radial[:,:,0]*kick)*damp
        vel[:,:,2]=(vel[:,:,2]+radial[:,:,1]*kick)*damp
        normal,area,tangent=jacobian(p)
        turns=length(np.gradient(tangent,axis=1))
        squeeze=np.clip((self.area0/(area+1e-8)-.78)*1.35,0,1)
        lip=ramp(.71,.97,v)
        rough=np.clip(.59+.31*self.lobes+.26*self.slow_fingers,0,1)
        cresting=ramp(.12,.37,p[:,:,1])*(.60*ramp(.11,.46,turns)+lip*.78)
        white=(cresting*(.47+.68*rough)+.24*squeeze*lip)*self.end_taper
        white+=ramp(.08,.32,p[:,:,1])*(.49*self.pore_rim+.21*self.tongues*lip)
        white+=np.clip(-incoming*.8,0,1)*impact*rough
        self.prev_foam=np.maximum(self.prev_foam*np.exp(-DT*6.7),np.clip(white,0,1))
        rim=p[self.jet_index,-1]
        lip=unit(rim-p[self.jet_index,-3])
        self.history.append((rim.copy(),vel[self.jet_index,-1].copy(),lip))
        if len(self.history)>32:
            self.history.pop(0)
        if frame%3==0 and .22<t<1.16:
            self.release_drops(t,impact,incoming)
        return normal,area

    def release_drops(self,t,impact,incoming):
        cap_ids=np.arange(NU)
        j=np.clip(NV-1-self.rng.integers(0,4,NU),0,NV-1)
        p=self.pos[cap_ids,j]; vel=self.vel[cap_ids,j]; f=self.prev_foam[cap_ids,j]
        spilling=impact[cap_ids,j] & (incoming[cap_ids,j]<-.28)
        eligible=(f>.22)&((p[:,1]>.115)|spilling)
        chance=(.13+.17*f+.23*spilling)*(.37+.84*self.tongues[:,0]+.26*np.maximum(self.lobes[:,0],0))
        picked=np.flatnonzero(eligible&(self.rng.random(NU)<chance))
        for i in picked:
            here=p[i].copy()
            radial=here[[0,2]]/max(.1,np.linalg.norm(here[[0,2]]))
            side=self.rng.normal(.16+.51*self.tongues[i,0],.21)
            out=self.rng.normal(.22,.18)
            speed=vel[i]*.65+np.float32([radial[0]*out-radial[1]*side,
                                         self.rng.uniform(.24,.88),radial[1]*out+radial[0]*side])
            radius=self.rng.uniform(.0014,.0046)*(1+.4*spilling[i])
            self.spray_births.append((t,*here,*speed,radius,self.rng.uniform(.45,.96)))
            # A breaking jet entrains an actual little fan of fine water:
            # the droplets inherit their common root and bulk velocity,
            # then separate through different launch angles and drag.
            if spilling[i] or (self.tongues[i,0]>.36 and self.rng.random()<.29):
                for n in range(self.rng.integers(2,6)):
                    offset=self.rng.normal(0.,(.005,.003,.005),3)
                    velocity=speed+self.rng.normal(0.,(.17,.24,.17),3)
                    if spilling[i]:
                        velocity[1]=abs(velocity[1])+.09
                    self.spray_births.append((t,*(here+offset),*velocity,radius*self.rng.uniform(.44,.85),
                                              self.rng.uniform(.41,.84)))

    def mesh(self,normal,area,t):
        # Each column has a constant material volume. Its thickness decreases
        # as the Jacobian grows. Near the bed the swash drains into the pool.
        thick=np.clip(self.material_volume/(area+1e-8),.0015,.049)
        active=ramp(.035,.34,t)*(1-ramp(1.05,1.51,t))
        thick*=.08+.92*active*ramp(.006,.083,self.pos[:,:,1])
        thick*=.025+.975*self.end_taper
        # Surface-volume reconstruction stops at the free meniscus. Blend
        # continuously to a sub-pixel edge around air pockets and the lip.
        thick=.00065+(thick-.00065)*self.edge_gauge
        mid=self.pos.copy()
        for i,j,region,pull in self.collars:
            mid[region]+=(self.pos[i,j]-self.pos[region])*pull
        outer=mid+normal*thick[:,:,None]*.54
        inner=mid-normal*thick[:,:,None]*.46
        outer[:,:,1]=np.maximum(.0008,outer[:,:,1])
        inner[:,:,1]=np.maximum(.00045,inner[:,:,1])
        vertices=np.concatenate([outer.reshape(-1,3),inner.reshape(-1,3)])
        f=self.prev_foam*active
        foam=np.concatenate([f.ravel(),.79*f.ravel()])
        jets,entrained=self.filaments(t)
        return (np.ascontiguousarray(np.vstack([vertices,jets]),dtype=np.float32),
                np.ascontiguousarray(np.concatenate([foam,entrained]),dtype=np.float32))

    def filaments(self,t):
        # Consecutive liquid jets leave the actual moving lip, with a spread
        # in pressure and a limited lifetime. Build each from its historical
        # birth positions, not a static fin stretched from the sheet.
        s=np.linspace(0,1,RINGS,dtype=np.float32)
        phi=np.arange(SIDES)*2*np.pi/SIDES
        nodes=[]
        foams=[]
        for j,(_,life,width,curl) in enumerate(FILAMENTS):
            i=self.jet_index[j]
            root=self.pos[i,-1]
            energetic=ramp(.17,.49,t)*(1-ramp(.83,1.22,t))*ramp(.015,.17,root[1])
            h=np.asarray(self.history,dtype=np.float32)
            if not len(h):
                spine=np.broadcast_to(root,(RINGS,3)).copy()
                spine[:,1]+=s*.0001
            else:
                age=s*life*energetic
                lookup=np.clip(len(h)-1-age*RATE,0,len(h)-1)
                low=lookup.astype(int)
                high=np.minimum(low+1,len(h)-1)
                frac=(lookup-low)[:,None,None]
                values=h[low,: ,j]*(1-frac)+h[high,: ,j]*frac
                head,velocity,local=values[:,0],values[:,1],values[:,2]
                tangent=unit(vec(-head[:,2],0,head[:,0]))
                radial=unit(vec(head[:,0],0,head[:,2]))
                speed=.55*velocity+.87*local+.51*tangent-.13*radial*curl
                speed[:,1]+=.28+.13*curl
                spine=head+speed*age[:,None]
                spine[:,1]-=5.8*age**2
                spine+=(-.10*radial*curl*life)*(s*s*energetic)[:,None]
                spine[0]-=.0025*energetic*local[0]
            axis=unit(np.gradient(spine,axis=0))
            side=unit(np.cross(axis,np.array([0,1.,.08])))
            up=np.cross(axis,side)
            radius=(.00004+width*energetic*(1-.92*s)**1.27*(1+.12*np.sin(s*9.+t*5)))
            section=side[:,None,:]*np.cos(phi)[None,:,None]+up[:,None,:]*np.sin(phi)[None,:,None]*.55
            rings=spine[:,None,:]+radius[:,None,None]*section
            nodes.append(np.vstack([rings.reshape(-1,3),spine[0],spine[-1]+axis[-1]*radius[-1]]))
            f=energetic*(.48+.35*np.sin(phi[None,:]+2*s[:,None]+t*5)**2+.16*s[:,None])
            foams.extend(np.concatenate([f.ravel(),[f[0,0],f[-1,0]]]))
        return np.vstack(nodes),np.asarray(foams)


def bake():
    states=[]
    sprays=[]
    for front in (False,True):
        w=Breaker(front)
        n,a,_=jacobian(w.pos)
        vertices,foam=w.mesh(n,a,0.)
        nkeys=round(END*STORAGE_RATE)+1
        verts=np.empty((nkeys,)+vertices.shape,np.float32)
        whites=np.empty((nkeys,)+foam.shape,np.float32)
        verts[0],whites[0]=vertices,foam
        stride=RATE//STORAGE_RATE
        for frame in range(1,(nkeys-1)*stride+1):
            t=frame/RATE
            normal,area=w.step(t,frame)
            if frame%stride==0:
                verts[frame//stride],whites[frame//stride]=w.mesh(normal,area,t)
        states.append((verts,whites))
        sprays.extend(w.spray_births)
    return states,np.asarray(sprays,np.float32).reshape(-1,9)


_saved=None
_lock=Lock()


def state():
    global _saved
    if _saved is None:
        with _lock:
            if _saved is None:
                _saved=bake()
    return _saved


def sample(t):
    states,_=state()
    pos=np.clip(float(t),0,END)*STORAGE_RATE
    i=min(int(pos),len(states[0][0])-2)
    k=np.float32(pos-i)
    return tuple((np.ascontiguousarray((1-k)*v[i]+k*v[i+1]),*topology(bool(layer)),
                  np.ascontiguousarray((1-k)*f[i]+k*f[i+1]))
                 for layer,(v,f) in enumerate(states))


def _airborne(t):
    _,all_drops=state()
    age=t-all_drops[:,0]
    keep=(age>=0)&(age<.48)
    drops=all_drops[keep]
    age=age[keep]
    drag=2.3
    factor=-np.expm1(-drag*age)/drag
    pos=drops[:,1:4]+drops[:,4:7]*factor[:,None]
    pos[:,1]-=5.8*(age-factor)/drag
    a=drops[:,8]*ramp(0,.018,age)*(1-ramp(.18,.45,age))*(1-ramp(1.23,1.49,t))
    ok=(pos[:,1]>.004)&(a>.012)
    velocity=drops[:,4:7]*np.exp(-drag*age[:,None])
    velocity[:,1]-=5.8*factor
    return (np.ascontiguousarray(np.column_stack([pos[ok],drops[ok,7],a[ok]]),dtype=np.float32),
            np.ascontiguousarray(velocity[ok],dtype=np.float32))


def spray(t):
    return _airborne(t)[0]


def spray_motion(t):
    """World velocities of the exact same surviving droplets, in spray() order."""
    return _airborne(t)[1]
