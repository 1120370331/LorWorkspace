using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LOR_DiceSystem;

namespace Steria
{
    // The card model, not its XML id or UI slot, owns enhancement until actual use.
    public sealed class BattleDiceCardBuf_ManualFlow : BattleDiceCardBuf
    {
        public BattleUnitModel Owner;
        public int Levels;
        public int Paid;
        public int DiceCount;
        public int[] DiceIndices;
        public bool Mass;
        public bool Committed;
        public int CommittedLevels;
        public int CommittedPaid;
        public bool Proxy;
        public int Amount => Mass ? Paid : DiceCount * Levels;
        protected override string keywordId => "SteriaManualFlow";
        public override int paramInBufDesc => Levels;
    }

    public static class FlowPlanning
    {
        private static readonly Dictionary<BattleDiceCardModel, BattleDiceCardBuf_ManualFlow> Plans =
            new Dictionary<BattleDiceCardModel, BattleDiceCardBuf_ManualFlow>();
        private static bool _committed;
        public static string Notice { get; private set; }
        public static int Revision { get; private set; }

        public static int Available(BattleUnitModel unit) => Math.Max(0, GetFlow(unit)?.stack ?? 0);
        private static BattleUnitBuf_Flow GetFlow(BattleUnitModel unit) => unit?.bufListDetail?
            .GetActivatedBufList().OfType<BattleUnitBuf_Flow>().FirstOrDefault(b => !b.IsDestroyed());

        public static BattleDiceCardBuf_ManualFlow Get(BattleDiceCardModel card)
        {
            BattleDiceCardBuf_ManualFlow plan;
            return card != null && Plans.TryGetValue(card, out plan) && !plan.IsDestroyed() ? plan : null;
        }

        public static int DiceCount(BattleDiceCardModel card) => card?.GetBehaviourList()?
            .Count(d => d.Type != BehaviourType.Standby) ?? 0;

        public static bool IsPlanning => Singleton<StageController>.Instance != null &&
            Singleton<StageController>.Instance.Phase == StageController.StagePhase.ApplyLibrarianCardPhase;

        public static bool IsInHand(BattleUnitModel owner, BattleDiceCardModel card) =>
            owner?.allyCardDetail?.GetHand()?.Contains(card) == true ||
            owner?.personalEgoDetail?.GetHand()?.Contains(card) == true ||
            (card?.XmlData?.IsFloorEgo() == true && Singleton<SpecialCardListModel>.Instance.GetHand().Contains(card));

        public static bool HasPlannedImmunity(BattleUnitModel owner)
        {
            if (owner == null) return false;
            if (DirectiveDreamHelper.HasStephanieProxy(owner)) return true;
            return BattleObjectManager.instance?.GetAliveList(owner.faction).Any(u =>
                !u.IsBreakLifeZero() && u.cardSlotDetail?.cardAry?.Any(p =>
                    p?.card?.GetID().id == 9002007 && p.card.GetID().packageId == "SteriaBuilding") == true) == true;
        }

