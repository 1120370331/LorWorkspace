using AceTaffy;
using LOR_DiceSystem;

public class PassiveAbility_1883101 : PassiveAbilityBase
{
    public override void OnWaveStart()
    {
        TaffyMechanics.AddPressure(owner, 3);
    }
}

public class PassiveAbility_1883102 : PassiveAbilityBase
{
    private bool _used;

    public override void OnRoundStart() { _used = false; }

    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        TryRepeat(behavior);
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        // SuccessAtk also fires after a winning clash, before OnWinParrying.
        // Only an attack rolled without an opposing die is a one-sided hit.
        if (behavior != null && behavior.TargetDice == null) TryRepeat(behavior);
    }

    private void TryRepeat(BattleDiceBehavior behavior)
    {
        if (_used || behavior == null || behavior.forbiddenBonusDice ||
            !TaffyMechanics.IsThirdAttack(behavior) || TaffyMechanics.Pressure(owner) < 6 ||
            !TaffyMechanics.SpendPressure(owner, 3)) return;
        _used = true;
        // The parrying and one-sided schedulers retain the current die and roll it
        // once more when this flag is set. A queued copy becomes the next page die.
        behavior.isBonusAttack = true;
    }
}

public class PassiveAbility_1883103 : PassiveAbilityBase
{
    private bool _active;
    private bool _granted;

    public override void OnRoundStart()
    {
        _active = false;
        _granted = false;
    }

    public void Activate()
    {
        if (_active) return;
        _active = true;
        owner.bufListDetail.AddBuf(new BattleUnitBuf_AceTaffyGuard { stack = 1 });
        if (_granted) return;
        _granted = true;
        var counter = new BattleDiceBehavior
        {
            behaviourInCard = new DiceBehaviour
            {
                Type = BehaviourType.Standby,
                Detail = BehaviourDetail.Guard,
                Min = 4,
                Dice = 8
            }
        };
        counter.SetIndex(0);
        // The keep-card is the engine's reserve counter-dice queue.
        var sourceCard = owner.cardSlotDetail.keepCard.card;
        if (sourceCard == null)
        {
            var slots = owner.cardSlotDetail.cardAry;
            foreach (var slot in slots)
            {
                if (slot != null && slot.card != null) { sourceCard = slot.card; break; }
            }
        }
        if (sourceCard == null)
        {
            var nativeCounterCard = ItemXmlDataList.instance.GetCardItem(705001, false);
            if (nativeCounterCard != null) sourceCard = BattleDiceCardModel.CreatePlayingCard(nativeCounterCard);
        }
        if (sourceCard != null) owner.cardSlotDetail.keepCard.AddBehaviour(sourceCard, counter);
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (_active && IsDefense(behavior))
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
    }

    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        if (!_active || !IsDefense(behavior)) return;
        TaffyMechanics.AddPressure(owner, 1);
        var target = behavior.TargetDice != null ? behavior.TargetDice.owner :
            behavior.card != null ? behavior.card.target : null;
        if (target != null) TaffyMechanics.AddSmoke(target, 1, owner);
    }

    private static bool IsDefense(BattleDiceBehavior behavior)
    {
        return (behavior.Type == BehaviourType.Def || behavior.Type == BehaviourType.Standby) &&
            (behavior.Detail == BehaviourDetail.Guard || behavior.Detail == BehaviourDetail.Evasion);
    }
}

public class BattleUnitBuf_AceTaffyGuard : BattleUnitBuf
{
    protected override string keywordId { get { return "AceTaffy_Guard"; } }
    protected override string keywordIconId { get { return "AceTaffy_Guard"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
    public override void OnRoundEnd() { Destroy(); }
}
