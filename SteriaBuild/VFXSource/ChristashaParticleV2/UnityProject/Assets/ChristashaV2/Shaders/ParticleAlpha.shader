Shader "Steria/Particles/UnlitAlphaV2"
{
    Properties { _MainTex ("Original particle brush", 2D) = "white" {} _Tint ("Instance tint",Color)=(1,1,1,1) _Stroke("Directional retreat",Float)=0 _Guard("Symmetric guard retreat",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float3 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float3 uv:TEXCOORD0; };
            sampler2D _MainTex;
            fixed4 _Tint;
            float _Stroke;
            float _Guard;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv.xy)*i.color*_Tint;
                float cut=saturate((i.uv.z-0.36)/0.62);
                float path=lerp(i.uv.x,1-abs(i.uv.x*2-1),_Guard);
                c.a*=lerp(1,smoothstep(cut-0.08,cut+0.035,path),_Stroke);
                return c;
            }
            ENDCG
        }
    }
}
