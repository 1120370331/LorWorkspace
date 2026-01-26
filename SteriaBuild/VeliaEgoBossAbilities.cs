using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;
using LOR_DiceSystem;

// 薇莉亚EGO BOSS的被动技能、Buff和卡牌能力实现
// 这些类需要在全局命名空间中，以便游戏能够找到

/// <summary>
/// 荆棘Buff - 薇莉亚EGO BOSS的核心机制
/// 被【潮】影响的【负面效果】
/// 每一幕开始时，施加荆棘层数层【束缚】和【流血】
/// 攻击时移除1层（单方面攻击时立即移除）
/// </summary>
public class BattleUnitBuf_Thorn : BattleUnitBuf
{
    protected override string keywordId => "SteriaThorn";
    protected override string keywordIconId => "SteriaThorn";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_Thorn: Init for {owner?.UnitData?.unitData?.name} with {stack} stacks");
    }

    /// <summary>
    /// 每一幕开始时，施加荆棘层数层束缚和流血（本幕生效）
    /// </summary>
    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (_owner == null || _owner.IsDead() || stack <= 0) return;

        // 施加荆棘层数层束缚（本幕生效）
        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Binding, stack, null);
        SteriaLogger.Log($"Thorn: Applied {stack} Binding (this round) to {_owner.UnitData?.unitData?.name}");

        // 施加荆棘层数层流血（本幕生效）
        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, stack, null);
        SteriaLogger.Log($"Thorn: Applied {stack} Bleeding (this round) to {_owner.UnitData?.unitData?.name}");
    }

    /// <summary>
    /// 发起单方面攻击时移除1层荆棘
    /// </summary>
    public void OnOneSidedAttack()
    {
        if (stack > 0)
        {
            stack--;
            SteriaLogger.Log($"Thorn: Removed 1 stack due to one-sided attack, remaining: {stack}");

            if (stack <= 0)
            {
                this.Destroy();
            }
        }
    }

    /// <summary>
    /// 获取当前荆棘层数
    /// </summary>
    public int GetThornStacks()
    {
        return stack;
    }

    /// <summary>
    /// 辅助方法：为目标添加荆棘层数
    /// giver 参数用于潮加成（在 HarmonyPatches 中处理）
    /// </summary>
    public static void AddThornStacks(BattleUnitModel target, int amount, BattleUnitModel giver = null)
    {
        if (target == null || amount <= 0) return;

        // 潮加成在 HarmonyPatches.CheckAndConsumeTideForThorn 中处理
        int finalAmount = amount;
        if (giver != null)
        {
            finalAmount += Steria.HarmonyHelpers.CheckAndConsumeTideForThorn(giver);
        }

        BattleUnitBuf_Thorn existingThorn = target.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Thorn) as BattleUnitBuf_Thorn;

        if (existingThorn != null && !existingThorn.IsDestroyed())
        {
            existingThorn.stack += finalAmount;
            SteriaLogger.Log($"AddThornStacks: Updated existing Thorn. New stack: {existingThorn.stack}");
        }
        else
        {
            BattleUnitBuf_Thorn newThorn = new BattleUnitBuf_Thorn();
            newThorn.stack = finalAmount;
            target.bufListDetail.AddBuf(newThorn);
            SteriaLogger.Log($"AddThornStacks: Created new Thorn buff with {finalAmount} stacks");
        }
    }

    /// <summary>
    /// 获取目标的荆棘层数
    /// </summary>
    public static int GetThornStacksOf(BattleUnitModel target)
    {
        if (target == null) return 0;

        BattleUnitBuf_Thorn thorn = target.bufListDetail?.GetActivatedBufList()
            ?.FirstOrDefault(b => b is BattleUnitBuf_Thorn) as BattleUnitBuf_Thorn;

        return thorn?.stack ?? 0;
    }
}

/// <summary>
/// 侵蚀 (ID: 9005001)
/// 每一幕开始时将意图使用的书页置入手中，其光芒消耗为0
/// 出牌顺序表循环
/// </summary>
public class PassiveAbility_9005001 : PassiveAbilityBase
{
    private int _roundCount = 0;
    private static readonly string MOD_ID = "SteriaBuilding";

    // 卡牌ID
    private static readonly int CARD_ZHIDU = 9005001;        // 止渡
    private static readonly int CARD_NINGSHI = 9005002;      // 凝视
    private static readonly int CARD_CHUANCI = 9005003;      // 穿刺
    private static readonly int CARD_QINGZHUSHIWO = 9005004; // 请注视我
    private static readonly int CARD_JINGJIZHICHAO = 9005005; // 荆棘之潮
    private static readonly int CARD_MANYANZENGSHENG = 9005006; // 蔓延增生

