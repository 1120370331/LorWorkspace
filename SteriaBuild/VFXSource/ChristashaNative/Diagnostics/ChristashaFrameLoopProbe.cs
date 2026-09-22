using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Steria.Vfx;
using UnityEngine;

// Runs through Unity's actual Update -> animation -> LateUpdate player loop.
// Reference assets stay in the local research bundle, never in the mod output.
public sealed class ChristashaFrameLoopProbe : MonoBehaviour
{
    public string Output;
    public string BundlePath;
    public string BrushPath;
    public string SkinPath;
    public bool RequireStable;
    public Action<bool> Finished;
    readonly string[] recipes = { "Slash", "Hit", "Pierce", "Projectile", "Cast", "Area", "Point", "PierceMiss", "ProjectileMiss", "PierceContext", "PierceContextRight", "ProjectileContext" };
    readonly string[] properties = { "_Color", "_MainColor", "MainColor", "_OutColor", "_DissolveColor" };
    readonly List<string> evidence = new List<string>();
    MaterialPropertyBlock block;
    readonly Dictionary<Renderer, Color[]> expected = new Dictionary<Renderer, Color[]>();
    AssetBundle bundle;
    AssetBundle strikeBundle;
    Texture2D brush, pixels;
    RenderTexture target;
    new Camera camera;
    ChristashaNativeParticleLibrary library;
    ChristashaNativeParticlePlayback playback;
    GameObject instance;
    GameObject actor, victim;
    Vector3 flightStart, flightEnd;
    int recipeIndex = -1, frame, driftCount;
    bool complete;

    void Start()
    {
        try
        {
            Directory.CreateDirectory(Output);
            block = new MaterialPropertyBlock();
            recipeIndex = -1;
            Time.captureFramerate = 60;
            bundle = AssetBundle.LoadFromFile(BundlePath);
            if (bundle == null) throw new Exception("Missing local reference bundle");
            brush = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            brush.LoadImage(File.ReadAllBytes(BrushPath));
            string strikePath=Path.GetFullPath("AssetBundles/steria_christasha_light_strike");
            strikeBundle=AssetBundle.LoadFromFile(strikePath);
            if(strikeBundle==null)throw new Exception("New strike bundle did not load");
            var strikeShader=strikeBundle.LoadAsset<Shader>("assets/nativeruntime/platinumstrike.shader");
            if(strikeShader==null || !strikeShader.isSupported)throw new Exception("New strike shader unsupported");
            File.Copy(strikePath,Path.Combine(Output,"steria_christasha_light_strike"),true);
            library = new ChristashaNativeParticleLibrary(path => bundle.LoadAsset<GameObject>(
                "assets/native_reference/" + path.Substring(path.LastIndexOf('/') + 1) + ".prefab"), brush, strikeShader);
            camera = new GameObject("ProbeCamera").AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.aspect = 16f / 9f;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 500;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask = 1 << 30;
            target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            target.Create(); camera.targetTexture = target;
            pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            evidence.Add("Unity " + Application.unityVersion + "; " + SystemInfo.graphicsDeviceType + "; actual Play Mode, captureFramerate=60");
            Next();
        }
        catch (Exception e) { Fail(e); }
    }

