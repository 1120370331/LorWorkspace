using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq; // Required for Linq
using LOR_DiceSystem; // Required for DiceStatBonus etc.
using BaseMod; // Required for PassiveAbilityBase and potentially Tools
using UnityEngine; // Required for Debug.Log and Singleton
using System.Reflection;

namespace Steria
{
    // --- Moved Harmony Helper Methods and Storage Here ---
    public static class HarmonyHelpers // Renamed from partial HarmonyPatches to avoid confusion
    {
        // Simple class to hold flow consumption data for a card instance during its action
        internal class FlowCardData
        {
            public bool ShouldConsumeFlow { get; set; } = false; // Does this card type consume flow?
            public int InitialFlowAvailable { get; set; } = 0; // How much flow was available *when the card was used*?
            public int CurrentFlowRemaining { get; set; } = 0; // How much flow is left for subsequent dice *in this action*?
        }

        // Storage for flow consumption *rules* per card instance (determined at card use start)
        internal static Dictionary<BattlePlayingCardDataInUnitModel, FlowCardData> _flowConsumingCards = new Dictionary<BattlePlayingCardDataInUnitModel, FlowCardData>();

        // Storage for flow consumed per card instance during an action
        internal static Dictionary<BattlePlayingCardDataInUnitModel, int> _flowConsumedByCardAction = new Dictionary<BattlePlayingCardDataInUnitModel, int>();
        // Storage for flow consumed per dice behavior instance during an action
        internal static Dictionary<BattleDiceBehavior, int> _flowConsumedByDiceAction = new Dictionary<BattleDiceBehavior, int>();
        // Storage for dice behaviors that triggered a repeat effect this action
        internal static HashSet<BattleDiceBehavior> _repeatTriggeredDice = new HashSet<BattleDiceBehavior>();

        public static int GetFlowConsumedByCard(BattlePlayingCardDataInUnitModel card) {
            // Manual implementation of GetValueOrDefault
            _flowConsumedByCardAction.TryGetValue(card, out int consumed);
            return consumed; // Returns 0 if key not found
        }

        public static int GetFlowConsumedByDice(DiceCardAbilityBase ability) {
            // Assuming 'ability.behavior' IS set by the game when calling ability methods:
            // Ensure ability and behavior are not null before accessing
            BattleDiceBehavior currentBehavior = ability?.behavior;
            if (currentBehavior != null) {
                 // Manual implementation of GetValueOrDefault
                 _flowConsumedByDiceAction.TryGetValue(currentBehavior, out int consumed);
                 return consumed;
            } else {
                 // Reduced log spam: Only log warning if ability itself wasn't null
                 if (ability != null) {
                     Debug.LogWarning($"[MyDLL] HarmonyHelpers.GetFlowConsumedByDice: Could not find behavior for ability {ability.GetType().Name}. This method might not work reliably.");
                 }
                 return 0; // Cannot determine consumption without behavior instance
            }
        }

         // Method to record consumption (called from patches)
         public static void RecordFlowConsumptionForCard(BattlePlayingCardDataInUnitModel card, int amount) {
                if (card != null) {
                     // Manual implementation of GetValueOrDefault + update
                     _flowConsumedByCardAction.TryGetValue(card, out int current);
                     _flowConsumedByCardAction[card] = current + amount;
                     Debug.Log($"[MyDLL] Recorded {amount} flow consumption for card {card.card.GetName()}. Total this action: {_flowConsumedByCardAction[card]}");
                }
         }
         // Method to record consumption (called from patches) - Added index parameter
         public static void RecordFlowConsumptionForDice(BattleDiceBehavior behavior, int index, int amount) { // Added index parameter
             if (behavior != null) {
                  // Manual implementation of GetValueOrDefault + update
                  _flowConsumedByDiceAction.TryGetValue(behavior, out int current);
                  _flowConsumedByDiceAction[behavior] = current + amount;
                  // Use the passed-in index for logging
                  Debug.Log($"[MyDLL] Recorded {amount} flow consumption for dice behavior (Index: {index}). Total this action: {_flowConsumedByDiceAction[behavior]}");
             }
         }
         public static void ClearFlowConsumptionTracking() { // Call this at appropriate times (e.g., end of action/round start)
                _flowConsumedByCardAction.Clear();
                _flowConsumedByDiceAction.Clear();
                _flowConsumingCards.Clear(); // Also clear the rules dictionary
                _repeatTriggeredDice.Clear(); // Clear the repeat set as well
                Debug.Log("[MyDLL] Cleared Flow Consumption Tracking dictionaries (including repeat set).");
         }

