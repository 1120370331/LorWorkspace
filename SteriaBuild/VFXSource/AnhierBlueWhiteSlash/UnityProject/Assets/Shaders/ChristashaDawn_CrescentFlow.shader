
Shader "Steria/ChristashaDawnCrescentFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _EdgeWidth ("Edge Width", Float) = 0.08
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _FlowScale ("Flow Scale", Float) = 28.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float _EmissionBoost;
            float _EdgeWidth;
            float _FlowSpeed;
            float _FlowScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float sideMask = 1.0 - smoothstep(0.0, _EdgeWidth, i.uv.x) * (1.0 - smoothstep(1.0 - _EdgeWidth, 1.0, i.uv.x));
                float flow = pow(saturate(sin((i.uv.y + _Time.y * _FlowSpeed) * _FlowScale) * 0.5 + 0.5), 3.0);
                float spark = pow(saturate(sin(i.uv.y * 61.0 - i.uv.x * 17.0 + _Time.y * _FlowSpeed * 5.7) * 0.5 + 0.5), 7.0);
                fixed4 c = tex * _TintColor * i.color;
                c.rgb *= _EmissionBoost * (1.0 + flow * 0.16 + sideMask * 0.26 + spark * 0.10);
                c.a *= saturate(0.70 + flow * 0.18 + sideMask * 0.12);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Alpha Blended"
}
