Shader "Steria/PlasmaLightningSlashFlow"
{
    Properties
    {
        _SlashTex ("Procedural RGBA Slash Mask", 2D) = "white" {}
        _NoiseTex ("Directional Plasma Noise", 2D) = "white" {}
        _TintColor ("Tint", Color) = (1,1,1,1)
        _CoreColor ("Hot Core", Color) = (1.000,0.973,1.000,1)
        _BodyColor ("Plasma Body", Color) = (0.776,0.467,1.000,1)
        _EdgeColor ("Electric Edge", Color) = (0.482,0.086,0.788,1)
        _Reveal ("Reveal Head", Float) = 0
        _Retreat ("Retreat Tail", Float) = -0.08
        _Alpha ("Alpha", Float) = 1
        _Intensity ("Intensity", Float) = 1.5
        _Phase ("Deterministic Flow Phase", Float) = 0
        _EdgeFade ("Thickness Edge Fade", Vector) = (0.055,0.075,0,0)
        _AbrasionStrength ("Abrasion Strength", Range(0,1)) = 0.15
        _FiberStrength ("Fiber Strength", Range(0,1)) = 0.48
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
            float4 _BodyColor;
            float4 _EdgeColor;
            float _Reveal;
            float _Retreat;
            float _Alpha;
            float _Intensity;
            float _Phase;
            float4 _EdgeFade;
            float _AbrasionStrength;
            float _FiberStrength;

            // Palette markers: _FFF8FF hot core, _C677FF plasma body, _7B16C9 electric edge.
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
                float revealMask = 1.0 - smoothstep(_Reveal - 0.022, _Reveal + 0.034, u);
                float retreatMask = smoothstep(_Retreat - 0.030, _Retreat + 0.040, u);
                float clampMask = step(0.0001, u) * step(u, 0.9999);
                float thicknessFade = smoothstep(0.0, _EdgeFade.x, v)
                    * (1.0 - smoothstep(1.0 - _EdgeFade.y, 1.0, v));
                float visible = revealMask * retreatMask * clampMask;

                float4 slash = tex2D(_SlashTex, float2(u, v));
                float2 flowUv = float2(u * 2.15 - _Phase * 0.16, v * 1.35 + _Phase * 0.030);
                float broadNoise = tex2D(_NoiseTex, flowUv).r;
                float pathStreak = tex2D(_NoiseTex, float2(u * 1.55 - _Phase * 0.22, v * 0.92 + 0.04)).g;
                float sparkNoise = tex2D(_NoiseTex, float2(u * 7.4 + _Phase * 0.19, v * 2.4 - 0.13)).b;

                float widthCoord = abs(v - 0.5) * 2.0;
                float edgeInstability = (broadNoise - 0.5) * 0.06;
                float edgeEnvelope = 1.0 - smoothstep(
                    0.94 + edgeInstability,
                    1.01 + edgeInstability,
                    widthCoord);
                float envelope = slash.a * thicknessFade * edgeEnvelope;
                float connectedCore = slash.r * pow(saturate(1.0 - abs(v - 0.50) * 3.6), 1.35);
                float fiberMask = slash.g * (1.0 - smoothstep(0.27, 0.34, u));
                float abrasion = slash.b * _AbrasionStrength;
                float edgeBand = envelope * pow(saturate(widthCoord), 2.2);
                float directionalFlow = saturate(0.60 + broadNoise * 0.22 + pathStreak * 0.18);
                float bodyEnergy = envelope * max(
                    0.60,
                    directionalFlow * (1.0 - abrasion * 0.18));

                float leadingHead = (1.0 - smoothstep(0.0, 0.034, abs(u - _Reveal)))
                    * pow(saturate(1.0 - abs(v - 0.50) * 4.8), 1.8)
                    * envelope;
                float fiberEnergy = fiberMask * (0.52 + pathStreak * 0.42 + sparkNoise * 0.06) * _FiberStrength;

                float3 color = _BodyColor.rgb * bodyEnergy * 0.76;
                color += _EdgeColor.rgb * edgeBand * (0.72 + sparkNoise * 0.25);
                color += _BodyColor.rgb * fiberEnergy * 0.78;
                color += _CoreColor.rgb * connectedCore * 1.14;
                color += _CoreColor.rgb * leadingHead * 1.50;
                color *= _TintColor.rgb * _Intensity;

                float alpha = bodyEnergy * 0.63
                    + edgeBand * 0.22
                    + connectedCore * 0.52
                    + fiberEnergy * 0.30
                    + leadingHead * 0.72;
                alpha = saturate(alpha) * visible * _Alpha * _TintColor.a * i.color.a;
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    Fallback "Legacy Shaders/Particles/Additive"
}
