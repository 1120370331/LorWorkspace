using LOR_DiceSystem;
using System;
using Steria;

public class DiceCardSelfAbility_AntieDreamMurmur : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] If Dream is exactly 4, gain 3 Dream. If Dream is above 4, consume 2 Dream, draw 2 pages, and recover 2 Light.";

    public override void OnUseCard()
    {
        if (owner == null)
        {
            return;
        }

        int dream = SivierCardHelper.GetDreamCount(owner);
        if (dream > 4)
        {
            SivierCardHelper.ConsumeDream(owner, 2);
            owner.allyCardDetail?.DrawCards(2);
            owner.cardSlotDetail?.RecoverPlayPoint(2);
            return;
        }

        if (dream >= 4)
        {
            SivierCardHelper.AddDreamToUnit(owner, 3);
        }
    }
}

public class DiceCardSelfAbility_AntieDeepDream : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Recover HP and Break equal to Dream stacks x 2. Next round, gain 5 Dream.";

    public override void OnUseCard()
    {
        if (owner == null)
        {
            return;
        }

        int dream = SivierCardHelper.GetDreamCount(owner);
        int recover = Math.Max(0, dream * 2);
        if (recover > 0)
        {
            owner.RecoverHP(recover);
            owner.breakDetail?.RecoverBreak(recover);
        }

        owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieDreamNextRound { stack = 5 });
    }
}

public class DiceCardSelfAbility_AntieDreamAwaken : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Consume up to 6 Dream. For each Dream consumed, this page's dice deal +25% damage and break damage. If 6 Dream was consumed, next round gain 1 All Dice Power Up.";

    public override void OnUseCard()
    {
        if (owner == null || card == null)
        {
            return;
        }

        int dream = SivierCardHelper.GetDreamCount(owner);
        int consumed = Math.Min(6, dream);
        if (consumed <= 0)
        {
            return;
        }

        SivierCardHelper.ConsumeDream(owner, consumed);
        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
        {
            dmgRate = consumed * 25,
            breakRate = consumed * 25
        });

        if (consumed >= 6)
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieAllDicePowerUpNextRound { stack = 1 });
        }
    }
}

public class BattleUnitBuf_AntieDreamNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner != null && !_owner.IsDead())
        {
            SivierCardHelper.AddDreamToUnit(_owner, Math.Max(0, stack));
        }
        Destroy();
    }
}

public class BattleUnitBuf_AntieAllDicePowerUpNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner != null && !_owner.IsDead())
        {
            _owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieAllDicePowerUp { stack = Math.Max(1, stack) });
        }
        Destroy();
    }
}

public class BattleUnitBuf_AntieAllDicePowerUp : BattleUnitBuf
{
    protected override string keywordId => "AntieAllDicePowerUp";
    protected override string keywordIconId => "SteriaDream";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior != null && stack > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = stack });
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        Destroy();
    }
}
