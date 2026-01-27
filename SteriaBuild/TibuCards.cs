using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BaseMod;
using Steria;

// Tibu card abilities

public class DiceCardSelfAbility_TibuTideWhisper : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Recover 1 Light";

    public override void OnUseCard()
    {
        owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public class DiceCardAbility_TibuTideWhisperWeak : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 1 Weak";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Weak, 1, owner);
    }
}

public class DiceCardSelfAbility_TibuDreamOmen : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Draw 1 page";

    public override void OnUseCard()
    {
        owner.allyCardDetail.DrawCards(1);
    }
}

public class DiceCardAbility_TibuDreamOmenVulnerable : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 1 Vulnerable";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Vulnerable, 1, owner);
    }
}

public class DiceCardAbility_TibuDreamOmenBleed : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 1 Bleed";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
    }
}

public class DiceCardSelfAbility_TibuElysium : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next round gain 1 Quickness";

    public override void OnUseCard()
    {
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
    }
}

public class DiceCardAbility_TibuElysiumBinding : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 1 Binding";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 1, owner);
    }
}

public class DiceCardAbility_TibuElysiumParalysis : DiceCardAbilityBase
{
    public static string Desc = "[Clash Win] Apply 1 Paralysis";

    public override void OnWinParrying()
    {
        if (card?.target == null) return;
        card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Paralysis, 1, owner);
    }
}

public class DiceCardAbility_TibuFateArrivesBonusDamage : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Deal bonus damage equal to 2 x total negative stacks";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;

        int negativeStacks = Steria.TibuAbilityHelper.CountNegativeStacks(target);
        int bonusDamage = negativeStacks * 2;
        if (bonusDamage > 0)
        {
            target.TakeDamage(bonusDamage, DamageType.Card_Ability, owner);
        }
    }
}

public class DiceCardAbility_TibuFateArrivesParryPower : DiceCardAbilityBase
{
    public static string Desc = "[Clash Start] Consume 1 Tide to gain +2 power";
    private bool _applied;

    public override void BeforeRollDice()
    {
        if (_applied || behavior == null || owner == null)
        {
            return;
        }

        if (!behavior.IsParrying())
        {
            return;
        }

        int consumed = Steria.TibuAbilityHelper.ConsumeTideStacks(owner, 1, true);
        if (consumed > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
            _applied = true;
        }
    }
}

public class DiceCardSelfAbility_TibuTimeReturn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Draw 2 pages. Consume 2 Tide to upgrade all positive buffs by 1.";

    public override void OnUseCard()
    {
        owner.allyCardDetail.DrawCards(2);

        int consumed = Steria.TibuAbilityHelper.ConsumeTideStacks(owner, 2, true);
        if (consumed >= 2)
        {
            Steria.TibuAbilityHelper.UpgradeBuffStacks(owner, BufPositiveType.Positive, 1, false);
        }
    }
}

public class DiceCardAbility_TibuTimeReturnRecoverBreak : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Recover 5 Break";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.breakDetail.RecoverBreak(5);
    }
}

public class DiceCardAbility_TibuTimeReturnGainStrengthProtection : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Strength and Protection";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, owner);
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 1, owner);
    }
}

public class DiceCardSelfAbility_TibuTimeRuin : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Consume 2 Tide to upgrade all target negative buffs by 1.";

    public override void OnUseCard()
    {
        if (card?.target == null) return;

        int consumed = Steria.TibuAbilityHelper.ConsumeTideStacks(owner, 2, true);
        if (consumed >= 2)
        {
            Steria.TibuAbilityHelper.UpgradeBuffStacks(card.target, BufPositiveType.Negative, 1, true);
        }
    }
}

public class DiceCardAbility_TibuTimeRuinExtendDebuff : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Extend target's negative buffs by 1";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        Steria.TibuAbilityHelper.ExtendNegativeBuffsOneRound(target);
    }
}

public class DiceCardAbility_TibuTimeRuinGainQuickness : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Quickness";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
    }
}

public class DiceCardSelfAbility_TibuTideAwakening : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Requires Tide; consume 1 Tide. Take target's next deck card at cost 0. Return it at round end without shuffling.";
    public const int CARD_ID = 9006007;
    public const int COOLDOWN_ROUNDS = 1;
    private static readonly FieldInfo DeckField = typeof(BattleAllyCardDetail)
        .GetField("_cardInDeck", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo DiscardField = typeof(BattleAllyCardDetail)
        .GetField("_cardInDiscarded", BindingFlags.Instance | BindingFlags.NonPublic);

    public override bool OnChooseCard(BattleUnitModel owner)
    {
        if (owner == null)
        {
            return false;
        }

        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        return tideBuf != null && tideBuf.stack > 0;
    }

    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        if (unit == null || targetUnit == null)
        {
            return;
        }

        if (Steria.SteriaEgoCooldownManager.IsOnCooldown(unit, CARD_ID))
        {
            int remaining = Steria.SteriaEgoCooldownManager.GetRemainingCooldown(unit, CARD_ID);
            SteriaLogger.Log($"TideAwakening: On cooldown, {remaining} rounds remaining");
            return;
        }

        BattleAllyCardDetail targetDetail = targetUnit.allyCardDetail;
        if (targetDetail == null)
        {
            return;
        }

        List<BattleDiceCardModel> deck = targetDetail.GetDeck();
        if (deck == null)
        {
            return;
        }

        List<BattleDiceCardModel> deckList = null;
        List<BattleDiceCardModel> discardList = null;

        if (deck.Count == 0)
        {
            deckList = DeckField?.GetValue(targetDetail) as List<BattleDiceCardModel>;
            discardList = DiscardField?.GetValue(targetDetail) as List<BattleDiceCardModel>;
            if (deckList == null || discardList == null || discardList.Count == 0)
            {
                return;
            }
        }

        int consumed = Steria.TibuAbilityHelper.ConsumeTideStacks(unit, 1, true);
        if (consumed < 1)
        {
            SteriaLogger.Log("TideAwakening: No Tide to consume");
            return;
        }

        if (deck.Count == 0)
        {
            deckList.AddRange(discardList);
            discardList.Clear();
            deck = targetDetail.GetDeck();
            if (deck == null || deck.Count == 0)
            {
                return;
            }
        }

        BattleDiceCardModel stolen = deck[deck.Count - 1];
        if (stolen == null)
        {
            return;
        }

        targetDetail.ExhaustCardAnyWhere(stolen);

        int originalCost = stolen.GetCost();
        stolen.SetCurrentCost(0);
        stolen.owner = unit;
        unit.allyCardDetail.AddCardToHand(stolen, false);

        BattleUnitBuf_TideBorrowedCard borrowedBuf = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_TideBorrowedCard) as BattleUnitBuf_TideBorrowedCard;
        if (borrowedBuf == null)
        {
            borrowedBuf = new BattleUnitBuf_TideBorrowedCard();
            unit.bufListDetail.AddBuf(borrowedBuf);
        }

        borrowedBuf.AddBorrowedCard(stolen, targetUnit, originalCost);

        Steria.SteriaEgoCooldownManager.StartCooldown(unit, CARD_ID);
    }
}
