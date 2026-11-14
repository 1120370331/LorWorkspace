using System;
using Battle.DiceAttackEffect;
using Steria;
using Steria.Vfx;
using UnityEngine;

public abstract class DiceAttackEffect_Steria_SlazeyaWindBase : DiceAttackEffect
{
    private const float TravelDuration = .26f;
    protected abstract SlazeyaWindKind Kind { get; }
    protected abstract ActionDetail PivotAction { get; }
    private SlazeyaWindVisual _visual;
    private GameObject _instance;
    private Vector3 _start;
    private bool _arrived;
    private bool _cleared;
    private BattleUnitView _pierceTarget;
    private bool IsFar { get { return Kind == SlazeyaWindKind.Projectile; } }
    private bool IsPierce { get { return Kind == SlazeyaWindKind.Pierce; } }

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            if (self == null || self.model == null || self.charAppearance == null || target == null || target.atkEffectRoot == null)
                throw new InvalidOperationException("Slazeya wind native self/target appearance missing");
            string suffix = PivotAction == ActionDetail.Slash ? "J" : PivotAction == ActionDetail.Hit ? "H" :
                PivotAction == ActionDetail.Penetrate ? "Z" : PivotAction == ActionDetail.Guard ? "G" : "F";
            gameObject.name = "SlazeyaWind_" + suffix;
            additionalScale = Kind == SlazeyaWindKind.Guard ? Vector3.one * -.5f : Vector3.zero;
            offset = Vector2.zero;
            addedDestroyTime = 0f;
            if (spr == null) spr = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            base.Initialize(self, target, Mathf.Max(.001f, destroyTime));
            // Always ask the native appearance API, including F (absent in base name routing).
            _selfTransform = self.charAppearance.GetAtkEffectPivot(PivotAction);
            if (_selfTransform == null) throw new InvalidOperationException("Slazeya wind native action pivot/root missing");
            if (IsFar)
            {
                var scene = SingletonBehavior<BattleSceneRoot>.Instance;
                if (scene == null) throw new InvalidOperationException("Slazeya wind neutral scene root missing");
                _start = _selfTransform.position;
                transform.SetParent(scene.transform, false);
                transform.position = _start;
                transform.localScale = Vector3.one;
                Orient(target.atkEffectRoot.position - _start);
            }
            else
            {
                transform.SetParent(_selfTransform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                // base.Initialize already applied native melee 2x / Guard 1x exactly once.
            }
            _bHasDamagedEffect = false;
            spr.enabled = false;
            animator.enabled = false;
            _instance = SlazeyaWindParticleAssets.Instantiate(Kind, transform);
            bool particleLayerExists = false;
            foreach (SortingLayer layer in SortingLayer.layers) if (layer.name == "Particle") particleLayerExists = true;
            if (!particleLayerExists) throw new InvalidOperationException("Slazeya wind native Particle sorting layer missing");
            foreach (ParticleSystemRenderer renderer in _instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
                renderer.sortingLayerName = "Particle";
            _visual = new SlazeyaWindVisual(_instance, Kind, unchecked((uint)gameObject.GetInstanceID()));
            if(IsPierce){_pierceTarget=target;AimPierce();}
            _destroyTime = Mathf.Max(_destroyTime, _visual.Duration);
            SetLayer("Effect");
            _instance.SetActive(true);
        }
        catch (Exception error)
        {
            Clear();
            SteriaLogger.LogError("Slazeya wind " + Kind + " initialization failed: " + error);
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    // Preserve native (1 + roll) scale and native cancellation clock. Neither replays particles.
    public override void SetScale(float scaleFactor) { base.SetScale(scaleFactor);if(IsPierce)AimPierce(); }
    public override void SetLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) throw new InvalidOperationException("Slazeya wind layer unavailable: " + layerName);
        // Initialization deliberately keeps particle children inactive until the driver is ready.
        // Native SetLayer excludes inactive children, so cover that subtree explicitly.
        base.SetLayer(layerName);
        foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }
    public override void SetDestroyTime(float destroyTime)
    {
        base.SetDestroyTime(destroyTime);
        if (_elapsed >= _destroyTime) { Clear(); UnityEngine.Object.Destroy(gameObject); }
    }

    protected override void Update()
    {
        if (_cleared || _visual == null) return;
        float dt = Mathf.Max(0f, Time.deltaTime);
        if (_elapsed + dt >= _destroyTime) { Clear(); base.Update(); return; }
        if(IsPierce)
        {
            if(_pierceTarget==null||_targetTransform==null||!_targetTransform.gameObject.activeInHierarchy)
            {Clear();UnityEngine.Object.Destroy(gameObject);return;}
            AimPierce();
        }
        if (IsFar)
        {
            if (_targetTransform == null || !_targetTransform.gameObject.activeInHierarchy)
            { Clear(); UnityEngine.Object.Destroy(gameObject); return; }
            if (!_arrived)
            {
                Vector3 target = _targetTransform.position;
                float nextTime = _elapsed + dt;
                float u = nextTime + .000001f >= TravelDuration ? 1f : Mathf.Clamp01(nextTime / TravelDuration);
                float eased = 1f - (1f - u) * (1f - u);
                transform.position = Vector3.Lerp(_start, target, eased);
                Orient(target - _start);
                if (u >= 1f)
                {
                    transform.position = target;
                    _visual.Arrive();
                    _arrived = true;
                }
            }
        }
        _visual.Advance(dt);
        base.Update();
    }

