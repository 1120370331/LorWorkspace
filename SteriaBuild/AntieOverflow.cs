using System;
using System.Collections.Generic;
using System.Linq;
using LOR_DiceSystem;

namespace Steria
{
    internal enum AntieResource
    {
        Flow,
        Dream,
        Tide
    }

    internal static class AntieOverflow
    {
        internal const string PackageId = "SteriaBuilding";
        internal const int FlowCup = 9011601;
        internal const int DreamCup = 9011602;
        internal const int TideCup = 9011603;
        internal static readonly int[] CupIds = { FlowCup, DreamCup, TideCup };

        internal static bool HasPassive(BattleUnitModel unit) => unit?.passiveDetail?.PassiveList
            .Any(p => p is PassiveAbility_9011003 && p.isActiavted) == true;

        internal static T Find<T>(BattleUnitModel unit) where T : BattleUnitBuf => unit?.bufListDetail?
            .GetActivatedBufList().OfType<T>().FirstOrDefault(buf => !buf.IsDestroyed() && buf.stack > 0);

        internal static void Record(BattleUnitModel spender, AntieResource resource, int amount)
        {
            if (spender == null || amount <= 0 || BattleObjectManager.instance == null) return;

            foreach (var ally in BattleObjectManager.instance.GetAliveList(spender.faction))
            {
                if (!HasPassive(ally)) continue;
                switch (resource)
                {
                    case AntieResource.Flow: Add<BattleUnitBuf_OverflowFlow>(ally, amount); break;
                    case AntieResource.Dream: Add<BattleUnitBuf_OverflowDream>(ally, amount); break;
                    case AntieResource.Tide: Add<BattleUnitBuf_OverflowTide>(ally, amount); break;
                }
            }
        }

        internal static bool SpendSpecial<T>(BattleUnitModel unit, AntieResource resource) where T : BattleUnitBuf
        {
            var buf = Find<T>(unit);
            if (buf == null) return false;

            if (--buf.stack == 0) buf.Destroy();
            Record(unit, resource, 1);
            return true;
        }

        private static void Add<T>(BattleUnitModel unit, int amount) where T : BattleUnitBuf, new()
        {
            var buf = Find<T>(unit);
            if (buf == null) unit.bufListDetail.AddBuf(new T { stack = amount });
            else buf.stack = (int)Math.Min(int.MaxValue, (long)buf.stack + amount);
        }

        internal static bool TryGetResource(BattleDiceCardModel card, out AntieResource resource)
        {
            resource = default;
            var id = card?.GetID();
            if (id?.packageId != PackageId) return false;
            switch (id.id)
            {
                case FlowCup: resource = AntieResource.Flow; return true;
                case DreamCup: resource = AntieResource.Dream; return true;
                case TideCup: resource = AntieResource.Tide; return true;
                default: return false;
            }
        }

        private static BattleUnitBuf OverflowBuff(BattleUnitModel owner, AntieResource resource)
        {
            switch (resource)
            {
                case AntieResource.Flow: return Find<BattleUnitBuf_OverflowFlow>(owner);
                case AntieResource.Dream: return Find<BattleUnitBuf_OverflowDream>(owner);
                default: return Find<BattleUnitBuf_OverflowTide>(owner);
            }
        }

        private static int Threshold(AntieResource resource)
        {
            switch (resource)
            {
                case AntieResource.Flow: return 30;
                case AntieResource.Dream: return 20;
                default: return 10;
            }
        }

        internal static bool CanUse(BattleUnitModel owner, AntieResource resource) => HasPassive(owner) &&
            !owner.IsDead() && (OverflowBuff(owner, resource)?.stack ?? 0) >= Threshold(resource);

        internal static void Use(BattleUnitModel owner, AntieResource resource)
        {
            if (!CanUse(owner, resource)) return;
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction).OrderBy(unit => unit.index).ToArray();
            var overflow = OverflowBuff(owner, resource);
            int amount = overflow.stack;
            overflow.stack = 0;
            overflow.Destroy();

