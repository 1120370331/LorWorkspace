using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Battle.DiceAttackEffect;
using Steria.Vfx;
using UnityEngine;

class RunContract
{
    static BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static GameObject scene;
    static string[] names = { "Slash", "Hit", "Pierce", "Guard", "FarHit" };
    static ActionDetail[] actions = { ActionDetail.Slash, ActionDetail.Hit, ActionDetail.Penetrate, ActionDetail.Guard, ActionDetail.Fire };
    static void Main(string[] args)
    {
        string assembly = Path.GetDirectoryName(typeof(RunContract).Assembly.Location);
        string dir = Path.Combine(Directory.GetParent(assembly).FullName, "Assemblies/AB");
        Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,SlazeyaWindParticleAssets.BundleName),"MANAGED ASSET API DOUBLE; NOT A REAL BUNDLE");
        ProductionFactory.Init();
        if(args.Contains("--sorting-only"))
        {
            Scene();var sf=Create("Slash",Actor(true,3,5),Actor(false,3,-5),0);
            var renderers=sf.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Check(renderers.Length==2&&renderers.All(r=>r.sortingLayerName=="Particle"),"inactive native PS renderers use Particle sorting layer");
            Check(renderers[0].sortingOrder==0&&renderers[1].sortingOrder==2,"authored sortingOrder is preserved");
            Console.WriteLine("SLAZEYA_WIND_SORTING_CONTRACT_PASS "+count+" checks; managed adapter boundary only");return;
        }
        foreach(bool left in new[]{true,false}) foreach(float map in new[]{1.5f,3f}) foreach(float roll in new[]{0f,1f})
        for(int i=0;i<names.Length;i++)
        {
            Scene();var self=Actor(left,map,5);var target=Actor(!left,map,-6.5f);var fx=Create(names[i],self,target,roll);var visual=Visual(fx);
            Check(fx.GetType().Name=="DiceAttackEffect_Steria_SlazeyaWind"+names[i],"factory exact type "+names[i]);
            Check(fx.transform.parent==(i==4?scene.transform:self.atkEffectRoot),"native Bada null action-pivot fallback "+names[i]);
            Check(Math.Abs(fx.transform.localScale.x-(i==3||i==4?1:2)*(1+roll))<.0001f,"native scale once "+names[i]);
            Check(!fx.HasDamagedEffect,"no unconditional damage feedback "+names[i]);
            Check(fx.GetComponentsInChildren<Transform>().All(t=>t.gameObject.layer==8),"layer propagation "+names[i]);
            if(i!=4)Check(Vector3.Distance(fx.transform.TransformPoint(Vector3.right),self.atkEffectRoot.TransformPoint(Vector3.right*((i==3?1:2)*(1+roll))))<.0001f,"complete native facing/map matrix "+names[i]);
            Tick(fx,.04f);float elapsed=visual.Elapsed;fx.SetDestroyTime(3);Check(visual.Elapsed==elapsed,"extension does not replay");
            fx.SetDestroyTime(.03f);Check(visual.StopCount==1&&fx.gameObject.destroyed,"shortened lifetime clears once");
        }
        // Explicit action pivots, including distinct F, must override Bada's absent references.
        for(int i=0;i<names.Length;i++)
        {
            Scene();var self=Actor(true,3,5);var target=Actor(false,3,-6.5f);var pivot=new GameObject("ExplicitPivot").transform;pivot.SetParent(self.charAppearance.transform,false);pivot.localPosition=new Vector3(-1,2,0);pivot.localRotation=Quaternion.Euler(0,0,18);pivot.localScale=new Vector3(1.15f,.9f,1);
            string suffix=i==0?"J":i==1?"H":i==2?"Z":i==3?"G":"F";typeof(CharacterAppearance).GetField("atkEffectPivot_"+suffix).SetValue(self.charAppearance,pivot);
            var fx=Create(names[i],self,target,0,.01f);Check(Life(fx)>=Visual(fx).Duration,"initialize accommodates visual duration");
            Check(i==4?Vector3.Distance(fx.transform.position,pivot.position)<.0001f:fx.transform.parent==pivot,"explicit native action pivot "+names[i]);
            Call(fx,"OnDisable");Check(Visual(fx).StopCount==1,"disable clears");Call(fx,"OnDestroy");Check(Visual(fx).StopCount==1,"destroy after disable is idempotent");
        }
        foreach(bool left in new[]{true,false}) foreach(float separation in new[]{.01f,12f})
        {
            Scene();var self=Actor(left,3,5);var target=Actor(!left,3,-6.5f);target.atkEffectRoot.position=self.atkEffectRoot.position+new Vector3(left?-separation:separation,.2f,0);
            var fx=Create("FarHit",self,target,1);var visual=Visual(fx);var start=fx.transform.position;
            for(int j=0;j<13;j++)
            {
                if(j==3){self.atkEffectRoot.position+=new Vector3(3,0,0);target.atkEffectRoot.position+=new Vector3(0,.2f,0);}
                Tick(fx,.02f);float t=Mathf.Clamp01((j+1)*.02f/.26f);float u=1-(1-t)*(1-t);
                Check(Vector3.Distance(fx.transform.position,Vector3.Lerp(start,target.atkEffectRoot.position,u))<.0001f,"frozen start, tracked target, bounded ease-out");
                var direction=fx.transform.TransformPoint(new Vector3(-1,0,0))-fx.transform.position;var delta=target.atkEffectRoot.position-start;
                Check(direction.x*delta.x+direction.y*delta.y>0,"projectile authored -X faces target without actor mirror");
            }
            Check(Vector3.Distance(fx.transform.position,target.atkEffectRoot.position)<.0001f&&visual.ArrivalCount==1,"exact target arrival once");
            var arrival=fx.transform.position;target.atkEffectRoot.position+=new Vector3(9,0,0);Tick(fx,.02f);Check(Vector3.Distance(fx.transform.position,arrival)<.0001f&&visual.ArrivalCount==1,"arrival freezes world point without repeated arrival");
            target.atkEffectRoot.gameObject.SetActive(false);Tick(fx,.02f);Check(visual.StopCount==1&&fx.gameObject.destroyed,"missing target clears without arrival replay");
        }
        Scene();var a=Actor(true,3,5);var b=Actor(false,3,-6.5f);var cancelled=Create("FarHit",a,b,0);var cv=Visual(cancelled);cancelled.SetDestroyTime(0);Check(cv.ArrivalCount==0&&cv.StopCount==1,"cancel never calls Arrive");
        File.Delete(Path.Combine(dir,SlazeyaWindParticleAssets.BundleName));
        typeof(SlazeyaWindParticleAssets).GetField("_bundle",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
        var prefabs=typeof(SlazeyaWindParticleAssets).GetField("Prefabs",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as System.Collections.IDictionary;prefabs.Clear();
        var missing=Create("Slash",a,b,0);Check(missing.gameObject.destroyed&&Steria.SteriaLogger.Errors.Count==1,"missing bundle diagnoses and destroys");
        Console.WriteLine("SLAZEYA_WIND_MANAGED_CONTRACT_PASS "+count+" checks; production adapter + extracted native methods; driver/Unity APIs doubled, no GPU claim");
    }
    static void Scene(){scene=new GameObject("NeutralBattleSceneRoot");SingletonBehavior<BattleSceneRoot>.Instance=scene.AddComponent<BattleSceneRoot>();}
    static BattleUnitView Actor(bool left,float map,float x)
    {
        var root=new GameObject("BadaView");root.transform.SetParent(scene.transform,false);root.transform.position=new Vector3(x,0,0);var v=root.AddComponent<BattleUnitView>();var rotation=new GameObject("characterRotationCenter").transform;rotation.SetParent(root.transform,false);rotation.localScale=Vector3.one*map;rotation.localRotation=Quaternion.Euler(0,left?0:180,0);v.characterRotationCenter=rotation;
        var appearance=new GameObject("CharacterAppearance");appearance.transform.SetParent(rotation,false);v.charAppearance=appearance.AddComponent<CharacterAppearance>();var pivot=new GameObject("atkEffectRoot").transform;pivot.SetParent(appearance.transform,false);pivot.localPosition=new Vector3(-2.2f,2.34f,0);v.charAppearance.atkEffectRoot=pivot;v.atkEffectRoot=pivot;v.model=new BattleUnitModel{view=v};return v;
    }
    static DiceAttackEffect Create(string suffix,BattleUnitView self,BattleUnitView target,float roll,float lifetime=1)
    {DiceAttackEffect result=null;bool native=ProductionFactory.DiceEffectManager_CreateBehaviourEffect_Prefix(null,ref result,"Steria_SlazeyaWind"+suffix,roll,self,target,lifetime);Check(!native&&result!=null,"production factory intercept");return result;}
    static SlazeyaWindVisual Visual(DiceAttackEffect f)=>(SlazeyaWindVisual)typeof(DiceAttackEffect_Steria_SlazeyaWindBase).GetField("_visual",F).GetValue(f);
    static float Life(DiceAttackEffect f)=>(float)typeof(DiceAttackEffect).GetField("_destroyTime",F).GetValue(f);
    static void Tick(DiceAttackEffect fx,float dt){Time.deltaTime=dt;Call(fx,"Update");}
    static void Call(DiceAttackEffect fx,string method){fx.GetType().GetMethod(method,F).Invoke(fx,null);}
    static void Check(bool pass,string name){if(!pass)throw new Exception("FAIL "+name);count++;}
}
