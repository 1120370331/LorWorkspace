using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AceTaffy;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

// Read actual BaseMod artwork, then resolve the exact production buff keys
// through the real BaseMod prefix and native GetBufIcon implementation.
public static class InventionIconFixture
{
    public static void Run()
    {
        string output=Environment.GetEnvironmentVariable("TAFFY_RECEPTION_OUTPUT");
        try
        {
            string ace=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../.."));
            string artwork=Path.Combine(ace,"AceTaffyMod/ArtWork");
            var patchType=typeof(BaseMod.Harmony_Patch);
            AccessTools.Field(patchType,"ArtWorks").SetValue(null,new Dictionary<string,Sprite>());
            AccessTools.Method(patchType,"GetArtWorks",new[]{typeof(DirectoryInfo)})
                .Invoke(null,new object[]{new DirectoryInfo(artwork)});
            var dict=(Dictionary<string,Sprite>)AccessTools.Field(patchType,"ArtWorks").GetValue(null);
            AccessTools.Field(typeof(BattleUnitBuf),"_bufIconDictionary").SetValue(null,new Dictionary<string,Sprite>());
            new Harmony("lor.acetaffy.invention.icons.fixture").Patch(
                AccessTools.Method(typeof(BattleUnitBuf),"GetBufIcon"),
                prefix:new HarmonyMethod(AccessTools.Method(patchType,"BattleUnitBuf_GetBufIcon_Pre")));

            var camera=new GameObject("Icon acceptance camera").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=3.5f;
            camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.15f,.18f);
            var rt=new RenderTexture(1200,700,24);camera.targetTexture=rt;
            var hashes=new HashSet<string>();
            var evidence=new List<string>();
            Check(new BattleUnitBuf_AceTaffyInventionProgress(),"AceTaffy_InventionProgress",dict,artwork,hashes,evidence,-5.5f,3.0f);
            for(int q=0;q<5;q++)
                Check(new BattleUnitBuf_AceTaffyInventionAura {Qualifier=(InventionQualifier)q,stack=1},
                    "AceTaffy_InventionAura_"+q,dict,artwork,hashes,evidence,-3.6f+q*1.8f,3.0f);
            for(int q=0;q<5;q++)
            for(int k=0;k<5;k++)
            {
                var device=new TaffyInvention((InventionQualifier)q,(InventionKind)k);
                var buf=new BattleUnitBuf_AceTaffyInvention {Device=device,stack=3};
                Check(buf,device.Key,dict,artwork,hashes,evidence,-5.3f+k*2.3f,2.2f-q*1.2f);
            }
            if(hashes.Count!=31)throw new Exception("Invention and aura icons must all be distinct");
            camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1200,700,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,1200,700),0,0);image.Apply();RenderTexture.active=null;
            File.WriteAllBytes(Path.Combine(output,"icons-native.png"),image.EncodeToPNG());
            File.WriteAllLines(Path.Combine(output,"icon-hashes.txt"),evidence.ToArray());
            Debug.Log("RECEPTION_COVERAGE icons only: actual BaseMod directory loader, icon prefix, native GetBufIcon, 64/32/24px Camera.Render; no full battle replay");
            Debug.Log("RECEPTION_PASS 31 distinct RGBA icons resolved by production buff keys");
            EditorApplication.Exit(0);
        }
        catch(Exception ex)
        {
            File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());
            Debug.LogError("RECEPTION_FAIL "+ex);EditorApplication.Exit(1);
        }
    }

    static void Check(BattleUnitBuf buf,string key,Dictionary<string,Sprite> loaded,string artwork,
        HashSet<string> hashes,List<string> evidence,float x,float y)
    {
        Sprite sprite=buf.GetBufIcon();
        if(sprite==null || !loaded.ContainsKey(key) || sprite!=loaded[key])throw new Exception("Wrong icon binding: "+key);
        if(sprite.texture.width!=128 || sprite.texture.height!=128)throw new Exception("Wrong size: "+key);
        if(sprite.texture.GetPixel(0,0).a!=0)throw new Exception("Opaque corner: "+key);
        using(var sha=SHA256.Create())
        {
            string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(artwork,key+".png")))).Replace("-","");
            hashes.Add(hash);evidence.Add(key+" "+hash);
        }
        foreach(int size in new[]{64,32,24})
        {
            var renderer=new GameObject(key+"_"+size).AddComponent<SpriteRenderer>();
            renderer.sprite=sprite;renderer.transform.position=new Vector3(x,y,0);
            renderer.transform.localScale=Vector3.one*(size/128f);x+=size/100f+.1f;
        }
        Debug.Log("RECEPTION_CHECK bound "+key);
    }
}
