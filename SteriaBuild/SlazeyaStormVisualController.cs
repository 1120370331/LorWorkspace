using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>R5 open cloud clusters, three-dimensional core collapse and continuous waterfall release.</summary>
public sealed class SlazeyaStormVisualController : IDisposable
{
    public const string BundleName="steria_slazeya_storm_mass";
    public const string PrefabName="SlazeyaStormMassPrefab";
    public const string ShaderName="Steria/SlazeyaStormFlow";
    public const string CloudVolumeShaderName="Steria/SlazeyaStormCloudVolume";
    public const int MacroCount=1;
    public const string CloudShaderName="Steria/SlazeyaStormCloud";
    public const string ParticleShaderName="Steria/SlazeyaStormParticles";
    public const string LightningShaderName="Steria/SlazeyaStormLightning";
    public const string Version="2026-09-06.cloud-recovery.1";
    public const float GatherDuration = 1.35f;
    public const float TailDuration = 1.20f;
    public const int SectorCount=14;
    public static readonly string[] ParticleNames={"GatherMist","CrestSpindrift","BurstSheets","BurstDroplets","ReturnMist","CrestLiftMist","GatherFoam","WaterfallStreaks"};
    [Serializable] public struct Footprint
    {
        public Vector3 Center;
        public float RadiusX,RadiusZ,Height;
        public Vector3 EnvelopeCenter,EnvelopeRadii;
        public Vector2 CloudInnerRadii;
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
        float cloudExpansion=1;
        foreach(Vector3 p in samples)
        {
            float x=(p.x-f.EnvelopeCenter.x)/f.RadiusX,z=(p.z-f.EnvelopeCenter.z)/f.RadiusZ;
            cloudExpansion=Mathf.Max(cloudExpansion,Mathf.Sqrt(x*x+z*z)/0.95f);
        }
        f.CloudInnerRadii=new Vector2(f.RadiusX*cloudExpansion,f.RadiusZ*cloudExpansion);
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
    public static float CollapseAt(float age) {return Ease((age-0.82f)/0.46f);}
    public static float WaveProgress(float age) {return Ease(age/0.23f);}
    public static float ClosedScale(float gather) {return Mathf.Lerp(1.14f,0.06f,CollapseAt(gather*GatherDuration));}
    private static Vector3 RotateY(Vector3 p,float angle)
    {float c=Mathf.Cos(angle),s=Mathf.Sin(angle);return new Vector3(c*p.x+s*p.z,p.y,-s*p.x+c*p.z);}
    public static Vector3 CollapseOffset(Vector3 offset,float collapse,float height)
    {
        float length=offset.magnitude;Vector3 direction=length>0.000001f?offset/length:Vector3.down;
        return RotateY(direction,0.55f*collapse)*Mathf.Lerp(length,0.06f*height,collapse);
    }
    public static Vector3 BubblePoint(Footprint f,float theta,float q,float sector,float low,float high,bool cap,float gather,float phase,float burstAge)
    {
        // Construct the finite initial geometry FIRST; its cap rises with the same radial map.
        Vector3 r=f.EnvelopeRadii;const float scale=1.14f;
        float ymin=(f.GroundY-f.EnvelopeCenter.y)/(r.y*scale);
        float phi0=Mathf.Asin(Mathf.Clamp(ymin,-0.999f,0.999f)),phi=Mathf.Lerp(phi0,Mathf.PI*0.5f,q);
        float warp=1+0.018f*(0.5f+0.5f*Mathf.Sin(theta*5+phase*2)*Mathf.Cos(phi*3+phase))*Mathf.Pow(Mathf.Sin(Mathf.PI*q),2);
        Vector3 point=cap?new Vector3(r.x*scale*Mathf.Sqrt(1-ymin*ymin)*q*Mathf.Cos(theta),f.GroundY-f.EnvelopeCenter.y,r.z*scale*Mathf.Sqrt(1-ymin*ymin)*q*Mathf.Sin(theta))
            :new Vector3(r.x*scale*Mathf.Cos(phi)*Mathf.Cos(theta)*warp,r.y*scale*Mathf.Sin(phi)*warp,r.z*scale*Mathf.Cos(phi)*Mathf.Sin(theta)*warp);
        return f.EnvelopeCenter+CollapseOffset(point,CollapseAt(gather*GatherDuration),f.Height);
    }
    public static Vector3 CorePoint(Footprint f,float theta,float q)
    {
        float phi=(q-0.5f)*Mathf.PI;
        return f.EnvelopeCenter+new Vector3(Mathf.Cos(theta)*Mathf.Cos(phi),Mathf.Sin(phi),Mathf.Sin(theta)*Mathf.Cos(phi))*f.Height*0.06f;
    }
    public static Vector3 ExpandedRingPoint(Footprint f,float theta,float q,float burstAge)
    {
        float progress=Out(burstAge/0.25f);Vector2 section=new Vector2(f.EnvelopeRadii.x,f.EnvelopeRadii.z);
        float strength=RingLobes(theta),fall=Mathf.Max(0,burstAge-0.12f);
        float side=1-0.68f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
        float width=0.10f+0.39f*strength+0.02f*Mathf.Sin(theta*11);
        float radius=1+progress*(0.025f+q*width)*side+fall*0.10f*side;
        float crest=0.18f*strength*Mathf.Sin(q*Mathf.PI);
        float y=f.EnvelopeCenter.y+f.Height*progress*(crest-0.53f*Mathf.Pow(q,1.25f)-0.11f*(1-strength));
        y-=f.Height*(fall*fall*(2.8f+2*q)+Ease((burstAge-0.27f)/0.30f)*0.16f);
        y+=f.Height*0.030f*progress*Mathf.Sin(theta*23+q*9-burstAge*12)*Mathf.Sin(q*Mathf.PI);
        float angle=theta+progress*q*(0.035f+0.065f*strength);
        return new Vector3(f.EnvelopeCenter.x+section.x*radius*Mathf.Cos(angle),Mathf.Max(f.GroundY+0.015f*f.Height,y),f.EnvelopeCenter.z+section.y*radius*Mathf.Sin(angle));
    }
    public static Vector3 RingPoint(Footprint f,float theta,float q,float burstAge)
    {return Vector3.Lerp(CorePoint(f,theta,q),ExpandedRingPoint(f,theta,q,burstAge),WaveProgress(burstAge));}
    private Vector3 WaveVelocity(float theta,float q)
    {
        const float dt=0.001f;
        Vector3 v=(RingPoint(_f,theta,q,_burstAge+dt)-RingPoint(_f,theta,q,Mathf.Max(0,_burstAge-dt)))/(dt+Mathf.Min(dt,_burstAge));
        return Vector3.ClampMagnitude(v,_f.Height*9)*WaveProgress(_burstAge);
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
    private readonly Renderer _ring;
    private readonly Renderer[] _macros=new Renderer[MacroCount];
    private readonly Vector4[] _macroCenters=new Vector4[12],_macroAxisX=new Vector4[12],_macroAxisY=new Vector4[12],_macroAxisZ=new Vector4[12];
    private readonly CloudMacroPose[] _macroPoses=new CloudMacroPose[MacroCount];
    private readonly Renderer[] _bolts,_renderers;
    private readonly ParticleSystem[] _particles;
    private readonly ParticleSystem.Particle[] _foamParcels=new ParticleSystem.Particle[168];
    private readonly MaterialPropertyBlock _block=new MaterialPropertyBlock();
    private readonly bool[] _boltAnchored=new bool[2],_jetReleased=new bool[SectorCount];
    private readonly bool[] _waterfallReleased=new bool[3];
    private float _phase,_burstAge,_burstPhase,_capturedGather,_collapsePhase;
    private bool _collapseStarted;
    private bool _disposed,_primaryLiftReleased,_secondaryLiftReleased,_primaryReturnReleased,_secondaryReturnReleased;
    private Vector3 _lightningPoint;
    public float Elapsed {get;private set;}
    public bool GatherReady {get{return Elapsed>=GatherDuration;}}
    public bool BurstTriggered {get;private set;}
    public bool IsComplete {get;private set;}
    public float BurstAge {get{return BurstTriggered?_burstAge:-1;}}
    public float Phase {get{return _phase;}}
    public float ShapePhase {get{return BurstTriggered?_burstPhase:_collapseStarted?_collapsePhase:_phase;}}
    public Vector3 CoreCenter {get{return _f.EnvelopeCenter;}}
    public float CoreRadius {get{return 0.06f*_f.Height;}}
    public float CoreVisibleLimit {get{return 0.12f*_f.Height;}}
    public float CollapseProgress {get{return CollapseAt(BurstTriggered?_capturedGather:Elapsed);}}
    public float Propagation {get{return BurstTriggered?WaveProgress(_burstAge):0;}}
    public float RadiusRatio {get;private set;}
    public float LightningEnergy {get;private set;}
    public float FoamGather {get{return CollapseProgress;}}
    public float Charge {get{return BurstTriggered?0:Ease((Elapsed-1.20f)/0.12f)*(1-0.20f*Ease((Elapsed-1.35f)/0.10f));}}
    public int AliveParticles {get{int n=0;if(_particles!=null)foreach(var p in _particles)if(p!=null)n+=p.particleCount;return n;}}
    public SlazeyaStormVisualController(GameObject root,Footprint footprint)
    {
        _root=root;_f=footprint;if(root==null)return;
        root.transform.SetParent(null,true);root.transform.position=footprint.Center;root.transform.rotation=Quaternion.identity;root.transform.localScale=Vector3.one;
        Transform ring=Required("AB_BurstRoot/RingJet");
        ring.localPosition=footprint.EnvelopeCenter-footprint.Center;ring.localScale=footprint.EnvelopeRadii;
        _ring=ring.GetComponent<Renderer>();
        for(int i=0;i<MacroCount;i++)_macros[i]=Required("AB_StormRoot/CloudMacroRoot/CloudMacro_"+i.ToString("D2")).GetComponent<Renderer>();
        _bolts=new[]{Required("AB_BurstRoot/LightningRoot/PrimaryBolt").GetComponent<Renderer>(),Required("AB_BurstRoot/LightningRoot/SecondaryBolt").GetComponent<Renderer>()};
        _particles=new ParticleSystem[ParticleNames.Length];
        for(int i=0;i<_particles.Length;i++){var p=Required("AB_ParticleRoot/"+ParticleNames[i]).GetComponent<ParticleSystem>();_particles[i]=p;p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.loop=false;main.playOnAwake=false;var emission=p.emission;emission.enabled=false;p.Pause(false);}
        _renderers=root.GetComponentsInChildren<Renderer>(true);ApplyVisuals();
    }
    private Transform Required(string path){var t=_root.transform.Find(path);if(t==null)throw new InvalidOperationException("R3 prefab missing "+path);return t;}
    public bool TriggerBurst()
    {
        if(_disposed||IsComplete||BurstTriggered)return false;
        _burstPhase=ShapePhase;_capturedGather=Elapsed;BurstTriggered=true;_burstAge=0;
        if(_root!=null){EmitSector(5);_jetReleased[5]=true;SimulateParticles(0);UpdateFoamParcels();ApplyVisuals();}return true;
    }
    public void Advance(float deltaTime)
    {
        if(_disposed||IsComplete||deltaTime<=0||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime))return;
        Elapsed+=deltaTime;_phase+=deltaTime*(BurstTriggered?0.65f:0.50f+0.70f*Mathf.Clamp01(Elapsed/GatherDuration));if(BurstTriggered)_burstAge+=deltaTime;
        if(!_collapseStarted&&Elapsed>=0.82f){_collapseStarted=true;_collapsePhase=_phase;}
        if(_root!=null)
        {
            if(BurstTriggered)
            {
                for(int i=0;i<SectorCount;i++)if(!_jetReleased[i]&&_burstAge>=0.02f+0.025f*Hash(i,4)){_jetReleased[i]=true;if(_burstAge<0.16f)EmitSector(i);}
                for(int i=0;i<3;i++)if(!_waterfallReleased[i]&&_burstAge>=0.06f+i*0.09f){_waterfallReleased[i]=true;if(_burstAge<0.36f)EmitWaterfallStreaks(i);}
                if(!_primaryLiftReleased&&_burstAge>=0.04f){_primaryLiftReleased=true;if(_burstAge<0.18f)EmitMist(5,12,0);}
                if(!_secondaryLiftReleased&&_burstAge>=0.11f){_secondaryLiftReleased=true;if(_burstAge<0.24f)EmitMist(5,8,1);}
                if(!_primaryReturnReleased&&_burstAge>=0.36f){_primaryReturnReleased=true;if(_burstAge<0.6f)EmitMist(4,12,0);}
                if(!_secondaryReturnReleased&&_burstAge>=0.50f){_secondaryReturnReleased=true;if(_burstAge<0.7f)EmitMist(4,10,1);}
            }
            SimulateParticles(deltaTime);
            UpdateFoamParcels();
            ApplyVisuals();
        }
        if(BurstTriggered&&_burstAge>=TailDuration){IsComplete=true;Clear();}
    }
    private void ApplyVisuals()
    {
        ApplyLightning();ApplyMacroClouds();float gather=Mathf.Clamp01((BurstTriggered?_capturedGather:Elapsed)/GatherDuration);
        RadiusRatio=BurstTriggered?Mathf.Lerp(0.06f,1.52f,Propagation):ClosedScale(gather);
        _block.Clear();_block.SetFloat("_Phase",_phase);_block.SetFloat("_ShapePhase",ShapePhase);_block.SetFloat("_Collapse",CollapseProgress);_block.SetFloat("_CoreRadius",CoreRadius);_block.SetFloat("_FoamGather",FoamGather);_block.SetFloat("_Gather",gather);_block.SetFloat("_Charge",Charge);
        _block.SetFloat("_BurstAge",BurstAge);_block.SetFloat("_Rupture",BurstTriggered?Mathf.Clamp01(_burstAge/0.10f):0);
        _block.SetVector("_Radii",_f.EnvelopeRadii);_block.SetFloat("_Height",_f.Height);
        _block.SetFloat("_Ground",_f.GroundY-_f.EnvelopeCenter.y);_block.SetFloat("_Band",_f.BandY-_f.EnvelopeCenter.y);
        _block.SetFloat("_Alpha",Mathf.Clamp01(Elapsed/0.18f));
        _block.SetVector("_LightningPosition",_lightningPoint+_f.Center);_block.SetFloat("_LightningEnergy",LightningEnergy);_block.SetFloat("_LightningRadius",Mathf.Lerp(CoreRadius,_f.Height*0.6f,Propagation));
        _ring.SetPropertyBlock(_block);
        _ring.enabled=BurstTriggered&&_burstAge<0.72f;
        for(int i=0;i<_particles.Length;i++)
        {
            _block.Clear();_block.SetFloat("_Phase",_phase);
            _block.SetFloat("_Alpha",i==0?0.20f:i==4?0.34f:i==5?0.24f:i==6?1:i==7?1:0.9f);
            if(i==6)
            {
                _block.SetVector("_CloudCenter",CoreCenter);_block.SetFloat("_CloudHeight",_f.Height);
                _block.SetVector("_CloudInnerRadii",new Vector4(_f.CloudInnerRadii.x,_f.CloudInnerRadii.y,0,0));
                _block.SetFloat("_CloudSpinAngle",-ShapePhase*0.80f-0.55f*CollapseProgress);_block.SetFloat("_DensityPhase",Elapsed);
                _block.SetFloat("_Collapse",CollapseProgress);
                _block.SetFloat("_Charge",BurstTriggered?1-Ease(_burstAge/0.065f):Charge);
                _block.SetFloat("_WaterMix",BurstTriggered?Ease(_burstAge/0.10f):0);
                _block.SetVector("_LightningPosition",_lightningPoint+_f.Center);_block.SetFloat("_LightningEnergy",LightningEnergy);
                _block.SetFloat("_LightningRadius",Mathf.Lerp(CoreRadius,_f.Height*0.6f,Propagation));
            }
            _particles[i].GetComponent<Renderer>().SetPropertyBlock(_block);
        }
    }
    private void SimulateParticles(float deltaTime)
    {
        // Zero-time callback flush makes just-emitted native geometry visible without aging it.
        foreach(var p in _particles)p.Simulate(deltaTime, false, false, false);
    }
    [Serializable] public struct CloudMacroPose
    {
        public Vector3 Center,Radii;
        public Quaternion Rotation;
        public float Scale;
        public Vector3 AxisX {get{return Rotation*Vector3.right;}}
        public Vector3 AxisY {get{return Rotation*Vector3.up;}}
        public Vector3 AxisZ {get{return Rotation*Vector3.forward;}}
    }
    // Public diagnostic: Radii are BOX half extents, not ellipsoid axes.
    public static CloudMacroPose InitialMacroPose(Footprint f,int index,float phase)
    {
        if(index!=0)throw new ArgumentOutOfRangeException("index");
        // Max positive distance perturbation=.175H: radial support -.025..925H;
        // the hole mask removes r<=0. Vertical half support .56*(1+.175/.30)+.10 < 1H.
        return new CloudMacroPose{Center=new Vector3(f.EnvelopeCenter.x,f.GroundY+f.Height*0.75f,f.EnvelopeCenter.z),
            Rotation=Quaternion.identity,Radii=new Vector3(f.CloudInnerRadii.x+0.935f*f.Height,1.0f*f.Height,f.CloudInnerRadii.y+0.935f*f.Height),Scale=1};
    }
    public float CloudUniformScale
    {
        get
        {
            var p=InitialMacroPose(_f,0,0);
            float minimumScale=0.056f*_f.Height/((p.Center-CoreCenter).magnitude+p.Radii.magnitude);
            return Mathf.Lerp(1,minimumScale,CollapseProgress);
        }
    }
    public CloudMacroPose CloudContainerPose {get{return MacroPose(0);}}
    public CloudMacroPose MacroPose(int index)
    {
        CloudMacroPose pose=InitialMacroPose(_f,index,ShapePhase);
        float collapse=CollapseProgress;pose.Scale=CloudUniformScale;
        pose.Center=CoreCenter+RotateY(pose.Center-CoreCenter,0.55f*collapse)*pose.Scale;
        pose.Radii*=pose.Scale;
        pose.Rotation=Quaternion.AngleAxis(0.55f*collapse*Mathf.Rad2Deg,Vector3.up);
        return pose;
    }
    private void ApplyMacroClouds()
    {
        for(int i=0;i<MacroCount;i++)
        {
            CloudMacroPose pose=MacroPose(i);_macroPoses[i]=pose;
            Transform node=_macros[i].transform;node.localPosition=pose.Center-_f.Center;node.localRotation=pose.Rotation;node.localScale=pose.Radii;
            _macroCenters[i]=pose.Center;_macroAxisX[i]=pose.AxisX/pose.Radii.x;_macroAxisY[i]=pose.AxisY/pose.Radii.y;_macroAxisZ[i]=pose.AxisZ/pose.Radii.z;
        }
        float opacity=Ease(Elapsed/0.18f)*(BurstTriggered?1-Ease(_burstAge/0.065f):1);
        float charge=BurstTriggered?1-Ease(_burstAge/0.065f):Charge;
        for(int i=0;i<MacroCount;i++)
        {
            _block.Clear();_block.SetFloat("_MacroCount",MacroCount);_block.SetFloat("_ProxyIndex",i);
            _block.SetVectorArray("_MacroCenters",_macroCenters);_block.SetVectorArray("_MacroAxisX",_macroAxisX);_block.SetVectorArray("_MacroAxisY",_macroAxisY);_block.SetVectorArray("_MacroAxisZ",_macroAxisZ);
            _block.SetVector("_CloudCenter",CoreCenter);_block.SetVector("_CloudInnerRadii",new Vector4(_f.CloudInnerRadii.x,_f.CloudInnerRadii.y,0,0));
            _block.SetFloat("_CloudHeight",_f.Height);_block.SetFloat("_CloudGroundY",_f.GroundY);_block.SetFloat("_CloudSpinAngle",-ShapePhase*0.80f);
            _block.SetFloat("_DensityPhase",Elapsed);_block.SetFloat("_Collapse",CollapseProgress);
            _block.SetFloat("_Charge",charge);_block.SetFloat("_Opacity",opacity);_block.SetFloat("_ExtinctionScale",1/_macroPoses[i].Scale);
            _block.SetFloat("_LocalScale",_macroPoses[i].Scale);
            _block.SetVector("_LightningPosition",_lightningPoint+_f.Center);_block.SetFloat("_LightningEnergy",LightningEnergy);
            _block.SetFloat("_LightningRadius",Mathf.Lerp(CoreRadius,_f.Height*0.6f,Propagation));
            _macros[i].SetPropertyBlock(_block);_macros[i].enabled=Elapsed>0&&(!BurstTriggered||_burstAge<0.065f);
        }
    }
    public static float CloudTheta(int group,float phase)
    {
        float step=Mathf.PI*2/24;
        return (group+0.5f)*step+(Hash(group,91)-0.5f)*step*0.40f-phase*0.80f;
    }
    public static Vector3 CloudSpine(Footprint f,int group,float phase)
    {
        float theta=CloudTheta(group,phase);Vector2 r=f.CloudInnerRadii;
        Vector3 normal=new Vector3(Mathf.Cos(theta)/r.x,0,Mathf.Sin(theta)/r.y).normalized;
        return new Vector3(f.EnvelopeCenter.x+r.x*Mathf.Cos(theta),f.GroundY+f.Height*(0.48f+0.14f*Mathf.Sin(theta)+0.035f*Mathf.Sin(3*theta)),f.EnvelopeCenter.z+r.y*Mathf.Sin(theta))+normal*f.Height*0.45f;
    }
    public static Vector2 CloudInitialSize(Footprint f,int index,float phase)
    {
        int role=index%7,group=index/7;float theta=CloudTheta(group,phase);
        float strength=RingLobes(theta),h=f.Height;
        float width=role<2?0.60f+Hash(index,92)*0.03f:role<4?0.51f+Hash(index,92)*0.06f:role==4?0.36f+Hash(index,92)*0.08f:0.55f+Hash(index,92)*0.15f;
        float height=role<2?0.48f+Hash(index,93)*0.06f:role<4?0.39f+Hash(index,93)*0.07f:role==4?0.30f+Hash(index,93)*0.08f:0.08f+Hash(index,93)*0.06f;
        if(Mathf.Sin(theta)<0)height*=0.85f;
        // Width connects neighbouring clusters; vertical size carries their strong/weak hierarchy.
        return new Vector2(width*(0.92f+0.08f*strength),height*(0.82f+0.18f*strength))*h;
    }
    public static Vector3 CloudInitialPosition(Footprint f,int index,float phase)
    {
        int role=index%7,group=index/7;float theta=CloudTheta(group,phase),h=f.Height;
        Vector2 r=f.CloudInnerRadii,size=CloudInitialSize(f,index,phase);
        Vector3 n=new Vector3(Mathf.Cos(theta)/r.x,0,Mathf.Sin(theta)/r.y).normalized;
        Vector3 t=new Vector3(-r.x*Mathf.Sin(theta),0,r.y*Mathf.Cos(theta)).normalized;
        float along=role==0?-0.060f:role==1?0.040f:role==2?-0.080f:role==3?0.065f:role==4?0:role==5?-0.110f:0.110f;
        float lift=role==0?-0.055f:role==1?0.055f:role==2?0.065f:role==3?-0.015f:role==4?0.10f:0;
        Vector3 offset=h*(n*((Hash(index,94)-0.5f)*0.06f)+t*(along+(Hash(index,95)-0.5f)*0.035f+phase*(role-3)*0.004f)+Vector3.up*(lift+(Hash(index,96)-0.5f)*0.025f));
        // Entire nonuniform card plus offset fits the cloud's outer support ball.
        offset=Vector3.ClampMagnitude(offset,Mathf.Max(0,0.44f*h-size.magnitude*0.5f));
        return CloudSpine(f,group,phase)+offset;
    }
    public static float CloudRoleEnd(int role)
    {return role<2?0:role==2?0.35f:role==3?0.55f:role==4?0.80f:role==5?1:0.65f;}
    public Vector3 FoamParcelPosition(int index)
    {
        Vector3 initial=CloudInitialPosition(_f,index,ShapePhase);
        Vector3 p=CoreCenter+RotateY(initial-CoreCenter,0.55f*CollapseProgress)*CloudUniformScale;
        p=Vector3.Lerp(p,CoreCenter, Ease((CollapseProgress-0.95f)/0.05f)*(1-CloudRoleEnd(index%7)));
        if(BurstTriggered)p=Vector3.Lerp(p,ExpandedRingPoint(_f,CloudTheta(index/7,ShapePhase)-0.55f,0.28f+0.62f*Hash(index,48),_burstAge),Propagation);
        return p;
    }
    public Vector2 CloudParcelSize(int index)
    {
        Vector2 core=Vector2.one*_f.Height*(0.035f+0.012f*Hash(index,97));
        Vector2 size=CloudInitialSize(_f,index,ShapePhase)*CloudUniformScale;
        size=Vector2.Lerp(size,core,Ease((CollapseProgress-0.95f)/0.05f));
        if(BurstTriggered)size=Vector2.Lerp(size,Vector2.one*_f.Height*(0.09f+0.06f*Hash(index,52)),Propagation);
        return size;
    }
    private void UpdateFoamParcels()
    {
        if(BurstTriggered&&_burstAge>0.65f){_particles[6].SetParticles(_foamParcels,0);return;}
        float visible=Ease(Elapsed/0.18f)*(BurstTriggered?1-Ease((_burstAge-0.18f)/0.47f):1);
        for(int i=0;i<_foamParcels.Length;i++)
        {
            int role=i%7;float theta=CloudTheta(i/7,ShapePhase);
            float strength=0.80f+0.20f*RingLobes(theta);
            float alpha=role<2?0.62f+0.10f*Hash(i,98):role<4?0.48f+0.10f*Hash(i,98):role==4?0.44f:0.055f+0.035f*Hash(i,98);
            if(Mathf.Sin(theta)<0)alpha*=0.80f;
            Vector2 size=CloudParcelSize(i);
            _foamParcels[i].position=FoamParcelPosition(i)-_f.Center;
            _foamParcels[i].velocity=Vector3.zero;
            _foamParcels[i].startLifetime=8;_foamParcels[i].remainingLifetime=4;
            _foamParcels[i].startSize3D=new Vector3(size.x,size.y,1);
            _foamParcels[i].startColor=new Color(role<2?0.15f:role==4?1:0.55f,role<5?1:0,1,visible*alpha*strength*Mathf.Lerp(1,0.28f,CollapseProgress)*(BurstTriggered?Mathf.Lerp(0.18f,1,Propagation):role>=5?0.65f:0.12f));
            Vector3 tangent=RingTangent(_f,theta);
            float tangentAngle=Mathf.Atan2(tangent.z*0.402f,tangent.x)*Mathf.Rad2Deg;
            _foamParcels[i].rotation=role>=5?tangentAngle:(role==1?180:role==2?-32:role==3?38:0)+(Hash(i,99)-0.5f)*32+tangentAngle*0.20f;
            if(role<5)_foamParcels[i].rotation+=Mathf.Sin(_phase*(0.7f+Hash(i,100)*0.5f)+i*0.73f)*12*(1-CollapseProgress);
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
            if(energy>0)
            {
                _boltAnchored[i]=true;float angle=i==0?2.3f:5.6f;Vector3 lip=RingPoint(_f,angle,0.60f,_burstAge)-_f.Center;
                Vector3 start=Vector3.Lerp(CoreCenter-_f.Center,lip,Propagation*0.40f);
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
            Vector3 p=RingPoint(_f,theta,q,_burstAge)-_f.Center+Vector3.up*h*0.025f*Propagation;
            Vector3 velocity=WaveVelocity(theta,q)*0.35f+h*Propagation*(n*(1.6f+Hash(seed,73)*1.2f)*side+t*0.12f-Vector3.up*(0.8f+Hash(seed,74)*0.9f));
            var emit=new ParticleSystem.EmitParams{position=p,velocity=velocity,startLifetime=0.28f+0.10f*Hash(seed,75),
                startSize=h*(0.075f+0.07f*Hash(seed,76))*Propagation,startColor=Color.white,randomSeed=(uint)(seed+9301)};
            _particles[7].Emit(emit,1);
            // Small detached droplets share the falling tongue's actual position and momentum.
            emit.startSize=h*(0.020f+0.025f*Hash(seed,77))*Propagation;emit.startLifetime=0.30f+0.12f*Hash(seed,78);
            _particles[3].Emit(emit,1);
        }
    }
    private void EmitSpray(int system,int index,float theta)
    {
        float h=_f.Height;Vector3 n=RingNormal(_f,theta),t=RingTangent(_f,theta);
        float q=0.28f+0.45f*Hash(index,3);
        Vector3 p=RingPoint(_f,theta,q,_burstAge)-_f.Center;
        float side=1-0.60f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta)),6);
        Vector3 velocity=BurstTriggered?WaveVelocity(theta,q)+h*Propagation*((3.5f+2f*Hash(index,5))*side*n+(0.2f+0.4f*Hash(index,6))*t+(0.3f+0.6f*Hash(index,7))*Vector3.up):t*h*0.65f;
        float tilt=Mathf.Atan2(velocity.y*0.916f+velocity.z*0.402f,velocity.x)*Mathf.Rad2Deg-(system==2?55f:90f);
        var emit=new ParticleSystem.EmitParams{position=p,velocity=velocity,startLifetime=system==1?0.35f:system==2?0.32f+Hash(index,8)*0.24f:0.50f+Hash(index,9)*0.32f,
            startSize=h*(BurstTriggered?Mathf.Lerp(0.025f,system==2?0.48f+Hash(index,10)*0.35f:0.035f+Hash(index,11)*0.065f,Propagation):0.035f),startColor=Color.white,rotation=tilt+(Hash(index,12)-0.5f)*20,randomSeed=(uint)(index+101+system*1031)};
        _particles[system].Emit(emit,1);
    }
    private void EmitMist(int system,int count,int batch)
    {
        for(int i=0;i<count;i++)
        {
            float theta=(i+0.3f+batch*0.5f)/count*Mathf.PI*2,h=_f.Height;Vector3 n=RingNormal(_f,theta),t=RingTangent(_f,theta);
            Vector3 world=RingPoint(_f,theta,system==4?0.95f:0.65f,_burstAge);
            if(system==4&&world.y>_f.GroundY+0.14f*h)continue;
            Vector3 p=world-_f.Center;
            if(system==4)p.y=_f.GroundY-_f.Center.y+0.05f*h;
            var emit=new ParticleSystem.EmitParams{position=p,velocity=WaveVelocity(theta,0.65f)*(system==4?0:0.08f)+h*Propagation*(n*(system==4?0.16f:0.40f)+t*0.15f+Vector3.up*(system==4?0.03f:0.34f)),startSize=h*(system==4?0.55f:0.35f)*Propagation,
                startLifetime=system==4?0.50f+Hash(i,29)*0.08f:0.5f+Hash(i,28)*0.12f,startColor=Color.white,randomSeed=(uint)(1201+i+batch*157+system*11)};
            _particles[system].Emit(emit,1);
        }
    }
    private void Clear(){LightningEnergy=0;if(_particles!=null)foreach(var p in _particles)if(p!=null)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);if(_renderers!=null)foreach(var r in _renderers)if(r!=null)r.enabled=false;}
    public void Dispose(){if(_disposed)return;_disposed=true;IsComplete=true;Clear();}
}
