using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Native OnRenderImage proxy preview using the exact game driver/filter/shader sources.</summary>
public static class VeliaTideMistBundleBuilder
{
    private const int Width = 1280, Height = 720;
    private const int FrameRate = 60, FrameCount = 205, FirstCallbackFrame = 36, SecondBeginFrame = 99, SecondCallbackFrame = 117, CompleteFrame = 200;
    private const string MaterialPath = "Assets/Materials/VeliaTideMistMaterial.mat";
    private const string MistSource = "SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/mist_density_lighting_4x4.png";
    private const string CloudSource = "SteriaBuild/VFXSource/VeliaTideMist/source_assets/reference_refinement/dawn_cloud_frame_v1.png";
    private const string CloudPath = "Assets/Textures/dawn_cloud_frame_v1.png";
    private static readonly List<Object> Owned = new List<Object>();
    private static readonly List<Rect> Protection = new List<Rect>();
    private static readonly List<Vector2> Hits = new List<Vector2>();

    [MenuItem("Steria/Velia Tide Mist/First native preview")]
    public static void PreviewFirst()
    {
        try
        {
            Require(Application.unityVersion == "2019.3.15f1", "Exact Unity 2019.3.15f1 required");
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "Native D3D11 is required (no -nographics)");
            Material material = PrepareMaterial();
            VerifyDriver();
            Export(material, Revision("first"));
            Debug.Log("VELIA_FIRST_NATIVE_PREVIEW_PASS");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    // Only run after main-thread review. Never copies artifacts into mod/game folders.
    [MenuItem("Steria/Velia Tide Mist/Build reviewed bundle and export")]
    public static void BuildBundle()
    {
        try
        {
            Require(Application.unityVersion == "2019.3.15f1", "Exact Unity version");
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "Native D3D11 is required");
            PrepareMaterial();
            string output = Path.GetFullPath("AssetBundles"); Directory.CreateDirectory(output);
            var builds = new[] { new AssetBundleBuild { assetBundleName = VeliaTideMistVisualController.BundleName, assetNames = new[] { MaterialPath } } };
            var manifest = BuildPipeline.BuildAssetBundles(output, builds, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Require(manifest != null, "Bundle build manifest");
            string path = Path.Combine(output, VeliaTideMistVisualController.BundleName);
            var bundle = AssetBundle.LoadFromFile(path);
            Require(bundle != null, "Native bundle load");
            try
            {
                Require(bundle.GetAllAssetNames().Length == 1 && bundle.GetAllAssetNames()[0] == MaterialPath.ToLowerInvariant(), "Exactly one explicit material root, no prefab or MonoBehaviour");
                var material = bundle.LoadAsset<Material>(VeliaTideMistVisualController.MaterialName);
                Require(material != null && material.shader.isSupported && material.GetTexture("_MistAtlas") != null && material.GetTexture("_CloudPlate") != null, "Material/shader/atlas/cloud readback");
                var cloudReceipt = VerifyCloudTexture((Texture2D)material.GetTexture("_CloudPlate"));
                VerifyDriver();
                Export(material, Revision("reviewed"), true);
                File.WriteAllText(Path.Combine(PreviewRoot(), Revision("reviewed"), "bundle-sha256.txt"), Digest(path) + "  " + path + "\n");
                var texture=(Texture2D)material.GetTexture("_MistAtlas");
                Require(texture.width==1024&&texture.height==1024,"Native atlas dimensions");
                Require(manifest.GetAllDependencies(VeliaTideMistVisualController.BundleName).Length==0,"Self-contained material Bundle");
                var receipt=new BundleReadbackReceipt {
                    bundlePath=path,bundleSha256=Digest(path),explicitAssetNames=bundle.GetAllAssetNames(),
                    materialName=material.name,shaderName=material.shader.name,shaderSupported=material.shader.isSupported,
                    atlasWidth=texture.width,atlasHeight=texture.height,atlasFormat=texture.format.ToString(),
                    atlasFilter=texture.filterMode.ToString(),atlasWrap=texture.wrapMode.ToString(),
                    sourceAtlasSha256=Digest(Path.Combine(Repo(),MistSource)), cloudPlate=cloudReceipt,
                    revision=Revision("reviewed")
                };
                File.WriteAllText(Path.Combine(PreviewRoot(),Revision("reviewed"),"bundle-readback.json"),JsonUtility.ToJson(receipt,true));
            }
            finally { bundle.Unload(true); }
            Debug.Log("VELIA_BUNDLE_NATIVE_EXPORT_PASS");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    private static Material PrepareMaterial()
    {
        Directory.CreateDirectory("Assets/Materials");
        Texture2D mist = ImportTexture(MistSource, "Assets/Textures/mist_density_lighting_4x4.png", true);
        Require(Digest(Path.Combine(Repo(), MistSource)) == "48F9EDADD9AC23C63B813B53576207227740ED528F94694F8FF256CA3399E029", "Read-only source atlas SHA");
        Texture2D cloud = ImportTexture(CloudSource, CloudPath, false);
        VerifyCloudTexture(cloud);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/VeliaTideMist.shader");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader) && shader.isSupported, "Native shader compilation");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
        material.name = VeliaTideMistVisualController.MaterialName;
        material.shader = shader;
        material.SetTexture("_MistAtlas", mist);
        material.SetTexture("_CloudPlate", cloud);
        material.SetVector("_State", Vector4.zero);
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        var allowed = new HashSet<string> { MaterialPath, "Assets/Shaders/VeliaTideMist.shader", "Assets/Textures/mist_density_lighting_4x4.png", CloudPath };
        string[] dependencies = AssetDatabase.GetDependencies(MaterialPath, true);
        Require(dependencies.Length == allowed.Count, "Material must contain exactly shader, data atlas and cloud color dependencies");
        foreach (string dependency in dependencies) Require(allowed.Contains(dependency), "Unexpected shipping dependency: " + dependency);
        return material;
    }

    private static Texture2D ImportTexture(string source, string destination, bool data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        string original = Path.Combine(Repo(), source);
        File.Copy(original, destination, true);
        Require(Digest(original) == Digest(destination), "Byte-identical import: " + source);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = !data;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = !data;
        importer.mipmapEnabled = false;
        importer.isReadable = !data;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        // Preserve the actual generated art ratio. Leave the old atlas/sprite import unchanged.
        if (source == CloudSource) importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
    }

    private static CloudReadbackReceipt VerifyCloudTexture(Texture2D texture)
    {
        var reference = JsonUtility.FromJson<SourceReferences>(File.ReadAllText(Path.Combine(Repo(), "SteriaBuild/VFXSource/VeliaTideMist/source_assets/references.json"))).cloudPlate;
        Require(reference != null && reference.path == CloudSource, "Fixed cloud source contract");
        string path = Path.Combine(Repo(), CloudSource);
        Require(Digest(path) == reference.sha256 && reference.sRGB && reference.alpha == "straight", "Cloud source hash / color semantics");
        byte[] png = File.ReadAllBytes(path);
        Require(png.Length > 33 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71 && png[24] == 8 && png[25] == 6, "True 8-bit RGBA PNG source");
        Require(texture != null && texture.width == reference.width && texture.height == reference.height, "Native cloud dimensions / no NPOT scaling");
        Require(texture.width > texture.height && texture.width <= 2048 && texture.height <= 2048, "Landscape art within import limit");
        Require(texture.format == TextureFormat.RGBA32 && texture.mipmapCount == 1 && texture.filterMode == FilterMode.Bilinear && texture.wrapMode == TextureWrapMode.Clamp, "Native cloud RGBA32 / no mips / Bilinear / Clamp");
        var importer = (TextureImporter)AssetImporter.GetAtPath(CloudPath);
        Require(importer.sRGBTexture && importer.alphaSource == TextureImporterAlphaSource.FromInput && importer.textureCompression == TextureImporterCompression.Uncompressed, "Color importer settings");
        Color32[] pixels = texture.GetPixels32();
        var raw = new Texture2D(2,2,TextureFormat.RGBA32,false);
        int minimum=255,maximum=0,opaque=0,soft=0,corridor=0,corridorClear=0,lower=0,lowerClear=0;
        try
        {
            Require(raw.LoadImage(png) && raw.width == texture.width && raw.height == texture.height, "Decode original cloud PNG");
            Color32[] original = raw.GetPixels32();
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
            {
                int p=y*texture.width+x, a=pixels[p].a;
                if(a != original[p].a) Require(false, "Native cloud alpha must match source at pixel " + p);
                minimum=Math.Min(minimum,a); maximum=Math.Max(maximum,a);
                if(a>=230)opaque++; if(a>0&&a<230)soft++;
                if(x>=texture.width*.54f&&x<texture.width*.56f){corridor++;if(a<=5)corridorClear++;}
                if(y<texture.height*.35f&&x>=texture.width*.15f&&x<texture.width*.85f){lower++;if(a<=5)lowerClear++;}
            }
        }
        finally {Object.DestroyImmediate(raw);}
        Require(minimum==0&&maximum>=250&&opaque/(float)pixels.Length>=.05f&&soft/(float)pixels.Length>.001f, "Cloud clear pixels, substantial cores and soft alpha edges");
        Require(corridorClear/(float)corridor>=.90f&&lowerClear/(float)lower>=.90f, "Cloud transparent central passage and lower battlefield");
        return new CloudReadbackReceipt {sourcePath=CloudSource,sourceSha256=Digest(path),width=texture.width,height=texture.height,
            format=texture.format.ToString(),filter=texture.filterMode.ToString(),wrap=texture.wrapMode.ToString(),sRGB=importer.sRGBTexture,
            alphaMin=minimum,alphaMax=maximum,opaqueFraction=opaque/(float)pixels.Length,softFraction=soft/(float)pixels.Length,
            centralClearFraction=corridorClear/(float)corridor,lowerClearFraction=lowerClear/(float)lower,alphaMatchesSource=true};
    }

    private static void VerifyDriver()
    {
        VerifySustainedPulseSamples();
        var d = new VeliaTideMistVisualController(); d.BeginDice(0);
        d.Advance(0.16f); float paused = d.Envelope;
        d.Advance(0f); d.Advance(float.NaN); d.Advance(-1f);
        Require(d.Envelope == paused && !d.DiceReady, "Pause/invalid delta do not advance");
        d.Advance(0.16f); Require(d.DiceReady, "0.32 second intro gate");
        d.TriggerPulse(new[] { new Vector2(0.3f,0.4f), new Vector2(0.3f,0.4f) }); d.TriggerPulse(null); d.BeginDice(0);
        Require(d.PulseCount == 1 && d.SuccessfulPointCount == 1, "Same dice/callback/target idempotency");
        d.Advance(0.05f); Require(d.Pulse > 0.99f, "First fast peak");
        d.Advance(0.68f); Require(d.PulseFinished && !d.IsComplete, "First .72-second tail holds same session");
        d.Advance(2f); Require(d.Pulse == 0 && d.Envelope == 1, "No idle re-flash");
        d.BeginDice(1); Require(d.DiceReady && !d.PulseFinished, "Second dice skips intro, resets gate");
        d.TriggerPulse(new Vector2[0]); d.Advance(0.05f);
        Require(d.PulseCount == 2 && d.Pulse > 0.99f && d.SuccessfulPointCount == 0, "Empty callback pulses without false hit");
        d.Advance(0.73f); Require(d.PulseFinished, "Second .77-second tail"); d.Finish(); d.Advance(0.41f);
        Require(d.IsComplete && d.Envelope == 0, "Single final fade clears");
        d = new VeliaTideMistVisualController(); d.BeginDice(0); d.Advance(0.1f); d.Cancel(); d.Advance(1f);
        Require(d.IsComplete && d.Envelope == 0 && d.PulseCount == 0, "Interrupted intro returns to zero");
        Debug.Log("VELIA_NATIVE_DRIVER_CHECKS_PASS");
    }

    private static void VerifySustainedPulseSamples()
    {
        // Fixed observable values from accepted r7, followed by the approved hold/tail.
        // These checks precede the controller change and must reject the old short pulse.
        float[] ages={.02f,.04f,.07f,.10f,.14f,.18f,.30f,.48f,.60f,.70f};
        float[][] expected={
            new[]{.5f,1f,1f,.93925f,.71825f,.42525f,.42525f,.42525f,.12909375f,.00411328125f},
            new[]{.5f,1f,1f,.960256f,.808704f,.589568f,.589568f,.589568f,.2525418f,.049875117f}
        };
        var material=new Material(Shader.Find("Steria/VeliaTideMist"));
        try
        {
            for(int die=0;die<2;die++)
            {
                float end=die==0 ? .72f : .77f;
                for(int i=0;i<ages.Length;i++)
                {
                    var d=new VeliaTideMistVisualController();d.BeginDice(die);d.Advance(.32f);
                    d.TriggerPulse(new[]{new Vector2(.3f,.4f)});d.Advance(ages[i]);
                    Require(Mathf.Abs(d.Pulse-expected[die][i])<.00002f,"Dice "+(die+1)+" pulse at +"+ages[i]+": "+d.Pulse+" expected "+expected[die][i]);
                    Require(!d.PulseFinished,"Hold/tail must retain the current dice gate");
                    d.Apply(material,16f/9f);
                    Require(Mathf.Abs(material.GetFloat("_PulseAge")-ages[i])<.000001f,"Shader sees real callback age, not held evaluation age");
                    if(ages[i]>=.14f)Require(material.GetFloat("_HitStrength")==0,"Local hit ends at .14s during sustained global light");
                    float pulse=d.Pulse,elapsed=d.Elapsed;
                    d.TriggerPulse(null);d.BeginDice(die);d.Advance(0);d.Apply(material,16f/9f);
                    Require(d.Pulse==pulse&&d.Elapsed==elapsed&&d.PulseCount==1,"Duplicate/pause/render apply cannot restart or advance held light");
                    if(ages[i]==.30f)
                    {
                        d.Cancel();d.Advance(1);d.Apply(material,16f/9f);
                        Require(d.IsComplete&&d.Envelope==0&&d.Pulse==0&&material.GetFloat("_HitStrength")==0,"Cancel clears a live hold immediately");
                    }
                }
                var tail=new VeliaTideMistVisualController();tail.BeginDice(die);tail.Advance(.32f);tail.TriggerPulse(new Vector2[0]);
                Require(Mathf.Abs(tail.PulseDuration-end)<.000001f,"Approved total pulse duration");
                tail.Advance(end-.0001f);Require(!tail.PulseFinished,"No premature dice release");
                tail.Advance(.0002f);Require(tail.PulseFinished&&tail.Pulse==0&&!tail.IsComplete&&tail.SuccessfulPointCount==0,"New end releases without false hits or early disposal");
                tail.Finish();tail.Advance(.39f);Require(!tail.IsComplete,"Normal fade still owns its .40s lifetime");
                tail.Advance(.02f);Require(tail.IsComplete&&tail.Envelope==0,"Normal fade completes after .40s");
            }
        }
        finally {Object.DestroyImmediate(material);}
        Debug.Log("VELIA_SUSTAINED_PULSE_SAMPLES_PASS: r7 early samples, .30/.48 hold, late tail, real shader age, local hit, pause, duplicates, cancel, endpoints");
    }

    private static void Export(Material template, string revision, bool bundleReadback=false)
    {
        string output = Path.Combine(PreviewRoot(), revision);
        Require(!Directory.Exists(output), "Use a unique preview revision; preserve previous review evidence: " + output);
        string sequence = Path.Combine(output, "sequence60"); Directory.CreateDirectory(sequence);
        var root = new GameObject("PROXY battlefield - real sprites - NOT actual combat HUD");
        Camera camera = new GameObject("OwnedNativePreviewCamera").AddComponent<Camera>();
        var rt = new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        var pixels = new Texture2D(Width,Height,TextureFormat.RGB24,false);
        camera.orthographic = true; camera.orthographicSize = 5f; camera.aspect = (float)Width/Height;
        camera.transform.position = new Vector3(0,0,-10);
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 40f;
        camera.backgroundColor = new Color(0.07f,0.09f,0.12f); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = rt;
        var driver = new VeliaTideMistVisualController();
        var filter = camera.gameObject.AddComponent<VeliaTideMistScreenFilter>();
        var csv = new StringBuilder("frame,time,envelope,pulse,pulseCount,ready,pulseFinished,complete,hits\n");
        var hashes = new StringBuilder();
        try
        {
            CreateStage(root, camera);
            driver.SetProtectionRects(Protection.ToArray());
            filter.Initialize(driver,template);
            driver.BeginDice(0);
            Color32[] baseline = null;
            int nearWhiteNewMax = 0;
            int completeDelta = 0;
            for (int frame=0;frame<FrameCount;frame++)
            {
                if(frame>0) driver.Advance(1f/FrameRate);
                if(frame==FirstCallbackFrame) driver.TriggerPulse(Hits.ToArray());
                if(frame==SecondBeginFrame) { Require(driver.PulseFinished,"Next dice scheduled after the sustained first tail"); driver.BeginDice(1); }
                if(frame==SecondCallbackFrame) driver.TriggerPulse(Hits.ToArray());
                if(frame>=SecondCallbackFrame && driver.PulseFinished && !driver.IsFinishing) driver.Finish();
                byte[] png=Capture(camera,rt,pixels);
                string path=Path.Combine(sequence,"frame_"+frame.ToString("D4")+".png");
                File.WriteAllBytes(path,png);
                string label=Label(frame);
                if(label!=null) { string named=Path.Combine(output,label+".png"); File.WriteAllBytes(named,png); hashes.AppendLine(Digest(named)+"  "+label+".png"); Debug.Log("VELIA_NATIVE_FRAME "+named); }
                if(frame==0) baseline=pixels.GetPixels32();
                if(frame==CompleteFrame)
                {
                    Color32[] complete=pixels.GetPixels32();
                    for(int p=0;p<complete.Length;p++) completeDelta=Math.Max(completeDelta,PixelDelta(baseline[p],complete[p]));
                    Require(completeDelta<=1,"Completed frame restores source before filter release");
                }
                if(frame==FirstCallbackFrame+3 || frame==SecondCallbackFrame+3)
                {
                    Color32[] peak=pixels.GetPixels32(); int added=0;
                    for(int p=0;p<peak.Length;p++) if(NearWhite(peak[p])&&!NearWhite(baseline[p])) added++;
                    nearWhiteNewMax=Math.Max(nearWhiteNewMax,added);
                    Require((float)added/peak.Length<=0.15f,"New near-white area <=15% at peak");
                }
                csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4},{5},{6},{7},{8}\n",frame,frame/(float)FrameRate,driver.Envelope,driver.Pulse,driver.PulseCount,driver.DiceReady,driver.PulseFinished,driver.IsComplete,driver.SuccessfulPointCount);
            }
            Require(driver.PulseCount==2&&driver.IsComplete,"Native sequence one reveal two peaks one fade");
            // Repeated camera renders must not advance any visual state.
            float age=driver.Elapsed; Capture(camera,rt,pixels); Capture(camera,rt,pixels); Require(age==driver.Elapsed,"OnRenderImage is clock-free");
            filter.Release(); Capture(camera,rt,pixels);
            Color32[] released=pixels.GetPixels32(); int releaseDelta=0;
            for(int p=0;p<released.Length;p++) releaseDelta=Math.Max(releaseDelta,PixelDelta(baseline[p],released[p]));
            Require(releaseDelta<=1,"Release restores exact source frame");
            nearWhiteNewMax=Math.Max(nearWhiteNewMax,ExportPropagation(template,filter,camera,rt,pixels,baseline,output));
            File.WriteAllText(Path.Combine(output,"timing.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(output,"frame-sha256.txt"),hashes.ToString());
            File.WriteAllText(Path.Combine(output,"native-report.txt"),
                (bundleReadback?"NATIVE_BUNDLE_READBACK_EXPORT: technical checks completed.\n":"FIRST_PREVIEW: source material native export.\n")+
                "Revision="+revision+"\nIndependent visual acceptance=NOT DETERMINED BY EXPORT; main-thread review required.\n"+
                "Unity="+Application.unityVersion+"\nGPU="+SystemInfo.graphicsDeviceName+"\nAPI="+SystemInfo.graphicsDeviceType+"\nColorSpace="+QualitySettings.activeColorSpace+
                "\nNative Camera.Render -> same VeliaTideMistScreenFilter.OnRenderImage -> Graphics.Blit\nMaterial origin="+(bundleReadback?"native AssetBundle.LoadFromFile / LoadAsset<Material>":"editor source material")+
                "\nActual repository sprites, proxy gloomy stage and proxy numbers/UI; NOT actual game/HUD acceptance.\nBottom-left viewport origin; red TOP LEFT / cyan BOTTOM RIGHT markers are source orientation probes.\n"+FrameCount+" frames at "+FrameRate+"Hz; callbacks frame"+FirstCallbackFrame+" and"+SecondCallbackFrame+"; next dice Begin at"+SecondBeginFrame+"; Finish only after second pulse tail.\nAdditional exact pulse ages .10/.18/.30/.48/.60 seconds for each dice use fresh instances of the same driver, same scene and native filter.\nPulse first/second=.72/.77s; original 0..0.18, hold .18..48, remaining fall .24/.29s. Shared cloud/fold/shadow motion time slows to .65 after .32s intro; static beam geometry unchanged.\nMax additional near-white pixels (peaks + propagation samples)="+nearWhiteNewMax+"/"+(Width*Height)+"\nSource restore max 8-bit delta="+releaseDelta+"\nTemplate untouched; OnRenderImage has no clock advancement.\n");
            WriteSourceReceipt(output);
            File.WriteAllText(Path.Combine(output,"cloud-readback.json"),JsonUtility.ToJson(VerifyCloudTexture((Texture2D)template.GetTexture("_CloudPlate")),true));
            File.WriteAllText(Path.Combine(output,"completion-check.txt"),"Frame"+CompleteFrame+" source restore max 8-bit delta="+completeDelta+"\n");
        }
        finally
        {
            filter.Release(); camera.targetTexture=null; rt.Release();
            Object.DestroyImmediate(root); Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(rt); Object.DestroyImmediate(pixels);
            foreach(var item in Owned) if(item!=null) Object.DestroyImmediate(item);
            Owned.Clear(); Protection.Clear(); Hits.Clear();
        }
    }

    private static byte[] Capture(Camera camera,RenderTexture rt,Texture2D pixels)
    {
        RenderTexture previous=RenderTexture.active;
        try { camera.Render(); RenderTexture.active=rt; pixels.ReadPixels(new Rect(0,0,Width,Height),0,0); pixels.Apply(); return pixels.EncodeToPNG(); }
        finally { RenderTexture.active=previous; }
    }
    private static int ExportPropagation(Material template,VeliaTideMistScreenFilter filter,Camera camera,RenderTexture rt,Texture2D pixels,Color32[] baseline,string output)
    {
        int maximum=0;
        var csv=new StringBuilder("dice,pulse_age,absolute_time,pulse,file,sha256\n");
        for(int dice=0;dice<2;dice++) foreach(float age in new[]{0.10f,0.18f,0.30f,0.48f,0.60f})
        {
            var sample=new VeliaTideMistVisualController();sample.SetProtectionRects(Protection.ToArray());sample.BeginDice(0);
            sample.Advance(FirstCallbackFrame/(float)FrameRate);sample.TriggerPulse(Hits.ToArray());
            if(dice==1){sample.Advance((SecondBeginFrame-FirstCallbackFrame)/(float)FrameRate);Require(sample.PulseFinished,"Propagation second dice follows first tail");sample.BeginDice(1);sample.Advance((SecondCallbackFrame-SecondBeginFrame)/(float)FrameRate);sample.TriggerPulse(Hits.ToArray());}
            sample.Advance(age);filter.Initialize(sample,template);
            string name="pulse"+(dice+1)+"_plus_"+age.ToString("0.00",CultureInfo.InvariantCulture).Replace('.', 'p')+"s.png";
            string path=Path.Combine(output,name);File.WriteAllBytes(path,Capture(camera,rt,pixels));
            int added=0;Color32[] frame=pixels.GetPixels32();
            for(int p=0;p<frame.Length;p++)if(NearWhite(frame[p])&&!NearWhite(baseline[p]))added++;
            Require((float)added/frame.Length<=0.15f,"Near-white limit during cloud propagation");maximum=Math.Max(maximum,added);
            csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F2},{2:F2},{3:F6},{4},{5}\n",dice+1,age,sample.Elapsed,sample.Pulse,name,Digest(path));
            Debug.Log("VELIA_NATIVE_PROPAGATION "+path);filter.Release();
        }
        File.WriteAllText(Path.Combine(output,"propagation.csv"),csv.ToString());
        return maximum;
    }
    private static string Label(int frame)
    {
        switch(frame) {
            case 0:return "00_initial";case 10:return "01_reveal";case 30:return "02_wait";
            case 39:return "03_peak1";case 49:return "04_post_peak1";case 90:return "05_between_dice";
            case 120:return "06_peak2";case 130:return "07_post_peak2";case 176:return "08_fade";case 200:return "09_complete";
            default:return null;
        }
    }

    private static void CreateStage(GameObject root,Camera camera)
    {
        Transform parent=root.transform;
        Quad(parent,new Vector3(0,0,6),new Vector2(18,10),new Color(0.09f,0.115f,0.15f));
        Quad(parent,new Vector3(0,-3.4f,5),new Vector2(18,3.2f),new Color(0.12f,0.13f,0.15f));
        for(int x=-8;x<=8;x+=2)
        {
            Quad(parent,new Vector3(x,1.2f,4),new Vector2(1.45f,5.4f),new Color(0.12f,0.145f,0.18f));
            Quad(parent,new Vector3(x,1.2f,3.9f),new Vector2(1.20f,4.9f),new Color(0.065f,0.085f,0.12f));
            for(int y=-1;y<4;y++)
                Quad(parent,new Vector3(x,y,3.8f),new Vector2(1.1f,0.035f),new Color(0.24f,0.255f,0.28f));
        }
        for(int row=0;row<8;row++) Quad(parent,new Vector3(0,-2.3f-row*0.33f,3),new Vector2(18,0.017f),new Color(0.22f,0.23f,0.25f));
        Quad(parent,new Vector3(0,4.55f,1),new Vector2(18,0.8f),new Color(0.035f,0.045f,0.06f));
        Quad(parent,new Vector3(0,-4.4f,1),new Vector2(18,1.2f),new Color(0.035f,0.045f,0.06f));
        Text(parent,"NATIVE UNITY PROXY  |  VELIA / TIDE MIST  |  9004004",new Vector3(-6.8f,4.7f,-2),0.18f,new Color(0.81f,0.85f,0.86f));
        Text(parent,"STAGE + NUMBERS + UI ARE PROXIES / REAL REPOSITORY CHARACTER SPRITES",new Vector3(-7.6f,-4.0f,-2),0.13f,new Color(0.55f,0.66f,0.72f));
        Text(parent,"TOP LEFT",new Vector3(-8.45f,4.1f,-2),0.12f,new Color(0.95f,0.35f,0.32f));
        Text(parent,"BOTTOM RIGHT",new Vector3(6.55f,-4.65f,-2),0.12f,new Color(0.25f,0.88f,0.9f));
        Character(parent,camera,"Velia",-5.1f,-2.7f,2.8f,false);
        Character(parent,camera,"Sivier",0.9f,-2.55f,3.0f,true);
        Character(parent,camera,"Sivier",5.1f,-2.7f,2.8f,true);
        // Fine stripe card panel is part of the source, useful for identifying source blur or UV flip.
        for(int x=0;x<80;x++) Quad(parent,new Vector3(-4.8f+x*0.025f,-4.65f,-1),new Vector2(0.0125f,0.25f),x%2==0?new Color(0.18f,0.2f,0.22f):new Color(0.8f,0.82f,0.84f));
    }

    private static void Character(Transform parent,Camera camera,string name,float x,float foot,float height,bool victim)
    {
        string source="SteriaBuild/SteriaModFolder/Resource/CharacterSkin/"+name+"/ClothCustom/Default.png";
        Texture2D texture=ImportTexture(source,"Assets/PreviewInputs/"+name+".png",false);
        Color32[] pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
        for(int y=0;y<texture.height;y++)for(int px=0;px<texture.width;px++) if(pixels[y*texture.width+px].a>20){minX=Math.Min(minX,px);maxX=Math.Max(maxX,px);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
        var sprite=Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(0.5f,0),100f);Owned.Add(sprite);
        var go=new GameObject(name+" REAL SPRITE");go.transform.SetParent(parent,false);go.transform.position=new Vector3(x,foot,0);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.flipX=victim;
        go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        Bounds b=renderer.bounds; Vector3 a=camera.WorldToViewportPoint(b.min),z=camera.WorldToViewportPoint(b.max);
        Protection.Add(Rect.MinMaxRect(a.x-0.006f,a.y-0.006f,z.x+0.006f,z.y+0.016f));
        if(victim) { Vector3 p=camera.WorldToViewportPoint(b.center);Hits.Add(new Vector2(p.x,p.y)); }
        Text(parent,victim?"18  /  12":"3  /  4",new Vector3(x-0.65f,foot+height+0.43f,-2),0.24f,new Color(0.94f,0.91f,0.76f));
        Quad(parent,new Vector3(x,foot-0.28f,-1),new Vector2(2.0f,0.13f),new Color(0.40f,0.18f,0.17f));
        Quad(parent,new Vector3(x-0.20f,foot-0.28f,-1.1f),new Vector2(1.55f,0.085f),new Color(0.68f,0.32f,0.28f));
        Text(parent,name.ToUpperInvariant()+"   42 / 60",new Vector3(x-0.9f,foot-0.47f,-2),0.15f,new Color(0.75f,0.81f,0.84f));
    }

    private static void Quad(Transform parent,Vector3 position,Vector2 size,Color color)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=new Vector3(size.x,size.y,1);
        var material=new Material(Shader.Find("Unlit/Color")){color=color};Owned.Add(material);go.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    private static void Text(Transform parent,string value,Vector3 position,float size,Color color)
    {
        var go=new GameObject("Proxy text "+value);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("Arial.ttf");text.text=value;text.fontSize=56;text.characterSize=size/4f;text.color=color;text.anchor=TextAnchor.UpperLeft;
        go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
    }
    private static bool NearWhite(Color32 p){return p.r>=242&&p.g>=242&&p.b>=230;}
    private static int PixelDelta(Color32 a,Color32 b){return Math.Max(Math.Abs(a.r-b.r),Math.Max(Math.Abs(a.g-b.g),Math.Abs(a.b-b.b)));}
    private static void WriteSourceReceipt(string output)
    {
        var files=new[]{"SteriaBuild/VeliaTideMistVisualController.cs","SteriaBuild/VeliaTideMistScreenFilter.cs","SteriaBuild/FarAreaEffect_Steria_VeliaTideMist.cs","SteriaBuild/BehaviourAction_Steria_VeliaTideMist.cs",MistSource,CloudSource,
            "SteriaBuild/VFXSource/VeliaTideMist/UnityProject/Assets/Shaders/VeliaTideMist.shader","SteriaBuild/VFXSource/VeliaTideMist/UnityProject/Assets/Editor/VeliaTideMistBundleBuilder.cs",
            "SteriaBuild/VFXSource/VeliaTideMist/source_assets/references.json","SteriaBuild/VFXSource/VeliaTideMist/verify_velia_tide_mist_source.ps1",
            "SteriaBuild/VFXSource/VeliaTideMist/export_velia_tide_mist_video.ps1","SteriaBuild/VFXSource/VeliaTideMist/build_velia_tide_mist.ps1","SteriaBuild/VeliaCards.cs"};
        var receipt=new StringBuilder();foreach(string file in files)receipt.AppendLine(Digest(Path.Combine(Repo(),file))+"  "+file);
        File.WriteAllText(Path.Combine(output,"source-sha256.txt"),receipt.ToString());
    }
    private static string Repo(){var d=new DirectoryInfo(Application.dataPath);while(d!=null&&!File.Exists(Path.Combine(d.FullName,"AGENTS.md")))d=d.Parent;Require(d!=null,"Repository root");return d.FullName;}
    private static string PreviewRoot(){return Path.Combine(Repo(),"preview_exports/velia_tide_mist");}
    private static string Revision(string fallback)
    {
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++) if(args[i]=="-veliaPreviewName")
        {
            string name=args[i+1]; Require(System.Text.RegularExpressions.Regex.IsMatch(name,"^[A-Za-z0-9_-]+$"),"Safe preview revision name");return name;
        }
        return fallback;
    }
    private static string Digest(string path){using(var hash=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","");}
    [Serializable]
    private sealed class BundleReadbackReceipt
    {
        public string status="NATIVE_BUNDLE_READBACK_CHECKS_COMPLETED",visualAcceptance="Not determined by exporter; consult main-thread independent review",revision;
        public string bundlePath,bundleSha256,materialName,shaderName,atlasFormat,atlasFilter,atlasWrap,sourceAtlasSha256;
        public string[] explicitAssetNames;
        public bool shaderSupported;
        public int atlasWidth,atlasHeight;
        public CloudReadbackReceipt cloudPlate;
    }
    [Serializable] private sealed class SourceReferences { public CloudSourceReference cloudPlate; }
    [Serializable] private sealed class CloudSourceReference { public string path,sha256,alpha; public int width,height; public bool sRGB; }
    [Serializable] private sealed class CloudReadbackReceipt
    {
        public string sourcePath,sourceSha256,format,filter,wrap;
        public int width,height,alphaMin,alphaMax;
        public bool sRGB,alphaMatchesSource;
        public float opaqueFraction,softFraction,centralClearFraction,lowerClearFraction;
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Velia verification: "+message);}
}
