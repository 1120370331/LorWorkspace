using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OceanBladeSlashUsableBundleBuilder
{
    private const string BundleName = "steria_ocean_blade_slash_usable";
    private const string PrefabName = "OceanBladeSlashUsablePrefab";
    private const string PrefabPath = "Assets/Prefabs/OceanBladeSlashUsablePrefab.prefab";
    private const string ShaderPath = "Assets/Shaders/OceanBladeSlashUsableFlow.shader";
    private const string ShaderName = "Steria/OceanBladeSlashUsableFlow";
    private const string TextureDir = "Assets/Textures/Generated/OceanBladeSlashUsable";
    private const string MaterialDir = "Assets/Materials/OceanBladeSlashUsable";
    private const string MeshDir = "Assets/Meshes/OceanBladeSlashUsable";

    [MenuItem("Steria/Build Ocean Blade Slash Usable Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefab();
        RenderPreview();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        VerifyBuiltBundle(output);
        Debug.Log("Built usable ocean blade slash bundle: " + Path.Combine(output, BundleName));
    }

    public static void EnsurePrefab()
    {
        Directory.CreateDirectory("Assets/Animations");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory(MaterialDir);
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory(MeshDir);
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Shaders");
        Directory.CreateDirectory("Assets/Textures");
        Directory.CreateDirectory("Assets/Textures/Generated");
        Directory.CreateDirectory(TextureDir);

        GenerateShader();
        GenerateTextures();
        AssetDatabase.Refresh();

        Mesh bodyMesh = CreateOrReplaceMesh(MeshDir + "/OceanBladeSlashUsable_BroadBody.asset", CreateSlashMesh("OceanBladeSlashUsable_BroadBody", 1.28f, 0.02f, 72, 0.00f));
        Mesh coreMesh = CreateOrReplaceMesh(MeshDir + "/OceanBladeSlashUsable_WhiteCore.asset", CreateSlashMesh("OceanBladeSlashUsable_WhiteCore", 0.64f, -0.04f, 72, 0.06f));
        Mesh rimMesh = CreateOrReplaceMesh(MeshDir + "/OceanBladeSlashUsable_CyanOuterRim.asset", CreateSlashMesh("OceanBladeSlashUsable_CyanOuterRim", 1.62f, 0.10f, 72, -0.08f));
        Mesh foamMesh = CreateOrReplaceMesh(MeshDir + "/OceanBladeSlashUsable_FoamWake.asset", CreateSlashMesh("OceanBladeSlashUsable_FoamWake", 0.92f, -0.13f, 72, 0.18f));
        Mesh shockMesh = CreateOrReplaceMesh(MeshDir + "/OceanBladeSlashUsable_ImpactShock.asset", CreateImpactShardMesh("OceanBladeSlashUsable_ImpactShock", 18));

        Material bodyMat = CreateFlowMaterial(
            MaterialDir + "/OceanBladeSlashUsable_BroadOceanBody.mat",
            new Color(0.35f, 0.86f, 1.00f, 0.80f),
            new Color(0.08f, 0.38f, 1.00f, 0.62f),
            0.92f,
            2.10f,
            0.50f,
            0.060f,
            0.12f,
            3080);
        Material coreMat = CreateFlowMaterial(
            MaterialDir + "/OceanBladeSlashUsable_WhiteHotCore.mat",
            new Color(0.96f, 1.00f, 1.00f, 1.00f),
            new Color(0.42f, 0.92f, 1.00f, 0.82f),
            0.74f,
            3.10f,
            0.36f,
            0.035f,
            0.06f,
            3105);
        Material rimMat = CreateFlowMaterial(
            MaterialDir + "/OceanBladeSlashUsable_CyanFoamRim.mat",
            new Color(0.70f, 0.97f, 1.00f, 0.86f),
            new Color(0.00f, 0.54f, 1.00f, 0.76f),
            1.06f,
            2.45f,
            0.64f,
            0.080f,
            0.15f,
            3115);
        Material foamMat = CreateFlowMaterial(
            MaterialDir + "/OceanBladeSlashUsable_BrokenFoamWake.mat",
            new Color(0.82f, 0.98f, 1.00f, 0.72f),
            new Color(0.18f, 0.68f, 1.00f, 0.48f),
            1.20f,
            1.55f,
            0.78f,
            0.12f,
            0.22f,
            3075);
        Material shockMat = CreateFlowMaterial(
            MaterialDir + "/OceanBladeSlashUsable_ImpactShockFan.mat",
            new Color(0.90f, 1.00f, 1.00f, 0.68f),
            new Color(0.08f, 0.62f, 1.00f, 0.40f),
            0.34f,
            1.55f,
            0.62f,
            0.16f,
            0.24f,
            3095);

        Material sparkMat = CreateParticleMaterial(TextureDir + "/ocean_blade_spark_cross.png", MaterialDir + "/OceanBladeSlashUsable_SparkCross.mat", new Color(0.80f, 0.97f, 1f, 0.90f), false);
        Material mistMat = CreateParticleMaterial(TextureDir + "/ocean_blade_mist.png", MaterialDir + "/OceanBladeSlashUsable_BackMist.mat", new Color(0.20f, 0.72f, 1f, 0.34f), true);
        Material impactMat = CreateParticleMaterial(TextureDir + "/ocean_blade_impact_flash.png", MaterialDir + "/OceanBladeSlashUsable_ImpactFlash.mat", new Color(0.95f, 1f, 1f, 0.92f), false);
        Material dropletMat = CreateParticleMaterial(TextureDir + "/ocean_blade_droplet.png", MaterialDir + "/OceanBladeSlashUsable_DropletShard.mat", new Color(0.50f, 0.90f, 1f, 0.75f), false);

        GameObject root = CreateRoot(PrefabName);
        OceanBladeSlashUsableDriver driver = root.AddComponent<OceanBladeSlashUsableDriver>();
        driver.duration = 0.72f;
        driver.loop = false;

        Transform practical = CreateGroup(root.transform, "PracticalSystem_NonEmpty", Vector3.zero);
        Transform meshRoot = CreateGroup(practical, "MeshSlash_ThickCrescent", Vector3.zero);
        Transform particleRoot = CreateGroup(practical, "ParticleSupport_ImpactMistSpeed", Vector3.zero);

        CreateMeshLayer(meshRoot, "OBS_BroadOceanBody_MeshFace", bodyMesh, bodyMat, new Vector3(0f, 0f, 0.00f), 0f, 4);
        CreateMeshLayer(meshRoot, "OBS_WhiteHotCore_MeshFace", coreMesh, coreMat, new Vector3(0.05f, 0.02f, -0.04f), -0.6f, 8);
        CreateMeshLayer(meshRoot, "OBS_CyanFoamOuterRim_MeshFace", rimMesh, rimMat, new Vector3(-0.02f, 0.04f, 0.03f), 0.4f, 7);
        CreateMeshLayer(meshRoot, "OBS_BrokenFoamRetreatWake_MeshFace", foamMesh, foamMat, new Vector3(-0.28f, -0.10f, 0.05f), -1.0f, 3);
        CreateMeshLayer(meshRoot, "OBS_LeadingImpactFoamShard_MeshFace", shockMesh, shockMat, new Vector3(4.20f, 1.24f, -0.03f), -12f, 6);

        CreateImpactFlash(particleRoot, "OBS_ContactFlash_Billboard", impactMat, new Vector3(4.22f, 1.24f, -0.08f), 0.105f, 0.18f, 0.58f, new Color(0.88f, 0.99f, 1f, 0.38f), 11);
        CreateImpactFlash(particleRoot, "OBS_CoolBloom_Secondary", impactMat, new Vector3(4.02f, 1.14f, 0.02f), 0.145f, 0.24f, 0.74f, new Color(0.22f, 0.72f, 1f, 0.10f), 2);
        CreateSpeedLines(particleRoot, "OBS_ForwardStretchSpeedLines", sparkMat, new Vector3(3.30f, 1.12f, -0.05f), -8f, 0.055f, 18, 0.035f, 0.105f, 6.0f, 13.5f, 4.8f, 10);
        CreateDropletBurst(particleRoot, "OBS_OceanDropletShardBurst", dropletMat, new Vector3(4.04f, 1.22f, -0.04f), 0.14f, 26, 0.040f, 0.16f, 2.6f, 7.2f, 9);
        CreateMistBackwash(particleRoot, "OBS_CrescentColdMistBackwash", mistMat, new Vector3(-1.10f, -0.52f, 0.08f), 0.20f, 10, 0.70f, 1.65f, 0.7f, 2.8f, 1);
        CreateMistBackwash(particleRoot, "OBS_UpperFoamVaporTrail", mistMat, new Vector3(0.50f, 0.74f, 0.09f), 0.15f, 7, 0.38f, 1.10f, 0.4f, 1.6f, 5);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        VerifyPrefabAsset();
        Debug.Log("Prepared usable ocean blade slash prefab: " + PrefabPath);
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
            throw new Exception("Preview failed: missing prefab " + PrefabPath);
        }

        CapturePreviewSet(previewDir, prefab, "black", new Color(0.012f, 0.016f, 0.024f, 1f), false);
        CapturePreviewSet(previewDir, prefab, "stage", new Color(0.19f, 0.24f, 0.29f, 1f), true);

        Debug.Log("Usable ocean blade slash previews complete: " + previewDir);
    }

    public static void VerifyBundle()
    {
        VerifyBuiltBundle(Path.GetFullPath("Assets/../AssetBundles"));
    }

    private static void GenerateShader()
    {
        File.WriteAllText(ShaderPath, @"Shader ""Steria/OceanBladeSlashUsableFlow""
{
    Properties
    {
        _SlashTex (""Wide Brush Mask"", 2D) = ""white"" {}
        _NoiseTex (""Foam Breakup Noise"", 2D) = ""white"" {}
        _TintColor (""Tint"", Color) = (1,1,1,1)
        _CoreColor (""Core Color"", Color) = (1,1,1,1)
        _EdgeColor (""Edge Color"", Color) = (0.1,0.7,1,1)
        _Reveal (""Reveal Head"", Float) = 0
        _Retreat (""Retreat Tail"", Float) = -0.2
        _Length (""Visible Length"", Float) = 0.9
        _Alpha (""Alpha"", Float) = 1
        _Intensity (""Intensity"", Float) = 2
        _NoiseStrength (""Noise Strength"", Float) = 0.5
        _BrushStrength (""Brush Strength"", Float) = 0.5
        _EdgeFade (""Thickness Edge Fade"", Vector) = (0.06,0.12,0,0)
        _DistortAmount (""Vertex Water Distort"", Float) = 0.04
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
            float _NoiseStrength;
            float _BrushStrength;
            float4 _EdgeFade;
            float _DistortAmount;

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
                float u = saturate(v.uv.x);
                float width = saturate(v.uv.y);
                float wave = sin(u * 35.0 + _Time.y * 9.0) * sin(width * 6.2831);
                v.vertex.xy += float2(0.0, wave * _DistortAmount * v.color.a);
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
                float tail = max(_Retreat, head - max(_Length, 0.04));
                float revealMask = smoothstep(tail, tail + 0.055, u) * (1.0 - smoothstep(head - 0.038, head + 0.034, u));
                float endFade = smoothstep(0.0, 0.030, u) * (1.0 - smoothstep(0.965, 1.0, u));
                float edgeFade = smoothstep(0.0, _EdgeFade.x, v) * (1.0 - smoothstep(1.0 - _EdgeFade.y, 1.0, v));

                float4 brush = tex2D(_SlashTex, float2(u, v));
                float n0 = tex2D(_NoiseTex, float2(u * 2.7 - _Time.y * 0.9, v * 1.15 + _Time.y * 0.14)).r;
                float streak = tex2D(_NoiseTex, float2(u * 10.0 - _Time.y * 2.0, v * 0.55 + 0.31)).g;
                float foam = tex2D(_NoiseTex, float2(u * 5.0 + 0.17, v * 3.3 - _Time.y * 0.35)).b;

                float core = pow(saturate(1.0 - abs(v - 0.52) * 3.6), 2.2);
                float outerRim = pow(saturate(1.0 - abs(v - 0.82) * 5.3), 1.65);
                float innerRim = pow(saturate(1.0 - abs(v - 0.20) * 6.0), 1.80);
                float leading = (1.0 - smoothstep(0.0, 0.040, abs(u - head))) * pow(saturate(1.0 - abs(v - 0.56) * 2.7), 1.5);
                float tailFoam = smoothstep(tail, tail + 0.12, u) * (1.0 - smoothstep(tail + 0.18, tail + 0.34, u));

                float breakup = lerp(1.0 - _NoiseStrength, 1.22, n0) + streak * 0.22 + foam * 0.08;
                float brushShape = lerp(1.0 - _BrushStrength, 1.25, brush.r);
                float holes = smoothstep(0.22, 0.76, brush.b * 0.52 + foam * 0.32 + streak * 0.20);

                float alpha = brush.a * edgeFade * endFade * revealMask;
                alpha *= saturate(breakup * brushShape);
                alpha *= lerp(0.62, 1.08, holes);
                alpha += leading * brush.a * edgeFade * 0.35;
                alpha += tailFoam * (outerRim + innerRim) * brush.a * 0.18;
                alpha = saturate(alpha) * _Alpha * _TintColor.a * i.color.a;

                float3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(core * 1.15 + brush.r * 0.45 + leading * 0.45));
                color = lerp(color, float3(0.06, 0.56, 1.0), saturate(outerRim * 0.35 + innerRim * 0.18));
                color += float3(0.30, 0.88, 1.0) * saturate(streak * 0.20 + tailFoam * 0.16);
                color *= _TintColor.rgb * _Intensity * (0.72 + n0 * 0.22 + foam * 0.16 + leading * 0.55);

                return fixed4(color, alpha);
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
        SavePng(TextureDir + "/ocean_blade_wide_mask.png", CreateWideSlashMaskTexture(1536, 384));
        SavePng(TextureDir + "/ocean_blade_foam_noise.png", CreateFoamNoiseTexture(512, 512));
        SavePng(TextureDir + "/ocean_blade_spark_cross.png", CreateSparkTexture(256, 256));
        SavePng(TextureDir + "/ocean_blade_mist.png", CreateMistTexture(512, 512));
        SavePng(TextureDir + "/ocean_blade_impact_flash.png", CreateImpactTexture(512, 512));
        SavePng(TextureDir + "/ocean_blade_droplet.png", CreateDropletTexture(256, 256));
        AssetDatabase.Refresh();

        ConfigureTexture(TextureDir + "/ocean_blade_wide_mask.png", TextureWrapMode.Clamp);
        ConfigureTexture(TextureDir + "/ocean_blade_foam_noise.png", TextureWrapMode.Repeat);
        ConfigureTexture(TextureDir + "/ocean_blade_spark_cross.png", TextureWrapMode.Clamp);
        ConfigureTexture(TextureDir + "/ocean_blade_mist.png", TextureWrapMode.Clamp);
        ConfigureTexture(TextureDir + "/ocean_blade_impact_flash.png", TextureWrapMode.Clamp);
        ConfigureTexture(TextureDir + "/ocean_blade_droplet.png", TextureWrapMode.Clamp);
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

    private static Material CreateFlowMaterial(string materialPath, Color coreColor, Color edgeColor, float length, float intensity, float brushStrength, float edgeIn, float edgeOut, int queue)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            throw new Exception("Missing shader " + ShaderName);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.shader = shader;
        mat.SetTexture("_SlashTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/ocean_blade_wide_mask.png"));
        mat.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/ocean_blade_foam_noise.png"));
        mat.SetColor("_TintColor", Color.white);
        mat.SetColor("_CoreColor", coreColor);
        mat.SetColor("_EdgeColor", edgeColor);
        mat.SetFloat("_Reveal", 0f);
        mat.SetFloat("_Retreat", -0.16f);
        mat.SetFloat("_Length", length);
        mat.SetFloat("_Alpha", 1f);
        mat.SetFloat("_Intensity", intensity);
        mat.SetFloat("_NoiseStrength", 0.42f);
        mat.SetFloat("_BrushStrength", brushStrength);
        mat.SetVector("_EdgeFade", new Vector4(edgeIn, edgeOut, 0f, 0f));
        mat.SetFloat("_DistortAmount", 0.04f);
        mat.renderQueue = queue;
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
        mat.renderQueue = alphaBlend ? 3060 : 3120;
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

    private static Mesh CreateSlashMesh(string name, float thicknessScale, float normalOffset, int segments, float centerBias)
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

            float pressure = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.42f);
            float startTaper = Smooth01(0.00f, 0.10f, t);
            float endTaper = 1f - Smooth01(0.89f, 1.0f, t);
            float leadingBulge = 1f + Mathf.Exp(-Mathf.Pow((t - 0.86f) / 0.085f, 2f)) * 0.58f;
            float width = (0.18f + 0.86f * pressure) * startTaper * endTaper * leadingBulge * thicknessScale;
            width *= 0.96f + Mathf.Sin(t * Mathf.PI * 4.0f) * 0.045f;
            float outerBias = Mathf.Lerp(-0.36f, 0.42f, Smooth01(0.10f, 0.82f, t)) + centerBias;

            Vector3 inner = center - normal * (width * (0.50f + outerBias) - normalOffset);
            Vector3 outer = center + normal * (width * (0.50f - outerBias) + normalOffset);

            vertices.Add(inner);
            vertices.Add(outer);
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            float alpha = Mathf.Clamp01(0.28f + pressure * 0.72f);
            colors.Add(new Color(1f, 1f, 1f, alpha));
            colors.Add(new Color(1f, 1f, 1f, alpha));

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

    private static Mesh CreateImpactShardMesh(string name, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        float length = 1.18f;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-0.08f, length, t);
            float pressure = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.55f);
            float width = Mathf.Lerp(0.05f, 0.38f, pressure) * (1f - Smooth01(0.86f, 1f, t));
            float wobble = Mathf.Sin(t * Mathf.PI * 5f) * 0.035f;

            vertices.Add(new Vector3(x, -width * 0.42f + wobble, 0f));
            vertices.Add(new Vector3(x, width * 0.58f + wobble, 0f));
            uvs.Add(new Vector2(Mathf.Lerp(0.70f, 0.98f, t), 0.18f));
            uvs.Add(new Vector2(Mathf.Lerp(0.70f, 0.98f, t), 0.82f));
            float alpha = Mathf.Clamp01(0.20f + pressure * 0.40f);
            colors.Add(new Color(1f, 1f, 1f, alpha));
            colors.Add(new Color(1f, 1f, 1f, alpha));

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
        Vector3 p0 = new Vector3(-4.20f, -1.72f, 0f);
        Vector3 p1 = new Vector3(-3.02f, 1.42f, 0f);
        Vector3 p2 = new Vector3(1.66f, 2.74f, 0f);
        Vector3 p3 = new Vector3(4.70f, 1.20f, 0f);
        float inv = 1f - t;
        return inv * inv * inv * p0
            + 3f * inv * inv * t * p1
            + 3f * inv * t * t * p2
            + t * t * t * p3;
    }

    private static Vector3 EvaluateTangent(float t)
    {
        Vector3 p0 = new Vector3(-4.20f, -1.72f, 0f);
        Vector3 p1 = new Vector3(-3.02f, 1.42f, 0f);
        Vector3 p2 = new Vector3(1.66f, 2.74f, 0f);
        Vector3 p3 = new Vector3(4.70f, 1.20f, 0f);
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

    private static void CreateMeshLayer(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, float zRot, int sortingOrder)
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
        main.duration = delay + lifetime + 0.12f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 4;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.08f),
            new Keyframe(0.16f, 1.0f),
            new Keyframe(0.44f, 0.58f),
            new Keyframe(1f, 0.06f)));
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(color, color.a, 0.08f, 0.30f);
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
        main.duration = delay + 0.42f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.09f, 0.24f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.48f, 0.84f, 1f, 0.18f), new Color(0.94f, 1f, 1f, 0.62f));
        main.maxParticles = 96;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = 0.95f;
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.72f, 0.94f, 1f, 0.48f), 0.48f, 0.12f, 0.30f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = lengthScale;
        renderer.velocityScale = 0.06f;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateDropletBurst(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, 155f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.52f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.40f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.40f, 0.86f, 1f, 0.24f), new Color(0.96f, 1f, 1f, 0.92f));
        main.maxParticles = 96;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.25f;
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.74f, 0.96f, 1f, 0.72f), 0.72f, 0.10f, 0.25f);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.1f;
        renderer.velocityScale = 0.08f;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateMistBackwash(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, -22f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.78f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.36f, 0.74f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.08f, 0.34f, 0.60f, 0.10f), new Color(0.52f, 0.88f, 1f, 0.36f));
        main.maxParticles = 48;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.70f;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.42f, 1.00f),
            new Keyframe(1f, 1.22f)));
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient(new Color(0.30f, 0.78f, 1f, 0.30f), 0.30f, 0.18f, 0.48f);
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
                new GradientAlphaKey(peakAlpha * 0.78f, holdTime),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void CapturePreviewSet(string previewDir, GameObject prefab, string prefix, Color background, bool stage)
    {
        GameObject root = UnityEngine.Object.Instantiate(prefab);
        root.name = PrefabName + "_" + prefix + "_PreviewInstance";
        OceanBladeSlashUsableDriver driver = root.GetComponent<OceanBladeSlashUsableDriver>();
        if (driver != null)
        {
            driver.enabled = false;
        }

        Camera camera = CreatePreviewCamera(background);
        GameObject stageRoot = null;
        if (stage)
        {
            stageRoot = CreatePreviewStage();
        }

        CapturePreviewState(camera, root, previewDir, prefix + "_01_early_reveal.png", 0.08f, 0.12f);
        CapturePreviewState(camera, root, previewDir, prefix + "_02_contact_peak.png", 0.18f, 0.19f);
        CapturePreviewState(camera, root, previewDir, prefix + "_03_full_body.png", 0.30f, 0.30f);
        CapturePreviewState(camera, root, previewDir, prefix + "_04_fade_retreat.png", 0.56f, 0.56f);

        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        if (stageRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(stageRoot);
        }
    }

    private static void CapturePreviewState(Camera camera, GameObject root, string previewDir, string fileName, float driverTime, float particleTime)
    {
        OceanBladeSlashUsableDriver driver = root.GetComponent<OceanBladeSlashUsableDriver>();
        if (driver != null)
        {
            driver.ApplyAt(driverTime);
        }

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem ps = particles[i];
            if (ps == null)
            {
                continue;
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.randomSeed = 71;
            ps.useAutoRandomSeed = false;
            ps.Simulate(particleTime, true, true, true);
        }

        string path = Path.Combine(previewDir, fileName);
        CaptureCamera(camera, path, 1280, 720);
        Debug.Log("Usable preview frame: " + path);
    }

    private static Camera CreatePreviewCamera(Color background)
    {
        GameObject camObj = new GameObject("OceanBladeSlashUsablePreviewCamera");
        Camera camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.orthographic = true;
        camera.orthographicSize = 3.55f;
        camera.transform.position = new Vector3(0.24f, 0.36f, -10f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static GameObject CreatePreviewStage()
    {
        GameObject root = new GameObject("OceanBladeSlashPreviewStage_NonBlack");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = "MutedStagePanel";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0.0f, -1.95f, 2.0f);
        floor.transform.localRotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(10.0f, 1.15f, 1f);
        Material floorMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
        floorMat.color = new Color(0.12f, 0.17f, 0.20f, 1f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "ScaleTargetSilhouette";
        target.transform.SetParent(root.transform, false);
        target.transform.localPosition = new Vector3(4.34f, 0.56f, 0.7f);
        target.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        target.transform.localScale = new Vector3(0.16f, 1.6f, 0.16f);
        Material targetMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
        targetMat.color = new Color(0.05f, 0.08f, 0.10f, 1f);
        target.GetComponent<Renderer>().sharedMaterial = targetMat;

        return root;
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
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Preview", "ocean_blade_slash_usable"));
        }

        return Path.Combine(current.FullName, "preview_exports", "ocean_blade_slash_usable");
    }

    private static void VerifyPrefabAsset()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new Exception("Verify failed: prefab missing " + PrefabPath);
        }

        Transform practical = prefab.transform.Find("PracticalSystem_NonEmpty");
        if (practical == null || practical.childCount < 2)
        {
            throw new Exception("Verify failed: PracticalSystem_NonEmpty is missing or empty");
        }

        MeshRenderer[] meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
        if (meshRenderers.Length < 5)
        {
            throw new Exception("Verify failed: expected at least 5 mesh renderers, got " + meshRenderers.Length);
        }
        if (particles.Length < 6)
        {
            throw new Exception("Verify failed: expected at least 6 particle systems, got " + particles.Length);
        }

        int shaderCount = 0;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                throw new Exception("Verify failed: mesh renderer has no mesh: " + renderer.name);
            }
            if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
            {
                throw new Exception("Verify failed: mesh renderer has missing material/shader: " + renderer.name);
            }
            if (renderer.sharedMaterial.shader.name == ShaderName)
            {
                shaderCount++;
                if (renderer.sharedMaterial.GetTexture("_SlashTex") == null || renderer.sharedMaterial.GetTexture("_NoiseTex") == null)
                {
                    throw new Exception("Verify failed: flow material missing textures: " + renderer.sharedMaterial.name);
                }
            }
        }

        if (shaderCount < 5)
        {
            throw new Exception("Verify failed: usable slash shader layers missing, shaderCount=" + shaderCount);
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
            throw new Exception("Verify failed: unable to load bundle " + bundlePath);
        }

        try
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (prefab == null)
            {
                throw new Exception("Verify failed: prefab missing from bundle " + PrefabName);
            }

            MeshRenderer[] meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            int shaderCount = 0;
            int validTextureMaterials = 0;
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                Material mat = meshRenderers[i].sharedMaterial;
                MeshFilter filter = meshRenderers[i].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    throw new Exception("Verify failed: missing mesh in bundled prefab " + meshRenderers[i].name);
                }
                if (mat != null && mat.shader != null && mat.shader.name == ShaderName)
                {
                    shaderCount++;
                    if (mat.GetTexture("_SlashTex") != null && mat.GetTexture("_NoiseTex") != null)
                    {
                        validTextureMaterials++;
                    }
                }
            }
            if (shaderCount < 5 || validTextureMaterials < 5)
            {
                throw new Exception("Verify failed: mesh shader/material texture refs incomplete. shaderCount=" + shaderCount + " validTextureMaterials=" + validTextureMaterials);
            }
            if (particles.Length < 6)
            {
                throw new Exception("Verify failed: particle support systems missing. particles=" + particles.Length);
            }

            string previewDir = GetPreviewDirectory();
            string[] previewFiles =
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
            for (int i = 0; i < previewFiles.Length; i++)
            {
                string path = Path.Combine(previewDir, previewFiles[i]);
                if (!File.Exists(path) || new FileInfo(path).Length < 12000)
                {
                    throw new Exception("Verify failed: preview missing or too small " + path);
                }
            }

            Debug.Log("Verify usable ocean blade slash passed: prefab=" + PrefabName + " meshRenderers=" + meshRenderers.Length + " shaderCount=" + shaderCount + " particles=" + particles.Length + " bundle=" + bundlePath);
        }
        finally
        {
            bundle.Unload(false);
        }
    }

    private static Texture2D CreateWideSlashMaskTexture(int width, int height)
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
                float pressure = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.42f);
                float taper = Smooth01(0.00f, 0.10f, u) * (1f - Smooth01(0.90f, 1.0f, u));
                float center = -0.055f + 0.075f * Mathf.Sin((u * 1.65f + 0.08f) * Mathf.PI) - u * 0.035f;
                float halfWidth = Mathf.Lerp(0.12f, 0.70f, pressure) * taper;
                float dist = Mathf.Abs(yy - center);
                float body = 1f - Smooth01(halfWidth * 0.58f, halfWidth, dist);
                float core = Mathf.Exp(-Mathf.Pow(dist / Mathf.Max(0.002f, halfWidth * 0.18f), 2.0f));
                float rim = Mathf.Exp(-Mathf.Pow((dist - halfWidth * 0.72f) / Mathf.Max(0.002f, halfWidth * 0.13f), 2.0f));
                float broadNoise = Mathf.PerlinNoise(u * 13.0f + 7.1f, v * 6.0f + 2.5f);
                float fineNoise = Mathf.PerlinNoise(u * 48.0f + 1.3f, v * 18.0f + 8.4f);
                float streak = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 29.0f + v * 4.2f) * Mathf.PI))), 7.0f);
                float breaks = Mathf.SmoothStep(0.30f, 0.92f, broadNoise * 0.48f + fineNoise * 0.28f + streak * 0.30f);
                float alpha = Mathf.Clamp01(body * (0.78f + broadNoise * 0.22f) + core * 0.44f + rim * 0.23f);
                alpha *= taper;
                if (breaks > 0.66f && dist > halfWidth * 0.18f)
                {
                    alpha *= Mathf.Lerp(0.48f, 0.82f, fineNoise);
                }
                alpha *= Mathf.Lerp(0.88f, 1.10f, streak);

                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(Mathf.Clamp01(core), Mathf.Clamp01(rim), Mathf.Clamp01(breaks), alpha);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateFoamNoiseTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float broad = Mathf.PerlinNoise(u * 4.0f + 2.0f, v * 3.0f + 5.0f);
                float fine = Mathf.PerlinNoise(u * 32.0f + 13.0f, v * 20.0f + 1.0f);
                float streak = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 18.0f + v * 1.15f) * Mathf.PI))), 8.0f);
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
                float slash = Mathf.Max(0f, 1f - Mathf.Abs(v + u * 0.18f) * 13f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.64f);
                float cross = Mathf.Max(0f, 1f - Mathf.Abs(u) * 18f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 1.15f);
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.42f, 2f) * 1.55f);
                float alpha = Mathf.Clamp01(slash + cross * 0.38f + glow * 0.25f);
                alpha *= 1f - Smooth01(0.78f, 1.0f, r);
                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(0.84f, 0.98f, 1f, alpha);
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
                float r = Mathf.Sqrt(u * u * 0.64f + v * v * 1.28f);
                float noise = Mathf.PerlinNoise(u * 2.0f + 11.0f, v * 2.8f + 4.0f);
                float alpha = (1f - Smooth01(0.08f, 0.96f, r)) * Mathf.Lerp(0.16f, 0.50f, noise);
                if (alpha > 0.010f)
                {
                    pixels[y * width + x] = new Color(0.24f, 0.72f, 1f, alpha);
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
                float core = 1f - Smooth01(0.02f, 0.16f, r);
                float slash = Mathf.Max(0f, 1f - Mathf.Abs(v + u * 0.24f) * 9f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.54f);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.52f, 2f) * 1.25f);
                float alpha = Mathf.Clamp01(core + slash * 0.88f + halo * 0.34f);
                alpha *= 1f - Smooth01(0.80f, 1.0f, r);
                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(0.93f, 0.99f, 1f, alpha);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateDropletTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u * 1.6f + v * v * 0.75f);
                float alpha = Mathf.Exp(-Mathf.Pow(r / 0.32f, 2f) * 2.4f);
                float shine = Mathf.Exp(-Mathf.Pow((u + 0.10f) / 0.09f, 2f) - Mathf.Pow((v - 0.07f) / 0.08f, 2f));
                alpha += shine * 0.35f;
                alpha *= 1f - Smooth01(0.62f, 0.90f, r);
                if (alpha > 0.012f)
                {
                    pixels[y * width + x] = new Color(0.70f, 0.96f, 1f, Mathf.Clamp01(alpha));
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

    private static float Smooth01(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}
