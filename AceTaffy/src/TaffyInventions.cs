using System;
using System.Collections.Generic;
using System.Linq;
using LOR_DiceSystem;

namespace AceTaffy
{
    public enum InventionQualifier { Odd, Weird, Fragile, Mysterious, Super }
    public enum InventionKind { Mechanism, Chain, Defense, Destruction, Energy }
    public sealed class TaffyInvention
    {
        public readonly InventionQualifier Qualifier;
        public readonly InventionKind Kind;
        public TaffyInvention(InventionQualifier qualifier, InventionKind kind) { Qualifier = qualifier; Kind = kind; }
        public int Scale { get { return Qualifier == InventionQualifier.Super ? 2 : 1; } }
        public int UnitId { get { return TaffyInventions.FirstUnitId + (int)Qualifier * 5 + (int)Kind; } }
        public string Key { get { return "AceTaffy_Invention_" + (int)Qualifier + "_" + (int)Kind; } }
        public static TaffyInvention FromBook(LorId id)
        {
            if (id == null || id.packageId != "AceTaffy" || id.id < TaffyInventions.FirstUnitId || id.id >= TaffyInventions.FirstUnitId + 25) return null;
            int value = id.id - TaffyInventions.FirstUnitId;
            return new TaffyInvention((InventionQualifier)(value / 5), (InventionKind)(value % 5));
        }
    }
    public static class TaffyInventions
    {
        public const int WorkshopEnemyId = 1884;
        public const int InventCardId = 1883010;
        public const int FirstUnitId = 1883400;
        public const int FirstCardId = 1883500;
        public const int MaxActiveUnits = 4;
        public const int EnemySlots = 6;
        public static readonly KeywordBuf[] RandomEffects = {
            KeywordBuf.Strength, KeywordBuf.Endurance, KeywordBuf.Quickness, KeywordBuf.Protection,
            KeywordBuf.Weak, KeywordBuf.Disarm, KeywordBuf.Binding, KeywordBuf.Vulnerable
        };
        public static PassiveAbility_1883202 Inventor(BattleUnitModel unit)
        { return unit == null || unit.passiveDetail == null ? null : unit.passiveDetail.PassiveList.OfType<PassiveAbility_1883202>().FirstOrDefault(); }
        public static PassiveAbility_1883205 UnitPassive(BattleUnitModel unit)
        { return unit == null || unit.passiveDetail == null ? null : unit.passiveDetail.PassiveList.OfType<PassiveAbility_1883205>().FirstOrDefault(); }
        public static bool IsWorkshop(BattleUnitModel unit)
        { return unit != null && unit.passiveDetail != null && unit.passiveDetail.HasPassive<PassiveAbility_1883204>(); }
        public static BattleUnitModel Workshop(BattleUnitModel owner)
        { return owner == null ? null : BattleObjectManager.instance.GetAliveList(owner.faction).Find(IsWorkshop); }
        public static BattleUnitBuf_AceTaffyInventionAura Aura(BattleUnitModel owner, BattleUnitModel source)
        {
            if (owner == null || source == null || owner.bufListDetail == null) return null;
            return owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>()
                .FirstOrDefault(x => !x.IsDestroyed() && x.Source == source);
        }
        public static bool IsInventCard(BattleDiceCardModel card)
        { return card != null && card.GetID() == new LorId("AceTaffy", InventCardId); }
        public static List<BattleUnitModel> AliveUnits(BattleUnitModel creator)
        {
            return BattleObjectManager.instance.GetAliveList(creator.faction).FindAll(unit => {
                var passive = UnitPassive(unit);
                return passive != null && passive.Creator == creator;
            });
        }
        public static int FreeSlot(Faction faction)
        {
            var alive = BattleObjectManager.instance.GetAliveList(faction);
            for (int i = 0; i < EnemySlots; i++) if (!alive.Any(u => u.index == i)) return i;
            return -1;
        }
        public static void RefreshPortraits()
        {
            int index = 0;
            foreach (var unit in BattleObjectManager.instance.GetList())
                SingletonBehavior<UI.UICharacterRenderer>.Instance.SetCharacter(unit.UnitData.unitData, index++, false, false);
            BattleObjectManager.instance.InitUI();
        }
        public static List<BattleUnitModel> AdjacentTargets(BattleUnitModel owner, BattleUnitModel main, int count)
        {
            if (main == null || count <= 0) return new List<BattleUnitModel>();
            return BattleObjectManager.instance.GetAliveList(main.faction)
                .Where(x => x != main && x != owner && x.IsTargetable(owner))
                .OrderBy(x => main.formation != null && x.formation != null ?
                    (main.formation.Pos - x.formation.Pos).sqrMagnitude : Math.Abs(main.index - x.index))
                .ThenBy(x => x.index).Take(count).ToList();
        }
        public static void EndImprovise(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.isDestroyed || card.owner.IsDead() || card.owner.IsBreakLifeZero()) return;
            var script = card.cardAbility as DiceCardSelfAbility_AceTaffy_Improvise;
            var target = script == null ? null : script.ResumeTarget();
            if (target != null) Singleton<StageController>.Instance.AddAllCardListInBattle(card, target, 0);
        }
    }
}
public class BattleUnitBuf_AceTaffyInventionProgress : BattleUnitBuf
{
    protected override string keywordId { get { return "AceTaffy_InventionProgress"; } }
    protected override string keywordIconId { get { return "AceTaffy_InventionProgress"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
}
public class BattleUnitBuf_AceTaffyScheduledInventions : BattleUnitBuf
{
    protected override string keywordId { get { return "AceTaffy_ScheduledInventions"; } }
    protected override string keywordIconId { get { return "AceTaffy_InventionProgress"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
}
public class BattleUnitBuf_AceTaffyInvention : BattleUnitBuf
{
    public AceTaffy.TaffyInvention Device;
    protected override string keywordId { get { return Device.Key; } }
    protected override string keywordIconId { get { return Device.Key; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
}
public class BattleUnitBuf_AceTaffyManufactureGuard : BattleUnitBuf
{
    protected override string keywordId { get { return "AceTaffy_ManufactureGuard"; } }
    protected override string keywordIconId { get { return "Protection"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
    public override StatBonus GetStatBonus() { return new StatBonus { dmgAdder = -stack }; }
}
public class BattleUnitBuf_AceTaffyManufactureVulnerable : BattleUnitBuf
{
    protected override string keywordId { get { return "AceTaffy_ManufactureVulnerable"; } }
    protected override string keywordIconId { get { return "Vulnerable"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Negative; } }
    public override StatBonus GetStatBonus() { return new StatBonus { dmgAdder = stack }; }
}
public class BattleUnitBuf_AceTaffyInventionAura : BattleUnitBuf
{
    public AceTaffy.InventionQualifier Qualifier;
    public BattleUnitModel Source;
    private readonly HashSet<BattlePlayingCardDataInUnitModel> _hitPages = new HashSet<BattlePlayingCardDataInUnitModel>();
    protected override string keywordId { get { return "AceTaffy_InventionAura_" + (int)Qualifier; } }
    protected override string keywordIconId { get { return keywordId; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
    public override int GetSpeedDiceAdder(int value) { return Qualifier == AceTaffy.InventionQualifier.Odd ? stack : 0; }
    public override void OnRoundStart() { _hitPages.Clear(); }
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        bool attack = behavior.Detail == BehaviourDetail.Slash || behavior.Detail == BehaviourDetail.Penetrate || behavior.Detail == BehaviourDetail.Hit;
        if ((Qualifier == AceTaffy.InventionQualifier.Super && attack) || (Qualifier == AceTaffy.InventionQualifier.Mysterious && !attack))
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = stack });
        if (Qualifier == AceTaffy.InventionQualifier.Fragile && attack)
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 15 * stack, breakRate = 15 * stack });
    }
    public override void OnSuccessAttack(BattleDiceBehavior behavior)
    {
        if (IsDestroyed() || stack <= 0 || Qualifier != AceTaffy.InventionQualifier.Weird || behavior.card == null || !_hitPages.Add(behavior.card)) return;
        var target = behavior.card.target;
        if (target != null && !target.IsDead()) target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Weak, stack, _owner);
    }
}
public class DiceCardSelfAbility_AceTaffy_Improvise : DiceCardSelfAbilityBase
{
    private readonly HashSet<int> _lost = new HashSet<int>();
    private List<BattleDiceBehavior> _original;
    private bool _resumed;
    public override bool IsTargetableAllUnit() { return true; }
    public override bool IsValidTarget(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    { return targetUnit != null && (targetUnit.faction != unit.faction || AceTaffy.TaffyInventions.IsWorkshop(targetUnit)); }
    public override void OnUseCard()
    { if (_original == null) _original = card.GetDiceBehaviorList().Where(x => x.Index < 3).ToList(); }
    public void Lose(int index) { _lost.Add(index); }
    public BattleUnitModel ResumeTarget()
    {
        var workshop = AceTaffy.TaffyInventions.Workshop(owner);
        if (_resumed || workshop == null || card.target == workshop || _original == null) return null;
        _resumed = true;
        foreach (var die in _original) if (die.DiceDestroyed) _lost.Add(die.Index);
        if (Enumerable.Range(0, 3).All(_lost.Contains)) return null;
        card.AddExcludedDices(_lost.ToList());
        return workshop;
    }
}
public class DiceCardAbility_AceTaffy_InventionStrike : DiceCardAbilityBase
{
    private void Lose()
    { var ability = card.cardAbility as DiceCardSelfAbility_AceTaffy_Improvise; if (ability != null) ability.Lose(behavior.Index); }
    public override void OnLoseParrying() { Lose(); }
    public override void OnDrawParrying() { Lose(); }
}
