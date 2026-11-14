using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using Steria.Vfx;

[InitializeOnLoad]
public static class SlazeyaWindPreview
{
    const string Key="SlazeyaWind.Output";
    const float MapScale=3f;
    [Serializable]class Fixture {public Motion[] motions;}
    [Serializable]class Motion {public int action;public Part[] parts;}
    [Serializable]class Part {public string file;public float ppu;public float[] pivot,position,scale;public int order;}
    static SlazeyaWindPreview(){EditorApplication.playModeStateChanged+=State;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void BuildAndPreview()
    {
        try
        {
            Check(Application.unityVersion=="2019.3.15f1","Exact editor required");
            Check(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,"D3D11 required");
            var args=Environment.GetCommandLineArgs();string output=args[Array.IndexOf(args,"-windOutput")+1];
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            SlazeyaWindSweptBuilder.Build();Directory.CreateDirectory("AssetBundles");
            var paths=new List<string>();foreach(SlazeyaWindKind k in Enum.GetValues(typeof(SlazeyaWindKind)))paths.Add(SlazeyaWindParticleBuilder.PrefabPath(k));
            var manifest=BuildPipeline.BuildAssetBundles("AssetBundles",new[]{new AssetBundleBuild{assetBundleName=SlazeyaWindVisual.BundleName,assetNames=paths.ToArray()}},BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
            Check(manifest!=null,"AB build failed");Check(manifest.GetAllDependencies(SlazeyaWindVisual.BundleName).Length==0,"External dependencies");
            File.Copy("AssetBundles/"+SlazeyaWindVisual.BundleName,Path.Combine(output,SlazeyaWindVisual.BundleName));
            File.WriteAllText(Path.Combine(output,"coverage.txt"),
                "Unity 2019.3.15f1 / Built-in Gamma / D3D11. Actual bundle, production driver and native PS. " +
                "Bada body images/matrices/pivots/PPU from the equipped skin, map S=3, roll=0. " +
                "Bada built-in Effect9 layers omitted to isolate this candidate. Projectile uses Bada Default as a labelled Fire-pose approximation. " +
                "Synthetic positions/camera; not a full game battle. Both facings, dark/mid backgrounds, fixed-seed replay, pause/cancel and live Update smoke.\n");
            SessionState.SetString(Key,output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;
        }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        var output=SessionState.GetString(Key,"");if(output=="")return;SessionState.EraseString(Key);
        try
        {
            var bundle=AssetBundle.LoadFromFile(Path.Combine(output,SlazeyaWindVisual.BundleName));Check(bundle!=null,"AB read failed");
            foreach(SlazeyaWindKind kind in Enum.GetValues(typeof(SlazeyaWindKind)))
            {
                var prefab=bundle.LoadAsset<GameObject>("SlazeyaWind"+kind);Check(prefab!=null,"Missing "+kind);
                foreach(var c in prefab.GetComponentsInChildren<Component>(true))Check(c is Transform||c is ParticleSystem||c is ParticleSystemRenderer,"Unexpected bundle component "+c.GetType());
                foreach(var r in prefab.GetComponentsInChildren<ParticleSystemRenderer>())Check(r.sharedMaterial.shader.isSupported,"Shader unsupported");
                foreach(bool mirrored in new[]{false,true})foreach(bool mid in new[]{false,true})Export(prefab,kind,output,mirrored,mid);
                if(kind==SlazeyaWindKind.Pierce)
                {
                    Export(prefab,kind,output,false,false,false);
                    VerifyPierce(prefab,output);
                }
            }
            var smoke=new GameObject("ActualPlayerLoopSmoke").AddComponent<SlazeyaWindFrameSmoke>();
            smoke.Initialize(bundle.LoadAsset<GameObject>("SlazeyaWindSlash"),ok=>{if(ok){Debug.Log("SLAZEYA_WIND_NATIVE_PASS");EditorApplication.Exit(0);}else EditorApplication.Exit(1);});
        }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    public static void Actor(int action,bool mirrored,float x)
    {
        var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText("Assets/Reference/bada-fixture.json"));
        var motion=Array.Find(fixture.motions,m=>m.action==action);
        var root=new GameObject("Bada-real-matrix-PPU-pivot");root.transform.position=new Vector3(x,0,0);root.transform.localScale=new Vector3(mirrored?-MapScale:MapScale,MapScale,MapScale);
        foreach(var p in motion.parts)
        {
            var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes("Assets/Reference/"+p.file));
            var go=new GameObject(p.file);go.transform.SetParent(root.transform,false);
            go.transform.localPosition=new Vector3(p.position[0],p.position[1],1);go.transform.localScale=new Vector3(p.scale[0],p.scale[1],1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(p.pivot[0],p.pivot[1]),p.ppu);
            sr.sortingOrder=-10+p.order;
        }
    }
    public static Camera MakeCamera(bool mid)
    {
        var c=new GameObject("NativeGPUCamera").AddComponent<Camera>();c.orthographic=true;c.orthographicSize=3.8f*MapScale;
        c.transform.position=new Vector3(0,2.55f*MapScale,-80);c.backgroundColor=mid?new Color(.31f,.31f,.31f):new Color(.035f,.048f,.058f);
        c.clearFlags=CameraClearFlags.SolidColor;c.allowHDR=false;c.allowMSAA=true;c.nearClipPlane=.1f;c.farClipPlane=160;return c;
    }
    static void Save(Camera c,string path)
    {
        var rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
        var t=new Texture2D(960,540,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,960,540),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);
        c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
    }
    static void Export(GameObject prefab,SlazeyaWindKind kind,string output,bool mirrored,bool mid,bool confirmed=true)
    {
        foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Object.DestroyImmediate(go);
        string name=kind+"-"+(mid?"mid":"dark")+"-"+(mirrored?"right":"left")+(confirmed?"":"-miss");var dir=Path.Combine(output,name);Directory.CreateDirectory(dir);
        float actorX=(kind==SlazeyaWindKind.Projectile||kind==SlazeyaWindKind.Pierce?4:1.5f)*MapScale*(mirrored?-1:1);
        Actor(kind==SlazeyaWindKind.Slash?4:kind==SlazeyaWindKind.Hit?6:kind==SlazeyaWindKind.Pierce?5:kind==SlazeyaWindKind.Guard?1:0,mirrored,actorX);
        if(kind==SlazeyaWindKind.Projectile)Actor(1,!mirrored,-actorX);
        if(kind==SlazeyaWindKind.Pierce)Actor(confirmed?7:1,!mirrored,-actorX);
        var camera=MakeCamera(mid);
        var instance=Object.Instantiate(prefab);float scale=kind==SlazeyaWindKind.Guard||kind==SlazeyaWindKind.Projectile?1:2;
        if(kind!=SlazeyaWindKind.Projectile)scale*=MapScale;
        Vector3 anchor=new Vector3(actorX+(mirrored?2.2f:-2.2f)*MapScale,2.34f*MapScale,0);
        if(kind!=SlazeyaWindKind.Projectile)
        {
            var attachment=new GameObject("NativeScaleAndFacing").transform;
            attachment.position=anchor;attachment.localScale=new Vector3(mirrored?-scale:scale,scale,scale);
            instance.transform.SetParent(attachment,false);instance.transform.localPosition=Vector3.zero;instance.transform.localScale=Vector3.one;
        }
        else{instance.transform.position=anchor;instance.transform.localScale=Vector3.one*scale;}
        if(kind==SlazeyaWindKind.Projectile)instance.transform.rotation=Quaternion.Euler(0,0,mirrored?180:0);
        var visual=new SlazeyaWindVisual(instance,kind,19037u);
        if(kind==SlazeyaWindKind.Pierce)
        {
            Vector3 contact=new Vector3(-actorX+(mirrored?-.35f:.35f)*MapScale,2.34f*MapScale,0);
            Vector3 exit=new Vector3(-actorX+(mirrored?1:-1)*MapScale*1.6f,contact.y,0);
            visual.AimPierce(contact,exit,MapScale*1.5f);
            if(confirmed)visual.ConfirmPierceImpact();
        }
        var rows=new System.Text.StringBuilder("frame,time,particles,x,y\n");
        int children=instance.GetComponentsInChildren<Transform>().Length;
        for(int f=0;f<=(kind==SlazeyaWindKind.Pierce?48:36);f++)
        {
            if(f>0)visual.Advance(1f/60);
            if(kind==SlazeyaWindKind.Projectile)
            {
                float u=Mathf.Clamp01(visual.Elapsed/.26f);u=1-(1-u)*(1-u);
                instance.transform.position=new Vector3(Mathf.Lerp(mirrored?-1.8f:1.8f,mirrored?1.8f:-1.8f,u)*MapScale,2.34f*MapScale,0);
                if(visual.Elapsed>=.26f)visual.Arrive();
            }
            Save(camera,Path.Combine(dir,"frame_"+f.ToString("D3")+".png"));
            if(f==7&&kind==SlazeyaWindKind.Pierce&&!mirrored&&!mid&&confirmed)
            {
                var renderer=instance.transform.Find("Main").GetComponent<ParticleSystemRenderer>();
                var original=renderer.sharedMaterial;var clay=new Material(Shader.Find("Unlit/Color"));clay.color=Color.white;
                renderer.sharedMaterial=clay;Save(camera,Path.Combine(output,"Pierce-white-model.png"));renderer.sharedMaterial=original;Object.DestroyImmediate(clay);
            }
            rows.AppendLine(f+","+visual.Elapsed.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+visual.LiveParticleCount+","+instance.transform.position.x+","+instance.transform.position.y);
        }
        Check(visual.IsComplete&&visual.LiveParticleCount==0,"Completion cleanup "+kind);
        visual.ApplyAt(.10f);int before=visual.LiveParticleCount;Check(before>0,"No live particles "+kind);
        Save(camera,Path.Combine(dir,"replay-a.png"));visual.Advance(0);Check(visual.LiveParticleCount==before,"Pause changed particles");
        visual.ApplyAt(.10f);Save(camera,Path.Combine(dir,"replay-b.png"));
        Check(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir,"replay-a.png")))==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir,"replay-b.png"))),"Replay pixels differ "+kind);
        Check(children==instance.GetComponentsInChildren<Transform>().Length,"Replay grew children");
        visual.StopAndClear();visual.StopAndClear();Check(visual.LiveParticleCount==0,"Cancel particles remain");
        File.WriteAllText(Path.Combine(dir,"metrics.csv"),rows.ToString());
        Debug.Log("[CASE] "+name+" completion/pause/replay/cancel PASS");
    }

    static void VerifyPierce(GameObject prefab,string output)
    {
        var instance=Object.Instantiate(prefab);instance.transform.position=new Vector3(6,7,0);instance.transform.localScale=Vector3.one*6;
        var visual=new SlazeyaWindVisual(instance,SlazeyaWindKind.Pierce,19037u);
        Vector3 contact=new Vector3(-6,7,0),exit=new Vector3(-15,7,0);
        visual.AimPierce(contact,exit,4.5f);visual.Advance(.10f);
        Check(!visual.PierceImpactOccurred&&instance.transform.Find("ImpactRing").GetComponent<ParticleSystem>().particleCount==0,"Ring before hit");
        Check(visual.ConfirmPierceImpact()&&!visual.ConfirmPierceImpact(),"Impact confirmation must be one-shot");
        visual.Advance(.025f);Check(visual.PierceImpactOccurred,"No ring at confirmed contact");
        var ring=instance.transform.Find("ImpactRing");Check(ring.GetComponent<ParticleSystem>().particleCount==1,"Ring duplicate or missing");
        Vector3 anchor=ring.position;float small=ring.lossyScale.y;
        instance.transform.position+=new Vector3(.5f,.2f,0);visual.Advance(.001f);
        Check(Vector3.Distance(anchor,ring.position)<.001f,"Ring drifted on sub-tick frame");
        instance.transform.position+=new Vector3(2,1,0);visual.Advance(.10f);
        Check(Vector3.Distance(anchor,ring.position)<.001f,"Ring followed attacker");
        Check(ring.lossyScale.y>small*1.5f,"Ring did not expand");
        Check(visual.PierceTipWorld.x<contact.x,"Thrust never penetrated behind contact");
        visual.StopAndClear();Check(visual.LiveParticleCount==0,"Cancel retained impact");
        visual.ApplyAt(.4f);Check(!visual.PierceImpactOccurred&&ring.GetComponent<ParticleSystem>().particleCount==0,"Miss or replay produced a hit ring");
        visual.StopAndClear();Object.DestroyImmediate(instance);
        File.WriteAllText(Path.Combine(output,"pierce-contract.txt"),"PASS: reach/confirmed gate/one-shot ring/expansion/world anchor/follow-through/cancel/miss replay\n");
    }
}
