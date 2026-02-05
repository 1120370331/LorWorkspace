using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// 克丽丝塔夏被动与BUFF（全局命名空间）

/// <summary>
/// 黄金之潮 Buff
/// </summary>
public class BattleUnitBuf_GoldenTide : BattleUnitBuf
{
    protected override string keywordId => "SteriaGoldenTide";
    protected override string keywordIconId => "GoldenTide";
    public override BufPositiveType positiveType => BufPositiveType.Positive;
}

/// <summary>
/// 月之引力 Buff（负面，回合结束清空，混乱伤害提高10%/层）
/// </summary>
public class BattleUnitBuf_MoonGravity : BattleUnitBuf
{
    protected override string keywordId => "SteriaMoonGravity";
    protected override string keywordIconId => "MoonGravity";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        int stacks = Math.Min(10, Math.Max(0, stack));
        if (stacks <= 0)
        {
            return 1f;
        }
        return 1f + stacks * 0.1f;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        this.Destroy();
    }
}

/// <summary>
/// 月之引力 - 下回合生效的暂存 Buff（本回合隐藏）
/// </summary>
public class BattleUnitBuf_MoonGravityNextRound : BattleUnitBuf
{
    protected override string keywordId => "SteriaMoonGravityNext";
    protected override string keywordIconId => "MoonGravity";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner == null)
        {
            this.Destroy();
            return;
        }

        int add = Math.Max(0, stack);
        if (add > 0)
        {
            BattleUnitBuf_MoonGravity buf = _owner.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_MoonGravity) as BattleUnitBuf_MoonGravity;
            if (buf != null)
            {
                buf.stack = Math.Min(10, buf.stack + add);
            }
            else
            {
                _owner.bufListDetail.AddBuf(new BattleUnitBuf_MoonGravity { stack = Math.Min(10, add) });
            }
        }

        this.Destroy();
    }
}

/// <summary>
/// 汐火联星锁定目标（连续2次同目标）
/// </summary>
public class BattleUnitBuf_ChristashaTwinStarLock : BattleUnitBuf
{
    public override bool Hide => true;
    public BattleUnitModel target;
    public int remaining = 2;
}

public static class ChristashaAbilityHelper
{
    private static readonly Dictionary<BattleUnitModel, int> _twinStarConsumed = new Dictionary<BattleUnitModel, int>();

    public static int GetTideStacks(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        BattleUnitBuf_Tide tide = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        return tide?.stack ?? 0;
    }

    public static int GetGoldenTideStacks(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        BattleUnitBuf_GoldenTide golden = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_GoldenTide) as BattleUnitBuf_GoldenTide;
        return golden?.stack ?? 0;
    }

    public static int GetTotalTideStacks(BattleUnitModel unit)
    {
        return GetTideStacks(unit) + GetGoldenTideStacks(unit);
    }

    public static void AddTwinStarTideConsumed(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        if (_twinStarConsumed.TryGetValue(unit, out int current))
        {
            _twinStarConsumed[unit] = current + amount;
        }
        else
        {
            _twinStarConsumed[unit] = amount;
        }
    }

    public static int GetTwinStarTideConsumed(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        return _twinStarConsumed.TryGetValue(unit, out int value) ? value : 0;
    }

    public static void ClearTwinStarTideConsumed()
    {
        _twinStarConsumed.Clear();
    }

    public static int ConsumeTideStacks(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return 0;
        BattleUnitBuf_Tide tide = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
        if (tide == null || tide.stack <= 0) return 0;

        int toConsume = Math.Min(amount, tide.stack);
        tide.stack -= toConsume;
        if (tide.stack <= 0)
        {
            tide.Destroy();
        }

        HarmonyHelpers.NotifyPassivesOnTideConsumed(unit, toConsume);
        return toConsume;
    }

    public static void AddGoldenTideStacks(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        BattleUnitBuf_GoldenTide golden = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_GoldenTide) as BattleUnitBuf_GoldenTide;
        if (golden != null)
        {
            golden.stack += amount;
        }
        else
        {
            golden = new BattleUnitBuf_GoldenTide { stack = amount };
            unit.bufListDetail.AddBuf(golden);
        }
    }

    public static int ConvertTideToGolden(BattleUnitModel unit, int amount)
    {
        int consumed = ConsumeTideStacks(unit, amount);
        if (consumed > 0)
        {
            AddGoldenTideStacks(unit, consumed);
        }
        return consumed;
    }

    public static void AddMoonGravity(BattleUnitModel target, int amount)
    {
        if (target == null || amount <= 0) return;
        BattleUnitBuf_MoonGravityNextRound buf = target.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_MoonGravityNextRound) as BattleUnitBuf_MoonGravityNextRound;
        int add = amount;
        if (buf != null)
        {
            buf.stack = Math.Min(10, buf.stack + add);
        }
        else
        {
            buf = new BattleUnitBuf_MoonGravityNextRound { stack = Math.Min(10, add) };
            target.bufListDetail.AddBuf(buf);
        }
    }
}

