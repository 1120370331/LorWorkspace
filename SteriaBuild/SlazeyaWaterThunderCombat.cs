using System;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using HarmonyLib;
using LOR_DiceSystem;
using Steria.WaterThunder;
using UnityEngine;

namespace Steria.Vfx
{
    /// <summary>Core identity owns ordinary melee appearance; card data remains unchanged.</summary>
    public static class SlazeyaWaterThunderDefaults
    {
        public static string Resolve(BattleUnitView view, BattleCardBehaviourResult result, ActionDetail action, string customResource)
        {
            if(result==null||!result.hasBehaviour)return customResource;
            var range=result.cardModel==null?result.diceBehaviourResult.range:result.cardModel.GetSpec().Ranged;
            return Resource(view==null?null:view.model,result.behaviourRawData,range,action,customResource)??customResource;
        }
        static string Resource(BattleUnitModel unit,DiceBehaviour dice,CardRange range,ActionDetail action,string customResource)
        {
            var id=unit==null||unit.equipment==null||unit.Book==null?null:unit.Book.BookId;
            if(id==null||id.packageId!="SteriaBuilding"||(id.id!=99000002&&id.id!=2)||dice==null||dice.Type!=BehaviourType.Atk||range!=CardRange.Near||
                dice.MotionDetail==MotionDetail.F||action==ActionDetail.Fire||customResource=="None"||dice.EffectRes=="None")return null;
            switch(dice.Detail)
            {
                case BehaviourDetail.Slash:return "Steria_SlazeyaWindSlash";
                case BehaviourDetail.Hit:return "Steria_SlazeyaWindHit";
                case BehaviourDetail.Penetrate:return "Steria_SlazeyaWindPierce";
                default:return null;
            }
        }
        public static bool TryCreateDirect(BattleUnitModel unit,float destroyTime,out DiceAttackEffect effect)
        {
            effect=null;var playing=unit==null?null:unit.currentDiceAction;
            if(playing==null||playing.card==null||playing.currentBehavior==null||unit.view==null||playing.target==null||playing.target.view==null)return false;
            var dice=playing.currentBehavior.behaviourInCard;
            var resource=Resource(unit,dice,playing.card.GetSpec().Ranged,ActionDetail.Default,null);
            if(resource==null)return false;
            float ratio=Mathf.Clamp01((float)playing.currentBehavior.DiceResultValue/Mathf.Max(1,dice.Dice));
            effect=SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource,ratio,unit.view,playing.target.view,destroyTime);
            return effect!=null;
        }
    }

    [HarmonyPatch(typeof(RencounterManager),"PrintEffect")]
    public static class SlazeyaCoreMeleePresentationPatch
    {
        static readonly FieldInfo Librarian=AccessTools.Field(typeof(RencounterManager),"_librarian"),Enemy=AccessTools.Field(typeof(RencounterManager),"_enemy"),
            LibrarianResult=AccessTools.Field(typeof(RencounterManager),"_currentLibrarianBehaviourResult"),EnemyResult=AccessTools.Field(typeof(RencounterManager),"_currentEnemyBehaviourResult");
        public static void Prefix(RencounterManager __instance,ActionDetail librarianActionDetail,ActionDetail enemyActionDetail,ref string liCustomRes,ref string enCustomRes)
        {
            liCustomRes=SlazeyaWaterThunderDefaults.Resolve((BattleUnitView)Librarian.GetValue(__instance),(BattleCardBehaviourResult)LibrarianResult.GetValue(__instance),librarianActionDetail,liCustomRes);
            enCustomRes=SlazeyaWaterThunderDefaults.Resolve((BattleUnitView)Enemy.GetValue(__instance),(BattleCardBehaviourResult)EnemyResult.GetValue(__instance),enemyActionDetail,enCustomRes);
        }
    }

    internal static class SlazeyaWaterThunderAssets
    {
        internal const string BundleName="steria_slazeya_water_thunder_assets";
        private static AssetBundle bundle;
        private static readonly string[] Names={"L1_WaterBlade","L2_SplashWake","L3_Lightning","L4_BladeCore","WaterEvaporationScatter","WaterThunderImpactParticles","WaterEdgeFoam"};
        private static readonly GameObject[] Prefabs=new GameObject[Names.Length];
        internal static WaterThunderSlash Create(Transform parent)
        {
            if(bundle==null)
            {
                string assembly=Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                string root=Directory.GetParent(assembly).FullName;
                string path=Path.Combine(root,"Assemblies","AB",BundleName);
                if(!File.Exists(path))throw new FileNotFoundException("Water/thunder AssetBundle missing",path);
                bundle=AssetBundle.LoadFromFile(path);if(bundle==null)throw new InvalidOperationException("Cannot load water/thunder bundle: "+path);
            }
            var rootObject=new GameObject("Slazeya Water Thunder - shared melee");rootObject.SetActive(false);rootObject.transform.SetParent(parent,false);
            var visual=rootObject.AddComponent<WaterThunderSlash>();visual.autoPlay=false;visual.mirror=true;visual.impactBurst=false;visual.sampleTime=0;
            try
            {
                for(int i=0;i<Names.Length;i++)
                {
                    if(Prefabs[i]==null)Prefabs[i]=bundle.LoadAsset<GameObject>(Names[i]);
                    if(Prefabs[i]==null)throw new InvalidOperationException("Missing water/thunder asset "+Names[i]);
                    var child=UnityEngine.Object.Instantiate(Prefabs[i],rootObject.transform,false);
                    if(i<4)visual.layers[i]=child.GetComponent<MeshRenderer>();
                    else if(i==4)visual.evaporationMist=child.GetComponent<MeshRenderer>();
                    else if(i==5){visual.impact=child.AddComponent<WaterThunderImpact>();visual.impact.systems=child.GetComponentsInChildren<ParticleSystem>(true);}
                    else visual.edgeFoam=child.GetComponent<ParticleSystem>();
                }
                if(visual.edgeFoam==null)throw new InvalidOperationException("Water/thunder edge foam binding missing");
                visual.splashSource=visual.layers[1].GetComponent<MeshFilter>().sharedMesh;
                foreach(var renderer in rootObject.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer.sharedMaterial==null||!renderer.sharedMaterial.shader.isSupported)throw new InvalidOperationException("Unsupported water/thunder material");
                    renderer.sortingLayerName="Particle";
                }
                return visual;
            }
            catch{UnityEngine.Object.Destroy(rootObject);throw;}
        }
    }
}

