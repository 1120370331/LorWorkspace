Shader "Skill/SwordSlashFlowBuiltIn"
{
    Properties
    {
        _MainTex ("Slash Mask", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "white" {}
        _TintColor ("Tint", Color) = (1,1,1,1)
        _CoreColor ("Core Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0.1,0.75,1,1)
        _Reveal ("Reveal", Float) = 0
        _Length ("Length", Float) = 0.42
        _Intensity ("Intensity", Float) = 2.4
        _Alpha ("Alpha", Float) = 1
        _NoiseTiling ("Noise Tiling", Vector) = (1,1,0,0)
        _NoiseSpeed ("Noise Speed", Float) = 1.8
        _EdgeFade ("Edge Fade", Vector) = (2.5,2.0,0,0)
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

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _TintColor;
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Reveal;
            float _Length;
            float _Intensity;
            float _Alpha;
            float4 _NoiseTiling;
            float _NoiseSpeed;
            float4 _EdgeFade;

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
                float lengthValue = max(_Length, 0.001);
                float revealOffset = lerp(-lengthValue, 1.0, saturate(_Reveal));
                float slashX = (i.uv.x - revealOffset) / lengthValue;
                float inRange = step(0.0, slashX) * step(slashX, 1.0);

                float2 slashUv = float2(slashX, i.uv.y);
                float mask = tex2D(_MainTex, slashUv).a * inRange;

                float2 centered = abs(i.uv - 0.5) * _EdgeFade.xy;
                float edgeFade = saturate(1.0 - centered.x) * saturate(1.0 - centered.y);
                float center = pow(saturate(1.0 - abs(i.uv.y - 0.5) * 2.0), 1.9);

                float2 noiseUv = i.uv * _NoiseTiling.xy + float2(-_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.21);
                float noise = tex2D(_NoiseTex, noiseUv).r;
                float waterLines = pow(saturate(sin((i.uv.x - _Time.y * 0.65) * 44.0 + i.uv.y * 9.0) * 0.5 + 0.5), 5.0);
                float leadingEdge = 1.0 - smoothstep(0.0, 0.055, abs(i.uv.x - saturate(_Reveal)));

                float alpha = saturate(mask * edgeFade * (0.62 + noise * 0.55 + waterLines * 0.20) + leadingEdge * mask * 0.34);
                float3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(center + leadingEdge * 0.5));
                color *= _TintColor.rgb * _Intensity * (0.72 + noise * 0.28 + waterLines * 0.26 + leadingEdge * 0.65);

                return fixed4(color, alpha * _Alpha * _TintColor.a * i.color.a);
            }
            ENDCG
        }
    }

    Fallback "Legacy Shaders/Particles/Additive"
}
