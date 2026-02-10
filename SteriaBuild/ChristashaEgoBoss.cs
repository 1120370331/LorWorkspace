using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// ========================================================
// 神备EGO：日沐曦渊 - Buff / 被动 / 关卡管理器
// ========================================================

public static class PrimalTidePowerScope
{
    [ThreadStatic]
    private static int _allowDepth;

    public static bool IsAllowanceActive => _allowDepth > 0;

    public static void RunWithAllowance(Action action)
    {
        if (action == null)
        {
            return;
        }

        _allowDepth++;
        try
        {
            action();
        }
        finally
        {
            _allowDepth--;
            if (_allowDepth < 0)
            {
                _allowDepth = 0;
            }
        }
    }
}

#region ===== Buffs =====

/// <summary>
/// 日光 Buff（负面）
/// 拥有本效果的单位受到烧伤伤害时，消耗1层本层数，并造成1/2的等量混乱伤害
/// </summary>
public class BattleUnitBuf_Sunlight : BattleUnitBuf
{
    protected override string keywordId => "SteriaSunlight";
    protected override string keywordIconId => "Sunlight";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    /// <summary>
    /// 当烧伤伤害结算时调用此方法（由 Harmony Patch 触发）
    /// </summary>
    public int OnBurnDamageDealt(int burnDamage)
    {
        if (_owner == null || stack <= 0 || burnDamage <= 0) return 0;
        int breakDmg = Mathf.CeilToInt(burnDamage / 2f);
        SteriaLogger.Log($"日光触发前: {_owner.UnitData?.unitData?.name}, 日光层数={stack}, 烧伤伤害={burnDamage}, 追加混乱={breakDmg}");
        stack--;
        if (stack <= 0) this.Destroy();
        _owner.breakDetail?.TakeBreakDamage(breakDmg, DamageType.Buf, null);
        SteriaLogger.Log($"日光触发后: {_owner.UnitData?.unitData?.name}, 剩余日光层数={Math.Max(0, stack)}");
        return breakDmg;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        // 日光不自然衰减，只在触发时消耗
    }
}

/// <summary>
/// 致盲 Buff（负面）
/// 拼点时骰子威力-X（X=层数）
/// 拥有本效果的单位无法更改敌人的速度骰子目标
/// 每回合结束层数-1；在生效后的第2回合结束时移除全部层数
/// </summary>
public class BattleUnitBuf_Blinding : BattleUnitBuf
{
    private int _activeRoundEndCount;

    protected override string keywordId => "SteriaBlinding";
    protected override string keywordIconId => "Blinding";
    public override BufPositiveType positiveType => BufPositiveType.Negative;
    public override int paramInBufDesc => stack;

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null || stack <= 0) return;
        behavior.ApplyDiceStatBonus(new DiceStatBonus { power = -Mathf.Max(0, stack) });
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        if (stack <= 0)
        {
            Destroy();
            return;
        }

        _activeRoundEndCount++;
        if (_activeRoundEndCount >= 2)
        {
            stack = 0;
            Destroy();
            return;
        }

        stack--;
        if (stack <= 0)
        {
            Destroy();
        }
    }

    // 致盲：无法更改敌人的速度骰子目标
    public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int currentSlot)
    {
        return null; // 返回null表示不改变目标
    }

    // 致盲：不可被嘲讽机制重定向
    public override bool IsTauntable()
    {
        return false;
    }
}

/// <summary>
/// Phase2激活标记 Buff（显示，用于全局追踪Phase2状态）
/// </summary>
public class BattleUnitBuf_Phase2Active : BattleUnitBuf
{
    protected override string keywordId => "SteriaPhase2Active";
    protected override string keywordIconId => "Phase2Active";
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    public override bool Hide => false;
    public override int paramInBufDesc => stack;
}

/// <summary>
/// 原初之潮（正面）
/// 拥有本效果的单位，其骰子威力只受[流]/[梦]相关效果影响
/// </summary>
public class BattleUnitBuf_PrimalTide : BattleUnitBuf
{
    protected override string keywordId => "SteriaPrimalTide";
    protected override string keywordIconId => "PrimalTide";
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    public override int paramInBufDesc => stack;

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        stack--;
        if (stack <= 0)
        {
            Destroy();
        }
    }
}

/// <summary>
/// 埃利耶水中的圣职 - 后续回合强壮+流
/// </summary>
public class BattleUnitBuf_EliyeHolyBuff : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner == null || stack <= 0) { this.Destroy(); return; }
        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, _owner);
        // 获得3层流
        BattleUnitBuf_Flow flow = _owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        if (flow != null) flow.stack += 3;
        else _owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 3 });
        stack--;
        if (stack <= 0) this.Destroy();
    }
}

/// <summary>
/// 黎明之光 渐强记录
/// </summary>
public class BattleUnitBuf_DawnLightGrowth : BattleUnitBuf
{
    public override bool Hide => true;
    public int GrowthCount;
    public const int MaxGrowth = 3; // 最多+6 (每次+2, 共3次)
}

#endregion

#region ===== Helper =====

public static class ChristashaEgoBossHelper
{
    private static readonly string MOD_ID = "SteriaBuilding";

