using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseMod;

namespace MyDLL
{
    // Custom Buff: Flow
    public class BattleUnitBuf_Flow : BattleUnitBuf
    {
        // --- Static Info --- 
        protected override string keywordId => "Flow"; // ID used in XML and potentially lookups
        protected override string keywordIconId => "FlowIcon"; // ID for the icon in UI (Needs corresponding resource)

        // --- Properties --- 
        // `stack` is inherited from BattleUnitBuf and represents the amount of Flow.

        // --- Overrides --- 
        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            // Optional: Initialize things when the buff is first added
            // For Flow, usually stack is set externally, so maybe not much needed here.
        }

        public override void OnAddBuf(int addedStack)
        {
            base.OnAddBuf(addedStack);
            // Optional: Logic when stack changes (e.g., display particle effects)
            // Update stack visualization or trigger other effects if needed.
             _owner.personalEgoDetail.RemoveCard(Tools.MakeLorId(1));
             _owner.personalEgoDetail.AddCard(Tools.MakeLorId(1));
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            // Optional: Logic at the start of each round (e.g., decay, gain)
            // Could potentially remove the buff if stack is 0, or decrease it.
        }

        // --- Custom Methods (Optional) --- 
        // public void UseFlow(int amount) { ... } // Potentially helper to decrease stack
        // public int GetFlow() { return stack; } // Simple getter
    }
} 