/// <summary>
/// 速战速决3 (ID: 9009001)
/// 初始拥有3颗速度骰子，情感等级达到3后额外获得1颗
/// </summary>
public class PassiveAbility_9009001 : PassiveAbilityBase
{
    public override void OnWaveStart()
    {
        base.OnWaveStart();
        if (owner?.Book != null && owner.faction == Faction.Enemy)
        {
            owner.Book.SetSpeedDiceNum(3);
            SteriaLogger.Log($"速战速决3: Set speed dice to 3 for {owner.UnitData?.unitData?.name}");
        }
    }

    public override int SpeedDiceNumAdder()
    {
        if (owner?.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 3)
        {
            return 1;
        }
        return 0;
    }
}

/// <summary>
/// 金色的乐章 (ID: 9009002)
/// - 消耗或转换潮时积累等量乐谱进度
/// - 触发沧海之声后，获得5层潮
/// </summary>
public class PassiveAbility_9009002 : PassiveAbilityBase
{
    private const string MOD_ID = "SteriaBuilding";
    private const int CARD_TWINSTAR_EGO = 9009007; // 汐火联星 (EGO装备)
    private bool _egoCardAdded;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _egoCardAdded = false;
        TryAddTwinStarEgo();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        TryAddTwinStarEgo();
    }

    public void OnTideConsumed(int amount)
    {
        if (amount <= 0 || owner == null) return;
        Steria.MusicScoreSystem.AddScoreFromTide(owner, amount);
    }

    public void OnSeaVoiceTriggered()
    {
        if (owner == null || owner.IsDead()) return;
        PassiveAbility_9004001.AddTideStacks(owner, 5);
    }

    private void TryAddTwinStarEgo()
    {
        if (_egoCardAdded || owner == null || owner.faction != Faction.Player) return;
        try
        {
            LorId egoCardId = new LorId(MOD_ID, CARD_TWINSTAR_EGO);
            owner.personalEgoDetail?.AddCard(egoCardId);
            _egoCardAdded = true;
            SteriaLogger.Log($"金色的乐章: Added 汐火联星 EGO card to {owner.UnitData?.unitData?.name}");
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"金色的乐章: Failed to add 汐火联星 EGO card: {ex.Message}");
        }
    }
}

/// <summary>
/// 金色的潮汐 (ID: 9009008)
/// 回合开始拥有大于3层潮时，将1层潮转化为黄金之潮
/// </summary>
public class PassiveAbility_9009008 : PassiveAbilityBase
{
    public override void OnRoundStartAfter()
    {
        base.OnRoundStartAfter();
        if (owner == null) return;

        int tide = ChristashaAbilityHelper.GetTideStacks(owner);
        if (tide > 3)
        {
            ChristashaAbilityHelper.ConvertTideToGolden(owner, 1);
        }
    }
}

