using System;
using LOR_DiceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Sound;
using Steria; // For SteriaLogger and BattleUnitBuf_Flow

// 这个文件包含 安希尔 的相关能力实现
// --- 安希尔 被动 ---
// 注意：PassiveAbility 类必须在全局命名空间中，游戏才能正确加载它们

// 神脉：职业等级（安希尔）(ID: 9000001)
public class PassiveAbility_9000001 : PassiveAbilityBase
{
    // +15 Max HP
    public override int GetMaxHpBonus() => 15;

    // +15 Max Break Resist
    public override int GetMaxBpBonus() => 15;

    // +1 速度 (通过 Patch 修改骰子面数实现)
    public override void OnWaveStart()
    {
        base.OnWaveStart();
        if (this.owner != null && !this.owner.IsDead())
        {
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
    private int _lightSpentSinceLastFlow = 0;

    public override void OnRoundStart()
    {
        if (_addFlowNextRound)
        {
             AddFlowStacks(2);
             _addFlowNextRound = false;
        }
        _attackedOneSideThisTurn = false;
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        Debug.Log($"[Steria] Passive 9000002 OnTakeDamageByAttack: Owner={this.owner.UnitData.unitData.name}, Attacker={atkDice?.owner?.UnitData?.unitData?.name ?? "null"}, Dmg={dmg}");

        if (_addFlowNextRound || _attackedOneSideThisTurn || atkDice?.owner == this.owner || atkDice?.card == null)
        {
             Debug.Log("[Steria] Passive 9000002: Skipping (Already triggered this turn, self attack, or no card).");
             return;
        }
        if (atkDice.card.target != this.owner)
        {
             Debug.Log("[Steria] Passive 9000002: Skipping (Attack target is not self).");
             return;
        }

        bool isOneSided = true;
        int opponentSlotOrder = atkDice.card.slotOrder;
        Debug.Log($"[Steria] Passive 9000002: Checking for clash. Attacker Slot Order: {opponentSlotOrder}");
        foreach (BattlePlayingCardDataInUnitModel playingCard in this.owner.cardSlotDetail.cardAry)
        {
            if (playingCard != null && playingCard.target == atkDice.owner && playingCard.slotOrder == opponentSlotOrder)
            {
                 Debug.Log($"[Steria] Passive 9000002: Clash detected with card {playingCard.card.GetName()} at slot {playingCard.slotOrder}. Not one-sided.");
                 isOneSided = false;
                 break;
            }
        }

        if (isOneSided)
        {
             Debug.Log("[Steria] Passive 9000002: One-sided attack detected! Triggering effect.");
             _attackedOneSideThisTurn = true;
             _addFlowNextRound = true;
             int healAmount = (int)(dmg * 0.5f);
             if (healAmount > 0) { this.owner.RecoverHP(healAmount); }
        }
    }

    // This method will be called by the patch
    public void OnActualLightSpend(int amount)
    {
         if (amount <= 0) return;
         _lightSpentSinceLastFlow += amount;
         int flowToAdd = _lightSpentSinceLastFlow / 3;
         if (flowToAdd > 0)
         {
             AddFlowStacks(flowToAdd);
             _lightSpentSinceLastFlow %= 3;
         }
    }

    private void AddFlowStacks(int amount)
    {
        if (amount <= 0 || this.owner == null || this.owner.bufListDetail == null) return;
        BattleUnitBuf_Flow existingFlow = this.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
         if (existingFlow != null && !existingFlow.IsDestroyed())
         {
             Debug.Log($"[Steria] Adding {amount} stack(s) to existing Flow buff.");
             existingFlow.stack += amount;
             existingFlow.OnAddBuf(amount);
         }
         else
         {
             Debug.Log($"[Steria] Adding new Flow buff with {amount} stack(s).");
             BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow();
             newFlow.stack = amount;
             this.owner.bufListDetail.AddBuf(newFlow);
         }
    }
}

// 回忆燃烧 (ID: 9000003)
public class PassiveAbility_9000003 : PassiveAbilityBase
{
    private int _discardCountThisRound = 0;
    private bool _buffAppliedThisRound = false;
    private bool _shouldGainBuffNextRound = false;

     public override void OnRoundStart()
     {
         if (_shouldGainBuffNextRound)
         {
             this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, this.owner);
             this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Protection, 1, this.owner);
             _shouldGainBuffNextRound = false;
         }
         _discardCountThisRound = 0;
         _buffAppliedThisRound = false;
     }

     public void Notify_CardDiscarded()
     {
         if (_buffAppliedThisRound) return;
         _discardCountThisRound++;
         if (_discardCountThisRound >= 2)
         {
             _shouldGainBuffNextRound = true;
             _buffAppliedThisRound = true;
         }
     }
}

// --- Buff: 回忆结晶能量 ---
public class BattleUnitBuf_CrystalizedPower : BattleUnitBuf
{
    protected override string keywordId => "MemCrystalPassive";
    protected override string keywordIconId => "MemCrystalPassive";

    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnAddBuf(int addedStack)
    {
        Debug.Log($"[Steria] BattleUnitBuf_CrystalizedPower ({this._owner?.UnitData?.unitData?.name ?? "Unknown Owner"}): OnAddBuf called. addedStack = {addedStack}, current stack BEFORE adjustment = {this.stack}");
        this.stack += addedStack;
        if (this.stack > 3)
        {
             Debug.Log($"[Steria] BattleUnitBuf_CrystalizedPower: Stack exceeded 3 ({this.stack}), clamping to 3.");
             this.stack = 3;
        }
         Debug.Log($"[Steria] BattleUnitBuf_CrystalizedPower: OnAddBuf finished. Final stack = {this.stack}");
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (this.stack <= 0 || behavior == null)
            return;

        Debug.Log($"[Steria] BattleUnitBuf_CrystalizedPower ({this._owner.UnitData.unitData.name}): Applying +{this.stack} power to dice type {behavior.Detail} (Buff Stack: {this.stack}).");
        behavior.ApplyDiceStatBonus(new DiceStatBonus { power = this.stack });
    }
}