    // 出牌顺序表（每行6张卡）
    // 荆棘之潮 | 止渡 | 凝视 | 止渡 | 穿刺 | 凝视
    // 蔓延增生 | 凝视 | 请注视我 | 止渡 | 凝视 | 穿刺
    // 穿刺 | 请注视我 | 穿刺 | 止渡 | 请注视我 | 凝视
    // 凝视 | 穿刺 | 止渡 | 请注视我 | 凝视 | 穿刺
    private static readonly int[][] _patterns = new int[][]
    {
        new int[] { CARD_JINGJIZHICHAO, CARD_ZHIDU, CARD_NINGSHI, CARD_ZHIDU, CARD_CHUANCI, CARD_NINGSHI },
        new int[] { CARD_MANYANZENGSHENG, CARD_NINGSHI, CARD_QINGZHUSHIWO, CARD_ZHIDU, CARD_NINGSHI, CARD_CHUANCI },
        new int[] { CARD_CHUANCI, CARD_QINGZHUSHIWO, CARD_CHUANCI, CARD_ZHIDU, CARD_QINGZHUSHIWO, CARD_NINGSHI },
        new int[] { CARD_NINGSHI, CARD_CHUANCI, CARD_ZHIDU, CARD_QINGZHUSHIWO, CARD_NINGSHI, CARD_CHUANCI },
    };

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _roundCount = 0;
        SteriaLogger.Log($"PassiveAbility_9005001 (侵蚀): Init for {self?.UnitData?.unitData?.name}");
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

        // 获取当前回合的出牌模式（循环）
        int patternIndex = _roundCount % _patterns.Length;
        int[] cards = _patterns[patternIndex];

        SteriaLogger.Log($"侵蚀: Round {_roundCount + 1}, Pattern {patternIndex + 1}, Adding {cards.Length} cards");

        // 消耗所有卡牌
        owner.allyCardDetail.ExhaustAllCards();

        // 将卡牌置入手牌
        int priority = 100;
        foreach (int cardId in cards)
        {
            AddNewCard(cardId, priority);
            priority -= 10;
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
                SteriaLogger.Log($"侵蚀: Added temp card {cardId} with cost 0, priority {priorityAdder}");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"侵蚀: Failed to add card {cardId}: {ex.Message}");
        }
    }
}

/// <summary>
/// 渗透天堂 (ID: 9005002)
/// 受到反震伤害x2，造成反震伤害x2，且造成反震伤害时造成等比伤害
/// </summary>
public class PassiveAbility_9005002 : PassiveAbilityBase
{
    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        SteriaLogger.Log($"PassiveAbility_9005002 (渗透天堂): Init for {self?.UnitData?.unitData?.name}");
    }

    /// <summary>
    /// 受到反震伤害x2
    /// </summary>
    public override int GetTakenGuardBreakDamageAdder(int dmg)
    {
        return dmg; // 返回dmg意味着受到的反震伤害翻倍
    }

    /// <summary>
    /// 造成反震伤害x2 - 设置GuardBreakMultiplier
    /// </summary>
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        // 只对防御型骰子设置反震倍率
        if (behavior.Detail == BehaviourDetail.Guard || behavior.Detail == BehaviourDetail.Evasion)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { guardBreakMultiplier = 2 });
        }
    }

    /// <summary>
    /// 造成反震伤害时造成等比伤害 + 特效
    /// 注意：反震伤害只发生在防御型骰子或近战骰子拼点胜利时
    /// </summary>
    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        base.OnWinParrying(behavior);

        if (behavior?.TargetDice?.owner == null || owner == null) return;

        // 检查是否会造成反震伤害
        // 反震伤害只发生在：我方防御型骰子拼点胜利时
        bool isMyDiceDefense = behavior.Detail == BehaviourDetail.Guard ||
                               behavior.Detail == BehaviourDetail.Evasion;

        // 如果我方不是防御型骰子，直接返回
        if (!isMyDiceDefense)
        {
            return;
        }

        BattleUnitModel target = behavior.TargetDice.owner;
        if (target.IsDead()) return;

        // 计算反震伤害（手动实现2倍）
        int myDiceValue = behavior.DiceResultValue;
        int targetDiceValue = 0;
        BehaviourDetail targetDetail = behavior.TargetDice.Detail;
        if (targetDetail == BehaviourDetail.Slash || targetDetail == BehaviourDetail.Penetrate || targetDetail == BehaviourDetail.Hit)
        {
            targetDiceValue = behavior.TargetDice.DiceResultValue;
        }

        // 基础反震伤害 * 2（渗透天堂效果）
        int baseCounterDamage = myDiceValue - targetDiceValue + behavior.GuardBreakAdder;
        int counterDamage = baseCounterDamage * 2;
        counterDamage += target.GetTakenGuardBreakDamageAdder(counterDamage);

        if (counterDamage > 0)
        {
            // 造成等比的普通伤害
            target.TakeDamage(counterDamage, DamageType.Passive, owner);
            SteriaLogger.Log($"渗透天堂: Dealt additional {counterDamage} damage to {target.UnitData?.unitData?.name}");

            // 播放特效
            owner.battleCardResultLog?.SetCreatureAbilityEffect("9/HokmaFirst_Guard", 0.8f);
            owner.battleCardResultLog?.SetCreatureEffectSound("Creature/SnowWhite_NormalAtk");
        }
    }
}

