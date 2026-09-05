using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>R3 shared enclosing bubble, foam transport and flexible circumferential rupture.</summary>
public sealed class SlazeyaStormVisualController : IDisposable
{
    public const string BundleName="steria_slazeya_storm_mass";
    public const string PrefabName="SlazeyaStormMassPrefab";
    public const string ShaderName="Steria/SlazeyaStormFlow";
    public const string ParticleShaderName="Steria/SlazeyaStormParticles";
    public const string LightningShaderName="Steria/SlazeyaStormLightning";
    public const string Version="2026-09-05.round3.8";
    public const float GatherDuration = 1.35f;
    public const float TailDuration = 1.20f;
    public const int SectorCount=14;
    public static readonly string[] ParticleNames={"GatherMist","CrestSpindrift","BurstSheets","BurstDroplets","ReturnMist","CrestLiftMist","GatherFoam","WaterfallStreaks"};
    [Serializable] public struct Footprint
    {
        public Vector3 Center;
        public float RadiusX,RadiusZ,Height;
        public Vector3 EnvelopeCenter,EnvelopeRadii;
        public float GroundY,MaxNormalizedRadius,BandY;
    }
    public static bool IsFinite(Vector3 v)
    {
        return !(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z));
    }
    public static Bounds ConservativeBody(Vector3 foot,float height)
    {
        return new Bounds(foot+Vector3.up*height*0.5f,new Vector3(height*0.60f,height,height*0.40f));
    }
    public static Vector3[] BodySamples(Vector3 foot,Bounds body)
    {
        var samples=new Vector3[10];samples[0]=foot;samples[1]=new Vector3(body.center.x,body.max.y,body.center.z);
        for(int i=0;i<8;i++) samples[i+2]=new Vector3((i&1)==0?body.min.x:body.max.x,(i&2)==0?body.min.y:body.max.y,(i&4)==0?body.min.z:body.max.z);
        return samples;
    }
    public static Footprint FitFootprint(Vector3[] feet,float height)
    {
        if(feet==null)throw new ArgumentNullException("feet");
        var body=new Bounds[feet.Length];for(int i=0;i<feet.Length;i++)body[i]=ConservativeBody(feet[i],height);
        return FitFootprint(feet,height,body);
    }
    public static Footprint FitFootprint(Vector3[] feet,float height,Bounds[] bodyBounds)
    {
        if(feet==null||feet.Length==0||bodyBounds==null||bodyBounds.Length!=feet.Length||height<=0||float.IsNaN(height)||float.IsInfinity(height))
            throw new ArgumentException("Need corresponding finite feet / world body bounds and a positive reference height.");
        Vector3 footMin=feet[0],footMax=feet[0];var ys=new float[feet.Length];var samples=new List<Vector3>();
        for(int i=0;i<feet.Length;i++)
        {
            if(!IsFinite(feet[i])||!IsFinite(bodyBounds[i].center)||!IsFinite(bodyBounds[i].size)||bodyBounds[i].size.y<=0)throw new ArgumentException("Invalid body sample; caller must supply an explicit conservative fallback.");
            footMin=Vector3.Min(footMin,feet[i]);footMax=Vector3.Max(footMax,feet[i]);ys[i]=feet[i].y;samples.AddRange(BodySamples(feet[i],bodyBounds[i]));
        }
        Array.Sort(ys);int mid=ys.Length/2;float medianY=ys.Length%2==1?ys[mid]:(ys[mid-1]+ys[mid])*0.5f;
        var f=new Footprint{Center=new Vector3((footMin.x+footMax.x)*0.5f,medianY+height*0.012f,(footMin.z+footMax.z)*0.5f),Height=height,
            RadiusX=Mathf.Max(1.2f*height,(footMax.x-footMin.x)*0.5f+0.6f*height),RadiusZ=Mathf.Max(0.8f*height,(footMax.z-footMin.z)*0.5f+0.6f*height)};
        float groundExpansion=1;
        foreach(var p in feet){float x=(p.x-f.Center.x)/f.RadiusX,z=(p.z-f.Center.z)/f.RadiusZ;groundExpansion=Mathf.Max(groundExpansion,Mathf.Sqrt(x*x+z*z)*1.025f);}
        f.RadiusX*=groundExpansion;f.RadiusZ*=groundExpansion;
        Vector3 min=samples[0],max=samples[0];foreach(var p in samples){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
        f.EnvelopeCenter=(min+max)*0.5f;
        float ry=Mathf.Max(0.85f*height,(max.y-min.y)*0.5f/0.70f);
        float a=Mathf.Max(f.RadiusX,(max.x-min.x)*0.5f+0.15f*height),c=Mathf.Max(f.RadiusZ,(max.z-min.z)*0.5f+0.15f*height),k=1;
        foreach(var p in samples)
        {
            Vector3 d=p-f.EnvelopeCenter;float ny=d.y/ry;
            k=Mathf.Max(k,Mathf.Sqrt((d.x*d.x/(a*a)+d.z*d.z/(c*c))/(0.81f-ny*ny)));
        }
        f.EnvelopeRadii=new Vector3(a*k,ry,c*k);
        float normalizedRadius=0;foreach(var p in samples)normalizedRadius=Mathf.Max(normalizedRadius,NormalizedRadius(f,p));
        if(normalizedRadius>0.90f)f.EnvelopeRadii*=normalizedRadius/0.90f;
        f.MaxNormalizedRadius=0;foreach(var p in samples)f.MaxNormalizedRadius=Mathf.Max(f.MaxNormalizedRadius,NormalizedRadius(f,p));
        f.GroundY=Mathf.Min(min.y,footMin.y)-0.02f*height;
        f.BandY=footMin.y+0.58f*height;
        return f;
    }
    public static float NormalizedRadius(Footprint f,Vector3 p)
    {
        Vector3 d=p-f.EnvelopeCenter;Vector3 r=f.EnvelopeRadii;
        return Mathf.Sqrt(d.x*d.x/(r.x*r.x)+d.y*d.y/(r.y*r.y)+d.z*d.z/(r.z*r.z));
    }
    public static Vector2 GroundSection(Footprint f,float worldY)
    {
        float y=(worldY-f.EnvelopeCenter.y)/f.EnvelopeRadii.y;
        float factor=Mathf.Sqrt(Mathf.Max(0,1-y*y));return new Vector2(f.EnvelopeRadii.x*factor,f.EnvelopeRadii.z*factor);
    }
    public static Vector3 RingNormal(Footprint f,float theta) {return new Vector3(Mathf.Cos(theta)/f.EnvelopeRadii.x,0,Mathf.Sin(theta)/f.EnvelopeRadii.z).normalized;}
    public static Vector3 RingTangent(Footprint f,float theta) {return new Vector3(-f.EnvelopeRadii.x*Mathf.Sin(theta),0,f.EnvelopeRadii.z*Mathf.Cos(theta)).normalized;}
    public static Vector3 BandPoint(Footprint f,float theta)
    {
        Vector2 section=GroundSection(f,f.BandY);return new Vector3(f.EnvelopeCenter.x+section.x*Mathf.Cos(theta),f.BandY,f.EnvelopeCenter.z+section.y*Mathf.Sin(theta));
    }
    private static float Ease(float t) {t=Mathf.Clamp01(t);return t*t*(3-2*t);}
    private static float Out(float t) {t=Mathf.Clamp01(t);return 1-(1-t)*(1-t);}
    public static float ClosedScale(float gather) {return 1.14f-0.14f*Ease((gather*GatherDuration-1.02f)/0.21f);}
    // The same parameter equations are used by the native shader. All closed-state motion is outward.
    public static Vector3 BubblePoint(Footprint f,float theta,float q,float sector,float low,float high,bool cap,float gather,float phase,float burstAge)
    {
        Vector3 r=f.EnvelopeRadii;float scale=ClosedScale(gather);
        float ymin=(f.GroundY-f.EnvelopeCenter.y)/(r.y*scale);
        float phi0=Mathf.Asin(Mathf.Clamp(ymin,-0.999f,0.999f)),phi=Mathf.Lerp(phi0,Mathf.PI*0.5f,q);
        float warp=1+0.018f*(0.5f+0.5f*Mathf.Sin(theta*5+phase*2)*Mathf.Cos(phi*3+phase))*Mathf.Pow(Mathf.Sin(Mathf.PI*q),2);
        Vector3 p=cap?new Vector3(r.x*scale*Mathf.Sqrt(1-ymin*ymin)*q*Mathf.Cos(theta),f.GroundY-f.EnvelopeCenter.y,r.z*scale*Mathf.Sqrt(1-ymin*ymin)*q*Mathf.Sin(theta))
            :new Vector3(r.x*scale*Mathf.Cos(phi)*Mathf.Cos(theta)*warp,r.y*scale*Mathf.Sin(phi)*warp,r.z*scale*Mathf.Cos(phi)*Mathf.Sin(theta)*warp);
        if(burstAge>=0)
        {
            float progress=Out((burstAge-(0.012f+0.022f*(0.5f+0.5f*Mathf.Sin(sector*9))))/0.20f);
            Vector3 n=RingNormal(f,sector),t=RingTangent(f,sector);
            float region=(low+high)*0.5f;
            Vector3 hinge=BandPoint(f,sector)-f.EnvelopeCenter;
            Vector3 d=p-hinge;float radial=Vector3.Dot(d,n),vertical=d.y;
            float bend=progress*(cap?0.1f:0.55f+0.80f*region)*Ease((q-low)/Mathf.Max(0.001f,high-low));
            float newRadial=radial*Mathf.Cos(bend)+vertical*Mathf.Sin(bend);
            float bandRadius=(GroundSection(f,f.BandY).x+GroundSection(f,f.BandY).y)*0.5f;
            p+=n*(newRadial-radial)*0.45f;
            p.y=cap?f.GroundY-f.EnvelopeCenter.y:hinge.y+vertical*Mathf.Cos(bend)+radial*Mathf.Sin(bend)*(f.Height/bandRadius)*0.25f;
            p+=n*bandRadius*(cap?0.44f:0.34f+region*0.28f)*progress+t*f.Height*0.14f*progress;
            p.y-=f.Height*2.8f*Mathf.Pow(Mathf.Max(0,burstAge-0.16f),2);
        }
        return f.EnvelopeCenter+p;
    }
    public static Vector3 RingPoint(Footprint f,float theta,float q,float burstAge)
    {
        float progress=Out(burstAge/0.25f);Vector2 section=GroundSection(f,f.BandY);
        float strength=RingLobes(theta),fall=Mathf.Max(0,burstAge-0.12f);
        float side=1-0.68f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
        float width=0.10f+0.39f*strength+0.02f*Mathf.Sin(theta*11);
        // A continuously outward-sloping sheet: no vertical rolled-over cuff at the outer edge.
        float radius=1+progress*(0.025f+q*width)*side+fall*0.10f*side;
        float crest=0.18f*strength*Mathf.Sin(q*Mathf.PI);
        float y=f.BandY+f.Height*progress*(crest-0.53f*Mathf.Pow(q,1.25f)-0.11f*(1-strength));
        y-=f.Height*(fall*fall*(2.8f+2*q)+Ease((burstAge-0.27f)/0.30f)*0.16f);
        y+=f.Height*0.030f*progress*Mathf.Sin(theta*23+q*9-burstAge*12)*Mathf.Sin(q*Mathf.PI);
        float angle=theta+progress*q*(0.035f+0.065f*strength);
        return new Vector3(f.EnvelopeCenter.x+section.x*radius*Mathf.Cos(angle),Mathf.Max(f.GroundY+0.015f*f.Height,y),f.EnvelopeCenter.z+section.y*radius*Mathf.Sin(angle));
    }
    private static float RingLobe(float theta,float center,float width)
    {
        float distance=Mathf.Atan2(Mathf.Sin(theta-center),Mathf.Cos(theta-center));
        return Mathf.Exp(-distance*distance/(width*width));
    }
    private static float RingLobes(float theta)
    {
        return Mathf.Max(RingLobe(theta,2.25f,0.48f),Mathf.Max(0.90f*RingLobe(theta,3.72f,0.38f),Mathf.Max(0.80f*RingLobe(theta,5.10f,0.46f),0.60f*RingLobe(theta,0.70f,0.32f))));
    }

    private readonly GameObject _root;
    private readonly Footprint _f;
    private readonly Renderer _bubble,_ring;
    private readonly Renderer[] _bolts,_renderers;
    private readonly ParticleSystem[] _particles;
    private readonly ParticleSystem.Particle[] _foamParcels=new ParticleSystem.Particle[168];
    private readonly MaterialPropertyBlock _block=new MaterialPropertyBlock();
    private readonly bool[] _boltAnchored=new bool[2],_jetReleased=new bool[SectorCount];
    private readonly bool[] _waterfallReleased=new bool[3];
    private float _phase,_burstAge,_burstPhase,_emitCredit,_capturedGather;
    private int _emitted;
    private bool _disposed,_primaryLiftReleased,_secondaryLiftReleased,_primaryReturnReleased,_secondaryReturnReleased;
    private Vector3 _lightningPoint;
    public float Elapsed {get;private set;}
    public bool GatherReady {get{return Elapsed>=GatherDuration;}}
    public bool BurstTriggered {get;private set;}
    public bool IsComplete {get;private set;}
    public float BurstAge {get{return BurstTriggered?_burstAge:-1;}}
    public float Phase {get{return _phase;}}
    public float ShapePhase {get{return BurstTriggered?_burstPhase:_phase;}}
    public float RadiusRatio {get;private set;}
    public float LightningEnergy {get;private set;}
    public float FoamGather {get{return Ease(((BurstTriggered?_capturedGather:Elapsed)-0.82f)/0.30f);}}
    public float Charge {get{return BurstTriggered?0:Ease((Elapsed-1.20f)/0.12f)*(1-0.65f*Ease((Elapsed-1.35f)/0.10f));}}
    public int AliveParticles {get{int n=0;if(_particles!=null)foreach(var p in _particles)if(p!=null)n+=p.particleCount;return n;}}
    public SlazeyaStormVisualController(GameObject root,Footprint footprint)
    {
        _root=root;_f=footprint;if(root==null)return;
        root.transform.SetParent(null,true);root.transform.position=footprint.Center;root.transform.rotation=Quaternion.identity;root.transform.localScale=Vector3.one;
        Transform storm=Required("AB_StormRoot"),ring=Required("AB_BurstRoot/RingJet");
        storm.localPosition=footprint.EnvelopeCenter-footprint.Center;storm.localScale=footprint.EnvelopeRadii;
        ring.localPosition=footprint.EnvelopeCenter-footprint.Center;ring.localScale=footprint.EnvelopeRadii;
        _bubble=Required("AB_StormRoot/BubbleEnvelope").GetComponent<Renderer>();_ring=ring.GetComponent<Renderer>();
        _bolts=new[]{Required("AB_BurstRoot/LightningRoot/PrimaryBolt").GetComponent<Renderer>(),Required("AB_BurstRoot/LightningRoot/SecondaryBolt").GetComponent<Renderer>()};
        _particles=new ParticleSystem[ParticleNames.Length];
        for(int i=0;i<_particles.Length;i++){var p=Required("AB_ParticleRoot/"+ParticleNames[i]).GetComponent<ParticleSystem>();_particles[i]=p;p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.loop=false;main.playOnAwake=false;var emission=p.emission;emission.enabled=false;p.Pause(false);}
        _renderers=root.GetComponentsInChildren<Renderer>(true);ApplyVisuals();
    }
    private Transform Required(string path){var t=_root.transform.Find(path);if(t==null)throw new InvalidOperationException("R3 prefab missing "+path);return t;}
    public bool TriggerBurst()
    {
        if(_disposed||IsComplete||BurstTriggered)return false;
        _burstPhase=_phase;_capturedGather=Elapsed;BurstTriggered=true;_burstAge=0;
        if(_root!=null){EmitSector(5);_jetReleased[5]=true;ApplyVisuals();}return true;
    }
    public void Advance(float deltaTime)
    {
        if(_disposed||IsComplete||deltaTime<=0||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime))return;
        Elapsed+=deltaTime;_phase+=deltaTime*(BurstTriggered?0.65f:0.50f+0.70f*Mathf.Clamp01(Elapsed/GatherDuration));if(BurstTriggered)_burstAge+=deltaTime;
        if(_root!=null)
        {
            if(!BurstTriggered)
            {
                _emitCredit+=deltaTime*26;int count=Mathf.Min(12,(int)_emitCredit);_emitCredit-=(int)_emitCredit;
                for(int i=0;i<count;i++)EmitSpray(1,_emitted++,(_emitted*2.399963f)%(Mathf.PI*2));
            }
            else
            {
                for(int i=0;i<SectorCount;i++)if(!_jetReleased[i]&&_burstAge>=0.02f+0.025f*Hash(i,4)){_jetReleased[i]=true;if(_burstAge<0.16f)EmitSector(i);}
                for(int i=0;i<3;i++)if(!_waterfallReleased[i]&&_burstAge>=0.06f+i*0.09f){_waterfallReleased[i]=true;if(_burstAge<0.36f)EmitWaterfallStreaks(i);}
                if(!_primaryLiftReleased&&_burstAge>=0.04f){_primaryLiftReleased=true;if(_burstAge<0.18f)EmitMist(5,12,0);}
                if(!_secondaryLiftReleased&&_burstAge>=0.11f){_secondaryLiftReleased=true;if(_burstAge<0.24f)EmitMist(5,8,1);}
                if(!_primaryReturnReleased&&_burstAge>=0.36f){_primaryReturnReleased=true;if(_burstAge<0.6f)EmitMist(4,12,0);}
                if(!_secondaryReturnReleased&&_burstAge>=0.50f){_secondaryReturnReleased=true;if(_burstAge<0.7f)EmitMist(4,10,1);}
            }
            foreach(var p in _particles)p.Simulate(deltaTime, false, false, false);
            UpdateFoamParcels();
            ApplyVisuals();
        }
        if(BurstTriggered&&_burstAge>=TailDuration){IsComplete=true;Clear();}
    }
    private void ApplyVisuals()
    {
        ApplyLightning();float gather=Mathf.Clamp01((BurstTriggered?_capturedGather:Elapsed)/GatherDuration);
        RadiusRatio=BurstTriggered?1+0.52f*Out(_burstAge/0.23f):ClosedScale(gather);
        _block.Clear();_block.SetFloat("_Phase",_phase);_block.SetFloat("_FoamGather",FoamGather);_block.SetFloat("_Gather",gather);_block.SetFloat("_Charge",Charge);
        _block.SetFloat("_BurstAge",BurstAge);_block.SetFloat("_Rupture",BurstTriggered?Mathf.Clamp01(_burstAge/0.10f):0);
        _block.SetVector("_Radii",_f.EnvelopeRadii);_block.SetFloat("_Height",_f.Height);
        _block.SetFloat("_Ground",_f.GroundY-_f.EnvelopeCenter.y);_block.SetFloat("_Band",_f.BandY-_f.EnvelopeCenter.y);
        _block.SetFloat("_Alpha",Mathf.Clamp01(Elapsed/0.18f));
        _block.SetVector("_LightningPosition",_lightningPoint+_f.Center);_block.SetFloat("_LightningEnergy",LightningEnergy);_block.SetFloat("_LightningRadius",_f.Height*0.6f);
        _bubble.SetPropertyBlock(_block);_ring.SetPropertyBlock(_block);
        _bubble.enabled=Elapsed>0&&(!BurstTriggered||_burstAge<0.57f);_ring.enabled=BurstTriggered&&_burstAge<0.72f;
        for(int i=0;i<_particles.Length;i++){_block.Clear();_block.SetFloat("_Phase",_phase);_block.SetFloat("_Alpha",i==0?0.20f:i==4?0.34f:i==5?0.24f:i==6?0.85f+0.15f*Charge:i==7?1:0.9f);_particles[i].GetComponent<Renderer>().SetPropertyBlock(_block);}
    }
    // Lagrangian parcels: fixed identities move on the actual closed shell, then inherit outward momentum.
    // No new latitude-only emitter or UV brightness mask substitutes for this material transport.
    public Vector3 FoamParcelPosition(int index)
    {
        float age=BurstTriggered?_capturedGather:Elapsed,phase=BurstTriggered?_burstPhase:_phase;
        float scale=ClosedScale(Mathf.Clamp01(age/GatherDuration));
        float phi0=Mathf.Asin(Mathf.Clamp((_f.GroundY-_f.EnvelopeCenter.y)/(_f.EnvelopeRadii.y*scale),-0.999f,0.999f));
        float bandPhi=Mathf.Asin(Mathf.Clamp((_f.BandY-_f.EnvelopeCenter.y)/(_f.EnvelopeRadii.y*scale),-0.999f,0.999f));
        float bandQ=(bandPhi-phi0)/(Mathf.PI*0.5f-phi0);
        float originQ=0.035f+0.89f*Hash(index,47);
        float theta=index*2.399963f+phase*0.68f+(originQ-bandQ)*FoamGather*0.75f;
        float q=Mathf.Lerp(originQ,bandQ+(Hash(index,48)-0.5f)*0.024f,FoamGather);
        Vector3 p=BubblePoint(_f,theta,q,theta,0,1,false,Mathf.Clamp01(age/GatherDuration),phase,-1);
        p+=RingNormal(_f,theta)*_f.Height*0.012f;
        if(BurstTriggered)
        {
            float t=_burstAge;
            float side=1-0.60f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
            p+=_f.Height*(RingNormal(_f,theta)*(3.2f+1.5f*Hash(index,49))*side*t/(1+1.7f*t)
                +RingTangent(_f,theta)*0.28f*t+Vector3.up*(0.55f*t-3.7f*t*t));
            p.y=Mathf.Max(_f.GroundY+0.02f*_f.Height,p.y);
        }
        return p;
    }
    private void UpdateFoamParcels()
    {
        if(BurstTriggered&&_burstAge>0.65f){_particles[6].SetParticles(_foamParcels,0);return;}
        float visible=Ease(Elapsed/0.22f)*(BurstTriggered?1-Ease((_burstAge-0.18f)/0.47f):1);
        for(int i=0;i<_foamParcels.Length;i++)
        {
            _foamParcels[i].position=FoamParcelPosition(i)-_f.Center;
            _foamParcels[i].velocity=Vector3.zero;
            _foamParcels[i].startLifetime=8;
            _foamParcels[i].remainingLifetime=8*(0.64f-0.16f*Hash(i,51));
            _foamParcels[i].startSize=_f.Height*(0.09f+0.12f*Hash(i,52))*(1-0.12f*FoamGather);
            _foamParcels[i].startColor=new Color(1,1,1,visible*(0.62f+0.28f*Hash(i,53)));
            _foamParcels[i].rotation=Hash(i,54)*360+_phase*22;
            _foamParcels[i].randomSeed=(uint)(i+7141);
        }
        _particles[6].SetParticles(_foamParcels,_foamParcels.Length);
    }
    private static float Pulse(float age,float start,float strength){float t=age-start;if(t<0||t>=0.05f)return 0;if(t<0.016f)return strength*Mathf.Lerp(0.32f,1,t/0.016f);return strength*(1-Mathf.Clamp01((t-0.033f)/0.017f));}
    private void ApplyLightning()
    {
        LightningEnergy=0;
        for(int i=0;i<2;i++)
        {
            float energy=BurstTriggered?Pulse(_burstAge,i==0?0:0.10f,i==0?1:0.60f):0;
            if(energy>0&&!_boltAnchored[i])
            {
                _boltAnchored[i]=true;float angle=i==0?2.3f:5.6f;Vector3 lip=BandPoint(_f,angle)-_f.Center,n=RingNormal(_f,angle);
                Vector3 start=lip-n*_f.Height*(i==0?0.65f:0.42f)+Vector3.up*_f.Height*0.2f;
                Vector3 dir=(lip-start).normalized,view=new Vector3(0,0.402f,-0.916f),normal=Vector3.Cross(dir,Vector3.Cross(view,dir)).normalized;
                _bolts[i].transform.localPosition=start;_bolts[i].transform.localRotation=Quaternion.LookRotation(normal,Vector3.Cross(normal,dir));_bolts[i].transform.localScale=Vector3.one*Vector3.Distance(start,lip);_lightningPoint=Vector3.Lerp(start,lip,0.75f);
            }
            _block.Clear();_block.SetFloat("_Pulse",energy);_bolts[i].SetPropertyBlock(_block);_bolts[i].enabled=energy>0;LightningEnergy+=energy;
        }
    }
    private static float Hash(int i,int salt){uint v=unchecked((uint)(i*1973+salt*9277+89173));v^=v<<13;v^=v>>17;v^=v<<5;return(v&65535u)/65535f;}
    private void EmitSector(int sector)
    {
        float angle=(sector+0.5f)/SectorCount*Mathf.PI*2;
        float strength=RingLobes(angle);
        if(strength>0.25f)EmitSpray(2,sector,angle);
        int droplets=3+Mathf.RoundToInt(strength*9);
        for(int j=0;j<droplets;j++)EmitSpray(3,sector*12+j,angle+(Hash(j,sector+1)-0.5f)*0.26f);
    }
    private void EmitWaterfallStreaks(int batch)
    {
        float[] centers={2.25f,3.72f,5.10f,0.70f};
        for(int lobe=0;lobe<centers.Length;lobe++)for(int j=0;j<(lobe==3?3:6);j++)
        {
            int seed=batch*31+lobe*7+j;float h=_f.Height;
            float theta=centers[lobe]+(Hash(seed,71)-0.5f)*(lobe==0?0.64f:0.48f);
            float q=0.28f+0.43f*Hash(seed,72),side=1-0.60f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
            Vector3 n=RingNormal(_f,theta),t=RingTangent(_f,theta);
            Vector3 p=RingPoint(_f,theta,q,_burstAge)-_f.Center+Vector3.up*h*0.025f;
            Vector3 velocity=h*(n*(1.6f+Hash(seed,73)*1.2f)*side+t*0.12f-Vector3.up*(0.8f+Hash(seed,74)*0.9f));
            var emit=new ParticleSystem.EmitParams{position=p,velocity=velocity,startLifetime=0.28f+0.10f*Hash(seed,75),
                startSize=h*(0.075f+0.07f*Hash(seed,76)),startColor=Color.white,randomSeed=(uint)(seed+9301)};
            _particles[7].Emit(emit,1);
            // Small detached droplets share the falling tongue's actual position and momentum.
            emit.startSize=h*(0.020f+0.025f*Hash(seed,77));emit.startLifetime=0.30f+0.12f*Hash(seed,78);
            _particles[3].Emit(emit,1);
        }
    }
    private void EmitSpray(int system,int index,float theta)
    {
        float h=_f.Height;Vector3 n=RingNormal(_f,theta),t=RingTangent(_f,theta);
        Vector3 p=BandPoint(_f,theta)-_f.Center;
        if(!BurstTriggered)
        {
            float worldY=_f.BandY+h*(Hash(index,3)-0.5f)*0.8f*(1-FoamGather);
            Vector2 section=GroundSection(_f,worldY);
            p=new Vector3(_f.EnvelopeCenter.x-_f.Center.x+section.x*Mathf.Cos(theta),worldY-_f.Center.y,_f.EnvelopeCenter.z-_f.Center.z+section.y*Mathf.Sin(theta));
        }
        float side=1-0.60f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
        Vector3 velocity=BurstTriggered?h*((3.5f+2f*Hash(index,5))*side*n+(0.2f+0.4f*Hash(index,6))*t+(0.3f+0.6f*Hash(index,7))*Vector3.up):t*h*0.65f;
        float tilt=Mathf.Atan2(velocity.y*0.916f+velocity.z*0.402f,velocity.x)*Mathf.Rad2Deg-(system==2?55f:90f);
        var emit=new ParticleSystem.EmitParams{position=p,velocity=velocity,startLifetime=system==1?0.35f:system==2?0.32f+Hash(index,8)*0.24f:0.50f+Hash(index,9)*0.32f,
            startSize=h*(system==1?0.035f:system==2?0.48f+Hash(index,10)*0.35f:0.035f+Hash(index,11)*0.065f),startColor=Color.white,rotation=tilt+(Hash(index,12)-0.5f)*20,randomSeed=(uint)(index+101+system*1031)};
        _particles[system].Emit(emit,1);
    }
    private void EmitMist(int system,int count,int batch)
    {
        for(int i=0;i<count;i++)
        {
            float theta=(i+0.3f+batch*0.5f)/count*Mathf.PI*2,h=_f.Height;Vector3 n=RingNormal(_f,theta),t=RingTangent(_f,theta);
            Vector3 p=BandPoint(_f,theta)-_f.Center;
            if(system==4){p+=n*h*0.85f;p.y=_f.GroundY-_f.Center.y+0.05f*h;}else p+=n*h*0.12f;
            var emit=new ParticleSystem.EmitParams{position=p,velocity=h*(n*(system==4?0.16f:0.40f)+t*0.15f+Vector3.up*(system==4?0.03f:0.34f)),startSize=h*(system==4?0.55f:0.35f),
                startLifetime=system==4?0.50f+Hash(i,29)*0.08f:0.5f+Hash(i,28)*0.12f,startColor=Color.white,randomSeed=(uint)(1201+i+batch*157+system*11)};
            _particles[system].Emit(emit,1);
        }
    }
    private void Clear(){LightningEnergy=0;if(_particles!=null)foreach(var p in _particles)if(p!=null)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);if(_renderers!=null)foreach(var r in _renderers)if(r!=null)r.enabled=false;}
    public void Dispose(){if(_disposed)return;_disposed=true;IsComplete=true;Clear();}
}