    private static int ConsumeTideBonusForSpecialDebuff(BattleUnitModel giver, string debuffName)
    {
        if (giver == null)
        {
            return 0;
        }

        BattleUnitBuf_Tide tideBuf = giver.bufListDetail?.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;

        if (tideBuf == null || tideBuf.stack <= 0)
        {
            return 0;
        }

        int bonus = tideBuf.ConsumeTideForBonus();
        if (bonus > 0)
        {
            SteriaLogger.Log($"潮增幅: {debuffName} +{bonus}, giver={giver.UnitData?.unitData?.name}");
        }

        return bonus;
    }

    public static void AddSunlight(BattleUnitModel target, int amount, BattleUnitModel giver = null)
    {
        if (target == null || amount <= 0) return;

        int finalAmount = amount + ConsumeTideBonusForSpecialDebuff(giver, "日光");

        BattleUnitBuf_Sunlight buf = target.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Sunlight) as BattleUnitBuf_Sunlight;
        if (buf != null) buf.stack += finalAmount;
        else { buf = new BattleUnitBuf_Sunlight { stack = finalAmount }; target.bufListDetail.AddBuf(buf); }

        SteriaLogger.Log($"施加日光: {target.UnitData?.unitData?.name} +{finalAmount}");
    }

    public static void AddBlinding(BattleUnitModel target, int amount, BattleUnitModel giver = null)
    {
        if (target == null || amount <= 0) return;

        int finalAmount = amount + ConsumeTideBonusForSpecialDebuff(giver, "致盲");

        List<BattleUnitBuf> readyList = target.bufListDetail.GetReadyBufList();
        BattleUnitBuf_Blinding readyBuf = readyList?
            .FirstOrDefault(b => b is BattleUnitBuf_Blinding) as BattleUnitBuf_Blinding;

        if (readyBuf != null)
        {
            readyBuf.stack += finalAmount;
        }
        else
        {
            target.bufListDetail.AddReadyBuf(new BattleUnitBuf_Blinding { stack = finalAmount });
        }

        SteriaLogger.Log($"致盲将于下一幕生效: {target.UnitData?.unitData?.name} +{finalAmount}");
    }

    public static bool IsPhase2Active(BattleUnitModel boss)
    {
        if (boss == null) return false;
        return boss.bufListDetail.GetActivatedBufList().Any(b => b is BattleUnitBuf_Phase2Active);
    }

    public static bool HasPrimalTide(BattleUnitModel unit)
    {
        if (unit == null || unit.bufListDetail == null)
        {
            return false;
        }

        BattleUnitBuf_PrimalTide primal = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_PrimalTide) as BattleUnitBuf_PrimalTide;
        return primal != null && primal.stack > 0;
    }

    public static void ApplyPrimalTideToAllUnits(int stack = 1)
    {
        if (stack <= 0 || BattleObjectManager.instance == null)
        {
            return;
        }

        List<BattleUnitModel> units = BattleObjectManager.instance.GetAliveList(false);
        if (units == null)
        {
            return;
        }

        foreach (BattleUnitModel unit in units)
        {
            if (unit == null || unit.IsDead() || unit.bufListDetail == null)
            {
                continue;
            }

            BattleUnitBuf_PrimalTide primal = unit.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_PrimalTide) as BattleUnitBuf_PrimalTide;
            if (primal == null)
            {
                unit.bufListDetail.AddBuf(new BattleUnitBuf_PrimalTide { stack = stack });
            }
            else if (primal.stack < stack)
            {
                primal.stack = stack;
            }
        }
    }

    /// <summary>
    /// 获取所有存活单位（敌我双方）的总烧伤层数
    /// </summary>
    public static int GetTotalBurnStacks()
    {
        int total = 0;
        if (BattleObjectManager.instance == null) return 0;
        foreach (var unit in BattleObjectManager.instance.GetAliveList(true))
        {
            if (unit == null) continue;
            total += unit.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
        }
        return total;
    }

    /// <summary>
    /// 移除目标的所有烧伤（包含本回合生效与待生效）
    /// </summary>
    public static int RemoveAllBurnStacks(BattleUnitModel target)
    {
        if (target == null || target.bufListDetail == null)
        {
            return 0;
        }

        int removed = target.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
        target.bufListDetail.RemoveBufAll(KeywordBuf.Burn);

        removed += RemoveBurnFromPendingList(target.bufListDetail.GetReadyBufList());
        removed += RemoveBurnFromPendingList(target.bufListDetail.GetReadyReadyBufList());

        return removed;
    }

    private static int RemoveBurnFromPendingList(List<BattleUnitBuf> pendingList)
    {
        if (pendingList == null || pendingList.Count == 0)
        {
            return 0;
        }

        int removed = 0;
        for (int index = pendingList.Count - 1; index >= 0; index--)
        {
            BattleUnitBuf pendingBuf = pendingList[index];
            if (pendingBuf == null || pendingBuf.IsDestroyed())
            {
                continue;
            }

            if (pendingBuf.bufType != KeywordBuf.Burn)
            {
                continue;
            }

            removed += Math.Max(0, pendingBuf.stack);
            pendingList.RemoveAt(index);
        }

        return removed;
    }
}