/// <summary>
/// 渴望的注视 (ID: 9005003)
/// 若战斗结束时未被单方面攻击，则下一幕开始时对所有敌人造成25点突刺伤害并施加1层荆棘
/// </summary>
public class PassiveAbility_9005003 : PassiveAbilityBase
{
    private bool _wasOneSidedAttackedThisRound = false;
    private bool _shouldPunishNextRound = false;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _wasOneSidedAttackedThisRound = false;
        _shouldPunishNextRound = false;
        SteriaLogger.Log($"PassiveAbility_9005003 (渴望的注视): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _wasOneSidedAttackedThisRound = false;
        _shouldPunishNextRound = false;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        // 如果上回合未被单方面攻击，惩罚所有敌人
        if (_shouldPunishNextRound && owner != null)
        {
            PunishAllEnemies();
        }

        _wasOneSidedAttackedThisRound = false;
        _shouldPunishNextRound = false;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();

        // 检查本回合是否被单方面攻击
        if (!_wasOneSidedAttackedThisRound)
        {
            _shouldPunishNextRound = true;
            SteriaLogger.Log($"渴望的注视: {owner?.UnitData?.unitData?.name} was not one-sided attacked, will punish next round");
        }
    }

    /// <summary>
    /// 当被攻击时调用
    /// 单方面攻击或反击骰子命中时算被攻击
    /// </summary>
    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);

        if (atkDice?.card == null || owner == null) return;

        BattleUnitModel attacker = atkDice.owner;
        if (attacker == null) return;

        // 检查薇莉亚是否有卡牌指向攻击者
        bool veliaHasCardToAttacker = false;
        int opponentSlotOrder = atkDice.card.slotOrder;

        foreach (var playingCard in owner.cardSlotDetail.cardAry)
        {
            if (playingCard != null && playingCard.target == attacker &&
                playingCard.slotOrder == opponentSlotOrder)
            {
                veliaHasCardToAttacker = true;
                break;
            }
        }

        // 检查攻击者是否有卡牌指向薇莉亚（判断是否是反击骰子）
        bool attackerHasCardToVelia = false;
        foreach (var playingCard in attacker.cardSlotDetail.cardAry)
        {
            if (playingCard != null && playingCard.target == owner)
            {
                attackerHasCardToVelia = true;
                break;
            }
        }

        // 单方面攻击：薇莉亚没有卡牌指向攻击者
        // 反击骰子：攻击者没有卡牌指向薇莉亚
        if (!veliaHasCardToAttacker || !attackerHasCardToVelia)
        {
            _wasOneSidedAttackedThisRound = true;
            SteriaLogger.Log($"渴望的注视: {owner.UnitData?.unitData?.name} was attacked (one-sided or counter)");
        }
    }

    private void PunishAllEnemies()
    {
        if (BattleObjectManager.instance == null) return;

        // 播放红色血丝闪烁特效
        CameraFilterUtil.RedColorFilter(-0.3f, 0.7f, 0.2f, 0.3f, 0.2f);
        // 播放振动特效
        CameraFilterUtil.EarthQuake(0.1f, 0.1f, 30f, 0.5f);

        Faction enemyFaction = owner.faction == Faction.Player ? Faction.Enemy : Faction.Player;
        var enemies = BattleObjectManager.instance.GetAliveList(enemyFaction);

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead()) continue;

            // 造成25点突刺伤害
            enemy.TakeDamage(25, DamageType.Passive, owner);

            // 施加3层荆棘（支持潮加成）
            BattleUnitBuf_Thorn.AddThornStacks(enemy, 3, owner);

            SteriaLogger.Log($"渴望的注视: Punished {enemy.UnitData?.unitData?.name} with 25 damage and 3 Thorn");
        }
    }
}

