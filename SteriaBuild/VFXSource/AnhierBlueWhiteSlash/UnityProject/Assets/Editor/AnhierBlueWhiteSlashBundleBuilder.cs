using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnhierBlueWhiteSlashBundleBuilder
{
    private const string BundleName = "steria_anhier_bluewhite_slash";
    private const string PrefabName = "AnhierBlueWhiteSlashPrefab";
    private const string PrefabPath = "Assets/Prefabs/AnhierBlueWhiteSlashPrefab.prefab";

    [MenuItem("Steria/Build Anhier BlueWhite Slash Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        CopyBundleToMod(output);
        VerifyBuiltBundle(output);
        Debug.Log("Built AssetBundle: " + output);
    }

    public static void EnsurePrefab()
    {
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Textures");

        GenerateTextures();
        Mesh bladeMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_BladeMesh.asset", CreateBladeMesh(1.25f, 0.28f, 18));
        Mesh thinBladeMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_ThinBladeMesh.asset", CreateBladeMesh(1.18f, 0.10f, 18));
        Mesh bladeSegA = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_BladeSegA.asset", CreateBladeSegmentMesh(1.25f, 0.28f, 18, 0.00f, 0.42f));
        Mesh bladeSegB = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_BladeSegB.asset", CreateBladeSegmentMesh(1.25f, 0.28f, 18, 0.28f, 0.72f));
        Mesh bladeSegC = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_BladeSegC.asset", CreateBladeSegmentMesh(1.25f, 0.28f, 18, 0.58f, 1.00f));
        Mesh thinSegA = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_ThinSegA.asset", CreateBladeSegmentMesh(1.18f, 0.10f, 18, 0.00f, 0.42f));
        Mesh thinSegB = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_ThinSegB.asset", CreateBladeSegmentMesh(1.18f, 0.10f, 18, 0.30f, 0.74f));
        Mesh thinSegC = CreateOrReplaceMesh("Assets/Meshes/AnhierBlueWhiteSlash_ThinSegC.asset", CreateBladeSegmentMesh(1.18f, 0.10f, 18, 0.60f, 1.00f));

        Material bladeCore = CreateMaterial("Assets/Textures/anhier_ab_blade_core.png", "Assets/Materials/AnhierAB_BladeCore_Add.mat", new Color(0.82f, 0.98f, 1f, 0.98f), false);
        Material bladeGlow = CreateMaterial("Assets/Textures/anhier_ab_blade_glow.png", "Assets/Materials/AnhierAB_BladeGlow_Add.mat", new Color(0.18f, 0.70f, 1f, 0.72f), false);
        Material bladeEcho = CreateMaterial("Assets/Textures/anhier_ab_blade_echo.png", "Assets/Materials/AnhierAB_BladeEcho_Add.mat", new Color(0.08f, 0.46f, 1f, 0.48f), false);
        Material impactGlow = CreateMaterial("Assets/Textures/anhier_ab_impact_glow.png", "Assets/Materials/AnhierAB_ImpactGlow_Add.mat", new Color(0.82f, 0.98f, 1f, 0.9f), false);
        Material spark = CreateMaterial("Assets/Textures/anhier_ab_spark.png", "Assets/Materials/AnhierAB_Spark_Add.mat", new Color(0.78f, 0.96f, 1f, 0.96f), false);
        Material smoke = CreateMaterial("Assets/Textures/anhier_ab_mist.png", "Assets/Materials/AnhierAB_Mist_Add.mat", new Color(0.28f, 0.80f, 1f, 0.38f), true);

        GameObject root = new GameObject(PrefabName);
        root.layer = 8;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        ParticleSystem rootParticle = root.AddComponent<ParticleSystem>();
        var rootMain = rootParticle.main;
        rootMain.playOnAwake = true;
        rootMain.loop = false;
        rootMain.duration = 1f;
        rootMain.startLifetime = 1f;
        rootMain.startSpeed = 0f;
        var rootEmission = rootParticle.emission;
        rootEmission.enabled = false;
        ParticleSystemRenderer rootRenderer = root.GetComponent<ParticleSystemRenderer>();
        rootRenderer.enabled = false;

        Transform bladeRoot = CreateGroup(root.transform, "AB_BladeRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(8.75f, -0.05f, 0f));

        CreateBladeParticle(bladeRoot, "AB_Blade_OuterBlue_A", bladeGlow, bladeSegA, Vector3.zero, -7f, 0.000f, 0.38f, 7.8f, 2, new Color(0.16f, 0.62f, 1f, 0.66f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_OuterBlue_B", bladeGlow, bladeSegB, Vector3.zero, -7f, 0.030f, 0.40f, 7.8f, 3, new Color(0.16f, 0.70f, 1f, 0.76f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_OuterBlue_C", bladeGlow, bladeSegC, Vector3.zero, -7f, 0.062f, 0.42f, 7.8f, 4, new Color(0.20f, 0.78f, 1f, 0.82f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_WhiteCore_A", bladeCore, thinSegA, new Vector3(0.24f, 0.05f, -0.02f), -7f, 0.018f, 0.32f, 7.6f, 6, new Color(0.84f, 0.98f, 1f, 0.82f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_WhiteCore_B", bladeCore, thinSegB, new Vector3(0.24f, 0.05f, -0.02f), -7f, 0.052f, 0.34f, 7.6f, 7, new Color(0.92f, 1f, 1f, 0.94f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_WhiteCore_C", bladeCore, thinSegC, new Vector3(0.24f, 0.05f, -0.02f), -7f, 0.086f, 0.36f, 7.6f, 8, new Color(0.96f, 1f, 1f, 1f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_SecondCrescent_B", bladeCore, thinSegB, new Vector3(0.38f, 0.26f, -0.03f), 7f, 0.070f, 0.30f, 6.4f, 9, new Color(0.58f, 0.92f, 1f, 0.58f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_SecondCrescent_C", bladeCore, thinSegC, new Vector3(0.38f, 0.26f, -0.03f), 7f, 0.105f, 0.32f, 6.4f, 10, new Color(0.70f, 0.96f, 1f, 0.76f), true);
        CreateBladeParticle(bladeRoot, "AB_Blade_BackEcho_A", bladeEcho, bladeSegA, new Vector3(-0.72f, 0.28f, 0.02f), -23f, 0.030f, 0.48f, 7.4f, 1, new Color(0.04f, 0.28f, 0.92f, 0.38f), false);
        CreateBladeParticle(bladeRoot, "AB_Blade_BackEcho_C", bladeEcho, bladeSegC, new Vector3(-0.72f, 0.28f, 0.02f), -23f, 0.095f, 0.54f, 7.4f, 1, new Color(0.04f, 0.34f, 1f, 0.48f), false);

        CreateSmokeParticle(bladeRoot, "AB_Mist_Backwash_A", smoke, new Vector3(0.2f, 0.05f, 0.04f), 0.02f, 3, 2.8f, 4.3f, new Color(0.48f, 0.86f, 1f, 0.36f), 0);
        CreateSmokeParticle(bladeRoot, "AB_Mist_Backwash_B", smoke, new Vector3(0.45f, -0.05f, 0.05f), 0.04f, 4, 3.4f, 5.2f, new Color(0.05f, 0.24f, 0.42f, 0.32f), -1);
        CreateArcStreakParticle(bladeRoot, "AB_Arc_GlowStreaks", impactGlow, new Vector3(0.35f, 0.06f, -0.01f), 0.05f, 12, 0.18f, 0.62f, 4.5f, 10.5f, 3.6f, 3);
        CreateEdgeStreakParticle(bladeRoot, "AB_Edge_WhiteNeedles", spark, new Vector3(0.4f, 0.18f, -0.04f), -8f, 0.025f, 24, 0.08f, 0.30f, 6.0f, 14.5f, 4.2f, 7);

        CreateImpactFlash(impactRoot, "AB_Impact_BloomWhite", impactGlow, new Vector3(0f, 0f, 0.02f), 0.11f, 0.42f, 8.5f, new Color(0.9f, 1f, 1f, 0.9f), 4);
        CreateImpactFlash(impactRoot, "AB_Impact_BlueHalo", impactGlow, new Vector3(0f, 0f, 0.03f), 0.12f, 0.48f, 11f, new Color(0.18f, 0.68f, 1f, 0.42f), 2);
        CreateImpactShardParticle(impactRoot, "AB_Impact_StretchedShards", spark, new Vector3(0.2f, 0.05f, -0.01f), 0.10f, 14, new Color(0.72f, 0.95f, 1f, 0.95f), 5);
        CreateImpactShardParticle(impactRoot, "AB_Impact_DimBlueDebris", spark, new Vector3(0.05f, 0.08f, 0f), 0.13f, 10, new Color(0.12f, 0.44f, 1f, 0.5f), 1);
        CreateSnowDustParticle(impactRoot, "AB_SnowDust_Rim", smoke, new Vector3(-0.15f, 0.10f, 0.06f), 0.14f, 14, 0.22f, 0.75f, 2.0f, 4.5f, 1);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        Debug.Log("Prepared Hanzhou-style particle prefab for bundle: " + PrefabPath);
    }

    private static void GenerateTextures()
    {
        SavePng("Assets/Textures/anhier_ab_blade_core.png", CreateSlashTexture(1024, 256, 0.045f, 0.24f, 1.08f, true, 0.04f));
        SavePng("Assets/Textures/anhier_ab_blade_glow.png", CreateSlashTexture(1024, 300, 0.085f, 0.48f, 0.82f, false, 0.12f));
        SavePng("Assets/Textures/anhier_ab_blade_echo.png", CreateSlashTexture(1024, 256, 0.11f, 0.42f, 0.5f, false, 0.22f));
        SavePng("Assets/Textures/anhier_ab_impact_glow.png", CreateImpactGlowTexture(512, 512));
        SavePng("Assets/Textures/anhier_ab_spark.png", CreateSparkTexture(256, 256));
        SavePng("Assets/Textures/anhier_ab_mist.png", CreateMistTexture(512, 512));
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
            ti.maxTextureSize = 2048;
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
        mat.renderQueue = alphaBlend ? 3000 : 3100;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Mesh CreateOrReplaceMesh(string path, Mesh mesh)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh CreateBladeMesh(float width, float height, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float arc = Mathf.Sin(t * Mathf.PI);
            float y = -height * 0.18f + arc * height * 0.82f - t * height * 0.22f;
            float localHalf = Mathf.Lerp(0.22f, 0.5f, Mathf.Pow(arc, 0.6f)) * height;
            localHalf *= Smooth01(0f, 0.18f, t) * (1f - Smooth01(0.9f, 1f, t));
            localHalf = Mathf.Max(localHalf, height * 0.03f);

            vertices.Add(new Vector3(x, y - localHalf, 0f));
            vertices.Add(new Vector3(x, y + localHalf, 0f));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i < segments)
            {
                int a = i * 2;
                triangles.Add(a);
                triangles.Add(a + 1);
                triangles.Add(a + 2);
                triangles.Add(a + 1);
                triangles.Add(a + 3);
                triangles.Add(a + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "AnhierBlueWhiteSlash_BladeMesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateBladeSegmentMesh(float width, float height, int segments, float uStart, float uEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        uStart = Mathf.Clamp01(uStart);
        uEnd = Mathf.Clamp01(uEnd);
        if (uEnd < uStart)
        {
            float temp = uStart;
            uStart = uEnd;
            uEnd = temp;
        }

        segments = Mathf.Max(2, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(uStart, uEnd, localT);
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float arc = Mathf.Sin(t * Mathf.PI);
            float y = -height * 0.18f + arc * height * 0.82f - t * height * 0.22f;
            float localHalf = Mathf.Lerp(0.22f, 0.5f, Mathf.Pow(arc, 0.6f)) * height;
            localHalf *= Smooth01(0f, 0.18f, t) * (1f - Smooth01(0.9f, 1f, t));
            localHalf = Mathf.Max(localHalf, height * 0.03f);

            vertices.Add(new Vector3(x, y - localHalf, 0f));
            vertices.Add(new Vector3(x, y + localHalf, 0f));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i < segments)
            {
                int a = i * 2;
                triangles.Add(a);
                triangles.Add(a + 1);
                triangles.Add(a + 2);
                triangles.Add(a + 1);
                triangles.Add(a + 3);
                triangles.Add(a + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "AnhierBlueWhiteSlash_BladeSegmentMesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void CreateBladeParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float zRot, float delay, float lifetime, float size, int sortingOrder, Color color, bool scaleOverLifetime)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 180f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.65f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.08f, 0.5f);

        if (scaleOverLifetime)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0.18f),
                new Keyframe(0.09f, 1.12f),
                new Keyframe(0.38f, 1f),
                new Keyframe(1f, 0.65f));
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateSmokeParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.85f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 2;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 8.5f;
        shape.arc = 220f;
        shape.rotation = new Vector3(0f, 0f, -18f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = CreateFadeGradient(color, color.a, 0.15f, 0.62f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.38f, 1f), new Keyframe(1f, 1.38f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.22f;
        noise.frequency = 0.9f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateArcStreakParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, float lengthScale, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, -8f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.8f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.34f, 0.82f, 1f, 0.72f), new Color(1f, 1f, 1f, 0.98f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 10f;
        shape.arc = 240f;
        shape.rotation = new Vector3(0f, 0f, -24f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(2.0f, 5.8f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.0f, 1.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.9f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 1.4f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.12f;
        r.lengthScale = lengthScale;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateEdgeStreakParticle(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, float lengthScale, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.38f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.88f, 1f, 0.78f), new Color(1f, 1f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(5.6f, 0.55f, 0.05f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(4.5f, 11f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.4f, 1.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.95f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.18f;
        r.lengthScale = lengthScale;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateImpactFlash(Transform parent, string name, Material mat, Vector3 pos, float delay, float lifetime, float size, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 1f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 2;

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
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.12f, 1f), new Keyframe(1f, 1.35f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateImpactShardParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, -8f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 1f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.75f;
        shape.angle = 28f;
        shape.arc = 220f;
        shape.rotation = new Vector3(0f, 90f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(2.4f, 5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, color.a, 0.08f, 0.5f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1.2f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.18f;
        r.lengthScale = 2.4f;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateSnowDustParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.8f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.84f, 1f, 0.48f), new Color(0.9f, 1f, 1f, 0.62f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 7.5f;
        shape.arc = 190f;
        shape.rotation = new Vector3(0f, 0f, -20f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.56f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.38f;
        noise.frequency = 1.1f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
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

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r * 0.75f, baseColor.g * 0.85f, baseColor.b, 1f), 0f),
                new GradientColorKey(new Color(0.92f, 1f, 1f, 1f), 0.18f),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.54f, holdTime),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateBlueWhiteFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.12f, 0.56f, 1f), 0f),
                new GradientColorKey(new Color(0.9f, 1f, 1f), 0.24f),
                new GradientColorKey(new Color(0.34f, 0.76f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.1f),
                new GradientAlphaKey(peakAlpha * 0.44f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Texture2D CreateSlashTexture(int width, int height, float coreWidth, float glowWidth, float alphaScale, bool hardCore, float waviness)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            float yy = (v - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float arc = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                float center = -0.34f + 0.58f * Mathf.Pow(arc, 0.72f) - 0.08f * u + waviness * 0.05f * Mathf.Sin((u * 4.2f + 0.15f) * Mathf.PI);
                float yFromBlade = Mathf.Abs(yy - center);
                float taperHead = Smooth01(0.02f, 0.16f, u);
                float taperTail = 1f - Smooth01(0.78f, 0.995f, u);
                float taper = Mathf.Pow(Mathf.Clamp01(taperHead * taperTail), 0.68f);
                float bladeBelly = Mathf.Pow(arc, 0.32f);
                float localCore = Mathf.Lerp(coreWidth * 0.75f, coreWidth * 1.55f, bladeBelly);
                float localGlow = Mathf.Lerp(glowWidth * 0.70f, glowWidth * 1.18f, bladeBelly);
                float core = Mathf.Exp(-Mathf.Pow(yFromBlade / Mathf.Max(0.001f, localCore), 2f) * 2.15f);
                float body = Mathf.Exp(-Mathf.Pow(yFromBlade / Mathf.Max(0.001f, localCore * 2.6f), 2f) * 1.45f);
                float glow = Mathf.Exp(-Mathf.Pow(yFromBlade / Mathf.Max(0.001f, localGlow), 2f) * 1.35f);
                float outerMask = 1f - Smooth01(localGlow * 0.86f, localGlow * 1.10f, yFromBlade);
                float upperTrim = 1f - Smooth01(center + localGlow * 0.74f, center + localGlow * 1.05f, yy);
                float lowerTrim = Smooth01(center - localGlow * 1.12f, center - localGlow * 0.82f, yy);
                float crescentMask = Mathf.Clamp01(outerMask * upperTrim * lowerTrim);
                float hash = Mathf.Sin((u * 32f + y * 0.014f) * Mathf.PI) * 0.5f + 0.5f;
                float alpha = Mathf.Clamp01((core * (hardCore ? 1.5f : 0.85f) + body * 0.55f + glow * 0.32f) * taper * crescentMask * alphaScale);
                alpha *= Mathf.Lerp(0.82f, 1.08f, hash * Smooth01(0.48f, 1f, u));

                if (alpha <= 0.018f)
                {
                    continue;
                }

                Color deepBlue = new Color(0.00f, 0.28f, 1f, alpha * 0.78f);
                Color iceBlue = new Color(0.13f, 0.82f, 1f, alpha);
                Color white = new Color(0.94f, 1f, 1f, Mathf.Min(1f, alpha * 1.18f));
                Color color = Color.Lerp(deepBlue, iceBlue, Mathf.Clamp01(body + glow * 0.22f));
                color = Color.Lerp(color, white, Mathf.Clamp01(core * (hardCore ? 1.2f : 0.55f)));
                color.a = alpha;
                pixels[y * width + x] = color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateImpactGlowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float core = 1f - Smooth01(0.02f, 0.20f, r);
                float horizontal = Mathf.Max(0f, 1f - Mathf.Abs(v) * 16f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.62f);
                float vertical = Mathf.Max(0f, 1f - Mathf.Abs(u) * 22f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 0.9f);
                float diagA = Mathf.Max(0f, 1f - Mathf.Abs(u - v) * 11f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 1.08f);
                float diagB = Mathf.Max(0f, 1f - Mathf.Abs(u + v) * 11f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 1.08f);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.46f, 2f) * 1.35f);
                float alpha = Mathf.Clamp01(core + horizontal * 0.82f + vertical * 0.38f + (diagA + diagB) * 0.2f + halo * 0.22f);
                alpha *= 1f - Smooth01(0.72f, 0.98f, r);
                if (alpha <= 0.012f)
                {
                    continue;
                }
                Color c = Color.Lerp(new Color(0.14f, 0.66f, 1f, alpha * 0.6f), new Color(0.94f, 1f, 1f, alpha), Mathf.Clamp01(core + horizontal));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateSparkTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float axisX = Mathf.Max(0f, 1f - Mathf.Abs(v) * 11f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 1.0f);
                float axisY = Mathf.Max(0f, 1f - Mathf.Abs(u) * 11f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 1.0f);
                float diagA = Mathf.Max(0f, 1f - Mathf.Abs(u - v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float diagB = Mathf.Max(0f, 1f - Mathf.Abs(u + v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.45f, 2f) * 1.7f);
                float alpha = Mathf.Clamp01(axisX + axisY * 0.72f + (diagA + diagB) * 0.34f + glow * 0.32f);
                alpha *= 1f - Smooth01(0.76f, 1f, r);
                if (alpha <= 0.014f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.76f, 0.96f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateMistTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u * 0.82f + v * v * 1.24f);
                float n = Mathf.Sin((u * 10.7f + v * 4.6f) * Mathf.PI) * Mathf.Sin((u * 2.9f - v * 8.8f) * Mathf.PI);
                float wisps = Mathf.Sin((u * 18f + Mathf.Sin(v * 4.5f) * 0.6f) * Mathf.PI) * 0.5f + 0.5f;
                float alpha = Mathf.Clamp01((1f - Smooth01(0.08f, 0.98f, r)) * (0.12f + 0.16f * n + 0.08f * wisps));
                if (alpha <= 0.01f)
                {
                    pixels[y * width + x] = clear;
                    continue;
                }
                pixels[y * width + x] = new Color(0.26f, 0.78f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void SavePng(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
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
            Debug.LogError("Bundle output not found: " + bundlePath);
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", ".."));
        string localAbDir = Path.Combine(projectRoot, "SteriaModFolder", "Assemblies", "AB");
        Directory.CreateDirectory(localAbDir);
        string localTarget = Path.Combine(localAbDir, BundleName);
        File.Copy(bundlePath, localTarget, true);
        File.Copy(bundlePath, localTarget + ".ab", true);
        Debug.Log("Copied bundle to project mod folder: " + localTarget);

        string[] steamTargetDirs =
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB",
            @"D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB"
        };

        foreach (string steamTargetDir in steamTargetDirs)
        {
            try
            {
                Directory.CreateDirectory(steamTargetDir);
                string steamTarget = Path.Combine(steamTargetDir, BundleName);
                File.Copy(bundlePath, steamTarget, true);
                File.Copy(bundlePath, steamTarget + ".ab", true);
                Debug.Log("Copied bundle to game mod folder: " + steamTarget);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not copy bundle to game folder " + steamTargetDir + ": " + ex.Message);
            }
        }
    }

    public static void VerifyBundle()
    {
        VerifyBuiltBundle(Path.GetFullPath("Assets/../AssetBundles"));
    }

    public static void RenderPreview()
    {
        EnsurePrefab();

        string previewDir = Path.GetFullPath("Assets/../Preview");
        Directory.CreateDirectory(previewDir);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("Preview failed: prefab not found at " + PrefabPath);
            EditorApplication.Exit(6);
            return;
        }

        GameObject root = Object.Instantiate(prefab);
        root.name = "AnhierBlueWhiteSlashPreview";
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject camObj = new GameObject("PreviewCamera");
        Camera camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 7.2f;
        camera.transform.position = new Vector3(4.2f, 0.2f, -18f);
        camera.transform.rotation = Quaternion.identity;

        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.randomSeed = 19;
            ps.useAutoRandomSeed = false;
            ps.Play(true);
        }

        float[] times = { 0.07f, 0.16f, 0.28f, 0.46f };
        for (int i = 0; i < times.Length; i++)
        {
            foreach (ParticleSystem ps in systems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.randomSeed = 19;
                ps.useAutoRandomSeed = false;
                ps.Simulate(times[i], true, true, true);
            }

            string file = Path.Combine(previewDir, "anhier_bluewhite_slash_preview_" + i.ToString("00") + "_" + Mathf.RoundToInt(times[i] * 1000f) + "ms.png");
            CaptureCamera(camera, file, 1280, 720);
            Debug.Log("Preview frame: " + file);
        }

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(camObj);
        Debug.Log("Preview complete: " + previewDir);
    }

    private static void CaptureCamera(Camera camera, string path, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture oldActive = RenderTexture.active;
        RenderTexture oldTarget = camera.targetTexture;

        camera.targetTexture = rt;
        RenderTexture.active = rt;
        camera.Render();
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());

        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
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
            Debug.LogError("Verify failed: bundle file not found in " + output);
            EditorApplication.Exit(2);
            return;
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError("Verify failed: AssetBundle.LoadFromFile returned null for " + bundlePath);
            EditorApplication.Exit(3);
            return;
        }

        string[] assetNames = bundle.GetAllAssetNames();
        Debug.Log("Verify bundle asset names: " + string.Join(", ", assetNames));

        GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
        if (prefab == null)
        {
            prefab = bundle.LoadAsset<GameObject>(PrefabPath.ToLowerInvariant());
        }

        if (prefab == null)
        {
            Debug.LogError("Verify failed: prefab not loadable as " + PrefabName);
            bundle.Unload(true);
            EditorApplication.Exit(4);
            return;
        }

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
        int meshParticleCount = 0;
        int stretchParticleCount = 0;
        foreach (Renderer renderer in renderers)
        {
            ParticleSystemRenderer psr = renderer as ParticleSystemRenderer;
            if (psr == null)
            {
                continue;
            }
            if (psr.renderMode == ParticleSystemRenderMode.Mesh)
            {
                meshParticleCount++;
            }
            if (psr.renderMode == ParticleSystemRenderMode.Stretch)
            {
                stretchParticleCount++;
            }
        }

        if (renderers.Length < 12 || particles.Length < 13 || meshParticleCount < 4 || stretchParticleCount < 3)
        {
            Debug.LogError("Verify failed: prefab lacks expected Hanzhou-style particle layers. renderers=" + renderers.Length + " particles=" + particles.Length + " meshParticles=" + meshParticleCount + " stretchParticles=" + stretchParticleCount);
            bundle.Unload(true);
            EditorApplication.Exit(5);
            return;
        }

        Debug.Log("Verify passed: loaded particle prefab " + prefab.name + " renderers=" + renderers.Length + " particles=" + particles.Length + " meshParticles=" + meshParticleCount + " stretchParticles=" + stretchParticleCount + " from " + bundlePath);
        bundle.Unload(true);
    }
}
