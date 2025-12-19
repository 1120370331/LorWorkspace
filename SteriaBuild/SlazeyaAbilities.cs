using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;

// 斯拉泽雅和司流者教徒的被动技能实现
// 这些类需要在全局命名空间中，以便游戏能够找到

/// <summary>
/// 神脉：梦之汐-司流-倾覆之大流 (ID: 9002001)
/// 效果：
/// - 消耗流提升的骰子威力变为2（每消耗1层流，骰子获得+2威力）
/// - 每造成10点伤害，下回合开始时获得1层流
/// - 每消耗10层流，下回合获得1层强壮
/// </summary>
public class PassiveAbility_9002001 : PassiveAbilityBase
{
    private int _damageAccumulator = 0;
    private int _flowToGainNextRound = 0;
    private int _flowConsumedAccumulator = 0;
    private int _strengthToGainNextRound = 0;

    // 标记是否拥有此被动（用于Harmony补丁检查 - 流获取x2，已废弃）
    public static bool HasPassive(BattleUnitModel unit)
    {
        return unit?.passiveDetail?.PassiveList?.Any(p => p is PassiveAbility_9002001) == true;
    }

    // 检查是否拥有流威力加成x2效果
    public static bool HasFlowPowerBonus(BattleUnitModel unit)
    {
        return unit?.passiveDetail?.PassiveList?.Any(p => p is PassiveAbility_9002001) == true;
    }

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _damageAccumulator = 0;
        _flowToGainNextRound = 0;
        _flowConsumedAccumulator = 0;
        _strengthToGainNextRound = 0;
        SteriaLogger.Log($"PassiveAbility_9002001 (神脉：梦之汐): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _damageAccumulator = 0;
        _flowToGainNextRound = 0;
        _flowConsumedAccumulator = 0;
        _strengthToGainNextRound = 0;
        SteriaLogger.Log($"PassiveAbility_9002001 (神脉：梦之汐): OnWaveStart for {owner?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        // 在回合开始时获得累积的流
        if (_flowToGainNextRound > 0 && owner != null)
        {
            Steria.CardAbilityHelper.AddFlowStacks(owner, _flowToGainNextRound);
            SteriaLogger.Log($"神脉：梦之汐: {owner.UnitData?.unitData?.name} gained {_flowToGainNextRound} flow from damage dealt last round");
            _flowToGainNextRound = 0;
        }
        // 在回合开始时获得累积的强壮
        if (_strengthToGainNextRound > 0 && owner != null)
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, _strengthToGainNextRound, owner);
            SteriaLogger.Log($"神脉：梦之汐: {owner.UnitData?.unitData?.name} gained {_strengthToGainNextRound} Strength from flow consumed last round");
            _strengthToGainNextRound = 0;
        }
        // 重置累积器
        _damageAccumulator = 0;
        _flowConsumedAccumulator = 0;
    }

    /// <summary>
    /// 当造成伤害时调用（由Harmony补丁触发）
    /// </summary>
    public void OnDamageDealt(int damage)
    {
        if (owner == null || damage <= 0) return;

        _damageAccumulator += damage;
        SteriaLogger.Log($"神脉：梦之汐: {owner.UnitData?.unitData?.name} dealt {damage} damage, accumulator: {_damageAccumulator}");

        // 每造成10点伤害，下回合获得1层流
        while (_damageAccumulator >= 10)
        {
            _damageAccumulator -= 10;
            _flowToGainNextRound++;
            SteriaLogger.Log($"神脉：梦之汐: Will gain 1 flow next round (total pending: {_flowToGainNextRound})");
        }
    }

    /// <summary>
    /// 当流被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnFlowConsumed(int amount)
    {
        if (owner == null || amount <= 0) return;

        _flowConsumedAccumulator += amount;
        SteriaLogger.Log($"神脉：梦之汐: {owner.UnitData?.unitData?.name} consumed {amount} flow, accumulator: {_flowConsumedAccumulator}");

        // 每消耗10层流，下回合获得1层强壮
        while (_flowConsumedAccumulator >= 10)
        {
            _flowConsumedAccumulator -= 10;
            _strengthToGainNextRound++;
            SteriaLogger.Log($"神脉：梦之汐: Will gain 1 Strength next round (total pending: {_strengthToGainNextRound})");
        }
    }
}