/// <summary>
/// 荆棘之路 (ID: 9005004)
/// 免疫流血、束缚
/// 命中拥有"荆棘"的单位时下回合获得1层强壮和忍耐
/// 与进攻型骰子拼点时：对方每有一层荆棘便使其威力-1
/// </summary>
public class PassiveAbility_9005004 : PassiveAbilityBase
{
    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        SteriaLogger.Log($"PassiveAbility_9005004 (荆棘之路): Init for {self?.UnitData?.unitData?.name}");
    }

    /// <summary>
    /// 免疫流血和束缚
    /// </summary>
    public override bool IsImmune(KeywordBuf buf)
    {
        // 免疫流血
        if (buf == KeywordBuf.Bleeding)
        {
            SteriaLogger.Log($"荆棘之路: Immune to Bleeding");
            return true;
        }

        // 免疫束缚
        if (buf == KeywordBuf.Binding)
        {
            SteriaLogger.Log($"荆棘之路: Immune to Binding");
            return true;
        }

        return base.IsImmune(buf);
    }

    /// <summary>
    /// 命中拥有荆棘的单位时获得强壮和忍耐
    /// </summary>
    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);

        if (behavior?.card?.target == null || owner == null) return;

        BattleUnitModel target = behavior.card.target;
        int thornStacks = BattleUnitBuf_Thorn.GetThornStacksOf(target);

        if (thornStacks > 0)
        {
            owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, owner);
            owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, 1, owner);
            SteriaLogger.Log($"荆棘之路: Hit target with Thorn, gained 1 Strength and 1 Endurance");
        }
    }

    /// <summary>
    /// 与进攻型骰子拼点时：对方每有一层荆棘便使其威力-1
    /// </summary>
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);

        if (behavior?.TargetDice == null || owner == null) return;

        // 检查对方骰子是否是进攻型
        BattleDiceBehavior targetDice = behavior.TargetDice;
        if (!IsAttackDice(targetDice.Detail)) return;

        // 获取对方的荆棘层数
        BattleUnitModel opponent = targetDice.owner;
        if (opponent == null) return;

        int thornStacks = BattleUnitBuf_Thorn.GetThornStacksOf(opponent);
        if (thornStacks > 0)
        {
            targetDice.ApplyDiceStatBonus(new DiceStatBonus { power = -thornStacks });
            SteriaLogger.Log($"荆棘之路: Reduced opponent's attack dice power by {thornStacks}");
        }
    }

    private bool IsAttackDice(BehaviourDetail detail)
    {
        return detail == BehaviourDetail.Slash ||
               detail == BehaviourDetail.Penetrate ||
               detail == BehaviourDetail.Hit;
    }
}

/// <summary>
/// 速战速决4 (ID: 9005005)
/// 初始拥有4颗速度骰子，情感等级达到3后额外获得1颗
/// </summary>
public class PassiveAbility_9005005 : PassiveAbilityBase
{
    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        SteriaLogger.Log($"PassiveAbility_9005005 (速战速决4): Init for {self?.UnitData?.unitData?.name}");
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        UpdateSpeedDice();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        UpdateSpeedDice();
    }

    private void UpdateSpeedDice()
    {
        if (owner?.Book == null) return;

        int baseDice = 4;
        int emotionLevel = owner.emotionDetail?.EmotionLevel ?? 0;

        // 情感等级达到3后额外获得1颗
        if (emotionLevel >= 3)
        {
            baseDice = 5;
        }

        owner.Book.SetSpeedDiceNum(baseDice);
        SteriaLogger.Log($"速战速决4: Set speed dice to {baseDice} (emotion level: {emotionLevel})");
    }
}

// ==================== 卡牌能力 ====================

/// <summary>
/// 止渡 - 骰子能力
/// 拼点失败：如果对方是进攻型骰子则施加5层流血和2层虚弱
/// </summary>
public class DiceCardAbility_VeliaEgo_ZhiDu : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        base.OnLoseParrying();

        // 使用与nosferatu_guard相同的方式获取对方骰子
        BattleDiceBehavior targetDice = behavior?.card?.target?.currentDiceAction?.currentBehavior;
        if (targetDice == null) return;

        BattleUnitModel target = targetDice.owner;
        if (target == null || target.IsDead()) return;

        // 检查对方是否是进攻型骰子
        if (IsAttackDice(targetDice.Detail))
        {
            // 施加5层流血
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 5, owner);

            // 施加2层虚弱
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Weak, 2, owner);

            // 播放特效
            owner?.battleCardResultLog?.SetCreatureAbilityEffect("9/HokmaFirst_Guard", 0.5f);

            SteriaLogger.Log($"止渡: Clash lose - applied 5 Bleeding and 2 Weak to {target.UnitData?.unitData?.name}");
        }
    }

    private bool IsAttackDice(BehaviourDetail detail)
    {
        return detail == BehaviourDetail.Slash ||
               detail == BehaviourDetail.Penetrate ||
               detail == BehaviourDetail.Hit;
    }
}

