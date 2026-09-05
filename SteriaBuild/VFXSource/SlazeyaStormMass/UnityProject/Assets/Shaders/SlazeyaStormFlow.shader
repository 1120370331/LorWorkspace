Shader "Steria/SlazeyaStormFlow"
{
 Properties
 {
  _BodyTex("Density foam tear coverage",2D)="white"{}
  _NormalRoughness("RGB normal / roughness",2D)="bump"{}
  _FlowTex("Flow direction",2D)="gray"{}
  _Radii("Envelope world radii",Vector)=(12,5,10,0)
  _Ground("Ground relative to envelope",Float)=-3
  _Band("Burst latitude relative height",Float)=0
  _Height("Reference body height",Float)=6
  _Phase("Explicit flow",Float)=0
  _ShapePhase("Captured collapse flow",Float)=0
  _Collapse("Three dimensional contraction",Float)=0
  _CoreRadius("Finite world core radius",Float)=0.36
  _Gather("Pressure",Float)=0
  _FoamGather("Material transport",Float)=0
  _Charge("Final local foam pressure highlight",Float)=0
  _BurstAge("Callback age",Float)=-1
  _Rupture("Flexible patch release",Float)=0
  _Alpha("Reveal",Float)=0
  _LightningPosition("Local illumination",Vector)=(0,0,0,0)
  _LightningEnergy("Bounded pulse",Float)=0
  _LightningRadius("Local light radius",Float)=1
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
  Blend One OneMinusSrcAlpha
  ZWrite Off
  ZTest LEqual
  CGINCLUDE
  #include "UnityCG.cginc"
  sampler2D _BodyTex,_NormalRoughness,_FlowTex;
  float3 _Radii;float _Ground,_Band,_Height,_Phase,_Gather,_FoamGather,_Charge,_BurstAge,_Rupture,_Alpha;
  float4 _LightningPosition;float _LightningEnergy,_LightningRadius;
  float _ShapePhase,_Collapse,_CoreRadius;
  struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 patch:TEXCOORD1;};
  struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float4 patch:TEXCOORD3;float3 local:TEXCOORD4;};
  float ease(float x){x=saturate(x);return x*x*(3-2*x);}
  float outCurve(float x){x=saturate(x);return 1-(1-x)*(1-x);}
  float3 ringNormal(float theta){return normalize(float3(cos(theta)/_Radii.x,0,sin(theta)/_Radii.z));}
  float3 ringTangent(float theta){return normalize(float3(-_Radii.x*sin(theta),0,_Radii.z*cos(theta)));}
  float2 bandSection(){return _Radii.xz;}
  float ringLobe(float theta,float center,float width)
  {
   float distance=atan2(sin(theta-center),cos(theta-center));return exp(-distance*distance/(width*width));
  }
  float ringLobes(float theta)
  {
   return max(ringLobe(theta,2.25,0.48),max(0.90*ringLobe(theta,3.72,0.38),max(0.80*ringLobe(theta,5.10,0.46),0.60*ringLobe(theta,0.70,0.32))));
  }
  float3 ExpandedRingPoint(float2 uv)
  {
   float theta=uv.x*6.283185307,q=uv.y,progress=outCurve(_BurstAge/0.25);
   float strength=ringLobes(theta),fall=max(0,_BurstAge-0.12);
   float side=1-0.68*pow(max(0,cos(theta)),6);
   float width=0.10+0.39*strength+0.02*sin(theta*11);
   float2 radius=bandSection()*(1+progress*(0.025+q*width)*side+fall*0.10*side);
   float crest=0.18*strength*sin(q*3.14159265);
   float y=_Height*progress*(crest-0.53*pow(q,1.25)-0.11*(1-strength));
   y-=_Height*(fall*fall*(2.8+2*q)+ease((_BurstAge-0.27)/0.30)*0.16);
   y+=_Height*0.030*progress*sin(theta*23+q*9-_BurstAge*12)*sin(q*3.14159265);
   float angle=theta+progress*q*(0.035+0.065*strength);
   return float3(radius.x*cos(angle),max(_Ground+0.015*_Height,y),radius.y*sin(angle));
  }
  float3 CorePoint(float2 uv)
  {
   float theta=uv.x*6.283185307,phi=(uv.y-0.5)*3.14159265;
   return float3(cos(theta)*cos(phi),sin(phi),sin(theta)*cos(phi))*_CoreRadius;
  }
  float3 RingPoint(float2 uv){return lerp(CorePoint(uv),ExpandedRingPoint(uv),ease(_BurstAge/0.23));}
  float3 BubblePoint(float2 uv,float4 patch)
  {
   if(patch.w>1.5)return RingPoint(uv);
   float theta=uv.x*6.283185307,q=uv.y,scale=1.14;
   float ymin=_Ground/(_Radii.y*scale),phi0=asin(clamp(ymin,-0.999,0.999)),phi=lerp(phi0,1.570796327,q);
   float warp=1+0.018*(0.5+0.5*sin(theta*5+_ShapePhase*2)*cos(phi*3+_ShapePhase))*pow(sin(3.14159265*q),2);
   float3 p=patch.w>0.5?float3(_Radii.x*scale*sqrt(1-ymin*ymin)*q*cos(theta),_Ground,_Radii.z*scale*sqrt(1-ymin*ymin)*q*sin(theta))
     :float3(_Radii.x*scale*cos(phi)*cos(theta)*warp,_Radii.y*scale*sin(phi)*warp,_Radii.z*scale*cos(phi)*sin(theta)*warp);
   // Radial transport includes the cap. Never divide ground by a vanishing collapse scale.
   float radius=length(p);float3 dir=radius>0.000001?p/radius:float3(0,-1,0);
   float angle=_Collapse*0.55,c=cos(angle),s=sin(angle);
   return float3(c*dir.x+s*dir.z,dir.y,-s*dir.x+c*dir.z)*lerp(radius,_CoreRadius,_Collapse);
  }
  float3 BubbleNormal(float2 uv,float4 patch)
  {
   // Welded pole fans: a finite near-pole tangent frame, never normalize a zero azimuth tangent.
   if(patch.w<0.5&&uv.y>0.9995&&_BurstAge<0)return float3(0,1,0);
   float2 a=uv;a.y=clamp(a.y,0.0005,0.9995);
   float e=0.0002;
   float3 ds=BubblePoint(a+float2(e,0),patch)-BubblePoint(a-float2(e,0),patch);
   float3 dq=BubblePoint(a+float2(0,e),patch)-BubblePoint(a-float2(0,e),patch);
   float3 n=patch.w>1.5?cross(ds,dq):cross(dq,ds);
   return dot(n,n)>1e-12?normalize(n):float3(0,1,0);
  }
  v2f vert(appdata v)
  {
   v2f o;float3 p=BubblePoint(v.uv,v.patch);
   o.position=UnityObjectToClipPos(float4(p/_Radii,1));o.world=mul(unity_ObjectToWorld,float4(p/_Radii,1)).xyz;
   o.normal=normalize(mul((float3x3)unity_ObjectToWorld,BubbleNormal(v.uv,v.patch)/_Radii));
   o.uv=v.uv;o.patch=v.patch;o.local=p;return o;
  }
  float4 frag(v2f i,float facing:VFACE):SV_Target
  {
   float theta=i.uv.x*6.283185307,cap=i.patch.w>0.5&&i.patch.w<1.5,ring=i.patch.w>1.5;
   float2 flow=tex2D(_FlowTex,float2(i.uv.x*2-_Phase*0.08,lerp(0.08,0.88,i.uv.y))).rg*2-1;
   float2 bodyUV=float2(i.uv.x*2-_Phase*0.17,lerp(0.10,0.86,i.uv.y))+flow*float2(0.035,0.02);
   float4 body=tex2D(_BodyTex,bodyUV),detail=tex2D(_NormalRoughness,bodyUV);
   // The original surface carries the texture into the core; there is no replacement core mesh.
   float pole=smoothstep(0.88,0.985,i.uv.y)*(1-cap)*(1-ring);
   float4 top=tex2D(_BodyTex,i.local.xz/max(_CoreRadius,_Radii.xz*(1-_Collapse))*0.45+0.5+float2(_Phase*0.025,0));
   body=lerp(body,top,pole);
   float3 n=normalize(i.normal);if(facing<0)n=-n;
   float3 tangent=cross(float3(0,1,0),n);if(dot(tangent,tangent)<0.001)tangent=float3(1,0,0);tangent=normalize(tangent);
   float3 bitangent=normalize(cross(n,tangent));n=normalize(n+tangent*(detail.r*2-1)*0.35+bitangent*(detail.g*2-1)*0.35);
   float3 view=normalize(_WorldSpaceCameraPos-i.world),light=normalize(float3(-0.4,0.8,-0.45));
   float wrapped=saturate((dot(n,light)+0.5)/1.5),rim=pow(1-saturate(abs(dot(n,view))),3);
   float roughness=lerp(0.45,0.80,detail.a);
   float spec=pow(saturate(dot(n,normalize(light+view))),lerp(42,12,roughness))*lerp(0.20,0.08,roughness);
   // Interior G: median .004, p75 .051, p90 .169. Preserve its authored clustered edges.
   float strand=abs(frac(i.uv.y*2.30-i.uv.x-_Phase*0.16+flow.y*0.035)-0.5);
   float spiral=1-smoothstep(0.13,0.27,strand);
   float4 foamLayer=tex2D(_BodyTex,bodyUV+float2(0.29,0.055));
   float clusters=smoothstep(0.018,0.15,body.g);
   float secondary=smoothstep(0.024,0.17,foamLayer.g);
   float foam=saturate(spiral*(0.15+0.85*max(clusters,secondary*0.78))+clusters*0.18);
   foam=lerp(foam,smoothstep(0.02,0.15,top.g),pole);
   float density=smoothstep(0.27,0.73,body.r);
   float ringStrength=ringLobes(theta);float4 foamData=body;
   if(ring)
   {
    float2 fallUV=float2(i.uv.x*12+0.025*sin(i.uv.y*7+theta*7),0.14+0.70*frac(i.uv.y*0.8-_BurstAge*1.8+0.13*sin(theta*13)));
    foamData=tex2D(_BodyTex,fallUV);
    float rivulet=pow(0.5+0.5*sin(theta*47+flow.x*4+i.uv.y*2),5);
    float leadingCrest=exp(-pow((i.uv.y-0.45)/0.24,2));
    float nearSide=smoothstep(-0.15,0.55,-sin(theta));
    float jetClusters=max(smoothstep(0.016,0.135,foamData.g),smoothstep(0.025,0.16,body.g)*0.62);
    foam=saturate(jetClusters*(0.40+0.90*leadingCrest)+leadingCrest*0.20*(0.45+0.55*nearSide)+rivulet*0.12);
    density=smoothstep(0.27,0.73,foamData.r);
   }
   // Steel-blue thin water and deep absorption grooves bracket the cold-white foam.
   float3 color=lerp(float3(0.40,0.49,0.56),float3(0.105,0.15,0.19),density);
   float groove=(1-spiral)*density*(1-ring);
   color*=(0.70+0.45*wrapped)*(1-0.20*groove);
   float transmission=(1-density)*(0.40+0.60*wrapped)*(0.10+0.20*body.b);
   color=lerp(color,float3(0.569,0.663,0.718),transmission);
   color+=float3(0.73,0.80,0.85)*(spec*(0.20+0.80*body.b)*(1-density*0.45)+rim*0.025);
   color=lerp(color,float3(0.91,0.945,0.97),foam);
   color+=float3(0.953,0.973,1)*foam*0.11;
   float coreCharge=_Charge*ease((_Collapse-0.75)/0.25)*(1-ring);
   if(_BurstAge>=0)coreCharge=(1-ease(_BurstAge/0.065))*(1-ring);
   color=lerp(color,float3(0.953,0.973,1),coreCharge*0.85);color+=coreCharge*float3(0.85,0.90,1);
   float localFlash=saturate(1-distance(i.world,_LightningPosition.xyz)/max(0.001,_LightningRadius));
   color+=float3(0.90,0.94,1)*_LightningEnergy*localFlash*localFlash*0.22;
   float alpha=(facing>0?0.24+0.08*density+foam*0.30:0.40+0.16*density+foam*0.13)*_Alpha;
   alpha=lerp(alpha,0.88,coreCharge);
   float rearContact=(facing<0?1:0)*(1-smoothstep(_Ground,_Ground+_Height*1.10,i.local.y))*(1-cap)*(1-ring)*(1-_Collapse);
   alpha*=1-rearContact*0.98;
   if(cap)alpha*=lerp(0.40,1,_Collapse);
   if(_BurstAge>=0&&!ring)alpha*=1-ease(_BurstAge/0.065);
   if(ring)
   {
    float release=ease((_BurstAge-0.04)/0.10),late=ease((_BurstAge-0.24)/0.18);
    float tongues=smoothstep(0.19,0.55,ringStrength),bridges=0.30*(1-release);
    float survival=1-ease((_BurstAge-0.22-0.12*ringStrength)/0.26);
    alpha=(0.50+foam*0.35)*max(bridges,tongues)*saturate(_BurstAge/0.045)*survival;
    float brokenFlow=0.5+0.22*sin(theta*31+flow.x*3+i.uv.y*0.7)+0.17*sin(theta*53+1.7)+0.11*sin(theta*17+i.uv.y*7);
    float outerEdge=0.53+0.39*ringStrength+0.13*(foamData.b-0.5)+0.06*sin(theta*23),innerEdge=0.02+late*(0.15+0.10*(1-ringStrength));
    alpha*=smoothstep(innerEdge,innerEdge+0.10,i.uv.y)*(1-smoothstep(outerEdge-0.12,outerEdge,i.uv.y));
    alpha*=0.55+0.45*saturate(foamData.r+foamData.g*2);
    alpha*=lerp(1,smoothstep(0.25,0.60,brokenFlow),release*smoothstep(0.30,0.85,i.uv.y));
    alpha*=lerp(1,smoothstep(0.32,0.67,brokenFlow),late);
    alpha*=1-0.72*pow(max(0,cos(theta)),6);
   }
   return float4(color * alpha,alpha);
  }
  ENDCG
  Pass
  {
   Cull Front
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
  ENDCG
  }
  Pass
  {
   Cull Back
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
  ENDCG
  }
 }
 Fallback Off
}
