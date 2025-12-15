using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine; // Needed for Debug.Log if used
// Assuming BaseMod is used for BattleObjectManager or other utilities if needed
using BaseMod; // Added for BattleObjectManager reference
using MyDLL; // Added to access HarmonyHelpers

// --- Flow Buff Placeholder ---
// NOTE: You need to implement this properly! Based on AnhierAbilities.cs
public class BattleUnitBuf_Flow : BattleUnitBuf
{
    // Corrected access modifiers based on reference and error
    // Updated keyword IDs to match existing localization
    protected override string keywordId => "SteriaFlow";
    protected override string keywordIconId => "SteriaFlow";
    // Removed keywordTemplateId override

    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // Basic stack adding
    public override void OnAddBuf(int addedStack)
    {
        this.stack += addedStack;
        this.stack = Mathf.Max(0, this.stack); // Prevent negative stacks
        // Optional: Add UI update logic if needed
    }

    // Consume Flow logic (Placeholder - Actual consumption likely needs Harmony patches)
    public int ConsumeFlow(int amount) {
        int consumed = Mathf.Min(this.stack, amount);
        this.stack -= consumed;
         this.stack = Mathf.Max(0, this.stack); // Ensure stack doesn't go below 0 after consumption
        if (this.stack <= 0) {
            this.Destroy();
        }
        return consumed;
    }

     public override void OnRoundEnd()
     {
          // Example: Reset flow every round if needed by design
          // this.Destroy();
     }

      // Helper to consume flow and apply bonus to dice
      // This needs to be called from a Harmony patch on dice rolling/power calculation
      // This function itself likely won't be called directly, but its logic integrated into patches.
      public void ConsumeFlowForDiceBonus(BattleDiceBehavior behavior, int diceCount, int flowToConsume)
      {
           // This is complex logic based on 设定.md and needs careful implementation within a patch.
           // "消耗X流并使书页X颗骰子威力+1"
           // "如果流>书页骰子数量，则从头开始再次分配流，直到流等于0"
           if (behavior == null || this.stack <= 0 || diceCount <= 0 || flowToConsume <= 0) return;

           int flowConsumed = 0;
           int diceApplied = 0;
           int currentDiceIndex = 0; // Assuming behaviors are processed in order by the game

           // This logic needs to happen *before* the dice roll, likely in a patch
           // that iterates through the card's behaviors. This buff method is not the right place.
           Debug.LogWarning("BattleUnitBuf_Flow.ConsumeFlowForDiceBonus logic needs to be integrated into Harmony patches, not called directly here.");
      }
}


// --- Slazeya Card Abilities ---

// SlazeyaGainFlow3NextTurn (Card Self Ability)
public class DiceCardSelfAbility_SlazeyaGainFlow3NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 3 Flow";

    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 3 });
    }
}

// SlazeyaGainFlow5OnRoundStart (Card Self Ability)
public class DiceCardSelfAbility_SlazeyaGainFlow5OnRoundStart : DiceCardSelfAbilityBase
{
     public static string Desc = "[Round Start] Gain 5 Flow";

    // Per setting "回合开始时：获得5层"流"", assuming this means when the card action starts
    public override void OnStartBattle()
    {
         MyDLL.CardAbilityHelper.AddFlowStacks(owner, 5);
    }
}


// SlazeyaFlowBonusX2 (Card Self Ability) - Complex Interaction
public class DiceCardSelfAbility_SlazeyaFlowBonusX2 : DiceCardSelfAbilityBase
{
    public static string Desc = "Flow bonus for this page is doubled";
    // Needs a marker buff to signal the patch
    public class BattleUnitBuf_FlowBonusX2Marker : BattleUnitBuf {
        // public override bool IsContagious() => false; // Removed invalid override
        public override BufPositiveType positiveType => BufPositiveType.Positive;
        public override void OnRoundEnd() { this.Destroy(); } // Lasts one round
    }

    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_FlowBonusX2Marker());
    }
}

// SlazeyaMassAttackTeamLightGain (Card Self Ability)
public class DiceCardSelfAbility_SlazeyaMassAttackTeamLightGain : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] For every 5 Flow spent, next turn all allies gain 1 Light";
    public override void OnUseCard()
    {
        // Corrected: Use HarmonyHelpers from MyDLL namespace
        int flowConsumedByThisCard = HarmonyHelpers.GetFlowConsumedByCard(this.card);

        if (flowConsumedByThisCard > 0) {
            int lightToGain = flowConsumedByThisCard / 5;
            if (lightToGain > 0)
            {
               if (BattleObjectManager.instance != null) {
                   foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction)) {
                       ally.bufListDetail.AddBuf(new BattleUnitBuf_GainLightNextTurn() { stack = lightToGain });
                   }
                   Debug.Log($"[MyDLL] SlazeyaMassAttackTeamLightGain: Granting {lightToGain} light next turn to allies due to {flowConsumedByThisCard} flow consumed.");
               } else {
                   Debug.LogError("[MyDLL] BattleObjectManager.instance is null!");
               }
            }
        } else {
             // Changed LogWarning to Debug for potentially frequent 0 consumption cases
             Debug.Log($"[MyDLL] SlazeyaMassAttackTeamLightGain: No Flow consumed for this card action (Value: {flowConsumedByThisCard}).");
        }
    }
}

