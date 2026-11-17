// Compatible derivative; original contract and source hash in provenance.json.
// Sword Slash Contract v1: Unity 2019.3 Built-in, transparent unlit.
Shader "Steria/WaterThunder/FormedArc"
{
    Properties
    {
        _ShapeTex ("Shape (Clamp, Linear)", 2D) = "white" {}
        _MaskTex ("Mask (Clamp, Linear)", 2D) = "white" {}

        _BladeBoundary ("Shared Blade Center Radius Inset", Vector) = (0.65,1.95,1.95,0.028)
        _WaterDepthTex ("Height B / Flow RG / Thickness A (Linear)", 2D) = "gray" {}
        _UVFlowDistance ("UV Flow Distance", Range(0,0.3)) = 0
        _DepthOffset ("Height Guided Offset", Range(0,0.06)) = 0
        _LightFlowTex ("Streaming Light RG (Repeat, Linear)", 2D) = "gray" {}
        _CoreFlowStrength ("Highlight Internal Flow", Range(0,1)) = 0
        _WaterNoiseTex ("Water Noise (Repeat, Linear)", 2D) = "gray" {}
        _WaterNoiseStrength ("Noise Flow Strength", Range(0,1)) = 0
        _NoiseStretch ("Noise UV Stretch", Vector) = (1.7,0.62,0,0)
        _NoiseSpin ("Noise UV Rotation Speed", Float) = 0.6
        _RippleTex ("User Water Surface Normal (Clamp, Linear)", 2D) = "gray" {}
        _RippleStrength ("Internal Water Ripples", Range(0,0.1)) = 0
        _RippleShine ("Ripple Moving Highlights", Range(0,1)) = 0
        _Liquid ("Liquid Droplet Surface", Range(0,1)) = 0
        _WaterNormalTex ("Water Normal RG (Clamp, Linear)", 2D) = "gray" {}
        _LiquidWobble ("Capillary Wobble", Range(0,1)) = 0.8
        _LiquidShine ("Water Specular", Range(0,2)) = 1.1
        _WhiteFluidTex ("Curved Four-Sided Fluid Mask", 2D) = "black" {}
        _WhiteFluid ("White Fluid Pattern", Range(0,1)) = 0
        _WhiteFluidStrength ("White Fluid Strength", Range(0,2)) = 0.55
        _WhiteFluidSpin ("Fluid Spin Rate", Float) = 4.5
        _Voltage ("Electrical Potential Envelope", Range(0,1)) = 0
        _VoltageParticle ("Particle Mesh Energy In Blue", Range(0,1)) = 0
        _ElectricalTime ("Local Electrical Clock (-1 uses Phase)", Float) = -1
        _VoltageDecay ("Voltage Discharge", Range(1,10)) = 3.2
        _LiquidDissolve ("Drop Dissolve Before Fade", Range(0,1)) = 1
        _EvapDissolveSoftness ("Water Dissolve Edge Softness", Range(0.01,0.15)) = 0.055
        _TailGrowth ("Tail Expansion After Head", Range(0,1)) = 1
        _SweepAcross ("Sweep Across Width", Range(0,1)) = 1
        _SweepCore ("Centered Pointed Core", Range(0,1)) = 0
        _TailMaskTex ("Trailing Water Fan Mask (Clamp, Linear)", 2D) = "white" {}
        _TailMaskStrength ("Tail Mask Strength", Range(0,1)) = 1
        _TailSpread ("Tail Fan Spread", Range(0,2)) = 1
        _SweepMaskTex ("Moving Brush Wedge (Clamp, Linear)", 2D) = "white" {}
        _SweepEnabled ("Swept Brightness", Range(0,1)) = 0
        _SweepProgress ("Sweep Progress", Range(0,1)) = 0
        _SweepWidth ("Bright Sweep Width", Range(0.08,1)) = 0.38
        _SweepWake ("Passed Water Afterglow", Range(0,1)) = 0.60
        _EvapScatterTex ("Water Vapour Scatter (Clamp)", 2D) = "black" {}
        _EvapProgress ("Scatter Evaporation Progress", Range(0,1)) = 0
        _EvapSpread ("Scatter Spread", Float) = 1
        _EvapLift ("Scatter Rise", Float) = 0
        _EvapFrame ("Scatter Local XY Frame", Vector) = (0.65,1.95,4.6,4.8)
        _HeadWeight ("Legacy Front Emphasis", Range(0,1)) = 0
        _HeadSpan ("Front Length", Range(0.03,1)) = 0.32
        _Evaporation ("Water Scatter Evaporation", Range(0,1)) = 0
        _UseShapeRGB ("Use Texture Color", Range(0,1)) = 1
        _Dissolve ("Fracture", Range(0,1)) = 0
        _DissolveTex ("Fracture Noise (Repeat, Linear)", 2D) = "white" {}
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

            sampler2D _WaterDepthTex, _LightFlowTex;
            float _UVFlowDistance, _DepthOffset, _CoreFlowStrength;
            sampler2D _WaterNoiseTex;
            float _WaterNoiseStrength, _NoiseSpin;
            float4 _NoiseStretch, _BladeBoundary;
            sampler2D _RippleTex;
            float _RippleStrength, _RippleShine;
            sampler2D _WaterNormalTex;
            float _Liquid, _LiquidWobble, _LiquidShine, _LiquidDissolve, _EvapDissolveSoftness, _TailGrowth, _SweepAcross, _SweepCore;
            sampler2D _WhiteFluidTex;
            float _WhiteFluid, _WhiteFluidStrength, _WhiteFluidSpin, _Voltage, _VoltageParticle, _ElectricalTime, _VoltageDecay;
            float _HeadWeight, _HeadSpan, _Evaporation;
            sampler2D _TailMaskTex, _SweepMaskTex, _EvapScatterTex;
            float _TailMaskStrength, _TailSpread, _SweepEnabled, _SweepProgress, _SweepWidth, _SweepWake;
            float _EvapProgress, _EvapSpread, _EvapLift;
            float4 _EvapFrame;
            float _UseShapeRGB;
            float _Dissolve;
            sampler2D _DissolveTex;
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
                float2 tailOffset : TEXCOORD2;
                float2 chargeData : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float2 localPosition : TEXCOORD1;
                float2 electrical : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.xy += v.tailOffset * (_TailSpread*_TailGrowth-1);
                float flutter=(sin(v.uv.x*23+v.uv.y*17-_Phase*9)+0.45*sin(v.uv.x*41-v.uv.y*9-_Phase*6))*0.16;
                float trailing=1-smoothstep(_Retreat+0.06,_Retreat+0.30,v.uv.x);
                v.vertex.xy += v.tailOffset*flutter*trailing*_TailMaskStrength*_TailGrowth;
                if(_TailMaskStrength>0.5)
                {
                    float2 radial=v.vertex.xy-_BladeBoundary.xy;
                    float limit=max(0.01,_BladeBoundary.z-_BladeBoundary.w);
                    v.vertex.xy=_BladeBoundary.xy+radial*min(1,limit/max(length(radial),0.0001));
                }
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.localPosition = v.vertex.xy;
                o.electrical = v.chargeData;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                if(_Liquid>0.5)
                {
                    // Color RGB carries stable seed, normalized age and ligament type for water only.
                    float seed=i.color.r, age=i.color.g, ligament=step(0.5,i.color.b);
                    float2 p=(i.uv-0.5)*2;
                    float angle=atan2(p.y,p.x);
                    float wave=age*(11+seed*7)+seed*19;
                    float damping=exp(-age*2.8);
                    float wobble=_LiquidWobble*damping*(0.075*cos(angle*2+wave)+0.032*sin(angle*3-wave*0.7));
                    float contour=1-smoothstep(0.84+wobble,0.94+wobble,length(p));
                    if(ligament>0.5)
                    {
                        float neck=smoothstep(0.18,0.65,age)*0.87*exp(-p.x*p.x*7);
                        float halfWidth=max(0,0.72*sqrt(saturate(1-p.x*p.x))-neck+0.035*sin(p.x*8+wave)*damping);
                        contour=(1-smoothstep(max(0,halfWidth-0.07),halfWidth+0.07,abs(p.y)))*smoothstep(0.01,0.06,halfWidth);
                        contour*=1-smoothstep(0.90,1,abs(p.x));
                        float split=smoothstep(0.45,0.82,age),separation=0.30+split*0.17;
                        float2 lobe=(p-float2(p.x<0?-separation:separation,0))/float2(0.32,0.65);
                        float threadDistance=abs(p.y)-halfWidth;
                        float daughterDistance=(length(lobe)-0.94)*0.45;
                        contour=(1-smoothstep(-0.055,0.055,lerp(threadDistance,daughterDistance,split)))*(1-smoothstep(0.94,1,abs(p.x)));
                    }
                    float2 waveUV=i.uv+float2(sin(wave+p.y*3),cos(wave*0.83+p.x*4))*0.019*_LiquidWobble*damping;
                    float3 normal=tex2D(_WaterNormalTex,saturate(waveUV)).rgb;
                    normal=normalize(float3(normal.xy*2-1,max(0.08,normal.z)));
                    float spec=pow(saturate(dot(normal,normalize(float3(-0.38,0.58,1)))),64)*_LiquidShine;
                    float fresnel=pow(1-saturate(normal.z),2.5)*saturate(normal.y*0.4-normal.x*0.2+0.35);
                    float thick=sqrt(saturate(1-dot(p,p)));
                    float caustic=exp(-abs(p.y+0.24*sin(p.x*3+wave))*12)*0.12*damping;
                    float3 water=tex2D(_ShapeTex,i.uv).rgb*(0.48+0.52*thick);
                    water+=float3(0.85,0.97,1)*spec+float3(0.12,0.52,0.8)*(fresnel*0.4+caustic);
                    float2 dropMaskUV=i.uv*0.60+float2(seed*0.30,(1-seed)*0.30);
                    float dropHeight=tex2D(_EvapScatterTex,dropMaskUV).g;
                    float dropCut=lerp(-0.10,1.10,smoothstep(0.62,0.98,age));
                    float dropKeep=1-smoothstep(dropHeight-0.065,dropHeight+0.065,dropCut);
                    float liquidAlpha=contour*tex2D(_MaskTex,i.uv).r*(0.10+thick*0.15+spec*0.48+fresnel*0.12)*_Alpha*i.color.a*lerp(1,dropKeep,_LiquidDissolve);
                    return half4(water*_BaseColor.rgb,liquidAlpha*_BaseColor.a);
                }
                // UV0.x is pathT and UV0.y is acrossBlade (inner=0, outer=1).
                float revealSoftness = max(_RevealSoftness, 0.001);
                float head = 1.0 - smoothstep(saturate(_Reveal), saturate(_Reveal) + revealSoftness, i.uv.x);
                float tail = smoothstep(saturate(_Retreat) - revealSoftness, saturate(_Retreat), i.uv.x);
                float lifetime = head * tail * step(_Retreat, _Reveal + revealSoftness);

                float2 distortUv = i.uv * _DistortTiling.xy + _Phase * _DistortSpeed.xy;
                float2 distort = (tex2D(_DistortTex, distortUv).rg * 2.0 - 1.0) * _DistortStrength;

                // Keep one-shot shape/lifetime edges in undistorted UV0. Distort periodic detail only.
                // Rotate then stretch a single noise map; never use it to punch holes in the body.
                float noiseAngle=0.28+_Phase*_NoiseSpin;
                float2 stretched=(i.uv-0.5)*_NoiseStretch.xy;
                float2 noiseUV=float2(stretched.x*cos(noiseAngle)-stretched.y*sin(noiseAngle),stretched.x*sin(noiseAngle)+stretched.y*cos(noiseAngle))+float2(-_Phase*.34,_Phase*.08);
                float flowNoise=tex2D(_WaterNoiseTex,noiseUV).r;
                float flowNoiseY=tex2D(_WaterNoiseTex,noiseUV+float2(.31,.43)).r;
                float2 noiseWarp=(float2(flowNoise,flowNoiseY)-.5)*.025*_WaterNoiseStrength;
                // Flow follows the blade tangent; height only adds a bounded lateral UV displacement.
                float phase0=frac(_Phase*0.85),phase1=frac(_Phase*0.85+0.5);
                float weight=1-abs(phase0*2-1);
                float4 depth=tex2D(_WaterDepthTex,i.uv);
                float2 direction=depth.rg*2-1;
                float2 lateral=float2(-direction.y,direction.x)*(depth.b-.5)*_DepthOffset;
                float2 offset0=-direction*(phase0-.5)*_UVFlowDistance+lateral+noiseWarp;
                float2 offset1=-direction*(phase1-.5)*_UVFlowDistance+lateral+noiseWarp;
                offset0=clamp(offset0,float2(-.10,-.035),float2(.10,.035));
                offset1=clamp(offset1,float2(-.10,-.035),float2(.10,.035));
                float2 normalUV=i.uv*float2(.76,.74)+float2(.12,.10);
                float3 normalA=tex2D(_RippleTex,normalUV+offset0*.45).rgb*2-1;
                float3 normalB=tex2D(_RippleTex,normalUV+offset1*.45).rgb*2-1;
                float3 rippleN=normalize(lerp(normalB,normalA,weight)+float3(0,0,.001));
                float4 shapeA=tex2D(_ShapeTex,saturate(i.uv+offset0));
                float4 shapeB=tex2D(_ShapeTex,saturate(i.uv+offset1));
                float4 shapeSample=lerp(shapeB,shapeA,weight);
                shapeSample.a=tex2D(_ShapeTex,i.uv).a; // Coverage never moves with the colour UVs.

                float shape = shapeSample.a;
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

                float fractureNoise = tex2D(_DissolveTex, i.uv * float2(3, 1) + float2(_Phase * 0.07, 0)).r;
                float fracture = 1 - smoothstep(fractureNoise - 0.10, fractureNoise + 0.10, _Dissolve);
                float front = pow(saturate(1 - max(0, _Reveal-i.uv.x) / max(_HeadSpan, 0.03)), 0.7);
                float across=lerp(i.uv.y,abs(i.uv.y-0.5)*2,_SweepCore);
                across=lerp(0.025,across,_SweepAcross);
                // The bright layer has a fixed span. Its rear cutoff can only advance.
                float sweepWidth=_SweepWidth;
                float2 sweepUV=float2((i.uv.x-_SweepProgress)/max(sweepWidth,0.08)+0.92,across);
                float sweep=tex2D(_SweepMaskTex,sweepUV).r * step(0,sweepUV.x)*step(sweepUV.x,1);
                float passed=max(0,_SweepProgress-i.uv.x);
                float spread=smoothstep(0.015,0.18,passed);
                float wakeWidth=1-smoothstep(0.04+spread*0.96,0.08+spread*0.96,across);
                float wake=(1-smoothstep(_SweepProgress-0.035,_SweepProgress+0.015,i.uv.x))*_SweepWake*smoothstep(0.012,0.10,passed)*wakeWidth;
                float sweepStrength=0.65+0.80*_SweepProgress;
                float coreTail=max(_Retreat,max(0,_SweepProgress-_SweepWidth*0.70));
                float coreRear=smoothstep(coreTail,coreTail+0.035,i.uv.x);
                float corePassed=1-smoothstep(_SweepProgress-0.018,_SweepProgress+0.005,i.uv.x);
                float coreCoverage=coreRear*corePassed*wakeWidth;
                float movingMask=lerp(max(wake,sweep*sweepStrength),coreCoverage,_SweepCore);
                float tailRelative=(i.uv.x-_Retreat)/0.30;
                float ribbonWeight=1-smoothstep(0.10,1,tailRelative);
                float2 ribbonUV=float2(tailRelative*0.48,i.uv.y+0.055*sin(i.uv.x*23-_Phase*9)+0.025*sin(i.uv.x*41+_Phase*6));
                mask*=lerp(1,tex2D(_TailMaskTex,saturate(ribbonUV)).r,_TailMaskStrength*ribbonWeight);
                float2 dissolveUV=(i.localPosition-_EvapFrame.xy)/_EvapFrame.zw+0.5;
                float4 dissolveData=tex2D(_EvapScatterTex,dissolveUV);
                float dissolveHeight=dissolveData.r*0.78+dissolveData.b*0.22;
                float cutoff=lerp(-0.10,1.10,_EvapProgress),soft=max(0.01,_EvapDissolveSoftness);
                float evaporated=1-smoothstep(dissolveHeight-soft,dissolveHeight+soft,cutoff);
                float wetEdge=(1-smoothstep(soft*0.5,soft*2.5,abs(dissolveHeight-cutoff)))*step(0.01,_EvapProgress);
                float coverage = saturate(shape * mask * lifetime * fracture) * lerp(1, front, _HeadWeight) * lerp(1,movingMask,_SweepEnabled) * lerp(1,evaporated,_Evaporation);
                float alpha = coverage * saturate(_BodyStrength + highlight) * _Alpha * _BaseColor.a * i.color.a;
                float3 color = _BaseColor.rgb * lerp(float3(1,1,1), shapeSample.rgb, _UseShapeRGB) * max(_BodyStrength, 0.0001);
                color += _EmissionColor.rgb * highlight * _Emission;
                color *= lerp(1,0.80+sweep*sweepStrength*1.55,_SweepEnabled*(1-_SweepCore));
                color+=float3(0.06,0.25,0.38)*wetEdge*_Evaporation;
                float rippleSpec=pow(saturate(dot(rippleN,normalize(float3(-0.35,0.55,1)))),22);
                color*=1+_RippleShine*(rippleN.z-0.70)*0.22;
                color+=float3(0.55,0.88,1)*rippleSpec*_RippleShine;
                color*=1+(flowNoise-.5)*.65*_WaterNoiseStrength;
                color+=float3(.04,.16,.24)*smoothstep(.56,.88,flowNoise)*_WaterNoiseStrength;
                float2 highlightFlow=tex2D(_LightFlowTex,float2(i.uv.x*1.4-_Phase*3.6,i.uv.y)).rg;
                color=lerp(color,color*float3(.45,.72,1),highlightFlow.g*_CoreFlowStrength);
                color+=float3(.10,.25,.36)*highlightFlow.r*_CoreFlowStrength;
                float2 pattern=i.uv*float2(3.25,1.1)+float2(-_Phase*0.8,_Phase*0.18);
                float cell=floor(pattern.x),spin=_Phase*_WhiteFluidSpin+cell*0.73;
                float2 local=frac(pattern)-0.5;
                float2 spun=float2(local.x*cos(spin)-local.y*sin(spin),local.x*sin(spin)+local.y*cos(spin))+0.5;
                float patternMask=tex2D(_WhiteFluidTex,spun).r;
                color+=float3(0.72,0.95,1)*patternMask*_WhiteFluid*_WhiteFluidStrength;
                float electricalClock=_ElectricalTime<0?_Phase:_ElectricalTime;
                float chargeStart=lerp(i.electrical.x,0,_VoltageParticle);
                float chargeAge=max(0,electricalClock-chargeStart);
                float reserve=saturate(lerp(i.electrical.y,i.color.b,_VoltageParticle));
                float charged=1-exp(-chargeAge*95);
                float discharge=exp(-chargeAge*(_VoltageDecay+6*(1-reserve)));
                float pulse=0.55+0.45*abs(cos(chargeAge*(52+reserve*17)+i.uv.x*9+i.localPosition.x*3));
                float potential=pow(max(reserve,0.001),0.70)*charged*discharge*pulse*step(chargeStart,electricalClock);
                float current=smoothstep(0.025,0.16,potential)*sqrt(max(potential,0));
                alpha*=lerp(1,current,_Voltage);
                color*=lerp(1,0.6+potential*1.4,_Voltage);
                color *= float3(i.color.rg,lerp(i.color.b,1,_VoltageParticle));
                return half4(color, alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
