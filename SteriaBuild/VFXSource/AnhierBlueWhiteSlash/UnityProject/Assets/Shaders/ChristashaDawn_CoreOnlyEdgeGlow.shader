
Shader "Steria/ChristashaDawnCoreOnlyEdgeGlow"
{
    Properties
    {
        _MainTex ("Energy Mask", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1.08,1.00,0.42,0.85)
        _HdrEmission ("HDR Emission", Color) = (1.08,1.00,0.52,1)
        _EmissionBoost ("Emission Boost", Float) = 1.0
        _Reveal ("Reveal", Float) = 1.0
        _Retreat ("Retreat", Float) = -0.08
        _UseParticleAge ("Use Particle Age Alpha", Float) = 1.0
        _RevealStartAge ("Reveal Start Age", Float) = 0.024
        _RevealEndAge ("Reveal End Age", Float) = 0.439
        _RetreatStartAge ("Retreat Start Age", Float) = 0.561
        _RetreatEndAge ("Retreat End Age", Float) = 0.976
        _RevealEdgeWidth ("Reveal Edge Width", Float) = 0.070
        _UseControlUV ("Use Control UV1", Float) = 0.0
        _EdgeGlowWidth ("Edge Glow Width", Float) = 0.24
        _EdgeGlowSoftness ("Edge Glow Softness", Float) = 0.42
        _FlowSpeed ("Flow Speed", Float) = 0.52
        _FlowTexTiling ("Flow Tiling", Float) = 2.6
        _FlowTexStrength ("Flow Strength", Float) = 0.92
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha One
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
            float _EdgeGlowWidth;
            float _EdgeGlowSoftness;
            float _FlowSpeed;
            float _FlowTexTiling;
            float _FlowTexStrength;

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
                float retreatThreshold = 1.0 - retreatProgress * (1.0 + edge);
                float revealMask = 1.0 - smoothstep(revealControl, revealControl + edge, pathT);
                float retreatMask = 1.0 - smoothstep(retreatThreshold, retreatThreshold + edge, outerToInner);
                float revealEdge = 1.0 - smoothstep(0.0, edge, abs(pathT - revealControl));
                float retreatEdge = 1.0 - smoothstep(0.0, edge, abs(outerToInner - retreatThreshold));
                float visible = saturate(revealMask * retreatMask);

                float bladeContact = 1.0 - smoothstep(0.32, 0.58, outerToInner);
                float outerRim = 1.0 - smoothstep(_EdgeGlowWidth, _EdgeGlowWidth + _EdgeGlowSoftness, outerToInner);
                float innerRim = smoothstep(1.0 - _EdgeGlowWidth - _EdgeGlowSoftness, 1.0 - _EdgeGlowWidth, outerToInner);
                float rimMask = saturate(max(outerRim, bladeContact * 0.92) + innerRim * 0.06);

                float2 flowUv = float2(
                    outerToInner * _FlowTexTiling + _Time.y * _FlowSpeed * 0.24,
                    pathT * _FlowTexTiling - _Time.y * _FlowSpeed * 0.62);
                float4 flowTex = tex2D(_MainTex, flowUv);
                float flow = saturate(max(flowTex.a, dot(flowTex.rgb, float3(0.299, 0.587, 0.114))));
                float pulse = pow(saturate(sin((pathT - _Time.y * _FlowSpeed) * 26.0 + outerToInner * 5.0) * 0.5 + 0.5), 3.0);
                float energy = lerp(0.34, 1.08, saturate(flow * _FlowTexStrength + pulse * 0.22));
                float particleCells = floor(pathT * 70.0) * 17.0 + floor(outerToInner * 26.0) * 31.0;
                float particleRand = frac(sin(particleCells + floor(_Time.y * 18.0) * 13.7) * 43758.5453);
                float outwardParticleFade = 1.0 - smoothstep(0.12, 0.72, outerToInner);
                float outwardParticles = step(0.86, particleRand) * outwardParticleFade * saturate(flow * 0.70 + pulse * 0.30);

                float alpha = visible * _TintColor.a * saturate(rimMask * (0.56 + flow * 0.36 + revealEdge * 0.16 + retreatEdge * 0.03) + outwardParticles * 0.22);
                float3 rgb = _TintColor.rgb * _HdrEmission.rgb * _EmissionBoost * energy;
                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Particles/Additive"
}