/// <summary>
/// 高阶月长石 (ID: 9009003)
/// - 受到的混乱伤害-3
/// - 每一舞台首次陷入混乱时：解除混乱并恢复所有混乱抗性
/// - 每一幕开始时：为所有友方角色恢复2点混乱抗性
/// </summary>
public class PassiveAbility_9009003 : PassiveAbilityBase
{
    private bool _breakRecovered;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _breakRecovered = false;
    }

    public override int GetBreakDamageReduction(BattleDiceBehavior behavior)
    {
        return 3;
    }

    public override int GetBreakDamageReductionAll(int dmg, DamageType dmgType, BattleUnitModel attacker)
    {
        return 3;
    }

    public override void OnBreakState()
    {
        base.OnBreakState();
        if (_breakRecovered || owner == null) return;
        _breakRecovered = true;
        BattleUnitBreakDetail bd = owner.breakDetail;
        if (bd != null)
        {
            int maxGauge = bd.GetDefaultBreakGauge();
            bd.RecoverBreakLife(owner.MaxBreakLife, false);
            bd.RecoverBreak(maxGauge);
            bd.nextTurnBreak = false;
        }
        owner.turnState = BattleUnitTurnState.WAIT_CARD;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (BattleObjectManager.instance == null || owner == null) return;
        foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            ally?.breakDetail?.RecoverBreak(2);
        }
    }
}

/// <summary>
/// 遗物：海公主的桂冠 (ID: 9009004)
/// - 消耗/转换潮时：下一回合随机使一个友方单位随机获得1层强壮/守护/恢复2点体力/恢复2点混乱抗性（不受潮/黄金之潮影响）
/// </summary>
public class PassiveAbility_9009004 : PassiveAbilityBase
{
    public void OnTideConsumed(int amount)
    {
        if (owner == null || amount <= 0) return;

        // 每层潮触发一次
        for (int i = 0; i < amount; i++)
        {
            BattleUnitBuf_ChristashaCrownNextTurn buf = owner.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_ChristashaCrownNextTurn) as BattleUnitBuf_ChristashaCrownNextTurn;
            if (buf == null)
            {
                buf = new BattleUnitBuf_ChristashaCrownNextTurn { stack = 1 };
                owner.bufListDetail.AddBuf(buf);
            }
            else
            {
                buf.stack += 1;
            }
        }
    }
}

public class BattleUnitBuf_ChristashaCrownNextTurn : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner == null || BattleObjectManager.instance == null)
        {
            this.Destroy();
            return;
        }

        List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(_owner.faction);
        if (allies == null || allies.Count == 0)
        {
            this.Destroy();
            return;
        }

        int times = Math.Max(0, stack);
        for (int i = 0; i < times; i++)
        {
            BattleUnitModel target = allies[UnityEngine.Random.Range(0, allies.Count)];
            int roll = UnityEngine.Random.Range(0, 4);
            if (roll == 0)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, null);
            }
            else if (roll == 1)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 1, null);
            }
            else if (roll == 2)
            {
                target.RecoverHP(2);
            }
            else
            {
                target.breakDetail?.RecoverBreak(2);
            }
        }

        this.Destroy();
    }
}

/// <summary>
/// 月光工坊烙印 (ID: 9009005)
/// 命中敌人时施加1层月之引力
/// </summary>
public class PassiveAbility_9009005 : PassiveAbilityBase
{
    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);
        BattleUnitModel target = behavior?.card?.target;
        if (target == null) return;
        ChristashaAbilityHelper.AddMoonGravity(target, 1);
    }
}

/// <summary>
/// 溢流潮生 (ID: 9009006)
/// - 每层潮/黄金之潮使伤害+5%（至多50%）
/// - 潮/黄金之潮>=10时：所有骰子威力+1
/// </summary>
public class PassiveAbility_9009006 : PassiveAbilityBase
{
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null || owner == null) return;
        int total = ChristashaAbilityHelper.GetTotalTideStacks(owner);
        if (total >= 10)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior)
    {
        if (behavior == null || behavior.Type != BehaviourType.Atk || owner == null) return;
        int total = ChristashaAbilityHelper.GetTotalTideStacks(owner);
        int stacks = Math.Min(10, Math.Max(0, total));
        if (stacks <= 0) return;
        behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = stacks * 5 });
    }
}

/// <summary>
/// 变幻莫测 (ID: 9009007)
/// 每一幕将指定书页置入手中且光芒消耗为0
/// </summary>
public class PassiveAbility_9009007 : PassiveAbilityBase
{
    private int _roundCount = 0;
    private static readonly string MOD_ID = "SteriaBuilding";

