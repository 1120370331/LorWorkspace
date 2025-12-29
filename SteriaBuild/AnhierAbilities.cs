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
    private const string MOD_ID = "SteriaBuilding";
    private const int PRECIOUS_MEMORY_CARD_ID = 9001006; // 珍贵的回忆

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

    // AI限制：敌人不会主动使用"珍贵的回忆"
    public override BattleDiceCardModel OnSelectCardAuto(BattleDiceCardModel origin, int currentDiceSlotIdx)
    {
        if (origin == null)
            return origin;

        // 检查是否是"珍贵的回忆"卡牌
        LorId cardId = origin.GetID();
        if (cardId.IsBasic() == false && cardId.packageId == MOD_ID && cardId.id == PRECIOUS_MEMORY_CARD_ID)
        {
            SteriaLogger.Log($"Passive 9000001: AI tried to select 珍贵的回忆, finding alternative card");

            // 从手牌中选择另一张卡
            List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
            foreach (BattleDiceCardModel card in hand)
            {
                if (card != origin && card.GetID().id != PRECIOUS_MEMORY_CARD_ID)
                {
                    SteriaLogger.Log($"Passive 9000001: Selected alternative card: {card.GetName()}");
                    return card;
                }
            }

            // 如果没有其他卡可选，返回null（不出牌）
            SteriaLogger.Log($"Passive 9000001: No alternative card found, returning null");
            return null;
        }

        return base.OnSelectCardAuto(origin, currentDiceSlotIdx);
    }
}

// 神脉：汐与梦（司流-自我之流）(ID: 9000002)
public class PassiveAbility_9000002 : PassiveAbilityBase
{
    private const string MOD_ID = "SteriaBuilding";
    private const int SELF_FLOW_EGO_CARD_ID = 9001011; // 自我之流 EGO卡牌ID
    private const int ENEMY_TRIGGER_INTERVAL = 4; // 敌方触发间隔（幕）
    private const int HP_COST = 12; // 生命值消耗
    private const int FLOW_GAIN = 5; // 获得的流层数

    private bool _attackedOneSideThisTurn = false;
    private bool _addFlowNextRound = false;
    private int _lightSpentSinceLastFlow = 0;
    private bool _egoCardAdded = false; // 是否已添加EGO卡牌
    private int _enemyTriggerCounter = 0; // 敌方触发计数器

    // 开幕时添加EGO卡牌（玩家）或初始化敌方计数器
    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _egoCardAdded = false;
        _enemyTriggerCounter = 0;