#endregion

#region ===== Passives =====

/// <summary>
/// 神备EGO：日沐曦渊 (ID: 9010001)
/// - 每回合将意图使用的书页置入手中，自身使用书页不消耗光芒
/// - 速度骰子：情感1~5为 4/5/6/7/7（情感4起包含原版自动+1）
/// - 当血量低于350后，免疫所有伤害并进入Phase2
/// </summary>
public class PassiveAbility_9010001 : PassiveAbilityBase
{
    private const int Phase2TransitionHp = 350;

    private int _roundCount = 0;
    private bool _phase2 = false;
    private bool _transitioning = false;
    private bool _phase2SecondTurnSetupPending = false;
    private static readonly string MOD_ID = "SteriaBuilding";

    // Phase 1 卡牌ID
    private const int CARD_DONT_FEAR = 9010001;     // 不要害怕，我在梦中 — 防御/梦
    private const int CARD_STAR_PIERCE = 9010002;   // 飞星连刺 — 远程突刺x4
    private const int CARD_ELIYE_HOLY = 9010003;    // 埃利耶，水中的圣职 — 增益
    private const int CARD_DAWN_APPROACH = 9010004;  // 渐明 — 日光+烧伤
    private const int CARD_GOLDEN_CROW = 9010005;    // 金乌 — 远程致盲+烧伤
    private const int CARD_BURST_LIGHT = 9010006;    // 迸射流光 — 高伤单体重投
    private const int CARD_BURST_MASS = 9010007;     // 迸发 — 群体清算
    private const int CARD_DAWN_LIGHT = 9010008;     // 黎明之光 — 群体交锋

    // Phase 2 专属
    private const int CARD_FLOW_COME_GO = 9010009;   // 随流而来，伴流而去 — 流转/流
    private const int CARD_ABYSS_SUNRISE = 9010010;  // 深渊之中，太阳升起 — 流转/潮
    private const int CARD_DAWN_ARRIVAL = 9010011;   // 我将和黎明一起到来 — 终极清算

    // ============================================================
    // 速度骰子设计（按需求）
    // ============================================================
    //   情感1:4 | 情感2:5 | 情感3:6 | 情感4:7 | 情感5:7
    //   注：情感4起有原版额外速度骰子
    // ============================================================
    private int GetCurrentSpeedDiceCount()
    {
        int emotionLevel = owner?.emotionDetail?.EmotionLevel ?? 0;
        if (emotionLevel >= 4) return 7;
        if (emotionLevel >= 3) return 6;
        if (emotionLevel >= 2) return 5;
        return 4;
    }

    // ============================================================
    // Phase 1 出牌模式（每模式8张，取前N张=当前速度骰子数）
    // ============================================================
    // 前4张 = 核心（情感0, 4骰子）
    // 第5张 = 情感1追加（5骰子）
    // 第6张 = 情感2追加（6骰子）
    // 第7张 = 情感4追加（7骰子）
    // 第8张 = 情感5追加（8骰子，全力出击）
    //
    //  [0] 稳固开局  核心: 防御+埃利耶+渐明+金乌        追加: 飞星 迸射 埃利耶 渐明
    //  [1] 远程试探  核心: 金乌+飞星+埃利耶+渐明        追加: 迸射 不怕 飞星 金乌
    //  [2] 全面施压  核心: 渐明+迸射+金乌+埃利耶        追加: 飞星 不怕 渐明 迸射
    //  [3] 光芒普照  核心: 黎明之光+渐明+不怕+埃利耶    追加: 金乌 迸射 飞星 渐明
    //  [4] 双线爆发  核心: 迸射+飞星+渐明+金乌          追加: 埃利耶 不怕 迸射 飞星
    //  [5] 烈焰收割  核心: 迸发+埃利耶+不怕+渐明        追加: 金乌 迸射 飞星 埃利耶
    //  [6] 重压轮替  核心: 金乌+迸射+埃利耶+渐明        追加: 飞星 不怕 金乌 迸射
    //  [7] 光耀再临  核心: 黎明之光+迸射+不怕+金乌      追加: 埃利耶 渐明 飞星 迸射
    // ============================================================
    private static readonly int[][] _phase1Patterns = new int[][]
    {
        /* 0 */ new int[] { CARD_DONT_FEAR,    CARD_ELIYE_HOLY,    CARD_DAWN_APPROACH, CARD_GOLDEN_CROW,   CARD_STAR_PIERCE,  CARD_BURST_LIGHT,  CARD_ELIYE_HOLY,    CARD_DAWN_APPROACH },
        /* 1 */ new int[] { CARD_GOLDEN_CROW,  CARD_STAR_PIERCE,   CARD_ELIYE_HOLY,    CARD_DAWN_APPROACH, CARD_BURST_LIGHT,  CARD_DONT_FEAR,    CARD_STAR_PIERCE,   CARD_GOLDEN_CROW   },
        /* 2 */ new int[] { CARD_DAWN_APPROACH, CARD_BURST_LIGHT,  CARD_GOLDEN_CROW,   CARD_ELIYE_HOLY,    CARD_STAR_PIERCE,  CARD_DONT_FEAR,    CARD_DAWN_APPROACH, CARD_BURST_LIGHT   },
        /* 3 */ new int[] { CARD_DAWN_LIGHT,   CARD_DAWN_APPROACH, CARD_DONT_FEAR,     CARD_ELIYE_HOLY,    CARD_GOLDEN_CROW,  CARD_BURST_LIGHT,  CARD_STAR_PIERCE,   CARD_DAWN_APPROACH },
        /* 4 */ new int[] { CARD_BURST_LIGHT,  CARD_STAR_PIERCE,   CARD_DAWN_APPROACH, CARD_GOLDEN_CROW,   CARD_ELIYE_HOLY,   CARD_DONT_FEAR,    CARD_BURST_LIGHT,   CARD_STAR_PIERCE   },
        /* 5 */ new int[] { CARD_BURST_MASS,   CARD_ELIYE_HOLY,    CARD_DONT_FEAR,     CARD_DAWN_APPROACH, CARD_GOLDEN_CROW,  CARD_BURST_LIGHT,  CARD_STAR_PIERCE,   CARD_ELIYE_HOLY    },
        /* 6 */ new int[] { CARD_GOLDEN_CROW,  CARD_BURST_LIGHT,   CARD_ELIYE_HOLY,    CARD_DAWN_APPROACH, CARD_STAR_PIERCE,  CARD_DONT_FEAR,    CARD_GOLDEN_CROW,   CARD_BURST_LIGHT   },
        /* 7 */ new int[] { CARD_DAWN_LIGHT,   CARD_BURST_LIGHT,   CARD_DONT_FEAR,     CARD_GOLDEN_CROW,   CARD_ELIYE_HOLY,   CARD_DAWN_APPROACH, CARD_STAR_PIERCE,  CARD_BURST_LIGHT   },
    };

