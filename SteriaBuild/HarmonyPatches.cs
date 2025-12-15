using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq; // Required for Linq
using LOR_DiceSystem; // Required for DiceStatBonus etc.
using BaseMod; // Required for PassiveAbilityBase and potentially Tools
using UnityEngine; // Required for Debug.Log and Singleton
using System.Reflection;

namespace MyDLL
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

        // Method called when a card is about to be used
        public static void RegisterCardUsage(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.owner == null || card.card == null) return;

            // Check if the card has the relevant ability (e.g., SlazeyaFlow)
            bool consumesFlow = card.card.XmlData.Keywords.Contains("SlazeyaFlowKeyword"); // Example: Check for a keyword

            if (consumesFlow)
            {
                // Correctly get Flow stack by finding the custom buff instance
                BattleUnitBuf_Flow flowBufGet = card.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
                int initialFlow = flowBufGet?.stack ?? 0; // Use null-conditional access and default to 0

                _flowConsumingCards[card] = new FlowCardData
                {
                    ShouldConsumeFlow = true,
                    InitialFlowAvailable = initialFlow,
                    CurrentFlowRemaining = initialFlow // Start with the initial amount
                };
                Debug.Log($"[MyDLL] Registered Card Usage: ID={card.card.GetID()}, Hash={card.GetHashCode()}, Owner={card.owner.UnitData.unitData.name}. Consumes Flow: True. Initial Flow: {initialFlow}");
            }
            else
            {
                 // Optionally track non-consuming cards if needed for debugging
                 // _flowConsumingCards[card] = new FlowCardData { ShouldConsumeFlow = false };
                 Debug.Log($"[MyDLL] Registered Card Usage: ID={card.card.GetID()}, Hash={card.GetHashCode()}, Owner={card.owner.UnitData.unitData.name}. Consumes Flow: False.");
            }
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

        // --- Helper method specific to Precious Memory discard notification ---
        private static void NotifyPreciousMemoryDiscard(BattleUnitModel owner, LorId discardedCardId)
        {
            if (owner == null || discardedCardId == null) return;
            LorId preciousMemoryId = new LorId("SteriaBuilding", 9001006);
            if (discardedCardId == preciousMemoryId)
            {
                Debug.Log($"[MyDLL] Precious Memory card (ID: {discardedCardId}) discarded for {owner.UnitData.unitData.name}! Triggering effects.");
                
                // Apply effects: Add Flow, Draw Card
                 BattleUnitBuf_Flow existingFlow = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
                 if (existingFlow != null && !existingFlow.IsDestroyed()) {
                     existingFlow.stack += 3; 
                     existingFlow.OnAddBuf(3); 
                 } else {
                     BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow { stack = 3 };
                     owner.bufListDetail.AddBuf(newFlow);
                 }
                owner.allyCardDetail.DrawCards(1);

                // ---- Corrected Exhaust Logic ----
                try 
                {
                     // Use ExhaustCard(LorId id) which handles removing the card from all piles (hand, deck, discard, etc.)
                    owner.allyCardDetail.ExhaustCard(preciousMemoryId); 
                    Debug.Log($"[MyDLL] Successfully triggered exhaustion of Precious Memory (ID: {preciousMemoryId}) using ExhaustCard(LorId).");
                }
                catch (Exception ex) 
                {
                     Debug.LogError($"[MyDLL] Error trying to exhaust Precious Memory card with ID {preciousMemoryId}: {ex}");
                }
                // ---- End of Corrected Exhaust Logic ----
            }
        }

        // Helper to notify Passive 9000003 about discard events
        private static void NotifyPassive9000003(BattleUnitModel owner)
        {
             if (owner?.passiveDetail?.PassiveList == null) return;
             var passive = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000003) as PassiveAbility_9000003;
             passive?.Notify_CardDiscarded();
        }


        // --- Patches for Discard Handling --- 
        // [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DiscardACardByAbility), new Type[] { typeof(BattleDiceCardModel) })]
        // public static class BattleAllyCardDetail_DiscardACardByAbility_Patch { ... } // Removed
        // [HarmonyPatch(typeof(BattleAllyCardDetail), "DisCardACardRandom")] 
        // public static class BattleAllyCardDetail_DisCardACardRandom_Patch { ... } // Removed
        // Add patches for other discard methods if needed, calling NotifyPassive9000003


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

        // --- Patches for Passive 9000003 (回忆燃烧) already handled in Discard patches above --- 


        // --- REMOVED PATCH for Passive 9000004 (回忆结晶) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.SetCurrentWave))]
        // public static class StageController_SetCurrentWave_Patch { ... } // Removed
        // --- END OF REMOVED PATCH for Passive 9000004 ---

        // --- Add Patches below ---

        [HarmonyPatch]
        public static class BattleAllyCardDetail_Discard_Patch
        {
            private const int PRECIOUS_MEMORY_NUMERIC_ID = 9001006; // Define the numeric ID constant

            // Target the method for discarding a LIST of cards via ability
            [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DiscardACardByAbility), new Type[] { typeof(List<BattleDiceCardModel>) })]
            [HarmonyPostfix]
            public static void DiscardACardByAbility_List_Postfix(BattleUnitModel ____self, List<BattleDiceCardModel> cardList)
            {
                Debug.Log($"[MyDLL] DiscardACardByAbility(List)_Postfix triggered for {cardList?.Count ?? 0} cards.");
                if (cardList != null && ____self != null)
                {
                    // Iterate through the list of discarded cards
                    foreach (BattleDiceCardModel card in cardList) 
                    {
                         HandlePreciousMemoryDiscard(____self, card); // Call helper for each card
                    }
                    // Notify Passive 9000003 after handling all cards in the list (if any)
                    // We notify once per discard *action*, passive counts internally
                    NotifyPassive9000003(____self); 
                }
            }

            // Target the method for discarding a random card (Keep this one)
            [HarmonyPatch(typeof(BattleAllyCardDetail), "DisCardACardRandom")] 
            [HarmonyPostfix]
            public static void DisCardACardRandom_Postfix(BattleUnitModel ____self, BattleDiceCardModel __result) 
            {
                Debug.Log($"[MyDLL] DisCardACardRandom_Postfix triggered, discarded card: {__result?.GetID()?.ToString() ?? "null"}");
                if (__result != null && ____self != null)
                {
                    HandlePreciousMemoryDiscard(____self, __result);
                    // Notify Passive 9000003 after handling the discarded card
                    NotifyPassive9000003(____self);
                }
            }
            
            // --- Helper function to handle the logic ---
            private static void HandlePreciousMemoryDiscard(BattleUnitModel owner, BattleDiceCardModel card)
            {
                if (owner == null || card == null || card.GetID() == null)
                {
                    Debug.LogWarning($"[MyDLL] HandlePreciousMemoryDiscard called with null owner, card, or card ID. Owner: {owner?.Book?.owner?.name}, Card: {card?.GetName()}");
                    return;
                }

                // More detailed log
                Debug.Log($"[MyDLL] HandlePreciousMemoryDiscard entered for owner {owner.Book.owner.name}. Card Name='{card.GetName()}', Card LorId='{card.GetID().ToString()}', ID Only='{card.GetID().id}', PkgId='{card.GetID().packageId}'"); 

                if (card.GetID().id == PRECIOUS_MEMORY_NUMERIC_ID) // Check if it's Precious Memory
                {
                    Debug.Log($"[MyDLL] Precious Memory (Numeric ID: {PRECIOUS_MEMORY_NUMERIC_ID}) discarded! Triggering effect for {owner.Book.owner.name}.");

                    // --- Draw 2 cards --- 
                    owner.allyCardDetail.DrawCards(2);
                    Debug.Log($"[MyDLL] Drew 2 cards for {owner.Book.owner.name} via Precious Memory discard.");

                    // --- Directly add Strength (likely permanent) --- 
                    try
                    {
                        owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, owner); 
                        Debug.Log($"[MyDLL] Directly added 1 Strength (permanent) to {owner.Book.owner.name} via Precious Memory discard.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[MyDLL] Error directly adding Strength KeywordBuf: {ex.Message}");
                    }
                    
                    // --- Re-add card exhaustion logic --- 
                    try 
                    {
                        // Use ExhaustCard(LorId id) which handles removing the card from all piles
                        owner.allyCardDetail.ExhaustCard(card.GetID()); 
                        Debug.Log($"[MyDLL] Exhausted Precious Memory card (ID: {card.GetID()}) after discard.");
                    }
                    catch (Exception ex) 
                    {
                        Debug.LogError($"[MyDLL] Error trying to exhaust Precious Memory card with ID {card.GetID()}: {ex}");
                    }
                }
            }
        }

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
            // Log card usage with unique hash code
            Debug.Log($"[MyDLL] Card Usage Start Prefix: ID={__instance.card?.GetID()}, Hash={__instance.GetHashCode()}, Owner={__instance.owner?.UnitData.unitData.name}");
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

        [HarmonyPatch(typeof(BattleDiceBehavior), "RollDice")]
        [HarmonyPrefix]
        public static bool BattleDiceBehavior_RollDice_FlowPatch(BattleDiceBehavior __instance, ref int ___behaviourPower)
        {
            if (__instance.card?.card == null || __instance.owner == null) return true; // Skip if card data or owner is missing

            BattlePlayingCardDataInUnitModel currentCard = __instance.card;
            // Log the start of the process for this specific card instance
            Debug.Log($"[MyDLL] FlowPatch: Processing RollDice for card {currentCard.card.GetID()} (Instance Hash: {currentCard.GetHashCode()}). Initial behaviorPower: {___behaviourPower}, Dice Index: {__instance.DiceVanillaValue}");

            // Apply power bonuses from passives/buffers BEFORE calculating flow consumption for this dice
            // Restore the IsLorEffect check and the Stage State check
             if (__instance.owner.cardSlotDetail.PlayPoint >= 1 && !(__instance.abilityList?.Any(a => a.card == null) ?? false))
            {
                List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(__instance.owner.faction);
                aliveList.Remove(__instance.owner);
                 // Restore the stage state check
                if (aliveList.Count > 0 && Singleton<StageController>.Instance.State == StageController.StageState.Battle)
                {
                    foreach (BattleUnitModel battleUnitModel in aliveList)
                    {
                        battleUnitModel.passiveDetail.OnRollDice(__instance);
                        foreach (BattleUnitBuf battleUnitBuf in battleUnitModel.bufListDetail.GetActivatedBufList())
                            battleUnitBuf.OnRollDice(__instance);
                    }
                }
                __instance.owner.passiveDetail.OnRollDice(__instance);
                foreach (BattleUnitBuf battleUnitBuf in __instance.owner.bufListDetail.GetActivatedBufList())
                {
                    battleUnitBuf.OnRollDice(__instance);
                }
                // Log after potential power modifications by buffs/passives
                Debug.Log($"[MyDLL] FlowPatch: BehaviorPower after buffs/passives for behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}): {___behaviourPower}");
            }

            // Check if flow should be consumed for this dice roll based on the card's rules (from _flowConsumingCards)
            if (HarmonyHelpers._flowConsumingCards.TryGetValue(currentCard, out var cardData) && cardData.ShouldConsumeFlow)
            {
                int availableFlow = cardData.CurrentFlowRemaining; // Use remaining flow for this action
                BattleDiceBehavior targetBehavior = __instance;

                // Log before applying power from flow
                Debug.Log($"[MyDLL] FlowPatch: Attempting flow power for behavior (Hash: {targetBehavior.GetHashCode()}, Index: {targetBehavior.Index}). Available flow for card action: {availableFlow}");

                // --- Flow Consumption Logic ---
                // Consume flow equal to the dice's *current* power, but no more than available flow.
                // Let's assume flow is consumed *after* other power buffs are applied, as the goal is to power up the dice *using* flow.
                // We will add power directly here based on consumed flow.

                int flowToConsume = Math.Max(0, ___behaviourPower); // Consume based on current power (min 0)
                flowToConsume = Math.Min(flowToConsume, availableFlow); // Cap consumption by available flow

                if (flowToConsume > 0)
                {
                    // Correctly consume Flow by finding the buff and modifying its stack
                    bool consumedSuccessfully = false; // Initialize as false
                    BattleUnitBuf_Flow flowBufUse = __instance.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;

                    if (flowBufUse != null && flowBufUse.stack >= flowToConsume)
                    {
                        flowBufUse.stack -= flowToConsume;
                        // Optional: Check if stack reaches zero and destroy the buff
                        if (flowBufUse.stack <= 0)
                        {
                             flowBufUse.Destroy();
                        }
                        consumedSuccessfully = true;
                        Debug.Log($"[MyDLL] FlowPatch: Successfully reduced Flow stack by {flowToConsume} for unit {__instance.owner.UnitData.unitData.name}. New stack: {flowBufUse?.stack ?? 0}"); // Use null-conditional for safety
                    }
                    else
                    {
                         Debug.LogWarning($"[MyDLL] FlowPatch: Failed to consume {flowToConsume} Flow. Buff not found or insufficient stack ({flowBufUse?.stack ?? 0}) for unit {__instance.owner.UnitData.unitData.name}.");
                    }

                    if (consumedSuccessfully)
                    {
                        // Calculate the power bonus
                        int bonusPower = flowToConsume;

                        // Check for the Flow Bonus x2 Marker
                        // Note: Assumes the marker buff class is defined in CardAbilities.cs and accessible here
                        // If not, you might need to define it within this file or ensure accessibility.
                        if (__instance.owner.bufListDetail.GetActivatedBufList().Any(b => b is DiceCardSelfAbility_SlazeyaFlowBonusX2.BattleUnitBuf_FlowBonusX2Marker))
                        {
                             bonusPower *= 2;
                             Debug.Log($"[MyDLL] FlowPatch: Flow Bonus x2 marker active! Doubling bonus power to {bonusPower}.");
                        }

                        // Apply power bonus equal to the (potentially doubled) flow consumed
                         DiceStatBonus flowBonus = new DiceStatBonus { power = bonusPower };
                         targetBehavior.ApplyDiceStatBonus(flowBonus);
                         // Log the power modification
                         Debug.Log($"[MyDLL] FlowPatch: Consumed {flowToConsume} Flow. Added +{bonusPower} power to behavior (Hash: {targetBehavior.GetHashCode()}, Index: {targetBehavior.Index}). New behaviorPower: {___behaviourPower}");

                        // Record the *actual* consumption for this specific dice behavior instance (NOT doubled)
                        HarmonyHelpers.RecordFlowConsumptionForDice(targetBehavior, targetBehavior.Index, flowToConsume); // Use behavior's index

                        // Update remaining flow *for this card action* in our tracker
                        cardData.CurrentFlowRemaining -= flowToConsume;
                        Debug.Log($"[MyDLL] FlowPatch: Recorded flow consumption for behavior (Hash: {targetBehavior.GetHashCode()}, Index: {targetBehavior.Index}): {flowToConsume}. Remaining flow for card action: {cardData.CurrentFlowRemaining}");
                    }
                    else
                    {
                         Debug.LogWarning($"[MyDLL] FlowPatch: Failed to consume {flowToConsume} Flow via custom logic for behavior (Hash: {targetBehavior.GetHashCode()}, Index: {targetBehavior.Index}). Not adding power or recording consumption."); // Updated log message
                        // Record zero consumption if the game couldn't consume it
                        HarmonyHelpers.RecordFlowConsumptionForDice(targetBehavior, targetBehavior.Index, 0);
                    }
                }
                else
                {
                    Debug.Log($"[MyDLL] FlowPatch: No flow to consume for behavior (Hash: {targetBehavior.GetHashCode()}, Index: {targetBehavior.Index}). Available: {availableFlow}, Dice Power: {___behaviourPower}. Recording zero consumption.");
                    // Still record zero consumption for this dice if it was eligible but had no power/flow
                     HarmonyHelpers.RecordFlowConsumptionForDice(targetBehavior, targetBehavior.Index, 0);
                }
            }
            else
            {
                 // Log if the card doesn't consume flow or wasn't found in the dictionary
                 if (!cardData.ShouldConsumeFlow)
                     Debug.Log($"[MyDLL] FlowPatch: Card {currentCard.card.GetID()} does not consume flow. Skipping flow logic for behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}).");
                 else // ShouldConsumeFlow was true, but TryGetValue failed (shouldn't happen if Register/Cleanup is correct)
                     Debug.LogWarning($"[MyDLL] FlowPatch: Card {currentCard.card.GetID()} was expected to consume flow but was not found in _flowConsumingCards. Skipping flow logic for behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}).");
                 // Record zero consumption for this dice if the card doesn't use flow
                 HarmonyHelpers.RecordFlowConsumptionForDice(__instance, __instance.Index, 0);
            }


            // No need to log total flow consumed here as it's handled by the helper methods and overall card consumption tracking.

            return true; // Continue to original RollDice method
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

                         // --- Trigger the Repeat using SetBonusAttackDice --- 
                         // This adds the *same* behavior instance as a bonus attack.
                         // The _triggered flag within the ability instance *should* prevent the bonus attack 
                         // from re-triggering the repeat marking if OnSucceedAttack runs again for it. 
                         // The removal from _repeatTriggeredDice is the main safeguard.
                         Debug.Log($"[MyDLL] BattleDiceBehavior_GiveDamage_RepeatPatch: Repeating dice behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}) via SetBonusAttackDice.");
                         __instance.SetBonusAttackDice(__instance);
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