    void Next()
    {
        if (playback != null)
        {
            playback.StopAndClear();
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                if (ps.particleCount != 0) throw new Exception("Cancel leaked particles");
            if (RequireStable)
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                    if (animator.enabled) throw new Exception("Cancelled Animator is still scheduled");
            instance.SetActive(false); Destroy(instance);
            DestroyActor(actor); DestroyActor(victim); actor=null; victim=null;
        }
        if (++recipeIndex >= recipes.Length)
        {
            evidence.Add("colorDriftCount=" + driftCount);
            File.WriteAllLines(Path.Combine(Output, "frame-loop.txt"), evidence);
            complete = true;
            bool ok = !RequireStable || driftCount == 0;
            File.WriteAllText(Path.Combine(Output, "complete.txt"), ok ? "PASS" : "FAIL: automatic Animator overwrote theme colors");
            if (Finished != null) Finished(ok);
            return;
        }
        string label = recipes[recipeIndex];
        string recipe = label.Replace("Miss", "").Replace("Context", "").Replace("Right", "");
        instance = library.CreatePrototype(recipe);
        foreach (Transform t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
        camera.orthographicSize = recipe == "Area" ? 20 : recipe == "Cast" ? 5.5f : recipe == "Point" ? 4 : recipe == "Pierce" ? 3.5f : recipe == "Projectile" ? 4.5f : 11.5f;
        camera.transform.position = recipe == "Area" ? new Vector3(0,6,-100) : recipe == "Pierce" ? new Vector3(-4.2f,0,-100) : new Vector3(0,0,-100);
        flightStart=new Vector3(5,0,0);flightEnd=new Vector3(-5,0,0);
        if(label.Contains("Context"))
        {
            bool right=recipe=="Projectile" || label.EndsWith("Right",StringComparison.Ordinal);
            float separation=recipe=="Projectile"?18:10;
            actor=Actor("Penetrate",new Vector3(right?-separation:separation,0,0),right);
            victim=Actor("Damaged",new Vector3(right?separation:-separation,0,0),!right);
            var pivot=new GameObject("NativeAttackPivot").transform;pivot.SetParent(actor.transform,false);
            pivot.localPosition=new Vector3(-1.92f,2.52f,-.15f);
            flightStart=pivot.position;flightEnd=victim.transform.TransformPoint(new Vector3(-1.92f,2.52f,-.15f));
            if(recipe=="Pierce") { instance.transform.SetParent(pivot,false);instance.transform.localScale=Vector3.one*.35f; }
            else instance.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2((flightEnd-flightStart).y,(flightEnd-flightStart).x)*Mathf.Rad2Deg-180);
            camera.orthographicSize=17;camera.transform.position=new Vector3(0,9,-100);
        }
        if (recipe == "Projectile") instance.transform.position = flightStart;
        foreach(var r in instance.GetComponentsInChildren<Renderer>(true))
            foreach(var material in r.sharedMaterials)
                if(material!=null && (material.shader==null || !material.shader.isSupported)) throw new Exception("Unsupported shader: "+r.name);
        if (RequireStable)
        {
            int children = instance.GetComponentsInChildren<Transform>(true).Length;
            var first = ChristashaNativeParticlePlayback.Play(instance,19037u);
            first.Advance(.1f); string before = Snapshot(instance); first.StopAndClear();
            var second = ChristashaNativeParticlePlayback.Play(instance,19037u);
            second.Advance(.1f); string after = Snapshot(instance); second.StopAndClear();
            if (before != after) throw new Exception(recipe + " same-seed replay changed");
            if (children != instance.GetComponentsInChildren<Transform>(true).Length) throw new Exception("Replay added children");
            evidence.Add(label + " replay/cancel/child-count PASS");
        }
        playback = ChristashaNativeParticlePlayback.Play(instance, 19037u);
        if(recipe=="Pierce" && label.Contains("Context"))
        {
            Vector3 end=ChristashaThrustEnvelope.Exit(instance.transform.position,flightEnd,victim.transform.position,3f);
            playback.AimStrike(instance.transform.InverseTransformPoint(flightEnd),instance.transform.InverseTransformPoint(end));
        }
        if (!label.EndsWith("Miss",StringComparison.Ordinal)) playback.ConfirmStrikeImpact();
        frame = 0;
        Directory.CreateDirectory(Path.Combine(Output, label));
    }

