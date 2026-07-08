using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SivierWishGuardBundleBuilder
{
    private const string BundleName = "steria_sivier_wish_guard";
    private const string PrefabName = "SivierWishGuardPrefab";

    private static readonly Color ColorVioletDeep = new Color(0.46f, 0.14f, 0.86f, 0.92f);
    private static readonly Color ColorBluePlasma = new Color(0.20f, 0.48f, 1f, 0.92f);
    private static readonly Color ColorWhiteCore = new Color(0.90f, 0.95f, 1f, 0.98f);
    private static readonly Color ColorDeepShadow = new Color(0.03f, 0.02f, 0.06f, 0.55f);

    [MenuItem("Steria/Build Sivier Wish Guard Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        CopyBundleToMod(output);
        VerifyBuiltBundle(output);
        Debug.Log("Built Sivier wish guard AssetBundle: " + output);
    }

    public static void EnsurePrefab()
    {
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Textures");

        GenerateTextures();

        Mesh quadMesh = CreateOrReplaceMesh("Assets/Meshes/SivierGuard_QuadMesh.asset", CreateQuadMesh("SivierGuard_QuadMesh", 1f));
        Mesh sparkMesh = CreateOrReplaceMesh("Assets/Meshes/SivierGuard_DiamondMesh.asset", CreateDiamondMesh("SivierGuard_DiamondMesh", 0.62f, 0.30f));

        Material ringViolet = CreateMaterial("Assets/Textures/sivier_guard_ring.png", "Assets/Materials/SivierGuard_RingViolet_Add.mat", ColorVioletDeep, false);
        Material ringBlue = CreateMaterial("Assets/Textures/sivier_guard_ring.png", "Assets/Materials/SivierGuard_RingBlue_Add.mat", ColorBluePlasma, false);
        Material glyphWhite = CreateMaterial("Assets/Textures/sivier_guard_glyph.png", "Assets/Materials/SivierGuard_GlyphWhite_Add.mat", ColorWhiteCore, false);
        Material glow = CreateMaterial("Assets/Textures/sivier_guard_glow.png", "Assets/Materials/SivierGuard_Glow_Add.mat", ColorBluePlasma, false);
        Material dots = CreateMaterial("Assets/Textures/sivier_guard_dots.png", "Assets/Materials/SivierGuard_Dots_Add.mat", ColorVioletDeep, false);
        Material spark = CreateMaterial("Assets/Textures/sivier_guard_spark.png", "Assets/Materials/SivierGuard_Spark_Add.mat", ColorWhiteCore, false);
        Material shockwave = CreateMaterial("Assets/Textures/sivier_guard_ring.png", "Assets/Materials/SivierGuard_Shockwave_Add.mat", ColorBluePlasma, false);
        Material shadow = CreateMaterial("Assets/Textures/sivier_guard_shadow.png", "Assets/Materials/SivierGuard_Shadow_Alpha.mat", ColorDeepShadow, true);

        CreateGuardPrefab(quadMesh, sparkMesh, ringViolet, ringBlue, glyphWhite, glow, dots, spark, shockwave, shadow);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateGuardPrefab(Mesh quadMesh, Mesh sparkMesh, Material ringViolet, Material ringBlue, Material glyphWhite, Material glow, Material dots, Material spark, Material shockwave, Material shadow)
    {
        GameObject root = CreateRoot(PrefabName);
        Transform circleRoot = CreateGroup(root.transform, "AB_CircleRoot", new Vector3(0f, 0.15f, -0.05f));

        CreateFlatLayer(circleRoot, "AB_Guard_GroundShadow", shadow, quadMesh, Vector3.zero, Quaternion.identity, 0.00f, 0.90f, 1.75f, 0f, ColorDeepShadow, 70, false);
        CreateFlatLayer(circleRoot, "AB_Guard_ChargeGlow", glow, quadMesh, Vector3.zero, Quaternion.identity, 0.00f, 0.92f, 1.95f, 0f, new Color(0.20f, 0.48f, 1f, 0.42f), 96, true);
        CreateMagicRingLayer(circleRoot, "AB_Guard_OuterRingViolet", ringViolet, quadMesh, new Vector3(0f, 0f, 0.01f), 0.00f, 0.86f, 1.55f, 2.55f, ColorVioletDeep, 100);
        CreateMagicRingLayer(circleRoot, "AB_Guard_MidRingBlue", ringBlue, quadMesh, new Vector3(0f, 0f, 0.00f), 0.05f, 0.80f, 1.15f, -3.35f, ColorBluePlasma, 104);
        CreateMagicRingLayer(circleRoot, "AB_Guard_InnerGlyphWhite", glyphWhite, quadMesh, new Vector3(0f, 0f, -0.01f), 0.10f, 0.74f, 0.62f, 1.65f, ColorWhiteCore, 108);
        CreateOrbitDots(circleRoot, "AB_Guard_RuneDots", dots, new Vector3(0f, 0f, -0.02f), 0.03f, 16, 0.06f, 0.16f, 0.55f, 1.35f, 128);

        CreateImpactFlash(circleRoot, "AB_Guard_SlamFlashWhite", glyphWhite, new Vector3(0f, 0f, -0.05f), 0.28f, 0.30f, 2.05f, ColorWhiteCore, 150);
        CreateImpactFlash(circleRoot, "AB_Guard_SlamShockwaveViolet", shockwave, new Vector3(0f, 0f, -0.04f), 0.30f, 0.36f, 2.65f, new Color(0.34f, 0.30f, 1f, 0.72f), 140);
        CreateRadialSparkBurst(circleRoot, "AB_Guard_SlamSparkBurst", spark, sparkMesh, new Vector3(0f, 0f, -0.03f), 0.30f, 14, 148);
        CreateFlatLayer(circleRoot, "AB_Guard_SlamShadowPulse", shadow, quadMesh, new Vector3(0f, -0.02f, 0.02f), Quaternion.identity, 0.28f, 0.30f, 1.20f, 0f, ColorDeepShadow, 86, false);

        SavePrefab(root, "Assets/Prefabs/SivierWishGuardPrefab.prefab");
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.layer = 8;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        ParticleSystem rootParticle = root.AddComponent<ParticleSystem>();
        var main = rootParticle.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        var emission = rootParticle.emission;
        emission.enabled = false;
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.enabled = false;
        return root;
    }

    private static Transform CreateGroup(Transform parent, string name, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        Debug.Log("Prepared Sivier wish guard prefab: " + path);
    }

    private static GameObject NewParticle(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one;
        go.AddComponent<ParticleSystem>();
        return go;
    }

    private static void CreateFlatLayer(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, Quaternion rot, float delay, float lifetime, float size, float zRot, Color color, int sortingOrder, bool pulse)
    {
        GameObject go = NewParticle(parent, name, pos, rot * Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.12f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.14f, 0.55f);

        if (pulse)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.35f, 1.08f), new Keyframe(0.70f, 0.85f), new Keyframe(1f, 1.0f)));
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateMagicRingLayer(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float delay, float lifetime, float size, float rotationSpeedRadians, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.12f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.12f, 0.52f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.10f), new Keyframe(0.22f, 1.08f), new Keyframe(1f, 0.92f)));

        var rotOver = ps.rotationOverLifetime;
        rotOver.enabled = true;
        rotOver.z = new ParticleSystem.MinMaxCurve(rotationSpeedRadians);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateOrbitDots(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 1.0f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.80f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.46f, 0.14f, 0.86f, 0.85f), new Color(0.60f, 0.70f, 1f, 0.90f));
        main.maxParticles = burst + 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.95f;
        shape.arc = 360f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.9f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateGuardFadeGradient(0.82f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateImpactFlash(Transform parent, string name, Material mat, Vector3 pos, float delay, float lifetime, float size, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.12f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.30f, 0.30f);
        main.startColor = color;
        main.maxParticles = 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, color.a, 0.08f, 0.42f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.24f), new Keyframe(0.14f, 1f), new Keyframe(1f, 1.38f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateRadialSparkBurst(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float delay, short burst, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.5f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.30f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ColorVioletDeep, ColorWhiteCore);
        main.maxParticles = burst + 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateGuardFadeGradient(0.92f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.12f;
        r.lengthScale = 2.0f;
        r.sortingOrder = sortingOrder;
    }

    private static void GenerateTextures()
    {
        SavePng("Assets/Textures/sivier_guard_ring.png", CreateRingTexture(512, 512));
        SavePng("Assets/Textures/sivier_guard_glyph.png", CreateGlyphTexture(256, 256));
        SavePng("Assets/Textures/sivier_guard_glow.png", CreateGlowTexture(512, 512));
        SavePng("Assets/Textures/sivier_guard_dots.png", CreateDotTexture(128, 128));
        SavePng("Assets/Textures/sivier_guard_spark.png", CreateSparkTexture(128, 128));
        SavePng("Assets/Textures/sivier_guard_shadow.png", CreateShadowTexture(512, 512));
        AssetDatabase.Refresh();
    }

    private static Material CreateMaterial(string texturePath, string materialPath, Color tint, bool alphaBlend)
    {
        TextureImporter ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }

        Shader shader = null;
        if (alphaBlend)
        {
            shader = Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        }
        if (shader == null)
        {
            shader = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (mat.HasProperty("_TintColor"))
        {
            mat.SetColor("_TintColor", tint);
        }
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }
        mat.color = tint;
        mat.renderQueue = alphaBlend ? 3000 : 3120;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Mesh CreateOrReplaceMesh(string path, Mesh mesh)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh CreateQuadMesh(string name, float size)
    {
        float half = size * 0.5f;
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = new[]
        {
            new Vector3(-half, -half, 0f),
            new Vector3(-half, half, 0f),
            new Vector3(half, half, 0f),
            new Vector3(half, -half, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateDiamondMesh(string name, float width, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = new[]
        {
            new Vector3(-width * 0.5f, 0f, 0f),
            new Vector3(0f, height * 0.5f, 0f),
            new Vector3(width * 0.5f, 0f, 0f),
            new Vector3(0f, -height * 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r * 0.70f, baseColor.g * 0.70f, baseColor.b * 0.78f, 1f), 0f),
                new GradientColorKey(new Color(0.92f, 0.95f, 1f, 1f), 0.22f),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.50f, holdTime),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateGuardFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.46f, 0.14f, 0.86f), 0f),
                new GradientColorKey(new Color(0.85f, 0.90f, 1f), 0.24f),
                new GradientColorKey(new Color(0.20f, 0.48f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.10f),
                new GradientAlphaKey(peakAlpha * 0.46f, 0.50f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Texture2D CreateRingTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                if (r > 0.94f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(v, u);
                float ringOuter = Mathf.Exp(-Mathf.Pow((r - 0.74f) / 0.020f, 2f));
                float ringMid = Mathf.Exp(-Mathf.Pow((r - 0.52f) / 0.016f, 2f)) * 0.62f;
                float ringInner = Mathf.Exp(-Mathf.Pow((r - 0.30f) / 0.013f, 2f)) * 0.50f;
                float runes = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 12f)), 20f) * Smooth01(0.28f, 0.36f, r) * (1f - Smooth01(0.68f, 0.78f, r)) * 0.58f;
                float ticks = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 32f)), 16f) * Mathf.Exp(-Mathf.Pow((r - 0.84f) / 0.032f, 2f)) * 0.72f;
                float alpha = Mathf.Clamp01(ringOuter + ringMid + ringInner + runes + ticks);
                alpha *= 1f - Smooth01(0.90f, 0.95f, r);
                if (alpha <= 0.015f)
                {
                    continue;
                }

                Color c = Color.Lerp(new Color(0.46f, 0.14f, 0.86f, alpha * 0.72f), new Color(0.72f, 0.82f, 1f, alpha), Mathf.Clamp01(ringOuter + ticks));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateGlyphTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float axisX = Mathf.Max(0f, 1f - Mathf.Abs(v) * 12f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.90f);
                float axisY = Mathf.Max(0f, 1f - Mathf.Abs(u) * 12f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 0.90f);
                float diagA = Mathf.Max(0f, 1f - Mathf.Abs(u - v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float diagB = Mathf.Max(0f, 1f - Mathf.Abs(u + v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.42f, 2f) * 1.6f);
                float alpha = Mathf.Clamp01(axisX + axisY * 0.70f + (diagA + diagB) * 0.34f + glow * 0.32f);
                alpha *= 1f - Smooth01(0.78f, 1f, r);
                if (alpha <= 0.014f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.85f, 0.90f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateGlowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float core = 1f - Smooth01(0.02f, 0.30f, r);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.55f, 2f) * 1.20f);
                float alpha = Mathf.Clamp01(core * 0.66f + halo * 0.40f);
                alpha *= 1f - Smooth01(0.80f, 0.98f, r);
                if (alpha <= 0.012f)
                {
                    continue;
                }
                Color c = Color.Lerp(new Color(0.46f, 0.14f, 0.86f, alpha * 0.60f), new Color(0.55f, 0.72f, 1f, alpha), Mathf.Clamp01(core));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateDotTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float core = Mathf.Exp(-Mathf.Pow(r / 0.42f, 2f) * 2.6f);
                if (core <= 0.02f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.55f, 0.62f, 1f, core);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateSparkTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float streak = Mathf.Max(0f, 1f - Mathf.Abs(v) * 10f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.85f);
                float core = Mathf.Exp(-Mathf.Pow(r / 0.30f, 2f) * 2.4f);
                float alpha = Mathf.Clamp01(streak + core);
                alpha *= 1f - Smooth01(0.82f, 1f, r);
                if (alpha <= 0.015f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.92f, 0.94f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateShadowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u * 0.82f + v * v * 1.18f);
                float feather = 1f - Smooth01(0.10f, 0.96f, r);
                float n = Mathf.Sin((u * 6.7f + v * 3.1f) * Mathf.PI) * Mathf.Sin((u * 2.4f - v * 5.8f) * Mathf.PI);
                float alpha = Mathf.Clamp01(feather * (0.24f + 0.08f * n));
                if (alpha <= 0.012f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.03f, 0.02f, 0.06f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Color[] ClearPixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }
        return pixels;
    }

    private static void SavePng(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static float Smooth01(float edge0, float edge1, float x)
    {
        if (Mathf.Approximately(edge0, edge1))
        {
            return x >= edge1 ? 1f : 0f;
        }

        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static void CopyBundleToMod(string output)
    {
        string bundlePath = Path.Combine(output, BundleName);
        if (!File.Exists(bundlePath))
        {
            bundlePath = Path.Combine(output, BundleName + ".ab");
        }
        if (!File.Exists(bundlePath))
        {
            throw new Exception("Bundle output not found: " + bundlePath);
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", ".."));
        string localAbDir = Path.Combine(projectRoot, "SteriaModFolder", "Assemblies", "AB");
        Directory.CreateDirectory(localAbDir);
        string localTarget = Path.Combine(localAbDir, BundleName);
        File.Copy(bundlePath, localTarget, true);
        File.Copy(bundlePath, localTarget + ".ab", true);
        Debug.Log("Copied Sivier wish guard bundle to project mod folder: " + localTarget);

        string[] steamTargetDirs =
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB",
            @"D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB"
        };

        foreach (string steamTargetDir in steamTargetDirs)
        {
            Directory.CreateDirectory(steamTargetDir);
            string target = Path.Combine(steamTargetDir, BundleName);
            File.Copy(bundlePath, target, true);
            File.Copy(bundlePath, target + ".ab", true);
            Debug.Log("Copied Sivier wish guard bundle to game mod folder: " + target);
        }
    }

    private static void VerifyBuiltBundle(string output)
    {
        string bundlePath = Path.Combine(output, BundleName);
        if (!File.Exists(bundlePath))
        {
            bundlePath = Path.Combine(output, BundleName + ".ab");
        }
        if (!File.Exists(bundlePath))
        {
            throw new Exception("Verify failed: bundle missing " + bundlePath);
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            throw new Exception("Verify failed: unable to load " + bundlePath);
        }

        try
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (prefab == null)
            {
                throw new Exception("Verify failed: prefab missing " + PrefabName);
            }
            int particles = prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
            int renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true).Length;
            if (particles < 9 || renderers < 9)
            {
                throw new Exception("Verify failed: prefab too sparse " + PrefabName + " particles=" + particles + " renderers=" + renderers);
            }
            Debug.Log("Verify Sivier wish guard prefab passed: " + PrefabName + " particles=" + particles + " renderers=" + renderers);
        }
        finally
        {
            bundle.Unload(false);
        }
    }
}
