using System;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using Steria.WaterThunder;
using UnityEngine;

namespace Steria.Vfx
{
    internal static class SlazeyaWaterThunderAssets
    {
        internal const string BundleName="steria_slazeya_water_thunder_assets";
        private static AssetBundle bundle;
        private static readonly string[] Names={"L1_WaterBlade","L2_SplashWake","L3_Lightning","L4_BladeCore","WaterEvaporationScatter","WaterThunderImpactParticles"};
        private static readonly GameObject[] Prefabs=new GameObject[6];
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
                    else{visual.impact=child.AddComponent<WaterThunderImpact>();visual.impact.systems=child.GetComponentsInChildren<ParticleSystem>(true);}
                }
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
    private const float ArtScale=.55f;
    protected abstract ActionDetail PivotAction {get;}
    internal abstract BehaviourDetail AttackDetail {get;}
    private WaterThunderSlash visual;
    private BattleUnitView victim;
    private Vector3 contactWorld;
    private Quaternion visualRotation;
    private bool confirmed,cleared;
    public override void Initialize(BattleUnitView self,BattleUnitView target,float destroyTime)
    {
        if(self==null||self.model==null||self.charAppearance==null||target==null||target.atkEffectRoot==null)
            throw new InvalidOperationException("Water/thunder attack requires native self/target appearance");
        string suffix=PivotAction==ActionDetail.Slash?"J":PivotAction==ActionDetail.Hit?"H":"Z";
        gameObject.name="SlazeyaWaterThunder_"+suffix;additionalScale=Vector3.zero;offset=Vector2.zero;addedDestroyTime=0;
        if(spr==null)spr=GetComponent<SpriteRenderer>();if(animator==null)animator=GetComponent<Animator>();
        base.Initialize(self,target,Mathf.Max(.001f,destroyTime));
        _selfTransform=self.charAppearance.GetAtkEffectPivot(PivotAction);
        if(_selfTransform==null)throw new InvalidOperationException("Water/thunder native action pivot missing");
        transform.SetParent(_selfTransform,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
        _bHasDamagedEffect=false;spr.enabled=false;animator.enabled=false;victim=target;
        float direction=target.WorldPosition.x>=self.WorldPosition.x?1:-1;
        float scale=target.characterRotationCenter==null?1:Mathf.Max(.1f,Mathf.Abs(target.characterRotationCenter.lossyScale.x));
        // Match the visible body side, not Bada's far-ahead weapon pivot.
        contactWorld=new Vector3(target.WorldPosition.x-direction*.35f*scale,target.atkEffectRoot.position.y,target.atkEffectRoot.position.z);
        visual=Steria.Vfx.SlazeyaWaterThunderAssets.Create(transform);
        visual.transform.localScale=Vector3.one*ArtScale;visualRotation=visual.transform.rotation;
        visual.ApplyAt(0);Align();
        _destroyTime=Mathf.Max(_destroyTime,WaterThunderSlash.Duration);
        SetLayer("Effect");visual.gameObject.SetActive(true);
    }
    private void Align()
    {
        if(visual==null)return;
        visual.transform.rotation=visualRotation;
        visual.transform.position=contactWorld-visual.transform.TransformVector(WaterThunderMotion.Contact);
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