    private static readonly int[] _phase1InitialOrder = new int[] { 0, 1, 2, 3, 4, 5 };
    private static readonly int[] _phase1LoopOrder    = new int[] { 7, 4, 6, 3, 2, 5 };

    // ============================================================
    // Phase 2 出牌模式（严格轮转）
    // ============================================================
    // - 3张群攻每回合轮流使用：
    //   回合1: 我将和黎明一起到来(9010011)
    //   回合2: 迸发(9010007)
    //   回合3: 黎明之光(9010008)
    //   之后循环
    // - 随流而来(9010009) 与 深渊之中(9010010) 每回合最多出现其一
    // - 迸射流光(9010006) 与 埃利耶(9010003) 每回合最多出现1次
    // - 金乌(9010005)/飞星连刺(9010002)/渐明(9010004) 不做次数上限
    // ============================================================
    private static readonly int[][] _phase2RotationPatterns = new int[][]
    {
        // 轮转1：我将和黎明一起到来 + 随流而来（渐明可超过2次）
        new int[] { CARD_DAWN_ARRIVAL, CARD_FLOW_COME_GO, CARD_DAWN_APPROACH, CARD_DAWN_APPROACH, CARD_GOLDEN_CROW, CARD_STAR_PIERCE, CARD_DAWN_APPROACH, CARD_DONT_FEAR },
        // 轮转2：迸发 + 深渊之中（飞星连刺可超过2次；迸射流光最多1次）
        new int[] { CARD_BURST_MASS, CARD_ABYSS_SUNRISE, CARD_STAR_PIERCE, CARD_GOLDEN_CROW, CARD_STAR_PIERCE, CARD_BURST_LIGHT, CARD_STAR_PIERCE, CARD_DONT_FEAR },
        // 轮转3：黎明之光 + 随流而来（金乌可超过2次；埃利耶最多1次）
        new int[] { CARD_DAWN_LIGHT, CARD_FLOW_COME_GO, CARD_GOLDEN_CROW, CARD_ELIYE_HOLY, CARD_GOLDEN_CROW, CARD_DAWN_APPROACH, CARD_GOLDEN_CROW, CARD_STAR_PIERCE },
    };

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _roundCount = 0;
        _phase2 = false;
        _transitioning = false;
        _phase2SecondTurnSetupPending = false;
        // 基础4颗速度骰子
        if (owner?.Book != null && owner.faction == Faction.Enemy)
        {
            owner.Book.SetSpeedDiceNum(4);
        }
    }

    public override int SpeedDiceNumAdder()
    {
        // 基础速度骰子设为4：
        // 情感1:+0=4 | 情感2:+1=5 | 情感3~5:+2=6
        // 情感5的第7颗由原版自动+1提供，避免叠到9颗
        int emotionLevel = owner?.emotionDetail?.EmotionLevel ?? 0;
        if (emotionLevel >= 3) return 2;
        if (emotionLevel >= 2) return 1;
        return 0;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (owner == null) return;

        if (_phase2 && _phase2SecondTurnSetupPending)
        {
            FinalizePhase2SecondTurnSetup();
        }

        // 计算当前速度骰子数量 → 决定出几张牌
        int speedDiceCount = GetCurrentSpeedDiceCount();

        int patternIndex;
        int[] fullPattern;

        if (_phase2)
        {
            // Phase2第1幕对应_roundCount=0：严格3群攻轮转
            int phase2Round = _roundCount + 1;
            patternIndex = Math.Max(0, phase2Round - 1) % _phase2RotationPatterns.Length;
            fullPattern = _phase2RotationPatterns[patternIndex];
        }
        else
        {
            // 出牌逻辑（类似变幻莫测，使用预设顺序表）
            int patternIndexPhase1;
            if (_roundCount < _phase1InitialOrder.Length)
            {
                patternIndexPhase1 = _phase1InitialOrder[_roundCount];
            }
            else
            {
                int loopIdx = (_roundCount - _phase1InitialOrder.Length) % _phase1LoopOrder.Length;
                patternIndexPhase1 = _phase1LoopOrder[loopIdx];
            }

            patternIndex = patternIndexPhase1;
            fullPattern = _phase1Patterns[patternIndexPhase1];
        }

        // 根据速度骰子数取前N张
        int cardCount = Math.Min(speedDiceCount, fullPattern.Length);

        SteriaLogger.Log($"神备EGO出牌: Phase{(_phase2 ? 2 : 1)} Round{_roundCount + 1} → 模式{patternIndex}, 速度骰子{speedDiceCount}, 出{cardCount}张");

        owner.allyCardDetail.ExhaustAllCards();
        int priority = 100;
        for (int i = 0; i < cardCount; i++)
        {
            int cardId = fullPattern[i];
            try
            {
                LorId lorId = new LorId(MOD_ID, cardId);
                BattleDiceCardModel card = owner.allyCardDetail.AddTempCard(lorId);
                if (card != null)
                {
                    card.SetCostToZero(true);
                    card.SetPriorityAdder(priority);
                    card.temporary = true;
                }
                priority -= 10;
            }
            catch (Exception ex)
            {
                SteriaLogger.LogError($"神备EGO出牌: 添加卡牌{cardId}失败: {ex.Message}");
            }
        }

        _roundCount++;
    }

    /// <summary>
    /// Phase转换检测 - 在受到伤害前减伤，防止boss在转阶段前被击杀
    /// Phase1时：如果伤害会让HP低于1，则减免到只剩1HP
    /// 转换中/Phase2过渡回合：锁定免伤
    /// </summary>
    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        if (_transitioning) return 9999;

        // 第一回合转阶段过渡：维持350HP并免疫伤害
        if (_phase2 && _phase2SecondTurnSetupPending)
        {
            return 9999;
        }

        if (!_phase2 && owner != null && owner.hp > 0)
        {
            // Phase1中，确保boss不会被直接击杀，至少保留1HP以触发转阶段
            int incomingDmg = (int)(behavior?.DiceResultValue ?? 0);
            if ((int)owner.hp - incomingDmg < 1)
            {
                return incomingDmg - ((int)owner.hp - 1);
            }
        }
        return 0;
    }

    public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
    {
        if (_phase2 && _phase2SecondTurnSetupPending && owner != null && dmg > 0)
        {
            if ((int)owner.hp != Phase2TransitionHp)
            {
                owner.SetHp(Phase2TransitionHp);
            }
            return true;
        }

        return false;
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        CheckPhaseTransition();
    }

    /// <summary>
    /// 也在回合结束时检查（防止被烧伤等非攻击伤害击杀）
    /// </summary>
    public override void OnRoundEnd()
    {
        base.OnRoundEnd();

        if (_phase2 && _phase2SecondTurnSetupPending && owner != null && !owner.IsDead())
        {
            if ((int)owner.hp != Phase2TransitionHp)
            {
                owner.SetHp(Phase2TransitionHp);
            }
            SteriaLogger.Log("神备EGO Phase2过渡回合: 维持HP=350，保留当前混乱状态");
        }

        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        if (_phase2 || _transitioning || owner == null) return;
        if (owner.hp <= 350)
        {
            _transitioning = true;
            EnterPhase2();
        }
    }

    private void EnterPhase2()
    {
        if (owner == null) return;
        _phase2 = true;
        _phase2SecondTurnSetupPending = true;
        SteriaLogger.Log("神备EGO: 进入Phase2 - 因为她要升起...");

        // 尝试同步书页HP（引擎中的MaxHp读取并不总是完全跟随，真实上限由9010003内核接管）
        try
        {
            owner.Book.SetHp(Phase2TransitionHp);
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"神备EGO Phase2: 设置MaxHP失败: {ex.Message}");
        }

        // 第一回合过渡：仅锁定350HP，保留当前混乱状态
        owner.SetHp(Phase2TransitionHp);
        SteriaLogger.Log("神备EGO Phase2过渡开始: 本回合锁定HP=350，混乱状态将于下一回合清除");

        // 添加Phase2标记
        owner.bufListDetail.AddBuf(new BattleUnitBuf_Phase2Active { stack = 1 });

        _transitioning = false;
        _roundCount = 0; // 重置出牌循环
    }

    private void FinalizePhase2SecondTurnSetup()
    {
        if (owner == null || owner.IsDead())
        {
            _phase2SecondTurnSetupPending = false;
            return;
        }

        // 第二回合开始：再次设置上限与当前血量
        try
        {
            owner.Book.SetHp(Phase2TransitionHp);
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"神备EGO Phase2第二回合: 设置MaxHP失败: {ex.Message}");
        }
        owner.SetHp(Phase2TransitionHp);

        // 第二回合开始：移除混乱状态
        try
        {
            BattleUnitBreakDetail breakDetail = owner.breakDetail;
            if (breakDetail != null)
            {
                int defaultBreakGauge = breakDetail.GetDefaultBreakGauge();
                breakDetail.RecoverBreakLife(owner.MaxBreakLife, false);
                breakDetail.RecoverBreak(defaultBreakGauge);
                breakDetail.nextTurnBreak = false;
            }

            owner.turnState = BattleUnitTurnState.WAIT_CARD;
            SteriaLogger.Log("神备EGO Phase2第二回合: 已重置HP/MaxHP并移除混乱状态");
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"神备EGO Phase2第二回合: 清除混乱状态失败: {ex.Message}");
        }

        // 第二回合开始：再激活隐藏被动
        foreach (var passive in owner.passiveDetail.PassiveList)
        {
            if (passive is PassiveAbility_9010003 p3) p3.Activate();
            if (passive is PassiveAbility_9010004 p4) p4.Activate();
            if (passive is PassiveAbility_9010012 p12) p12.Activate();
        }

        _phase2SecondTurnSetupPending = false;
    }
}

