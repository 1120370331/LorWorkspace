using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored Shuriken prefab, one scaled clock, plus the accepted palm cue.</summary>
public sealed class VeliaPotentialObservationVisual : IDisposable
{
    public const string BundleName="steria_velia_potential_observation";
    public const string MaterialName="VeliaObservationMaterial";
    public const string PrefabName="VeliaObservationParticles";
    public const float Duration=1.45f, PoseDuration=0.90f;
    private const float Step=1f/120f;
    private readonly GameObject _root;
    private readonly Transform _bodyRoot;
    private readonly ParticleSystem[] _systems;
    private readonly ParticleSystemRenderer[] _lightRenderers;
    private readonly MaterialPropertyBlock _lightProperties=new MaterialPropertyBlock();
    private readonly Mesh _hand;
    private readonly List<Vector3> _vertices=new List<Vector3>(64);
    private readonly List<Color> _colors=new List<Color>(64);
    private readonly List<int> _triangles=new List<int>(192);
    private Vector3 _palm;
    private float _baseY;
    private float _height=1, _direction=1, _accumulated;
    private bool _disposed, _handVisible=true;
    public float Age { get; private set; }
    public bool Complete { get { return Age>=Duration || _disposed; } }
    public int HandVertexCount { get { return _disposed?0:_hand.vertexCount; } }
    public int LiveParticleCount
    { get { int count=0;if(!_disposed)foreach(var p in _systems)if(p!=null)count+=p.particleCount;return count; } }

    public VeliaPotentialObservationVisual(GameObject particlePrefab,Material handMaterial,int layer,int sortingLayer,int order)
    {
        if(particlePrefab==null)throw new ArgumentNullException("particlePrefab");
        if(handMaterial==null)throw new ArgumentNullException("handMaterial");
        _root=new GameObject("VeliaPotentialObservation");_root.SetActive(false);
        try
        {
            // Inactive staging prevents first-frame auto-emission; template stays read-only.
            var instance=UnityEngine.Object.Instantiate(particlePrefab,_root.transform,false);
            _bodyRoot=instance.transform.Find("BodyRoot");
            if(_bodyRoot==null)throw new ArgumentException("Particle prefab has no BodyRoot");
            _systems=_bodyRoot.GetComponentsInChildren<ParticleSystem>(true);
            string[] roles={"CoreFlow","SoftLift","Glints"};
            _lightRenderers=new ParticleSystemRenderer[roles.Length];
            if(_systems.Length!=roles.Length)throw new ArgumentException("Particle prefab must contain three light-mask roles");
            for(int i=0;i<roles.Length;i++)
            {
                var child=_bodyRoot.Find(roles[i]);
                var system=child==null?null:child.GetComponent<ParticleSystem>();
                if(system==null)throw new ArgumentException("Missing ParticleSystem "+roles[i]);
                system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=system.main;main.playOnAwake=false;main.loop=false;
                main.simulationSpace=ParticleSystemSimulationSpace.Local;
                main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpeed=1;
                // Preserve the authored deterministic seed, not Unity's shared RNG.
                system.useAutoRandomSeed=false;
                child.gameObject.layer=layer;
                var renderer=system.GetComponent<ParticleSystemRenderer>();
                // Prefab owns the relative foreground/background recipe. Apply
                // target sorting once without duplicating its offsets in code.
                renderer.sortingLayerID=sortingLayer;renderer.sortingOrder=order+renderer.sortingOrder;
                _lightRenderers[i]=renderer;
                system.Pause(false);
            }
            var handObject=new GameObject("RightPalmLight");handObject.layer=layer;handObject.transform.SetParent(_root.transform,false);
            _hand=new Mesh{name="RightPalmLight"};_hand.MarkDynamic();
            handObject.AddComponent<MeshFilter>().sharedMesh=_hand;
            var handRenderer=handObject.AddComponent<MeshRenderer>();handRenderer.sharedMaterial=handMaterial;
            handRenderer.sortingLayerID=sortingLayer;handRenderer.sortingOrder=order+3;
            handRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;handRenderer.receiveShadows=false;
            instance.SetActive(true); // Authored prefab is inactive; staging parent still guards activation.
            _root.SetActive(true);
            foreach(var system in _systems){system.Simulate(0,false,true,false);system.Pause(false);}
        }
        catch{UnityEngine.Object.Destroy(_root);throw;}
    }

