using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SwordSlashPrefabBundleBuilderTemplate
{
    private const string RootDir = "Assets/SkillSwordSlash";
    private const string BundleName = "skill_ocean_bluewhite_slash";
    private const string PrefabName = "OceanBlueWhiteSlashPrefab";
    private const string PrefabPath = RootDir + "/Prefabs/OceanBlueWhiteSlashPrefab.prefab";

    [MenuItem("Skill VFX/Create Ocean BlueWhite Slash Prefab")]
    public static void EnsurePrefab()
    {
        EnsureDirectories();
        GenerateTextures();
        AssetDatabase.Refresh();

        Mesh outerMesh = CreateOrReplaceMesh(RootDir + "/Meshes/OceanSlash_OuterArc.asset", CreateArcMesh(-118f, 118f, 1.05f, 1.70f, 88, 0.34f, 0.05f));
        Mesh coreMesh = CreateOrReplaceMesh(RootDir + "/Meshes/OceanSlash_WhiteCore.asset", CreateArcMesh(-108f, 112f, 1.20f, 1.42f, 82, 0.48f, 0.02f));
        Mesh foamMesh = CreateOrReplaceMesh(RootDir + "/Meshes/OceanSlash_FoamRim.asset", CreateArcMesh(-122f, 116f, 1.58f, 1.86f, 72, 0.58f, 0.07f));

        Material outerMat = CreateFlowMaterial("Ocean_OuterCyan_Add", new Color(0.08f, 0.72f, 1.75f, 0.68f), new Color(0.70f, 1.0f, 1.0f, 1f), new Color(0.02f, 0.42f, 1.25f, 1f), 0.55f, 1.65f);
        Material coreMat = CreateFlowMaterial("Ocean_WhiteCore_Add", new Color(0.92f, 1.0f, 1.0f, 0.98f), new Color(1.5f, 1.85f, 2.0f, 1f), new Color(0.36f, 0.95f, 1.8f, 1f), 0.34f, 2.85f);
        Material foamMat = CreateFlowMaterial("Ocean_FoamRim_Add", new Color(0.68f, 0.96f, 1.0f, 0.58f), new Color(1.2f, 1.65f, 1.75f, 1f), new Color(0.12f, 0.72f, 1.35f, 1f), 0.62f, 1.25f);
        Material sparkMat = CreateParticleMaterial("Ocean_FoamSpark_Add", RootDir + "/Textures/ocean_soft_spark.png", new Color(0.76f, 0.98f, 1f, 0.82f), false);
        Material mistMat = CreateParticleMaterial("Ocean_Mist_Alpha", RootDir + "/Textures/ocean_soft_spark.png", new Color(0.18f, 0.68f, 1f, 0.30f), true);

        GameObject root = CreateParticleRoot(PrefabName);
        SwordSlashRevealAnimator animator = root.AddComponent<SwordSlashRevealAnimator>();
        animator.duration = 0.82f;
        animator.revealDelay = 0.02f;
        animator.revealDuration = 0.28f;
        animator.holdDuration = 0.10f;
        animator.fadeDuration = 0.36f;

        Transform arcRoot = CreateGroup(root.transform, "OceanSlash_ArcRoot", Vector3.zero, Quaternion.Euler(0f, 0f, -8f), new Vector3(3.2f, 2.0f, 1f));
        CreateArcParticleLayer(arcRoot, "Outer_Cyan_WaveBody_PS", outerMat, outerMesh, Vector3.zero, Quaternion.identity, 0.000f, 0.74f, 1.00f, new Color(0.14f, 0.76f, 1f, 0.76f), 1, true);
        CreateArcParticleLayer(arcRoot, "White_Core_LeadingEdge_PS", coreMat, coreMesh, new Vector3(0.10f, 0.02f, -0.03f), Quaternion.Euler(0f, 0f, -1.5f), 0.025f, 0.52f, 1.02f, new Color(0.96f, 1f, 1f, 0.98f), 5, true);
        CreateArcParticleLayer(arcRoot, "Foam_Rim_Breakup_PS", foamMat, foamMesh, new Vector3(-0.06f, 0.06f, 0.02f), Quaternion.Euler(0f, 0f, 1.8f), 0.045f, 0.64f, 1.03f, new Color(0.70f, 0.98f, 1f, 0.70f), 4, true);
        CreateArcParticleLayer(arcRoot, "Back_Echo_DeepBlue_PS", outerMat, outerMesh, new Vector3(-0.22f, 0.18f, 0.05f), Quaternion.Euler(0f, 0f, -11f), 0.075f, 0.58f, 0.92f, new Color(0.04f, 0.34f, 1f, 0.42f), 0, false);

        Transform particlesRoot = CreateGroup(root.transform, "OceanSlash_Particles", Vector3.zero, Quaternion.identity, Vector3.one);
        CreateStretchParticle(particlesRoot, "LeadingEdge_WhiteSpeedLines_PS", sparkMat, new Vector3(3.65f, 0.28f, -0.09f), -10f, 0.020f, 14, 0.06f, 0.20f, 4.8f, 11.0f, 2.8f, 7);
        CreateSprayParticle(particlesRoot, "LeadingEdge_FoamSpray", sparkMat, new Vector3(4.9f, 0.18f, -0.08f), -8f, 0.04f, 26, 0.07f, 0.24f, 5.2f, 12.0f, 4);
        CreateSprayParticle(particlesRoot, "Trailing_Bubbles", sparkMat, new Vector3(0.65f, 0.18f, -0.02f), -26f, 0.08f, 18, 0.04f, 0.16f, 1.2f, 5.4f, 2);
        CreateMistParticle(particlesRoot, "Blue_WaterMist_Backwash", mistMat, new Vector3(0.1f, 0.12f, 0.05f), 0.05f, 7, 0.8f, 2.2f, 1);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        VerifyPrefab();
        Debug.Log("Created complete slash prefab: " + PrefabPath);
    }

    [MenuItem("Skill VFX/Build Ocean BlueWhite Slash Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        VerifyBundle(output);
        Debug.Log("Built AssetBundle: " + Path.Combine(output, BundleName));
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDir);
        Directory.CreateDirectory(RootDir + "/Meshes");
        Directory.CreateDirectory(RootDir + "/Materials");
        Directory.CreateDirectory(RootDir + "/Prefabs");
        Directory.CreateDirectory(RootDir + "/Textures");
    }

    private static void GenerateTextures()
    {
        SaveTexture(RootDir + "/Textures/ocean_slash_mask.png", CreateSlashMaskTexture(1024, 256, true), TextureWrapMode.Clamp);
        SaveTexture(RootDir + "/Textures/ocean_noise.png", CreateNoiseTexture(512, 512, 18.0f, 7.7f), TextureWrapMode.Repeat);
        SaveTexture(RootDir + "/Textures/ocean_soft_spark.png", CreateSoftSparkTexture(256, 256), TextureWrapMode.Clamp);
    }

    private static Texture2D CreateSlashMaskTexture(int width, int height, bool brushBreakup)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float center = 1f - Mathf.Abs(v - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float taper = 1f - Mathf.SmoothStep(0.50f, 1.0f, u) * 0.84f;
                float body = Mathf.SmoothStep(0.02f, 0.16f, u) * (1f - Mathf.SmoothStep(0.92f, 1.0f, u));
                float alpha = body * Mathf.SmoothStep(0.02f, 0.72f, center / Mathf.Max(taper, 0.04f));
                if (brushBreakup)
                {
                    float n = Mathf.PerlinNoise(x * 0.020f + 11.3f, y * 0.045f + 5.1f);
                    alpha *= Mathf.SmoothStep(0.10f, 0.82f, alpha + (n - 0.5f) * 0.24f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateNoiseTexture(int width, int height, float scale, float seed)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)width;
                float ny = y / (float)height;
                float n = Mathf.PerlinNoise(nx * scale + seed, ny * scale + seed);
                n = n * 0.68f + Mathf.PerlinNoise(nx * scale * 3.7f + seed * 2.0f, ny * scale * 1.2f + seed * 0.3f) * 0.32f;
                float streak = Mathf.Sin(nx * Mathf.PI * 36f + ny * 4.5f) * 0.5f + 0.5f;
                float value = Mathf.Clamp01(n * 0.82f + Mathf.Pow(streak, 6f) * 0.18f);
                tex.SetPixel(x, y, new Color(value, value, value, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateSoftSparkTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x / (float)(width - 1) - 0.5f;
                float dy = y / (float)(height - 1) - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float alpha = Mathf.Pow(1f - Mathf.SmoothStep(0f, 1f, r), 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private static void SaveTexture(string path, Texture2D texture, TextureWrapMode wrapMode)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrapMode;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static Material CreateFlowMaterial(string name, Color tint, Color core, Color edge, float length, float baseIntensity)
    {
        string path = RootDir + "/Materials/" + name + ".mat";
        Shader shader = Shader.Find("Skill/SwordSlashFlowBuiltIn") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(RootDir + "/Textures/ocean_slash_mask.png");
        Texture2D noise = AssetDatabase.LoadAssetAtPath<Texture2D>(RootDir + "/Textures/ocean_noise.png");
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mask);
        if (mat.HasProperty("_NoiseTex")) mat.SetTexture("_NoiseTex", noise);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", tint);
        if (mat.HasProperty("_CoreColor")) mat.SetColor("_CoreColor", core);
        if (mat.HasProperty("_EdgeColor")) mat.SetColor("_EdgeColor", edge);
        if (mat.HasProperty("_Length")) mat.SetFloat("_Length", length);
        if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", baseIntensity);
        if (mat.HasProperty("_NoiseTiling")) mat.SetVector("_NoiseTiling", new Vector4(1.25f, 1.0f, 0f, 0f));
        if (mat.HasProperty("_NoiseSpeed")) mat.SetFloat("_NoiseSpeed", 2.35f);
        if (mat.HasProperty("_EdgeFade")) mat.SetVector("_EdgeFade", new Vector4(2.45f, 2.15f, 0f, 0f));
        mat.renderQueue = 3100;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateParticleMaterial(string name, string texturePath, Color tint, bool alphaBlend)
    {
        string path = RootDir + "/Materials/" + name + ".mat";
        Shader shader = alphaBlend
            ? (Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended"))
            : (Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive"));
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        mat.color = tint;
        mat.renderQueue = alphaBlend ? 3000 : 3150;
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

    private static Mesh CreateArcMesh(float startDeg, float endDeg, float innerRadius, float outerRadius, int segments, float taper, float wave)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            float widthScale = 1f - taper * Mathf.Abs(t - 0.5f) * 2f;
            float mid = (innerRadius + outerRadius) * 0.5f;
            float half = (outerRadius - innerRadius) * 0.5f * Mathf.Max(0.10f, widthScale);
            float wobble = Mathf.Sin(t * Mathf.PI * 3f) * wave;
            float inner = mid - half + wobble;
            float outer = mid + half + wobble;

            vertices.Add(new Vector3(Mathf.Cos(angle) * inner, Mathf.Sin(angle) * inner, 0f));
            vertices.Add(new Vector3(Mathf.Cos(angle) * outer, Mathf.Sin(angle) * outer, 0f));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i < segments)
            {
                int a = i * 2;
                triangles.Add(a);
                triangles.Add(a + 2);
                triangles.Add(a + 1);
                triangles.Add(a + 1);
                triangles.Add(a + 2);
                triangles.Add(a + 3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Ocean Sword Slash Arc";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static GameObject CreateParticleRoot(string name)
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

    private static GameObject NewParticle(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = Vector3.one;
        go.AddComponent<ParticleSystem>();
        return go;
    }

    private static Transform CreateGroup(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        GameObject go = new GameObject(name);
        Transform t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPosition;
        t.localRotation = localRotation;
        t.localScale = localScale;
        return t;
    }

    private static void CreateMeshLayer(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateArcParticleLayer(Transform parent, string name, Material material, Mesh mesh, Vector3 localPosition, Quaternion localRotation, float delay, float lifetime, float size, Color color, int sortingOrder, bool scaleOverLifetime)
    {
        GameObject go = NewParticle(parent, name, localPosition, localRotation);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.16f;
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

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.08f, 0.58f);

        if (scaleOverLifetime)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.28f), new Keyframe(0.16f, 1.08f), new Keyframe(0.58f, 0.98f), new Keyframe(1f, 0.46f)));
        }

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sortingOrder = sortingOrder;
        renderer.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateSprayParticle(Transform parent, string name, Material material, Vector3 localPosition, float zRotation, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, localPosition, Quaternion.Euler(0f, 0f, zRotation));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.75f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.46f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.66f, 0.96f, 1f, 0.78f), new Color(1f, 1f, 1f, 0.98f));
        main.maxParticles = burst + 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        shape.arc = 165f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(minSpeed * 0.18f, maxSpeed * 0.28f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.4f, 1.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = CreateFadeGradient(new Color(0.76f, 0.98f, 1f, 0.95f), 0.95f, 0.12f, 0.56f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateStretchParticle(Transform parent, string name, Material material, Vector3 localPosition, float zRotation, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, float lengthScale, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, localPosition, Quaternion.Euler(0f, 0f, zRotation));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.62f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.66f, 0.94f, 1f, 0.82f), new Color(1f, 1f, 1f, 1f));
        main.maxParticles = burst + 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.45f, 0.28f, 0.04f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(1.8f, 5.4f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.8f, 0.9f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = CreateFadeGradient(new Color(0.82f, 0.98f, 1f, 0.92f), 0.92f, 0.08f, 0.46f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.14f;
        renderer.lengthScale = lengthScale;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateMistParticle(Transform parent, string name, Material material, Vector3 localPosition, float delay, short burst, float minSize, float maxSize, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, localPosition, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.95f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.44f, 0.82f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.82f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.18f, 0.68f, 1f, 0.26f);
        main.maxParticles = burst + 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.8f;
        shape.arc = 240f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.45f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.55f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = CreateFadeGradient(new Color(0.18f, 0.68f, 1f, 0.32f), 0.32f, 0.22f, 0.62f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.42f), new Keyframe(0.48f, 1f), new Keyframe(1f, 1.35f)));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color color, float alpha, float fadeIn, float fadeOut)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.25f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, fadeIn), new GradientAlphaKey(alpha * 0.7f, fadeOut), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void VerifyPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new Exception("Prefab was not created: " + PrefabPath);
        }

        int renderers = prefab.GetComponentsInChildren<Renderer>(true).Length;
        int particles = prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
        ParticleSystemRenderer[] particleRenderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
        int visibleParticleRenderers = 0;
        int meshParticleRenderers = 0;
        int stretchParticleRenderers = 0;
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            ParticleSystemRenderer renderer = particleRenderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            visibleParticleRenderers++;
            if (renderer.sharedMaterial == null)
            {
                throw new Exception("Particle renderer missing material: " + renderer.name);
            }
            if (renderer.renderMode == ParticleSystemRenderMode.Mesh)
            {
                meshParticleRenderers++;
                if (renderer.mesh == null)
                {
                    throw new Exception("Mesh particle renderer missing mesh: " + renderer.name);
                }
            }
            else if (renderer.renderMode == ParticleSystemRenderMode.Stretch)
            {
                stretchParticleRenderers++;
            }
        }

        foreach (ParticleSystem ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.transform == prefab.transform)
            {
                continue;
            }

            if (ps.emission.burstCount <= 0)
            {
                throw new Exception("ParticleSystem has no burst emission: " + ps.name);
            }
        }

        if (renderers < 8 || particles < 8 || visibleParticleRenderers < 7 || meshParticleRenderers < 4 || stretchParticleRenderers < 1)
        {
            throw new Exception("Prefab too sparse: renderers=" + renderers + " particles=" + particles + " visibleParticleRenderers=" + visibleParticleRenderers + " meshParticleRenderers=" + meshParticleRenderers + " stretchParticleRenderers=" + stretchParticleRenderers);
        }
    }

    private static void VerifyBundle(string output)
    {
        string path = Path.Combine(output, BundleName);
        if (!File.Exists(path))
        {
            path = Path.Combine(output, BundleName + ".ab");
        }
        if (!File.Exists(path))
        {
            throw new Exception("Bundle missing: " + path);
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            throw new Exception("Unable to load bundle: " + path);
        }

        try
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (prefab == null)
            {
                throw new Exception("Prefab missing from bundle: " + PrefabName);
            }
        }
        finally
        {
            bundle.Unload(false);
        }
    }
}
