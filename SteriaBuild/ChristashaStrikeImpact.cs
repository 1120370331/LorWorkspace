using Battle.DiceAttackEffect;
using HarmonyLib;
using UnityEngine;

// RencounterManager calls CreateBehaviourEffect, then (only for Result.Win)
// CreateDamagedEffectByBehaviour in the same synchronous presentation block.
// Consume that exact native feedback event; never infer success from arrival,
// damage totals, currentDiceAction, or a timer left over from a previous dice.
internal static class ChristashaStrikeImpactRouter
{
    static DiceAttackEffect_Steria_ChristashaParticleV2 pending;
    static BattleUnitView victim;
    static int frame;

    internal static void Register(DiceAttackEffect effect, BattleUnitView target)
    {
        pending = effect as DiceAttackEffect_Steria_ChristashaParticleV2;
        victim = target;
        frame = Time.frameCount;
    }

    internal static bool Consume(BattleUnitView target)
    {
        var candidate = pending;
        bool matches = candidate != null && victim == target && frame == Time.frameCount;
        pending = null; victim = null;
        return matches && candidate.TryConfirmImpact(target);
    }
}

[HarmonyPatch(typeof(DiceEffectManager), nameof(DiceEffectManager.CreateBehaviourEffect))]
public static class ChristashaStrikePresentationPatch
{
    [HarmonyPostfix]
    public static void Postfix(DiceAttackEffect __result, BattleUnitView target)
    {
        ChristashaStrikeImpactRouter.Register(__result, target);
    }
}

[HarmonyPatch(typeof(DiceEffectManager), nameof(DiceEffectManager.CreateDamagedEffectByBehaviour))]
public static class ChristashaStrikeConfirmedImpactPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BattleUnitView self)
    {
        // Unrelated effects keep the original feedback. Only a live, matched
        // custom strike replaces it with its delayed ring / gold impact burst.
        return !ChristashaStrikeImpactRouter.Consume(self);
    }
}
