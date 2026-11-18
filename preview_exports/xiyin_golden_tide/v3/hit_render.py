"""Composite a short refracting impact shell on both sides of an individual unit."""
from functools import lru_cache
from pathlib import Path
import math
import sys

import cv2
import numpy as np
from numba import njit

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/"v2"))
import water_render as water
import hit_model
import center_flow


@lru_cache(maxsize=8)
def _field_geometry(t,front,cx,cy,radius,tilt,height,width):
    """Both color and occlusion are evaluated from this very same inward-flow geometry."""
    vertices,faces,uv,foam=center_flow.sample(t)[1 if front else 0]
    if not len(faces):
        return None
    co=math.sqrt(1-tilt**2)
    projected=np.empty_like(vertices)
    projected[:,0]=cx+radius*vertices[:,0]
    projected[:,1]=cy+radius*(tilt*vertices[:,2]-co*vertices[:,1])
    projected[:,2]=tilt*vertices[:,1]+co*vertices[:,2]
    x0,y0=np.maximum([0,0],np.floor(projected[:,:2].min(0)-3).astype(int))
    x1,y1=np.minimum([width,height],np.ceil(projected[:,:2].max(0)+4).astype(int))
    if x1<=x0 or y1<=y0:
        return None
    projected[:,0]-=x0;projected[:,1]-=y0
    normals,tangents=water._attributes(vertices,faces,uv)
    buffers=water._trace(projected,faces,normals,tangents,uv,foam,int(x1-x0),int(y1-y0))
    return (int(x0),int(y0),int(x1),int(y1)),buffers


def global_front_depth(cam,t,shape):
    """Return the z-buffer of the inward-moving foreground wave, in field-radius units."""
    result=np.full(shape[:2],-100.,np.float32)
    if 0<t<1.67:
        cached=_field_geometry(t,True,cam.cx,cam.cy,cam.radius,cam.flatten,*shape[:2])
        if cached is not None:
            (x0,y0,x1,y1),buffers=cached
            result[y0:y1,x0:x1]=buffers[0]
    return result


