using System;
using System.Collections.Generic;
using System.Linq;
using AceTaffy;
using LOR_DiceSystem;

public class PassiveAbility_1883205 : PassiveAbilityBase
{
    public BattleUnitModel Creator;
    public TaffyInvention Device { get; private set; }
    private int _winding;
    private readonly HashSet<BattlePlayingCardDataInUnitModel> _qualifierPages = new HashSet<BattlePlayingCardDataInUnitModel>();
    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        Device = TaffyInvention.FromBook(self.Book.ClassInfo.id);
        if (Device == null) throw new InvalidOperationException("Invalid invention book identity");
    }
    public override void OnUnitCreated()
    { owner.bufListDetail.AddBuf(new BattleUnitBuf_AceTaffyInvention { Device = Device, stack = 1 }); }
    public override int SpeedDiceNumAdder()
    { return owner.emotionDetail == null ? 0 : -owner.emotionDetail.SpeedDiceNumAdder(); }
    public override void OnRoundStart()
    {
        _qualifierPages.Clear();

    }
    public override void OnAfterRollSpeedDice()
    {
        owner.allyCardDetail.ExhaustAllCards();
        int first = TaffyInventions.FirstCardId + 2 * (int)Device.Kind;
        for (int i = 0; i < Math.Min(2, owner.speedDiceResult.Count); i++)
        {
            var card = owner.allyCardDetail.AddTempCard(new LorId("AceTaffy", first + i));
            if (card == null) throw new InvalidOperationException("Missing invention page " + (first + i));
            card.SetPriorityAdder(1000 - i * 100 - card.XmlData.Priority);
        }
    }
    public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
    {
        if (Device.Kind == InventionKind.Energy)
            TaffyMechanics.AddSmoke(owner, 2 * Device.Scale, owner);
    }
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (Device.Kind == InventionKind.Destruction)
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 20 * Device.Scale, breakRate = 20 * Device.Scale });
    }
    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        if (Device.Kind == InventionKind.Mechanism && ++_winding >= 3)
        {
            _winding -= 3;
            owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, Device.Scale, owner);
        }
        TriggerQualifier(behavior);
    }
    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        if (Device.Qualifier == InventionQualifier.Odd) TriggerQualifier(behavior);
    }
    private void TriggerQualifier(BattleDiceBehavior behavior)
    {
        if (behavior == null || behavior.card == null || behavior.card.isKeepedCard) return;
        var page = behavior.card;
        if (Device.Qualifier == InventionQualifier.Odd)
        {
            var dice = page.card.XmlData.DiceBehaviourList;
            if (behavior.Type != BehaviourType.Atk || behavior.Index < 0 || behavior.Index >= dice.Count ||
                dice.Take(behavior.Index).Any(d => d.Type == BehaviourType.Atk) || !_qualifierPages.Add(page)) return;
            if (!behavior.forbiddenBonusDice && RandomUtil.valueForProb < .5f) behavior.isBonusAttack = true;
            return;
        }
        var target = page.target;
        if (target == null || target.IsDead() || !_qualifierPages.Add(page)) return;
        switch (Device.Qualifier)
        {
            case InventionQualifier.Weird:
                target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Weak, 1, owner); break;
            case InventionQualifier.Fragile:
                owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Vulnerable, 1, owner);
                target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Vulnerable, 1, owner); break;
            case InventionQualifier.Mysterious:
                var effects = TaffyInventions.RandomEffects;
                owner.bufListDetail.AddKeywordBufByEtc(effects[RandomUtil.Range(0, effects.Length - 1)], 1, owner);
                target.bufListDetail.AddKeywordBufByEtc(effects[RandomUtil.Range(0, effects.Length - 1)], 1, owner); break;
        }
    }
    public override void OnRoundEnd()
    { if (Device.Kind == InventionKind.Energy && !owner.IsDead()) owner.RecoverHP(5 * Device.Scale); }
}

public abstract class TaffyDeviceDebuffDice : DiceCardAbilityBase
{
    protected abstract KeywordBuf Debuff { get; }
    protected virtual int Amount { get { return 1; } }
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null || target.IsDead()) return;
        var unit = TaffyInventions.UnitPassive(owner);
        int amount = Amount * (unit == null ? 1 : unit.Device.Scale);
        if (Debuff == KeywordBuf.Smoke) TaffyMechanics.AddSmoke(target, amount, owner);
        else target.bufListDetail.AddKeywordBufByCard(Debuff, amount, owner);
    }
}
public class DiceCardAbility_AceTaffy_DeviceBleed : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Bleeding; } } }
public class DiceCardAbility_AceTaffy_DeviceWeak : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Weak; } } }
public class DiceCardAbility_AceTaffy_DeviceDisarm : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Disarm; } } }
public class DiceCardAbility_AceTaffy_DeviceBind : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Binding; } } }
public class DiceCardAbility_AceTaffy_DeviceParalyze : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Paralysis; } } }
public class DiceCardAbility_AceTaffy_DeviceVulnerable : TaffyDeviceDebuffDice
{ protected override KeywordBuf Debuff { get { return KeywordBuf.Vulnerable; } } }
public class DiceCardAbility_AceTaffy_DeviceSmoke : TaffyDeviceDebuffDice
{
    protected override KeywordBuf Debuff { get { return KeywordBuf.Smoke; } }
    protected override int Amount { get { return 2; } }
}
public class DiceCardSelfAbility_AceTaffy_DeviceOverload : DiceCardSelfAbilityBase
{
    private bool _paid;
    public override void OnUseCard() { if (!_paid) { _paid = true; owner.LoseHp(5); } }
}
public class DiceCardSelfAbility_AceTaffy_DeviceChain : DiceCardSelfAbilityBase
{
    private Queue<BattleUnitModel> _targets;
    public override void OnUseCard()
    {
        if (_targets != null) return;
        var unit = TaffyInventions.UnitPassive(owner);
        _targets = new Queue<BattleUnitModel>(TaffyInventions.AdjacentTargets(owner, card.target, unit == null ? 1 : unit.Device.Scale));
    }
    public override void OnEndBattle()
    {
        if (_targets == null || owner.IsDead() || owner.IsBreakLifeZero() || card.isDestroyed) return;
        while (_targets.Count > 0)
        {
            var target = _targets.Dequeue();
            if (target.IsDead() || !target.IsTargetable(owner)) continue;
            Singleton<StageController>.Instance.AddAllCardListInBattle(card, target, 0);
            return;
        }
    }
}
