using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// 希维尔被动能力 - 需要在全局命名空间中

/// <summary>
/// 梦之汐音 (ID: 9008001)
/// - 拼点时不消耗梦
/// - 受到单方面攻击时，消耗1层梦减少3点伤害
/// - 每消耗3层梦，下回合获得1层守护
/// </summary>
public class PassiveAbility_9008001 : PassiveAbilityBase
{
    // 追踪本场战斗消耗的梦层数
    private int _dreamConsumedTotal = 0;
    private int _protectionGranted = 0;

    // 追踪是否正在被单方面攻击
    private bool _isBeingOneSided = false;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _dreamConsumedTotal = 0;
        _protectionGranted = 0;
        _isBeingOneSided = false;
    }

    /// <summary>
    /// 被单方面攻击开始时
    /// </summary>
    public override void OnStartTargetedOneSide(BattlePlayingCardDataInUnitModel attackerCard)
    {
        base.OnStartTargetedOneSide(attackerCard);
        _isBeingOneSided = true;
        SteriaLogger.Log($"PassiveAbility_9008001: OneSide attack started");
    }

    /// <summary>
    /// 被单方面攻击结束时
    /// </summary>
    public override void OnEndOneSideVictim(BattlePlayingCardDataInUnitModel attackerCard)
    {
        base.OnEndOneSideVictim(attackerCard);
        _isBeingOneSided = false;
        SteriaLogger.Log($"PassiveAbility_9008001: OneSide attack ended");
    }

    /// <summary>
    /// 受到单方面攻击时，消耗1层梦减少3点伤害
    /// </summary>
    public override int GetDamageReductionAll()
    {
        // 只在单方面攻击时触发，不包括拼点失败后的伤害
        if (!_isBeingOneSided) return 0;

        BattleUnitBuf dreamBuf = SivierCardHelper.GetDreamBuf(owner);

        if (dreamBuf != null && dreamBuf.stack > 0)
        {
            // 消耗1层梦
            dreamBuf.stack--;
            _dreamConsumedTotal++;
            SteriaLogger.Log($"PassiveAbility_9008001: Consumed 1 Dream for OneSide damage reduction, total consumed: {_dreamConsumedTotal}");

            // 通知追踪系统
            DiceCardSelfAbility_SivierWishBuried.OnDreamConsumed(owner, 1);

            CheckProtectionGrant();

            if (dreamBuf.stack <= 0)
            {
                dreamBuf.Destroy();
            }

            return 3; // 减少3点伤害
        }
        return 0;
    }

    /// <summary>
    /// 检查是否应该获得守护
    /// </summary>
    private void CheckProtectionGrant()
    {
        int shouldHaveProtection = _dreamConsumedTotal / 3;
        if (shouldHaveProtection > _protectionGranted)
        {
            int newProtection = shouldHaveProtection - _protectionGranted;
            owner.bufListDetail.AddBuf(new BattleUnitBuf_SivierProtectionNextTurn { protectionStacks = newProtection });
            _protectionGranted = shouldHaveProtection;
            SteriaLogger.Log($"PassiveAbility_9008001: Queued {newProtection} Protection for next turn");
        }
    }

    /// <summary>
    /// 通知梦被消耗（由其他效果调用）
    /// </summary>
    public void OnDreamConsumed(int amount)
    {
        _dreamConsumedTotal += amount;
        CheckProtectionGrant();
    }
}

/// <summary>
/// 散落的愿望 (ID: 9008002)
/// - 使用防御骰子拼点胜利时，获得1层梦
/// </summary>
public class PassiveAbility_9008002 : PassiveAbilityBase
{
    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        base.OnWinParrying(behavior);

        // 检查是否是防御骰子
        if (behavior.Detail == BehaviourDetail.Guard || behavior.Detail == BehaviourDetail.Evasion)
        {
            // 获得1层梦
            BattleUnitBuf dreamBuf = SivierCardHelper.GetDreamBuf(owner);

            if (dreamBuf != null)
            {
                dreamBuf.stack++;
            }
            else
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_Dream { stack = 1 });
            }
            SteriaLogger.Log($"PassiveAbility_9008002: Gained 1 Dream from defense parry win");
        }
    }
}

/// <summary>
/// 梦境共鸣 (ID: 9008003)
/// - 回合开始时，若拥有5层以上的梦
/// - 全队友方单位获得1层守护
/// </summary>
public class PassiveAbility_9008003 : PassiveAbilityBase
{
    public override void OnRoundStart()
    {
        base.OnRoundStart();

        BattleUnitBuf dreamBuf = SivierCardHelper.GetDreamBuf(owner);

        if (dreamBuf != null && dreamBuf.stack >= 5)
        {
            // 为全队友方单位获得1层守护
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 1, owner);
            }
            SteriaLogger.Log($"PassiveAbility_9008003: Granted 1 Protection to all allies (Dream stacks: {dreamBuf.stack})");
        }
    }
}

/// <summary>
/// 加速音律 (ID: 9008004)
/// - 每回合使超过2个敌方速度骰子改变目标后：下回合获得1层迅捷
/// </summary>
public class PassiveAbility_9008004 : PassiveAbilityBase
{
    private int _redirectCount = 0;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _redirectCount = 0;
    }

    /// <summary>
    /// 当敌方速度骰子改变目标时调用
    /// </summary>
    public void OnEnemySpeedDiceRedirected()
    {
        _redirectCount++;
        SteriaLogger.Log($"PassiveAbility_9008004: Enemy speed dice redirected, count: {_redirectCount}");

        if (_redirectCount > 2)
        {
            // 下回合获得1层迅捷（只触发一次）
            if (_redirectCount == 3)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_SivierQuicknessNextTurn { quicknessStacks = 1 });
                SteriaLogger.Log($"PassiveAbility_9008004: Queued 1 Quickness for next turn");
            }
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        _redirectCount = 0;
    }
}
