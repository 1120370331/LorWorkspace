using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Steria.AntieVfx;
using Object=UnityEngine.Object;

public static class AntiePreview
{
    static string output;
    static readonly float[] Times={.025f,.075f,.145f,.230f,.400f,.620f,.850f};
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);Debug.Log("ANTIE_CHECK "+message);}
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();output=args[Array.IndexOf(args,"-antieOutput")+1];Directory.CreateDirectory(output);
            Check(Application.unityVersion=="2019.3.15f1","exact editor");Check(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,"real D3D11 GPU");
            AntieBuilder.Build();VerifyAssets();
            for(int k=0;k<3;k++)Render((AntieAttackKind)k);
            BuildAndReload();
            var stage=AntieBuilder.Stage(AntieAttackKind.SlashHorizontal,true);stage.autoPlay=true;stage.animationDriven=true;stage.GetComponent<Animator>().enabled=true;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),AntieBuilder.Root+"/Scenes/AntieAuthoring.unity");AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(output,"complete.json"),"{\"status\":\"technical-pass\",\"unity\":\"2019.3.15f1\",\"renderer\":\"D3D11\",\"profiles\":3,\"animationClips\":3,\"nativeBundleReloadPixelEqual\":true,\"gameIntegrated\":false,\"deployed\":false}");
            Debug.Log("ANTIE_ICE_INDIGO_PASS");EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void VerifyAssets()
    {
        foreach(string name in new[]{"Steria/Antie/IceIndigoSweptSlash","Steria/Antie/SoftRadialGlow"})
        {var shader=Shader.Find(name);Check(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"shader "+name);}
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{AntieBuilder.Root+"/Textures"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Check(texture.width==1254&&texture.height==1254,"unchanged source resolution "+path);
            Check(importer.wrapMode==TextureWrapMode.Clamp&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"source sampling "+path);
            Check(importer.sRGBTexture==path.Contains("SlashColor"),"data/colour space "+path);
        }
        var hit=AssetDatabase.LoadAssetAtPath<Mesh>(AntieBuilder.Root+"/Meshes/HitVertical_0.asset");var slash=AssetDatabase.LoadAssetAtPath<Mesh>(AntieBuilder.Root+"/Meshes/SlashHorizontal_0.asset");
        Check(hit.bounds.size.y>hit.bounds.size.x*1.5f&&hit.bounds.size.z<.00001f,"Hit keeps its original vertical screen plane");
        var horizontalNormal=slash.normals[slash.vertexCount/2];
        Check(Mathf.Abs(horizontalNormal.y)>.95f&&Mathf.Abs(horizontalNormal.z)>.25f&&Mathf.Abs(horizontalNormal.z)<.31f&&Mathf.Abs(horizontalNormal.x)<.001f,"horizontal cut lies near the ground with a slight screen tilt");
        Check(slash.bounds.size.x>slash.bounds.size.y*8&&slash.bounds.size.z>slash.bounds.size.y*3,"horizontal cut keeps width and real ground-plane depth");
        var outerBow=AntieSlash.Curve(AntieAttackKind.SlashHorizontal,0,.5f)-Vector3.Lerp(AntieSlash.Curve(AntieAttackKind.SlashHorizontal,0,0),AntieSlash.Curve(AntieAttackKind.SlashHorizontal,0,1),.5f);
        var inward=AntieSlash.Normal(AntieAttackKind.SlashHorizontal,0,.5f);
        Check(outerBow.z<-.5f&&outerBow.y<-.1f,"horizontal convex edge bows toward camera and slightly downward");
        Check(inward.y>.20f&&inward.z>.85f,"horizontal concave side rises away from the camera");
        foreach(string name in new[]{"PierceDual_0","PierceDual_1"})
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AntieBuilder.Root+"/Meshes/"+name+".asset");
            var normal=mesh.normals[mesh.vertexCount/2];
            Check(Mathf.Abs(normal.x)>.90f&&Mathf.Abs(normal.z)>.30f&&Mathf.Abs(normal.z)<.43f,"mesh normal primarily faces enemy with a slight camera angle "+name);
        }
        var a=AntieSlash.Curve(AntieAttackKind.PierceDual,0,1)-AntieSlash.Curve(AntieAttackKind.PierceDual,0,0);
        var b=AntieSlash.Curve(AntieAttackKind.PierceDual,1,1)-AntieSlash.Curve(AntieAttackKind.PierceDual,1,0);
        Check(a.x<0&&b.x>0&&a.y<0&&b.y<0,"both dual blades reverse front/back while retaining downward crossings");
        var convex=AntieSlash.Curve(AntieAttackKind.PierceDual,1,.5f)-Vector3.Lerp(AntieSlash.Curve(AntieAttackKind.PierceDual,1,0),AntieSlash.Curve(AntieAttackKind.PierceDual,1,1),.5f);
        Check(convex.x>.02f,"second blade convexity points toward the enemy");
        Check(AntieSlash.Delay(AntieAttackKind.PierceDual,1)>.06f,"second blade has a real animation delay");
    }
    static void Render(AntieAttackKind kind)
    {
        string name=AntieBuilder.Names[(int)kind],folder=Path.Combine(output,name);Directory.CreateDirectory(folder);
        foreach(string sub in new[]{"stages","motion","flow"})Directory.CreateDirectory(Path.Combine(folder,sub));
        var p=AntieBuilder.Stage(kind);var camera=Camera.main;p.ApplyAt(.23f);byte[] cold=Capture(camera,null);
        string baseline=Path.Combine(Directory.GetParent(output).FullName,"candidate-01",name,"stages","04.png");
        if(kind==AntieAttackKind.HitVertical&&File.Exists(baseline))Check(Equal(cold,File.ReadAllBytes(baseline)),"vertical slash is pixel-identical to candidate-01");
        for(int i=0;i<Times.Length;i++){p.ApplyAt(Times[i]);Capture(camera,Path.Combine(folder,"stages",(i+1).ToString("D2")+".png"));}
        for(int i=0;i<70;i++){p.ApplyAt(i/60f);Capture(camera,Path.Combine(folder,"motion",i.ToString("D3")+".png"));}
        p.ApplyAt(.23f);Check(Equal(cold,Capture(camera,null)),"absolute-time replay "+name);
        p.ApplyAt(AntieSlash.Duration);Check(!p.blades[0].enabled&&!p.glows[0].enabled,"final cleanup "+name);if(p.blades[1]!=null)Check(!p.blades[1].enabled,"second blade cleanup");
        Capture(camera,Path.Combine(folder,"end-clear.png"));
        foreach(bool mirror in new[]{false,true})foreach(bool grey in new[]{false,true})
        {
            p.mirror=mirror;camera.backgroundColor=grey?new Color(.32f,.32f,.32f):new Color(.018f,.03f,.052f);p.ApplyAt(.23f);
            Capture(camera,Path.Combine(folder,"peak-"+(mirror?"left":"right")+"-"+(grey?"grey":"dark")+".png"));
        }
        p.mirror=false;camera.backgroundColor=new Color(.018f,.03f,.052f);p.showGlow=false;p.ApplyAt(.23f);
        if(kind!=AntieAttackKind.HitVertical)
        {
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            camera.transform.position=new Vector3(8,1.95f,0);camera.transform.LookAt(new Vector3(.2f,1.95f,0));
            Capture(camera,Path.Combine(folder,"enemy-view.png"));
            camera.transform.position=new Vector3(0,8,0);camera.transform.LookAt(new Vector3(0,1.65f,0),Vector3.forward);
            Capture(camera,Path.Combine(folder,"top-view.png"));
            camera.transform.position=position;camera.transform.rotation=rotation;
        }
        var original=new Material[2];var white=new Material(Shader.Find("Unlit/Color")){color=new Color(.8f,.86f,.9f)};
        for(int i=0;i<2;i++)if(p.blades[i]!=null){original[i]=p.blades[i].sharedMaterial;p.blades[i].sharedMaterial=white;}
        Capture(camera,Path.Combine(folder,"white-model.png"));for(int i=0;i<2;i++)if(p.blades[i]!=null)p.blades[i].sharedMaterial=original[i];Object.DestroyImmediate(white);
        var block=new MaterialPropertyBlock();byte[] first=null,last=null;
        for(int frame=0;frame<36;frame++)
        {
            p.ApplyAt(.23f);
            for(int i=0;i<2;i++)if(p.blades[i]!=null){p.blades[i].GetPropertyBlock(block);block.SetFloat("_Phase",frame/60f+i*.03f);block.SetFloat("_FlowStrength",1);p.blades[i].SetPropertyBlock(block);}
            last=Capture(camera,Path.Combine(folder,"flow",frame.ToString("D3")+".png"));if(frame==0)first=last;
        }
        Check(!Equal(first,last),"provided flow map animates within fixed shape "+name);
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AntieBuilder.Root+"/Animations/"+name+".anim");
        Check(clip!=null&&Mathf.Abs(clip.length-AntieSlash.Duration)<.001f,"animation duration "+name);
        AnimationMode.StartAnimationMode();AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(p.gameObject,clip,.12f);AnimationMode.EndSampling();float sampled=p.sampleTime;AnimationMode.StopAnimationMode();
        Check(Mathf.Abs(sampled-.12f)<.001f,"Animator clip actually drives the effect clock "+name);
        var context=AntieBuilder.Stage(kind,true);context.ApplyAt(.23f);Capture(Camera.main,Path.Combine(folder,"character-context.png"));
        context.autoPlay=context.animationDriven=true;context.GetComponent<Animator>().enabled=true;EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),AntieBuilder.Root+"/Scenes/Antie"+name+".unity");
    }
    static void BuildAndReload()
    {
        string folder=Path.Combine(output,"bundle");Directory.CreateDirectory(folder);var paths=new List<string>();foreach(string name in AntieBuilder.Names)paths.Add(AntieBuilder.Root+"/Native/"+name+".prefab");
        const string bundleName="steria_antie_ice_indigo_combat";
        var manifest=BuildPipeline.BuildAssetBundles(folder,new[]{new AssetBundleBuild{assetBundleName=bundleName,assetNames=paths.ToArray()}},BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
        Check(manifest!=null&&manifest.GetAllDependencies(bundleName).Length==0,"self-contained three-profile native bundle");
        var bundle=AssetBundle.LoadFromFile(Path.Combine(folder,bundleName));Check(bundle!=null,"cold load actual bundle");
        for(int k=0;k<3;k++)
        {
            var old=AntieBuilder.Stage((AntieAttackKind)k);Object.DestroyImmediate(old.gameObject);
            var prefab=bundle.LoadAsset<GameObject>(AntieBuilder.Names[k]);Check(prefab!=null,"bundle native profile "+k);
            foreach(var c in prefab.GetComponentsInChildren<Component>(true))Check(c is Transform||c is MeshRenderer||c is MeshFilter,"bundle excludes custom behaviour/Animator");
            var go=Object.Instantiate(prefab);go.transform.position=new Vector3(0,.08f,0);var p=go.AddComponent<AntieSlash>();p.kind=(AntieAttackKind)k;p.autoPlay=p.animationDriven=false;p.BindNative();p.ApplyAt(.23f);
            string name=AntieBuilder.Names[k];Check(Equal(File.ReadAllBytes(Path.Combine(output,name,"stages/04.png")),Capture(Camera.main,Path.Combine(output,name,"bundle-reloaded.png"))),"cold bundle pixels equal "+name);
        }
        bundle.Unload(true);
    }
    static bool Equal(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    static byte[] Capture(Camera camera,string path)
    {
        var target=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);var before=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(960,540,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,540),0,0);texture.Apply();byte[] png=texture.EncodeToPNG();
        if(path!=null)File.WriteAllBytes(path,png);Object.DestroyImmediate(texture);camera.targetTexture=null;RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);return png;
    }
}
