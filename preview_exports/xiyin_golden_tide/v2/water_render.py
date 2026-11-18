"""Orthographic refraction, foam and light transport on the simulated water shells."""
from functools import lru_cache
from pathlib import Path
import math

import cv2
import numpy as np
from numba import njit
from scipy.ndimage import gaussian_filter, map_coordinates
from PIL import Image

import wave_sim


@njit(nogil=True)
def _clamp(x):
    return max(0., min(1., x))


@njit(nogil=True)
def _ramp(lo, hi, x):
    f = _clamp((x - lo) / (hi - lo))
    return f*f*(3.-2.*f)


@njit(nogil=True)
def _attributes(position, faces, uv):
    # Area-weighted normals. The flow tangent is derived from the material coordinates.
    normals = np.zeros_like(position)
    tangents = np.zeros_like(position)
    for i, j, k in faces:
        ax, ay, az = position[j] - position[i]
        bx, by, bz = position[k] - position[i]
        nx, ny, nz = ay*bz-az*by, az*bx-ax*bz, ax*by-ay*bx
        du1, dv1 = uv[j] - uv[i]
        du2, dv2 = uv[k] - uv[i]
        det = du1*dv2-du2*dv1
        weight = math.sqrt(nx*nx+ny*ny+nz*nz)
        if abs(det) > 1.e-9:
            tx, ty, tz = ax*dv2-bx*dv1, ay*dv2-by*dv1, az*dv2-bz*dv1
            norm = math.sqrt(tx*tx+ty*ty+tz*tz)
            mult = math.copysign(weight / max(1.e-10, norm), det)
            tx, ty, tz = tx*mult, ty*mult, tz*mult
        else:
            tx, ty, tz = 0., 0., 0.
        for idx in (i,j,k):
            normals[idx,0] += nx
            normals[idx,1] += ny
            normals[idx,2] += nz
            tangents[idx,0] += tx
            tangents[idx,1] += ty
            tangents[idx,2] += tz
    for i in range(len(position)):
        for array in (normals,tangents):
            x, y, z = array[i]
            d = max(1.e-10, math.sqrt(x*x+y*y+z*z))
            array[i,0],array[i,1],array[i,2] = x/d,y/d,z/d
    return normals, tangents


@njit(nogil=True)
def _trace(projected, faces, normals, tangents, uv, foam, w, h):
    near = np.full((h,w), -100., np.float32)
    far = np.full((h,w), 100., np.float32)
    water_path = np.zeros((h,w), np.float32)
    surface = np.zeros((h,w,9),np.float32)
    for i,j,k in faces:
        x0,y0,z0 = projected[i]
        x1,y1,z1 = projected[j]
        x2,y2,z2 = projected[k]
        ex,ey,fx,fy = x1-x0,y1-y0,x2-x0,y2-y0
        det = ex*fy-ey*fx
        if abs(det) < 1.e-7:
            continue
        xmin=max(0,math.ceil(min(x0,x1,x2)-.5))
        xmax=min(w-1,math.floor(max(x0,x1,x2)-.5))
        ymin=max(0,math.ceil(min(y0,y1,y2)-.5))
        ymax=min(h-1,math.floor(max(y0,y1,y2)-.5))
        turn = -1. if det>0. else 1.
        inv_det = 1./det
        for y in range(ymin,ymax+1):
            dy = y+.5-y0
            for x in range(xmin,xmax+1):
                dx=x+.5-x0
                b=(dx*fy - dy*fx)*inv_det
                c=(dy*ex - dx*ey)*inv_det
                a=1.-b-c
                if min(a,b,c)<0.:
                    continue
                depth=a*z0+b*z1+c*z2
                # Sum front/back crossings. A curled lip can make several disjoint ray intervals.
                water_path[y,x]+=turn*depth
                far[y,x]=min(far[y,x],depth)
                if depth <=near[y,x]:
                    continue
                near[y,x]=depth
                for n in range(3):
                    surface[y,x,n]=a*normals[i,n]+b*normals[j,n]+c*normals[k,n]
                    surface[y,x,n+3]=a*tangents[i,n]+b*tangents[j,n]+c*tangents[k,n]
                surface[y,x,6]=a*uv[i,0]+b*uv[j,0]+c*uv[k,0]
                surface[y,x,7]=a*uv[i,1]+b*uv[j,1]+c*uv[k,1]
                surface[y,x,8]=a*foam[i]+b*foam[j]+c*foam[k]
    return near,far,water_path,surface


