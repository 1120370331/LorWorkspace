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
        this.owner.allyCardDetail.DrawCards(1);
    }
}
#endregion

#region 奥塔尔的荣耀 (ID: 9001003) Dice 1 & 2
public class DiceCardAbility_AnhierClashWinDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[拼点胜利] 抽取1张书页";
    public override void OnWinParrying()
    {
        this.owner.allyCardDetail.DrawCards(1);
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


// --- Self Card Abilities (Effects On Use) ---

#region 拙劣控流 (ID: 9001001) On Use
public class DiceCardSelfAbility_AnhierRecoverLight1 : DiceCardSelfAbilityBase
{
    public static string Desc = "[使用时] 恢复1点光芒";
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
    public static string Desc = "[使用时] 丢弃手中所有书页，并抽取等量的书页";
    public override void OnUseCard()
    {
        List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
        int discardedCount = 0;
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            BattleDiceCardModel cardToDiscard = hand[i];
            this.owner.allyCardDetail.DiscardACardByAbility(cardToDiscard);
            discardedCount++;
        }

        if (discardedCount > 0)
        {
            this.owner.allyCardDetail.DrawCards(discardedCount);
        }
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
    public static string Desc = "[使用时] 抽取2张书页，下一幕开始时获得3层[流]";

    public override void OnUseCard()
    {
        SteriaLogger.Log("内调之流: OnUseCard triggered - Drawing 2 cards and adding flow buff");
        this.owner.allyCardDetail.DrawCards(2);
        this.owner.bufListDetail.AddBuf(new BattleUnitBuf_AddFlowNextRound { amount = 3 });
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
// 特殊效果：本书页受"流"强化时，只获得加成而不消耗其层数（通过Harmony补丁实现）
public class DiceCardSelfAbility_AnhierQingSiFengLiu : DiceCardSelfAbilityBase
{
    public static string Desc = "[流转]";
    // 无使用时效果，仅有流转被动
}
#endregion

#region 清司风流 (ID: 9001008) Dice 1
// 命中时：若自身流层数大于10则重新投掷本骰子并使流层数-10（可重复触发）
public class DiceCardAbility_AnhierQingSiFengLiuDice1 : DiceCardAbilityBase
{
    public static string Desc = "[命中时] 若自身[流]层数大于10，则重新投掷本骰子并使[流]层数-10（可重复触发）";

    public override void OnSucceedAttack()
    {
        SteriaLogger.Log("清司风流 Dice1: OnSucceedAttack triggered");

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

            // 重新投掷本骰子（创建新的BattleDiceBehavior并添加）
            SteriaLogger.Log("清司风流 Dice1: Adding bonus dice (re-roll)");
            BattleDiceBehavior newDice = new BattleDiceBehavior();
            newDice.behaviourInCard = this.behavior.behaviourInCard.Copy();
            newDice.SetIndex(this.card.GetDiceBehaviorList().Count);
            this.card.AddDice(newDice);
        }
    }
}
#endregion
