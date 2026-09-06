Shader "Steria/VeliaTideMist"
{
    Properties
    {
        _MainTex("Unmodified camera source",2D)="white"{}
        _MistAtlas("Read-only imported density and lighting atlas",2D)="black"{}
        _State("Envelope pulse explicit-clock second-die",Vector)=(0,0,0,0)
        _PulseAge("Seconds since current callback, explicit driver clock",Float)=-1
        _SunReveal("Delayed sun reveal",Float)=0
        _HitStrength("Local successful hits",Float)=0
        _Aspect("Viewport aspect",Float)=1.777778
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
            sampler2D _MainTex, _MistAtlas;
            float4 _MainTex_TexelSize, _MistAtlas_TexelSize, _State;
            float _SunReveal, _HitStrength, _Aspect, _PulseAge;
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
                float drift=sin(_State.z*0.16)*0.012;
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
            float beam(float2 uv,float slope,float startX,float width)
            {
                float y=0.98-uv.y;
                float x=uv.x-(startX+slope*y);
                return exp(-x*x/(width*width))*smoothstep(0.08,0.24,y)*(1-smoothstep(0.35,0.85,y));
            }
            float4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.sourceUV);
                float2 uv=i.viewportUV;
                float envelope=saturate(_State.x), pulse=saturate(_State.y);
                float protection=0;
                [loop] for(int r=0;r<16;r++) { if(r>=_ProtectionCount) break; protection=max(protection,softBody(uv,_ProtectionRects[r])); }
                // Conservatively preserve the central character band and the HUD margins too.
                float band=smoothstep(0.18,0.30,uv.y)*(1-smoothstep(0.49,0.61,uv.y));
                float hud=smoothstep(0.055,0.15,uv.y)*(1-smoothstep(0.91,0.99,uv.y));
                // Body interiors still receive at least 25% of the light. Combine protection
                // fields with max, not multiplication, so the band cannot cut another hole.
                float lightBand=exp(-pow((uv.y-0.38)/0.18,2));
                float preserve=(1-0.75*max(protection,lightBand*0.55))*hud;
                // Different-height banks enter the frame without adding another opacity layer.
                float4 left=mist(uv,float2(0.16,0.60),float2(0.80,0.89),1,0.34);
                float4 right=mist(uv,float2(0.84,0.52),float2(0.86,0.92),-1,-0.22);
                // The existing third bank crests across the sun's lower-left edge. Its
                // coverage, baked light and shadow all come from the same authored atlas.
                float4 low=mist(uv,float2(0.41,0.47),float2(0.78,0.47),1,0.08);
                // Middle-frame A peaks at .83-.87; normalize once, then cap the combined
                // mist amount rather than independently stacking opacity from each bank.
                float lowDensity=low.a*0.90;
                float density=saturate(max(max(left.a,right.a),lowDensity)/0.86);
                float3 cloudData=(left.rgb*left.a+right.rgb*right.a+low.rgb*lowDensity)/max(left.a+right.a+lowDensity,0.0001);
                float lighting=cloudData.g;
                float absorption=cloudData.r;
                // Baked light versus absorption separates the cool belly from the lit surface.
                float cloudShade=saturate(lighting*0.85-absorption*0.35);
                float3 cold=lerp(float3(0.204,0.294,0.376),float3(0.56,0.65,0.706),cloudShade);
                // R5 approved opacity budget: unoccupied cloud bodies may reach .48.
                // Registered bodies receive the existing .08 light mist through a wider
                // elliptical feather; the global horizontal band is fallback only.
                float mistProtection=_ProtectionCount>0 ? 1-pow(1-protection,3) : band;
                float mistAlpha=density*lerp(0.48,0.08,mistProtection)*envelope*hud;
                float cloudPreserve=(1-0.75*(_ProtectionCount>0 ? protection : lightBand*0.55))*hud;
                float3 color=source.rgb;

                float expand=1+0.15*_State.w*pulse;
                float2 sunQ=(uv-float2(0.50,0.72))*float2(1,_Aspect>0 ? 1/_Aspect : 0.5625)/(0.225*expand);
                float radius2=dot(sunQ,sunQ);
                float lowerCut=smoothstep(0.36,0.69,uv.y-density*0.10);
                // Continuous Gaussian energy from a small warm-white core through a golden
                // shoulder to a much weaker halo. No uniform disk or channel-clipped plateau.
                float sunBody=exp(-radius2*3.2)*lowerCut;
                float sunCore=exp(-radius2*8.5)*lowerCut;
                float halo=exp(-radius2*1.1)*lowerCut;
                float occlusion=exp(-density*1.1);
                float beams=beam(uv,-0.45,0.43,0.058)+beam(uv,0.32,0.56,0.072)+beam(uv,0.10,0.51,0.042)*0.40;
                // Cloud illumination has its own much wider field. It is not multiplied by
                // the narrow solar halo, which previously left the banks effectively unlit.
                float2 cloudQ=(uv-float2(0.50,0.69))/float2(0.58,0.68);
                float cloudDistance=length(cloudQ);
                float cloudField=exp(-dot(cloudQ,cloudQ)*0.55);
                // Narrow the warm-facing surface continuously, retaining the opaque cool
                // belly underneath; no binary threshold or independent glowing ribbon.
                float litSurface=pow(saturate(lighting*(1-absorption*0.35)),2.2);
                litSurface*=1-cloudData.b*0.80;
                float innerCloud=exp(-pow((abs(uv.x-0.5)-0.23)/0.15,2))*smoothstep(0.27,0.58,uv.y);
                float front=0.63+0.55*smoothstep(0.035,0.18,max(_PulseAge,0));
                // A filled, broadly feathered arrival field, NEVER the difference of two
                // radii: there is no ring silhouette, and it exists only on authored clouds.
                float arrival=1-smoothstep(front,front+0.28,cloudDistance);
                float rimPulse=pulse*lerp(innerCloud,arrival,_State.w);
                float cloudRim=density*litSurface*cloudField*(0.045+0.90*rimPulse);
                float warmWeight=(sunBody*(0.21+0.95*pulse)+halo*(0.025+0.080*pulse))*occlusion;
                warmWeight+=beams*(0.030+0.080*pulse)*(1-density*0.6);
                warmWeight*=envelope*_SunReveal*preserve;
                float coreWeight=sunCore*(0.16+3.25*pulse)*envelope*_SunReveal*preserve*occlusion;
                float3 energy=float3(1,0.64,0.28)*warmWeight+float3(1,0.93,0.78)*coreWeight;
                float3 emission=1-exp(-energy);
                // Soft global gain also avoids clipping fine source detail in bright UI/skin.
                color+=color*(1-color)*(0.06*pulse*envelope*preserve);
                color=color+(1-color)*emission;
                // Place the cloud body in front of the light, so its natural contour can
                // interrupt the lower sun instead of being washed out by a later sun add.
                // All solar energy/propagation curves remain the approved R4 values.
                color=color*(1-mistAlpha)+cold*mistAlpha;
                float3 cloudEmission=1-exp(-float3(1,0.82,0.50)*cloudRim*envelope*_SunReveal*cloudPreserve);
                color=color+(1-color)*cloudEmission;

                float local=0;
                [loop] for(int h=0;h<16;h++)
                {
                    if(h>=_PointCount) break;
                    float2 d=(uv-_HitPoints[h].xy)*float2(_Aspect,1);
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