/// <summary>
/// 困于黑夜太久了吗... (ID: 9010002)
/// - 进攻型骰子拼点胜利：双方施加2层烧伤
/// - 防御型骰子拼点失败：双方施加2层烧伤
/// - 反击型骰子拼点失败：获得5层烧伤
/// </summary>
public class PassiveAbility_9010002 : PassiveAbilityBase
{
    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        base.OnWinParrying(behavior);
        if (behavior == null || owner == null) return;
        if (behavior.Type == BehaviourType.Atk)
        {
            // 双方施加2层烧伤
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
            BattleUnitModel target = behavior.card?.target;
            target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
        }
    }

    public override void OnLoseParrying(BattleDiceBehavior behavior)
    {
        base.OnLoseParrying(behavior);
        if (behavior == null || owner == null) return;
        if (behavior.Type == BehaviourType.Def)
        {
            // 防御型骰子拼点失败：双方施加2层烧伤
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
            BattleUnitModel target = behavior.card?.target;
            target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
        }
        else if (behavior.Type == BehaviourType.Standby) // 反击型
        {
            // 反击型骰子拼点失败：获得5层烧伤
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 5, owner);
        }
    }
}

/// <summary>
/// 因为她要升起... (ID: 9010003)
/// Phase2被动（初始隐藏）
/// - 承受致命伤害时：立刻回满当前最大生命值，最大生命值上限-25，并获得25层烧伤
/// - 所有单位受烧伤伤害时降低boss等量1/2最大生命值
/// - 最大生命值不高于1后接待结束
/// </summary>
public class PassiveAbility_9010003 : PassiveAbilityBase
{
    private const int Phase2InitialHpCap = 350;
    private const int FatalLockBurnStack = 25;
    private const int FatalLockHpCapReduction = 25;

