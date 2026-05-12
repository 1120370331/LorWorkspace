using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Steria
{
    [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.GiveDeflectDamage))]
    public static class BattleDiceBehavior_GiveDeflectDamage_TidalNemesisPatch
    {
        public static void Prefix(BattleDiceBehavior __instance, BattleDiceBehavior targetDice, out BattleUnitBuf_TidalNemesisTempBreakBoost __state)
        {
            __state = null;

            try
            {
                PassiveAbility_9011002 passive = PassiveAbility_9011002.GetPassive(__instance?.owner);
                if (passive == null || !passive.ShouldBoostDeflectDamage(__instance, targetDice))
                {
                    return;
                }

                BattleUnitModel receiver = targetDice?.owner;
                if (receiver?.bufListDetail == null)
                {
                    return;
                }

                __state = new BattleUnitBuf_TidalNemesisTempBreakBoost();
                receiver.bufListDetail.AddBuf(__state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] TidalNemesis deflect prefix error: {ex}");
                __state = null;
            }
        }

        public static void Postfix(BattleUnitBuf_TidalNemesisTempBreakBoost __state)
        {
            try
            {
                __state?.Destroy();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] TidalNemesis deflect postfix error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.GiveDeflectDamage))]
    public static class BattleDiceBehavior_GiveDeflectDamage_AntieFlowFormPatch
    {
        public static void Postfix(BattleDiceBehavior __instance, BattleDiceBehavior targetDice)
        {
            try
            {
                PassiveAbility_9011001 passive = __instance?.owner?.passiveDetail?.PassiveList
                    ?.Find(p => p is PassiveAbility_9011001) as PassiveAbility_9011001;
                passive?.TryApplyFlowFormBonusDamage(__instance, targetDice?.owner);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Antie Flow Form deflect bonus patch error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(BattleUnitBreakDetail), nameof(BattleUnitBreakDetail.OnRecoverBreakByEvaision))]
    public static class BattleUnitBreakDetail_OnRecoverBreakByEvaision_TidalNemesisPatch
    {
        private static readonly FieldInfo SelfField = AccessTools.Field(typeof(BattleUnitBreakDetail), "_self");

        public static void Prefix(BattleUnitBreakDetail __instance, ref int value)
        {
            try
            {
                BattleUnitModel owner = SelfField?.GetValue(__instance) as BattleUnitModel;
                PassiveAbility_9011002 passive = PassiveAbility_9011002.GetPassive(owner);
                BattleDiceBehavior behavior = owner?.currentDiceAction?.currentBehavior;
                if (passive == null || !passive.ShouldBoostEvasionRecovery(behavior))
                {
                    return;
                }

                value = Mathf.FloorToInt(value * 1.5f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] TidalNemesis evasion recovery patch error: {ex}");
            }
        }
    }
}
