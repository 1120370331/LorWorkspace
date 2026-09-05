using System;
using UnityEngine;

/// <summary>Canonical Round2 water-surface and native-particle driver. No game rules or autonomous clock.</summary>
public sealed class SlazeyaStormVisualController : IDisposable
{
    public const string BundleName = "steria_slazeya_storm_mass";
    public const string PrefabName = "SlazeyaStormMassPrefab";
    public const string ShaderName = "Steria/SlazeyaStormFlow";
    public const string ParticleShaderName = "Steria/SlazeyaStormParticles";
    public const string LightningShaderName = "Steria/SlazeyaStormLightning";
    public const string Version = "2026-09-05.round2.6";
    public const float GatherDuration = 1.35f;
    public const float TailDuration = 1.20f;
    public static readonly string[] SurfaceNames = { "PrimaryBreaker", "SecondaryBreaker", "InflowA", "InflowB", "InflowC" };
    public static readonly string[] ParticleNames = { "GatherMist", "CrestSpindrift", "BurstSheets", "BurstDroplets", "ReturnMist", "CrestLiftMist" };

    [Serializable] public struct Footprint
    {
        public Vector3 Center;
        public float RadiusX, RadiusZ, Height;
    }
    private readonly GameObject _root;
    private readonly Footprint _footprint;
    private readonly Renderer[] _surfaces;
    private readonly ParticleSystem[] _particles;
    private readonly Renderer[] _renderers;
    private readonly Renderer[] _bolts;
    private readonly bool[] _boltAnchored=new bool[2];
    private readonly Vector3[] _crestOrigins=new Vector3[5];
    private Vector3 _lightningPoint;
    private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
    private float _phase, _burstAge, _burstPhase, _emitCredit, _mistCredit, _capturedGather;
    private int _emitted;
    private bool _disposed, _secondaryReleased, _primaryLiftReleased, _secondaryLiftReleased, _primaryReturnReleased, _secondaryReturnReleased;
    public float Elapsed { get; private set; }
    public bool GatherReady { get { return Elapsed >= GatherDuration; } }
    public bool BurstTriggered { get; private set; }
    public bool IsComplete { get; private set; }
    public float BurstAge { get { return BurstTriggered ? _burstAge : -1f; } }
    public float Phase { get { return _phase; } }
    public float RadiusRatio { get; private set; }
    public float LightningEnergy { get; private set; }
    public float ShapePhase { get { return BurstTriggered ? _burstPhase + _burstAge * 0.13f : 0.38f * Ease(Elapsed / GatherDuration); } }
    public int AliveParticles
    {
        get { int count = 0; if (_particles != null) foreach (var p in _particles) if (p != null) count += p.particleCount; return count; }
    }

