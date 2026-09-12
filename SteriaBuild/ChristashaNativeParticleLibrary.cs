using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Steria.Vfx
{
    /// <summary>Recipe hooks around the sole native Animator/particle clock.</summary>
    public interface IChristashaNativePlaybackHooks
    {
        float Duration { get; }
        void BeforeAnimator(float deltaTime, float elapsed);
        void AfterAnimator(float deltaTime, float elapsed);
        void AfterParticles(float deltaTime, float elapsed);
        void StopAndClear();
    }

    /// <summary>
    /// Runtime recipes derived from the installed game's native visual resources.
    /// Does not load reference bundles, ship game artwork, or execute game scripts.
    /// Call Dispose only after destroying all instances made by this library.
    /// </summary>
    public sealed class ChristashaNativeParticleLibrary : IDisposable
    {
        public const string SlashResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_slash_down";
        public const string HitPressureResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_greatsword";
        public const string HitCoreResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_glovetrail_cross";
        readonly Func<string, GameObject> resolver;
        readonly Texture2D originalBrush;
        readonly List<Object> owned = new List<Object>();
        GameObject cache;
        GameObject slash;
        GameObject hit;
        ChristashaNativeMassRecipes massRecipes;
        ChristashaNativePierceRecipes pierceRecipes;

        public ChristashaNativeParticleLibrary(Func<string, GameObject> resolver, Texture2D originalBrush = null)
        {
            if (resolver == null) throw new ArgumentNullException("resolver");
            this.resolver = resolver;
            this.originalBrush = originalBrush;
        }

        /// <summary>Inactive, raw-native-units recipe dispatch; caller owns the instance.</summary>
        public GameObject CreatePrototype(string recipe, Transform parent = null)
        {
            switch (recipe)
            {
                case "Slash": return CreateSlashPrototype(parent);
                case "Hit": return CreateHitPrototype(parent);
                case "Cast": case "Area": case "Point":
                    if (massRecipes == null) massRecipes = new ChristashaNativeMassRecipes(resolver, originalBrush);
                    return massRecipes.CreatePrototype(recipe, parent);
                case "Pierce": case "Projectile":
                    if (pierceRecipes == null) pierceRecipes = new ChristashaNativePierceRecipes(resolver, originalBrush);
                    return pierceRecipes.CreatePrototype(recipe, parent);
                default: throw new ArgumentException("Unknown Christasha native recipe: " + recipe, "recipe");
            }
        }

        /// <summary>Returns an inactive instance. The caller owns its placement and destruction.</summary>
        public GameObject CreateSlashPrototype(Transform parent = null)
        {
            if (slash == null) BuildSlash();
            var instance = Object.Instantiate(slash, parent, false);
            instance.name = "ChristashaNative_SlashPrototype";
            return instance;
        }

        /// <summary>Returns an inactive Hit instance in raw native units.</summary>
        public GameObject CreateHitPrototype(Transform parent = null)
        {
            if (hit == null) BuildHit();
            var instance = Object.Instantiate(hit, parent, false);
            instance.name = "ChristashaNative_HitPrototype";
            return instance;
        }

        void BuildHit()
        {
            var pressure = resolver(HitPressureResource);
            if (pressure == null) throw new InvalidOperationException("Missing installed native Greatsword source");
            var hitCache = new GameObject("ChristashaNative_HitInactiveCache");
            hitCache.SetActive(false);
            if (Application.isPlaying) Object.DontDestroyOnLoad(hitCache);
            owned.Add(hitCache);
            hit = CloneHitSlamSourceSubset(pressure, hitCache.transform);
            hit.name = "ChristashaNative_HitTemplate"; hit.SetActive(false);
            var body = FindSystem(hit, "Main_Trail1");
            var rim = FindSystem(hit, "Main_Trail2");
            var wave = FindSystem(hit, "Ground_Wave1");
            var upper = FindSystem(hit, "Upper_Particle");
            var start = FindSystem(hit, "Upper_Particle_Start");
            var smoke = FindSystem(hit, "Smoke");

            // The three native sword carriers, ancestor transforms, original clip,
            // .7/.5 lifetimes, 43.633/8.727 rad/s phases, and .15s SwingTrail remain.
            // Only the contact group is reduced: the sword descent is the main action.
            ConfigureHitBurst(wave, .15f, .25f, 1);
            var main = wave.main; main.startSize = 13f;
            ConfigureHitBurst(upper, .15f, .27f, 9);
            main = upper.main; main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f); main.gravityModifier = .35f; main.startSize = .30f;
            var shape = upper.shape; shape.radius = .9f; shape.angle = 40f;
            ConfigureHitBurst(start, .16f, .18f, 6);
            main = start.main; main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f); main.gravityModifier = .3f;
            shape = start.shape; shape.radius = .4f; shape.angle = 20f;
            ConfigureHitBurst(smoke, .17f, .34f, 3);
            main = smoke.main; main.startSize = new ParticleSystem.MinMaxCurve(4f, 7f); main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            shape = smoke.shape; shape.radius = .6f;

            // Keep every retained native renderer on an owned material. Playback
            // maps each native animated channel by its role, not a global gold tint.
            foreach (var p in hit.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = p.GetComponent<ParticleSystemRenderer>();
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null) materials[i] = OwnMaterial(materials[i], "Hit_" + p.name);
                renderer.sharedMaterials = materials;
            }
            if (originalBrush != null)
            {
                body.GetComponent<ParticleSystemRenderer>().sharedMaterial.SetTexture("_MainTex", PrepareBrushMask(originalBrush, false, .22f));
                rim.GetComponent<ParticleSystemRenderer>().sharedMaterial.SetTexture("_MainTex", PrepareHitRimMask(originalBrush));
            }
            Material spray = OwnMaterial(upper.GetComponent<ParticleSystemRenderer>().sharedMaterial, "HitGoldFragments");
            SetRgb(spray, "MainColor", new Color(1.3f, 1.2f, .9f));
            Material foil = HitBrushMaterial();
            var support = new GameObject("HitDetachedPressure"); support.transform.SetParent(hit.transform, false);
            MakeSupport(support.transform, "HitFoil", foil, HitBrushMesh(.24f), 32, 5);
            MakeSupport(support.transform, "HitThreads", foil, HitBrushMesh(.055f), 32, 4);
            MakeSupport(support.transform, "HitDust", spray, null, 64, 6);
            foreach (var p in support.GetComponentsInChildren<ParticleSystem>(true))
            {
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.82f, .77f, .62f), .5f), new GradientColorKey(new Color(.50f, .50f, .48f), 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .45f), new GradientAlphaKey(0, 1) });
                var color = p.colorOverLifetime; color.color = new ParticleSystem.MinMaxGradient(gradient);
            }
        }

        Material HitBrushMaterial()
        {
            if (originalBrush == null) throw new InvalidOperationException("Hit needs the original local tide-brush texture");
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Installed game lacks Sprites/Default");
            Color[] pixels = originalBrush.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1, 1, 1, Mathf.Clamp01(pixels[i].r * 1.6f));
            var mask = new Texture2D(originalBrush.width, originalBrush.height, TextureFormat.RGBA32, false, true)
            { name = "Christasha_HitBrush_Alpha", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            mask.SetPixels(pixels); mask.Apply(false, true); owned.Add(mask);
            var material = new Material(shader) { name = "Christasha_HitBrokenFoil" };
            material.SetTexture("_MainTex", mask); material.SetColor("_Color", new Color(1f, .96f, .83f, 1));
            owned.Add(material); return material;
        }

        Texture2D PrepareHitRimMask(Texture2D source)
        {
            var original = source.GetPixels();
            var pixels = new Color[original.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                float v = (i / source.width + .5f) / source.height;
                float outerRim = Mathf.Clamp01((.14f - v) / .035f);
                // U is native arc travel. Reposition the local knife aperture toward
                // the descending left side so its lower end reaches the native
                // (-6.87,-2.5) contact group; particle angles and clip stay untouched.
                int x = i % source.width + Mathf.RoundToInt(source.width * .24f);
                float brush = x < source.width ? original[(i / source.width) * source.width + x].r : 0f;
                float coverage = Mathf.Clamp01(brush * 1.6f) * outerRim;
                pixels[i] = new Color(coverage, coverage, coverage, 1);
            }
            var mask = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true)
            { name = "Christasha_HitNarrowBladeRim", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            mask.SetPixels(pixels); mask.Apply(false, true); owned.Add(mask); return mask;
        }

        Mesh HitBrushMesh(float width)
        {
            const int steps = 10;
            var mesh = new Mesh { name = "Christasha_HitLocalBrokenStroke" };
            var vertices = new Vector3[(steps + 1) * 2]; var uv = new Vector2[vertices.Length]; var indices = new int[steps * 6];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, bend = .055f * Mathf.Sin(t * Mathf.PI);
                vertices[i * 2] = new Vector3(t - .5f, bend - width, 0);
                vertices[i * 2 + 1] = new Vector3(t - .5f, bend + width, 0);
                // Authored brush is vertical: its fine upper tip points along +X.
                uv[i * 2] = new Vector2(0, t); uv[i * 2 + 1] = new Vector2(1, t);
                if (i == steps) continue;
                int j = i * 6, v = i * 2;
                indices[j] = v; indices[j + 1] = v + 1; indices[j + 2] = v + 2;
                indices[j + 3] = v + 1; indices[j + 4] = v + 3; indices[j + 5] = v + 2;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        static void ConfigureHitBurst(ParticleSystem system, float delay, float lifetime, short count)
        {
            if (system == null) throw new InvalidOperationException("Native Hit subset lacks a required module");
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.startDelay = delay; main.startLifetime = lifetime;
            main.loop = false; main.duration = .5f; main.playOnAwake = false;
            var emission = system.emission;
            emission.enabled = true; emission.rateOverTime = 0f; emission.rateOverDistance = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        /// <summary>Local preview/reference assembly; retains native ancestors for clip binding.</summary>
        public static GameObject CloneHitSlamSourceSubset(GameObject pressure, Transform inactiveParent)
        {
            if (inactiveParent == null || inactiveParent.gameObject.activeInHierarchy) throw new ArgumentException("Hit subset requires inactive parent");
            var root = new GameObject("NativeHit_GreatswordSlamSubset"); root.transform.SetParent(inactiveParent, false);
            var greatsword = CloneVisualOnly(pressure, root.transform);
            RetainHitSystems(greatsword, new HashSet<string> { "Main_Trail1", "Main_Trail2", "SwingTrail", "Ground_Wave1", "Upper_Particle", "Upper_Particle_Start", "Smoke" });
            return root;
        }

        static void RetainHitSystems(GameObject root, HashSet<string> names)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!(renderer is ParticleSystemRenderer) || !names.Contains(renderer.name)) Object.DestroyImmediate(renderer);
            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
                if (!names.Contains(system.name)) Object.DestroyImmediate(system);
            // Ancestor transforms are intentionally retained. SwingTrail is below
            // Main_Trail1 and Ground_Wave1 below Ground_Light1 in the original tree;
            // deleting those GameObjects would silently delete accepted modules too.
        }

        void BuildSlash()
        {
            GameObject source = resolver(SlashResource);
            if (source == null) throw new InvalidOperationException("Missing installed game VFX: " + SlashResource);
            cache = new GameObject("ChristashaNative_InactiveCache");
            cache.SetActive(false);
            if (Application.isPlaying) Object.DontDestroyOnLoad(cache);
            owned.Add(cache);
            slash = new GameObject("ChristashaNative_SlashTemplate");
            slash.transform.SetParent(cache.transform, false);
            slash.SetActive(false);

            // This extra art-space shapes a long diagonal sweep without normalizing the
            // original 0.3 scale / X flip / independent Trail transforms or their curves.
            var art = new GameObject("TideArtSpace");
            art.transform.SetParent(slash.transform, false);
            art.transform.localScale = new Vector3(1.22f, .76f, 1f);
            art.transform.localRotation = Quaternion.Euler(0f, 0f, -21f);
            GameObject native = CloneVisualOnly(source, art.transform);
            native.name = "NativeSlash";
            var main = FindSystem(native, "Main_Trail1");
            var contour = FindSystem(native, "Main_Trail2");
            var debris = FindSystem(native, "Particle1");
            if (main == null || contour == null || debris == null)
                throw new InvalidOperationException("Native Slash_Down hierarchy differs from the verified recipe.");

            Texture2D mask = originalBrush == null ? null : PrepareBrushMask(originalBrush);
            Texture2D platinumBodyMask = originalBrush == null ? null : PrepareBrushMask(originalBrush, false, .22f);

            Material blade = OwnMaterial(main.GetComponent<ParticleSystemRenderer>().sharedMaterial, "DawnPressureBody");
            if (platinumBodyMask != null) blade.SetTexture("_MainTex", platinumBodyMask);
            // These shaders intentionally use alpha=0 in their HDR color properties.
            // Alpha is not opacity here. Preserve it and preserve the black contour role.
            SetRgb(blade, "_MainColor", new Color(1.02f, .975f, .795f));
            SetRgb(blade, "_OutColor", new Color(1.62f, 1.50f, 1.245f) / .22f);
            main.GetComponent<ParticleSystemRenderer>().sharedMaterial = blade;

            Material dark = OwnMaterial(contour.GetComponent<ParticleSystemRenderer>().sharedMaterial, "DawnCuttingRim");
            if (mask != null) dark.SetTexture("_MainTex", mask);
            // Material defaults are black, but the original Animator actually writes
            // HDR (3,3,3,0) here: this second Trail is the early bright cutting rim.
            SetRgb(dark, "_MainColor", new Color(3.15f, 2.79f, 2.16f));
            contour.GetComponent<ParticleSystemRenderer>().sharedMaterial = dark;
            // The native HDR cutting rim must start with the attack, not one frame
            // late. Its independent 26 rad/s curve remains distinct from Trail1.
            var contourMain = contour.main;
            contourMain.startDelay = 0f;
            contourMain.startSizeXMultiplier *= .91f;
            contour.transform.localRotation *= Quaternion.Euler(0f, 0f, -11f);

            Material burn = OwnMaterial(debris.GetComponent<ParticleSystemRenderer>().sharedMaterial, "WarmDetachedSparks");
            SetRgb(burn, "MainColor", new Color(1.75f, 1.10f, .40f));
            debris.GetComponent<ParticleSystemRenderer>().sharedMaterial = burn;
            var debrisMain = debris.main;
            debrisMain.startColor = new ParticleSystem.MinMaxGradient(new Color(.42f, .17f, .035f, 1f), new Color(1f, .83f, .42f, 1f));

            // A separate, offset return stroke. The native torus is the carrier, but
            // this is a thin oblique chord with its own delay/life/phase, not a shell.
            var returnObject = Object.Instantiate(main.gameObject, main.transform.parent, false);
            returnObject.name = "GoldReturn";
            for (int i = returnObject.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(returnObject.transform.GetChild(i).gameObject);
            returnObject.transform.localPosition += new Vector3(-2f, 1.2f, .6f);
            returnObject.transform.localRotation *= Quaternion.Euler(0, 0, 38);
            var returning = returnObject.GetComponent<ParticleSystem>();
            var returningMain = returning.main;
            returningMain.startSize3D = true;
            returningMain.startSizeX = 18f; returningMain.startSizeY = 7.4f; returningMain.startSizeZ = 18f;
            returningMain.startLifetime = .33f; returningMain.startDelay = .040f;
            var rotation = returning.rotationOverLifetime; rotation.zMultiplier *= .72f;
            Material returningMat = OwnMaterial(blade, "GoldReturn");
            if (originalBrush != null) returningMat.SetTexture("_MainTex", PrepareBrushMask(originalBrush, true));
            SetRgb(returningMat, "_MainColor", new Color(1.45f, 1.00f, .40f));
            SetRgb(returningMat, "_OutColor", new Color(1.65f, 1.12f, .46f));
            returningMat.SetFloat("_MaskPower", 4.5f);
            var returningRenderer = returnObject.GetComponent<ParticleSystemRenderer>();
            returningRenderer.sharedMaterials = new[] { returningMat };
            returningRenderer.SetPropertyBlock(null);
            returningRenderer.sortingOrder = 2;

            // Independent particles emitted along the observed moving native front.
            // Their meshes are original small fragments, never a full slash-card image.
            var support = new GameObject("DetachedTide");
            support.transform.SetParent(slash.transform, false);
            MakeSupport(support.transform, "TideSplinters", burn, FragmentMesh(false), 48, 5);
            MakeSupport(support.transform, "TideFilaments", burn, FragmentMesh(true), 48, 4);
            MakeSupport(support.transform, "TideDust", burn, null, 96, 6);
        }

        Material OwnMaterial(Material source, string role)
        {
            if (source == null || source.shader == null) throw new InvalidOperationException("Native material is unavailable: " + role);
            var material = new Material(source) { name = "ChristashaNative_" + role };
            owned.Add(material);
            return material;
        }

        Texture2D PrepareBrushMask(Texture2D source, bool returning = false, float coverageStrength = 1f)
        {
            // The native shader's white edge expects a full-white mask plateau.
            // Generated brush luminance is mostly sub-white photographic brush grain;
            // calibrate scalar coverage, not the artwork silhouette or color palette.
            Color[] pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float coverage = Mathf.Clamp01(pixels[i].r * 1.6f);
                if (returning)
                {
                    // UV.x follows the native torus path; UV.y crosses its depth.
                    // Two unequal tapering strands exclude the former rounded head.
                    // The original local brush still supplies all fine breakup.
                    float u = (i % source.width + .5f) / source.width;
                    float v = (i / source.width + .5f) / source.height;
                    float span1 = Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - .16f) / .71f));
                    float span2 = Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - .29f) / .48f));
                    float center1 = .075f + .04f * Mathf.Sin(u * Mathf.PI);
                    float center2 = .19f + .045f * Mathf.Sin(u * Mathf.PI);
                    float strand1 = Mathf.Clamp01((.045f * span1 - Mathf.Abs(v - center1)) / .010f);
                    float strand2 = Mathf.Clamp01((.027f * span2 - Mathf.Abs(v - center2)) / .009f);
                    coverage *= Mathf.Max(strand1, strand2);
                }
                coverage *= coverageStrength;
                pixels[i] = new Color(coverage, coverage, coverage, 1f);
            }
            var mask = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true)
            {
                name = returning ? "Christasha_TideBrush_SharpReturn" : "Christasha_TideBrush_NativeCoverage", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            mask.SetPixels(pixels); mask.Apply(false, true);
            owned.Add(mask); return mask;
        }

        static void SetRgb(Material material, string property, Color rgb)
        {
            if (!material.HasProperty(property)) throw new InvalidOperationException(material.name + " lacks " + property);
            Color original = material.GetColor(property);
            material.SetColor(property, new Color(rgb.r, rgb.g, rgb.b, original.a));
        }

        Mesh FragmentMesh(bool filament)
        {
            var mesh = new Mesh { name = filament ? "Christasha_TideThread" : "Christasha_BrokenFoil" };
            if (!filament)
            {
                mesh.vertices = new[] { new Vector3(-.53f, 0f), new Vector3(-.1f, .19f), new Vector3(.13f, .23f), new Vector3(.55f, 0f), new Vector3(.10f, -.13f), new Vector3(-.2f, -.09f) };
                mesh.uv = new[] { new Vector2(0f, .5f), new Vector2(.4f, 1f), new Vector2(.6f, 1f), new Vector2(1f, .5f), new Vector2(.6f, 0f), new Vector2(.3f, 0f) };
                mesh.triangles = new[] { 0, 1, 5, 1, 4, 5, 1, 2, 4, 2, 3, 4 };
            }
            else
            {
                const int sections = 14;
                var vertices = new Vector3[(sections + 1) * 2];
                var uv = new Vector2[vertices.Length];
                var indices = new int[sections * 6];
                for (int i = 0; i <= sections; i++)
                {
                    float t = i / (float)sections;
                    float y = .10f * Mathf.Sin(t * Mathf.PI) - .03f * t;
                    float w = .025f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.6f);
                    vertices[i * 2] = new Vector3(t - .5f, y - w, 0);
                    vertices[i * 2 + 1] = new Vector3(t - .5f, y + w, 0);
                    uv[i * 2] = new Vector2(t, 0); uv[i * 2 + 1] = new Vector2(t, 1);
                    if (i == sections) continue;
                    int j = i * 6, v = i * 2;
                    indices[j] = v; indices[j + 1] = v + 1; indices[j + 2] = v + 2;
                    indices[j + 3] = v + 1; indices[j + 4] = v + 3; indices[j + 5] = v + 2;
                }
                mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
            }
            mesh.RecalculateBounds();
            owned.Add(mesh);
            return mesh;
        }

        static void MakeSupport(Transform parent, string name, Material material, Mesh mesh, int capacity, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.playOnAwake = false; main.loop = false; main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f; main.startLifetime = .4f; main.startSize = 1f;
            main.maxParticles = capacity; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.None;
            var emission = p.emission; emission.enabled = false;
            var shape = p.shape; shape.enabled = false;
            var size = p.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, .75f), new Keyframe(.12f, 1f), new Keyframe(.65f, .65f), new Keyframe(1f, 0f)));
            var color = p.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.68f, .34f, .11f), .48f), new GradientColorKey(new Color(.14f, .055f, .016f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .45f), new GradientAlphaKey(0, 1) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mesh == null ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Mesh;
            if (mesh != null) renderer.mesh = mesh;
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
        }

        /// <summary>Clones under an inactive parent so game Awake/OnEnable cannot execute.</summary>
        public static GameObject CloneVisualOnly(GameObject source, Transform inactiveParent)
        {
            if (inactiveParent == null || inactiveParent.gameObject.activeInHierarchy)
                throw new ArgumentException("Visual cloning requires an inactive staging parent.");
            var go = Object.Instantiate(source, inactiveParent, false);
            foreach (MonoBehaviour c in go.GetComponentsInChildren<MonoBehaviour>(true)) if (c != null) Object.DestroyImmediate(c);
            foreach (AudioSource c in go.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(c);
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) Object.DestroyImmediate(c);
            foreach (Rigidbody c in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(c);
            foreach (Rigidbody2D c in go.GetComponentsInChildren<Rigidbody2D>(true)) Object.DestroyImmediate(c);
            foreach (Camera c in go.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c);
            foreach (Light c in go.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(c);
            foreach (Animator animator in go.GetComponentsInChildren<Animator>(true))
            {
                animator.fireEvents = false; animator.speed = 0f;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            foreach (Animation animation in go.GetComponentsInChildren<Animation>(true))
            {
                // The initial native recipe uses Animator. Fail closed for legacy clips
                // rather than permit animation events to drive gameplay unexpectedly.
                animation.enabled = false;
            }
            foreach (ParticleSystem particle in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particle.main;
                main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None;
            }
            return go;
        }

        internal static ParticleSystem FindSystem(GameObject root, string name)
        {
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true)) if (p.name == name) return p;
            return null;
        }

        public void Dispose()
        {
            if (massRecipes != null) { massRecipes.Dispose(); massRecipes = null; }
            if (pierceRecipes != null) { pierceRecipes.Dispose(); pierceRecipes = null; }
            // Templates first, then their owned assets. Installed Resources remain untouched.
            if (cache != null) Object.DestroyImmediate(cache);
            foreach (Object value in owned) if (value != null) Object.DestroyImmediate(value);
            owned.Clear(); slash = null; hit = null; cache = null;
        }
    }

    /// <summary>Pure manual clock; no MonoBehaviour, battle side effects, or automatic Update.</summary>
    public sealed class ChristashaNativeParticlePlayback
    {
        readonly GameObject root;
        readonly ParticleSystem[] systems;
        readonly Animator[] animators;
        readonly HashSet<ParticleSystem> subEmitters = new HashSet<ParticleSystem>();
        readonly ParticleSystem mainTrail, splinters, filaments, dust;
        readonly ParticleSystemRenderer bladeRenderer, contourRenderer;
        readonly MaterialPropertyBlock colorBlock = new MaterialPropertyBlock();
        readonly HitPlayback hitPlayback;
        readonly IChristashaNativePlaybackHooks recipeHooks;
        Color nativeMainColor, nativeEdgeColor, nativeContourColor;
        bool colorsSampled;
        readonly ParticleSystem.Particle[] front = new ParticleSystem.Particle[4];
        readonly float speed;
        System.Random random;
        float elapsed;
        int nextEmission;
        bool stopped;
        const float Length = 1.2f;
        const float StepSize = 1f / 120f;
        public float Duration { get { return NativeDuration / speed; } }
        float NativeDuration { get { return recipeHooks != null ? recipeHooks.Duration : hitPlayback == null ? Length : 1f; } }
        public float Elapsed { get { return elapsed / speed; } }

        public static ChristashaNativeParticlePlayback Play(GameObject instance, uint seed, float playbackSpeed = 1f)
        {
            return new ChristashaNativeParticlePlayback(instance, seed, playbackSpeed);
        }

        ChristashaNativeParticlePlayback(GameObject instance, uint seed, float playbackSpeed)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (playbackSpeed <= 0 || float.IsNaN(playbackSpeed) || float.IsInfinity(playbackSpeed)) throw new ArgumentOutOfRangeException("playbackSpeed");
            root = instance; speed = playbackSpeed; random = new System.Random(unchecked((int)seed));
            systems = root.GetComponentsInChildren<ParticleSystem>(true);
            animators = root.GetComponentsInChildren<Animator>(true);
            bool slashRecipe = root.transform.Find("TideArtSpace/NativeSlash") != null && root.transform.Find("DetachedTide/TideSplinters") != null;
            mainTrail = slashRecipe ? ChristashaNativeParticleLibrary.FindSystem(root, "Main_Trail1") : null;
            splinters = slashRecipe ? ChristashaNativeParticleLibrary.FindSystem(root, "TideSplinters") : null;
            filaments = slashRecipe ? ChristashaNativeParticleLibrary.FindSystem(root, "TideFilaments") : null;
            dust = slashRecipe ? ChristashaNativeParticleLibrary.FindSystem(root, "TideDust") : null;
            if (root.transform.Find("HitDetachedPressure/HitFoil") != null) hitPlayback = new HitPlayback(root);
            recipeHooks = ChristashaNativeMassRecipes.TryCreatePlayback(root, seed) ?? ChristashaNativePierceRecipes.TryCreatePlayback(root, seed);
            if (splinters != null)
            {
                bladeRenderer = mainTrail.GetComponent<ParticleSystemRenderer>();
                contourRenderer = ChristashaNativeParticleLibrary.FindSystem(root, "Main_Trail2").GetComponent<ParticleSystemRenderer>();
            }
            for (int index = 0; index < systems.Length; index++)
            {
                var p = systems[index];
                p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = p.main; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None;
                p.useAutoRandomSeed = false; p.randomSeed = seed + (uint)(997 * index);
                var children = p.subEmitters;
                for (int i = 0; i < children.subEmittersCount; i++)
                {
                    var child = children.GetSubEmitterSystem(i); if (child != null) subEmitters.Add(child);
                }
            }
            root.SetActive(true);
            foreach (var a in animators)
            {
                a.fireEvents = false; a.applyRootMotion = false;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate; a.speed = 0;
                if (a.enabled && a.gameObject.activeInHierarchy) { a.Rebind(); a.Update(0); }
            }
            foreach (var p in systems)
            {
                if (p.gameObject.activeInHierarchy && !subEmitters.Contains(p)) p.Simulate(0, false, true, false);
                p.Pause(false);
            }
            Step(0);
        }

        public void Advance(float deltaTime)
        {
            if (stopped || root == null) return;
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException("deltaTime");
            float length = NativeDuration;
            float target = Mathf.Min(length, elapsed + deltaTime * speed);
            while (target - elapsed > .000001f)
            {
                float dt = Mathf.Min(StepSize, target - elapsed);
                elapsed += dt; Step(dt);
            }
            if (elapsed >= length - .000001f) StopAndClear();
        }

        void Step(float dt)
        {
            if (recipeHooks != null) recipeHooks.BeforeAnimator(dt, elapsed);
            if (hitPlayback != null) hitPlayback.BeforeAnimator();
            if (bladeRenderer != null && colorsSampled)
            {
                // Constant clip bindings may be written once by Animator rather than
                // every Update. Restore the sampled native channels before evaluating
                // again so a theme multiplier cannot compound between ticks.
                bladeRenderer.GetPropertyBlock(colorBlock);
                colorBlock.SetColor("_MainColor", nativeMainColor);
                colorBlock.SetColor("_OutColor", nativeEdgeColor);
                bladeRenderer.SetPropertyBlock(colorBlock); colorBlock.Clear();
                contourRenderer.GetPropertyBlock(colorBlock);
                colorBlock.SetColor("_MainColor", nativeContourColor);
                contourRenderer.SetPropertyBlock(colorBlock); colorBlock.Clear();
            }
            foreach (var a in animators)
                if (a.enabled && a.gameObject.activeInHierarchy) { a.speed = 1; a.Update(dt); a.speed = 0; }
            if (recipeHooks != null) recipeHooks.AfterAnimator(dt, elapsed);
            if (hitPlayback != null) hitPlayback.AfterAnimator(elapsed);
            if (bladeRenderer != null)
            {
                // Native Animator also animates material channels. Preserve its mask
                // properties and apply only role color *after* sampling the native clip.
                bladeRenderer.GetPropertyBlock(colorBlock);
                // User-directed platinum revision: native body coverage is attenuated
                // in MainTex.R, then filled with near-unit warm white. Its narrow Out
                // term compensates only that alpha attenuation, preserving the sharp
                // original etched edge without a high-HDR broad surface.
                nativeMainColor = colorBlock.GetColor("_MainColor");
                nativeEdgeColor = colorBlock.GetColor("_OutColor"); colorsSampled = true;
                colorBlock.SetColor("_MainColor", new Color(nativeEdgeColor.r * .68f, nativeEdgeColor.g * .65f, nativeEdgeColor.b * .53f, nativeMainColor.a));
                colorBlock.SetColor("_OutColor", new Color(nativeEdgeColor.r * 1.08f / .22f, nativeEdgeColor.g / .22f, nativeEdgeColor.b * .83f / .22f, nativeEdgeColor.a));
                bladeRenderer.SetPropertyBlock(colorBlock);
                colorBlock.Clear();
                contourRenderer.GetPropertyBlock(colorBlock);
                nativeContourColor = colorBlock.GetColor("_MainColor");
                colorBlock.SetColor("_MainColor", new Color(nativeContourColor.r * 1.05f, nativeContourColor.g * .93f, nativeContourColor.b * .72f, nativeContourColor.a));
                contourRenderer.SetPropertyBlock(colorBlock);
                colorBlock.Clear();
            }
            foreach (var p in systems)
            {
                if (p.gameObject.activeInHierarchy && !subEmitters.Contains(p)) p.Simulate(dt, false, false, false);
                p.Pause(false);
            }
            if (splinters != null && mainTrail != null)
                while (nextEmission < 15 && elapsed + .000001f >= .024f + nextEmission * .010f)
                {
                    EmitTide(nextEmission); nextEmission++;
                }
            if (hitPlayback != null)
                while (nextEmission < 2 && elapsed + .000001f >= .150f + nextEmission * .035f)
                {
                    EmitHit(nextEmission); nextEmission++;
                }
            if (recipeHooks != null) recipeHooks.AfterParticles(dt, elapsed);
            // Zero-time sync initializes newborn render data in Unity 2019 without
            // consuming a second simulation tick or driving a sub-emitter twice.
            foreach (var p in systems)
            {
                if (p.gameObject.activeInHierarchy && !subEmitters.Contains(p)) p.Simulate(0, false, false, false);
                p.Pause(false);
            }
        }

        float Range(float a, float b) { return Mathf.Lerp(a, b, (float)random.NextDouble()); }

        void EmitHit(int pulse)
        {
            Vector3 impact = hitPlayback.ContactPosition;
            for (int i = 0; i < (pulse == 0 ? 6 : 3); i++)
            {
                float angle = (i % 2 == 0 ? Range(8f, 32f) : Range(148f, 173f)) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Vector3 at = impact + direction * Range(.05f, .24f) + new Vector3(0, Range(-.05f, .12f), -.1f);
                Emit(hitPlayback.Foil, at, direction * Range(6f, 12f), Range(.45f, 1.1f), angle * Mathf.Rad2Deg + Range(-10, 10), Range(.15f, .25f), new Color(1f, Range(.91f, 1f), .78f, 1));
            }
            for (int i = 0; i < 5; i++)
            {
                float angle = (i % 2 == 0 ? Range(4f, 24f) : Range(156f, 176f)) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Emit(hitPlayback.Threads, impact + direction * Range(.1f, .3f) + new Vector3(0, 0, -.12f), direction * Range(8f, 14f), Range(.8f, 1.8f), angle * Mathf.Rad2Deg, Range(.13f, .23f), new Color(1f, .91f, .70f, 1));
                Emit(hitPlayback.Dust, impact + direction * Range(.1f, .5f), direction * Range(5f, 11f), Range(.04f, .09f), 0, Range(.18f, .30f), new Color(1f, .90f, .65f, 1));
            }
        }

        sealed class HitPlayback
        {
            readonly GameObject root;
            readonly ParticleSystem[] native;
            readonly ParticleSystem wave, upper, start;
            readonly List<HitColor> colors = new List<HitColor>();
            public readonly ParticleSystem Foil, Threads, Dust;
            public Vector3 ContactPosition { get { return root.transform.InverseTransformPoint(wave.transform.position); } }

            public HitPlayback(GameObject root)
            {
                this.root = root;
                Foil = ChristashaNativeParticleLibrary.FindSystem(root, "HitFoil");
                Threads = ChristashaNativeParticleLibrary.FindSystem(root, "HitThreads");
                Dust = ChristashaNativeParticleLibrary.FindSystem(root, "HitDust");
                wave = ChristashaNativeParticleLibrary.FindSystem(root, "Ground_Wave1");
                upper = ChristashaNativeParticleLibrary.FindSystem(root, "Upper_Particle");
                start = ChristashaNativeParticleLibrary.FindSystem(root, "Upper_Particle_Start");
                native = new[] {
                    ChristashaNativeParticleLibrary.FindSystem(root, "Main_Trail1"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "Main_Trail2"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "SwingTrail"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "Ground_Wave1"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "Upper_Particle"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "Upper_Particle_Start"),
                    ChristashaNativeParticleLibrary.FindSystem(root, "Smoke") };
                foreach (var p in native)
                {
                    Color tint;
                    switch (p.name)
                    {
                        case "Main_Trail2": tint = new Color(1.05f, .93f, .72f); break;
                        case "Ground_Wave1": tint = new Color(.50f, .48f, .40f); break;
                        case "Upper_Particle": tint = new Color(1.05f, .98f, .78f); break;
                        case "Upper_Particle_Start": tint = new Color(.74f, .68f, .50f); break;
                        case "Smoke": tint = new Color(.36f, .38f, .40f); break;
                        default: tint = new Color(1.08f, 1f, .84f); break;
                    }
                    colors.Add(new HitColor(p.GetComponent<ParticleSystemRenderer>(), tint));
                }
            }

            public void BeforeAnimator() { foreach (var color in colors) color.Restore(); }
            public void StopAndClear() { foreach (var color in colors) color.Restore(); }

            public void AfterAnimator(float age)
            {
                foreach (var color in colors) color.Map(age);
                // Preserve the sword trio's original matrices/clip. Only the distant
                // child starter spray is brought to the native lower contact group.
                start.transform.position = upper.transform.position;
                start.transform.rotation = root.transform.rotation * Quaternion.LookRotation(new Vector3(-1f, .24f, 0), Vector3.forward);
            }
        }

        sealed class HitColor
        {
            readonly ParticleSystemRenderer renderer;
            readonly MaterialPropertyBlock native = new MaterialPropertyBlock();
            readonly MaterialPropertyBlock mapped = new MaterialPropertyBlock();
            readonly string property;
            readonly Color materialColor, materialOutColor, tint;
            bool sampled;
            public HitColor(ParticleSystemRenderer renderer, Color tint)
            {
                this.renderer = renderer; this.tint = tint;
                var material = renderer.sharedMaterial;
                property = material.HasProperty("MainColor") ? "MainColor" : "_MainColor";
                if (!material.HasProperty(property)) throw new InvalidOperationException("Hit source lacks declared role color " + renderer.name);
                materialColor = material.GetColor(property);
                materialOutColor = material.HasProperty("_OutColor") ? material.GetColor("_OutColor") : Color.clear;
            }
            public void Restore() { if (sampled) renderer.SetPropertyBlock(native); }
            public void Map(float age)
            {
                renderer.GetPropertyBlock(native);
                renderer.GetPropertyBlock(mapped);
                Color original = native.isEmpty ? materialColor : native.GetColor(property);
                if (renderer.name == "Main_Trail1")
                {
                    Color edge = native.isEmpty ? materialOutColor : native.GetColor("_OutColor");
                    mapped.SetColor(property, new Color(edge.r * .68f, edge.g * .65f, edge.b * .53f, original.a));
                    mapped.SetColor("_OutColor", new Color(edge.r * 1.08f / .22f, edge.g / .22f, edge.b * .83f / .22f, edge.a));
                }
                else if (renderer.name == "Main_Trail2")
                {
                    // Greatsword's initial zero is black light, not invisibility.
                    // Keep its native opacity/angle but make the narrow knife edge
                    // platinum from the first frame, with the native later rise.
                    float level = 1.4f + .3f * Mathf.Max(original.r, Mathf.Max(original.g, original.b));
                    mapped.SetColor(property, new Color(level * 1.05f, level * .98f, level * .85f, original.a));
                    // This Outline shader uses pow(1-v, _Mask) for cross-blade
                    // coverage. Tighten that native exponent after contact to leave
                    // a thin bright trace, rather than darkening opaque RGB.
                    float nativeMask = Mathf.Max(10f, native.GetFloat("_Mask"));
                    float retreat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.19f, .47f, age));
                    mapped.SetFloat("_Mask", Mathf.Lerp(nativeMask, Mathf.Max(nativeMask, 80f), retreat));
                }
                else if (renderer.name == "SwingTrail")
                {
                    // The source uses a black pressure flash. The chosen character
                    // theme instead uses a subdued platinum pressure pulse; its raw
                    // procedural/particle opacity envelope is retained.
                    mapped.SetColor(property, new Color(1.2f, 1.16f, 1.05f, original.a));
                    // Native Flash1's U threshold confines the .15s descending
                    // follow-through instead of drawing a full concentric circle.
                    mapped.SetFloat("_MaskPower", Mathf.Max(7.2f, native.GetFloat("_MaskPower")));
                }
                else mapped.SetColor(property, new Color(original.r * tint.r, original.g * tint.g, original.b * tint.b, original.a));
                if (renderer.name == "Smoke") mapped.SetFloat("_Alp", native.GetFloat("_Alp") * .35f);
                renderer.SetPropertyBlock(mapped); sampled = true;
            }
        }

        void EmitTide(int index)
        {
            if (mainTrail.GetParticles(front) == 0) return;
            ParticleSystem.Particle particle = front[0];
            Vector3 size = particle.GetCurrentSize3D(mainTrail);
            // Sample a point on the inherited torus near its cutting end. Using the
            // actual native particle rotation keeps the scattered flow tied to its sweep.
            Quaternion spin = Quaternion.Euler(particle.rotation3D);
            Vector3 rim = spin * Vector3.Scale(new Vector3(1.28f, .32f, 0), size);
            Vector3 world = mainTrail.transform.TransformPoint(particle.position + rim);
            Vector3 local = root.transform.InverseTransformPoint(world);
            Vector3 center = root.transform.InverseTransformPoint(mainTrail.transform.position);
            Vector3 radial = (local - center).normalized;
            Vector3 tangent = new Vector3(-radial.y, radial.x, 0);
            float direction = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            local.z = -.06f;
            if (index % 2 == 0)
                Emit(splinters, local + radial * Range(-.30f, .15f), tangent * Range(3f, 6f) + radial * Range(.8f, 2.1f), Range(.85f, 1.65f), direction + Range(-24, 24), Range(.18f, .36f), new Color(1f, Range(.70f, .95f), .48f, 1));
            Emit(filaments, local + radial * Range(-.6f, .3f), tangent * Range(2f, 5f) + radial * Range(.2f, 1f), Range(1.2f, 2.8f), direction + Range(-10, 10), Range(.23f, .46f), new Color(1f, .62f, .20f, 1));
            for (int i = 0; i < 3; i++)
                Emit(dust, local + radial * Range(-.9f, .6f) + tangent * Range(-.6f, .6f), tangent * Range(1.5f, 7f) + radial * Range(.4f, 2.5f), Range(.04f, .12f), 0, Range(.21f, .51f), new Color(1f, Range(.64f, .94f), .38f, 1));
        }

        static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float rotation, float lifetime, Color color)
        {
            var particle = new ParticleSystem.EmitParams
            {
                position = position, velocity = velocity, startSize = size,
                rotation = rotation, startLifetime = lifetime, startColor = color,
                applyShapeToPosition = false
            };
            system.Emit(particle, 1);
        }

        public void StopAndClear()
        {
            stopped = true;
            if (recipeHooks != null) recipeHooks.StopAndClear();
            if (hitPlayback != null) hitPlayback.StopAndClear();
            foreach (var p in systems) if (p != null) p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var a in animators) if (a != null) a.speed = 0;
        }
    }
}