    GameObject Actor(string motion,Vector3 position,bool right)
    {
        var xml=new XmlDocument();xml.Load(Path.Combine(SkinPath,"ModInfo.xml"));
        var pivot=xml.SelectSingleNode("/ModInfo/ClothInfo/"+motion+"/Pivot");
        float px=float.Parse(pivot.Attributes["pivot_x"].Value,System.Globalization.CultureInfo.InvariantCulture);
        float py=float.Parse(pivot.Attributes["pivot_y"].Value,System.Globalization.CultureInfo.InvariantCulture);
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        texture.LoadImage(File.ReadAllBytes(Path.Combine(SkinPath,"ClothCustom",motion+".png")));
        var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2((px+512)/1024,(py+512)/1024),50);
        var go=new GameObject("ContextActor_"+motion);go.layer=30;go.transform.position=position;go.transform.localScale=Vector3.one*3;
        go.transform.rotation=Quaternion.Euler(0,right?180:0,0);
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingLayerName="Character";
        return go;
    }
    void DestroyActor(GameObject go)
    {
        if(go==null)return;var sprite=go.GetComponent<SpriteRenderer>().sprite;
        go.SetActive(false);Destroy(sprite.texture);Destroy(sprite);Destroy(go);
    }

    static string Snapshot(GameObject root)
    {
        var result = new StringBuilder();
        foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var values = new ParticleSystem.Particle[p.main.maxParticles]; int count = p.GetParticles(values);
            result.Append(p.name).Append(':').Append(count);
            for (int i=0;i<count;i++) result.Append(values[i].position.ToString("F4")).Append(values[i].remainingLifetime.ToString("F4"));
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) result.Append(t.name).Append(t.localPosition.ToString("F4")).Append(t.localScale.ToString("F4"));
        return result.ToString();
    }

    void Update()
    {
        if (complete || playback == null) return;
        try
        {
            if (frame > Mathf.CeilToInt(playback.Duration * 60) + 1) Next();
            if (complete) return;
            if (recipes[recipeIndex].StartsWith("Projectile",StringComparison.Ordinal))
            {
                float progress=Mathf.Clamp01((frame+1)/60f/ChristashaProjectileArc.FlightTime);
                instance.transform.position=ChristashaProjectileArc.Position(flightStart,flightEnd,progress);
                Vector3 tangent=ChristashaProjectileArc.Tangent(flightStart,flightEnd,progress);
                instance.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-180f);
            }
            playback.Advance(1f / 60f);
            frame++;
            expected.Clear();
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.activeInHierarchy || r.sharedMaterial == null) continue;
                r.GetPropertyBlock(block);
                var values = new Color[properties.Length];
                for (int i = 0; i < properties.Length; i++) values[i] = block.GetColor(properties[i]);
                expected.Add(r, values);
            }
        }
        catch (Exception e) { Fail(e); }
    }

    void LateUpdate()
    {
        if (complete || playback == null) return;
        try
        {
            var impact = instance.transform.Find("ImpactRing");
            if (impact != null && recipes[recipeIndex].EndsWith("Miss",StringComparison.Ordinal) && impact.GetComponent<LineRenderer>().enabled)
                throw new Exception("Miss emitted confirmed-hit ring");
            if(frame==15 && recipes[recipeIndex].StartsWith("Pierce",StringComparison.Ordinal))
            {
                var blade=instance.transform.Find("StrikeBlade");
                Vector3 tail=blade.TransformPoint(new Vector3(3.6f,0,0)),origin=instance.transform.position;
                if(Mathf.Abs(tail.x-origin.x)>.03f || Mathf.Abs(tail.y-origin.y)>.03f)throw new Exception("Thrust tail detached from the release point");
                Vector3 contactPoint=instance.transform.TransformPoint(instance.transform.Find("StrikeTarget").localPosition);
                if(Vector3.Distance(blade.position,origin)<Vector3.Distance(contactPoint,origin)+1f)throw new Exception("Thrust does not pass through the victim");
                float height=blade.GetComponent<MeshRenderer>().bounds.size.y;
                if(recipes[recipeIndex].Contains("Context") && height<5f)throw new Exception("Thrust too narrow at actor scale");
                evidence.Add(recipes[recipeIndex]+" span PASS origin="+origin+" contact="+contactPoint+" tip="+blade.position+" tail="+tail+" height="+height);
            }
            foreach (var pair in expected)
            {
                pair.Key.GetPropertyBlock(block);
                for (int i = 0; i < properties.Length; i++)
                {
                    if (!pair.Key.sharedMaterial.HasProperty(properties[i])) continue;
                    Color after = block.GetColor(properties[i]);
                    if (Vector4.Distance(pair.Value[i], after) > .0001f)
                    {
                        driftCount++;
                        if (frame <= 10) evidence.Add(recipes[recipeIndex] + " frame=" + frame + " " + pair.Key.name + "." + properties[i] + " Update=" + pair.Value[i].ToString("F3") + " LateUpdate=" + after.ToString("F3"));
                    }
                }
            }
            bool fullSequence=recipes[recipeIndex].StartsWith("Pierce",StringComparison.Ordinal)||recipes[recipeIndex].StartsWith("Projectile",StringComparison.Ordinal);
            if (fullSequence || frame == 1 || frame == 3 || frame == 6 || frame == 9 || frame == 10 || frame == 15 || frame == 17 || frame == 21 || frame == 30 || frame == 45 || frame == 78)
            {
                for (int bg = 0; bg < 2; bg++)
                {
                    camera.backgroundColor = bg == 0 ? new Color(.035f,.045f,.06f) : new Color(.32f,.34f,.37f);
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0,0,960,540),0,0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(Output, recipes[recipeIndex], (bg == 0 ? "dark_" : "mid_") + frame.ToString("D4") + ".png"), pixels.EncodeToPNG());
                    RenderTexture.active = null;
                }
            }
        }
        catch (Exception e) { Fail(e); }
    }

    void Fail(Exception error)
    {
        complete = true; Debug.LogException(error);
        File.WriteAllText(Path.Combine(Output, "error.txt"), error.ToString());
        if (Finished != null) Finished(false);
    }
}
