Shader "Steria/SlazeyaStormParticles"
{
    Properties
    {
        _Atlas("Native lifetime atlas",2D)="white"{}
        _Grid("Columns rows animated mist",Vector)=(4,4,1,0)
        _Alpha("Opacity",Float)=0
        _DetailOpacity("Material detail contribution",Range(0,1))=1
        _Phase("Explicit clock",Float)=0
        _DebugAge("Builder-only native stream probe",Float)=0
        _SheetBreakup("Open asymmetric spray membranes",Float)=0
        _MistDark("Mist absorption",Color)=(0.208,0.275,0.322,1)
        _MistLight("Mist broad illumination",Color)=(0.604,0.671,0.722,1)
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
            sampler2D _Atlas;
            float4 _Grid;
            float4 _MistDark,_MistLight;
            float _Alpha,_Phase,_DebugAge,_SheetBreakup,_DetailOpacity;
            // Native streams pack UV.xy, AgePercent.z, StableRandomX.w.
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float4 uvAge:TEXCOORD0;};
            struct v2f {float4 position:SV_POSITION;float4 color:COLOR;float4 uvAge:TEXCOORD0;};
            v2f vert(appdata v) {v2f o;o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uvAge=v.uvAge;return o;}
            float4 atlasFrame(float frame,float2 uv)
            {
                // Input contract: PNG top-left starts frame 0; Unity UV y increases upward.
                float2 cell=float2(fmod(frame,_Grid.x),_Grid.y-1-floor(frame/_Grid.x));
                float2 inset=0.5/256.0;
                float2 tile=lerp(inset,1-inset,saturate(uv));
                return tex2D(_Atlas,(cell+tile)/_Grid.xy);
            }
            float4 frag(v2f i):SV_Target
            {
                if(_DebugAge>0.5) return float4(saturate(i.uvAge.z),saturate(i.uvAge.w),0,1);
                float count=_Grid.x*_Grid.y;
                float frame=_Grid.z>0.5?saturate(i.uvAge.z)*(count-1):min(count-1,floor(i.uvAge.w*count));
                float4 data=lerp(atlasFrame(floor(frame),i.uvAge.xy),atlasFrame(min(count-1,floor(frame)+1),i.uvAge.xy),frac(frame));
                float3 color;
                float alpha;
                if(_Grid.w>0.5)
                {
                    color=lerp(_MistDark.rgb,_MistLight.rgb,saturate(data.g*(1-data.r*0.25)));
                    color*=1-data.b*0.35;
                    // Authored A already contains density^.88 and lifetime. R only shapes lighting.
                    alpha=data.a*_Alpha;
                }
                else
                {
                    color=lerp(float3(0.145,0.204,0.251),float3(0.863,0.898,0.922),saturate(data.g*0.76+data.b*0.23));
                    alpha=data.a*(0.45+0.55*max(data.r,data.g))*_Alpha*i.color.a;
                    // Remove the closed basin silhouette while retaining the authored membrane,
                    // fingers and evolving holes. Different seeded cuts stay coherent over lifetime.
                    float cutHeight=0.20+(1-i.uvAge.x)*lerp(0.07,0.24,i.uvAge.w);
                    float openBase=smoothstep(cutHeight,cutHeight+0.18,i.uvAge.y);
                    float openSide=smoothstep(0.03,0.18,i.uvAge.x+i.uvAge.w*0.13);
                    alpha*=lerp(1,openBase*openSide,_SheetBreakup);
                }
                alpha*=_DetailOpacity;
                return float4(color*alpha,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
