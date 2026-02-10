using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// 希维尔相关Buff - 需要在全局命名空间中

/// <summary>
/// 梦 Buff - 希维尔的核心机制
/// 拼点时：消耗1层梦使敌人骰子威力-1
/// 受到单方面攻击时：消耗1层梦减少3点伤害（由被动9008001处理）
/// 拥有被动9008001时：拼点不消耗梦
/// </summary>
public class BattleUnitBuf_Dream : BattleUnitBuf
{
    protected override string keywordId => "SteriaDream";
    protected override string keywordIconId => "SteriaDream";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 标记：本次拼点是否已经触发过梦效果（防止同一骰子重复触发）
    private BattleDiceBehavior _lastTriggeredDice = null;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_Dream: Init for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        SteriaLogger.Log($"BattleUnitBuf_Dream: Added {addedStack} stacks, total: {this.stack}");
    }

    /// <summary>
    /// 拼点时：消耗1层梦使敌人骰子威力-1
    /// 如果拥有被动9008001（梦之汐音），则不消耗梦
    /// </summary>
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null || stack <= 0) return;
        if (_lastTriggeredDice == behavior) return;

        // 只在拼点时触发（有对手骰子时）
        BattleDiceBehavior targetDice = behavior.TargetDice;
        if (targetDice == null) return;

        _lastTriggeredDice = behavior;

        // 降低敌人骰子威力-1
        targetDice.ApplyDiceStatBonus(new DiceStatBonus { power = -1 });

        // 检查是否拥有被动9008001（拼点时不消耗梦）
        bool hasPassive9008001 = _owner?.passiveDetail?.PassiveList?
            .Any(p => p is PassiveAbility_9008001) ?? false;

        if (!hasPassive9008001)
        {
            // 消耗1层梦
            stack--;
            SteriaLogger.Log($"BattleUnitBuf_Dream: Consumed 1 stack (parry), remaining: {stack}");

            // 通知被动系统梦被消耗
            NotifyDreamConsumed(1);

            if (stack <= 0) this.Destroy();
        }
        else
        {
            SteriaLogger.Log($"BattleUnitBuf_Dream: Parry effect triggered without consuming (9008001)");
        }
    }

    /// <summary>
    /// 通知相关系统梦被消耗
    /// </summary>
    private void NotifyDreamConsumed(int amount)
    {
        if (_owner == null || amount <= 0) return;

        // 通知被动9008001追踪消耗
        var passive = _owner.passiveDetail?.PassiveList?
            .FirstOrDefault(p => p is PassiveAbility_9008001) as PassiveAbility_9008001;
        passive?.OnDreamConsumed(amount);

        // 通知愿望终将埋葬于深海追踪
        DiceCardSelfAbility_SivierWishBuried.OnDreamConsumed(_owner, amount);
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _lastTriggeredDice = null;
    }
}

/// <summary>
/// 愿望之盾 Buff - 抵抗伤害的护盾
/// 抵抗X点伤害(每抵抗1点使层数-1，若层数不足则照常受到剩余伤害)
/// </summary>
public class BattleUnitBuf_WishShield : BattleUnitBuf
{
    protected override string keywordId => "SteriaWishShield";
    protected override string keywordIconId => "SteriaWishShield";
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    private static System.Reflection.FieldInfo _guardReductionField;
    private readonly Dictionary<BattleDiceBehavior, int> _pendingAbsorb = new Dictionary<BattleDiceBehavior, int>();

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_WishShield: Init for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        SteriaLogger.Log($"BattleUnitBuf_WishShield: Added {addedStack} stacks, total: {this.stack}");
    }

    /// <summary>
    /// 受到伤害前，愿望之盾吸收伤害
    /// </summary>
    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        if (this.stack <= 0 || behavior == null) return 0;

        int guardReduction = 0;
        if (_guardReductionField == null)
        {
            _guardReductionField = typeof(BattleDiceBehavior).GetField("_damageReductionByGuard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
        if (_guardReductionField != null)
        {
            guardReduction = (int)_guardReductionField.GetValue(behavior);
        }

        int raw = behavior.DiceResultValue - guardReduction + behavior.DamageAdder;
        try
        {
            raw += behavior.owner?.UnitData?.unitData?.giftInventory?.GetStatBonus_Dmg(behavior.Detail) ?? 0;
        }
        catch
        {
        }

        if (raw < 0) raw = 0;
        int reduction = Math.Min(this.stack, raw);
        if (reduction <= 0) return 0;

        _pendingAbsorb[behavior] = reduction;
        SteriaLogger.Log($"BattleUnitBuf_WishShield: Providing {reduction} damage reduction (raw={raw})");
        return reduction;
    }

    /// <summary>
    /// 受到伤害后，减少护盾层数
    /// </summary>
    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);

        int absorbed = 0;
        if (atkDice != null && _pendingAbsorb.TryGetValue(atkDice, out absorbed))
        {
            _pendingAbsorb.Remove(atkDice);
        }
        else if (dmg > 0)
        {
            absorbed = Math.Min(dmg, this.stack);
        }

        if (this.stack > 0 && absorbed > 0)
        {
            this.stack -= absorbed;
            SteriaLogger.Log($"BattleUnitBuf_WishShield: Absorbed {absorbed} damage, remaining: {this.stack}");

            if (this.stack <= 0)
            {
                this.Destroy();
            }
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 愿望之盾在回合结束时不消失，持续到被消耗完
    }
}

/// <summary>
/// 下回合获得守护的辅助Buff
/// </summary>
public class BattleUnitBuf_SivierProtectionNextTurn : BattleUnitBuf
{
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    public int protectionStacks = 1;

    public override void OnRoundStart()
    {
        if (_owner != null && !_owner.IsDead())
        {
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, protectionStacks, _owner);
            SteriaLogger.Log($"SivierProtectionNextTurn: Granted {protectionStacks} Protection to {_owner.UnitData?.unitData?.name}");
        }
        this.Destroy();
    }
}

/// <summary>
/// 下回合获得迅捷的辅助Buff
/// </summary>
public class BattleUnitBuf_SivierQuicknessNextTurn : BattleUnitBuf
{
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    public int quicknessStacks = 1;

    public override void OnRoundStart()
    {
        if (_owner != null && !_owner.IsDead())
        {
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, quicknessStacks, _owner);
            SteriaLogger.Log($"SivierQuicknessNextTurn: Granted {quicknessStacks} Quickness to {_owner.UnitData?.unitData?.name}");
        }
        this.Destroy();
    }
}

/// <summary>
/// 敌人无法恢复光芒的Debuff（愿望终将埋葬于深海）
/// </summary>
public class BattleUnitBuf_NoLightRecovery : BattleUnitBuf
{
    protected override string keywordId => "SteriaNoLightRecovery";
    protected override string keywordIconId => "SteriaNoLightRecovery";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    private int _remainingActs = 2;

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        _remainingActs--;
        if (_remainingActs <= 0)
        {
            this.Destroy();
        }
    }
}
