using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// ========================================================
// 神备EGO：日沐曦渊 - 卡牌能力
// ========================================================

#region ===== 不要害怕，我在梦中 (9010001) =====

/// <summary>
/// 不要害怕，我在梦中 - 书页能力
/// 回合开始时：获得4层梦，本幕自身所有防御型骰子威力+4
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoDontFear : DiceCardSelfAbilityBase
{
    public override void OnStartBattle()
    {
        base.OnStartBattle();
        if (owner == null) return;

        // 获得4层梦
        BattleUnitBuf_Dream dreamBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
        if (dreamBuf != null) dreamBuf.stack += 4;
        else
        {
            var newDream = new BattleUnitBuf_Dream { stack = 4 };
            owner.bufListDetail.AddBuf(newDream);
        }
    }

    // 防御型骰子威力+4在BeforeRollDice中处理
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null) return;
        if (behavior.Type == BehaviourType.Def || behavior.Type == BehaviourType.Standby)
        {
            // Standby包含反击型骰子中的防御部分
            if (behavior.Detail == BehaviourDetail.Guard || behavior.Detail == BehaviourDetail.Evasion)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 4 });
            }
        }
    }
}

/// <summary>
/// 不要害怕，我在梦中 - 骰子1: 拼点失败时对自身施加5层烧伤
/// </summary>
public class DiceCardAbility_ChristashaEgoDontFearDice1 : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        if (owner == null) return;
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 5, owner);
    }
}

/// <summary>
/// 反击骰子 - 拼点胜利: 获得1层潮
/// </summary>
public class DiceCardAbility_ChristashaEgoCounterGainTide1 : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 1);
    }
}

#endregion

#region ===== 埃利耶，水中的圣职 (9010003) =====

/// <summary>
/// 埃利耶，水中的圣职 - 书页能力
/// 使用时：接下来两回合每回合开始时获得1层强壮以及3层流
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoEliyeHoly : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        BattleUnitBuf_EliyeHolyBuff buf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_EliyeHolyBuff) as BattleUnitBuf_EliyeHolyBuff;
        if (buf != null) buf.stack = Math.Max(buf.stack, 2);
        else { buf = new BattleUnitBuf_EliyeHolyBuff { stack = 2 }; owner.bufListDetail.AddBuf(buf); }
    }
}

/// <summary>
/// 埃利耶 - 命中时将1层潮转化为黄金之潮
/// </summary>
public class DiceCardAbility_ChristashaEgoConvertTide1OnHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        ChristashaAbilityHelper.ConvertTideToGolden(owner, 1);
    }
}

#endregion

#region ===== 渐明 (9010004) =====

/// <summary>
/// 渐明 - 书页能力
/// 使用时：对敌方施加1层日光
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoDawnApproach : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        BattleUnitModel target = card?.target;
        if (target != null)
        {
            ChristashaEgoBossHelper.AddSunlight(target, 1, owner);
        }
    }
}

/// <summary>
/// 渐明/随流而来 - 命中时施加1层日光
/// </summary>
public class DiceCardAbility_ChristashaEgoApplySunlight1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        ChristashaEgoBossHelper.AddSunlight(target, 1, owner);
    }
}

#endregion

#region ===== 金乌 (9010005) =====

/// <summary>
/// 金乌 - 书页能力
/// 使用时：对敌方施加1层致盲
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoGoldenCrow : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        BattleUnitModel target = card?.target;
        if (target != null)
        {
            ChristashaEgoBossHelper.AddBlinding(target, 1, owner);
        }
    }
}

/// <summary>
/// 金乌 - 骰子1: 命中时施加4层烧伤
/// </summary>
public class DiceCardAbility_ChristashaEgoBurn4 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 4, owner);
    }
}

