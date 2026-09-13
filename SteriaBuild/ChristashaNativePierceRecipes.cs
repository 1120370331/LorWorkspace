using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Steria.Vfx
{
    /// <summary>Native Roland Dagger-derived Pierce and Projectile recipes.</summary>
    public sealed class ChristashaNativePierceRecipes : IDisposable
    {
        public const string DaggerResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_dagger";
        const string Marker = "__ChristashaNativePierce_";
        readonly Func<string, GameObject> resolver;
        readonly Texture2D brush;
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        readonly Dictionary<Texture, Texture2D> masks = new Dictionary<Texture, Texture2D>();
        GameObject cache;
        Material brushMaterial;
        Material whiteBrushMaterial;

        public ChristashaNativePierceRecipes(Func<string, GameObject> resolver, Texture2D originalBrush)
        {
            if (resolver == null) throw new ArgumentNullException("resolver");
            this.resolver = resolver; brush = originalBrush;
        }

        public GameObject CreatePrototype(string recipe, Transform parent = null)
        {
            if (recipe != "Pierce" && recipe != "Projectile") throw new ArgumentException("Unknown Pierce recipe: " + recipe, "recipe");
            GameObject template;
            if (!templates.TryGetValue(recipe, out template))
            {
                EnsureCache();
                template = new GameObject("ChristashaNative_" + recipe + "Template");
                template.SetActive(false); template.transform.SetParent(cache.transform, false);
                new GameObject(Marker + recipe).transform.SetParent(template.transform, false);
                try { Build(template, recipe); templates.Add(recipe, template); }
                catch { Object.DestroyImmediate(template); throw; }
            }
            GameObject instance = Object.Instantiate(template, parent, false);
            instance.SetActive(false);
            instance.name = "ChristashaNative_" + recipe + "Prototype";
            return instance;
        }

        void EnsureCache()
        {
            if (cache != null) return;
            cache = new GameObject("ChristashaNativePierce_InactiveCache"); cache.SetActive(false);
            if (Application.isPlaying) Object.DontDestroyOnLoad(cache);
            owned.Add(cache);
        }

        void Build(GameObject root, string recipe)
        {
            GameObject source = resolver(DaggerResource);
            if (source == null) throw new InvalidOperationException("Missing installed native Dagger source: " + DaggerResource);
            var art = new GameObject("DaggerArtSpace"); art.transform.SetParent(root.transform, false);
            art.transform.localScale = recipe == "Projectile" ? new Vector3(.92f, .92f, .92f) : new Vector3(1.0f, 1.0f, 1.0f);
            GameObject native = ChristashaNativeParticleLibrary.CloneVisualOnly(source, art.transform);
            native.name = "NativeDagger";
            Retain(native, new HashSet<string> { "Slash1", "SlashSmoke", "Smoke", "Burn", "Distortion", "Particle1", "Particle2", "Rush_Particle" });
            ConfigureNative(native, recipe);
            OwnNativeMaterials(native, recipe);
            AddSupport(root.transform, "PierceGoldBundle", .065f, 64, 3);
            AddSupport(root.transform, "PierceBackscatter", .022f, 64, 4);
            AddSupport(root.transform, "PierceAirTail", .012f, 64, 2);
        }

        static void Retain(GameObject root, HashSet<string> names)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!names.Contains(renderer.name)) Object.DestroyImmediate(renderer);
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
                if (!names.Contains(p.name)) Object.DestroyImmediate(p);
        }

        static void ConfigureNative(GameObject root, string recipe)
        {
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = p.main; main.playOnAwake = false; main.loop = false; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (p.name == "Slash1") { main.startDelay = 0f; main.startLifetime = recipe == "Projectile" ? .30f : .40f; main.startSizeMultiplier *= recipe == "Projectile" ? .86f : .95f; }
                if (p.name == "SlashSmoke") { main.startDelay = .018f; main.startLifetime = recipe == "Projectile" ? .34f : .52f; main.startSizeMultiplier *= .72f; }
                if (p.name == "Smoke") { main.startDelay = .028f; main.startLifetime = recipe == "Projectile" ? .38f : .58f; main.startSizeMultiplier *= .70f; }
                if (p.name == "Burn") { main.startDelay = .008f; main.startLifetime = recipe == "Projectile" ? .28f : .40f; main.startSizeMultiplier *= .62f; }
                if (p.name == "Distortion") { main.startDelay = .045f; main.startLifetime = recipe == "Projectile" ? .40f : .62f; main.startSizeMultiplier *= .56f; }
                if (p.name == "Particle1") { main.startDelay = .012f; main.startLifetime = recipe == "Projectile" ? .32f : .52f; }
                if (p.name == "Particle2") { main.startDelay = .020f; main.startLifetime = recipe == "Projectile" ? .40f : .68f; }
                if (p.name == "Rush_Particle") { main.startDelay = .006f; main.startLifetime = recipe == "Projectile" ? .28f : .38f; }
            }
        }

        void OwnNativeMaterials(GameObject root, string recipe)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) if (materials[i] != null)
                {
                    var m = new Material(materials[i]) { name = "ChristashaNative_Pierce_" + renderer.name };
                    owned.Add(m); materials[i] = m;
                    if (brush != null && (renderer.name == "Particle1" || renderer.name == "Rush_Particle"))
                        foreach (var prop in new[] { "_MainTex", "_Main" }) if (m.HasProperty(prop))
                        {
                            Texture source = m.GetTexture(prop); if (source != null) m.SetTexture(prop, PierceMask(source));
                        }
                }
                renderer.sharedMaterials = materials;
            }
        }

        Texture2D PierceMask(Texture source)
        {
            Texture2D result;
            if (masks.TryGetValue(source, out result)) return result;
            var pixels = brush.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float c = Mathf.Clamp01(pixels[i].r * 1.55f);
                pixels[i] = new Color(c, c, c, 1f);
            }
            result = new Texture2D(brush.width, brush.height, TextureFormat.RGBA32, false, true);
            result.name = "Christasha_Pierce_LocalBrokenCore"; result.wrapMode = TextureWrapMode.Clamp; result.filterMode = FilterMode.Bilinear;
            result.SetPixels(pixels); result.Apply(false, true); owned.Add(result); masks.Add(source, result); return result;
        }

        void AddSupport(Transform parent, string name, float width, int capacity, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1f; main.startSpeed = 0f; main.startLifetime = .3f; main.startSize = 1f; main.maxParticles = capacity; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var color = ps.colorOverLifetime; color.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(name == "PierceGoldBundle" ? new Color(1f,.94f,.76f) : new Color(1f,.78f,.28f), .44f), new GradientColorKey(new Color(.54f,.23f,.045f), 1) }, new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(.78f,.42f), new GradientAlphaKey(0,1) }); color.color = new ParticleSystem.MinMaxGradient(grad);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, .35f), new Keyframe(.10f, 1f), new Keyframe(.56f, .55f), new Keyframe(1, 0)));
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = StripMesh(width); r.sharedMaterial = name == "PierceGoldBundle" ? WhiteBrushMaterial() : BrushMaterial(); r.sortingOrder = order;
        }

        Mesh StripMesh(float width)
        {
            var mesh = new Mesh { name = "Christasha_Pierce_BrokenStrip" };
            mesh.vertices = new[] { new Vector3(-.5f,-width,0), new Vector3(-.5f,width,0), new Vector3(.5f,-width*.72f,0), new Vector3(.5f,width*.72f,0) };
            mesh.uv = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,0), new Vector2(1,1) }; mesh.triangles = new[] { 0,1,2,2,1,3 }; mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        Material BrushMaterial()
        {
            if (brushMaterial != null) return brushMaterial;
            if (brush == null) throw new InvalidOperationException("Pierce recipes require original local tide-brush texture");
            Shader shader = Shader.Find("Sprites/Default"); if (shader == null) throw new InvalidOperationException("Sprites/Default missing");
            var pixels = brush.GetPixels(); for (int i = 0; i < pixels.Length; i++) { float a = Mathf.Clamp01(pixels[i].r * 1.55f); pixels[i] = new Color(1f, 1f, 1f, a); }
            var mask = new Texture2D(brush.width, brush.height, TextureFormat.RGBA32, false, true); mask.name = "Christasha_Pierce_BrushAlpha"; mask.wrapMode = TextureWrapMode.Clamp; mask.filterMode = FilterMode.Bilinear; mask.SetPixels(pixels); mask.Apply(false, true); owned.Add(mask);
            var mat = new Material(shader) { name = "Christasha_Pierce_Brush" }; mat.SetTexture("_MainTex", mask); mat.SetColor("_Color", new Color(1f,.86f,.48f,1f)); owned.Add(mat); brushMaterial = mat; return mat;
        }

        Material WhiteBrushMaterial()
        {
            if (whiteBrushMaterial != null) return whiteBrushMaterial;
            var baseMat = BrushMaterial(); whiteBrushMaterial = new Material(baseMat) { name = "Christasha_Pierce_WhiteCore" }; whiteBrushMaterial.SetColor("_Color", new Color(1f,.99f,.92f,1f)); owned.Add(whiteBrushMaterial); return whiteBrushMaterial;
        }

        public static IChristashaNativePlaybackHooks TryCreatePlayback(GameObject instance, uint seed)
        {
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(Marker, StringComparison.Ordinal)) return new Playback(instance, t.name.Substring(Marker.Length), seed);
            return null;
        }

        sealed class Playback : IChristashaNativePlaybackHooks
        {
            readonly GameObject root; readonly string recipe; readonly System.Random random;
            readonly ParticleSystem core, back, air; readonly List<RoleColor> colors = new List<RoleColor>();
            int emitted; readonly float duration;
            public float Duration { get { return duration; } }
            public Playback(GameObject root, string recipe, uint seed)
            {
                this.root = root; this.recipe = recipe; random = new System.Random(unchecked((int)seed));
                // Retain the original burst speed at 91% while extending only the
                // retreat tail: .62/.91≈.68 and .29/.91≈.32. The first core burst
                // remains at its native early timestamps for the .26s projectile gate.
                duration = recipe == "Projectile" ? .32f : .68f;
                core = ChristashaNativeParticleLibrary.FindSystem(root, "PierceGoldBundle"); back = ChristashaNativeParticleLibrary.FindSystem(root, "PierceBackscatter"); air = ChristashaNativeParticleLibrary.FindSystem(root, "PierceAirTail");
                foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
                    if (p.name != "PierceGoldBundle" && p.name != "PierceBackscatter" && p.name != "PierceAirTail") colors.Add(new RoleColor(p.GetComponent<Renderer>(), p.name));
                foreach (var c in colors) c.Reset();
            }
            public void BeforeAnimator(float dt, float elapsed) { foreach (var c in colors) c.Restore(); }
            public void AfterAnimator(float dt, float elapsed) { foreach (var c in colors) c.Map(elapsed, duration); }
            public void AfterParticles(float dt, float elapsed)
            {
                int total = recipe == "Projectile" ? 11 : 17; float start = .012f, interval = recipe == "Projectile" ? .016f : .020f;
                while (emitted < total && elapsed + .000001f >= start + emitted * interval)
                {
                    float t = emitted / (float)Mathf.Max(1, total - 1); float x = Mathf.Lerp(.10f, -2.8f, t); float spread = Mathf.Lerp(.012f, .20f, t);
                    Emit(core, new Vector3(x, Range(-spread, spread), -.20f), new Vector3(-Range(4f,8f), Range(-1f,1f), 0), Range(.52f,.98f), Range(.18f,.35f));
                    if (emitted % 2 == 0) Emit(back, new Vector3(x + Range(.05f,.35f), Range(-.32f,.38f), -.16f), new Vector3(Range(1f,4f), Range(-2f,2f), 0), Range(.18f,.44f), Range(.20f,.44f));
                    if (emitted % 3 == 0) Emit(air, new Vector3(x + Range(.20f,.55f), Range(-.44f,.44f), -.08f), new Vector3(Range(1f,4f), Range(-1.5f,1.5f), 0), Range(.10f,.26f), Range(.24f,.52f));
                    emitted++;
                }
            }
            float Range(float a,float b) { return Mathf.Lerp(a,b,(float)random.NextDouble()); }
            void Emit(ParticleSystem p, Vector3 at, Vector3 velocity, float size, float life) { if (p == null) return; bool white = p == core; p.Emit(new ParticleSystem.EmitParams { position=at, velocity=velocity, startSize=size, startLifetime=life, startColor=white ? new Color(1f,.98f,.88f,1f) : new Color(1f,Range(.82f,.98f),Range(.38f,.72f),1f) }, 1); }
            public void StopAndClear() { foreach (var c in colors) c.Restore(); emitted = 0; foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true)) if (p != null) p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); }
        }

        sealed class RoleColor
        {
            readonly Renderer renderer; readonly string role; readonly Material material; readonly MaterialPropertyBlock source = new MaterialPropertyBlock(); readonly MaterialPropertyBlock mapped = new MaterialPropertyBlock(); bool sampled;
            readonly string[] props = { "_Color", "_MainColor", "MainColor", "_DissolveColor" };
            public RoleColor(Renderer renderer, string role) { this.renderer = renderer; this.role = role; material = renderer.sharedMaterial; }
            public void Reset() { renderer.SetPropertyBlock(null); mapped.Clear(); foreach (var prop in props) if (material != null && material.HasProperty(prop)) mapped.SetColor(prop, material.GetColor(prop)); renderer.SetPropertyBlock(mapped); renderer.GetPropertyBlock(source); sampled = true; }
            public void Restore() { if (sampled) renderer.SetPropertyBlock(source); }
            public void Map(float age, float duration)
            {
                renderer.GetPropertyBlock(source); renderer.GetPropertyBlock(mapped); bool any=false; bool missingBlock=source.isEmpty; float fade=1f-Mathf.SmoothStep(.56f,1f,age/duration);
                foreach (var prop in props) if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(prop))
                {
                    Color c=missingBlock ? material.GetColor(prop) : source.GetColor(prop); Color tint;
                    if (role == "Slash1") tint = new Color(1.12f,1.04f,.84f); else if (role.Contains("Smoke") || role == "Smoke") tint = new Color(1.00f,.72f,.30f); else if (role == "Distortion") tint = new Color(1.00f,.78f,.40f); else tint = new Color(1.05f,.86f,.48f);
                    mapped.SetColor(prop, new Color(c.r*tint.r*fade,c.g*tint.g*fade,c.b*tint.b*fade,c.a)); any=true;
                }
                if (any) { renderer.SetPropertyBlock(mapped); sampled=true; }
            }
        }

        public void Dispose()
        {
            if (cache != null) Object.DestroyImmediate(cache);
            foreach (var o in owned) if (o != null) Object.DestroyImmediate(o);
            owned.Clear(); templates.Clear(); cache=null; brushMaterial=null; whiteBrushMaterial=null;
        }
    }
}
