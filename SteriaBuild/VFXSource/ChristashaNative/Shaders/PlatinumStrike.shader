Shader "Steria/Christasha/PlatinumStrike"
{
    Properties
    {
        _MainTex ("Local authored brush", 2D) = "white" {}
        _Color ("Instance tint", Color) = (1,1,1,1)
        _Role ("0 thrust / 1 sword / 2 wake / 3 shock / 4 flash / 5 hilt", Float) = 0
        _Phase ("Manual visual seconds", Float) = 0
        _Opacity ("Opacity", Float) = 1
        _Dissolve ("Retreat erosion", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Culling", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull [_Cull] ZWrite Off Lighting Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            float _Role, _Phase, _Opacity, _Dissolve;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v)
            { v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float detail(float2 p) { return .62*noise(p)+.26*noise(p*2.13+7.7)+.12*noise(p*4.31+1.9); }
            float4 frag(v2f i):SV_Target
            {
                float u=saturate(i.uv.x),v=i.uv.y*2-1;
                float grain=detail(float2(u*5.7-_Phase*4.8,v*9.3+_Phase*.7));
                float coverage=0,hot=0,edge=0;
                if(_Role<1.5)
                {
                    // UV.x is tip -> root, UV.y crosses the energy volume.
                    float cross=v;
                    float warp=(grain-.5)*.15*u;
                    float brush=tex2D(_MainTex,float2(.51+(cross+warp)*(.035+(_Role<.5?.24:.29)*u),.97-u*.94)).r;
                    float grain2=detail(float2(u*13-_Phase*8,cross*17));
                    float filaments=smoothstep(.28,.72,grain2)*pow(saturate(1-abs(cross)),.7);
                    float ridge=pow(saturate(1-abs(cross+(grain-.5)*.10)),22);
                    float rootFade=1-smoothstep(.63,1,u);
                    coverage=(smoothstep(.025,.25,brush)*.78+filaments*.16)*(.28+.72*rootFade);
                    coverage*=smoothstep(_Dissolve*.8,_Dissolve*.8+.16,grain+brush*.48);
                    if(_Role<.5)coverage*=1-smoothstep(.88,1,u);
                    hot=saturate(brush*1.4+ridge*.52-.38);
                    edge=ridge*(.30+.70*grain2)*rootFade;
                    if(_Role>.5)
                    {
                        // A luminous sword retains a legible narrow blade/hilt,
                        // while streaks and holes move inside its silhouette.
                        coverage=.76+grain*.24;
                        hot=.58+ridge*.32+grain2*.10;
                        edge=ridge*.20;
                        coverage*=1-_Dissolve*.80;
                    }
                }
                else if(_Role<2.5)
                {
                    float warped=v+(grain-.5)*.34;
                    float profile=pow(saturate(1-abs(warped)),1.7);
                    float stroke=tex2D(_MainTex,float2(.51+warped*.25,.12+u*.83)).r;
                    float strands=detail(float2(u*8+_Phase*5,v*16));
                    coverage=profile*(.15+stroke*.85)*smoothstep(.16+u*.29,.68,strands)*pow(1-u,.35)*smoothstep(0,.09,u);
                    hot=pow(profile,4)*strands;edge=stroke*.25;
                }
                else if(_Role<3.5)
                {
                    float angle=u*6.2831853;
                    // Noise is sampled on a circle, so the UV seam is continuous.
                    float n=detail(float2(cos(angle),sin(angle))*5.3+_Phase*1.5);
                    float fine=detail(float2(cos(angle),sin(angle))*17+v*2.5-_Phase*2);
                    float cross=v+(n-.5)*.7;
                    float band=pow(saturate(1-abs(cross)),2);
                    float brush=tex2D(_MainTex,float2(.51+cos(angle)*.20,.44+sin(angle)*.30)).r;
                    coverage=band*smoothstep(.10+_Dissolve*.69,.33+_Dissolve*.69,n)*(.5+.5*fine)*(.6+brush*.4);
                    hot=pow(saturate(1-abs(cross-.24)),8)*(.5+.5*fine);edge=band*brush*.40;
                }
                else if(_Role<4.5)
                {
                    float2 p=i.uv*2-1;float r=length(p),a=atan2(p.y,p.x);
                    float rays=pow(detail(float2(cos(a),sin(a))*9.3),5)*2.4;
                    coverage=pow(saturate(1-r/(.24+rays)),1.4);
                    hot=pow(saturate(1-r*2.2),2);edge=coverage*.45;
                }
                else
                {
                    coverage=(.46+.26*grain)*(1-_Dissolve);
                    hot=.52+.24*pow(saturate(1-abs(v)),2);
                    edge=.12;
                }
                float3 gold=float3(1.0,.78,.38),cream=float3(1.0,.98,.86);
                float3 rgb=lerp(gold,cream,saturate(hot))*lerp(.50,1.55,hot)+edge*float3(1.45,1.36,1.12);
                return float4(rgb*_Color.rgb*i.color.rgb,saturate(coverage)*_Opacity*_Color.a*i.color.a);
            }
            ENDCG
        }
    }
}
