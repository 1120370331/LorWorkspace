Shader "Steria/AdditiveGlow"
{
    Properties
    {
        _MainTex ("形状贴图", 2D) = "white" {}
        _NoiseTex ("噪声贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1, 0.2, 0.2, 1)
        _Intensity ("发光强度", Range(0, 5)) = 2
        _NoiseStrength ("噪声强度", Range(0, 1)) = 0.3
        _Speed ("动画速度", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One  // Additive混合：叠加发光
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _Color;
            float _Intensity;
            float _NoiseStrength;
            float _Speed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 采样形状
                float shape = tex2D(_MainTex, i.uv).r;

                // 采样噪声（带动画）
                float2 noiseUV = i.uv + _Time.y * _Speed * 0.1;
                float noise = tex2D(_NoiseTex, noiseUV).r;

                // 混合
                float alpha = shape * (1 - _NoiseStrength + noise * _NoiseStrength);

                // 输出发光颜色
                return _Color * alpha * _Intensity;
            }
            ENDCG
        }
    }
}
