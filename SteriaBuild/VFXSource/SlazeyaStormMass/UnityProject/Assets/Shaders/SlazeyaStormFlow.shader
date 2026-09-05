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
  struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 patch:TEXCOORD1;};
  struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float4 patch:TEXCOORD3;float3 local:TEXCOORD4;};
  float ease(float x){x=saturate(x);return x*x*(3-2*x);}
  float outCurve(float x){x=saturate(x);return 1-(1-x)*(1-x);}
  float3 ringNormal(float theta){return normalize(float3(cos(theta)/_Radii.x,0,sin(theta)/_Radii.z));}
  float3 ringTangent(float theta){return normalize(float3(-_Radii.x*sin(theta),0,_Radii.z*cos(theta)));}
  float2 bandSection(){return _Radii.xz*sqrt(max(0,1-(_Band/_Radii.y)*(_Band/_Radii.y)));}
  float ringLobe(float theta,float center,float width)
  {
   float distance=atan2(sin(theta-center),cos(theta-center));return exp(-distance*distance/(width*width));
  }
  float ringLobes(float theta)
  {
   return max(ringLobe(theta,2.25,0.48),max(0.90*ringLobe(theta,3.72,0.38),max(0.80*ringLobe(theta,5.10,0.46),0.60*ringLobe(theta,0.70,0.32))));
  }
  float3 RingPoint(float2 uv)
  {
   float theta=uv.x*6.283185307,q=uv.y,progress=outCurve(_BurstAge/0.25);
   float strength=ringLobes(theta),fall=max(0,_BurstAge-0.12);
   float side=1-0.68*pow(max(0,cos(theta)),6);
   float width=0.10+0.39*strength+0.02*sin(theta*11);
   float2 radius=bandSection()*(1+progress*(0.025+q*width)*side+fall*0.10*side);
   float crest=0.18*strength*sin(q*3.14159265);
   float y=_Band+_Height*progress*(crest-0.53*pow(q,1.25)-0.11*(1-strength));
   y-=_Height*(fall*fall*(2.8+2*q)+ease((_BurstAge-0.27)/0.30)*0.16);
   y+=_Height*0.030*progress*sin(theta*23+q*9-_BurstAge*12)*sin(q*3.14159265);
   float angle=theta+progress*q*(0.035+0.065*strength);
   return float3(radius.x*cos(angle),max(_Ground+0.015*_Height,y),radius.y*sin(angle));
  }
  float3 BubblePoint(float2 uv,float4 patch)
  {
   if(patch.w>1.5)return RingPoint(uv);
   float theta=uv.x*6.283185307,q=uv.y,scale=1.14-0.14*ease((_Gather*1.35-1.02)/0.21);
   float ymin=_Ground/(_Radii.y*scale),phi0=asin(clamp(ymin,-0.999,0.999)),phi=lerp(phi0,1.570796327,q);
   float warp=1+0.018*(0.5+0.5*sin(theta*5+_Phase*2)*cos(phi*3+_Phase))*pow(sin(3.14159265*q),2);
   float3 p;
   if(patch.w>0.5)p=float3(_Radii.x*scale*sqrt(1-ymin*ymin)*q*cos(theta),_Ground,_Radii.z*scale*sqrt(1-ymin*ymin)*q*sin(theta));
   else p=float3(_Radii.x*scale*cos(phi)*cos(theta)*warp,_Radii.y*scale*sin(phi)*warp,_Radii.z*scale*cos(phi)*sin(theta)*warp);
   if(_BurstAge>=0)
   {
    float sector=patch.x,low=patch.y,high=patch.z,region=(low+high)*0.5;
    float progress=outCurve((_BurstAge-(0.012+0.022*(0.5+0.5*sin(sector*9))))/0.20);
    float3 n=ringNormal(sector),t=ringTangent(sector);float2 band=bandSection();
    float3 hinge=float3(band.x*cos(sector),_Band,band.y*sin(sector)),d=p-hinge;
    float radial=dot(d,n),vertical=d.y;
    float bend=progress*(patch.w>0.5?0.1:0.55+0.80*region)*ease((q-low)/max(0.001,high-low));
    float newRadial=radial*cos(bend)+vertical*sin(bend);
    p+=n*(newRadial-radial)*0.45;
    p.y=patch.w>0.5?_Ground:hinge.y+vertical*cos(bend)+radial*sin(bend)*(_Height/((band.x+band.y)*0.5))*0.25;
    p+=n*(band.x+band.y)*0.5*(patch.w>0.5?0.44:0.34+region*0.28)*progress+t*_Height*0.14*progress;
    p.y-=_Height*2.8*pow(max(0,_BurstAge-0.16),2);
   }
   return p;
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
   float2 bodyUV=float2(i.uv.x*2-_Phase*0.22,lerp(0.10,0.86,i.uv.y))+flow*float2(0.04,0.025);
   float4 body=tex2D(_BodyTex,bodyUV),detail=tex2D(_NormalRoughness,bodyUV);
   // Blend to pole-safe XZ sampling. Authored A/gutter NEVER controls closed-body coverage.
   float pole=smoothstep(0.88,0.985,i.uv.y)*(1-cap)*(1-ring);
   float4 top=tex2D(_BodyTex,i.local.xz/_Radii.xz*0.45+0.5+float2(_Phase*0.025,0));body=lerp(body,top,pole);
   float3 n=normalize(i.normal);if(facing<0)n=-n;
   float3 tangent=cross(float3(0,1,0),n);if(dot(tangent,tangent)<0.001)tangent=float3(1,0,0);tangent=normalize(tangent);
   float3 bitangent=normalize(cross(n,tangent));n=normalize(n+tangent*(detail.r*2-1)*0.38+bitangent*(detail.g*2-1)*0.38);
   float3 view=normalize(_WorldSpaceCameraPos-i.world);float rim=pow(1-saturate(abs(dot(n,view))),2.5);
   float wrapped=saturate((dot(n,normalize(float3(-0.4,0.8,-0.45)))+0.5)/1.5);
   float relativeHeight=(i.local.y-_Band)/_Height;
   // Inverse material transport: upper and lower foam blocks move into the same latitude.
   float contraction=lerp(1,0.12,_FoamGather),transport=relativeHeight/contraction;
   float2 foamUV=float2(i.uv.x*3+transport*0.16-_Phase*0.28,0.54+0.34*(0.5+0.5*sin(transport*1.3+theta+_Phase*0.35)));
   if(ring)foamUV=float2(i.uv.x*4+i.uv.y*0.35-_Phase*0.65,0.53+0.35*(0.5+0.5*sin(i.uv.y*5-_BurstAge*8+theta)));
   float4 foamData=tex2D(_BodyTex,foamUV+flow*0.018);
   float spiralDistance=abs(frac(i.uv.x*2-transport*0.34-_Phase*0.20+foamData.r*0.06)-0.5);
   float spiralWindow=1-smoothstep(0.12,0.33,spiralDistance);
   float foam=pow(saturate(foamData.g*3.5+foamData.r*0.08),0.65)*(0.35+0.65*spiralWindow);
   float belt=exp(-pow(relativeHeight/lerp(1.4,0.13,_FoamGather),2));
   foam*=lerp(1,belt*1.9+0.12,_FoamGather);
   foam=lerp(foam,pow(saturate(top.g*3.5+top.r*0.13),0.65),pole*(1-_FoamGather));
   float frontLow=(facing>0?1:0)*smoothstep(-0.10,0.35,-i.local.z/_Radii.z)*(1-smoothstep(_Ground+_Height*0.30,_Band+_Height*0.15,i.local.y))*(1-cap)*(1-ring);
   float2 lowerUV=float2(i.uv.x*3+transport*0.12-_Phase*0.40,0.56+0.30*(0.5+0.5*sin(transport*0.7+theta*2)));
   float4 lowerData=tex2D(_BodyTex,lowerUV+flow*0.025);
   float lowerFoam=pow(saturate(lowerData.g*3.4+lowerData.r*0.12),0.65)*frontLow*(1-_FoamGather*0.65);
   foam=max(foam,lowerFoam*0.80);
   float churn=0.5+0.25*sin(theta*31+transport*19+flow.x*3)+0.25*sin(theta*57-transport*23+flow.y*4);
   foam*=0.48+0.65*smoothstep(0.30,0.76,churn);
   float ringStrength=ringLobes(theta);
   if(ring)
   {
    // Streamwise sampling follows the rolled lip into the falling wall, with irregular whitewater tongues.
    float2 fallUV=float2(i.uv.x*12+0.025*sin(i.uv.y*7+theta*7),0.14+0.70*frac(i.uv.y*0.8-_BurstAge*1.8+0.13*sin(theta*13)));
    foamData=tex2D(_BodyTex,fallUV);
    float leadingCrest=exp(-pow((i.uv.y-0.57)/0.24,2));
    float rivulet=pow(0.5+0.5*sin(theta*47+flow.x*4+i.uv.y*2),3);
    foam=saturate(pow(saturate(foamData.g*4+foamData.r*0.20),0.62)*(0.70+leadingCrest*0.7)+rivulet*0.22);
   }
   float3 color=lerp(float3(0.055,0.29,0.42),float3(0.20,0.65,0.79),0.35+body.r*0.50);
   color*=0.78+0.40*wrapped;color+=float3(0.16,0.44,0.52)*rim*0.20;
   // Local transmitted fill joins the near lower film to the lit side walls. Geometry is unchanged.
   color=lerp(color,float3(0.31,0.68,0.75)*(0.80+lowerData.r*0.30),frontLow*0.82);
   color=lerp(color,float3(0.74,0.94,0.97),saturate(foam)*0.91);
   color+=float3(0.34,0.62,0.68)*foam*0.24;
   // A short broad white-cyan pressure peak within dense foam sectors, still local to this surface.
   float chargePatch=_BurstAge<0?_Charge*exp(-pow(relativeHeight/0.28,2))*(0.15+0.85*smoothstep(0.12,0.75,ringStrength))*(0.60+0.40*churn):0;
   color+=float3(1.20,1.45,1.50)*chargePatch;
   float flash=_BurstAge>=0?1-saturate(_BurstAge/0.08):0;
   color+=float3(0.52,0.80,0.85)*flash*exp(-relativeHeight*relativeHeight/0.025)*0.65;
   float localFlash=saturate(1-distance(i.world,_LightningPosition.xyz)/max(0.001,_LightningRadius));color+=float3(0.46,0.87,0.93)*_LightningEnergy*localFlash*localFlash*0.28;
   float alpha=(facing>0?0.25+0.07*body.r+rim*0.08+foam*0.12+frontLow*0.22:0.37+body.r*0.15+foam*0.10)*_Alpha;
   alpha=saturate(alpha+chargePatch*0.35);
   // The stage depth clips the rear wall at ground; fade that wall before the intersection,
   // while the continuous near film carries the lower volume without a dark elliptical cut.
   float rearContact=(facing<0?1:0)*(1-smoothstep(_Ground,_Ground+_Height*1.10,i.local.y))*(1-cap)*(1-ring);
   alpha*=1-rearContact*0.98;
   if(cap)alpha*=0.40;
   if(_BurstAge>=0&&!ring)
   {
    float sectorFrac=frac(i.uv.x*14+0.08*sin(i.uv.y*27+theta*3)+0.00001),latitude=(i.uv.y-i.patch.y)/max(0.001,i.patch.z-i.patch.y);
    float ragged=0.04+0.12*(1-body.b);
    float patchEdge=smoothstep(ragged,ragged+0.09,sectorFrac)*(1-smoothstep(1-ragged-0.09,1-ragged,sectorFrac));
    patchEdge*=smoothstep(ragged,ragged+0.12,latitude)*(1-smoothstep(1-ragged-0.12,1-ragged,latitude));
    float erosion=saturate((_BurstAge-0.015)/0.20)+i.uv.y*0.12;
    float tear=1-smoothstep(body.b*0.50+0.15,body.b*0.50+0.30,erosion);
    alpha*=lerp(1,patchEdge*sqrt(body.a),_Rupture)*tear;
    // The geometrically peeling sectors retain transported whitewater, not smooth glass panels.
    float waterThreads=saturate(foam*1.25+body.g*1.4)*sqrt(saturate(body.b));
    alpha*=lerp(1,waterThreads,ease(_BurstAge/0.033));
   }
   if(ring)
   {
    float release=ease((_BurstAge-0.04)/0.10),late=ease((_BurstAge-0.24)/0.18);
    float tongues=smoothstep(0.19,0.55,ringStrength);
    // Weak bridges tear through their full radial width; by B+.23 the high wall cannot close a ring.
    float bridges=0.30*(1-release);
    float survival=1-ease((_BurstAge-0.22-0.12*ringStrength)/0.26);
    alpha=(0.48+foam*0.27)*max(bridges,tongues)*saturate(_BurstAge/0.045)*survival;
    float brokenFlow=0.5+0.22*sin(theta*31+flow.x*3+i.uv.y*0.7)+0.17*sin(theta*53+1.7)+0.11*sin(theta*17+i.uv.y*7);
    float outerEdge=0.53+0.39*ringStrength+0.13*(foamData.b-0.5)+0.06*sin(theta*23);
    float innerEdge=0.02+late*(0.15+0.10*(1-ringStrength));
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