/// <summary>
/// 凝视 - 卡牌使用时能力
/// 使用时：使敌人的所有混乱抗性在本回合中变为"致命"
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_NingShi : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();

        if (card?.target == null) return;

        BattleUnitModel target = card.target;

        // 添加临时Buff来修改混乱抗性（回合结束后恢复）
        BattleUnitBuf_VeliaGaze existingBuf = target.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_VeliaGaze) as BattleUnitBuf_VeliaGaze;

        if (existingBuf == null)
        {
            BattleUnitBuf_VeliaGaze gazeBuf = new BattleUnitBuf_VeliaGaze();
            target.bufListDetail.AddBuf(gazeBuf);
            SteriaLogger.Log($"凝视: Applied Gaze debuff to {target.UnitData?.unitData?.name}");
        }
    }
}

/// <summary>
/// 凝视Buff - 使所有混乱抗性变为致命，回合结束后恢复
/// </summary>
public class BattleUnitBuf_VeliaGaze : BattleUnitBuf
{
    protected override string keywordId => "VeliaGaze";
    protected override string keywordIconId => "VeliaGaze";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        this.stack = 1;

        // 设置所有混乱抗性为致命
        owner.Book.SetResistBP(BehaviourDetail.Slash, AtkResist.Weak);
        owner.Book.SetResistBP(BehaviourDetail.Penetrate, AtkResist.Weak);
        owner.Book.SetResistBP(BehaviourDetail.Hit, AtkResist.Weak);

        SteriaLogger.Log($"VeliaGaze: Set all break resist to Weak for {owner.UnitData?.unitData?.name}");
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        this.Destroy();
    }

    public override void Destroy()
    {
        // 恢复原始抗性
        _owner?.Book?.SetOriginalResists();
        SteriaLogger.Log($"VeliaGaze: Restored original resists for {_owner?.UnitData?.unitData?.name}");
        base.Destroy();
    }
}

/// <summary>
/// 凝视 - 骰子能力
/// 对方每有1层荆棘便造成1点额外伤害
/// </summary>
public class DiceCardAbility_VeliaEgo_NingShiDice : DiceCardAbilityBase
{
    public override void BeforeGiveDamage()
    {
        base.BeforeGiveDamage();

        if (card?.target == null) return;

        int thornStacks = BattleUnitBuf_Thorn.GetThornStacksOf(card.target);
        if (thornStacks > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmg = thornStacks });
            SteriaLogger.Log($"凝视: Added {thornStacks} bonus damage from Thorn");
        }
    }
}

/// <summary>
/// 穿刺 - 卡牌使用时能力
/// 与速度高于自身的对象拼点时使本书页骰子威力-10
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_ChuanCi : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();

        if (card?.target == null || owner == null) return;

        // 获取自身使用这张卡的速度骰子值
        int mySpeed = card.speedDiceResultValue;

        // 获取目标在对应槽位的速度骰子值
        int targetSpeed = 0;
        if (card.targetSlotOrder >= 0)
        {
            var targetSpeedDice = card.target.GetSpeedDiceResult(card.targetSlotOrder);
            if (targetSpeedDice != null)
            {
                targetSpeed = targetSpeedDice.value;
            }
        }

        // 如果目标速度高于自身，骰子威力-10
        if (targetSpeed > mySpeed)
        {
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = -10 });
            SteriaLogger.Log($"穿刺: Target speed ({targetSpeed}) > my speed ({mySpeed}), dice power -10");
        }
    }
}

/// <summary>
/// 弱化穿刺 - 卡牌使用时能力
/// 与速度高于自身的对象拼点时使本书页骰子威力-5
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_WeakChuanCi : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();

        if (card?.target == null || owner == null) return;

        int mySpeed = card.speedDiceResultValue;
        int targetSpeed = 0;
        if (card.targetSlotOrder >= 0)
        {
            var targetSpeedDice = card.target.GetSpeedDiceResult(card.targetSlotOrder);
            if (targetSpeedDice != null)
            {
                targetSpeed = targetSpeedDice.value;
            }
        }

        if (targetSpeed > mySpeed)
        {
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = -5 });
            SteriaLogger.Log($"弱化穿刺: Target speed ({targetSpeed}) > my speed ({mySpeed}), dice power -5");
        }
    }
}

/// <summary>
/// 穿刺 - 骰子1能力
/// 命中时：下回合封印对方一颗速度骰子
/// </summary>
public class DiceCardAbility_VeliaEgo_ChuanCiDice1 : DiceCardAbilityBase
{
    public override string[] Keywords => new string[] { "Butterfly_Seal" };

    public override void OnSucceedAttack()
    {
        BattleUnitModel target = card?.target;
        if (target == null || target.IsDead()) return;

        // 添加封印buff（下回合生效）
        target.bufListDetail.AddBuf(new BattleUnitBuf_VeliaSeal());
        SteriaLogger.Log($"穿刺: Added Seal (next round) to {target.UnitData?.unitData?.name}");
    }
}

/// <summary>
/// 薇莉亚封印Buff - 封印1颗速度骰子，下回合生效
/// </summary>
public class BattleUnitBuf_VeliaSeal : BattleUnitBuf
{
    private bool _activated = false;

