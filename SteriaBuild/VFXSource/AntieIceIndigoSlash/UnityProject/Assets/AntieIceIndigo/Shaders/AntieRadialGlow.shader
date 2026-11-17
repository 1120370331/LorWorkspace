Shader "Steria/Antie/SoftRadialGlow"
{
    Properties{_MaskTex("User radial grayscale mask",2D)="black"{} _Color("Colour",Color)=(.18,.72,1,1) _Alpha("Alpha",Range(0,1))=0}
    SubShader
    {
        Tags{"Queue"="Transparent-10" "RenderType"="Transparent"}
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MaskTex;float4 _Color;float _Alpha;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            half4 frag(v2f i):SV_Target{return half4(_Color.rgb,tex2D(_MaskTex,i.uv).r*_Alpha);}
            ENDCG
        }
    }
    Fallback Off
}
