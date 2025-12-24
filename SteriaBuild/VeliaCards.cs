using LOR_DiceSystem;
using System;
using System.Collections;
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

        // 获取潮层数并消耗
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus(); // 消耗潮获得加成
        }

        int healAmount = 5 + tideBonus;

        // 找到血量最低的友方单位（排除自己）
        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => u != owner && !u.IsDead() && !u.IsBreakLifeZero())
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
/// 自身每层"潮"使本书页骰子威力+1
/// </summary>
public class DiceCardSelfAbility_VeliaTideMist : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn all allies gain 1 Strength. Self becomes Staggered and gains 4 Protection. Dice Power +1 per Tide stack.";

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        // 获取潮层数（不消耗）
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        int tideStacks = tideBuf?.stack ?? 0;

        if (tideStacks > 0 && behavior != null)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = tideStacks });
            Debug.Log($"[Steria] VeliaTideMist: Applied +{tideStacks} dice power from Tide");
        }
    }

    public override void OnUseCard()
    {
        if (BattleObjectManager.instance == null) return;

        // 获取潮层数并消耗
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus(); // 消耗潮获得加成
        }

        int strengthAmount = 1 + tideBonus;

        // 给所有友方单位添加强壮（受潮加成）
        // 注意：传入null作为actor，避免Harmony补丁重复消耗潮（我们已经在上面手动消耗了）
        foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            ally.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, strengthAmount, null);
        }
        Debug.Log($"[Steria] VeliaTideMist: All allies gained {strengthAmount} Strength (1 + {tideBonus} Tide, consumed)");

        // 自身获得4层守护（下回合生效）
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 4, owner);
        Debug.Log($"[Steria] VeliaTideMist: Will gain 4 Protection next round");

        // 自身获得4层坚韧（下回合生效，混乱减伤）
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.BreakProtection, 4, owner);
        Debug.Log($"[Steria] VeliaTideMist: Will gain 4 BreakProtection next round");

        // 自身陷入眩晕（下回合生效）
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Stun, 1, owner);
        Debug.Log($"[Steria] VeliaTideMist: Will be Stunned next round");
    }
}

/// <summary>
/// 潜力观测 - EGO装备书页
/// 装备时提高友方目标目前最高的正面效果等级1级（受"潮"加成）
/// 只能选中友方单位
/// 冷却时间：1幕（使用Steria自定义冷却系统）
/// </summary>
public class DiceCardSelfAbility_VeliaPotentialObservation : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Increase target ally's highest positive buff by 1 (+ Tide bonus). Cooldown: 1 round.";

    // 卡牌ID
    public const int CARD_ID = 9004005;
    // 冷却时间常量
    public const int COOLDOWN_ROUNDS = 1;

    // 限制只能选中友方单位
    public override bool IsOnlyAllyUnit()
    {
        return true;
    }

    // 对于Instance类型的EGO装备书页，使用OnUseInstance而不是OnUseCard
    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        Debug.Log($"[Steria] VeliaPotentialObservation: OnUseInstance called, target={targetUnit?.UnitData?.unitData?.name}");

        // 检查自定义冷却
        if (Steria.SteriaEgoCooldownManager.IsOnCooldown(unit, CARD_ID))
        {
            int remaining = Steria.SteriaEgoCooldownManager.GetRemainingCooldown(unit, CARD_ID);
            Debug.Log($"[Steria] VeliaPotentialObservation: On cooldown, {remaining} rounds remaining");
            return;
        }

        // 验证目标是友方
        if (targetUnit == null || targetUnit.faction != unit.faction)
        {
            Debug.Log($"[Steria] VeliaPotentialObservation: Invalid target (not ally)");
            return;
        }

        // 切换到Special动作，然后延迟切回
        if (unit.view?.charAppearance != null)
        {
            unit.view.charAppearance.ChangeMotion(ActionDetail.Special);
            // 启动协程延迟切回Standing
            unit.view.StartCoroutine(DelayedResetMotion(unit.view.charAppearance, 1.0f));
        }

        // 应用效果
        ApplyEffect(unit, targetUnit);

        // 设置自定义冷却
        Steria.SteriaEgoCooldownManager.StartCooldown(unit, CARD_ID);
    }

    // 认可的正面效果列表
    private static readonly KeywordBuf[] _validPositiveBuffs = new KeywordBuf[]
    {
        KeywordBuf.Strength,    // 强壮
        KeywordBuf.Protection,  // 守护
        KeywordBuf.Quickness,   // 迅捷
        KeywordBuf.Endurance,   // 忍耐（加防御骰子威力）
        KeywordBuf.BreakProtection, // 振奋（降低混乱伤害）
        KeywordBuf.SlashPowerUp,     // 斩击威力提升
        KeywordBuf.PenetratePowerUp, // 突刺威力提升
        KeywordBuf.HitPowerUp,       // 打击威力提升
        KeywordBuf.DefensePowerUp,   // 防御威力提升
    };

    private void ApplyEffect(BattleUnitModel unit, BattleUnitModel target)
    {
        // 获取潮层数用于加成
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus();
        }

        int totalBonus = 1 + tideBonus;

        // 只查找认可的正面效果（KeywordBuf类型）
        var validBufs = new List<BattleUnitBuf>();
        foreach (var buf in target.bufListDetail.GetActivatedBufList())
        {
            if (buf.stack > 0 && _validPositiveBuffs.Contains(buf.bufType))
            {
                validBufs.Add(buf);
            }
        }

        if (validBufs.Count == 0)
        {
            // 没有认可的正面效果，给予强壮
            target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, totalBonus, null);
            Debug.Log($"[Steria] VeliaPotentialObservation: Granted {totalBonus} Strength to {target.UnitData?.unitData?.name}");
        }
        else
        {
            // 找到层数最高的正面效果
            int maxStack = validBufs.Max(b => b.stack);
            var maxStackBufs = validBufs.Where(b => b.stack == maxStack).ToList();
            var selectedBuf = maxStackBufs[UnityEngine.Random.Range(0, maxStackBufs.Count)];
            selectedBuf.stack += totalBonus;
            Debug.Log($"[Steria] VeliaPotentialObservation: Increased {selectedBuf.GetType().Name} by {totalBonus}");
        }
    }

    /// <summary>
    /// 延迟切回Standing动作的协程
    /// </summary>
    private IEnumerator DelayedResetMotion(CharacterAppearance appearance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (appearance != null)
        {
            appearance.ChangeMotion(ActionDetail.Standing);
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