        // 存储每颗骰子的威力加成（由流分配）- 使用卡牌实例+骰子索引作为键
        private static Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>> _flowPowerBonusPerCard =
            new Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>>();

        // 不消耗流、只获得加成的卡牌ID集合（可复用）
        // 添加新卡牌时只需在此集合中添加对应ID即可
        private static readonly HashSet<int> _flowBonusOnlyCardIds = new HashSet<int>
        {
            9001001,  // 拙劣控流
            9001008,  // 清司风流
            // 在此添加更多具有相同效果的卡牌ID...
        };

        /// <summary>
        /// 检查卡牌是否具有"只获得流加成而不消耗流"的效果
        /// </summary>
        public static bool IsFlowBonusOnlyCard(int cardId)
        {
            return _flowBonusOnlyCardIds.Contains(cardId);
        }

        /// <summary>
        /// 注册新的"只获得流加成而不消耗流"的卡牌ID（运行时动态添加）
        /// </summary>
        public static void RegisterFlowBonusOnlyCard(int cardId)
        {
            _flowBonusOnlyCardIds.Add(cardId);
            SteriaLogger.Log($"Registered card ID {cardId} as FlowBonusOnly card");
        }

        // Method called when a card is about to be used
        // 新逻辑：所有书页都会消耗流，在使用时一次性分配威力加成
        // 特殊：_flowBonusOnlyCardIds 中的卡牌只获得加成而不消耗流
        public static void RegisterCardUsage(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.owner == null || card.card == null) return;

            // 获取当前流的层数
            BattleUnitBuf_Flow flowBuf = card.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
            int flowStacks = flowBuf?.stack ?? 0;

            SteriaLogger.Log($"RegisterCardUsage: Card={card.card.GetID()}, Owner={card.owner.UnitData.unitData.name}, Flow={flowStacks}, CardHash={card.GetHashCode()}");

            if (flowStacks <= 0)
            {
                SteriaLogger.Log("RegisterCardUsage: No flow to consume");
                return;
            }

            // 获取书页的骰子数量（从XML数据获取，而不是运行时实例）
            int diceCount = card.card.XmlData.DiceBehaviourList?.Count ?? 0;
            if (diceCount == 0)
            {
                SteriaLogger.Log("RegisterCardUsage: No dice in card XML data");
                return;
            }

            // 检查是否是"流转"卡牌（不受流影响：不获得加成也不消耗流）
            bool isFlowTransfer = IsFlowBonusOnlyCard(card.card.GetID().id);
            if (isFlowTransfer)
            {
                SteriaLogger.Log($"RegisterCardUsage: [流转] card detected (ID: {card.card.GetID().id}) - not affected by flow");
                return; // 流转卡牌完全不受流影响，直接返回
            }

            SteriaLogger.Log($"RegisterCardUsage: Distributing {flowStacks} flow to {diceCount} dice");

            // 分配流到骰子：循环分配，每1层流给1颗骰子+1威力
            Dictionary<int, int> powerBonusMap = new Dictionary<int, int>();
            for (int i = 0; i < flowStacks; i++)
            {
                int diceIndex = i % diceCount;
                if (!powerBonusMap.ContainsKey(diceIndex))
                    powerBonusMap[diceIndex] = 0;
                powerBonusMap[diceIndex]++;
            }

