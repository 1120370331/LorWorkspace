Shader "Steria/WaterThunder/LightNeedle"
{
    Properties
    {
        _NoiseTex ("Rotated and Stretched Noise", 2D) = "gray" {}
        _LightFlowTex ("High Contrast Streaming UV", 2D) = "gray" {}
        _UVFlowStrength ("UV Flow Contrast", Range(0,2)) = 1
        _ShadowTex ("UV Dark Blue Shadow", 2D) = "black" {}
        _WhiteColor ("White Emission", Color) = (1,1,1,1)
        _ShadowColor ("Dark Blue UV Shade", Color) = (0.025,0.15,0.55,1)
        _Emission ("Emission", Float) = 1.8
        _NeedleTime ("Local Flow Time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+60" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _NoiseTex, _ShadowTex, _LightFlowTex;
            float4 _WhiteColor, _ShadowColor;
            float _Emission, _NeedleTime, _UVFlowStrength;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            half4 frag(v2f i):SV_Target
            {
                float angle=.18+_NeedleTime*.65;
                float2 p=(i.uv-.5)*float2(3.2,.85);
                float2 uv=float2(p.x*cos(angle)-p.y*sin(angle),p.x*sin(angle)+p.y*cos(angle))+float2(-_NeedleTime*2.8,0);
                float n=tex2D(_NoiseTex,uv).r;
                float2 stream=tex2D(_LightFlowTex,float2(i.uv.x*1.2-_NeedleTime*3.8,i.uv.y)).rg;
                float shade=saturate(tex2D(_ShadowTex,i.uv).r+(n-.5)*.22+(stream.g-.5)*.65*_UVFlowStrength);
                float longitudinal=pow(saturate(sin(i.uv.x*3.14159265)),0.72);
                float y=i.uv.y-.5;
                float core=exp(-(y*y)/(.16*.16));
                float halo=exp(-(y*y)/(.25*.25));
                float3 light=lerp(_WhiteColor.rgb*_Emission*(.85+stream.r*.35*_UVFlowStrength),_ShadowColor.rgb,shade)*core*longitudinal;
                float alpha=i.color.a*halo*longitudinal;
                return half4(light*i.color.rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