/// <summary>
/// 梦想教皇 (ID: 9002002)
/// 每一幕开始时，随机使三名除自己之外的友方单位获得三种不同的"倾诉梦想"效果
/// </summary>
public class PassiveAbility_9002002 : PassiveAbilityBase
{
    private static readonly System.Random _random = new System.Random();

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (owner == null || BattleObjectManager.instance == null) return;

        // 获取除自己之外的存活友方单位（额外检查确保单位确实存活）
        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => u != owner && !u.IsDead() && !u.IsBreakLifeZero())
            .ToList();

        if (allies.Count == 0)
        {
            SteriaLogger.Log("PassiveAbility_9002002 (梦想教皇): No allies to grant dream buffs");
            return;
        }

        // 三种倾诉梦想效果
        List<Action<BattleUnitModel>> dreamEffects = new List<Action<BattleUnitModel>>
        {
            (unit) => {
                unit.bufListDetail.AddBuf(new BattleUnitBuf_DreamVision());
                SteriaLogger.Log($"梦想教皇: 赋予 {unit.UnitData?.unitData?.name} 倾诉梦想：远望");
            },
            (unit) => {
                unit.bufListDetail.AddBuf(new BattleUnitBuf_DreamIllusion());
                SteriaLogger.Log($"梦想教皇: 赋予 {unit.UnitData?.unitData?.name} 倾诉梦想：幻梦");
            },
            (unit) => {
                unit.bufListDetail.AddBuf(new BattleUnitBuf_DreamExecution());
                SteriaLogger.Log($"梦想教皇: 赋予 {unit.UnitData?.unitData?.name} 倾诉梦想：奉行");
            }
        };

        // 随机打乱效果顺序
        List<Action<BattleUnitModel>> shuffledEffects = dreamEffects.OrderBy(x => _random.Next()).ToList();

        // 随机打乱友方单位顺序
        List<BattleUnitModel> shuffledAllies = allies.OrderBy(x => _random.Next()).ToList();

        // 分配效果：最多3个效果给最多3个不同的友方单位
        int effectsToGrant = Math.Min(3, Math.Min(shuffledEffects.Count, shuffledAllies.Count));

        for (int i = 0; i < effectsToGrant; i++)
        {
            shuffledEffects[i](shuffledAllies[i]);
        }

        SteriaLogger.Log($"PassiveAbility_9002002 (梦想教皇): Granted {effectsToGrant} dream effects to allies");
    }
}

/// <summary>
/// 御风司流 (ID: 9002003)
/// 累计消耗15层流后，永久获得1层"守护"
/// 再消耗15层流后，永久获得1层"强壮"
/// </summary>
public class PassiveAbility_9002003 : PassiveAbilityBase
{
    private int _totalFlowConsumed = 0;
    private bool _protectionGranted = false;
    private bool _strengthGranted = false;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _totalFlowConsumed = 0;
        _protectionGranted = false;
        _strengthGranted = false;
        SteriaLogger.Log($"PassiveAbility_9002003 (御风司流): Init for {self?.UnitData?.unitData?.name}");
    }

    /// <summary>
    /// 当流被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnFlowConsumed(int amount)
    {
        if (owner == null || amount <= 0) return;

        _totalFlowConsumed += amount;
        SteriaLogger.Log($"御风司流: {owner.UnitData?.unitData?.name} consumed {amount} flow, total: {_totalFlowConsumed}");

        // 检查是否达到15层阈值（守护）
        if (!_protectionGranted && _totalFlowConsumed >= 15)
        {
            _protectionGranted = true;
            // 永久守护：使用AddKeywordBufThisRoundByEtc并设置为不会消失
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 1, owner);
            SteriaLogger.Log($"御风司流: 永久赋予 {owner.UnitData?.unitData?.name} 1层守护");
        }

        // 检查是否达到30层阈值（强壮）
        if (!_strengthGranted && _totalFlowConsumed >= 30)
        {
            _strengthGranted = true;
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, owner);
            SteriaLogger.Log($"御风司流: 永久赋予 {owner.UnitData?.unitData?.name} 1层强壮");
        }
    }

    // 获取当前消耗的流总量（用于调试）
    public int GetTotalFlowConsumed() => _totalFlowConsumed;
}

