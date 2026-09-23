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
    readonly string[] recipes = { "Slash", "Hit", "Pierce", "Projectile", "Portal", "Cast", "Area", "Point", "PierceMiss", "ProjectileMiss", "SlashContext", "SlashContextRight", "HitContext", "HitContextRight", "PierceContext", "PierceContextRight", "PierceContextMiss", "PierceContextRightMiss", "ProjectileContext", "ProjectileContextRight", "ProjectileContextMiss", "ProjectileContextRightMiss", "XiyinTimeline" };
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
    ChristashaNativeParticlePlayback playback, areaPlayback, pointPlayback, portalPlayback;
    GameObject instance, areaInstance, pointInstance, portalInstance;
    GameObject actor, victim, secondVictim, thirdVictim;
    readonly List<GameObject> extraPointInstances=new List<GameObject>();
    readonly List<ChristashaNativeParticlePlayback> extraPointPlayback=new List<ChristashaNativeParticlePlayback>();
    Vector3 flightStart, flightEnd, portalCenter;
    float portalRadius;
    bool portalFrozen;
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
            if(portalPlayback!=null)
            {
                portalPlayback.StopAndClear();
                foreach(var line in portalInstance.GetComponentsInChildren<LineRenderer>(true))
                    if(line.enabled)throw new Exception("Cancelled portal retained a visible line");
                portalInstance.SetActive(false);Destroy(portalInstance);portalPlayback=null;portalInstance=null;
            }
            if(areaPlayback!=null){areaPlayback.StopAndClear();areaInstance.SetActive(false);Destroy(areaInstance);areaPlayback=null;areaInstance=null;}
            if(pointPlayback!=null){pointPlayback.StopAndClear();pointInstance.SetActive(false);Destroy(pointInstance);pointPlayback=null;pointInstance=null;}
            for(int i=0;i<extraPointPlayback.Count;i++){extraPointPlayback[i].StopAndClear();extraPointInstances[i].SetActive(false);Destroy(extraPointInstances[i]);}
            extraPointPlayback.Clear();extraPointInstances.Clear();
            DestroyActor(actor); DestroyActor(victim); DestroyActor(secondVictim);DestroyActor(thirdVictim);
            actor=null; victim=null; secondVictim=null;thirdVictim=null;
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
        string recipe = label == "XiyinTimeline" ? "Cast" : label.Replace("Miss", "").Replace("Context", "").Replace("Right", "");
        instance = library.CreatePrototype(recipe);
        foreach (Transform t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
        camera.orthographicSize = label == "XiyinTimeline" || recipe == "Area" ? 20 : recipe == "Cast" ? 5.5f : recipe == "Point" ? 4 : recipe == "Pierce" ? 3.5f : recipe == "Projectile" ? 10f : recipe == "Portal" ? 4.5f : 11.5f;
        camera.transform.position = label == "XiyinTimeline" || recipe == "Area" ? new Vector3(0,6,-100) : recipe == "Pierce" ? new Vector3(-4.2f,0,-100) : recipe == "Projectile" ? new Vector3(0,5,-100) : new Vector3(0,0,-100);
        flightStart=new Vector3(5,0,0);flightEnd=new Vector3(-5,0,0);
        portalRadius=ChristashaProjectileArc.PortalRadius(3f);portalFrozen=false;
        portalCenter=flightStart;
        if(label.Contains("Context"))
        {
            bool right=label.Contains("Right");
            float separation=recipe=="Projectile"?18:10;
            actor=Actor(recipe=="Slash"?"Slash":recipe=="Hit"?"Hit":recipe=="Projectile"?"Default":"Penetrate",new Vector3(right?-separation:separation,0,0),right);
            victim=Actor("Damaged",new Vector3(right?separation:-separation,0,0),!right);
            var pivot=new GameObject("NativeAttackPivot").transform;pivot.SetParent(actor.transform,false);
            pivot.localPosition=new Vector3(-1.92f,2.52f,-.15f);
            flightStart=pivot.position;flightEnd=victim.transform.TransformPoint(new Vector3(-1.92f,2.52f,-.15f));
            if(recipe=="Projectile")portalCenter=ChristashaProjectileArc.PortalAnchor(actor.transform.position,actor.transform);
            if(recipe=="Pierce" || recipe=="Slash" || recipe=="Hit")
            {
                instance.transform.SetParent(pivot,false);
                instance.transform.localScale=Vector3.one*(recipe=="Slash"?.45f:recipe=="Hit"?.55f:.35f);
                if(recipe=="Hit")instance.transform.localRotation=Quaternion.Euler(-10f,0f,0f);
            }
            else instance.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2((flightEnd-flightStart).y,(flightEnd-flightStart).x)*Mathf.Rad2Deg-180);
            camera.orthographicSize=17;camera.transform.position=new Vector3(0,9,-100);
        }
        if(label=="XiyinTimeline")
        {
            actor=Actor("Slash",new Vector3(19,0,0),false);
            victim=Actor("Damaged",new Vector3(-7,0,0),true);
            secondVictim=Actor("Damaged",new Vector3(-17,0,0),true);
            thirdVictim=Actor("Damaged",new Vector3(3,0,0),true);
            instance.transform.position=actor.transform.position;
            evidence.Add("XiyinTimeline: Cast t=0; Area t=.30; three Points t=.75 only as a simulated successful manager callback. Real callback contract is checked by the managed fixture.");
        }
        if (recipe == "Projectile")
        {
            flightStart=ChristashaProjectileArc.PortalLaunchPoint(portalCenter,flightEnd,portalRadius);
            instance.transform.position=flightStart;
            Vector3 tangent=ChristashaProjectileArc.PortalTangent(flightStart,flightEnd,0f);
            instance.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-180f);
            portalInstance=library.CreatePrototype("Portal");
            foreach(Transform t in portalInstance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            portalInstance.transform.position=portalCenter;
            portalInstance.transform.localScale=Vector3.one*portalRadius;
            evidence.Add(label+" rear portal anchor="+portalCenter+" launch="+flightStart+" radius="+portalRadius);
        }
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
            if(portalInstance!=null)
            {
                int portalChildren=portalInstance.GetComponentsInChildren<Transform>(true).Length;
                var portalFirst=ChristashaNativeParticlePlayback.Play(portalInstance,19040u);
                portalFirst.Advance(.1f);string portalBefore=Snapshot(portalInstance);portalFirst.StopAndClear();
                var portalSecond=ChristashaNativeParticlePlayback.Play(portalInstance,19040u);
                portalSecond.Advance(.1f);string portalAfter=Snapshot(portalInstance);portalSecond.StopAndClear();
                if(portalBefore!=portalAfter || portalChildren!=portalInstance.GetComponentsInChildren<Transform>(true).Length)
                    throw new Exception(label+" portal replay changed");
                evidence.Add(label+" portal replay/cancel/child-count PASS");
            }
        }
        playback = ChristashaNativeParticlePlayback.Play(instance, 19037u);
        if(portalInstance!=null)portalPlayback=ChristashaNativeParticlePlayback.Play(portalInstance,19040u);
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
            if (frame > Mathf.CeilToInt((recipes[recipeIndex]=="XiyinTimeline"?1.75f:playback.Duration) * 60) + 1) Next();
            if (complete) return;
            if(recipes[recipeIndex]=="XiyinTimeline")
            {
                if(frame==18)
                {
                    areaInstance=library.CreatePrototype("Area");
                    foreach(Transform t in areaInstance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                    areaInstance.transform.position=new Vector3(1,0,0);
                    areaInstance.transform.localScale=new Vector3(38f/ChristashaNativeMassRecipes.AreaDesignWidth,1,1);
                    areaPlayback=ChristashaNativeParticlePlayback.Play(areaInstance,19038u);
                }
                if(frame==45)
                {
                    pointInstance=library.CreatePrototype("Point");
                    foreach(Transform t in pointInstance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                    pointInstance.transform.position=victim.transform.TransformPoint(new Vector3(-1.92f,2.52f,-.15f));
                    pointInstance.transform.localScale=Vector3.one*2.2f;
                    pointPlayback=ChristashaNativeParticlePlayback.Play(pointInstance,19039u);
                    foreach(var targetActor in new[]{secondVictim,thirdVictim})
                    {
                        var extra=library.CreatePrototype("Point");
                        foreach(Transform t in extra.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                        extra.transform.position=targetActor.transform.TransformPoint(new Vector3(-1.92f,2.52f,-.15f));
                        extra.transform.localScale=Vector3.one*2.2f;
                        extraPointInstances.Add(extra);
                        extraPointPlayback.Add(ChristashaNativeParticlePlayback.Play(extra,19039u+(uint)extraPointPlayback.Count+1u));
                    }
                }
            }
            if (recipes[recipeIndex].StartsWith("Projectile",StringComparison.Ordinal))
            {
                float age=(frame+1)/60f;
                if(victim!=null)flightEnd=victim.transform.TransformPoint(new Vector3(-1.92f,2.52f,-.15f));
                if(actor!=null && !portalFrozen)
                    portalCenter=ChristashaProjectileArc.PortalAnchor(actor.transform.position,actor.transform);
                if(!portalFrozen)
                {
                    flightStart=ChristashaProjectileArc.PortalLaunchPoint(portalCenter,flightEnd,portalRadius);
                    portalInstance.transform.position=portalCenter;
                    if(age>=ChristashaProjectileArc.LaunchTime)portalFrozen=true;
                }
                float progress=Mathf.Clamp01((age-ChristashaProjectileArc.LaunchTime)/ChristashaProjectileArc.TravelTime);
                instance.transform.position=age<ChristashaProjectileArc.LaunchTime?flightStart:
                    ChristashaProjectileArc.PortalPosition(flightStart,flightEnd,progress);
                Vector3 tangent=ChristashaProjectileArc.PortalTangent(flightStart,flightEnd,progress);
                instance.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-180f);
            }
            playback.Advance(1f / 60f);
            if(portalPlayback!=null)portalPlayback.Advance(1f/60f);
            if(areaPlayback!=null)areaPlayback.Advance(1f/60f);
            if(pointPlayback!=null)pointPlayback.Advance(1f/60f);
            foreach(var extra in extraPointPlayback)extra.Advance(1f/60f);
            frame++;
            expected.Clear();
            var visibleRoots=new List<GameObject>{instance,portalInstance,areaInstance,pointInstance};
            visibleRoots.AddRange(extraPointInstances);
            foreach (var root in visibleRoots)
            {
                if(root==null)continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.gameObject.activeInHierarchy || r.sharedMaterial == null) continue;
                    r.GetPropertyBlock(block);
                    var values = new Color[properties.Length];
                    for (int i = 0; i < properties.Length; i++) values[i] = block.GetColor(properties[i]);
                    expected.Add(r, values);
                }
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
            if(recipes[recipeIndex].StartsWith("PierceContext",StringComparison.Ordinal) &&
                !recipes[recipeIndex].EndsWith("Miss",StringComparison.Ordinal) && (frame==10 || frame==12 || frame==17))
            {
                var line=impact.GetComponent<LineRenderer>();
                var flash=instance.transform.Find("ImpactFlash").GetComponent<MeshRenderer>();
                var body=instance.transform.Find("StrikeBlade").GetComponent<MeshRenderer>();
                line.GetPropertyBlock(block);float ringOpacity=block.GetFloat("_Opacity");
                flash.GetPropertyBlock(block);float flashOpacity=block.GetFloat("_Opacity");
                body.GetPropertyBlock(block);float bodyOpacity=block.GetFloat("_Opacity");
                evidence.Add(recipes[recipeIndex]+" frame="+frame+" ring="+line.enabled+"/"+ringOpacity+
                    " flash="+flash.enabled+"/"+flashOpacity+" body="+body.enabled+"/"+bodyOpacity+
                    " ringOrder="+line.sortingOrder+" bodyOrder="+body.sortingOrder+
                    " contact="+instance.transform.TransformPoint(instance.transform.Find("StrikeTarget").localPosition)+
                    " ringBounds="+line.bounds+" p0="+line.GetPosition(0)+" p16="+line.GetPosition(16));
            }
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
            if((recipes[recipeIndex]=="Area" || recipes[recipeIndex]=="XiyinTimeline") && (frame==30 || frame==42 || frame==45 || frame==52))
            {
                var areaRoot=areaInstance!=null?areaInstance:instance;
                var crest=ChristashaNativeMassRecipes.FindSystem(areaRoot,"MassCrest");
                evidence.Add(recipes[recipeIndex]+" frame="+frame+" crestParticles="+(crest==null?-1:crest.particleCount));
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
            bool fullSequence=recipes[recipeIndex].Contains("Context") || recipes[recipeIndex]=="XiyinTimeline" || recipes[recipeIndex].StartsWith("Pierce",StringComparison.Ordinal)||recipes[recipeIndex].StartsWith("Projectile",StringComparison.Ordinal);
            if (fullSequence || frame == 1 || frame == 3 || frame == 6 || frame == 9 || frame == 10 || frame == 15 || frame == 17 || frame == 21 || frame == 24 || frame == 27 || frame == 30 || frame == 36 || frame == 45 || frame == 60 || frame == 78)
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
            if(recipes[recipeIndex]=="PierceContext" && (frame==12 || frame==17))
            {
                var blade=instance.transform.Find("StrikeBlade").GetComponent<MeshRenderer>();
                bool wasEnabled=blade.enabled;blade.enabled=false;
                camera.backgroundColor=new Color(.32f,.34f,.37f);camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,"PierceContext","contact-without-blade-"+frame.ToString("D4")+".png"),pixels.EncodeToPNG());
                RenderTexture.active=null;blade.enabled=wasEnabled;
            }
            if(recipes[recipeIndex]=="PierceContext" && frame==17)
            {
                var ring=impact.GetComponent<LineRenderer>();
                var renderers=instance.GetComponentsInChildren<Renderer>(true);
                var states=new bool[renderers.Length];
                for(int i=0;i<renderers.Length;i++){states[i]=renderers[i].enabled;if(renderers[i]!=ring)renderers[i].enabled=false;}
                var actorSprite=actor.GetComponent<SpriteRenderer>();var victimSprite=victim.GetComponent<SpriteRenderer>();
                bool actorVisible=actorSprite.enabled,victimVisible=victimSprite.enabled;
                actorSprite.enabled=false;victimSprite.enabled=false;
                camera.backgroundColor=new Color(.32f,.34f,.37f);camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,"PierceContext","ring-only-0017.png"),pixels.EncodeToPNG());
                RenderTexture.active=null;
                actorSprite.enabled=actorVisible;victimSprite.enabled=victimVisible;
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=states[i];
            }
            if(recipes[recipeIndex]=="Portal" && frame==6)
            {
                var tick=instance.transform.Find("PortalTick0").GetComponent<LineRenderer>();
                evidence.Add("Portal frame=6 tick0="+tick.enabled+" width="+tick.widthMultiplier+
                    " bounds="+tick.bounds+" p0="+tick.GetPosition(0)+" p1="+tick.GetPosition(1));
                var arcs=new LineRenderer[3];var arcStates=new bool[3];
                for(int i=0;i<3;i++){arcs[i]=instance.transform.Find("PortalArc"+i).GetComponent<LineRenderer>();arcStates[i]=arcs[i].enabled;arcs[i].enabled=false;}
                camera.backgroundColor=new Color(.32f,.34f,.37f);camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,"Portal","ticks-only-0006.png"),pixels.EncodeToPNG());
                RenderTexture.active=null;
                for(int i=0;i<3;i++)arcs[i].enabled=arcStates[i];
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
