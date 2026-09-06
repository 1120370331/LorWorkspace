/*
MIT License

Copyright (c) 2019 Sebastian Lague

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
// Adapted from Sebastian Lague / Clouds sampleDensity, lightmarch and hg (MIT, 2019).
// Full copyright/permission text accompanies this file and ThirdParty/NOTICE.txt.
#ifndef SLAZEYA_STORM_CLOUD_DENSITY_INCLUDED
#define SLAZEYA_STORM_CLOUD_DENSITY_INCLUDED
Texture3D<float4> _CloudShapeTex;
Texture3D<float4> _CloudErosionTex;
SamplerState sampler_CloudShapeTex;
SamplerState sampler_CloudErosionTex;
float _ShapePeriodH,_DetailPeriodH,_Coverage,_ErosionStrength;
float _CloudHeight,_CloudSpinAngle,_DensityPhase,_Collapse,_MacroCount,_ProxyIndex;
float _LocalScale,_CloudGroundY,_SinkTravel,_SinkMaxRadius,_CoreAccumulation;
float4 _CloudCenter,_CloudInnerRadii;
float4 _MacroCenters[12],_MacroAxisX[12],_MacroAxisY[12],_MacroAxisZ[12];

struct CloudBaseSample
{
    float baseDensity,unionEnvelope,owned,total,coreBlend;
    float signedDistance,bodyStrength,holeMask,compensation,inverseGradient;
    float3 carrier;
};
float CloudTextureLOD(float worldStep,float period,float resolution,float inverseGradient)
{
    float averageRadius=(_CloudInnerRadii.x+_CloudInnerRadii.y)*0.5;
    float maximumCarrierGradient=max(1,max(averageRadius/_CloudInnerRadii.x,averageRadius/_CloudInnerRadii.y));
    float voxelWorld=_CloudHeight*period/(resolution*maximumCarrierGradient*max(1,inverseGradient));
    return max(0,log2(max(1,worldStep/max(1e-6,voxelWorld))));
}
float CloudTextureLOD(float worldStep,float period,float resolution)
{return CloudTextureLOD(worldStep,period,resolution,1);}
// Both eye and light rays use the same single box. Directions are not normalized
// after transformation, so all intersections and integration lengths stay in world units.
float2 CloudBoxInterval(float3 o,float3 d)
{
    float3 safeD=float3(d.x>=0?max(d.x,1e-9):min(d.x,-1e-9),d.y>=0?max(d.y,1e-9):min(d.y,-1e-9),d.z>=0?max(d.z,1e-9):min(d.z,-1e-9));
    float3 t0=(-1-o)/safeD,t1=(1-o)/safeD;
    float3 lo=min(t0,t1),hi=max(t0,t1);
    return float2(max(lo.x,max(lo.y,lo.z)),min(hi.x,min(hi.y,hi.z)));
}
CloudBaseSample SampleCloudBase(float3 world,float worldStep)
{
    CloudBaseSample result=(CloudBaseSample)0;
    float3 delta=world-_MacroCenters[0].xyz;
    float3 box=float3(dot(delta,_MacroAxisX[0].xyz),dot(delta,_MacroAxisY[0].xyz),dot(delta,_MacroAxisZ[0].xyz));
    if(any(abs(box)>1))return result;
    float3 currentQ=(world-_CloudCenter.xyz)/_CloudHeight;
    float currentRadius=length(currentQ);
    // Visual accumulation comes only from transported identities entering the nucleus.
    result.unionEnvelope=8*_CoreAccumulation*(1-smoothstep(0.70,1,currentRadius/0.03));
    result.baseDensity=result.unionEnvelope;
    float3 q=cloudSinkInverse(currentQ,_SinkTravel,result.compensation,result.inverseGradient);
    float2 radii=_CloudInnerRadii.xy/_CloudHeight;
    float ground=(_CloudGroundY-_CloudCenter.y)/_CloudHeight;
    if((_SinkTravel>0&&length(q.xz)>_SinkMaxRadius)||abs(q.x)>radii.x+0.935||abs(q.z)>radii.y+0.935||abs(q.y-ground-0.75)>1)return result;
    float theta=atan2(q.z/radii.y,q.x/radii.x);
    float2 edge=radii*float2(cos(theta),sin(theta));
    float r=length(q.xz-edge)*(length(q.xz/radii)>=1?1:-1);
    float v=q.y-(ground+0.75+0.10*sin(theta));
    float h=0.48+0.08*sin(theta);
    float D=0.30*(1-length(float2((r-0.45)/0.30,v/h)));
    result.holeMask=smoothstep(0,0.035,r); // Frozen inner hole remains empty before final core fill.
    result.carrier=cloudRingCarrier(q,radii,ground,_CloudSpinAngle,_DensityPhase);
    float4 shapeTex=_CloudShapeTex.SampleLevel(sampler_CloudShapeTex,result.carrier/_ShapePeriodH,CloudTextureLOD(worldStep,_ShapePeriodH,64,result.inverseGradient));
    float n=0.090*(2*shapeTex.g-1)+0.060*(2*shapeTex.b-1)+0.025*(2*shapeTex.a-1);
    result.signedDistance=D+n;
    float bodyField=dot(shapeTex.gba,float3(0.25,0.50,0.25));
    result.bodyStrength=max(0,(bodyField-0.38)*5);
    result.baseDensity=max(smoothstep(0,0.035,D+n)*result.bodyStrength*result.holeMask*result.compensation,result.unionEnvelope);
    result.owned=1;result.total=1;
    return result;
}
float2 SampleCloudDensityFromBase(float3 world,float worldStep,CloudBaseSample source)
{
    if(source.baseDensity<=0)return float2(0,0);
    if(source.bodyStrength<=0)return source.unionEnvelope.xx;
    float3 uv=source.carrier/_DetailPeriodH+_DensityPhase*float3(0.015,-0.025,0.010);
    float detail=dot(_CloudErosionTex.SampleLevel(sampler_CloudErosionTex,uv,CloudTextureLOD(worldStep,_DetailPeriodH,32,source.inverseGradient)).rgb,float3(0.625,0.25,0.125));
    float density=smoothstep(0,0.035,source.signedDistance-0.035*(1-detail))*source.bodyStrength*source.holeMask;
    density=max(density*source.compensation,source.unionEnvelope);
    return float2(density,density);
}
float2 SampleCloudDensity(float3 world,float worldStep)
{return SampleCloudDensityFromBase(world,worldStep,SampleCloudBase(world,worldStep));}

// Sebastian Lague hg / phase adaptation. 4*pi restores unit scale for isotropic scattering.
float HG(float mu,float g)
{
    float g2=g*g;
    return (1-g2)/(12.566370614359*pow(max(1e-5,1+g2-2*g*mu),1.5));
}
float CloudPhaseHG(float mu)
{return 12.566370614359*(0.8*HG(mu,0.35)+0.2*HG(mu,-0.20));}
#endif