    private void Orient(Vector3 direction)
    {
        if (direction.sqrMagnitude > .000001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180f);
    }

    private void AimPierce()
    {
        if(_visual==null||_instance==null||_pierceTarget==null||_targetTransform==null)return;
        Vector3 origin=_instance.transform.position,contact=_targetTransform.position;
        Vector3 delta=new Vector3(contact.x-origin.x,contact.y-origin.y,0);
        if(delta.sqrMagnitude<.000001f)delta=new Vector3(_pierceTarget.WorldPosition.x-origin.x,contact.y-origin.y,0);
        if(delta.sqrMagnitude<.000001f)delta=_instance.transform.TransformVector(Vector3.left);
        delta.z=0;
        float distance=Mathf.Max(.001f,delta.magnitude);
        Vector3 direction=delta/distance;
        float victimScale=_pierceTarget.characterRotationCenter==null?1f:Mathf.Abs(_pierceTarget.characterRotationCenter.lossyScale.x);
        victimScale=Mathf.Max(.1f,victimScale);
        // Bada's attack pivot sits far in front of its body. Keep the native
        // impact height, but put this contact ring on the chest's facing side.
        contact.x=_pierceTarget.WorldPosition.x-direction.x*victimScale*.35f;
        delta=new Vector3(contact.x-origin.x,contact.y-origin.y,0);
        distance=delta.magnitude;
        if(distance<.001f){contact=origin+direction*.01f;delta=contact-origin;distance=delta.magnitude;}
        direction=delta/distance;
        Vector3 center=new Vector3(_pierceTarget.WorldPosition.x,contact.y,contact.z);
        float exitDistance=Mathf.Max(distance+victimScale*.9f,Vector3.Dot(center-origin,direction)+victimScale*1.6f);
        Vector3 exit=origin+direction*exitDistance;exit.z=contact.z;
        _visual.AimPierce(contact,exit,victimScale*1.5f);
    }

    internal bool TryConfirmPierceImpact(BattleUnitView target)
    {
        if(!IsPierce||_cleared||_visual==null||target==null||target!=_pierceTarget||
            target.atkEffectRoot!=_targetTransform||!gameObject.activeInHierarchy)return false;
        AimPierce();return _visual.ConfirmPierceImpact();
    }

    private void Clear()
    {
        if (_cleared) return;
        _cleared = true;
        SlazeyaWindImpactRouter.Invalidate(this);
        if (_visual != null) _visual.StopAndClear();
        if (_instance != null) { _instance.SetActive(false); UnityEngine.Object.Destroy(_instance); }
        _pierceTarget=null;
    }
    protected void OnDisable() { Clear(); }
    protected override void OnDestroy() { Clear(); base.OnDestroy(); }
}

public sealed class DiceAttackEffect_Steria_SlazeyaWindSlash : DiceAttackEffect_Steria_SlazeyaWindBase
{ protected override SlazeyaWindKind Kind { get { return SlazeyaWindKind.Slash; } } protected override ActionDetail PivotAction { get { return ActionDetail.Slash; } } }
public sealed class DiceAttackEffect_Steria_SlazeyaWindHit : DiceAttackEffect_Steria_SlazeyaWindBase
{ protected override SlazeyaWindKind Kind { get { return SlazeyaWindKind.Hit; } } protected override ActionDetail PivotAction { get { return ActionDetail.Hit; } } }
public sealed class DiceAttackEffect_Steria_SlazeyaWindPierce : DiceAttackEffect_Steria_SlazeyaWindBase
{ protected override SlazeyaWindKind Kind { get { return SlazeyaWindKind.Pierce; } } protected override ActionDetail PivotAction { get { return ActionDetail.Penetrate; } } }
public sealed class DiceAttackEffect_Steria_SlazeyaWindGuard : DiceAttackEffect_Steria_SlazeyaWindBase
{ protected override SlazeyaWindKind Kind { get { return SlazeyaWindKind.Guard; } } protected override ActionDetail PivotAction { get { return ActionDetail.Guard; } } }
public sealed class DiceAttackEffect_Steria_SlazeyaWindFarHit : DiceAttackEffect_Steria_SlazeyaWindBase
{ protected override SlazeyaWindKind Kind { get { return SlazeyaWindKind.Projectile; } } protected override ActionDetail PivotAction { get { return ActionDetail.Fire; } } }