            var weights = new double[allies.Length];
            var capacities = new int[allies.Length];
            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                switch (resource)
                {
                    case AntieResource.Flow:
                        int maxBreak = ally.breakDetail.GetDefaultBreakGauge();
                        int missingBreak = Math.Max(0, maxBreak - ally.breakDetail.breakGauge);
                        capacities[i] = missingBreak > 0 && ally.CanRecoverBreak(1) ? missingBreak : 0;
                        weights[i] = missingBreak / (double)Math.Max(1, maxBreak);
                        break;
                    case AntieResource.Dream:
                        weights[i] = 1.0 / Math.Max(1, ally.MaxHp);
                        capacities[i] = amount;
                        break;
                    case AntieResource.Tide:
                        int maxHp = Math.Max(1, ally.MaxHp);
                        double missingHp = Math.Max(0, maxHp - ally.hp);
                        capacities[i] = missingHp > 0 && ally.CanRecoverHp(1) ? (int)Math.Ceiling(missingHp) : 0;
                        weights[i] = missingHp / maxHp;
                        break;
                }
            }

            int[] shares = Allocate(amount, weights, capacities);
            int specials = amount / 10;
            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                int share = shares[i];
                switch (resource)
                {
                    case AntieResource.Flow:
                        if (share > 0) ally.breakDetail.RecoverBreak(share);
                        Add<BattleUnitBuf_StrongFlow>(ally, specials);
                        break;
                    case AntieResource.Dream:
                        if (share > 0) SivierCardHelper.AddWishShieldToUnit(ally, share);
                        Add<BattleUnitBuf_DeepDream>(ally, specials);
                        break;
                    case AntieResource.Tide:
                        if (share > 0) ally.RecoverHP(share);
                        Add<BattleUnitBuf_HolyTide>(ally, specials);
                        break;
                }
            }
        }

        internal static int[] Allocate(int amount, double[] weights, int[] capacities)
        {
            var shares = new int[weights.Length];
            var eligible = Enumerable.Range(0, weights.Length)
                .Where(i => weights[i] > 0 && capacities[i] > 0).ToList();

            // Resolve capped recipients first, then divide the remaining pool at the same weights.
            while (amount > 0 && eligible.Count > 0)
            {
                double scale = amount / eligible.Sum(i => weights[i]);
                var capped = eligible.Where(i => scale * weights[i] >= capacities[i]).ToArray();
                if (capped.Length > 0)
                {
                    foreach (int i in capped)
                    {
                        shares[i] = Math.Min(amount, capacities[i]);
                        amount -= shares[i];
                        eligible.Remove(i);
                    }
                    continue;
                }

                int remainder = amount;
                foreach (int i in eligible)
                {
                    shares[i] = (int)(scale * weights[i]);
                    remainder -= shares[i];
                }
                foreach (int i in eligible.OrderByDescending(i => scale * weights[i] - shares[i]).ThenBy(i => i).Take(remainder))
                    shares[i]++;
                break;
            }
            return shares;
        }
    }

    public abstract class BattleUnitBuf_AntieOverflowResource : BattleUnitBuf
    {
        public override BufPositiveType positiveType => BufPositiveType.Positive;
    }

    public sealed class BattleUnitBuf_OverflowFlow : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaOverflowFlow";
        protected override string keywordIconId => "SteriaFlow";
    }

    public sealed class BattleUnitBuf_OverflowDream : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaOverflowDream";
        protected override string keywordIconId => "SteriaDream";
    }

    public sealed class BattleUnitBuf_OverflowTide : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaOverflowTide";
        protected override string keywordIconId => "SeaTides";
    }

    public sealed class BattleUnitBuf_StrongFlow : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaStrongFlow";
        protected override string keywordIconId => "SteriaFlow";
    }

    public sealed class BattleUnitBuf_DeepDream : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaDeepDream";
        protected override string keywordIconId => "SteriaDream";
    }

    public sealed class BattleUnitBuf_HolyTide : BattleUnitBuf_AntieOverflowResource
    {
        protected override string keywordId => "SteriaHolyTide";
        protected override string keywordIconId => "SeaTides";
    }
}

