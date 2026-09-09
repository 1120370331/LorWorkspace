using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnhierBlueWhiteSlashRefinedBundleBuilder
{
    private const string BundleName = "steria_anhier_bluewhite_slash_refined";
    private const string PrefabName = "AnhierBlueWhiteSlashRefinedPrefab";
    private const string PrefabPath = "Assets/Prefabs/AnhierBlueWhiteSlashRefinedPrefab.prefab";
    private const string ShaderPath = "Assets/Shaders/AnhierBlueWhiteSlashRefinedFlow.shader";
    private const string FlowShaderName = "Steria/AnhierBlueWhiteSlashRefinedFlow";

    [MenuItem("Steria/Build Anhier BlueWhite Slash Refined Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        RenderPreview();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        VerifyBuiltBundle(output);
        Debug.Log("Built refined Anhier blue-white slash AssetBundle: " + output);
    }

    public static void EnsurePrefab()
    {
        Directory.CreateDirectory("Assets/Animations");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Shaders");
        Directory.CreateDirectory("Assets/Textures");
        Directory.CreateDirectory("Assets/Textures/Generated");

        GenerateShader();
        GenerateTextures();
        AssetDatabase.Refresh();

        Mesh coreMesh = CreateOrReplaceMesh(
            "Assets/Meshes/AnhierBlueWhiteSlashRefined_CoreMesh.asset",
            CreateRefinedSlashMesh("AnhierBlueWhiteSlashRefined_CoreMesh", 0.72f, 0.00f, 48));
        Mesh rimMesh = CreateOrReplaceMesh(
            "Assets/Meshes/AnhierBlueWhiteSlashRefined_RimMesh.asset",
            CreateRefinedSlashMesh("AnhierBlueWhiteSlashRefined_RimMesh", 1.02f, -0.020f, 48));
        Mesh wakeMesh = CreateOrReplaceMesh(
            "Assets/Meshes/AnhierBlueWhiteSlashRefined_WakeMesh.asset",
            CreateRefinedSlashMesh("AnhierBlueWhiteSlashRefined_WakeMesh", 0.58f, 0.055f, 48));

        Material core = CreateFlowMaterial(
            "Assets/Materials/AnhierBlueWhiteSlashRefined_Core.mat",
            new Color(0.93f, 1.00f, 1.00f, 1.00f),
            new Color(0.28f, 0.82f, 1.00f, 0.88f),
            0.62f,
            2.42f,
            0.86f);
        Material rim = CreateFlowMaterial(
            "Assets/Materials/AnhierBlueWhiteSlashRefined_CyanRim.mat",
            new Color(0.52f, 0.94f, 1.00f, 0.88f),
            new Color(0.02f, 0.34f, 1.00f, 0.72f),
            0.70f,
            1.92f,
            0.88f);
        Material wake = CreateFlowMaterial(
            "Assets/Materials/AnhierBlueWhiteSlashRefined_WhiteWake.mat",
            new Color(1.00f, 1.00f, 1.00f, 0.78f),
            new Color(0.18f, 0.72f, 1.00f, 0.42f),
            0.78f,
            1.18f,
            0.72f);
        Material spark = CreateParticleMaterial(
            "Assets/Textures/Generated/anhier_bluewhite_refined_spark.png",
            "Assets/Materials/AnhierBlueWhiteSlashRefined_Spark.mat",
            new Color(0.82f, 0.97f, 1.00f, 0.95f),
            false);
        Material mist = CreateParticleMaterial(
            "Assets/Textures/Generated/anhier_bluewhite_refined_mist.png",
            "Assets/Materials/AnhierBlueWhiteSlashRefined_Mist.mat",
            new Color(0.24f, 0.74f, 1.00f, 0.36f),
            true);
        Material impact = CreateParticleMaterial(
            "Assets/Textures/Generated/anhier_bluewhite_refined_impact.png",
            "Assets/Materials/AnhierBlueWhiteSlashRefined_Impact.mat",
            new Color(0.88f, 0.99f, 1.00f, 0.90f),
            false);

        GameObject root = CreateRoot(PrefabName);
        AnhierBlueWhiteSlashRefinedDriver driver = root.AddComponent<AnhierBlueWhiteSlashRefinedDriver>();
        driver.duration = 0.62f;
        driver.loop = false;

        Transform slashRoot = CreateGroup(root.transform, "ABR_TargetMeshSlash", Vector3.zero);
        Transform supportRoot = CreateGroup(root.transform, "ABR_ParticleSupportOnly", Vector3.zero);

        CreateMeshLayer(slashRoot, "ABR_CyanPressureRim", rimMesh, rim, new Vector3(0f, 0f, 0.025f), 0f, 1, new Color(0.55f, 0.92f, 1f, 0.86f));
        CreateMeshLayer(slashRoot, "ABR_WhiteHotCore", coreMesh, core, new Vector3(0.04f, 0.02f, -0.010f), 0f, 3, Color.white);
        CreateMeshLayer(slashRoot, "ABR_InnerRetreatWake", wakeMesh, wake, new Vector3(-0.18f, -0.04f, -0.025f), -1.4f, 2, new Color(0.84f, 0.98f, 1f, 0.72f));

        CreateImpactFlash(supportRoot, "ABR_ContactPeakFlash", impact, new Vector3(3.86f, 1.38f, -0.06f), 0.13f, 0.16f, 1.05f, new Color(0.86f, 0.98f, 1f, 0.48f), 5);
        CreateSpeedLines(supportRoot, "ABR_LeadingSpeedLines", spark, new Vector3(2.75f, 1.10f, -0.04f), -22f, 0.060f, 10, 0.025f, 0.075f, 4.4f, 8.0f, 1.55f, 4);
        CreateSparkBurst(supportRoot, "ABR_BlueWhiteContactSparks", spark, new Vector3(3.78f, 1.43f, -0.05f), 0.12f, 16, 0.035f, 0.13f, 2.2f, 5.4f, 7);
        CreateMistBackwash(supportRoot, "ABR_ColdMistBackwash", mist, new Vector3(-0.55f, -0.28f, 0.05f), 0.18f, 6, 0.55f, 1.25f, 0.8f, 2.6f, 0);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();

        Debug.Log("Prepared refined mesh/shader slash prefab: " + PrefabPath);
    }

    public static void RenderPreview()
    {
        EnsurePrefab();

        string previewDir = GetPreviewDirectory();
        Directory.CreateDirectory(previewDir);
        foreach (string file in Directory.GetFiles(previewDir, "*.png"))
        {
            File.Delete(file);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new Exception("Preview failed: missing prefab at " + PrefabPath);
        }

        GameObject root = UnityEngine.Object.Instantiate(prefab);
        root.name = PrefabName + "_PreviewInstance";
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        AnhierBlueWhiteSlashRefinedDriver driver = root.GetComponent<AnhierBlueWhiteSlashRefinedDriver>();
        if (driver != null)
        {
            driver.enabled = false;
        }

        Camera camera = CreatePreviewCamera();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

        CapturePreviewState(camera, renderers, particles, previewDir, "01_early_reveal.png", 0.08f, 0.30f, -0.08f, 0.58f, 0.78f, 1.38f);
        CapturePreviewState(camera, renderers, particles, previewDir, "02_contact_peak.png", 0.16f, 0.96f, -0.03f, 1.02f, 1.00f, 2.85f);
        CapturePreviewState(camera, renderers, particles, previewDir, "03_full_arc.png", 0.28f, 1.15f, -0.05f, 1.30f, 0.92f, 2.15f);
        CapturePreviewState(camera, renderers, particles, previewDir, "04_fade_retreat.png", 0.47f, 1.16f, 0.18f, 1.22f, 0.34f, 1.08f);

        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);

        Debug.Log("Refined slash preview complete: " + previewDir);
    }

    public static void VerifyBundle()
    {
        VerifyBuiltBundle(Path.GetFullPath("Assets/../AssetBundles"));
    }

    private static void GenerateShader()
    {
        File.WriteAllText(ShaderPath, @"Shader ""Steria/AnhierBlueWhiteSlashRefinedFlow""
{
    Properties
    {
        _SlashTex (""Slash Clamp Mask"", 2D) = ""white"" {}
        _NoiseTex (""Breakup Noise"", 2D) = ""white"" {}
        _TintColor (""Tint"", Color) = (1,1,1,1)
        _CoreColor (""Core Color"", Color) = (1,1,1,1)
        _EdgeColor (""Edge Color"", Color) = (0.1,0.7,1,1)
        _Reveal (""Reveal Head"", Float) = 0
        _Retreat (""Retreat Tail"", Float) = -0.1
        _Length (""Visible Length"", Float) = 0.7
        _Alpha (""Alpha"", Float) = 1
        _Intensity (""Intensity"", Float) = 2.2
        _EdgeFade (""Thickness Edge Fade"", Vector) = (0.08,0.10,0,0)
        _NoiseStrength (""Noise Strength"", Float) = 0.32
        _BrushStrength (""Brush Strength"", Float) = 0.36
        _FlowSpeed (""Flow Speed"", Float) = 1.15
    }

    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""IgnoreProjector""=""True"" ""RenderType""=""Transparent"" }
        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            sampler2D _SlashTex;
            sampler2D _NoiseTex;
            float4 _TintColor;
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Reveal;
            float _Retreat;
            float _Length;
            float _Alpha;
            float _Intensity;
            float4 _EdgeFade;
            float _NoiseStrength;
            float _BrushStrength;
            float _FlowSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float u = saturate(i.uv.x);
                float v = saturate(i.uv.y);
                float head = _Reveal;
                float tail = max(_Retreat, head - max(_Length, 0.02));
                float revealMask = smoothstep(tail, tail + 0.045, u) * (1.0 - smoothstep(head - 0.030, head + 0.026, u));
                float clampMask = step(0.0001, u) * step(u, 0.9999);
                float edgeFade = smoothstep(0.0, _EdgeFade.x, v) * (1.0 - smoothstep(1.0 - _EdgeFade.y, 1.0, v));
                float endFade = smoothstep(0.0, 0.025, u) * (1.0 - smoothstep(0.970, 1.0, u));

                float4 slash = tex2D(_SlashTex, float2(u, v));
                float2 noiseUv = float2(u * 2.35 - _Time.y * _FlowSpeed, v * 1.15 + _Time.y * 0.17);
                float noise = tex2D(_NoiseTex, noiseUv).r;
                float streak = tex2D(_NoiseTex, float2(u * 7.5 - _Time.y * _FlowSpeed * 1.8, v * 0.72 + 0.31)).g;
                float core = pow(saturate(1.0 - abs(v - 0.52) * 3.35), 2.65);
                float rim = pow(saturate(1.0 - abs(v - 0.76) * 5.8), 1.9);
                float leading = (1.0 - smoothstep(0.0, 0.034, abs(u - head))) * pow(saturate(1.0 - abs(v - 0.58) * 3.4), 1.8);
                float brush = lerp(1.0 - _BrushStrength, 1.18, slash.r);
                float breakup = lerp(1.0 - _NoiseStrength, 1.16, noise) + streak * 0.16;
                float innerBreak = lerp(0.54, 1.0, saturate(noise * 0.72 + slash.b * 0.54 + streak * 0.30));

                float alpha = slash.a * revealMask * edgeFade * endFade * clampMask;
                alpha *= saturate(brush * breakup);
                alpha *= lerp(1.0, innerBreak, core * 0.82);
                alpha = saturate(alpha + leading * slash.a * edgeFade * 0.26);

                float3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(core + slash.r * 0.55 + leading * 0.50));
                color = lerp(color, float3(0.10, 0.58, 1.0), rim * 0.24);
                color *= _TintColor.rgb * _Intensity * (0.74 + noise * 0.22 + streak * 0.18 + leading * 0.46);

                return fixed4(color, alpha * _Alpha * _TintColor.a * i.color.a);
            }
            ENDCG
        }
    }

    Fallback ""Legacy Shaders/Particles/Additive""
}
");
    }

    private static void GenerateTextures()
    {
        SavePng("Assets/Textures/Generated/anhier_bluewhite_refined_slash_mask.png", CreateSlashMaskTexture(1024, 256));
        SavePng("Assets/Textures/Generated/anhier_bluewhite_refined_noise.png", CreateNoiseTexture(512, 512));
        SavePng("Assets/Textures/Generated/anhier_bluewhite_refined_spark.png", CreateSparkTexture(256, 256));
        SavePng("Assets/Textures/Generated/anhier_bluewhite_refined_mist.png", CreateMistTexture(512, 512));
        SavePng("Assets/Textures/Generated/anhier_bluewhite_refined_impact.png", CreateImpactTexture(512, 512));
        AssetDatabase.Refresh();

        ConfigureTexture("Assets/Textures/Generated/anhier_bluewhite_refined_slash_mask.png", TextureWrapMode.Clamp);
        ConfigureTexture("Assets/Textures/Generated/anhier_bluewhite_refined_noise.png", TextureWrapMode.Repeat);
        ConfigureTexture("Assets/Textures/Generated/anhier_bluewhite_refined_spark.png", TextureWrapMode.Clamp);
        ConfigureTexture("Assets/Textures/Generated/anhier_bluewhite_refined_mist.png", TextureWrapMode.Clamp);
        ConfigureTexture("Assets/Textures/Generated/anhier_bluewhite_refined_impact.png", TextureWrapMode.Clamp);
    }

    private static void ConfigureTexture(string assetPath, TextureWrapMode wrap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = wrap;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Material CreateFlowMaterial(string materialPath, Color coreColor, Color edgeColor, float length, float intensity, float brushStrength)
    {
        Shader shader = Shader.Find(FlowShaderName);
        if (shader == null)
        {
            throw new Exception("Missing shader " + FlowShaderName);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.shader = shader;
        mat.SetTexture("_SlashTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Generated/anhier_bluewhite_refined_slash_mask.png"));
        mat.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Generated/anhier_bluewhite_refined_noise.png"));
        mat.SetColor("_TintColor", Color.white);
        mat.SetColor("_CoreColor", coreColor);
        mat.SetColor("_EdgeColor", edgeColor);
        mat.SetFloat("_Reveal", 0f);
        mat.SetFloat("_Retreat", -0.1f);
        mat.SetFloat("_Length", length);
        mat.SetFloat("_Alpha", 1f);
        mat.SetFloat("_Intensity", intensity);
        mat.SetVector("_EdgeFade", new Vector4(0.08f, 0.11f, 0f, 0f));
        mat.SetFloat("_NoiseStrength", 0.30f);
        mat.SetFloat("_BrushStrength", brushStrength);
        mat.SetFloat("_FlowSpeed", 1.12f);
        mat.renderQueue = 3100;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateParticleMaterial(string texturePath, string materialPath, Color tint, bool alphaBlend)
    {
        Shader shader = alphaBlend
            ? Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            : Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.shader = shader;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_TintColor"))
        {
            mat.SetColor("_TintColor", tint);
        }
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }
        mat.renderQueue = alphaBlend ? 3090 : 3110;
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
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh CreateRefinedSlashMesh(string name, float thicknessScale, float normalOffset, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 center = EvaluateCenterline(t);
            Vector3 tangent = EvaluateTangent(t).normalized;
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;

            float pressure = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.64f);
            float leadingNeedle = Mathf.Pow(1f - Smooth01(0.88f, 1.0f, t), 1.15f);
            float trailingTaper = Smooth01(0.0f, 0.12f, t);
            float contactPressure = 1f + Mathf.Exp(-Mathf.Pow((t - 0.91f) / 0.055f, 2f)) * 0.24f;
            float width = (0.10f + 0.50f * pressure) * leadingNeedle * trailingTaper * thicknessScale * contactPressure;
            width *= 0.94f + Mathf.Sin(t * Mathf.PI * 3.0f) * 0.030f;
            float outerBias = Mathf.Lerp(-0.42f, 0.58f, Smooth01(0.18f, 0.88f, t));

            Vector3 inner = center - normal * (width * (0.50f + outerBias) - normalOffset);
            Vector3 outer = center + normal * (width * (0.50f - outerBias) + normalOffset);

            vertices.Add(inner);
            vertices.Add(outer);
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            float vertexAlpha = Mathf.Clamp01(0.35f + pressure * 0.65f);
            colors.Add(new Color(1f, 1f, 1f, vertexAlpha));
            colors.Add(new Color(1f, 1f, 1f, vertexAlpha));

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
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Vector3 EvaluateCenterline(float t)
    {
        Vector3 p0 = new Vector3(-3.85f, -1.62f, 0f);
        Vector3 p1 = new Vector3(-2.15f, 1.10f, 0f);
        Vector3 p2 = new Vector3(1.76f, 2.40f, 0f);
        Vector3 p3 = new Vector3(4.28f, 1.40f, 0f);
        float inv = 1f - t;
        return inv * inv * inv * p0
            + 3f * inv * inv * t * p1
            + 3f * inv * t * t * p2
            + t * t * t * p3;
    }

    private static Vector3 EvaluateTangent(float t)
    {
        Vector3 p0 = new Vector3(-3.85f, -1.62f, 0f);
        Vector3 p1 = new Vector3(-2.15f, 1.10f, 0f);
        Vector3 p2 = new Vector3(1.76f, 2.40f, 0f);
        Vector3 p3 = new Vector3(4.28f, 1.40f, 0f);
        float inv = 1f - t;
        return 3f * inv * inv * (p1 - p0)
            + 6f * inv * t * (p2 - p1)
            + 3f * t * t * (p3 - p2);
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
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private static void CreateMeshLayer(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, float zRot, int sortingOrder, Color vertexColor)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        go.transform.localScale = Vector3.one;

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = sortingOrder;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
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

    private static void CreateImpactFlash(Transform parent, string name, Material mat, Vector3 pos, float delay, float lifetime, float size, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.08f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 2;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.12f),
            new Keyframe(0.12f, 1.0f),
            new Keyframe(1f, 0.10f)));
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(color, color.a, 0.07f, 0.18f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateSpeedLines(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, float lengthScale, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.32f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.80f, 1f, 0.08f), new Color(0.78f, 0.96f, 1f, 0.42f));
        main.maxParticles = 64;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.78f;
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.66f, 0.92f, 1f, 0.38f), 0.38f, 0.10f, 0.24f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = lengthScale;
        renderer.velocityScale = 0.14f;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateSparkBurst(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, 18f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.36f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.12f, 0.60f, 1f, 0.64f), new Color(0.96f, 1f, 1f, 0.98f));
        main.maxParticles = 64;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 31f;
        shape.radius = 0.14f;
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.84f, 0.98f, 1f, 0.95f), 0.95f, 0.10f, 0.30f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.4f;
        renderer.velocityScale = 0.11f;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateMistBackwash(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, -18f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.62f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.14f, 0.42f, 0.70f, 0.12f), new Color(0.45f, 0.86f, 1f, 0.32f));
        main.maxParticles = 32;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.40f;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.55f, 1.00f),
            new Keyframe(1f, 1.28f)));
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.30f, 0.78f, 1f, 0.30f), 0.30f, 0.18f, 0.42f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r, baseColor.g, baseColor.b), 0f),
                new GradientColorKey(new Color(baseColor.r, baseColor.g, baseColor.b), 1f)
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

    private static void CapturePreviewState(Camera camera, Renderer[] renderers, ParticleSystem[] particles, string previewDir, string fileName, float particleTime, float reveal, float retreat, float length, float alpha, float intensity)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetFloat("_Reveal", reveal);
            block.SetFloat("_Retreat", retreat);
            block.SetFloat("_Length", length);
            block.SetFloat("_Alpha", alpha);
            block.SetFloat("_Intensity", intensity);
            renderer.SetPropertyBlock(block);
        }

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem ps = particles[i];
            if (ps == null)
            {
                continue;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.randomSeed = 37;
            ps.useAutoRandomSeed = false;
            ps.Simulate(particleTime, true, true, true);
        }

        string path = Path.Combine(previewDir, fileName);
        CaptureCamera(camera, path, 1280, 720);
        Debug.Log("Refined preview frame: " + path);
    }

    private static Camera CreatePreviewCamera()
    {
        GameObject camObj = new GameObject("AnhierBlueWhiteSlashRefinedPreviewCamera");
        Camera camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.022f, 0.030f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 3.35f;
        camera.transform.position = new Vector3(0.18f, 0.32f, -10f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
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
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
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
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Preview", "anhier_bluewhite_slash_refined"));
        }

        return Path.Combine(current.FullName, "preview_exports", "anhier_bluewhite_slash_refined");
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

            MeshRenderer[] meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            int refinedShaderRenderers = 0;
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name == FlowShaderName)
                {
                    refinedShaderRenderers++;
                }
            }

            if (refinedShaderRenderers < 3)
            {
                throw new Exception("Verify failed: main slash mesh/shader layers missing. refinedShaderRenderers=" + refinedShaderRenderers);
            }
            if (particles.Length < 4)
            {
                throw new Exception("Verify failed: support particle systems missing. particles=" + particles.Length);
            }

            string[] previewFiles =
            {
                "01_early_reveal.png",
                "02_contact_peak.png",
                "03_full_arc.png",
                "04_fade_retreat.png"
            };
            string previewDir = GetPreviewDirectory();
            for (int i = 0; i < previewFiles.Length; i++)
            {
                string path = Path.Combine(previewDir, previewFiles[i]);
                if (!File.Exists(path))
                {
                    throw new Exception("Verify failed: preview frame missing " + path);
                }
            }

            Debug.Log("Verify refined slash passed: prefab=" + PrefabName + " meshRenderers=" + meshRenderers.Length + " refinedShaderRenderers=" + refinedShaderRenderers + " particles=" + particles.Length + " bundle=" + bundlePath);
        }
        finally
        {
            bundle.Unload(false);
        }
    }

    private static Texture2D CreateSlashMaskTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            float yy = (v - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float pressure = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.52f);
                float center = 0.06f * Mathf.Sin((u * 2.8f + 0.15f) * Mathf.PI) - 0.05f * u + 0.025f;
                float taper = Smooth01(0.015f, 0.12f, u) * (1f - Smooth01(0.88f, 1.0f, u));
                float halfWidth = Mathf.Lerp(0.050f, 0.40f, pressure) * taper;
                float dist = Mathf.Abs(yy - center);
                float body = 1f - Smooth01(halfWidth * 0.48f, halfWidth, dist);
                float core = Mathf.Exp(-Mathf.Pow(dist / Mathf.Max(0.001f, halfWidth * 0.17f), 2f));
                float edgeFiber = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 21.0f + v * 2.9f) * Mathf.PI))), 8.0f);
                float scratch = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 47.0f - v * 5.5f) * Mathf.PI))), 10.0f);
                float brushNoise = Mathf.PerlinNoise(u * 20.0f + 3.7f, v * 7.4f + 9.1f);
                float internalCut = Mathf.SmoothStep(0.50f, 0.88f, scratch * 0.70f + brushNoise * 0.45f);
                float alpha = Mathf.Clamp01((body * (0.74f + brushNoise * 0.18f) + core * 0.40f + edgeFiber * 0.16f) * taper);
                alpha *= Mathf.Lerp(0.74f, 1.06f, brushNoise);
                if (internalCut > 0.56f && dist > halfWidth * 0.10f)
                {
                    alpha *= Mathf.Lerp(0.54f, 0.82f, brushNoise);
                }

                if (alpha <= 0.012f)
                {
                    continue;
                }

                pixels[y * width + x] = new Color(Mathf.Clamp01(core), Mathf.Clamp01(edgeFiber), Mathf.Clamp01(brushNoise), alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateNoiseTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float broad = Mathf.PerlinNoise(u * 5.0f + 2.3f, v * 3.0f + 7.1f);
                float fine = Mathf.PerlinNoise(u * 24.0f + 13.9f, v * 9.0f + 1.4f);
                float streak = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 15.0f + v * 0.8f) * Mathf.PI))), 7.0f);
                pixels[y * width + x] = new Color(broad, streak, fine, 1f);
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
                float horizontal = Mathf.Max(0f, 1f - Mathf.Abs(v) * 12f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.70f);
                float vertical = Mathf.Max(0f, 1f - Mathf.Abs(u) * 18f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 1.20f);
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.40f, 2f) * 1.8f);
                float alpha = Mathf.Clamp01(horizontal + vertical * 0.35f + glow * 0.24f);
                alpha *= 1f - Smooth01(0.72f, 1.0f, r);
                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(0.82f, 0.97f, 1f, alpha);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateMistTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u * 0.72f + v * v * 1.28f);
                float noise = Mathf.PerlinNoise(u * 2.1f + 11.0f, v * 2.6f + 4.0f);
                float alpha = (1f - Smooth01(0.10f, 0.96f, r)) * Mathf.Lerp(0.18f, 0.52f, noise);
                if (alpha > 0.010f)
                {
                    pixels[y * width + x] = new Color(0.26f, 0.74f, 1f, alpha);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateImpactTexture(int width, int height)
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
                float core = 1f - Smooth01(0.03f, 0.18f, r);
                float slashLine = Mathf.Max(0f, 1f - Mathf.Abs(v + u * 0.23f) * 10f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.62f);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.48f, 2f) * 1.42f);
                float alpha = Mathf.Clamp01(core + slashLine * 0.82f + halo * 0.32f);
                alpha *= 1f - Smooth01(0.76f, 1.0f, r);
                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(0.90f, 0.99f, 1f, alpha);
                }
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
}
