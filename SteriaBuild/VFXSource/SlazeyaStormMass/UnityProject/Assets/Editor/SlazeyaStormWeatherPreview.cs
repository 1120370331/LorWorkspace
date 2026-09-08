using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Native candidate diagnostics on the existing preview camera and stage.</summary>
public static class SlazeyaStormWeatherPreview
{
    private static object Existing(string name, params object[] args)
    { return typeof(SlazeyaStormMassBundleBuilder).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args); }
    private static void Check(bool pass, string message) { if (!pass) throw new Exception("Weather: " + message); }
    public static void Capture(GameObject prefab, Material template, string output)
    {
        Check(template != null && template.shader.isSupported, "bundle weather material readback");
        string folder = Path.Combine(output, "weather"); Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "sequence"));
        var feet = new[] { new Vector3(4,0,-3), new Vector3(10,0,3), new Vector3(16,0,-2), new Vector3(7,0,5), new Vector3(15,0,5) };
        Bounds[] targetBodies = (Bounds[])Existing("PreviewBodies", feet, 6f);
        var bodies = new List<Bounds>(targetBodies); bodies.Add(SlazeyaStormVisualController.ConservativeBody(new Vector3(-15,0,0),6));
        var f = SlazeyaStormVisualController.FitFootprint(feet,6,targetBodies);
        Lifecycle(template,f,folder);
        GameObject stage = (GameObject)Existing("CreateStage",true), root = Object.Instantiate(prefab);
        Camera camera = (Camera)Existing("CreateCamera",true);
        var rt = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32) { antiAliasing=4 };
        var pixels = new Texture2D(1280,720,TextureFormat.RGBA32,false); camera.targetTexture=rt;
        var trace = new StringBuilder("file,elapsed,burst_age,envelope,cloud_weight,grade_weight,rain_weight\n");
        var times = new SortedSet<float>(); for(int i=0;i<=102;i++)times.Add(i/30f);
        float[] gather={.20f,.55f,.94f,1.28f,2f},burst={0,.05f,7f/60f,.25f,.45f,.80f,1.10f,1.20f};
        foreach(float t in gather)times.Add(t); foreach(float b in burst)times.Add(2.1f+b); times.Add(.65f);times.Add(2.0f+.1f);
        using(var visual=new SlazeyaStormVisualController(root,f))
        using(var weather=new SlazeyaStormWeatherController(visual))
        {
            var filter=camera.gameObject.AddComponent<SlazeyaStormWeatherScreenFilter>(); filter.Initialize(weather,template);
            float previous=0; int frame=0;
            foreach(float time in times)
            {
                float remaining=time-previous;
                while(remaining>0.000001f){float dt=Mathf.Min(1f/60f,remaining);visual.Advance(dt);remaining-=dt;}
                previous=time;
                if(time>=2.1f&&!visual.BurstTriggered)visual.TriggerBurst();
                weather.Project(camera,f,bodies.ToArray());
                bool sample=false;foreach(float t in gather)if(Mathf.Abs(time-t)<.00001f)sample=true;
                foreach(float b in burst)if(Mathf.Abs(time-(2.1f+b))<.00001f)sample=true;
                if(Mathf.Abs(time-.65f)<.00001f)sample=true;
                string key=visual.BurstTriggered?"B"+visual.BurstAge.ToString("F6",CultureInfo.InvariantCulture):"G"+time.ToString("F2",CultureInfo.InvariantCulture);
                Color32[] off=null;
                if(sample)
                {
                    weather.SetWeights(0,0,0); Save(camera,rt,pixels,folder,key+"_off.png",weather,visual,trace); off=pixels.GetPixels32();
                }
                weather.SetWeights(1,1,1);
                byte[] png=(byte[])Existing("Render",camera,rt,pixels);
                if(sample)
                {
                    File.WriteAllBytes(Path.Combine(folder,key+"_on.png"),png);Row(trace,key+"_on.png",weather,visual);
                    if(visual.BurstAge>=1.0999f) Check(MaxDifference(off,pixels.GetPixels32())<=1,"B1.10 exact pass-through");
                }
                if(Mathf.Abs(time*30-Mathf.Round(time*30))<.0001f)File.WriteAllBytes(Path.Combine(folder,"sequence/frame_"+(frame++).ToString("D4")+".png"),png);
                if(Mathf.Abs(time-.55f)<.00001f||Mathf.Abs(time-1.28f)<.00001f||Mathf.Abs(time-2.35f)<.00001f)
                {
                    weather.SetWeights(1,0,0);Save(camera,rt,pixels,folder,key+"_cloud.png",weather,visual,trace);
                    weather.SetWeights(0,1,0);Save(camera,rt,pixels,folder,key+"_grade.png",weather,visual,trace);
                    weather.SetWeights(0,0,1);Save(camera,rt,pixels,folder,key+"_rain.png",weather,visual,trace);
                }
            }
            filter.Release();Object.DestroyImmediate(filter);
        }
        File.WriteAllText(Path.Combine(folder,"samples.csv"),trace.ToString());
        camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(stage);Object.DestroyImmediate(root);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        CaptureReverse(prefab,template,folder,feet);
    }
    private static void CaptureReverse(GameObject prefab,Material template,string folder,Vector3[] referenceFeet)
    {
        var feet=(Vector3[])referenceFeet.Clone();for(int i=0;i<feet.Length;i++)feet[i].x=-feet[i].x;
        var bodies=(Bounds[])Existing("PreviewBodies",feet,6f);
        var protections=new List<Bounds>(bodies);protections.Add(SlazeyaStormVisualController.ConservativeBody(new Vector3(15,0,0),6));
        var f=SlazeyaStormVisualController.FitFootprint(feet,6,bodies);
        var stage=(GameObject)Existing("CreateStage",true);stage.transform.localScale=new Vector3(-1,1,1);
        var camera=(Camera)Existing("CreateCamera",true);var root=Object.Instantiate(prefab);
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        var pixels=new Texture2D(1280,720,TextureFormat.RGBA32,false);camera.targetTexture=rt;
        using(var visual=new SlazeyaStormVisualController(root,f))using(var weather=new SlazeyaStormWeatherController(visual))
        {
            var filter=camera.gameObject.AddComponent<SlazeyaStormWeatherScreenFilter>();filter.Initialize(weather,template);
            for(int step=0;step<33;step++)visual.Advance(1f/60f);
            weather.Project(camera,f,protections.ToArray());
            weather.SetWeights(0,0,0);File.WriteAllBytes(Path.Combine(folder,"reverse_G0.55_off.png"),(byte[])Existing("Render",camera,rt,pixels));
            weather.SetWeights(1,1,1);File.WriteAllBytes(Path.Combine(folder,"reverse_G0.55_on.png"),(byte[])Existing("Render",camera,rt,pixels));
            weather.SetWeights(1,0,0);File.WriteAllBytes(Path.Combine(folder,"reverse_G0.55_cloud.png"),(byte[])Existing("Render",camera,rt,pixels));
            filter.Release();Object.DestroyImmediate(filter);
        }
        camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(stage);Object.DestroyImmediate(root);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
    }
    private static int MaxDifference(Color32[] a,Color32[] b)
    { int d=0;for(int i=0;i<a.Length;i++){d=Math.Max(d,Math.Abs(a[i].r-b[i].r));d=Math.Max(d,Math.Abs(a[i].g-b[i].g));d=Math.Max(d,Math.Abs(a[i].b-b[i].b));d=Math.Max(d,Math.Abs(a[i].a-b[i].a));}return d; }
    private static void Save(Camera c,RenderTexture rt,Texture2D pixels,string folder,string name,SlazeyaStormWeatherController w,SlazeyaStormVisualController v,StringBuilder trace)
    { File.WriteAllBytes(Path.Combine(folder,name),(byte[])Existing("Render",c,rt,pixels));Row(trace,name,w,v); }
    private static void Row(StringBuilder s,string name,SlazeyaStormWeatherController w,SlazeyaStormVisualController v)
    {s.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F7},{2:F7},{3:F7},{4},{5},{6}",name,v.Elapsed,v.BurstAge,w.Envelope,w.CloudWeight,w.GradeWeight,w.RainWeight));}
    private static void Lifecycle(Material template,SlazeyaStormVisualController.Footprint f,string folder)
    {
        var report=new StringBuilder();
        using(var visual=new SlazeyaStormVisualController(null,f))
        using(var a=new SlazeyaStormWeatherController(visual))
        using(var b=new SlazeyaStormWeatherController(visual))
        {
            var one=new GameObject("WeatherIndependentOne"); var two=new GameObject("WeatherIndependentTwo");
            var fa=one.AddComponent<SlazeyaStormWeatherScreenFilter>();var fb=two.AddComponent<SlazeyaStormWeatherScreenFilter>();
            fa.Initialize(a,null);Check(!fa.IsInitialized,"missing material rejected");
            fa.Initialize(a,template);fb.Initialize(b,template);
            visual.Advance(.55f);var m=new Material(template);a.Apply(m,16f/9f);Vector4 state=m.GetVector("_Weather");
            visual.Advance(0);a.Apply(m,16f/9f);Check(m.GetVector("_Weather")==state,"pause freezes phase and weights");
            a.Apply(m,16f/9f);Check(m.GetVector("_Weather")==state,"render repeats cannot advance");
            fa.enabled=false;Check(a.IsComplete&&!b.IsComplete&&fb.IsInitialized&&!visual.IsComplete,"disable only its weather");
            fa.Release();fa.Release();Check(fb.IsInitialized,"idempotent independent release");
            visual.TriggerBurst();visual.Advance(.1f);float e=b.Envelope;visual.TriggerBurst();Check(Mathf.Abs(e-b.Envelope)<1e-6,"duplicate callback does not reset");
            visual.Advance(1f);Check(b.Envelope==0,"B1.10 zero");visual.Advance(.1f);Check(b.IsComplete,"B1.20 complete");
            fb.Release();Object.DestroyImmediate(one);Object.DestroyImmediate(two);Object.DestroyImmediate(m);
            report.AppendLine("PASS missing material; pause; repeated apply; filter disable; independent cloned instances; repeated release; duplicate callback; B1.10 zero; B1.20 complete");
        }
        using(var v=new SlazeyaStormVisualController(null,f))using(var w=new SlazeyaStormWeatherController(v))
        {
            v.Advance(.1f);float before=w.Envelope;v.TriggerBurst();v.Advance(.04f);Check(w.Envelope<=before+1e-6f,"early callback never re-enters");w.Cancel();Check(w.Envelope==0&&!v.IsComplete,"cancel does not cancel visual");
        }
        File.WriteAllText(Path.Combine(folder,"native-lifecycle.txt"),report+"PASS early callback continuous; weather cancellation leaves visual active\n");
    }
}
