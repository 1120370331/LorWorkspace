using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;

// 薇莉亚的被动技能和Buff实现
// 这些类需要在全局命名空间中，以便游戏能够找到

/// <summary>
/// 潮Buff - 薇莉亚的核心机制
/// 每次使用书页为敌人施加负面效果/为友方施加正面效果时：
/// 消耗1层潮，并使施加的效果层数+1
/// </summary>
public class BattleUnitBuf_Tide : BattleUnitBuf
{
    protected override string keywordId => "SteriaTide";
    protected override string keywordIconId => "SeaTides"; // 对应 Resource/ArtWork/SeaTides.png
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_Tide: Init for {owner?.UnitData?.unitData?.name} with {stack} stacks");
    }

    /// <summary>
    /// 消耗潮来增强效果
    /// </summary>
    /// <returns>消耗的潮层数（用于增强效果）</returns>
    public int ConsumeTideForBonus()
    {
        if (stack <= 0) return 0;

        stack--;
        SteriaLogger.Log($"Tide: Consumed 1 stack, remaining: {stack}");

        if (stack <= 0)
        {
            this.Destroy();
        }

        return 1; // 返回消耗的层数
    }

    /// <summary>
    /// 获取当前潮层数
    /// </summary>
    public int GetTideStacks()
    {
        return stack;
    }
}

/// <summary>
/// 神脉：梦之汐-司潮-潜力观测 (ID: 9004001)
/// 效果：
/// - 每回合开始时：将1张0费装备书页"潜力观测"置入ego装备区
/// - 消耗3点光芒后：下回合获得1层"潮"
/// - 作为敌方单位登场时，效果改为每回合自动为一个随机友方角色释放此效果
/// </summary>
public class PassiveAbility_9004001 : PassiveAbilityBase
{
    private static readonly string MOD_ID = "SteriaBuilding";
    private static readonly int CARD_POTENTIAL_OBSERVATION = 9004005; // 潜力观测卡牌ID

    private int _lightSpentAccumulator = 0;
    private int _tideToGainNextRound = 0;
    private bool _usedPotentialThisRound = false;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _lightSpentAccumulator = 0;
        _tideToGainNextRound = 0;
        _usedPotentialThisRound = false;
        SteriaLogger.Log($"PassiveAbility_9004001 (神脉：梦之汐-司潮-潜力观测): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _lightSpentAccumulator = 0;
        _tideToGainNextRound = 0;
        _usedPotentialThisRound = false;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _usedPotentialThisRound = false;

        // 获得累积的潮
        if (_tideToGainNextRound > 0 && owner != null)
        {
            AddTideStacks(owner, _tideToGainNextRound);
            SteriaLogger.Log($"神脉-潜力观测: {owner.UnitData?.unitData?.name} gained {_tideToGainNextRound} Tide");
            _tideToGainNextRound = 0;
        }

        // 重置光芒累积器
        _lightSpentAccumulator = 0;

        if (owner == null) return;

        // 判断是敌方还是友方
        if (owner.faction == Faction.Enemy)
        {
            // 敌方单位：自动为随机友方角色释放潜力观测效果
            AutoApplyPotentialObservation();
        }
        else
        {
            // 友方单位：将潜力观测卡牌置入EGO装备区
            AddPotentialObservationCard();
        }
    }

    /// <summary>
    /// 将潜力观测卡牌置入EGO装备区
    /// </summary>
    private void AddPotentialObservationCard()
    {
        try
        {
            LorId lorId = new LorId(MOD_ID, CARD_POTENTIAL_OBSERVATION);
            BattleDiceCardModel card = owner.allyCardDetail.AddNewCardToDeck(lorId);
            if (card != null)
            {
                card.SetCostToZero(true);
                // 设置为EGO卡牌
                card.SetPriorityAdder(50);
                SteriaLogger.Log($"神脉-潜力观测: Added Potential Observation card to {owner.UnitData?.unitData?.name}");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"神脉-潜力观测: Failed to add card: {ex.Message}");
        }
    }

