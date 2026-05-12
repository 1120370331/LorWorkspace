using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Steria;

public class DiceCardSelfAbility_AntieTideSpeech : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Draw 1 page and gain 1 Tide.";

    public override void OnUseCard()
    {
        if (owner == null)
        {
            return;
        }

        owner.allyCardDetail?.DrawCards(1);
        PassiveAbility_9004001.AddTideStacks(owner, 1);
    }
}

public class DiceCardSelfAbility_AntieTidePressure : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Consume up to 4 Tide. For each Tide consumed, apply Binding/Weak/Vulnerable/Paralysis to the target in order. If 4 Tide was consumed, extend target negative effects by 1 round.";

    private static readonly KeywordBuf[] Debuffs =
    {
        KeywordBuf.Binding,
        KeywordBuf.Weak,
        KeywordBuf.Vulnerable,
        KeywordBuf.Paralysis
    };

    public override void OnUseCard()
    {
        BattleUnitModel target = card?.target;
        if (owner == null || target == null || target == owner || target.IsDead())
        {
            return;
        }

        int tide = AntieTideCardHelper.GetTideStacks(owner);
        int toConsume = Math.Min(4, tide);
        int consumed = 0;
        for (int i = 0; i < toConsume; i++)
        {
            KeywordBuf debuff = Debuffs[i];
            if (!AntieTideCardHelper.TryConsumeTide(owner, target, debuff, true))
            {
                break;
            }

            AntieTideCardHelper.AddKeywordBufByCardWithoutTide(target, debuff, 1, owner);
            consumed++;
        }

        if (consumed >= 4)
        {
            TibuAbilityHelper.ExtendNegativeBuffsOneRound(target);
        }
    }
}

public class DiceCardSelfAbility_AntieTideAwaken : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Consume up to 3 Tide. Next round, gain Strength and Endurance equal to consumed Tide. If non-resource positive stacks are at least 6 when used, this page's dice gain +3 power.";
    private const int PositiveStackThreshold = 6;

    public override void OnUseCard()
    {
        if (owner == null)
        {
            return;
        }

        int positiveStacks = AntieTideCardHelper.CountNonResourcePositiveStacks(owner);
        if (positiveStacks >= PositiveStackThreshold)
        {
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 3 });
        }

        int tide = AntieTideCardHelper.GetTideStacks(owner);
        int toConsume = Math.Min(3, tide);
        int consumed = 0;
        for (int i = 0; i < toConsume; i++)
        {
            if (!AntieTideCardHelper.TryConsumeTide(owner, owner, KeywordBuf.Strength, true))
            {
                break;
            }

            consumed++;
        }

        if (consumed <= 0)
        {
            return;
        }

        owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieTideAwakenNextRound
        {
            stack = consumed
        });
    }
}

public class BattleUnitBuf_AntieTideAwakenNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (_owner != null && !_owner.IsDead())
        {
            int amount = Math.Max(1, stack);
            HarmonyHelpers.RunWithoutTideEnhancement(() =>
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, amount, _owner);
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, amount, _owner);
            });
        }

        Destroy();
    }
}

internal static class AntieTideCardHelper
{
    public static int GetTideStacks(BattleUnitModel unit)
    {
        BattleUnitBuf_Tide tide = unit?.bufListDetail?.GetActivatedBufList()
            ?.FirstOrDefault(buf => buf is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        return Math.Max(0, tide?.stack ?? 0);
    }

    public static bool TryConsumeTide(BattleUnitModel owner, BattleUnitModel enhancedTarget, KeywordBuf enhancedBufType, bool enhancedByTide)
    {
        if (owner == null)
        {
            return false;
        }

        BattleUnitBuf_Tide tide = owner.bufListDetail?.GetActivatedBufList()
            ?.FirstOrDefault(buf => buf is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tide == null || tide.stack <= 0)
        {
            return false;
        }

        bool hasProxy = DirectiveDreamHelper.HasStephanieProxy(owner);
        if (!hasProxy)
        {
            tide.stack--;
        }

        HarmonyHelpers.NotifyPassivesOnTideConsumed(
            owner,
            1,
            enhancedTarget: enhancedTarget,
            enhancedBufType: enhancedBufType,
            enhancedByTide: enhancedByTide);

        if (hasProxy)
        {
            owner.currentDiceAction?.currentBehavior?.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }

        if (!hasProxy && tide.stack <= 0)
        {
            tide.Destroy();
        }

        return true;
    }

    public static void AddKeywordBufByCardWithoutTide(BattleUnitModel target, KeywordBuf keyword, int stack, BattleUnitModel actor)
    {
        if (target == null || stack <= 0)
        {
            return;
        }

        HarmonyHelpers.RunWithoutTideEnhancement(() =>
        {
            target.bufListDetail.AddKeywordBufByCard(keyword, stack, actor);
        });
    }

    public static int CountNonResourcePositiveStacks(BattleUnitModel unit)
    {
        if (unit?.bufListDetail == null)
        {
            return 0;
        }

        int total = 0;
        CountPositiveStacks(unit.bufListDetail.GetActivatedBufList(), ref total);
        CountPositiveStacks(unit.bufListDetail.GetReadyBufList(), ref total);
        CountPositiveStacks(unit.bufListDetail.GetReadyReadyBufList(), ref total);
        return total;
    }

    private static void CountPositiveStacks(IEnumerable<BattleUnitBuf> bufs, ref int total)
    {
        if (bufs == null)
        {
            return;
        }

        foreach (BattleUnitBuf buf in bufs)
        {
            if (buf == null || buf.IsDestroyed() || buf.stack <= 0 || buf.positiveType != BufPositiveType.Positive || IsResourceOrMarker(buf))
            {
                continue;
            }

            total += buf.stack;
        }
    }

    private static bool IsResourceOrMarker(BattleUnitBuf buf)
    {
        return buf is BattleUnitBuf_Tide ||
               buf is BattleUnitBuf_Dream ||
               buf is BattleUnitBuf_GoldenTide ||
               buf is BattleUnitBuf_AntieFormBase ||
               buf is BattleUnitBuf_AntieFormCooldown ||
               buf is BattleUnitBuf_AntieTideAwakenNextRound ||
               buf.GetType().FullName == "Steria.BattleUnitBuf_Flow";
    }
}