    public static bool IsFinite(Vector3 value)
    {
        return !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y)
            || float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));
    }

    public static Footprint FitFootprint(Vector3[] feet, float height)
    {
        if (feet == null || feet.Length == 0 || height <= 0f || float.IsNaN(height) || float.IsInfinity(height))
            throw new ArgumentException("A storm footprint needs finite feet and a measured visible body height.");
        Vector3 min = feet[0], max = feet[0];
        float[] levels = new float[feet.Length];
        for (int i = 0; i < feet.Length; i++)
        {
            if (!IsFinite(feet[i])) throw new ArgumentException("Non-finite foot position");
            min = Vector3.Min(min, feet[i]); max = Vector3.Max(max, feet[i]); levels[i] = feet[i].y;
        }
        Array.Sort(levels);
        int middle = levels.Length / 2;
        float groundY = levels.Length % 2 == 1 ? levels[middle] : (levels[middle - 1] + levels[middle]) * 0.5f;
        var result = new Footprint
        {
            Center = new Vector3((min.x + max.x) * 0.5f, groundY + height * 0.012f, (min.z + max.z) * 0.5f),
            RadiusX = Mathf.Max(height * 1.2f, (max.x - min.x) * 0.5f + height * 0.6f),
            RadiusZ = Mathf.Max(height * 0.8f, (max.z - min.z) * 0.5f + height * 0.6f),
            Height = height
        };
        float expansion = 1f;
        foreach (Vector3 foot in feet)
        {
            float nx = (foot.x - result.Center.x) / result.RadiusX;
            float nz = (foot.z - result.Center.z) / result.RadiusZ;
            float normalizedRadius = Mathf.Sqrt(nx * nx + nz * nz);
            expansion = Mathf.Max(expansion, normalizedRadius * 1.025f);
        }
        result.RadiusX *= expansion; result.RadiusZ *= expansion;
        return result;
    }


    // Radial/vertical cross-section: foot, belly, shoulder, rolled lip, hooked tip.
    // Builder serializes all three poses; the shader interpolates them on the SAME surface.
    private static readonly Vector2[] InflowSection = {
        new Vector2(1.12f,0.015f), new Vector2(1.02f,0.04f), new Vector2(0.91f,0.07f),
        new Vector2(0.80f,0.08f), new Vector2(0.72f,0.035f) };
    private static readonly Vector2[] CurledSection = {
        new Vector2(0.96f,0.025f), new Vector2(0.96f,0.42f), new Vector2(0.80f,0.91f),
        new Vector2(0.56f,1.06f), new Vector2(0.73f,0.68f) };
    private static readonly Vector2[] OverturnedSection = {
        new Vector2(0.84f,0.025f), new Vector2(0.93f,0.42f), new Vector2(1.00f,1.03f),
        new Vector2(1.14f,1.37f), new Vector2(1.19f,1.19f) };

    public static Vector2 SectionPoint(int pose, float q)
    {
        Vector2[] control = pose == 0 ? InflowSection : pose == 1 ? CurledSection : OverturnedSection;
        float index = Mathf.Clamp01(q) * 4f;
        int k = Mathf.Min(3, (int)index);
        float t = index - k;
        Vector2 a = control[Mathf.Max(0,k-1)], b = control[k], c = control[k+1], d = control[Mathf.Min(4,k+2)];
        return 0.5f * ((2f*b) + (-a+c)*t + (2f*a-5f*b+4f*c-d)*t*t + (-a+3f*b-3f*c+d)*t*t*t);
    }

    public static Vector3 SurfacePose(int kind, float s, float q, int pose)
    {
        if (kind >= 2)
        {
            float angle = (-0.92f + (kind-2)*2.05f) + (s-0.5f) * (kind==2 ? 0.80f : 0.52f);
            float taper = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(s)*Mathf.PI),0.6f);
            float inflowRadius = pose == 0 ? Mathf.Lerp(1.15f,0.72f,q) : pose==1 ? Mathf.Lerp(1.04f,0.58f,q) : Mathf.Lerp(0.81f,1.17f,q);
            angle += q * (pose==2 ? 0.22f : -0.30f);
            return new Vector3(Mathf.Cos(angle)*inflowRadius,0.014f + Mathf.Sin(q*Mathf.PI)*taper*(pose==2 ? 0.18f:0.075f),Mathf.Sin(angle)*inflowRadius);
        }
        bool primary = kind==0;
        float theta = (primary ? -0.17f : 3.65f) + s*(primary ? 3.68f : 1.94f);
        float taperEnd = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(s)*Mathf.PI),0.50f);
        float pressure = primary ? 0.85f + 0.65f*Mathf.Exp(-Mathf.Pow((s-0.54f)/0.32f,2))
            : 0.26f + 0.58f*Mathf.Exp(-Mathf.Pow((s-0.30f)/0.32f,2));
        Vector2 section = SectionPoint(pose,q);
        // Macro water fingers and V tears grow out of the old crest, not another hidden flower.
        float lobe = primary ? 0.24f+0.76f*Mathf.Max(Bell(s,0.25f,0.105f)*0.78f,Mathf.Max(Bell(s,0.53f,0.12f),Bell(s,0.81f,0.09f)*0.65f))
            : 0.30f+0.70f*Mathf.Max(Bell(s,0.28f,0.16f)*0.85f,Bell(s,0.73f,0.12f)*0.64f);
        float fingers = pose==2 ? Mathf.Lerp(1f,lobe,Mathf.SmoothStep(0f,1f,(q-0.48f)/0.40f)) : 1f;
        float radius = section.x + 0.035f*Mathf.Sin(s*7f+q*2f) * taperEnd;
        float height = section.y * pressure * taperEnd * fingers;
        theta += q*q*(pose==0 ? -0.08f : pose==1 ? -0.27f : 0.18f) + 0.04f*Mathf.Sin(s*8f)*q;
        return new Vector3(Mathf.Cos(theta)*radius,height,Mathf.Sin(theta)*radius);
    }
    private static float Bell(float s,float center,float width) {return Mathf.Exp(-Mathf.Pow((s-center)/width,2));}

    // Matches shader vertex interpolation. Used for native crest emission and morph diagnostics.
    public static Vector3 DeformSurface(int kind, float s, float q, float gather, float overturn, float fall, float shapePhase)
    {
        float g = Ease((gather - 0.10f*q - 0.13f*(1f-s)) / 0.72f);
        float o = Ease(overturn * (0.80f + 0.20f*q));
        Vector3 p = Vector3.Lerp(SurfacePose(kind,s,q,0),SurfacePose(kind,s,q,1),g);
        p = Vector3.Lerp(p,SurfacePose(kind,s,q,2),o);
        p.y = Mathf.Max(0.018f,p.y - fall*fall*(1.65f + 0.5f*q));
        float angle = shapePhase*(0.45f+0.55f*q);
        return new Vector3(p.x*Mathf.Cos(angle)-p.z*Mathf.Sin(angle),p.y,p.x*Mathf.Sin(angle)+p.z*Mathf.Cos(angle));
    }

    public SlazeyaStormVisualController(GameObject root, Footprint footprint)
    {
        _root = root; _footprint = footprint;
        if (root == null) return;
        root.transform.SetParent(null,true);
        root.transform.position = footprint.Center;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        Transform ground = Required("AB_GroundRoot"), storm = Required("AB_StormRoot");
        Required("AB_BurstRoot"); Required("AB_ParticleRoot");
        _bolts=new[]{Required("AB_BurstRoot/LightningRoot/PrimaryBolt").GetComponent<Renderer>(),
            Required("AB_BurstRoot/LightningRoot/SecondaryBolt").GetComponent<Renderer>()};
        Vector3 scale = new Vector3(footprint.RadiusX,footprint.Height,footprint.RadiusZ);
        ground.localScale = scale; storm.localScale = scale;
        _surfaces = new Renderer[SurfaceNames.Length];
        for (int i=0;i<_surfaces.Length;i++) _surfaces[i]=Required((i<2?"AB_StormRoot/":"AB_GroundRoot/")+SurfaceNames[i]).GetComponent<Renderer>();
        _particles = new ParticleSystem[ParticleNames.Length];
        for (int i=0;i<_particles.Length;i++)
        {
            var p=Required("AB_ParticleRoot/"+ParticleNames[i]).GetComponent<ParticleSystem>(); _particles[i]=p;
            p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main; main.loop=false; main.playOnAwake=false;
            var emission=p.emission; emission.enabled=false;
            p.Pause(false);
        }
        _renderers=root.GetComponentsInChildren<Renderer>(true);
        ApplyVisuals();
    }

    private Transform Required(string path)
    {
        Transform result=_root.transform.Find(path);
        if(result==null) throw new InvalidOperationException("Round2 storm prefab missing "+path);
        return result;
    }

    public bool TriggerBurst()
    {
        if(_disposed || IsComplete || BurstTriggered) return false;
        _burstPhase=ShapePhase; _capturedGather=Elapsed;
        BurstTriggered=true; _burstAge=0f;
        if(_root!=null) { ReleaseCrest(0); ApplyVisuals(); }
        return true;
    }

    public void Advance(float deltaTime)
    {
        if(_disposed || IsComplete || deltaTime<=0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        Elapsed+=deltaTime;
        _phase+=deltaTime*(BurstTriggered?0.85f:GatherReady?0.23f:0.35f+0.40f*Mathf.Clamp01(Elapsed/GatherDuration));
        if(BurstTriggered) _burstAge+=deltaTime;
        if(_root!=null)
        {
            if(!BurstTriggered)
            {
                _emitCredit+=deltaTime*(GatherReady?16f:32f);
                int count=Mathf.Min(20,(int)_emitCredit); _emitCredit-=(int)_emitCredit;
                for(int i=0;i<count;i++) { int id=_emitted++; EmitAtCrest(1,id,id%4==0?1:0); }
                _mistCredit+=deltaTime*11f;
                count=Mathf.Min(8,(int)_mistCredit); _mistCredit-=(int)_mistCredit;
                for(int i=0;i<count;i++) EmitAtCrest(0,_emitted++,0);
            }
            else
            {
                if(!_secondaryReleased && _burstAge>=0.05f) { _secondaryReleased=true; ReleaseCrest(1); }
                if(!_primaryLiftReleased&&_burstAge>=0.04f){_primaryLiftReleased=true;if(_burstAge<0.18f)EmitMistCluster(0,5,10);}
                if(!_secondaryLiftReleased&&_burstAge>=0.11f){_secondaryLiftReleased=true;if(_burstAge<0.24f)EmitMistCluster(1,5,5);}
                if(!_primaryReturnReleased&&_burstAge>=0.36f){_primaryReturnReleased=true;if(_burstAge<0.60f)EmitMistCluster(0,4,8);}
                if(!_secondaryReturnReleased&&_burstAge>=0.50f){_secondaryReturnReleased=true;if(_burstAge<0.70f)EmitMistCluster(1,4,7);}
            }
            foreach(ParticleSystem p in _particles) p.Simulate(deltaTime, false, false, false);
            ApplyVisuals();
        }
        if(BurstTriggered && _burstAge>=TailDuration) { IsComplete=true; Clear(); }
    }

    private float GatherFor(int kind)
    {
        return Mathf.Clamp01(((BurstTriggered?_capturedGather:Elapsed)-(kind==1?0.13f:0f))/1.10f);
    }
    private float OverturnFor(int kind)
    {
        if(!BurstTriggered)return 0f;
        float release=Mathf.Clamp01((_burstAge-(kind==1?0.05f:0f))/0.20f);
        return 1f-(1f-release)*(1f-release); // Front-loaded impulse, maximum crown still at B+0.20.
    }
    private float Fall { get { return BurstTriggered?Mathf.Clamp01((_burstAge-0.20f)/0.50f):0f; } }
    private static float Ease(float value) { value=Mathf.Clamp01(value); return value*value*(3f-2f*value); }

    private void ApplyVisuals()
    {
        ApplyLightning();
        float dissolve=BurstTriggered?Mathf.Clamp01((_burstAge-0.22f)/0.38f):0f;
        float flash=BurstTriggered?Mathf.Pow(1f-Mathf.Clamp01(_burstAge/0.10f),1.1f):0f;
        RadiusRatio=BurstTriggered?Mathf.Lerp(0.96f,1.19f,OverturnFor(0)):Mathf.Lerp(1.12f,0.96f,Ease(Elapsed/GatherDuration));
        for(int i=0;i<_surfaces.Length;i++)
        {
            float fade=1f-Ease(BurstTriggered?(_burstAge-0.58f)/0.26f:0f);
            float alpha=(i<2?0.89f:0.43f)*Mathf.Clamp01(Elapsed/(i==1?0.38f:0.20f))*fade;
            _block.Clear();
            _block.SetFloat("_Phase",_phase); _block.SetFloat("_ShapePhase",ShapePhase);
            _block.SetFloat("_Gather",GatherFor(i)); _block.SetFloat("_Overturn",OverturnFor(i));
            _block.SetFloat("_Fall",Fall); _block.SetFloat("_Alpha",alpha);
            _block.SetFloat("_Dissolve",dissolve); _block.SetFloat("_Flash",i<2?flash:flash*0.25f);
            _block.SetVector("_LightningPosition",_root.transform.TransformPoint(_lightningPoint));
            _block.SetFloat("_LightningRadius",_footprint.Height*0.55f);_block.SetFloat("_LightningEnergy",LightningEnergy);
            _surfaces[i].SetPropertyBlock(_block); _surfaces[i].enabled=alpha>0.001f;
        }
        for(int i=0;i<_particles.Length;i++)
        {
            _block.Clear(); _block.SetFloat("_Phase",_phase);
            bool mist=i==0||i==4||i==5;
            _block.SetFloat("_Alpha",(i==0?0.27f:i==4?0.20f:i==5?0.30f:0.83f)*(mist?1f:1f-Ease(BurstTriggered?(_burstAge-1.02f)/0.16f:0)));
            _particles[i].GetComponent<Renderer>().SetPropertyBlock(_block);
        }
    }

    private static float Pulse(float age,float start,float strength)
    {
        float t=age-start;
        if(t<0||t>=0.05f)return 0;
        if(t<0.016f)return strength*Mathf.Lerp(0.32f,1,t/0.016f);
        return strength*(1-Mathf.Clamp01((t-0.033f)/0.017f));
    }
    private void ApplyLightning()
    {
        LightningEnergy=0;
        for(int i=0;i<2;i++)
        {
            float energy=BurstTriggered?Pulse(_burstAge,i==0?0:0.10f,i==0?1f:0.60f):0;
            if(energy>0&&!_boltAnchored[i])
            {
                _boltAnchored[i]=true;
                Vector3 lip=CrestPoint(i,i==0?0.54f:0.54f,0.80f);
                Vector3 radial=new Vector3(lip.x,0,lip.z).normalized;
                Vector3 start=lip-radial*_footprint.Height*(i==0?0.65f:0.42f)+Vector3.up*_footprint.Height*0.14f;
                Vector3 direction=(lip-start).normalized;
                Vector3 facing=new Vector3(0,0.402f,-0.916f);
                Vector3 normal=Vector3.Cross(direction,Vector3.Cross(facing,direction)).normalized;
                _bolts[i].transform.localPosition=start;
                _bolts[i].transform.localRotation=Quaternion.LookRotation(normal,Vector3.Cross(normal,direction));
                _bolts[i].transform.localScale=Vector3.one*Vector3.Distance(start,lip);
                _lightningPoint=Vector3.Lerp(start,lip,0.75f);
            }
            _block.Clear();_block.SetFloat("_Pulse",energy);_bolts[i].SetPropertyBlock(_block);_bolts[i].enabled=energy>0;
            LightningEnergy+=energy;
        }
    }

    private Vector3 CrestPoint(int kind,float s,float q)
    {
        Vector3 p=DeformSurface(kind,s,q,GatherFor(kind),OverturnFor(kind),Fall,ShapePhase);
        return new Vector3(p.x*_footprint.RadiusX,p.y*_footprint.Height,p.z*_footprint.RadiusZ);
    }
    private static float Hash(int index,int salt)
    {
        uint v=unchecked((uint)(index*1973+salt*9277+89173)); v^=v<<13;v^=v>>17;v^=v<<5;
        return (v&65535u)/65535f;
    }
    private void ReleaseCrest(int kind)
    {
        int offset=kind==0?0:3;
        for(int i=0;i<(kind==0?3:2);i++)_crestOrigins[offset+i]=CrestPoint(kind,kind==0?(i==0?0.25f:i==1?0.53f:0.81f):(i==0?0.28f:0.73f),0.76f);
        int sheets=kind==0?6:3, drops=kind==0?64:34;
        for(int i=0;i<sheets;i++) EmitAtCrest(2,i,kind);
        for(int i=0;i<drops;i++) EmitAtCrest(3,i,kind);
    }
    private void EmitAtCrest(int system,int index,int kind)
    {
        float h=_footprint.Height;
        float s=Mathf.Lerp(0.16f,0.88f,Hash(index,kind+11));
        if(system==2||system==3)
        {
            float origin=kind==0?(index%3==0?0.25f:index%3==1?0.53f:0.81f):(index%2==0?0.28f:0.73f);
            s=Mathf.Clamp01(origin+(Hash(index,9)-0.5f)*(system==2?0.10f:0.18f));
        }
        bool mist=system==0||system==4;
        Vector3 p=CrestPoint(kind,s,mist?0.05f:0.72f+Hash(index,3)*0.05f);
        Vector3 tangent=(CrestPoint(kind,Mathf.Min(0.99f,s+0.01f),0.9f)-CrestPoint(kind,Mathf.Max(0.01f,s-0.01f),0.9f)).normalized;
        Vector3 radial=new Vector3(p.x,0,p.z).normalized;
        Vector3 velocity=tangent*h*(mist?0.10f:0.46f);
        if(system==2||system==3) velocity+=radial*h*(system==2?4.0f:4.4f)*(kind==0?1f:0.75f)+Vector3.up*h*(0.55f+0.50f*Hash(index,6));
        else if(system==1) velocity+=radial*h*0.13f+Vector3.up*h*0.12f;
        if(mist) p.y=h*(system==4?0.04f:0.09f);
        // Atlas +V is the water-finger direction. Tip it into the emitted momentum,
        // with different lengths/lifetimes, instead of upright repeated crown stamps.
        float projectedUp=velocity.y*0.916f+velocity.z*0.402f;
        float tilt=Mathf.Atan2(projectedUp,velocity.x)*Mathf.Rad2Deg-90f;
        var emit=new ParticleSystem.EmitParams {
            position=p,velocity=velocity,startLifetime=system==0?0.8f:system==1?0.43f:system==2?0.38f+Hash(index,7)*0.42f:system==3?0.53f+Hash(index,7)*0.40f:0.70f,
            startSize=h*(system==0?0.46f:system==4?0.50f:system==2?0.38f+Hash(index,8)*0.48f:system==3?0.032f+Hash(index,8)*0.075f:0.030f+Hash(index,8)*0.02f),
            startColor=Color.white,rotation=mist?0f:tilt+Mathf.Lerp(-19f,19f,Hash(index,4)),randomSeed=(uint)(1+index+kind*157+system*1031)
        };
        _particles[system].Emit(emit,1);
    }

    private void EmitMistCluster(int kind,int system,int count)
    {
        float h=_footprint.Height;
        for(int i=0;i<count;i++)
        {
            int cluster=i%(kind==0?3:2),origin=(kind==0?0:3)+cluster;
            float s=kind==0?(cluster==0?0.25f:cluster==1?0.53f:0.81f):(cluster==0?0.28f:0.73f);
            Vector3 p=system==5?CrestPoint(kind,s+(Hash(i,17)-0.5f)*0.07f,0.79f):_crestOrigins[origin];
            Vector3 radial=new Vector3(p.x,0,p.z).normalized;
            Vector3 tangent=new Vector3(-radial.z,0,radial.x);
            if(system==5)p+=radial*h*0.045f;
            else
            {
                // Short visual flight estimate from the captured lip; no physics, raycast or gameplay.
                p+=radial*h*0.65f+tangent*h*0.12f;
                float nr=Mathf.Sqrt(p.x*p.x/(_footprint.RadiusX*_footprint.RadiusX)+p.z*p.z/(_footprint.RadiusZ*_footprint.RadiusZ));
                if(nr>1.07f){p.x*=1.07f/nr;p.z*=1.07f/nr;}
                p.y=h*(0.03f+Hash(i,23)*0.025f);
            }
            p+=tangent*(Hash(i,31)-0.5f)*h*0.22f;
            Vector3 velocity=system==5?tangent*h*0.20f+radial*h*(0.25f+Hash(i,32)*0.20f)+Vector3.up*h*(0.30f+Hash(i,33)*0.20f)
                :radial*h*0.15f+tangent*h*0.075f+Vector3.up*h*0.025f;
            var emit=new ParticleSystem.EmitParams{position=p,velocity=velocity,
                startSize=h*(system==5?0.22f+Hash(i,34)*0.13f:0.35f+Hash(i,35)*0.20f),
                startLifetime=system==5?0.45f+Hash(i,36)*0.18f:0.45f+Hash(i,37)*0.14f,
                startColor=Color.white,randomSeed=(uint)(611+i+kind*173+system*31)};
            _particles[system].Emit(emit,1);
        }
    }

    private void Clear()
    {
        LightningEnergy=0;
        if(_particles!=null) foreach(var p in _particles) if(p!=null) p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(_renderers!=null) foreach(var r in _renderers) if(r!=null) r.enabled=false;
    }
    public void Dispose() { if(_disposed)return;_disposed=true;IsComplete=true;Clear(); }
}