// --- Slazeya Dice Abilities ---

// SlazeyaGainLight1
public class DiceCardAbility_SlazeyaGainLight1 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Light";
    public override void OnSucceedAttack()
    {
        owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

// SlazeyaClashWinGainFlow2NextTurn
public class DiceCardAbility_SlazeyaClashWinGainFlow2NextTurn : DiceCardAbilityBase
{
    public static string Desc = "[Clash Win] Next turn gain 2 Flow";
    public override void OnWinParrying()
    {
         owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 2 });
    }
}

// SlazeyaClashLosePowerUpNextDice
public class DiceCardAbility_SlazeyaClashLosePowerUpNextDice : DiceCardAbilityBase
{
     public static string Desc = "[Clash Lose] Power up next dice by this dice's base power";
     // Implementation requires Harmony Patch on clash resolution or LoseParrying
     public override void OnLoseParrying()
     {
         // The logic to find the next dice and apply power needs a patch.
         // A patch on LoseParrying can get 'this' behavior (__instance)
         // and then iterate through this.card.GetDiceBehaviorList() to find the next one.
         Debug.LogWarning("SlazeyaClashLosePowerUpNextDice effect requires Harmony patching.");
     }
}

// SlazeyaDraw2
public class DiceCardAbility_SlazeyaDraw2 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Draw 2 pages";
    public override void OnSucceedAttack()
    {
        owner.allyCardDetail.DrawCards(2);
    }
}

// SlazeyaGainFlow1OnHit
public class DiceCardAbility_SlazeyaGainFlow1OnHit : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Flow";
    public override void OnSucceedAttack()
    {
       MyDLL.CardAbilityHelper.AddFlowStacks(owner, 1);
    }
}

// SlazeyaRepeatOnFlow5
public class DiceCardAbility_SlazeyaRepeatOnFlow5 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] If Flow spent >= 5, repeat dice once (Max 1)";
    private bool _triggered = false; // Track if already triggered for this attack sequence

    public override void OnSucceedAttack()
    {
         // Reset trigger at the start of this attack check, in case of multi-hit dice
         // This instance flag prevents multiple repeats *from the same dice instance* if OnSucceedAttack is called multiple times.
         _triggered = false;

         // Corrected: Use HarmonyHelpers from MyDLL namespace
         int flowConsumedByThisDice = HarmonyHelpers.GetFlowConsumedByDice(this);

         if (!_triggered && flowConsumedByThisDice >= 5)
         {
            _triggered = true;
            // Record this behavior instance in the helper set for the patch to pick up
            // Ensure this.behavior is not null
            if (this.behavior != null) 
            {
                 HarmonyHelpers._repeatTriggeredDice.Add(this.behavior);
                 Debug.Log($"[MyDLL] SlazeyaRepeatOnFlow5: Condition met ({flowConsumedByThisDice} flow consumed). Marked behavior (Hash: {this.behavior.GetHashCode()}, Index: {this.behavior.Index}) for repeat.");
            }
            else 
            {
                 Debug.LogWarning($"[MyDLL] SlazeyaRepeatOnFlow5: Condition met but this.behavior was null. Cannot mark for repeat.");
            }
            // Actual repeat logic is handled by Harmony Patch on BattleDiceBehavior.GiveDamage
         }
    }

    // Removed redundant placeholder
}

// SlazeyaMassAttackBonusDamage
public class DiceCardAbility_SlazeyaMassAttackBonusDamage : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Add bonus damage equal to Flow spent x2";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        // Corrected: Use HarmonyHelpers from MyDLL namespace
        int flowConsumedByThisCard = HarmonyHelpers.GetFlowConsumedByCard(this.card);

        if (flowConsumedByThisCard > 0) {
             int bonusDamage = flowConsumedByThisCard * 2;
            target.TakeDamage(bonusDamage, DamageType.Card_Ability, owner);
             Debug.Log($"[MyDLL] SlazeyaMassAttackBonusDamage: Dealt {bonusDamage} bonus damage to {target.UnitData.unitData.name} due to {flowConsumedByThisCard} flow consumed.");
        } else {
             // Changed LogWarning to Debug for potentially frequent 0 consumption cases
             Debug.Log($"[MyDLL] SlazeyaMassAttackBonusDamage: No Flow consumed for this card action (Value: {flowConsumedByThisCard}).");
        }
    }
}


// --- Cultist Card Abilities ---

// CultistGainFlow3NextTurn
public class DiceCardSelfAbility_CultistGainFlow3NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 3 Flow";
    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 3 }); // Reusing the helper buff
    }
}