/// <summary>
/// 司流者 (ID: 9002004)
/// 每消耗3层流，从以下效果中随机获得一种：
/// 下回合获得一层"强壮"/"守护"/"忍耐"/"迅捷"/"坚韧"/"振奋"
/// </summary>
public class PassiveAbility_9002004 : PassiveAbilityBase
{
    private static readonly System.Random _random = new System.Random();
    private int _flowConsumedAccumulator = 0;

    // 可能获得的buff类型
    private static readonly KeywordBuf[] _possibleBuffs = new KeywordBuf[]
    {
        KeywordBuf.Strength,    // 强壮
        KeywordBuf.Protection,  // 守护
        KeywordBuf.Endurance,   // 忍耐
        KeywordBuf.Quickness,   // 迅捷
        KeywordBuf.DmgUp,       // 坚韧 (使用DmgUp作为替代，实际游戏中可能需要调整)
        KeywordBuf.Burn         // 振奋 (使用Burn作为占位符，需要确认正确的KeywordBuf)
    };

    // 实际使用的buff（排除不存在的）
    private static readonly KeywordBuf[] _actualBuffs = new KeywordBuf[]
    {
        KeywordBuf.Strength,    // 强壮
        KeywordBuf.Protection,  // 守护
        KeywordBuf.Endurance,   // 忍耐
        KeywordBuf.Quickness,   // 迅捷
    };

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _flowConsumedAccumulator = 0;
        SteriaLogger.Log($"PassiveAbility_9002004 (司流者): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        // 每幕开始时重置累计器（如果需要的话）
        // 根据设计，应该是累计消耗，不重置
    }

    /// <summary>
    /// 当流被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnFlowConsumed(int amount)
    {
        if (owner == null || amount <= 0) return;

        _flowConsumedAccumulator += amount;
        SteriaLogger.Log($"司流者: {owner.UnitData?.unitData?.name} consumed {amount} flow, accumulator: {_flowConsumedAccumulator}");

        // 每消耗3层流，随机获得一种buff
        while (_flowConsumedAccumulator >= 3)
        {
            _flowConsumedAccumulator -= 3;

            // 随机选择一种buff
            KeywordBuf selectedBuff = _actualBuffs[_random.Next(_actualBuffs.Length)];

            // 下回合获得buff（使用AddKeywordBufByEtc默认下回合生效）
            owner.bufListDetail.AddKeywordBufByEtc(selectedBuff, 1, owner);
            SteriaLogger.Log($"司流者: 下回合赋予 {owner.UnitData?.unitData?.name} 1层 {selectedBuff}");
        }
    }
}

/// <summary>
/// 变幻莫测 (ID: 9002006)
/// 每一幕将意图使用的书页置入手中且光芒消耗为0
/// 顺序：1,2,1,2,3,1,2,4 循环
/// </summary>
public class PassiveAbility_9002006 : PassiveAbilityBase
{
    private int _roundCount = 0;
    private static readonly string MOD_ID = "SteriaBuilding";

    // 卡牌ID
    private static readonly int CARD_OCEAN_COMMAND = 9002006;      // 洋流，听我的号令
    private static readonly int CARD_CHASE_DREAM = 9002001;        // 逐梦随流
    private static readonly int CARD_ALL_FLOW = 9002003;           // 万物之流
    private static readonly int CARD_ENDLESS_FLOW = 9002007;       // 随我流向无尽的尽头
    private static readonly int CARD_STORM_SPLIT = 9002004;        // 风暴分流
    private static readonly int CARD_ENDLESS_STREAM = 9002002;     // 川流不息
    private static readonly int CARD_MASS_ATTACK = 9002005;        // 倾覆万千之流
    private static readonly int CARD_HUNDRED_RIVERS = 9002008;     // 百川逐风

