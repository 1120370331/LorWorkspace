using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Battle.DiceAttackEffect;
using UnityEditor;
using UnityEngine;
using Steria;

public static class AnhierTexturePreview
{
    [Serializable] class FixtureConfig { public string badaBundle, fireBundle; }
    [Serializable] class NativeSprite { public string name,file,sha256; public float ppu,pivotX,pivotY; public int sortingLayer,sortingOrder; public Vector3 position,scale; public Quaternion rotation; }
    [Serializable] class NativeEffect { public string profile,resource; public NativeSprite[] sprites; }
    [Serializable] class NativeData { public NativeEffect[] effects; }
    [Serializable] class Evidence { public string profile,action,nativeResource,facing; public float mapScale,roll,lifetime,alphaAt250,alphaAt400; public float[] selfMatrix,targetMatrix,pivotMatrix,effectMatrix; public Vector3 contentMin,contentMax,rendererBounds; public int sortingLayer,sortingOrder; public bool parentMatches; }
    [Serializable] class EvidenceList { public List<Evidence> rows=new List<Evidence>(); }
    static readonly string Here=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
    static readonly string Output=Path.Combine(Here,"previews");
    static readonly string Reference=Path.Combine(Application.dataPath,"Reference");
    static readonly string[] Profiles={"SeaPierce","SeaFarHit","MemorySlash","MemoryPierce","MemoryHit","MemoryGuard"};
    static Camera camera; static RenderTexture rt; static GameObject stage; static FixtureConfig config; static NativeData natives;
    static readonly List<string> checks=new List<string>(); static readonly EvidenceList evidence=new EvidenceList();
    static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    static BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Output);
            config=JsonUtility.FromJson<FixtureConfig>(File.ReadAllText(Path.Combine(Reference,"fixture-config.json")));
            natives=JsonUtility.FromJson<NativeData>(File.ReadAllText(Path.Combine(Reference,"native-effects.json")));
            Check(LayerMask.NameToLayer("Effect")==8,"native Effect GameObject layer 8");
            Check(SortingLayer.NameToID("Particle")==-1396713383,"native Particle sorting ID");
            foreach(string profile in Profiles)
            {
                foreach(bool left in new[]{true,false})
                {
                    // Same actor, motion, camera, roll and mapScale for native/candidate.
                    Scene(3f);
                    ActionDetail action=Action(profile);
                    BattleUnitView self=Actor(profile=="SeaFarHit"?config.fireBundle:config.badaBundle,action,left,3f,left?5f:-5f);
                    BattleUnitView target=Actor(config.badaBundle,ActionDetail.Guard,!left,3f,left?-6.5f:6.5f);
                    Check(self.charAppearance.GetCurrentMotionDetail()==action,profile+" actual "+action+" native prefab pose");
                    var effect=Candidate(profile,self,target,1f);
                    var core=effect.GetComponent<AnhierTextureTimeline>();
                    Check(core!=null,profile+" public Initialize produced timeline");
                    Check(effect.transform.parent==self.charAppearance.GetAtkEffectPivot(action),profile+" actual native pivot parent");
                    Check(effect.transform.localRotation==Quaternion.identity,profile+" inherited rotation once");
                    Check(Mathf.Abs(effect.transform.localScale.x-(profile=="MemoryGuard"||profile=="SeaFarHit"?2f:4f))<.0001f,profile+" native roll1 scale");
                    Tick(effect,.25f);float alpha250=core.Alpha;Check(alpha250>=.999f,profile+" full native alpha at 250ms");
                    Tick(effect,.4f);float alpha400=core.Alpha;
                    Tick(effect,0f);
                    var body=effect.transform.Find("Pigment").GetComponent<SpriteRenderer>();
                    Check(body.sortingLayerID==-1396713383 && body.sortingOrder==0,profile+" native renderer sorting");
                    Check(!effect.HasDamagedEffect,profile+" no unconditional damaged spark");
                    if(profile=="MemoryHit")Check(target.GetComponentsInChildren<SpriteRenderer>().All(x=>x.name!="TargetWaterSplash"),"H has no target hurt starburst");
                    if(profile=="SeaFarHit")
                    {
                        var contact=target.atkEffectRoot.Find("TargetWaterSplash").GetComponent<SpriteRenderer>();
                        Check(contact.enabled && contact.color.a>.99f*.9f,"F contact active on event frame");
                        Check(contact.gameObject.layer==8 && contact.sortingLayerID==-1396713383,"F independent contact sorting/layer");
                    }
                    evidence.rows.Add(new Evidence{profile=profile,action=self.charAppearance.GetCurrentMotionDetail().ToString(),nativeResource=natives.effects.First(x=>x.profile==profile).resource,facing=left?"LEFT":"RIGHT",mapScale=3f,roll=1f,lifetime=Life(effect),alphaAt250=alpha250,alphaAt400=alpha400,selfMatrix=Matrix(self.transform),targetMatrix=Matrix(target.atkEffectRoot),pivotMatrix=Matrix(effect.transform.parent),effectMatrix=Matrix(effect.transform),rendererBounds=body.bounds.size,sortingLayer=body.sortingLayerID,sortingOrder=body.sortingOrder,parentMatches=effect.transform.parent==self.charAppearance.GetAtkEffectPivot(action)});
                    string dir=Path.Combine(Output,profile+(left?"_left":"_right"));Directory.CreateDirectory(dir);
                    for(int i=0;i<=40;i++){Tick(effect,i/60f);Save(Path.Combine(dir,"candidate_"+i.ToString("00")+".png"));}
                    // Lifetime override changes the same public adapter's normalized clock.
                    effect.SetDestroyTime(2f);Tick(effect,.5f);Check(core.Alpha>.999f,profile+" SetDestroyTime respected by full-shape window");
                    effect.SetDestroyTime(1f);Tick(effect,.667f);if(profile!="SeaFarHit")Check(core.Complete,"native visual ends .667 before 1s destruction");
                    Dispose(effect);
                    var native=Native(profile,self,target);
                    for(int i=0;i<=40;i++){NativeTick(native,i/60f,profile=="SeaFarHit");Save(Path.Combine(dir,"native_"+i.ToString("00")+".png"));}
                    Dispose(native);
                    if(left && profile=="MemoryHit")OldComparison(self,target,dir);
                }
            }
            // Fallback is exercised on a skin which truly lacks Fire.
            Scene(2.25f);var fallback=Actor(config.badaBundle,ActionDetail.Fire,true,2.25f,5f);Check(fallback.charAppearance.GetCurrentMotionDetail()==ActionDetail.Default,"Bada missing Fire uses actual native Default fallback");
            var other=Actor(config.badaBundle,ActionDetail.Guard,false,2.25f,-6.5f);
            var fallbackFx=Candidate("SeaFarHit",fallback,other,1f);Check(fallbackFx.transform.parent==fallback.atkEffectRoot,"F null pivot native AtkEffectRoot fallback");Dispose(fallbackFx);
            // Non-unit, non-uniform, rotated explicit action pivot: exact Unity matrices,
            // not a scalar substitute. Map scale is never capped at 1.75.
            foreach(float scale in new[]{1.5f,2.25f,3f})
            {
                Scene(scale);var self=Actor(config.badaBundle,ActionDetail.Penetrate,true,scale,5f);var target=Actor(config.badaBundle,ActionDetail.Guard,false,scale,-6.5f);
                var pivot=new GameObject("ExplicitRotatedZ").transform;pivot.SetParent(self.charAppearance.transform,false);pivot.localPosition=new Vector3(-2.1f,2.4f,0);pivot.localRotation=Quaternion.Euler(0,0,18);pivot.localScale=new Vector3(1.15f,.9f,1);self.charAppearance.atkEffectPivot_Z=pivot;
                var fx=Candidate("MemoryPierce",self,target,1f);
                Vector3 expected=pivot.TransformPoint(new Vector3(4f,0,0));Vector3 actual=fx.transform.TransformPoint(Vector3.right);
                Check(Vector3.Distance(expected,actual)<.0001f,"map "+scale+" full rotated/nonuniform hierarchy once");
                Check(fx.transform.lossyScale.x>scale*3.9f,"map "+scale+" no actorScale cap");
                Tick(fx,.15f);Save(Path.Combine(Output,"map"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"_rotated.png"));Dispose(fx);
            }
            File.WriteAllText(Path.Combine(Output,"runtime-evidence.json"),JsonUtility.ToJson(evidence,true));
            File.WriteAllText(Path.Combine(Output,"checks.txt"),string.Join("\n",checks.ToArray()));
            Debug.Log("ANHIER_TEXTURE_FIXTURE_PASS "+checks.Count+" checks");EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);File.WriteAllText(Path.Combine(Output,"failure.txt"),ex.ToString());EditorApplication.Exit(1);}
    }
    static ActionDetail Action(string p){return p=="MemorySlash"?ActionDetail.Slash:p=="MemoryHit"?ActionDetail.Hit:p=="MemoryGuard"?ActionDetail.Guard:p=="SeaFarHit"?ActionDetail.Fire:ActionDetail.Penetrate;}
    static string Suffix(string p){return p=="MemorySlash"?"J":p=="MemoryHit"?"H":p=="MemoryGuard"?"G":"Z";}
    static DiceAttackEffect Candidate(string profile,BattleUnitView self,BattleUnitView target,float lifetime)
    {
        Type type=typeof(DiceAttackEffect_Steria_AnhierTextureBase).Assembly.GetType("DiceAttackEffect_Steria_AnhierTexture"+profile,true);
        var go=new GameObject(profile);go.transform.SetParent(stage.transform,false);var effect=(DiceAttackEffect)go.AddComponent(type);
        typeof(DiceAttackEffect).GetMethod("Awake",Fields).Invoke(effect,null);
        effect.Initialize(self,target,lifetime);effect.SetScale(1f);effect.SetLayer("Effect");return effect;
    }
    static float Life(DiceAttackEffect e){return (float)typeof(DiceAttackEffect).GetField("_destroyTime",Fields).GetValue(e);}
    static void Tick(DiceAttackEffect e,float time)
    {
        // Native Update is invoked with an exact event time while editing. No second
        // renderer path or adapter bypass: inherited elapsed and public lifetime apply.
        typeof(DiceAttackEffect).GetField("_elapsed",Fields).SetValue(e,time-Time.deltaTime);
        typeof(DiceAttackEffect_Steria_AnhierTextureBase).GetMethod("Update",Fields).Invoke(e,null);
    }
    static void NativeTick(DiceAttackEffect e,float time,bool far)
    {
        typeof(DiceAttackEffect).GetField("_elapsed",Fields).SetValue(e,time-Time.deltaTime);
        typeof(DiceAttackEffect).GetMethod("Update",Fields).Invoke(e,null);
        // Independently decoded native clip coefficients (not candidate NativeAlpha).
        float d=Mathf.Max(0,time-(far?1f/3f:.25f));float u=far?Mathf.Clamp01(d*6):0;
        float alpha=far?1-u*u*(3-2*u):Mathf.Clamp01(27.647995f*d*d*d-17.279999f*d*d+1);
        if(time>=(far?.5f:2f/3f))alpha=0;
        foreach(var sr in e.GetComponentsInChildren<SpriteRenderer>()){sr.color=new Color(1,1,1,alpha);sr.enabled=alpha>0;}
    }
    static DiceAttackEffect Native(string profile,BattleUnitView self,BattleUnitView target)
    {
        NativeEffect data=natives.effects.First(x=>x.profile==profile);bool far=profile=="SeaFarHit";
        var root=new GameObject(data.resource.Split('/').Last().Split('_')[0]+"_"+Suffix(profile));root.transform.SetParent(stage.transform,false);
        DiceAttackEffect effect=far?(DiceAttackEffect)root.AddComponent<DiceAttackEffect_RedHoodMercenary_Gun>():root.AddComponent<DiceAttackEffect>();
        var main=root.GetComponent<SpriteRenderer>();
        foreach(var item in data.sprites)
        {
            SpriteRenderer sr;
            bool isRoot=item.name=="RedHoodMercenary_Gun"||!far;
            if(isRoot)sr=main;else{var go=new GameObject(item.name);go.transform.SetParent(root.transform,false);sr=go.AddComponent<SpriteRenderer>();sr.transform.localPosition=item.position;sr.transform.localRotation=item.rotation;sr.transform.localScale=item.scale;}
            sr.sprite=LoadSprite(item);sr.sharedMaterial=AnhierTextureAssets.AlphaMaterial;sr.sortingLayerID=item.sortingLayer;sr.sortingOrder=item.sortingOrder;
            if(far && item.name=="Hit")typeof(DiceAttackEffect_RedHoodMercenary_Gun).GetField("_targetEffect",Fields).SetValue(effect,sr);
            if(far && item.name=="Bullet")typeof(DiceAttackEffect_RedHoodMercenary_Gun).GetField("_bullet",Fields).SetValue(effect,sr);
        }
        typeof(DiceAttackEffect).GetMethod("Awake",Fields).Invoke(effect,null);
        if(profile=="MemoryGuard")typeof(DiceAttackEffect).GetField("additionalScale",Fields).SetValue(effect,Vector3.one*-.5f);
        effect.Initialize(self,target,1f);effect.SetScale(1f);effect.SetLayer("Effect");root.GetComponent<Animator>().enabled=false;
        // No unsupported trail animation is invented for the native Fire+Hit reference.
        if(far)foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="Bullet"||sr.name=="Trail")sr.gameObject.SetActive(false);
        return effect;
    }
    static Sprite LoadSprite(NativeSprite p)
    {
        Sprite sprite;if(sprites.TryGetValue(p.file,out sprite))return sprite;
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Path.Combine(Reference,p.file)));t.filterMode=FilterMode.Bilinear;
        sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(p.pivotX,p.pivotY),p.ppu,0,SpriteMeshType.FullRect);sprites[p.file]=sprite;return sprite;
    }
    static BattleUnitView Actor(string bundle,ActionDetail action,bool left,float scale,float x)
    {
        GameObject prefab;
        if(!prefabs.TryGetValue(bundle,out prefab)){var ab=AssetBundle.LoadFromFile(bundle);prefab=ab.LoadAllAssets<GameObject>().First(g=>g.GetComponent<CharacterAppearance>()!=null);prefabs[bundle]=prefab;}
        var root=new GameObject("ActualBattleUnitView");root.transform.SetParent(stage.transform,false);root.transform.position=new Vector3(x*scale,0,0);
        var view=root.AddComponent<BattleUnitView>();view.enabled=false;
        var rotation=new GameObject("characterRotationCenter").transform;rotation.SetParent(root.transform,false);rotation.localScale=Vector3.one*scale;rotation.localRotation=Quaternion.Euler(0,left?0:180,0);view.characterRotationCenter=rotation;
        var appearance=UnityEngine.Object.Instantiate(prefab,rotation,false).GetComponent<CharacterAppearance>();view.charAppearance=appearance;view.atkEffectRoot=appearance.atkEffectRoot;
        view.model=(BattleUnitModel)FormatterServices.GetUninitializedObject(typeof(BattleUnitModel));view.model.view=view;
        appearance.Initialize(Path.GetFileName(bundle));appearance.ChangeMotion(action);appearance.ChangeLayer("Character");
        foreach(var sr in appearance.GetComponentsInChildren<SpriteRenderer>(true))sr.sharedMaterial=AnhierTextureAssets.AlphaMaterial;
        return view;
    }
    static void Scene(float scale)
    {
        if(stage!=null)UnityEngine.Object.DestroyImmediate(stage);stage=new GameObject("ControlledNativeScene");
        // Actual native gun Initialize requires the native scene singleton. Awake and
        // gameplay startup are suppressed in this editor fixture; no battle is started.
        var sceneGo=new GameObject("BattleSceneRootContext");sceneGo.SetActive(false);sceneGo.transform.SetParent(stage.transform,false);
        var scene=sceneGo.AddComponent<BattleSceneRoot>();scene.enabled=false;
        typeof(SingletonBehavior<BattleSceneRoot>).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,scene);
        if(camera==null){camera=new GameObject("CommonCamera").AddComponent<Camera>();camera.orthographic=true;rt=new RenderTexture(960,540,24);camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;}
        camera.orthographicSize=11.5f*scale;camera.transform.position=new Vector3(0,2.8f*scale,-200);camera.farClipPlane=500;camera.backgroundColor=new Color(.16f,.17f,.2f);
    }
    static float[] Matrix(Transform t){var m=t.localToWorldMatrix;var f=new float[16];for(int i=0;i<16;i++)f[i]=m[i];return f;}
    static void Dispose(DiceAttackEffect e)
    {
        foreach(var sr in stage.GetComponentsInChildren<SpriteRenderer>(true))if(sr.name=="TargetWaterSplash")UnityEngine.Object.DestroyImmediate(sr.gameObject);
        if(e!=null)UnityEngine.Object.DestroyImmediate(e.gameObject);
    }
    static void OldComparison(BattleUnitView self,BattleUnitView target,string dir)
    {
        // Immutable old v1 source snapshot: old H was a detached target starburst.
        // Show source drawing at its measured old scalar on the identical action/camera.
        string root=Path.Combine(Output,"previous-v1/resources");if(!Directory.Exists(root))throw new Exception("v1 source snapshot missing");
        var p=JsonUtility.FromJson<AnhierTextureProfile>(File.ReadAllText(Path.Combine(root,"MemoryHit.json")));
        var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(Path.Combine(root,p.body[2])));
        var go=new GameObject("V1_MeasuredDetachedTargetBurst");go.transform.SetParent(stage.transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sharedMaterial=AnhierTextureAssets.AlphaMaterial;sr.sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(p.pivotX,p.pivotY),p.pixelsPerUnit);
        go.transform.position=target.atkEffectRoot.position;go.transform.localScale=new Vector3(-1.75f*1.12f,1.75f*1.12f,1);sr.sortingOrder=110;
        Save(Path.Combine(dir,"previous-v1.png"));UnityEngine.Object.DestroyImmediate(go);
    }
    static void Check(bool pass,string message){if(!pass)throw new Exception("FAIL "+message);checks.Add("PASS "+message);}
    static void Save(string file){camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=prior;}
}