        // Releasing a reservation is not gaining Flow: never multiply or defer it.
        private static void AdjustBalance(BattleUnitModel owner, int delta)
        {
            if (delta == 0 || owner == null) return;
            BattleUnitBuf_Flow flow = GetFlow(owner);
            if (flow == null)
            {
                if (delta > 0) owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = delta });
                return;
            }
            flow.stack = Math.Max(0, flow.stack + delta);
            if (flow.stack == 0) flow.Destroy();
        }

        public static bool TryEnhance(BattleUnitModel owner, BattleDiceCardModel card, out string message, bool automatic = false)
        {
            message = null;
            if (_committed || owner == null || card == null || owner.IsDead() || owner.IsBreakLifeZero()) return false;
            if (!automatic && (!IsPlanning || !owner.IsControlable() || !IsInHand(owner, card))) return false;
            Reconcile();
            int count = DiceCount(card);
            var plan = Get(card);
            if (plan != null && plan.Owner != owner) { message = "这张书页已由另一名角色分配流"; return false; }
            if (count <= 0) { message = "反击骰不参与普通流强化"; return false; }
            int limit = HarmonyHelpers.GetManualFlowLimit(owner, card);
            if ((plan?.Levels ?? 0) >= limit) { message = "本书页已达到流强化上限"; return false; }
            if (Available(owner) < count) { message = "流不足：需要至少 " + count + "（参与强化的骰子数）"; return false; }
            bool mass = HarmonyHelpers.IsFlowMassCard(card);
            if (plan == null)
            {
                plan = new BattleDiceCardBuf_ManualFlow
                {
                    Owner = owner, DiceCount = count, Mass = mass,
                    DiceIndices = card.GetBehaviourList().Select((die, index) => new { die, index })
                        .Where(x => x.die.Type != BehaviourType.Standby).Select(x => x.index).ToArray()
                };
                Plans.Add(card, plan);
                card.AddBuf(plan);
            }
            plan.Proxy = DirectiveDreamHelper.HasStephanieProxy(owner);
            int cost = mass ? Available(owner) : count;
            // Mass attacks consume all Flow; normal immunity does not waive that special cost.
            bool free = !mass && HasPlannedImmunity(owner);
            plan.Levels++;
            plan.Committed = false;
            if (!free)
            {
                plan.Paid += cost;
                if (!plan.Proxy) AdjustBalance(owner, -cost);
            }
            Revision++;
            message = mass ? "已为群攻投入 " + cost + " 流" : "流强化 " + plan.Levels + "/" + limit;
            return true;
        }

        // Planning reservations are repriced if an immunity page is added/removed.
        // Hooks are committed once, so UI edits cannot farm Flow-consumption passives.
        public static void Reconcile()
        {
            if (_committed) return;
            foreach (var pair in Plans.ToArray())
            {
                var plan = pair.Value;
                if (plan.Mass || plan.Committed || plan.Proxy) continue;
                bool free = HasPlannedImmunity(plan.Owner);
                int pendingLevels = plan.Levels - plan.CommittedLevels;
                int desired = plan.CommittedPaid + (free ? 0 : pendingLevels * plan.DiceCount);
                if (desired == plan.Paid) continue;
                if (desired < plan.Paid)
                {
                    AdjustBalance(plan.Owner, plan.Paid - desired);
                    plan.Paid = desired;
                }
                else
                {
                    int available = Available(plan.Owner) + plan.Paid - plan.CommittedPaid;
                    int affordable = Math.Min(pendingLevels, Math.Max(0, available / plan.DiceCount));
                    int newPaid = plan.CommittedPaid + affordable * plan.DiceCount;
                    AdjustBalance(plan.Owner, plan.Paid - newPaid);
                    plan.Paid = newPaid;
                    if (affordable < pendingLevels)
                    {
                        plan.Levels = plan.CommittedLevels + affordable;
                        Notice = "免耗效果已取消；流不足的强化已撤回";
                    }
                    if (plan.Levels == 0) { pair.Key.RemoveBuf(plan); Plans.Remove(pair.Key); }
                }
                Revision++;
            }
        }

        public static void Commit()
        {
            if (_committed) return;
            Reconcile();
            foreach (var pair in Plans.ToArray())
            {
                var plan = pair.Value;
                int newPaid = plan.Paid - plan.CommittedPaid;
                int newAmount = plan.Mass ? newPaid : (plan.Levels - plan.CommittedLevels) * plan.DiceCount;
                plan.Committed = true;
                plan.CommittedLevels = plan.Levels;
                plan.CommittedPaid = plan.Paid;
                if (newAmount > 0) HarmonyHelpers.NotifyPassivesOnFlowConsumed(plan.Owner, newAmount);
                if (!plan.Proxy && newPaid > 0 && HarmonyHelpers.IsManualFlowTransfer(pair.Key))
                    HarmonyHelpers.ScheduleManualFlowRefund(plan.Owner, newPaid, pair.Key.GetID().id);
            }
            _committed = true;
            Revision++;
        }

        public static void PrepareEnemies()
        {
            if (_committed || BattleObjectManager.instance == null) return;
            foreach (var unit in BattleObjectManager.instance.GetAliveList(false).Where(u => u.faction == Faction.Enemy || !u.IsControlable()))
            {
                // AI loadouts may change between initial preparation and the final scene lock.
                // Release only this unit's uncommitted reservations before deterministically replanning.
                // Consumption hooks/refunds are still committed once, never during these edits.
                foreach (var previous in Plans.Where(p => p.Value.Owner == unit && !p.Value.Committed).ToArray())
                {
                    var plan = previous.Value;
                    if (!plan.Proxy) AdjustBalance(unit, plan.Paid - plan.CommittedPaid);
                    plan.Levels = plan.CommittedLevels;
                    plan.Paid = plan.CommittedPaid;
                    plan.Committed = true;
                    if (plan.Levels == 0)
                    {
                        previous.Key.RemoveBuf(plan);
                        Plans.Remove(previous.Key);
                    }
                    Revision++;
                }
                // Printed cost preserves intent even when an enemy passive makes every page free.
                // Count all printed dice for priority; the shared enhancement gate still excludes Standby.
                var cards = unit.cardSlotDetail.cardAry.Where(c => c?.card != null)
                    .Select(c => c.card).Distinct()
                    .OrderByDescending(c => c.GetOriginCost())
                    .ThenBy(c => c.GetBehaviourList().Count)
                    .ToList();
                foreach (var card in cards)
                {
                    string ignored;
                    while (TryEnhance(unit, card, out ignored, true)) { }
                }
            }
        }

        // A new scene opens allocation without erasing paid enhancements still held in hand.
        public static void BeginRound()
        {
            _committed = false;
            HarmonyHelpers.ResetManualFlowActions();
            Notice = null;
            Revision++;
        }

        internal static void ConsumeEnhancement(BattleDiceCardModel card)
        {
            var plan = Get(card);
            if (plan == null) return;
            card.RemoveBuf(plan);
            Plans.Remove(card);
            Revision++;
        }

        public static void Reset()
        {
            foreach (var pair in Plans) pair.Key.RemoveBuf(pair.Value);
            Plans.Clear();
            _committed = false;
            HarmonyHelpers.ResetManualFlowActions();
            Notice = null;
            Revision++;
        }
    }

    public static partial class HarmonyHelpers
    {
        internal static void ResetManualFlowActions()
        {
            _flowPowerBonusPerCard.Clear();
            _flowEnhancementCountPerCard.Clear();
            _massAttackFlowConsumed.Clear();
            _flowBonusAppliedDice.Clear();
            _flowConsumedByCardAction.Clear();
            _flowConsumedByDiceAction.Clear();
        }
        internal static int GetManualFlowLimit(BattleUnitModel unit, BattleDiceCardModel card)
        {
            if (IsFlowMassCard(card)) return 1;
            int limit = 1;
            int special;
            if (card.GetID().packageId == "SteriaBuilding" && _multiFlowBonusCards.TryGetValue(card.GetID().id, out special)) limit = special;
            return limit + (PassiveAbility_9002001.HasExtraFlowEnhancement(unit) ? 1 : 0);
        }
        internal static bool IsFlowMassCard(BattleDiceCardModel card) => card != null &&
            card.GetID().packageId == "SteriaBuilding" && _consumeAllFlowNoBonus.Contains(card.GetID().id);
        internal static bool IsManualFlowTransfer(BattleDiceCardModel card)
        {
            if (card.GetID().packageId == "SteriaBuilding" && IsFlowTransferCard(card.GetID().id)) return true;
            var action = FlowPlanning.Get(card)?.Owner?.cardSlotDetail?.cardAry?.FirstOrDefault(p => p?.card == card);
            return HasFlowTransferKeyword(action ?? new BattlePlayingCardDataInUnitModel { card = card });
        }
        internal static void ScheduleManualFlowRefund(BattleUnitModel owner, int amount, int id) => ScheduleFlowTransferRefund(owner, amount, id);

        internal static void BindManualFlow(BattlePlayingCardDataInUnitModel card)
        {
            if (card?.card == null || _flowPowerBonusPerCard.ContainsKey(card) || _massAttackFlowConsumed.ContainsKey(card)) return;
            var plan = FlowPlanning.Get(card.card);
            if (plan == null || plan.Owner != card.owner || !plan.Committed) return;
            _flowConsumedByCardAction[card] = plan.Amount;
            if (plan.Mass)
            {
                _massAttackFlowConsumed[card] = plan.Amount;
                FlowPlanning.ConsumeEnhancement(card.card);
                return;
            }
            var power = new Dictionary<int, int>();
            var count = new Dictionary<int, int>();
            var dice = card.card.GetBehaviourList();
            foreach (int i in plan.DiceIndices)
            {
                if (i >= dice.Count || dice[i].Type == BehaviourType.Standby) continue;
                count[i] = plan.Levels;
                power[i] = plan.Levels + (plan.Proxy ? 1 : 0);
            }
            _flowPowerBonusPerCard[card] = power;
            _flowEnhancementCountPerCard[card] = count;
            FlowPlanning.ConsumeEnhancement(card.card);
        }

        // Hundred Rivers creates a derived action after use; it inherits the locked result,
        // not a persistent hand buff, and never repeats payment or consumption notifications.
        public static void InheritManualFlow(BattlePlayingCardDataInUnitModel source, BattlePlayingCardDataInUnitModel target)
        {
            if (source == null || target == null) return;
            Dictionary<int, int> values;
            int amount;
            if (_flowPowerBonusPerCard.TryGetValue(source, out values))
                _flowPowerBonusPerCard[target] = new Dictionary<int, int>(values);
            if (_flowEnhancementCountPerCard.TryGetValue(source, out values))
                _flowEnhancementCountPerCard[target] = new Dictionary<int, int>(values);
            if (_flowConsumedByCardAction.TryGetValue(source, out amount)) _flowConsumedByCardAction[target] = amount;
            if (_massAttackFlowConsumed.TryGetValue(source, out amount)) _massAttackFlowConsumed[target] = amount;
        }
    }

    // A round-start scope keeps existing next-round producers from being delayed twice.
    public static class FlowGainTiming
    {
        private static int _roundStartDepth;
        public static bool Settling => _roundStartDepth > 0;
        public static void BeginRound()
        {
            _roundStartDepth++;
            FlowPlanning.BeginRound();
            if (BattleObjectManager.instance == null) return;
            foreach (var unit in BattleObjectManager.instance.GetAliveList(false))
            {
                foreach (var pending in unit.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().ToArray())
                {
                    CardAbilityHelper.ApplyFlowStacksNow(unit, pending.stack);
                    unit.bufListDetail.RemoveBuf(pending);
                }
            }
        }
        public static void EndRoundStart() { _roundStartDepth = Math.Max(0, _roundStartDepth - 1); }
        public static void Gain(BattleUnitModel owner, int amount)
        {
            if (Settling) { CardAbilityHelper.ApplyFlowStacksNow(owner, amount); return; }
            var pending = owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().FirstOrDefault(b => !b.IsDestroyed());
            if (pending == null) owner.bufListDetail.AddBuf(new BattleUnitBuf_PendingFlow { stack = amount });
            else pending.stack += amount;
        }
    }

    public sealed class BattleUnitBuf_PendingFlow : BattleUnitBuf
    {
        protected override string keywordId => "SteriaPendingFlow";
        protected override string keywordIconId => "SteriaFlow";
        public override BufPositiveType positiveType => BufPositiveType.Positive;
    }

    [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
    public static class FlowRoundStartPatch
    {
        [HarmonyPrefix]
        public static void Prefix(bool ____bCalledRoundStart_system, out bool __state)
        {
            __state = !____bCalledRoundStart_system;
            if (__state) FlowGainTiming.BeginRound();
        }
        [HarmonyFinalizer]
        public static void Finalizer(bool __state) { if (__state) FlowGainTiming.EndRoundStart(); }
    }

    [HarmonyPatch(typeof(StageController), "CompleteApplyingLibrarianCardPhase")]
    public static class FlowPlanningCommitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(StageController __instance)
        {
            if (__instance.Phase != StageController.StagePhase.ArrangeEquippedCards) return;
            FlowPlanning.PrepareEnemies();
            FlowPlanning.Commit();
        }
    }

    [HarmonyPatch(typeof(StageController), "ApplyEnemyCardPhase")]
    public static class FlowEnemyPlanningPatch
    {
        [HarmonyPostfix]
        public static void Postfix(StageController __instance)
        {
            if (__instance.Phase == StageController.StagePhase.ApplyLibrarianCardPhase) FlowPlanning.PrepareEnemies();
        }
    }

    [HarmonyPatch(typeof(StageController), "SetCurrentWave")]
    public static class FlowPlanningWaveResetPatch
    {
        [HarmonyPrefix] public static void Prefix() { FlowPlanning.Reset(); }
    }

    [HarmonyPatch(typeof(StageController), "CloseBattleScene")]
    public static class FlowPlanningBattleResetPatch
    {
        [HarmonyPrefix] public static void Prefix() { FlowPlanning.Reset(); }
    }
}