    protected override string keywordId => "Butterfly_Seal";
    protected override string keywordIconId => "Stun";
    public override KeywordBuf bufType => KeywordBuf.SealKeyword;
    public override bool independentBufIcon => true;
    public override int paramInBufDesc => 1;
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _activated = true;
    }

    public override int SpeedDiceBreakedAdder()
    {
        return _activated ? 1 : 0;
    }

    public override void OnRoundEnd()
    {
        if (_activated)
        {
            this.Destroy();
        }
        else
        {
            _activated = true;
        }
    }
}

/// <summary>
/// 请注视我 - 卡牌使用时能力
/// 战斗开始时：若对方在本回合中攻击非本单位目标，则对其造成15点突刺伤害+突刺混乱伤害
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_QingZhuShiWo : DiceCardSelfAbilityBase
{
    public override void OnStartBattle()
    {
        base.OnStartBattle();

        if (card?.target == null || owner == null) return;

        // 添加一个Buff来监控对方的攻击目标
        BattleUnitBuf_WatchingYou watchBuf = new BattleUnitBuf_WatchingYou();
        watchBuf.SetWatcher(owner);
        card.target.bufListDetail.AddBuf(watchBuf);

        SteriaLogger.Log($"请注视我: Watching {card.target.UnitData?.unitData?.name}");
    }
}

/// <summary>
/// 监视Buff - 用于请注视我效果
/// </summary>
public class BattleUnitBuf_WatchingYou : BattleUnitBuf
{
    private BattleUnitModel _watcher;
    private bool _hasPunished = false;

    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public void SetWatcher(BattleUnitModel watcher)
    {
        _watcher = watcher;
    }

    public override void OnUseCard(BattlePlayingCardDataInUnitModel curCard)
    {
        base.OnUseCard(curCard);

        if (_hasPunished || _watcher == null || _watcher.IsDead()) return;

        // 检查攻击目标是否不是监视者
        if (curCard?.target != null && curCard.target != _watcher)
        {
            _hasPunished = true;

            // 造成15点突刺伤害
            _owner.TakeDamage(15, DamageType.Buf, _watcher);

            // 获取目标对突刺的混乱抗性，然后造成15点突刺混乱伤害
            AtkResist penetrateResist = _owner.GetResistBP(BehaviourDetail.Penetrate);
            _owner.TakeBreakDamage(15, DamageType.Buf, _watcher, penetrateResist);

            // 施加2层荆棘
            BattleUnitBuf_Thorn.AddThornStacks(_owner, 2, _watcher);

            SteriaLogger.Log($"请注视我: Punished {_owner.UnitData?.unitData?.name} - 15 damage, 15 break damage (resist: {penetrateResist}), 2 Thorn");
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        this.Destroy();
    }
}

/// <summary>
/// 请注视我 - 骰子3能力
/// 命中时：使目标获得3层荆棘
/// </summary>
public class DiceCardAbility_VeliaEgo_QingZhuShiWoDice3 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();

        if (card?.target == null) return;

        BattleUnitBuf_Thorn.AddThornStacks(card.target, 1, owner);
        SteriaLogger.Log($"请注视我: Added 1 Thorn to {card.target.UnitData?.unitData?.name}");
    }
}

/// <summary>
/// 荆棘之潮 - 卡牌使用时能力
/// 战斗开始时：对所有没有荆棘的敌人施加1层荆棘
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_JingJiZhiChao : DiceCardSelfAbilityBase
{
    public override void OnStartBattle()
    {
        base.OnStartBattle();

        if (owner == null || BattleObjectManager.instance == null) return;

        Faction enemyFaction = owner.faction == Faction.Player ? Faction.Enemy : Faction.Player;
        var enemies = BattleObjectManager.instance.GetAliveList(enemyFaction);

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead()) continue;

            int thornStacks = BattleUnitBuf_Thorn.GetThornStacksOf(enemy);
            if (thornStacks <= 0)
            {
                BattleUnitBuf_Thorn.AddThornStacks(enemy, 1, owner);
                SteriaLogger.Log($"荆棘之潮: Added 1 Thorn to {enemy.UnitData?.unitData?.name}");
            }
        }
    }
}

/// <summary>
/// 荆棘之潮 - 骰子1能力
/// 命中时：施加3层荆棘
/// 注意：这是范围攻击，需要使用OnSucceedAreaAttack
/// </summary>
public class DiceCardAbility_VeliaEgo_JingJiZhiChaoDice1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();

        if (card?.target == null) return;

        BattleUnitBuf_Thorn.AddThornStacks(card.target, 3, owner);
        SteriaLogger.Log($"荆棘之潮: Added 3 Thorn to {card.target.UnitData?.unitData?.name}");
    }

    // 范围攻击时对每个目标施加荆棘
    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        base.OnSucceedAreaAttack(target);

        if (target == null) return;

        BattleUnitBuf_Thorn.AddThornStacks(target, 3, owner);
        SteriaLogger.Log($"荆棘之潮(范围): Added 3 Thorn to {target.UnitData?.unitData?.name}");
    }
}