    /// <summary>
    /// 敌方单位自动释放潜力观测效果
    /// </summary>
    private void AutoApplyPotentialObservation()
    {
        if (BattleObjectManager.instance == null) return;

        // 获取存活的友方单位（对于敌方来说是Enemy阵营）
        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => !u.IsDead() && !u.IsBreakLifeZero())
            .ToList();

        if (allies.Count == 0) return;

        // 随机选择一个友方单位
        BattleUnitModel target = allies[UnityEngine.Random.Range(0, allies.Count)];

        // 应用潜力观测效果
        ApplyPotentialObservationEffect(target);
    }

    /// <summary>
    /// 应用潜力观测效果到目标
    /// </summary>
    public void ApplyPotentialObservationEffect(BattleUnitModel target)
    {
        if (target == null || _usedPotentialThisRound) return;

        _usedPotentialThisRound = true;

        // 获取潮层数用于加成
        int tideBonus = 0;
        BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tideBuf != null && tideBuf.stack > 0)
        {
            tideBonus = tideBuf.ConsumeTideForBonus();
        }

        int totalBonus = 1 + tideBonus;

        // 查找目标当前最高的正面效果
        var positiveBufs = target.bufListDetail.GetActivatedBufList()
            .Where(b => b.positiveType == BufPositiveType.Positive && b.stack > 0)
            .ToList();

        if (positiveBufs.Count == 0)
        {
            // 没有正面效果，给予强壮
            target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, totalBonus, owner);
            SteriaLogger.Log($"潜力观测: Granted {totalBonus} Strength to {target.UnitData?.unitData?.name} (no existing positive buffs)");
        }
        else
        {
            // 找到层数最高的正面效果
            int maxStack = positiveBufs.Max(b => b.stack);
            var maxStackBufs = positiveBufs.Where(b => b.stack == maxStack).ToList();

            // 如果有多个相同层数的，随机选一个
            var selectedBuf = maxStackBufs[UnityEngine.Random.Range(0, maxStackBufs.Count)];

            // 增加该效果的层数
            selectedBuf.stack += totalBonus;
            SteriaLogger.Log($"潜力观测: Increased {selectedBuf.GetType().Name} by {totalBonus} for {target.UnitData?.unitData?.name}");
        }
    }

    /// <summary>
    /// 当光芒被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnLightSpent(int amount)
    {
        if (owner == null || amount <= 0) return;

        _lightSpentAccumulator += amount;
        SteriaLogger.Log($"神脉-潜力观测: {owner.UnitData?.unitData?.name} spent {amount} light, accumulator: {_lightSpentAccumulator}");

        // 每消耗3点光芒，下回合获得1层潮
        while (_lightSpentAccumulator >= 3)
        {
            _lightSpentAccumulator -= 3;
            _tideToGainNextRound++;
            SteriaLogger.Log($"神脉-潜力观测: Will gain 1 Tide next round (total pending: {_tideToGainNextRound})");
        }
    }

    /// <summary>
    /// 标记本回合已使用潜力观测
    /// </summary>
    public void MarkPotentialUsed()
    {
        _usedPotentialThisRound = true;
    }

    /// <summary>
    /// 检查本回合是否已使用潜力观测
    /// </summary>
    public bool HasUsedPotentialThisRound()
    {
        return _usedPotentialThisRound;
    }

    /// <summary>
    /// 辅助方法：添加潮层数
    /// </summary>
    public static void AddTideStacks(BattleUnitModel target, int amount)
    {
        if (target == null || amount <= 0) return;

        BattleUnitBuf_Tide existingTide = target.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;

        if (existingTide != null)
        {
            existingTide.stack += amount;
            SteriaLogger.Log($"AddTideStacks: Updated existing Tide. New stack: {existingTide.stack}");
        }
        else
        {
            BattleUnitBuf_Tide newTide = new BattleUnitBuf_Tide();
            newTide.stack = amount;
            target.bufListDetail.AddBuf(newTide);
            SteriaLogger.Log($"AddTideStacks: Created new Tide buff with {amount} stacks");
        }
    }
}