public sealed class PassiveAbility_9011003 : PassiveAbilityBase
{
    public override void OnUnitCreated() => AddCups();

    public override void OnWaveStart() => AddCups();

    public override void OnRoundStartAfter()
    {
        AddCups();
        if (owner.faction != Faction.Enemy || owner.IsDead() || owner.IsBreakLifeZero() || !owner.IsActionable()) return;

        // Vanilla enemy planning only looks in the ordinary hand.
        foreach (var card in owner.personalEgoDetail.GetHand())
        {
            if (Steria.AntieOverflow.TryGetResource(card, out var resource) && Steria.AntieOverflow.CanUse(owner, resource))
                owner.cardSlotDetail.AddCard(card, owner, -1, true);
        }
    }

    private void AddCups()
    {
        foreach (int id in Steria.AntieOverflow.CupIds)
            owner.personalEgoDetail.AddCard(new LorId(Steria.AntieOverflow.PackageId, id));

        // Vanilla's personal E.G.O. copy drops the XML cooldown and explicit tooltip IDs.
        foreach (var card in owner.personalEgoDetail.GetCardAll())
        {
            if (!Steria.AntieOverflow.TryGetResource(card, out _)) continue;
            if (card.MaxCooltimeValue != 0)
            {
                card.XmlData.EgoMaxCooltimeValue = 0;
                card.SetMaxCooltime();
                card.SetCurrentCostMax();
            }
            if (card.XmlData.Keywords.Count == 0)
                card.XmlData.Keywords.AddRange(ItemXmlDataList.instance.GetCardItem(card.GetID()).Keywords);
        }
    }
}

public abstract class DiceCardSelfAbility_AntieOverflowCup : DiceCardSelfAbilityBase
{
    internal abstract Steria.AntieResource Resource { get; }

    public override bool OnChooseCard(BattleUnitModel owner) => Steria.AntieOverflow.CanUse(owner, Resource);

    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        Steria.AntieOverflow.Use(unit, Resource);
    }
}

public sealed class DiceCardSelfAbility_AntieFlowCup : DiceCardSelfAbility_AntieOverflowCup
{
    public static string Desc = "[On Equip] Requires 30 Overflow Flow. Spend it all to divide that much Stagger recovery among allies, favoring lower Stagger Resist ratios. Each ally gains 1 Strong Flow per 10 spent.";
    public override string[] Keywords => new[] { "SteriaOverflowFlow", "SteriaStrongFlow" };
    internal override Steria.AntieResource Resource => Steria.AntieResource.Flow;
}

public sealed class DiceCardSelfAbility_AntieDreamCup : DiceCardSelfAbility_AntieOverflowCup
{
    public static string Desc = "[On Equip] Requires 20 Overflow Dream. Spend it all to divide that much Wish Shield among allies, favoring lower max HP. Each ally gains 1 Deep Dream per 10 spent.";
    public override string[] Keywords => new[] { "SteriaOverflowDream", "SteriaWishShield", "SteriaDeepDream" };
    internal override Steria.AntieResource Resource => Steria.AntieResource.Dream;
}

public sealed class DiceCardSelfAbility_AntieTideCup : DiceCardSelfAbility_AntieOverflowCup
{
    public static string Desc = "[On Equip] Requires 10 Overflow Tide. Spend it all to divide that much HP recovery among allies, favoring lower HP ratios. Each ally gains 1 Holy Tide per 10 spent.";
    public override string[] Keywords => new[] { "SteriaOverflowTide", "SteriaHolyTide" };
    internal override Steria.AntieResource Resource => Steria.AntieResource.Tide;
}