// ==================== 蔓延增生 & 荆棘幻影 ====================

/// <summary>
/// 蔓延增生 - 卡牌使用时能力
/// 使用时：下一幕召唤一个"荆棘幻影"
/// </summary>
public class DiceCardSelfAbility_VeliaEgo_ManYanZengSheng : DiceCardSelfAbilityBase
{
    private static readonly string MOD_ID = "SteriaBuilding";

    public override void OnUseCard()
    {
        base.OnUseCard();
        if (owner == null) return;

        // 添加一个Buff，下一幕召唤荆棘幻影
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SummonThornPhantom());
        SteriaLogger.Log($"蔓延增生: Will summon Thorn Phantom next round");
    }
}

/// <summary>
/// 蔓延增生 - 骰子1能力
/// 命中时：施加1层荆棘
/// </summary>
public class DiceCardAbility_VeliaEgo_ManYanDice1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        if (card?.target == null) return;

        BattleUnitBuf_Thorn.AddThornStacks(card.target, 1, owner);
        SteriaLogger.Log($"蔓延增生: Added 1 Thorn to {card.target.UnitData?.unitData?.name}");
    }
}

/// <summary>
/// 召唤荆棘幻影Buff - 下一幕召唤荆棘幻影
/// </summary>
public class BattleUnitBuf_SummonThornPhantom : BattleUnitBuf
{
    private static readonly string MOD_ID = "SteriaBuilding";
    private static readonly int PHANTOM_ENEMY_ID = 6;

    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        SummonPhantom();
        this.Destroy();
    }

    private void SummonPhantom()
    {
        if (_owner == null) return;

        try
        {
            int index = BattleObjectManager.instance.GetAliveList(_owner.faction).Count;
            LorId enemyId = new LorId(MOD_ID, PHANTOM_ENEMY_ID);
            BattleUnitModel phantom = Singleton<StageController>.Instance.AddNewUnit(
                _owner.faction,
                enemyId,
                index,
                -1
            );

            if (phantom != null)
            {
                SteriaLogger.Log($"SummonThornPhantom: Summoned Thorn Phantom with LorId {MOD_ID}:{PHANTOM_ENEMY_ID}");

                // 立即给薇莉亚buff（因为召唤时OnRoundStart已经过了）
                GiveVeliaBuffsImmediately(phantom);
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"SummonThornPhantom: Failed - {ex.Message}");
        }
    }

    private void GiveVeliaBuffsImmediately(BattleUnitModel phantom)
    {
        if (phantom == null || BattleObjectManager.instance == null) return;

        // 找到薇莉亚（同阵营，有侵蚀被动9005001的单位）
        foreach (var ally in BattleObjectManager.instance.GetAliveList(phantom.faction))
        {
            if (ally == phantom || ally.IsDead()) continue;

            bool isVelia = false;
            if (ally.passiveDetail?.PassiveList != null)
            {
                foreach (var passive in ally.passiveDetail.PassiveList)
                {
                    if (passive is PassiveAbility_9005001)
                    {
                        isVelia = true;
                        break;
                    }
                }
            }

            if (isVelia)
            {
                ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 4, phantom);
                ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.BreakProtection, 4, phantom);
                SteriaLogger.Log($"SummonThornPhantom: Immediately gave 4 Protection and 4 BreakProtection to {ally.UnitData?.unitData?.name}");
                break;
            }
        }
    }
}

/// <summary>
/// 变幻莫测 (ID: 9005006) - 荆棘幻影
/// 每一幕开始时从牌库随机抽取=速度骰子数量的牌，光芒消耗为0
/// </summary>
public class PassiveAbility_9005006 : PassiveAbilityBase
{
    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (owner == null) return;

        // 获取速度骰子数量
        int speedDiceCount = owner.speedDiceCount;

        // 抽取等于速度骰子数量的牌
        owner.allyCardDetail.DrawCards(speedDiceCount);

        // 将手牌中的所有牌设为0费
        foreach (var card in owner.allyCardDetail.GetHand())
        {
            card?.SetCostToZero(true);
        }

        SteriaLogger.Log($"变幻莫测: Drew {speedDiceCount} cards with 0 cost");
    }
}

/// <summary>
/// 渴望的注视-初 (ID: 9005007) - 弱化版
/// 15点伤害而非25点
/// </summary>
public class PassiveAbility_9005007 : PassiveAbilityBase
{
    private bool _wasOneSidedAttackedThisRound = false;
    private bool _shouldPunishNextRound = false;

