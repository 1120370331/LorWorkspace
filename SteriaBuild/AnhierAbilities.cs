using System;
using LOR_DiceSystem;
using System.Collections.Generic; 
using System.Linq; // Required for Linq operations like Count()
using UnityEngine;
using BaseMod; // 确保引用 BaseMod
using Sound; // Added for BattleEffectManager
// Removed using Battle.CreatureEffect; 
// using AttackEffectManager directly (no namespace)

namespace MyDLL
{
    // Removed BattleUnitBuf_AnhierStats as stats are now handled directly or via patches

    // 这个文件包含 安希尔 的相关能力实现
     // --- 安希尔 被动 --- 

    // 神脉：职业等级（安希尔）(ID: 9000001)
    public class PassiveAbility_9000001 : PassiveAbilityBase
    {
        // Stats are now handled by overrides below or patches

        // +15 Max HP
        public override int GetMaxHpBonus() => 15;

        // +15 Max Break Resist
        public override int GetMaxBpBonus() => 15;
        
        // +1 速度 (通过 Patch 修改骰子面数实现)
        public override void OnWaveStart()
        {
            base.OnWaveStart();
            // 移除添加迅捷的代码
            if (this.owner != null && !this.owner.IsDead())
            {
                // this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Quickness, 1, this.owner);
                // 恢复 HP (保留)
                this.owner.RecoverHP(15);
            }
        }

        // +30% Damage is handled via Harmony Patch on BattleUnitModel.TakeDamage
        // +30% Stagger Damage is handled via Harmony Patch on BattleUnitModel.TakeBreakDamage
    }

    // 神脉：汐与梦（司流-自我之流）(ID: 9000002)
    public class PassiveAbility_9000002 : PassiveAbilityBase
    {
        private bool _attackedOneSideThisTurn = false;
        private bool _addFlowNextRound = false; 
        private int _lightSpentSinceLastFlow = 0; // Track light spent for flow gain

        public override void OnRoundStart()
        {
            if (_addFlowNextRound)
            {
                 AddFlowStacks(3);
                 _addFlowNextRound = false; 
            }
            _attackedOneSideThisTurn = false;
            // _lightSpentSinceLastFlow persists across rounds until flow is granted
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            base.OnTakeDamageByAttack(atkDice, dmg);
            Debug.Log($"[MyDLL] Passive 9000002 OnTakeDamageByAttack: Owner={this.owner.UnitData.unitData.name}, Attacker={atkDice?.owner?.UnitData?.unitData?.name ?? "null"}, Dmg={dmg}");

            if (_addFlowNextRound || _attackedOneSideThisTurn || atkDice?.owner == this.owner || atkDice?.card == null)
            { 
                 Debug.Log("[MyDLL] Passive 9000002: Skipping (Already triggered this turn, self attack, or no card).");
                 return; 
            }
            if (atkDice.card.target != this.owner)
            { 
                 Debug.Log("[MyDLL] Passive 9000002: Skipping (Attack target is not self).");
                 return; 
            }

            bool isOneSided = true;
            int opponentSlotOrder = atkDice.card.slotOrder;
            Debug.Log($"[MyDLL] Passive 9000002: Checking for clash. Attacker Slot Order: {opponentSlotOrder}");
            foreach (BattlePlayingCardDataInUnitModel playingCard in this.owner.cardSlotDetail.cardAry)
            {
                if (playingCard != null && playingCard.target == atkDice.owner && playingCard.slotOrder == opponentSlotOrder) 
                {
                     Debug.Log($"[MyDLL] Passive 9000002: Clash detected with card {playingCard.card.GetName()} at slot {playingCard.slotOrder}. Not one-sided.");
                     isOneSided = false;
                     break;
                }
            }

            if (isOneSided)
            {
                 Debug.Log("[MyDLL] Passive 9000002: One-sided attack detected! Triggering effect.");
                 _attackedOneSideThisTurn = true; 
                 _addFlowNextRound = true; 
                 int healAmount = (int)(dmg * 0.5f);
                 if (healAmount > 0) { this.owner.RecoverHP(healAmount); }
            }
        }
        
