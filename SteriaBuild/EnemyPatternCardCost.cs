using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Steria
{
    /// <summary>
    /// Enemy pattern pages spend no Light, but retain their printed cost in battle UI.
    /// Track instances rather than IDs so copied pages and ordinary discounts keep their own display.
    /// </summary>
    public static class EnemyPatternCardCost
    {
        private sealed class Grant
        {
            internal readonly BattleUnitModel Owner;
            internal Grant(BattleUnitModel owner) { Owner = owner; }
        }

        private static readonly ConditionalWeakTable<BattleDiceCardModel, Grant> Grants =
            new ConditionalWeakTable<BattleDiceCardModel, Grant>();

        public static void MakeFree(BattleDiceCardModel card)
        {
            if (card == null) return;
            card.SetCostToZero(true);
            if (card.owner == null || card.owner.faction != Faction.Enemy ||
                card.GetID().packageId != "SteriaBuilding") return;

            Grants.Remove(card);
            Grants.Add(card, new Grant(card.owner));
        }

        public static int GetDisplayCost(BattleDiceCardModel card)
        {
            int payable = card.GetCost();
            Grant grant;
            if (payable == 0 && card.owner != null && card.owner.faction == Faction.Enemy &&
                Grants.TryGetValue(card, out grant) && ReferenceEquals(grant.Owner, card.owner))
                return card.GetOriginCost();
            return payable;
        }
    }

    // Both hand cards and the battle detail preview bind through SetCard. Replace only
    // its presentation reads: native number sprites, discount styling and hover colors
    // then agree, while AI, reservations and spending still use the real GetCost().
    [HarmonyPatch(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.SetCard))]
    public static class EnemyPatternCardCostUiPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo getCost = AccessTools.Method(typeof(BattleDiceCardModel), nameof(BattleDiceCardModel.GetCost));
            MethodInfo displayCost = AccessTools.Method(typeof(EnemyPatternCardCost), nameof(EnemyPatternCardCost.GetDisplayCost));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(getCost))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = displayCost;
                }
                yield return instruction;
            }
        }
    }
}