@lru_cache(maxsize=1)
def _maps():
    # Four repeatable material channels: tangent slopes, foam membranes and pore density.
    # The normal map is the existing user-authored project water material, with no wave image.
    path = (Path(__file__).resolve().parents[3] / "SteriaBuild/VFXSource/"
            "SlazeyaWaterThunderSlash/source_assets/UserWaterNormal_original.png")
    normal=np.asarray(Image.open(path).convert("RGB"),np.float32)/127.5-1.
    size=512
    normal=cv2.resize(normal,(size,size),interpolation=cv2.INTER_LINEAR)
    y,x=np.indices((size,size),np.float32)
    rng=np.random.default_rng(4987)
    def noise(scale):
        cell=rng.normal(size=(scale,scale)).astype(np.float32)
        return map_coordinates(cell,[y*scale/size,x*scale/size],order=3,mode="grid-wrap")
    # Only gas fraction is sampled from the UV mask. Its holes and tiny shear
    # branches modulate scattering and roughness on the physically moving surface.
    pores_image=Image.open(Path(__file__).resolve().parent/"art/foam-uv.png").convert("L")
    pores_image=pores_image.resize((size,size),Image.Resampling.LANCZOS)
    foam=np.asarray(pores_image,np.float32)/255.
    # Crossfade the opposite edge over a narrow guard strip to avoid a repeated seam.
    for axis in (0,1):
        tileview=foam if axis==0 else foam.T
        blend=np.linspace(0,1,18,dtype=np.float32)[:,None]
        edges=(tileview[0]+tileview[-1])*.5
        tileview[:18]=edges[None,:]*(1-blend)+tileview[:18]*blend
        tileview[-18:]=tileview[-18:]*(1-blend)+edges[None,:]*blend
    foam=np.sqrt(foam)
    pores=np.clip(.48+.21*noise(7)+.16*noise(17)+.10*noise(37)+.06*noise(87),0.,1.)
    # Parallel rays refracted by a small rippling surface produce a Jacobian-density
    # pattern of bright streaks and dark gaps below the liquid, independent of foam.
    ripple=(2.5*noise(7)+.52*noise(21)+.12*noise(52)).astype(np.float32)
    dx=np.roll(ripple,-1,1)-np.roll(ripple,1,1)
    dy=np.roll(ripple,-1,0)-np.roll(ripple,1,0)
    target_x=(x+43.*dx+12.*normal[:,:,0])%size
    target_y=(y+33.*dy+10.*normal[:,:,1])%size
    ix,iy=target_x.astype(int),target_y.astype(int)
    fx,fy=target_x-ix,target_y-iy
    photons=np.zeros((size,size),np.float32)
    for i,j,energy in ((0,0,(1-fx)*(1-fy)),(1,0,fx*(1-fy)),(0,1,(1-fx)*fy),(1,1,fx*fy)):
        np.add.at(photons,((iy+j)%size,(ix+i)%size),energy)
    rays=gaussian_filter(photons,.75,mode="wrap")
    caustics=np.clip((rays-.49)/2.6,0.,1.7)
    mat=np.ascontiguousarray(np.dstack((normal[:,:,0],normal[:,:,1],foam,pores,caustics)),np.float32)
    return np.asarray([mat]+[gaussian_filter(mat,(s,s,0),mode="wrap") for s in (.9,2.4,5.3)],np.float32)


