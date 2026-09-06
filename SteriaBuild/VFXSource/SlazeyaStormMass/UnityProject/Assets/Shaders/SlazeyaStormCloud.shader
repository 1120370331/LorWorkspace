Shader "Steria/SlazeyaStormCloud"
{
 Properties
 {
  _Atlas("Cloud density lighting shadow coverage",2D)="white"{}
  _FlowTex("Existing flow RG",2D)="gray"{}
  _Alpha("Explicit reveal",Float)=0
  _Phase("Explicit local cloud clock",Float)=0
  _Charge("Core pressure",Float)=0
  _WaterMix("Continuous condensation",Float)=0
  _CloudCenter("Frozen cloud/core center",Vector)=(0,0,0,0)
  _CloudHeight("Reference H",Float)=6
  _CloudInnerRadii("Fixed elliptical hole",Vector)=(9.6,7.6,0,0)
  _CloudSpinAngle("Carrier theta spin",Float)=0
  _DensityPhase("Independent density advection clock",Float)=0
  _Collapse("Density field contraction",Float)=0
  _DebugDensity("Native evolving density probe",Float)=0
  _CloudDark("Absorbing cloud",Color)=(0.157,0.212,0.259,1)
  _CloudMid("Cloud middle body",Color)=(0.420,0.500,0.570,1)
  _CloudLight("Lit cloud folds",Color)=(0.700,0.770,0.820,1)
  _LightningPosition("Local discharge position",Vector)=(0,0,0,0)
  _LightningEnergy("Local discharge energy",Float)=0
  _LightningRadius("Local discharge reach",Float)=1
  _DebugFrame("Native fixed frame probe",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Blend One OneMinusSrcAlpha
  ZWrite Off
  ZTest LEqual
  Cull Off
  Pass
  {
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _Atlas,_FlowTex;
   float _Alpha,_Phase,_Charge,_WaterMix,_LightningEnergy,_LightningRadius,_DebugFrame;
   float4 _CloudDark,_CloudMid,_CloudLight,_LightningPosition;
   float4 _CloudCenter,_CloudInnerRadii;float _CloudHeight,_Collapse,_DebugDensity,_CloudSpinAngle,_DensityPhase;
   // UV.xy / AgePercent.z / StableRandomX.w, identical native stream layout to the other particles.
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float4 uvAge:TEXCOORD0;};
   struct v2f{float4 position:SV_POSITION;float4 color:COLOR;float4 uvAge:TEXCOORD0;float3 world:TEXCOORD1;};
   v2f vert(appdata v)
   {
    v2f o;o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uvAge=v.uvAge;
    o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
   }
   float4 cloudFrame(float frame,float2 uv)
   {
    float2 cell=float2(fmod(frame,4),3-floor(frame/4));
    float inset=0.5/256.0;
    return tex2D(_Atlas,(cell+lerp(inset,1-inset,saturate(uv)))/4);
   }
   #include "SlazeyaStormCloudNoise.cginc"
   float4 frag(v2f i):SV_Target
   {
    float seed=saturate(i.uvAge.w);
    // Complete cloud frames 1..7 remain stable through arbitrary waits. No lifetime-frame cycling.
    float frame=1+min(5.999,seed*6);
    if(_DebugFrame>0.5)return float4(frame/7,seed,0,1);
    float2 flow=tex2D(_FlowTex,float2(seed*3+i.uvAge.x*0.25,i.uvAge.y*0.65+0.15)).rg*2-1;
    // Main cloud cards spend their support budget on the dense atlas interior, not its empty margins.
    // Wisps and the post-burst wet phase retain the original full-tile silhouette.
    float bodyRole=saturate(i.color.g)*(1-_WaterMix);
    float2 denseUV=float2(0.515,0.460)+(i.uvAge.xy-0.5)*float2(0.80,0.56);
    float2 uv=saturate(lerp(i.uvAge.xy,denseUV,bodyRole)+flow*0.006*sin(_Phase*0.7+seed*6.28));
    float4 data=lerp(cloudFrame(floor(frame),uv),cloudFrame(min(7,floor(frame)+1),uv),frac(frame));
    // One continuous 3D density field shared by overlapping cards, advected through three scales.
    // Its coordinates contract with the carrier cloud, and coverage retains a floor for every sector.
    float density=sharedCloudField(i.world,_CloudCenter.xyz,_CloudInnerRadii.xy,_CloudHeight,_CloudSpinAngle,_DensityPhase,_Collapse);
    float dynamicAmplitude=lerp(1,0.15,_Collapse);
    float coverage=lerp(1,lerp(0.35,1,density),dynamicAmplitude);
    float variation=(density-0.5)*0.24*dynamicAmplitude;
    if(_DebugDensity>0.5)return float4(density,density,density,1);
    float illumination=saturate(0.18+data.g*0.72-data.b*0.28+variation+(i.color.r-0.5)*0.10);
    float3 color=lerp(_CloudDark.rgb,_CloudMid.rgb,smoothstep(0.10,0.55,illumination));
    color=lerp(color,_CloudLight.rgb,smoothstep(0.42,0.80,illumination));
    color*=(1-0.22*data.r)*(1-0.12*density*dynamicAmplitude);
    // Absorbing body under the illuminated rolls; coverage is not used as an emission mask.
    float selfShadow=smoothstep(0.38,0.82,data.r)*(0.22+0.40*data.b)*bodyRole*(1-_Charge);
    color*=1-0.30*selfShadow;
    float litFold=smoothstep(0.70,0.92,data.g)*(1-data.b)*smoothstep(0.30,0.95,i.color.r);
    color=lerp(color,float3(0.863,0.898,0.922),litFold*0.65);
    float3 wet=lerp(float3(0.208,0.275,0.322),float3(0.863,0.898,0.922),saturate(data.g*0.76+data.b*0.23));
    color=lerp(color,wet,_WaterMix);
    color=lerp(color,float3(0.953,0.973,1),_Charge*0.9)+float3(0.85,0.90,1)*_Charge;
    float localLight=saturate(1-distance(i.world,_LightningPosition.xyz)/max(0.001,_LightningRadius));
    color+=float3(0.90,0.94,1)*_LightningEnergy*localLight*localLight*0.22;
    // Convert A to optical depth once. A is neither multiplied by itself nor by baked R density.
    float opticalDepth=-log(max(0.001,1-data.a));
    float thickRole=1-smoothstep(0.20,0.50,i.color.r);
    float thickness=lerp(1,lerp(1.8,2.8,thickRole),bodyRole);
    float opticalCoverage=1-exp(-opticalDepth*thickness);
    // A soft, irregular footprint prevents cropped tiles from exposing straight rectangular edges.
    float2 card=i.uvAge.xy*2-1;
    float contour=dot(card,card)+0.10*(density-0.5);
    float softEdge=lerp(1,1-smoothstep(0.72,1.04,contour),bodyRole);
    float alpha=opticalCoverage*i.color.a*_Alpha*coverage*softEdge;
    return float4(color*alpha,alpha);
   }
   ENDCG
  }
 }
 Fallback Off
}
