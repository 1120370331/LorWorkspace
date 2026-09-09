using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Steria;

public static class AnhierTexturePreview
{
    private static readonly string Here=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
    private static readonly string Repo=Path.GetFullPath(Path.Combine(Here,"../../.."));
    private static readonly string Output=Path.Combine(Here,"previews");
    private static Camera camera;
    private static RenderTexture rt;
    private static GameObject stage;
    private static List<string> checks=new List<string>();
    private static string[] Profiles={"SeaPierce","SeaFarHit","MemorySlash","MemoryPierce","MemoryHit","MemoryGuard"};
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Output);
            string resources=Path.Combine(Repo,"SteriaBuild/SteriaModFolder/Resource/Effects/AnhierTextureCombat");
            foreach(string profile in Profiles)
            {
                var assets=AnhierTextureAssets.Load(resources,profile);
                Check(object.ReferenceEquals(assets,AnhierTextureAssets.Load(resources,profile)),profile+" shared decoded cache");
                Scene(false,false);
                var go=new GameObject(profile);var effect=go.AddComponent<AnhierTextureTimeline>();effect.Initialize(assets,0,20);
                // Native char_418 AtkEffectRoot = (-2.2,2.34,0); its action pivots
                // use that root. Character default source faces LEFT. Mirror once.
                var origin=new Vector3(-1.3f,1.19f,-.1f);var target=new Vector3(4.5f,1.19f,-.1f);
                string frames=Path.Combine(Output,profile);Directory.CreateDirectory(frames);
                for(int i=0;i<31;i++)
                {
                    effect.Sample(i/60f,origin,target,false,1,0);
                    Save(Path.Combine(frames,i.ToString("00")+".png"));
                }
                effect.Sample(.034f,origin,target,false,1,0);
                Check(effect.CurrentFrame==2,profile+" visible peak at 34 ms");
                effect.Sample(1f/60f,origin,target,false,1,0);
                Check(effect.CurrentFrame==1,profile+" first unfolding frame retained at exactly 60 fps");
                effect.Sample(.034f,origin,target,false,1,0);
                var renderers=go.GetComponentsInChildren<SpriteRenderer>();
                Check(renderers.Length<=3,profile+" <= 3 renderers");
                foreach(var sr in renderers)Check(sr.sharedMaterial==AnhierTextureAssets.AlphaMaterial,profile+" shared material "+sr.name);
                var secondGo=new GameObject("IndependentInstance");var second=secondGo.AddComponent<AnhierTextureTimeline>();second.Initialize(assets,0,40);
                second.Sample(.29f,new Vector3(1,2,-.2f),new Vector3(-3,2,-.2f),true,1,2);
                Check(effect.CurrentFrame==2,profile+" concurrent phase isolation");
                Check(renderers[0].color==Color.white,profile+" concurrent color isolation");
                UnityEngine.Object.DestroyImmediate(secondGo);
                effect.Sample(.6f,origin,target,false,1,0);
                Check(effect.Complete,profile+" ends at authored lifetime");
                foreach(var sr in renderers)Check(!sr.enabled,profile+" end renderer disabled "+sr.name);
                // Two opposite facings, contrasting background; same world scale and pivots.
                UnityEngine.Object.DestroyImmediate(go);Scene(true,true);
                go=new GameObject(profile+"_left");effect=go.AddComponent<AnhierTextureTimeline>();effect.Initialize(assets,0,20);
                effect.Sample(.034f,new Vector3(1.3f,1.19f,-.1f),new Vector3(-4.5f,1.19f,-.1f),true,1,0);
                Save(Path.Combine(Output,profile+"_left_light.png"));
                effect.AccentsEnabled=false;
                SaveSample(effect,.034f,new Vector3(1.3f,1.19f,-.1f),new Vector3(-4.5f,1.19f,-.1f),Path.Combine(Output,profile+"_body_only.png"));
                UnityEngine.Object.DestroyImmediate(go);
            }
            bool failed=false;
            try{AnhierTextureAssets.Load(Path.Combine(Output,"missing"),"MemorySlash");}catch(IOException){failed=true;}
            Check(failed,"missing resource yields actionable exception without blank cached entry");
            File.WriteAllText(Path.Combine(Output,"checks.txt"),string.Join("\n",checks.ToArray()));
            Debug.Log("ANHIER_TEXTURE_FIXTURE_PASS "+checks.Count+" checks");
            EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);File.WriteAllText(Path.Combine(Output,"failure.txt"),ex.ToString());EditorApplication.Exit(1);}
    }
    private static void SaveSample(AnhierTextureTimeline e,float time,Vector3 from,Vector3 to,string file){e.Sample(time,from,to,true,1,0);Save(file);}
    private static void Check(bool pass,string message){if(!pass)throw new Exception("FAIL "+message);checks.Add("PASS "+message);}
    private static Sprite LoadSprite(string name,float ppu,Vector2 pivot)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Reference",name+".png")));t.filterMode=FilterMode.Bilinear;
        return Sprite.Create(t,new Rect(0,0,t.width,t.height),pivot,ppu);
    }
    private static void Scene(bool light,bool left)
    {
        if(stage!=null)UnityEngine.Object.DestroyImmediate(stage);
        stage=new GameObject("FixtureStage");
        if(camera==null)
        {
            camera=new GameObject("FixtureCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;
            camera.transform.position=new Vector3(0,2.1f,-20);camera.clearFlags=CameraClearFlags.SolidColor;
            rt=new RenderTexture(960,540,24);camera.targetTexture=rt;
        }
        camera.backgroundColor=light?new Color(.82f,.81f,.77f):new Color(.06f,.07f,.10f);
        if(!light)
        {
            var background=new GameObject("IllustratedFixtureBackground");background.transform.SetParent(stage.transform,false);
            var sr=background.AddComponent<SpriteRenderer>();sr.sprite=LoadSprite("fixture-library-background",60,new Vector2(.5f,.5f));sr.sortingOrder=-20;
            background.transform.position=new Vector3(0,2.1f,2);
        }
        var actor=new GameObject("NativeBadaDefault_ReferenceOnly");actor.transform.SetParent(stage.transform,false);
        var body=actor.AddComponent<SpriteRenderer>();body.sprite=LoadSprite("bada-default-reference",100,new Vector2(.5f,50f/650f));body.sortingOrder=0;
        actor.transform.position=new Vector3(left?3.5f:-3.5f,-1.15f,0);actor.transform.localScale=new Vector3(left?1:-1,1,1);
        var target=new GameObject("TargetScaleMarker");target.transform.SetParent(stage.transform,false);
        var tr=target.AddComponent<SpriteRenderer>();tr.sprite=body.sprite;tr.color=light?new Color(.2f,.23f,.27f,.28f):new Color(.64f,.68f,.77f,.35f);tr.sortingOrder=0;
        target.transform.position=new Vector3(left?-4.5f:4.5f,-1.15f,.1f);target.transform.localScale=new Vector3(left?-1:1,1,1);
    }
    private static void Save(string file)
    {
        camera.Render();var before=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
        File.WriteAllBytes(file,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=before;
    }
}