    private static readonly int CARD_GOLDEN_TIDE = 9009001;   // 金火璀璨之潮
    private static readonly int CARD_HOMECOMING = 9009002;    // 何以归乡
    private static readonly int CARD_ELIYE = 9009003;         // 海银工坊: 埃利耶
    private static readonly int CARD_LIGHT_PURGE = 9009004;   // 涤光驱暗
    private static readonly int CARD_BURST = 9009005;         // 迸射流光
    private static readonly int CARD_GLORY = 9009006;         // 汐音: 光耀万海
    private static readonly int CARD_TWINSTAR = 9009007;      // 汐火联星

    private static readonly int[][] _patterns = new int[][]
    {
        new int[] { CARD_GOLDEN_TIDE, CARD_HOMECOMING, CARD_ELIYE, CARD_LIGHT_PURGE, CARD_GOLDEN_TIDE, CARD_BURST }, // 模式1: 资源启动
        new int[] { CARD_LIGHT_PURGE, CARD_BURST, CARD_ELIYE, CARD_GOLDEN_TIDE, CARD_LIGHT_PURGE, CARD_HOMECOMING },  // 模式2: 压力输出
        new int[] { CARD_ELIYE, CARD_LIGHT_PURGE, CARD_HOMECOMING, CARD_BURST, CARD_GOLDEN_TIDE, CARD_ELIYE },         // 模式3: 增益与削弱
        new int[] { CARD_GLORY, CARD_BURST, CARD_LIGHT_PURGE, CARD_GOLDEN_TIDE, CARD_ELIYE, CARD_HOMECOMING },          // 模式4: 高压收束
        new int[] { CARD_BURST, CARD_LIGHT_PURGE, CARD_GOLDEN_TIDE, CARD_BURST, CARD_HOMECOMING, CARD_ELIYE },          // 模式5: 双爆发
        new int[] { CARD_GOLDEN_TIDE, CARD_ELIYE, CARD_LIGHT_PURGE, CARD_HOMECOMING, CARD_GOLDEN_TIDE, CARD_GLORY },    // 模式6: 乐章轮替
    };

    private static readonly int[][] _emotionBonusCards = new int[][]
    {
        new int[] { CARD_LIGHT_PURGE }, // 模式1
        new int[] { CARD_BURST },       // 模式2
        new int[] { CARD_ELIYE },       // 模式3
        new int[] { CARD_TWINSTAR },    // 模式4
        new int[] { CARD_HOMECOMING },  // 模式5
        new int[] { CARD_BURST },       // 模式6
    };

    private static readonly int[] _initialSequence = new int[] { 0, 1, 2, 3, 4, 5 };
    private static readonly int[] _loopSequence = new int[] { 1, 2, 4, 0, 3, 5 };

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _roundCount = 0;
        SteriaLogger.Log($"PassiveAbility_9009007 (变幻莫测): Init for {self?.UnitData?.unitData?.name}");
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
        if (_roundCount < _initialSequence.Length)
        {
            patternIndex = _initialSequence[_roundCount];
        }
        else
        {
            int loopIndex = (_roundCount - _initialSequence.Length) % _loopSequence.Length;
            patternIndex = _loopSequence[loopIndex];
        }

        int[] cards = _patterns[patternIndex];
        bool hasEmotionBonus = owner.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 3;
        int[] bonusCards = hasEmotionBonus && patternIndex < _emotionBonusCards.Length ? _emotionBonusCards[patternIndex] : new int[0];

        SteriaLogger.Log($"变幻莫测(克丽丝塔夏): Round {_roundCount + 1}, Pattern {patternIndex + 1}, Adding {cards.Length} cards, EmotionLevel={owner.emotionDetail?.EmotionLevel}, BonusCards={bonusCards.Length}");

        owner.allyCardDetail.ExhaustAllCards();

        int priority = 100;
        foreach (int cardId in cards)
        {
            AddNewCard(cardId, priority);
            priority -= 10;
        }

        if (bonusCards.Length > 0)
        {
            foreach (int cardId in bonusCards)
            {
                if (cardId == CARD_TWINSTAR)
                {
                    // 汐火联星需要连续2次使用
                    AddNewCard(cardId, priority);
                    priority -= 10;
                    AddNewCard(cardId, priority);
                    priority -= 10;
                    continue;
                }
                AddNewCard(cardId, priority);
                priority -= 10;
            }
        }

        _roundCount++;
    }

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
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"变幻莫测(克丽丝塔夏): Failed to add card {cardId}: {ex.Message}");
        }
    }
}
