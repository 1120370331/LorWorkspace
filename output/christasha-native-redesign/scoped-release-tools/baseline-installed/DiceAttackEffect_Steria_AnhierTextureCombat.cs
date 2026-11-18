using System;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using Steria;
using UnityEngine;

public abstract class DiceAttackEffect_Steria_AnhierTextureBase : DiceAttackEffect
{
    protected abstract string ProfileName { get; }
    protected abstract ActionDetail PivotAction { get; }
    private AnhierTextureTimeline _timeline;
    private float _animationDuration;
    private bool IsFar { get { return PivotAction == ActionDetail.Fire; } }

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            if (self == null || self.model == null || self.charAppearance == null || target == null)
                throw new InvalidOperationException("native self/target appearance missing");
            // The native name heuristic examines only the last underscore component.
            string suffix = PivotAction == ActionDetail.Slash ? "J" : PivotAction == ActionDetail.Hit ? "H" :
                PivotAction == ActionDetail.Guard ? "G" : IsFar ? "F" : "Z";
            gameObject.name = "SteriaAnhierTexture_" + suffix;
            additionalScale = Vector3.one * (IsFar ? 1f : PivotAction == ActionDetail.Guard ? -.5f : 0f);
            offset = Vector2.zero;
            addedDestroyTime = 0f;
            if (spr == null) spr = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            base.Initialize(self, target, Mathf.Max(.001f, destroyTime) * (IsFar ? .5f : 1f));
            if (IsFar)
            {
                // Base H/J/Z/G heuristic has no F branch; the native appearance API does.
                _selfTransform = self.charAppearance.GetAtkEffectPivot(ActionDetail.Fire);
                Transform sceneRoot = SingletonBehavior<BattleSceneRoot>.Instance.transform;
                transform.SetParent(sceneRoot, false);
                transform.position = _selfTransform.position;
                transform.rotation = _selfTransform.rotation;
                // Serialized RedHood Gun additionalScale=(1,1,1): local 2, then
                // base.SetScale(roll). Scene ownership deliberately excludes actor map scale.
                transform.localScale = Vector3.one * 2f;
            }
            // Native Animator.speed is set only by Initialize, not SetDestroyTime.
            _animationDuration = _destroyTime;
            _bHasDamagedEffect = false;
            spr.enabled = false;
            animator.enabled = false;
            string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            string modRoot = Directory.GetParent(assemblyDir).FullName;
            var assets = AnhierTextureAssets.Load(Path.Combine(modRoot, "Resource", "Effects", "AnhierTextureCombat"), ProfileName);
            int layer = LayerMask.NameToLayer("Effect");
            if (layer < 0) throw new InvalidOperationException("native Effect GameObject layer unavailable");
            _timeline = gameObject.AddComponent<AnhierTextureTimeline>();
            _timeline.Initialize(assets, layer, IsFar ? target.atkEffectRoot : null);
            Render();
        }
        catch (Exception e)
        {
            SteriaLogger.LogError("AnhierTexture " + ProfileName + " failed: " + e);
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    public override void SetScale(float scaleFactor)
    {
        base.SetScale(scaleFactor);
        Render();
    }

    public override void SetDestroyTime(float destroyTime)
    {
        base.SetDestroyTime(Mathf.Max(.001f, destroyTime));
        Render();
    }

    public override void SetLayer(string layerName)
    {
        base.SetLayer(layerName);
        if (_timeline != null) _timeline.SetLayer(LayerMask.NameToLayer(layerName));
    }

    protected override void Update()
    {
        // Visual fade ends before object lifetime for native melee. Base destruction
        // remains authoritative, including caller SetDestroyTime changes.
        base.Update();
        Render();
    }

    private void Render()
    {
        if (_timeline != null) _timeline.Sample(_elapsed, _animationDuration);
    }
}

public sealed class DiceAttackEffect_Steria_AnhierTextureSeaPierce : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "SeaPierce"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Penetrate; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureSeaFarHit : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "SeaFarHit"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Fire; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureMemorySlash : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "MemorySlash"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Slash; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureMemoryPierce : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "MemoryPierce"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Penetrate; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureMemoryHit : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "MemoryHit"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Hit; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureMemoryGuard : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "MemoryGuard"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Guard; } } }

public sealed class DiceAttackEffect_Steria_AnhierTextureSeaSlash : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "SeaSlash"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Slash; } } }
public sealed class DiceAttackEffect_Steria_AnhierTextureSeaHit : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "SeaHit"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Hit; } } }

public sealed class DiceAttackEffect_Steria_AnhierTextureSeaGuard : DiceAttackEffect_Steria_AnhierTextureBase
{ protected override string ProfileName { get { return "SeaGuard"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Guard; } } }
