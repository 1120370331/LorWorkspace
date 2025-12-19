using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;

// 薇莉亚的战斗书页能力实现
// 这些类需要在全局命名空间中，以便游戏能够找到

// --- 薇莉亚 Card Self Abilities ---

/// <summary>
/// 和谐共生 - 使用时恢复1点光芒
/// </summary>
public class DiceCardSelfAbility_VeliaHarmony : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Recover 1 Light";

    public override void OnUseCard()
    {
        owner.cardSlotDetail.RecoverPlayPoint(1);
        Debug.Log($"[Steria] VeliaHarmony: Recovered 1 Light");
    }
}

/// <summary>
/// 传播希望 - 使用时抽2张牌
/// </summary>
public class DiceCardSelfAbility_VeliaSpreadHope : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Draw 2 pages";

    public override void OnUseCard()
    {
        owner.allyCardDetail.DrawCards(2);
        Debug.Log($"[Steria] VeliaSpreadHope: Drew 2 pages");
    }
}

/// <summary>
/// 振作起来 - 使用时为血量最低的友方单位恢复5点血量和混乱抗性（每有一层潮使此效果+1）
/// </summary>
public class DiceCardSelfAbility_VeliaCheerUp : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Heal the ally with lowest HP for 5 HP and BP (+ Tide stacks)";

    public override void OnUseCard()
    {
        if (BattleObjectManager.instance == null) return;

        // 获取潮层数
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus();
        }

        int healAmount = 5 + tideBonus;

        // 找到血量最低的友方单位
        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => !u.IsDead() && !u.IsBreakLifeZero())
            .OrderBy(u => u.hp)
            .ToList();

        if (allies.Count > 0)
        {
            BattleUnitModel target = allies[0];
            target.RecoverHP(healAmount);
            target.breakDetail.RecoverBreak(healAmount);
            Debug.Log($"[Steria] VeliaCheerUp: Healed {target.UnitData?.unitData?.name} for {healAmount} HP and BP");
        }
    }
}

/// <summary>
/// 潮雾晨光 - 使用时：下一幕开始时所有友方单位获得1层强壮，自身陷入眩晕并获得4层守护
/// </summary>
public class DiceCardSelfAbility_VeliaTideMist : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn all allies gain 1 Strength. Self becomes Staggered and gains 4 Protection";

    public override void OnUseCard()
    {
        if (BattleObjectManager.instance == null) return;

        // 给所有友方单位添加下回合获得强壮的Buff
        foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            ally.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, owner);
        }
        Debug.Log($"[Steria] VeliaTideMist: All allies will gain 1 Strength next turn");

        // 自身获得4层守护（下回合生效）
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 4, owner);
        Debug.Log($"[Steria] VeliaTideMist: Will gain 4 Protection next round");

        // 自身陷入眩晕（下回合生效）
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Stun, 1, owner);
        Debug.Log($"[Steria] VeliaTideMist: Will be Stunned next round");
    }
}