        // Removed approximate OnUseCard check
        // Gain Flow on light cost is now handled via Harmony Patch on BattlePlayingCardSlotDetail.SpendCost
        // This method will be called by the patch
        public void OnActualLightSpend(int amount)
        {
             if (amount <= 0) return;
             _lightSpentSinceLastFlow += amount;
             int flowToAdd = _lightSpentSinceLastFlow / 3;
             if (flowToAdd > 0)
             {
                 AddFlowStacks(flowToAdd);
                 _lightSpentSinceLastFlow %= 3; // Keep remainder for next spending
             }
        }
        
        // Helper to add flow (could be moved to a static helper class)
        private void AddFlowStacks(int amount)
        {
            if (amount <= 0 || this.owner == null || this.owner.bufListDetail == null) return;
            // Try to find existing buf
            BattleUnitBuf_Flow existingFlow = this.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
             if (existingFlow != null && !existingFlow.IsDestroyed())
             {
                 Debug.Log($"[MyDLL] Adding {amount} stack(s) to existing Flow buff.");
                 existingFlow.stack += amount;
                 existingFlow.OnAddBuf(amount); // Manually call OnAddBuf if needed
             }
             else
             {
                 Debug.Log($"[MyDLL] Adding new Flow buff with {amount} stack(s).");
                 BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow();
                 newFlow.stack = amount;
                 this.owner.bufListDetail.AddBuf(newFlow); // Use AddBuf to handle adding to list
                 // Note: AddBuf might internally call Init and OnAddBuf, check BaseMod/Game source if issues persist
             }
        }
    }

    // 回忆燃烧 (ID: 9000003) - Restored
    public class PassiveAbility_9000003 : PassiveAbilityBase
    {
        // Discard detection is handled via Harmony Patches on discard methods
        private int _discardCountThisRound = 0;
        private bool _buffAppliedThisRound = false;
        private bool _shouldGainBuffNextRound = false; // Flag to add buff next round

