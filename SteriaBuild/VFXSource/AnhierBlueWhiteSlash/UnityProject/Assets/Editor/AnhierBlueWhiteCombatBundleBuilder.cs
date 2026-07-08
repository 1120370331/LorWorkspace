using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnhierBlueWhiteCombatBundleBuilder
{
    private const string BundleName = "steria_anhier_bluewhite_combat";
    private const string PiercePrefabName = "AnhierBlueWhitePiercePrefab";
    private const string HitPrefabName = "AnhierBlueWhiteHitPrefab";
    private const string FarHitPrefabName = "AnhierBlueWhiteFarHitPrefab";
    private const float FarHitCasterCircleScale = 1.5f;
    private const float FarHitCasterCirclePitchDegrees = 25f;

    [MenuItem("Steria/Build Anhier BlueWhite Combat Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefabs();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        CopyBundleToMod(output);
        VerifyBuiltBundle(output);
        Debug.Log("Built Anhier combat AssetBundle: " + output);
    }

    public static void EnsurePrefabs()
    {
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Textures");

        GenerateTextures();

        Mesh lanceMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierCombat_LanceMesh.asset", CreateNeedleMesh(1.22f, 0.13f, 14));
        Mesh shortLanceMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierCombat_ShortLanceMesh.asset", CreateNeedleMesh(0.92f, 0.18f, 12));
        Mesh shockMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierCombat_ShockMesh.asset", CreateDiamondMesh(0.88f, 0.34f));
        Mesh circleMesh = CreateOrReplaceMesh("Assets/Meshes/AnhierCombat_CircleQuadMesh.asset", CreateQuadMesh(1f));

        Material beam = CreateMaterial("Assets/Textures/anhier_combat_beam.png", "Assets/Materials/AnhierCombat_Beam_Add.mat", new Color(0.72f, 0.96f, 1f, 0.94f), false);
        Material glow = CreateMaterial("Assets/Textures/anhier_combat_glow.png", "Assets/Materials/AnhierCombat_Glow_Add.mat", new Color(0.26f, 0.76f, 1f, 0.70f), false);
        Material spark = CreateMaterial("Assets/Textures/anhier_combat_spark.png", "Assets/Materials/AnhierCombat_Spark_Add.mat", new Color(0.84f, 0.98f, 1f, 0.94f), false);
        Material mist = CreateMaterial("Assets/Textures/anhier_combat_mist.png", "Assets/Materials/AnhierCombat_Mist_Alpha.mat", new Color(0.34f, 0.78f, 1f, 0.34f), true);
        Material circle = CreateMaterial("Assets/Textures/anhier_combat_magic_circle.png", "Assets/Materials/AnhierCombat_MagicCircle_Add.mat", new Color(0.68f, 0.94f, 1f, 0.82f), false);

        CreatePiercePrefab(beam, glow, spark, mist, lanceMesh);
        CreateHitPrefab(beam, glow, spark, mist, shortLanceMesh, shockMesh);
        CreateFarHitPrefab(beam, glow, spark, mist, circle, lanceMesh, circleMesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreatePiercePrefab(Material beam, Material glow, Material spark, Material mist, Mesh lanceMesh)
    {
        GameObject root = CreateRoot(PiercePrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(8.5f, 0.05f, -0.02f));

        CreateMeshParticle(travelRoot, "AB_Pierce_DeepBlueTrail", glow, lanceMesh, new Vector3(-0.26f, 0.02f, 0.02f), -2f, 0.000f, 0.42f, 4.0f, 3, new Color(0.08f, 0.42f, 1f, 0.54f), true);
        CreateMeshParticle(travelRoot, "AB_Pierce_WhiteCore_A", beam, lanceMesh, new Vector3(0.00f, 0.04f, -0.02f), 0f, 0.018f, 0.32f, 3.8f, 8, new Color(0.90f, 1f, 1f, 0.96f), true);
        CreateMeshParticle(travelRoot, "AB_Pierce_WhiteCore_B", beam, lanceMesh, new Vector3(0.34f, 0.02f, -0.03f), 0f, 0.052f, 0.30f, 3.6f, 9, new Color(0.94f, 1f, 1f, 1f), true);
        CreateStretchParticle(travelRoot, "AB_Pierce_SpeedNeedles", spark, new Vector3(0.20f, 0.06f, -0.04f), 0f, 0.035f, 8, 0.08f, 0.26f, 5.0f, 10.5f, 2.2f, 7);
        CreateMistParticle(travelRoot, "AB_Pierce_ColdWake", mist, new Vector3(-0.18f, -0.04f, 0.06f), 0.035f, 4, 1.4f, 2.4f, 0);

        CreateImpactFlash(impactRoot, "AB_Pierce_ImpactStarWhite", glow, Vector3.zero, 0.085f, 0.34f, 4.0f, new Color(0.92f, 1f, 1f, 0.9f), 10);
        CreateImpactFlash(impactRoot, "AB_Pierce_ImpactBlueNeedle", beam, new Vector3(0.12f, 0.0f, -0.02f), 0.095f, 0.30f, 3.2f, new Color(0.24f, 0.78f, 1f, 0.72f), 9);
        CreateShardParticle(impactRoot, "AB_Pierce_IceShards", spark, new Vector3(0f, 0.02f, -0.03f), 0.09f, 8, 5);

        SavePrefab(root, "Assets/Prefabs/AnhierBlueWhitePiercePrefab.prefab");
    }

    private static void CreateHitPrefab(Material beam, Material glow, Material spark, Material mist, Mesh shortLanceMesh, Mesh shockMesh)
    {
        GameObject root = CreateRoot(HitPrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(5.6f, 0.14f, -0.02f));

        CreateMeshParticle(travelRoot, "AB_Hit_CompressionBlue", glow, shockMesh, new Vector3(0.35f, 0.02f, 0f), -8f, 0.000f, 0.30f, 3.2f, 3, new Color(0.16f, 0.62f, 1f, 0.54f), true);
        CreateMeshParticle(travelRoot, "AB_Hit_WhiteSmashCore", beam, shortLanceMesh, new Vector3(0.66f, 0.05f, -0.02f), -4f, 0.025f, 0.26f, 2.8f, 8, new Color(0.96f, 1f, 1f, 0.94f), true);
        CreateStretchParticle(travelRoot, "AB_Hit_ShortSpeedLines", spark, new Vector3(0.55f, 0.04f, -0.04f), -6f, 0.035f, 7, 0.12f, 0.34f, 3.0f, 7.0f, 1.6f, 7);

        CreateImpactFlash(impactRoot, "AB_Hit_ImpactWhiteBurst", glow, Vector3.zero, 0.060f, 0.42f, 5.0f, new Color(0.94f, 1f, 1f, 0.92f), 10);
        CreateImpactFlash(impactRoot, "AB_Hit_RoundBlueShock", glow, new Vector3(0f, 0f, 0.02f), 0.075f, 0.50f, 5.8f, new Color(0.18f, 0.68f, 1f, 0.42f), 4);
        CreateShardParticle(impactRoot, "AB_Hit_SplinterShards", spark, new Vector3(0.05f, 0.04f, -0.04f), 0.070f, 8, 6);
        CreateMistParticle(impactRoot, "AB_Hit_SnowDust", mist, new Vector3(-0.14f, 0.10f, 0.06f), 0.110f, 5, 1.6f, 2.8f, 1);

        SavePrefab(root, "Assets/Prefabs/AnhierBlueWhiteHitPrefab.prefab");
    }

    private static void CreateFarHitPrefab(Material beam, Material glow, Material spark, Material mist, Material circle, Mesh lanceMesh, Mesh circleMesh)
    {
        GameObject root = CreateRoot(FarHitPrefabName);
        Transform casterRoot = CreateGroup(root.transform, "AB_CasterRoot", new Vector3(0.72f, 0.18f, -0.01f));
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(0f, 0.18f, -0.02f));

        CreateCasterCircleParticle(casterRoot, "AB_FarHit_CasterCircle_Outer", circle, circleMesh, Vector3.zero, 0.000f, 0.58f, 2.8f, 0f, new Color(0.42f, 0.86f, 1f, 0.72f), 9);
        CreateCasterCircleParticle(casterRoot, "AB_FarHit_CasterCircle_Inner", circle, circleMesh, new Vector3(0.02f, 0f, -0.01f), 0.025f, 0.46f, 2.0f, 0.55f, new Color(0.90f, 1f, 1f, 0.86f), 10);
        CreateCasterCircleParticle(casterRoot, "AB_FarHit_CasterCircle_Flash", glow, circleMesh, new Vector3(0.03f, 0f, 0.01f), 0.050f, 0.26f, 2.4f, -0.25f, new Color(0.84f, 0.98f, 1f, 0.50f), 7);

        const float pathStartX = -4.25f;
        const float pathEndX = 4.25f;
        CreateFarHitSegmentedTrail(travelRoot, "AB_FarHit_PathTrail_Ghost", glow, lanceMesh, 8, pathStartX, pathEndX, -0.13f, 0.01f, 0.000f, 0.027f, 0.34f, 2.55f, 4f, new Color(0.10f, 0.46f, 1f, 0.44f), 5, 0.10f);
        CreateFarHitSegmentedTrail(travelRoot, "AB_FarHit_PathTrail_Core", beam, lanceMesh, 9, pathStartX, pathEndX, 0.07f, -0.03f, 0.012f, 0.026f, 0.28f, 2.35f, -2f, new Color(0.90f, 1f, 1f, 0.88f), 9, 0.06f);
        CreateFarHitSegmentedTrail(travelRoot, "AB_FarHit_PathTrail_Sparks", spark, lanceMesh, 10, pathStartX + 0.18f, pathEndX - 0.10f, 0.20f, -0.06f, 0.028f, 0.024f, 0.24f, 0.95f, -7f, new Color(0.58f, 0.92f, 1f, 0.82f), 7, 0.22f);

        CreateImpactFlash(impactRoot, "AB_FarHit_TargetCrossWhite", glow, Vector3.zero, 0.070f, 0.42f, 4.8f, new Color(0.94f, 1f, 1f, 0.92f), 10);
        CreateImpactFlash(impactRoot, "AB_FarHit_BlueHalo", glow, new Vector3(0f, 0f, 0.02f), 0.085f, 0.50f, 5.8f, new Color(0.18f, 0.70f, 1f, 0.44f), 4);
        CreateShardParticle(impactRoot, "AB_FarHit_RicochetShards", spark, new Vector3(0.05f, 0.03f, -0.04f), 0.080f, 9, 6);
        CreateMistParticle(impactRoot, "AB_FarHit_ColdDust", mist, new Vector3(-0.10f, 0.10f, 0.06f), 0.120f, 5, 1.5f, 2.6f, 1);

        SavePrefab(root, "Assets/Prefabs/AnhierBlueWhiteFarHitPrefab.prefab");
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

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        Debug.Log("Prepared Anhier combat prefab: " + path);
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

    private static void CreateMeshParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float zRot, float delay, float lifetime, float size, int sortingOrder, Color color, bool scaleOverLifetime)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 180f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.7f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 3;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.08f, 0.48f);

        if (scaleOverLifetime)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.20f), new Keyframe(0.12f, 1.12f), new Keyframe(0.50f, 0.95f), new Keyframe(1f, 0.52f)));
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateStretchParticle(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, float lengthScale, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.55f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.20f, 0.20f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.86f, 1f, 0.74f), new Color(1f, 1f, 1f, 0.98f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = burst + 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.2f, 0.48f, 0.05f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(2.5f, 7.0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.92f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.16f;
        r.lengthScale = lengthScale;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateFarHitSegmentedTrail(Transform parent, string namePrefix, Material mat, Mesh mesh, int segments, float startX, float endX, float y, float z, float delay, float stepDelay, float lifetime, float size, float zRot, Color color, int sortingOrder, float waveAmplitude)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = segments <= 1 ? 0.5f : i / (float)(segments - 1);
            float centerWeight = Mathf.Sin(t * Mathf.PI);
            float alternatingOffset = (i % 2 == 0 ? 1f : -1f) * waveAmplitude * 0.26f;
            Vector3 pos = new Vector3(
                Mathf.Lerp(startX, endX, t),
                y + Mathf.Sin((t * 1.65f + 0.12f) * Mathf.PI) * waveAmplitude + alternatingOffset,
                z);

            float sectionDelay = delay + stepDelay * i;
            float sectionSize = size * Mathf.Lerp(0.64f, 1.08f, centerWeight);
            Color sectionColor = color;
            sectionColor.a = color.a * Mathf.Lerp(0.56f, 1f, centerWeight);
            CreateFarHitTrailSegment(parent, namePrefix + "_" + (i + 1).ToString("00"), mat, mesh, pos, zRot + alternatingOffset * 16f, sectionDelay, lifetime, sectionSize, sortingOrder, sectionColor);
        }
    }

    private static void CreateFarHitTrailSegment(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float zRot, float delay, float lifetime, float size, int sortingOrder, Color color)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 180f, zRot));
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
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, color.a, 0.10f, 0.38f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.18f), new Keyframe(0.16f, 1.08f), new Keyframe(0.62f, 0.82f), new Keyframe(1f, 0.42f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateImpactFlash(Transform parent, string name, Material mat, Vector3 pos, float delay, float lifetime, float size, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.8f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
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
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.24f), new Keyframe(0.13f, 1f), new Keyframe(1f, 1.34f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateCasterCircleParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float delay, float lifetime, float size, float rotation, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, FarHitCasterCirclePitchDegrees, 0f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.72f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size * FarHitCasterCircleScale;
        main.startRotation = rotation;
        main.startColor = color;
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, color.a, 0.10f, 0.44f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.16f, 1.06f), new Keyframe(0.70f, 1.02f), new Keyframe(1f, 0.72f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateShardParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, -8f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.9f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 8.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.72f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.58f, 0.90f, 1f, 0.80f), new Color(1f, 1f, 1f, 0.98f));
        main.maxParticles = burst + 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.72f;
        shape.angle = 34f;
        shape.arc = 230f;
        shape.rotation = new Vector3(0f, 90f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.92f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.18f;
        r.lengthScale = 2.3f;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateMistParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.8f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.76f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.40f, 0.82f, 1f, 0.36f), new Color(0.90f, 1f, 1f, 0.48f));
        main.maxParticles = burst + 3;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 3.8f;
        shape.arc = 210f;
        shape.rotation = new Vector3(0f, 0f, -18f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateBlueWhiteFadeGradient(0.50f);

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

    private static void GenerateTextures()
    {
        SavePng("Assets/Textures/anhier_combat_beam.png", CreateBeamTexture(512, 128));
        SavePng("Assets/Textures/anhier_combat_glow.png", CreateImpactGlowTexture(512, 512));
        SavePng("Assets/Textures/anhier_combat_spark.png", CreateSparkTexture(256, 256));
        SavePng("Assets/Textures/anhier_combat_mist.png", CreateMistTexture(512, 512));
        SavePng("Assets/Textures/anhier_combat_magic_circle.png", CreateMagicCircleTexture(512, 512));
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
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh CreateNeedleMesh(float width, float height, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float taper = Smooth01(0f, 0.12f, t) * (1f - Smooth01(0.88f, 1f, t));
            float head = Mathf.Lerp(0.60f, 0.18f, Smooth01(0.70f, 1f, t));
            float localHalf = Mathf.Max(height * 0.02f, height * taper * head);
            float y = Mathf.Sin(t * Mathf.PI) * height * 0.12f;

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
        mesh.name = "AnhierCombat_NeedleMesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateDiamondMesh(float width, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = "AnhierCombat_DiamondMesh";
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

    private static Mesh CreateQuadMesh(float size)
    {
        float half = size * 0.5f;
        Mesh mesh = new Mesh();
        mesh.name = "AnhierCombat_CircleQuadMesh";
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

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r * 0.75f, baseColor.g * 0.85f, baseColor.b, 1f), 0f),
                new GradientColorKey(new Color(0.94f, 1f, 1f, 1f), 0.18f),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.52f, holdTime),
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
                new GradientColorKey(new Color(0.92f, 1f, 1f), 0.22f),
                new GradientColorKey(new Color(0.32f, 0.76f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.10f),
                new GradientAlphaKey(peakAlpha * 0.44f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Texture2D CreateBeamTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float taper = Smooth01(0.02f, 0.15f, u) * (1f - Smooth01(0.82f, 1f, u));
                float widthAtU = Mathf.Lerp(0.035f, 0.13f, Mathf.Sin(u * Mathf.PI));
                float core = Mathf.Exp(-Mathf.Pow(Mathf.Abs(v) / Mathf.Max(0.001f, widthAtU), 2f) * 2.2f);
                float glow = Mathf.Exp(-Mathf.Pow(Mathf.Abs(v) / Mathf.Max(0.001f, widthAtU * 3.2f), 2f) * 1.4f);
                float alpha = Mathf.Clamp01((core * 1.15f + glow * 0.35f) * taper);
                if (alpha <= 0.012f) continue;
                Color c = Color.Lerp(new Color(0.03f, 0.34f, 1f, alpha * 0.74f), new Color(0.94f, 1f, 1f, alpha), Mathf.Clamp01(core));
                c.a = alpha;
                pixels[y * width + x] = c;
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
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float core = 1f - Smooth01(0.02f, 0.18f, r);
                float horizontal = Mathf.Max(0f, 1f - Mathf.Abs(v) * 16f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.68f);
                float vertical = Mathf.Max(0f, 1f - Mathf.Abs(u) * 18f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 0.9f);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.48f, 2f) * 1.35f);
                float alpha = Mathf.Clamp01(core + horizontal * 0.82f + vertical * 0.42f + halo * 0.26f);
                alpha *= 1f - Smooth01(0.74f, 0.98f, r);
                if (alpha <= 0.012f) continue;
                Color c = Color.Lerp(new Color(0.12f, 0.62f, 1f, alpha * 0.62f), new Color(0.95f, 1f, 1f, alpha), Mathf.Clamp01(core + horizontal));
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
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                float axisX = Mathf.Max(0f, 1f - Mathf.Abs(v) * 12f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.95f);
                float axisY = Mathf.Max(0f, 1f - Mathf.Abs(u) * 12f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 0.95f);
                float diagA = Mathf.Max(0f, 1f - Mathf.Abs(u - v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float diagB = Mathf.Max(0f, 1f - Mathf.Abs(u + v) * 8f) * Mathf.Max(0f, 1f - (Mathf.Abs(u) + Mathf.Abs(v)) * 0.82f);
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.42f, 2f) * 1.7f);
                float alpha = Mathf.Clamp01(axisX + axisY * 0.70f + (diagA + diagB) * 0.34f + glow * 0.30f);
                alpha *= 1f - Smooth01(0.76f, 1f, r);
                if (alpha <= 0.014f) continue;
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
                float n = Mathf.Sin((u * 9.7f + v * 4.2f) * Mathf.PI) * Mathf.Sin((u * 3.2f - v * 8.3f) * Mathf.PI);
                float alpha = Mathf.Clamp01((1f - Smooth01(0.10f, 0.98f, r)) * (0.11f + 0.14f * n));
                if (alpha <= 0.01f)
                {
                    pixels[y * width + x] = clear;
                    continue;
                }
                pixels[y * width + x] = new Color(0.28f, 0.78f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateMagicCircleTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(u * u + v * v);
                if (r > 0.92f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(v, u);
                float ringOuter = Mathf.Exp(-Mathf.Pow((r - 0.72f) / 0.018f, 2f));
                float ringMid = Mathf.Exp(-Mathf.Pow((r - 0.52f) / 0.014f, 2f)) * 0.68f;
                float ringInner = Mathf.Exp(-Mathf.Pow((r - 0.30f) / 0.012f, 2f)) * 0.54f;
                float radial = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 8f)), 24f) * Smooth01(0.28f, 0.36f, r) * (1f - Smooth01(0.69f, 0.78f, r)) * 0.55f;
                float ticks = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 24f)), 18f) * Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.035f, 2f)) * 0.80f;
                float diamond = Mathf.Max(0f, 1f - Mathf.Abs((Mathf.Abs(u) + Mathf.Abs(v)) - 0.45f) * 26f) * 0.34f;
                float alpha = Mathf.Clamp01(ringOuter + ringMid + ringInner + radial + ticks + diamond);
                alpha *= 1f - Smooth01(0.88f, 0.94f, r);
                if (alpha <= 0.015f)
                {
                    continue;
                }

                Color c = Color.Lerp(new Color(0.10f, 0.58f, 1f, alpha * 0.68f), new Color(0.92f, 1f, 1f, alpha), Mathf.Clamp01(ringOuter + ticks));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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
        Debug.Log("Copied combat bundle to project mod folder: " + localTarget);

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
            Debug.Log("Copied combat bundle to game mod folder: " + target);
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
            string[] prefabNames = { PiercePrefabName, HitPrefabName, FarHitPrefabName };
            foreach (string prefabName in prefabNames)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
                if (prefab == null)
                {
                    throw new Exception("Verify failed: prefab missing " + prefabName);
                }
                int particles = prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
                int renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true).Length;
                if (particles < 7 || renderers < 7)
                {
                    throw new Exception("Verify failed: prefab too sparse " + prefabName + " particles=" + particles + " renderers=" + renderers);
                }
                Debug.Log("Verify combat prefab passed: " + prefabName + " particles=" + particles + " renderers=" + renderers);
            }
        }
        finally
        {
            bundle.Unload(false);
        }
    }
}
