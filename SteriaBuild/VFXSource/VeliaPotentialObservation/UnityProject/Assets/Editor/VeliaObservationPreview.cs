using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

    // Native rendering fixture. It replays the production visual; it does not simulate game settlement.
[InitializeOnLoad]
public static class VeliaObservationPreview
{
    private const string MaterialPath = "Assets/Materials/VeliaObservationMaterial.mat";
    private const string OutputKey = "VeliaObservationPreview.Output";
    private const int Width = 1280, Height = 720;
    private static readonly List<Object> Owned = new List<Object>();
    private static readonly int[] Stages = { 6, 18, 33, 54, 75, 90 };
    private static readonly string Model = "SteriaBuild/CharacterModels/Velia_Layered_v1";
    private static bool InvalidVelocityCurves;
    [Serializable] private sealed class Anchors { public float[] rightPalm; }
    [Serializable] private sealed class Readback
    {
        public string unity, graphics, colorSpace, material, shader, bundleSha256;
        public string[] explicitAssets;
        public bool shaderSupported;
    }
    [Serializable] private sealed class Geometry
    {
        public bool mirrored;
        public Vector2 palmPixel;
        public Vector3 palmWorld, targetFeet;
        public float targetHeight, pixelsPerUnit;
        public Vector2 casterPivot;
    }
    [Serializable] private sealed class LightMetric
    {
        public float age,topH,centroidH,energy,peak,coreBandMean,edgeBandMean,outerBandMean,hotspotYH,hotspotWidthH;
        public int visiblePixels,nearWhitePixels;
    }
    [Serializable] private sealed class LightMetrics
    {
        public string hotspotDefinition="Native RGB maximum channel >= 65% frame peak (minimum .03); energy-weighted Y and central 90% energy X width";
        public LightMetric[] stages;
    }