    // 每种模式的卡牌组合（基础版）
    // 1: 洋流，听我的号令 | 百川逐风 | 逐梦随流 | 万物之流
    // 2: 随我流向无尽的尽头 | 万物之流 | 风暴分流 | 逐梦随流
    // 3: 万物之流 | 风暴分流 | 百川逐风 | 川流不息
    // 4: 倾覆万千之流 | 逐梦随流 | 百川逐风 | 川流不息
    private static readonly int[][] _patterns = new int[][]
    {
        new int[] { CARD_OCEAN_COMMAND, CARD_HUNDRED_RIVERS, CARD_CHASE_DREAM, CARD_ALL_FLOW },        // 模式1
        new int[] { CARD_ENDLESS_FLOW, CARD_ALL_FLOW, CARD_STORM_SPLIT, CARD_CHASE_DREAM },            // 模式2
        new int[] { CARD_ALL_FLOW, CARD_STORM_SPLIT, CARD_HUNDRED_RIVERS, CARD_ENDLESS_STREAM },       // 模式3
        new int[] { CARD_MASS_ATTACK, CARD_CHASE_DREAM, CARD_HUNDRED_RIVERS, CARD_ENDLESS_STREAM },    // 模式4
    };

    // 情感等级3后的额外卡牌
    // 模式1: +百川逐风, +风暴分流
    // 模式2: +百川逐风, +逐梦随流
    // 模式3: +逐梦随流, +风暴分流
    // 模式4: +万物之流, +逐梦随流
    private static readonly int[][] _emotionBonusCards = new int[][]
    {
        new int[] { CARD_HUNDRED_RIVERS, CARD_STORM_SPLIT },    // 模式1额外卡牌
        new int[] { CARD_HUNDRED_RIVERS, CARD_CHASE_DREAM },    // 模式2额外卡牌
        new int[] { CARD_CHASE_DREAM, CARD_STORM_SPLIT },       // 模式3额外卡牌
        new int[] { CARD_ALL_FLOW, CARD_CHASE_DREAM },          // 模式4额外卡牌
    };

    // 循环顺序：1,2,1,2,3,1,2,4 (索引从0开始所以是0,1,0,1,2,0,1,3)
    private static readonly int[] _sequence = new int[] { 0, 1, 0, 1, 2, 0, 1, 3 };

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _roundCount = 0;
        SteriaLogger.Log($"PassiveAbility_9002006 (变幻莫测): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _roundCount = 0;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (owner == null) return;

        int patternIndex;

        // 第4幕(索引3)强制使用模式4
        if (_roundCount == 3)
        {
            patternIndex = 3; // 模式4
        }
        else if (_roundCount < 3)
        {
            // 前3幕按顺序：1,2,1
            patternIndex = _sequence[_roundCount];
        }
        else
        {
            // 第5幕开始，从循环的第4个位置继续（跳过已经用过的前4个）
            int adjustedIndex = (_roundCount - 4) % _sequence.Length;
            patternIndex = _sequence[adjustedIndex];
        }
        int[] cards = _patterns[patternIndex];

        // 检查情感等级是否达到3
        bool hasEmotionBonus = owner.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 3;
        int[] bonusCards = hasEmotionBonus && patternIndex < _emotionBonusCards.Length ? _emotionBonusCards[patternIndex] : new int[0];

        SteriaLogger.Log($"变幻莫测: Round {_roundCount + 1}, Pattern {patternIndex + 1}, Adding {cards.Length} cards, EmotionLevel={owner.emotionDetail?.EmotionLevel}, BonusCards={bonusCards.Length}");

        // 消耗所有卡牌（像原版变幻莫测一样）
        owner.allyCardDetail.ExhaustAllCards();
        SteriaLogger.Log($"变幻莫测: Exhausted all cards");

        // 将基础卡牌置入手牌（使用AddTempCard + 优先级）
        int priority = 100;
        foreach (int cardId in cards)
        {
            AddNewCard(cardId, priority);
            priority -= 10;
        }

        // 将情感等级3额外卡牌置入手牌
        if (bonusCards.Length > 0)
        {
            foreach (int cardId in bonusCards)
            {
                AddNewCard(cardId, priority);
                priority -= 10;
            }
        }

        _roundCount++;
    }

    /// <summary>
    /// 添加临时卡牌（像原版变幻莫测一样）
    /// </summary>
    private void AddNewCard(int cardId, int priorityAdder)
    {
        try
        {
            LorId lorId = new LorId(MOD_ID, cardId);
            BattleDiceCardModel card = owner.allyCardDetail.AddTempCard(lorId);
            if (card != null)
            {
                card.SetCostToZero(true);
                card.SetPriorityAdder(priorityAdder);
                card.temporary = true;
                SteriaLogger.Log($"变幻莫测: Added temp card {cardId} with cost 0, priority {priorityAdder}");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"变幻莫测: Failed to add card {cardId}: {ex.Message}");
        }
    }
}