/// <summary>
/// 潜力观测 - EGO装备书页
/// 装备时提高友方目标目前最高的正面效果等级1级（受"潮"加成）
/// </summary>
public class DiceCardSelfAbility_VeliaPotentialObservation : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Increase target ally's highest positive buff by 1 (+ Tide bonus)";

    public override void OnUseCard()
    {
        // 检查是否已使用过
        var passive = owner.passiveDetail.PassiveList
            .FirstOrDefault(p => p is PassiveAbility_9004001) as PassiveAbility_9004001;

        if (passive != null && passive.HasUsedPotentialThisRound())
        {
            Debug.Log($"[Steria] VeliaPotentialObservation: Already used this round");
            return;
        }

        // 获取目标（如果是对自己使用，则选择随机友方）
        BattleUnitModel target = card.target;
        if (target == null || target.faction != owner.faction)
        {
            // 选择随机友方
            if (BattleObjectManager.instance != null)
            {
                var allies = BattleObjectManager.instance.GetAliveList(owner.faction)
                    .Where(u => !u.IsDead() && !u.IsBreakLifeZero())
                    .ToList();
                if (allies.Count > 0)
                {
                    target = allies[UnityEngine.Random.Range(0, allies.Count)];
                }
            }
        }

        if (target == null) return;

        // 应用效果
        if (passive != null)
        {
            passive.ApplyPotentialObservationEffect(target);
        }
        else
        {
            // 如果没有被动，直接应用基础效果
            ApplyBasicEffect(target);
        }
    }

    private void ApplyBasicEffect(BattleUnitModel target)
    {
        // 获取潮层数用于加成
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus();
        }

        int totalBonus = 1 + tideBonus;

        // 查找目标当前最高的正面效果
        var positiveBufs = target.bufListDetail.GetActivatedBufList()
            .Where(b => b.positiveType == BufPositiveType.Positive && b.stack > 0)
            .ToList();

        if (positiveBufs.Count == 0)
        {
            // 没有正面效果，给予强壮
            target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, totalBonus, owner);
            Debug.Log($"[Steria] VeliaPotentialObservation: Granted {totalBonus} Strength to {target.UnitData?.unitData?.name}");
        }
        else
        {
            // 找到层数最高的正面效果
            int maxStack = positiveBufs.Max(b => b.stack);
            var maxStackBufs = positiveBufs.Where(b => b.stack == maxStack).ToList();
            var selectedBuf = maxStackBufs[UnityEngine.Random.Range(0, maxStackBufs.Count)];
            selectedBuf.stack += totalBonus;
            Debug.Log($"[Steria] VeliaPotentialObservation: Increased {selectedBuf.GetType().Name} by {totalBonus}");
        }
    }
}

// --- 薇莉亚 Dice Abilities ---

/// <summary>
/// 和谐共生骰子 - 每拥有一层"潮"便使本骰子最小值+1
/// </summary>
public class DiceCardAbility_VeliaHarmonyDice : DiceCardAbilityBase
{
    public static string Desc = "Min roll +1 for each Tide stack";

    public override void BeforeRollDice()
    {
        // 获取潮层数
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        int tideStacks = tideBuf?.stack ?? 0;

        if (tideStacks > 0 && behavior != null)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { min = tideStacks });
            Debug.Log($"[Steria] VeliaHarmonyDice: Applied +{tideStacks} min roll from Tide");
        }
    }
}

/// <summary>
/// 潮雾晨光骰子1 - 命中时为随机一名友方单位恢复2点体力
/// </summary>
public class DiceCardAbility_VeliaTideMistHealHP : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] A random ally recovers 2 HP";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (BattleObjectManager.instance == null) return;

        var allies = BattleObjectManager.instance.GetAliveList(owner.faction)
            .Where(u => !u.IsDead())
            .ToList();

        if (allies.Count > 0)
        {
            BattleUnitModel randomAlly = allies[UnityEngine.Random.Range(0, allies.Count)];
            randomAlly.RecoverHP(2);
            Debug.Log($"[Steria] VeliaTideMistHealHP: {randomAlly.UnitData?.unitData?.name} recovered 2 HP");
        }
    }
}

/// <summary>
/// 潮雾晨光骰子2 - 命中时为随机一名友方单位恢复2点混乱抗性
/// </summary>
public class DiceCardAbility_VeliaTideMistHealBP : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] A random ally recovers 2 BP";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (BattleObjectManager.instance == null) return;

        var allies = BattleObjectManager.instance.GetAliveList(owner.faction)
            .Where(u => !u.IsDead())
            .ToList();

        if (allies.Count > 0)
        {
            BattleUnitModel randomAlly = allies[UnityEngine.Random.Range(0, allies.Count)];
            randomAlly.breakDetail.RecoverBreak(2);
            Debug.Log($"[Steria] VeliaTideMistHealBP: {randomAlly.UnitData?.unitData?.name} recovered 2 BP");
        }
    }
}