// 回忆结晶 (ID: 9000004)
public class PassiveAbility_9000004 : PassiveAbilityBase
{
    private static int _currentSceneCount = 0;
    private static bool _sceneCounterIncrementedThisRound = false;
    private static int _lastProcessedRound = -1;

    private int _currentStacks = 0;
    private const int SCENES_PER_STACK = 5;
    private const int MAX_STACKS = 3;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        Debug.Log($"[Steria] Passive 9000004 ({this.owner?.UnitData?.unitData?.name ?? "Unknown Owner"}): Initializing passive. Initial _currentStacks = {_currentStacks}");
    }

    public static void ResetSceneCounter()
    {
        _currentSceneCount = 0;
        _sceneCounterIncrementedThisRound = false;
        _lastProcessedRound = -1;
        Debug.Log("[Steria] Passive 9000004: Static counters reset.");
    }

    public static int GetCurrentSceneCount()
    {
         return _currentSceneCount;
    }

    public static int IncrementAndGetSceneCount()
    {
        _currentSceneCount++;
        Debug.Log($"[Steria] Passive 9000004 Static Counter: Incremented scene count to {_currentSceneCount}");
        return _currentSceneCount;
    }

    public static void SetRoundFlag(bool value)
    {
         _sceneCounterIncrementedThisRound = value;
    }

    public static void SetLastProcessedRound(int round)
    {
         _lastProcessedRound = round;
    }

    public static bool GetRoundFlag()
    {
        return _sceneCounterIncrementedThisRound;
    }

    public static int GetLastProcessedRound()
    {
         return _lastProcessedRound;
    }

    public void NotifySceneChanged(int currentGlobalScene)
    {
        Debug.Log($"[Steria] Passive 9000004 ({(this.owner?.UnitData?.unitData?.name ?? "Unknown Owner")}): NotifySceneChanged called for global scene {currentGlobalScene}. Current instance stacks: {_currentStacks}");

        if (this.owner == null || this.owner.IsDead())
        {
            Debug.LogWarning($"[Steria] Passive 9000004: Owner is null or dead. Skipping update.");
            return;
        }

        int expectedStacks = Mathf.Min(MAX_STACKS, currentGlobalScene / SCENES_PER_STACK);
        Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Calculated expected stacks: {expectedStacks} (based on global scene {currentGlobalScene}).");

        if (expectedStacks != _currentStacks)
        {
            Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks ({expectedStacks}) differ from current ({_currentStacks}). Updating.");
            _currentStacks = expectedStacks;

            BattleUnitBuf_CrystalizedPower existingBuf = this.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_CrystalizedPower) as BattleUnitBuf_CrystalizedPower;

            if (_currentStacks > 0)
            {
                if (existingBuf != null && !existingBuf.IsDestroyed())
                {
                    Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Updating existing CrystalizedPower buff stack to {_currentStacks}.");
                    existingBuf.stack = _currentStacks;
                }
                else
                {
                    Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Adding new CrystalizedPower buff with stack {_currentStacks}.");
                    BattleUnitBuf_CrystalizedPower newBuf = new BattleUnitBuf_CrystalizedPower { stack = _currentStacks };
                    this.owner.bufListDetail.AddBuf(newBuf);
                }
            }
            else
            {
                 if (existingBuf != null && !existingBuf.IsDestroyed())
                 {
                     Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks are 0. Removing existing CrystalizedPower buff.");
                     existingBuf.Destroy();
                 }
            }
        }
        else
        {
             Debug.Log($"[Steria] Passive 9000004 ({this.owner.UnitData.unitData.name}): Expected stacks ({expectedStacks}) match current ({_currentStacks}). No change needed.");
        }
    }
}

// 不会忘记的那个梦想 (ID: 9000005)
public class PassiveAbility_9000005 : PassiveAbilityBase
{
    private const int FLOW_COST_PER_EFFECT = 3;
    private int _internalFlowCounter = 0;
    private const string MOD_ID = "SteriaBuilding";

    public void OnFlowConsumed(int amountConsumed)
    {
        if (amountConsumed <= 0) return;

        _internalFlowCounter += amountConsumed;
        Debug.Log($"[Steria] Passive 9000005: Flow consumed {amountConsumed}, counter: {_internalFlowCounter}/{FLOW_COST_PER_EFFECT}");

        int cardsToAdd = _internalFlowCounter / FLOW_COST_PER_EFFECT;
        if (cardsToAdd > 0)
        {
            LorId preciousMemoryId = new LorId(MOD_ID, 9001006);
            for (int i = 0; i < cardsToAdd; i++)
            {
                Debug.Log($"[Steria] Passive 9000005: Adding Precious Memory to hand");
                this.owner.allyCardDetail.AddNewCard(preciousMemoryId);
            }
            _internalFlowCounter %= FLOW_COST_PER_EFFECT;
        }
    }
}