/// <summary>
/// 金乌 - 骰子2: 拼点失败时消除对方燃烧层数
/// </summary>
public class DiceCardAbility_ChristashaEgoRemoveTargetBurn : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        BattleUnitModel target = ResolveParryTarget();
        if (target == null)
        {
            return;
        }

        int removed = ChristashaEgoBossHelper.RemoveAllBurnStacks(target);
        if (removed > 0)
        {
            SteriaLogger.Log($"ChristashaEgoGoldenCrow: Removed {removed} Burn stacks from {target.UnitData?.unitData?.name}");
        }
    }

    private BattleUnitModel ResolveParryTarget()
    {
        BattleDiceBehavior targetDice = behavior?.TargetDice;
        if (targetDice?.owner != null)
        {
            return targetDice.owner;
        }

        BattlePlayingCardDataInUnitModel playingCard = behavior?.card;
        if (playingCard?.target != null && playingCard.target != owner)
        {
            return playingCard.target;
        }

        BattleUnitModel cardTarget = card?.target;
        if (cardTarget != null && cardTarget != owner)
        {
            return cardTarget;
        }

        return null;
    }
}

/// <summary>
/// 金乌 - 骰子3: 拼点失败时自身获得5层烧伤
/// </summary>
public class DiceCardAbility_ChristashaEgoSelfBurn5OnLose : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        if (owner == null) return;
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 5, owner);
    }
}

#endregion

#region ===== 迸发 (9010007) =====

/// <summary>
/// 迸发 - 书页能力（群体清算）
/// 本单位每有5层烧伤，使本书页骰子威力+1
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoBurst : DiceCardSelfAbilityBase
{
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null || owner == null) return;
        int ownBurn = owner.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
        int bonus = ownBurn / 5;
        if (bonus > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = bonus });
        }
    }
}

#endregion

#region ===== 黎明之光 (9010008) =====

/// <summary>
/// 黎明之光 - 书页能力（群体交锋）
/// [渐强] 每次使用后骰子威力+2（最多+6）
/// 本书页不造成伤害
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoDawnLight : DiceCardSelfAbilityBase
{
    private static readonly Dictionary<BattleUnitModel, int> _growthByOwner = new Dictionary<BattleUnitModel, int>();

    public override void OnUseCard()
    {
        // 渐强：记录成长
        if (owner == null) return;
        if (!_growthByOwner.ContainsKey(owner)) _growthByOwner[owner] = 0;
        // 实际增长在战斗结束后应用
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null || owner == null) return;
        // 渐强加成
        int growth = _growthByOwner.ContainsKey(owner) ? _growthByOwner[owner] : 0;
        if (growth > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = growth });
        }
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior)
    {
        base.BeforeGiveDamage(behavior);
        if (behavior == null) return;
        // 本书页不造成伤害
        behavior.ApplyDiceStatBonus(new DiceStatBonus { dmg = -9999 });
    }

    public override void OnEndAreaAttack()
    {
        base.OnEndAreaAttack();
        ApplyGrowth();
    }

    public override void AfterAction()
    {
        base.AfterAction();
        ApplyGrowth();
    }

    private void ApplyGrowth()
    {
        if (owner == null) return;
        if (!_growthByOwner.ContainsKey(owner)) _growthByOwner[owner] = 0;
        if (_growthByOwner[owner] < 6) // 最多+6
        {
            _growthByOwner[owner] += 2;
            if (_growthByOwner[owner] > 6) _growthByOwner[owner] = 6;
        }
    }

    public static void ClearGrowth()
    {
        _growthByOwner.Clear();
    }
}

/// <summary>
/// 黎明之光 - 命中时施加3层烧伤
/// </summary>
public class DiceCardAbility_ChristashaEgoDawnLightBurn3 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 3, owner);
    }

    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 3, owner);
    }
}

#endregion

#region ===== Phase 2 Cards =====

