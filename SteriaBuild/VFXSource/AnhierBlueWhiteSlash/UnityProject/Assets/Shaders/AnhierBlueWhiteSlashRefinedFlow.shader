Shader "Steria/AnhierBlueWhiteSlashRefinedFlow"
{
    Properties
    {
        _SlashTex ("Slash Clamp Mask", 2D) = "white" {}
        _NoiseTex ("Breakup Noise", 2D) = "white" {}
        _TintColor ("Tint", Color) = (1,1,1,1)
        _CoreColor ("Core Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0.1,0.7,1,1)
        _Reveal ("Reveal Head", Float) = 0
        _Retreat ("Retreat Tail", Float) = -0.1
        _Length ("Visible Length", Float) = 0.7
        _Alpha ("Alpha", Float) = 1
        _Intensity ("Intensity", Float) = 2.2
        _EdgeFade ("Thickness Edge Fade", Vector) = (0.08,0.10,0,0)
        _NoiseStrength ("Noise Strength", Float) = 0.32
        _BrushStrength ("Brush Strength", Float) = 0.36
        _FlowSpeed ("Flow Speed", Float) = 1.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha One
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

            sampler2D _SlashTex;
            sampler2D _NoiseTex;
            float4 _TintColor;
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Reveal;
            float _Retreat;
            float _Length;
            float _Alpha;
            float _Intensity;
            float4 _EdgeFade;
            float _NoiseStrength;
            float _BrushStrength;
            float _FlowSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
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
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float u = saturate(i.uv.x);
                float v = saturate(i.uv.y);
                float head = _Reveal;
                float tail = max(_Retreat, head - max(_Length, 0.02));
                float revealMask = smoothstep(tail, tail + 0.045, u) * (1.0 - smoothstep(head - 0.030, head + 0.026, u));
                float clampMask = step(0.0001, u) * step(u, 0.9999);
                float edgeFade = smoothstep(0.0, _EdgeFade.x, v) * (1.0 - smoothstep(1.0 - _EdgeFade.y, 1.0, v));
                float endFade = smoothstep(0.0, 0.025, u) * (1.0 - smoothstep(0.970, 1.0, u));

                float4 slash = tex2D(_SlashTex, float2(u, v));
                float2 noiseUv = float2(u * 2.35 - _Time.y * _FlowSpeed, v * 1.15 + _Time.y * 0.17);
                float noise = tex2D(_NoiseTex, noiseUv).r;
                float streak = tex2D(_NoiseTex, float2(u * 7.5 - _Time.y * _FlowSpeed * 1.8, v * 0.72 + 0.31)).g;
                float core = pow(saturate(1.0 - abs(v - 0.52) * 3.35), 2.65);
                float rim = pow(saturate(1.0 - abs(v - 0.76) * 5.8), 1.9);
                float leading = (1.0 - smoothstep(0.0, 0.034, abs(u - head))) * pow(saturate(1.0 - abs(v - 0.58) * 3.4), 1.8);
                float brush = lerp(1.0 - _BrushStrength, 1.18, slash.r);
                float breakup = lerp(1.0 - _NoiseStrength, 1.16, noise) + streak * 0.16;
                float innerBreak = lerp(0.54, 1.0, saturate(noise * 0.72 + slash.b * 0.54 + streak * 0.30));

                float alpha = slash.a * revealMask * edgeFade * endFade * clampMask;
                alpha *= saturate(brush * breakup);
                alpha *= lerp(1.0, innerBreak, core * 0.82);
                alpha = saturate(alpha + leading * slash.a * edgeFade * 0.26);

                float3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(core + slash.r * 0.55 + leading * 0.50));
                color = lerp(color, float3(0.10, 0.58, 1.0), rim * 0.24);
                color *= _TintColor.rgb * _Intensity * (0.74 + noise * 0.22 + streak * 0.18 + leading * 0.46);

                return fixed4(color, alpha * _Alpha * _TintColor.a * i.color.a);
            }
            ENDCG
        }
    }

    Fallback "Legacy Shaders/Particles/Additive"
}