public abstract class DiceAttackEffect_Steria_SlazeyaWaterThunderBase : DiceAttackEffect
{
    // Authoring units to native 2x melee/map units; native roll scaling remains untouched.
    private const float ArtScale=.50f;
    protected abstract ActionDetail PivotAction {get;}
    internal abstract BehaviourDetail AttackDetail {get;}
    private WaterThunderSlash visual;
    private BattleUnitView victim;
    private bool confirmed,cleared;
    public override void Initialize(BattleUnitView self,BattleUnitView target,float destroyTime)
    {
        if(self==null||self.model==null||self.charAppearance==null||target==null||target.atkEffectRoot==null)
            throw new InvalidOperationException("Water/thunder attack requires native self/target appearance");
        string suffix=PivotAction==ActionDetail.Slash?"J":PivotAction==ActionDetail.Hit?"H":"Z";
        gameObject.name="SlazeyaWaterThunder_"+suffix;additionalScale=Vector3.zero;offset=Vector2.zero;addedDestroyTime=0;
        if(spr==null)spr=GetComponent<SpriteRenderer>();if(animator==null)animator=GetComponent<Animator>();
        base.Initialize(self,target,Mathf.Max(.001f,destroyTime));
        _selfTransform=self.characterRotationCenter;
        if(_selfTransform==null)throw new InvalidOperationException("Water/thunder character facing root missing");
        transform.SetParent(_selfTransform,false);transform.localPosition=new Vector3(-1.3f,0,0);transform.localRotation=Quaternion.identity;
        _bHasDamagedEffect=false;spr.enabled=false;animator.enabled=false;victim=target;
        visual=Steria.Vfx.SlazeyaWaterThunderAssets.Create(transform);
        Align();visual.ApplyAt(0);
        _destroyTime=Mathf.Max(_destroyTime,WaterThunderSlash.Duration);
        SetLayer("Effect");visual.gameObject.SetActive(true);
    }
    private void Align()
    {
        if(visual==null)return;
        visual.transform.localPosition=WaterThunderMotion.GameArtOffset(ArtScale);
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=new Vector3(-ArtScale,ArtScale,ArtScale);
    }
    public override void SetScale(float scaleFactor){base.SetScale(scaleFactor);Align();}
    public override void SetLayer(string layerName)
    {
        int layer=LayerMask.NameToLayer(layerName);if(layer<0)throw new InvalidOperationException("Native effect layer missing: "+layerName);
        base.SetLayer(layerName);foreach(var child in GetComponentsInChildren<Transform>(true))child.gameObject.layer=layer;
    }
    public override void SetDestroyTime(float destroyTime)
    {
        base.SetDestroyTime(destroyTime);if(_elapsed>=_destroyTime){Clear();UnityEngine.Object.Destroy(gameObject);}
    }
    internal bool Confirm(BehaviourDetail kind,BattleUnitView target)
    {
        if(cleared||visual==null||target==null||target!=victim||kind!=AttackDetail||!gameObject.activeInHierarchy)return false;
        confirmed=true;visual.impactBurst=true;return true;
    }
    protected override void Update()
    {
        if(cleared||visual==null)return;
        float dt=Mathf.Max(0,Time.deltaTime);
        if(_elapsed+dt>=_destroyTime){Clear();base.Update();return;}
        Align();visual.impactBurst=confirmed;visual.ApplyAt(_elapsed+dt);base.Update();
    }
    private void Clear()
    {
        if(cleared)return;cleared=true;SlazeyaWindImpactRouter.Invalidate(this);
        if(visual!=null){visual.ApplyAt(WaterThunderSlash.Duration);visual.gameObject.SetActive(false);UnityEngine.Object.Destroy(visual.gameObject);visual=null;}
        victim=null;
    }
    protected void OnDisable(){Clear();}
    protected override void OnDestroy(){Clear();base.OnDestroy();}
}

public sealed class DiceAttackEffect_Steria_SlazeyaWaterThunderSlash : DiceAttackEffect_Steria_SlazeyaWaterThunderBase
{protected override ActionDetail PivotAction{get{return ActionDetail.Slash;}}internal override BehaviourDetail AttackDetail{get{return BehaviourDetail.Slash;}}}
public sealed class DiceAttackEffect_Steria_SlazeyaWaterThunderHit : DiceAttackEffect_Steria_SlazeyaWaterThunderBase
{protected override ActionDetail PivotAction{get{return ActionDetail.Hit;}}internal override BehaviourDetail AttackDetail{get{return BehaviourDetail.Hit;}}}
public sealed class DiceAttackEffect_Steria_SlazeyaWaterThunderPierce : DiceAttackEffect_Steria_SlazeyaWaterThunderBase
{protected override ActionDetail PivotAction{get{return ActionDetail.Penetrate;}}internal override BehaviourDetail AttackDetail{get{return BehaviourDetail.Penetrate;}}}

