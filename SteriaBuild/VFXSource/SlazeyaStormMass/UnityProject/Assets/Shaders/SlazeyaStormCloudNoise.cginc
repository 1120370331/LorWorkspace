// Project-local shared density for macro cloud volumes and boundary wisps.
#ifndef SLAZEYA_STORM_CLOUD_NOISE_INCLUDED
#define SLAZEYA_STORM_CLOUD_NOISE_INCLUDED
   float cloudHash(float3 p)
   {p=frac(p*0.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
   float valueNoise(float3 p)
   {
    float3 cell=floor(p),u=frac(p);u=u*u*(3-2*u);
    return lerp(lerp(lerp(cloudHash(cell),cloudHash(cell+float3(1,0,0)),u.x),
                     lerp(cloudHash(cell+float3(0,1,0)),cloudHash(cell+float3(1,1,0)),u.x),u.y),
                lerp(lerp(cloudHash(cell+float3(0,0,1)),cloudHash(cell+float3(1,0,1)),u.x),
                     lerp(cloudHash(cell+float3(0,1,1)),cloudHash(cell+float3(1,1,1)),u.x),u.y),u.z);
   }
   float cloudFbm(float3 p)
   {return valueNoise(p*0.9)*0.55+valueNoise(p*2.4)*0.30+valueNoise(p*6.0)*0.15;}

// Continuous radial/vertical cross-section roll. The angular modulation is periodic
// at the wrap; inverse spin transports the same material in the cloud's wind direction.
// Analytic monotone sink, H-normalized. CPU controller implements these same P/I functions.
float cloudSinkP(float r) {return r>=0.35?((r+0.25)*(r+0.25)-0.36)*0.5:0.21*log(max(r,1e-12)/0.35);}
float cloudSinkI(float s) {return s>=0?sqrt(0.36+2*s)-0.25:0.35*exp(s/0.21);}
float cloudSinkDerivative(float r) {return r>=0.35?r+0.25:0.21/max(r,1e-12);}
float3 cloudSinkInverse(float3 q,float travel,out float compensation,out float inverseGradient)
{
    compensation=1;inverseGradient=1;if(travel<=0)return q;
    float r=length(q.xz),u=saturate(r/0.35),sy=u*u*(3-2*u);
    float safeSy=max(sy,1e-12);
    if(r<1e-12){compensation=8;inverseGradient=1e12;return float3(0,q.y/safeSy,0);}
    float r0=cloudSinkI(cloudSinkP(r)+travel);
    float radial=cloudSinkDerivative(r0)/cloudSinkDerivative(r);
    float J=(r/r0)*radial*sy;
    float syDerivative=r<0.35?6*u*(1-u)/0.35:0;
    compensation=min(8,1/max(J,1e-20));
    inverseGradient=max(max(r0/r,1/max(radial,1e-20)),1/safeSy)+abs(q.y)*syDerivative/(safeSy*safeSy);
    return float3(q.x*r0/r,q.y/safeSy,q.z*r0/r);
}
float3 cloudRingCarrier(float3 q,float2 radii,float ground,float spin,float phase)
{
    float radius=(radii.x+radii.y)*0.5;
    float3 p=float3(q.x/radii.x*radius,q.y,q.z/radii.y*radius);
    float c=cos(spin),s=sin(spin);p.xz=float2(c*p.x+s*p.z,-s*p.x+c*p.z);
    float theta=atan2(p.z,p.x),rho=length(p.xz);
    float y0=ground+0.75+0.10*sin(theta);
    float2 section=float2(rho-radius-0.45,p.y-y0);
    float roll=(2.4+0.7*sin(3*theta))*phase;
    c=cos(roll);s=sin(roll);section=float2(c*section.x+s*section.y,-s*section.x+c*section.y);
    float rr=radius+0.45+section.x;
    return float3(rr*cos(theta),y0+section.y,rr*sin(theta));
}
float3 cloudCarrier(float3 world,float3 center,float2 innerRadii,float height,float spin,float phase,float collapse)
{
    float3 local=world-center;
    float radius=(innerRadii.x+innerRadii.y)/(2*height);
    float3 p=float3(local.x/innerRadii.x*radius,local.y/height,local.z/innerRadii.y*radius);
    float c=cos(spin),s=sin(spin);p.xz=float2(c*p.x+s*p.z,-s*p.x+c*p.z);
    p.y-=phase*0.10;float3 original=p;
    return p+0.06*sin(original.zxy*0.8+float3(0.9,0.7,0.8)*phase);
}
float sharedCloudField(float3 world,float3 center,float2 innerRadii,float height,float spin,float phase,float collapse)
{
    return smoothstep(0.28,0.72,cloudFbm(cloudCarrier(world,center,innerRadii,height,spin,phase,collapse)));
}
#endif