    public void SetAnchors(Vector3 palm,Vector3 feet,float standingHeight,bool mirrored)
    {
        if(_disposed)return;
        _palm=palm;_height=Mathf.Max(.1f,standingHeight);_direction=mirrored?-1:1;
        _baseY=feet.y;
        _bodyRoot.position=feet;_bodyRoot.localScale=new Vector3(_height*_direction,_height,_height);
        UpdateLightProperties();
    }

    private void UpdateLightProperties()
    {
        // One cast clock drives mask propagation. Each cloned renderer receives
        // its own properties; concurrent targets never modify shared materials.
        foreach(var renderer in _lightRenderers)
        {
            renderer.GetPropertyBlock(_lightProperties);
            _lightProperties.SetFloat("_Age",Age);
            _lightProperties.SetFloat("_BaseY",_baseY);
            _lightProperties.SetFloat("_Height",_height);
            renderer.SetPropertyBlock(_lightProperties);
        }
    }

    public static Vector3 SpritePoint(SpriteRenderer renderer,Vector2 topLeftPixel)
    {
        Sprite sprite=renderer.sprite;
        Vector2 point=new Vector2(topLeftPixel.x-sprite.pivot.x,sprite.rect.height-topLeftPixel.y-sprite.pivot.y)/sprite.pixelsPerUnit;
        if(renderer.flipX)point.x=-point.x;if(renderer.flipY)point.y=-point.y;
        return renderer.transform.TransformPoint(point);
    }

    public void Advance(float deltaTime)
    {
        if(_disposed || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))return;
        _accumulated+=Mathf.Clamp(deltaTime,0,Duration);
        while(_accumulated+0.000001f>=Step && Age<Duration)
        {
            _accumulated-=Step;
            foreach(var system in _systems)
            {
                // Stock Emission/Shape/Velocity/Size/Color modules in the bundled
                // prefab own births, motion and fade. No duplicate automatic clock.
                system.Simulate(Step,false,false,false);system.Pause(false);
                // Synchronize newborn renderer data (Unity2019) without advancing age.
                system.Simulate(0,false,false,false);system.Pause(false);
            }
            Age=Mathf.Min(Duration,Age+Step);
            if(Age>=Duration-.00001f)
            {
                Age=Duration;_accumulated=0;
                foreach(var system in _systems)system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        UpdateLightProperties();
        DrawHand();
    }

    public void HideHand(){_handVisible=false;if(!_disposed)_hand.Clear();}
    private static float Smooth(float x){return Mathf.SmoothStep(0,1,Mathf.Clamp01(x));}
    private static Color White(float a){return new Color(1,.98f,.91f,a);}
    private static Color Gold(float a){return new Color(1,.72f,.24f,a);}
    private void Vertex(float x,float y,Color color)
    {_vertices.Add(_palm+new Vector3(x*_height*_direction,y*_height,-.025f));_colors.Add(color);}
    private void Ray(float angle,float length,float width,float alpha)
    {
        int start=_vertices.Count;float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
        Vertex(-s*width,c*width,Gold(0));Vertex(0,0,White(alpha));
        Vertex(s*width,-c*width,Gold(0));Vertex(c*length,s*length,Gold(0));
        _triangles.AddRange(new[]{start,start+1,start+3,start+1,start+2,start+3});
    }
    private void DrawHand()
    {
        _vertices.Clear();_colors.Clear();_triangles.Clear();
        if(_handVisible && Age<.72f)
        {
            float a=Smooth(Age/.10f)*(1-Smooth((Age-.28f)/.44f));
            float radius=.045f+Smooth(Age/.18f)*.03f;
            const int count=32;int start=_vertices.Count;Vertex(0,0,White(a));
            for(int i=0;i<=count;i++)
            {
                float angle=i/(float)count*Mathf.PI*2;
                Vertex(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,Gold(0));
                if(i<count)_triangles.AddRange(new[]{start,start+i+1,start+i+2});
            }
            float flash=Smooth((Age-.10f)/.06f)*(1-Smooth((Age-.28f)/.28f));
            Ray(Mathf.PI*.5f,.27f,.026f,flash);Ray(-Mathf.PI*.5f,.13f,.014f,flash*.7f);
            Ray(0,.10f,.012f,flash*.7f);Ray(Mathf.PI,.10f,.012f,flash*.7f);
        }
        _hand.Clear();_hand.SetVertices(_vertices);_hand.SetColors(_colors);_hand.SetTriangles(_triangles,0);_hand.RecalculateBounds();
    }
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var system in _systems)if(system!=null)system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        UnityEngine.Object.Destroy(_hand);
        if(_root!=null){_root.SetActive(false);UnityEngine.Object.Destroy(_root);}
    }
}
