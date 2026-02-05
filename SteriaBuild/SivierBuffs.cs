using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steria;

// 希维尔相关Buff - 需要在全局命名空间中

/// <summary>
/// 梦 Buff - 希维尔的核心机制
/// 通过书页消耗梦，为全队施加"愿望之盾"（每层梦=2点护盾）
/// </summary>
public class BattleUnitBuf_Dream : BattleUnitBuf
{
    protected override string keywordId => "SteriaDream";
    protected override string keywordIconId => "SteriaDream";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

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

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        // 梦不会自动消失，只在特定情况下消耗
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