         public override void OnRoundStart()
         {
             // Check if buff should be added at the start of the round
             if (_shouldGainBuffNextRound)
             {
                 this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, this.owner);
                 this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Protection, 1, this.owner);
                 _shouldGainBuffNextRound = false; // Reset flag
             }
             // Reset round counters and flags
             _discardCountThisRound = 0;
             _buffAppliedThisRound = false;
         }

         // This method will be called by Harmony patches on Discard methods
         public void Notify_CardDiscarded() 
         {
             if (_buffAppliedThisRound) return; // If triggered this round, do nothing
             _discardCountThisRound++;
             if (_discardCountThisRound >= 2)
             {
                 // Set flag to add buff next round instead of immediately
                 _shouldGainBuffNextRound = true; 
                 _buffAppliedThisRound = true; // Mark as triggered this round to prevent multiple triggers
             }
         }
    }

    // --- 新增 Buff: 回忆结晶能量 ---
    public class BattleUnitBuf_CrystalizedPower : BattleUnitBuf
    {
        // 设置 Keyword ID 和 Icon ID (使用已加载的图标)
        protected override string keywordId => "MemCrystalPassive"; 
        protected override string keywordIconId => "MemCrystalPassive"; // Use the loaded icon name

        public override BufPositiveType positiveType => BufPositiveType.Positive; // 标记为增益效果

        public override void OnAddBuf(int addedStack)
        {
            // Add log here to see when OnAddBuf is called and with what values
            Debug.Log($"[MyDLL] BattleUnitBuf_CrystalizedPower ({this._owner?.UnitData?.unitData?.name ?? "Unknown Owner"}): OnAddBuf called. addedStack = {addedStack}, current stack BEFORE adjustment = {this.stack}");
            // 限制最大层数 - This logic might conflict if the passive already sets the correct stack
            // Let's adjust it: Ensure stack never EXCEEDS 3, but allow passive to set it correctly.
            this.stack += addedStack; // Apply added stack first
            if (this.stack > 3) 
            {
                 Debug.Log($"[MyDLL] BattleUnitBuf_CrystalizedPower: Stack exceeded 3 ({this.stack}), clamping to 3.");
                 this.stack = 3;
            }
             Debug.Log($"[MyDLL] BattleUnitBuf_CrystalizedPower: OnAddBuf finished. Final stack = {this.stack}");
        }

        // 在掷骰前为所有骰子增加威力
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            // Remove the check for IsAttackDice to apply to ALL dice types
            if (this.stack <= 0 || behavior == null)
                return;

            Debug.Log($"[MyDLL] BattleUnitBuf_CrystalizedPower ({this._owner.UnitData.unitData.name}): Applying +{this.stack} power to dice type {behavior.Detail} (Buff Stack: {this.stack})."); // Added log for clarity
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = this.stack });
            // Optional: Flash effect
            // SingletonBehavior<DiceEffectManager>.Instance.IndicateDiceEffect(behavior.owner.view.characterRotationCenter, DiceEffectType.Strength, behavior.owner.faction, Color.cyan);
        }
    }

    // 回忆结晶 (ID: 9000004)
    public class PassiveAbility_9000004 : PassiveAbilityBase
    {
        // Static variables for global scene tracking
        private static int _currentSceneCount = 0;
        private static bool _sceneCounterIncrementedThisRound = false; // Flag to ensure increment only once per logical round (hooked via BattleObjectManager.OnRoundStart)
        private static int _lastProcessedRound = -1; // Tracks the last game round processed by the patch

        // Instance variables for stacks on this unit
        private int _currentStacks = 0;      
        private const int SCENES_PER_STACK = 5; 
        private const int MAX_STACKS = 3;

        // Override Init to initialize instance variables
        public override void Init(BattleUnitModel self)
        {
            base.Init(self);
            // _currentStacks should be initialized to 0 by default
            Debug.Log($"[MyDLL] Passive 9000004 ({this.owner?.UnitData?.unitData?.name ?? "Unknown Owner"}): Initializing passive. Initial _currentStacks = {_currentStacks}"); 
        }

        // Static method to reset counters at the start of a battle (called by Harmony patch)
        public static void ResetSceneCounter()
        {
            _currentSceneCount = 0;
            _sceneCounterIncrementedThisRound = false; 
            _lastProcessedRound = -1; 
            Debug.Log("[MyDLL] Passive 9000004: Static counters reset (_currentSceneCount=0, _sceneCounterIncrementedThisRound=false, _lastProcessedRound=-1)."); 
        }

        // Static method to get the current scene count (used by patch)
        public static int GetCurrentSceneCount()
        {
             return _currentSceneCount;
        }
        
        // Static method to increment and get the current scene count (called by patch)
        public static int IncrementAndGetSceneCount()
        {
            _currentSceneCount++;
            Debug.Log($"[MyDLL] Passive 9000004 Static Counter: Incremented scene count to {_currentSceneCount}");
            return _currentSceneCount;
        }
        
        // Static method to set the round flag (called by patch)
        public static void SetRoundFlag(bool value)
        {
             _sceneCounterIncrementedThisRound = value;
        }
        
        // Static method to update the last processed round (called by patch)
        public static void SetLastProcessedRound(int round)
        {
             _lastProcessedRound = round;
        }
        
        // Static getter for the round flag (used by patch)
        public static bool GetRoundFlag()
        {
            return _sceneCounterIncrementedThisRound;
        }

        // Static getter for the last processed round (used by patch)
        public static int GetLastProcessedRound()
        {
             return _lastProcessedRound;
        }


        // Instance method called by Harmony patch when scene/round changes
        public void NotifySceneChanged(int currentGlobalScene) 
        {
            Debug.Log($"[MyDLL] Passive 9000004 ({(this.owner?.UnitData?.unitData?.name ?? "Unknown Owner")}): NotifySceneChanged called for global scene {currentGlobalScene}. Current instance stacks: {_currentStacks}");

            if (this.owner == null || this.owner.IsDead())
            {
                Debug.LogWarning($"[MyDLL] Passive 9000004 ({(this.owner?.UnitData?.unitData?.name ?? "Dead/Null Owner")}): Owner is null or dead. Skipping update.");
                return;
            }

            int expectedStacks = Mathf.Min(MAX_STACKS, currentGlobalScene / SCENES_PER_STACK);
            Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Calculated expected stacks: {expectedStacks} (based on global scene {currentGlobalScene}).");

            // Check if the expected stacks have changed OR if the buff needs initial application/correction
            if (expectedStacks != _currentStacks)
            {
                Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks ({expectedStacks}) differ from current ({_currentStacks}). Updating.");
                _currentStacks = expectedStacks; // Update the instance counter

                // Now manage the buff based on the new _currentStacks value
                BattleUnitBuf_CrystalizedPower existingBuf = this.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_CrystalizedPower) as BattleUnitBuf_CrystalizedPower;

                if (_currentStacks > 0) // If stacks should be > 0, add or update the buff
                {
                    if (existingBuf != null && !existingBuf.IsDestroyed())
                    {
                        Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Updating existing CrystalizedPower buff stack to {_currentStacks}.");
                        existingBuf.stack = _currentStacks;
                        // Potentially call OnUpdateBuf or similar if needed, though changing stack usually suffices.
                    }
                    else
                    {
                        Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Adding new CrystalizedPower buff with stack {_currentStacks}.");
                        BattleUnitBuf_CrystalizedPower newBuf = new BattleUnitBuf_CrystalizedPower { stack = _currentStacks };
                        this.owner.bufListDetail.AddBuf(newBuf);
                        // AddBuf should call the buff's Init and OnAddBuf
                    }
                }
                else // If expectedStacks is 0, remove the buff if it exists
                {
                     if (existingBuf != null && !existingBuf.IsDestroyed())
                     {
                         Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks are 0. Removing existing CrystalizedPower buff.");
                         existingBuf.Destroy(); // Remove the buff
                     }
                     else 
                     {
                         // No buff to remove, already correct state.
                         Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks are 0. No buff present or already destroyed.");
                     }
                }
            } 
            else // Moved the 'else' block outside the 'if (expectedStacks != _currentStacks)' block
            {
                 Debug.Log($"[MyDLL] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks ({expectedStacks}) match current ({_currentStacks}). No change needed.");
            }
        } // End of NotifySceneChanged method
        
        // REMOVED OnWaveStart override
    }

    // 不会忘记的那个梦想 (ID: 9000005)
    public class PassiveAbility_9000005 : PassiveAbilityBase
    {
        private const int FLOW_COST_PER_EFFECT = 5;
        private int _internalFlowCounter = 0; 
        private const string MOD_ID = "SteriaBuilding"; // 改回正确的 ModId


        public void OnFlowConsumed(int amountConsumed)
        {
            if (amountConsumed <= 0) return;
            _internalFlowCounter += amountConsumed;
            int cardsToAdd = _internalFlowCounter / FLOW_COST_PER_EFFECT;
            if (cardsToAdd > 0)
            {
                // 使用正确的 ModId 构造 LorId
                LorId preciousMemoryId = new LorId(MOD_ID, 9001006);
                for (int i = 0; i < cardsToAdd; i++)
                {
                    Debug.Log($"[MyDLL] Passive 9000005 (OnFlowConsumed): Attempting to add card with ID: {preciousMemoryId}");
                    this.owner.allyCardDetail.AddNewCard(preciousMemoryId);
                }
                _internalFlowCounter %= FLOW_COST_PER_EFFECT;
            }
        }
    }
} 