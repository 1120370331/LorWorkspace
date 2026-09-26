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
        Material brushMaterial, crestMaterial, lineMaterial, scoreMaterial;

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
                if(p.name=="Trail")
                {
                    // Xiao's native shader uses particle COLOR.r/g as two mask
                    // erosion thresholds. Freeze the open crest at its .20s
                    // phase until the .75s battle release, then finish the
                    // native dissolve rather than showing only strings.
                    var color=p.colorOverLifetime;color.enabled=true;
                    var gradient=new Gradient();
                    gradient.SetKeys(new[]{
                        new GradientColorKey(new Color(.55f,0f,0f),0f),
                        new GradientColorKey(new Color(.6125f,.15f,0f),.25f),
                        new GradientColorKey(new Color(.6125f,.15f,0f),.59f),
                        new GradientColorKey(new Color(1f,1f,0f),.70f),
                        new GradientColorKey(new Color(1f,1f,0f),1f)
                    },new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(1f,.59f),
                        new GradientAlphaKey(0f,.70f),new GradientAlphaKey(0f,1f)});
                    color.color=gradient;
                }
                if (p.name == "Trail2") { main.startDelay = .055f; main.startSizeMultiplier *= .78f; }
                if (p.name == "Trail3") { main.startDelay = .095f; main.startSizeMultiplier *= .63f; }
                if (p.name == "Trail4") { main.startDelay = .035f; main.startSizeMultiplier *= .45f; }
                if (p.name == "Trail_Wave")
                {
                    main.startDelay = .075f; main.startLifetime = .68f; main.startSizeMultiplier *= .78f;
                    // Its normalized .55 is Area age .45, the manager release.
                    // Fade via particle color rather than shader RGB: zero RGB
                    // leaves opaque black pieces in the native wave shaders.
                    var color=p.colorOverLifetime;color.enabled=true;
                    var gradient=new Gradient();
                    gradient.SetKeys(new[]{new GradientColorKey(new Color(0f,.5f,0f),0f),
                        new GradientColorKey(new Color(.55f,.5f,0f),.55f),
                        new GradientColorKey(new Color(1f,.5f,0f),.70f),
                        new GradientColorKey(new Color(1f,.5f,0f),1f)},
                        new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(1f,.55f),
                        new GradientAlphaKey(0f,.70f),new GradientAlphaKey(0f,1f)});
                    color.color=gradient;
                }
                if (p.name.StartsWith("Spark", StringComparison.Ordinal))
                {
                    main.startLifetime = new ParticleSystem.MinMaxCurve(.28f, .56f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,.90f,.63f), new Color(1,1,.92f));
                    var color = p.colorOverLifetime; color.enabled = false;
                }
            }
            AddSupport(root.transform, "MassFoam", .21f, 96);
            AddSupport(root.transform, "MassSpray", .07f, 96);
            // Short, separately broken brush strokes keep a broad wavefront visible
            // between the native expansion and the manager's result callback.
            AddSupport(root.transform, "MassCrest", .42f, 32);
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
            AddScoreRing(root.transform, 5);
        }

        void BuildPoint(GameObject root)
        {
            var native = Source(PointResource, root.transform);
            Retain(native, new HashSet<string> { "Trail1_cross" });
            CloneMaterials(native, false, false);
            var cross = FindSystem(root, "Trail1_cross");
            var main = cross.main; main.loop = false; main.duration = .4f;
            main.startDelay = 0f; main.startLifetime = .12f; main.startSize = 1.08f;
            main.startSpeed = 0f; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var shape = cross.shape; shape.enabled = false;
            var emission = cross.emission; emission.enabled = true; emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            AddSupport(root.transform, "MassFoam", .19f, 24);
            AddSupport(root.transform, "MassSpray", .055f, 32);
            AddSupport(root.transform, "MassContact", .13f, 12);
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

        Material CrestMaterial()
        {
            if(crestMaterial!=null)return crestMaterial;
            if(brush==null)throw new InvalidOperationException("Mass crest needs the original readable local tide brush");
            var texture=new Texture2D(brush.width,brush.height,TextureFormat.RGBA32,false,true);
            var pixels=brush.GetPixels();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(pixels[i].r),.55f));
            texture.SetPixels(pixels);texture.Apply(false,true);texture.wrapMode=TextureWrapMode.Clamp;
            texture.name="ChristashaMass_CrestBrushAlpha";owned.Add(texture);
            crestMaterial=new Material(Shader.Find("Sprites/Default")){name="ChristashaMass_CrestBrush"};
            crestMaterial.SetTexture("_MainTex",texture);owned.Add(crestMaterial);return crestMaterial;
        }

        void AddSupport(Transform parent, string name, float width, int capacity)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false);
            var p = go.AddComponent<ParticleSystem>(); p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.playOnAwake=false; main.loop=false; main.duration=1.5f;
            main.startSpeed=0; main.maxParticles=capacity; main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=p.emission; emission.enabled=false;
            var shape=p.shape; shape.enabled=false;
            var col=p.colorOverLifetime; col.enabled=true;
            var gradient=new Gradient();
            if(name=="MassCrest")gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.94f,.89f,.74f),1)},
                // Emitted at Area age .145 for .64s: normalized .48 is the .75s
                // manager release. Let Point and native HP text lead after it.
                new[]{new GradientAlphaKey(.82f,0),new GradientAlphaKey(.96f,.2f),new GradientAlphaKey(.90f,.48f),new GradientAlphaKey(0f,.65f),new GradientAlphaKey(0f,1f)});
            else gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.83f,.74f,.49f),1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.4f),new GradientAlphaKey(0,1)});
            col.color=gradient;
            var size=p.sizeOverLifetime;size.enabled=true;
            size.size=name=="MassCrest"?new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.91f),new Keyframe(.2f,1),new Keyframe(.72f,1),new Keyframe(1,.78f))):
                new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.24f));
            Mesh mesh;
            if(name=="MassCrest")mesh=CrestArcMesh();
            else
            {
                mesh=new Mesh { name="ChristashaMass_BrushStrip" };
                mesh.vertices=new[]{new Vector3(-.5f,-width,0),new Vector3(.5f,-width,0),new Vector3(-.5f,width,0),new Vector3(.5f,width,0)};
                float edge=name=="MassContact" ? .22f : 0f;
                mesh.uv=new[]{new Vector2(edge,0),new Vector2(edge,1),new Vector2(1f-edge,0),new Vector2(1f-edge,1)};
                mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();owned.Add(mesh);
            }
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
            renderer.alignment=ParticleSystemRenderSpace.Local;renderer.mesh=mesh;
            renderer.sharedMaterial=name=="MassCrest"?CrestMaterial():BrushMaterial();renderer.sortingOrder=5;
        }

        void AddScoreRing(Transform parent, int capacity)
        {
            // A staff-line sheet that circles the caster during the Cast wind-up.
            // This is a quad-mesh ParticleSystem, never a LineRenderer: the Cast
            // playback overwrites every LineRenderer with its rising sweep, and
            // any system not named "Mass*" would be captured by the native MPB remap.
            var go = new GameObject("MassScore"); go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParticleSystem>(); p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.playOnAwake = false; main.loop = false; main.duration = 1.5f;
            main.startSpeed = 0; main.maxParticles = capacity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = p.emission; emission.enabled = false;
            var shape = p.shape; shape.enabled = false;
            var col = p.colorOverLifetime; col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0), new GradientColorKey(new Color(1f,.94f,.78f),1)},
                new[]{new GradientAlphaKey(0f,0), new GradientAlphaKey(.92f,.18f), new GradientAlphaKey(.78f,.62f), new GradientAlphaKey(0f,1f)});
            col.color = gradient;
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.mesh = ScoreSheetMesh();
            renderer.sharedMaterial = ScoreMaterial();
            renderer.sortingOrder = 5;
        }

        Mesh ScoreSheetMesh()
        {
            // One page of five staff lines. The alpha comes from the local brush row
            // so the sheet frays the same way as the rest of the library; the lines
            // are geometry, not a full-screen plate.
            const int lines = 5, segments = 24;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int line = 0; line < lines; line++)
            {
                float y = (line / (float)(lines - 1) - .5f) * .72f;
                int start = vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float x = (t - .5f) * 1.9f;
                    // Break each staff line at seeded-free fixed gaps so the page reads
                    // as measures rather than one continuous rule.
                    float bend = Mathf.Sin(t * 6.2831853f) * .022f;
                    vertices.Add(new Vector3(x, y + bend, -.02f + line * .002f));
                    vertices.Add(new Vector3(x, y + bend + .032f, -.02f + line * .002f));
                    uv.Add(new Vector2(t, .5f)); uv.Add(new Vector2(t, .5f));
                    if (i == segments) continue;
                    int v = start + i * 2;
                    triangles.Add(v); triangles.Add(v + 2); triangles.Add(v + 1);
                    triangles.Add(v + 1); triangles.Add(v + 2); triangles.Add(v + 3);
                }
            }
            var mesh = new Mesh { name = "ChristashaMass_ScoreSheet" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        Material ScoreMaterial()
        {
            if (scoreMaterial != null) return scoreMaterial;
            if (brush == null) throw new InvalidOperationException("Mass score needs the original readable local tide brush");
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default missing");
            var texture = new Texture2D(brush.width, brush.height, TextureFormat.RGBA32, false, true);
            var pixels = brush.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1, 1, 1, Mathf.Clamp01(pixels[i].r * 1.15f));
            texture.SetPixels(pixels); texture.Apply(false, true); texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = "ChristashaMass_ScoreAlpha"; owned.Add(texture);
            scoreMaterial = new Material(shader) { name = "ChristashaMass_Score" };
            scoreMaterial.SetTexture("_MainTex", texture); owned.Add(scoreMaterial); return scoreMaterial;
        }

        Mesh CrestArcMesh()
        {
            // One curved brush stroke, not evenly repeated triangular stamps.
            // Its original local brush supplies broken alpha and frayed edges.
            const int segments=36;
            var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];
            var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float t=i/(float)segments;
                float a=Mathf.Lerp(.62f,3.80f,t);
                var center=new Vector3(-.5f+14f*Mathf.Cos(a),9f+10f*Mathf.Sin(a),-.36f);
                var tangent=new Vector3(-14f*Mathf.Sin(a),10f*Mathf.Cos(a),0).normalized;
                var normal=new Vector3(tangent.y,-tangent.x,0);
                float width=1.58f*Mathf.Pow(Mathf.Sin(Mathf.PI*t),.48f);
                vertices[i*2]=center-normal*width;vertices[i*2+1]=center+normal*width;
                uv[i*2]=new Vector2(.21f,t);uv[i*2+1]=new Vector2(.79f,t);
                if(i==segments)continue;
                int j=i*6,v=i*2;
                triangles[j]=v;triangles[j+1]=v+2;triangles[j+2]=v+1;
                triangles[j+3]=v+1;triangles[j+4]=v+2;triangles[j+5]=v+3;
            }
            var mesh=new Mesh{name="ChristashaMass_OpenBrushCrest"};
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();owned.Add(mesh);return mesh;
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
            readonly string recipe; readonly Transform art; readonly Vector3 scale,position; readonly Quaternion rotation;
            readonly ParticleSystem foam,spray,crest,contact,score; readonly LineRenderer[] strings;
            readonly List<Colors> colors=new List<Colors>(); readonly System.Random random;
            readonly WaveVariant variant;
            ParticleSystem.Particle[] scoreParticles;
            int emitted,crestEmitted,scoreEmitted; bool contactEmitted;
            // The caller's speed multiplier is applied by the shared driver.
            public float Duration {get{return recipe=="Area"?1.40f:recipe=="Cast"?.84f:.58f;}}
            public Playback(GameObject root,string recipe,uint seed)
            {
                this.recipe=recipe;random=new System.Random(unchecked((int)seed));
                art=root.transform.Find(recipe=="Area"?"MassWaveArt":"MassCastArt");
                if(art!=null){scale=art.localScale;position=art.localPosition;rotation=art.localRotation;}
                foam=FindSystem(root,"MassFoam");spray=FindSystem(root,"MassSpray");
                crest=FindSystem(root,"MassCrest");contact=FindSystem(root,"MassContact");
                score=FindSystem(root,"MassScore");
                if(score!=null)scoreParticles=new ParticleSystem.Particle[score.main.maxParticles];
                // Per-dice variety. The instance seed already differs per dice, so this
                // draws a different silhouette for each one without touching placement,
                // the native carrier, or the manager's release timing.
                variant=WaveVariants[random.Next(WaveVariants.Length)];
                strings=root.GetComponentsInChildren<LineRenderer>(true);
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
                    float unfold=Mathf.SmoothStep(.15f,1f,Mathf.Clamp01(age/Mathf.Max(.01f,variant.unfoldTime)));
                    art.localScale=Vector3.Scale(scale,new Vector3(unfold*Mathf.Lerp(.34f,1,unfold)*variant.scaleBias,Mathf.Lerp(.34f,1,unfold),unfold*Mathf.Lerp(.34f,1,unfold)*variant.scaleBias));
                    art.localRotation=rotation*Quaternion.Euler(0,0,variant.rotationBias);
                    art.localPosition=position+new Vector3(Mathf.Lerp(-13,0,unfold),Undulation(age,unfold),0);
                }
                if(recipe=="Cast")AnimateScore(age);
                for(int i=0;i<strings.Length;i++)
                {
                    float start=recipe=="Area"?.09f+i*.03f:.025f+i*.045f;
                    float u=Mathf.Clamp01((age-start)/(recipe=="Area"?.24f:.23f));
                    float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(recipe=="Area"?.70f:.48f,recipe=="Area"?1.32f:.78f,age));
                    var line=strings[i];line.enabled=u>0&&fade>.001f;
                    line.startColor=new Color(1,.89f,.62f,.60f*fade);line.endColor=new Color(1,.98f,.85f,.78f*fade);
                    for(int n=0;n<48;n++)
                    {
                        float t=(n/47f)*u;
                        float travel=Mathf.Clamp01(t+variant.stringPhase);
                        Vector3 p=recipe=="Area"?Cubic(new Vector3(-17,1+i*.30f,0),new Vector3(-5,2,0),new Vector3(7,17+i*.85f+variant.riseHeight*.35f,0),new Vector3(15,5+i*.55f,0),travel):Cubic(new Vector3(-2.4f,-.5f+i*.35f,0),new Vector3(-1,3,0),new Vector3(2.8f,2.2f,0),new Vector3(4.5f,.65f+i*.32f,0),travel);
                        p.z=-.5f-i*.01f;line.SetPosition(n,p);
                    }
                }
            }
            public void AfterParticles(float dt,float age)
            {
                int total=recipe=="Area"?20:recipe=="Point"?1:9;
                float begin=recipe=="Area"?.125f:recipe=="Point"?0:.04f;
                float interval=recipe=="Area"?.018f:recipe=="Point"?.022f:.025f;
                while(emitted<total&&age+.000001f>=begin+emitted*interval)
                {
                    if(recipe=="Area")
                    {
                        float a=Mathf.Lerp(variant.crestStart,variant.crestEnd,emitted/19f);
                        var at=new Vector3(-.5f+14f*Mathf.Cos(a),9f+10f*Mathf.Sin(a)*variant.riseHeight/10f,-.4f);
                        at.y+=Undulation(age,Mathf.SmoothStep(.15f,1f,Mathf.Clamp01(age/Mathf.Max(.01f,variant.unfoldTime))));
                        var tangent=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0);
                        Emit(foam,at,tangent*Range(3,9)+Vector3.up*Range(1,3),Range(1.1f,2.15f),a*Mathf.Rad2Deg+90,Range(.38f,.68f));
                        Emit(spray,at,tangent*Range(5,13),Range(1.25f,2.3f),a*Mathf.Rad2Deg+90,Range(.34f,.62f));
                    }
                    else if(recipe=="Point")for(int i=0;i<9;i++)
                    {
                        float a=Range(0,Mathf.PI*2);var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                        EmitTinted(foam,d*.1f,d*Range(5,10),Range(.8f,1.45f),a*Mathf.Rad2Deg,Range(.30f,.45f),new Color(1f,.96f,.84f,.95f));
                        Emit(spray,d*.15f,d*Range(8,14),Range(1f,1.7f),a*Mathf.Rad2Deg,Range(.28f,.42f));
                    }
                    else
                    {
                        float a=emitted*.67f;var at=new Vector3(Mathf.Cos(a)*2,Mathf.Sin(a)*1.5f,0);
                        Emit(foam,at,-at*2,Range(.3f,.65f),a*Mathf.Rad2Deg+80,.42f);
                    }
                    emitted++;
                }
                if(recipe=="Cast" && score!=null && scoreEmitted==0)
                {
                    // Five sheets live for the whole wind-up; the ring animates them
                    // directly, so they are emitted once and never re-spawned.
                    for(int i=0;i<5;i++)
                        EmitTinted(score,Vector3.zero,Vector3.zero,Range(.52f,.70f),Range(-25f,25f),.86f,
                            new Color(1f,Range(.93f,.99f),Range(.72f,.88f),.92f));
                    scoreEmitted=1;
                }
                if(recipe=="Area" && crest!=null && crestEmitted==0 && age>=.145f)
                {
                    EmitTinted(crest,Vector3.zero,Vector3.left*.55f,1f,0f,.64f,new Color(1f,.98f,.86f,.95f));
                    crestEmitted=1;
                }
                if(recipe=="Point" && contact!=null && !contactEmitted)
                {
                    for(int i=0;i<2;i++)
                        EmitTinted(contact,Vector3.zero,Vector3.zero,2.05f,18f+i*72f,.16f,new Color(1f,.99f,.91f,.95f));
                    for(int i=0;i<5;i++)
                    {
                        float a=(i+.17f)*Mathf.PI*2f/5f;
                        var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                        EmitTinted(contact,d*.12f,d*Range(1.8f,3.2f),Range(1.45f,2.15f),a*Mathf.Rad2Deg,Range(.10f,.15f),new Color(1f,.98f,.88f,.95f));
                    }
                    contactEmitted=true;
                }
            }
            /// <summary>
            /// Travelling vertical swell of the wavefront. Two detuned sines keep the
            /// roll from reading as a single clean oscillation, and the envelope holds
            /// the amplitude down during the unfold so the wave plants before it lifts.
            /// </summary>
            float Undulation(float age,float unfold)
            {
                float travel=age*variant.undulationFrequency*2.4f;
                float primary=Mathf.Sin(travel);
                float secondary=Mathf.Sin(travel*1.73f+1.1f);
                float envelope=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(unfold));
                return (primary*.72f+secondary*.28f)*variant.undulationAmplitude*envelope;
            }

            /// <summary>
            /// The sheet-music ring orbits the caster for the whole Cast wind-up.
            /// Positions are written directly because the systems are Paused and only
            /// stepped by the shared manual clock.
            /// </summary>
            void AnimateScore(float age)
            {
                if(score==null||scoreParticles==null)return;
                int count=score.GetParticles(scoreParticles);
                if(count<=0)return;
                // The root sits at the caster's feet and a game unit is about three
                // world units tall, so the ring is sized to circle the torso rather
                // than the shins.
                float radius=1.05f+Mathf.Sin(age*2.1f)*.10f;
                float spin=age*2.35f;
                for(int i=0;i<count;i++)
                {
                    // Each sheet holds its own orbital offset so the ring never
                    // collapses into a single rotating card.
                    float lane=(i+.5f)/Mathf.Max(1f,count)*Mathf.PI*2f;
                    float angle=lane+spin;
                    float lift=1.45f+Mathf.Sin(age*1.5f+lane*1.7f)*.35f;
                    var position=new Vector3(Mathf.Cos(angle)*radius,lift+Mathf.Sin(angle*2f)*.08f,Mathf.Sin(angle)*radius*.55f);
                    scoreParticles[i].position=position;
                    scoreParticles[i].rotation3D=new Vector3(0,Mathf.Lerp(-58f,58f,Mathf.Sin(angle)),Mathf.Lerp(-7f,7f,Mathf.Cos(angle*1.3f)));
                }
                score.SetParticles(scoreParticles,count);
            }

            void Emit(ParticleSystem p,Vector3 at,Vector3 velocity,float size,float rotation,float life)
            {EmitTinted(p,at,velocity,size,rotation,life,new Color(1,Range(.88f,.98f),Range(.63f,.83f),1));}
            void EmitTinted(ParticleSystem p,Vector3 at,Vector3 velocity,float size,float rotation,float life,Color tint)
            {if(p==null)return;var e=new ParticleSystem.EmitParams{position=at,velocity=velocity,startSize=size,rotation=rotation,startLifetime=life,startColor=tint,applyShapeToPosition=false};p.Emit(e,1);}
            float Range(float a,float b){return a+(b-a)*(float)random.NextDouble();}
            public void StopAndClear(){foreach(var line in strings)line.enabled=false;foreach(var c in colors)c.Restore();if(art!=null){art.localScale=scale;art.localPosition=position;art.localRotation=rotation;}}
        }

        /// <summary>
        /// One authored golden-wave silhouette. The native torus is shared by every
        /// mass instance, so per-dice variety comes from these runtime parameters
        /// rather than from separate recipe names - the recipe name is asserted by
        /// the managed contracts and stays "Area".
        /// </summary>
        struct WaveVariant
        {
            public float crestStart, crestEnd;   // emission arc, radians
            public float riseHeight;             // vertical gain of the emission arc
            public float unfoldTime;             // how fast the native carrier opens
            public float undulationAmplitude;    // vertical travel of the wavefront
            public float undulationFrequency;    // number of crests across the sweep
            public float stringPhase;            // per-variant travel offset of the fly lines
            public float scaleBias;              // uniform art-scale bias
            public float rotationBias;           // authored Z tilt of the carrier
        }

        static readonly WaveVariant[] WaveVariants = new[]
        {
            // Broad, slow, single dominant crest.
            new WaveVariant{crestStart=-1.1f,crestEnd=2.25f,riseHeight=10f,unfoldTime=.20f,
                undulationAmplitude=.55f,undulationFrequency=1.6f,stringPhase=0f,scaleBias=1f,rotationBias=0f},
            // Tall and narrow, quicker to open.
            new WaveVariant{crestStart=-.55f,crestEnd=1.85f,riseHeight=13f,unfoldTime=.16f,
                undulationAmplitude=.34f,undulationFrequency=2.4f,stringPhase=.35f,scaleBias=1.06f,rotationBias=-4f},
            // Wide and flat, low travelling swell.
            new WaveVariant{crestStart=-1.45f,crestEnd=2.55f,riseHeight=7.5f,unfoldTime=.24f,
                undulationAmplitude=.78f,undulationFrequency=1.15f,stringPhase=-.28f,scaleBias=.96f,rotationBias=3f},
            // Double-lobed, deeper roll.
            new WaveVariant{crestStart=-1.25f,crestEnd=2.05f,riseHeight=9f,unfoldTime=.19f,
                undulationAmplitude=.92f,undulationFrequency=2.9f,stringPhase=.18f,scaleBias=1.02f,rotationBias=-2f},
            // Late-opening long swell.
            new WaveVariant{crestStart=-.85f,crestEnd=2.4f,riseHeight=11.5f,unfoldTime=.28f,
                undulationAmplitude=.44f,undulationFrequency=1.9f,stringPhase=-.45f,scaleBias=.98f,rotationBias=5f},
        };

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
                    bool invalid=float.IsNaN(value.r)||float.IsNaN(value.g)||float.IsNaN(value.b)||float.IsInfinity(value.r)||float.IsInfinity(value.g)||float.IsInfinity(value.b);
                    float fallbackGate=recipe=="Area"?.70f:recipe=="Cast"?.55f:.28f;
                    if(invalid || (!native.isEmpty&&age<fallbackGate&&Mathf.Abs(value.r)+Mathf.Abs(value.g)+Mathf.Abs(value.b)<.05f))
                        value=ChristashaNativeParticleLibrary.SafeColor(material,prop,Color.white);
                    float energy=Mathf.Max(value.r,Mathf.Max(value.g,value.b));
                    if(recipe=="Area" && prop=="_Color" && (renderer.name=="Trail" || renderer.name=="Trail_Wave"))
                    {
                        // Native Xiao color animation removes the large carrier
                        // before our .75s release. Retain its textured body under
                        // the broken crest, then let both fall away after contact.
                        float enter=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.12f,.22f,age));
                        float leave=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.48f,.68f,age));
                        energy=Mathf.Max(energy,(renderer.name=="Trail"?1.35f:1.05f)*enter*leave);
                    }
                    Color tint;
                    if(recipe=="Area")
                    {
                        tint=renderer.name=="Trail"?new Color(.69f,.67f,.59f):renderer.name=="Trail2"?new Color(.78f,.69f,.51f):
                            renderer.name=="Trail_Wave"?new Color(.86f,.81f,.69f):new Color(.49f,.46f,.36f);
                        if(renderer.name.StartsWith("Spark",StringComparison.Ordinal))tint=new Color(1.12f,1.03f,.76f);
                        if(prop=="_DissolveColor")tint=new Color(.24f,.21f,.14f);
                    }
                    else tint=recipe=="Cast"?new Color(1.15f,1.08f,.86f):new Color(1.2f,1.15f,.98f);
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
            owned.Clear();templates.Clear();tideTextures.Clear();cache=null;brushMaterial=null;crestMaterial=null;lineMaterial=null;scoreMaterial=null;
        }
    }
}
