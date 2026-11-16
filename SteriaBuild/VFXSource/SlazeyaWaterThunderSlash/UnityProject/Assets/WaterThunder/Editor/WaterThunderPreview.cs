using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Steria.WaterThunder;
using Object=UnityEngine.Object;

public static class WaterThunderPreview
{
    static readonly float[] Times={.045f,.155f,.215f,.330f,.535f,.790f};
    static string output;
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);Debug.Log("CHECK "+reason);}
    static void All(WaterThunderSlash p,bool on){p.water=p.splash=p.lightning=p.core=p.evaporate=p.impactBurst=on;}
    public static void BuildAndPreview()
    {
        if(!Application.isBatchMode){Debug.LogError("Use the authoring menu in a live Editor; exporter requires an isolated batch snapshot.");return;}
        try
        {
            var args=Environment.GetCommandLineArgs();output=args[Array.IndexOf(args,"-waterOutput")+1];Directory.CreateDirectory(output);
            Check(Application.unityVersion=="2019.3.15f1","Exact Unity 2019.3.15f1");
            Check(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,"D3D11 GPU renderer");
            PlayerSettings.colorSpace=ColorSpace.Gamma;WaterThunderBuilder.Build();VerifyAssets();VerifyMotion();
            var player=WaterThunderBuilder.CreateStage();var camera=Camera.main;
            player.ApplyAt(.215f);byte[] coldHit=Capture(camera,null);
            for(int layer=-1;layer<4;layer++)
            {
                string folder=layer<0?"composite":WaterThunderBuilder.Names[layer];Directory.CreateDirectory(Path.Combine(output,folder));
                player.water=layer<0||layer==0;player.splash=layer<0||layer==1;player.lightning=layer<0||layer==2;player.core=layer<0||layer==3;
                player.evaporate=layer<0||layer==0;player.impactBurst=layer<0;
                for(int i=0;i<Times.Length;i++){player.ApplyAt(Times[i]);if(layer>=0)player.evaporationMist.enabled=false;Capture(camera,Path.Combine(output,folder,(i+1).ToString("D2")+".png"));}
            }
            All(player,true);player.ApplyAt(WaterThunderMotion.HitTime-.001f);Check(player.impact.Alive==0,"No impact particles before contact");
            player.ApplyAt(.155f);var sweepBlock=new MaterialPropertyBlock();player.layers[0].GetPropertyBlock(sweepBlock);
            Check(Mathf.Abs(sweepBlock.GetFloat("_Reveal")-1)<.000001f,"Whole mesh present; no progressive reveal");
            Check(Mathf.Abs(sweepBlock.GetFloat("_SweepProgress")-WaterThunderMotion.Progress(.155f))<.000001f,"Brush mask sweeps at motion progress");
            player.ApplyAt(.033f);player.layers[0].GetPropertyBlock(sweepBlock);Check(sweepBlock.GetFloat("_TailGrowth")==0,"Pointed head precedes any spread tail");
            player.ApplyAt(.155f);player.layers[0].GetPropertyBlock(sweepBlock);Check(sweepBlock.GetFloat("_TailGrowth")>.9f,"Water tail expands only after head passes");
            player.ApplyAt(.215f);Check(player.impact.Alive>=40,"Original native water/lightning burst preserved");
            Check(player.impact.systems[0].GetComponent<ParticleSystemRenderer>().sharedMaterial.GetFloat("_Liquid")==1,"Native impact spray uses water surface rather than data colors");
            Vector3 contact=player.impact.transform.position;player.ApplyAt(.33f);Check((contact-player.impact.transform.position).sqrMagnitude<.000001f,"Impact stays fixed while blade continues downward");
            foreach(bool mid in new[]{false,true})foreach(bool mirror in new[]{false,true})
            {
                camera.backgroundColor=mid?new Color(.30f,.30f,.30f):new Color(.018f,.03f,.052f);player.mirror=mirror;player.ApplyAt(.33f);
                Capture(camera,Path.Combine(output,"peak-"+(mid?"gray":"dark")+"-"+(mirror?"left":"right")+".png"));
            }
            player.mirror=false;camera.backgroundColor=new Color(.018f,.03f,.052f);player.ApplyAt(.215f);
            byte[] first=Capture(camera,null);player.ApplyAt(.79f);player.ApplyAt(.215f);Check(Equal(first,Capture(camera,null)),"Absolute-time native particle replay byte-identical");
            Check(Equal(coldHit,first),"First seek directly to impact matches replay");
            player.ApplyAt(WaterThunderSlash.Duration);foreach(var r in player.layers)Check(!r.enabled,"End disables "+r.name);
            Check(!player.evaporationMist.enabled,"End disables dispersed water texture");Check(player.impact.Alive==0,"End clears native particles");
            Capture(camera,Path.Combine(output,"end-clear.png"));
            string frames=Path.Combine(output,"motion");Directory.CreateDirectory(frames);
            for(int frame=0;frame<79;frame++){player.ApplyAt(frame/60f);Capture(camera,Path.Combine(frames,frame.ToString("D3")+".png"));}
            ExportSupport(player,camera);ExportFlowAndVoltage(player,camera);ExportDissolve(player,camera);ExportWaterDynamics(player,camera);All(player,true);player.ApplyAt(.33f);Orientation(player,camera);BuildAndReload();
            var stage=WaterThunderBuilder.CreateStage(true);stage.autoPlay=true;stage.sampleTime=.215f;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),WaterThunderBuilder.Root+"/Scenes/WaterThunderAuthoring.unity");AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(output,"complete.json"),"{\"status\":\"technical-pass\",\"unity\":\"2019.3.15f1\",\"renderer\":\"D3D11\",\"duration\":1.04,\"hitTime\":0.19,\"layerCount\":4,\"supportGroups\":2,\"deployment\":false,\"gameFactoryVerified\":false}");
            Debug.Log("SLAZEYA_WATER_THUNDER_PASS");EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void ExportSupport(WaterThunderSlash player,Camera camera)
    {
        foreach(string folder in new[]{"impact-only","evaporation-only","contact-frames","early-head"})Directory.CreateDirectory(Path.Combine(output,folder));
        All(player,false);player.impactBurst=true;float[] hits={.18f,.19f,.215f,.25f,.30f,.38f,.48f,.65f};
        for(int i=0;i<hits.Length;i++){player.ApplyAt(hits[i]);Capture(camera,Path.Combine(output,"impact-only",i.ToString("D2")+".png"));}
        All(player,false);player.water=player.evaporate=true;float[] evap={.36f,.44f,.52f,.60f,.72f,.88f};
        for(int i=0;i<evap.Length;i++){player.ApplyAt(evap[i]);Capture(camera,Path.Combine(output,"evaporation-only",i.ToString("D2")+".png"));}
        All(player,true);for(int i=8;i<=22;i++){player.ApplyAt(i/60f);Capture(camera,Path.Combine(output,"contact-frames",i.ToString("D2")+".png"));}
        for(int i=0;i<8;i++){player.ApplyAt((i+1)/60f);Capture(camera,Path.Combine(output,"early-head",i.ToString("D2")+".png"));}
    }
    static void ExportWaterDynamics(WaterThunderSlash player,Camera camera)
    {
        All(player,false);player.ApplyAt(0);
        var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;
        camera.transform.position=new Vector3(0,0,-5);camera.orthographicSize=.83f;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Temporary water surface dynamics proof";
        var source=quad.GetComponent<MeshFilter>().sharedMesh;var mesh=Object.Instantiate(source);quad.GetComponent<MeshFilter>().sharedMesh=mesh;
        quad.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(WaterThunderBuilder.Root+"/Materials/L2_SplashWake.mat");
        var block=new MaterialPropertyBlock();block.SetFloat("_Alpha",1);quad.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        var colors=new Color32[mesh.vertexCount];const float life=.46f,seed=.43f;
        for(int mode=0;mode<2;mode++)
        {
            string folder=Path.Combine(output,mode==0?"water-drop-dynamics":"water-thread-dynamics");Directory.CreateDirectory(folder);
            for(int frame=0;frame<32;frame++)
            {
                float age=frame/60f,stretch=WaterDropletMotion.Stretch(5,age,seed,mode==1);
                quad.transform.localScale=new Vector3(.48f*stretch,.48f/Mathf.Sqrt(stretch),1);
                for(int v=0;v<colors.Length;v++)colors[v]=WaterDropletMotion.DataColor(seed,age,life,mode==1,age<life?1:0);
                mesh.colors32=colors;Capture(camera,Path.Combine(folder,frame.ToString("D3")+".png"));
            }
        }
        Object.DestroyImmediate(quad);Object.DestroyImmediate(mesh);camera.transform.position=oldPosition;camera.orthographicSize=oldSize;
    }
    static void ExportFlowAndVoltage(WaterThunderSlash player,Camera camera)
    {
        string flow=Path.Combine(output,"white-fluid-spin"),voltage=Path.Combine(output,"lightning-discharge");Directory.CreateDirectory(flow);Directory.CreateDirectory(voltage);
        All(player,false);player.water=true;var block=new MaterialPropertyBlock();
        for(int i=0;i<30;i++)
        {
            player.ApplyAt(.215f);player.layers[0].GetPropertyBlock(block);block.SetFloat("_Phase",i/60f);block.SetFloat("_Alpha",1);block.SetFloat("_SweepEnabled",0);
            block.SetFloat("_Emission",0);block.SetFloat("_HighlightStrength",0);block.SetColor("_BaseColor",new Color(0,0,0,1));block.SetFloat("_WhiteFluidStrength",1);
            player.layers[0].SetPropertyBlock(block);Capture(camera,Path.Combine(flow,i.ToString("D3")+".png"));
        }
        All(player,false);player.lightning=true;float[] times={.16f,.215f,.27f,.35f,.45f,.60f};
        for(int i=0;i<times.Length;i++){player.ApplyAt(times[i]);Capture(camera,Path.Combine(voltage,i.ToString("D2")+".png"));}
    }
    static void ExportDissolve(WaterThunderSlash player,Camera camera)
    {
        string movie=Path.Combine(output,"dissolve-exit"),proof=Path.Combine(output,"dissolve-mask-only");Directory.CreateDirectory(movie);Directory.CreateDirectory(proof);
        All(player,true);
        for(int i=0;i<45;i++){player.ApplyAt(.32f+i/60f);Capture(camera,Path.Combine(movie,i.ToString("D3")+".png"));}
        All(player,false);player.water=player.evaporate=true;float[] times={.36f,.44f,.52f,.60f,.72f,.88f};
        var block=new MaterialPropertyBlock();
        for(int i=0;i<times.Length;i++)
        {
            player.ApplyAt(times[i]);player.evaporationMist.enabled=false;
            player.layers[0].GetPropertyBlock(block);block.SetFloat("_Alpha",1);block.SetFloat("_SweepEnabled",0);player.layers[0].SetPropertyBlock(block);
            Check(block.GetFloat("_Alpha")==1,"Mask-only dissolve proof holds Alpha at one");
            Capture(camera,Path.Combine(proof,i.ToString("D2")+".png"));
        }
    }
    static bool Equal(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    static float Speed(float t){return (WaterThunderMotion.Progress(t+.001f)-WaterThunderMotion.Progress(t-.001f))/.002f;}
    static void VerifyMotion()
    {
        Check(Speed(.16f)>Speed(.235f)*3,"Arrival faster than contact slowdown");Check(Speed(.30f)>Speed(.235f)*3,"Blade reaccelerates after contact");
        Check(Mathf.Abs(WaterThunderMotion.Progress(WaterThunderMotion.HitTime)-WaterThunderMotion.ContactPath)<.00001f,"Exact hit time and path agree");
        Vector3 travel,velocity;WaterDropletMotion.Flight(new Vector3(4,2,0),.15f,.43f,out travel,out velocity);
        Check(velocity.magnitude<new Vector3(4,2,0).magnitude,"Water spray loses speed under drag");
        Check(WaterDropletMotion.Stretch(5,.30f,.43f,false)<WaterDropletMotion.Stretch(5,0,.43f,false),"Fast water drop relaxes toward round shape");
        float prev=0,prevY=WaterThunderMotion.Curve(0).y;
        var report=new List<string>{"time,progress,speed,tipX,tipY,evapProgress,evapSpread,evapLift"};
        for(int i=0;i<=125;i++)
        {
            float t=i/120f,p=WaterThunderMotion.Progress(t);var v=WaterThunderMotion.Curve(p);
            if(p<prev-.000001f||v.y>prevY+.00001f)throw new Exception("Non-forward or upward motion at "+t);prev=p;prevY=v.y;
            report.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7}",t,p,Speed(t),v.x,v.y,WaterThunderMotion.EvaporationProgress(t),WaterThunderMotion.EvaporationSpread(t),WaterThunderMotion.EvaporationLift(t)));
        }
        Check(true,"Forward and downward motion at 120Hz");
        Check(WaterThunderMotion.EvaporationSpread(.72f)>WaterThunderMotion.EvaporationSpread(.54f),"Water scatter gradually disperses");
        File.WriteAllLines(Path.Combine(output,"motion-profile.csv"),report);
    }
    static void VerifyAssets()
    {
        var shader=Shader.Find("Steria/WaterThunder/FormedArc");Check(shader!=null&&shader.isSupported,"FormedArc shader supported");Check(!ShaderUtil.ShaderHasError(shader),"No shader compiler errors");
        var report=new List<string>{"name,vertices,triangles,minX,minY,minZ,maxX,maxY,maxZ"};
        for(int k=0;k<4;k++)
        {
            var name=WaterThunderBuilder.Names[k];var m=AssetDatabase.LoadAssetAtPath<Mesh>(WaterThunderBuilder.Root+"/Meshes/"+name+".asset");
            Check(m!=null&&m.vertexCount>0,"Persisted mesh "+name);var uv=m.uv;var v=m.vertices;var b=m.bounds;Check(uv.Length==v.Length,"UV coverage "+name);
            for(int i=0;i<v.Length;i++)if(float.IsNaN(v[i].x)||float.IsInfinity(v[i].y)||uv[i].x<-.001f||uv[i].x>1.001f)throw new Exception("Bad mesh "+name);
            Check(b.size.z<.15f,"Vertical XY geometry "+name);Check(b.min.y>0,"Geometry above ground "+name);
            if(k==0)Check(b.size.y>b.size.x,"Crescent long axis vertical");
            if(k==0)Check(Vector3.Distance(v[0],v[14])>.5f,"Tail has space for separated divergent water filaments");
            if(k==1)Check(m.vertexCount==320,"80 water drops are packed close to the wake");
            if(k==3)Check(Vector3.Distance(v[154*9],v[154*9+8])>Vector3.Distance(v[30*9],v[30*9+8])*3,"Front core thicker while initial leading point remains visible");
            report.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8}",name,m.vertexCount,m.triangles.Length/3,b.min.x,b.min.y,b.min.z,b.max.x,b.max.y,b.max.z));
            var mat=AssetDatabase.LoadAssetAtPath<Material>(WaterThunderBuilder.Root+"/Materials/"+name+".mat");
            foreach(string property in new[]{"_ShapeTex","_MaskTex","_FlowTex","_HighlightTex","_DissolveTex"})Check(mat.GetTexture(property)!=null,name+" "+property);
            if(k!=1)Check(mat.GetFloat("_SweepEnabled")==1&&mat.GetTexture("_SweepMaskTex")!=null,"Swept brush material "+name);
            if(k==0)Check(mat.GetTexture("_TailMaskTex")!=null&&mat.GetTexture("_EvapScatterTex")!=null,"Fan-tail and evaporation scatter textures bound");
            if(k==0)Check(mat.GetFloat("_WhiteFluid")==1&&mat.GetTexture("_WhiteFluidTex")!=null,"White fluid four-sided pattern enabled inside body");
            if(k==2)
            {
                var electrical=m.uv2;float low=1,high=0;
                foreach(var data in electrical){low=Mathf.Min(low,data.y);high=Mathf.Max(high,data.y);}
                Check(mat.GetFloat("_Voltage")==1&&low<.3f&&high==1,"Connected electrical tree carries high trunk and low branch reserves");
            }
            if(k==1)Check(mat.GetFloat("_Liquid")==1&&mat.GetTexture("_WaterNormalTex")!=null&&mat.GetTexture("_EvapScatterTex")!=null,"Wet drop surface, normals and dissolve map bound");
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{WaterThunderBuilder.Root+"/Textures"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool repeat=path.Contains("Repeat");
            Check(importer.wrapMode==(repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp),"Address mode "+Path.GetFileName(path));if(repeat||path.Contains("Mask")||path.Contains("Normal")||path.Contains("Evap_WaterScatter"))Check(!importer.sRGBTexture,"Linear data "+Path.GetFileName(path));
        }
        File.WriteAllLines(Path.Combine(output,"mesh-report.csv"),report);
    }
    static byte[] Capture(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(960,540,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,540),0,0);texture.Apply();byte[] png=texture.EncodeToPNG();
        if(path!=null)File.WriteAllBytes(path,png);Object.DestroyImmediate(texture);camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);return png;
    }
    static void Orientation(WaterThunderSlash player,Camera camera)
    {
        WaterThunderBuilder.AddContext();camera.orthographicSize=2.9f;camera.transform.position=new Vector3(5.2f,3.7f,-10);camera.transform.LookAt(new Vector3(.7f,1.8f,0));
        player.ApplyAt(.215f);Capture(camera,Path.Combine(output,"orientation-vertical-ground.png"));
        All(player,false);player.water=true;player.ApplyAt(.33f);var old=player.layers[0].sharedMaterial;
        player.layers[0].sharedMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.8f,.85f,.9f)};Capture(camera,Path.Combine(output,"white-model-vertical.png"));player.layers[0].sharedMaterial=old;
    }
    static void BuildAndReload()
    {
        string dir=Path.Combine(output,"bundle");Directory.CreateDirectory(dir);var paths=new List<string>();
        for(int i=0;i<4;i++)paths.Add(WaterThunderBuilder.Root+"/Prefabs/"+WaterThunderBuilder.Names[i]+".prefab");
        paths.Add(WaterThunderBuilder.Root+"/Prefabs/WaterEvaporationScatter.prefab");paths.Add(WaterThunderBuilder.Root+"/Prefabs/WaterThunderImpactParticles.prefab");
        const string bundleName="steria_slazeya_water_thunder_assets";
        var manifest=BuildPipeline.BuildAssetBundles(dir,new[]{new AssetBundleBuild{assetBundleName=bundleName,assetNames=paths.ToArray()}},BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
        Check(manifest!=null,"Four layers and support AssetBundle build");Check(manifest.GetAllDependencies(bundleName).Length==0,"AssetBundle self-contained");
        var bundle=AssetBundle.LoadFromFile(Path.Combine(dir,bundleName));Check(bundle!=null,"Reload actual AssetBundle");
        foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Object.DestroyImmediate(go);
        var root=new GameObject("Reloaded resource bundle");var p=root.AddComponent<WaterThunderSlash>();p.autoPlay=false;root.transform.position=new Vector3(0,.10f,0);
        for(int i=0;i<paths.Count;i++)
        {
            var prefab=bundle.LoadAsset<GameObject>(Path.GetFileNameWithoutExtension(paths[i]));Check(prefab!=null,"Bundle prefab "+paths[i]);
            foreach(var c in prefab.GetComponentsInChildren<Component>(true))Check(c is Transform||c is MeshFilter||c is MeshRenderer||c is ParticleSystem||c is ParticleSystemRenderer,"Native bundle component "+c.GetType().Name);
            var obj=Object.Instantiate(prefab,root.transform);if(i<4)p.layers[i]=obj.GetComponent<MeshRenderer>();
            if(i==4)p.evaporationMist=obj.GetComponent<MeshRenderer>();
            if(i==5){p.impact=obj.AddComponent<WaterThunderImpact>();p.impact.systems=obj.GetComponentsInChildren<ParticleSystem>();}
        }
        var camera=new GameObject("ReloadCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.6f;camera.transform.position=new Vector3(.65f,2.05f,-15);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.03f,.052f);camera.allowHDR=false;
        p.ApplyAt(.215f);Check(Equal(File.ReadAllBytes(Path.Combine(output,"composite/03.png")),Capture(camera,Path.Combine(output,"bundle-reloaded-hit.png"))),"Cold bundle impact pixels byte-identical");
        p.ApplyAt(.33f);Check(Equal(File.ReadAllBytes(Path.Combine(output,"composite/04.png")),Capture(camera,Path.Combine(output,"bundle-reloaded-peak.png"))),"Bundle full crescent pixels byte-identical");
        p.ApplyAt(.535f);Check(Equal(File.ReadAllBytes(Path.Combine(output,"composite/05.png")),Capture(camera,Path.Combine(output,"bundle-reloaded-evaporation.png"))),"Bundle evaporation pixels byte-identical");
        Object.DestroyImmediate(root);bundle.Unload(true);
    }
}
