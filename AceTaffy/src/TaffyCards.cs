using LOR_DiceSystem;

// The XML loader registers public classes by the exact DiceCard(Self)Ability_ prefix.
// All smoke and pressure changes go through the shared mechanics contract.
public sealed class DiceCardSelfAbility_AceTaffy_Ignition : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        AceTaffy.TaffyMechanics.AddPressure(owner, 1);
        int smoke = AceTaffy.TaffyMechanics.EffectiveSmoke(owner);
        if (smoke > 0) owner.cardSlotDetail.RecoverPlayPoint(1);
        if (smoke >= 6) owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public sealed class DiceCardAbility_AceTaffy_IgnitionHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        AceTaffy.TaffyMechanics.AddSmoke(owner, 1, owner);
        if (target != null) AceTaffy.TaffyMechanics.AddSmoke(target, 1, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_RivetPressure : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null && AceTaffy.TaffyMechanics.EffectiveSmoke(target) > 0)
            AceTaffy.TaffyMechanics.AddPressure(owner, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_RivetSmoke : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        AceTaffy.TaffyMechanics.AddSmoke(owner, 2, owner);
        if (target != null) AceTaffy.TaffyMechanics.AddSmoke(target, 2, owner);
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_ExhaustRecycle : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (!AceTaffy.TaffyMechanics.SpendOrdinarySmoke(owner, 3)) return;
        owner.allyCardDetail.DrawCards(2);
        AceTaffy.TaffyMechanics.AddPressure(owner, 3);
    }
}

public sealed class DiceCardAbility_AceTaffy_RecycleGuard : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        AceTaffy.TaffyMechanics.AddPressure(owner, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_RecycleHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.allyCardDetail.DrawCards(1);
        AceTaffy.TaffyMechanics.AddSmoke(owner, 2, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_PistonPressure : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null && AceTaffy.TaffyMechanics.EffectiveSmoke(target) >= 3)
            AceTaffy.TaffyMechanics.AddPressure(owner, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_PistonFinal : DiceCardAbilityBase
{
    public override void BeforeRollDice()
    {
        // Native BeforeRollDice restores the pre-roll stat bonus before each roll.
        // Re-evaluate pressure for a repeated/copied die instead of latching it.
        if (AceTaffy.TaffyMechanics.Pressure(owner) >= 6)
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_FlueReversal : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        AceTaffy.TaffyMechanics.AddSmoke(owner, 2, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_FlueTransfer : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        BattleUnitModel target = behavior.TargetDice != null ? behavior.TargetDice.owner : null;
        if (target != null) AceTaffy.TaffyMechanics.TransferSmoke(owner, target, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_FlueHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null) AceTaffy.TaffyMechanics.AddSmoke(target, 2, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_FlueCounter : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null && AceTaffy.TaffyMechanics.EffectiveSmoke(target) >= 5)
            owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_SafetyValve : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        AceTaffy.TaffyMechanics.PayHp(owner, 4);
        AceTaffy.TaffyMechanics.AddPressure(owner, 2);
    }
}

public sealed class DiceCardAbility_AceTaffy_ValveGuard : DiceCardAbilityBase
{
    private bool _bonusApplied;

    public override void OnWinParrying()
    {
        if (!_bonusApplied && card.ApplyDiceStatBonus(DiceMatch.NextDefenseDice, new DiceStatBonus { power = 1 }) > 0)
            _bonusApplied = true;
    }
}

public sealed class DiceCardAbility_AceTaffy_ValveAttack : DiceCardAbilityBase
{
    private bool _bonusApplied;

    public override void OnWinParrying()
    {
        if (!_bonusApplied && card.ApplyDiceStatBonus(DiceMatch.NextAttackDice, new DiceStatBonus { power = 2 }) > 0)
            _bonusApplied = true;
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_BoilerArmor : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        int power = AceTaffy.TaffyMechanics.Pressure(owner) / 3;
        if (power > 3) power = 3;
        if (power > 0)
            card.ApplyDiceStatBonus(DiceMatch.NextGuardDice, new DiceStatBonus { power = power });
    }
}

public sealed class DiceCardAbility_AceTaffy_BoilerGuardSmoke : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        BattleUnitModel target = behavior.TargetDice != null ? behavior.TargetDice.owner : null;
        if (target != null) AceTaffy.TaffyMechanics.AddSmoke(target, 3, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_BoilerGuardPressure : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        AceTaffy.TaffyMechanics.AddPressure(owner, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_BoilerRecover : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (AceTaffy.TaffyMechanics.LostHpThisRound(owner))
            owner.breakDetail.RecoverBreakLife(3);
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_Supercritical : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        AceTaffy.TaffyMechanics.PayHp(owner, 5);
        AceTaffy.TaffyMechanics.AddSmoke(owner, 6, owner);
        AceTaffy.TaffyMechanics.AddPressure(owner, 3);
    }
}

public sealed class DiceCardAbility_AceTaffy_SupercriticalSmoke : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null) AceTaffy.TaffyMechanics.AddSmoke(target, 2, owner);
    }
}

public sealed class DiceCardAbility_AceTaffy_SupercriticalPower : DiceCardAbilityBase
{
    public override void BeforeRollDice()
    {
        if (AceTaffy.TaffyMechanics.Pressure(owner) >= 6)
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
    }
}

public sealed class DiceCardAbility_AceTaffy_SupercriticalPressure : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        AceTaffy.TaffyMechanics.AddPressure(owner, 1);
    }
}

public sealed class DiceCardAbility_AceTaffy_SupercriticalCounter : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        AceTaffy.TaffyMechanics.AddSmoke(owner, 2, owner);
    }
}

public sealed class DiceCardSelfAbility_AceTaffy_SteamDetonation : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        // Snapshot the combined resource now; the final hit reads the live amount instead.
        int power = AceTaffy.TaffyMechanics.EffectiveSmoke(owner) / 3;
        if (power > 0)
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = power });
    }
}

public sealed class DiceCardAbility_AceTaffy_DetonationDestroy : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        BattlePlayingCardDataInUnitModel opposingPage = behavior.TargetDice != null ? behavior.TargetDice.card : null;
        if (opposingPage != null)
            opposingPage.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
    }
}

public sealed class DiceCardAbility_AceTaffy_DetonationGuard : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        AceTaffy.TaffyMechanics.AddPressure(owner, 3);
    }
}

public sealed class DiceCardAbility_AceTaffy_DetonationBreak : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        int damage = AceTaffy.TaffyMechanics.EffectiveSmoke(owner) / 3;
        if (target != null && damage > 0)
            target.breakDetail.TakeBreakDamage(damage, DamageType.Card_Ability, owner);
    }
}
