using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChristashaDawnCombatBundleBuilder
{
    private const string BundleName = "steria_christasha_dawn_combat";
    private const string SlashHorizontalPrefabName = "ChristashaDawnSlashHorizontalPrefab";
    private const string SlashVerticalPrefabName = "ChristashaDawnSlashVerticalPrefab";
    private const string PierceNearPrefabName = "ChristashaDawnPierceNearPrefab";
    private const string PierceFarPrefabName = "ChristashaDawnPierceFarPrefab";
    private const string HitPrefabName = "ChristashaDawnHitPrefab";

    private static readonly Color ColorDawnWhite = new Color(1f, 0.98f, 0.78f, 0.96f);
    private static readonly Color ColorDawnGold = new Color(1f, 0.78f, 0.10f, 0.86f);
    private static readonly Color ColorDawnAmber = new Color(1f, 0.48f, 0.02f, 0.62f);
    private static readonly Color ColorDeepBlack = new Color(0.02f, 0.015f, 0.018f, 0.58f);

    [MenuItem("Steria/Build Christasha Dawn Combat Bundle")]
    public static void BuildBundle()
    {
        EnsurePrefabs();
        string output = Path.GetFullPath("Assets/../AssetBundles");
        Directory.CreateDirectory(output);
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        CopyBundleToMod(output);
        VerifyBuiltBundle(output);
        Debug.Log("Built Christasha dawn combat AssetBundle: " + output);
    }

    public static void EnsurePrefabs()
    {
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Shaders");
        Directory.CreateDirectory("Assets/Textures");

        GenerateShaders();
        GenerateTextures();

        Mesh arcWide = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ArcWide.asset", CreateArcMesh("ChristashaDawn_ArcWide", 1.58f, 0.44f, 22, 0f, 1f));
        Mesh arcThin = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ArcThin.asset", CreateArcMesh("ChristashaDawn_ArcThin", 1.48f, 0.18f, 22, 0f, 1f));
        Mesh arcSegA = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ArcSegA.asset", CreateArcMesh("ChristashaDawn_ArcSegA", 1.58f, 0.44f, 12, 0.00f, 0.40f));
        Mesh arcSegB = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ArcSegB.asset", CreateArcMesh("ChristashaDawn_ArcSegB", 1.58f, 0.44f, 12, 0.27f, 0.72f));
        Mesh arcSegC = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ArcSegC.asset", CreateArcMesh("ChristashaDawn_ArcSegC", 1.58f, 0.44f, 12, 0.58f, 1.00f));
        Mesh thinSegA = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ThinSegA.asset", CreateArcMesh("ChristashaDawn_ThinSegA", 1.48f, 0.18f, 12, 0.00f, 0.42f));
        Mesh thinSegB = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ThinSegB.asset", CreateArcMesh("ChristashaDawn_ThinSegB", 1.48f, 0.18f, 12, 0.30f, 0.74f));
        Mesh thinSegC = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ThinSegC.asset", CreateArcMesh("ChristashaDawn_ThinSegC", 1.48f, 0.18f, 12, 0.60f, 1.00f));
        Mesh hSlashSweepEnter = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_HSlashSweepEnter.asset", CreateSlashSweepMesh("ChristashaDawn_HSlashSweepEnter", -5.45f, 5.60f, -0.08f, 1.06f, 0.58f, 28, 0.00f, 0.46f));
        Mesh hSlashSweepMid = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_HSlashSweepMid.asset", CreateSlashSweepMesh("ChristashaDawn_HSlashSweepMid", -5.45f, 5.60f, -0.08f, 1.06f, 0.56f, 34, 0.00f, 0.78f));
        Mesh hSlashSweep = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_HSlashSweep.asset", CreateSlashSweepMesh("ChristashaDawn_HSlashSweep", -5.45f, 5.60f, -0.08f, 1.06f, 0.62f, 42, 0.00f, 1.00f));
        Mesh hSlashSweepCore = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_HSlashSweepCore.asset", CreateSlashSweepMesh("ChristashaDawn_HSlashSweepCore", -5.32f, 5.62f, 0.02f, 0.90f, 0.24f, 42, 0.02f, 0.98f));
        Mesh hSlashSweepRetreat = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_HSlashSweepRetreat.asset", CreateSlashSweepMesh("ChristashaDawn_HSlashSweepRetreat", -5.05f, 5.45f, -0.16f, 0.82f, 0.30f, 30, 0.34f, 1.00f));
        Vector3 hCrescentP0 = new Vector3(0.12f, -0.24f, 0f);
        Vector3 hCrescentP1 = new Vector3(1.02f, 0.98f, 0f);
        Vector3 hCrescentP2 = new Vector3(2.72f, 0.94f, 0f);
        Vector3 hCrescentP3 = new Vector3(3.62f, -0.20f, 0f);
        Mesh hDaoguangEnter = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ReferenceCrescentLead.asset", CreateReferenceCrescentSlashMesh("ChristashaDawn_ReferenceCrescentLead", 4.05f, 1.42f, 0.12f, 0.00f, 0.42f));
        Mesh hDaoguangMid = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ReferenceCrescentMid.asset", CreateReferenceCrescentSlashMesh("ChristashaDawn_ReferenceCrescentMid", 4.10f, 1.48f, 0.14f, 0.00f, 0.76f));
        Mesh hDaoguangFull = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ReferenceCrescentEnd.asset", CreateReferenceCrescentSlashMesh("ChristashaDawn_ReferenceCrescentEnd", 4.12f, 1.52f, 0.16f, 0.00f, 1.00f));
        Mesh hDaoguangBelly = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ReferenceCrescentBelly.asset", CreateReferenceCrescentSlashMesh("ChristashaDawn_ReferenceCrescentBelly", 4.22f, 1.62f, 0.18f, 0.00f, 1.00f));
        Mesh hDaoguangExit = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_ReferenceCrescentCore.asset", CreateReferenceCrescentSlashMesh("ChristashaDawn_ReferenceCrescentCore", 4.00f, 1.18f, 0.08f, 0.08f, 1.00f));
        Mesh vCleaveEnterTop = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VCleaveEnterTop.asset", CreateVerticalCleaveSweepMesh("ChristashaDawn_VCleaveEnterTop", 2.42f, -2.22f, 0.08f, 0.56f, -0.28f, 24, 0.00f, 0.42f));
        Mesh vCleaveMid = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VCleaveMid.asset", CreateVerticalCleaveSweepMesh("ChristashaDawn_VCleaveMid", 2.42f, -2.22f, 0.08f, 0.58f, -0.28f, 30, 0.00f, 0.76f));
        Mesh vCleaveFull = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VCleaveFull.asset", CreateVerticalCleaveSweepMesh("ChristashaDawn_VCleaveFull", 2.42f, -2.22f, 0.08f, 0.62f, -0.28f, 38, 0.00f, 1.00f));
        Mesh vCleaveCore = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VCleaveCore.asset", CreateVerticalCleaveSweepMesh("ChristashaDawn_VCleaveCore", 2.30f, -2.12f, 0.10f, 0.26f, -0.22f, 38, 0.02f, 0.98f));
        Mesh vCleaveRetreat = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VCleaveRetreat.asset", CreateVerticalCleaveSweepMesh("ChristashaDawn_VCleaveRetreat", 1.98f, -2.20f, 0.16f, 0.34f, -0.18f, 26, 0.30f, 1.00f));
        Vector3 vCrescentP0 = new Vector3(-0.22f, 1.22f, 0f);
        Vector3 vCrescentP1 = new Vector3(0.66f, 0.54f, 0f);
        Vector3 vCrescentP2 = new Vector3(0.58f, -0.58f, 0f);
        Vector3 vCrescentP3 = new Vector3(-0.18f, -1.24f, 0f);
        Mesh vDaoguangEnterTop = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VBezierCrescentLeadTop.asset", CreateBezierCrescentMesh("ChristashaDawn_VBezierCrescentLeadTop", vCrescentP0, vCrescentP1, vCrescentP2, vCrescentP3, 0.210f, 24, 0.00f, 0.40f));
        Mesh vDaoguangMid = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VBezierCrescentMid.asset", CreateBezierCrescentMesh("ChristashaDawn_VBezierCrescentMid", vCrescentP0, vCrescentP1, vCrescentP2, vCrescentP3, 0.210f, 32, 0.00f, 0.74f));
        Mesh vDaoguangFull = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VBezierCrescentEndBottom.asset", CreateBezierCrescentMesh("ChristashaDawn_VBezierCrescentEndBottom", vCrescentP0, vCrescentP1, vCrescentP2, vCrescentP3, 0.210f, 42, 0.00f, 1.00f));
        Mesh vDaoguangExitLower = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_VBezierCrescentCore.asset", CreateBezierCrescentMesh("ChristashaDawn_VBezierCrescentCore", vCrescentP0, vCrescentP1, vCrescentP2, vCrescentP3, 0.080f, 44, 0.03f, 0.98f));
        Mesh[] vCrescentOuterReveal = CreateVerticalCrescentRevealMeshes("Assets/Meshes/ChristashaDawn_VParticleOuter", "ChristashaDawn_VParticleOuter", 5.05f, 0.72f, 1.02f, 0.34f, 0.34f, 0.00f);
        Mesh[] vCrescentInnerReveal = CreateVerticalCrescentRevealMeshes("Assets/Meshes/ChristashaDawn_VParticleInner", "ChristashaDawn_VParticleInner", 4.78f, 0.45f, 0.82f, 0.20f, 0.22f, -0.018f);
        Mesh[] vCrescentShadowReveal = CreateVerticalCrescentRevealMeshes("Assets/Meshes/ChristashaDawn_VParticleShadow", "ChristashaDawn_VParticleShadow", 4.70f, 0.30f, 0.68f, 0.15f, 0.12f, 0.035f);
        Mesh[] vCrescentGlowReveal = CreateVerticalCrescentRevealMeshes("Assets/Meshes/ChristashaDawn_VParticleGlow", "ChristashaDawn_VParticleGlow", 5.24f, 1.00f, 1.32f, 0.48f, 0.40f, 0.018f);
        Mesh[] vCrescentCoreReveal = CreateVerticalCrescentRevealMeshes("Assets/Meshes/ChristashaDawn_VParticleCore", "ChristashaDawn_VParticleCore", 4.95f, 0.18f, 0.32f, 0.09f, 0.16f, -0.050f);
        Mesh[] providedVerticalLayerMeshes = CreateProvidedVerticalCrescentLayerMeshes();
        Mesh lanceMesh = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_LanceMesh.asset", CreateNeedleMesh("ChristashaDawn_LanceMesh", 1.35f, 0.16f, 16));
        Mesh cleaveMesh = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_CleaveMesh.asset", CreateNeedleMesh("ChristashaDawn_CleaveMesh", 1.22f, 0.26f, 16));
        Mesh diamondMesh = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_DiamondMesh.asset", CreateDiamondMesh("ChristashaDawn_DiamondMesh", 0.92f, 0.38f));
        Mesh quadMesh = CreateOrReplaceMesh("Assets/Meshes/ChristashaDawn_QuadMesh.asset", CreateQuadMesh("ChristashaDawn_QuadMesh", 1f));

        Material blade = CreateMaterial("Assets/Textures/christasha_dawn_blade.png", "Assets/Materials/ChristashaDawn_Blade_Add.mat", ColorDawnWhite, false);
        Material glow = CreateMaterial("Assets/Textures/christasha_dawn_glow.png", "Assets/Materials/ChristashaDawn_Glow_Add.mat", ColorDawnGold, false);
        Material star = CreateMaterial("Assets/Textures/christasha_dawn_star.png", "Assets/Materials/ChristashaDawn_Star_Add.mat", ColorDawnWhite, false);
        Material dots = CreateMaterial("Assets/Textures/christasha_dawn_dots.png", "Assets/Materials/ChristashaDawn_Dots_Add.mat", ColorDawnGold, false);
        Material black = CreateMaterial("Assets/Textures/christasha_dawn_black.png", "Assets/Materials/ChristashaDawn_Black_Alpha.mat", ColorDeepBlack, true);
        Material circle = CreateMaterial("Assets/Textures/christasha_dawn_circle.png", "Assets/Materials/ChristashaDawn_Circle_Add.mat", ColorDawnGold, false);
        Material daoguang = CreateMaterial("Assets/Textures/christasha_dawn_daoguang.png", "Assets/Materials/ChristashaDawn_Daoguang_Add.mat", ColorDawnWhite, false);
        Material daoguangGold = CreateMaterial("Assets/Textures/christasha_dawn_daoguang.png", "Assets/Materials/ChristashaDawn_DaoguangGold_Add.mat", ColorDawnGold, false);
        Material daoguangDark = CreateMaterial("Assets/Textures/christasha_dawn_daoguang.png", "Assets/Materials/ChristashaDawn_DaoguangDark_Alpha.mat", ColorDeepBlack, true);
        Material daoguangVertical = CreateMaterial("Assets/Textures/christasha_dawn_daoguang_vertical.png", "Assets/Materials/ChristashaDawn_DaoguangVertical_Add.mat", ColorDawnWhite, false);
        Material daoguangVerticalGold = CreateMaterial("Assets/Textures/christasha_dawn_daoguang_vertical.png", "Assets/Materials/ChristashaDawn_DaoguangVerticalGold_Add.mat", ColorDawnGold, false);
        Material daoguangVerticalDark = CreateMaterial("Assets/Textures/christasha_dawn_daoguang_vertical.png", "Assets/Materials/ChristashaDawn_DaoguangVerticalDark_Alpha.mat", ColorDeepBlack, true);
        Material crescentGlow = CreateCrescentSlashMaterial("Assets/Textures/christasha_dawn_crescent_particle.png", "Assets/Materials/ChristashaDawn_CrescentGlow_Flow.mat", new Color(1f, 0.42f, 0.02f, 0.34f), 1.25f, 0.12f, 0.24f);
        Material crescentOuter = CreateCrescentSlashMaterial("Assets/Textures/christasha_dawn_crescent_particle.png", "Assets/Materials/ChristashaDawn_CrescentOuter_Flow.mat", new Color(1f, 0.56f, 0.04f, 0.68f), 1.70f, 0.080f, 0.38f);
        Material crescentInner = CreateCrescentSlashMaterial("Assets/Textures/christasha_dawn_crescent_particle.png", "Assets/Materials/ChristashaDawn_CrescentInner_Flow.mat", new Color(1f, 0.92f, 0.46f, 0.62f), 1.95f, 0.055f, 0.52f);
        Material crescentCore = CreateCrescentSlashMaterial("Assets/Textures/christasha_dawn_crescent_core.png", "Assets/Materials/ChristashaDawn_CrescentCore_Flow.mat", new Color(1f, 0.98f, 0.72f, 0.86f), 3.25f, 0.038f, 0.68f);
        Material crescentShadow = CreateMaterial("Assets/Textures/christasha_dawn_crescent_shadow.png", "Assets/Materials/ChristashaDawn_CrescentShadow_Alpha.mat", new Color(0.13f, 0.075f, 0.025f, 0.42f), true);
        Material[] providedVerticalLayers = CreateProvidedVerticalCrescentLayerMaterials();

        Mesh[] wideSegments = { arcSegA, arcSegB, arcSegC };
        Mesh[] thinSegments = { thinSegA, thinSegB, thinSegC };
        CreateSlashHorizontalPrefab(blade, glow, star, dots, black, daoguang, daoguangGold, daoguangDark, hSlashSweepEnter, hSlashSweepMid, hSlashSweep, hSlashSweepCore, hSlashSweepRetreat, hDaoguangEnter, hDaoguangMid, hDaoguangFull, hDaoguangBelly, hDaoguangExit);
        CreateSlashVerticalPrefab(star, dots, black, crescentGlow, crescentOuter, crescentInner, crescentCore, crescentShadow, vCrescentGlowReveal, vCrescentOuterReveal, vCrescentInnerReveal, vCrescentCoreReveal, vCrescentShadowReveal, quadMesh);
        CreateMeleePiercePrefab(blade, glow, star, dots, black, lanceMesh, diamondMesh);
        CreateFarPiercePrefab(blade, glow, star, dots, black, circle, lanceMesh, diamondMesh, quadMesh);
        CreateHitPrefab(blade, glow, star, dots, black, cleaveMesh, diamondMesh, quadMesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateSlashHorizontalPrefab(Material blade, Material glow, Material star, Material dots, Material black, Material daoguang, Material daoguangGold, Material daoguangDark, Mesh sweepEnter, Mesh sweepMid, Mesh sweepFull, Mesh sweepCore, Mesh sweepRetreat, Mesh daoguangEnter, Mesh daoguangMid, Mesh daoguangFull, Mesh daoguangBelly, Mesh daoguangExit)
    {
        GameObject root = CreateRoot(SlashHorizontalPrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(7.55f, 0.12f, -0.04f));

        CreateMeshParticle3D(travelRoot, "AB_DawnSlashH_CrescentBackRim3D", daoguangDark, daoguangBelly, new Vector3(0.00f, -0.20f, 0.12f), new Vector3(12f, 0f, -8f), 0.260f, 0.64f, 6.8f, 116, new Color(0.05f, 0.03f, 0.012f, 0.34f), false);
        CreateMeshParticle3D(travelRoot, "AB_DawnSlashH_CrescentBellyBody3D", daoguangGold, daoguangBelly, new Vector3(0.00f, -0.14f, -0.02f), new Vector3(4f, 0f, -5f), 0.120f, 0.78f, 6.9f, 128, new Color(1f, 0.70f, 0.06f, 0.34f), true);
        CreateMeshParticle(travelRoot, "AB_DawnSlashH_CrescentSoftGlow", daoguangGold, daoguangExit, new Vector3(0.00f, -0.12f, 0.01f), -4f, 0.260f, 0.72f, 7.1f, 118, new Color(1f, 0.70f, 0.05f, 0.34f), false);
        CreateMeshParticle(travelRoot, "AB_DawnSlashH_CrescentMainLeadBezier", daoguang, daoguangEnter, new Vector3(0.00f, -0.10f, -0.06f), -4f, 0.000f, 0.82f, 6.2f, 142, new Color(1f, 0.98f, 0.78f, 0.88f), false);
        CreateMeshParticle(travelRoot, "AB_DawnSlashH_CrescentMainMidBezier", daoguang, daoguangMid, new Vector3(0.00f, -0.11f, -0.065f), -4f, 0.180f, 0.72f, 6.4f, 143, new Color(1f, 0.98f, 0.78f, 0.94f), false);
        CreateMeshParticle(travelRoot, "AB_DawnSlashH_CrescentMainEndBezier", daoguang, daoguangFull, new Vector3(0.00f, -0.10f, -0.07f), -4f, 0.380f, 0.62f, 6.6f, 144, new Color(1f, 0.98f, 0.78f, 0.90f), false);
        CreateMeshParticle(travelRoot, "AB_DawnSlashH_CrescentCore", blade, daoguangExit, new Vector3(0.02f, -0.08f, -0.10f), -4f, 0.420f, 0.58f, 4.8f, 148, new Color(1f, 0.98f, 0.76f, 0.54f), false);
        CreateMeshParticle3D(travelRoot, "AB_DawnSlashH_CrescentRotatingSheen3D", blade, daoguangFull, new Vector3(0.06f, -0.06f, -0.14f), new Vector3(-8f, 0f, -1f), 0.240f, 0.58f, 5.7f, 151, new Color(1f, 0.98f, 0.82f, 0.46f), true);
        CreateMeshParticle3D(travelRoot, "AB_DawnSlashH_CrescentFrontEdge3D", blade, daoguangExit, new Vector3(0.04f, -0.02f, -0.18f), new Vector3(-12f, 0f, -2f), 0.320f, 0.54f, 5.2f, 152, new Color(1f, 0.98f, 0.82f, 0.54f), true);
        CreateStarParticle(travelRoot, "AB_DawnSlashH_CrescentDust", star, new Vector3(0.10f, -0.04f, -0.10f), -4f, 0.520f, 8, 0.09f, 0.22f, 1.2f, 3.0f, 132);
        CreateDotMatrixParticle(travelRoot, "AB_DawnSlashH_CrescentFadeDots", dots, new Vector3(0.10f, -0.08f, -0.12f), 0.700f, 12, 0.05f, 0.16f, 0.8f, 2.5f, 126);

        CreateImpactFlash(impactRoot, "AB_DawnSlashH_CrescentImpactWhite", star, Vector3.zero, 0.700f, 0.32f, 5.2f, new Color(1f, 0.98f, 0.78f, 0.68f), 144);
        CreateImpactFlash(impactRoot, "AB_DawnSlashH_CrescentImpactGold", glow, new Vector3(0f, 0f, 0.03f), 0.720f, 0.36f, 6.4f, new Color(1f, 0.62f, 0.04f, 0.34f), 116);
        CreateImpactFlash(impactRoot, "AB_DawnSlashH_CrescentImpactShade", black, new Vector3(0f, -0.02f, 0.05f), 0.690f, 0.30f, 4.8f, new Color(0.02f, 0.015f, 0.018f, 0.24f), 84);

        SavePrefab(root, "Assets/Prefabs/ChristashaDawnSlashHorizontalPrefab.prefab");
    }

    private static void CreateSlashVerticalPrefab(Material star, Material dots, Material black, Material crescentGlow, Material crescentOuter, Material crescentInner, Material crescentCore, Material crescentShadow, Mesh[] glowReveal, Mesh[] outerReveal, Mesh[] innerReveal, Mesh[] coreReveal, Mesh[] shadowReveal, Mesh quadMesh)
    {
        GameObject root = CreateRoot(SlashVerticalPrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", new Vector3(1.25f, 0f, 0f));
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(7.25f, 0.16f, -0.04f));

        CreateCrescentVerticalParticleSlash(
            travelRoot,
            "AB_DawnSlashV_TopCrossStar",
            "AB_DawnSlashV_BottomCrossStar",
            "AB_DawnSlashV_SoftOuterGlow",
            "AB_DawnSlashV_OuterDeepGold",
            "AB_DawnSlashV_InnerPaleGold",
            "AB_DawnSlashV_WhiteCore",
            "AB_DawnSlashV_InnerBrownBlackStarfield",
            star,
            dots,
            black,
            crescentGlow,
            crescentOuter,
            crescentInner,
            crescentCore,
            crescentShadow,
            glowReveal,
            outerReveal,
            innerReveal,
            coreReveal,
            shadowReveal,
            quadMesh);
        CreateImpactFlash(impactRoot, "AB_DawnSlashV_CrescentImpactWhite", star, Vector3.zero, 0.500f, 0.20f, 1.18f, new Color(1f, 0.98f, 0.78f, 0.050f), 144);
        CreateImpactFlash(impactRoot, "AB_DawnSlashV_CrescentImpactShade", black, new Vector3(0f, -0.02f, 0.05f), 0.485f, 0.30f, 2.0f, new Color(0.02f, 0.015f, 0.018f, 0.070f), 84);

        SavePrefab(root, "Assets/Prefabs/ChristashaDawnSlashVerticalPrefab.prefab");
    }

    private static void CreateMeleePiercePrefab(Material blade, Material glow, Material star, Material dots, Material black, Mesh lanceMesh, Mesh diamondMesh)
    {
        GameObject root = CreateRoot(PierceNearPrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(6.2f, 0.08f, -0.04f));

        CreatePiercePathSegments(travelRoot, "AB_DawnPierceNear_BlackUnderPath", black, lanceMesh, 7, -3.15f, 3.10f, -0.05f, 0.07f, 0.000f, 0.020f, 0.40f, 2.8f, -1f, ColorDeepBlack, 80);
        CreatePiercePathSegments(travelRoot, "AB_DawnPierceNear_GoldThrustPath", glow, lanceMesh, 8, -3.05f, 3.20f, 0.00f, 0.00f, 0.018f, 0.018f, 0.34f, 3.0f, 0f, ColorDawnGold, 112);
        CreatePiercePathSegments(travelRoot, "AB_DawnPierceNear_WhiteCorePath", blade, lanceMesh, 9, -2.95f, 3.30f, 0.08f, -0.05f, 0.040f, 0.016f, 0.28f, 2.7f, 1f, ColorDawnWhite, 134);
        CreateStarParticle(travelRoot, "AB_DawnPierceNear_InlineStars", star, new Vector3(0.10f, 0.08f, -0.10f), 0f, 0.060f, 10, 0.07f, 0.22f, 2.8f, 6.4f, 138);
        CreateDotMatrixParticle(travelRoot, "AB_DawnPierceNear_DotCutFade", dots, new Vector3(0.16f, -0.03f, -0.12f), 0.115f, 18, 0.05f, 0.18f, 1.0f, 3.6f, 128);

        CreateMeshParticle(impactRoot, "AB_DawnPierceNear_TargetBlackNeedle", black, diamondMesh, Vector3.zero, 0f, 0.050f, 0.32f, 3.2f, 82, ColorDeepBlack, true);
        CreateImpactFlash(impactRoot, "AB_DawnPierceNear_TargetWhitePuncture", star, Vector3.zero, 0.070f, 0.28f, 3.2f, ColorDawnWhite, 142);
        CreateImpactFlash(impactRoot, "AB_DawnPierceNear_TargetGoldPin", glow, new Vector3(0.04f, 0f, 0.02f), 0.090f, 0.32f, 3.6f, new Color(1f, 0.66f, 0.06f, 0.34f), 116);
        CreateDotMatrixParticle(impactRoot, "AB_DawnPierceNear_TargetFineDots", dots, new Vector3(0.08f, 0.05f, -0.04f), 0.105f, 12, 0.06f, 0.20f, 1.6f, 4.2f, 136);

        SavePrefab(root, "Assets/Prefabs/ChristashaDawnPierceNearPrefab.prefab");
    }

    private static void CreateFarPiercePrefab(Material blade, Material glow, Material star, Material dots, Material black, Material circle, Mesh lanceMesh, Mesh diamondMesh, Mesh quadMesh)
    {
        GameObject root = CreateRoot(PierceFarPrefabName);
        Transform casterRoot = CreateGroup(root.transform, "AB_CasterRoot", new Vector3(0.72f, 0.20f, -0.02f));
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(8.5f, 0.12f, -0.04f));

        CreateCasterCircleParticle(casterRoot, "AB_DawnPierceFar_CasterCircleBlack", black, quadMesh, Vector3.zero, 0.000f, 0.58f, 3.4f, 0f, ColorDeepBlack, 78);
        CreateCasterCircleParticle(casterRoot, "AB_DawnPierceFar_CasterCircleGold", circle, quadMesh, new Vector3(0.02f, 0f, -0.02f), 0.025f, 0.56f, 2.7f, 0.40f, new Color(1f, 0.74f, 0.08f, 0.72f), 114);
        CreateCasterCircleParticle(casterRoot, "AB_DawnPierceFar_CasterCircleWhite", circle, quadMesh, new Vector3(0.04f, 0f, -0.04f), 0.050f, 0.42f, 2.1f, -0.30f, ColorDawnWhite, 128);

        CreatePiercePathSegments(travelRoot, "AB_DawnPierceFar_StaticGoldPath", glow, lanceMesh, 10, -4.4f, 4.4f, -0.04f, -0.04f, 0.000f, 0.025f, 0.30f, 2.8f, -1f, ColorDawnGold, 106);
        CreatePiercePathSegments(travelRoot, "AB_DawnPierceFar_StaticWhiteNeedles", blade, lanceMesh, 12, -4.3f, 4.5f, 0.09f, -0.08f, 0.030f, 0.020f, 0.24f, 2.25f, 1f, ColorDawnWhite, 128);
        CreateStarParticle(travelRoot, "AB_DawnPierceFar_FlyingStars", star, new Vector3(0.10f, 0.12f, -0.10f), 0f, 0.055f, 16, 0.08f, 0.28f, 4.5f, 9.5f, 136);
        CreateDotMatrixParticle(travelRoot, "AB_DawnPierceFar_DotAfterimage", dots, new Vector3(0.15f, -0.04f, -0.12f), 0.120f, 26, 0.06f, 0.22f, 1.8f, 5.2f, 130);

        CreateMeshParticle(impactRoot, "AB_DawnPierceFar_BlackImpactPocket", black, diamondMesh, Vector3.zero, 0f, 0.055f, 0.42f, 5.5f, 80, ColorDeepBlack, true);
        CreateImpactFlash(impactRoot, "AB_DawnPierceFar_TargetWhiteStar", star, Vector3.zero, 0.085f, 0.34f, 5.2f, ColorDawnWhite, 142);
        CreateImpactFlash(impactRoot, "AB_DawnPierceFar_TargetGoldBurst", glow, new Vector3(0.06f, 0f, 0.03f), 0.100f, 0.42f, 6.0f, new Color(1f, 0.66f, 0.06f, 0.48f), 116);
        CreateDotMatrixParticle(impactRoot, "AB_DawnPierceFar_TargetDissolveDots", dots, new Vector3(0.12f, 0.08f, -0.04f), 0.120f, 22, 0.08f, 0.30f, 2.4f, 6.0f, 140);

        SavePrefab(root, "Assets/Prefabs/ChristashaDawnPierceFarPrefab.prefab");
    }

    private static void CreateHitPrefab(Material blade, Material glow, Material star, Material dots, Material black, Mesh cleaveMesh, Mesh diamondMesh, Mesh quadMesh)
    {
        GameObject root = CreateRoot(HitPrefabName);
        Transform travelRoot = CreateGroup(root.transform, "AB_TravelRoot", Vector3.zero);
        Transform impactRoot = CreateGroup(root.transform, "AB_ImpactRoot", new Vector3(0f, 0.18f, -0.04f));

        CreateMeshParticle(travelRoot, "AB_DawnHit_OverheadBlackColumn", black, cleaveMesh, new Vector3(0f, 1.00f, 0.08f), -90f, 0.000f, 0.62f, 6.4f, 78, ColorDeepBlack, true);
        CreateMeshParticle(travelRoot, "AB_DawnHit_DescendingGoldCleave", glow, cleaveMesh, new Vector3(0f, 0.82f, 0.02f), -90f, 0.035f, 0.46f, 6.0f, 112, ColorDawnGold, true);
        CreateMeshParticle(travelRoot, "AB_DawnHit_DescendingWhiteCore", blade, cleaveMesh, new Vector3(0f, 0.66f, -0.04f), -90f, 0.075f, 0.34f, 5.5f, 136, ColorDawnWhite, true);
        CreateStarParticle(travelRoot, "AB_DawnHit_FlyingStarShards", star, new Vector3(0.02f, 0.32f, -0.08f), -90f, 0.085f, 18, 0.10f, 0.32f, 3.0f, 7.2f, 138);
        CreateDotMatrixParticle(travelRoot, "AB_DawnHit_DotColumnFade", dots, new Vector3(0f, 0.20f, -0.12f), 0.150f, 32, 0.08f, 0.26f, 1.2f, 4.4f, 130);

        CreateMeshParticle(impactRoot, "AB_DawnHit_BlackSplitPocket", black, diamondMesh, Vector3.zero, 0f, 0.075f, 0.44f, 5.8f, 82, ColorDeepBlack, true);
        CreateImpactFlash(impactRoot, "AB_DawnHit_WhiteImpactStar", star, Vector3.zero, 0.095f, 0.42f, 5.6f, ColorDawnWhite, 144);
        CreateImpactFlash(impactRoot, "AB_DawnHit_GoldImpactBloom", glow, new Vector3(0f, 0f, 0.03f), 0.110f, 0.54f, 6.8f, new Color(1f, 0.64f, 0.04f, 0.48f), 116);
        CreateDotMatrixParticle(impactRoot, "AB_DawnHit_ImpactDissolveDots", dots, new Vector3(0.08f, 0.10f, -0.05f), 0.130f, 26, 0.09f, 0.32f, 2.2f, 5.8f, 140);

        SavePrefab(root, "Assets/Prefabs/ChristashaDawnHitPrefab.prefab");
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
        Debug.Log("Prepared Christasha dawn prefab: " + path);
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

    private static void CreateSequentialArcLayer(Transform parent, string namePrefix, Material mat, Mesh[] segments, float zRot, float delay, float stepDelay, float lifetime, float size, int sortingOrder, Color color)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            Color sectionColor = color;
            sectionColor.a = color.a * Mathf.Lerp(0.72f, 1f, i / Mathf.Max(1f, segments.Length - 1f));
            CreateMeshParticle(parent, namePrefix + "_" + (i + 1).ToString("00"), mat, segments[i], new Vector3(0.08f * i, 0.02f * i, -0.02f * i), zRot, delay + stepDelay * i, lifetime, size, sortingOrder + i, sectionColor, true);
        }
    }

    private static void CreatePiercePathSegments(Transform parent, string namePrefix, Material mat, Mesh mesh, int segments, float startX, float endX, float y, float z, float delay, float stepDelay, float lifetime, float size, float zRot, Color color, int sortingOrder)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = segments <= 1 ? 0.5f : i / (float)(segments - 1);
            float centerWeight = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin((t * 1.7f + 0.18f) * Mathf.PI) * 0.12f;
            Vector3 pos = new Vector3(Mathf.Lerp(startX, endX, t), y + wave, z);
            Color sectionColor = color;
            sectionColor.a = color.a * Mathf.Lerp(0.48f, 1f, centerWeight);
            CreateMeshParticle(parent, namePrefix + "_" + (i + 1).ToString("00"), mat, mesh, pos, zRot + wave * 14f, delay + stepDelay * i, lifetime, size * Mathf.Lerp(0.62f, 1.05f, centerWeight), sortingOrder + i % 3, sectionColor, true);
        }
    }

    private static void CreateMeshParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float zRot, float delay, float lifetime, float size, int sortingOrder, Color color, bool scaleOverLifetime)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 180f, zRot));
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
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.10f, 0.52f);

        if (scaleOverLifetime)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.16f), new Keyframe(0.12f, 1.12f), new Keyframe(0.56f, 0.98f), new Keyframe(1f, 0.44f)));
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateMeshParticle3D(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, Vector3 euler, float delay, float lifetime, float size, int sortingOrder, Color color, bool scaleOverLifetime)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(euler.x, euler.y, euler.z));
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
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a), 0.12f, 0.48f);

        if (scaleOverLifetime)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.18f), new Keyframe(0.14f, 1.08f), new Keyframe(0.58f, 0.98f), new Keyframe(1f, 0.38f)));

            var rotOver = ps.rotationOverLifetime;
            rotOver.enabled = true;
            rotOver.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 0.42f);
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateTexturedSlashWipe3D(Transform parent, string name, Material mat, Mesh[] meshes, int layerIndex, Vector3 pos, Vector3 euler, float delay, float lifetime, float size, int sortingOrder, Color color, float peakTime, float holdTime, float fadeStart)
    {
        const int layerCount = 6;
        const int stripCount = 10;
        float wipeTime = 0.34f;
        float textureFadeStart = 0.42f;
        float textureCutOutTime = 0.24f;
        for (int strip = 0; strip < stripCount; strip++)
        {
            float n = strip / (float)(stripCount - 1);
            float energyTiming = EvaluateAcceleratingSlashTiming(n);
            float stripDelay = delay + wipeTime * energyTiming;
            float stripEndTime = delay + textureFadeStart + textureCutOutTime * energyTiming;
            float stripLifetime = Mathf.Max(0.24f, stripEndTime - stripDelay);
            Color stripColor = color;
            stripColor.a *= Mathf.Lerp(0.96f, 0.68f, n);
            Vector3 stripPos = pos + new Vector3(0.004f * Mathf.Sin(n * Mathf.PI), -0.004f * strip, -0.003f * strip);
            int meshIndex = layerCount + layerIndex * stripCount + strip;
            CreateTexturedSlashLayer3D(parent, name + "_Wipe_" + (strip + 1).ToString("00"), mat, meshes[meshIndex], stripPos, euler, stripDelay, stripLifetime, size, sortingOrder + strip % 3, stripColor, peakTime, holdTime, fadeStart, strip == 0 || strip == stripCount - 1);
        }
    }

    private static float EvaluateAcceleratingSlashTiming(float normalizedPosition)
    {
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        return Mathf.Sqrt(normalizedPosition);
    }

    private static void CreateTexturedSlashLayer3D(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, Vector3 euler, float delay, float lifetime, float size, int sortingOrder, Color color, float peakTime, float holdTime, float fadeStart, bool subtleScale)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(euler.x, euler.y, euler.z));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.18f;
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
        col.color = CreateTextureCutoutGradient(color, Mathf.Min(1f, color.a), peakTime, holdTime, fadeStart);

        if (subtleScale)
        {
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.92f), new Keyframe(0.22f, 1.01f), new Keyframe(0.72f, 1.00f), new Keyframe(1f, 0.96f)));
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateStarParticle(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.9f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ColorDawnGold, ColorDawnWhite);
        main.maxParticles = burst + 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(4.8f, 1.1f, 0.05f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(1.0f, 4.0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-2.4f, 2.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateDawnFadeGradient(0.92f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.32f;
        noise.frequency = 1.3f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.12f;
        r.lengthScale = 1.8f;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateSlashTangentParticles(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float minSize, float maxSize, Color color, bool stretch, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.94f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = burst + 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(0.70f, 2.10f, 0.04f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0.40f, 1.60f);
        velocity.y = new ParticleSystem.MinMaxCurve(-4.60f, -2.10f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateSlashTraceGradient(color, color.a, 0.10f, 0.34f, 0.62f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.72f), new Keyframe(0.18f, 1.0f), new Keyframe(1f, 0f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.10f;
        noise.frequency = 1.8f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretch)
        {
            r.velocityScale = 0.08f;
            r.lengthScale = 1.20f;
        }
        r.sortingOrder = sortingOrder;
    }

    private static void CreateCrescentVerticalParticleSlash(Transform parent, string topStarName, string bottomStarName, string glowName, string outerName, string innerName, string coreName, string shadowName, Material star, Material dots, Material black, Material crescentGlow, Material crescentOuter, Material crescentInner, Material crescentCore, Material crescentShadow, Mesh[] glowReveal, Mesh[] outerReveal, Mesh[] innerReveal, Mesh[] coreReveal, Mesh[] shadowReveal, Mesh quadMesh)
    {
        Vector3 crescentPos = new Vector3(0.05f, -0.16f, -0.24f);
        Vector3 crescentEuler = new Vector3(-12f, 16f, -4f);
        CreateCrescentStarParticle(parent, topStarName, star, quadMesh, new Vector3(0.07f, 2.24f, -0.32f), new Vector3(-10f, 16f, -4f), 0.000f, 0.46f, 1.02f, new Color(1f, 0.96f, 0.66f, 0.34f), 166);

        for (int i = 0; i < outerReveal.Length; i++)
        {
            float n = i / (float)(outerReveal.Length - 1);
            float energyTiming = EvaluateAcceleratingSlashTiming(n);
            float delay = 0.025f + 0.34f * energyTiming;
            float glowAlpha = Mathf.Lerp(0.18f, 0.11f, n);
            float outerAlpha = Mathf.Lerp(0.66f, 0.48f, n);
            float innerAlpha = Mathf.Lerp(0.52f, 0.40f, n);
            float coreAlpha = Mathf.Lerp(0.54f, 0.34f, n);
            float shadowAlpha = Mathf.Lerp(0.26f, 0.16f, n);

            string label = (i + 1).ToString("00");
            Vector3 sweepOffset = new Vector3(0.010f * i, -0.005f * i, -0.004f * i);
            CreateCrescentBladeParticle(parent, glowName + "_" + label, crescentGlow, glowReveal[i], crescentPos + sweepOffset + new Vector3(0.020f, -0.006f, 0.026f), crescentEuler, Mathf.Max(0f, delay - 0.016f), 0.76f, 1.03f, 136 + i, new Color(1f, 0.50f, 0.03f, glowAlpha), true, true);
            CreateCrescentBladeParticle(parent, outerName + "_" + label, crescentOuter, outerReveal[i], crescentPos + sweepOffset, crescentEuler, delay, 0.70f, 1.02f, 150 + i, new Color(1f, 0.58f, 0.04f, outerAlpha), true, true);
            CreateCrescentBladeParticle(parent, innerName + "_" + label, crescentInner, innerReveal[i], crescentPos + sweepOffset + new Vector3(0.035f, 0.002f, -0.030f), crescentEuler, delay + 0.014f, 0.64f, 1.00f, 158 + i, new Color(1f, 0.90f, 0.46f, innerAlpha), false, true);
            CreateCrescentBladeParticle(parent, coreName + "_" + label, crescentCore, coreReveal[i], crescentPos + sweepOffset + new Vector3(0.060f, 0.010f, -0.060f), crescentEuler, delay + 0.020f, 0.36f, 1.00f, 170 + i, new Color(1f, 0.98f, 0.78f, coreAlpha), false, true);
            CreateCrescentBladeParticle(parent, shadowName + "_Mesh_" + label, crescentShadow, shadowReveal[i], crescentPos + sweepOffset + new Vector3(-0.045f, -0.016f, 0.040f), new Vector3(-14f, 16f, -4f), delay + 0.030f, 0.76f, 1.00f, 120 + i, new Color(0.12f, 0.064f, 0.018f, shadowAlpha), false, false);
        }

        CreateCrescentEdgeDust(parent, "AB_DawnSlashV_OuterEdgeDust", star, new Vector3(0.22f, -0.12f, -0.28f), new Vector3(0.68f, 4.48f, 0.10f), 0.090f, 24, new Color(1f, 0.72f, 0.12f, 0.20f), 142);
        CreateCrescentEdgeDust(parent, "AB_DawnSlashV_OuterPaleDissolve", dots, new Vector3(0.14f, -0.24f, -0.30f), new Vector3(0.62f, 4.08f, 0.08f), 0.190f, 20, new Color(1f, 0.92f, 0.56f, 0.15f), 146);
        CreateCrescentShadowDust(parent, shadowName, black, new Vector3(-0.08f, -0.18f, -0.10f), 0.170f, 20, new Color(0.10f, 0.058f, 0.018f, 0.34f), 122);
        CreateCrescentStarParticle(parent, bottomStarName, star, quadMesh, new Vector3(-0.02f, -2.54f, -0.32f), new Vector3(-10f, 16f, -4f), 0.455f, 0.40f, 0.90f, new Color(1f, 0.94f, 0.58f, 0.28f), 164);
    }

    private static void CreateCrescentBladeParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, Vector3 euler, float delay, float lifetime, float size, int sortingOrder, Color color, bool outwardPulse, bool downwardDrift)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(euler.x, euler.y, euler.z));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.14f;
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
        col.color = CreateCrescentRevealGradient(color, Mathf.Min(1f, color.a), 0.12f, 0.48f, 0.82f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.90f), new Keyframe(0.16f, 1.04f), new Keyframe(0.66f, 1.00f), new Keyframe(1f, 0.82f)));

        if (outwardPulse || downwardDrift)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = outwardPulse ? new ParticleSystem.MinMaxCurve(0.04f, 0.20f) : new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            velocity.y = downwardDrift ? new ParticleSystem.MinMaxCurve(-0.18f, -0.05f) : new ParticleSystem.MinMaxCurve(0f);
            velocity.z = outwardPulse ? new ParticleSystem.MinMaxCurve(-0.025f, 0.060f) : new ParticleSystem.MinMaxCurve(0f);
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateCrescentStarParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, Vector3 euler, float delay, float lifetime, float size, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(euler.x, euler.y, euler.z));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.10f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
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
        col.color = CreateCrescentRevealGradient(color, color.a, 0.16f, 0.28f, 0.62f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.08f), new Keyframe(0.18f, 1.18f), new Keyframe(0.48f, 1.00f), new Keyframe(1f, 0.04f)));

        var rotOver = ps.rotationOverLifetime;
        rotOver.enabled = true;
        rotOver.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 0.42f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void CreateCrescentEdgeDust(Transform parent, string name, Material mat, Vector3 pos, Vector3 box, float delay, short burst, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(-8f, 18f, -4f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.88f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.070f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = burst + 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = box;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0.16f, 0.88f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.20f, -0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.12f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateCrescentRevealGradient(color, color.a, 0.10f, 0.30f, 0.70f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.26f, 1f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 0.72f;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateCrescentShadowDust(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(-8f, 18f, -4f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.92f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.76f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.24f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.030f, 0.090f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = burst + 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(0.42f, 3.40f, 0.08f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.22f, 0.03f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.020f, 0.020f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateCrescentRevealGradient(color, color.a, 0.16f, 0.40f, 0.82f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.60f), new Keyframe(0.36f, 1.0f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateDotMatrixParticle(Transform parent, string name, Material mat, Vector3 pos, float delay, short burst, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.identity);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 1.0f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.78f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.70f, 0.05f, 0.70f), ColorDawnWhite);
        main.maxParticles = burst + 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(5.2f, 1.6f, 0.06f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.0f, 3.0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateDawnFadeGradient(0.76f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.82f), new Keyframe(0.40f, 1.0f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateSlashArcParticle(Transform parent, string name, Material mat, Vector3 pos, float zRot, float delay, short burst, float radius, float arc, float minSize, float maxSize, float minSpeed, float maxSpeed, int sortingOrder, Color color)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(0f, 0f, zRot));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.86f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = burst + 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = arc;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.4f, 2.8f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.2f, 2.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, color.a, 0.10f, 0.46f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.20f), new Keyframe(0.16f, 1f), new Keyframe(1f, 0.18f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 1.4f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = maxSpeed > 2f ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        r.velocityScale = 0.12f;
        r.lengthScale = 2.2f;
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
        col.color = CreateFadeGradient(color, color.a, 0.10f, 0.46f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.20f), new Keyframe(0.13f, 1f), new Keyframe(1f, 1.32f)));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;
    }

    private static void CreateCasterCircleParticle(Transform parent, string name, Material mat, Mesh mesh, Vector3 pos, float delay, float lifetime, float size, float rotation, Color color, int sortingOrder)
    {
        GameObject go = NewParticle(parent, name, pos, Quaternion.Euler(62f, 27f, 0f));
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + lifetime + 0.12f;
        main.startDelay = delay;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
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
        col.color = CreateFadeGradient(color, color.a, 0.12f, 0.52f);

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.42f), new Keyframe(0.16f, 1.06f), new Keyframe(1f, 0.72f)));

        var rotOver = ps.rotationOverLifetime;
        rotOver.enabled = true;
        rotOver.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 1.15f);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.sortingOrder = sortingOrder;
        r.alignment = ParticleSystemRenderSpace.Local;
    }

    private static void GenerateTextures()
    {
        SavePng("Assets/Textures/christasha_dawn_daoguang.png", CreateDaoguangTexture(1024, 256));
        SavePng("Assets/Textures/christasha_dawn_daoguang_vertical.png", CreateVerticalDaoguangTexture(256, 1024));
        SavePng("Assets/Textures/christasha_dawn_blade.png", CreateBladeTexture(512, 160));
        SavePng("Assets/Textures/christasha_dawn_glow.png", CreateGlowTexture(512, 512));
        SavePng("Assets/Textures/christasha_dawn_star.png", CreateStarTexture(256, 256));
        SavePng("Assets/Textures/christasha_dawn_dots.png", CreateDotTexture(256, 256));
        SavePng("Assets/Textures/christasha_dawn_black.png", CreateBlackTexture(512, 512));
        SavePng("Assets/Textures/christasha_dawn_circle.png", CreateCircleTexture(512, 512));
        SavePng("Assets/Textures/christasha_dawn_crescent_particle.png", CreateCrescentParticleTexture(512, 1024));
        SavePng("Assets/Textures/christasha_dawn_crescent_core.png", CreateCrescentCoreTexture(512, 1024));
        SavePng("Assets/Textures/christasha_dawn_crescent_shadow.png", CreateCrescentShadowTexture(512, 1024));
        AssetDatabase.Refresh();
    }

    private static void GenerateShaders()
    {
        File.WriteAllText("Assets/Shaders/ChristashaDawn_CrescentFlow.shader", @"
Shader ""Steria/ChristashaDawnCrescentFlow""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _TintColor (""Tint Color"", Color) = (1,1,1,1)
        _EmissionBoost (""Emission Boost"", Float) = 1.0
        _EdgeWidth (""Edge Width"", Float) = 0.08
        _FlowSpeed (""Flow Speed"", Float) = 0.35
        _FlowScale (""Flow Scale"", Float) = 28.0
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""IgnoreProjector""=""True"" ""RenderType""=""Transparent"" }
        Blend SrcAlpha OneMinusSrcAlpha
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float _EmissionBoost;
            float _EdgeWidth;
            float _FlowSpeed;
            float _FlowScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
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
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float sideMask = 1.0 - smoothstep(0.0, _EdgeWidth, i.uv.x) * (1.0 - smoothstep(1.0 - _EdgeWidth, 1.0, i.uv.x));
                float flow = pow(saturate(sin((i.uv.y + _Time.y * _FlowSpeed) * _FlowScale) * 0.5 + 0.5), 3.0);
                float spark = pow(saturate(sin(i.uv.y * 61.0 - i.uv.x * 17.0 + _Time.y * _FlowSpeed * 5.7) * 0.5 + 0.5), 7.0);
                fixed4 c = tex * _TintColor * i.color;
                c.rgb *= _EmissionBoost * (1.0 + flow * 0.16 + sideMask * 0.26 + spark * 0.10);
                c.a *= saturate(0.70 + flow * 0.18 + sideMask * 0.12);
                return c;
            }
            ENDCG
        }
    }
    Fallback ""Legacy Shaders/Particles/Alpha Blended""
}
");
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

    private static Material CreateCrescentSlashMaterial(string texturePath, string materialPath, Color tint, float emissionBoost, float edgeWidth, float flowSpeed)
    {
        TextureImporter ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }

        Shader shader = Shader.Find("Steria/ChristashaDawnCrescentFlow")
            ?? Shader.Find("Particles/Alpha Blended")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            ?? Shader.Find("Sprites/Default");

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
        if (mat.HasProperty("_EmissionBoost"))
        {
            mat.SetFloat("_EmissionBoost", emissionBoost);
        }
        if (mat.HasProperty("_EdgeWidth"))
        {
            mat.SetFloat("_EdgeWidth", edgeWidth);
        }
        if (mat.HasProperty("_FlowSpeed"))
        {
            mat.SetFloat("_FlowSpeed", flowSpeed);
        }
        if (mat.HasProperty("_FlowScale"))
        {
            mat.SetFloat("_FlowScale", 30f);
        }
        mat.color = tint;
        mat.renderQueue = 3140;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material[] CreateProvidedVerticalCrescentLayerMaterials()
    {
        string[] texturePaths =
        {
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_01.png",
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_02.png",
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_03.png",
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_04.png",
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_05.png",
            "Assets/Textures/ChristashaProvidedVerticalCrescent/Cropped/christasha_vertical_crescent_layer_06.png"
        };

        Material[] frames = new Material[texturePaths.Length];
        for (int i = 0; i < texturePaths.Length; i++)
        {
            string label = (i + 1).ToString("00");
            string materialPath = "Assets/Materials/ChristashaDawn_ProvidedVerticalCrescentLayer_" + label + "_Alpha.mat";
            frames[i] = CreateMaterial(texturePaths[i], materialPath, ColorDawnWhite, true);
        }
        return frames;
    }

    private static Mesh[] CreateProvidedVerticalCrescentLayerMeshes()
    {
        // Verifier anchor: ChristashaDawn_ProvidedVerticalCrescentLayer_06
        float[] widths = { 1.2097f, 1.2187f, 0.6203f, 1.2085f, 1.1867f, 1.1803f };
        float[] heights = { 2.2539f, 2.2927f, 1.7978f, 2.2491f, 2.1957f, 2.2466f };
        float[] offsetsX = { 0.0051f, -0.0006f, 0.0141f, 0.0045f, 0.0154f, 0.0147f };
        float[] offsetsY = { -0.0061f, 0.0036f, -0.0255f, -0.0061f, -0.0255f, -0.0267f };

        const int layerCount = 6;
        const int stripCount = 10;
        Mesh[] meshes = new Mesh[layerCount + layerCount * stripCount];
        for (int i = 0; i < layerCount; i++)
        {
            string label = (i + 1).ToString("00");
            meshes[i] = CreateOrReplaceMesh(
                "Assets/Meshes/ChristashaDawn_ProvidedVerticalCrescentLayer_" + label + ".asset",
                CreateProvidedVerticalCrescentMesh("ChristashaDawn_ProvidedVerticalCrescentLayer_" + label, widths[i], heights[i], 0.18f, 18, offsetsX[i], offsetsY[i], true));
            for (int strip = 0; strip < stripCount; strip++)
            {
                float tStart = strip / (float)stripCount;
                float tEnd = Mathf.Min(1f, (strip + 1.10f) / stripCount);
                string stripLabel = (strip + 1).ToString("00");
                meshes[layerCount + i * stripCount + strip] = CreateOrReplaceMesh(
                    "Assets/Meshes/ChristashaDawn_ProvidedVerticalCrescentLayer_" + label + "_Wipe_" + stripLabel + ".asset",
                    CreateProvidedVerticalCrescentMesh("ChristashaDawn_ProvidedVerticalCrescentLayer_" + label + "_Wipe_" + stripLabel, widths[i], heights[i], 0.18f, 8, offsetsX[i], offsetsY[i], true, tStart, tEnd));
            }
        }
        return meshes;
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

    private static Mesh CreateArcMesh(string name, float width, float height, int segments, float uStart, float uEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        uStart = Mathf.Clamp01(uStart);
        uEnd = Mathf.Clamp01(uEnd);
        segments = Mathf.Max(2, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(uStart, uEnd, localT);
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float arc = Mathf.Sin(t * Mathf.PI);
            float y = -height * 0.20f + arc * height * 0.90f - t * height * 0.12f;
            float localHalf = Mathf.Lerp(0.18f, 0.52f, Mathf.Pow(arc, 0.55f)) * height;
            localHalf *= Smooth01(0f, 0.14f, t) * (1f - Smooth01(0.91f, 1f, t));
            localHalf = Mathf.Max(localHalf, height * 0.025f);

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateSlashSweepMesh(string name, float startX, float endX, float baseY, float height, float thickness, int segments, float uStart, float uEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        uStart = Mathf.Clamp01(uStart);
        uEnd = Mathf.Clamp01(uEnd);
        segments = Mathf.Max(3, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float u = Mathf.Lerp(uStart, uEnd, localT);
            float arc = Mathf.Sin(u * Mathf.PI);
            float x = Mathf.Lerp(startX, endX, u);
            float y = baseY - height * 0.28f + arc * height * 0.96f - u * height * 0.16f + Mathf.Sin((u * 2.35f + 0.10f) * Mathf.PI) * height * 0.035f;
            float revealTaper = Smooth01(0f, 0.08f, localT) * (1f - Smooth01(0.92f, 1f, localT));
            float sweepTaper = Smooth01(0.00f, 0.10f, u) * (1f - Smooth01(0.90f, 1f, u));
            float localHalf = thickness * Mathf.Lerp(0.12f, 0.62f, Mathf.Pow(Mathf.Max(0.001f, arc), 0.50f)) * Mathf.Max(0.18f, revealTaper) * Mathf.Max(0.30f, sweepTaper);
            localHalf = Mathf.Max(localHalf, thickness * 0.035f);

            vertices.Add(new Vector3(x, y - localHalf, 0f));
            vertices.Add(new Vector3(x, y + localHalf, 0f));
            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, 1f));

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateProvidedVerticalCrescentMesh(string name, float width, float height, float zBend, int segments, float offsetX, float offsetY, bool flipX)
    {
        return CreateProvidedVerticalCrescentMesh(name, width, height, zBend, segments, offsetX, offsetY, flipX, 0f, 1f);
    }

    private static Mesh CreateProvidedVerticalCrescentMesh(string name, float width, float height, float zBend, int segments, float offsetX, float offsetY, bool flipX, float tStart, float tEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        segments = Mathf.Max(2, segments);
        tStart = Mathf.Clamp01(tStart);
        tEnd = Mathf.Clamp01(tEnd);
        if (tEnd < tStart)
        {
            float tmp = tStart;
            tStart = tEnd;
            tEnd = tmp;
        }
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(tStart, tEnd, localT);
            float y = Mathf.Lerp(height * 0.5f, -height * 0.5f, t);
            float curve = Mathf.Sin(t * Mathf.PI);
            float centerX = offsetX + width * 0.08f - curve * width * 0.12f;
            float halfWidth = width * Mathf.Lerp(0.46f, 0.58f, curve);
            float z = zBend * curve;
            float uLeft = flipX ? 1f : 0f;
            float uRight = flipX ? 0f : 1f;

            vertices.Add(new Vector3(centerX - halfWidth, y + offsetY, -z * 0.35f));
            vertices.Add(new Vector3(centerX + halfWidth, y + offsetY, z));
            uvs.Add(new Vector2(uLeft, 1f - t));
            uvs.Add(new Vector2(uRight, 1f - t));

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateReferenceCrescentSlashMesh(string name, float width, float height, float zBend, float uStart, float uEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        uStart = Mathf.Clamp01(uStart);
        uEnd = Mathf.Clamp01(uEnd);
        if (uEnd < uStart)
        {
            float tmp = uStart;
            uStart = uEnd;
            uEnd = tmp;
        }

        int segments = 30;
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float u = Mathf.Lerp(uStart, uEnd, localT);
            float arc = Mathf.Sin(u * Mathf.PI);
            float centerY = (-0.34f + 0.64f * Mathf.Pow(Mathf.Max(0f, arc), 0.58f) - 0.10f * u + 0.035f * Mathf.Sin((u * 2.25f + 0.08f) * Mathf.PI)) * height;
            float halfY = (0.30f + 0.26f * Mathf.Pow(Mathf.Max(0.001f, arc), 0.46f)) * height;
            float x = width * u;
            float z = zBend * Mathf.Sin(u * Mathf.PI);

            vertices.Add(new Vector3(x, centerY - halfY, z - zBend * 0.28f));
            vertices.Add(new Vector3(x, centerY + halfY, z + zBend * 0.28f));
            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, 1f));

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateBezierCrescentMesh(string name, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float thickness, int segments, float tStart, float tEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        tStart = Mathf.Clamp01(tStart);
        tEnd = Mathf.Clamp01(tEnd);
        if (tEnd < tStart)
        {
            float tmp = tStart;
            tStart = tEnd;
            tEnd = tmp;
        }

        segments = Mathf.Max(3, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(tStart, tEnd, localT);
            Vector3 center = EvaluateCubicBezier(p0, p1, p2, p3, t);
            Vector3 tangent = CubicBezierTangent(p0, p1, p2, p3, t);
            if (tangent.sqrMagnitude < 0.000001f)
            {
                tangent = Vector3.right;
            }

            tangent.Normalize();
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f).normalized;
            float globalTaper = Smooth01(0f, 0.10f, t) * (1f - Smooth01(0.90f, 1f, t));
            float localTaper = Smooth01(0f, 0.08f, localT) * (1f - Smooth01(0.92f, 1f, localT));
            float crescentWidth = Mathf.Lerp(0.18f, 1.00f, Mathf.Sin(t * Mathf.PI));
            float half = thickness * crescentWidth * Mathf.Max(0.20f, globalTaper) * Mathf.Max(0.24f, localTaper);
            half = Mathf.Max(half, thickness * 0.030f);

            vertices.Add(center - normal * half);
            vertices.Add(center + normal * half);
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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh[] CreateVerticalCrescentRevealMeshes(string pathPrefix, string namePrefix, float height, float outerWidth, float bellyWidth, float innerCut, float depthBend, float zOffset)
    {
        float[] revealEnds = { 0.22f, 0.36f, 0.50f, 0.64f, 0.78f, 0.90f, 1.00f };
        Mesh[] meshes = new Mesh[revealEnds.Length];
        for (int i = 0; i < revealEnds.Length; i++)
        {
            string label = (i + 1).ToString("00");
            meshes[i] = CreateOrReplaceMesh(pathPrefix + "_" + label + ".asset", CreateVerticalCrescentMesh(namePrefix + "_" + label, height, outerWidth, bellyWidth, innerCut, depthBend, zOffset, 42, 0.00f, revealEnds[i]));
        }
        return meshes;
    }

    private static Mesh CreateVerticalCrescentMesh(string name, float height, float outerWidth, float bellyWidth, float innerCut, float depthBend, float zOffset, int segments, float tStart, float tEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        tStart = Mathf.Clamp01(tStart);
        tEnd = Mathf.Clamp01(tEnd);
        if (tEnd < tStart)
        {
            float tmp = tStart;
            tStart = tEnd;
            tEnd = tmp;
        }

        segments = Mathf.Max(4, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(tStart, tEnd, localT);
            float y = Mathf.Lerp(height * 0.5f, -height * 0.5f, t);
            float arc = Mathf.Sin(t * Mathf.PI);
            float taper = Smooth01(0.00f, 0.07f, t) * (1f - Smooth01(0.93f, 1.00f, t));
            float centerX = -0.16f + 0.62f * arc - 0.20f * t + 0.045f * Mathf.Sin((t * 2.1f + 0.08f) * Mathf.PI);
            float outer = (outerWidth * 0.18f + bellyWidth * Mathf.Pow(Mathf.Max(0.001f, arc), 0.54f)) * Mathf.Max(0.16f, taper);
            float inner = Mathf.Max(outer * 0.16f, outer - innerCut * Mathf.Lerp(0.65f, 1.20f, arc));
            float outsideX = centerX - outer * 0.58f;
            float insideX = centerX + inner * 0.92f;
            float zCurve = zOffset + depthBend * (arc - 0.5f) * 0.36f;
            float edgeLift = depthBend * Mathf.Pow(Mathf.Max(0f, arc), 0.78f) * 0.20f;

            vertices.Add(new Vector3(outsideX, y, zCurve - edgeLift));
            vertices.Add(new Vector3(insideX, y, zCurve + edgeLift));
            uvs.Add(new Vector2(0f, 1f - t));
            uvs.Add(new Vector2(1f, 1f - t));

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateHorizontalCrescentMeshXY(string name, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float thickness, float verticalLift, float depthProjection, int segments, float tStart, float tEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        tStart = Mathf.Clamp01(tStart);
        tEnd = Mathf.Clamp01(tEnd);
        if (tEnd < tStart)
        {
            float tmp = tStart;
            tStart = tEnd;
            tEnd = tmp;
        }

        segments = Mathf.Max(3, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float t = Mathf.Lerp(tStart, tEnd, localT);
            Vector3 center = EvaluateCubicBezier(p0, p1, p2, p3, t);
            Vector3 tangent = CubicBezierTangent(p0, p1, p2, p3, t);
            if (tangent.sqrMagnitude < 0.000001f)
            {
                tangent = Vector3.right;
            }

            tangent.z = 0f;
            tangent.Normalize();
            Vector3 normalXY = new Vector3(-tangent.y, tangent.x, 0f).normalized;
            float globalTaper = Smooth01(0f, 0.10f, t) * (1f - Smooth01(0.90f, 1f, t));
            float localTaper = Smooth01(0f, 0.08f, localT) * (1f - Smooth01(0.92f, 1f, localT));
            float crescentWidth = Mathf.Lerp(0.18f, 1.00f, Mathf.Sin(t * Mathf.PI));
            float half = thickness * crescentWidth * Mathf.Max(0.20f, globalTaper) * Mathf.Max(0.24f, localTaper);
            half = Mathf.Max(half, thickness * 0.030f);
            float lift = verticalLift * Mathf.Sin(t * Mathf.PI);
            float edgeTilt = verticalLift * 0.30f * crescentWidth;

            Vector3 nearEdge = new Vector3(center.x - normalXY.x * half, center.y - normalXY.y * half, lift - edgeTilt);
            Vector3 farEdge = new Vector3(center.x + normalXY.x * half, center.y + normalXY.y * half, lift + edgeTilt);
            vertices.Add(ProjectHorizontalCrescentPoint(nearEdge, depthProjection, localT));
            vertices.Add(ProjectHorizontalCrescentPoint(farEdge, depthProjection, localT));
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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Vector3 ProjectHorizontalCrescentPoint(Vector3 horizontalPoint, float depthProjection, float localT)
    {
        float depth = horizontalPoint.y;
        float height = horizontalPoint.z;
        float depthPerspective = Mathf.Clamp(1f + depth * depthProjection * 0.11f, 0.78f, 1.24f);
        float screenShear = depth * depthProjection * 0.42f + height * 0.22f;
        float centerWeight = Mathf.Sin(Mathf.Clamp01(localT) * Mathf.PI);
        float screenX = horizontalPoint.x * depthPerspective + screenShear * (0.72f + centerWeight * 0.22f);
        float screenY = height * 1.55f + depth * depthProjection * 0.34f - Mathf.Abs(depth) * depthProjection * 0.045f;
        float residualZ = height * 0.10f + depth * 0.035f;
        return new Vector3(screenX, screenY, residualZ);
    }

    private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0
            + 3f * oneMinusT * oneMinusT * t * p1
            + 3f * oneMinusT * t * t * p2
            + t * t * t * p3;
    }

    private static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return 3f * oneMinusT * oneMinusT * (p1 - p0)
            + 6f * oneMinusT * t * (p2 - p1)
            + 3f * t * t * (p3 - p2);
    }

    private static Mesh CreateDaoguangStripMesh(string name, float width, float height, float uStart, float uEnd)
    {
        uStart = Mathf.Clamp01(uStart);
        uEnd = Mathf.Clamp01(uEnd);
        if (uEnd < uStart)
        {
            float tmp = uStart;
            uStart = uEnd;
            uEnd = tmp;
        }

        float x0 = Mathf.Lerp(-width * 0.5f, width * 0.5f, uStart);
        float x1 = Mathf.Lerp(-width * 0.5f, width * 0.5f, uEnd);
        float y0 = -height * 0.5f;
        float y1 = height * 0.5f;

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = new[]
        {
            new Vector3(x0, y0, 0f),
            new Vector3(x0, y1, 0f),
            new Vector3(x1, y1, 0f),
            new Vector3(x1, y0, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(uStart, 0f),
            new Vector2(uStart, 1f),
            new Vector2(uEnd, 1f),
            new Vector2(uEnd, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateVerticalCleaveSweepMesh(string name, float topY, float bottomY, float centerX, float width, float bend, int segments, float vStart, float vEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        vStart = Mathf.Clamp01(vStart);
        vEnd = Mathf.Clamp01(vEnd);
        segments = Mathf.Max(3, segments);
        for (int i = 0; i <= segments; i++)
        {
            float localT = i / (float)segments;
            float v = Mathf.Lerp(vStart, vEnd, localT);
            float centerWeight = Mathf.Sin(v * Mathf.PI);
            float y = Mathf.Lerp(topY, bottomY, v);
            float x = centerX + (v - 0.5f) * bend + Mathf.Sin((v * 1.15f + 0.08f) * Mathf.PI) * width * 0.18f;
            float revealTaper = Smooth01(0f, 0.08f, localT) * (1f - Smooth01(0.92f, 1f, localT));
            float cleaveTaper = Smooth01(0.00f, 0.12f, v) * (1f - Smooth01(0.88f, 1f, v));
            float localHalf = width * Mathf.Lerp(0.16f, 0.58f, Mathf.Pow(Mathf.Max(0.001f, centerWeight), 0.55f)) * Mathf.Max(0.18f, revealTaper) * Mathf.Max(0.32f, cleaveTaper);
            localHalf = Mathf.Max(localHalf, width * 0.035f);

            vertices.Add(new Vector3(x - localHalf, y, 0f));
            vertices.Add(new Vector3(x + localHalf, y, 0f));
            uvs.Add(new Vector2(v, 0f));
            uvs.Add(new Vector2(v, 1f));

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateVerticalDaoguangStripMesh(string name, float width, float height, float topStart, float topEnd)
    {
        topStart = Mathf.Clamp01(topStart);
        topEnd = Mathf.Clamp01(topEnd);
        if (topEnd < topStart)
        {
            float tmp = topStart;
            topStart = topEnd;
            topEnd = tmp;
        }

        float yTop = Mathf.Lerp(height * 0.5f, -height * 0.5f, topStart);
        float yBottom = Mathf.Lerp(height * 0.5f, -height * 0.5f, topEnd);
        float x0 = -width * 0.5f;
        float x1 = width * 0.5f;
        float uvTop = 1f - topStart;
        float uvBottom = 1f - topEnd;

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = new[]
        {
            new Vector3(x0, yBottom, 0f),
            new Vector3(x0, yTop, 0f),
            new Vector3(x1, yTop, 0f),
            new Vector3(x1, yBottom, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, uvBottom),
            new Vector2(0f, uvTop),
            new Vector2(1f, uvTop),
            new Vector2(1f, uvBottom)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateNeedleMesh(string name, float width, float height, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float taper = Smooth01(0f, 0.10f, t) * (1f - Smooth01(0.88f, 1f, t));
            float head = Mathf.Lerp(0.70f, 0.16f, Smooth01(0.66f, 1f, t));
            float localHalf = Mathf.Max(height * 0.02f, height * taper * head);
            float y = Mathf.Sin(t * Mathf.PI) * height * 0.10f;

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
        mesh.name = name;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
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

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r * 0.72f, baseColor.g * 0.72f, baseColor.b * 0.70f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.78f, 1f), 0.20f),
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

    private static ParticleSystem.MinMaxGradient CreateSlashTraceGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime, float fadeStart)
    {
        peakTime = Mathf.Clamp(peakTime, 0.08f, 0.48f);
        holdTime = Mathf.Clamp(holdTime, peakTime + 0.06f, 0.82f);
        fadeStart = Mathf.Clamp(fadeStart, holdTime + 0.04f, 0.94f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(baseColor, peakTime),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha * 0.34f, peakTime * 0.46f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha, holdTime),
                new GradientAlphaKey(peakAlpha * 0.42f, fadeStart),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateTextureCutoutGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime, float fadeStart)
    {
        peakTime = Mathf.Clamp(peakTime, 0.06f, 0.30f);
        holdTime = Mathf.Clamp(holdTime, peakTime + 0.04f, 0.56f);
        fadeStart = Mathf.Clamp(fadeStart, holdTime + 0.02f, 0.74f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(baseColor, peakTime),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha * 0.58f, peakTime * 0.44f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.86f, holdTime),
                new GradientAlphaKey(peakAlpha * 0.18f, fadeStart),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateCrescentRevealGradient(Color baseColor, float peakAlpha, float peakTime, float holdTime, float fadeStart)
    {
        peakTime = Mathf.Clamp(peakTime, 0.06f, 0.28f);
        holdTime = Mathf.Clamp(holdTime, peakTime + 0.08f, 0.66f);
        fadeStart = Mathf.Clamp(fadeStart, holdTime + 0.08f, 0.92f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.96f, 0.46f, 0.02f), 0f),
                new GradientColorKey(new Color(1f, 0.92f, 0.46f), peakTime),
                new GradientColorKey(baseColor, 0.72f),
                new GradientColorKey(new Color(baseColor.r * 0.72f, baseColor.g * 0.62f, baseColor.b * 0.42f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha * 0.38f, peakTime * 0.42f),
                new GradientAlphaKey(peakAlpha, peakTime),
                new GradientAlphaKey(peakAlpha * 0.78f, holdTime),
                new GradientAlphaKey(peakAlpha * 0.20f, fadeStart),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateDawnFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.48f, 0.02f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.78f), 0.24f),
                new GradientColorKey(new Color(1f, 0.76f, 0.08f), 1f)
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

    private static Texture2D CreateDaoguangTexture(int width, int height)
    {
        return CreateReferenceDawnCrescentTexture(width, height);
    }

    // CC0 reference visual language: OpenGameArt "Slash effect" by MetaShinryu.
    // The source texture is used as permissive silhouette guidance, then recolored and reshaped into Christasha's dawn palette.
    private static Texture2D CreateReferenceDawnCrescentTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Texture2D reference = LoadReferenceTexture("Assets/Reference/MetaShinryu_CC0_SlashGradual_reference.png");
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float arc = Mathf.Sin(u * Mathf.PI);
                float center = -0.42f + 0.94f * Mathf.Pow(Mathf.Max(0f, arc), 0.58f) - 0.16f * u + 0.045f * Mathf.Sin((u * 2.2f + 0.10f) * Mathf.PI);
                float distance = Mathf.Abs(v - center);
                float taper = Smooth01(0.00f, 0.070f, u) * (1f - Smooth01(0.94f, 1.00f, u));
                float bodyWidth = Mathf.Lerp(0.18f, 0.40f, Mathf.Pow(Mathf.Max(0.001f, arc), 0.42f));
                float coreWidth = Mathf.Lerp(0.030f, 0.092f, Mathf.Pow(Mathf.Max(0.001f, arc), 0.62f));
                float body = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, bodyWidth), 2f) * 1.05f);
                float innerCut = Mathf.Exp(-Mathf.Pow((v - center + bodyWidth * 0.36f) / Mathf.Max(0.001f, bodyWidth * 0.54f), 2f) * 1.35f);
                float moonBody = Mathf.Clamp01(body - innerCut * 0.56f);
                float core = Mathf.Exp(-Mathf.Pow((v - center - bodyWidth * 0.12f) / Mathf.Max(0.001f, coreWidth), 2f) * 2.35f);
                float outerEdge = Mathf.Exp(-Mathf.Pow((v - center - bodyWidth * 0.52f) / Mathf.Max(0.001f, bodyWidth * 0.16f), 2f));
                float lowerSmokeEdge = Mathf.Exp(-Mathf.Pow((v - center + bodyWidth * 0.70f) / Mathf.Max(0.001f, bodyWidth * 0.28f), 2f));
                float tipBloom = Mathf.Exp(-Mathf.Pow((u - 0.84f) / 0.11f, 2f)) * Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, bodyWidth * 0.84f), 2f));
                float referenceAlpha = SampleReferenceCrescentAlpha(reference, u, v);
                float noise = Mathf.PerlinNoise(u * 13.0f + 3.1f, v * 3.1f + 5.4f);
                float ragged = Mathf.Lerp(0.92f, 1.08f, noise);
                float alpha = Mathf.Clamp01((moonBody * 0.92f + referenceAlpha * 0.58f + core * 0.88f + outerEdge * 0.36f + tipBloom * 0.30f) * taper * ragged);

                float ember = Mathf.Pow(Mathf.Clamp01(1f - distance / Mathf.Max(0.001f, bodyWidth * 1.08f)), 2.0f) * taper;
                if (noise > 0.84f && ember > 0.13f)
                {
                    alpha = Mathf.Clamp01(alpha + (noise - 0.84f) * 0.22f);
                }

                if (alpha <= 0.010f)
                {
                    continue;
                }

                Color darkEdge = new Color(0.05f, 0.030f, 0.012f, alpha * Mathf.Clamp01(lowerSmokeEdge * 0.50f));
                Color gold = new Color(1f, 0.62f, 0.03f, alpha * 0.84f);
                Color white = new Color(1f, 0.98f, 0.78f, alpha);
                Color c = Color.Lerp(gold, white, Mathf.Clamp01(core * 1.08f + tipBloom * 0.42f + outerEdge * 0.32f + referenceAlpha * 0.24f));
                c = Color.Lerp(c, darkEdge, Mathf.Clamp01(lowerSmokeEdge * 0.26f));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D LoadReferenceTexture(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(fullPath));
        tex.Apply();
        return tex;
    }

    private static float SampleReferenceCrescentAlpha(Texture2D reference, float u, float v)
    {
        if (reference == null)
        {
            return 0f;
        }

        float sampleU = Mathf.Clamp01(u * 0.96f + 0.02f);
        float sampleV = Mathf.Clamp01(0.50f + v * 0.42f);
        Color src = reference.GetPixelBilinear(sampleU, sampleV);
        float luma = Mathf.Max(src.r, Mathf.Max(src.g, src.b));
        return Mathf.Clamp01(Mathf.Max(src.a, luma) * 1.18f);
    }

    private static Texture2D CreateVerticalDaoguangTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float topT = 1f - y / (height - 1f);
            float spine = Mathf.Sin(topT * Mathf.PI);
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float center = -0.06f + 0.14f * Mathf.Sin((topT * 1.28f + 0.10f) * Mathf.PI) - 0.08f * topT;
                float distance = Mathf.Abs(u - center);
                float taper = Smooth01(0.00f, 0.085f, topT) * (1f - Smooth01(0.92f, 1.00f, topT));
                float bodyWidth = Mathf.Lerp(0.07f, 0.21f, Mathf.Pow(Mathf.Max(0.001f, spine), 0.48f));
                float coreWidth = Mathf.Lerp(0.018f, 0.054f, Mathf.Pow(Mathf.Max(0.001f, spine), 0.60f));
                float body = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, bodyWidth), 2f) * 1.45f);
                float core = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, coreWidth), 2f) * 3.0f);
                float leftEdge = Mathf.Exp(-Mathf.Pow((u - center + bodyWidth * 0.58f) / Mathf.Max(0.001f, bodyWidth * 0.22f), 2f));
                float rightEdge = Mathf.Exp(-Mathf.Pow((u - center - bodyWidth * 0.50f) / Mathf.Max(0.001f, bodyWidth * 0.24f), 2f));
                float leadingBloom = Mathf.Exp(-Mathf.Pow((topT - 0.22f) / 0.16f, 2f)) * Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, bodyWidth * 0.86f), 2f));
                float noise = Mathf.PerlinNoise(u * 3.5f + 8.2f, topT * 12.0f + 1.3f);
                float alpha = Mathf.Clamp01((body * 0.56f + core * 1.08f + rightEdge * 0.22f + leadingBloom * 0.30f) * taper * Mathf.Lerp(0.90f, 1.06f, noise));

                if (noise > 0.87f && body > 0.24f)
                {
                    alpha = Mathf.Clamp01(alpha + (noise - 0.87f) * 0.24f);
                }

                if (alpha <= 0.010f)
                {
                    continue;
                }

                Color darkEdge = new Color(0.06f, 0.035f, 0.012f, alpha * Mathf.Clamp01(leftEdge * 0.55f));
                Color gold = new Color(1f, 0.60f, 0.02f, alpha * 0.82f);
                Color white = new Color(1f, 0.98f, 0.78f, alpha);
                Color c = Color.Lerp(gold, white, Mathf.Clamp01(core * 0.92f + leadingBloom * 0.45f + rightEdge * 0.22f));
                c = Color.Lerp(c, darkEdge, Mathf.Clamp01(leftEdge * 0.30f));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCrescentParticleTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float t = 1f - y / (height - 1f);
            float arc = Mathf.Sin(t * Mathf.PI);
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float distance = Mathf.Abs(u * 0.92f);
                float taper = Smooth01(0.00f, 0.08f, t) * (1f - Smooth01(0.93f, 1.00f, t));
                float body = Mathf.Exp(-Mathf.Pow(distance / 0.76f, 2f) * 1.10f);
                float outerEdge = Mathf.Exp(-Mathf.Pow((u + 0.74f) / 0.18f, 2f));
                float innerEdge = Mathf.Exp(-Mathf.Pow((u - 0.58f) / 0.22f, 2f)) * 0.48f;
                float slashFibers = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((t * 36.0f + u * 6.0f) * Mathf.PI)), 9f) * 0.18f;
                float grain = Mathf.PerlinNoise(u * 10.0f + 4.1f, t * 22.0f + 0.7f);
                float alpha = Mathf.Clamp01((body * 0.72f + outerEdge * 0.38f + innerEdge * 0.18f + slashFibers + Mathf.Max(0f, grain - 0.78f) * 0.28f) * taper);
                if (alpha <= 0.012f)
                {
                    continue;
                }

                float hot = Mathf.Clamp01(outerEdge * 0.88f + slashFibers * 1.40f + Mathf.Max(0f, grain - 0.84f) * 2.0f);
                Color c = Color.Lerp(new Color(1f, 0.50f, 0.02f, alpha * 0.82f), new Color(1f, 0.94f, 0.54f, alpha), hot);
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCrescentCoreTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float t = 1f - y / (height - 1f);
            float arc = Mathf.Sin(t * Mathf.PI);
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float taper = Smooth01(0.00f, 0.06f, t) * (1f - Smooth01(0.94f, 1.00f, t));
                float bladeLine = Mathf.Exp(-Mathf.Pow((u + 0.42f) / 0.12f, 2f) * 2.4f);
                float hotRidge = Mathf.Exp(-Mathf.Pow((u + 0.58f) / 0.055f, 2f) * 2.0f);
                float flow = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((t * 42.0f + u * 4.0f) * Mathf.PI)), 8f);
                float alpha = Mathf.Clamp01((bladeLine * 0.82f + hotRidge * 0.56f + flow * 0.16f * Mathf.Pow(Mathf.Max(0.001f, arc), 0.35f)) * taper);
                if (alpha <= 0.010f)
                {
                    continue;
                }

                Color c = Color.Lerp(new Color(1f, 0.78f, 0.16f, alpha * 0.72f), new Color(1f, 0.99f, 0.84f, alpha), Mathf.Clamp01(hotRidge + bladeLine * 0.62f));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCrescentShadowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float t = 1f - y / (height - 1f);
            float arc = Mathf.Sin(t * Mathf.PI);
            for (int x = 0; x < width; x++)
            {
                float u = (x / (width - 1f) - 0.5f) * 2f;
                float center = -0.05f + 0.42f * arc - 0.16f * t;
                float distance = Mathf.Abs(u - center);
                float taper = Smooth01(0.04f, 0.12f, t) * (1f - Smooth01(0.90f, 1.00f, t));
                float bodyWidth = Mathf.Lerp(0.06f, 0.24f, Mathf.Pow(Mathf.Max(0.001f, arc), 0.56f));
                float body = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, bodyWidth), 2f) * 1.45f);
                float starNoise = Mathf.PerlinNoise(u * 24.0f + 1.3f, t * 32.0f + 9.1f);
                float pinStar = starNoise > 0.78f ? (starNoise - 0.78f) * 1.9f : 0f;
                float alpha = Mathf.Clamp01((body * 0.22f + pinStar * 0.30f) * taper);
                if (alpha <= 0.010f)
                {
                    continue;
                }

                pixels[y * width + x] = new Color(0.09f, 0.050f, 0.016f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateBladeTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = ClearPixels(width, height);
        for (int y = 0; y < height; y++)
        {
            float v = (y / (height - 1f) - 0.5f) * 2f;
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float arc = Mathf.Sin(u * Mathf.PI);
                float center = -0.24f + 0.44f * Mathf.Pow(arc, 0.70f) - 0.06f * u + 0.035f * Mathf.Sin((u * 3.5f + 0.1f) * Mathf.PI);
                float distance = Mathf.Abs(v - center);
                float taper = Smooth01(0.02f, 0.14f, u) * (1f - Smooth01(0.84f, 1f, u));
                float coreWidth = Mathf.Lerp(0.030f, 0.085f, Mathf.Pow(arc, 0.55f));
                float glowWidth = Mathf.Lerp(0.10f, 0.28f, Mathf.Pow(arc, 0.45f));
                float core = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, coreWidth), 2f) * 2.1f);
                float glow = Mathf.Exp(-Mathf.Pow(distance / Mathf.Max(0.001f, glowWidth), 2f) * 1.25f);
                float alpha = Mathf.Clamp01((core * 1.25f + glow * 0.36f) * taper);
                if (alpha <= 0.014f)
                {
                    continue;
                }

                Color c = Color.Lerp(new Color(1f, 0.56f, 0.02f, alpha * 0.72f), new Color(1f, 0.98f, 0.80f, alpha), Mathf.Clamp01(core + glow * 0.12f));
                c.a = alpha;
                pixels[y * width + x] = c;
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
                float core = 1f - Smooth01(0.02f, 0.18f, r);
                float horizontal = Mathf.Max(0f, 1f - Mathf.Abs(v) * 14f) * Mathf.Max(0f, 1f - Mathf.Abs(u) * 0.68f);
                float vertical = Mathf.Max(0f, 1f - Mathf.Abs(u) * 16f) * Mathf.Max(0f, 1f - Mathf.Abs(v) * 0.78f);
                float halo = Mathf.Exp(-Mathf.Pow(r / 0.50f, 2f) * 1.25f);
                float alpha = Mathf.Clamp01(core + horizontal * 0.72f + vertical * 0.42f + halo * 0.30f);
                alpha *= 1f - Smooth01(0.76f, 0.98f, r);
                if (alpha <= 0.012f)
                {
                    continue;
                }
                Color c = Color.Lerp(new Color(1f, 0.54f, 0.02f, alpha * 0.62f), new Color(1f, 0.98f, 0.78f, alpha), Mathf.Clamp01(core + horizontal));
                c.a = alpha;
                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateStarTexture(int width, int height)
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
                float glow = Mathf.Exp(-Mathf.Pow(r / 0.44f, 2f) * 1.6f);
                float alpha = Mathf.Clamp01(axisX + axisY * 0.70f + (diagA + diagB) * 0.34f + glow * 0.32f);
                alpha *= 1f - Smooth01(0.78f, 1f, r);
                if (alpha <= 0.014f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(1f, 0.96f, 0.72f, alpha);
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
                float gridX = Mathf.Abs(Mathf.Sin((u + 1.15f) * 16f * Mathf.PI));
                float gridY = Mathf.Abs(Mathf.Sin((v + 1.05f) * 16f * Mathf.PI));
                float dot = Mathf.Pow(1f - Mathf.Max(gridX, gridY), 6f);
                float r = Mathf.Sqrt(u * u * 0.9f + v * v * 1.2f);
                float alpha = Mathf.Clamp01(dot * (1f - Smooth01(0.08f, 0.98f, r)));
                if (alpha <= 0.015f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(1f, 0.76f, 0.10f, alpha * 0.86f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateBlackTexture(int width, int height)
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
                float alpha = Mathf.Clamp01(feather * (0.22f + 0.08f * n));
                if (alpha <= 0.012f)
                {
                    continue;
                }
                pixels[y * width + x] = new Color(0.02f, 0.015f, 0.02f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCircleTexture(int width, int height)
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
                if (r > 0.92f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(v, u);
                float ringOuter = Mathf.Exp(-Mathf.Pow((r - 0.72f) / 0.018f, 2f));
                float ringMid = Mathf.Exp(-Mathf.Pow((r - 0.50f) / 0.014f, 2f)) * 0.64f;
                float ringInner = Mathf.Exp(-Mathf.Pow((r - 0.28f) / 0.012f, 2f)) * 0.52f;
                float rays = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 10f)), 24f) * Smooth01(0.28f, 0.36f, r) * (1f - Smooth01(0.68f, 0.78f, r)) * 0.56f;
                float ticks = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 28f)), 18f) * Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.035f, 2f)) * 0.76f;
                float alpha = Mathf.Clamp01(ringOuter + ringMid + ringInner + rays + ticks);
                alpha *= 1f - Smooth01(0.88f, 0.94f, r);
                if (alpha <= 0.015f)
                {
                    continue;
                }
                Color c = Color.Lerp(new Color(1f, 0.50f, 0.02f, alpha * 0.68f), new Color(1f, 0.98f, 0.76f, alpha), Mathf.Clamp01(ringOuter + ticks));
                c.a = alpha;
                pixels[y * width + x] = c;
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
        Debug.Log("Copied Christasha dawn bundle to project mod folder: " + localTarget);

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
            Debug.Log("Copied Christasha dawn bundle to game mod folder: " + target);
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
            string[] prefabNames =
            {
                SlashHorizontalPrefabName,
                SlashVerticalPrefabName,
                PierceNearPrefabName,
                PierceFarPrefabName,
                HitPrefabName
            };

            foreach (string prefabName in prefabNames)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
                if (prefab == null)
                {
                    throw new Exception("Verify failed: prefab missing " + prefabName);
                }
                int particles = prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
                int renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true).Length;
                if (particles < 9 || renderers < 9)
                {
                    throw new Exception("Verify failed: prefab too sparse " + prefabName + " particles=" + particles + " renderers=" + renderers);
                }
                Debug.Log("Verify Christasha dawn prefab passed: " + prefabName + " particles=" + particles + " renderers=" + renderers);
            }
        }
        finally
        {
            bundle.Unload(false);
        }
    }
}
