
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
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.075
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _CoreFillStrength ("Core Fill Strength", Float) = 1.0
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
        _BrushFiberStrength ("Brush Fiber Strength", Float) = 0.82
        _BrushAbrasionStrength ("Brush Abrasion Strength", Float) = 0.70
        _SoftEnvelopeStrength ("Soft Envelope Strength", Float) = 0.78
        _LuminousBodyStrength ("Luminous Body Strength", Float) = 0.86
        _CenterPlateauWidth ("Center Plateau Width", Float) = 0.50
        _CenterFalloffPower ("Center Falloff Power", Float) = 2.10
        _OuterGoldStrength ("Outer Gold Strength", Float) = 0.52
        _OuterGoldContact ("Outer Gold Contact", Float) = 0.34
        _OuterGoldFalloff ("Outer Gold Falloff", Float) = 0.34
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
        _SweepMotionStrength ("Sweep Motion Strength", Float) = 0.85
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
            float _OuterGoldStrength;
            float _OuterGoldContact;
            float _OuterGoldFalloff;
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
                float revealMask = 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float revealEdge = 1.0 - smoothstep(0.0, edge, abs(pathT - revealControl));
                float retreatEdge = smoothstep(0.02, 0.24, retreatProgress) * (1.0 - smoothstep(0.76, 1.0, retreatProgress));
                float visible = saturate(revealMask);

                float centerDistance = abs(outerToInner - 0.50) * 2.0;
                float ageBehindHead = max(0.0, revealControl - pathT);
                float sweepAge = saturate(ageBehindHead / max(_SweepTrailLength, 0.001));
                float tailFade = 1.0 - smoothstep(0.70, 1.0, sweepAge);
                float tailSharpen = pow(saturate((sweepAge - _TailSharpenStart) / max(1.0 - _TailSharpenStart, 0.001)), max(_TailSharpenPower, 0.01));
                float tailWidth = lerp(1.0 - saturate(_SweepTailThinness), 1.0, tailFade);
                tailWidth *= lerp(1.0, 0.38, tailSharpen);
                float tailWidthMask = 1.0 - smoothstep(tailWidth, min(tailWidth + edge * 1.35, 1.0), centerDistance);
                visible *= lerp(1.0, tailWidthMask, saturate(_SweepTailThinness));
                float plateauEdge = saturate(_CenterPlateauWidth);
                float corePlateau = 1.0 - smoothstep(plateauEdge, 1.0, centerDistance);
                corePlateau = pow(saturate(corePlateau), max(_CenterFalloffPower, 0.01));
                float ridgeWidth = max(_TrailRidgeWidth, 0.001);
                float outerBladeRidge = 1.0 - smoothstep(0.0, ridgeWidth, abs(outerToInner - 0.16));
                float innerLightRidge = 1.0 - smoothstep(0.0, ridgeWidth * 0.72, abs(outerToInner - 0.54));
                float trailRidge = saturate(outerBladeRidge * 0.72 + innerLightRidge * 0.42 + revealEdge * 0.62);
                float trailBand = pow(saturate(sin((pathT - _Time.y * _FlowSpeed * 1.35) * (_FlowScale * 0.42) + outerToInner * 6.0) * 0.5 + 0.5), 5.0);
                trailBand *= saturate(0.35 + corePlateau * 0.65) * (1.0 - tailSharpen * 0.35);
                float goldContact = saturate(_OuterGoldContact);
                float goldFalloff = max(_OuterGoldFalloff, 0.001);
                float goldOutwardFade = smoothstep(0.0, goldContact, outerToInner);
                float goldInwardFade = 1.0 - smoothstep(goldContact, goldContact + goldFalloff, outerToInner);
                float outerGoldCurve = saturate(goldOutwardFade * goldInwardFade);
                float3 outerGoldColor = lerp(float3(1.02, 0.94, 0.34), float3(1.05, 1.03, 0.80), goldOutwardFade);

                float brushDrift = sin(pathT * 10.0 - _Time.y * _FlowSpeed * 0.72 + outerToInner * 4.0) * 0.012;
                float maskPath = saturate(pathT);
                float2 maskUv = float2(
                    saturate(outerToInner + brushDrift),
                    maskPath);
                float4 brushTex = tex2D(_FlowTex, maskUv);
                float brushCore = saturate(brushTex.r * _BrushCoreStrength);
                float goldFiber = saturate(brushTex.g * _BrushFiberStrength);
                float abrasionCut = saturate(brushTex.b * _BrushAbrasionStrength);
                float softEnvelope = saturate(brushTex.a * _SoftEnvelopeStrength);
                goldFiber *= 0.78 + outerGoldCurve * 0.22;
                float leadingPressure = smoothstep(0.18, 0.74, pathT) * (1.0 - smoothstep(0.90, 1.0, pathT));
                float luminousBody = saturate(
                    corePlateau
                    * (0.34 + softEnvelope * 0.66)
                    * (0.58 + leadingPressure * 0.42));
                luminousBody *= 1.0 - abrasionCut * 0.42;

                float2 flowUv = i.uv;
                float brushSigned = saturate(brushCore * 0.46 + goldFiber * 0.54) * 2.0 - 1.0;
                flowUv.x += brushSigned * _FlowTexDistortStrength * (0.16 + corePlateau * 0.28);
                flowUv.y += brushSigned * _FlowTexDistortStrength * 0.08;

                float4 tex = tex2D(_MainTex, flowUv);
                float freshCore = 1.0 - smoothstep(max(revealControl - _CoreFillTail, 0.0), revealControl, pathT);
                freshCore *= 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float bodyContinuity = saturate(softEnvelope * 0.46 + corePlateau * 0.04 + freshCore * 0.08);
                float coreFill = saturate((brushCore * 0.86 + freshCore * 0.14 + revealEdge * 0.08) * _CoreFillStrength);

                if (_DebugForceVisible > 0.5)
                {
                    float previewAlpha = saturate(bodyContinuity * 0.34 + luminousBody * 0.52 + goldFiber * 0.24 + coreFill * 0.48);
                    float3 previewColor = lerp(float3(0.52, 0.28, 0.035), float3(1.04, 0.86, 0.30), goldFiber);
                    previewColor = lerp(previewColor, float3(1.06, 1.02, 0.72), saturate(luminousBody * _LuminousBodyStrength));
                    previewColor = lerp(previewColor, float3(1.08, 1.06, 0.86), coreFill);
                    return float4(previewColor * _HdrEmission.rgb * max(_EmissionBoost, 1.0), visible * previewAlpha);
                }

                fixed4 particleColor = lerp(fixed4(1.0, 1.0, 1.0, 1.0), i.color, useParticleAge);
                particleColor.a = lerp(1.0, i.color.a, useParticleAge);
                float4 c = tex * particleColor;
                float3 shadowGold = float3(0.16, 0.075, 0.010);
                float3 warmGold = lerp(float3(0.82, 0.54, 0.075), outerGoldColor, outerGoldCurve);
                float3 whiteCoreColor = float3(1.10, 1.07, 0.84);
                float3 brushColor = lerp(shadowGold, warmGold, saturate(bodyContinuity * 0.42 + goldFiber * 0.94));
                float3 luminousBodyColor = lerp(warmGold, whiteCoreColor, saturate(0.44 + luminousBody * 0.46));
                brushColor = lerp(brushColor, luminousBodyColor, saturate(luminousBody * _LuminousBodyStrength));
                brushColor = lerp(brushColor, whiteCoreColor, coreFill);
                brushColor += goldFiber * float3(0.26, 0.18, 0.025);
                brushColor *= 1.0 - abrasionCut * 0.72;
                brushColor += revealEdge * float3(0.18, 0.15, 0.07);
                c.rgb = c.rgb * brushColor * _TintColor.rgb * _HdrEmission.rgb * _EmissionBoost;

                float alphaProfile = saturate(
                    bodyContinuity * 0.12
                    + luminousBody * 0.52
                    + goldFiber * 0.40
                    + coreFill * 0.62
                    + revealEdge * 0.10
                    + trailRidge * 0.05
                    + retreatEdge * 0.02);
                alphaProfile *= 1.0 - abrasionCut * 0.72;
                alphaProfile *= lerp(0.86, 1.0, tailFade);
                float brightnessWeight = saturate(coreFill * 0.72 + goldFiber * 0.18 + bodyContinuity * 0.10);
                float tailExtinction = tailSharpen * saturate(_TailExtinctionBoost);
                float extinctionInput = saturate(retreatProgress * lerp(1.85 + tailExtinction, 0.78 + tailExtinction * 0.35, brightnessWeight) + tailExtinction * 0.28);
                float extinction = extinctionInput * extinctionInput * (3.0 - 2.0 * extinctionInput);
                float alphaFade = saturate(1.0 - extinction);
                c.rgb *= lerp(0.35, 1.0, alphaFade);
                c.a *= visible * alphaProfile * alphaFade;
                return c;
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Alpha Blended"
}