// CultistFlow2CheckGainLightDraw
public class DiceCardSelfAbility_CultistFlow2CheckGainLightDraw : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] If Flow spent >= 2, next turn gain 2 Light and draw 1 page";
    public override void OnUseCard()
    {
        // Corrected: Use HarmonyHelpers from MyDLL namespace
        int flowConsumedByThisCard = HarmonyHelpers.GetFlowConsumedByCard(this.card);

        if (flowConsumedByThisCard >= 2)
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_GainLightNextTurn() { stack = 2 });
            owner.bufListDetail.AddBuf(new BattleUnitBuf_DrawNextTurn() { stack = 1 });
            Debug.Log($"[MyDLL] CultistFlow2CheckGainLightDraw: Granting light/draw next turn due to {flowConsumedByThisCard} flow consumed.");
        } else {
             // Changed LogWarning to Debug
             Debug.Log($"[MyDLL] CultistFlow2CheckGainLightDraw: Flow consumed ({flowConsumedByThisCard}) < 2. No effect.");
        }
    }
}

// CultistGainFlowAllyCountNextTurn
public class DiceCardSelfAbility_CultistGainFlowAllyCountNextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain Flow equal to number of surviving allies";
    public override void OnUseCard()
    {
         if (BattleObjectManager.instance != null) {
             int aliveAllies = BattleObjectManager.instance.GetAliveList(owner.faction).Count(x => x != owner);
             if (aliveAllies > 0) {
                  owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = aliveAllies });
                  Debug.Log($"[MyDLL] CultistGainFlowAllyCountNextTurn: Granting {aliveAllies} flow next turn.");
             }
         } else {
              Debug.LogError("[MyDLL] BattleObjectManager.instance is null!");
         }
    }
}

// CultistFlowBonusX2
public class DiceCardSelfAbility_CultistFlowBonusX2 : DiceCardSelfAbilityBase
{
     public static string Desc = "Flow bonus for this page is doubled";
     // Needs a marker buff to signal the patch (can reuse Slazeya's marker)
     public override void OnUseCard() {
          owner.bufListDetail.AddBuf(new DiceCardSelfAbility_SlazeyaFlowBonusX2.BattleUnitBuf_FlowBonusX2Marker());
     }
}


// --- Cultist Dice Abilities ---

// CultistDraw1
public class DiceCardAbility_CultistDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Draw 1 page";
    public override void OnSucceedAttack()
    {
        owner.allyCardDetail.DrawCards(1);
    }
}

// CultistGainFlow3
public class DiceCardAbility_CultistGainFlow3 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 3 Flow";
    public override void OnSucceedAttack()
    {
       MyDLL.CardAbilityHelper.AddFlowStacks(owner, 3);
    }
}


// --- Helper Buffs (Implementations + Fixes) ---

// Buff to gain Flow next turn
public class BattleUnitBuf_SlazeyaFlowNextTurn : BattleUnitBuf
{
    protected override string keywordId => "MyMod_FlowNextTurn"; // Needs registration via Localize/EffectTexts
    protected override string keywordIconId => "FlowIcon"; // Needs an icon resource
    // Removed keywordTemplateId

    public override void OnRoundStart()
    {
        if (_owner == null) { this.Destroy(); return; }
        MyDLL.CardAbilityHelper.AddFlowStacks(_owner, this.stack);
        _owner.bufListDetail.RemoveBuf(this);
    }
}

// Buff to gain Light next turn
public class BattleUnitBuf_GainLightNextTurn : BattleUnitBuf
{
     public override void OnRoundStart()
     {
        if (_owner == null) { this.Destroy(); return; }
        _owner.cardSlotDetail.RecoverPlayPoint(this.stack);
        _owner.bufListDetail.RemoveBuf(this);
     }
}

// Buff to draw cards next turn
public class BattleUnitBuf_DrawNextTurn : BattleUnitBuf
{
     public override void OnRoundStart()
     {
         if (_owner == null) { this.Destroy(); return; }
        _owner.allyCardDetail.DrawCards(this.stack);
         _owner.bufListDetail.RemoveBuf(this);
     }
}

// --- CardAbilityHelper Class (Copied from AnhierCards.cs) ---
// Ensure this class is defined within the scope or accessible
// Added namespace MyDLL for consistency and potential future conflicts avoidance
namespace MyDLL 
{
    public static class CardAbilityHelper
    {
        // Helper to add Flow stacks safely
        public static void AddFlowStacks(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0) return;
            // Ensure BattleUnitBuf_Flow is accessible (assuming it's in MyDLL namespace too)
            BattleUnitBuf_Flow existingFlow = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
            if (existingFlow != null)
            {
                 existingFlow.stack += amount;
                 // Make sure OnAddBuf is called if it has logic beyond stack increase
                 // existingFlow.OnAddBuf(amount); // Call only if needed
                 Debug.Log($"[MyDLL][CardAbilityHelper] Added {amount} Flow to {owner.UnitData.unitData.name}. New stack: {existingFlow.stack}");
            }
            else
            {
                BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow { stack = amount }; // Directly set stack
                owner.bufListDetail.AddBuf(newFlow); // AddBuf should handle Init and internal logic
                Debug.Log($"[MyDLL][CardAbilityHelper] Added new Flow buff ({amount} stacks) to {owner.UnitData.unitData.name}.");
            }
        }
    }
} 