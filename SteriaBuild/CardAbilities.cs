using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine; // Needed for Debug.Log if used
// Assuming BaseMod is used for BattleObjectManager or other utilities if needed
using BaseMod; // Added for BattleObjectManager reference
using Steria; // Added to access HarmonyHelpers

// NOTE: BattleUnitBuf_Flow is defined in BattleUnitBuf_Flow.cs (MyDLL namespace)

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
         Steria.CardAbilityHelper.AddFlowStacks(owner, 5);
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
       Steria.CardAbilityHelper.AddFlowStacks(owner, 1);
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
       Steria.CardAbilityHelper.AddFlowStacks(owner, 3);
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
        Steria.CardAbilityHelper.AddFlowStacks(_owner, this.stack);
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
// Added namespace Steria for consistency and potential future conflicts avoidance
namespace Steria 
{
    public static class CardAbilityHelper
    {
        // Helper to add Flow stacks safely
        public static void AddFlowStacks(BattleUnitModel owner, int amount)
        {
            SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks called: owner={owner?.UnitData?.unitData?.name}, amount={amount}");

            if (owner == null || amount <= 0)
            {
                SteriaLogger.LogWarning($"CardAbilityHelper.AddFlowStacks: Early return (owner null or amount <= 0)");
                return;
            }

            BattleUnitBuf_Flow existingFlow = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
            SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: existingFlow = {(existingFlow != null ? "found" : "null")}");

            if (existingFlow != null)
            {
                existingFlow.stack += amount;
                SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: Updated existing Flow. New stack: {existingFlow.stack}");
            }
            else
            {
                BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow();
                newFlow.stack = amount;
                owner.bufListDetail.AddBuf(newFlow);
                SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: Created new Flow buff with {amount} stacks");
            }
        }
    }
} 