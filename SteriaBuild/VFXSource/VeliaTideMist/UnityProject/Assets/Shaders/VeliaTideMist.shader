Shader "Steria/VeliaTideMist"
{
    Properties
    {
        _MainTex("Unmodified camera source",2D)="white"{}
        _MistAtlas("Read-only imported density and lighting atlas",2D)="black"{}
        _CloudPlate("Straight RGBA sRGB cloud color and coverage",2D)="black"{}
        _State("Envelope pulse explicit-clock second-die",Vector)=(0,0,0,0)
        _PulseAge("Seconds since current callback, explicit driver clock",Float)=-1
        _SunReveal("Delayed sun reveal",Float)=0
        _HitStrength("Local successful hits",Float)=0
        _Aspect("Viewport aspect",Float)=1.777778
        _MirrorX("Mirror effect toward opposing formation only",Float)=0
    }
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
            sampler2D _MainTex, _MistAtlas, _CloudPlate;
            float4 _MainTex_TexelSize, _MistAtlas_TexelSize, _CloudPlate_TexelSize, _State;
            float _SunReveal, _HitStrength, _Aspect, _PulseAge, _MirrorX;
            int _PointCount, _ProtectionCount;
            float4 _HitPoints[16], _ProtectionRects[16];
            struct v2f { float4 vertex:SV_POSITION; float2 sourceUV:TEXCOORD0; float2 viewportUV:TEXCOORD1; };
            v2f vert(appdata_img v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.sourceUV=v.texcoord; o.viewportUV=v.texcoord;
                // Source RT orientation is independent of our bottom-left viewport masks.
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y<0) o.sourceUV.y=1-o.sourceUV.y;
                #endif
                return o;
            }
            float4 atlasFrame(float frame,float2 uv)
            {
                float2 cell=float2(fmod(frame,4),3-floor(frame/4));
                float2 inset=_MistAtlas_TexelSize.xy*2;
                return tex2D(_MistAtlas,(cell+lerp(inset,1-inset,saturate(uv)))/4);
            }
            float4 mist(float2 uv,float2 center,float2 extent,float mirror,float tilt)
            {
                float drift=sin(_State.z*0.34)*0.025;
                float2 local=(uv-center-float2(drift*mirror,0))/extent;
                float cs=cos(tilt),sn=sin(tilt);
                float2 q=float2(cs*local.x+sn*local.y,-sn*local.x+cs*local.y)+0.5;
                if(mirror<0) q.x=1-q.x;
                float edge=smoothstep(0,0.08,q.x)*smoothstep(0,0.08,q.y)*(1-smoothstep(0.92,1,q.x))*(1-smoothstep(0.92,1,q.y));
                // Breathe slowly between authored middle frames; never loop the lifetime atlas.
                float frame=6.5+1.5*sin(_State.z*0.18);
                return lerp(atlasFrame(floor(frame),q),atlasFrame(floor(frame)+1,q),frac(frame))*edge;
            }
            float softBody(float2 uv,float4 rect)
            {
                // Bounds are only the input contract. The protection field is a broad ellipse,
                // never a box-shaped source cutout. Feather both inside and outside the body.
                float2 center=(rect.xy+rect.zw)*0.5;
                float2 radii=max((rect.zw-rect.xy)*0.5,float2(0.008,0.015));
                radii+=float2(0.035/_Aspect,0.045);
                float2 q=(uv-center)/radii;
                float ellipse=dot(q,q);
                return exp(-0.55*ellipse*ellipse);
            }
            float2 cloudUV(float2 uv,float side)
            {
                // Match physical art aspect, anchored at its authored upper-center opening.
                // Neither dice index nor pulse affects silhouette, scale or motion clock.
                float artAspect=_CloudPlate_TexelSize.z/_CloudPlate_TexelSize.w;
                float entry=0.015*(1-smoothstep(0,0.32,_State.z));
                // Continuous post-intro slowdown shared by rendered banks and ray shadows.
                // Keep real time for the entry, fog and early surface-light propagation.
                float motionTime=min(_State.z,0.32)+max(_State.z-0.32,0)*0.65;
                float drift=sin(motionTime*(side<0 ? 0.13 : 0.11))*0.008;
                float2 q=float2(uv.x-side*(entry+drift),(uv.y-0.74)*artAspect/_Aspect+0.72);
                // Only 1.5 source pixels of broad fold drift; never warp the large masses.
                q+=float2(sin(q.y*12+motionTime*0.15),sin(q.x*10-motionTime*0.12))*_CloudPlate_TexelSize.xy*1.5;
                return q;
            }
            float4 cloudBank(float2 uv,float side)
            {
                float2 q=cloudUV(uv,side);
                float4 c=tex2Dlod(_CloudPlate,float4(q,0,0));
                // The approved asymmetric plate's clear passage is at U=.55; its left
                // lower billow extends beyond .50. Split only inside genuine transparent art.
                float halfMask=side<0 ? 1-smoothstep(0.54,0.55,q.x) : smoothstep(0.55,0.56,q.x);
                c.a*=halfMask*step(0,q.y)*step(q.y,1);
                return c;
            }
            float cloudShadow(float2 uv,float2 sun)
            {
                float depth=0;
                [unroll] for(int stepIndex=0;stepIndex<6;stepIndex++)
                {
                    float t=(stepIndex+0.5)/6.0;
                    float2 p=lerp(uv,sun,t);
                    // One low-frequency coverage sample per step, never camera RGB or bodies.
                    float a=cloudBank(p,p.x<0.55 ? -1 : 1).a;
                    depth+=smoothstep(0.06,0.88,a);
                }
                return exp(-depth*0.85);
            }
            float movingSlope(float baseSlope,float amplitudeDegrees,float phase)
            {
                // Whole-card clock, continuous across callbacks and BeginDice. Angles are
                // measured in screen space; convert back to UV slope at the actual aspect.
                float angle=atan(baseSlope*_Aspect)+radians(amplitudeDegrees)*sin(6.28318530718*_State.z/3.6+phase);
                return tan(angle)/_Aspect;
            }
            float beam(float2 uv,float2 sun,float slope,float width,float spread)
            {
                float y=sun.y-uv.y;
                float x=(uv.x-sun.x-slope*y)*_Aspect;
                float w=(width+max(0,y)*spread)*_Aspect;
                return exp(-pow(x/w,2))*smoothstep(0.025,0.13,y)*(1-smoothstep(0.40,0.70,y));
            }
            float4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.sourceUV);
                float2 screenUV=i.viewportUV;
                float2 fxUV=float2(lerp(screenUV.x,1-screenUV.x,saturate(_MirrorX)),screenUV.y);
                float envelope=saturate(_State.x), pulse=saturate(_State.y);
                if(envelope<=0) return source;
                float protection=0;
                [loop] for(int r=0;r<16;r++) { if(r>=_ProtectionCount) break; protection=max(protection,softBody(screenUV,_ProtectionRects[r])); }
                // Use a central fallback only when no projected body bounds are available.
                float band=smoothstep(0.18,0.30,screenUV.y)*(1-smoothstep(0.49,0.61,screenUV.y));
                float hud=smoothstep(0.055,0.15,screenUV.y)*(1-smoothstep(0.91,0.99,screenUV.y));
                float lightBand=exp(-pow((screenUV.y-0.38)/0.18,2));
                float preserve=(1-0.75*(_ProtectionCount>0 ? protection : lightBand*0.55))*hud;
                // Keep the soft-body core budget while avoiding r6's tripled broad dark patch.
                float bodyProtection=_ProtectionCount>0 ? 1-pow(1-protection,1.5) : band;
                float4 left=cloudBank(fxUV,-1), right=cloudBank(fxUV,1);
                float coverage=left.a+right.a*(1-left.a);
                // Straight-alpha input becomes associated only here, once; no RGB averaging
                // across three transparent density layers. Independent art banks retain folds.
                float3 cloudColor=(left.rgb*left.a+right.rgb*right.a*(1-left.a))/max(coverage,0.0001);
                float cloudAlpha=coverage*lerp(0.86,0.075,bodyProtection)*envelope*hud;
                float3 color=source.rgb;
                // Move the light into the existing scalloped aperture, closer to its lower
                // left billow. The art banks themselves retain their original placement.
                float2 sun=float2(0.515,0.70);
                float2 sunDelta=(fxUV-sun)*float2(_Aspect,1);
                float radius=length(sunDelta)/(0.074*_Aspect);
                // A compact high-gradient core fades into two lower-energy atmospheric
                // scales. No finite disk edge and no broad saturated body plateau.
                float sunCore=exp(-radius*radius*5.2);
                float shoulder=exp(-radius*radius*1.05);
                float halo=exp(-dot(sunDelta,sunDelta)/pow(0.22*_Aspect,2)*2.0);
                float lightEnvelope=pow(envelope,1.30)*_SunReveal;
                // Three broad shafts have dark angular intervals. The same art's opacity
                // blocks them along the light path; source RGB and body ellipses never cast.
                // The opening is asymmetric: at y=.54 its clear span is approximately
                // x=.515..665. Route all three shafts through that real opening instead
                // of sending a nominal left shaft into the opaque bank as r6 did.
                float beams=beam(fxUV,sun,movingSlope(0.11,10.0,-1.68),0.011,0.030)*0.95+
                    beam(fxUV,sun,movingSlope(0.41,7.5,-1.62),0.010,0.040)+beam(fxUV,sun,movingSlope(0.73,5.3,-1.55),0.012,0.045)*0.72;
                float transmission=1;
                [branch] if(beams>0.002) transmission=cloudShadow(fxUV,sun);
                float beamLight=beams*transmission*(0.13+0.63*pulse);
                float3 energy=float3(1,0.97,0.88)*sunCore*(0.72+3.65*pulse);
                energy+=float3(1,0.85,0.62)*shoulder*(0.22+0.65*pulse);
                energy+=float3(1,0.74,0.43)*halo*(0.075+0.20*pulse);
                energy+=float3(1,0.84,0.57)*beamLight;
                energy*=lightEnvelope*preserve;
                color+=color*(1-color)*(0.035*pulse*envelope*preserve);
                color+=(1-color)*(1-exp(-energy));

                // Use the authored neutral lit planes, weighted toward the implied sun.
                // Shadowed blue-gray bellies stay cold even at peak; no uniform alpha outline.
                float luminance=dot(cloudColor,float3(0.2126,0.7152,0.0722));
                float facing=smoothstep(0.30,0.61,fxUV.y)*(0.68+0.32*(1-smoothstep(0.12,0.52,abs(fxUV.x-sun.x))));
                float litSurface=smoothstep(0.37,0.75,luminance)*facing;
                float distanceFromSun=length((fxUV-sun)/float2(0.48,0.52));
                float inner=exp(-distanceFromSun*distanceFromSun*3.2);
                float travel=smoothstep(0.035,0.18,max(_PulseAge,0));
                float arrival=1-smoothstep(0.38+travel*0.67,0.68+travel*0.67,distanceFromSun);
                // Filled broad propagation: farther surfaces arrive as inner surfaces relax.
                // This changes lighting only. There is no annulus and no expanding cloud UV.
                float outward=arrival*lerp(1,0.28+1.42*smoothstep(0.25,0.90,distanceFromSun),travel);
                // Spread follows the driver's held/extended pulse through its new endpoint;
                // early travel still uses the unchanged .035-.18s real callback age.
                float spreadPulse=pulse*(1+0.75*travel);
                float cloudLight=litSurface*(0.08+lerp(pulse*inner*1.65,spreadPulse*outward*2.10,_State.w));
                float3 warmSurface=1-exp(-float3(1,0.63,0.20)*cloudLight*lightEnvelope);
                cloudColor+=(1-cloudColor)*warmSurface;
                // Thick clouds sit in front of sun/shafts exactly once, including dark cores.
                color=lerp(color,cloudColor,cloudAlpha);

                // Only two low, thin foreground wisps use the old data atlas.
                float4 fogLeft=mist(fxUV,float2(0.23,0.215),float2(0.76,0.21),1,0.035);
                float4 fogRight=mist(fxUV,float2(0.77,0.245),float2(0.70,0.18),-1,-0.045);
                float fogDensity=saturate(max(fogLeft.a,fogRight.a)/0.86);
                // Visible cold air between feet, with a low actor-core budget and no solid
                // horizontal white strip. Texture gaps and unequal bank heights remain.
                float fogAlpha=fogDensity*lerp(0.21,0.025,bodyProtection)*envelope*hud;
                float3 fogColor=float3(0.48,0.57,0.64);
                fogColor+=(1-fogColor)*(1-exp(-float3(1,0.80,0.45)*(beamLight*1.1+0.07*pulse)*lightEnvelope));
                color=lerp(color,fogColor,fogAlpha);

                float local=0;
                [loop] for(int h=0;h<16;h++)
                {
                    if(h>=_PointCount) break;
                    float2 d=(screenUV-_HitPoints[h].xy)*float2(_Aspect,1);
                    float2 slash=float2(d.x*0.83+d.y*0.55,-d.x*0.55+d.y*0.83);
                    float streak=exp(-pow(slash.x/0.043,2)-pow(slash.y/0.0037,2));
                    float soft=exp(-dot(d,d)/0.00036)*0.18;
                    // max, never summed: crowded successes cannot multiply the full-screen flash.
                    local=max(local,streak*0.60+soft);
                }
                color=1-(1-color)*(1-float3(1,0.96,0.82)*saturate(local*_HitStrength));
                return float4(color,source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
