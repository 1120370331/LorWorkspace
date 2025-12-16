using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steria; // For CardAbilityHelper and SteriaLogger

// --- Dice Card Abilities (Effects on Dice) ---

#region 拙劣控流 (ID: 9001001) Dice 1
public class DiceCardAbility_AnhierFlow1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 获得1层[流]";
    public override string[] Keywords => new string[] { "SteriaFlow" };
    public override void OnSucceedAttack()
    {
        CardAbilityHelper.AddFlowStacks(this.owner, 1);
    }
}
#endregion

// 珍贵的回忆 (ID: 9001006) On Use
// 主要效果在 Harmony 补丁的弃牌逻辑里处理，这里保持脚本存在以匹配 XML
public class DiceCardSelfAbility_AnhierPreciousDiscard : DiceCardSelfAbilityBase
{
    public static string Desc = "[佚亡][被弃置时] 摧毁本书页，并抽取2张书页并在下一幕获得1层[强壮]";
    // 不需要在 OnUseCard 里做额外处理，弃置效果由 Harmony 补丁统一监听
}

#region 回忆侧斩 (ID: 9001002) Dice 1
public class DiceCardAbility_AnhierDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 抽取1张书页";
    public override void OnSucceedAttack()
    {
        SteriaLogger.Log($"[AnhierDraw1] OnSucceedAttack triggered! Owner={this.owner?.UnitData?.unitData?.name}");
        this.owner.allyCardDetail.DrawCards(1);
        SteriaLogger.Log($"[AnhierDraw1] DrawCards(1) called");
    }
}
#endregion

#region 奥塔尔的荣耀 (ID: 9001003) Dice 1 & 2
public class DiceCardAbility_AnhierGainLight1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 恢复1点光芒";
    public override void OnSucceedAttack()
    {
        this.owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public class DiceCardAbility_AnhierClashWinDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[拼点胜利] 抽取1张书页";
    public override void OnWinParrying()
    {
        this.owner.allyCardDetail.DrawCards(1);
    }
}

public class DiceCardAbility_AnhierClashWinGainLight1 : DiceCardAbilityBase
{
    public static string Desc = "[拼点胜利] 恢复1点光芒";
    public override void OnWinParrying()
    {
        this.owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public class DiceCardAbility_AnhierGainLight2 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 恢复2点光芒";
    public override void OnSucceedAttack()
    {
        this.owner.cardSlotDetail.RecoverPlayPoint(2);
    }
}
#endregion

#region 攫取回忆 (ID: 9001005) Dice 1 & 2
public class DiceCardAbility_AnhierGrabDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 抽取1张书页";
    public override void OnSucceedAttack()
    {
        this.owner.allyCardDetail.DrawCards(1);
    }
}
#endregion

#region 以执为攻 (ID: 9001009) Dice 2
public class DiceCardAbility_AnhierBindingNextTurn : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 下回合使其获得1层[束缚]";
    public override void OnSucceedAttack()
    {
        BattleUnitModel target = this.card?.target;
        if (target != null && !target.IsDead())
        {
            // AddKeywordBufByEtc 默认下一幕生效，直接赋予即可
            target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Binding, 1, this.owner);
            SteriaLogger.Log($"以执为攻: Applied 1 Binding to {target.UnitData?.unitData?.name}");
        }
    }
}
#endregion


// --- Self Card Abilities (Effects On Use) ---

#region 拙劣控流 (ID: 9001001) On Use
public class DiceCardSelfAbility_AnhierRecoverLight1 : DiceCardSelfAbilityBase
{
    public static string Desc = "[流转] [使用时] 恢复1点光芒";

    public override string[] Keywords => new string[] { "SteriaFlowTransfer" };

    public override void OnUseCard()
    {
        this.owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}
#endregion

#region 回忆侧斩 (ID: 9001002) On Use
public class DiceCardSelfAbility_AnhierDiscardRandom1 : DiceCardSelfAbilityBase
{
    public static string Desc = "[使用时] 从手中随机丢弃1张书页";
    public override void OnUseCard()
    {
        this.owner.allyCardDetail.DisCardACardRandom();
    }
}
#endregion

#region 奥塔尔的荣耀 (ID: 9001003) On Use
public class DiceCardSelfAbility_AnhierDiscardAllDraw : DiceCardSelfAbilityBase
{
    public static string Desc = "[使用时] 丢弃2张手牌来使本书页骰子威力+1";