            // 存储威力加成映射
            _flowPowerBonusPerCard[card] = powerBonusMap;
            foreach (var kvp in powerBonusMap)
            {
                SteriaLogger.Log($"RegisterCardUsage: Dice index {kvp.Key} will get +{kvp.Value} power from flow");
            }

            // 消耗所有流
            int totalConsumed = flowStacks;
            flowBuf.stack = 0;
            flowBuf.Destroy();
            SteriaLogger.Log($"RegisterCardUsage: Consumed all {totalConsumed} flow");

            // 通知 PassiveAbility_9000005
            var passive9000005 = card.owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9000005) as PassiveAbility_9000005;
            if (passive9000005 != null)
            {
                SteriaLogger.Log($"RegisterCardUsage: Notifying PassiveAbility_9000005 of {totalConsumed} flow consumed");
                passive9000005.OnFlowConsumed(totalConsumed);
            }
        }

        // 获取骰子的流威力加成（通过卡牌和骰子索引）
        public static int GetFlowPowerBonusForDice(BattlePlayingCardDataInUnitModel card, int diceIndex)
        {
            if (card != null && _flowPowerBonusPerCard.TryGetValue(card, out var bonusMap))
            {
                if (bonusMap.TryGetValue(diceIndex, out int bonus))
                {
                    return bonus;
                }
            }
            return 0;
        }

        // 清除骰子的流威力加成记录
        public static void ClearFlowPowerBonusForDice(BattlePlayingCardDataInUnitModel card, int diceIndex)
        {
            if (card != null && _flowPowerBonusPerCard.TryGetValue(card, out var bonusMap))
            {
                bonusMap.Remove(diceIndex);
                if (bonusMap.Count == 0)
                {
                    _flowPowerBonusPerCard.Remove(card);
                }
            }
        }

        // 清除卡牌的所有流威力加成记录
        public static void ClearAllFlowPowerBonusForCard(BattlePlayingCardDataInUnitModel card)
        {
            if (card != null)
            {
                _flowPowerBonusPerCard.Remove(card);
            }
        }

        // 检查卡牌是否有流威力加成记录
        public static bool HasFlowBonusForCard(BattlePlayingCardDataInUnitModel card)
        {
            return card != null && _flowPowerBonusPerCard.ContainsKey(card);
        }

        // 调试用：输出当前所有流威力加成记录
        public static void DebugLogAllFlowBonuses()
        {
            SteriaLogger.Log($"=== Flow Bonus Dictionary Debug ===");
            SteriaLogger.Log($"Total cards in dictionary: {_flowPowerBonusPerCard.Count}");
            foreach (var kvp in _flowPowerBonusPerCard)
            {
                SteriaLogger.Log($"  Card Hash={kvp.Key.GetHashCode()}, CardID={kvp.Key.card?.GetID()}");
                foreach (var diceKvp in kvp.Value)
                {
                    SteriaLogger.Log($"    Dice {diceKvp.Key}: +{diceKvp.Value} power");
                }
            }
            SteriaLogger.Log($"=== End Debug ===");
        }

        // Method called after a card has finished its action
        public static void CleanupCardUsage(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null) return;
            bool removedRule = _flowConsumingCards.Remove(card);
            bool removedConsumption = _flowConsumedByCardAction.Remove(card); // Also clear the total consumed for this card
            Debug.Log($"[MyDLL] Cleaned up Card Usage: ID={card.card?.GetID()}, Hash={card.GetHashCode()}. Removed Rule: {removedRule}, Removed Consumption: {removedConsumption}");

            // Clear dice consumption for behaviors associated with this card - IMPORTANT
             List<BattleDiceBehavior> behaviors = card.GetDiceBehaviorList();
             if (behaviors != null) {
                foreach(var behavior in behaviors) {
                     if (behavior != null) {
                         _flowConsumedByDiceAction.Remove(behavior);
                         // Also remove from repeat set if the action ends prematurely
                         _repeatTriggeredDice.Remove(behavior);
                     }
                }
             }
        }
    }


    // Contains all Harmony Patches for the mod
    // BaseMod will automatically discover and apply patches from classes
    // marked with [HarmonyPatch] if the assembly is loaded correctly.
    // However, base PatchAll only applies patches defined in this file or similar structure in the DLL.
    // Individual mods need their own PatchAll call in their Initializer.
    [HarmonyPatch] 
    public static class HarmonyPatches 
    {
        // Removed _harmonyInstance and _patched fields
        // Removed Initialize() and Unpatch() methods - ModInitializer handles PatchAll now

        // --- REMOVED TEST PATCH --- 
        // [HarmonyPatch(typeof(PassiveAbilityBase), nameof(PassiveAbilityBase.OnRoundStart))]
        // public static class PassiveAbilityBase_OnRoundStart_TestPatch { ... } // Removed
        // --- END OF REMOVED TEST PATCH ---

        // --- Helper to notify Passive 9000003 about discard events ---
        private static void NotifyPassive9000003(BattleUnitModel owner)
        {
            if (owner?.passiveDetail?.PassiveList == null) return;
            var passive = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000003) as PassiveAbility_9000003;
            passive?.Notify_CardDiscarded();
        }

        // --- Helper to handle 珍贵的回忆 被弃置时的效果 ---
        private const int PRECIOUS_MEMORY_NUMERIC_ID = 9001006;
        private const string MOD_PACKAGE_ID = "SteriaBuilding";

        private static void HandlePreciousMemoryDiscard(BattleUnitModel owner, BattleDiceCardModel card)
        {
            if (owner == null || card == null || card.GetID() == null)
            {
                SteriaLogger.LogWarning("HandlePreciousMemoryDiscard: null owner/card/ID");
                return;
            }

            LorId cardId = card.GetID();
            SteriaLogger.Log($"HandlePreciousMemoryDiscard: Card='{card.GetName()}', ID={cardId.id}, PkgId='{cardId.packageId}'");

            // 检查是否是珍贵的回忆 (同时检查 packageId 和 id)
            bool isPreciousMemory =
                cardId.id == PRECIOUS_MEMORY_NUMERIC_ID &&
                (string.IsNullOrEmpty(cardId.packageId) || cardId.packageId == MOD_PACKAGE_ID);

            if (!isPreciousMemory)
                return;

            SteriaLogger.Log("珍贵的回忆 被弃置! 触发效果: 抽2张牌 + 下一幕获得1层[强壮]");

            // 抽2张牌
            owner.allyCardDetail.DrawCards(2);
            SteriaLogger.Log("抽取了2张牌");

            // 添加一个临时Buff，下回合开始时再实际给予1层强壮
            try
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_PreciousMemoryStrength());
                SteriaLogger.Log("添加了 PreciousMemoryStrength 临时Buff（下一幕开始时获得1层强壮）");
            }
            catch (Exception ex)
            {
                SteriaLogger.LogError($"添加 PreciousMemoryStrength Buff 失败: {ex.Message}");
            }

            // 消耗这张牌（从所有牌堆中移除）
            try
            {
                owner.allyCardDetail.ExhaustCard(cardId);
                SteriaLogger.Log("消耗了珍贵的回忆");
            }
            catch (Exception ex)
            {
                SteriaLogger.LogError($"消耗珍贵的回忆失败: {ex.Message}");
            }
        }


        // --- Patches for Passive 9000001 (神脉：职业等级) --- 
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeDamage))]
        public static class PassiveAbility_9000001_Patch_TakeDamage
        {
            public static void Prefix(BattleUnitModel __instance, ref int v, DamageType type, BattleUnitModel attacker)
            {
                if (attacker?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && v > 0 && type == DamageType.Attack)
                {
                    int originalDmg = v;
                    v = (int)Math.Round(v * 1.3f);
                    Debug.Log($"[MyDLL] Passive 9000001 (Attacker: {attacker.UnitData.unitData.name}): Increasing damage to {__instance.UnitData.unitData.name} from {originalDmg} to {v} (+30%)");
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeBreakDamage))]
        public static class PassiveAbility_9000001_Patch_TakeBreakDamage
        {
            public static void Prefix(BattleUnitModel __instance, ref int damage, DamageType type, BattleUnitModel attacker, AtkResist atkResist)
            {
                if (attacker?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && damage > 0)
                {
                    int originalBreakDmg = damage;
                    damage = (int)Math.Round(damage * 1.3f);
                    Debug.Log($"[MyDLL] Passive 9000001 (Attacker: {attacker.UnitData.unitData.name}): Increasing break damage to {__instance.UnitData.unitData.name} from {originalBreakDmg} to {damage} (+30%)");
                }
            }
        }

        [HarmonyPatch(typeof(SpeedDiceRule), nameof(SpeedDiceRule.Roll))]
        public static class PassiveAbility_9000001_Patch_SpeedDiceRoll
        {
            public static void Postfix(SpeedDiceRule __instance, ref List<SpeedDice> __result, BattleUnitModel unitModel)
            {
                if (unitModel?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && __result != null)
                {
                    foreach (SpeedDice dice in __result)
                    {
                        dice.faces += 1;
                    }
                }
            }
        }


        // --- Patches for Passive 9000002 (神脉：汐与梦) --- 
        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), nameof(BattlePlayingCardSlotDetail.SpendCost))]
        public static class BattlePlayingCardSlotDetail_SpendCost_Patch
        {
            public static void Postfix(BattlePlayingCardSlotDetail __instance, int value)
            {
                var ownerField = AccessTools.Field(typeof(BattlePlayingCardSlotDetail), "_owner");
                BattleUnitModel owner = ownerField?.GetValue(__instance) as BattleUnitModel;
                if (owner != null && value > 0)
                {
                    var passive = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000002) as PassiveAbility_9000002;
                    passive?.OnActualLightSpend(value);
                }
            }
        }

        // --- Patches for Passive 9000003 (回忆燃烧) 以及 珍贵的回忆 弃置触发 ---

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnDiscardByAbility))]
        public static class BattleUnitModel_OnDiscardByAbility_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleUnitModel __instance, List<BattleDiceCardModel> cards)
            {
                try
                {
                    if (__instance == null || cards == null || cards.Count == 0)
                        return;

                    foreach (BattleDiceCardModel card in cards)
                    {
                        HandlePreciousMemoryDiscard(__instance, card);
                    }

                    // 通知 回忆燃烧 被动本幕有弃牌发生
                    NotifyPassive9000003(__instance);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MyDLL] Error in BattleUnitModel_OnDiscardByAbility_Patch Postfix: {ex}");
                }
            }
        }

        // --- Patch for BattleAllyCardDetail.DiscardACardByAbility (单张弃牌) ---
        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DiscardACardByAbility))]
        public static class BattleAllyCardDetail_DiscardACardByAbility_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleAllyCardDetail __instance, BattleDiceCardModel card)
            {
                try
                {
                    if (__instance == null || card == null)
                        return;

                    // 获取 owner
                    var ownerField = AccessTools.Field(typeof(BattleAllyCardDetail), "_owner");
                    BattleUnitModel owner = ownerField?.GetValue(__instance) as BattleUnitModel;

                    if (owner == null)
                    {
                        SteriaLogger.LogWarning("DiscardACardByAbility_Patch: Could not get owner");
                        return;
                    }

                    SteriaLogger.Log($"DiscardACardByAbility_Patch: Card='{card.GetName()}', Owner={owner.UnitData?.unitData?.name}");
                    HandlePreciousMemoryDiscard(owner, card);
                    NotifyPassive9000003(owner);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in BattleAllyCardDetail_DiscardACardByAbility_Patch Postfix: {ex}");
                }
            }
        }

        // --- Patch for BattleAllyCardDetail.DisCardACardRandom (随机弃牌) ---
        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DisCardACardRandom))]
        public static class BattleAllyCardDetail_DisCardACardRandom_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleAllyCardDetail __instance, BattleDiceCardModel __result)
            {
                try
                {
                    if (__instance == null || __result == null)
                        return;

                    // 获取 owner
                    var ownerField = AccessTools.Field(typeof(BattleAllyCardDetail), "_owner");
                    BattleUnitModel owner = ownerField?.GetValue(__instance) as BattleUnitModel;

                    if (owner == null)
                    {
                        SteriaLogger.LogWarning("DisCardACardRandom_Patch: Could not get owner");
                        return;
                    }

                    SteriaLogger.Log($"DisCardACardRandom_Patch: Card='{__result.GetName()}', Owner={owner.UnitData?.unitData?.name}");
                    HandlePreciousMemoryDiscard(owner, __result);
                    NotifyPassive9000003(owner);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in BattleAllyCardDetail_DisCardACardRandom_Patch Postfix: {ex}");
                }
            }
        }

        // --- REMOVED PATCH for Passive 9000004 (回忆结晶) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.SetCurrentWave))]
        // public static class StageController_SetCurrentWave_Patch { ... } // Removed
        // --- END OF REMOVED PATCH for Passive 9000004 ---

        // --- REMOVED BattleUnitModel_OnWaveStart_Patch Class ---

        // --- REMOVED Patch for Battle Start (No longer needed as logic is in passive Init/OnWaveStart) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        // public static class StageController_StartBattle_Patch { ... }

        // --- REMOVED Patch for Scene Start (Logic moved to Passive OnWaveStart) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.NextWave))]
        // public static class StageController_NextWave_Patch { ... } 
        
        // --- Patch for Battle Start to Reset Scene Counter for Passive 9000004 --- 
        [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        public static class StageController_StartBattle_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                PassiveAbility_9000004.ResetSceneCounter(); // Resets static counters
            }
        }

        // --- NEW Patch for Round Start System Phase (via StageController) to Handle Scene Logic for Passive 9000004 ---
        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")] // Target the private method by string name
        public static class StageController_RoundStartPhase_System_Patch // Renamed class
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try 
                {
                    int currentGameScene = Singleton<StageController>.Instance.RoundTurn;
                    int lastProcessedScene = PassiveAbility_9000004.GetLastProcessedRound();

                    Debug.Log($"[MyDLL] StageController.RoundStartPhase_System Postfix: Current Scene/Round = {currentGameScene}, Last Processed = {lastProcessedScene}");

                    // Check if this round has already been processed by this patch logic
                    if (currentGameScene > lastProcessedScene)
                    {
                        // Mark this scene/round as processed
                        PassiveAbility_9000004.SetLastProcessedRound(currentGameScene); 
                        // No need for SetRoundFlag here

                        // Increment the global scene counter
                        int currentSceneCount = PassiveAbility_9000004.IncrementAndGetSceneCount(); 
                        
                        Debug.Log($"[MyDLL] StageController.RoundStartPhase_System Postfix: New scene detected ({currentGameScene}). Incremented global counter to {currentSceneCount}. Notifying units.");

                        // Notify all alive units that have the passive
                        if (BattleObjectManager.instance != null)
                        {
                            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList())
                            {
                                if (unit?.passiveDetail?.PassiveList == null) continue;
                                var passive = unit.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000004) as PassiveAbility_9000004;
                                if (passive != null)
                                {
                                    passive.NotifySceneChanged(currentSceneCount); 
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[MyDLL] StageController.RoundStartPhase_System Postfix: BattleObjectManager instance was null when trying to notify!");
                        }
                    }
                    else
                    {
                        Debug.Log($"[MyDLL] StageController.RoundStartPhase_System Postfix: Scene {currentGameScene} already processed or not new (Last Processed: {lastProcessedScene}). Skipping increment/notify.");
                    }
                }
                catch (Exception ex)
                {
                     Debug.LogError($"[MyDLL] Error in StageController_RoundStartPhase_System_Patch Postfix: {ex}");
                }
            }
        }

        // --- Patch to Clear Flow Consumption Tracking at Round Start ---
        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
        [HarmonyPrefix] // Run before the original method
        public static void StageController_RoundStartPhase_System_ClearTracking_Prefix()
        {
            try
            {
                // Call helper from the dedicated helper class
                HarmonyHelpers.ClearFlowConsumptionTracking(); 
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MyDLL] Error in StageController_RoundStartPhase_System_ClearTracking_Prefix: {ex}");
            }
        }

        // --- NEW PATCHES FOR FLOW MECHANICS ---

        // Patch for Flow consumption and dice power bonus application
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnUseCard")]
        [HarmonyPrefix]
        public static void BattlePlayingCardDataInUnitModel_OnUseCard_Prefix(BattlePlayingCardDataInUnitModel __instance)
        {
            Debug.Log($"[Steria] Card Usage Start: ID={__instance.card?.GetID()}, Hash={__instance.GetHashCode()}, Owner={__instance.owner?.UnitData.unitData.name}");
            HarmonyHelpers.RegisterCardUsage(__instance);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnUseCard")]
        [HarmonyPostfix]
        public static void BattlePlayingCardDataInUnitModel_OnUseCard_Postfix(BattlePlayingCardDataInUnitModel __instance)
        {
            // Log card usage end
            Debug.Log($"[MyDLL] Card Usage End Postfix: ID={__instance.card?.GetID()}, Hash={__instance.GetHashCode()}");
            HarmonyHelpers.CleanupCardUsage(__instance);
        }

        // 新的简化流消耗逻辑：在 RollDice 时应用预先计算好的威力加成
        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.RollDice))]
        [HarmonyPrefix]
        public static void BattleDiceBehavior_RollDice_FlowPatch(BattleDiceBehavior __instance)
        {
            // 添加调试日志 - 每次 RollDice 都输出
            SteriaLogger.Log($"RollDice Patch TRIGGERED: Card={__instance.card?.card?.GetID()}, DiceIndex={__instance.Index}, CardHash={__instance.card?.GetHashCode()}");

            if (__instance.card?.card == null || __instance.owner == null)
            {
                SteriaLogger.Log("RollDice Patch: card or owner is null, skipping");
                return;
            }

            // 检查字典中是否有这个卡牌的记录
            bool hasCardInDict = HarmonyHelpers.HasFlowBonusForCard(__instance.card);
            SteriaLogger.Log($"RollDice Patch: HasFlowBonusForCard={hasCardInDict}");

            // 使用卡牌实例和骰子索引来查找威力加成
            int diceIndex = __instance.Index;
            int flowBonus = HarmonyHelpers.GetFlowPowerBonusForDice(__instance.card, diceIndex);
            SteriaLogger.Log($"RollDice Patch: DiceIndex={diceIndex}, FlowBonus={flowBonus}");

            if (flowBonus > 0)
            {
                SteriaLogger.Log($"RollDice FlowPatch: Applying +{flowBonus} power from flow to dice index {diceIndex}");
                __instance.ApplyDiceStatBonus(new DiceStatBonus { power = flowBonus });
                // 清除记录，避免重复应用
                HarmonyHelpers.ClearFlowPowerBonusForDice(__instance.card, diceIndex);
            }
        }


        // Patch for SlazeyaClashLosePowerUpNextDice
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnLoseParrying))] 
        public static class BattleUnitModel_OnLoseParrying_Patch // Renamed class
        {
             [HarmonyPostfix]
             // Method signature includes the unit (__instance) and the behavior that lost (behavior)
             public static void Postfix(BattleUnitModel __instance, BattleDiceBehavior behavior) 
             {
                 // Check if the ability DiceCardAbility_SlazeyaClashLosePowerUpNextDice is present on the behavior that lost
                 if (behavior?.abilityList.Exists(a => a is DiceCardAbility_SlazeyaClashLosePowerUpNextDice) == true)
                 {
                      try
                      {
                          BattlePlayingCardDataInUnitModel currentCard = behavior.card; // Get card from the behavior
                          if (currentCard == null) return;
                          List<BattleDiceBehavior> behaviorList = currentCard.GetDiceBehaviorList();
                          if (behaviorList == null) return;

                          int currentDiceIdx = -1;
                          for (int i = 0; i < behaviorList.Count; i++)
                          {
                              if (behaviorList[i] == behavior) // Find the index of the behavior that lost
                              {
                                  currentDiceIdx = i;
                                  break;
                              }
                          }

                          if (currentDiceIdx != -1 && currentDiceIdx + 1 < behaviorList.Count) // Check if there is a next dice
                          {
                              BattleDiceBehavior nextBehaviour = behaviorList[currentDiceIdx + 1];
                              // Use DiceVanillaValue instead of Dice
                              int basePower = behavior.DiceVanillaValue; 
                              if (basePower > 0)
                              {
                                  DiceStatBonus bonus = new DiceStatBonus { power = basePower };
                                  nextBehaviour.ApplyDiceStatBonus(bonus);
                                  Debug.Log($"[MyDLL] SlazeyaClashLosePowerUpNextDice: Powered up dice {currentDiceIdx + 1} by {basePower} due to dice {currentDiceIdx} losing clash on unit {__instance.UnitData.unitData.name}.");
                              }
                          }
                      }
                      catch (Exception ex)
                      {
                           Debug.LogError($"[MyDLL] Error in BattleUnitModel_OnLoseParrying_Patch Postfix: {ex}");
                      }
                 }
             }
        }

        // --- Patch for SlazeyaRepeatOnFlow5 Dice Repeat --- 
        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.GiveDamage))] // Patch after damage is dealt
        public static class BattleDiceBehavior_GiveDamage_RepeatPatch
        {
             [HarmonyPostfix]
             public static void Postfix(BattleDiceBehavior __instance) // __instance is the BattleDiceBehavior that just finished GiveDamage
             {
                 try
                 {
                     // Check if this behavior instance was marked for repeat by the ability
                     if (HarmonyHelpers._repeatTriggeredDice.Contains(__instance))
                     {
                         HarmonyHelpers._repeatTriggeredDice.Remove(__instance); // Remove immediately to prevent potential infinite loops

                         // --- Trigger the Repeat by setting isBonusAttack flag ---
                         // This marks the dice for a bonus attack.
                         // The _triggered flag within the ability instance *should* prevent the bonus attack
                         // from re-triggering the repeat marking if OnSucceedAttack runs again for it.
                         // The removal from _repeatTriggeredDice is the main safeguard.
                         Debug.Log($"[MyDLL] BattleDiceBehavior_GiveDamage_RepeatPatch: Repeating dice behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}) via isBonusAttack.");
                         __instance.isBonusAttack = true;
                     }
                 }
                 catch (Exception ex)
                 {
                      Debug.LogError($"[MyDLL] Error in BattleDiceBehavior_GiveDamage_RepeatPatch Postfix: {ex}");
                 }
             }
        }

        // TODO: Add patch for SlazeyaRepeatOnFlow5 (Highly experimental / difficult) - Removed TODO as patch is added above

    } // End of HarmonyPatches class
} // End of MyDLL namespace
