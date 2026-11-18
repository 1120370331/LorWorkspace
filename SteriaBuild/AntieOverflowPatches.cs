using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;

namespace Steria
{
    [HarmonyPatch(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.SetEgoCoolTimeGauge))]
    internal static class AntieCupCooldownPatch
    {
        // Vanilla treats a zero maximum as an empty gauge. The cup's OnChooseCard handles its resource gate.
        private static bool Prefix(BattleDiceCardUI __instance, GameObject ___ob_EgoCoolTime, ref bool ___isEgoCoolTimeLock)
        {
            if (!AntieOverflow.TryGetResource(__instance.CardModel, out _)) return true;
            ___isEgoCoolTimeLock = false;
            if (___ob_EgoCoolTime != null)
            {
                ___ob_EgoCoolTime.SetActive(false);
                __instance.SetEgoFrameLockColor(false);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.SpendCardAndCost))]
    internal static class AntieHolyTideEquipPatch
    {
        private static void Prefix(BattleUnitModel __instance, BattleDiceCardModel card, out bool __state)
        {
            __state = card != null && !card.costSpended && card.GetSpec().Ranged == CardRange.Instance &&
                AntieOverflow.Find<BattleUnitBuf_HolyTide>(__instance) != null && card.GetCost() >= 4;
        }

        private static void Postfix(BattleUnitModel __instance, BattleDiceCardModel card, bool __state)
        {
            // Instance pages pay directly through their slot and have no rolled dice to bless.
            if (__state && card.costSpended)
                AntieOverflow.SpendSpecial<BattleUnitBuf_HolyTide>(__instance, AntieResource.Tide);
        }
    }

    [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), nameof(BattlePlayingCardSlotDetail.AddCard))]
    internal static class AntieCupSubmissionPatch
    {
        private static bool Prefix(BattleUnitModel ____self, BattleDiceCardModel card)
        {
            if (!AntieOverflow.TryGetResource(card, out var resource)) return true;
            return AntieOverflow.CanUse(____self, resource) && ____self.personalEgoDetail.GetHand().Contains(card);
        }
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnUseCard))]
    internal static class AntieHolyTideUsePatch
    {
        private static readonly ConditionalWeakTable<BattlePlayingCardDataInUnitModel, object> BlessedActions =
            new ConditionalWeakTable<BattlePlayingCardDataInUnitModel, object>();
        private static readonly object Blessing = new object();

        internal static bool IsBlessed(BattlePlayingCardDataInUnitModel action) =>
            action != null && BlessedActions.TryGetValue(action, out _);

        private static void Prefix(BattleUnitModel __instance, BattlePlayingCardDataInUnitModel card, out bool __state)
        {
            __state = card?.card != null && card.owner == __instance && !card.isKeepedCard && !card.isDestroyed &&
                !card.card.costSpended && !IsBlessed(card) &&
                AntieOverflow.Find<BattleUnitBuf_HolyTide>(__instance) != null && card.card.GetCost() >= 4;
        }

        private static void Postfix(BattleUnitModel __instance, BattlePlayingCardDataInUnitModel card, bool __state)
        {
            if (__state && card.card.costSpended && !IsBlessed(card) &&
                AntieOverflow.SpendSpecial<BattleUnitBuf_HolyTide>(__instance, AntieResource.Tide))
                BlessedActions.Add(card, Blessing);
        }
    }

    [HarmonyPatch(typeof(BattleParryingManager), "RollDice")]
    internal static class AntieParryingRollPatch
    {
        private static void Prefix(BattleParryingManager.ParryingTeam ____teamEnemy,
            BattleParryingManager.ParryingTeam ____teamLibrarian, out AntieRollScope __state)
        {
            __state = AntieRollScope.Current;
            AntieRollScope.Current = new AntieRollScope { Enemy = ____teamEnemy, Librarian = ____teamLibrarian };
        }

        private static void Finalizer(AntieRollScope __state) => AntieRollScope.Current = __state;
    }

    [HarmonyPatch(typeof(BattleOneSidePlayManager), "Decision")]
    internal static class AntieOneSideRollPatch
    {
        private static void Prefix(BattlePlayingCardDataInUnitModel ____playingCard, BattleUnitModel ___victim,
            out AntieRollScope __state)
        {
            __state = AntieRollScope.Current;
            AntieRollScope.Current = new AntieRollScope { Unopposed = ____playingCard, Victim = ___victim };
        }

        private static void Finalizer(AntieRollScope __state) => AntieRollScope.Current = __state;
    }

    [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.RollDice))]
    internal static class AntieDiceRollPatch
    {
        private static readonly ConditionalWeakTable<BattleDiceBehavior, HashSet<BattleUnitModel>> DeepDreamTargets =
            new ConditionalWeakTable<BattleDiceBehavior, HashSet<BattleUnitModel>>();

        [HarmonyPriority(Priority.Last)]
        private static void Prefix(BattleDiceBehavior __instance, DiceStatBonus ____statBonus)
        {
            var scope = AntieRollScope.Current;
            if (scope?.Retrying == __instance || __instance.card?.owner == null) return;

            if (AntieHolyTideUsePatch.IsBlessed(__instance.card))
                ApplyPowerBonus(__instance, 4);

            if (scope == null) return;
            var defender = scope.GetDefender(__instance);
            if (defender != null && !defender.IsDead() && defender.faction != __instance.owner.faction)
            {
                bool weakened = DeepDreamTargets.TryGetValue(__instance, out var targets) && targets.Contains(defender);
                if (!weakened && AntieOverflow.SpendSpecial<BattleUnitBuf_DeepDream>(defender, AntieResource.Dream))
                {
                    DeepDreamTargets.GetOrCreateValue(__instance).Add(defender);
                    weakened = true;
                }
                if (weakened) ApplyPowerBonus(__instance, -4, -4);
            }

            // Save the pre-roll stats after vanilla BeforeRollDice and the existing Flow/Tide roll prefixes.
            // Retrying restores this state so an OnRollDice bonus from the first roll cannot compound.
            if (__instance.Type == BehaviourType.Atk && !MusicDiceSystem.IsMusicDiceBehaviour(__instance) &&
                AntieOverflow.Find<BattleUnitBuf_StrongFlow>(__instance.owner) != null)
            {
                if (scope.Enemy?.playingCard.currentBehavior == __instance)
                    scope.EnemySnapshot = (__instance, ____statBonus.Copy());
                if (scope.Librarian?.playingCard.currentBehavior == __instance)
                    scope.LibrarianSnapshot = (__instance, ____statBonus.Copy());
            }
        }

        internal static void ApplyPowerBonus(BattleDiceBehavior dice, int power, int damage = 0)
        {
            MusicDicePowerScope.RunWithAllowance(() => PrimalTidePowerScope.RunWithAllowance(() =>
                dice.ApplyDiceStatBonus(new DiceStatBonus { power = power, dmg = damage })));
        }
    }

    [HarmonyPatch(typeof(BattleParryingManager), "AfterRollDice")]
    internal static class AntieStrongFlowRetryPatch
    {
        private static readonly AccessTools.FieldRef<BattleDiceBehavior, DiceStatBonus> StatBonus =
            AccessTools.FieldRefAccess<BattleDiceBehavior, DiceStatBonus>("_statBonus");

        private static void Prefix()
        {
            var scope = AntieRollScope.Current;
            if (scope?.Enemy == null || scope.Librarian == null) return;

            // At most one retry per die in this clash. Once this returns, vanilla compares the results,
            // emits the actual victory/defeat events and advances those same dice as usual.
            for (int i = 0; i < 2 && scope.Enemy.DiceExists() && scope.Librarian.DiceExists(); i++)
            {
                int comparison = scope.Enemy.diceValue.CompareTo(scope.Librarian.diceValue);
                if (comparison == 0) return;
                var loser = comparison < 0 ? scope.Enemy : scope.Librarian;
                var snapshot = comparison < 0 ? scope.EnemySnapshot : scope.LibrarianSnapshot;
                var dice = loser.playingCard.currentBehavior;
                if (snapshot.Die != dice || dice.DiceDestroyed || !AntieOverflow.SpendSpecial<BattleUnitBuf_StrongFlow>(loser.unit, AntieResource.Flow)) return;

                if (comparison < 0) scope.EnemySnapshot = default;
                else scope.LibrarianSnapshot = default;
                StatBonus(dice) = snapshot.Stats;
                AntieDiceRollPatch.ApplyPowerBonus(dice, 4);
                scope.Retrying = dice;
                try
                {
                    loser.RollDice();
                    scope.Enemy.UpdateDiceFinalValue();
                    scope.Librarian.UpdateDiceFinalValue();
                }
                finally
                {
                    scope.Retrying = null;
                }
            }
        }
    }

    internal sealed class AntieRollScope
    {
        [ThreadStatic] internal static AntieRollScope Current;
        internal BattleParryingManager.ParryingTeam Enemy;
        internal BattleParryingManager.ParryingTeam Librarian;
        internal BattlePlayingCardDataInUnitModel Unopposed;
        internal BattleUnitModel Victim;
        internal (BattleDiceBehavior Die, DiceStatBonus Stats) EnemySnapshot;
        internal (BattleDiceBehavior Die, DiceStatBonus Stats) LibrarianSnapshot;
        internal BattleDiceBehavior Retrying;

        internal BattleUnitModel GetDefender(BattleDiceBehavior dice)
        {
            if (Victim != null && Unopposed.currentBehavior == dice && dice.Type == BehaviourType.Atk) return Victim;

            BattleParryingManager.ParryingTeam other;
            if (Enemy?.playingCard.currentBehavior == dice) other = Librarian;
            else if (Librarian?.playingCard.currentBehavior == dice) other = Enemy;
            else return null;

            var opposingDie = other.playingCard.currentBehavior;
            if (opposingDie == null) return dice.Type == BehaviourType.Atk ? other.unit : null;
            return opposingDie.Type == BehaviourType.Def || (opposingDie.Type == BehaviourType.Standby &&
                (opposingDie.Detail == BehaviourDetail.Guard || opposingDie.Detail == BehaviourDetail.Evasion)) ? other.unit : null;
        }
    }
}
