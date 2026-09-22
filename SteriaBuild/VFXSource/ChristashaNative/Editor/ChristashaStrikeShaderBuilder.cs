using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChristashaStrikeShaderBuilder
{
    public const string BundleName = "steria_christasha_light_strike";
    public static void Build()
    {
        const string asset="Assets/NativeRuntime/PlatinumStrike.shader";
        AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(asset);
        if(shader==null || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Platinum strike shader failed compilation");
        Directory.CreateDirectory("AssetBundles");
        var manifest=BuildPipeline.BuildAssetBundles("AssetBundles",new[]{new AssetBundleBuild{assetBundleName=BundleName,assetNames=new[]{asset}}},
            BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
        if(manifest==null)throw new InvalidOperationException("Platinum strike bundle build failed");
        Debug.Log("CHRISTASHA_STRIKE_SHADER_BUNDLE_PASS");
    }
}
