using System;
using System.Collections.Generic;
using Steria;
using UnityEngine;

/// <summary>Cast -> native release gate -> successful-target points -> finish. Never settles damage.</summary>
public sealed class FarAreaEffect_Steria_ChristashaXiyinV2 : FarAreaEffect
{
    private const float AreaStartTime = .3f;
    // Area begins at .30; its native crest is held through .75 before combat resolves.
    private const float ReleaseTime = .75f;
    private const float MissingCallbackDeadline = 1.8f;
    private const int MaxTargetPoints = 16;
    private readonly List<ChristashaParticleInstance> _instances = new List<ChristashaParticleInstance>();
    private readonly HashSet<BattleUnitModel> _seen = new HashSet<BattleUnitModel>();
    private BattlePlayingCardDataInUnitModel _card;
    private BattleDiceBehavior _behavior;
    private float _clock;
    private bool _released;
    private bool _areaStarted;
    private bool _callbackReceived;
    private Vector3 _castPosition;
    private Quaternion _castRotation;

    public override bool HasIndependentAction { get { return false; } }

    public override void Init(BattleUnitModel self, params object[] args)
    {
        ClearInstances();
        base.Init(self, args);
        _seen.Clear();
        _clock = 0f;
        _released = false;
        _areaStarted = false;
        _callbackReceived = false;
        _card = self == null ? null : self.currentDiceAction;
        _behavior = _card == null ? null : _card.currentBehavior;
        isRunning = true;
        try
        {
            if (!OwnerIsCurrent()) { Finish(); return; }
            _castPosition = self.view.WorldPosition;
            Transform facing = self.view.characterRotationCenter ?? self.view.transform;
            Vector3 left = facing.TransformPoint(new Vector3(-1f, 0f, 0f)) - facing.position;
            // Flip about Y, never Z: the three rising strings stay above the floor.
            _castRotation = Quaternion.Euler(0f, left.x > 0f ? 0f : 180f, 0f);
            ChristashaParticleInstance cast = ChristashaParticleInstance.CreateWorld("Cast", _castPosition, _castRotation, Vector3.one);
            Add(cast);
            // Missing visual resources must not hold the manager's pre-damage gate.
            if (cast == null) Release();
        }
        catch (Exception error) { FailSoft(error); }
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        if (_isDoneEffect || _callbackReceived) return;
        _callbackReceived = true; // Empty and duplicate callbacks are still terminal events.
        isRunning = false;
        try
        {
            if (!OwnerIsCurrent()) { Finish(); return; }
            Release();
            if (damagedUnitList != null)
            {
                foreach (BattleUnitModel unit in damagedUnitList)
                {
                    if (_seen.Count >= MaxTargetPoints) break;
                    if (unit == null || unit.view == null || !_seen.Add(unit)) continue;
                    // Read the target at the actual native result callback, including
                    // lethal hits; dead targets may still have a valid visible view.
                    Vector3 position = unit.view.atkEffectRoot != null ? unit.view.atkEffectRoot.position : unit.view.WorldPosition;
                    Add(ChristashaParticleInstance.CreateWorld("Point", position, Quaternion.identity, Vector3.one));
                }
            }
            if (_instances.Count == 0) Finish();
        }
        catch (Exception error) { FailSoft(error); }
    }

    protected override void Update()
    {
        if (_isDoneEffect) return;
        try
        {
            if (!OwnerIsCurrent()) { Finish(); return; }
            _clock += Mathf.Max(0f, Time.deltaTime);
            if (!_areaStarted && _clock >= AreaStartTime) BeginArea();
            if (!_released && _clock >= ReleaseTime) Release();
            for (int index = _instances.Count - 1; index >= 0; index--)
            {
                ChristashaParticleInstance instance = _instances[index];
                instance.Advance(Time.deltaTime);
                if (!instance.Complete) continue;
                instance.Dispose();
                _instances.RemoveAt(index);
            }
            if ((_callbackReceived && _instances.Count == 0) || (!_callbackReceived && _clock >= MissingCallbackDeadline)) Finish();
        }
        catch (Exception error) { FailSoft(error); }
    }

    private void Release()
    {
        if (_released) return;
        _released = true;
        isRunning = false; // BattleFarAreaPlayManager may now resolve all combat results once.
        BeginArea();
    }

    private void BeginArea()
    {
        if (_areaStarted) return;
        _areaStarted = true;
        float minX = _castPosition.x, maxX = _castPosition.x, ground = _castPosition.y;
        BattleFarAreaPlayManager manager = Singleton<BattleFarAreaPlayManager>.Instance;
        if (manager != null && ReferenceEquals(manager.attacker, _self) && manager.victims != null)
        {
            foreach (BattleFarAreaPlayManager.VictimInfo victim in manager.victims)
            {
                if (victim == null || victim.unitModel == null || victim.unitModel.view == null) continue;
                Vector3 point = victim.unitModel.view.WorldPosition;
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                ground = Mathf.Min(ground, point.y);
            }
        }
        // This is an affected-area cue, not a success cue: the complete native
        // victim extent is used even when every defender wins their clash.
        Vector3 center = new Vector3((minX + maxX) * .5f, ground, _castPosition.z);
        Vector3 scale = new Vector3(Mathf.Max(1f, (maxX - minX + 2f) / Steria.Vfx.ChristashaNativeMassRecipes.AreaDesignWidth), 1f, 1f);
        Add(ChristashaParticleInstance.CreateWorld("Area", center, _castRotation, scale));
    }

    private bool OwnerIsCurrent()
    {
        return _self != null && _self.view != null && _self.view.gameObject.activeInHierarchy &&
            !_self.IsDead() && _card != null && _behavior != null &&
            ReferenceEquals(_self.currentDiceAction, _card) && ReferenceEquals(_card.currentBehavior, _behavior);
    }

    private void Add(ChristashaParticleInstance instance) { if (instance != null) _instances.Add(instance); }

    private void ClearInstances()
    {
        foreach (ChristashaParticleInstance instance in _instances) instance.Dispose();
        _instances.Clear();
    }

    private void Finish()
    {
        isRunning = false;
        _isDoneEffect = true;
        ClearInstances();
        UnityEngine.Object.Destroy(gameObject);
    }

    private void FailSoft(Exception error)
    {
        SteriaLogger.LogError("ChristashaXiyinV2 visual: " + error);
        Finish();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _isDoneEffect = true;
        ClearInstances();
        // These effects are deliberately never pooled; a disabled dice must not
        // leave its root behind when the manager replaces its effect reference.
        UnityEngine.Object.Destroy(gameObject);
    }

    private void OnDestroy() { isRunning = false; _isDoneEffect = true; ClearInstances(); }
}
