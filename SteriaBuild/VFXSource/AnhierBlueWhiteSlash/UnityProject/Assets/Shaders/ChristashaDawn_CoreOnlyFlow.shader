
Shader "Steria/ChristashaDawnCoreOnlyFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FlowTex ("Flow Mask (CC0)", 2D) = "gray" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _HdrEmission ("HDR Emission", Color) = (1,1,1,1)
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _DebugForceVisible ("Debug Force Visible", Float) = 1.0
        _Reveal ("Reveal", Float) = 1.0
        _Retreat ("Retreat", Float) = -0.08
        _UseParticleAge ("Use Particle Age Alpha", Float) = 1.0
        _RevealStartAge ("Reveal Start Age", Float) = 0.024
        _RevealEndAge ("Reveal End Age", Float) = 0.439
        _RetreatStartAge ("Retreat Start Age", Float) = 0.561
        _RetreatEndAge ("Retreat End Age", Float) = 0.976
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.075
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _CoreFillStrength ("Core Fill Strength", Float) = 1.0
        _CoreFillTail ("Core Fill Tail", Float) = 0.28
        _DistortStrength ("Distort Strength", Float) = 0.018
        _DistortScale ("Distort Scale", Float) = 22.0
        _DistortSpeed ("Distort Speed", Float) = 1.75
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _FlowScale ("Flow Scale", Float) = 28.0
        _FlowTexStrength ("Flow Texture Strength", Float) = 0.34
        _FlowTexDistortStrength ("Flow Texture Distort Strength", Float) = 0.016
        _FlowTexTiling ("Flow Texture Tiling", Float) = 2.4
        _CenterPlateauWidth ("Center Plateau Width", Float) = 0.52
        _CenterFalloffPower ("Center Falloff Power", Float) = 2.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZTest Always
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
            sampler2D _FlowTex;
            float4 _TintColor;
            float4 _HdrEmission;
            float _EmissionBoost;
            float _DebugForceVisible;
            float _Reveal;
            float _Retreat;
            float _UseParticleAge;
            float _RevealStartAge;
            float _RevealEndAge;
            float _RetreatStartAge;
            float _RetreatEndAge;
            float _RevealEdgeWidth;
            float _UseControlUV;
            float _CoreFillStrength;
            float _CoreFillTail;
            float _DistortStrength;
            float _DistortScale;
            float _DistortSpeed;
            float _FlowSpeed;
            float _FlowScale;
            float _FlowTexStrength;
            float _FlowTexDistortStrength;
            float _FlowTexTiling;
            float _CenterPlateauWidth;
            float _CenterFalloffPower;

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
                float outerToInner = lerp(saturate(i.uv.x), saturate(i.controlUv.y), useControlUv);
                float edge = max(_RevealEdgeWidth, 0.001);

                float useParticleAge = saturate(_UseParticleAge);
                float particleAge = saturate(i.color.a);
                float revealT = saturate((particleAge - _RevealStartAge) / max(_RevealEndAge - _RevealStartAge, 0.001));
                revealT = 1.0 - pow(1.0 - revealT, 3.0);
                float retreatT = saturate((particleAge - _RetreatStartAge) / max(_RetreatEndAge - _RetreatStartAge, 0.001));
                retreatT = retreatT < 0.5 ? 4.0 * retreatT * retreatT * retreatT : 1.0 - pow(-2.0 * retreatT + 2.0, 3.0) * 0.5;
                float revealControl = lerp(_Reveal, revealT, useParticleAge);
                float retreatControl = lerp(_Retreat, retreatT, useParticleAge);

                float retreatProgress = saturate(retreatControl);
                float retreatThreshold = 1.0 - retreatProgress * (1.0 + edge);
                float revealMask = 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float retreatMask = 1.0 - smoothstep(retreatThreshold, retreatThreshold + edge, outerToInner);
                float revealEdge = 1.0 - smoothstep(0.0, edge, abs(pathT - revealControl));
                float retreatEdge = 1.0 - smoothstep(0.0, edge, abs(outerToInner - retreatThreshold));
                float visible = saturate(revealMask * retreatMask);

                float centerDistance = abs(outerToInner - 0.50) * 2.0;
                float plateauEdge = saturate(_CenterPlateauWidth);
                float corePlateau = 1.0 - smoothstep(plateauEdge, 1.0, centerDistance);
                corePlateau = pow(saturate(corePlateau), max(_CenterFalloffPower, 0.01));

                if (_DebugForceVisible > 0.5)
                {
                    float previewFill = 0.86 + corePlateau * 0.14 + revealEdge * 0.08;
                    return float4(_TintColor.rgb * _HdrEmission.rgb * max(_EmissionBoost, 1.0) * previewFill, visible);
                }

                float2 flowUv = i.uv;
                float distortA = sin(pathT * _DistortScale + _Time.y * _DistortSpeed + outerToInner * 5.0);
                float distortB = sin(pathT * _DistortScale * 0.37 - _Time.y * _DistortSpeed * 0.70 + outerToInner * 8.0);
                float distort = distortA * distortB;
                float2 maskUv = float2(
                    outerToInner * _FlowTexTiling + _Time.y * _FlowSpeed * 0.18 + distort * 0.16,
                    pathT * _FlowTexTiling - _Time.y * _FlowSpeed * 0.55 + distort * 0.10);
                float4 flowTex = tex2D(_FlowTex, maskUv);
                float flowMask = saturate(max(flowTex.a, dot(flowTex.rgb, float3(0.299, 0.587, 0.114))));
                float flowSigned = flowMask * 2.0 - 1.0;
                flowUv.x += distort * _DistortStrength * (0.20 + corePlateau * 0.80);
                flowUv.y += distort * _DistortStrength * 0.22;
                flowUv.x += flowSigned * _FlowTexDistortStrength * (0.30 + corePlateau * 0.70);
                flowUv.y += flowSigned * _FlowTexDistortStrength * 0.28;

                float4 tex = tex2D(_MainTex, flowUv);
                float flowA = pow(saturate(sin((pathT + _Time.y * _FlowSpeed) * _FlowScale) * 0.5 + 0.5), 3.0);
                float flowB = pow(saturate(sin((pathT - _Time.y * _FlowSpeed * 0.82) * _FlowScale + outerToInner * 7.0) * 0.5 + 0.5), 4.0);
                float freshCore = 1.0 - smoothstep(max(revealControl - _CoreFillTail, 0.0), revealControl, pathT);
                freshCore *= 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float flowTextureEnergy = 0.92 + flowMask * 0.22 + flowA * 0.06 + flowB * 0.06;
                float coreFill = saturate((0.68 + corePlateau * 0.32 + freshCore * 0.16 + flowA * 0.05 + flowB * 0.06) * _CoreFillStrength);
                coreFill *= lerp(1.0, flowTextureEnergy, saturate(_FlowTexStrength));

                fixed4 particleColor = lerp(fixed4(1.0, 1.0, 1.0, 1.0), i.color, useParticleAge);
                particleColor.a = lerp(1.0, i.color.a, useParticleAge);
                float4 c = tex * _TintColor * particleColor;
                c.rgb = c.rgb * _HdrEmission.rgb * _EmissionBoost * coreFill;
                c.a *= visible * saturate(0.84 + corePlateau * 0.13 + flowMask * _FlowTexStrength * 0.16 + revealEdge * 0.16 + retreatEdge * 0.04);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Alpha Blended"
}
