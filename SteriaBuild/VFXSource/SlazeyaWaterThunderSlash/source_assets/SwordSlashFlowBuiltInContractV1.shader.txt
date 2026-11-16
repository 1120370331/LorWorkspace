// Sword Slash Contract v1: Unity 2019.3 Built-in, transparent unlit.
Shader "Skill/SwordSlashFlowBuiltInContractV1"
{
    Properties
    {
        _ShapeTex ("Shape (Clamp, Linear)", 2D) = "white" {}
        _MaskTex ("Mask (Clamp, Linear)", 2D) = "white" {}

        _Reveal ("Reveal", Range(0, 1)) = 1
        _Retreat ("Retreat", Range(0, 1)) = 0
        _RevealSoftness ("Reveal Softness", Range(0.001, 0.25)) = 0.035
        _Phase ("Normalized Phase", Float) = 0

        _HighlightTex ("Highlight (Repeat, Linear)", 2D) = "white" {}
        _HighlightTiling ("Highlight Tiling", Vector) = (1, 1, 0, 0)
        _HighlightSpeed ("Highlight Speed", Vector) = (1, 0, 0, 0)
        _HighlightBandCenter ("Highlight Band Center", Range(0, 1)) = 0.5
        _HighlightBandWidth ("Highlight Band Width", Range(0.001, 1)) = 0.2
        _HighlightBandSoftness ("Highlight Band Softness", Range(0.001, 0.25)) = 0.035
        _HighlightStrength ("Highlight Strength", Float) = 1

        _FlowTex ("Flow (Repeat, Linear)", 2D) = "gray" {}
        _FlowTiling ("Flow Tiling", Vector) = (1, 1, 0, 0)
        _FlowSpeed ("Flow Speed", Vector) = (0.5, 0, 0, 0)
        _FlowStrength ("Flow Strength", Range(0, 0.25)) = 0.02

        _DistortTex ("Distort (Repeat, Linear)", 2D) = "gray" {}
        _DistortTiling ("Distort Tiling", Vector) = (1, 1, 0, 0)
        _DistortSpeed ("Distort Speed", Vector) = (0.25, 0.17, 0, 0)
        _DistortStrength ("Self UV Distort Strength", Range(0, 0.25)) = 0.012

        [HDR] _BaseColor ("Base Color", Color) = (0.15, 0.65, 1, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0.7, 0.95, 1, 1)
        _Emission ("Emission", Float) = 2
        _Alpha ("Alpha", Range(0, 1)) = 1
        _BodyStrength ("Body Strength", Range(0, 1)) = 0.65

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("Z Write", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ShapeTex;
            sampler2D _MaskTex;
            sampler2D _HighlightTex;
            sampler2D _FlowTex;
            sampler2D _DistortTex;

            float _Reveal;
            float _Retreat;
            float _RevealSoftness;
            float _Phase;

            float4 _HighlightTiling;
            float4 _HighlightSpeed;
            float _HighlightBandCenter;
            float _HighlightBandWidth;
            float _HighlightBandSoftness;
            float _HighlightStrength;

            float4 _FlowTiling;
            float4 _FlowSpeed;
            float _FlowStrength;
            float4 _DistortTiling;
            float4 _DistortSpeed;
            float _DistortStrength;

            float4 _BaseColor;
            float4 _EmissionColor;
            float _Emission;
            float _Alpha;
            float _BodyStrength;

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

            half4 frag(v2f i) : SV_Target
            {
                // UV0.x is pathT and UV0.y is acrossBlade (inner=0, outer=1).
                float revealSoftness = max(_RevealSoftness, 0.001);
                float head = 1.0 - smoothstep(saturate(_Reveal), saturate(_Reveal) + revealSoftness, i.uv.x);
                float tail = smoothstep(saturate(_Retreat) - revealSoftness, saturate(_Retreat), i.uv.x);
                float lifetime = head * tail * step(_Retreat, _Reveal + revealSoftness);

                float2 distortUv = i.uv * _DistortTiling.xy + _Phase * _DistortSpeed.xy;
                float2 distort = (tex2D(_DistortTex, distortUv).rg * 2.0 - 1.0) * _DistortStrength;

                // Keep one-shot shape/lifetime edges in undistorted UV0. Distort periodic detail only.
                float shape = tex2D(_ShapeTex, i.uv).a;
                float mask = tex2D(_MaskTex, i.uv).r;

                float2 flowUv = i.uv * _FlowTiling.xy + _Phase * _FlowSpeed.xy;
                float2 flow = (tex2D(_FlowTex, flowUv).rg * 2.0 - 1.0) * _FlowStrength;
                float2 highlightUv = i.uv * _HighlightTiling.xy + _Phase * _HighlightSpeed.xy + flow + distort;
                float highlightTexture = tex2D(_HighlightTex, highlightUv).r;

                float halfWidth = max(_HighlightBandWidth * 0.5, 0.0005);
                float bandDistance = abs(i.uv.y - _HighlightBandCenter);
                float bandSoftness = max(_HighlightBandSoftness, 0.0005);
                float band = 1.0 - smoothstep(max(halfWidth - bandSoftness, 0.0), halfWidth + bandSoftness, bandDistance);
                float highlight = band * highlightTexture * _HighlightStrength;

                float coverage = saturate(shape * mask * lifetime);
                float alpha = coverage * saturate(_BodyStrength + highlight) * _Alpha * _BaseColor.a * i.color.a;
                float3 color = _BaseColor.rgb * max(_BodyStrength, 0.0001);
                color += _EmissionColor.rgb * highlight * _Emission;
                color *= i.color.rgb;
                return half4(color, alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
