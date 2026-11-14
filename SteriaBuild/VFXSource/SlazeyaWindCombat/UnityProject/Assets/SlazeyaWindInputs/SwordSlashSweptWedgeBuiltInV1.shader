Shader "Skill/SwordSlashSweptWedgeBuiltInV1"
{
    Properties
    {
        _WedgeTex ("Clamp Wedge/Brush Mask", 2D) = "black" {}
        _HighlightTex ("Repeat Highlight", 2D) = "white" {}
        _MaskTex ("Clamp Mesh Mask", 2D) = "white" {}
        _Sweep ("Sweep Position", Range(0,1)) = 0.5
        _WedgeSpan ("Wedge Span", Range(0.02,1)) = 0.35
        _Phase ("Detail Phase", Float) = 0
        _HighlightTiling ("Highlight Tiling", Vector) = (1,1,0,0)
        _HighlightSpeed ("Highlight Speed", Vector) = (1,0,0,0)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0,12)) = 2
        _Alpha ("Alpha", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _WedgeTex;
            sampler2D _HighlightTex;
            sampler2D _MaskTex;
            float _Sweep;
            float _WedgeSpan;
            float _Phase;
            float4 _HighlightTiling;
            float4 _HighlightSpeed;
            float4 _BaseColor;
            float4 _EmissionColor;
            float _Emission;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
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
                float span = max(_WedgeSpan, 0.0001);
                float2 wedgeUv = float2((i.uv.x - (_Sweep - span)) / span, i.uv.y);
                float inside = step(0.0, wedgeUv.x) * step(wedgeUv.x, 1.0);
                float wedge = tex2D(_WedgeTex, saturate(wedgeUv)).a * inside;
                float mask = tex2D(_MaskTex, i.uv).a;
                float2 detailUv = i.uv * _HighlightTiling.xy + _Phase * _HighlightSpeed.xy;
                float highlight = tex2D(_HighlightTex, detailUv).r;
                float coverage = wedge * mask * i.color.a * _Alpha;
                float3 rgb = _BaseColor.rgb + _EmissionColor.rgb * highlight * _Emission;
                return float4(rgb * i.color.rgb * coverage, coverage * _BaseColor.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