    public override void OnUseCard()
    {
        List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
        int discardedCount = 0;
        int maxDiscard = Math.Min(2, hand.Count);

        // 丢弃最多2张手牌
        for (int i = 0; i < maxDiscard; i++)
        {
            if (hand.Count > 0)
            {
                this.owner.allyCardDetail.DisCardACardRandom();
                discardedCount++;
            }
        }

        // 每丢弃2张牌，骰子威力+1
        int powerBonus = discardedCount / 2;
        if (powerBonus > 0)
        {
            foreach (BattleDiceBehavior behavior in this.card.GetDiceBehaviorList())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = powerBonus });
            }
            SteriaLogger.Log($"奥塔尔的荣耀: Discarded {discardedCount} cards, power bonus +{powerBonus}");
        }
    }

    // 被丢弃时：抽1张书页
    public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
    {
        unit?.allyCardDetail?.DrawCards(1);
        SteriaLogger.Log("奥塔尔的荣耀: OnDiscard - Drew 1 card");
    }
}
#endregion

#region 回忆协奏之刃 (ID: 9001004) On Use
public class DiceCardSelfAbility_AnhierDiscardAllPowerUp : DiceCardSelfAbilityBase
{
    public static string Desc = "[使用时] 丢弃手中所有书页，并使本书页所有骰子威力+{0}";

    public override void OnUseCard()
    {
        List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
        int discardedCount = 0;
        List<BattleDiceCardModel> handClone = new List<BattleDiceCardModel>(hand);
        foreach (BattleDiceCardModel cardToDiscard in handClone)
        {
            if (cardToDiscard == this.card.card) continue;
            this.owner.allyCardDetail.DiscardACardByAbility(cardToDiscard);
            discardedCount++;
        }

        int powerBonus = discardedCount;
        if (powerBonus > 0)
        {
            foreach (BattleDiceBehavior behavior in this.card.GetDiceBehaviorList())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = powerBonus });
            }
        }
    }
}
#endregion

#region 内调之流 (ID: 9001007) On Use
public class DiceCardSelfAbility_AnhierGainFlow5Draw2NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[流转] [使用时] 抽取2张书页，下回合获得5层[流]";

    public override string[] Keywords => new string[] { "SteriaFlowTransfer", "SteriaFlow" };

    public override void OnUseCard()
    {
        SteriaLogger.Log("内调之流: OnUseCard triggered - Drawing 2 cards and adding flow buff");
        this.owner.allyCardDetail.DrawCards(2);
        this.owner.bufListDetail.AddBuf(new BattleUnitBuf_AddFlowNextRound { amount = 5 });
    }
}

public class BattleUnitBuf_AddFlowNextRound : BattleUnitBuf
{
    public int amount = 0;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_AddFlowNextRound: Init called, amount={amount}");
    }

    public override void OnRoundStart()
    {
        SteriaLogger.Log($"BattleUnitBuf_AddFlowNextRound: OnRoundStart called, amount={amount}");
        if (this._owner != null && amount > 0)
        {
            CardAbilityHelper.AddFlowStacks(this._owner, amount);
            SteriaLogger.Log($"BattleUnitBuf_AddFlowNextRound: Added {amount} flow stacks");
        }
        this.Destroy();
    }
}
#endregion

#region 清司风流 (ID: 9001008) On Use
// 特殊效果：本书页不受流影响（通过Harmony补丁实现）
public class DiceCardSelfAbility_AnhierQingSiFengLiu : DiceCardSelfAbilityBase
{
    public static string Desc = "[流转]";

    public override string[] Keywords => new string[] { "SteriaFlowTransfer" };

    // 无使用时效果，仅有流转被动
}
#endregion

#region 清司风流 (ID: 9001008) Dice 1
// 命中时：若自身流层数>=10则重新投掷本骰子并使流层数-10（可重复触发）
public class DiceCardAbility_AnhierQingSiFengLiuDice1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 若自身[流]层数>=10，则重新投掷本骰子并使[流]层数-10（可重复触发）";
    public override string[] Keywords => new string[] { "SteriaFlow" };

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        SteriaLogger.Log("清司风流 Dice1: OnSucceedAttack triggered");

        // 直接创建特效和播放音效
        try
        {
            // 播放音效
            Sound.SoundEffectPlayer.PlaySound("Battle/Kali_Atk");

            // 创建特效
            if (this.owner?.view != null && target?.view != null)
            {
                SingletonBehavior<DiceEffectManager>.Instance?.CreateBehaviourEffect(
                    "Kali_Z", 1f, this.owner.view, target.view, 1f);
            }

            // 屏幕震动
            SteriaEffectHelper.AddScreenShake(0.02f, 0.01f, 70f, 0.3f);
        }
        catch (System.Exception ex)
        {
            SteriaLogger.Log($"清司风流 effect error: {ex.Message}");
        }

        // 获取当前流的层数
        BattleUnitBuf_Flow flowBuf = this.owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;

        int flowStacks = flowBuf?.stack ?? 0;
        SteriaLogger.Log($"清司风流 Dice1: Current flow stacks = {flowStacks}");

        if (flowStacks >= 10)
        {
            // 消耗10层流
            flowBuf.stack -= 10;
            SteriaLogger.Log($"清司风流 Dice1: Consumed 10 flow, remaining = {flowBuf.stack}");

            if (flowBuf.stack <= 0)
            {
                flowBuf.Destroy();
            }

            // 使用游戏内置方法触发骰子重复攻击
            SteriaLogger.Log("清司风流 Dice1: Activating bonus attack dice (re-roll)");
            ActivateBonusAttackDice();
        }
    }
}
#endregion