        // 如果是玩家单位，添加EGO卡牌到EGO槽位
        if (this.owner != null && this.owner.faction == Faction.Player)
        {
            LorId egoCardId = new LorId(MOD_ID, SELF_FLOW_EGO_CARD_ID);
            this.owner.personalEgoDetail.AddCard(egoCardId);
            _egoCardAdded = true;
            SteriaLogger.Log($"Passive 9000002: Added 自我之流 EGO card to {this.owner.UnitData?.unitData?.name}");
        }
    }

    public override void OnRoundStart()
    {
        if (_addFlowNextRound)
        {
             AddFlowStacks(2); // 削弱：3→2
             _addFlowNextRound = false;
        }
        _attackedOneSideThisTurn = false;

        // 敌方单位：每4幕自动触发一次效果
        if (this.owner != null && this.owner.faction == Faction.Enemy && !this.owner.IsDead())
        {
            _enemyTriggerCounter++;
            SteriaLogger.Log($"Passive 9000002: Enemy trigger counter = {_enemyTriggerCounter}/{ENEMY_TRIGGER_INTERVAL}");

            // 第1幕触发，之后每4幕触发一次（1, 5, 9, 13...）
            if (_enemyTriggerCounter == 1 || (_enemyTriggerCounter - 1) % ENEMY_TRIGGER_INTERVAL == 0)
            {
                TriggerSelfFlowEffect();
            }
        }
    }

    // 触发自我之流效果：扣除25点生命值，获得10层流
    private void TriggerSelfFlowEffect()
    {
        if (this.owner == null || this.owner.IsDead()) return;

        SteriaLogger.Log($"Passive 9000002: Triggering 自我之流 effect for {this.owner.UnitData?.unitData?.name}");

        // 扣除生命值
        this.owner.LoseHp(HP_COST);
        SteriaLogger.Log($"Passive 9000002: Lost {HP_COST} HP, current HP: {this.owner.hp}");

        // 获得流层数
        AddFlowStacks(FLOW_GAIN);
        SteriaLogger.Log($"Passive 9000002: Added {FLOW_GAIN} Flow stacks");
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
// 每丢弃4张书页：下回合获得1层"强壮、守护"，并失去5点混乱抗性
public class PassiveAbility_9000003 : PassiveAbilityBase
{
    private int _discardCountThisRound = 0;
    private int _triggerCountNextRound = 0; // 下回合需要触发的次数

     public override void OnRoundStart()
     {
         // 根据上回合触发次数，给予对应层数的强壮、守护，并扣除混乱抗性
         if (_triggerCountNextRound > 0)
         {
             this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, _triggerCountNextRound, this.owner);
             this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Protection, _triggerCountNextRound, this.owner);
             // 失去5点混乱抗性（每次触发）
             int breakResistLoss = 5 * _triggerCountNextRound;
             this.owner.breakDetail.TakeBreakDamage(breakResistLoss, DamageType.Passive, this.owner, AtkResist.Normal);
             SteriaLogger.Log($"回忆燃烧: 获得{_triggerCountNextRound}层强壮和守护，失去{breakResistLoss}点混乱抗性");
             _triggerCountNextRound = 0;
         }
         _discardCountThisRound = 0;
     }

     public void Notify_CardDiscarded()
     {
         _discardCountThisRound++;
         // 每丢弃4张书页触发一次
         if (_discardCountThisRound >= 4)
         {
             _triggerCountNextRound++;
             _discardCountThisRound -= 4; // 重置计数，允许继续累积
             SteriaLogger.Log($"回忆燃烧: 本回合已丢弃4张书页，下回合将触发效果（当前累计{_triggerCountNextRound}次）");
         }
     }
}

// --- Buff: 回忆结晶能量 ---
// 仅用于显示图标和层数，实际的骰子威力加成在被动的 BeforeRollDice 中处理
public class BattleUnitBuf_CrystalizedPower : BattleUnitBuf
{
    protected override string keywordId => "MemCrystalPassive";
    protected override string keywordIconId => "MemCrystalPassive";

    public override BufPositiveType positiveType => BufPositiveType.Positive;
}

// 回忆结晶 (ID: 9000004)
// 舞台每经过5幕，便使自身骰子威力+1（至多+3）
// 重写：使用实例变量，每个单位独立计数，新战斗自动重置
public class PassiveAbility_9000004 : PassiveAbilityBase
{
    // 使用实例变量而不是静态变量，每个单位独立计数
    private int _roundCount = 0;          // 当前单位经历的幕数
    private int _lastProcessedRound = -1; // 上次处理的回合号（防止同一回合重复计数）
    private int _currentStacks = 0;       // 当前威力加成层数

