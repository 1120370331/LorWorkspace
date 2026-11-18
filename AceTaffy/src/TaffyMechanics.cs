using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LOR_DiceSystem;

namespace AceTaffy
{
    // The native Smoke stack is the aggregate seen by native consumers. Pressure is
    // separately displayed and tracked here; ordinary Smoke is aggregate - Pressure.
    public static class TaffyMechanics
    {
        private static bool _installed;
        private sealed class HpLossState { public bool Lost; }
        private static readonly ConditionalWeakTable<BattleUnitModel, HpLossState> HpLoss =
            new ConditionalWeakTable<BattleUnitModel, HpLossState>();
        [ThreadStatic] private static BattleUnitModel _executingCardOwner;

        public static void Install(Harmony harmony)
        {
            if (_installed) return;
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBuf_smoke), "OnAddBuf"),
                prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(SmokeAdded)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBuf_smoke), "UseStack"),
                prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(UseSmoke)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBuf_smoke), "GetDamageIncreaseRate"),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(SmokeDamageRate)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(BattleUnitBuf), "bufActivatedNameWithStack"),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(SmokeNameWithOrdinaryStack)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBuf), "GetUIData"),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(SmokeUIData)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitInformationUI_BuffSlot), "SetToolTipData"),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(InformationSmokeStack)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBuf_smoke), "OnRoundEnd"),
                prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(BeforeSmokeRoundEnd)));
            foreach (string method in new[] { "TakeDamage", "LoseHp" })
                harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), method),
                    prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(BeforeHpLoss)),
                    postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(AfterHpLoss)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "OnRoundEnd"),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(ClearHpLoss)));
            foreach (string method in new[] { "OnUseCard", "OnStartBattle", "OnStartBattleAfterCreateBehaviour",
                "OnStartParrying", "OnStartOneSideAction", "OnSucceedAttack", "OnRollDice",
                "BeforeRollDice", "OnSucceedAreaAttack", "OnWinParryingAttack", "OnWinParryingDefense",
                "AfterAction", "OnEndBattle", "OnEndAreaAttack", "OnActivateResonance",
                "OnSucceedAtkEvent", "OnSucceedDefEvent" })
            {
                var target = AccessTools.Method(typeof(BattlePlayingCardDataInUnitModel), method);
                if (target == null) throw new MissingMethodException("BattlePlayingCardDataInUnitModel", method);
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(EnterCardScope)),
                    postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(ExitCardScope)));
            }
            harmony.Patch(AccessTools.Method(typeof(BattleDiceBehavior), "OnEventDiceAbility"),
                prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(EnterDiceScope)),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(ExitCardScope)));
            // OnEventDiceAbility also dispatches owner passive callbacks after the
            // authored dice ability. Those callbacks are not page HP payments.
            foreach (string method in new[] { "BeforeRollDice", "OnRollDice", "AfterDiceAction",
                "OnSucceedAttack", "OnWinParrying", "OnLoseParrying", "OnDrawParrying" })
                harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), method,
                        new[] { typeof(BattleDiceBehavior) }),
                    prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(SuspendCardScope)),
                    postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(ExitCardScope)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBufListDetail), "RemoveBuf", new[] { typeof(BattleUnitBuf) }),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(AfterRemoveBuf)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBufListDetail), "RemoveBufAll", new[] { typeof(Type) }),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(AfterRemoveBufType)));
            harmony.Patch(AccessTools.Method(typeof(BattleUnitBufListDetail), "RemoveBufAll", new[] { typeof(KeywordBuf) }),
                postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(AfterRemoveBufKeyword)));
            foreach (string method in new[] { "AddKeywordBufThisRoundByEtc", "AddKeywordBufByEtc",
                "AddKeywordBufThisRoundByCard", "AddKeywordBufByCard", "AddKeywordBufNextNextByCard" })
                harmony.Patch(AccessTools.Method(typeof(BattleUnitBufListDetail), method),
                    prefix: new HarmonyMethod(typeof(TaffyMechanics), nameof(BeforeSmokeGain)),
                    postfix: new HarmonyMethod(typeof(TaffyMechanics), nameof(AfterSmokeGain)));
            _installed = true;
        }

        private static BattleUnitBuf_AceTaffyPressure PressureBuf(BattleUnitModel unit)
        {
            return unit == null ? null : unit.bufListDetail.GetActivatedBufList()
                .Find(b => b is BattleUnitBuf_AceTaffyPressure && !b.IsDestroyed()) as BattleUnitBuf_AceTaffyPressure;
        }

        private static BattleUnitBuf_smoke SmokeBuf(BattleUnitModel unit)
        {
            return unit == null ? null : unit.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke) as BattleUnitBuf_smoke;
        }

        public static int Pressure(BattleUnitModel unit)
        {
            var b = PressureBuf(unit);
            return b == null ? 0 : Math.Max(0, b.stack);
        }

        public static int EffectiveSmoke(BattleUnitModel unit)
        {
            var b = SmokeBuf(unit);
            return b == null ? Pressure(unit) : Math.Max(0, b.stack);
        }

        public static int Smoke(BattleUnitModel unit)
        {
            return Math.Max(0, EffectiveSmoke(unit) - Pressure(unit));
        }

        public static void AddPressure(BattleUnitModel unit, int amount)
        {
            if (unit == null || amount <= 0) return;
            int added = Math.Min(amount, 10 - Pressure(unit));
            if (added <= 0) return;
            var b = PressureBuf(unit);
            if (b == null)
            {
                b = new BattleUnitBuf_AceTaffyPressure { stack = 0 };
                unit.bufListDetail.AddBuf(b);
            }
            var smoke = SmokeBuf(unit);
            if (smoke == null)
            {
                smoke = new BattleUnitBuf_smoke { stack = 0 };
                unit.bufListDetail.AddBuf(smoke);
            }
            b.stack += added;
            smoke.stack += added; // Direct mutation avoids treating pressure as a smoke gain.
        }

        public static bool SpendPressure(BattleUnitModel unit, int amount)
        {
            if (amount <= 0) return true;
            var b = PressureBuf(unit);
            if (b == null || b.stack < amount) return false;
            b.stack -= amount;
            var smoke = SmokeBuf(unit);
            if (smoke != null) smoke.stack = Math.Max(0, smoke.stack - amount);
            if (b.stack == 0) b.Destroy();
            return true;
        }

        public static void AddSmoke(BattleUnitModel unit, int amount, BattleUnitModel source)
        {
            if (unit != null && amount > 0)
                unit.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Smoke, amount, source ?? unit);
        }

        public static bool SpendSmoke(BattleUnitModel unit, int amount)
        {
            if (amount <= 0) return true;
            var b = SmokeBuf(unit);
            return b != null && b.UseStack(amount);
        }

        public static bool SpendOrdinarySmoke(BattleUnitModel unit, int amount)
        {
            if (amount <= 0) return true;
            var smoke = SmokeBuf(unit);
            if (smoke == null || Smoke(unit) < amount) return false;
            smoke.stack -= amount;
            return true;
        }

        public static void TransferSmoke(BattleUnitModel from, BattleUnitModel to, int amount)
        {
            if (from == null || to == null || amount <= 0 || EffectiveSmoke(from) < amount) return;
            if (SpendSmoke(from, amount)) AddSmoke(to, amount, from);
        }

        public static void PayHp(BattleUnitModel unit, int amount)
        {
            if (unit == null || amount <= 0) return;
            int paid = unit.LoseHp(amount);
            if (paid <= 0) return;
            // The LoseHp postfix handles calls made inside a card callback. Direct
            // helpers still get the same event once without relying on that scope.
            if (!Object.ReferenceEquals(_executingCardOwner, unit)) OnSelfCardHpLoss(unit);
        }

        private static void OnSelfCardHpLoss(BattleUnitModel unit)
        {
            var marker = unit.bufListDetail.GetActivatedBufList()
                .Find(b => b is BattleUnitBuf_AceTaffySelfHarm && !b.IsDestroyed()) as BattleUnitBuf_AceTaffySelfHarm;
            if (marker == null)
            {
                marker = new BattleUnitBuf_AceTaffySelfHarm();
                unit.bufListDetail.AddBuf(marker);
            }
            marker.stack = 1;
            if (unit.passiveDetail.HasPassive<PassiveAbility_1883101>()) AddPressure(unit, 1);
            var guard = unit.passiveDetail.PassiveList.Find(p => p is PassiveAbility_1883103) as PassiveAbility_1883103;
            if (guard != null) guard.Activate();
        }

        public static void EnterCardScope(BattlePlayingCardDataInUnitModel __instance, out BattleUnitModel __state)
        {
            __state = _executingCardOwner;
            _executingCardOwner = __instance == null ? null : __instance.owner;
        }

        public static void EnterDiceScope(BattleDiceBehavior __instance, out BattleUnitModel __state)
        {
            __state = _executingCardOwner;
            _executingCardOwner = __instance == null || __instance.card == null ? null : __instance.owner;
        }

        public static void ExitCardScope(BattleUnitModel __state)
        {
            _executingCardOwner = __state;
        }

        public static void SuspendCardScope(out BattleUnitModel __state)
        {
            __state = _executingCardOwner;
            _executingCardOwner = null;
        }

        public static bool LostHpThisRound(BattleUnitModel unit)
        {
            return unit != null && HpLoss.GetOrCreateValue(unit).Lost;
        }

        public static bool IsThirdAttack(BattleDiceBehavior behavior)
        {
            if (behavior == null || behavior.card == null || behavior.Type != BehaviourType.Atk) return false;
            var dice = behavior.card.GetOriginalDiceBehaviorList();
            if (behavior.Index < 0 || behavior.Index >= dice.Count ||
                dice[behavior.Index].Type != BehaviourType.Atk) return false;
            int ordinal = 0;
            for (int i = 0; i <= behavior.Index; i++)
            {
                if (dice[i].Type == BehaviourType.Atk) ordinal++;
            }
            return ordinal == 3;
        }

        public static bool SmokeAdded(BattleUnitBuf_smoke __instance, int addedStack, BattleUnitModel ____owner)
        {
            if (____owner == null) return true;
            if (____owner.IsImmune(KeywordBuf.Smoke))
            {
                __instance.stack = 0;
                __instance.Destroy();
                return false;
            }
            bool heart = ____owner.passiveDetail.HasPassive<PassiveAbility_1883101>();
            if (heart || Pressure(____owner) > 0)
            {
                bool active = Object.ReferenceEquals(__instance, SmokeBuf(____owner));
                __instance.stack = Math.Min(__instance.stack,
                    (heart ? 15 : 10) + (active ? Pressure(____owner) : 0));
                return false;
            }
            return true;
        }

        private static int AllSmoke(BattleUnitBufListDetail detail)
        {
            int total = 0;
            var active = detail.GetActivatedBuf(KeywordBuf.Smoke);
            var ready = detail.GetReadyBuf(KeywordBuf.Smoke);
            if (active != null) total += active.stack;
            if (ready != null) total += ready.stack;
            foreach (var b in detail.GetReadyReadyBufList())
                if (b.bufType == KeywordBuf.Smoke) total += b.stack;
            return total;
        }

        public static void BeforeSmokeGain(BattleUnitBufListDetail __instance, KeywordBuf bufType, out int __state)
        {
            __state = bufType == KeywordBuf.Smoke ? AllSmoke(__instance) : -1;
        }

        public static void AfterSmokeGain(BattleUnitBufListDetail __instance, KeywordBuf bufType,
            int stack, int __state, BattleUnitModel ____self)
        {
            if (__state < 0 || stack <= 0 || ____self == null ||
                !____self.passiveDetail.HasPassive<PassiveAbility_1883101>()) return;
            if (AllSmoke(__instance) > __state) AddPressure(____self, 1);
        }

        public static bool UseSmoke(BattleUnitBuf_smoke __instance, int v, BattleUnitModel ____owner, ref bool __result)
        {
            if (____owner == null || Pressure(____owner) == 0) return true;
            if (v <= 0) return true;
            if (__instance.stack < v) { __result = false; return false; }
            int fromPressure = Math.Max(0, v - Smoke(____owner));
            if (fromPressure > 0)
            {
                var b = PressureBuf(____owner);
                b.stack -= fromPressure;
                if (b.stack == 0) b.Destroy();
            }
            __instance.stack -= v;
            __result = true;
            return false;
        }

        public static void SmokeDamageRate(BattleUnitBuf_smoke __instance, BattleUnitModel ____owner, ref int __result)
        {
            if (____owner == null || Pressure(____owner) == 0 && !____owner.passiveDetail.HasPassive<PassiveAbility_1883101>()) return;
            int pressure = Pressure(____owner);
            int pressureRate = pressure == 10 ? 20 : pressure >= 6 ? 10 : 0;
            bool smokePenaltySuppressed = ____owner.passiveDetail.HasPassive<PassiveAbility_1883101>() ||
                ____owner.passiveDetail.HasPassive<PassiveAbility_240026>() ||
                ____owner.IsImmune(KeywordBuf.Smoke);
            __result = (smokePenaltySuppressed ? 0 : EffectiveSmoke(____owner) * 5) + pressureRate;
        }

        public static bool BeforeSmokeRoundEnd(BattleUnitBuf_smoke __instance, BattleUnitModel ____owner)
        {
            if (____owner == null || Pressure(____owner) == 0) return true;
            if (Smoke(____owner) > 0) __instance.stack--;
            return false;
        }

        public static void BeforeHpLoss(BattleUnitModel __instance, out float __state)
        {
            __state = __instance.hp;
        }

        public static void AfterHpLoss(BattleUnitModel __instance, float __state)
        {
            if (__instance.hp >= __state) return;
            HpLoss.GetOrCreateValue(__instance).Lost = true;
            if (Object.ReferenceEquals(_executingCardOwner, __instance)) OnSelfCardHpLoss(__instance);
        }

        public static void ClearHpLoss(BattleUnitModel __instance)
        {
            HpLoss.GetOrCreateValue(__instance).Lost = false;
        }

        private static void RestoreAggregateSmoke(BattleUnitBufListDetail detail, BattleUnitModel unit)
        {
            if (unit == null || Pressure(unit) <= 0 || detail.GetActivatedBuf(KeywordBuf.Smoke) != null) return;
            detail.AddBuf(new BattleUnitBuf_smoke { stack = Pressure(unit) });
        }

        public static void AfterRemoveBuf(BattleUnitBufListDetail __instance, BattleUnitBuf buf, BattleUnitModel ____self)
        {
            if (buf is BattleUnitBuf_smoke) RestoreAggregateSmoke(__instance, ____self);
        }

        public static void AfterRemoveBufType(BattleUnitBufListDetail __instance, Type buf, BattleUnitModel ____self)
        {
            if (buf == typeof(BattleUnitBuf_smoke)) RestoreAggregateSmoke(__instance, ____self);
        }

        public static void AfterRemoveBufKeyword(BattleUnitBufListDetail __instance, KeywordBuf bufType,
            BattleUnitModel ____self)
        {
            if (bufType == KeywordBuf.Smoke) RestoreAggregateSmoke(__instance, ____self);
        }

        private static bool VisibleSmoke(BattleUnitBuf buf, BattleUnitModel owner)
        {
            return owner != null && buf is BattleUnitBuf_smoke && Pressure(owner) > 0 &&
                Object.ReferenceEquals(buf, SmokeBuf(owner));
        }

        public static void SmokeNameWithOrdinaryStack(BattleUnitBuf __instance,
            BattleUnitModel ____owner, ref string __result)
        {
            if (!VisibleSmoke(__instance, ____owner)) return;
            int total = __instance.stack;
            string suffix = total > 0 ? " " + total : "";
            string name = suffix.Length > 0 && __result.EndsWith(suffix)
                ? __result.Substring(0, __result.Length - suffix.Length) : __result;
            int ordinary = Smoke(____owner);
            __result = name + (ordinary > 0 ? " " + ordinary : "");
        }

        public static void SmokeUIData(BattleUnitBuf __instance, BattleUnitModel ____owner,
            ref BattleBufUIData __result)
        {
            if (VisibleSmoke(__instance, ____owner) && __result != null)
                __result.stack = Smoke(____owner);
        }

        public static void InformationSmokeStack(BattleUnitInformationUI_BuffSlot __instance,
            BattleUnitBuf buf)
        {
            var smoke = buf as BattleUnitBuf_smoke;
            if (smoke == null) return;
            var owner = Traverse.Create(smoke).Field("_owner").GetValue<BattleUnitModel>();
            if (!VisibleSmoke(smoke, owner)) return;
            var label = Traverse.Create(__instance).Field("_txt_stacknum").GetValue();
            if (label != null)
                AccessTools.Property(label.GetType(), "text").SetValue(label, Smoke(owner).ToString(), null);
        }
    }

    public class BattleUnitBuf_AceTaffyPressure : BattleUnitBuf
    {
        protected override string keywordId { get { return "AceTaffy_Pressure"; } }
        protected override string keywordIconId { get { return "AceTaffy_Pressure"; } }
        public override BufPositiveType positiveType { get { return BufPositiveType.Positive; } }
        public override void Destroy()
        {
            if (stack > 0 && _owner != null)
            {
                var smoke = _owner.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke);
                if (smoke != null) smoke.stack = Math.Max(0, smoke.stack - stack);
            }
            base.Destroy();
        }
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            bool attack = behavior.Detail == BehaviourDetail.Slash ||
                behavior.Detail == BehaviourDetail.Penetrate || behavior.Detail == BehaviourDetail.Hit;
            if (!attack) return;
            if (stack == 10 || stack >= 6 && behavior.Type == BehaviourType.Atk &&
                TaffyMechanics.IsThirdAttack(behavior))
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
        public override void OnRoundEnd()
        {
            if (stack > 0)
            {
                stack--;
                var smoke = _owner.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke);
                if (smoke != null) smoke.stack = Math.Max(0, smoke.stack - 1);
            }
            if (stack <= 0) Destroy();
        }
    }

    public class BattleUnitBuf_AceTaffySelfHarm : BattleUnitBuf
    {
        public override bool Hide { get { return true; } }
        public override void OnRoundEnd() { Destroy(); }
    }
}
