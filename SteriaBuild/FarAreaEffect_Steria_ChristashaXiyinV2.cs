using System;
using System.Collections.Generic;
using Steria;
using Steria.Vfx;
using UnityEngine;

/// <summary>One tide per die. The native manager resolves results and supplies successful victims.</summary>
public sealed class FarAreaEffect_Steria_ChristashaXiyinV2 : FarAreaEffect
{
    private const float MissingCallbackDeadline = 1.8f;
    private static readonly float ReleaseTime = ChristashaXiyinTide.ReleaseTime;
    private readonly HashSet<BattleUnitModel> _seen = new HashSet<BattleUnitModel>();
    private BattlePlayingCardDataInUnitModel _card;
    private BattleDiceBehavior _behavior;
    private ChristashaXiyinTide _tide;
    private float _clock;
    private bool _callbackReceived;

    public override bool HasIndependentAction { get { return false; } }

    public override void Init(BattleUnitModel self, params object[] args)
    {
        ClearVisual();
        base.Init(self, args);
        _seen.Clear();
        _clock = 0f;
        _callbackReceived = false;
        _card = self == null ? null : self.currentDiceAction;
        _behavior = _card == null ? null : _card.currentBehavior;
        isRunning = true;
        try
        {
            if (!OwnerIsCurrent()) { Finish(); return; }
            var subjects = new List<BattleUnitView>();
            var manager = Singleton<BattleFarAreaPlayManager>.Instance;
            if (manager != null && ReferenceEquals(manager.attacker, self) && manager.victims != null)
                foreach (BattleFarAreaPlayManager.VictimInfo victim in manager.victims)
                {
                    if (victim == null || victim.unitModel == null || ReferenceEquals(victim.unitModel, self)) continue;
                    var view = victim.unitModel.view;
                    if (view != null && !subjects.Contains(view)) subjects.Add(view);
                }
            _tide = ChristashaXiyinTide.Create(transform, self.view, subjects);
            if (_tide == null) isRunning = false;
        }
        catch (Exception error) { isRunning = false; LogVisualError(error); }
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        if (_isDoneEffect || _callbackReceived) return;
        _callbackReceived = true;
        isRunning = false;
        try
        {
            if (!OwnerIsCurrent() || _tide == null) { Finish(); return; }
            if (damagedUnitList == null) return;
            foreach (BattleUnitModel unit in damagedUnitList)
            {
                if (unit == null || unit.view == null || ReferenceEquals(unit, _self) || !_seen.Add(unit)) continue;
                _tide.Confirm(unit.view, _clock);
            }
        }
        catch (Exception error) { LogVisualError(error); Finish(); }
    }

    protected override void Update()
    {
        if (_isDoneEffect) return;
        try
        {
            if (!OwnerIsCurrent()) { Finish(); return; }
            _clock += Mathf.Max(0f, Time.deltaTime);
            if (_clock + .00001f >= ReleaseTime) isRunning = false;
            if (_tide != null) _tide.Present(_clock);
            if ((_callbackReceived && (_tide == null || _tide.Complete(_clock))) ||
                (!_callbackReceived && _clock >= MissingCallbackDeadline)) Finish();
        }
        catch (Exception error) { LogVisualError(error); Finish(); }
    }

    private bool OwnerIsCurrent()
    {
        return _self != null && _self.view != null && _self.view.gameObject.activeInHierarchy &&
            !_self.IsDead() && _card != null && _behavior != null &&
            ReferenceEquals(_self.currentDiceAction, _card) && ReferenceEquals(_card.currentBehavior, _behavior);
    }

    private void ClearVisual()
    {
        if (_tide != null) _tide.Dispose();
        _tide = null;
    }

    private void Finish()
    {
        isRunning = false;
        _isDoneEffect = true;
        ClearVisual();
        Destroy(gameObject);
    }

    private static void LogVisualError(Exception error) { SteriaLogger.LogError("ChristashaXiyinV2 visual: " + error); }

    protected override void OnDisable()
    {
        base.OnDisable();
        _isDoneEffect = true;
        ClearVisual();
        Destroy(gameObject);
    }

    private void OnDestroy() { isRunning = false; _isDoneEffect = true; ClearVisual(); }
}
