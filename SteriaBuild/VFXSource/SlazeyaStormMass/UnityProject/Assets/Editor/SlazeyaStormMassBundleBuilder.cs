using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class SlazeyaStormMassBundleBuilder
{
    private const string PrefabPath="Assets/Prefabs/SlazeyaStormMassPrefab.prefab";
    private const float H=6f;
    private const int CallbackStep=93;
    private static float ActualCallbackTime;
    private static readonly List<double> RenderReadbackMs=new List<double>(),PngEncodeMs=new List<double>();
    private static readonly Vector3[] Feet={new Vector3(4,0,-3),new Vector3(10,0,3),new Vector3(16,0,-2),new Vector3(7,0,5),new Vector3(15,0,5)};
    private static readonly Vector3 Caster=new Vector3(-15,0,0);
    private static readonly string[] SampleNames={"01_cloud_ring","02_cloud_swirl","02a_collapse_start","02b_collapse_inflow","02c_half_collapse","03_core_charge","03a_core_arrived","03b_core_peak","03c_core_hold","03d_callback_before","04_core_B0","04a_front_B1","04b_front_B2","04c_gap_B4","04d_flash_B7","05_front_B11","06_waterfall_B14","07_falling_fragments","08_low_mist","09_clear"};
    private static readonly float[] GatherSamples={0.25f,0.80f,0.82f,0.95f,1.10f,1.23f,1.28f,80f/60f,1.35f,92f/60f};
    private static readonly float[] BurstSamples={0,1f/60f,2f/60f,4f/60f,7f/60f,11f/60f,14f/60f,0.45f,0.90f,1.25f};
    private static readonly List<float> ActualSampleTimes=new List<float>();
    public static string PreviewOutputOverride; // Isolated native diagnostics only; normal builds leave this null.
    private static readonly string[] TextureFiles={"water_body_rgba.png","water_normal_roughness.png","water_flow_rg.png","foam_spray_4x4.png","mist_density_lighting_4x4.png","droplet_spindrift_4x2.png"};
    private static readonly List<string> TextureHashes=new List<string>();
    private static bool FirstCandidate;
    private static string Argument(string name)
    {
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];
        return null;
    }
    private static void ConfigurePreview()
    {
        string path=Argument("-previewDirectory");
        Require(!string.IsNullOrEmpty(path),"explicit unique -previewDirectory required");
        PreviewOutputOverride=Path.GetFullPath(path);
        Require(!File.Exists(Path.Combine(PreviewOutputOverride,"manifest.json")),"never overwrite an exported preview candidate");
        Directory.CreateDirectory(PreviewOutputOverride);
        FirstCandidate=Argument("-previewStage")=="FirstCandidate";
    }

    public static void BuildBundle()
    {
        try
        {
            ConfigurePreview();
            RenderReadbackMs.Clear();PngEncodeMs.Clear();
            Require(Application.unityVersion=="2019.3.15f1","Unity 2019.3.15f1");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PlayerSettings.colorSpace=ColorSpace.Gamma; QualitySettings.antiAliasing=4;
            foreach(string dir in new[]{"Assets/Textures/Round2","Assets/Materials/Round3","Assets/Meshes/Round3","Assets/Materials/Round5","Assets/Meshes/Round5","Assets/Prefabs"}) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset("Assets/Shaders/SlazeyaStormCloudNoise.cginc",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Shaders/SlazeyaStormCloudDensity.cginc",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Shaders/SlazeyaStormCloudVolume.shader",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            Shader waterShader=Shader.Find(SlazeyaStormVisualController.ShaderName), particleShader=Shader.Find(SlazeyaStormVisualController.ParticleShaderName);
            Shader volumeShader=Shader.Find(SlazeyaStormVisualController.CloudVolumeShaderName);
            if(volumeShader!=null){var compileMaterial=new Material(volumeShader);ShaderUtil.CompilePass(compileMaterial,0,true);Object.DestroyImmediate(compileMaterial);}
            if(volumeShader!=null)foreach(var message in ShaderUtil.GetShaderMessages(volumeShader))Debug.LogError(message.message+" at "+message.file+":"+message.line);
            Require(volumeShader!=null&&volumeShader.isSupported&&!ShaderUtil.ShaderHasError(volumeShader),"bounded local cloud volume shader supported");
            Shader cloudShader=Shader.Find(SlazeyaStormVisualController.CloudShaderName);
            Require(cloudShader!=null&&cloudShader.isSupported&&!ShaderUtil.ShaderHasError(cloudShader),"independent R5 cloud shader supported");
            Shader lightningShader=Shader.Find(SlazeyaStormVisualController.LightningShaderName);
            Require(waterShader!=null&&waterShader.isSupported&&!ShaderUtil.ShaderHasError(waterShader),"R5 deformed water shader supported");
            Require(particleShader!=null&&particleShader.isSupported&&!ShaderUtil.ShaderHasError(particleShader),"R5 native age atlas shader supported");
            Require(lightningShader!=null&&lightningShader.isSupported&&!ShaderUtil.ShaderHasError(lightningShader),"finite local lightning shader supported");
            Texture2D[] textures=ImportWorkerTextures();
            Material water=WaterMaterial("BreakerWater",waterShader,textures,false);
            Material low=WaterMaterial("InflowWater",waterShader,textures,true);
            Material spray=ParticleMaterial("FoamSheets",particleShader,textures[3],new Vector4(4,4,1,0));
            // The revised worker atlas supplies an open asymmetric membrane; consume its full A.
            spray.SetFloat("_SheetBreakup",0);
            Material mist=ParticleMaterial("VolumeMist",particleShader,textures[4],new Vector4(4,4,1,1));
            Material returnMist=ParticleMaterial("ReturnMist",particleShader,textures[4],new Vector4(4,4,1,1));
            returnMist.SetColor("_MistDark",new Color(0.208f,0.275f,0.322f));returnMist.SetColor("_MistLight",new Color(0.604f,0.671f,0.722f));
            Material liftMist=ParticleMaterial("CrestLiftMist",particleShader,textures[4],new Vector4(4,4,1,1));
            liftMist.SetColor("_MistDark",new Color(0.208f,0.275f,0.322f));liftMist.SetColor("_MistLight",new Color(0.784f,0.835f,0.875f));
            Material drops=ParticleMaterial("Spindrift",particleShader,textures[5],new Vector4(4,2,0,0));
            Material gatheredFoam=ParticleMaterial("GatheredWhitewater",particleShader,textures[4],new Vector4(4,4,1,0));
            Material parcelFoam=new Material(cloudShader){name="StormCloudClusters"};
            parcelFoam.SetTexture("_Atlas",textures[4]);parcelFoam.SetTexture("_FlowTex",textures[2]);
            SaveAsset(parcelFoam,"Assets/Materials/Round3/StormCloudClusters.mat");
            var root=new GameObject(SlazeyaStormVisualController.PrefabName);
            Transform ground=Child(root.transform,"AB_GroundRoot"),storm=Child(root.transform,"AB_StormRoot");
            Transform lightning=Child(Child(root.transform,"AB_BurstRoot"),"LightningRoot");
            var lightningMaterial=new Material(lightningShader){name="FiniteDischarge"};lightningMaterial.SetColor("_Core",new Color(0.953f,0.973f,1));lightningMaterial.SetColor("_Edge",new Color(0.659f,0.749f,0.820f));SaveAsset(lightningMaterial,"Assets/Materials/Round3/FiniteDischarge.mat");
            MakeLightning(lightning,"PrimaryBolt",lightningMaterial,0);MakeLightning(lightning,"SecondaryBolt",lightningMaterial,1);
            Texture3D[] volumeNoise=SlazeyaStormCloudNoiseBaker.Ensure(PreviewDirectory());
            MakeMacroClouds(storm,volumeShader,volumeNoise);
            Transform particles=Child(root.transform,"AB_ParticleRoot");
            MakeRingSurface(root.transform.Find("AB_BurstRoot"),"RingJet",water);
            MakeParticles(particles,"GatherMist",mist,40,false);
            MakeParticles(particles,"CrestSpindrift",drops,64,true);
            MakeParticles(particles,"BurstSheets",spray,48,false);
            MakeParticles(particles,"BurstDroplets",drops,320,true);
            MakeParticles(particles,"ReturnMist",returnMist,32,false);
            MakeParticles(particles,"CrestLiftMist",liftMist,24,false);
            MakeParticles(particles,"GatherFoam",parcelFoam,168,false);
            MakeParticles(particles,"WaterfallStreaks",gatheredFoam,128,true);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles")); Directory.CreateDirectory(output);
            var build=BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild{assetBundleName=SlazeyaStormVisualController.BundleName,assetNames=new[]{PrefabPath}}},
                BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
            Require(build!=null,"Windows64 LZ4 R5 bundle");
            string bundlePath=Path.Combine(output,SlazeyaStormVisualController.BundleName);
            AssetBundle bundle=AssetBundle.LoadFromFile(bundlePath); Require(bundle!=null,"R5 bundle readback");
            try
            {
                GameObject prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);
                VerifyPrefab(prefab);SlazeyaStormCloudNoiseBaker.VerifyBundle(prefab,PreviewDirectory());
                VerifyBurstAbundance(prefab);VerifyGpuWavefront();
                if(FirstCandidate)VerifyFoamTransport(prefab,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H)));
                else VerifyController(prefab);
                string previews=PreviewDirectory(); Directory.CreateDirectory(previews);
                ActualSampleTimes.Clear();
                if(Argument("-streakFramesOnly")!="true")CaptureSet(prefab,previews,"black",false);
                CaptureSet(prefab,previews,"battlefield",true);
                if(!FirstCandidate)CaptureAtlasDiagnostics(prefab,previews);
                WriteCaptureTiming(previews);WriteManifest(previews,bundlePath);
            }
            finally {bundle.Unload(true);}
            Debug.Log("SLAZEYA_ROUND6_BUILD_PREVIEW_PASS "+bundlePath);
        }
        catch(Exception ex) {Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    // Run in an isolated project containing the frozen original controller and the original AB.
    public static void ExportBaselinePreview()
    {
        try
        {
            ConfigurePreview();RenderReadbackMs.Clear();PngEncodeMs.Clear();ActualSampleTimes.Clear();
            Require(Application.unityVersion=="2019.3.15f1","baseline Unity version");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PlayerSettings.colorSpace=ColorSpace.Gamma;QualitySettings.antiAliasing=4;
            string path=Argument("-baselineBundle");
            Require(Digest(path)=="918B3213A65BA542FAEC7F83CE6926A57CE464A81A8990DD65272B6E3AACB8C5","frozen original baseline AB");
            Require(Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs"))=="3DA8605425D84085F1F62704A5BC69BC01629941C188813E17D4C0961EF025C9","frozen original baseline controller");
            var bundle=AssetBundle.LoadFromFile(path);
            try {CaptureSet(bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName),PreviewDirectory(),"battlefield",true);WriteCaptureTiming(PreviewDirectory());WriteManifest(PreviewDirectory(),path);}
            finally {bundle.Unload(true);}
            Debug.Log("SLAZEYA_BASELINE_PREVIEW_PASS");
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    public static void ExportBuiltPreview()
    {
        try
        {
            ConfigurePreview();RenderReadbackMs.Clear();PngEncodeMs.Clear();ActualSampleTimes.Clear();
            Require(Application.unityVersion=="2019.3.15f1","Unity version for frozen bundle export");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PlayerSettings.colorSpace=ColorSpace.Gamma;QualitySettings.antiAliasing=4;
            string bundlePath=Argument("-existingBundle");
            Require(Digest(bundlePath)==Argument("-expectedBundle"),"frozen reviewed-frame bundle hash");
            Require(Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs"))==Argument("-expectedController"),"frozen reviewed-frame controller hash");
            TextureHashes.Clear();foreach(string name in TextureFiles)TextureHashes.Add(Digest(Path.Combine(Application.dataPath,"Textures/Round2/"+name)));
            var bundle=AssetBundle.LoadFromFile(bundlePath);
            try
            {
                var prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);
                CaptureSet(prefab,PreviewDirectory(),"black",false);CaptureSet(prefab,PreviewDirectory(),"battlefield",true);
                WriteCaptureTiming(PreviewDirectory());WriteManifest(PreviewDirectory(),bundlePath);
            }
            finally{bundle.Unload(true);}
            Debug.Log("SLAZEYA_FROZEN_BUNDLE_EXPORT_PASS");
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    private static void Require(bool ok,string text)
    {
        if(!ok) throw new Exception("FAIL "+text);
        Debug.Log("PASS "+text);
    }
    public static void DiagnoseVolumeShader()
    {
        var shader=Shader.Find(SlazeyaStormVisualController.CloudVolumeShaderName);
        Debug.Log("Volume found="+(shader!=null)+" supported="+(shader!=null&&shader.isSupported));
        if(shader!=null){var material=new Material(shader);ShaderUtil.CompilePass(material,0,true);Debug.Log("Volume hasErrors="+ShaderUtil.ShaderHasError(shader)+" setPass="+material.SetPass(0));Object.DestroyImmediate(material);}
        if(shader!=null)foreach(var message in ShaderUtil.GetShaderMessages(shader))Debug.LogError(message.message+" at "+message.file+":"+message.line);
    }
    [Serializable] private class CoverageSweepReport
    {
        public string baselineVersion,baselineBundleSha256,controllerSha256,graphicsDevice,output;
        public string status="Readback-only material diagnostic, not an accepted candidate; no Bundle build or asset changes";
        public float time=0.55f,erosionStrength=0.30f;
        public float[] coverages={0.30f,0.40f,0.50f,0.60f},sigmaTs={4.5f,7.5f};
        public int renderedCombinations;
    }
    public static void DiagnoseCoverageSweep()
    {
        try
        {
            const string expectedBundle="4737D3C7EBC789FA219968B6D0C39E849F9ED44149F54826748E73816C332E59";
            const string expectedController="B4C4940A1E68ABF2D85E3DC8C18A70D40ADCD9A7C67E8E7DDF3B86E9951EAB5E";
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles/"+SlazeyaStormVisualController.BundleName));
            Require(Digest(path)==expectedBundle,"sweep reads exact existing R5.6 Bundle");
            Require(Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs"))==expectedController,"sweep controller matches baseline");
            string output=Path.Combine(PreviewDirectory(),"coverage-sweep-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);
            Debug.Log("COVERAGE_SWEEP_OUTPUT "+output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var bundle=AssetBundle.LoadFromFile(path);Require(bundle!=null,"readback-only sweep Bundle load");
            GameObject root=null,stage=null;Camera camera=null;RenderTexture rt=null;Texture2D pixels=null;
            try
            {
                var prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);
                SlazeyaStormCloudNoiseBaker.VerifyBundle(prefab,output);
                root=Object.Instantiate(prefab);stage=CreateStage(true);camera=CreateCamera(true);
                rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
                var report=new CoverageSweepReport{baselineVersion="2026-09-06.round5.6",baselineBundleSha256=expectedBundle,controllerSha256=expectedController,graphicsDevice=SystemInfo.graphicsDeviceName,output=output};
                using(var driver=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
                {
                    while(driver.Elapsed<0.55f-0.000001f)driver.Advance(Mathf.Min(1f/120f,0.55f-driver.Elapsed));
                    foreach(var r in root.GetComponentsInChildren<ParticleSystemRenderer>())r.enabled=false;
                    var volumes=root.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<Renderer>();
                    var original=new MaterialPropertyBlock[volumes.Length];var positions=new Vector3[volumes.Length];var rotations=new Quaternion[volumes.Length];var scales=new Vector3[volumes.Length];
                    for(int i=0;i<volumes.Length;i++){original[i]=new MaterialPropertyBlock();volumes[i].GetPropertyBlock(original[i]);positions[i]=volumes[i].transform.position;rotations[i]=volumes[i].transform.rotation;scales[i]=volumes[i].transform.localScale;}
                    foreach(float coverage in report.coverages)
                    {
                        string key="coverage_"+Mathf.RoundToInt(coverage*100).ToString("D2");string slices=Path.Combine(output,key+"_slice");Directory.CreateDirectory(slices);
                        CaptureDensitySlice(root,driver,slices,coverage,false);
                        foreach(float sigma in report.sigmaTs)
                        {
                            for(int i=0;i<volumes.Length;i++)
                            {
                                var block=new MaterialPropertyBlock();volumes[i].GetPropertyBlock(block);block.SetFloat("_Coverage",coverage);block.SetFloat("_SigmaT",sigma);block.SetFloat("_ErosionStrength",0.30f);volumes[i].SetPropertyBlock(block);
                            }
                            string imageName=key+"_sigma_"+Mathf.RoundToInt(sigma*10).ToString("D2")+".png";
                            File.WriteAllBytes(Path.Combine(output,imageName),Render(camera,rt,pixels));report.renderedCombinations++;
                            for(int i=0;i<volumes.Length;i++)Require(volumes[i].transform.position==positions[i]&&volumes[i].transform.rotation==rotations[i]&&volumes[i].transform.localScale==scales[i],"sweep geometry frozen "+i);
                            Require(Mathf.Abs(driver.Elapsed-0.55f)<0.00001f&&!driver.BurstTriggered,"sweep time and lifecycle frozen");
                        }
                    }
                    for(int i=0;i<volumes.Length;i++)volumes[i].SetPropertyBlock(original[i]);
                }
                File.WriteAllText(Path.Combine(output,"sweep-manifest.json"),JsonUtility.ToJson(report,true));
                Require(Digest(path)==expectedBundle,"readback sweep did not modify Bundle");
                Debug.Log("COVERAGE_SWEEP_COMPLETE "+output);
            }
            finally
            {
                if(camera!=null)camera.targetTexture=null;if(root!=null)Object.DestroyImmediate(root);if(stage!=null)Object.DestroyImmediate(stage);if(camera!=null)Object.DestroyImmediate(camera.gameObject);if(rt!=null)Object.DestroyImmediate(rt);if(pixels!=null)Object.DestroyImmediate(pixels);bundle.Unload(true);
            }
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    public static void DiagnoseB0()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        string output=PreviewDirectory();var bundle=AssetBundle.LoadFromFile(Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles/"+SlazeyaStormVisualController.BundleName)));
        var root=Object.Instantiate(bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName));
        var f=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H));
        var stage=CreateStage(true);Camera camera=CreateCamera(true);var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        using(var driver=new SlazeyaStormVisualController(root,f))
        {
            for(int i=0;i<93;i++)driver.Advance(1f/60f);driver.TriggerBurst();
            var renderers=root.GetComponentsInChildren<Renderer>();foreach(var r in renderers)r.enabled=false;
            var report=new StringBuilder("renderer,count,particle_size,baked_center,baked_size,local_radius,world_radius\n");
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>())
            {
                if(ps.particleCount==0)continue;var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.enabled=true;
                File.WriteAllBytes(Path.Combine(output,"diagnostic_b0_"+ps.name+".png"),Render(camera,rt,pixels));
                var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);var mesh=new Mesh();renderer.BakeMesh(mesh,camera,true);
                float localRadius=0,worldRadius=0;foreach(var v in mesh.vertices){localRadius=Mathf.Max(localRadius,Vector3.Distance(ps.transform.TransformPoint(v),f.EnvelopeCenter));worldRadius=Mathf.Max(worldRadius,Vector3.Distance(v,f.EnvelopeCenter));}
                report.AppendLine(ps.name+","+count+",\""+particles[0].GetCurrentSize3D(ps)+"\",\""+mesh.bounds.center+"\",\""+mesh.bounds.size+"\","+localRadius+","+worldRadius);
                Object.DestroyImmediate(mesh);renderer.enabled=false;
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();renderer.enabled=true;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                File.WriteAllBytes(Path.Combine(output,"diagnostic_b0_"+filter.name+".png"),Render(camera,rt,pixels));
                report.AppendLine(filter.name+",burst="+block.GetFloat("_BurstAge")+",core="+block.GetFloat("_CoreRadius")+",pulse="+block.GetFloat("_Pulse"));renderer.enabled=false;
            }
            File.WriteAllText(Path.Combine(output,"diagnostic-b0-baked.csv"),report.ToString());
        }
        camera.targetTexture=null;Object.DestroyImmediate(root);Object.DestroyImmediate(stage);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);bundle.Unload(true);
    }
    private static Transform Child(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    private static void SaveAsset(Object asset,string path)
    {
        if(AssetDatabase.LoadAssetAtPath<Object>(path)!=null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset,path);
    }
    private static Texture2D[] ImportWorkerTextures()
    {
        string source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../source_assets/round2"));
        var textures=new Texture2D[TextureFiles.Length];TextureHashes.Clear();
        for(int i=0;i<TextureFiles.Length;i++)
        {
            string file=Path.Combine(source,TextureFiles[i]);
            Require(File.Exists(file),"approved worker source asset "+TextureFiles[i]);
            string imported="Assets/Textures/Round2/"+TextureFiles[i];
            File.Copy(file,imported,true);TextureHashes.Add(Digest(file));
            Require(Digest(file)==Digest(imported),"exact worker PNG copy "+TextureFiles[i]);
            AssetDatabase.ImportAsset(imported,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(imported);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapModeU=i<3?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.wrapModeV=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.maxTextureSize=1024;
            importer.SaveAndReimport();
            textures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(imported);
            int width=i==2?512:1024, height=i==3||i==4?1024:512;
            Require(textures[i]!=null&&textures[i].width==width&&textures[i].height==height,"frozen PNG dimensions "+TextureFiles[i]);
        }
        return textures;
    }
    private static Material WaterMaterial(string name,Shader shader,Texture2D[] textures,bool low)
    {
        var material=new Material(shader){name=name};
        material.SetTexture("_BodyTex",textures[0]);material.SetTexture("_NormalRoughness",textures[1]);material.SetTexture("_FlowTex",textures[2]);
        material.SetFloat("_Alpha",0);
        SaveAsset(material,"Assets/Materials/Round3/"+name+".mat");return material;
    }
    private static Material ParticleMaterial(string name,Shader shader,Texture texture,Vector4 grid)
    {
        var material=new Material(shader){name=name};material.SetTexture("_Atlas",texture);material.SetVector("_Grid",grid);
        SaveAsset(material,"Assets/Materials/Round3/"+name+".mat");return material;
    }
    private static void MakeMacroClouds(Transform storm,Shader shader,Texture3D[] noise)
    {
        var material=new Material(shader){name="CloudMacroVolume"};material.SetFloat("_StepsPerH",48);material.SetFloat("_SigmaT",4.5f);material.SetFloat("_ShadowStrength",1);
        material.SetTexture("_CloudShapeTex",noise[0]);material.SetTexture("_CloudErosionTex",noise[1]);
        // Approved physical-distance cloud lobes in one continuous ring field.
        material.SetFloat("_ShapePeriodH",2);material.SetFloat("_DetailPeriodH",1.2f);material.SetFloat("_Coverage",1);material.SetFloat("_ErosionStrength",0.035f);material.SetFloat("_LightSteps",48);
        SaveAsset(material,"Assets/Materials/Round5/CloudMacroVolume.mat");
        // Copy the engine box's geometry only. The temporary primitive and its Collider are destroyed.
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
        var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]*=2;mesh.vertices=vertices;mesh.name="CloudUnitBox";mesh.RecalculateBounds();
        Object.DestroyImmediate(temporary);SaveAsset(mesh,"Assets/Meshes/Round5/CloudUnitBox.asset");
        Transform parent=Child(storm,"CloudMacroRoot");
        for(int i=0;i<SlazeyaStormVisualController.MacroCount;i++)
        {
            Transform node=Child(parent,"CloudMacro_"+i.ToString("D2"));node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    private struct RawCloudInputRecord
    {public Vector4 worldStep,carrier,shapeUVLod,shapeRGBA,detailUVLod,detailRGBA,density;}
    [Serializable] private class RawCloudInputReport
    {
        public string baselineBundleSha256,controllerSha256,shapeNumericHash,detailNumericHash,graphicsDevice;
        public int samples;public float geometryTime,maxGpuCpuError;
        public Vector4 shapeMinimum,shapeMaximum,detailMinimum,detailMaximum,shapeUVMinimum,shapeUVMaximum,detailUVMinimum,detailUVMaximum,carrierMinimum,carrierMaximum;
        public string method="Native CS probe uses the runtime density include, actual baseline material textures and MPB values. World center plus six axis offsets per macro, each with view and light step LOD; compared with independently filtered Bundle texels.";
    }
    private static Color ProbeFilterMip(Color32[] pixels,int size,Vector3 uv)
    {
        Vector3 p=new Vector3((uv.x-Mathf.Floor(uv.x))*size-0.5f,(uv.y-Mathf.Floor(uv.y))*size-0.5f,(uv.z-Mathf.Floor(uv.z))*size-0.5f);
        int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y),z=Mathf.FloorToInt(p.z);Vector3 f=new Vector3(p.x-x,p.y-y,p.z-z);Color sum=Color.clear;
        for(int dz=0;dz<2;dz++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
        {
            int ix=((x+dx)%size+size)%size,iy=((y+dy)%size+size)%size,iz=((z+dz)%size+size)%size;
            float weight=(dx==0?1-f.x:f.x)*(dy==0?1-f.y:f.y)*(dz==0?1-f.z:f.z);sum+=(Color)pixels[ix+size*(iy+iz*size)]*weight;
        }
        return sum;
    }
    private static Color ProbeFilterVolume(Color32[][] mips,int size,Vector4 uvLod)
    {
        float lod=Mathf.Clamp(uvLod.w,0,mips.Length-1);int low=Mathf.FloorToInt(lod),high=Mathf.Min(low+1,mips.Length-1);
        Color a=ProbeFilterMip(mips[low],Mathf.Max(1,size>>low),uvLod),b=ProbeFilterMip(mips[high],Mathf.Max(1,size>>high),uvLod);
        return Color.Lerp(a,b,lod-low);
    }
    private static void ProbeRange(ref Vector4 minimum,ref Vector4 maximum,Vector4 value)
    {for(int i=0;i<4;i++){minimum[i]=Mathf.Min(minimum[i],value[i]);maximum[i]=Mathf.Max(maximum[i],value[i]);}}
    public static void DiagnoseRawCoverageInputs()
    {
        try
        {
            const string bundleHash="4737D3C7EBC789FA219968B6D0C39E849F9ED44149F54826748E73816C332E59",driverHash="B4C4940A1E68ABF2D85E3DC8C18A70D40ADCD9A7C67E8E7DDF3B86E9951EAB5E";
            string output=Path.Combine(PreviewDirectory(),"coverage-sweep-20260906-050209","raw-gpu-inputs");Directory.CreateDirectory(output);
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles/"+SlazeyaStormVisualController.BundleName));
            Require(Digest(path)==bundleHash&&Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs"))==driverHash,"raw GPU probe uses unchanged R5.6 baseline");
            var bundle=AssetBundle.LoadFromFile(path);GameObject root=null;
            try
            {
                var prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);SlazeyaStormCloudNoiseBaker.VerifyBundle(prefab,output);
                root=Object.Instantiate(prefab);
                using(var driver=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
                {
                    while(driver.Elapsed<0.55f-0.000001f)driver.Advance(Mathf.Min(1f/120f,0.55f-driver.Elapsed));
                    var renderer=root.transform.Find("AB_StormRoot/CloudMacroRoot/CloudMacro_00").GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var material=renderer.sharedMaterial;
                    var shape=(Texture3D)material.GetTexture("_CloudShapeTex");var detail=(Texture3D)material.GetTexture("_CloudErosionTex");
                    var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/CloudRawInputProbe.compute");int kernel=compute.FindKernel("CSRawInputs");
                    foreach(string name in new[]{"_MacroCenters","_MacroAxisX","_MacroAxisY","_MacroAxisZ"}){var vectors=new List<Vector4>();block.GetVectorArray(name,vectors);compute.SetVectorArray(name,vectors.ToArray());}
                    foreach(string name in new[]{"_CloudHeight","_CloudSpinAngle","_DensityPhase","_Collapse","_MacroCount","_ProxyIndex","_LocalScale","_CloudGroundY","_SinkTravel","_SinkMaxRadius","_CoreAccumulation"})compute.SetFloat(name,block.GetFloat(name));
                    foreach(string name in new[]{"_ShapePeriodH","_DetailPeriodH","_Coverage","_ErosionStrength"})compute.SetFloat(name,material.GetFloat(name));
                    foreach(string name in new[]{"_CloudCenter","_CloudInnerRadii"})compute.SetVector(name,block.GetVector(name));
                    compute.SetTexture(kernel,"_CloudShapeTex",shape);compute.SetTexture(kernel,"_CloudErosionTex",detail);
                    var queries=new List<Vector4>();
                    for(int index=0;index<SlazeyaStormVisualController.MacroCount;index++)
                    {
                        var pose=driver.MacroPose(index);Vector3[] locations={pose.Center,pose.Center+pose.AxisX*pose.Radii.x*0.6f,pose.Center-pose.AxisX*pose.Radii.x*0.6f,pose.Center+pose.AxisY*pose.Radii.y*0.6f,pose.Center-pose.AxisY*pose.Radii.y*0.6f,pose.Center+pose.AxisZ*pose.Radii.z*0.6f,pose.Center-pose.AxisZ*pose.Radii.z*0.6f};
                        foreach(var pos in locations){queries.Add(new Vector4(pos.x,pos.y,pos.z,H*pose.Scale/24));queries.Add(new Vector4(pos.x,pos.y,pos.z,H*pose.Scale*0.24f));}
                    }
                    var input=new ComputeBuffer(queries.Count,16);var result=new ComputeBuffer(queries.Count,112);
                    try
                    {
                        input.SetData(queries.ToArray());compute.SetBuffer(kernel,"_ProbeQueries",input);compute.SetBuffer(kernel,"_ProbeResults",result);compute.SetInt("_ProbeCount",queries.Count);compute.Dispatch(kernel,(queries.Count+31)/32,1,1);
                        var values=new RawCloudInputRecord[queries.Count];result.GetData(values);
                        var shapeMips=new Color32[shape.mipmapCount][];var detailMips=new Color32[detail.mipmapCount][];for(int i=0;i<shapeMips.Length;i++)shapeMips[i]=shape.GetPixels32(i);for(int i=0;i<detailMips.Length;i++)detailMips[i]=detail.GetPixels32(i);
                        Vector4 high=Vector4.one*float.MaxValue,low=Vector4.one*float.MinValue;
                        var report=new RawCloudInputReport{baselineBundleSha256=bundleHash,controllerSha256=driverHash,graphicsDevice=SystemInfo.graphicsDeviceName,samples=queries.Count,geometryTime=driver.Elapsed,
                            shapeNumericHash=SlazeyaStormCloudNoiseBaker.TextureHash(shape),detailNumericHash=SlazeyaStormCloudNoiseBaker.TextureHash(detail),shapeMinimum=high,shapeMaximum=low,detailMinimum=high,detailMaximum=low,shapeUVMinimum=high,shapeUVMaximum=low,detailUVMinimum=high,detailUVMaximum=low,carrierMinimum=high,carrierMaximum=low};
                        var csv=new StringBuilder("macro,location,step_kind,world_x,world_y,world_z,world_step,carrier_x,carrier_y,carrier_z,shape_u,shape_v,shape_w,shape_lod,shape_r,shape_g,shape_b,shape_a,detail_u,detail_v,detail_w,detail_lod,detail_r,detail_g,detail_b,detail_a,base,eroded,max_gpu_cpu_error\n");
                        for(int i=0;i<values.Length;i++)
                        {
                            var v=values[i];Color expectedShape=ProbeFilterVolume(shapeMips,shape.width,v.shapeUVLod),expectedDetail=ProbeFilterVolume(detailMips,detail.width,v.detailUVLod);float error=0;
                            for(int channel=0;channel<4;channel++){error=Mathf.Max(error,Mathf.Abs(v.shapeRGBA[channel]-expectedShape[channel]));error=Mathf.Max(error,Mathf.Abs(v.detailRGBA[channel]-expectedDetail[channel]));}
                            report.maxGpuCpuError=Mathf.Max(report.maxGpuCpuError,error);ProbeRange(ref report.shapeMinimum,ref report.shapeMaximum,v.shapeRGBA);ProbeRange(ref report.detailMinimum,ref report.detailMaximum,v.detailRGBA);ProbeRange(ref report.shapeUVMinimum,ref report.shapeUVMaximum,v.shapeUVLod);ProbeRange(ref report.detailUVMinimum,ref report.detailUVMaximum,v.detailUVLod);ProbeRange(ref report.carrierMinimum,ref report.carrierMaximum,v.carrier);
                            csv.Append((i/14)+","+((i/2)%7)+","+(i%2==0?"view":"light"));
                            foreach(var vector in new[]{v.worldStep,v.carrier,v.shapeUVLod,v.shapeRGBA,v.detailUVLod,v.detailRGBA})for(int c=0;c<(vector==v.carrier?3:4);c++)csv.Append(","+vector[c].ToString("F7",CultureInfo.InvariantCulture));
                            csv.AppendLine(","+v.density.y.ToString("F7",CultureInfo.InvariantCulture)+","+v.density.z.ToString("F7",CultureInfo.InvariantCulture)+","+error.ToString("F7",CultureInfo.InvariantCulture));
                        }
                        File.WriteAllText(Path.Combine(output,"raw-gpu-inputs.csv"),csv.ToString());File.WriteAllText(Path.Combine(output,"raw-gpu-inputs.json"),JsonUtility.ToJson(report,true));
                        Require(report.maxGpuCpuError<0.002f,"runtime-coordinate GPU sampling matches actual Bundle texels: "+report.maxGpuCpuError);
                        Require(report.shapeMaximum.x-report.shapeMinimum.x>0.03f&&report.shapeMinimum.x<0.95f,"runtime Shape input is spatially varying, not default white");
                        Debug.Log("RAW_CLOUD_GPU_INPUTS_COMPLETE "+output);
                    }
                    finally{input.Release();result.Release();}
                }
            }
            finally{if(root!=null)Object.DestroyImmediate(root);bundle.Unload(true);}
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    [Serializable] private class DirectPwReport
    {
        public string baselineBundleSha256,controllerSha256,densitySourceSha256,volumeSourceSha256,output;
        public string status="Diagnostic source shader on existing Bundle geometry/textures; not a rebuilt or accepted candidate";
        public float geometryTime=0.55f,sigmaT=7.5f,erosionStrength=0.50f;
        public float[] coverages={0.25f,0.30f,0.35f};
        public string formula="shape=shapeTex.r; existing height/coverage remap and inverse-density cubed erosion retained";
    }
    public static void DiagnoseDirectPw(){DiagnoseDirectPw(false);}
    public static void DiagnoseCloudRepair(){DiagnoseDirectPw(true);}
    private static void DiagnoseDirectPw(bool repair)
    {
        try
        {
            const string bundleHash="4737D3C7EBC789FA219968B6D0C39E849F9ED44149F54826748E73816C332E59",controllerHash="B4C4940A1E68ABF2D85E3DC8C18A70D40ADCD9A7C67E8E7DDF3B86E9951EAB5E";
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles/"+SlazeyaStormVisualController.BundleName));
            Require(Digest(path)==bundleHash&&Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs"))==controllerHash,"direct PW diagnostic baseline geometry/controller unchanged");
            string output=Path.Combine(PreviewDirectory(),(repair?"cloud-repair-":"direct-pw-")+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);Debug.Log("DIRECT_PW_OUTPUT "+output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.ImportAsset("Assets/Shaders/SlazeyaStormCloudDensity.cginc",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Shaders/SlazeyaStormCloudVolume.shader",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Shaders/CloudDensitySlice.compute",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var sourceShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SlazeyaStormCloudVolume.shader");
            var compileMaterial=new Material(sourceShader);ShaderUtil.CompilePass(compileMaterial,0,true);Object.DestroyImmediate(compileMaterial);
            foreach(var message in ShaderUtil.GetShaderMessages(sourceShader))Debug.LogError(message.message+" at "+message.line);
            Require(sourceShader!=null&&sourceShader.isSupported&&!ShaderUtil.ShaderHasError(sourceShader),"direct PW source shader compiles");
            var bundle=AssetBundle.LoadFromFile(path);GameObject root=null,stage=null;Camera camera=null;RenderTexture rt=null;Texture2D pixels=null;Material temporary=null;
            try
            {
                var prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);SlazeyaStormCloudNoiseBaker.VerifyBundle(prefab,output);
                root=Object.Instantiate(prefab);stage=CreateStage(true);camera=CreateCamera(true);
                rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
                var report=new DirectPwReport{baselineBundleSha256=bundleHash,controllerSha256=controllerHash,densitySourceSha256=Digest(Path.Combine(Application.dataPath,"Shaders/SlazeyaStormCloudDensity.cginc")),volumeSourceSha256=Digest(Path.Combine(Application.dataPath,"Shaders/SlazeyaStormCloudVolume.shader")),output=output};
                using(var driver=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
                {
                    while(driver.Elapsed<0.55f-0.000001f)driver.Advance(Mathf.Min(1f/120f,0.55f-driver.Elapsed));
                    foreach(var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>())renderer.enabled=false;
                    var volumes=root.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<Renderer>();
                    var originalMaterials=new Material[volumes.Length];var originalSort=new int[volumes.Length];
                    var positions=new Vector3[volumes.Length];var rotations=new Quaternion[volumes.Length];var scales=new Vector3[volumes.Length];
                    temporary=new Material(sourceShader);temporary.CopyPropertiesFromMaterial(volumes[0].sharedMaterial);temporary.shader=sourceShader;temporary.SetFloat("_SigmaT",7.5f);temporary.SetFloat("_ErosionStrength",0.50f);
                    for(int i=0;i<volumes.Length;i++){originalMaterials[i]=volumes[i].sharedMaterial;originalSort[i]=volumes[i].sortingOrder;positions[i]=volumes[i].transform.position;rotations[i]=volumes[i].transform.rotation;scales[i]=volumes[i].transform.localScale;volumes[i].sharedMaterial=temporary;}
                    Vector3 mainPosition=camera.transform.position;Quaternion mainRotation=camera.transform.rotation;Color background=camera.backgroundColor;
                    if(repair){report.coverages=new[]{0.30f};report.formula="Direct PW unchanged; exit-bounded lightmarch of eroded union density; 12/24 midpoint convergence; NOT a visually accepted cloud candidate";}
                    foreach(float coverage in report.coverages)
                    {
                        temporary.SetFloat("_Coverage",coverage);string key="coverage_"+Mathf.RoundToInt(coverage*100).ToString("D2");
                        string slice=Path.Combine(output,key+"_slice");Directory.CreateDirectory(slice);CaptureDensitySlice(root,driver,slice,null,false);
                        camera.transform.position=mainPosition;camera.transform.rotation=mainRotation;
                        File.WriteAllBytes(Path.Combine(output,key+"_main.png"),Render(camera,rt,pixels));
                        camera.transform.position=driver.CoreCenter+new Vector3(45,4,0);camera.transform.LookAt(driver.CoreCenter);
                        File.WriteAllBytes(Path.Combine(output,key+"_side.png"),Render(camera,rt,pixels));
                        var scenery=stage.GetComponentsInChildren<Renderer>();foreach(var renderer in scenery)renderer.enabled=false;camera.backgroundColor=Color.white;temporary.SetFloat("_DebugTransmittance",1);
                        File.WriteAllBytes(Path.Combine(output,key+"_side_T.png"),Render(camera,rt,pixels));
                        temporary.SetFloat("_DebugTransmittance",0);camera.backgroundColor=background;foreach(var renderer in scenery)renderer.enabled=true;
                        if(repair||Mathf.Abs(coverage-0.30f)<0.0001f)
                        {
                            // Separate bounded diagnostics for light integration versus volume compositing.
                            temporary.SetFloat("_ShadowStrength",0);File.WriteAllBytes(Path.Combine(output,key+"_side_no_shadow.png"),Render(camera,rt,pixels));temporary.SetFloat("_ShadowStrength",1);
                            if(repair){temporary.SetFloat("_LightSteps",24);File.WriteAllBytes(Path.Combine(output,key+"_side_light24.png"),Render(camera,rt,pixels));temporary.SetFloat("_LightSteps",12);}
                            if(repair)
                            {
                                // Geometry stays fixed: adjacent explicit density phases isolate light stability.
                                for(int frame=0;frame<2;frame++)
                                {
                                    for(int i=0;i<volumes.Length;i++){var block=new MaterialPropertyBlock();volumes[i].GetPropertyBlock(block);block.SetFloat("_DensityPhase",driver.Elapsed+(frame+1)/60f);volumes[i].SetPropertyBlock(block);}
                                    File.WriteAllBytes(Path.Combine(output,key+"_side_density_next"+(frame+1)+".png"),Render(camera,rt,pixels));
                                }
                                for(int i=0;i<volumes.Length;i++){var block=new MaterialPropertyBlock();volumes[i].GetPropertyBlock(block);block.SetFloat("_DensityPhase",driver.Elapsed);volumes[i].SetPropertyBlock(block);}
                            }
                            if(!repair){
                            for(int i=0;i<volumes.Length;i++)volumes[i].sortingOrder=100+i;
                            File.WriteAllBytes(Path.Combine(output,key+"_side_sort_forward.png"),Render(camera,rt,pixels));
                            for(int i=0;i<volumes.Length;i++)volumes[i].sortingOrder=100+volumes.Length-1-i;
                            File.WriteAllBytes(Path.Combine(output,key+"_side_sort_reverse.png"),Render(camera,rt,pixels));
                            for(int i=0;i<volumes.Length;i++)volumes[i].sortingOrder=originalSort[i];}
                        }
                        for(int i=0;i<volumes.Length;i++)Require(volumes[i].transform.position==positions[i]&&volumes[i].transform.rotation==rotations[i]&&volumes[i].transform.localScale==scales[i],"direct PW geometry frozen "+i);
                    }
                    for(int i=0;i<volumes.Length;i++)volumes[i].sharedMaterial=originalMaterials[i];
                }
                File.WriteAllText(Path.Combine(output,"direct-pw-manifest.json"),JsonUtility.ToJson(report,true));
                Require(Digest(path)==bundleHash,"direct PW diagnosis preserves original Bundle");Debug.Log("DIRECT_PW_COMPLETE "+output);
            }
            finally
            {
                if(camera!=null)camera.targetTexture=null;if(root!=null)Object.DestroyImmediate(root);if(stage!=null)Object.DestroyImmediate(stage);if(camera!=null)Object.DestroyImmediate(camera.gameObject);if(rt!=null)Object.DestroyImmediate(rt);if(pixels!=null)Object.DestroyImmediate(pixels);if(temporary!=null)Object.DestroyImmediate(temporary);bundle.Unload(true);
            }
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    private static void MakeRingSurface(Transform parent,string name,Material material)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var patch=new List<Vector4>();var triangles=new List<int>();
        const int sectors=SlazeyaStormVisualController.SectorCount;
        for(int sector=0;sector<sectors;sector++)
        {
            float u0=(float)sector/sectors,u1=(float)(sector+1)/sectors;
            AddPatch(u0,u1,0,1,(u0+u1)*Mathf.PI,2,16,24,vertices,uv,patch,triangles);
        }
        var mesh=new Mesh{name=name+"_R5",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.SetUVs(1,patch);
        mesh.bounds=new Bounds(Vector3.zero,Vector3.one*8);SaveAsset(mesh,"Assets/Meshes/Round3/"+name+".asset");
        Transform node=Child(parent,name);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private static void AddPatch(float u0,float u1,float low,float high,float sector,int kind,int around,int rows,List<Vector3> vertices,List<Vector2> uv,List<Vector4> patch,List<int> triangles)
    {
        int start=vertices.Count;bool top=kind==0&&high==1;
        int regularRows=top?rows:rows+1;
        for(int i=0;i<=around;i++)for(int j=0;j<regularRows;j++)
        {
            float u=Mathf.Lerp(u0,u1,(float)i/around),v=Mathf.Lerp(low,high,(float)j/rows);
            vertices.Add(new Vector3(Mathf.Cos(u*Mathf.PI*2),v,Mathf.Sin(u*Mathf.PI*2)));
            uv.Add(new Vector2(u,v));patch.Add(new Vector4(sector,low,high,kind));
        }
        for(int i=0;i<around;i++)for(int j=0;j<regularRows-1;j++)
        {
            int k=start+i*regularRows+j,n=k+regularRows;
            if(kind==2){triangles.Add(k);triangles.Add(n);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(n);triangles.Add(n+1);}
            else{triangles.Add(k);triangles.Add(k+1);triangles.Add(n);triangles.Add(k+1);triangles.Add(n+1);triangles.Add(n);}
        }
        if(top)
        {
            // A single pole vertex per fracture-sector fan: identical position/normal across sectors until B.
            int pole=vertices.Count;vertices.Add(Vector3.up);uv.Add(new Vector2((u0+u1)*0.5f,1));patch.Add(new Vector4(sector,low,high,kind));
            for(int i=0;i<around;i++){int k=start+i*regularRows+regularRows-1;triangles.Add(k);triangles.Add(pole);triangles.Add(k+regularRows);}
        }
    }
    private static void MakeParticles(Transform parent,string name,Material material,int capacity,bool stretch)
    {
        var node=Child(parent,name);var ps=node.gameObject.AddComponent<ParticleSystem>();
        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.duration=4f;main.loop=false;main.playOnAwake=false;main.startSpeed=0;
        main.startLifetime=1f;main.maxParticles=capacity;main.startSize=0.1f;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Local;
        main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        main.gravityModifier=name=="WaterfallStreaks"?2.8f:name=="BurstDroplets"?1.8f:name=="BurstSheets"?0.7f:0f;
        if(name=="BurstDroplets"||name=="BurstSheets")
        {
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;
            velocity.speedModifier=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.35f,0.55f),new Keyframe(1,0.10f)));
        }
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var sheet=ps.textureSheetAnimation;sheet.enabled=false;
        ps.useAutoRandomSeed=false;ps.randomSeed=name=="BurstDroplets"?2125u:name=="WaterfallStreaks"?2017u:(uint)(capacity+1945);
        bool mist=name=="GatherMist"||name=="ReturnMist"||name=="CrestLiftMist";
        var color=ps.colorOverLifetime;color.enabled=!mist;
        color.color=new Gradient{colorKeys=new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            alphaKeys=new[]{new GradientAlphaKey(0.4f,0),new GradientAlphaKey(1,0.10f),new GradientAlphaKey(0.8f,0.65f),new GradientAlphaKey(0,1)}};
        if(name=="GatherFoam"){color.enabled=false;main.startSize3D=true;}
        if(name=="WaterfallStreaks")main.startSize3D=true;
        if(name=="CrestLiftMist"||name=="ReturnMist")
        {
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.7f,name=="CrestLiftMist"?2f:1.5f),new Keyframe(1,name=="CrestLiftMist"?2.1f:1.55f)));
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;
            velocity.speedModifier=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0.35f)));
        }
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.renderMode=stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
        if(name=="GatherFoam")renderer.sortMode=ParticleSystemSortMode.Distance;
        renderer.lengthScale=1.7f;renderer.velocityScale=0.025f;renderer.cameraVelocityScale=0;
        if(name=="WaterfallStreaks"){renderer.lengthScale=3.35f;renderer.velocityScale=0.055f;}
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,ParticleSystemVertexStream.AgePercent,ParticleSystemVertexStream.StableRandomX});
    }

    private static void MakeLightning(Transform parent,string name,Material material,int variant)
    {
        float sign=variant==0?1:-1;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        var path=new[]{new Vector3(0,0,0),new Vector3(0.15f,0.055f*sign,0),new Vector3(0.32f,-0.06f*sign,0),
            new Vector3(0.51f,0.047f*sign,0),new Vector3(0.72f,-0.043f*sign,0),new Vector3(0.87f,0.038f*sign,0),new Vector3(1,0,0)};
        AddBoltStroke(path,0.023f,vertices,uv,triangles);
        AddBoltStroke(new[]{path[3],new Vector3(0.56f,-0.09f*sign,0),new Vector3(0.63f,-0.13f*sign,0),new Vector3(0.60f,-0.22f*sign,0)},0.010f,vertices,uv,triangles);
        var mesh=new Mesh{name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateBounds();
        SaveAsset(mesh,"Assets/Meshes/Round3/"+name+".asset");
        Transform node=Child(parent,name);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.enabled=false;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private static void AddBoltStroke(Vector3[] points,float halfWidth,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
    {
        int offset=vertices.Count;
        for(int i=0;i<points.Length;i++)
        {
            Vector3 direction=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
            Vector3 across=new Vector3(-direction.y,direction.x,0)*halfWidth;
            vertices.Add(points[i]-across);vertices.Add(points[i]+across);
            uv.Add(new Vector2((float)i/(points.Length-1),0));uv.Add(new Vector2((float)i/(points.Length-1),1));
            if(i==points.Length-1)continue;
            int k=offset+i*2;triangles.Add(k);triangles.Add(k+2);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+3);
        }
    }

    private static void VerifyPrefab(GameObject prefab)
    {
        Require(prefab!=null,"fixed-name R5 prefab readback");
        Require(prefab.transform.Find("AB_StormRoot/BubbleEnvelope")==null&&prefab.transform.Find("AB_GroundRoot").GetComponentsInChildren<Renderer>().Length==0,"open cloud hole: no old shell or bottom water renderer");
        Require(prefab.GetComponentsInChildren<Collider>(true).Length==0,"all cloud proxies are visual-only, without colliders");
        Require(prefab.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<MeshRenderer>().Length==1,"one continuous cloud box renderer");
        foreach(Transform node in prefab.GetComponentsInChildren<Transform>(true))
            foreach(Component c in node.GetComponents<Component>())
                Require(c!=null&&!(c is MonoBehaviour),"native component "+node.name);
        foreach(Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
        {
            Material m=r.sharedMaterial;
            Require(m!=null&&m.shader!=null&&m.shader.isSupported,"readback supported shader "+r.name);
            bool water=m.shader.name==SlazeyaStormVisualController.ShaderName;
            bool particle=m.shader.name==SlazeyaStormVisualController.ParticleShaderName;
            bool cloud=m.shader.name==SlazeyaStormVisualController.CloudShaderName;
            bool volume=m.shader.name==SlazeyaStormVisualController.CloudVolumeShaderName;
            Require(water||particle||cloud||volume||m.shader.name==SlazeyaStormVisualController.LightningShaderName,"R5 shader whitelist "+r.name);
            foreach(string prop in water?new[]{"_BodyTex","_NormalRoughness","_FlowTex"}:volume?new[]{"_CloudShapeTex","_CloudErosionTex"}:cloud?new[]{"_Atlas","_FlowTex"}:particle?new[]{"_Atlas"}:new string[0])
                Require(m.GetTexture(prop)!=null,"real texture dependency "+r.name+prop);
        }
        foreach(MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            Require(f.sharedMesh!=null,"morph mesh "+f.name);
            if(f.name.EndsWith("Bolt"))continue;
            if(f.name.StartsWith("CloudMacro_")){foreach(Vector3 vertex in f.sharedMesh.vertices)RequireFinite(vertex,"unit macro proxy");continue;}
            var patchData=new List<Vector4>();f.sharedMesh.GetUVs(1,patchData);
            Require(patchData.Count==f.sharedMesh.vertexCount,"bubble / ring patch topology "+f.name);
        }
        foreach(ParticleSystem p in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            Require(!p.main.loop&&!p.main.playOnAwake&&!p.emission.enabled&&p.main.maxParticles>0,"finite explicit system "+p.name);
            var streams=new List<ParticleSystemVertexStream>();p.GetComponent<ParticleSystemRenderer>().GetActiveVertexStreams(streams);
            Require(streams.Contains(ParticleSystemVertexStream.AgePercent),"native particle-age atlas stream "+p.name);
        }
    }
    private static Bounds[] PreviewBodies(Vector3[] feet,float height)
    {
        var bodies=new Bounds[feet.Length];
        for(int i=0;i<feet.Length;i++)bodies[i]=new Bounds(feet[i]+Vector3.up*height*0.5f,new Vector3(2.18f,height,0.70f));
        return bodies;
    }
    [Serializable] private class CloudFitCase
    {
        public string name;public float time,maxBodyRadius,maxClusterSupport,halfX,halfZ,minFrameMargin,minMacroHeight;
        public Vector2 innerRadii;public int[] mainCloudsPer30Degrees=new int[12];
    }
    [Serializable] private class CloudFitReport {public List<CloudFitCase> cases=new List<CloudFitCase>();}
    private static void VerifyCloudRing(GameObject prefab)
    {
        var report=new CloudFitReport();var frameEvidence=new StringBuilder("sample,pixels,min_frame,max_frame\n");
        string output=PreviewDirectory();Directory.CreateDirectory(output);
        for(int scenario=0;scenario<4;scenario++)
        {
            Vector3[] feet=scenario==0?new[]{Feet[1]}:(Vector3[])Feet.Clone();if(scenario==3)feet[2].y+=0.35f*H;
            Bounds[] bodies=PreviewBodies(feet,H);if(scenario==2)bodies[2]=new Bounds(feet[2]+Vector3.up*H*0.9f,new Vector3(2.18f,H*1.8f,0.7f));
            var f=SlazeyaStormVisualController.FitFootprint(feet,H,bodies);
            var root=Object.Instantiate(prefab);Camera camera=CreateCamera(true);
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            using(var driver=new SlazeyaStormVisualController(root,f))
            {
                foreach(float time in new[]{0.25f,0.55f,0.60f,0.80f})
                {
                    while(driver.Elapsed<time-0.000001f)driver.Advance(Mathf.Min(1f/120f,time-driver.Elapsed));
                    var item=new CloudFitCase{name=new[]{"single","five","tall_1_8H","raised_foot_0_35H"}[scenario],time=time,innerRadii=f.CloudInnerRadii,minFrameMargin=10000};
                    foreach(var pair in bodies)foreach(Vector3 sample in SlazeyaStormVisualController.BodySamples(pair.center-Vector3.up*pair.extents.y,pair))
                    {Vector3 d=sample-f.EnvelopeCenter;float q=Mathf.Sqrt(d.x*d.x/(f.CloudInnerRadii.x*f.CloudInnerRadii.x)+d.z*d.z/(f.CloudInnerRadii.y*f.CloudInnerRadii.y));item.maxBodyRadius=Mathf.Max(item.maxBodyRadius,q);}
                    Require(item.maxBodyRadius<=0.95001f,"all body XZ samples inside initial source hole "+item.name+"/"+time);
                    var ps=root.transform.Find("AB_ParticleRoot/GatherFoam").GetComponent<ParticleSystem>();var particles=new ParticleSystem.Particle[168];
                    Require(ps.GetParticles(particles)==168,"native cloud identities "+item.name);
                    for(int i=0;i<168;i++)
                    {
                        Vector3 world=ps.transform.TransformPoint(particles[i].position),size=particles[i].GetCurrentSize3D(ps);
                        float half=Mathf.Sqrt(size.x*size.x+size.y*size.y)*0.5f;
                        Vector3 sourceSpine=SlazeyaStormVisualController.CloudSpine(f,i/7,driver.CloudSourcePhase);
                        Vector3 spine=SlazeyaStormVisualController.CloudForward(sourceSpine,driver.CoreCenter,f.Height,driver.CloudSinkTravel);
                        float support=Vector3.Distance(world,spine)+half;item.maxClusterSupport=Mathf.Max(item.maxClusterSupport,support);
                        RequireFinite(world,"transported cloud card");
                        if(Vector3.Distance(world,driver.FoamParcelPosition(i))>H*0.0001f)throw new Exception("Native transported cloud position mismatch "+i);
                        if(driver.CloudSinkTravel<=0&&support>H*0.45001f)throw new Exception("Initial cloud card breaches source-hole support budget "+i);
                        item.halfX=Mathf.Max(item.halfX,Mathf.Abs(world.x-f.EnvelopeCenter.x)+half);item.halfZ=Mathf.Max(item.halfZ,Mathf.Abs(world.z-f.EnvelopeCenter.z)+half);
                        float theta=Mathf.Atan2(spine.z-f.EnvelopeCenter.z,spine.x-f.EnvelopeCenter.x);if(theta<0)theta+=Mathf.PI*2;

                        // Full native nonuniform card corners, including the tangent-aligned cloud silk.
                        float angle=particles[i].rotation*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                        for(int corner=0;corner<4;corner++)
                        {
                            float x=((corner&1)==0?-1:1)*size.x*0.5f,y=((corner&2)==0?-1:1)*size.y*0.5f;
                            Vector3 point=world+camera.transform.right*(c*x-s*y)+camera.transform.up*(s*x+c*y);
                            Vector3 screen=camera.WorldToViewportPoint(point);float margin=Mathf.Min(screen.x*1280,(1-screen.x)*1280,screen.y*720,(1-screen.y)*720);
                            item.minFrameMargin=Mathf.Min(item.minFrameMargin,margin);
                        }
                    }
                    InspectMacroRing(root,driver,f,camera,item);
                    foreach(int count in item.mainCloudsPer30Degrees)Require(count>0,"effective main cloud in every 30-degree sector "+item.name);
                    if(scenario==1)
                    {
                        Require(Mathf.Abs(f.CloudInnerRadii.x-9.6f)<0.0001f&&Mathf.Abs(f.CloudInnerRadii.y-7.6f)<0.0001f,"standard inner hole 9.6 x 7.6");
                        item.minFrameMargin=Mathf.Min(item.minFrameMargin,InspectCloudRasterMargin(root,camera,rt,pixels));
                        Require(item.minFrameMargin>=24,"standard actual cloud/card frame margin "+item.minFrameMargin);
                        VerifyCloudFrames(root,camera,rt,pixels,"G"+time,frameEvidence);
                        if(time==0.55f){CaptureFixedDensity(root,camera,rt,pixels,driver);CaptureDensitySlice(root,driver);CaptureMacroViews(root,camera,rt,pixels,driver);}
                    }
                    report.cases.Add(item);
                }
                if(scenario==1)
                {
                    var stage=CreateStage(true);
                    camera.transform.position=f.EnvelopeCenter+new Vector3(45,4,0);camera.transform.LookAt(f.EnvelopeCenter);
                    File.WriteAllBytes(Path.Combine(output,"cloud_ring_five_side.png"),Render(camera,rt,pixels));Object.DestroyImmediate(stage);
                }
            }
            Object.DestroyImmediate(root);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
        File.WriteAllText(Path.Combine(output,"cloud-ring-fit.json"),JsonUtility.ToJson(report,true));
        File.WriteAllText(Path.Combine(output,"native-cloud-fixed-frames.csv"),frameEvidence.ToString());
    }
    private static void InspectMacroRing(GameObject root,SlazeyaStormVisualController driver,SlazeyaStormVisualController.Footprint f,Camera camera,CloudFitCase item)
    {
        var p=driver.CloudContainerPose;
        // Bound actual density support, rather than treating the empty box corners as cloud.
        // D+n>0 implies -.025H<r<.925H; a smooth mask excludes the inner hole.
        for(int sector=0;sector<12;sector++)
        {
            float theta=(sector+0.5f)*Mathf.PI/6;
            Vector3 normal=new Vector3(Mathf.Cos(theta)/f.CloudInnerRadii.x,0,Mathf.Sin(theta)/f.CloudInnerRadii.y).normalized;
            Vector3 spine=new Vector3(f.EnvelopeCenter.x+f.CloudInnerRadii.x*Mathf.Cos(theta),f.GroundY+f.Height*(0.75f+0.10f*Mathf.Sin(theta)),f.EnvelopeCenter.z+f.CloudInnerRadii.y*Mathf.Sin(theta))+normal*f.Height*0.45f;
            spine=SlazeyaStormVisualController.CloudForward(spine,driver.CoreCenter,f.Height,driver.CloudSinkTravel);
            Vector3 d=spine-p.Center;
            Require(Mathf.Abs(Vector3.Dot(d,p.AxisX))<p.Radii.x&&Mathf.Abs(Vector3.Dot(d,p.AxisY))<p.Radii.y&&Mathf.Abs(Vector3.Dot(d,p.AxisZ))<p.Radii.z,"transported spine inside single box "+sector);

        }
        InspectCloudDensitySectors(root,item);
    }
    private static void InspectCloudDensitySectors(GameObject root,CloudFitCase item)
    {
        var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/CloudDensitySlice.compute");
        int kernel=compute.FindKernel("CSRingSupport");
        var renderer=root.transform.Find("AB_StormRoot/CloudMacroRoot/CloudMacro_00").GetComponent<Renderer>();
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var material=renderer.sharedMaterial;
        foreach(string name in new[]{"_MacroCenters","_MacroAxisX","_MacroAxisY","_MacroAxisZ"})
        {var vectors=new List<Vector4>();block.GetVectorArray(name,vectors);compute.SetVectorArray(name,vectors.ToArray());}
        foreach(string name in new[]{"_CloudHeight","_CloudSpinAngle","_DensityPhase","_Collapse","_MacroCount","_ProxyIndex","_LocalScale","_CloudGroundY","_SinkTravel","_SinkMaxRadius","_CoreAccumulation"})compute.SetFloat(name,block.GetFloat(name));
        foreach(string name in new[]{"_ShapePeriodH","_DetailPeriodH","_Coverage","_ErosionStrength"})compute.SetFloat(name,material.GetFloat(name));
        foreach(string name in new[]{"_CloudCenter","_CloudInnerRadii"})compute.SetVector(name,block.GetVector(name));
        compute.SetFloat("_SliceWorldStep",block.GetFloat("_CloudHeight")*block.GetFloat("_LocalScale")/48);
        compute.SetTexture(kernel,"_CloudShapeTex",material.GetTexture("_CloudShapeTex"));compute.SetTexture(kernel,"_CloudErosionTex",material.GetTexture("_CloudErosionTex"));
        var buffer=new ComputeBuffer(12288,16);
        try
        {
            compute.SetBuffer(kernel,"_SliceOutput",buffer);compute.Dispatch(kernel,192,1,1);
            var values=new Vector4[12288];buffer.GetData(values);
            var minY=new float[12];var maxY=new float[12];
            for(int sector=0;sector<12;sector++){minY[sector]=float.MaxValue;maxY[sector]=float.MinValue;}
            Vector4 center=block.GetVector("_CloudCenter");
            for(int i=0;i<values.Length;i++)if(values[i].x>0.05f)
            {
                int sector=i/1024;item.mainCloudsPer30Degrees[sector]++;
                float y=values[i].z;
                item.halfX=Mathf.Max(item.halfX,Mathf.Abs(values[i].y-center.x));item.halfZ=Mathf.Max(item.halfZ,Mathf.Abs(values[i].w-center.z));
                minY[sector]=Mathf.Min(minY[sector],y);maxY[sector]=Mathf.Max(maxY[sector],y);
            }
            item.minMacroHeight=float.MaxValue;
            for(int sector=0;sector<12;sector++)item.minMacroHeight=Mathf.Min(item.minMacroHeight,
                item.mainCloudsPer30Degrees[sector]>0?maxY[sector]-minY[sector]:0);

        }
        finally{buffer.Release();}
    }
    private static float InspectCloudRasterMargin(GameObject root,Camera camera,RenderTexture rt,Texture2D pixels)
    {
        // A containing box has empty corners. Measure actual native absorption support
        // with the fixed camera instead of asserting obsolete 15x13 ellipsoid extents.
        var all=root.GetComponentsInChildren<Renderer>();var enabled=new bool[all.Length];
        var volume=root.transform.Find("AB_StormRoot/CloudMacroRoot/CloudMacro_00").GetComponent<Renderer>();
        var saved=new MaterialPropertyBlock();volume.GetPropertyBlock(saved);
        var diagnostic=new MaterialPropertyBlock();volume.GetPropertyBlock(diagnostic);diagnostic.SetFloat("_DebugTransmittance",1);
        Color background=camera.backgroundColor;int minX=1280,minY=720,maxX=-1,maxY=-1;
        try
        {
            for(int i=0;i<all.Length;i++){enabled[i]=all[i].enabled;all[i].enabled=all[i]==volume;}
            volume.SetPropertyBlock(diagnostic);camera.backgroundColor=Color.white;Render(camera,rt,pixels);
            var values=pixels.GetPixels32();
            for(int y=0;y<720;y++)for(int x=0;x<1280;x++)if(values[y*1280+x].r<253)
            {minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        }
        finally
        {
            volume.SetPropertyBlock(saved);camera.backgroundColor=background;
            for(int i=0;i<all.Length;i++)all[i].enabled=enabled[i];
        }
        Require(maxX>=minX&&maxY>=minY,"native cloud absorption is visible");
        return Mathf.Min(minX,minY,1279-maxX,719-maxY);
    }
    private static Renderer[] FieldRenderers(GameObject root)
    {
        var result=new List<Renderer>();foreach(var r in root.GetComponentsInChildren<Renderer>())
            if(r.sharedMaterial.shader.name==SlazeyaStormVisualController.CloudShaderName||r.sharedMaterial.shader.name==SlazeyaStormVisualController.CloudVolumeShaderName)result.Add(r);
        return result.ToArray();
    }
    private static void CaptureMacroViews(GameObject root,Camera camera,RenderTexture rt,Texture2D pixels,SlazeyaStormVisualController driver)
    {
        var stage=CreateStage(true);string output=PreviewDirectory();var particles=root.GetComponentsInChildren<ParticleSystemRenderer>();var was=new bool[particles.Length];
        for(int i=0;i<particles.Length;i++){was[i]=particles[i].enabled;particles[i].enabled=false;}
        File.WriteAllBytes(Path.Combine(output,"macro_only_G055.png"),Render(camera,rt,pixels));
        var renderers=root.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<Renderer>();var blocks=new MaterialPropertyBlock[renderers.Length];
        for(int i=0;i<renderers.Length;i++){blocks[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(blocks[i]);var changed=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(changed);changed.SetFloat("_ShadowStrength",0);renderers[i].SetPropertyBlock(changed);}
        File.WriteAllBytes(Path.Combine(output,"macro_only_no_shadow_G055.png"),Render(camera,rt,pixels));
        for(int i=0;i<renderers.Length;i++)renderers[i].SetPropertyBlock(blocks[i]);
        // Same full box path at H/96 versus production H/48, each bounded to 1024 segments.
        for(int i=0;i<renderers.Length;i++){var changed=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(changed);changed.SetFloat("_StepsPerH",96);renderers[i].SetPropertyBlock(changed);}
        File.WriteAllBytes(Path.Combine(output,"macro_only_stepsPerH96_G055.png"),Render(camera,rt,pixels));
        for(int i=0;i<renderers.Length;i++)renderers[i].SetPropertyBlock(blocks[i]);
        // True T map: black absorption composited over diagnostic white, without actors.
        // This affects only this diagnostic view; the normal scene/camera is restored immediately.
        var scenery=stage.GetComponentsInChildren<Renderer>();foreach(var r in scenery)r.enabled=false;
        Color background=camera.backgroundColor;camera.backgroundColor=Color.white;
        for(int i=0;i<renderers.Length;i++){var changed=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(changed);changed.SetFloat("_DebugTransmittance",1);renderers[i].SetPropertyBlock(changed);}
        File.WriteAllBytes(Path.Combine(output,"macro_only_T_G055.png"),Render(camera,rt,pixels));
        for(int i=0;i<renderers.Length;i++)renderers[i].SetPropertyBlock(blocks[i]);
        camera.backgroundColor=background;foreach(var r in scenery)r.enabled=true;
        Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
        camera.transform.position=driver.CoreCenter+new Vector3(45,4,0);camera.transform.LookAt(driver.CoreCenter);
        File.WriteAllBytes(Path.Combine(output,"macro_only_side_G055.png"),Render(camera,rt,pixels));
        camera.transform.position=position;camera.transform.rotation=rotation;
        for(int i=0;i<particles.Length;i++)particles[i].enabled=was[i];Object.DestroyImmediate(stage);
    }
    private static void VerifyCloudFrames(GameObject root,Camera camera,RenderTexture rt,Texture2D pixels,string label,StringBuilder evidence)
    {
        var ps=root.transform.Find("AB_ParticleRoot/GatherFoam").GetComponent<ParticleSystem>();var renderer=ps.GetComponent<Renderer>();
        var macros=root.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<Renderer>();var macroEnabled=new bool[macros.Length];for(int i=0;i<macros.Length;i++){macroEnabled[i]=macros[i].enabled;macros[i].enabled=false;}
        var original=new ParticleSystem.Particle[168];int count=ps.GetParticles(original);var altered=(ParticleSystem.Particle[])original.Clone();
        for(int pass=0;pass<2;pass++)
        {
            for(int i=0;i<count;i++)altered[i].remainingLifetime=altered[i].startLifetime*(pass==0?0.90f:0.10f);ps.SetParticles(altered,count);
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_DebugFrame",1);renderer.SetPropertyBlock(block);Render(camera,rt,pixels);
            int observed=0;float min=7,max=1;
            foreach(Color32 pixel in pixels.GetPixels32())if(pixel.b==0&&pixel.g>3)
            {
                float frame=pixel.r/255f*7,expected=1+Mathf.Min(5.999f,pixel.g/255f*6);
                if(Mathf.Abs(frame-expected)>0.055f||frame<0.97f||frame>7.03f)throw new Exception("Cloud fixed-frame native stream mismatch "+label);
                observed++;min=Mathf.Min(min,frame);max=Mathf.Max(max,frame);
            }
            Require(observed>(label=="G2.0"?0:16),"native cloud frames 1..7 independent of real age "+label+"/"+pass);
            evidence.AppendLine(label+"/age"+(pass==0?"0.1":"0.9")+","+observed+","+min+","+max);
            block.SetFloat("_DebugFrame",0);renderer.SetPropertyBlock(block);
        }
        ps.SetParticles(original,count);for(int i=0;i<macros.Length;i++)macros[i].enabled=macroEnabled[i];
    }
    private static void CaptureFixedDensity(GameObject root,Camera camera,RenderTexture rt,Texture2D pixels,SlazeyaStormVisualController driver)
    {
        // Same live cloud particles, positions, sizes, rotations, atlas frames and camera throughout.
        // Only _DensityPhase changes. The normal views accompany the isolated scalar-field readback.
        string output=PreviewDirectory();var stage=CreateStage(true);
        var renderers=FieldRenderers(root);var saved=new MaterialPropertyBlock[renderers.Length];var probe=new MaterialPropertyBlock[renderers.Length];
        for(int i=0;i<renderers.Length;i++){saved[i]=new MaterialPropertyBlock();probe[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(saved[i]);renderers[i].GetPropertyBlock(probe[i]);}
        Color32[] first=null;var csv=new StringBuilder("density_phase,field_pixels,brightening_pixels,darkening_pixels,mean_absolute_field_change,frozen_geometry_time\n");
        float[] phases={0.35f,0.55f,0.75f};
        for(int sample=0;sample<phases.Length;sample++)
        {
            for(int i=0;i<renderers.Length;i++){probe[i].SetFloat("_DensityPhase",phases[sample]);probe[i].SetFloat("_DebugDensity",0);renderers[i].SetPropertyBlock(probe[i]);}
            File.WriteAllBytes(Path.Combine(output,"density_fixed_"+sample+".png"),Render(camera,rt,pixels));
            for(int i=0;i<renderers.Length;i++){probe[i].SetFloat("_DebugDensity",1);renderers[i].SetPropertyBlock(probe[i]);}
            File.WriteAllBytes(Path.Combine(output,"density_field_"+sample+".png"),Render(camera,rt,pixels));
            var current=pixels.GetPixels32();if(first==null)first=current;
            int count=0,positive=0,negative=0;double absolute=0;
            for(int i=0;i<current.Length;i++)if(first[i].r==first[i].g&&first[i].g==first[i].b&&first[i].r>3)
            {
                int delta=current[i].r-first[i].r;count++;absolute+=Math.Abs(delta)/255.0;
                if(delta>2)positive++;if(delta<-2)negative++;
            }
            double mean=absolute/Math.Max(1,count);
            if(sample>0)Require(count>100&&positive>16&&negative>16&&mean>0.002,"fixed cloud geometry: density changes locally in both directions "+phases[sample]+" mean="+mean);
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F3},{1},{2},{3},{4:F6},{5:F4}",phases[sample],count,positive,negative,mean,driver.Elapsed));
        }
        for(int i=0;i<renderers.Length;i++)renderers[i].SetPropertyBlock(saved[i]);Object.DestroyImmediate(stage);
        File.WriteAllText(Path.Combine(output,"native-cloud-density-fixed.csv"),csv.ToString());
    }
    [Serializable] private class DensitySliceReport
    {
        public int resolution=256,samples,baseOccupied,erodedOccupied,erodedToZero;
        public float geometryTime,worldStep,maximumBase,maximumEroded,maximumRemoved;
        public float coverage=0.82f,erosionStrength=0.30f,meanOccupiedBase,meanOccupiedEroded;
        public Vector3 center;public Vector2 halfExtent;
        public string channels="Same fixed XZ plane: R=base, G=physical-distance eroded density, B=removed density, A=core ellipsoid envelope";
    }
    private static void CaptureDensitySlice(GameObject root,SlazeyaStormVisualController driver,string outputOverride=null,float? coverageOverride=null,bool separateMaps=true)
    {
        const int size=256;string output=outputOverride??PreviewDirectory();
        var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/CloudDensitySlice.compute");Require(compute!=null,"independent runtime density slice compute");
        int kernel=compute.FindKernel("CSSlice");var renderer=root.transform.Find("AB_StormRoot/CloudMacroRoot/CloudMacro_00").GetComponent<Renderer>();
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var material=renderer.sharedMaterial;
        foreach(string name in new[]{"_MacroCenters","_MacroAxisX","_MacroAxisY","_MacroAxisZ"}){var vectors=new List<Vector4>();block.GetVectorArray(name,vectors);compute.SetVectorArray(name,vectors.ToArray());}
        foreach(string name in new[]{"_CloudHeight","_CloudSpinAngle","_DensityPhase","_Collapse","_MacroCount","_ProxyIndex","_LocalScale","_CloudGroundY","_SinkTravel","_SinkMaxRadius","_CoreAccumulation"})compute.SetFloat(name,block.GetFloat(name));
        foreach(string name in new[]{"_ShapePeriodH","_DetailPeriodH","_Coverage","_ErosionStrength"})compute.SetFloat(name,material.GetFloat(name));
        if(coverageOverride.HasValue)compute.SetFloat("_Coverage",coverageOverride.Value);
        foreach(string name in new[]{"_CloudCenter","_CloudInnerRadii"})compute.SetVector(name,block.GetVector(name));
        compute.SetTexture(kernel,"_CloudShapeTex",material.GetTexture("_CloudShapeTex"));compute.SetTexture(kernel,"_CloudErosionTex",material.GetTexture("_CloudErosionTex"));
        Vector4 hole=block.GetVector("_CloudInnerRadii");float height=block.GetFloat("_CloudHeight");
        var report=new DensitySliceReport{samples=size*size,geometryTime=driver.Elapsed,center=driver.CoreCenter,halfExtent=new Vector2(hole.x+height*1.1f,hole.y+height*1.1f),worldStep=height*block.GetFloat("_LocalScale")/48};
        report.coverage=coverageOverride??material.GetFloat("_Coverage");report.erosionStrength=material.GetFloat("_ErosionStrength");
        compute.SetInt("_SliceResolution",size);compute.SetVector("_SliceCenter",report.center);compute.SetVector("_SliceHalfSize",new Vector4(report.halfExtent.x,report.halfExtent.y,0,0));compute.SetFloat("_SliceWorldStep",report.worldStep);
        var buffer=new ComputeBuffer(size*size,16);
        try
        {
            compute.SetBuffer(kernel,"_SliceOutput",buffer);compute.Dispatch(kernel,size/8,size/8,1);var values=new Vector4[size*size];buffer.GetData(values);
            var maps=new[]{new Color32[size*size],new Color32[size*size],new Color32[size*size]};var pair=new Color32[size*size*2];
            for(int i=0;i<values.Length;i++)
            {
                var v=values[i];RequireFinite(new Vector3(v.x,v.y,v.z),"runtime density slice");
                if(v.x>0.0001f)report.baseOccupied++;if(v.y>0.0001f)report.erodedOccupied++;if(v.x>0.0001f&&v.y<=0.0001f)report.erodedToZero++;
                if(v.x>0.0001f)report.meanOccupiedBase+=v.x;if(v.y>0.0001f)report.meanOccupiedEroded+=v.y;
                if(v.y>v.x+0.00001f||v.y<0)throw new Exception("Erosion increased density or returned negative density");
                report.maximumBase=Mathf.Max(report.maximumBase,v.x);report.maximumEroded=Mathf.Max(report.maximumEroded,v.y);report.maximumRemoved=Mathf.Max(report.maximumRemoved,v.z);
                for(int channel=0;channel<3;channel++){byte grey=(byte)Mathf.RoundToInt(Mathf.Clamp01(v[channel])*255);maps[channel][i]=new Color32(grey,grey,grey,255);}
                int row=i/size,column=i%size;pair[row*size*2+column]=maps[0][i];pair[row*size*2+size+column]=maps[1][i];
            }
            string[] names={"density_slice_base.png","density_slice_eroded.png","density_slice_removed.png"};
            if(separateMaps)for(int channel=0;channel<3;channel++){var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);texture.SetPixels32(maps[channel]);texture.Apply();File.WriteAllBytes(Path.Combine(output,names[channel]),texture.EncodeToPNG());Object.DestroyImmediate(texture);}
            var compare=new Texture2D(size*2,size,TextureFormat.RGBA32,false,true);compare.SetPixels32(pair);compare.Apply();File.WriteAllBytes(Path.Combine(output,"density_slice_base_vs_eroded.png"),compare.EncodeToPNG());Object.DestroyImmediate(compare);
            report.meanOccupiedBase/=Mathf.Max(1,report.baseOccupied);report.meanOccupiedEroded/=Mathf.Max(1,report.erodedOccupied);
            File.WriteAllText(Path.Combine(output,"density-slice.json"),JsonUtility.ToJson(report,true));
            Require(report.baseOccupied>0&&report.erodedOccupied>0&&report.erodedToZero>0,"native base/detail slice removes thin edges to zero while retaining cloud body");
        }
        finally{buffer.Release();}
    }
    public static void DiagnoseStreakStretch()
    {
        try
        {
            ConfigurePreview();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("StreakStretchProbe");var camera=CreateCamera(true);
            var material=new Material(Shader.Find(SlazeyaStormVisualController.ParticleShaderName));
            MakeParticles(root.transform,"WaterfallStreaks",material,128,true);
            var ps=root.GetComponentInChildren<ParticleSystem>();var renderer=ps.GetComponent<ParticleSystemRenderer>();var main=ps.main;main.startSize3D=true;
            var csv=new StringBuilder("size_x,size_y,size_z,vertex_count,edge01,edge12,uv0x,uv0y,uv0z,uv0w\n");
            foreach(Vector3 size in new[]{Vector3.one,new Vector3(2,1,1),new Vector3(1,2,1),new Vector3(1,0.25f,1)})
            {
                var particle=new ParticleSystem.Particle{position=new Vector3(10,3,1),velocity=new Vector3(3,-2,1),startSize3D=size,startLifetime=1,remainingLifetime=0.6f,startColor=Color.white,randomSeed=9301};
                ps.Simulate(0,false,false,false);ps.SetParticles(new[]{particle},1);camera.Render();var mesh=new Mesh();renderer.BakeMesh(mesh,camera,false);Vector3[] v=mesh.vertices;var uv=new List<Vector4>();mesh.GetUVs(0,uv);
                Debug.Log("STRETCH_PROBE alive="+ps.particleCount+" vertices="+v.Length+" uv="+uv.Count);
                Require(v.Length>=4,"native Stretch quad BakeMesh");Vector4 u=uv.Count>0?uv[0]:Vector4.zero;
                csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",size.x,size.y,size.z,v.Length,(v[1]-v[0]).magnitude,(v[2]-v[1]).magnitude,u.x,u.y,u.z,u.w));
                Object.DestroyImmediate(mesh);
            }
            File.WriteAllText(Path.Combine(PreviewDirectory(),"native-stretch-axis-probe.csv"),csv.ToString());
            Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(material);Debug.Log("SLAZEYA_STRETCH_PROBE_PASS");
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    private static void VerifyBurstAbundance(GameObject prefab)
    {
        int[] capacities={40,64,48,320,32,24,168,128};
        Require(prefab.GetComponentsInChildren<ParticleSystem>(true).Length==8,"exactly eight native particle systems");
        int rings=0;foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))if(r.name=="RingJet")rings++;
        Require(rings==1,"one original RingJet renderer");
        var csv=new StringBuilder("hz,cycle,burst_age,system,alive,capacity,unique_emitted\n");
        var summary=new StringBuilder("hz,cycle,initial_sheets,initial_drops,second_sheets,second_drops,waterfall_streaks,satellite_drops,maximum_alive\n");
        foreach(int hz in new[]{60,240})for(int cycle=0;cycle<2;cycle++)
        {
            var root=Object.Instantiate(prefab);int maximumAlive=0;
            using(var driver=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
            {
                var systems=new ParticleSystem[8];var seeds=new HashSet<uint>[8];
                for(int i=0;i<8;i++)
                {
                    systems[i]=root.transform.Find("AB_ParticleRoot/"+SlazeyaStormVisualController.ParticleNames[i]).GetComponent<ParticleSystem>();seeds[i]=new HashSet<uint>();
                    Require(systems[i].main.maxParticles==capacities[i],"native capacity "+systems[i].name);
                }
                Require(systems[3].randomSeed==2125u&&systems[7].randomSeed==2017u,"capacity-independent original native seeds 2125/2017");
                for(int i=0;i<93;i++)driver.Advance(1f/60f);
                Require(driver.TriggerBurst()&&!driver.TriggerBurst(),"one burst per fresh instance");
                for(int step=0;step<=hz*1.25f;step++)
                {
                    if(step>0)driver.Advance(1f/hz);
                    maximumAlive=Mathf.Max(maximumAlive,driver.AliveParticles);
                    for(int i=0;i<8;i++)
                    {
                        var buffer=new ParticleSystem.Particle[capacities[i]];int count=systems[i].GetParticles(buffer);
                        if(i!=6&&count>=capacities[i])throw new Exception("Dynamic pool saturated: "+systems[i].name);
                        for(int j=0;j<count;j++)seeds[i].Add(buffer[j].randomSeed);
                        csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3},{4},{5},{6}",hz,cycle,driver.BurstAge,systems[i].name,count,capacities[i],seeds[i].Count));
                    }
                    if(step==(int)(hz*0.8f))Require(systems[2].particleCount==0&&systems[7].particleCount==0,"B+.8 has no wide sheets or waterfall streaks");
                }
                int firstSheets=0,secondSheets=0,firstDrops=0,secondDrops=0,satellites=0;
                foreach(uint seed in seeds[2]){if(seed<10000)firstSheets++;else secondSheets++;}
                foreach(uint seed in seeds[3]){if(seed<9301)firstDrops++;else if(seed<10000)satellites++;else secondDrops++;}
                summary.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8}",hz,cycle,firstSheets,firstDrops,secondSheets,secondDrops,seeds[7].Count,satellites,maximumAlive));
                Require(firstSheets==7&&firstDrops==90,"unchanged initial 14-sector 7 sheets / 90 drops");
                Require(secondSheets==10&&secondDrops==56,"new 14-sector 10 sheets / 56 drops");
                Require(seeds[7].Count==112&&satellites==112,"four 8/8/8/4 waterfalls and 112 satellite drops");
                Require(maximumAlive<=640,"native peak <=640, observed "+maximumAlive);
                Require(driver.IsComplete&&driver.AliveParticles==0,"fresh instance clears at tail end");
                foreach(var r in root.GetComponentsInChildren<Renderer>())Require(!r.enabled,"tail renderer off "+r.name);
                driver.Dispose();driver.Dispose();Require(!driver.TriggerBurst(),"disposed instance cannot replay");
            }
            Object.DestroyImmediate(root);
        }
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-burst-counts.csv"),csv.ToString());
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-burst-summary.csv"),summary.ToString());
        var staleRoot=Object.Instantiate(prefab);
        using(var driver=new SlazeyaStormVisualController(staleRoot,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
        {
            driver.Advance(1.55f);driver.TriggerBurst();driver.Advance(0.31f);driver.Advance(0.01f);
            foreach(var ps in staleRoot.GetComponentsInChildren<ParticleSystem>())
            {
                var buffer=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(buffer);
                if(ps.name=="WaterfallStreaks")Require(count==0,"missed waterfall window consumed without late emission");
                if(ps.name=="BurstSheets"||ps.name=="BurstDroplets")for(int i=0;i<count;i++)Require(buffer[i].randomSeed<10000,"missed second sector window consumed without late emission");
            }
        }
        Object.DestroyImmediate(staleRoot);
    }
    private static void VerifyGpuWavefront()
    {
        // Compile the exact production CGINCLUDE, replacing only the diagnostic output pass.
        string flow=File.ReadAllText(Path.Combine(Application.dataPath,"Shaders/SlazeyaStormFlow.shader"));
        int start=flow.IndexOf("CGINCLUDE",StringComparison.Ordinal)+9,end=flow.IndexOf("ENDCG",start,StringComparison.Ordinal);
        string probe="Shader \"Hidden/SlazeyaBurstFrontProbe\" { SubShader { Pass { ZTest Always Cull Off ZWrite Off\nCGPROGRAM\n#pragma target 3.0\n#pragma vertex vert_img\n#pragma fragment ProbeFrag\n"+flow.Substring(start,end-start)+"\nfloat4 ProbeFrag(v2f_img i):SV_Target{return float4(RingPoint(i.uv),1);}\nENDCG\n} } }";
        probe=probe.Replace("\r\n","\n");
        const string path="Assets/Editor/SlazeyaBurstFrontProbe.shader";
        Require(!File.Exists(path),"temporary GPU probe path unused");File.WriteAllText(path,probe);
        Material material=null;RenderTexture rt=null;Texture2D pixels=null;RenderTexture previous=RenderTexture.active;
        try
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
            if(shader!=null)foreach(var message in ShaderUtil.GetShaderMessages(shader))Debug.Log("GPU_PROBE_DIAGNOSTIC "+message.message+" line="+message.line);
            Require(shader!=null&&!ShaderUtil.ShaderHasError(shader),"production Flow GPU probe compiles");
            material=new Material(shader);var f=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H));
            material.SetVector("_Radii",f.EnvelopeRadii);material.SetFloat("_Height",H);material.SetFloat("_Ground",f.GroundY-f.EnvelopeCenter.y);material.SetFloat("_CoreRadius",H*0.06f);
            rt=new RenderTexture(64,9,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);pixels=new Texture2D(64,9,TextureFormat.RGBAFloat,false,true);
            var csv=new StringBuilder("burst_age,samples,max_cpu_gpu_world_error\n");
            foreach(float age in new[]{0f,0.05f,7f/60f,0.25f,0.45f,0.8f})
            {
                material.SetFloat("_BurstAge",age);Graphics.Blit(Texture2D.blackTexture,rt,material);RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,64,9),0,0);pixels.Apply();
                Color[] values=pixels.GetPixels();float maximum=0;
                for(int y=0;y<9;y++)for(int x=0;x<64;x++)
                {
                    Color value=values[y*64+x];Vector3 gpu=new Vector3(value.r,value.g,value.b)+f.EnvelopeCenter;
                    RequireFinite(gpu,"GPU wave point");Vector3 cpu=SlazeyaStormVisualController.RingPoint(f,(x+0.5f)/64*Mathf.PI*2,(y+0.5f)/9,age);
                    maximum=Mathf.Max(maximum,Vector3.Distance(cpu,gpu));
                }
                csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F6},576,{1:G9}",age,maximum));
                Require(maximum<0.0001f*H,"actual GPU/CPU front agreement at B+"+age+" error="+maximum);
            }
            File.WriteAllText(Path.Combine(PreviewDirectory(),"native-cpu-gpu-wavefront.csv"),csv.ToString());
        }
        finally
        {
            RenderTexture.active=previous;if(material!=null)Object.DestroyImmediate(material);if(rt!=null)Object.DestroyImmediate(rt);if(pixels!=null)Object.DestroyImmediate(pixels);AssetDatabase.DeleteAsset(path);
        }
    }
    private static void VerifyController(GameObject prefab)
    {
        var footprint=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H));
        VerifyFoamTransport(prefab,footprint);
        foreach(Vector3 foot in Feet)
        {
            float x=(foot.x-footprint.Center.x)/footprint.RadiusX,z=(foot.z-footprint.Center.z)/footprint.RadiusZ;
            Require(x*x+z*z<=1.00001f,"frozen ellipse covers "+foot);
        }
        VerifyCloudRing(prefab);
        var root=Object.Instantiate(prefab);
        using(var driver=new SlazeyaStormVisualController(root,footprint))
        {
            for(int i=0;i<120;i++)driver.Advance(1f/60f);
            Require(driver.GatherReady&&!driver.BurstTriggered&&!driver.IsComplete,"hold cannot invent burst");
            Require(driver.TriggerBurst()&&!driver.TriggerBurst(),"burst callback idempotent");
            Require(driver.AliveParticles>0,"native crest emission");
            for(int i=0;i<79;i++)driver.Advance(1f/60f);
            Require(driver.IsComplete&&driver.AliveParticles==0,"R5 tail clears native particles");
            foreach(Renderer r in root.GetComponentsInChildren<Renderer>())Require(!r.enabled,"clear renderer "+r.name);
        }
        Object.DestroyImmediate(root);
        using(var missing=new SlazeyaStormVisualController(null,footprint))
        {
            missing.Advance(1.4f);missing.TriggerBurst();missing.Advance(1.3f);Require(missing.IsComplete,"missing bundle lifecycle");
        }
    }
    private static float InspectCoreSupport(GameObject root,SlazeyaStormVisualController driver,SlazeyaStormVisualController.Footprint f,StringBuilder csv)
    {
        float maximum=0;
        foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if(!renderer.enabled)continue;float support=0;
            var ps=renderer.GetComponent<ParticleSystem>();
            if(ps!=null)
            {
                var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);
                var pr=(ParticleSystemRenderer)renderer;
                for(int i=0;i<count;i++)
                {
                    Vector3 cardSize=particles[i].GetCurrentSize3D(ps);
                    float half=Mathf.Sqrt(cardSize.x*cardSize.x+cardSize.y*cardSize.y)*0.5f;
                    if(pr.renderMode==ParticleSystemRenderMode.Stretch)half=half*(1+pr.lengthScale)+particles[i].velocity.magnitude*pr.velocityScale;
                    support=Mathf.Max(support,Vector3.Distance(ps.transform.TransformPoint(particles[i].position),driver.CoreCenter)+half);
                }
            }
            else
            {
                Mesh mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                if(renderer.name.StartsWith("CloudMacro_"))
                {
                    for(int corner=0;corner<8;corner++)
                    {
                        Vector3 v=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                        Vector3 world=renderer.transform.TransformPoint(v);RequireFinite(world,"macro core corner");support=Mathf.Max(support,Vector3.Distance(world,driver.CoreCenter));
                    }
                    if(support>H*0.05701f)throw new Exception("Macro full corner core support exceeded "+renderer.name+" "+support);
                }
                else if(renderer.name.EndsWith("Bolt"))
                {foreach(Vector3 v in mesh.vertices)support=Mathf.Max(support,Vector3.Distance(renderer.transform.TransformPoint(v),driver.CoreCenter));}
                else
                {
                    var patches=new List<Vector4>();mesh.GetUVs(1,patches);Vector2[] uv=mesh.uv;
                    for(int i=0;i<uv.Length;i++)
                    {
                        Vector4 patch=patches[i];float theta=uv[i].x*Mathf.PI*2;
                        Vector3 point=SlazeyaStormVisualController.RingPoint(f,theta,uv[i].y,driver.BurstAge);
                        RequireFinite(point,renderer.name);support=Mathf.Max(support,Vector3.Distance(point,driver.CoreCenter));
                    }
                }
            }
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2},{3:F6},{4:F6}",driver.Elapsed,driver.BurstAge,renderer.name,support,driver.CoreVisibleLimit));
            if(renderer.name=="GatherFoam"&&support>H*0.06001f)throw new Exception("Cloud parcel core support exceeded "+support);
            if(support>driver.CoreVisibleLimit+H*0.0001f)throw new Exception("Core support exceeded by "+renderer.name+": "+support);
            maximum=Mathf.Max(maximum,support);
        }
        return maximum;
    }
    private static void RequireFinite(Vector3 point,string name)
    {if(!SlazeyaStormVisualController.IsFinite(point))throw new Exception("Nonfinite deformed point "+name);}
    private static void VerifyFoamTransport(GameObject prefab,SlazeyaStormVisualController.Footprint footprint)
    {
        Directory.CreateDirectory(PreviewDirectory());var root=Object.Instantiate(prefab);
        var parcels=new ParticleSystem.Particle[168];var previousRadii=new float[168];for(int i=0;i<168;i++)previousRadii[i]=float.MaxValue;
        var csv=new StringBuilder("time,parcel,x,y,z,core_distance,collapse,charge\n");
        var support=new StringBuilder("time,burst_age,renderer,support_radius,limit\n");
        using(var driver=new SlazeyaStormVisualController(root,footprint))
        {
            var ps=root.transform.Find("AB_ParticleRoot/GatherFoam").GetComponent<ParticleSystem>();
            float[] samples={0.65f,0.78f,0.94f,0.98f,1.08f,1.18f,1.24f,1.28f,1.333333f,1.35f,2.0f};float previous=0;
            foreach(float time in samples)
            {
                while(previous<time-0.000001f){float dt=Mathf.Min(1f/120f,time-previous);driver.Advance(dt);previous+=dt;}
                int count=ps.GetParticles(parcels);Require(count==168,"persistent native foam identities "+time);
                for(int i=0;i<count;i++)
                {
                    Vector3 world=ps.transform.TransformPoint(parcels[i].position);float distance=Vector3.Distance(world,driver.CoreCenter);
                    if(Vector3.Distance(world,driver.FoamParcelPosition(i))>0.0001f)throw new Exception("Native parcel mismatch "+i);
                    if(distance>previousRadii[i]+H*0.0001f)throw new Exception("3D contraction reversed seed "+i);
                    previousRadii[i]=distance;
                    if(time>=1.28f&&distance>driver.CoreRadius+H*0.0001f)throw new Exception("Seed outside core "+i);
                    csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F5},{1},{2:F5},{3:F5},{4:F5},{5:F5},{6:F5},{7:F5}",time,i,world.x,world.y,world.z,distance,driver.CollapseProgress,driver.Charge));
                }
                if(time>=1.32f)InspectCoreSupport(root,driver,footprint,support);
            }
            Require(!driver.BurstTriggered&&driver.Charge>=0.75f,"holds ONLY the lit core until manager callback");
            Camera camera=CreateCamera(true);var stage=CreateStage(true);var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            File.WriteAllBytes(Path.Combine(PreviewDirectory(),"battlefield_core_hold_G2.png"),Render(camera,rt,pixels));
            var cloudHold=new StringBuilder("sample,pixels,min_frame,max_frame\n");VerifyCloudFrames(root,camera,rt,pixels,"G2.0",cloudHold);File.WriteAllText(Path.Combine(PreviewDirectory(),"native-cloud-hold-frames.csv"),cloudHold.ToString());
            camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(stage);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
            driver.TriggerBurst();InspectCoreSupport(root,driver,footprint,support);
            Require(driver.Propagation==0,"B0 starts inside core with zero propagation");
        }
        Object.DestroyImmediate(root);
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-foam-transport.csv"),csv.ToString());
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-core-support.csv"),support.ToString());
        var wave=new StringBuilder("burst_age,w,water_x,water_y,water_z,water_radius,adjacent_step\n");Vector3 previousPoint=SlazeyaStormVisualController.RingPoint(footprint,2.25f,0.72f,0);
        float previousRadius=0;
        for(int i=0;i<=56;i++)
        {
            float b=i/240f;Vector3 point=SlazeyaStormVisualController.RingPoint(footprint,2.25f,0.72f,b);
            float radius=Vector3.Distance(point,footprint.EnvelopeCenter),step=Vector3.Distance(point,previousPoint);
            if(step>H*0.15f||radius+0.0001f<previousRadius)throw new Exception("Wavefront discontinuity "+b);
            wave.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2:F6},{3:F6},{4:F6},{5:F6},{6:F6}",b,SlazeyaStormVisualController.WaveProgress(b),point.x,point.y,point.z,radius,step));
            previousRadius=radius;previousPoint=point;
        }
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-wavefront.csv"),wave.ToString());
        Require(true,"R5 3D core seeds, all rendered support, B0 and continuous wavefront");
    }
    private static byte[] Render(Camera camera,RenderTexture rt,Texture2D pixels)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();RenderTexture old=RenderTexture.active;
        camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=old;
        timer.Stop();RenderReadbackMs.Add(timer.Elapsed.TotalMilliseconds);timer.Restart();byte[] png=pixels.EncodeToPNG();timer.Stop();PngEncodeMs.Add(timer.Elapsed.TotalMilliseconds);return png;
    }
    [Serializable] private class CaptureTiming
    {
        public string unityVersion,graphicsDevice,graphicsApi;
        public string metric="CPU wall-clock Camera.Render + synchronous ReadPixels/Apply; includes GPU wait. Not isolated GPU time or game FPS.";
        public int width=1280,height=720,renderedViews,outerStepsPerReferenceH=48,maxViewSteps=1024,lightSamples=48,maxLightSegments=132,coreStepsPerRadius=8,lightUpdateEvery=1;
        public string stepPolicy="Eye steps start at H/48 and refine by the local cylindrical inverse-map gradient; .06H core interval uses at most .03H/8 step. The 1024-step budget reserves the core and covers all outer distance with exact boundary/last segments. Light uses 48 outer samples plus core refinement (up to 32 inner segments and 2 split segments; diagnostic 96 outer, loop cap 132). No global scale-based extinction or texture LOD.";
        public double totalRenderReadbackMs,meanRenderReadbackMs,p95RenderReadbackMs,maxRenderReadbackMs,totalPngEncodeMs;
    }
    private static void WriteCaptureTiming(string output)
    {
        var sorted=RenderReadbackMs.ToArray();Array.Sort(sorted);double total=0,encoding=0;foreach(double n in sorted)total+=n;foreach(double n in PngEncodeMs)encoding+=n;
        var timing=new CaptureTiming{unityVersion=Application.unityVersion,graphicsDevice=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),renderedViews=sorted.Length,
            totalRenderReadbackMs=total,meanRenderReadbackMs=total/Math.Max(1,sorted.Length),p95RenderReadbackMs=sorted.Length>0?sorted[Math.Min(sorted.Length-1,(int)(sorted.Length*0.95))]:0,maxRenderReadbackMs=sorted.Length>0?sorted[sorted.Length-1]:0,totalPngEncodeMs=encoding};
        File.WriteAllText(Path.Combine(output,"native-capture-timing.json"),JsonUtility.ToJson(timing,true));
    }
    private static void CaptureSet(GameObject prefab,string output,string label,bool stage)
    {
        bool streakFramesOnly=Argument("-streakFramesOnly")=="true";
        string sequence=Path.Combine(output,label+"_sequence"),peak=Path.Combine(output,label+"_peak60"),cloudMotion=Path.Combine(output,label+"_cloud60");
        Directory.CreateDirectory(sequence);Directory.CreateDirectory(peak);Directory.CreateDirectory(cloudMotion);
        string comparison=Path.Combine(output,"fixed-comparison"),shake=Path.Combine(output,"camera-approximation_sequence");
        Directory.CreateDirectory(comparison);
        bool shakeIllustration=stage&&Argument("-cameraApproximation")=="true";
        if(shakeIllustration)Directory.CreateDirectory(shake);
        var comparisonTrace=new StringBuilder("file,requested_burst_age,sampled_burst_age,driver_burst_age,alive_particles,sheets,drops,streaks\n");
        var shakeTrace=new StringBuilder("frame,scaled_time,burst_age,pixel_offset_x,pixel_offset_y\n");
        GameObject scenery=CreateStage(stage);Camera camera=CreateCamera(stage);
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        GameObject root=Object.Instantiate(prefab);var trace=new StringBuilder("time,burst_age,driver_burst_age,phase,foam_gather,radius_ratio,alive_particles,lightning_energy,ready,burst,complete\n");
        int nextSample=0,frame=0,maximumAlive=0;
        using(var controller=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
        {
            for(int step=0;step<=180;step++)
            {
                float time=step/60f;
                if(step>0)controller.Advance(1f/60f);
                if(step==CallbackStep){ActualCallbackTime=time;controller.TriggerBurst();}
                float burstAge=controller.BurstTriggered?time-ActualCallbackTime:-1f;
                maximumAlive=Mathf.Max(maximumAlive,controller.AliveParticles);
                trace.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7:F4},{8},{9},{10}",time,burstAge,controller.BurstAge,controller.Phase,controller.FoamGather,controller.RadiusRatio,controller.AliveParticles,controller.LightningEnergy,controller.GatherReady,controller.BurstTriggered,controller.IsComplete));
                if(burstAge>0.155f||(burstAge>0.055f&&burstAge<0.095f))
                    if(controller.LightningEnergy>0.001f)throw new Exception("Lightning escaped finite pulse windows");
                bool sample=!streakFramesOnly&&nextSample<SampleNames.Length&&(nextSample<GatherSamples.Length?time+0.0001f>=GatherSamples[nextSample]:burstAge+0.0001f>=BurstSamples[nextSample-GatherSamples.Length]);
                bool peakFrame=step>=CallbackStep-1&&step<=CallbackStep+14;
                int relativeStep=step-CallbackStep;
                bool compareFrame=relativeStep==3||relativeStep==7||relativeStep==15||relativeStep==21||relativeStep==27||relativeStep==48;
                if(compareFrame||(!streakFramesOnly&&(step%2==0||sample||peakFrame||(step>=32&&step<=34))))
                {
                    byte[] png=Render(camera,rt,pixels);
                    if(step==CallbackStep)VerifyCorePixels(root,camera,rt,pixels,controller,label);
                    if(!streakFramesOnly&&step%2==0)
                    {
                        string frameName="frame_"+frame.ToString("D4")+".png";
                        File.WriteAllBytes(Path.Combine(sequence,frameName),png);
                        if(shakeIllustration)
                        {
                            float dx=0,dy=0;byte[] offsetPng=png;Vector3 fixedPosition=camera.transform.position;
                            if(burstAge>=0&&burstAge<0.25f)
                            {
                                float envelope=1-burstAge/0.25f;dx=20*envelope*Mathf.Cos(65*burstAge);dy=10*envelope*Mathf.Sin(65*burstAge);
                                try {camera.transform.position=fixedPosition-(camera.transform.right*dx+camera.transform.up*dy)*(camera.orthographicSize*2/720);offsetPng=Render(camera,rt,pixels);}
                                finally {camera.transform.position=fixedPosition;}
                            }
                            File.WriteAllBytes(Path.Combine(shake,frameName),offsetPng);
                            shakeTrace.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4:F6}",frame,time,burstAge,dx,dy));
                        }
                        frame++;
                    }
                    if(compareFrame)
                    {
                        string name=label+"_B"+relativeStep.ToString("D2")+".png";
                        File.WriteAllBytes(Path.Combine(comparison,name),png);
                        float requested=relativeStep==7?0.12f:relativeStep/60f;
                        int sheets=root.transform.Find("AB_ParticleRoot/BurstSheets").GetComponent<ParticleSystem>().particleCount;
                        int drops=root.transform.Find("AB_ParticleRoot/BurstDroplets").GetComponent<ParticleSystem>().particleCount;
                        int streaks=root.transform.Find("AB_ParticleRoot/WaterfallStreaks").GetComponent<ParticleSystem>().particleCount;
                        comparisonTrace.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4},{5},{6},{7}",name,requested,relativeStep/60f,controller.BurstAge,controller.AliveParticles,sheets,drops,streaks));
                        if(stage&&(relativeStep==15||relativeStep==21||relativeStep==27))InspectStreakMesh(root,camera,output,relativeStep);
                    }
                    if(step>=32&&step<=34)File.WriteAllBytes(Path.Combine(cloudMotion,"frame_"+(step-32).ToString("D3")+".png"),png);
                    if(peakFrame)File.WriteAllBytes(Path.Combine(peak,"frame_"+(step-CallbackStep+1).ToString("D3")+".png"),png);
                    if(sample)
                    {
                        string name=label+"_"+SampleNames[nextSample];File.WriteAllBytes(Path.Combine(output,name+".png"),png);
                        if(label=="black"||Argument("-baselineBundle")!=null)ActualSampleTimes.Add(time);
                        Debug.Log("Native R5 "+name+" t="+time+" B="+burstAge+" particles="+controller.AliveParticles);
                        if(SampleNames[nextSample]=="01_cloud_ring"||SampleNames[nextSample]=="02_cloud_swirl"||SampleNames[nextSample]=="03b_core_peak"||SampleNames[nextSample]=="06_waterfall_B14")
                        {
                            var particleRenderers=root.GetComponentsInChildren<ParticleSystemRenderer>();
                            foreach(var r in particleRenderers)r.enabled=false;
                            File.WriteAllBytes(Path.Combine(output,name+"_main_shape.png"),Render(camera,rt,pixels));
                            foreach(var r in particleRenderers)r.enabled=true;
                        }
                        nextSample++;
                    }
                }
            }
            Require(streakFramesOnly||nextSample==SampleNames.Length,"all actual callback-relative representative frames "+label);
            Require(controller.IsComplete&&maximumAlive>0,"finite native sequence "+label+" maximumAlive="+maximumAlive);
        }
        File.WriteAllText(Path.Combine(output,label+"_timeline.csv"),trace.ToString());
        File.WriteAllText(Path.Combine(comparison,label+"-samples.csv"),comparisonTrace.ToString());
        if(shakeIllustration)
        {
            File.WriteAllText(Path.Combine(output,"camera-approximation-offsets.csv"),shakeTrace.ToString());
            File.WriteAllText(Path.Combine(output,"camera-approximation.txt"),"camera approximation; not native game EarthQuake shader\nSeparate native preview-camera translation only; fixed A/B frames retain the original pose. Duration .25 scaled seconds from B; 1280x720; dx=20*(1-B/.25)*cos(65*B), dy=10*(1-B/.25)*sin(65*B) pixels; zero outside [0,.25). World offset = -(camera.right*dx + camera.up*dy)*(2*13.8/720). Pose restored immediately after each render; simulation is never advanced for this pass. Game API parameters and actual EarthQuake output are NOT inferred from this illustration.\n");
        }
        Object.DestroyImmediate(root);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(scenery);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
    }
    private static void InspectStreakMesh(GameObject root,Camera camera,string output,int step)
    {
        var ps=root.transform.Find("AB_ParticleRoot/WaterfallStreaks").GetComponent<ParticleSystem>();
        var renderer=ps.GetComponent<ParticleSystemRenderer>();var mesh=new Mesh();renderer.BakeMesh(mesh,camera,false);
        Vector3[] vertices=mesh.vertices;var uv=new List<Vector4>();mesh.GetUVs(0,uv);
        Require(vertices.Length==ps.particleCount*4&&uv.Count==vertices.Length,"native streak quad and packed UV/age stream");
        var data=new StringBuilder("quad,length_world,width_world,age_percent,stable_random,atlas_frame\n");
        for(int i=0;i<vertices.Length;i+=4)
            data.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:G9},{2:G9},{3:G9},{4:G9},{5:G9}",i/4,(vertices[i+1]-vertices[i]).magnitude,(vertices[i+2]-vertices[i+1]).magnitude,uv[i].z,uv[i].w,Mathf.Clamp01(uv[i].z)*15));
        File.WriteAllText(Path.Combine(output,"native-streak-mesh-B"+step.ToString("D2")+".csv"),data.ToString());
        var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);int longFlows=0;
        data=new StringBuilder("seed,size_x_width,size_y_length,start_lifetime,remaining_lifetime,long_flow\n");
        for(int i=0;i<count;i++)
        {
            var p=particles[i];Vector3 size=p.GetCurrentSize3D(ps);bool longFlow=size.y>size.x*0.5f;if(longFlow)longFlows++;
            data.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:G9},{2:G9},{3:G9},{4:G9},{5}",p.randomSeed,size.x,size.y,p.startLifetime,p.remainingLifetime,longFlow));
        }
        if(step==15)Require(count==112&&longFlows==32,"native birth population 32 long / 80 short streaks");
        Require(renderer.sharedMaterial.GetVector("_Grid")==new Vector4(4,4,1,0),"streaks retain original 16-frame animated foam atlas");
        File.WriteAllText(Path.Combine(output,"native-streak-particles-B"+step.ToString("D2")+".csv"),data.ToString());Object.DestroyImmediate(mesh);
    }
    private static void VerifyCorePixels(GameObject root,Camera camera,RenderTexture rt,Texture2D pixels,SlazeyaStormVisualController controller,string label)
    {
        Color32[] active=pixels.GetPixels32();var renderers=root.GetComponentsInChildren<Renderer>();var enabled=new bool[renderers.Length];
        for(int i=0;i<renderers.Length;i++){enabled[i]=renderers[i].enabled;renderers[i].enabled=false;}
        Render(camera,rt,pixels);Color32[] baseline=pixels.GetPixels32();
        for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
        Vector3 screen=camera.WorldToViewportPoint(controller.CoreCenter);float x=screen.x*1280,y=screen.y*720;
        float radius=controller.CoreVisibleLimit/(camera.orthographicSize*2)*720+3;
        int outside=0,maxDifference=0;
        for(int i=0;i<active.Length;i++)
        {
            float dx=i%1280-x,dy=i/1280-y;if(dx*dx+dy*dy<=radius*radius)continue;
            int difference=Mathf.Max(Mathf.Abs(active[i].r-baseline[i].r),Mathf.Abs(active[i].g-baseline[i].g),Mathf.Abs(active[i].b-baseline[i].b));
            maxDifference=Mathf.Max(maxDifference,difference);if(difference>3)outside++;
        }
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-core-pixels-"+label+".csv"),"burst_age,core_x,core_y,allowed_pixel_radius,outside_pixels,max_difference\n0,"+x+","+y+","+radius+","+outside+","+maxDifference);
        Require(outside==0,"B0 native raster support stays at the core "+label+" outside="+outside+" max="+maxDifference);
    }
    private static void CaptureAtlasDiagnostics(GameObject prefab,string output)
    {
        var evidence=new StringBuilder("atlas,sample,burst_age,native_min_age,native_max_age,rendered_median_age,pixels\n");
        for(int atlas=0;atlas<2;atlas++)
        {
            var root=Object.Instantiate(prefab);
            Camera camera=CreateCamera(false);
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            string name=atlas==0?"foam":"mist";
            ParticleSystem target=root.transform.Find("AB_ParticleRoot/"+(atlas==0?"BurstSheets":"ReturnMist")).GetComponent<ParticleSystem>();
            var renderer=target.GetComponent<ParticleSystemRenderer>();
            int[] steps=atlas==0?new[]{4,12,24}:new[]{24,40,61};
            var particleData=new ParticleSystem.Particle[target.main.maxParticles];
            using(var controller=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
            {
                controller.Advance(1.55f);controller.TriggerBurst();int sample=0;
                for(int step=0;step<=steps[2];step++)
                {
                    if(step>0)controller.Advance(1f/60f);
                    if(step!=steps[sample])continue;
                    foreach(Renderer r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
                    renderer.enabled=true;
                    File.WriteAllBytes(Path.Combine(output,"atlas_"+name+"_"+sample+".png"),Render(camera,rt,pixels));
                    int count=target.GetParticles(particleData);Require(count>0,"native "+name+" sample particles");
                    float min=1,max=0;
                    for(int i=0;i<count;i++){float age=1-particleData[i].remainingLifetime/particleData[i].startLifetime;min=Mathf.Min(min,age);max=Mathf.Max(max,age);}
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_DebugAge",1);renderer.SetPropertyBlock(block);
                    Render(camera,rt,pixels);
                    var observed=new List<byte>();
                    foreach(Color32 pixel in pixels.GetPixels32())if(pixel.b==0&&pixel.g>3)observed.Add(pixel.r);
                    Require(observed.Count>16,"native age probe visible "+name);
                    observed.Sort();float median=observed[observed.Count/2]/255f;
                    Require(median>=min-0.04f&&median<=max+0.04f,"native TEXCOORD UV.xy AgePercent.z StableRandomX.w "+name+" sample="+sample+" age="+median+" expected="+min+".."+max);
                    evidence.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6}",name,sample,step/60f,min,max,median,observed.Count));
                    block.SetFloat("_DebugAge",0);renderer.SetPropertyBlock(block);
                    sample++;if(sample==3)break;
                }
            }
            Object.DestroyImmediate(root);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
        File.WriteAllText(Path.Combine(output,"native-atlas-age-verification.csv"),evidence.ToString());
    }

    [Serializable] private class Manifest
    {
        public string version=SlazeyaStormVisualController.Version;
        public string unityVersion,graphicsDevice,colorSpace,bundle,bundleSha256,controllerSha256;
        public string source="AssetBundle.LoadFromFile -> script-free prefab -> canonical controller -> native shader and particles";
        public int width=1280,height=720,sequenceFps=30,simulationHz=60;
        public float referenceBodyHeight=H,callbackTime,orthographicSize=13.8f;
        public float coreRadius=H*0.06f,coreVisibleLimit=H*0.12f;
        public string cloudNoise="Frozen linear Shape64 and independent Erosion32 with explicit LOD. Shape64 G/B/A are Worley W4/W8/W16; 2H period gives .5/.25/.125H cells. bodyField=dot(GBA,(.25,.50,.25)); bodyStrength=max(0,(bodyField-.38)*5), multiplied by signed-distance occupancy with a protected empty hole. Independent detail subtracts .035H*(1-detail) before the .035H density ramp; no global density floor or inverse-cubed erosion.";
        public string cloud="One containing box integrates the frozen cloud source through an analytic XZ sink inverse. Source height is preserved outside r=.35H and compresses locally inside; matching forward transport drives 168 persistent parcels. Gather inflow is G.65 to G1.28; source XZ quantiles are cached once for monotone C1 time anchors .65/.94/1.02/1.12/1.23/1.28. Local Jacobian compensation is capped at 8; texture LOD uses the full inverse gradient. Eye H/48 ceiling with adaptive refinement and 1024 full-path budget; light 48 outer plus fine core segments (loop cap132). SigmaT=4.5, source noise/roll unchanged; .03H spherical accumulation, .032H terminal box, unchanged callback waterfall and SFX.";
        public string propagation="smoothstep(B/0.23); B0,+1,+2,+4,+7,+11,+14 native 60Hz samples";
        public float gatherDuration=SlazeyaStormVisualController.GatherDuration,tailDuration=SlazeyaStormVisualController.TailDuration;
        public Vector3 cameraPosition=new Vector3(2,27,-56),cameraLookAt=new Vector3(2,2,1),casterFoot=Caster;
        public Vector3[] recipientFeet=Feet;
        public string[] representativeFiles=SampleNames;
        public float[] representativeTimes;
        public string[] textureFiles=TextureFiles,textureSha256;
        public SlazeyaStormVisualController.Footprint footprint;
        public string normals="Finite parametric ellipsoid/rupture derivatives; finite pole fan normal; closed body coverage independent of texture A gutter. Fixed effect-owned illumination.";
        public string atlas="PNG top-left row-major, bilinear tile clamp plus authored 12px gutters, real AgePercent.z, dual-frame interpolation, stable random droplet variant.";
        public string lightning="B[0,.05) primary; B[.05,.10) dark; B[.10,.15) secondary 60%; then zero. One branched mesh per pulse, same-surface core/edge; bounded local water illumination.";
        public string mist="Authored A coverage only, R lighting, no particle color-lifetime double fade; lift clusters B.04/.11; return clusters B.36/.50, finite explicit Simulate.";
        public string limitations="Native Unity visual proof only. Independent art and game PlayMode acceptance belong to main thread.";
    }
    private static string Digest(string path)
    {
        using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
    }
    private static void WriteManifest(string output,string bundle)
    {
        var manifest=new Manifest{unityVersion=Application.unityVersion,graphicsDevice=SystemInfo.graphicsDeviceName,colorSpace=QualitySettings.activeColorSpace.ToString(),
            bundle=bundle,bundleSha256=Digest(bundle),controllerSha256=Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs")),callbackTime=ActualCallbackTime,
            representativeTimes=ActualSampleTimes.ToArray(),textureSha256=TextureHashes.ToArray(),footprint=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))};
        if(Argument("-streakFramesOnly")=="true")
        {
            manifest.representativeFiles=new string[0];manifest.representativeTimes=new float[0];
            manifest.source+="; sparse fixed-comparison frames only, see battlefield-samples.csv; full sequence not exported";
        }
        File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));
    }
    private static string PreviewDirectory()
    {
        if(!string.IsNullOrEmpty(PreviewOutputOverride))return Path.GetFullPath(PreviewOutputOverride);
        var dir=new DirectoryInfo(Application.dataPath);
        while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"AGENTS.md")))dir=dir.Parent;
        if(dir==null)throw new Exception("Cannot locate repository preview directory");
        return Path.Combine(dir.FullName,"preview_exports/slazeya_storm_mass/round5");
    }

    private static Camera CreateCamera(bool stage)
    {
        Camera camera = new GameObject("NativeStormPreviewCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = stage ? new Color(0.095f,0.084f,0.077f) : Color.black;
        camera.orthographic = true; camera.orthographicSize = 13.8f;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 160;
        camera.transform.position = new Vector3(2f,27f,-56f);
        camera.transform.LookAt(new Vector3(2f,2f,1f));
        camera.allowHDR = false;
        return camera;
    }

    private static Material StageMaterial(Color color)
    {
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = color; material.hideFlags = HideFlags.DontSave;
        return material;
    }

    private static GameObject CreateStage(bool stage)
    {
        var root = new GameObject("ScaleReferences");
        if (stage)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform,false);
            floor.transform.localScale = new Vector3(8,1,6);
            floor.GetComponent<Renderer>().sharedMaterial = StageMaterial(new Color(0.19f,0.17f,0.15f));
            var gridMaterial = StageMaterial(new Color(0.23f,0.21f,0.185f));
            for (int i = -4; i <= 4; i++)
            {
                Cube(root.transform,new Vector3(i*6f,0.009f,0),new Vector3(0.025f,0.01f,40),gridMaterial);
                Cube(root.transform,new Vector3(0,0.01f,i*5f),new Vector3(65,0.01f,0.025f),gridMaterial);
            }
        }
        var victimMaterial = StageMaterial(stage ? new Color(0.43f,0.39f,0.32f) : new Color(0.21f,0.23f,0.24f));
        foreach (var foot in Feet) Character(root.transform,foot,victimMaterial);
        Character(root.transform,Caster,StageMaterial(new Color(0.22f,0.37f,0.43f)));
        return root;
    }

    private static void Cube(Transform root, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(root,false); cube.transform.localPosition = position; cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void Character(Transform parent, Vector3 foot, Material material)
    {
        var root = Child(parent,"ReferenceCharacter_H6"); root.localPosition = foot;
        Cube(root,new Vector3(-0.35f,H*0.22f,0),new Vector3(0.42f,H*0.44f,0.55f),material);
        Cube(root,new Vector3(0.35f,H*0.22f,0),new Vector3(0.42f,H*0.44f,0.55f),material);
        Cube(root,new Vector3(0,H*0.62f,0),new Vector3(1.45f,H*0.36f,0.7f),material);
        Cube(root,new Vector3(-0.94f,H*0.58f,0),new Vector3(0.30f,H*0.34f,0.4f),material);
        Cube(root,new Vector3(0.94f,H*0.58f,0),new Vector3(0.30f,H*0.34f,0.4f),material);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root,false); head.transform.localPosition = new Vector3(0,H*0.90f,0);
        head.transform.localScale = Vector3.one * H*0.20f;
        head.GetComponent<Renderer>().sharedMaterial = material;
    }


}
