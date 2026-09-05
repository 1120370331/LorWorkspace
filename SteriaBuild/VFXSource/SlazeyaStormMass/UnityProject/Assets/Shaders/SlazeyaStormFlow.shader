Shader "Steria/SlazeyaStormFlow"
{
    Properties
    {
        _BodyTex("Density / Foam / Tear / Coverage",2D)="white"{}
        _NormalRoughness("Tangent normal / Roughness",2D)="bump"{}
        _FlowTex("Flow RG / Height B",2D)="gray"{}
        _Deep("Absorption",Color)=(0.031,0.184,0.275,1)
        _Mid("Water curtain",Color)=(0.086,0.478,0.631,1)
        _Crest("Lit water lip",Color)=(0.604,0.906,0.922,1)
        _LightDirection("Effect-owned broad key",Vector)=(-0.38,0.78,-0.49,0)
        _Phase("Explicit material clock",Float)=0
        _ShapePhase("Captured crest momentum",Float)=0
        _Gather("Roll into pressure pose",Float)=0
        _Overturn("Turn old lip outward",Float)=0
        _Fall("Gravity after crown",Float)=0
        _Dissolve("Directional tears",Float)=0
        _Flash("Local impact crest",Float)=0
        _Alpha("Lifetime opacity",Float)=0
        _Inflow("Low water tongues",Float)=0
        _LightningPosition("Local discharge world point",Vector)=(0,0,0,0)
        _LightningRadius("Local light radius",Float)=1
        _LightningEnergy("Two finite pulses",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True"}
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
            sampler2D _BodyTex,_NormalRoughness,_FlowTex;
            float4 _Deep,_Mid,_Crest,_LightDirection;
            float4 _LightningPosition;
            float _LightningRadius,_LightningEnergy;
            float _Phase,_ShapePhase,_Gather,_Overturn,_Fall,_Dissolve,_Flash,_Alpha,_Inflow;
            struct appdata {
                float4 vertex:POSITION; float3 baseDs:NORMAL; float4 baseDq:TANGENT;
                float2 uv:TEXCOORD0; float3 curled:TEXCOORD1; float3 overturned:TEXCOORD2;
                float3 gatherDs:TEXCOORD3; float3 gatherDq:TEXCOORD4; float3 overturnDs:TEXCOORD5; float3 overturnDq:TEXCOORD6;
            };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 worldDs:TEXCOORD2; float3 worldDq:TEXCOORD3; };
            float ease(float x) {x=saturate(x);return x*x*(3-2*x);}
            float easeDerivative(float x) {return x>0&&x<1?6*x*(1-x):0;}
            v2f vert(appdata v)
            {
                v2f o;
                float s=v.uv.x,q=v.uv.y;
                float gatherInput=(_Gather-0.10*q-0.13*(1-s))/0.72;
                float g=ease(gatherInput),dg=easeDerivative(gatherInput);
                float overturnInput=_Overturn*(0.80+0.20*q),overturn=ease(overturnInput);
                float3 gathered=lerp(v.vertex.xyz,v.curled,g);
                float3 gatherDs=lerp(v.baseDs,v.gatherDs,g)+(v.curled-v.vertex.xyz)*dg*(0.13/0.72);
                float3 gatherDq=lerp(v.baseDq.xyz,v.gatherDq,g)+(v.curled-v.vertex.xyz)*dg*(-0.10/0.72);
                float3 ds=lerp(gatherDs,v.overturnDs,overturn);
                float3 dq=lerp(gatherDq,v.overturnDq,overturn)+(v.overturned-gathered)*easeDerivative(overturnInput)*_Overturn*0.20;
                float3 p=lerp(gathered,v.overturned,overturn);
                p.y-=_Fall*_Fall*(1.65+0.5*q);dq.y-=_Fall*_Fall*0.5;
                if(p.y<0.018){p.y=0.018;ds.y=0;dq.y=0;}
                float angle=_ShapePhase*(0.45+0.55*q);
                float sn=sin(angle),cs=cos(angle);
                float2 rotationDerivative=float2(-p.x*sn-p.z*cs,p.x*cs-p.z*sn)*_ShapePhase*0.55;
                ds.xz=float2(ds.x*cs-ds.z*sn,ds.x*sn+ds.z*cs);
                dq.xz=float2(dq.x*cs-dq.z*sn,dq.x*sn+dq.z*cs)+rotationDerivative;
                p.xz=float2(p.x*cs-p.z*sn,p.x*sn+p.z*cs);
                o.world=mul(unity_ObjectToWorld,float4(p,1)).xyz;
                o.worldDs=mul((float3x3)unity_ObjectToWorld,ds);
                o.worldDq=mul((float3x3)unity_ObjectToWorld,dq);
                o.position=UnityWorldToClipPos(o.world);
                o.uv=v.uv;
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float s=i.uv.x,q=i.uv.y;
                float2 flow=tex2D(_FlowTex,float2(s-_Phase*0.06,q)).rg*2-1;
                // Keep the physical hooked tip within the authored coverage field. The geometric
                // endpoint still feathers; PNG's empty top gutter must not erase the entire return curl.
                float2 uv=float2(s*1.3-_Phase*0.24,lerp(0.045,0.87,q))+flow*float2(0.045,0.018);
                float4 body=tex2D(_BodyTex,uv);
                float4 detail=tex2D(_NormalRoughness,uv);
                // Smooth derivatives include nonuniform scaling, pose weights, lip delay, shear and gravity.
                // They describe the deformed geometry, not a static base pose or faceted pixel derivative.
                float3 tangent=normalize(i.worldDs);
                float3 bitangent=normalize(i.worldDq);
                float3 normal=normalize(cross(tangent,bitangent));
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                if(dot(normal,view)<0) normal=-normal;
                tangent=normalize(tangent-normal*dot(normal,tangent));
                bitangent=normalize(cross(normal,tangent));
                float3 micro=detail.rgb*2-1;
                micro.xy*=0.22;
                normal=normalize(normal*max(0.6,micro.z)+tangent*micro.x+bitangent*micro.y);
                float3 light=normalize(_LightDirection.xyz);
                float wrapped=saturate((dot(normal,light)+0.32)/1.32);
                float upward=saturate(normal.y*0.5+0.5);
                float thickness=saturate(body.r*0.28+(1-q)*0.54);
                float lipShade=lerp(0.40,1,saturate(upward*0.8+wrapped*0.55));
                float3 color=lerp(_Mid.rgb,_Deep.rgb,thickness*0.66);
                color*=0.68+wrapped*0.52+upward*0.16;
                color*=lerp(lipShade,1,_Inflow);
                float fresnel=pow(1-saturate(dot(normal,view)),3);
                color+=_Crest.rgb*fresnel*(0.04+q*0.09)*(1-body.r*0.5);
                float roughness=clamp(detail.a,0.35,0.92);
                float specular=pow(saturate(dot(normal,normalize(light+view))),lerp(44,9,roughness));
                color+=_Crest.rgb*specular*(0.12+body.g*0.12)*(1-roughness*0.55);
                float lip=smoothstep(0.48,0.85,q);
                float foam=saturate(pow(max(0,body.g),0.62)*1.8)*(0.15+0.85*lip);
                color=lerp(color,_Crest.rgb,foam*0.88);
                color=lerp(color,float3(0.906,1,1),foam*_Flash*0.65);
                // The blue belly survives the flash. No fullscreen exposure or uniform white rim.
                color+=_Crest.rgb*_Flash*lip*(0.16+0.30*foam);
                float localFlash=saturate(1-distance(i.world,_LightningPosition.xyz)/max(0.001,_LightningRadius));
                color+=float3(0.46,0.87,0.93)*_LightningEnergy*localFlash*localFlash*0.28;
                float ends=smoothstep(0,0.04,s)*(1-smoothstep(0.91,1,s));
                float foot=smoothstep(0,0.045,q);
                float hookedTip=1-smoothstep(0.97,1,q);
                float tear=1-smoothstep(body.b*0.64+0.20,body.b*0.64+0.33,_Dissolve+q*0.18*_Dissolve);
                float alpha=_Alpha*ends*foot*hookedTip*body.a*(0.79+body.r*0.18)*tear;
                return float4(color * alpha,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