    private const int ROUNDS_PER_STACK = 5;
    private const int MAX_STACKS = 3;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        // Init 在每场新战斗开始时都会被调用，自动重置所有计数
        _roundCount = 0;
        _lastProcessedRound = -1;
        _currentStacks = 0;
        SteriaLogger.Log($"回忆结晶 ({self?.UnitData?.unitData?.name}): Init - 所有计数已重置");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        // 新 wave 开始时不重置计数（同一场战斗的不同 wave 之间保持计数）
        // 但需要更新 buff 显示
        SteriaLogger.Log($"回忆结晶 ({this.owner?.UnitData?.unitData?.name}): OnWaveStart - roundCount={_roundCount}, stacks={_currentStacks}");
        UpdateCrystalBuff();
    }

    public override void OnRoundStart()
    {
        int currentRound = Singleton<StageController>.Instance?.RoundTurn ?? 0;

        // 防止同一回合重复计数
        if (currentRound > _lastProcessedRound)
        {
            _lastProcessedRound = currentRound;
            _roundCount++;
            SteriaLogger.Log($"回忆结晶 ({this.owner?.UnitData?.unitData?.name}): 新回合 {currentRound}, 累计幕数={_roundCount}");
        }

        UpdateCrystalBuff();
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (_currentStacks <= 0 || behavior == null)
            return;

        SteriaLogger.Log($"回忆结晶 ({this.owner?.UnitData?.unitData?.name}): 骰子威力+{_currentStacks}");
        behavior.ApplyDiceStatBonus(new DiceStatBonus { power = _currentStacks });
    }

    private void UpdateCrystalBuff()
    {
        if (this.owner == null || this.owner.IsDead())
            return;

        // 计算期望的层数：每5幕+1层，最多3层
        int expectedStacks = Mathf.Min(MAX_STACKS, _roundCount / ROUNDS_PER_STACK);

        if (expectedStacks != _currentStacks)
        {
            SteriaLogger.Log($"回忆结晶 ({this.owner.UnitData.unitData.name}): 层数变化 {_currentStacks} -> {expectedStacks} (幕数={_roundCount})");
            _currentStacks = expectedStacks;

            // 更新或创建 buff
            BattleUnitBuf_CrystalizedPower existingBuf = this.owner.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_CrystalizedPower) as BattleUnitBuf_CrystalizedPower;

            if (_currentStacks > 0)
            {
                if (existingBuf != null && !existingBuf.IsDestroyed())
                {
                    existingBuf.stack = _currentStacks;
                }
                else
                {
                    BattleUnitBuf_CrystalizedPower newBuf = new BattleUnitBuf_CrystalizedPower { stack = _currentStacks };
                    this.owner.bufListDetail.AddBuf(newBuf);
                }
            }
            else if (existingBuf != null && !existingBuf.IsDestroyed())
            {
                existingBuf.Destroy();
            }
        }
    }
}

// 不会忘记的那个梦想 (ID: 9000005)
// 每消耗5层流，下回合开始时将1张珍贵的回忆置入手牌
// 弃置5张珍贵的回忆后，停止触发此被动，并将"忘却之梦"置入EGO装备区
public class PassiveAbility_9000005 : PassiveAbilityBase
{
    private const int FLOW_COST_PER_EFFECT = 5;
    private const int PRECIOUS_MEMORY_DISCARD_THRESHOLD = 5;
    private const int PRECIOUS_MEMORY_CARD_ID = 9001006;
    private const int FORGOTTEN_DREAM_CARD_ID = 9001012; // 忘却之梦
    private const string MOD_ID = "SteriaBuilding";

    private int _internalFlowCounter = 0;
    private int _cardsToAddNextRound = 0;
    private int _preciousMemoryDiscardCount = 0; // 弃置珍贵回忆计数
    private bool _passiveDisabled = false; // 被动是否已停止触发
    private bool _forgottenDreamAdded = false; // 是否已添加忘却之梦

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _internalFlowCounter = 0;
        _cardsToAddNextRound = 0;
        _preciousMemoryDiscardCount = 0;
        _passiveDisabled = false;
        _forgottenDreamAdded = false;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        // 被动已停止则不添加珍贵的回忆
        if (_passiveDisabled) return;