    private bool _activated = false;
    private int _hpCap = Phase2InitialHpCap;

    public override bool isHide => !_activated;

    public void Activate()
    {
        _activated = true;
        _hpCap = Phase2InitialHpCap;
        ApplyHpCap(refillToCap: true, "激活");
        SteriaLogger.Log($"因为她要升起... 已激活 (HP上限={_hpCap})");
    }

    public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
    {
        if (!_activated || owner == null || dmg <= 0) return false;

        if (owner.hp - dmg < 1)
        {
            TriggerFatalLock("BeforeTakeDamage");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 致命触发逻辑通过BeforeTakeDamage处理；此处不再额外做1HP保底减伤，避免重复触发
    /// </summary>
    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        return 0;
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        EnsureHpStateAfterDamage();
    }

    public override void AfterTakeDamage(BattleUnitModel attacker, int dmg)
    {
        base.AfterTakeDamage(attacker, dmg);
        EnsureHpStateAfterDamage();
    }

    public override void OnLoseHp(int dmg)
    {
        base.OnLoseHp(dmg);
        EnsureHpStateAfterDamage();
    }

    public override int GetMinHp()
    {
        if (!_activated) return base.GetMinHp();
        return 1;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        if (!_activated || owner == null || owner.IsDead()) return;

        ApplyHpCap(refillToCap: false, "回合结束校准");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (!_activated || owner == null || owner.IsDead()) return;

        ApplyHpCap(refillToCap: false, "回合开始校准");
    }

    /// <summary>
    /// 当任何单位受到烧伤伤害时调用（由HarmonyPatch触发）
    /// 1. 触发日光的混乱伤害
    /// 2. 降低boss等量1/2的最大生命值上限
    /// </summary>
    public static void OnAnyUnitBurnDamage(BattleUnitModel unit, int burnDamage)
    {
        if (unit == null || burnDamage <= 0) return;

        SteriaLogger.Log($"OnAnyUnitBurnDamage: {unit.UnitData?.unitData?.name}, burnDamage={burnDamage}");

        // 触发日光效果
        BattleUnitBuf_Sunlight sunlight = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Sunlight) as BattleUnitBuf_Sunlight;
        if (sunlight != null && sunlight.stack > 0)
        {
            sunlight.OnBurnDamageDealt(burnDamage);
        }
        else
        {
            SteriaLogger.Log($"OnAnyUnitBurnDamage: {unit.UnitData?.unitData?.name} 无日光可触发");
        }

        // Phase2激活时：所有单位的烧伤伤害还会降低boss的最大生命值
        ReduceBossMaxHpByBurn(burnDamage);
    }

