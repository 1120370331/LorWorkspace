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
Shader "Steria/SlazeyaStormCloudVolume"
{
 Properties
 {
  _Opacity("Explicit lifecycle opacity",Range(0,1))=0
  _StepsPerH("Fixed world steps per scaled H",Range(16,96))=48
  _CloudShapeTex("Baked linear Perlin-Worley shape",3D)="white"{}
  _CloudErosionTex("Baked independent edge erosion",3D)="white"{}
  _ShapePeriodH("Shape period in H",Float)=2
  _DetailPeriodH("Detail period in H",Float)=1.2
  _Coverage("Shape occupancy",Range(0,1))=0.82
  _ErosionStrength("Legacy diagnostic only",Range(0,1))=0.035
  _DebugTransmittance("T diagnostic: black absorption on white",Float)=0
  _SigmaT("Extinction per reference H",Float)=4.5
  _ShadowStrength("Directional density occlusion",Range(0,1))=1
  _LightSteps("Exit-bounded light integral",Range(48,96))=48
  _CloudHeight("Reference H",Float)=6
  _CloudGroundY("Frozen ground height",Float)=0
  _CloudCenter("Frozen core",Vector)=(0,0,0,0)
  _CloudInnerRadii("Fixed hole",Vector)=(9.6,7.6,0,0)
  _CloudSpinAngle("Signed carrier rotation",Float)=0
  _DensityPhase("Explicit density clock",Float)=0
  _Collapse("Carrier contraction",Float)=0
  _Charge("Core emission",Float)=0
  _ExtinctionScale("Finite contraction compensation",Float)=1
  _LocalScale("Legacy diagnostic scale",Float)=1
  _SinkTravel("Analytic inward transport",Float)=0
  _SinkMaxRadius("Source support radius in H",Float)=4
  _CoreAccumulation("Arrived cloud fraction",Float)=0
  _ProxyIndex("Owned density share",Float)=0
  _MacroCount("Single continuous box",Float)=1
  _DebugDensity("Native density comparison",Float)=0
  _DebugSampling("GPU integral parameter audit",Float)=0
  _LightningPosition("Local discharge",Vector)=(0,0,0,0)
  _LightningEnergy("Local discharge energy",Float)=0
  _LightningRadius("Local discharge radius",Float)=1
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
  Cull Back
  ZTest LEqual
  ZWrite Off
  Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "SlazeyaStormCloudNoise.cginc"
   #include "SlazeyaStormCloudDensity.cginc"
   float _Opacity,_StepsPerH,_SigmaT,_ShadowStrength,_Charge,_LightSteps;
   float _ExtinctionScale,_DebugSampling,_DebugDensity,_DebugTransmittance,_LightningEnergy,_LightningRadius;
   float4 _LightningPosition;
   struct appdata {float4 vertex:POSITION;};
   struct v2f {float4 position:SV_POSITION;float3 world:TEXCOORD0;};
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
   // Seb lightmarch: integrate up to the actual container exit, not a fixed short horizon.
   // Here the container is the single enclosing box; t stays in world units.
   float2 coreRayInterval(float3 o,float3 d,float enter,float leave)
   {
    if(_SinkTravel<=0)return float2(leave,leave);
    float3 q=o-_CloudCenter.xyz;float b=dot(q,d),disc=b*b-dot(q,q)+pow(_CloudHeight*0.06,2);
    if(disc<=0)return float2(leave,leave);
    float root=sqrt(disc);float a=max(enter,-b-root),z=min(leave,-b+root);
    return z>a?float2(a,z):float2(leave,leave);
   }
   float lightTransmission(float3 samplePosition,float sigma)
   {
    if(_ShadowStrength<=0)return 1;
    float3 direction=normalize(float3(-0.4,0.8,-0.45));
    float3 delta=samplePosition-_MacroCenters[0].xyz;
    float3 o=float3(dot(delta,_MacroAxisX[0].xyz),dot(delta,_MacroAxisY[0].xyz),dot(delta,_MacroAxisZ[0].xyz));
    float3 d=float3(dot(direction,_MacroAxisX[0].xyz),dot(direction,_MacroAxisY[0].xyz),dot(direction,_MacroAxisZ[0].xyz));
    float leave=max(0,CloudBoxInterval(o,d).y);
    int count=(int)clamp(_LightSteps,48,96);
    float2 core=coreRayInterval(samplePosition,direction,0,leave);
    float outerLength=core.x+leave-core.y,innerLength=core.y-core.x;
    int innerCount=innerLength>0?max(1,(int)ceil(innerLength/(_CloudHeight*0.03/8))):0;
    float outerStep=outerLength/max(1,count),innerStep=innerLength/max(1,innerCount);
    float opticalDepth=0,cursor=0;
    [loop]for(int lightIndex=0;lightIndex<132;lightIndex++)
    {
     if(cursor>=leave||opticalDepth>4.6)break;
     bool inside=cursor>=core.x&&cursor<core.y;
     float boundary=inside?core.y:cursor<core.x?core.x:leave;
     float lightStep=min(inside?innerStep:outerStep,boundary-cursor);
     if(lightStep<=0)break;
     float3 p=samplePosition+direction*(cursor+0.5*lightStep);cursor+=lightStep;
     opticalDepth+=SampleCloudDensity(p,lightStep).x*lightStep*sigma;
    }
    return exp(-opticalDepth*_ShadowStrength);
   }
   float4 frag(v2f i):SV_Target
   {
    if(_DebugSampling>0.5)return float4(clamp(_LightSteps,48,96)/96,clamp(_StepsPerH,16,96)/96,1,1);
    float3 direction,origin;
    if(unity_OrthoParams.w>0.5)
    {
     direction=normalize(-UNITY_MATRIX_V[2].xyz);
     origin=i.world-direction*dot(i.world-_WorldSpaceCameraPos,direction);
    }
    else {origin=_WorldSpaceCameraPos;direction=normalize(i.world-origin);}
    // Rebase near the proxy before the slab intersection: small cores never subtract large camera-space squares.
    float cameraTravel=dot(i.world-origin,direction);float3 rayBase=origin+direction*cameraTravel;
    float3 localOrigin=mul(unity_WorldToObject,float4(rayBase,1)).xyz;
    float3 localDirection=mul((float3x3)unity_WorldToObject,direction); // NOT normalized: t remains world length.
    float2 interval=CloudBoxInterval(localOrigin,localDirection);
    float enter=max(interval.x,-cameraTravel),leave=interval.y;
    if(leave<=enter)return 0;
    float fixedStep=_CloudHeight/clamp(_StepsPerH,16,96);
    // Cover the entire box even for unusually wide formations. Ordinary paths
    // retain H/48; only paths exceeding the budget increase their world step.
    fixedStep=max(fixedStep,(leave-enter)/984);
    float2 core=coreRayInterval(rayBase,direction,enter,leave);
    float coreStep=_CloudHeight*0.03/8;
    float sigma=_SigmaT/max(0.001,_CloudHeight);
    float transmission=1,cursor=enter;float3 radiance=0;float fieldSum=0;int sampled=0;
    float phase=CloudPhaseHG(dot(direction,normalize(float3(-0.4,0.8,-0.45))));
    [loop]for(int step=0;step<1024;step++)
    {
     if(cursor>=leave||transmission<0.02)break;
     bool inside=cursor>=core.x&&cursor<core.y;
     float boundary=inside?core.y:cursor<core.x?core.x:leave;
     // Inward compression also raises inverse gradients outside the .06H nucleus.
     // Limit source-space travel per eye sample, not just the final core's world step.
     float compensation,inverseGradient;
     cloudSinkInverse((rayBase+direction*cursor-_CloudCenter.xyz)/_CloudHeight,_SinkTravel,compensation,inverseGradient);
     float localStep=fixedStep/max(1,inverseGradient);
     // Reserve exact fine-core segments; spend the remaining budget over ALL outer distance.
     float coreRemaining=max(0,core.y-max(cursor,core.x));
     float outerRemaining=max(0,core.x-cursor)+max(0,leave-max(cursor,core.y));
     float outerBudget=max(1,1024-step-ceil(coreRemaining/coreStep)-2);
     localStep=max(localStep,outerRemaining/outerBudget);
     float ds=min(inside?coreStep:localStep,boundary-cursor);
     if(ds<=0)break;
     float3 samplePosition=rayBase+direction*(cursor+0.5*ds);cursor+=ds;
     float2 rho=SampleCloudDensity(samplePosition,ds);fieldSum+=rho.x;sampled++;
     if(rho.y<=0.0001)continue;
     float light=lightTransmission(samplePosition,sigma);
     float opacity=1-exp(-sigma*rho.y*ds);
     float3 scatter=float3(0.10,0.135,0.17)+float3(0.72,0.79,0.85)*light*phase;
     float coreCharge=_Charge*(1-smoothstep(0.03,0.06,distance(samplePosition,_CloudCenter.xyz)/_CloudHeight));
     scatter=lerp(scatter,float3(0.95,0.98,1)+float3(0.95,0.97,1)*coreCharge,coreCharge);
     float flash=saturate(1-distance(samplePosition,_LightningPosition.xyz)/max(0.001,_LightningRadius));
     scatter+=float3(0.9,0.95,1)*_LightningEnergy*flash*flash*0.20;
     radiance+=transmission*opacity*scatter;transmission*=1-opacity;
    }
    if(_DebugTransmittance>0.5)return float4(0,0,0,(1-transmission)*_Opacity);
    if(_DebugDensity>0.5){float field=fieldSum/max(1,sampled);return float4(field,field,field,1);}
    // Radiance is already premultiplied by the integrated transmittance; do not multiply alpha twice.
    return float4(radiance*_Opacity,(1-transmission)*_Opacity);
   }
   ENDCG
  }
 }
 Fallback Off
}
