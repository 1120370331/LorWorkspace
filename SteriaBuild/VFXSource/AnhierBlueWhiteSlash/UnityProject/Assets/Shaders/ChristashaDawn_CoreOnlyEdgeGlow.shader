
Shader "Steria/ChristashaDawnCoreOnlyEdgeGlow"
{
    Properties
    {
        _MainTex ("Energy Mask", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1.00,0.82,0.50,0.42)
        _HdrEmission ("HDR Emission", Color) = (1.18,1.08,0.72,1)
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _Reveal ("Reveal", Float) = 1.0
        _Retreat ("Retreat", Float) = -0.08
        _UseParticleAge ("Use Particle Age Alpha", Float) = 1.0
        _RevealStartAge ("Reveal Start Age", Float) = 0.024
        _RevealEndAge ("Reveal End Age", Float) = 0.439
        _RetreatStartAge ("Retreat Start Age", Float) = 0.561
        _RetreatEndAge ("Retreat End Age", Float) = 0.976
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.030
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _FlowSpeed ("Flow Speed", Float) = 0.52
        _FlowTexTiling ("Flow Tiling", Float) = 2.6
        _SoftEnvelopeStrength ("Soft Envelope Strength", Float) = 0.60
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend One One
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
            float4 _TintColor;
            float4 _HdrEmission;
            float _EmissionBoost;
            float _Reveal;
            float _Retreat;
            float _UseParticleAge;
            float _RevealStartAge;
            float _RevealEndAge;
            float _RetreatStartAge;
            float _RetreatEndAge;
            float _RevealEdgeWidth;
            float _UseControlUV;
            float _FlowSpeed;
            float _FlowTexTiling;
            float _SoftEnvelopeStrength;

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
                o.uv = v.texcoord;
                o.controlUv = v.texcoord1;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
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
                float extinctionInput = saturate(retreatProgress * 1.08);
                float extinction = extinctionInput * extinctionInput * (3.0 - 2.0 * extinctionInput);
                float alphaFade = pow(saturate(1.0 - extinction), 1.8);
                float finalPathVisibility = sharedVisibility * alphaFade;

                float brushDrift = sin(pathT * 8.0 - _Time.y * _FlowSpeed * 0.56) * 0.008;
                float glowMaskPath = saturate(pathT);
                float2 brushUv = float2(
                    saturate(outerToInner + brushDrift),
                    glowMaskPath);
                float4 brushTex = tex2D(_MainTex, brushUv);
                float softEnvelope = pow(saturate(brushTex.a * _SoftEnvelopeStrength), 0.75);
                float energy = saturate(0.78 + softEnvelope * 0.22);
                float attachedCrossMask = 1.0 - smoothstep(0.03, 0.11, abs(outerToInner - 0.28));
                float glowEnvelope = smoothstep(0.16, 0.58, softEnvelope);

                float localGlowAlpha = _TintColor.a * attachedCrossMask * glowEnvelope * 0.70;
                float3 rgb = _TintColor.rgb * _HdrEmission.rgb * _EmissionBoost * energy;
                rgb *= lerp(0.01, 1.0, alphaFade);
                float3 premultipliedGlow = rgb * localGlowAlpha * finalPathVisibility;
                return float4(premultipliedGlow, 0.0);
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Additive"
}