#region 以执为攻 (ID: 9001009) On Use
// 本舞台中，每弃置5张书页便使本书页骰子威力+1（按角色独立计数）
public class DiceCardSelfAbility_AnhierDiscardPowerUp : DiceCardSelfAbilityBase
{
    public static string Desc = "本舞台中，每弃置5张书页便使本书页骰子威力+1";

    // 按角色存储弃牌计数
    private static Dictionary<BattleUnitModel, int> _discardCountPerUnit = new Dictionary<BattleUnitModel, int>();

    public static void IncrementDiscardCount(BattleUnitModel unit)
    {
        if (unit == null) return;
        if (!_discardCountPerUnit.ContainsKey(unit))
            _discardCountPerUnit[unit] = 0;
        _discardCountPerUnit[unit]++;
        SteriaLogger.Log($"以执为攻: {unit.UnitData?.unitData?.name} discard count incremented to {_discardCountPerUnit[unit]}");
    }

    public static void ResetAllDiscardCounts()
    {
        _discardCountPerUnit.Clear();
        SteriaLogger.Log("以执为攻: All discard counts reset");
    }

    public static int GetDiscardCount(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        _discardCountPerUnit.TryGetValue(unit, out int count);
        return count;
    }

    public override void OnUseCard()
    {
        int discardCount = GetDiscardCount(this.owner);
        int powerBonus = discardCount / 5;
        SteriaLogger.Log($"以执为攻: OnUseCard - {this.owner?.UnitData?.unitData?.name} discards: {discardCount}, Power bonus: +{powerBonus}");

        if (powerBonus > 0)
        {
            foreach (BattleDiceBehavior behavior in this.card.GetDiceBehaviorList())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = powerBonus });
            }
        }
    }
}
#endregion

#region 寒地后勤 (ID: 9001010) On Use & On Discard
public class DiceCardSelfAbility_AnhierColdLogistics : DiceCardSelfAbilityBase
{
    public static string Desc = "[使用时] 恢复2点光芒\n[被弃置时] 恢复2点光芒";

    // 使用时：恢复2点光芒
    public override void OnUseCard()
    {
        this.owner.cardSlotDetail.RecoverPlayPoint(2);
        SteriaLogger.Log($"寒地后勤: OnUseCard - Recovered 2 light for {this.owner?.UnitData?.unitData?.name}");
    }

    // 被丢弃时：恢复2点光芒
    public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
    {
        if (unit != null)
        {
            unit.cardSlotDetail.RecoverPlayPoint(2);
            SteriaLogger.Log($"寒地后勤: OnDiscard - Recovered 2 light for {unit.UnitData?.unitData?.name}");
        }
    }
}
#endregion

#region 自我之流 (ID: 9001011) EGO装备
// EGO装备（Instance类型）：装备时扣除25点生命值，获得10层流
public class DiceCardSelfAbility_AnhierSelfFlow : DiceCardSelfAbilityBase
{
    public static string Desc = "[装备时] 扣除25点生命值，获得10层[流]";
    public override string[] Keywords => new string[] { "SteriaFlow" };

    // Instance类型卡牌使用OnUseInstance而不是OnUseCard
    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        SteriaLogger.Log($"自我之流: OnUseInstance triggered for {unit?.UnitData?.unitData?.name}");

        // 扣除25点生命值
        unit.LoseHp(25);
        SteriaLogger.Log($"自我之流: Lost 25 HP, current HP: {unit.hp}");

        // 获得10层流
        CardAbilityHelper.AddFlowStacks(unit, 10);
        SteriaLogger.Log($"自我之流: Added 10 Flow stacks");
    }
}
#endregion