    /// <summary>
    /// 降低boss最大生命值（烧伤伤害的1/2，向下取整）
    /// </summary>
    private static void ReduceBossMaxHpByBurn(int burnDamage)
    {
        if (BattleObjectManager.instance == null || burnDamage <= 0) return;

        // 查找拥有此被动的boss
        foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Enemy))
        {
            if (unit == null) continue;
            var passive = unit.passiveDetail?.PassiveList?
                .FirstOrDefault(p => p is PassiveAbility_9010003) as PassiveAbility_9010003;
            if (passive != null && passive._activated)
            {
                int hpReduction = burnDamage / 2;
                if (hpReduction <= 0) return;

                passive.ReduceHpCap(hpReduction, $"烧伤{burnDamage}");
                break;
            }
        }
    }

    private int GetIncomingDamage(BattleDiceBehavior behavior)
    {
        if (behavior == null) return 0;

        int damage = behavior.DiceResultValue + behavior.DamageAdder;
        try
        {
            damage += behavior.owner?.UnitData?.unitData?.giftInventory?.GetStatBonus_Dmg(behavior.Detail) ?? 0;
        }
        catch
        {
        }

        return Math.Max(0, damage);
    }

    private void EnsureHpStateAfterDamage()
    {
        if (!_activated || owner == null || owner.IsDead()) return;

        if (owner.hp <= 1)
        {
            TriggerFatalLock("AfterDamage");
            return;
        }

        ApplyHpCap(refillToCap: false, "受伤后校准");
    }

    private void TriggerFatalLock(string source)
    {
        if (!_activated || owner == null || owner.IsDead()) return;

        ReduceHpCap(FatalLockHpCapReduction, $"{source}致命伤害");
        if (owner == null || owner.IsDead()) return;

        owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Burn, FatalLockBurnStack, owner);
        ApplyHpCap(refillToCap: true, $"{source}致命触发");
        SteriaLogger.Log($"因为她要升起: {source} 触发锁血，立刻回满HP，最大生命值上限-{FatalLockHpCapReduction}，并施加{FatalLockBurnStack}层烧伤");
    }

    private void ReduceHpCap(int amount, string reason)
    {
        if (!_activated || owner == null || owner.IsDead() || amount <= 0) return;

        int beforeCap = Math.Max(1, _hpCap);
        int afterCap = Math.Max(1, beforeCap - amount);
        _hpCap = afterCap;

        ApplyHpCap(refillToCap: false, reason);
        SteriaLogger.Log($"烧伤降低MaxHP: {beforeCap} → {afterCap} ({reason},减{amount})");

        if (_hpCap <= 1)
        {
            SteriaLogger.Log("因为她要升起: 最大生命值上限≤1,接待结束");
            owner.Die();
        }
    }

    private void ApplyHpCap(bool refillToCap, string reason)
    {
        if (owner == null || owner.IsDead()) return;

        _hpCap = Math.Max(1, _hpCap);

        try
        {
            owner.Book?.SetHp(_hpCap);
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"因为她要升起: 同步BookHP失败: {ex.Message}");
        }

        int hpBefore = (int)owner.hp;
        if (refillToCap)
        {
            owner.SetHp(_hpCap);
        }
        else
        {
            int clamped = Mathf.Clamp(hpBefore, 1, _hpCap);
            if (clamped != hpBefore)
            {
                owner.SetHp(clamped);
            }
        }

        int hpAfter = (int)owner.hp;
        if (hpBefore != hpAfter || refillToCap)
        {
            SteriaLogger.Log($"因为她要升起: {reason}, HP {hpBefore} → {hpAfter}, 上限={_hpCap}");
        }
    }
}

/// <summary>
/// 太阳就快要来了... (ID: 9010004)
/// Phase2被动（初始隐藏）
/// - 每幕开始时为所有单位施加烧伤（层数每幕递增）
/// - 每拥有25层烧伤获得1层强壮
/// </summary>
public class PassiveAbility_9010004 : PassiveAbilityBase
{
    private bool _activated = false;
    private int _burnPerRound = 0;

    public override bool isHide => !_activated;

    public void Activate()
    {
        _activated = true;
        _burnPerRound = 0;
        SteriaLogger.Log("太阳就快要来了... 已激活");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (!_activated || owner == null || BattleObjectManager.instance == null) return;

        _burnPerRound++;

        // 为所有单位施加烧伤
        foreach (var unit in BattleObjectManager.instance.GetAliveList(true))
        {
            if (unit == null) continue;
            unit.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Burn, _burnPerRound, owner);
        }

