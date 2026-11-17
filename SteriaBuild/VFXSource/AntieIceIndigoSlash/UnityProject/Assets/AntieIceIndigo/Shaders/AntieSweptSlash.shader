Shader "Steria/Antie/IceIndigoSweptSlash"
{
    Properties
    {
        _ColorTex ("User ice indigo colour",2D)="black"{}
        _ShapeMask ("User grayscale shape and brush",2D)="black"{}
        _FlowTex ("User horizontal flow (Linear Clamp)",2D)="gray"{}
        _TextureRect ("Source crop XY width height",Vector)=(.016,.326,.904,.430)
        _Emission ("Emission",Range(.5,2.5))=1.35
        _FlowSpeed ("Profile flow speed",Float)=1.15
        _FlowOffset ("Profile flow direction",Vector)=(.12,0,0,0)
        _Tint ("Profile tint",Color)=(1,1,1,1)
        _FlowStrength ("Internal flow",Range(0,1))=.32
        _SweepWidth ("Brush span",Range(.1,.8))=.34
        _SweepProgress ("Brush progress",Range(0,1))=0
        _Reveal ("Reveal",Range(0,1))=1
        _Retreat ("Retreat",Range(0,1))=0
        _Phase ("Phase",Float)=0
        _Dissolve ("Dissolve",Range(0,1))=0
        _Alpha ("Alpha",Range(0,1))=1
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _ColorTex,_ShapeMask,_FlowTex;
            float4 _TextureRect,_FlowOffset,_Tint;
            float _FlowSpeed;float _Emission,_FlowStrength,_SweepWidth,_SweepProgress,_Reveal,_Retreat,_Phase,_Dissolve,_Alpha;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            float2 SourceUV(float2 uv){return float2(_TextureRect.x+uv.x*_TextureRect.z,_TextureRect.y+(1-uv.y)*_TextureRect.w);}
            half4 frag(v2f i):SV_Target
            {
                float2 original=SourceUV(i.uv);
                float3 paint=tex2D(_ColorTex,original).rgb;
                float fixedShape=pow(tex2D(_ShapeMask,original).r,.66);
                float brushU=(i.uv.x-_SweepProgress)/max(.08,_SweepWidth)+.90;
                float brush=tex2D(_ShapeMask,SourceUV(float2(saturate(brushU),i.uv.y))).r*step(0,brushU)*step(brushU,1);
                float passed=max(0,_SweepProgress-i.uv.x),spread=smoothstep(.01,.10,passed);
                float wake=(1-smoothstep(_SweepProgress-.025,_SweepProgress+.01,i.uv.x))*.72;
                wake*=1-smoothstep(.05+spread*.90,.08+spread*.92,i.uv.y);
                float moving=max(wake,brush*1.45);
                float head=1-smoothstep(_Reveal,_Reveal+.02,i.uv.x);
                float tail=smoothstep(_Retreat-.025,_Retreat,i.uv.x);
                float phase0=frac(_Phase*_FlowSpeed),phase1=frac(_Phase*_FlowSpeed+.5),weight=1-abs(phase0*2-1);
                float2 flowUV=i.uv*.72+.14;
                float flowA=tex2D(_FlowTex,flowUV-_FlowOffset.xy*phase0).r;
                float flowB=tex2D(_FlowTex,flowUV-_FlowOffset.xy*phase1).r;
                float flow=lerp(flowB,flowA,weight);
                float erosion=tex2D(_FlowTex,i.uv*.78+.11).r;
                float keep=1-smoothstep(erosion-.06,erosion+.06,lerp(-.15,1.15,_Dissolve));
                float level=max(paint.r,max(paint.g,paint.b));
                float3 colour=paint/max(.10,level)*(.62+.48*level)*_Emission;
                colour*=lerp(1,.76+flow*.64,_FlowStrength);
                colour=lerp(colour,float3(1.35,1.55,1.65),brush*.20);
                float alpha=saturate(fixedShape*moving*head*tail*keep*_Alpha);
                return half4(colour*_Tint.rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