@njit(nogil=True)
def _tex(tile,u,v,lod):
    n=tile.shape[1]
    xx,yy=(u%1.)*n,(v%1.)*n
    i,j=int(xx),int(yy)
    i1,j1=(i+1)%n,(j+1)%n
    fx,fy=xx-i,yy-j
    vals=np.empty(5,np.float32)
    for c in range(5):
        a=tile[lod,j,i,c]*(1-fx)+tile[lod,j,i1,c]*fx
        b=tile[lod,j1,i,c]*(1-fx)+tile[lod,j1,i1,c]*fx
        vals[c]=a+(b-a)*fy
    return vals


@lru_cache(maxsize=1)
def _color_tables():
    # Work in radiance. Background colors round-trip through the film curve at zero extra light.
    channel=np.arange(256,dtype=np.float32)/255.
    decode=-np.log(np.maximum(.0001,1-channel**2.18))
    radiance=np.linspace(0,10,65536)
    encode=np.uint8(np.rint(255*(1-np.exp(-radiance))**(1/2.18)))
    return decode,encode


@njit(nogil=True)
def _shade(source, image, ox, oy, cam_x, cam_y, radius, tilt, time, near, far, path, data, mats, decode, encode):
    h,w=near.shape
    height,width=source.shape[:2]
    co=math.sqrt(1-tilt*tilt)
    for y in range(h):
        for x in range(w):
            if near[y,x]<-10.:
                continue
            g=data[y,x]
            nx,ny,nz=g[0],g[1],g[2]
            u,v=g[6],g[7]
            thickness=min(max(0.,near[y,x]-far[y,x]),abs(path[y,x]))
            thickness=min(.42,thickness)
            dx_uv=0.
            if x+1<w and abs(near[y,x+1]-near[y,x])<.09:
                dx_uv=max(abs(u-data[y,x+1,6]),abs(v-data[y,x+1,7]))
            if y+1<h and abs(near[y+1,x]-near[y,x])<.09:
                dx_uv=max(dx_uv,abs(v-data[y+1,x,7]),abs(u-data[y+1,x,6]))
            foot=dx_uv*512
            lod=0 if foot<1.5 else (1 if foot<3.5 else (2 if foot<7. else 3))
            # Long ripples follow liquid parcels while faster, smaller capillaries cross the flow.
            p=_tex(mats,u*2.75+v*.16+time*.039,v*.77-time*.026,lod)
            q=_tex(mats,u*6.23-v*.44-time*.083,v*2.61+u*.75+time*.05,min(3,lod+1))
            # The larger flow envelope, capillary slopes and foam patches each use their own scale.
            broad=_tex(mats,u*.79+v*.21+.04*time,v*.51+u*.13-.08*time,0)
            lace=_tex(mats,u*3.22+v*.24-.06*time,.88*v-u*.25+.04*time,min(2,lod))
            tx,ty,tz=g[3],g[4],g[5]
            nlen=max(1.e-7, math.sqrt(nx*nx+ny*ny+nz*nz))
            nx,ny,nz=nx/nlen,ny/nlen,nz/nlen
            dot=nx*tx+ny*ty+nz*tz
            tx,ty,tz=tx-nx*dot,ty-ny*dot,tz-nz*dot
            tlen=max(1.e-7,math.sqrt(tx*tx+ty*ty+tz*tz))
            tx,ty,tz=tx/tlen,ty/tlen,tz/tlen
            bx,by,bz=ny*tz-nz*ty,nz*tx-nx*tz,nx*ty-ny*tx
            du=p[0]*.64+q[0]*.33+broad[0]*.37
            dv=p[1]*.58+q[1]*.27+broad[1]*.30
            nx,ny,nz=nx+tx*du+bx*dv,ny+ty*du+by*dv,nz+tz*du+bz*dv
            nlen=max(1.e-7, math.sqrt(nx*nx+ny*ny+nz*nz))
            nx,ny,nz=nx/nlen,ny/nlen,nz/nlen
            view=ny*tilt+nz*co
            if view<0:
                nx,ny,nz,view=-nx,-ny,-nz,-view
            grazing=1-min(view,1.)
            fresnel=.026+.974*grazing**5
            rx,ry,rz=2*view*nx,2*view*ny-tilt,2*view*nz-co
            # The overhead softbox, narrow grazing light and a warm inner sun are reflected
            # by the moving 3-D normals. Their positions are fixed in the stage.
            sky=.012+.069*max(0.,ry)
            oblong=math.exp(-((ry-.51+.22*rx)/.103)**2)*(.06+.94*max(0.,-rz))
            sun=max(0.,-.49*rx+.64*ry-.592*rz)
            pearl=max(0.,.54*rx+.58*ry+.61*rz)
            spec=1.8*sun**19+6.5*sun**135+1.05*pearl**56
            refract_scale=radius*(.014+min(.19,1.21*thickness))*(.25+.69*view)
            sx=min(width-1,max(0.,ox+x-nx*refract_scale))
            sy=min(height-1,max(0.,oy+y-(-ny*co+nz*tilt)*refract_scale))
            ix,iy=int(sx),int(sy)
            ix1,iy1=min(width-1,ix+1),min(height-1,iy+1)
            ax,ay=sx-ix,sy-iy
            rawfoam=_clamp(g[8])
            wy=near[y,x]*tilt-(oy+y-cam_y)*co/radius
            wx=(ox+x-cam_x)/radius
            wz=near[y,x]*co+(oy+y-cam_y)*tilt/radius
            wet=_ramp(.009,.085,wy)
            curl=_ramp(.08,.72,rawfoam)
            foam_seed=.52*broad[3]+.30*p[3]+.18*q[3]
            # Submerged foam, sparse fingers and concentrated lip scud use the same
            # Lagrangian source field. Patches join only at high local gas fraction.
            web=_clamp(.95*lace[2]+.58*q[2])
            gas=rawfoam*.85+.53*(foam_seed-.49)+.15*(lace[3]-.5)
            surface_caps=_ramp(.72,.93,gas)*(.35+.65*_ramp(.28,.72,lace[3]))
            capband=_ramp(.67,.96,v)*_ramp(.14,.73,rawfoam)
            froth=_clamp(_ramp(.16,.72,gas)*(_ramp(.09,.63,web)*.92+.012)
                         +surface_caps+.69*capband*_ramp(.055,.49,lace[2]))
            # The material runs out in broken fingers around the bed and at both ends;
            # screen opacity follows the shallow film and its actual stitched boundaries.
            shore=_ramp(.025,.13+(.55-foam_seed)*.07,v)
            ends=_ramp(.007,.092+(.58-foam_seed)*.07,u)*_ramp(.004,.09+(.53-foam_seed)*.08,1.-u)
            depth_weight=_ramp(.001,.034,thickness)
            contact_film=_ramp(.43,.81,foam_seed)*(.17+.50*web)
            footweb=_ramp(.048,.51,.90*lace[2]+.43*q[2])
            foamfoot=(1.-wet)*footweb*(.26+.74*_ramp(.23,.70,foam_seed))
            coverage=ends*shore*(wet+(1.-wet)*(.115*depth_weight+contact_film+.86*foamfoot))
            coverage=_clamp(coverage)
            froth=_clamp(froth+foamfoot)
            liploss=_ramp(.78,.996,v)*_ramp(.20,.70,rawfoam)*_ramp(.38,.70,p[3])*(1.-.84*web)
            coverage*=1.-liploss
            # Backlit strands are focused along changes in the normal field. Their
            # warm caustics do not turn dark gaps or the entire body into diffuse gold.
            rim_sun=max(0.,-.51*nx+.72*ny+.43*nz)
            side_sun=max(0.,.60*nx+.44*ny-.66*nz)
            sheetglow=wet*(.10+.53*rim_sun**3+.21*side_sun**5)
            # Two banks of low backlight pick up different portions of the curling walls.
            # The intervening volume sees the dim stone environment through the water.
            rear_light=math.exp(-((wx+.54)/.23)**2-((wz+.52)/.34)**2)
            front_light=math.exp(-((wx-.51)/.27)**2-((wz-.59)/.30)**2)
            sunlit=max(rear_light,front_light)
            shaft=.035+.965*sunlit
            along_lip=_ramp(.49,.91,v)*(.27+.73*curl)
            reflection_field=.085+.52*sunlit+.32*along_lip
            slope=.97*p[0]+.91*p[1]+.49*q[1]+.33*broad[1]
            ridge=max(0.,1.-abs(slope-.13)*6.6)**3
            focus=(.24+.76*_ramp(.23,.76,foam_seed))*(.46+.54*curl)
            caustic=shaft*wet*focus*(.85*ridge*max(.12,rim_sun)+.44*ridge*side_sun)
            # Long, interrupted reflections from overhead and shoreline light. A reflected
            # environmental feature moves across the water when a normal tilts, independently
            # of the attached UV foam. Each reflection has a broad lobe and a tight bright centre.
            horizon_d=ry-(.13+.26*rx-.07*rz)
            window_d=rx-(.15-.55*ry+.12*rz)
            slit_d=ry-(.71-.46*rx)
            sky_strips=(3.3*math.exp(-(horizon_d/.11)**2)+14.*math.exp(-(horizon_d/.032)**2)
                        +6.*math.exp(-(window_d/.096)**2)*(.55+.45*rz)
                        +8.*math.exp(-(slit_d/.055)**2))*(.29+.78*_ramp(.19,.84,p[3]))
            # A bottom-up golden beam through the curved wall forms amber caustic fans
            # below the whitewater. Normal slopes break its peak into fine travelling points.
            line_phase=u*48.+v*3.4+4.8*broad[0]+2.8*broad[1]+1.29*p[1]+.54*q[1]
            vertical=.5+.5*math.cos(line_phase)
            focus_bands=shaft*wet*focus*(.25+.75*_ramp(.20,.9,rawfoam+.36*v))*(vertical**9+.55*vertical**32)
            rays=(.69*p[4]+.25*broad[4]+.31*q[4])
            liquid_caustic=(max(0.,rays-.21)**1.62)*shaft*wet*(.55+.78*rim_sun+.24*side_sun)*(.34+.77*v)
            focused=max(0.,rays-.20)**3*wet*shaft*.25
            microburst=_ramp(.28,.76,foam_seed)*(.38+web*.7)
            crestlight= _ramp(.22,.82,rawfoam)*(.12+.52*microburst)*_ramp(.08,.34,wy)
            for ch in range(3):
                top=decode[source[iy,ix,ch]]*(1-ax)+decode[source[iy,ix1,ch]]*ax
                bottom=decode[source[iy1,ix,ch]]*(1-ax)+decode[source[iy1,ix1,ch]]*ax
                background=top*(1-ay)+bottom*ay
                absorption=(3.2,8.7,23.5)[ch]
                tr=math.exp(-thickness*absorption)
                # Deep water retains a dark honey shade; its fine lip reflects champagne sky.
                scattering=(.93,.20,.020)[ch]*(1-tr)*(.42+1.15*shaft*sheetglow+.41*broad[3])
                light=(background*tr+scattering+(sheetglow*(.055+.12*shaft)+caustic)*(1.24,.63,.15)[ch])*(1-fresnel)
                light+=(sky*(.98,1.,1.12)[ch]+reflection_field*(oblong*(2.1,1.70,1.04)[ch]+spec*(4.8,4.1,3.17)[ch]))*fresnel
                light+=sky_strips*fresnel*reflection_field*(1.00,.82,.57)[ch]
                light+=focus_bands*(1.14,.56,.16)[ch] + crestlight*(.80,.47,.18)[ch]
                light+=liquid_caustic*(1.63,.78,.15)[ch]+focused*(1.7,1.4,.85)[ch]
                light+=spec*reflection_field*(.10,.089,.073)[ch]
                foam_light=(.55+1.17*foam_seed+.55*max(0.,ny)+1.03*web)*(1.,.95,.84)[ch]
                light=light*(1-froth*.95)+foam_light*froth
                lit=encode[min(65535,int(max(0.,light)*6553.5))]
                dry=float(source[oy+y,ox+x,ch])
                image[oy+y,ox+x,ch]=round(dry+coverage*(float(lit)-dry))


