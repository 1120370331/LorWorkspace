Shader "Steria/SlazeyaStormLightning"
{
    Properties
    {
        _Pulse("Explicit finite discharge",Float)=0
        _Core("Near-white core",Color)=(0.953,0.973,1,1)
        _Edge("Cold steel feather",Color)=(0.659,0.749,0.820,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent+1" "RenderType"="Transparent"}
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Pulse;float4 _Core,_Edge;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            float4 frag(v2f i):SV_Target
            {
                float width=abs(i.uv.y-0.5)*2;
                float core=1-smoothstep(0.12,0.34,width),edge=pow(saturate(1-width),2);
                float ends=smoothstep(0,0.06,i.uv.x)*(1-smoothstep(0.88,1,i.uv.x));
                float3 color=lerp(_Edge.rgb,_Core.rgb,core);
                return float4(color*(core*0.85+edge*0.25)*ends*_Pulse,0);
            }
            ENDCG
        }
    }
    Fallback Off
}