/// <summary>
/// 速战速决4 (ID: 9002007)
/// 初始拥有4颗速度骰子，情感等级达到3后额外获得1颗
/// </summary>
public class PassiveAbility_9002007 : PassiveAbilityBase
{
    public override void OnWaveStart()
    {
        base.OnWaveStart();
        // 设置初始速度骰子数为4
        if (owner?.Book != null)
        {
            owner.Book.SetSpeedDiceNum(4);
            SteriaLogger.Log($"速战速决4: Set speed dice to 4 for {owner.UnitData?.unitData?.name}");
        }
    }

    public override int SpeedDiceNumAdder()
    {
        // 情感等级达到3后额外获得1颗
        if (owner?.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 3)
        {
            return 1;
        }
        return 0;
    }
}

/// <summary>
/// 斯拉泽雅司流者 (ID: 9002005)
/// - 每造成10点伤害：下回合获得1层流
/// - 每消耗10层流：下回合获得1层强壮
/// </summary>
public class PassiveAbility_9002005 : PassiveAbilityBase
{
    private int _damageAccumulator = 0;
    private int _flowConsumedAccumulator = 0;
    private int _flowToGainNextRound = 0;
    private int _strengthToGainNextRound = 0;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _damageAccumulator = 0;
        _flowConsumedAccumulator = 0;
        _flowToGainNextRound = 0;
        _strengthToGainNextRound = 0;
        SteriaLogger.Log($"PassiveAbility_9002005 (斯拉泽雅司流者): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _damageAccumulator = 0;
        _flowConsumedAccumulator = 0;
        _flowToGainNextRound = 0;
        _strengthToGainNextRound = 0;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        // 在回合开始时获得累积的流
        if (_flowToGainNextRound > 0 && owner != null)
        {
            Steria.CardAbilityHelper.AddFlowStacks(owner, _flowToGainNextRound);
            SteriaLogger.Log($"斯拉泽雅司流者: {owner.UnitData?.unitData?.name} gained {_flowToGainNextRound} flow from damage dealt last round");
            _flowToGainNextRound = 0;
        }

        // 在回合开始时获得累积的强壮
        if (_strengthToGainNextRound > 0 && owner != null)
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, _strengthToGainNextRound, owner);
            SteriaLogger.Log($"斯拉泽雅司流者: {owner.UnitData?.unitData?.name} gained {_strengthToGainNextRound} Strength from flow consumed last round");
            _strengthToGainNextRound = 0;
        }

        // 重置累积器
        _damageAccumulator = 0;
        _flowConsumedAccumulator = 0;
    }

    /// <summary>
    /// 当造成伤害时调用（由Harmony补丁触发）
    /// </summary>
    public void OnDamageDealt(int damage)
    {
        if (owner == null || damage <= 0) return;

        _damageAccumulator += damage;
        SteriaLogger.Log($"斯拉泽雅司流者: {owner.UnitData?.unitData?.name} dealt {damage} damage, accumulator: {_damageAccumulator}");

        // 每造成10点伤害，下回合获得1层流
        while (_damageAccumulator >= 10)
        {
            _damageAccumulator -= 10;
            _flowToGainNextRound++;
            SteriaLogger.Log($"斯拉泽雅司流者: Will gain 1 flow next round (total pending: {_flowToGainNextRound})");
        }
    }

    /// <summary>
    /// 当流被消耗时调用（由Harmony补丁触发）
    /// </summary>
    public void OnFlowConsumed(int amount)
    {
        if (owner == null || amount <= 0) return;

        _flowConsumedAccumulator += amount;
        SteriaLogger.Log($"斯拉泽雅司流者: {owner.UnitData?.unitData?.name} consumed {amount} flow, accumulator: {_flowConsumedAccumulator}");

        // 每消耗10层流，下回合获得1层强壮
        while (_flowConsumedAccumulator >= 10)
        {
            _flowConsumedAccumulator -= 10;
            _strengthToGainNextRound++;
            SteriaLogger.Log($"斯拉泽雅司流者: Will gain 1 Strength next round (total pending: {_strengthToGainNextRound})");
        }
    }
}
