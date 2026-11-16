using Battle.DiceAttackEffect;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;

// Match the same native winning-presentation pair used by the existing strike
// effects. A timer or reaching the victim never implies a successful hit.
internal static class SlazeyaWindImpactRouter
{
    private static DiceAttackEffect_Steria_SlazeyaWindPierce pending;
    private static DiceAttackEffect_Steria_SlazeyaWaterThunderBase waterPending;
    private static BattleUnitView victim;
    private static int frame;

    internal static void Register(DiceAttackEffect effect, BattleUnitView target)
    {
        pending=effect as DiceAttackEffect_Steria_SlazeyaWindPierce;
        waterPending=effect as DiceAttackEffect_Steria_SlazeyaWaterThunderBase;
        victim=target;frame=Time.frameCount;
    }

    internal static bool Consume(BehaviourDetail type, BattleUnitView target)
    {
        if(waterPending!=null&&victim==target&&frame==Time.frameCount)
            return waterPending.Confirm(type,target);
        var candidate=pending;
        bool matches=candidate!=null && victim==target && frame==Time.frameCount && type==BehaviourDetail.Penetrate;
        pending=null;victim=null;
        waterPending=null;
        return matches && candidate.TryConfirmPierceImpact(target);
    }

    internal static void Invalidate(DiceAttackEffect effect)
    {
        if(pending==effect){pending=null;victim=null;}
        if(waterPending==effect){waterPending=null;victim=null;}
    }
}

[HarmonyPatch(typeof(DiceEffectManager),nameof(DiceEffectManager.CreateBehaviourEffect))]
public static class SlazeyaWindImpactPresentationPatch
{
    [HarmonyPostfix]
    public static void Postfix(DiceAttackEffect __result,BattleUnitView target)
    { SlazeyaWindImpactRouter.Register(__result,target); }
}

[HarmonyPatch(typeof(DiceEffectManager),nameof(DiceEffectManager.CreateDamagedEffectByBehaviour))]
public static class SlazeyaWindConfirmedImpactPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BehaviourDetail atkType,BattleUnitView self)
    {
        // Only our confirmed near thrust replaces this generic immediate spark.
        // Its own contact flash/ring is synchronized with the visible stab.
        return !SlazeyaWindImpactRouter.Consume(atkType,self);
    }
}