def draw_field_floor(scene,cam,t):
    """Reflect and ground the two rollers at their current inward-moving footprints."""
    if not .035<t<1.54:
        return scene
    co=math.sqrt(1-cam.flatten**2);h,w=scene.shape[:2]
    dims=((h+3)//4,(w+3)//4)
    reflected=np.zeros(dims,np.float32)
    shadow=np.zeros_like(reflected)
    for pos,_,_,foam in center_flow.sample(t):
        x,y,z=pos.T
        lift=np.clip((y-.001)*9,0,1)
        for dst,px,py,energy in (
                (reflected,cam.cx+cam.radius*x,cam.cy+cam.radius*(cam.flatten*z+co*y*.78),
                 (.18+foam**1.6)*(.14+.83*lift)),
                (shadow,cam.cx+cam.radius*(x+.19*y),cam.cy+cam.radius*cam.flatten*(z+.26*y),
                 (.09+.35*lift)*np.exp(-.85*y))):
            ix=np.rint(px*.25).astype(np.int32);iy=np.rint(py*.25).astype(np.int32)
            ok=(ix>=0)&(ix<dims[1])&(iy>=0)&(iy<dims[0])
            np.add.at(dst,(iy[ok],ix[ok]),energy[ok])
    rc=cv2.resize(1-np.exp(-cv2.GaussianBlur(reflected,(0,0),2.7)*.93),(w,h))
    sc=cv2.resize(1-np.exp(-cv2.GaussianBlur(shadow,(0,0),3.8)*.48),(w,h))
    iy,ix=np.ogrid[:h,:w]
    rad=((ix-cam.cx)/cam.radius)**2+((iy-cam.cy)/(cam.radius*cam.flatten))**2
    wet=np.clip((1.72-rad)/.55,0,1)*np.clip((rad-.07)/.12,0,1)
    base=scene.astype(np.float32)
    rough=base[:,:,0]-cv2.GaussianBlur(base[:,:,0],(0,0),3.)
    glitter=np.clip(.46+rough*.054,.08,1.)**1.9
    env=water._ramp(.035,.25,t)*(1-water._ramp(1.02,1.52,t))
    scene[:]=np.uint8(np.clip(base*(1-(sc*.17*env)[:,:,None])+
                     (env*rc*wet*(.18+.83*glitter))[:,:,None]*[99,63,22],0,255))
    return scene


def draw_field(scene,cam,t,layer):
    """Shade one simulated tide wall using the existing physically shaded water material."""
    if not 0<t<1.67:
        return scene
    if layer not in ("rear","front"):
        raise ValueError(f"Unknown wave layer {layer}")
    front=layer=="front"
    cached=_field_geometry(t,front,cam.cx,cam.cy,cam.radius,cam.flatten,*scene.shape[:2])
    if cached is None:
        return scene
    (x0,y0,x1,y1),(near,far,thick,attr)=cached
    background=scene.copy()
    water._shade(background,scene,x0,y0,cam.cx,cam.cy,cam.radius,cam.flatten,t,near,far,thick,
                 attr,water._maps(),*water._color_tables())
    env=water._ramp(.015,.17,t)*(1-water._ramp(1.22,1.65,t))
    if env<.999:
        area=scene[y0:y1,x0:x1]
        cv2.addWeighted(area,float(env),background[y0:y1,x0:x1],1.-float(env),0,dst=area)
    pts=center_flow.spray(t)
    if len(pts):
        pick=pts[:,2]>=0 if front else pts[:,2]<0
        vel=center_flow.spray_motion(t)
        den=water._spray(scene,cam.cx,cam.cy,cam.radius,cam.flatten,pts[pick],vel[pick],near,x0,y0)
        mist=cv2.resize(np.clip((cv2.GaussianBlur(den,(0,0),1.95)-.024)*.55,0,.19),
                        (scene.shape[1],scene.shape[0]))
        scene[:]=np.uint8(np.clip(scene+(238.-scene)*mist[:,:,None],0,255))
    return scene


@njit(nogil=True)
def _trace_slice(projected, faces, normals, tangent, uv, foam, bbox, cut_y, tilt, scale, front):
    x0,y0,x1,y1=bbox
    h,w=y1-y0,x1-x0
    near=np.full((h,w),-5.,np.float32)
    path=np.zeros((h,w),np.float32)
    material=np.zeros((h,w,9),np.float32)
    for i,j,k in faces:
        a=projected[i];b=projected[j];c=projected[k]
        ex,ey=b[0]-a[0],b[1]-a[1]
        fx,fy=c[0]-a[0],c[1]-a[1]
        det=ex*fy-ey*fx
        if abs(det)<1.e-7:
            continue
        xmin=max(x0,math.ceil(min(a[0],b[0],c[0])-.5))
        xmax=min(x1-1,math.floor(max(a[0],b[0],c[0])-.5))
        ymin=max(y0,math.ceil(min(a[1],b[1],c[1])-.5))
        ymax=min(y1-1,math.floor(max(a[1],b[1],c[1])-.5))
        signed=-1. if det>0 else 1.
        inv=1./det
        for py in range(ymin,ymax+1):
            dy=py+.5-a[1]
            # In local viewer-depth coordinates, this is z=0 (the victim's sprite).
            plane=-(py+.5-cut_y)*tilt/(scale*math.sqrt(1-tilt*tilt))
            for px in range(xmin,xmax+1):
                dx=px+.5-a[0]
                wb=(dx*fy-dy*fx)*inv
                wc=(dy*ex-dx*ey)*inv
                wa=1-wb-wc
                if min(wa,wb,wc)<0:
                    continue
                dep=wa*a[2]+wb*b[2]+wc*c[2]
                delta=dep-plane
                if (delta>=0) != front:
                    continue
                y=py-y0;x=px-x0
                # Only the portion of ray inside the chosen halfspace absorbs light.
                path[y,x]+=delta*signed
                if dep<=near[y,x]:
                    continue
                near[y,x]=dep
                for axis in range(3):
                    material[y,x,axis]=wa*normals[i,axis]+wb*normals[j,axis]+wc*normals[k,axis]
                    material[y,x,axis+3]=wa*tangent[i,axis]+wb*tangent[j,axis]+wc*tangent[k,axis]
                material[y,x,6]=wa*uv[i,0]+wb*uv[j,0]+wc*uv[k,0]
                material[y,x,7]=wa*uv[i,1]+wb*uv[j,1]+wc*uv[k,1]
                material[y,x,8]=wa*foam[i]+wb*foam[j]+wc*foam[k]
    return near,path,material


@njit(nogil=True)
def _shade_shell(source,scene,near,path,material,origin,tilt,size,body_h,cx,foot_y,sign,age,seed,tex,dec,enc,tide,depth0,depth_scale,second):
    ox,oy=origin; h,w=near.shape
    sh,sw=scene.shape[:2]
    co=math.sqrt(1-tilt*tilt)
    envelope=water._ramp(.001,.021,age)*(1-water._ramp(.45,.627,age))
    contact=1-water._ramp(.025,.15,age)
    for y in range(h):
        sy=oy+y
        for x in range(w):
            dep=near[y,x]
            if dep< -4.:
                continue
            sx=ox+x
            if tide.shape[0]>0:
                main=tide[sy,sx]
                in_front=main>-20. and dep*depth_scale+depth0>main
                if second != in_front:
                    continue
            elif second:
                continue
            m=material[y,x]
            u,v,gas=m[6],m[7],m[8]
            coarse=water._tex(tex,u*.74+age*.14+.23*seed,v*.46+.33*seed,0)
            ripple=water._tex(tex,u*1.58+v*.09+age*.21,v*.65-u*.31+.18*seed,1)
            foam=water._tex(tex,u*1.17+v*.18+age*.18,v*.62-u*.25+.39*seed,1)
            nx,ny,nz=m[0],m[1],m[2]
            inv=1/max(1.e-8,math.sqrt(nx*nx+ny*ny+nz*nz))
            nx,ny,nz=nx*inv,ny*inv,nz*inv
            tx,ty,tz=m[3],m[4],m[5]
            d=tx*nx+ty*ny+tz*nz
            tx,ty,tz=tx-d*nx,ty-d*ny,tz-d*nz
            inv=1/max(1.e-8,math.sqrt(tx*tx+ty*ty+tz*tz))
            tx,ty,tz=tx*inv,ty*inv,tz*inv
            bx,by,bz=ny*tz-nz*ty,nz*tx-nx*tz,nx*ty-ny*tx
            du=.56*ripple[0]+.31*coarse[0]
            dv=.48*ripple[1]+.32*coarse[1]
            nx,ny,nz=nx+tx*du+bx*dv,ny+ty*du+by*dv,nz+tz*du+bz*dv
            inv=1/max(1.e-8,math.sqrt(nx*nx+ny*ny+nz*nz))
            nx,ny,nz=nx*inv,ny*inv,nz*inv
            incidence=ny*tilt+nz*co
            if incidence<0:
                nx,ny,nz,incidence=-nx,-ny,-nz,-incidence
            fresnel=.026+.974*(1-min(1.,incidence))**5
            ray=min(.46,abs(path[y,x]))
            bend=size*(.012+.62*ray)*(.33+.67*incidence)
            ex=min(sw-1,max(0.,sx-bend*nx))
            ey=min(sh-1,max(0.,sy+bend*(ny*co-nz*tilt)))
            ix,iy=int(ex),int(ey)
            jx,jy=min(sw-1,ix+1),min(sh-1,iy+1)
            fx,fy=ex-ix,ey-iy
            rx,ry,rz=2*incidence*nx,2*incidence*ny-tilt,2*incidence*nz-co
            slit=math.exp(-((ry-.51+.24*rx)/.079)**2)
            pearl=max(0.,-.49*rx+.74*ry-.46*rz)
            # A crest is pearly only where refraction and entrained foam agree.
            # The travelling slip of light runs towards earlier, leading parcels.
            traveling=(.5+.5*math.cos(6.5*u+age*17.5+seed*.53))**5
            lace=water._clamp(1.24*foam[2]+.32*ripple[2])
            flank=abs(2*v-1)
            turbulent=water._ramp(.44,.91,gas+.25*(coarse[3]-.5))
            edge=water._ramp(.42,.96,flank)
            wetted_side=1.-water._ramp(.045,.19,age)
            world_x=(sx-cx)*sign/size
            world_y=(dep*tilt*size-(sy-foot_y)*co)/body_h
            against_body=math.exp(-((world_x+.42)/.215)**2-((world_y-.47)/.22)**2)
            compression=against_body*water._ramp(.003,.028,age)*(1-water._ramp(.065,.168,age))
            gaspatch=water._ramp(.19,.74,foam[3]+.18*ripple[3]-.10)
            bubbles=water._clamp(turbulent*gaspatch*(.015+.87*water._ramp(.23,.77,lace))
                                *(.20+.75*edge+.57*wetted_side)+.20*edge*water._ramp(.48,.86,lace))
            # The physically compressed jet has very different normals and effective
            # roughness on its two wet shoulders. Reflected streaks are narrow, mobile
            # and broken, leaving most of the body transparent over the dark armour.
            ridge_v=.69+.14*math.sin(5.8*u+age*9.8+seed*.81)+.13*(coarse[3]-.50)
            lead_ridge=math.exp(-((v-ridge_v)/(.058+.049*gas))**2)
            low_ridge=math.exp(-((v-(.21+.10*math.sin(u*9.-age*12.+seed)))/.074)**2)
            lifting=(.22+.78*water._ramp(.14,.72,lace+.48*foam[3]))*(.34+.66*traveling)
            wet_edge=(lead_ridge+.44*low_ridge)*lifting*(1-water._ramp(.34,.55,age))
            impact_scud=compression*water._ramp(.23,.73,lace+.42*coarse[3])
            bubbles=water._clamp(bubbles+.42*wet_edge*turbulent+.93*impact_scud)
            gentle=water._ramp(.075,.67,v)*water._ramp(.02,.28,1-v)
            shaft=gentle*traveling
            endcap=water._ramp(.004,.105+.070*foam[3],u)*water._ramp(.008,.18+.12*ripple[3],1-u)
            sidecover=water._ramp(.024,.12+.10*foam[3],.5-.5*flank)
            edgecoverage=.26+.74*max(sidecover,water._ramp(.28,.80,lace))
            baseopacity=envelope*(.49+.51*water._ramp(.003,.064,ray))
            baseopacity*=edgecoverage*(.18+.82*endcap)
            # Resolving the fall into a thinning lace field prevents an opaque band at the feet.
            runoff=water._ramp(.34,.57,age)
            cover=baseopacity*(1-runoff*(1-.69*bubbles)*.54)
            for c in range(3):
                atop=dec[source[iy,ix,c]]*(1-fx)+dec[source[iy,jx,c]]*fx
                abot=dec[source[jy,ix,c]]*(1-fx)+dec[source[jy,jx,c]]*fx
                bg=atop*(1-fy)+abot*fy
                trans=math.exp(-ray*(3.5,9.2,19.)[c])
                pigment=(1.35,.30,.067)[c]*(1-trans)*(.57+.64*coarse[3]+.38*shaft)
                focused=shaft*(.054+.51*max(0.,ripple[4]-.13)**1.6)*(1.,.73,.47)[c]
                reflection=(.036+.62*pearl**25+4.6*pearl**98
                            +slit*(.62+.75*traveling))*(1.,.88,.69)[c]
                wet=(bg*trans+pigment+focused)*(1-fresnel)+reflection*fresnel
                wet+=(.025+.065*contact)*(.52+.48*coarse[3])*(.83,.75,.55)[c]
                wet+=wet_edge*(1.19,1.26,1.27)[c]
                wet+=compression*(.16+.59*max(0.,ripple[4]-.14))*(1.02,1.03,.89)[c]
                scud=(.66+1.33*lace+.30*traveling+.36*contact)*(1.,.986,.937)[c]
                lit=wet*(1-bubbles)+scud*bubbles
                out=enc[min(65535,int(max(0.,lit)*6553.5))]
                dry=float(source[sy,sx,c])
                scene[sy,sx,c]=round(dry+cover*(float(out)-dry))


@lru_cache(maxsize=20)
def _shell_cache(age,seed,cx,foot_y,bw,bh,sign,front,tilt,height,width):
    (verts,faces,uv,foam),drops=hit_model.sample_hit(age,seed)
    if not len(faces):
        return None,drops
    pos=verts.copy()
    pos[:,0]*=sign
    pos[:,1]*=bh/bw
    co=math.sqrt(1-tilt*tilt)
    proj=np.empty_like(pos)
    proj[:,0]=cx+bw*pos[:,0]
    proj[:,1]=foot_y+bw*(tilt*pos[:,2]-co*pos[:,1])
    proj[:,2]=tilt*pos[:,1]+co*pos[:,2]
    lo=np.maximum((0,0),np.floor(proj[:,:2].min(0)-1).astype(int))
    hi=np.minimum((width,height),np.ceil(proj[:,:2].max(0)+2).astype(int))
    if np.any(hi<=lo):
        return None,drops
    normals,tangent=water._attributes(pos,faces,uv)
    bounds=tuple(int(n) for n in (*lo,*hi))
    hit=_trace_slice(proj,faces,normals,tangent,uv,foam,bounds,foot_y,tilt,bw,front)
    return (hit,bounds[:2]),drops


@njit(nogil=True)
def _whitewater(scene,drops,cx,foot_y,bw,bh,tilt,sign,front,tide,depth0,depth_scale,second,age):
    co=math.sqrt(1-tilt*tilt);h,w=scene.shape[:2]
    for px,py,pz,radius,energy in drops:
        if (pz>=0)!=front:
            continue
        sx=cx+sign*bw*px
        sy=foot_y+tilt*bw*pz-co*bh*py
        ix,iy=int(sx+.5),int(sy+.5)
        if energy<.009 or not (0<=ix<w and 0<=iy<h):
            continue
        if tide.shape[0]:
            main=tide[iy,ix]
            dep=co*pz+tilt*py*(bh/bw)
            after=main>-20 and dep*depth_scale+depth0>main
            if after!=second:
                continue
        elif second:
            continue
        rad=max(.37,min(4.1,bw*radius))
        xl=max(0,int(sx-2*rad-1));xh=min(w,math.ceil(sx+2*rad+2))
        yl=max(0,int(sy-2.6*rad-1));yh=min(h,math.ceil(sy+2.6*rad+2))
        for y in range(yl,yh):
            for x in range(xl,xh):
                u=(x-sx)/(rad+.38);v=(y-sy)/(rad*1.22+.38)
                a=math.exp(-1.93*(u*u+v*v))*energy
                shine=math.exp(-5.1*((u+.24)**2+(v+.20)**2))*energy
                for c in range(3):
                    old=float(scene[y,x,c])
                    value=old*(1-a*.53)+a*(137,130,120)[c]+shine*(107,106,100)[c]
                    scene[y,x,c]=min(255,round(value))


_EMPTY_DEPTH=np.empty((0,0),np.float32)


def draw_hit(scene,cam,age,seed,cx,foot_y,body_w,body_h,sign,layer,front_depth=None,after_world=False):
    """Render one target-local shell half, optionally split against the global front tide."""
    if not 0.<age<hit_model.END or min(body_w,body_h)<1.:
        return scene
    if layer not in ("front","back"):
        raise ValueError(f"Unknown impact layer {layer}")
    front=layer=="front"
    tilt=float(cam.flatten); bw=float(body_w); bh=float(body_h)
    sign=1 if sign>=0 else -1
    cache,drops=_shell_cache(float(age),int(seed),float(cx),float(foot_y),bw,bh,sign,
                             front,tilt,*scene.shape[:2])
    # The back shell must be laid in before its sprite. The foreground shell alone
    # straddles both composition slots of the global near breaker.
    depth=_EMPTY_DEPTH if front_depth is None or not front else front_depth
    co=math.sqrt(1-tilt**2)
    base_depth=(foot_y-cam.cy)/cam.radius*co/max(.001,tilt)
    scale=bw/cam.radius
    if cache is not None:
        (near,path,mat),origin=cache
        _shade_shell(scene.copy(),scene,near,path,mat,origin,tilt,bw,bh,cx,foot_y,sign,age,int(seed),water._maps(),
                     *water._color_tables(),depth,base_depth,scale,after_world)
    _whitewater(scene,drops,cx,foot_y,bw,bh,tilt,sign,front,depth,base_depth,scale,after_world,age)
    return scene