/// <summary>
/// 斯拉泽雅的挂坠 (ID: 9004002)
/// 效果：
/// - 受到的伤害/混乱伤害-5
/// - 第一次陷入混乱后：本场战斗中被动失效
/// </summary>
public class PassiveAbility_9004002 : PassiveAbilityBase
{
    private bool _isDisabled = false;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _isDisabled = false;
        SteriaLogger.Log($"PassiveAbility_9004002 (斯拉泽雅的挂坠): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _isDisabled = false;
    }

    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        if (_isDisabled) return 0;
        return 5;
    }

    public override int GetBreakDamageReduction(BattleDiceBehavior behavior)
    {
        if (_isDisabled) return 0;
        return 5;
    }

    public override void OnBreakState()
    {
        base.OnBreakState();
        if (!_isDisabled)
        {
            _isDisabled = true;
            SteriaLogger.Log($"斯拉泽雅的挂坠: Disabled for {owner?.UnitData?.unitData?.name} after entering break state");
        }
    }
}

/// <summary>
/// 为了大家…… (ID: 9004003)
/// 效果：舞台开始时，获得友方单位数量层"潮"
/// </summary>
public class PassiveAbility_9004003 : PassiveAbilityBase
{
    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        SteriaLogger.Log($"PassiveAbility_9004003 (为了大家……): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();

        if (owner == null || BattleObjectManager.instance == null) return;

        // 获取友方单位数量
        int allyCount = BattleObjectManager.instance.GetAliveList(owner.faction).Count;

        if (allyCount > 0)
        {
            PassiveAbility_9004001.AddTideStacks(owner, allyCount);
            SteriaLogger.Log($"为了大家……: {owner.UnitData?.unitData?.name} gained {allyCount} Tide (ally count)");
        }
    }
}

/// <summary>
/// 呃……先溜了…… (ID: 9004004)
/// 效果：若自身体力值低于1，则不会死亡而是免疫所有伤害并离开战局
/// </summary>
public class PassiveAbility_9004004 : PassiveAbilityBase
{
    private bool _hasEscaped = false;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _hasEscaped = false;
        SteriaLogger.Log($"PassiveAbility_9004004 (呃……先溜了……): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _hasEscaped = false;
    }

    public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
    {
        if (_hasEscaped || owner == null) return false;

        // 检查是否会导致死亡
        if (owner.hp - dmg < 1 && !owner.IsDead())
        {
            // 触发撤离
            TriggerEscape();
            return true; // 阻止伤害
        }
        return false;
    }

    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        if (_hasEscaped)
        {
            // 已撤离，免疫所有伤害
            return 9999;
        }
        return 0;
    }

    public override int GetBreakDamageReduction(BattleDiceBehavior behavior)
    {
        if (_hasEscaped)
        {
            return 9999;
        }
        return 0;
    }

    /// <summary>
    /// 触发撤离效果
    /// </summary>
    private void TriggerEscape()
    {
        if (_hasEscaped || owner == null) return;

        _hasEscaped = true;
        SteriaLogger.Log($"呃……先溜了……: {owner.UnitData?.unitData?.name} is escaping!");

        // 使用游戏内置的撤离机制（类似六协会南部一科敌人）
        // 设置 forceRetreat 为 true，然后调用 Die()，死亡动画会变成撤离动画
        owner.forceRetreat = true;
        owner.Die();
        SteriaLogger.Log($"呃……先溜了……: {owner.UnitData?.unitData?.name} has retreated!");
    }

    /// <summary>
    /// 在受到致命伤害前检查
    /// </summary>
    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);

        if (_hasEscaped || owner == null) return;

        // 如果血量即将低于1
        if (owner.hp < 1 && !owner.IsDead())
        {
            TriggerEscape();
        }
    }
}

/// <summary>
/// 辅助Buff：下回合获得潮
/// </summary>
public class BattleUnitBuf_TideNextTurn : BattleUnitBuf
{
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        if (_owner != null && !_owner.IsDead())
        {
            PassiveAbility_9004001.AddTideStacks(_owner, this.stack);
            SteriaLogger.Log($"TideNextTurn: Granted {this.stack} Tide to {_owner.UnitData?.unitData?.name}");
        }
        this.Destroy();
    }
}
