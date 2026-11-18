Shader "Steria/VeliaObservationParticle"
{
    Properties
    {
        _MainTex ("Linear scalar R light mask", 2D) = "black" {}
        _Tint ("Light tint", Color) = (1,1,1,1)
        _MaskMode ("0 Surge, 1 Source, 2 Small streak", Float) = 0
        _CoreEnergy ("Warm white energy", Float) = .65
        _HaloEnergy ("Pale gold radiance", Float) = .3
        _EmissionGain ("Energy multiplier", Float) = 1
        _FaceMask ("Soft face protection", Range(0,1)) = 0
        _Age ("Instance scaled age", Float) = 0
        _BaseY ("Instance feet world Y", Float) = 0
        _Height ("Instance standing height", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; half4 _Tint;
            float _MaskMode,_CoreEnergy,_HaloEnergy,_EmissionGain,_FaceMask,_Age,_BaseY,_Height;
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float worldY:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Tint;o.uv=v.uv;
                o.worldY=mul(unity_ObjectToWorld,v.vertex).y;return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);
            }
            half4 frag(v2f i):SV_Target
            {
                float h=(i.worldY-_BaseY)/max(.001,_Height);
                float2 uv=i.uv;
                float m=0,halo=0,coverage=1,frontEnergy=1;
                if(_MaskMode<.5)
                {
                    float expand=smoothstep(.58,.90,_Age);
                    float n=noise(float2(uv.x*4,uv.y*5-_Age*1.5));
                    float detail=noise(float2(uv.x*11+7,uv.y*13-_Age*3));
                    float2 warped=uv;
                    warped.x=.5+(uv.x-.5)/(1+.28*expand)+(n-.5)*.028+(detail-.5)*.012;
                    // Preserve stable coordinates throughout reveal/retreat. The
                    // source mask is bottom-white/top-dark, not an alpha picture.
                    warped.y=uv.y+(detail-.5)*.012;
                    m=saturate(tex2D(_MainTex,warped).r);
                    float2 broad=warped;broad.x=.5+(broad.x-.5)/1.22;
                    halo=saturate(tex2D(_MainTex,broad).r);
                    float front=lerp(.20,1.32,smoothstep(.16,.58,_Age));
                    float reveal=1-smoothstep(front-.12,front+.06,h);
                    float lowerEdge=lerp(-.14,.64,smoothstep(.58,.90,_Age));
                    float bottom=smoothstep(lowerEdge-.16,lowerEdge+.17,h);
                    float envelope=smoothstep(.16,.23,_Age)*(1-smoothstep(.63,1.35,_Age));
                    float erosion=smoothstep(.90,1.35,_Age);
                    float density=.65*n+.35*detail;
                    float dissolve=smoothstep(.08+.92*erosion,.24+.92*erosion,density);
                    float border=smoothstep(0,.055,uv.x)*(1-smoothstep(.945,1,uv.x))
                        *smoothstep(0,.16,uv.y)*(1-smoothstep(.75,1,uv.y));
                    coverage=reveal*bottom*envelope*dissolve*border*(.88+.12*density);
                    frontEnergy=1+1.30*exp(-pow((h-front+.10)/.17,2));
                }
                else
                {
                    m=saturate(tex2D(_MainTex,uv).r);
                    float2 broad=.5+(uv-.5)/1.22;
                    halo=saturate(tex2D(_MainTex,broad).r);
                    if(_MaskMode<1.5)
                        coverage=smoothstep(.16,.26,_Age)*(1-smoothstep(.75,1.35,_Age));
                }
                // Current light-mask materials set FaceMask=0. Natural energy
                // falloff protects the face without splitting the light field.
                float face=1-.88*exp(-pow((h-.86)/.18,2));
                coverage*=lerp(1,face,saturate(_FaceMask))*i.color.a;
                float core=pow(m,2.4)*_CoreEnergy*frontEnergy;
                float radiance=pow(halo,.65)*_HaloEnergy;
                half3 coreColour=(_MaskMode>.5 && _MaskMode<1.5)?half3(1,.995,.975):half3(1,.98,.90);
                half3 light=(core*coreColour+radiance*half3(1,.78,.36))*i.color.rgb*_EmissionGain;
                // Blend One One: coverage is applied exactly once to emitted RGB.
                return half4(light*coverage,0);
            }
            ENDCG
        }
    }
}
