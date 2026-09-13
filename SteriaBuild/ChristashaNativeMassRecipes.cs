using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Steria.Vfx
{
    /// <summary>Visual-only Xiyin recipes. Installed resources are read-only inputs, never bundled.</summary>
    public sealed class ChristashaNativeMassRecipes : IDisposable
    {
        public const string AreaResource = "prefabs/battle/diceattackeffects/new/fx/mon/xiao/fx_mon_xiao_areatrail1";
        public const string CastResource = "prefabs/battle/creatureeffect/new_illusioncardfx/4_n/fx_illusioncard_4_n_orchestra_start";
        public const string PointResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_glovetrail_cross";
        public const float AreaDesignWidth = 32f;
        const string Marker = "__ChristashaNativeMass_";
        readonly Func<string, GameObject> resolver;
        readonly Texture2D brush;
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        readonly Dictionary<Texture, Texture2D> tideTextures = new Dictionary<Texture, Texture2D>();
        GameObject cache;
        Material brushMaterial, lineMaterial;

        public ChristashaNativeMassRecipes(Func<string, GameObject> resolver, Texture2D originalBrush)
        {
            if (resolver == null) throw new ArgumentNullException("resolver");
            this.resolver = resolver; brush = originalBrush;
        }

        public GameObject CreatePrototype(string recipe, Transform parent = null)
        {
            if (recipe != "Cast" && recipe != "Area" && recipe != "Point") throw new ArgumentException("Unknown mass recipe: " + recipe);
            GameObject template;
            if (!templates.TryGetValue(recipe, out template))
            {
                if (cache == null)
                {
                    cache = new GameObject("ChristashaNativeMass_InactiveCache"); cache.SetActive(false);
                    if (Application.isPlaying) Object.DontDestroyOnLoad(cache);
                }
                template = new GameObject("ChristashaNativeMass_" + recipe); template.SetActive(false);
                template.transform.SetParent(cache.transform, false);
                new GameObject(Marker + recipe).transform.SetParent(template.transform, false);
                try
                {
                    if (recipe == "Area") BuildArea(template);
                    else if (recipe == "Cast") BuildCast(template);
                    else BuildPoint(template);
                    templates.Add(recipe, template);
                }
                catch { Object.DestroyImmediate(template); throw; }
            }
            GameObject instance = Object.Instantiate(template, parent, false); instance.SetActive(false); return instance;
        }

        GameObject Source(string path, Transform parent)
        {
            var source = resolver(path);
            if (source == null) throw new InvalidOperationException("Installed native mass resource unavailable: " + path);
            return ChristashaNativeParticleLibrary.CloneVisualOnly(source, parent);
        }

        void BuildArea(GameObject root)
        {
            var art = new GameObject("MassWaveArt"); art.transform.SetParent(root.transform, false);
            // Native torus, tilt and independent angular curves stay intact inside this art space.
            art.transform.localScale = new Vector3(6.4f, 7.6f, 6.4f);
            art.transform.localPosition = new Vector3(9.5f, 2.4f, 0);
            var native = Source(AreaResource, art.transform);
            Retain(native, new HashSet<string> { "Trail", "Trail2", "Trail_Wave", "Spark1", "Spark2" });
            CloneMaterials(native, true, false);
            foreach (var p in native.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = p.main; main.loop = false; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (p.name == "Trail2") { main.startDelay = .055f; main.startSizeMultiplier *= .78f; }
                if (p.name == "Trail3") { main.startDelay = .095f; main.startSizeMultiplier *= .63f; }
                if (p.name == "Trail4") { main.startDelay = .035f; main.startSizeMultiplier *= .45f; }
                if (p.name == "Trail_Wave") { main.startDelay = .075f; main.startSizeMultiplier *= .72f; }
                if (p.name.StartsWith("Spark", StringComparison.Ordinal))
                {
                    main.startLifetime = new ParticleSystem.MinMaxCurve(.28f, .56f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,.90f,.63f), new Color(1,1,.92f));
                    var color = p.colorOverLifetime; color.enabled = false;
                }
            }
            AddSupport(root.transform, "MassFoam", .21f, 96);
            AddSupport(root.transform, "MassSpray", .07f, 96);
            AddStrings(root.transform, 3);
        }

        void BuildCast(GameObject root)
        {
            var art = new GameObject("MassCastArt"); art.transform.SetParent(root.transform, false);
            art.transform.localScale = new Vector3(.14f, .14f, .14f);
            art.transform.localPosition = new Vector3(0, -.98f, 0);
            var native = Source(CastResource, art.transform);
            Retain(native, new HashSet<string> { "Line1", "Line2" });
            CloneMaterials(native, false, true);
            foreach (var p in native.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = p.main; main.loop = false; main.startLifetime = .68f;
                main.startDelay = p.name == "Line1" ? 0f : .055f;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (p.name == "Line2") main.startSizeMultiplier *= .66f;
            }
            AddSupport(root.transform, "MassFoam", .075f, 32);
            AddStrings(root.transform, 2);
        }

        void BuildPoint(GameObject root)
        {
            var native = Source(PointResource, root.transform);
            Retain(native, new HashSet<string> { "Trail1_cross" });
            CloneMaterials(native, false, false);
            var cross = FindSystem(root, "Trail1_cross");
            var main = cross.main; main.loop = false; main.duration = .4f;
            main.startDelay = 0f; main.startLifetime = .065f; main.startSize = .68f;
            main.startSpeed = 0f; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var shape = cross.shape; shape.enabled = false;
            var emission = cross.emission; emission.enabled = true; emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            AddSupport(root.transform, "MassFoam", .19f, 24);
            AddSupport(root.transform, "MassSpray", .055f, 32);
        }

        static void Retain(GameObject root, HashSet<string> names)
        {
            // Keep Transform ancestors because original clips bind by their paths.
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!names.Contains(renderer.name)) Object.DestroyImmediate(renderer);
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
                if (!names.Contains(p.name)) Object.DestroyImmediate(p);
        }

        void CloneMaterials(GameObject root, bool tide, bool openScore)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    var material = new Material(materials[i]) { name = "ChristashaMass_" + renderer.name };
                    owned.Add(material); materials[i] = material;
                    if (tide && renderer.name == "Trail") material.SetTexture("_Mask", FoamCoverage(material.GetTexture("_Mask"), material.GetTexture("_Main")));
                    foreach (var property in new[] { "_Main", "_MainTex" })
                    {
                        if (!material.HasProperty(property)) continue;
                        var source = material.GetTexture(property); if (source == null) continue;
                        // Xiao's DXBC reads only _Main.G as an intensity field. Preserve
                        // that scalar exactly; its orange identity comes from animated
                        // _Color/_DissolveColor, not the unused texture R/B channels.
                        if (tide && !(renderer.name == "Trail" && property == "_Main")) material.SetTexture(property, TideTexture(source, false));
                        if (openScore) material.SetTexture(property, TideTexture(source, true));
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        Texture2D TideTexture(Texture source, bool score)
        {
            Texture2D cached;
            if (!score && tideTextures.TryGetValue(source, out cached)) return cached;
            Texture2D result = ReadTexture(source);
            var pixels = result.GetPixels();
            for (int y = 0; y < result.height; y++) for (int x = 0; x < result.width; x++)
            {
                int i = x + y * result.width; Color c = pixels[i];
                float energy = .2126f*c.r + .7152f*c.g + .0722f*c.b;
                Color tint = Color.Lerp(new Color(.83f,.56f,.19f), new Color(1f,.98f,.87f), Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.22f,.83f,energy)));
                float cut = 1f;
                if (score)
                {
                    float angle = Mathf.Atan2(y / (float)result.height - .5f, x / (float)result.width - .5f);
                    // An unequal open score segment; never the original full-screen circle.
                    cut = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-2.2f,-1.85f,angle)) * (1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,1.65f,angle)));
                }
                pixels[i] = new Color(energy*tint.r*cut, energy*tint.g*cut, energy*tint.b*cut, c.a*cut);
            }
            result.SetPixels(pixels); result.Apply(false,true);
            result.name = "Christasha_Runtime_" + (score ? "OpenScore_" : "Tide_") + source.name;
            result.wrapMode = source.wrapMode; result.filterMode = source.filterMode;
            owned.Add(result); if (!score) tideTextures.Add(source, result); return result;
        }

        Texture2D FoamCoverage(Texture maskSource, Texture intensitySource)
        {
            var mask=ReadTexture(maskSource);var intensity=ReadTexture(intensitySource);
            var pixels=mask.GetPixels();
            for(int y=0;y<mask.height;y++)for(int x=0;x<mask.width;x++)
            {
                // Native alpha ignores intensity: without this low-energy opening,
                // white-gold remapping reveals opaque black islands under the foam.
                float g=intensity.GetPixelBilinear((x+.5f)/mask.width,(y+.5f)/mask.height).g;
                float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.30f,g));
                float m=pixels[x+y*mask.width].r*opening;
                pixels[x+y*mask.width]=new Color(m,m,m,1);
            }
            Object.DestroyImmediate(intensity);mask.SetPixels(pixels);mask.Apply(false,true);
            mask.wrapMode=maskSource.wrapMode;mask.filterMode=FilterMode.Bilinear;mask.name="Christasha_Runtime_XiaoFoamCoverage";owned.Add(mask);return mask;
        }

        static Texture2D ReadTexture(Texture source)
        {
            // Runtime inputs are often non-readable. Read only a temporary GPU copy.
            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D result = null;
            try
            {
                Graphics.Blit(source, rt); RenderTexture.active = rt;
                result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); result.Apply();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
            return result;
        }

        Material BrushMaterial()
        {
            if (brushMaterial != null) return brushMaterial;
            if (brush == null) throw new InvalidOperationException("Mass recipes need the original readable local tide brush");
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default missing");
            var texture = new Texture2D(brush.width, brush.height, TextureFormat.RGBA32, false, true);
            var pixels = brush.GetPixels();
            for (int i=0;i<pixels.Length;i++) pixels[i] = new Color(1,1,1,Mathf.Clamp01(pixels[i].r*1.6f));
            texture.SetPixels(pixels); texture.Apply(false,true); texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = "ChristashaMass_LocalFoamAlpha"; owned.Add(texture);
            brushMaterial = new Material(shader) { name = "ChristashaMass_Foam" };
            brushMaterial.SetTexture("_MainTex", texture); owned.Add(brushMaterial); return brushMaterial;
        }

        void AddSupport(Transform parent, string name, float width, int capacity)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false);
            var p = go.AddComponent<ParticleSystem>(); p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.playOnAwake=false; main.loop=false; main.duration=1.5f;
            main.startSpeed=0; main.maxParticles=capacity; main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            var emission=p.emission; emission.enabled=false;
            var shape=p.shape; shape.enabled=false;
            var col=p.colorOverLifetime; col.enabled=true;
            var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.83f,.74f,.49f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.4f),new GradientAlphaKey(0,1)});col.color=gradient;
            var size=p.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.24f));
            var mesh=new Mesh { name="ChristashaMass_BrushStrip" };
            mesh.vertices=new[]{new Vector3(-.5f,-width,0),new Vector3(.5f,-width,0),new Vector3(-.5f,width,0),new Vector3(.5f,width,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,0),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();owned.Add(mesh);
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;renderer.sharedMaterial=BrushMaterial();renderer.sortingOrder=5;
        }

        void AddStrings(Transform parent, int count)
        {
            if(lineMaterial==null){lineMaterial=new Material(Shader.Find("Sprites/Default")){name="ChristashaMass_Strings"};owned.Add(lineMaterial);}
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("MassString"+i);go.transform.SetParent(parent,false);
                var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=48;line.sharedMaterial=lineMaterial;line.sortingOrder=4;
                line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.16f,.55f),new Keyframe(.65f,1),new Keyframe(1,0));
                line.widthMultiplier=.085f-i*.014f;line.numCornerVertices=2;line.numCapVertices=2;line.enabled=false;
            }
        }

        internal static ParticleSystem FindSystem(GameObject root,string name)
        {foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))if(p.name==name)return p;return null;}

        public static IChristashaNativePlaybackHooks TryCreatePlayback(GameObject instance,uint seed)
        {
            foreach(var t in instance.GetComponentsInChildren<Transform>(true))
                if(t.name.StartsWith(Marker,StringComparison.Ordinal))return new Playback(instance,t.name.Substring(Marker.Length),seed);
            return null;
        }

        sealed class Playback : IChristashaNativePlaybackHooks
        {
            readonly string recipe; readonly Transform art; readonly Vector3 scale,position;
            readonly ParticleSystem foam,spray; readonly LineRenderer[] strings;
            readonly List<Colors> colors=new List<Colors>(); readonly System.Random random;
            int emitted;
            // Tail extension is visual-only: the manager still releases Area at its
            // existing total=.5s gate. The caller's speed multiplier is applied by
            // the shared driver, so these are raw wall seconds at speed=1.
            public float Duration {get{return recipe=="Area"?1.40f:recipe=="Cast"?.84f:.58f;}}
            public Playback(GameObject root,string recipe,uint seed)
            {
                this.recipe=recipe;random=new System.Random(unchecked((int)seed));
                art=root.transform.Find(recipe=="Area"?"MassWaveArt":"MassCastArt");
                if(art!=null){scale=art.localScale;position=art.localPosition;}
                foam=FindSystem(root,"MassFoam");spray=FindSystem(root,"MassSpray");strings=root.GetComponentsInChildren<LineRenderer>(true);
                foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))
                    if(!p.name.StartsWith("Mass",StringComparison.Ordinal))colors.Add(new Colors(p.GetComponent<Renderer>(),recipe));
                foreach(var c in colors)c.Reset();
            }
            public void BeforeAnimator(float dt,float age){foreach(var c in colors)c.Restore();}
            public void AfterAnimator(float dt,float age)
            {
                foreach(var c in colors)c.Map(age);
                if(art!=null&&recipe=="Area")
                {
                    float unfold=Mathf.SmoothStep(.15f,1f,Mathf.Clamp01(age/.2f));
                    art.localScale=Vector3.Scale(scale,new Vector3(unfold,Mathf.Lerp(.34f,1,unfold),unfold));
                    art.localPosition=position+new Vector3(Mathf.Lerp(-13,0,unfold),0,0);
                }
                for(int i=0;i<strings.Length;i++)
                {
                    float start=recipe=="Area"?.09f+i*.03f:.025f+i*.045f;
                    float u=Mathf.Clamp01((age-start)/(recipe=="Area"?.24f:.23f));
                    float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(recipe=="Area"?.70f:.48f,recipe=="Area"?1.32f:.78f,age));
                    var line=strings[i];line.enabled=u>0&&fade>.001f;
                    line.startColor=new Color(1,.81f,.42f,.52f*fade);line.endColor=new Color(1,.96f,.75f,.72f*fade);
                    for(int n=0;n<48;n++)
                    {
                        float t=(n/47f)*u;
                        Vector3 p=recipe=="Area"?Cubic(new Vector3(-17,1+i*.30f,0),new Vector3(-5,2,0),new Vector3(7,17+i*.85f,0),new Vector3(15,5+i*.55f,0),t):Cubic(new Vector3(-2.4f,-.5f+i*.35f,0),new Vector3(-1,3,0),new Vector3(2.8f,2.2f,0),new Vector3(4.5f,.65f+i*.32f,0),t);
                        p.z=-.5f-i*.01f;line.SetPosition(n,p);
                    }
                }
            }
            public void AfterParticles(float dt,float age)
            {
                int total=recipe=="Area"?20:recipe=="Point"?2:9;
                float begin=recipe=="Area"?.125f:recipe=="Point"?0:.04f;
                float interval=recipe=="Area"?.018f:recipe=="Point"?.022f:.025f;
                while(emitted<total&&age+.000001f>=begin+emitted*interval)
                {
                    if(recipe=="Area")
                    {
                        float a=Mathf.Lerp(-1.1f,2.25f,emitted/19f);
                        var at=new Vector3(-.5f+14f*Mathf.Cos(a),9f+10f*Mathf.Sin(a),-.4f);
                        var tangent=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0);
                        Emit(foam,at,tangent*Range(3,9)+Vector3.up*Range(1,3),Range(1.1f,2.15f),a*Mathf.Rad2Deg+90,Range(.38f,.68f));
                        Emit(spray,at,tangent*Range(5,13),Range(1.25f,2.3f),a*Mathf.Rad2Deg+90,Range(.34f,.62f));
                    }
                    else if(recipe=="Point")for(int i=0;i<7;i++)
                    {
                        float a=Range(0,Mathf.PI*2);var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                        Emit(foam,d*.1f,d*Range(5,11),Range(.45f,1.05f),a*Mathf.Rad2Deg,Range(.25f,.48f));
                        Emit(spray,d*.15f,d*Range(7,15),Range(.8f,1.4f),a*Mathf.Rad2Deg,Range(.25f,.48f));
                    }
                    else
                    {
                        float a=emitted*.67f;var at=new Vector3(Mathf.Cos(a)*2,Mathf.Sin(a)*1.5f,0);
                        Emit(foam,at,-at*2,Range(.3f,.65f),a*Mathf.Rad2Deg+80,.42f);
                    }
                    emitted++;
                }
            }
            void Emit(ParticleSystem p,Vector3 at,Vector3 velocity,float size,float rotation,float life)
            {if(p==null)return;var e=new ParticleSystem.EmitParams{position=at,velocity=velocity,startSize=size,rotation=rotation,startLifetime=life,startColor=new Color(1,Range(.88f,.98f),Range(.63f,.83f),1),applyShapeToPosition=false};p.Emit(e,1);}
            float Range(float a,float b){return a+(b-a)*(float)random.NextDouble();}
            public void StopAndClear(){foreach(var line in strings)line.enabled=false;foreach(var c in colors)c.Restore();if(art!=null){art.localScale=scale;art.localPosition=position;}}
        }

        sealed class Colors
        {
            readonly Renderer renderer; readonly Material material;readonly string recipe;
            readonly MaterialPropertyBlock native=new MaterialPropertyBlock(),mapped=new MaterialPropertyBlock();
            readonly List<string> properties=new List<string>(); bool sampled;
            public Colors(Renderer renderer,string recipe)
            {
                this.renderer=renderer;this.recipe=recipe;material=renderer.sharedMaterial;
                foreach(var s in new[]{"_Color","_MainColor","MainColor","_DissolveColor"})if(material.HasProperty(s))properties.Add(s);
            }
            public void Reset(){renderer.SetPropertyBlock(null);sampled=false;}
            public void Restore(){if(sampled)renderer.SetPropertyBlock(native);}
            public void Map(float age)
            {
                renderer.GetPropertyBlock(native);renderer.GetPropertyBlock(mapped);
                foreach(var prop in properties)
                {
                    Color value=native.isEmpty?material.GetColor(prop):native.GetColor(prop);
                    // MaterialPropertyBlock has no reliable per-property presence
                    // query on Unity 2019. Treat an all-zero read as a missing or
                    // invalid field only when the owned material carries a finite,
                    // non-zero value. Genuine animated zero fields (for example the
                    // native MainTrail body) remain zero and are never whitened.
                    Color fallback=material.GetColor(prop);
                    bool invalid=float.IsNaN(value.r)||float.IsNaN(value.g)||float.IsNaN(value.b)||float.IsInfinity(value.r)||float.IsInfinity(value.g)||float.IsInfinity(value.b);
                    float fallbackGate=recipe=="Area"?.70f:recipe=="Cast"?.55f:.28f;
                    if(!native.isEmpty&&!invalid&&age<fallbackGate&&Mathf.Abs(value.r)+Mathf.Abs(value.g)+Mathf.Abs(value.b)<.05f&&Mathf.Abs(fallback.r)+Mathf.Abs(fallback.g)+Mathf.Abs(fallback.b)>.05f)value=fallback;
                    float energy=Mathf.Max(value.r,Mathf.Max(value.g,value.b));
                    Color tint;
                    if(recipe=="Area")
                    {
                        tint=renderer.name=="Trail"?new Color(.48f,.46f,.41f):renderer.name=="Trail2"?new Color(.70f,.56f,.32f):new Color(.40f,.375f,.29f);
                        if(renderer.name.StartsWith("Spark",StringComparison.Ordinal))tint=new Color(1.12f,1.03f,.76f);
                        if(prop=="_DissolveColor")tint=new Color(.12f,.105f,.07f);
                    }
                    else tint=recipe=="Cast"?new Color(1.15f,1.03f,.70f):new Color(1.2f,1.13f,.91f);
                    mapped.SetColor(prop,new Color(energy*tint.r,energy*tint.g,energy*tint.b,value.a));
                }
                sampled=true;renderer.SetPropertyBlock(mapped);
            }
        }

        static Vector3 Cubic(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
        {float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}

        public void Dispose()
        {
            if(cache!=null)Object.DestroyImmediate(cache);
            foreach(var item in owned)if(item!=null)Object.DestroyImmediate(item);
            owned.Clear();templates.Clear();tideTextures.Clear();cache=null;brushMaterial=null;lineMaterial=null;
        }
    }
}