    static VeliaObservationPreview()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        Application.logMessageReceived += (message,stack,type)=>{if(message.Contains("Particle Velocity curves must all be in the same mode"))InvalidVelocityCurves=true;};
    }
    private static string Repo() { return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../..")); }
    private static string FileAt(string relative) { return Path.Combine(Repo(), relative); }
    private static void Require(bool result, string message)
    { if (!result) throw new InvalidOperationException(message); }
    private static string Hash(string path)
    { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    private static string Argument(string key)
    {
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++) if(args[i]==key)return args[i+1];
        throw new InvalidOperationException("Missing argument "+key);
    }

    public static void BuildAndPreview()
    {
        try
        {
            Require(Application.unityVersion=="2019.3.15f1", "Unity version mismatch");
            Require(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11 required");
            string output=Path.GetFullPath(Argument("-observationOutput"));
            Require(Directory.Exists(output), "Wrapper must create unique output directory");
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/VeliaObservation.shader");
            Require(shader!=null && shader.isSupported, "Authored shader unsupported");
            Directory.CreateDirectory("Assets/Materials");
            Material authored=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(authored==null) { authored=new Material(shader); authored.name=VeliaPotentialObservationVisual.MaterialName; AssetDatabase.CreateAsset(authored,MaterialPath); }
            else { authored.shader=shader; authored.name=VeliaPotentialObservationVisual.MaterialName; EditorUtility.SetDirty(authored); }
            VeliaObservationParticleBuilder.Build(Repo(),output);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("AssetBundles");
            var build=new AssetBundleBuild { assetBundleName=VeliaPotentialObservationVisual.BundleName,assetNames=new[]{MaterialPath,VeliaObservationParticleBuilder.PrefabPath} };
            var manifest=BuildPipeline.BuildAssetBundles("AssetBundles",new[]{build},BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
            Require(manifest!=null,"Bundle build failed");
            string bundlePath=Path.GetFullPath(Path.Combine("AssetBundles",VeliaPotentialObservationVisual.BundleName));
            File.Copy(bundlePath,Path.Combine(output,VeliaPotentialObservationVisual.BundleName));
            var bundle=AssetBundle.LoadFromFile(bundlePath);
            Require(bundle!=null,"Bundle native load failed");
            Material material=bundle.LoadAsset<Material>(VeliaPotentialObservationVisual.MaterialName);
            Require(material!=null && material.shader!=null && material.shader.isSupported,"Bundled material/shader readback failed");
            var prefab=bundle.LoadAsset<GameObject>("VeliaObservationParticles");
            VeliaObservationParticleBuilder.VerifyLoadedPrefab(prefab,output);
            var roots=new HashSet<string>(bundle.GetAllAssetNames());
            Require(roots.Count==2 && roots.Contains(MaterialPath.ToLowerInvariant()) && roots.Contains(VeliaObservationParticleBuilder.PrefabPath.ToLowerInvariant()),"Bundle must expose hand material and particle prefab roots");
            Require(manifest.GetAllDependencies(VeliaPotentialObservationVisual.BundleName).Length==0,"Bundle dependencies are not self-contained");
            File.WriteAllText(Path.Combine(output,"bundle-readback.json"),JsonUtility.ToJson(new Readback {
                unity=Application.unityVersion,graphics=SystemInfo.graphicsDeviceType.ToString(),colorSpace=QualitySettings.activeColorSpace.ToString(),
                material=material.name,shader=material.shader.name,shaderSupported=material.shader.isSupported,
                bundleSha256=Hash(bundlePath),explicitAssets=bundle.GetAllAssetNames()},true));
            Debug.Log("[CASE] ParticlePrefabTextureMaterialReadback PASS");
            bundle.Unload(true);
            File.WriteAllText(Path.Combine(output,"coverage.txt"),
                "Covered: exact production visual and Shuriken prefab, native bundled textures/materials, actual full-canvas character exports/XML pivots at 50 PPU, both facings, selected recipient only, zero dt pause, HideHand, completion/cancel/replay, native Update loop.\n"+
                "Not covered: BattleUnitModel, EGO cooldown, enemy post-speed-roll callback or full reception. These belong to integration checks, not this visual fixture.\n");
            SessionState.SetString(OutputKey,output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    private static Sprite LoadSprite(string png,string xml,string action)
    {
        var doc=new XmlDocument();doc.Load(FileAt(xml));
        XmlNode pivot=doc.SelectSingleNode("/ModInfo/ClothInfo/"+action+"/Pivot");
        Require(pivot!=null,"Missing exported XML action "+action);
        float x=float.Parse(pivot.Attributes["pivot_x"].Value,CultureInfo.InvariantCulture);
        float y=float.Parse(pivot.Attributes["pivot_y"].Value,CultureInfo.InvariantCulture);
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);Owned.Add(texture);
        Require(texture.LoadImage(File.ReadAllBytes(FileAt(png))),"PNG load failed "+png);
        Require(texture.width==512 && texture.height==512,"Expected full 512px exported canvas");
        texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
        var sprite=Sprite.Create(texture,new Rect(0,0,512,512),new Vector2((x+512)/1024,(y+512)/1024),50,0,SpriteMeshType.FullRect);
        Owned.Add(sprite); return sprite;
    }
    private static SpriteRenderer Character(string name,Sprite sprite,float x,bool mirrored)
    {
        var go=new GameObject(name);Owned.Add(go);
        go.transform.position=new Vector3(x,0,0);go.transform.localScale=new Vector3(mirrored?-1:1,1,1);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=0;
        return renderer;
    }
    private static Rect Content(Sprite sprite)
    {
        Color32[] pixels=sprite.texture.GetPixels32(); int minX=512,minY=512,maxX=0,maxY=0;
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)if(pixels[y*512+x].a>32)
        { minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x+1);maxY=Math.Max(maxY,y+1); }
        Require(maxX>minX && maxY>minY,"Empty character alpha");
        return new Rect(minX,minY,maxX-minX,maxY-minY);
    }
    private static Camera Camera()
    {
        var go=new GameObject("PreviewCamera");Owned.Add(go);
        var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.0f;
        camera.transform.position=new Vector3(0,3.3f,-20);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(0.055f,0.070f,0.095f,1);camera.nearClipPlane=.1f;camera.farClipPlane=60;
        camera.allowHDR=false;camera.allowMSAA=false;
        var floor=new GameObject("GroundReference");Owned.Add(floor);
        var line=floor.AddComponent<LineRenderer>();line.positionCount=2;line.SetPositions(new[]{new Vector3(-20,-.06f,1),new Vector3(20,-.06f,1)});
        line.startWidth=line.endWidth=.012f;line.startColor=line.endColor=new Color(.20f,.23f,.28f,1);
        var mat=new Material(Shader.Find("Sprites/Default"));Owned.Add(mat);line.sharedMaterial=mat;line.sortingOrder=-10;
        return camera;
    }
    private static void Save(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(Width,Height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var read=new Texture2D(Width,Height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,Width,Height),0,0);read.Apply();
        File.WriteAllBytes(path,read.EncodeToPNG());Object.DestroyImmediate(read);
        camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
    }
    private static void Export(GameObject particlePrefab,Material material,string output,bool mirrored,bool midGray)
    {
        Owned.Clear();
        string facing=(midGray?"mid-":"dark-")+(mirrored?"mirrored":"canonical");string dir=Path.Combine(output,facing);Directory.CreateDirectory(dir);
        string veliaXml=Model+"/build/Velia_Layered_v1_Projection/ModInfo.Xml";
        Sprite raised=LoadSprite(Model+"/build/assembled/S2.png",veliaXml,"S2");
        Sprite normal=LoadSprite(Model+"/build/assembled/Default.png",veliaXml,"Default");
        string targetRoot="SteriaBuild/CharacterModels/Sivier_Layered_v1/build/runtime/Resource/CharacterSkin/Sivier";
        Sprite selected=LoadSprite(targetRoot+"/ClothCustom/Default.png",targetRoot+"/ModInfo.Xml","Default");
        string otherRoot="SteriaBuild/CharacterModels/Ailierel_Layered_v1/build/runtime/Resource/CharacterSkin/Ailierel";
        Sprite unselected=LoadSprite(otherRoot+"/ClothCustom/Default.png",otherRoot+"/ModInfo.Xml","Default");
        var caster=Character("Velia",raised,mirrored?4.7f:-4.7f,mirrored);
        var target=Character("Selected_Sivier",selected,0,mirrored);
        Character("Unselected_Ailierel",unselected,mirrored?-4.7f:4.7f,mirrored);
        var camera=Camera();
        if(midGray)camera.backgroundColor=new Color(.28f,.28f,.28f,1);
        Anchors anchors=JsonUtility.FromJson<Anchors>(File.ReadAllText(FileAt(Model+"/sources/observation/anchors.json")));
        Vector2 palmPixel=new Vector2(anchors.rightPalm[0],anchors.rightPalm[1]);
        Vector3 palm=VeliaPotentialObservationVisual.SpritePoint(caster,palmPixel);
        Rect bounds=Content(selected);
        Vector3 feet=VeliaPotentialObservationVisual.SpritePoint(target,new Vector2(selected.pivot.x,512-bounds.yMin));
        Vector3 crown=VeliaPotentialObservationVisual.SpritePoint(target,new Vector2(selected.pivot.x,512-bounds.yMax));
        float height=Vector3.Distance(feet,crown);
        File.WriteAllText(Path.Combine(dir,"geometry.json"),JsonUtility.ToJson(new Geometry {mirrored=mirrored,palmPixel=palmPixel,palmWorld=palm,targetFeet=feet,targetHeight=height,pixelsPerUnit=50,casterPivot=raised.pivot},true));
        var visual=new VeliaPotentialObservationVisual(particlePrefab,material,0,0,0);visual.SetAnchors(palm,feet,height,mirrored);
        visual.Advance(0);Require(visual.Age==0,"Zero-dt progressed clock");
        string replaySnapshot=null;
        float coreAt30=0,coreAt55=0,glintAt37=0,glintAt55=0,headAt30=0,headAt55=0,headAt90=0;
        LightMetric lightAt30=null,lightAt55=null,lightAt90=null;var lightMetrics=new List<LightMetric>();
        var metrics=new StringBuilder("frame,time,alive,core,hotspot,glints,minX_H,maxX_H,minY_H,maxY_H\n");
        for(int frame=0;frame<=90;frame++)
        {
            if(frame>0)visual.Advance(1f/60);
            if(frame>=54)caster.sprite=normal;
            metrics.AppendLine(ParticleMetrics(frame,visual.Age,feet,height));
            if(frame==18){replaySnapshot=ParticleSnapshot();coreAt30=ParticleTop("CoreFlow",feet,height);headAt30=ParticleTop("SoftLift",feet,height);}
            if(frame==22)glintAt37=ParticleTop("Glints",feet,height);
            if(frame==33){coreAt55=ParticleTop("CoreFlow",feet,height);glintAt55=ParticleTop("Glints",feet,height);headAt55=ParticleTop("SoftLift",feet,height);}
            if(frame==54)headAt90=ParticleTop("SoftLift",feet,height);
            Save(camera,Path.Combine(dir,"frame_"+frame.ToString("D3")+".png"));
            if(Array.IndexOf(Stages,frame)>=0)File.Copy(Path.Combine(dir,"frame_"+frame.ToString("D3")+".png"),Path.Combine(output,facing+"_"+frame.ToString("D3")+".png"));
            if(!mirrored && !midGray && Array.IndexOf(Stages,frame)>=0)
            {
                var light=SaveLightOnly(camera,Path.Combine(output,"light-only_"+frame.ToString("D3")+".png"),feet,height,visual.Age);lightMetrics.Add(light);
                if(frame==18)lightAt30=light;if(frame==33)lightAt55=light;if(frame==54)lightAt90=light;
            }
        }
        File.WriteAllText(Path.Combine(dir,"particle-metrics.csv"),metrics.ToString());
        Require(!InvalidVelocityCurves,"Unity rejected particle velocity curve modes");
        Require(Mathf.Abs(coreAt55-coreAt30)<.0001f,"R6 core carrier must stay anchored; shader owns rising front");
        Require(glintAt55>glintAt37+.10f,"Small straight glints failed actual upward motion");
        Require(headAt55>headAt30+.25f && headAt90>headAt55+.25f,"Main hotspot particle did not rise through the body");
        if(lightAt30!=null && lightAt55!=null)
        {
            File.WriteAllText(Path.Combine(output,"light-only-metrics.json"),JsonUtility.ToJson(new LightMetrics{stages=lightMetrics.ToArray()},true));
            Require(lightAt30.visiblePixels>0 && lightAt55.topH>lightAt30.topH+.15f,"Native mask light front did not rise from .30 to .55 seconds");
            Require(lightAt90!=null && lightAt55.hotspotYH>lightAt30.hotspotYH+.20f && lightAt90.hotspotYH>lightAt55.hotspotYH+.15f,"Bright native hotspot remains fixed while dim cone reveals");
            Require(lightAt90.hotspotWidthH>lightAt30.hotspotWidthH*1.15f,"Native hotspot did not open laterally");
            Debug.Log("[CASE] NativeMaskFront PASS .30 topH="+lightAt30.topH+" .55 topH="+lightAt55.topH+" anchoredCoreY="+coreAt55);
            Debug.Log("[CASE] NativeHotspotPropagation PASS GPU high-bandY="+lightAt30.hotspotYH+","+lightAt55.hotspotYH+","+lightAt90.hotspotYH+" width="+lightAt30.hotspotWidthH+","+lightAt55.hotspotWidthH+","+lightAt90.hotspotWidthH+" PS centre="+headAt30+","+headAt55+","+headAt90);
        }
        Debug.Log("[CASE] GlintMotion "+facing+" PASS .37="+glintAt37+" .55="+glintAt55);
        Require(visual.Complete && visual.LiveParticleCount==0 && visual.HandVertexCount==0,"Final effect did not clear all particles and hand geometry");
        visual.Dispose();Require(visual.Complete && visual.LiveParticleCount==0 && visual.HandVertexCount==0,"Dispose contract");
        var replay=new VeliaPotentialObservationVisual(particlePrefab,material,0,0,0);replay.SetAnchors(palm,feet,height,mirrored);
        for(int i=0;i<18;i++)replay.Advance(1f/60);
        Require(replay.LiveParticleCount>0,"Replay has no live particles");
        Require(ParticleSnapshot()==replaySnapshot,"Fixed-seed particle replay differs at same age");
        if(!mirrored && !midGray) CheckHandInterruption(replay,camera,output);
        replay.Dispose();Require(replay.LiveParticleCount==0 && replay.HandVertexCount==0,"Cancellation did not clear");
        Save(camera,Path.Combine(output,facing+"_cancel.png"));
        Debug.Log("[CASE] NativeVisual "+facing+" zero-dt/completion/disposal/replay PASS; palm="+palm+" H="+height);
        Cleanup();
    }
    private static void Cleanup()
    { foreach(var item in Owned) if(item!=null)Object.DestroyImmediate(item);Owned.Clear(); }

    private static ParticleSystem[] LiveParticles()
    {
        var result=new List<ParticleSystem>();
        foreach(var ps in Object.FindObjectsOfType<ParticleSystem>())if(ps.gameObject.activeInHierarchy)result.Add(ps);
        result.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));return result.ToArray();
    }
    private static string ParticleSnapshot()
    {
        var text=new StringBuilder();
        foreach(var ps in LiveParticles())
        {
            var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);
            text.Append(ps.name).Append(':').Append(count).Append(';');
            for(int i=0;i<count;i++)text.Append(particles[i].position.ToString("F6")).Append('/').Append(particles[i].remainingLifetime.ToString("F6",CultureInfo.InvariantCulture)).Append(';');
        }
        return text.ToString();
    }
    private static float ParticleTop(string name,Vector3 feet,float height)
    {
        float top=float.NegativeInfinity;
        foreach(var ps in LiveParticles())if(ps.name==name)
        {
            var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);
            for(int i=0;i<count;i++)top=Mathf.Max(top,(ps.transform.TransformPoint(particles[i].position).y-feet.y)/height);
        }
        return top;
    }
    private static string ParticleMetrics(int frame,float age,Vector3 feet,float height)
    {
        int total=0;var counts=new Dictionary<string,int>();float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
        foreach(var ps in LiveParticles())
        {
            var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);counts[ps.name]=count;total+=count;
            for(int i=0;i<count;i++)
            {Vector3 p=(ps.transform.TransformPoint(particles[i].position)-feet)/height;minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
        }
        if(total==0)minX=maxX=minY=maxY=0;
        return string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2},{3},{4},{5},{6:F5},{7:F5},{8:F5},{9:F5}",frame,age,total,
            counts.ContainsKey("CoreFlow")?counts["CoreFlow"]:0,counts.ContainsKey("SoftLift")?counts["SoftLift"]:0,counts.ContainsKey("Glints")?counts["Glints"]:0,minX,maxX,minY,maxY);
    }
    private static LightMetric SaveLightOnly(Camera camera,string path,Vector3 feet,float height,float age)
    {
        var disabled=new List<Renderer>();
        foreach(var renderer in Object.FindObjectsOfType<Renderer>())
            if(renderer.enabled && (renderer is SpriteRenderer || renderer is LineRenderer || renderer.name=="RightPalmLight")){disabled.Add(renderer);renderer.enabled=false;}
        Color prior=camera.backgroundColor;camera.backgroundColor=Color.black;Save(camera,path);camera.backgroundColor=prior;
        foreach(var renderer in disabled)renderer.enabled=true;
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();
        int count=0,white=0,coreSamples=0,edgeSamples=0,outerSamples=0;
        float top=0,energy=0,weightedY=0,peak=0,coreSum=0,edgeSum=0,outerSum=0;
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
        {
            float worldX=camera.transform.position.x+((x+.5f)/Width-.5f)*camera.orthographicSize*2*Width/Height;
            if(Mathf.Abs(worldX-feet.x)>.65f*height)continue;
            float h=(camera.transform.position.y+((y+.5f)/Height-.5f)*camera.orthographicSize*2-feet.y)/height;
            Color32 p=pixels[y*Width+x];float light=Mathf.Max(p.r,Mathf.Max(p.g,p.b))/255f;
            peak=Mathf.Max(peak,light);if(p.r>=250&&p.g>=250&&p.b>=250)white++;
            float xH=Mathf.Abs(worldX-feet.x)/height;
            if(h>=.10f&&h<=.65f)
            {
                if(xH<.08f){coreSamples++;coreSum+=light;}
                else if(xH>=.20f&&xH<=.35f){edgeSamples++;edgeSum+=light;}
                else if(xH>.35f&&xH<=.50f){outerSamples++;outerSum+=light;}
            }
            if(light>5f/255f){count++;top=Mathf.Max(top,h);energy+=light;weightedY+=light*h;}
        }
        Object.DestroyImmediate(texture);
        float highThreshold=Mathf.Max(.03f,peak*.65f),hotEnergy=0,hotY=0;var hotColumns=new float[Width];
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
        {
            float xH=(camera.transform.position.x+((x+.5f)/Width-.5f)*camera.orthographicSize*2*Width/Height-feet.x)/height;
            if(Mathf.Abs(xH)>.65f)continue;
            Color32 p=pixels[y*Width+x];float light=Mathf.Max(p.r,Mathf.Max(p.g,p.b))/255f;if(light<highThreshold)continue;
            float h=(camera.transform.position.y+((y+.5f)/Height-.5f)*camera.orthographicSize*2-feet.y)/height;
            hotEnergy+=light;hotY+=light*h;hotColumns[x]+=light;
        }
        float cumulative=0;int low=0,high=0;bool foundLow=false;
        if(hotEnergy>0)for(int x=0;x<Width;x++){cumulative+=hotColumns[x];if(!foundLow&&cumulative>=hotEnergy*.05f){low=x;foundLow=true;}if(cumulative>=hotEnergy*.95f){high=x;break;}}
        return new LightMetric{age=age,topH=top,centroidH=energy>0?weightedY/energy:0,energy=energy,visiblePixels=count,peak=peak,nearWhitePixels=white,
            coreBandMean=coreSamples>0?coreSum/coreSamples:0,edgeBandMean=edgeSamples>0?edgeSum/edgeSamples:0,outerBandMean=outerSamples>0?outerSum/outerSamples:0,
            hotspotYH=hotEnergy>0?hotY/hotEnergy:0,hotspotWidthH=hotEnergy>0?(high-low)*camera.orthographicSize*2/Height/height:0};
    }
    private static void CheckHandInterruption(VeliaPotentialObservationVisual visual,Camera camera,string output)
    {
        int handBefore=visual.HandVertexCount,bodyBefore=visual.LiveParticleCount;
        Require(handBefore>0 && bodyBefore>0,"Interruption fixture must begin with both hand and fountain visible");
        float age=visual.Age;
        string particlesBefore=ParticleSnapshot();
        string before=Path.Combine(output,"zero-dt-before.png"),after=Path.Combine(output,"zero-dt-after.png");
        Save(camera,before);visual.Advance(0);Save(camera,after);
        Require(visual.Age==age && Hash(before)==Hash(after) && particlesBefore==ParticleSnapshot(),"Zero dt changed event age, particle state or native rendering");
        visual.HideHand();
        Require(visual.HandVertexCount==0,"HideHand did not immediately clear palm mesh");
        visual.Advance(0);
        Require(visual.Age==age && visual.HandVertexCount==0 && visual.LiveParticleCount==bodyBefore && particlesBefore==ParticleSnapshot(),"Hidden palm returned or fountain changed at zero dt");
        visual.Advance(.1f);
        Require(visual.Age>age && visual.HandVertexCount==0 && visual.LiveParticleCount>0 && particlesBefore!=ParticleSnapshot(),"Fountain failed to continue independently of hidden palm");
        Save(camera,Path.Combine(output,"hand-interrupted-fountain-continues.png"));
        Debug.Log("[CASE] HideHandAndZeroDt PASS handBefore="+handBefore+" handAfter=0 body="+bodyBefore+" ageBefore="+age+" ageAfter="+visual.Age+" nativeZeroDtPixels=identical");
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        string output=SessionState.GetString(OutputKey,"");if(string.IsNullOrEmpty(output))return;
        SessionState.EraseString(OutputKey);
        try
        {
            var bundle=AssetBundle.LoadFromFile(Path.Combine(output,VeliaPotentialObservationVisual.BundleName));
            Require(bundle!=null,"Play-loop bundle load");
            var material=bundle.LoadAsset<Material>(VeliaPotentialObservationVisual.MaterialName);
            var prefab=bundle.LoadAsset<GameObject>("VeliaObservationParticles");
            Export(prefab,material,output,false,false);
            Export(prefab,material,output,true,false);
            Export(prefab,material,output,false,true);
            Export(prefab,material,output,true,true);
            var camera=Camera();
            var runner=new GameObject("NativeUpdateSmoke").AddComponent<VeliaObservationFrameSmoke>();
            runner.Initialize(prefab,material,(result,frames,age)=>{
                File.WriteAllText(Path.Combine(output,"play-loop.txt"),"result="+result+" frames="+frames+" age="+age.ToString("F3",CultureInfo.InvariantCulture)+"\n");
                if(result && !InvalidVelocityCurves) { Debug.Log("[CASE] RealMonoBehaviourUpdate PASS frames="+frames+" age="+age); Debug.Log("VELIA_OBSERVATION_NATIVE_PASS");EditorApplication.Exit(0); }
                else { Debug.LogError("Real Update smoke failed");EditorApplication.Exit(1); }
            },()=>Save(camera,Path.Combine(output,"play-loop-peak.png")));
        }
        catch(Exception ex) {Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