@njit(nogil=True)
def _spray(scene,cx,cy,radius,tilt,points,velocity,near,x0,y0):
    """Velocity-aligned beads and condensed white spray in overlapping particle plumes."""
    co=math.sqrt(1.-tilt**2)
    h,w=scene.shape[:2]
    density=np.zeros(((h+1)//2,(w+1)//2),np.float32)
    for i in range(len(points)):
        x,y,z,r,alpha=points[i]
        if alpha<.02 or r<=0:
            continue
        sx=cx+radius*x
        sy=cy+radius*(z*tilt-y*co)
        ix,iy=int(sx+.5),int(sy+.5)
        if not 0<=ix<w or not 0<=iy<h:
            continue
        if 0<=iy-y0<near.shape[0] and 0<=ix-x0<near.shape[1] and near[iy-y0,ix-x0]>y*tilt+z*co+.005:
            continue
        vx,vy,vz=velocity[i]
        dx,dy=vx,vz*tilt-vy*co
        speed=math.hypot(dx,dy)
        if speed>1.e-6:
            dx,dy=dx/speed,dy/speed
        else:
            dx,dy=0.,-1.
        size=max(.36,min(4.5,r*radius))
        stretch=1.+min(2.05,speed*.94)
        half_l=size*stretch+.36
        half_w=size/math.sqrt(stretch)+.38
        rad=math.ceil(half_l*1.65+1.)
        density[iy//2,ix//2]+=alpha*min(1.65,size)
        for py in range(max(0,iy-rad),min(h,iy+rad+1)):
            for px in range(max(0,ix-rad),min(w,ix+rad+1)):
                xx,yy=px-sx,py-sy
                u=(xx*dx+yy*dy)/half_l
                v=(-xx*dy+yy*dx)/half_w
                body=math.exp(-1.73*(u*u+v*v))*min(1.,size)*alpha
                glint=math.exp(-6.2*((u-.08)**2+(v+.20)**2))*alpha
                for ch in range(3):
                    original=scene[py,px,ch]
                    reflected=(129.,123.,110.)[ch]*body+(111.,111.,105.)[ch]*glint
                    scene[py,px,ch]=min(255,round(original*(1-body*.56)+reflected))
    return density


def draw_floor_response(scene,cam,t):
    """Project the actual moving crests and whitewater into the wetted, rough ground."""
    if not .035<t<1.52:
        return
    co=math.sqrt(1-cam.flatten**2)
    h,w=scene.shape[:2]
    down=4
    hh,ww=(h+down-1)//down,(w+down-1)//down
    reflected=np.zeros((hh,ww),np.float32)
    contact=np.zeros_like(reflected)
    for verts,_,_,foam in wave_sim.sample(t):
        x,y,z=verts.T
        lift=np.clip((y-.001)*9,0,1)
        def deposit(dst,px,py,value):
            ix=np.rint(px/down).astype(np.int32)
            iy=np.rint(py/down).astype(np.int32)
            ok=(ix>=0)&(ix<ww)&(iy>=0)&(iy<hh)&(value>.002)
            np.add.at(dst,(iy[ok],ix[ok]),value[ok])
        # Virtual image of illuminated particles, blurred by stone roughness.
        deposit(reflected,cam.cx+cam.radius*x,cam.cy+cam.radius*(cam.flatten*z+co*y*.78),
                (.18+foam**1.6)*(.14+.83*lift))
        deposit(contact,cam.cx+cam.radius*(x+.19*y),cam.cy+cam.radius*cam.flatten*(z+.26*y),
                (.09+.35*lift)*np.exp(-.85*y))
    rc=cv2.GaussianBlur(reflected,(0,0),2.7)
    rc=cv2.resize(1-np.exp(-rc*.93),(w,h),interpolation=cv2.INTER_LINEAR)
    sc=cv2.resize(1-np.exp(-cv2.GaussianBlur(contact,(0,0),3.8)*.48),(w,h))
    iy,ix=np.ogrid[:h,:w]
    radial=((ix-cam.cx)/cam.radius)**2+((iy-cam.cy)/(cam.radius*cam.flatten))**2
    wet=np.clip((1.72-radial)/.55,0.,1.).astype(np.float32)
    wet*=np.clip((radial-.07)/.12,0.,1.).astype(np.float32)
    base=scene.astype(np.float32)
    rough=base[:,:,0]-cv2.GaussianBlur(base[:,:,0],(0,0),3.)
    glitter=np.clip(.46+rough*.054,.08,1.)**1.9
    env=_ramp(.035,.25,t)*(1-_ramp(1.02,1.52,t))
    scene[:]=np.uint8(np.clip(base*(1-(sc*.17*env)[:,:,None])+
                       (env*rc*wet*(.18+.83*glitter))[:,:,None]*[99,63,22],0,255))


def draw_water(scene,cam,t,layer):
    """Composite the selected physical shell into the input RGB battle frame in place."""
    if not 0.<t<1.67:
        return scene
    rear=layer=="rear"
    if not rear and layer!="front":
        raise ValueError(f"Unknown water layer {layer}")
    pos,tri,uv,foam=wave_sim.sample(t)[0 if rear else 1]
    if len(pos)==0 or len(tri)==0:
        return scene
    co=math.sqrt(1.-cam.flatten**2)
    proj=np.empty_like(pos)
    proj[:,0]=cam.cx+cam.radius*pos[:,0]
    proj[:,1]=cam.cy+cam.radius*(cam.flatten*pos[:,2]-co*pos[:,1])
    proj[:,2]=cam.flatten*pos[:,1]+co*pos[:,2]
    x0,y0=np.maximum([0,0],np.floor(proj[:,:2].min(0)-3).astype(int))
    x1,y1=np.minimum([scene.shape[1],scene.shape[0]],np.ceil(proj[:,:2].max(0)+4).astype(int))
    if x1<=x0 or y1<=y0:
        return scene
    proj[:,0]-=x0
    proj[:,1]-=y0
    normals,tangents=_attributes(pos,tri,uv)
    near,far,depth,data=_trace(proj,tri,normals,tangents,uv,foam,int(x1-x0),int(y1-y0))
    background=scene.copy()
    decode,encode=_color_tables()
    _shade(background,scene,int(x0),int(y0),cam.cx,cam.cy,cam.radius,cam.flatten,t,near,far,depth,data,_maps(),decode,encode)
    # Fade in shallow water so the first frame does not pop out as a complete surface.
    onset=_ramp(.015,.17,t)
    release=1.-_ramp(1.22,1.65,t)
    env=onset*release
    if env < .999:
        roi=scene[y0:y1,x0:x1]
        cv2.addWeighted(roi,float(env),background[y0:y1,x0:x1],1.-float(env),0,dst=roi)
    points=wave_sim.spray(t)
    if len(points):
        selected=points[:,2]<0 if rear else points[:,2]>=0
        velocities=wave_sim.spray_motion(t) if hasattr(wave_sim,"spray_motion") else np.zeros((len(points),3),np.float32)
        density=_spray(scene,cam.cx,cam.cy,cam.radius,cam.flatten,points[selected],velocities[selected],near,x0,y0)
        mist=cv2.GaussianBlur(density,(0,0),1.95)
        # Tiny drops become a soft aerated plume only where many real trajectories overlap.
        mist=cv2.resize(np.clip((mist-.024)*.55,0,.19),(scene.shape[1],scene.shape[0]))
        scene[:]=np.uint8(np.clip(scene+(238.-scene)*mist[:,:,None],0,255))
    return scene
