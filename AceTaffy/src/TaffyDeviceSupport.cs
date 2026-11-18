using System.Linq;
using AceTaffy;
using LOR_DiceSystem;

// Queue at scene end so production during OnRoundStart cannot promote a reward
// early on units whose native buff-start callback has not run yet.
public class BattleUnitBuf_AceTaffyNextSceneSupport : BattleUnitBuf
{
    public KeywordBuf Reward;
    public BattleUnitModel Source;
    protected override string keywordId { get { return "AceTaffy_Overdrive_" + Reward; } }
    protected override string keywordIconId { get { return Reward.ToString(); } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
    public override void OnRoundEnd()
    {
        if (IsDestroyed()) return;
        if (!_owner.IsDead()) _owner.bufListDetail.AddKeywordBufByEtc(Reward, stack, Source);
        Destroy();
    }
}

// Read living providers at the point of use: deaths remove support immediately,
// and mid-scene summons receive support without depending on OnRoundStart order.
public class BattleUnitBuf_AceTaffyDeviceSupport : BattleUnitBuf
{
    public BattleUnitModel Creator;
    protected override string keywordId { get { return "AceTaffy_DeviceSupport"; } }
    protected override string keywordIconId { get { return "Protection"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
    public int Support(InventionKind kind)
    {
        if (Creator == null || Creator.IsDead() || _owner.IsDead()) return 0;
        if (kind != InventionKind.Defense && TaffyInventions.UnitPassive(_owner) == null) return 0;
        return TaffyInventions.AliveUnits(Creator).Where(x => x != _owner)
            .Select(TaffyInventions.UnitPassive).Where(x => x.Device.Kind == kind).Sum(x => x.Device.Scale);
    }
    public override StatBonus GetStatBonus()
    { return new StatBonus { dmgAdder = -2 * Support(InventionKind.Defense) }; }
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        bool attack = behavior.Detail == BehaviourDetail.Slash || behavior.Detail == BehaviourDetail.Penetrate || behavior.Detail == BehaviourDetail.Hit;
        behavior.ApplyDiceStatBonus(new DiceStatBonus {
            power = Support(InventionKind.Energy),
            dmgRate = attack ? 15 * Support(InventionKind.Mechanism) : 0,
            breakRate = attack ? 15 * Support(InventionKind.Chain) : 0,
            max = attack ? Support(InventionKind.Destruction) : 0
        });
    }
}