/// <summary>
/// 随流而来，伴流而去 (9010009) - 书页能力
/// 流转。回合开始时：获得4层流，本幕进攻型骰子造成的伤害+4
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoFlowComeGo : DiceCardSelfAbilityBase
{
    public override void OnStartBattle()
    {
        base.OnStartBattle();
        if (owner == null) return;
        // 获得4层流
        BattleUnitBuf_Flow flow = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        if (flow != null) flow.stack += 4;
        else owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 4 });
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior)
    {
        base.BeforeGiveDamage(behavior);
        if (behavior == null) return;
        if (behavior.Type == BehaviourType.Atk)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmg = 4 });
        }
    }
}

/// <summary>
/// 深渊之中，太阳升起 (9010010) - 书页能力
/// 流转。回合开始时：获得4层潮，本幕所有单位受流威力加成+1
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoAbyssSunrise : DiceCardSelfAbilityBase
{
    public override void OnStartBattle()
    {
        base.OnStartBattle();
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 4);
        HarmonyPatches.AddGlobalFlowPowerBonusThisRound(1, owner);
    }
}

/// <summary>
/// 命中时施加1层致盲
/// </summary>
public class DiceCardAbility_ChristashaEgoApplyBlinding1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        ChristashaEgoBossHelper.AddBlinding(target, 1, owner);
    }
}

/// <summary>
/// 命中时摧毁本骰子（反击骰子使用一次后失效）
/// </summary>
public class DiceCardAbility_ChristashaEgoDestroyDice : DiceCardAbilityBase
{
    private bool _destroyRequested;

    private void TryDestroySelfDice(string trigger)
    {
        if (_destroyRequested)
        {
            return;
        }

        _destroyRequested = true;

        if (card != null && behavior != null)
        {
            List<BattleDiceBehavior> list = card.GetDiceBehaviorList();
            int idx = list?.IndexOf(behavior) ?? -1;

            if (idx < 0 && list != null)
            {
                int behaviorIdx = behavior.Index;
                if (behaviorIdx >= 0 && behaviorIdx < list.Count && ReferenceEquals(list[behaviorIdx], behavior))
                {
                    idx = behaviorIdx;
                }
            }

            if (idx >= 0)
            {
                card.DestroyDice(match => match.index == idx);
                SteriaLogger.Log($"ChristashaEgoDestroyDice: Destroyed counter dice index={idx}, trigger={trigger}");
            }
            else
            {
                SteriaLogger.Log($"ChristashaEgoDestroyDice: index resolve failed, trigger={trigger}");
            }
        }

        // 仅通过卡牌索引摧毁当前骰子；避免直接销毁行为对象导致误伤后续待使用骰子
    }

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        TryDestroySelfDice("OnSucceedAttack(target)");
    }

    public override void OnWinParrying()
    {
        base.OnWinParrying();
        TryDestroySelfDice("OnWinParrying");
    }

    public override void OnLoseParrying()
    {
        base.OnLoseParrying();
        TryDestroySelfDice("OnLoseParrying");
    }

    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        TryDestroySelfDice("OnSucceedAttack()");
    }
}

/// <summary>
/// 我将和黎明一起到来 (9010011) - 书页能力
/// 群体清算。所有单位每有10层烧伤使威力+1
/// 使用时：为所有敌人施加1层日光和致盲，自身获得6层烧伤
/// </summary>
public class DiceCardSelfAbility_ChristashaEgoDawnArrival : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null || BattleObjectManager.instance == null) return;

        // 为所有敌人施加1层日光和致盲
        Faction enemyFaction = (owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy;
        foreach (var enemy in BattleObjectManager.instance.GetAliveList(enemyFaction))
        {
            if (enemy == null) continue;
            ChristashaEgoBossHelper.AddSunlight(enemy, 1, owner);
            ChristashaEgoBossHelper.AddBlinding(enemy, 1, owner);
        }

        // 自身获得6层烧伤
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 6, owner);
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null) return;
        int totalBurn = ChristashaEgoBossHelper.GetTotalBurnStacks();
        int bonus = totalBurn / 10;
        if (bonus > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = bonus });
        }
    }
}

#endregion
