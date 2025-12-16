using System;
using UnityEngine;
using Steria;

/// <summary>
/// 水系斩击特效骰子 - 在拼点和命中时触发特效
/// </summary>
public class DiceCardAbility_SteriaWaterSlashEffect : DiceCardAbilityBase
{
    private bool _effectTriggered = false;

    public override void OnWinParrying() => TriggerEffectOnce();
    public override void OnLoseParrying() => TriggerEffectOnce();
    public override void OnDrawParrying() => TriggerEffectOnce();
    public override void BeforeGiveDamage() => TriggerEffectOnce();

    private void TriggerEffectOnce()
    {
        if (_effectTriggered) return;
        _effectTriggered = true;

        try
        {
            BattleUnitView selfView = this.owner?.view;
            BattleUnitView targetView = this.card?.target?.view;
            if (selfView == null) return;

            var effectObj = new GameObject("Steria_WaterSlash");
            var effect = effectObj.AddComponent<DiceAttackEffect_Steria_WaterSlash>();
            effect?.Initialize(selfView, targetView, 1f);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WaterSlashEffect error: {ex.Message}");
        }
    }
}

/// <summary>
/// 水系打击特效骰子 - 在拼点和命中时触发特效
/// </summary>
public class DiceCardAbility_SteriaWaterHitEffect : DiceCardAbilityBase
{
    private bool _effectTriggered = false;

    public override void OnWinParrying() => TriggerEffectOnce();
    public override void OnLoseParrying() => TriggerEffectOnce();
    public override void OnDrawParrying() => TriggerEffectOnce();
    public override void BeforeGiveDamage() => TriggerEffectOnce();

    private void TriggerEffectOnce()
    {
        if (_effectTriggered) return;
        _effectTriggered = true;

        try
        {
            BattleUnitView selfView = this.owner?.view;
            BattleUnitView targetView = this.card?.target?.view;
            if (selfView == null) return;

            var effectObj = new GameObject("Steria_WaterHit");
            var effect = effectObj.AddComponent<DiceAttackEffect_Steria_WaterHit>();
            effect?.Initialize(selfView, targetView, 1f);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WaterHitEffect error: {ex.Message}");
        }
    }
}

/// <summary>
/// 水系突刺特效骰子 - 在拼点和命中时触发特效
/// </summary>
public class DiceCardAbility_SteriaWaterPenetrateEffect : DiceCardAbilityBase
{
    private bool _effectTriggered = false;

    public override void OnWinParrying() => TriggerEffectOnce();
    public override void OnLoseParrying() => TriggerEffectOnce();
    public override void OnDrawParrying() => TriggerEffectOnce();
    public override void BeforeGiveDamage() => TriggerEffectOnce();

    private void TriggerEffectOnce()
    {
        if (_effectTriggered) return;
        _effectTriggered = true;

        try
        {
            BattleUnitView selfView = this.owner?.view;
            BattleUnitView targetView = this.card?.target?.view;
            if (selfView == null) return;

            var effectObj = new GameObject("Steria_WaterPenetrate");
            var effect = effectObj.AddComponent<DiceAttackEffect_Steria_WaterPenetrate>();
            effect?.Initialize(selfView, targetView, 1f);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WaterPenetrateEffect error: {ex.Message}");
        }
    }
}
