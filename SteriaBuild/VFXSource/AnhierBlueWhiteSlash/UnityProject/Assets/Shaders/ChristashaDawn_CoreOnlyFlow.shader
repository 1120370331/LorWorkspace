
Shader "Steria/ChristashaDawnCoreOnlyFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FlowTex ("Flow Mask (CC0)", 2D) = "gray" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _HdrEmission ("HDR Emission", Color) = (1,1,1,1)
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _DebugForceVisible ("Debug Force Visible", Float) = 0.0
        _Reveal ("Reveal", Float) = 1.0
        _Retreat ("Retreat", Float) = -0.08
        _UseParticleAge ("Use Particle Age Alpha", Float) = 1.0
        _RevealStartAge ("Reveal Start Age", Float) = 0.024
        _RevealEndAge ("Reveal End Age", Float) = 0.439
        _RetreatStartAge ("Retreat Start Age", Float) = 0.561
        _RetreatEndAge ("Retreat End Age", Float) = 0.976
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.030
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _CoreFillStrength ("Core Fill Strength", Float) = 0.80
        _CoreWidthScale ("Core Width Scale", Float) = 1.50
        _CoreFillTail ("Core Fill Tail", Float) = 0.28
        _DistortStrength ("Distort Strength", Float) = 0.018
        _DistortScale ("Distort Scale", Float) = 22.0
        _DistortSpeed ("Distort Speed", Float) = 1.75
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _FlowScale ("Flow Scale", Float) = 28.0
        _FlowTexStrength ("Flow Texture Strength", Float) = 0.28
        _FlowTexDistortStrength ("Flow Texture Distort Strength", Float) = 0.020
        _FlowTexTiling ("Flow Texture Tiling", Float) = 2.4
        _BrushCoreStrength ("Brush Core Strength", Float) = 1.0
        _BrushFiberStrength ("Brush Fiber Strength", Float) = 0.44
        _BrushAbrasionStrength ("Brush Abrasion Strength", Float) = 0.16
        _SoftEnvelopeStrength ("Soft Envelope Strength", Float) = 0.30
        _LuminousBodyStrength ("Luminous Body Strength", Float) = 0.68
        _CenterPlateauWidth ("Center Plateau Width", Float) = 0.46
        _CenterFalloffPower ("Center Falloff Power", Float) = 1.70
        _GoldBandCenter ("Gold Band Center", Float) = 0.24
        _GoldBandWidth ("Gold Band Width", Float) = 0.22
        _GoldBandStrength ("Gold Band Strength", Float) = 0.34
        _GoldBandAlpha ("Gold Band Alpha", Float) = 0.22
        _OuterGoldStrength ("Outer Gold Strength", Float) = 0.30
        _OuterGoldContact ("Outer Gold Contact", Float) = 0.08
        _OuterGoldFalloff ("Outer Gold Falloff", Float) = 0.055
        _OuterGoldEdgeWidth ("Outer Gold Edge Width", Float) = 0.12
        _OuterGoldEdgeSoftness ("Outer Gold Edge Softness", Float) = 0.06
        _OuterGoldEdgeStrength ("Outer Gold Edge Strength", Float) = 0.45
        _OuterGoldEdgeAlpha ("Outer Gold Edge Alpha", Float) = 0.06
        _InnerGoldEdgeWidth ("Inner Gold Edge Width", Float) = 0.12
        _InnerGoldEdgeSoftness ("Inner Gold Edge Softness", Float) = 0.06
        _InnerGoldEdgeStrength ("Inner Gold Edge Strength", Float) = 0.30
        _InnerGoldEdgeAlpha ("Inner Gold Edge Alpha", Float) = 0.04
        _InnerAfterglowStart ("Inner Afterglow Start", Float) = 0.198
        _InnerAfterglowEnd ("Inner Afterglow End", Float) = 0.842
        _InnerAfterglowCenter ("Inner Afterglow Center", Float) = 0.52
        _InnerAfterglowCrossCenter ("Inner Afterglow Cross Center", Float) = 0.50
        _InnerAfterglowWidth ("Inner Afterglow Width", Float) = 0.11
        _InnerAfterglowBrightness ("Inner Afterglow Brightness", Float) = 1.08
        _InnerAfterglowRetreatDelay ("Inner Afterglow Retreat Delay", Float) = 0.04
        _DiagnosticMode ("Diagnostic Mode", Float) = 0.0
        _SweepArcDegrees ("Sweep Arc Degrees", Float) = -18.0
        _SweepTrailLength ("Sweep Trail Length", Float) = 0.74
        _SweepTailThinness ("Sweep Tail Thinness", Float) = 0.72
        _TailSharpenStart ("Tail Sharpen Start", Float) = 0.36
        _TailSharpenPower ("Tail Sharpen Power", Float) = 1.75
        _TailExtinctionBoost ("Tail Extinction Boost", Float) = 0.62
        _TrailBodyAlpha ("Trail Body Alpha", Float) = 0.72
        _TrailRidgeStrength ("Trail Ridge Strength", Float) = 0.46
        _TrailRidgeWidth ("Trail Ridge Width", Float) = 0.14
        _TrailFlowBandStrength ("Trail Flow Band Strength", Float) = 0.24
        _SweepMotionStrength ("Sweep Motion Strength", Float) = 0.0
        _SweepPivot ("Sweep Pivot", Vector) = (-1.10,3.10,0,0)
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
            float _CoreWidthScale;
            float _CoreFillTail;
            float _DistortStrength;
            float _DistortScale;
            float _DistortSpeed;
            float _FlowSpeed;
            float _FlowScale;
            float _FlowTexStrength;
            float _FlowTexDistortStrength;
            float _FlowTexTiling;
            float _BrushCoreStrength;
            float _BrushFiberStrength;
            float _BrushAbrasionStrength;
            float _SoftEnvelopeStrength;
            float _LuminousBodyStrength;
            float _CenterPlateauWidth;
            float _CenterFalloffPower;
            float _GoldBandCenter;
            float _GoldBandWidth;
            float _GoldBandStrength;
            float _GoldBandAlpha;
            float _OuterGoldStrength;
            float _OuterGoldContact;
            float _OuterGoldFalloff;
            float _OuterGoldEdgeWidth;
            float _OuterGoldEdgeSoftness;
            float _OuterGoldEdgeStrength;
            float _OuterGoldEdgeAlpha;
            float _InnerGoldEdgeWidth;
            float _InnerGoldEdgeSoftness;
            float _InnerGoldEdgeStrength;
            float _InnerGoldEdgeAlpha;
            float _InnerAfterglowStart;
            float _InnerAfterglowEnd;
            float _InnerAfterglowCenter;
            float _InnerAfterglowCrossCenter;
            float _InnerAfterglowWidth;
            float _InnerAfterglowBrightness;
            float _InnerAfterglowRetreatDelay;
            float _DiagnosticMode;
            float _SweepArcDegrees;
            float _SweepTrailLength;
            float _SweepTailThinness;
            float _TailSharpenStart;
            float _TailSharpenPower;
            float _TailExtinctionBoost;
            float _TrailBodyAlpha;
            float _TrailRidgeStrength;
            float _TrailRidgeWidth;
            float _TrailFlowBandStrength;
            float _SweepMotionStrength;
            float4 _SweepPivot;

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
                float useControlUv = saturate(_UseControlUV);
                float pathT = lerp(1.0 - v.texcoord.y, saturate(v.texcoord1.x), useControlUv);
                float useParticleAge = saturate(_UseParticleAge);
                float particleAge = saturate(v.color.a);
                float revealT = saturate((particleAge - _RevealStartAge) / max(_RevealEndAge - _RevealStartAge, 0.001));
                revealT = 1.0 - pow(1.0 - revealT, 3.0);
                float revealControl = lerp(_Reveal, revealT, useParticleAge);
                float ageBehindHead = max(0.0, revealControl - pathT);
                float sweepAge = saturate(ageBehindHead / max(_SweepTrailLength, 0.001));
                float sweepGate = 1.0 - pow(1.0 - saturate(revealControl * 1.18), 2.0);
                float sweepRotation = radians(_SweepArcDegrees) * _SweepMotionStrength * sweepGate * (0.28 + sweepAge * 0.72);
                float2 sweepLocal = v.vertex.xy - _SweepPivot.xy;
                float sweepSin = sin(sweepRotation);
                float sweepCos = cos(sweepRotation);
                v.vertex.xy = float2(sweepLocal.x * sweepCos - sweepLocal.y * sweepSin, sweepLocal.x * sweepSin + sweepLocal.y * sweepCos) + _SweepPivot.xy;
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
                float retreatThreshold = retreatProgress * (1.0 + edge) - edge;
                float pathRetreatFade = smoothstep(retreatThreshold, retreatThreshold + edge, pathT);
                float revealMask = 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float sharedVisibility = saturate(revealMask * pathRetreatFade);

                float centerDistance = abs(outerToInner - 0.50) * 2.0;
                float plateauEdge = saturate(_CenterPlateauWidth);
                float corePlateau = 1.0 - smoothstep(plateauEdge, 1.0, centerDistance);
                corePlateau = pow(saturate(corePlateau), max(_CenterFalloffPower, 0.01));
                float goldBand = 1.0 - smoothstep(_GoldBandWidth * 0.62, _GoldBandWidth, abs(outerToInner - _GoldBandCenter));
                float goldBandEnergy = saturate(goldBand * _GoldBandStrength);
                float3 goldBandColor = lerp(float3(0.88, 0.67, 0.24), float3(1.08, 0.96, 0.62), goldBand);

                float brushDrift = sin(pathT * 10.0 - _Time.y * _FlowSpeed * 0.72 + outerToInner * 4.0) * 0.012;
                float maskPath = saturate(pathT);
                float2 maskUv = float2(
                    saturate(outerToInner + brushDrift),
                    maskPath);
                float4 brushTex = tex2D(_FlowTex, maskUv);
                float brushCoreBase = brushTex.r;
                float coreSampleU = saturate(0.5 + (saturate(outerToInner + brushDrift) - 0.5) / max(_CoreWidthScale, 0.001));
                float widenedBrushCore = tex2D(_FlowTex, float2(coreSampleU, maskPath)).r;
                float brushCore = saturate(max(brushCoreBase * 0.35, widenedBrushCore) * _BrushCoreStrength);
                float goldFiber = saturate(brushTex.g * _BrushFiberStrength);
                float abrasionCut = saturate(brushTex.b * _BrushAbrasionStrength);
                float softEnvelope = saturate(brushTex.a * _SoftEnvelopeStrength);
                float goldFiberPresence = goldFiber * goldBand;
                float coreHotspotWidth = saturate(0.30 * _CoreWidthScale);
                float tipCoreFade = 1.0 - smoothstep(0.94, 0.995, pathT);
                float coreHotspot = 1.0 - smoothstep(coreHotspotWidth, min(coreHotspotWidth + 0.10, 1.0), centerDistance);
                coreHotspot *= 0.72 + brushCore * 0.28;
                coreHotspot *= tipCoreFade;
                float leadingPressure = smoothstep(0.18, 0.74, pathT) * (1.0 - smoothstep(0.90, 1.0, pathT));
                float luminousBody = saturate(
                    coreHotspot
                    * (0.64 + brushCore * 0.24 + softEnvelope * 0.12)
                    * (0.90 + leadingPressure * 0.10));
                luminousBody *= 1.0 - abrasionCut * 0.04;

                float2 flowUv = i.uv;
                float brushSigned = saturate(brushCore * 0.46 + goldFiberPresence * 0.54) * 2.0 - 1.0;
                flowUv.x += brushSigned * _FlowTexDistortStrength * (0.16 + corePlateau * 0.28);
                flowUv.y += brushSigned * _FlowTexDistortStrength * 0.08;

                float4 tex = tex2D(_MainTex, flowUv);
                float bodyContinuity = saturate(softEnvelope * 0.50 + corePlateau * 0.08);
                float coreFill = saturate((coreHotspot * 0.52 + brushCore * 0.48) * _CoreFillStrength);
                coreFill *= tipCoreFade;

                float afterglowCenter = saturate(_InnerAfterglowCenter);
                float afterglowHalfLength = max(min(afterglowCenter - _InnerAfterglowStart, _InnerAfterglowEnd - afterglowCenter), 0.001);
                float afterglowStart = afterglowCenter - afterglowHalfLength;
                float afterglowEnd = afterglowCenter + afterglowHalfLength;
                float afterglowFeather = max(afterglowHalfLength * 0.32, 0.001);
                float afterglowPathMask = smoothstep(afterglowStart, afterglowStart + afterglowFeather, pathT)
                    * (1.0 - smoothstep(afterglowEnd - afterglowFeather, afterglowEnd, pathT));
                float afterglowHalfWidth = max(_InnerAfterglowWidth * 0.5, 0.001);
                float afterglowCrossDistance = abs(outerToInner - _InnerAfterglowCrossCenter);
                float afterglowCrossMask = 1.0 - smoothstep(afterglowHalfWidth * 0.72, afterglowHalfWidth, afterglowCrossDistance);
                float afterglowRetreatProgress = saturate((retreatProgress - _InnerAfterglowRetreatDelay) / max(1.0 - _InnerAfterglowRetreatDelay, 0.001));
                float afterglowExtinction = afterglowRetreatProgress * afterglowRetreatProgress * (3.0 - 2.0 * afterglowRetreatProgress);
                float afterglowAlphaFade = pow(saturate(1.0 - afterglowExtinction), 1.8);
                float afterglowRetreatEnergy = lerp(1.0, 0.46, smoothstep(0.0, 0.45, retreatProgress));
                float afterglowMask = afterglowPathMask * afterglowCrossMask * afterglowAlphaFade * afterglowRetreatEnergy;
                float3 afterglowColor = float3(_InnerAfterglowBrightness, _InnerAfterglowBrightness, _InnerAfterglowBrightness);
                float brightnessWeight = saturate(coreFill * 0.72 + goldFiberPresence * 0.18 + bodyContinuity * 0.10);
                float extinctionRate = lerp(1.12, 1.06, brightnessWeight);
                float extinctionInput = saturate(retreatProgress * extinctionRate);
                float extinction = extinctionInput * extinctionInput * (3.0 - 2.0 * extinctionInput);
                float alphaFade = pow(saturate(1.0 - extinction), 1.8);
                float finalPathVisibility = sharedVisibility * alphaFade;

                if (_DebugForceVisible > 0.5)
                {
                    float previewBodyAlpha = saturate(
                        bodyContinuity * 0.19
                        + softEnvelope * 0.17
                        + luminousBody * 0.60
                        + coreFill * 0.66);
                    float previewAttachedGoldAlphaScale = 1.0 + goldFiberPresence * 0.10 + goldBand * _GoldBandAlpha;
                    float previewAlpha = saturate(previewBodyAlpha * previewAttachedGoldAlphaScale);
                    previewAlpha = smoothstep(0.12, 0.40, previewAlpha);
                    float3 previewColor = float3(0.94, 0.91, 0.82);
                    previewColor = lerp(previewColor, float3(1.00, 0.98, 0.86), saturate(luminousBody * _LuminousBodyStrength));
                    previewColor = lerp(previewColor, float3(1.10, 1.08, 0.96), coreFill);
                    previewColor = lerp(previewColor, goldBandColor, goldBandEnergy);
                    previewColor = lerp(previewColor, afterglowColor, saturate(afterglowMask * 0.72));
                    return float4(previewColor * _HdrEmission.rgb * max(_EmissionBoost, 1.0), saturate((previewAlpha + afterglowMask * 0.18) * finalPathVisibility));
                }

                fixed4 particleColor = lerp(fixed4(1.0, 1.0, 1.0, 1.0), i.color, useParticleAge);
                particleColor.a = lerp(1.0, i.color.a, useParticleAge);
                float4 c = tex * particleColor;
                float3 whiteCoreColor = float3(1.10, 1.08, 0.96);
                float3 neutralBodyColor = float3(0.94, 0.91, 0.82);
                float3 brushColor = neutralBodyColor;
                float3 luminousBodyColor = lerp(float3(1.00, 0.98, 0.86), whiteCoreColor, saturate(0.58 + luminousBody * 0.42));
                brushColor = lerp(brushColor, luminousBodyColor, saturate(luminousBody * _LuminousBodyStrength));
                brushColor = lerp(brushColor, whiteCoreColor, coreFill);
                brushColor += goldFiberPresence * float3(0.035, 0.025, 0.008);
                brushColor = lerp(brushColor, goldBandColor, goldBandEnergy);
                brushColor = lerp(brushColor, afterglowColor, saturate(afterglowMask * 0.72));
                brushColor *= 1.0 - abrasionCut * 0.04;
                c.rgb = c.rgb * brushColor * _TintColor.rgb * _HdrEmission.rgb * _EmissionBoost;

                float bodyAlphaProfile = saturate(
                    bodyContinuity * 0.19
                    + softEnvelope * 0.17
                    + luminousBody * 0.60
                    + coreFill * 0.66);
                float attachedGoldAlphaScale = 1.0 + goldFiberPresence * 0.10 + goldBand * _GoldBandAlpha;
                float alphaProfile = saturate(bodyAlphaProfile * attachedGoldAlphaScale);
                alphaProfile *= 1.0 - abrasionCut * 0.04;
                alphaProfile = smoothstep(0.12, 0.40, alphaProfile);

                if (_DiagnosticMode > 2.5)
                {
                    return float4(afterglowColor * _HdrEmission.rgb * max(_EmissionBoost, 1.0), afterglowMask * 0.82 * finalPathVisibility);
                }
                if (_DiagnosticMode > 1.5)
                {
                    float goldDiagnosticAlpha = finalPathVisibility * saturate(goldBand * 0.58 + goldFiberPresence * 0.42);
                    float3 goldDiagnosticColor = goldBandColor;
                    return float4(goldDiagnosticColor * _HdrEmission.rgb * max(_EmissionBoost, 1.0), goldDiagnosticAlpha);
                }
                if (_DiagnosticMode > 0.5)
                {
                    float bodyLocalAlpha = saturate(bodyContinuity * 0.19 + softEnvelope * 0.17 + luminousBody * 0.60 + coreFill * 0.66);
                    bodyLocalAlpha = smoothstep(0.12, 0.40, bodyLocalAlpha);
                    float bodyDiagnosticAlpha = bodyLocalAlpha * finalPathVisibility;
                    float3 bodyDiagnosticColor = lerp(neutralBodyColor, whiteCoreColor, saturate(luminousBody * 0.68 + coreFill));
                    return float4(bodyDiagnosticColor * _HdrEmission.rgb * max(_EmissionBoost, 1.0), bodyDiagnosticAlpha);
                }

                c.rgb *= lerp(0.01, 1.0, alphaFade);
                float3 afterglowRgb = afterglowColor * _TintColor.rgb * _HdrEmission.rgb * _EmissionBoost;
                c.rgb = lerp(c.rgb, afterglowRgb, saturate(afterglowMask * 0.82));
                c.a = saturate((c.a * alphaProfile + afterglowMask * 0.18) * finalPathVisibility);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Alpha Blended"
}