        // 下回合开始时添加珍贵的回忆
        if (_cardsToAddNextRound > 0 && this.owner != null && !this.owner.IsDead())
        {
            LorId preciousMemoryId = new LorId(MOD_ID, PRECIOUS_MEMORY_CARD_ID);
            for (int i = 0; i < _cardsToAddNextRound; i++)
            {
                SteriaLogger.Log($"不会忘记的那个梦想: 将珍贵的回忆置入手牌 ({i + 1}/{_cardsToAddNextRound})");
                this.owner.allyCardDetail.AddNewCard(preciousMemoryId);
            }
            _cardsToAddNextRound = 0;
        }
    }

    public void OnFlowConsumed(int amountConsumed)
    {
        if (amountConsumed <= 0 || _passiveDisabled) return;

        // 记录总消耗的流（用于忘却之梦追伤计算）
        _totalFlowConsumedThisStage += amountConsumed;

        _internalFlowCounter += amountConsumed;
        SteriaLogger.Log($"不会忘记的那个梦想: 消耗了{amountConsumed}层流，累计{_internalFlowCounter}/{FLOW_COST_PER_EFFECT}，总消耗{_totalFlowConsumedThisStage}");

        int cardsToAdd = _internalFlowCounter / FLOW_COST_PER_EFFECT;
        if (cardsToAdd > 0)
        {
            _cardsToAddNextRound += cardsToAdd;
            _internalFlowCounter %= FLOW_COST_PER_EFFECT;
            SteriaLogger.Log($"不会忘记的那个梦想: 下回合将获得{cardsToAdd}张珍贵的回忆（累计{_cardsToAddNextRound}张）");
        }
    }

    /// <summary>
    /// 当珍贵的回忆被弃置时调用
    /// </summary>
    public void OnPreciousMemoryDiscarded()
    {
        if (_passiveDisabled) return;

        _preciousMemoryDiscardCount++;
        SteriaLogger.Log($"不会忘记的那个梦想: 珍贵的回忆被弃置，计数 {_preciousMemoryDiscardCount}/{PRECIOUS_MEMORY_DISCARD_THRESHOLD}");

        if (_preciousMemoryDiscardCount >= PRECIOUS_MEMORY_DISCARD_THRESHOLD)
        {
            _passiveDisabled = true;
            SteriaLogger.Log("不会忘记的那个梦想: 被动已停止触发");
            AddForgottenDreamToEgo();
        }
    }

    private void AddForgottenDreamToEgo()
    {
        if (_forgottenDreamAdded || this.owner == null || this.owner.IsDead()) return;

        _forgottenDreamAdded = true;
        LorId forgottenDreamId = new LorId(MOD_ID, FORGOTTEN_DREAM_CARD_ID);

        // 调试：检查卡牌是否存在
        var cardItem = ItemXmlDataList.instance.GetCardItem(forgottenDreamId, true);
        if (cardItem == null)
        {
            SteriaLogger.Log($"不会忘记的那个梦想: 错误 - 找不到卡牌 {forgottenDreamId}");
            return;
        }
        SteriaLogger.Log($"不会忘记的那个梦想: 找到卡牌 {cardItem.Name}, IsPersonal={cardItem.IsPersonal()}, IsEgo={cardItem.IsEgo()}");

        // 敌人单位：直接置入手牌
        if (this.owner.faction == Faction.Enemy)
        {
            this.owner.allyCardDetail.AddNewCard(forgottenDreamId);
            SteriaLogger.Log("不会忘记的那个梦想: 敌人单位 - 将忘却之梦直接置入手牌");
        }
        else
        {
            // 玩家单位：置入EGO装备区
            // 使用 true 参数确保能找到mod卡牌
            this.owner.personalEgoDetail.AddCard(forgottenDreamId);
            SteriaLogger.Log($"不会忘记的那个梦想: 将忘却之梦置入EGO装备区, 当前EGO数量={this.owner.personalEgoDetail.GetCardAll().Count}");
        }
    }

    /// <summary>
    /// 获取本舞台已消耗的流总数（用于忘却之梦的伤害计算）
    /// </summary>
    public static int GetTotalFlowConsumedThisStage(BattleUnitModel unit)
    {
        // 从被动中获取累计消耗的流
        var passive = unit?.passiveDetail?.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9000005) as PassiveAbility_9000005;
        return passive?._totalFlowConsumedThisStage ?? 0;
    }

    private int _totalFlowConsumedThisStage = 0;

    public void RecordFlowConsumed(int amount)
    {
        _totalFlowConsumedThisStage += amount;
    }
}
