using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Steria.Vfx;
using Object=UnityEngine.Object;

// Local-only isolated original-data replay. Never starts the game or touches desktop UI.
public static class ChristashaNativeMassPreview
{
    static string Arg(string key){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,key)+1];}
    public static void Render()
    {
        try
        {
            string output=Arg("-candidateOutput");Directory.CreateDirectory(output);
            var bundle=AssetBundle.LoadFromFile(Arg("-referenceBundle"));
            if(bundle==null)throw new Exception("Reference bundle missing");
            Func<string,GameObject> resolver=path=>bundle.LoadAsset<GameObject>("assets/native_reference/"+path.Substring(path.LastIndexOf('/')+1)+".prefab");
            var sources=new[]{resolver(ChristashaNativeMassRecipes.AreaResource),resolver(ChristashaNativeMassRecipes.CastResource),resolver(ChristashaNativeMassRecipes.PointResource)};
            string sourceBefore=SourceSnapshot(sources);
            var brush=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            brush.LoadImage(File.ReadAllBytes("Assets/NativeRuntime/tide-brush-imagegen-v1.png"),false);
            using(var library=new ChristashaNativeMassRecipes(resolver,brush))
            {
                foreach(var recipe in new[]{"Cast","Area","Point"})
                {
                    var instance=library.CreatePrototype(recipe);
                    instance.name="AdapterRenamed_"+recipe;
                    Check(instance,Path.Combine(output,recipe+"-checks.txt"));
                    Capture(new[]{new Event(instance,0)},Path.Combine(output,recipe),recipe=="Area"?20:recipe=="Cast"?5.5f:4,recipe=="Area"?new Vector3(0,6,-100):new Vector3(0,0,-100),recipe=="Area"?84:48);
                    Object.DestroyImmediate(instance);
                }
                var events=new List<Event>();
                var cast=library.CreatePrototype("Cast");cast.transform.position=new Vector3(-16,3,0);events.Add(new Event(cast,0));
                var area=library.CreatePrototype("Area");events.Add(new Event(area,.3f));
                foreach(float x in new[]{-3f,5f,13f}){var point=library.CreatePrototype("Point");point.transform.position=new Vector3(x,3,-.6f);events.Add(new Event(point,.5f));}
                Capture(events.ToArray(),Path.Combine(output,"sequence"),20,new Vector3(0,6,-100),96);
                foreach(var e in events)Object.DestroyImmediate(e.root);
                var stage=new GameObject("NativeOnlyInactiveStage");stage.SetActive(false);
                for(int i=0;i<2;i++)
                {
                    var original=ChristashaNativeParticleLibrary.CloneVisualOnly(sources[i],stage.transform);original.SetActive(false);stage.SetActive(true);
                    // Exact original data at the SAME camera as the themed variant; note explicit art scale differences.
                    Capture(new[]{new Event(original,0)},Path.Combine(output,i==0?"original-area":"original-cast"),i==0?20:5.5f,i==0?new Vector3(0,6,-100):new Vector3(0,0,-100),84);
                    Object.DestroyImmediate(original);stage.SetActive(false);
                }
                Object.DestroyImmediate(stage);
            }
            if(sourceBefore!=SourceSnapshot(sources))throw new Exception("Installed source mutated");
            Object.DestroyImmediate(brush);bundle.Unload(true);
            File.WriteAllText(Path.Combine(output,"complete.txt"),"PASS: Unity "+Application.unityVersion+" / "+SystemInfo.graphicsDeviceType+" compile and original-data Editor replay; no source mutation. Not game combat acceptance.");
            Debug.Log("CHRISTASHA_MASS_PREVIEW_PASS");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    sealed class Event
    {
        public GameObject root;public float start;public ChristashaNativeParticlePlayback play;
        public Event(GameObject root,float start){this.root=root;this.start=start;}
    }
    static string SourceSnapshot(GameObject[] sources)
    {
        var lines=new List<string>();
        foreach(var source in sources)
        {
            foreach(var t in source.GetComponentsInChildren<Transform>(true))lines.Add(EditorJsonUtility.ToJson(t));
            foreach(var p in source.GetComponentsInChildren<ParticleSystem>(true))lines.Add(EditorJsonUtility.ToJson(p));
            foreach(var r in source.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m!=null)lines.Add(EditorJsonUtility.ToJson(m));
        }
        return string.Join("\n",lines.ToArray());
    }
    static void Check(GameObject root,string path)
    {
        if(root.activeSelf)throw new Exception("Factory returned active root");
        if(root.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||root.GetComponentsInChildren<AudioSource>(true).Length!=0)throw new Exception("Game/audio components");
        int count=root.GetComponentsInChildren<Transform>(true).Length;
        var a=ChristashaNativeParticlePlayback.Play(root,19037u);a.Advance(.2f);string first=Snapshot(root);a.StopAndClear();
        foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))if(p.particleCount!=0)throw new Exception("Cancel did not clear "+p.name);
        var b=ChristashaNativeParticlePlayback.Play(root,19037u);b.Advance(.2f);string second=Snapshot(root);b.StopAndClear();
        if(first!=second){File.WriteAllText(path+".first",first);File.WriteAllText(path+".second",second);throw new Exception("Same-seed replay differs "+root.name);}
        if(root.GetComponentsInChildren<Transform>(true).Length!=count)throw new Exception("Play grows hierarchy");
        root.SetActive(false);File.WriteAllText(path,"PASS inactive factory, renamed-root marker dispatch, no Mono/audio, cancel clears every PS, same-seed replay including MPBs/transforms, no hierarchy growth\n"+first);
    }
    static string Snapshot(GameObject root)
    {
        var lines=new List<string>();
        foreach(var t in root.GetComponentsInChildren<Transform>(true))lines.Add(t.name+t.localPosition.ToString("F4")+t.localScale.ToString("F4")+t.localEulerAngles.ToString("F4"));
        foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var particles=new ParticleSystem.Particle[Mathf.Max(1,p.main.maxParticles)];int n=p.GetParticles(particles);lines.Add(p.name+" "+n);
            for(int i=0;i<n;i++)lines.Add(particles[i].position.ToString("F4")+particles[i].remainingLifetime.ToString("F4"));
            var block=new MaterialPropertyBlock();p.GetComponent<Renderer>().GetPropertyBlock(block);lines.Add(block.GetColor("_Color")+" "+block.GetColor("_MainColor"));
        }
        return string.Join("\n",lines.ToArray());
    }
    static void Capture(Event[] events,string folder,float ortho,Vector3 cameraPosition,int frames)
    {
        Directory.CreateDirectory(folder);
        var metadata=new List<string>{"Original data Editor replay / theme. Unity2019.3.15 Built-in Gamma D3D11; same conversion/shaders/manual120Hz clock. No desktop or game capture.","Camera ortho="+ortho+" position="+cameraPosition+". 960x540 60fps. Area art native scale=(6.4,7.6,6.4), cast scale=.14; no postprocess."};
        foreach(var e in events)foreach(var t in e.root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var cg=new GameObject("Mass_OffscreenCamera");var cam=cg.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=ortho;cam.aspect=16f/9;cam.nearClipPlane=.01f;cam.farClipPlane=500;cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.transform.position=cameraPosition;cam.allowHDR=true;
        var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);rt.Create();cam.targetTexture=rt;
        var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        var timeline=new List<string>{"frame,time,particles"};
        for(int f=0;f<=frames;f++)
        {
            float age=f/60f;int count=0;
            foreach(var e in events)
            {
                if(e.play==null&&age+.000001f>=e.start)e.play=ChristashaNativeParticlePlayback.Play(e.root,19037u);
                else if(e.play!=null)e.play.Advance(1f/60);
                foreach(var p in e.root.GetComponentsInChildren<ParticleSystem>(true))count+=p.particleCount;
            }
            timeline.Add(f+","+age+","+count);
            if(new[]{0,1,3,6,12,18,24,36,48}.Contains(f))foreach(var e in events)foreach(var r in e.root.GetComponentsInChildren<Renderer>(true))
            {
                var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
                metadata.Add("f="+f+" "+r.name+" bounds="+r.bounds+" MPB Color="+block.GetColor("_Color")+" Main="+block.GetColor("_MainColor")+" Dissolve="+block.GetColor("_DissolveColor"));
                if(f==1)foreach(var m in r.sharedMaterials)if(m!=null){metadata.Add("  "+m.name+" shader="+m.shader.name+" supported="+m.shader.isSupported);if(!m.shader.isSupported)throw new Exception("Unsupported "+m.shader.name);}
            }
            for(int bg=0;bg<2;bg++)
            {
                cam.backgroundColor=bg==0?new Color(.035f,.045f,.06f):new Color(.32f,.34f,.37f);cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,(bg==0?"dark_":"mid_")+f.ToString("D4")+".png"),pixels.EncodeToPNG());
            }
            if(f==12&&Path.GetFileName(folder)=="Area")
            {
                var renderers=events.SelectMany(e=>e.root.GetComponentsInChildren<Renderer>(true)).ToArray();
                var states=renderers.Select(r=>r.enabled).ToArray();
                foreach(var r in renderers)r.enabled=false;
                for(int index=0;index<renderers.Length;index++)
                {
                    var r=renderers[index];if(!states[index])continue;r.enabled=true;
                    cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"layer_"+r.name+".png"),pixels.EncodeToPNG());r.enabled=false;
                }
                for(int index=0;index<renderers.Length;index++)renderers[index].enabled=states[index];
            }
        }
        foreach(var e in events){if(e.play!=null)e.play.StopAndClear();e.root.SetActive(false);}
        File.WriteAllLines(Path.Combine(folder,"metadata.txt"),metadata);File.WriteAllLines(Path.Combine(folder,"timeline.csv"),timeline);
        cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(rt);Object.DestroyImmediate(cg);
    }
}
