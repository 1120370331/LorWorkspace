using LOR_DiceSystem; // Required for KeywordBuf
using UnityEngine;

namespace MyDLL
{
    // This buff grants 1 Strength at the start of the next round and then removes itself.
    public class BattleUnitBuf_PreciousMemoryStrength : BattleUnitBuf
    {
        protected override string keywordId => "PreciousMemoryStrength_Buff"; // Use a unique keywordId for this buff itself if needed for identification
        protected override string keywordIconId => "PreciousMemoryStrength"; // Icon for this temporary buff

        // Keep track if the Strength has been granted already
        private bool _strengthGranted = false;

        public override void OnAddBuf(int stack)
        {
            base.OnAddBuf(stack);
            Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength Added to {this._owner?.Book?.owner?.name}");
        }

        // Override OnRoundStart to trigger the effect
        public override void OnRoundStart()
        {
            base.OnRoundStart(); // Call base method
            Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength OnRoundStart triggered for {this._owner?.Book?.owner?.name}. Granted: {_strengthGranted}, Destroyed: {this.IsDestroyed()}");
            
            // Only grant Strength once and if the owner exists and the buff itself isn't destroyed
            if (!_strengthGranted && this._owner != null && !this.IsDestroyed())
            {
                Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Attempting to grant 1 Strength to {this._owner.Book.owner.name}.");
                try
                {
                    // Correct way to add Strength from within a Buf's effect
                    this._owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, this._owner); 
                    Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Successfully added Strength KeywordBuf.");
                    _strengthGranted = true; // Mark as granted AFTER successful addition
                }
                catch (System.Exception ex)
                {
                     Debug.LogError($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Error adding Strength KeywordBuf: {ex.Message}");
                     // Optionally destroy self on error? Or let it retry next round?
                     // Let's destroy it to prevent potential loops/errors.
                     this.Destroy();
                     return; // Exit early on error
                }
                
                // Destroy the temporary buff immediately after granting Strength
                 Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Attempting to destroy self.");
                this.Destroy(); 
                Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Self destruction called. IsDestroyed flag: {this.IsDestroyed()}");
            }
            else if (_strengthGranted && !this.IsDestroyed())
            {
                 // If Strength was granted but somehow the buff still exists, destroy it.
                 Debug.LogWarning($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Strength already granted but buff still exists. Forcing destroy.");
                 this.Destroy();
            }
            else if (this.IsDestroyed())
            {
                Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength OnRoundStart: Buff was already destroyed.");
            }
        }

         public override void OnRoundEnd()
         {
             base.OnRoundEnd();
             Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength OnRoundEnd for {this._owner?.Book?.owner?.name}. Destroyed: {this.IsDestroyed()}");
             // As a fallback, ensure it's destroyed at round end if not already.
             if (!_strengthGranted && !this.IsDestroyed())
             {
                 Debug.LogWarning($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Destroying self at round end because Strength was not granted (maybe owner died?).");
                 this.Destroy();
             }
             else if (_strengthGranted && !this.IsDestroyed())
             {
                 // Should have been destroyed in OnRoundStart, but destroy here just in case.
                 Debug.LogWarning($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength: Destroying self at round end as a fallback.");
                 this.Destroy();
             }
         }

        public override void OnDie()
        {
             base.OnDie();
             Debug.Log($"[MyDLL] BattleUnitBuf_PreciousMemoryStrength OnDie for {this._owner?.Book?.owner?.name}");
             // Ensure self-destruction on owner death
             if (!this.IsDestroyed()) this.Destroy();
        }
    }
} 