using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlasmaLightningSlashBundleBuilder
{
    private const string BundleName = "steria_plasma_lightning_slash";
    private const string PrefabName = "PlasmaLightningSlashPrefab";
    private const string PrefabPath = "Assets/Prefabs/PlasmaLightningSlashPrefab.prefab";
    private const string ShaderPath = "Assets/Shaders/PlasmaLightningSlashFlow.shader";
    private const string ShaderName = "Steria/PlasmaLightningSlashFlow";
    private const string TextureDir = "Assets/Textures/Generated/PlasmaLightningSlash";
    private const string MaterialDir = "Assets/Materials/PlasmaLightningSlash";
    private const string MeshDir = "Assets/Meshes/PlasmaLightningSlash";
    private const string NativeEvidenceFileName = "native_particle_evidence.txt";
    private static readonly string[] PreviewFiles =
    {
        "black_01_early_reveal.png",
        "black_02_contact_peak.png",
        "black_03_full_body.png",
        "black_04_fade_retreat.png",
        "stage_01_early_reveal.png",
        "stage_02_contact_peak.png",
        "stage_03_full_body.png",
        "stage_04_fade_retreat.png"
    };

    [MenuItem("Steria/Build Plasma Lightning Slash Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        RenderPreviewPrepared();

        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = new[] { PrefabPath }
        };
        BuildPipeline.BuildAssetBundles(
            output,
            new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);
        VerifyBuiltBundle(output);
        Debug.Log("Built plasma lightning slash AssetBundle: " + Path.Combine(output, BundleName));
    }

    [MenuItem("Steria/Ensure Plasma Lightning Slash Prefab")]
    public static void EnsurePrefab()
    {
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory(MaterialDir);
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory(MeshDir);
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Shaders");
        Directory.CreateDirectory("Assets/Textures");
        Directory.CreateDirectory("Assets/Textures/Generated");
        Directory.CreateDirectory(TextureDir);

        EnsureCheckedInShader();
        GenerateTextures();
        AssetDatabase.Refresh();

        Mesh ribbonMesh = CreateOrReplaceMesh(
            MeshDir + "/PlasmaLightningSlash_MainRibbon.asset",
            CreatePlasmaRibbonMesh("PlasmaLightningSlash_MainRibbon"));
        Mesh[] branchMeshes =
        {
            CreateOrReplaceMesh(
                MeshDir + "/PlasmaLightningSlash_BranchArc_01.asset",
                CreateBranchArcMesh("PlasmaLightningSlash_BranchArc_01", 0.58f, 0.050f, 0.35f)),
            CreateOrReplaceMesh(
                MeshDir + "/PlasmaLightningSlash_BranchArc_02.asset",
                CreateBranchArcMesh("PlasmaLightningSlash_BranchArc_02", 0.71f, -0.042f, 1.30f)),
            CreateOrReplaceMesh(
                MeshDir + "/PlasmaLightningSlash_BranchArc_03.asset",
                CreateBranchArcMesh("PlasmaLightningSlash_BranchArc_03", 0.52f, 0.034f, 2.15f)),
            CreateOrReplaceMesh(
                MeshDir + "/PlasmaLightningSlash_BranchArc_04.asset",
                CreateBranchArcMesh("PlasmaLightningSlash_BranchArc_04", 0.84f, -0.056f, 2.85f))
        };
        Mesh contactMesh = CreateOrReplaceMesh(
            MeshDir + "/PlasmaLightningSlash_ContactNeedle.asset",
            CreateContactNeedleMesh("PlasmaLightningSlash_ContactNeedle"));

        Material slashMaterial = CreateFlowMaterial(
            MaterialDir + "/PlasmaLightningSlash_MainFlow.mat");
        Material branchMaterial = CreateParticleMaterial(
            TextureDir + "/plasma_support_line.png",
            MaterialDir + "/PlasmaLightningSlash_BranchArc.mat",
            new Color(0.78f, 0.30f, 1.00f, 0.78f),
            3130);
        Material sparkMaterial = CreateParticleMaterial(
            TextureDir + "/plasma_micro_spark.png",
            MaterialDir + "/PlasmaLightningSlash_MicroSpark.mat",
            new Color(0.88f, 0.54f, 1.00f, 0.90f),
            3140);
        Material contactMaterial = CreateParticleMaterial(
            TextureDir + "/plasma_support_line.png",
            MaterialDir + "/PlasmaLightningSlash_ContactNeedle.mat",
            new Color(1.00f, 0.94f, 1.00f, 0.92f),
            3150);

        GameObject root = CreateRoot(PrefabName);
        PlasmaLightningSlashDriver driver = root.AddComponent<PlasmaLightningSlashDriver>();
        driver.duration = 0.50f;
        driver.loop = false;

        Transform bladeRoot = CreateGroup(root.transform, "AB_BladeRoot", Vector3.zero);
        Vector3 impactPosition = EvaluateCenterline(1f);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", impactPosition);

        CreateMeshLayer(
            bladeRoot,
            "PLS_ContinuousPlasmaShear",
            ribbonMesh,
            slashMaterial,
            10);
        CreateBranchArcs(
            impactRoot,
            branchMaterial,
            branchMeshes,
            12);
        CreateMicroSparks(
            impactRoot,
            "PLS_StretchedMicroSparks_16",
            sparkMaterial,
            14);
        CreateContactNeedle(
            impactRoot,
            "PLS_AsymmetricContactNeedle_1",
            contactMaterial,
            contactMesh,
            16);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
        if (importer == null)
        {
            throw new Exception("Unable to assign plasma slash bundle to " + PrefabPath);
        }
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        VerifyPrefabAsset();
        Debug.Log("Prepared independent plasma lightning slash prefab: " + PrefabPath);
    }

    [MenuItem("Steria/Render Plasma Lightning Slash Preview")]
    public static void RenderPreview()
    {
        EnsurePrefab();
        RenderPreviewPrepared();
    }

    [MenuItem("Steria/Verify Plasma Lightning Slash Bundle")]
    public static void VerifyBundle()
    {
        VerifyBuiltBundle(Path.GetFullPath("Assets/../AssetBundles"));
    }

    private static void EnsureCheckedInShader()
    {
        if (!File.Exists(ShaderPath))
        {
            throw new Exception("Checked-in plasma shader is missing: " + ShaderPath);
        }

        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
        if (Shader.Find(ShaderName) == null)
        {
            throw new Exception("Unable to import checked-in shader " + ShaderName);
        }
    }

    private static void GenerateTextures()
    {
        SavePng(
            TextureDir + "/plasma_slash_rgba_mask.png",
            CreateProceduralMaskTexture(1024, 256));
        SavePng(
            TextureDir + "/plasma_directional_noise.png",
            CreateDirectionalNoiseTexture(512, 256));
        SavePng(
            TextureDir + "/plasma_support_line.png",
            CreateSupportLineTexture(256, 64));
        SavePng(
            TextureDir + "/plasma_micro_spark.png",
            CreateMicroSparkTexture(128, 128));

        AssetDatabase.Refresh();
        ConfigureTexture(TextureDir + "/plasma_slash_rgba_mask.png", TextureWrapMode.Clamp, 1024);
        ConfigureTexture(TextureDir + "/plasma_directional_noise.png", TextureWrapMode.Repeat, 512);
        ConfigureTexture(TextureDir + "/plasma_support_line.png", TextureWrapMode.Clamp, 256);
        ConfigureTexture(TextureDir + "/plasma_micro_spark.png", TextureWrapMode.Clamp, 128);
    }

    private static void ConfigureTexture(string assetPath, TextureWrapMode wrapMode, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new Exception("Generated texture importer is missing: " + assetPath);
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = wrapMode;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material CreateFlowMaterial(string materialPath)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            throw new Exception("Missing shader " + ShaderName);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = shader;
        material.SetTexture(
            "_SlashTex",
            AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/plasma_slash_rgba_mask.png"));
        material.SetTexture(
            "_NoiseTex",
            AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/plasma_directional_noise.png"));
        material.SetColor("_TintColor", Color.white);
        material.SetColor("_CoreColor", new Color(1.000f, 0.973f, 1.000f, 1.00f));
        material.SetColor("_BodyColor", new Color(0.776f, 0.467f, 1.000f, 0.88f));
        material.SetColor("_EdgeColor", new Color(0.482f, 0.086f, 0.788f, 0.78f));
        material.SetFloat("_Reveal", 0.015f);
        material.SetFloat("_Retreat", -0.085f);
        material.SetFloat("_Alpha", 1f);
        material.SetFloat("_Intensity", 1.45f);
        material.SetFloat("_Phase", 0f);
        material.SetVector("_EdgeFade", new Vector4(0.055f, 0.075f, 0f, 0f));
        material.SetFloat("_AbrasionStrength", 0.15f);
        material.SetFloat("_FiberStrength", 0.46f);
        material.renderQueue = 3120;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateParticleMaterial(
        string texturePath,
        string materialPath,
        Color tint,
        int renderQueue)
    {
        Shader shader = Shader.Find("Particles/Additive")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Sprites/Default");
        if (shader == null)
        {
            throw new Exception("No additive particle shader is available.");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = shader;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            throw new Exception("Particle texture is missing: " + texturePath);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", tint);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }
        material.renderQueue = renderQueue;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh CreateOrReplaceMesh(string path, Mesh mesh)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh CreatePlasmaRibbonMesh(string name)
    {
        const int pathSegments = 68;
        List<Vector3> vertices = new List<Vector3>((pathSegments + 1) * 2);
        List<Vector2> uvs = new List<Vector2>((pathSegments + 1) * 2);
        List<Color> colors = new List<Color>((pathSegments + 1) * 2);
        List<int> triangles = new List<int>(pathSegments * 6);

        for (int i = 0; i <= pathSegments; i++)
        {
            float pathT = i / (float)pathSegments;
            Vector3 center = EvaluateCenterline(pathT);
            Vector3 tangent = EvaluateTangent(pathT).normalized;
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;

            float pressure = Mathf.Exp(-Mathf.Pow((pathT - 0.55f) / 0.30f, 2f));
            float tailRamp = Mathf.Lerp(0.14f, 1f, Smoother01(0.04f, 0.48f, pathT));
            float tailTip = Smoother01(0.00f, 0.045f, pathT);
            float spearTip = 1f - Smoother01(0.79f, 1.00f, pathT);
            float width = (0.075f + pressure * 0.72f) * tailRamp * tailTip * spearTip;
            float fibrousEdge = pathT < 0.30f
                ? Mathf.Sin(pathT * Mathf.PI * 15f) * (1f - pathT / 0.30f) * 0.018f
                : 0f;
            float outerPressure = Mathf.Lerp(0.46f, 0.58f, Smoother01(0.18f, 0.68f, pathT));

            Vector3 inner = center - normal * (width * (1f - outerPressure) - fibrousEdge);
            Vector3 outer = center + normal * (width * outerPressure + fibrousEdge);
            vertices.Add(inner);
            vertices.Add(outer);
            uvs.Add(new Vector2(pathT, 0f));
            uvs.Add(new Vector2(pathT, 1f));
            colors.Add(Color.white);
            colors.Add(Color.white);

            if (i < pathSegments)
            {
                int index = i * 2;
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);
                triangles.Add(index + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 EvaluateCenterline(float t)
    {
        Vector3 p0 = new Vector3(-4.50f, -0.45f, 0f);
        Vector3 p1 = new Vector3(-1.80f, 0.18f, 0f);
        Vector3 p2 = new Vector3(1.50f, 0.95f, 0f);
        Vector3 p3 = new Vector3(4.55f, 0.33f, 0f);
        float inverse = 1f - t;
        return inverse * inverse * inverse * p0
            + 3f * inverse * inverse * t * p1
            + 3f * inverse * t * t * p2
            + t * t * t * p3;
    }

    private static Vector3 EvaluateTangent(float t)
    {
        Vector3 p0 = new Vector3(-4.50f, -0.45f, 0f);
        Vector3 p1 = new Vector3(-1.80f, 0.18f, 0f);
        Vector3 p2 = new Vector3(1.50f, 0.95f, 0f);
        Vector3 p3 = new Vector3(4.55f, 0.33f, 0f);
        float inverse = 1f - t;
        return 3f * inverse * inverse * (p1 - p0)
            + 6f * inverse * t * (p2 - p1)
            + 3f * t * t * (p3 - p2);
    }

    private static Mesh CreateBranchArcMesh(
        string name,
        float length,
        float primaryBend,
        float phase)
    {
        const int segments = 14;
        List<Vector3> vertices = new List<Vector3>((segments + 1) * 2);
        List<Vector2> uvs = new List<Vector2>((segments + 1) * 2);
        List<int> triangles = new List<int>(segments * 6);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float taper = Mathf.Sin(t * Mathf.PI);
            float x = t * length;
            float y = Mathf.Sin(t * Mathf.PI) * primaryBend
                + Mathf.Sin(t * Mathf.PI * 3.0f + phase) * 0.014f * taper;
            float nextT = Mathf.Min(1f, t + 0.01f);
            float nextTaper = Mathf.Sin(nextT * Mathf.PI);
            float nextX = nextT * length;
            float nextY = Mathf.Sin(nextT * Mathf.PI) * primaryBend
                + Mathf.Sin(nextT * Mathf.PI * 3.0f + phase) * 0.014f * nextTaper;
            Vector2 tangent = new Vector2(nextX - x, nextY - y).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float width = 0.022f * Smoother01(0f, 0.06f, t) * (1f - Smoother01(0.76f, 1f, t));

            vertices.Add(new Vector3(x - normal.x * width, y - normal.y * width, 0f));
            vertices.Add(new Vector3(x + normal.x * width, y + normal.y * width, 0f));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i < segments)
            {
                int index = i * 2;
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);
                triangles.Add(index + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateContactNeedleMesh(string name)
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(new List<Vector3>
        {
            new Vector3(-0.12f, -0.019f, 0f),
            new Vector3(-0.025f, 0.078f, 0f),
            new Vector3(0.64f, 0.009f, 0f),
            new Vector3(0.085f, -0.045f, 0f)
        });
        mesh.SetUVs(0, new List<Vector2>
        {
            new Vector2(0f, 0.35f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0.52f),
            new Vector2(0.12f, 0f)
        });
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.layer = 8;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }

    private static Transform CreateGroup(Transform parent, string name, Vector3 localPosition)
    {
        GameObject group = new GameObject(name);
        group.layer = 8;
        group.transform.SetParent(parent, false);
        group.transform.localPosition = localPosition;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    private static void CreateMeshLayer(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        int sortingOrder)
    {
        GameObject layer = new GameObject(name);
        layer.layer = 8;
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = Vector3.zero;
        layer.transform.localRotation = Quaternion.identity;
        layer.transform.localScale = Vector3.one;

        MeshFilter filter = layer.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static GameObject NewParticle(
        Transform parent,
        string name,
        Vector3 localPosition,
        Quaternion rotation)
    {
        GameObject particleObject = new GameObject(name);
        particleObject.layer = 8;
        particleObject.transform.SetParent(parent, false);
        particleObject.transform.localPosition = localPosition;
        particleObject.transform.localRotation = rotation;
        particleObject.transform.localScale = Vector3.one;
        particleObject.AddComponent<ParticleSystem>();
        return particleObject;
    }

    private static void CreateBranchArcs(
        Transform parent,
        Material material,
        Mesh[] meshes,
        int sortingOrder)
    {
        if (meshes == null || meshes.Length != 4)
        {
            throw new ArgumentException("Four authored branch meshes are required.", "meshes");
        }

        Vector3[] origins =
        {
            new Vector3(-0.10f, 0.045f, 0f),
            new Vector3(-0.17f, -0.060f, 0f),
            new Vector3(-0.24f, 0.078f, 0f),
            new Vector3(-0.30f, -0.025f, 0f)
        };
        float[] angles = { 151f, 163f, 177f, 192f };
        float[] delays = { 0.152f, 0.156f, 0.161f, 0.166f };
        float[] lifetimes = { 0.135f, 0.128f, 0.118f, 0.142f };
        float[] sizes = { 0.96f, 0.88f, 1.00f, 0.84f };

        for (int i = 0; i < meshes.Length; i++)
        {
            CreateSingleBranchArc(
                parent,
                "PLS_BackwardBranch_0" + (i + 1),
                origins[i],
                angles[i],
                delays[i],
                lifetimes[i],
                sizes[i],
                material,
                meshes[i],
                sortingOrder + i);
        }
    }

    private static void CreateSingleBranchArc(
        Transform parent,
        string name,
        Vector3 localPosition,
        float angle,
        float delay,
        float lifetime,
        float size,
        Material material,
        Mesh mesh,
        int sortingOrder)
    {
        GameObject go = NewParticle(
            parent,
            name,
            localPosition,
            Quaternion.Euler(0f, 0f, angle));
        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.46f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = 0f;
        main.startColor = new Color(0.88f, 0.48f, 1.00f, 0.82f);
        main.maxParticles = 1;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(
            new Color(0.82f, 0.34f, 1.00f, 0.82f),
            0.78f,
            0.06f,
            0.38f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateMicroSparks(
        Transform parent,
        string name,
        Material material,
        int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, Vector3.zero, Quaternion.identity);
        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.48f;
        main.startDelay = 0.150f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.075f, 0.190f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.058f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.34f, 0.92f, 1.00f, 0.13f),
            new Color(0.96f, 0.70f, 1.00f, 0.92f));
        main.maxParticles = 16;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.10f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-8.2f, -3.4f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.6f, 1.4f);
        velocity.z = 0f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(
            new Color(0.90f, 0.52f, 1.00f, 0.82f),
            0.82f,
            0.05f,
            0.36f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 0.45f;
        renderer.velocityScale = 0.035f;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateContactNeedle(
        Transform parent,
        string name,
        Material material,
        Mesh mesh,
        int sortingOrder)
    {
        GameObject go = NewParticle(
            parent,
            name,
            new Vector3(-0.06f, 0.006f, 0f),
            Quaternion.Euler(0f, 0f, -7f));
        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.40f;
        main.startDelay = 0.150f;
        main.startLifetime = 0.125f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startColor = new Color(1.00f, 0.91f, 1.00f, 0.90f);
        main.maxParticles = 1;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule contactShape = particle.shape;
        contactShape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particle.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0.00f, 0.06f),
                new Keyframe(0.18f, 0.82f),
                new Keyframe(0.24f, 1.00f),
                new Keyframe(0.36f, 0.76f),
                new Keyframe(0.60f, 0.35f),
                new Keyframe(1.00f, 0.02f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateContactNeedleGradient();

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.sortingOrder = sortingOrder;
    }

    private static ParticleSystem.MinMaxGradient CreateContactNeedleGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1.00f, 0.90f, 1.00f), 0f),
                new GradientColorKey(new Color(1.00f, 0.90f, 1.00f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.70f, 0.12f),
                new GradientAlphaKey(0.94f, 0.24f),
                new GradientAlphaKey(0.68f, 0.36f),
                new GradientAlphaKey(0.35f, 0.60f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(
        Color color,
        float peakAlpha,
        float peakTime,
        float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(color.r, color.g, color.b), 0f),
                new GradientColorKey(new Color(color.r, color.g, color.b), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.72f, holdTime),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void RenderPreviewPrepared()
    {
        string previewDir = GetPreviewDirectory();
        Directory.CreateDirectory(previewDir);
        for (int i = 0; i < PreviewFiles.Length; i++)
        {
            string previewPath = Path.Combine(previewDir, PreviewFiles[i]);
            if (File.Exists(previewPath))
            {
                File.Delete(previewPath);
            }
        }

        string evidencePath = Path.Combine(previewDir, NativeEvidenceFileName);
        if (File.Exists(evidencePath))
        {
            File.Delete(evidencePath);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new Exception("Preview prefab is missing: " + PrefabPath);
        }

        CapturePreviewSet(
            previewDir,
            prefab,
            "black",
            new Color(0.006f, 0.006f, 0.010f, 1f),
            false);
        CapturePreviewSet(
            previewDir,
            prefab,
            "stage",
            new Color(0.205f, 0.215f, 0.225f, 1f),
            true);
        Debug.Log("Plasma lightning slash previews complete: " + previewDir);
    }

    private static void CapturePreviewSet(
        string previewDir,
        GameObject prefab,
        string prefix,
        Color background,
        bool stage)
    {
        GameObject root = UnityEngine.Object.Instantiate(prefab);
        root.name = PrefabName + "_" + prefix + "_Preview";
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        PlasmaLightningSlashDriver driver = root.GetComponent<PlasmaLightningSlashDriver>();
        if (driver == null)
        {
            UnityEngine.Object.DestroyImmediate(root);
            throw new Exception("Preview instance has no PlasmaLightningSlashDriver.");
        }
        driver.enabled = false;

        Camera camera = CreatePreviewCamera(background);
        GameObject stageRoot = stage ? CreatePreviewStage() : null;

        CapturePreviewState(camera, root, previewDir, prefix + "_01_early_reveal.png", 0.065f);
        CapturePreviewState(camera, root, previewDir, prefix + "_02_contact_peak.png", 0.180f);
        CapturePreviewState(camera, root, previewDir, prefix + "_03_full_body.png", 0.225f);
        CapturePreviewState(camera, root, previewDir, prefix + "_04_fade_retreat.png", 0.410f);

        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        if (stageRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(stageRoot);
        }
    }

    private static void CapturePreviewState(
        Camera camera,
        GameObject root,
        string previewDir,
        string fileName,
        float sampleTime)
    {
        PlasmaLightningSlashDriver driver = root.GetComponent<PlasmaLightningSlashDriver>();
        driver.ApplyAt(sampleTime);

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        List<string> rendererModes = new List<string>(particles.Length);
        int enabledRendererCount = 0;
        int aliveParticleCount = 0;
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.randomSeed = (uint)(1701 + i * 97);
            particle.useAutoRandomSeed = false;
            particle.Simulate(sampleTime, true, true, true);
            aliveParticleCount += particle.particleCount;

            ParticleSystemRenderer particleRenderer = particle.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.enabled = true;
                enabledRendererCount++;
                rendererModes.Add(particleRenderer.renderMode.ToString());
            }
        }

        AppendNativeParticleEvidence(
            previewDir,
            fileName,
            sampleTime,
            particles.Length,
            enabledRendererCount,
            aliveParticleCount,
            rendererModes);

        string path = Path.Combine(previewDir, fileName);
        CaptureCamera(camera, path, 1280, 720);
        Debug.Log("Plasma preview frame: " + path);
    }

    private static void AppendNativeParticleEvidence(
        string previewDir,
        string fileName,
        float sampleTime,
        int rendererCount,
        int enabledRendererCount,
        int aliveParticleCount,
        List<string> rendererModes)
    {
        string line = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0}|time={1:0.000}|nativeParticleRenderers={2}|enabled={3}|alive={4}|modes={5}",
            fileName,
            sampleTime,
            rendererCount,
            enabledRendererCount,
            aliveParticleCount,
            string.Join(",", rendererModes.ToArray()));
        File.AppendAllText(
            Path.Combine(previewDir, "native_particle_evidence.txt"),
            line + Environment.NewLine);
        Debug.Log("Native particle preview evidence: " + line);
    }

    private static Camera CreatePreviewCamera(Color background)
    {
        GameObject cameraObject = new GameObject("PlasmaLightningSlashPreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.orthographic = true;
        camera.orthographicSize = 3.25f;
        camera.transform.position = new Vector3(0.30f, 0.12f, -10f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static GameObject CreatePreviewStage()
    {
        GameObject root = new GameObject("PlasmaLightningSlashCombatGrayStage");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = "MutedCombatFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0f, -1.72f, 2f);
        floor.transform.localScale = new Vector3(12f, 1.18f, 1f);
        floor.GetComponent<Renderer>().sharedMaterial = CreatePreviewColorMaterial(
            new Color(0.105f, 0.115f, 0.125f, 1f));

        GameObject caster = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caster.name = "CasterScaleSilhouette";
        caster.transform.SetParent(root.transform, false);
        caster.transform.localPosition = new Vector3(-4.42f, -0.62f, 1.20f);
        caster.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
        caster.transform.localScale = new Vector3(0.34f, 1.78f, 0.18f);
        caster.GetComponent<Renderer>().sharedMaterial = CreatePreviewColorMaterial(
            new Color(0.050f, 0.052f, 0.060f, 1f));

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "TargetScaleSilhouette";
        target.transform.SetParent(root.transform, false);
        target.transform.localPosition = new Vector3(4.58f, -0.52f, 1.20f);
        target.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        target.transform.localScale = new Vector3(0.36f, 1.92f, 0.18f);
        target.GetComponent<Renderer>().sharedMaterial = CreatePreviewColorMaterial(
            new Color(0.044f, 0.046f, 0.052f, 1f));

        return root;
    }

    private static Material CreatePreviewColorMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        material.color = color;
        return material;
    }

    private static void CaptureCamera(Camera camera, string path, int width, int height)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture oldActive = RenderTexture.active;
        RenderTexture oldTarget = camera.targetTexture;

        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        camera.Render();
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());

        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(renderTexture);
    }

    private static string GetPreviewDirectory()
    {
        DirectoryInfo current = new DirectoryInfo(Application.dataPath);
        while (current != null && !string.Equals(current.Name, "lor", StringComparison.OrdinalIgnoreCase))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Preview", "plasma_lightning_slash"));
        }

        return Path.Combine(current.FullName, "preview_exports", "plasma_lightning_slash");
    }

    private static void VerifyPrefabAsset()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new Exception("Verify failed: prefab missing " + PrefabPath);
        }

        VerifyPrefabObject(prefab, "project prefab");
    }

    private static void VerifyPrefabObject(GameObject prefab, string label)
    {
        Transform bladeRoot = prefab.transform.Find("AB_BladeRoot");
        Transform impactRoot = prefab.transform.Find("AB_ImpactRoot");
        if (bladeRoot == null || impactRoot == null)
        {
            throw new Exception("Verify failed: " + label + " lacks AB_BladeRoot or AB_ImpactRoot.");
        }

        if (prefab.GetComponentsInChildren<TrailRenderer>(true).Length != 0)
        {
            throw new Exception("Verify failed: instant slash must not contain a TrailRenderer.");
        }

        MeshRenderer[] mainRenderers = bladeRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (mainRenderers.Length != 1)
        {
            throw new Exception(
                "Verify failed: main slash must be one continuous mesh renderer, got "
                + mainRenderers.Length);
        }

        MeshFilter filter = mainRenderers[0].GetComponent<MeshFilter>();
        Material mainMaterial = mainRenderers[0].sharedMaterial;
        if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount != 138)
        {
            throw new Exception("Verify failed: 68-segment main ribbon mesh is missing or malformed.");
        }
        if (mainMaterial == null
            || mainMaterial.shader == null
            || mainMaterial.shader.name != ShaderName
            || mainMaterial.GetTexture("_SlashTex") == null
            || mainMaterial.GetTexture("_NoiseTex") == null)
        {
            throw new Exception("Verify failed: main flow shader or procedural textures are missing.");
        }

        ParticleSystem[] particles = impactRoot.GetComponentsInChildren<ParticleSystem>(true);
        if (particles.Length != 6)
        {
            throw new Exception("Verify failed: expected six native support particle systems, got " + particles.Length);
        }

        int meshRendererCount = 0;
        int stretchRendererCount = 0;
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystemRenderer renderer = particles[i].GetComponent<ParticleSystemRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                throw new Exception("Verify failed: support renderer is empty on " + particles[i].name);
            }
            if (renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh == null)
            {
                throw new Exception("Verify failed: mesh support renderer has no mesh on " + particles[i].name);
            }
            if (renderer.renderMode == ParticleSystemRenderMode.Mesh)
            {
                meshRendererCount++;
            }
            else if (renderer.renderMode == ParticleSystemRenderMode.Stretch)
            {
                stretchRendererCount++;
            }
        }
        if (meshRendererCount != 5 || stretchRendererCount != 1)
        {
            throw new Exception(
                "Verify failed: expected five Mesh and one Stretch support renderer, got Mesh="
                + meshRendererCount
                + " Stretch="
                + stretchRendererCount);
        }

        Debug.Log(
            "Verify plasma slash object passed: "
            + label
            + " mainRenderers="
            + mainRenderers.Length
            + " supportParticles="
            + particles.Length);
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
                throw new Exception("Verify failed: bundled prefab missing " + PrefabName);
            }
            VerifyPrefabObject(prefab, "bundled prefab");

            string previewDir = GetPreviewDirectory();
            for (int i = 0; i < PreviewFiles.Length; i++)
            {
                string path = Path.Combine(previewDir, PreviewFiles[i]);
                if (!File.Exists(path) || new FileInfo(path).Length < 12000)
                {
                    throw new Exception("Verify failed: preview missing or too small " + path);
                }
            }

            string nativeEvidencePath = Path.Combine(previewDir, NativeEvidenceFileName);
            if (!File.Exists(nativeEvidencePath))
            {
                throw new Exception("Verify failed: native particle evidence is missing " + nativeEvidencePath);
            }

            Debug.Log(
                "Verify plasma lightning slash bundle passed: prefab="
                + PrefabName
                + " previews="
                + PreviewFiles.Length
                + " bundle="
                + bundlePath);
        }
        finally
        {
            bundle.Unload(false);
        }
    }

    private static Texture2D CreateProceduralMaskTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);

        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            float widthCoord = Mathf.Abs(v - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float endFade = Smoother01(0.00f, 0.012f, u)
                    * (1f - Smoother01(0.985f, 1.00f, u));
                float envelope = (1f - Smoother01(0.70f, 1.00f, widthCoord)) * endFade;
                float connectedCore = Mathf.Exp(-Mathf.Pow((v - 0.50f) / 0.070f, 2f)) * endFade;

                float trailingWeight = 1f - Smoother01(0.22f, 0.31f, u);
                float fiberA = Mathf.Exp(-Mathf.Pow(
                    (v - (0.39f + Mathf.Sin(u * 7.2f) * 0.025f)) / 0.030f,
                    2f));
                float fiberB = Mathf.Exp(-Mathf.Pow(
                    (v - (0.53f + Mathf.Sin(u * 9.1f + 1.7f) * 0.030f)) / 0.026f,
                    2f));
                float fiberC = Mathf.Exp(-Mathf.Pow(
                    (v - (0.66f + Mathf.Sin(u * 6.4f + 0.4f) * 0.020f)) / 0.028f,
                    2f));
                float fibers = Mathf.Clamp01(Mathf.Max(fiberA, Mathf.Max(fiberB, fiberC)))
                    * trailingWeight
                    * envelope;

                float broad = Mathf.PerlinNoise(u * 4.5f + 4.1f, v * 3.2f + 8.2f);
                float fine = Mathf.PerlinNoise(u * 9.0f + 12.7f, v * 2.1f + 1.8f);
                float abrasion = Smoother01(0.68f, 0.92f, broad * 0.58f + fine * 0.42f);
                abrasion *= envelope * (1f - connectedCore * 0.82f);

                pixels[y * width + x] = new Color(
                    Mathf.Clamp01(connectedCore),
                    Mathf.Clamp01(fibers),
                    Mathf.Clamp01(abrasion),
                    Mathf.Clamp01(envelope));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateDirectionalNoiseTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float broad = Mathf.PerlinNoise(u * 2.4f + 2.2f, v * 3.2f + 7.8f);
                float fiberPhase = v * 8.5f + u * 1.45f + (broad - 0.5f) * 1.75f;
                float streak = Mathf.Pow(
                    Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin(fiberPhase * Mathf.PI))),
                    5.0f);
                float sparks = Mathf.PerlinNoise(u * 11.0f + 11.3f, v * 13.0f + 1.6f);
                texture.SetPixel(x, y, new Color(broad, streak, sparks, 1f));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateSupportLineTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            float cross = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.18f, 2f));
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float lengthFade = Smoother01(0f, 0.08f, u)
                    * (1f - Smoother01(0.66f, 1f, u));
                float alpha = cross * lengthFade;
                pixels[y * width + x] = new Color(1f, 0.86f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateMicroSparkTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float line = Mathf.Exp(-Mathf.Pow(v / 0.075f, 2f))
                    * (1f - Smoother01(0.62f, 1f, Mathf.Abs(u)));
                float core = Mathf.Exp(-Mathf.Pow(u / 0.18f, 2f) - Mathf.Pow(v / 0.18f, 2f));
                float alpha = Mathf.Clamp01(line + core * 0.46f);
                pixels[y * width + x] = new Color(1f, 0.92f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Color[] ClearPixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0f, 0f, 0f, 0f);
        }
        return pixels;
    }

    private static void SavePng(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static float Smoother01(float edge0, float edge1, float value)
    {
        if (Mathf.Approximately(edge0, edge1))
        {
            return value >= edge1 ? 1f : 0f;
        }

        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
