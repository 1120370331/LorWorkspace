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
    private Transform _originPivot, _targetPivot;
    private BattleUnitView _selfView, _otherView;
    private float _powerScale, _actorScale = 1f;
    private bool _faceLeft;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        // Do not invoke name-heuristic base positioning or its inherited mirror/2x scale.
        _bHasDamagedEffect = false;
        _self = self == null ? null : self.model;
        _selfView = self; _otherView = target;
        if (spr != null) spr.enabled = false;
        if (animator != null) animator.enabled = false;
        try
        {
            _originPivot = Pivot(self, PivotAction);
            // Native damaged effects use the stable target root, not its attack pose pivot.
            _targetPivot = target == null ? null : target.atkEffectRoot;
            if (_targetPivot == null && target != null) _targetPivot = target.transform;
            _selfTransform = _originPivot; _targetTransform = _targetPivot;
            if (_originPivot == null) throw new InvalidOperationException("self action pivot missing");
            // Guard is always rooted on self (the successful defender), never target.
            transform.SetParent(null, true);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (self != null && self.characterRotationCenter != null)
                _actorScale = Mathf.Abs(self.characterRotationCenter.lossyScale.y);
            string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            string modRoot = Directory.GetParent(assemblyDir).FullName;
            var assets = AnhierTextureAssets.Load(Path.Combine(modRoot, "Resource", "Effects", "AnhierTextureCombat"), ProfileName);
            _timeline = gameObject.AddComponent<AnhierTextureTimeline>();
            _timeline.Initialize(assets, 8, 110);
            _destroyTime = assets.Profile.duration;
            _elapsed = 0;
            Render();
        }
        catch (Exception e)
        {
            // Keep native damaged feedback available and safely remove partial children.
            SteriaLogger.LogError("AnhierTexture " + ProfileName + " failed: " + e);
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private static Transform Pivot(BattleUnitView view, ActionDetail action)
    {
        if (view == null) return null;
        try { return view.charAppearance == null ? view.atkEffectRoot : view.charAppearance.GetAtkEffectPivot(action) ?? view.atkEffectRoot; }
        catch { return view.atkEffectRoot; }
    }

    public override void SetScale(float scaleFactor)
    {
        _powerScale = float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor) ? 0 : Mathf.Clamp(scaleFactor, 0, 2);
        if (_timeline != null) Render();
    }

    // Authored event-time duration stays independent of base animation speed overrides.
    public override void SetDestroyTime(float destroyTime) { }

    protected override void Update()
    {
        if (_timeline == null || _originPivot == null) { UnityEngine.Object.Destroy(gameObject); return; }
        _elapsed += Time.deltaTime;
        Render();
        if (_timeline.Complete) UnityEngine.Object.Destroy(gameObject);
    }

    private void Render()
    {
        if (_originPivot == null) return;
        Vector3 origin = _originPivot.position;
        Vector3 target = _targetPivot != null ? _targetPivot.position : origin + Vector3.right;
        if (_selfView != null && _otherView != null)
            _faceLeft = (_otherView.WorldPosition - _selfView.WorldPosition).x < 0;
        else if (_self != null) _faceLeft = _self.direction == Direction.LEFT;
        origin.z -= .04f; target.z -= .04f;
        _timeline.Sample(_elapsed, origin, target, _faceLeft, _actorScale, _powerScale);
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
