Shader "Steria/SlazeyaStormWeather"
{
    Properties { _MainTex("Camera source",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _Weather, _CloudEllipse, _Core, _Bodies[16];
            float _Aspect;
            int _BodyCount;
            struct v2f { float4 vertex:SV_POSITION; float2 sourceUV:TEXCOORD0; float2 uv:TEXCOORD1; };
            v2f vert(appdata_img v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.sourceUV=v.texcoord; o.uv=v.texcoord;
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0) o.sourceUV.y=1-o.sourceUV.y;
                #endif
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float bodyProtection(float2 uv)
            {
                float protection=0;
                for(int i=0;i<_BodyCount;i++)
                {
                    float2 center=(_Bodies[i].xy+_Bodies[i].zw)*.5;
                    float2 radii=max((_Bodies[i].zw-_Bodies[i].xy)*.5,float2(.008,.015))+float2(.035/_Aspect,.045);
                    float2 q=(uv-center)/radii;
                    protection=max(protection,exp(-.4*pow(dot(q,q),2)));
                }
                return protection;
            }
            float cloudArc(float2 uv,float angle,float span,float radius,float2 offset,float thickness,float seed,float speed)
            {
                // The initial world BOX remains the anchor; its vertical support is wider
                // than the visible silhouette. Only weather uses this effective contour.
                float2 radii=_CloudEllipse.zw*float2(1,.78);
                float2 q=(uv-_CloudEllipse.xy)/max(radii,.001);
                // Keep the open gestures in the available air for either target formation.
                if(_CloudEllipse.x<.5) q.x=-q.x;
                q-=offset*2;
                float theta=atan2(q.y,q.x);
                float center=angle-.15*sin(_Weather.w*speed/.15);
                float a=atan2(sin(theta-center),cos(theta-center));
                float end=1-smoothstep(span*.20,span*.50,abs(a));
                // Irregular broad shoulders and a deliberate torn gap, not an ellipse outline.
                float radial=length(q)-radius;
                float bend=.026*sin(a*13+seed)+.015*sin(a*27+seed*3);
                float broad=noise(float2(a*4.5+seed-_Weather.w*.65,seed+radial*13));
                float fine=noise(float2(a*39+seed*9-_Weather.w*1.10,radial*43+seed));
                float width=thickness*lerp(.55,1.15,broad);
                float ribbon=exp(-pow((radial-bend)/max(width,.005),2)*2);
                float torn=.45+.55*smoothstep(.10,.60,broad-.23*(1-fine));
                // A local eroded pocket, not an angular wedge through every radial sample.
                // The unequal feathers and bounded radial warp prevent a ruler-straight cut.
                float gapCenter=span*.08+.026*sin(radial*32+seed)+.012*(noise(float2(radial*48+seed,seed*3))-.5);
                float side=a-gapCenter;
                float2 pocket=float2(side/(side<0?.065:.09),(radial-bend-width*.15)/width);
                // Retain thin residual air instead of clipping the shoulder into an opaque-looking hole.
                float erosion=1-.90*exp(-dot(pocket,pocket));
                return ribbon*torn*erosion*end;
            }
            float rainLayer(float2 uv,float nearLayer)
            {
                // Work at 720p scale. Pixel dimensions scale together with source height.
                float2 p=uv*float2(720*_Aspect,720);
                float slope=lerp(.255,.365,nearLayer),speed=lerp(370,590,nearLayer);
                p=float2(p.x-slope*p.y,p.y+_Weather.w*speed);
                float cell=lerp(72,165,nearLayer),seed=lerp(17,93,nearLayer);
                float2 id=floor(p/cell); float alpha=0;
                for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                {
                    float2 k=id+float2(x,y),h=k+seed;
                    float r=hash(h),r2=hash(h+41.3);
                    float2 pos=(k+float2(.1+.8*r,.15+.7*r2))*cell;
                    float2 d=p-pos;
                    float len=lerp(lerp(8,16,r2),lerp(18,32,r2),nearLayer);
                    float width=lerp(lerp(.65,1,r),lerp(1,1.4,r),nearLayer);
                    float aa=max(.5,720*abs(_MainTex_TexelSize.y));
                    float stroke=1-smoothstep(width*.5,width*.5+aa,abs(d.x));
                    float tail=smoothstep(-len*.5,-len*.32,d.y)*(1-smoothstep(-len*.1,len*.5,d.y));
                    float visible=step(r,lerp(.86,.83,nearLayer));
                    alpha=max(alpha,stroke*tail*visible*lerp(.45,1,hash(h+8.8)));
                }
                return alpha*lerp(.16,.24,nearLayer);
            }
            float4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.sourceUV); float2 uv=i.uv;
                float edge=smoothstep(.04,.075,uv.x)*(1-smoothstep(.925,.96,uv.x))*smoothstep(.20,.235,uv.y)*(1-smoothstep(.845,.88,uv.y));
                float body=bodyProtection(uv),core=0;
                if(_Core.z>.5) core=1-smoothstep(.45,1.25,length((uv-_Core.xy)/float2(.15,.19)));
                float protect=(1-.85*core)*(1-.85*body)*edge;
                float luma=dot(source.rgb,float3(.2126,.7152,.0722));
                float preserve=1-.8*smoothstep(.50,.90,luma);
                float3 grade=lerp(source.rgb,luma.xxx,.12)*float3(.78,.90,1.05);
                float mix=.28*_Weather.y*preserve*edge*(1-.60*body);
                float3 color=lerp(source.rgb,grade,mix);
                color=lerp(color,float3(.12,.19,.24),.025*_Weather.y*edge*(1-.60*body)*smoothstep(.01,.16,luma)*(1-smoothstep(.18,.5,luma)));
                float cloud=0;
                if(_Core.z>.5)
                {
                    cloud=cloudArc(uv,2.85,.85,1.10,float2(-.04,.015),.12,3.7,.11);
                    cloud=max(cloud,.70*cloudArc(uv,3.98,.62,1.10,float2(-.04,-.005),.10,12.1,.075));
                    cloud=max(cloud,.40*cloudArc(uv,3.32,.30,1.18,float2(-.05,0),.045,23.4,.15));
                }
                float3 cloudColor=lerp(float3(.13,.19,.23),float3(.50,.59,.64),saturate(cloud*.9));
                color=lerp(color,cloudColor,min(.30,cloud*.30)*_Weather.x*protect);
                float distant=rainLayer(uv,0),nearby=rainLayer(uv,1)*(1-.7*core);
                float rain=max(distant,nearby)*_Weather.z*protect;
                color=lerp(color,float3(.48,.57,.63),rain);
                return float4(color,source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
