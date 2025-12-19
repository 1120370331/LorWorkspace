using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steria;

// 倾诉梦想系列Buff - 由梦想教皇被动赋予
// 这些Buff需要在全局命名空间中，以便游戏能够找到

/// <summary>
/// 倾诉梦想：远望
/// 每消耗3点光芒，下回合获得1层"强壮"
/// </summary>
public class BattleUnitBuf_DreamVision : BattleUnitBuf
{
    protected override string keywordId => "SteriaDreamVision";
    protected override string keywordIconId => "DreamVision"; // 红色云朵图标
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 追踪本幕消耗的光芒
    private int _lightSpentThisRound = 0;
    // 追踪已经触发的强壮层数（防止重复计算）
    private int _strengthGrantedThisRound = 0;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        _lightSpentThisRound = 0;
        _strengthGrantedThisRound = 0;
        SteriaLogger.Log($"BattleUnitBuf_DreamVision: Init for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _lightSpentThisRound = 0;
        _strengthGrantedThisRound = 0;
    }

    /// <summary>
    /// 当光芒被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnLightSpent(int amount)
    {
        if (_owner == null || amount <= 0) return;

        _lightSpentThisRound += amount;
        SteriaLogger.Log($"DreamVision: {_owner.UnitData?.unitData?.name} spent {amount} light, total this round: {_lightSpentThisRound}");

        // 计算应该获得的强壮层数
        int strengthToGrant = _lightSpentThisRound / 3;
        int newStrength = strengthToGrant - _strengthGrantedThisRound;

        if (newStrength > 0)
        {
            // 下回合获得强壮（使用AddKeywordBufByEtc默认下回合生效）
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, newStrength, _owner);
            _strengthGrantedThisRound = strengthToGrant;
            SteriaLogger.Log($"DreamVision: Granted {newStrength} Strength (next turn) to {_owner.UnitData?.unitData?.name}");
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 每幕结束时销毁，由梦想教皇每幕重新分配
        this.Destroy();
    }
}

/// <summary>
/// 倾诉梦想：幻梦
/// 每使用1张书页，下回合恢复2点体力和混乱抗性
/// </summary>
public class BattleUnitBuf_DreamIllusion : BattleUnitBuf
{
    protected override string keywordId => "SteriaDreamIllusion";
    protected override string keywordIconId => "DreamIllusion"; // 绿色云朵图标
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 追踪本幕使用的书页数
    private int _cardsUsedThisRound = 0;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        _cardsUsedThisRound = 0;
        SteriaLogger.Log($"BattleUnitBuf_DreamIllusion: Init for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _cardsUsedThisRound = 0;
    }

    /// <summary>
    /// 当书页被使用时调用（由Harmony补丁触发）
    /// </summary>
    public void OnCardUsed()
    {
        if (_owner == null) return;

        _cardsUsedThisRound++;
        SteriaLogger.Log($"DreamIllusion: {_owner.UnitData?.unitData?.name} used a card, total this round: {_cardsUsedThisRound}");

        // 每使用1张书页，下回合恢复2点体力和混乱抗性
        // 使用辅助Buff来实现下回合恢复
        _owner.bufListDetail.AddBuf(new BattleUnitBuf_DreamIllusionHealNextTurn());
        SteriaLogger.Log($"DreamIllusion: Queued 2 HP/BP recovery for next turn");
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 每幕结束时销毁，由梦想教皇每幕重新分配
        this.Destroy();
    }
}

/// <summary>
/// 幻梦效果的辅助Buff - 下回合恢复体力和混乱抗性
/// </summary>
public class BattleUnitBuf_DreamIllusionHealNextTurn : BattleUnitBuf
{
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        if (_owner != null && !_owner.IsDead())
        {
            _owner.RecoverHP(2);
            _owner.breakDetail.RecoverBreak(2);
            SteriaLogger.Log($"DreamIllusionHeal: Recovered 2 HP and 2 BP for {_owner.UnitData?.unitData?.name}");
        }
        this.Destroy();
    }
}

/// <summary>
/// 倾诉梦想：奉行
/// 造成伤害时，下回合获得1层"流"
/// </summary>
public class BattleUnitBuf_DreamExecution : BattleUnitBuf
{
    protected override string keywordId => "SteriaDreamExecution";
    protected override string keywordIconId => "DreamExecution"; // 蓝色云朵图标
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 追踪本幕是否已经造成过伤害（每次造成伤害都触发）
    private int _damageDealtCount = 0;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        _damageDealtCount = 0;
        SteriaLogger.Log($"BattleUnitBuf_DreamExecution: Init for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _damageDealtCount = 0;
    }

    /// <summary>
    /// 当造成伤害时调用（由Harmony补丁触发）
    /// </summary>
    public void OnDamageDealt()
    {
        if (_owner == null) return;

        _damageDealtCount++;
        SteriaLogger.Log($"DreamExecution: {_owner.UnitData?.unitData?.name} dealt damage, count: {_damageDealtCount}");

        // 下回合获得1层流
        _owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 1 });
        SteriaLogger.Log($"DreamExecution: Queued 1 Flow for next turn");
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 每幕结束时销毁，由梦想教皇每幕重新分配
        this.Destroy();
    }
}

/// <summary>
/// 全体友方本幕不消耗流的标记Buff
/// 由"随我流向无尽的尽头"卡牌赋予
/// </summary>
public class BattleUnitBuf_NoFlowConsumption : BattleUnitBuf
{
    protected override string keywordId => "SteriaNoFlowConsumption";
    protected override string keywordIconId => "FlowLocked"; // 带锁的流图标
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 本幕结束时销毁
        this.Destroy();
    }
}
