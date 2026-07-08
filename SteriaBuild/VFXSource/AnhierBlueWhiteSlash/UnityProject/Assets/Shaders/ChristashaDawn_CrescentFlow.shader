
Shader "Steria/ChristashaDawnCrescentFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _HdrEmission ("HDR Emission", Color) = (1,1,1,1)
        _BladeInnerColor ("Blade Inner Color", Color) = (1,0.92,0.42,1)
        _BladeOuterColor ("Blade Outer Color", Color) = (1,0.72,0.05,1)
        _BladeGradientStrength ("Blade Gradient Strength", Float) = 0.0
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _EdgeWidth ("Edge Width", Float) = 0.08
        _CenterPlateauWidth ("Center Plateau Width", Float) = 0.34
        _CenterFalloffPower ("Center Falloff Power", Float) = 2.8
        _Reveal ("Reveal", Float) = 1.0
        _Retreat ("Retreat", Float) = -0.08
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.075
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _CoreFillStrength ("Core Fill Strength", Float) = 0.0
        _CoreFillTail ("Core Fill Tail", Float) = 0.28
        _DistortStrength ("Distort Strength", Float) = 0.018
        _DistortScale ("Distort Scale", Float) = 22.0
        _DistortSpeed ("Distort Speed", Float) = 1.75
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
            float4 _TintColor;
            float4 _HdrEmission;
            float4 _BladeInnerColor;
            float4 _BladeOuterColor;
            float _BladeGradientStrength;
            float _EmissionBoost;
            float _EdgeWidth;
            float _CenterPlateauWidth;
            float _CenterFalloffPower;
            float _Reveal;
            float _Retreat;
            float _RevealEdgeWidth;
            float _UseControlUV;
            float _CoreFillStrength;
            float _CoreFillTail;
            float _DistortStrength;
            float _DistortScale;
            float _DistortSpeed;
            float _FlowSpeed;
            float _FlowScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 controlUv : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.controlUv = v.texcoord1;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float useControlUv = saturate(_UseControlUV);
                float pathT = lerp(1.0 - i.uv.y, saturate(i.controlUv.x), useControlUv);
                float edge = max(_RevealEdgeWidth, 0.001);
                float outerToInner = lerp(saturate(i.uv.x), saturate(i.controlUv.y), useControlUv);
                float retreatProgress = saturate(_Retreat);
                float retreatThreshold = 1.0 - retreatProgress * (1.0 + edge);
                float revealMask = 1.0 - smoothstep(_Reveal, _Reveal + edge, pathT);
                float retreatMask = 1.0 - smoothstep(retreatThreshold, retreatThreshold + edge, outerToInner);
                float revealEdge = 1.0 - smoothstep(0.0, edge, abs(pathT - _Reveal));
                float retreatEdge = 1.0 - smoothstep(0.0, edge, abs(outerToInner - retreatThreshold));
                float visible = saturate(revealMask * retreatMask);
                float sideMask = 1.0 - smoothstep(0.0, _EdgeWidth, i.uv.x) * (1.0 - smoothstep(1.0 - _EdgeWidth, 1.0, i.uv.x));
                float centerMask = 1.0 - saturate(abs(i.uv.x - 0.50) * 2.0);
                float centerDistance = abs(i.uv.x - 0.50) * 2.0;
                float plateauEdge = saturate(_CenterPlateauWidth);
                float trapezoidProfile = 1.0 - smoothstep(plateauEdge, 1.0, centerDistance);
                trapezoidProfile = pow(saturate(trapezoidProfile), max(_CenterFalloffPower, 0.01));
                float3 trapezoidEmission = _HdrEmission.rgb;
                float3 bladeGradient = lerp(_BladeOuterColor.rgb, _BladeInnerColor.rgb, saturate(i.uv.x));
                trapezoidEmission = lerp(trapezoidEmission, bladeGradient, saturate(_BladeGradientStrength));
                float coreMask = pow(saturate(centerMask), 1.65) * smoothstep(0.02, 0.18, pathT) * (1.0 - smoothstep(0.90, 1.0, pathT));
                float freshCore = 1.0 - smoothstep(max(_Reveal - _CoreFillTail, 0.0), _Reveal, pathT);
                freshCore *= 1.0 - smoothstep(_Reveal, _Reveal + edge, pathT);
                float coreFill = visible * coreMask * saturate(0.34 + freshCore * 0.82) * _CoreFillStrength;
                float2 flowUv = i.uv;
                float distortA = sin(pathT * _DistortScale + _Time.y * _DistortSpeed + i.uv.x * 5.0);
                float distortB = sin(pathT * _DistortScale * 0.37 - _Time.y * _DistortSpeed * 0.70 + i.uv.x * 8.0);
                float distort = distortA * distortB;
                flowUv.x += distort * _DistortStrength * (0.18 + centerMask * 0.82);
                flowUv.y += distort * _DistortStrength * 0.22;
                float4 tex = tex2D(_MainTex, flowUv);
                float flow = pow(saturate(sin((pathT + _Time.y * _FlowSpeed) * _FlowScale) * 0.5 + 0.5), 3.0);
                float energyFlow = pow(saturate(sin((pathT - _Time.y * _FlowSpeed * 0.82) * _FlowScale + flowUv.x * 7.0) * 0.5 + 0.5), 4.0) * 0.65
                    + pow(saturate(sin((pathT * 1.7 + _Time.y * _FlowSpeed * 0.55) * _FlowScale * 0.47 + flowUv.x * 11.0) * 0.5 + 0.5), 6.0) * 0.35;
                float spark = pow(saturate(sin(pathT * 61.0 - i.uv.x * 17.0 + _Time.y * _FlowSpeed * 5.7) * 0.5 + 0.5), 7.0);
                float4 c = tex * _TintColor * i.color;
                c.rgb *= trapezoidEmission * _EmissionBoost * (0.70 + trapezoidProfile * 0.22 + flow * 0.12 + energyFlow * 0.18 + sideMask * 0.06 + spark * 0.04 + revealEdge * 0.46 + retreatEdge * 0.10);
                c.rgb = lerp(c.rgb, float3(1.0, 1.0, 1.0) * _HdrEmission.rgb * _EmissionBoost * (0.76 + energyFlow * 0.18), saturate(coreFill));
                c.a *= visible * saturate(0.82 + flow * 0.10 + energyFlow * 0.14 + sideMask * 0.05 + revealEdge * 0.20 + coreFill * 0.30);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Alpha Blended"
}