    public override void OnWaveStart() { base.OnWaveStart(); _wasOneSidedAttackedThisRound = false; _shouldPunishNextRound = false; }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_shouldPunishNextRound && owner != null)
        {
            Faction enemyFaction = owner.faction == Faction.Player ? Faction.Enemy : Faction.Player;
            foreach (var enemy in BattleObjectManager.instance.GetAliveList(enemyFaction))
            {
                if (!enemy.IsDead())
                {
                    enemy.TakeDamage(15, DamageType.Passive, owner);
                    BattleUnitBuf_Thorn.AddThornStacks(enemy, 1, owner);
                }
            }
        }
        _wasOneSidedAttackedThisRound = false;
        _shouldPunishNextRound = false;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        if (!_wasOneSidedAttackedThisRound) _shouldPunishNextRound = true;
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        if (atkDice?.card == null || owner == null) return;

        bool isOneSided = true;
        int opponentSlotOrder = atkDice.card.slotOrder;
        foreach (var playingCard in owner.cardSlotDetail.cardAry)
        {
            if (playingCard != null && playingCard.target == atkDice.owner && playingCard.slotOrder == opponentSlotOrder)
            { isOneSided = false; break; }
        }
        if (isOneSided) _wasOneSidedAttackedThisRound = true;
    }
}

/// <summary>
/// 荆棘幻影 (ID: 9005008)
/// 存在时，薇莉亚获得4层守护和抵御。自身所有书页威力-4。
/// 红色半透明遮罩特效
/// </summary>
public class PassiveAbility_9005008 : PassiveAbilityBase
{
    private bool _colorApplied = false;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        ApplyRedColorFilter();
        GiveVeliaBuffs();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        ApplyRedColorFilter();
        GiveVeliaBuffs();
    }

    private void ApplyRedColorFilter()
    {
        if (_colorApplied || owner?.view?.charAppearance == null) return;

        // 红色半透明遮罩 (R=1, G=0.4, B=0.4, A=0.7)
        Color redTint = new Color(1f, 0.4f, 0.4f, 0.7f);
        owner.view.charAppearance.ChangeColor(redTint, 0f, null);
        _colorApplied = true;
        SteriaLogger.Log($"荆棘幻影: Applied red color filter");
    }

    private void GiveVeliaBuffs()
    {
        if (owner == null || BattleObjectManager.instance == null) return;

        // 找到薇莉亚（同阵营，有侵蚀被动9005001的单位）
        foreach (var ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            if (ally == owner || ally.IsDead()) continue;

            // 检查是否有侵蚀被动
            bool isVelia = false;
            if (ally.passiveDetail?.PassiveList != null)
            {
                foreach (var passive in ally.passiveDetail.PassiveList)
                {
                    if (passive is PassiveAbility_9005001)
                    {
                        isVelia = true;
                        break;
                    }
                }
            }

            if (isVelia)
            {
                ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 4, owner);
                ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.BreakProtection, 4, owner);
                SteriaLogger.Log($"荆棘幻影: Gave 4 Protection and 4 BreakProtection to {ally.UnitData?.unitData?.name}");
                break;
            }
        }
    }

    // 自身所有书页威力-4
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        behavior.ApplyDiceStatBonus(new DiceStatBonus { power = -4 });
    }

    // 死亡时移除薇莉亚的守护和振奋
    public override void OnDie()
    {
        base.OnDie();
        RemoveVeliaBuffs();
    }

    private void RemoveVeliaBuffs()
    {
        if (owner == null || BattleObjectManager.instance == null) return;

        foreach (var ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            if (ally == owner || ally.IsDead()) continue;

            bool isVelia = false;
            if (ally.passiveDetail?.PassiveList != null)
            {
                foreach (var passive in ally.passiveDetail.PassiveList)
                {
                    if (passive is PassiveAbility_9005001)
                    {
                        isVelia = true;
                        break;
                    }
                }
            }

            if (isVelia)
            {
                // 减少4层守护
                BattleUnitBuf protectionBuf = ally.bufListDetail.GetActivatedBuf(KeywordBuf.Protection);
                if (protectionBuf != null)
                {
                    protectionBuf.stack -= 4;
                    if (protectionBuf.stack <= 0) protectionBuf.Destroy();
                }
                // 减少4层振奋
                BattleUnitBuf breakProtectionBuf = ally.bufListDetail.GetActivatedBuf(KeywordBuf.BreakProtection);
                if (breakProtectionBuf != null)
                {
                    breakProtectionBuf.stack -= 4;
                    if (breakProtectionBuf.stack <= 0) breakProtectionBuf.Destroy();
                }
                SteriaLogger.Log($"荆棘幻影: Removed 4 Protection and 4 BreakProtection from {ally.UnitData?.unitData?.name}");
                break;
            }
        }
    }
}