        SteriaLogger.Log($"太阳就快要来了: 第{_burnPerRound}幕,为所有单位施加{_burnPerRound}层烧伤");

        // 每拥有25层烧伤获得1层强壮
        int burnStacks = owner.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
        int strengthGain = burnStacks / 25;
        if (strengthGain > 0)
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, strengthGain, owner);
        }
    }
}

/// <summary>
/// 曙光庇护 (ID: 9010012)
/// Phase2被动（初始隐藏）
/// - 激活后不会陷入混乱
/// </summary>
public class PassiveAbility_9010012 : PassiveAbilityBase
{
    private bool _activated = false;
    public override bool isHide => !_activated;

    public void Activate()
    {
        _activated = true;
        RecoverBreakState("激活");
        SteriaLogger.Log("曙光庇护 已激活: 二阶段不会陷入混乱");
    }

    public override int GetBreakDamageReduction(BattleDiceBehavior behavior)
    {
        if (!_activated)
        {
            return base.GetBreakDamageReduction(behavior);
        }

        return 9999;
    }

    public override int GetBreakDamageReductionAll(int dmg, DamageType dmgType, BattleUnitModel attacker)
    {
        if (!_activated)
        {
            return base.GetBreakDamageReductionAll(dmg, dmgType, attacker);
        }

        return Math.Max(0, dmg);
    }

    public override void OnBreakState()
    {
        base.OnBreakState();
        if (!_activated)
        {
            return;
        }

        RecoverBreakState("OnBreakState");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (!_activated)
        {
            return;
        }

        RecoverBreakState("OnRoundStart");
    }

    private void RecoverBreakState(string reason)
    {
        if (!_activated || owner == null || owner.IsDead())
        {
            return;
        }

        BattleUnitBreakDetail breakDetail = owner.breakDetail;
        if (breakDetail == null)
        {
            return;
        }

        int defaultBreakGauge = breakDetail.GetDefaultBreakGauge();
        breakDetail.RecoverBreakLife(owner.MaxBreakLife, false);
        breakDetail.RecoverBreak(defaultBreakGauge);
        breakDetail.nextTurnBreak = false;
        owner.turnState = BattleUnitTurnState.WAIT_CARD;
        SteriaLogger.Log($"曙光庇护: 已校准混乱状态 ({reason})");
    }
}

/// <summary>
/// 福斯福洛斯之剑 (ID: 9010005)
/// 每次进攻型骰子时从以下效果中轮流选择：
/// 施加5层烧伤 / 施加1层致盲 / 施加1层日光
/// </summary>
public class PassiveAbility_9010005 : PassiveAbilityBase
{
    private int _cycleIndex = 0;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _cycleIndex = 0;
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);
        if (behavior == null || owner == null) return;
        if (behavior.Type != BehaviourType.Atk) return;

        BattleUnitModel target = behavior.card?.target;
        if (target == null) return;

        int effect = _cycleIndex % 3;
        _cycleIndex++;

        switch (effect)
        {
            case 0: // 施加5层烧伤
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 5, owner);
                break;
            case 1: // 施加1层致盲
                ChristashaEgoBossHelper.AddBlinding(target, 1, owner);
                break;
            case 2: // 施加1层日光
                ChristashaEgoBossHelper.AddSunlight(target, 1, owner);
                break;
        }
    }
}

/// <summary>
/// 斯蒂芬妮之冠 (ID: 9010006)
/// 每一幕开始时随机获得增益（受潮影响）
/// 返还上一幕消耗的潮层数的一半
/// </summary>
public class PassiveAbility_9010006 : PassiveAbilityBase
{
    private int _lastTideConsumed = 0;

    public void OnTideConsumed(int amount)
    {
        _lastTideConsumed += amount;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (owner == null) return;

        // 返还上一幕消耗潮的一半
        int returnAmount = _lastTideConsumed / 2;
        if (returnAmount > 0)
        {
            PassiveAbility_9004001.AddTideStacks(owner, returnAmount);
            SteriaLogger.Log($"斯蒂芬妮之冠: 返还{returnAmount}层潮");
        }
        _lastTideConsumed = 0;

        // 随机增益
        int roll = UnityEngine.Random.Range(0, 3);
        switch (roll)
        {
            case 0: // 1层强壮 + 3层流
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, owner);
                BattleUnitBuf_Flow flow = owner.bufListDetail.GetActivatedBufList()
                    .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
                if (flow != null) flow.stack += 3;
                else owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 3 });
                break;
            case 1: // 1层坚韧 + 3层梦
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 1, owner);
                BattleUnitBuf_Dream dreamBuf = owner.bufListDetail.GetActivatedBufList()
                    .FirstOrDefault(b => b is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
                if (dreamBuf != null) dreamBuf.stack += 3;
                else
                {
                    var newDream = new BattleUnitBuf_Dream { stack = 3 };
                    owner.bufListDetail.AddBuf(newDream);
                }
                break;
            case 2: // 1层守护 + 振奋(混乱抗性伤害-1) + 3层潮
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 1, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.BreakProtection, 1, owner);
                PassiveAbility_9004001.AddTideStacks(owner, 3);
                break;
        }
    }
}

#endregion
