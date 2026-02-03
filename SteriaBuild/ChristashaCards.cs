using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LOR_DiceSystem;
using UnityEngine;
using Steria;

// 克丽丝塔夏战斗书页能力（全局命名空间）

public class DiceCardSelfAbility_ChristashaGoldenTide : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        owner.cardSlotDetail.RecoverPlayPoint(1);
        ChristashaAbilityHelper.ConvertTideToGolden(owner, 1);
    }
}

public class DiceCardSelfAbility_ChristashaHomecoming : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner?.allyCardDetail == null) return;

        BattleDiceCardModel drawn = DrawOneCardAndGet(owner.allyCardDetail);
        if (drawn == null) return;

        int cost = drawn.GetCost();
        if (cost % 2 == 0)
        {
            owner.allyCardDetail.DrawCards(1);
        }
        else
        {
            PassiveAbility_9004001.AddTideStacks(owner, 1);
        }
    }

    private static BattleDiceCardModel DrawOneCardAndGet(BattleAllyCardDetail detail)
    {
        if (detail == null) return null;

        try
        {
            FieldInfo deckField = typeof(BattleAllyCardDetail).GetField("_cardInDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo discardField = typeof(BattleAllyCardDetail).GetField("_cardInDiscarded", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo handField = typeof(BattleAllyCardDetail).GetField("_cardInHand", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxDrawField = typeof(BattleAllyCardDetail).GetField("_maxDrawHand", BindingFlags.NonPublic | BindingFlags.Instance);

            List<BattleDiceCardModel> deck = deckField?.GetValue(detail) as List<BattleDiceCardModel>;
            List<BattleDiceCardModel> discard = discardField?.GetValue(detail) as List<BattleDiceCardModel>;
            List<BattleDiceCardModel> hand = handField?.GetValue(detail) as List<BattleDiceCardModel>;
            int maxDraw = maxDrawField != null ? (int)maxDrawField.GetValue(detail) : 0;

            if (deck == null || discard == null || hand == null) return null;
            if (maxDraw > 0 && hand.Count >= maxDraw) return null;

            if (deck.Count == 0)
            {
                if (discard.Count == 0) return null;
                deck.AddRange(discard);
                discard.Clear();
                detail.Shuffle();
            }

            if (deck.Count <= 0) return null;
            BattleDiceCardModel drawn = deck[deck.Count - 1];
            detail.AddCardToHand(drawn, false);
            deck.RemoveAt(deck.Count - 1);
            return drawn;
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"ChristashaHomecoming: DrawOneCardAndGet failed: {ex.Message}");
            return null;
        }
    }
}

public class DiceCardSelfAbility_ChristashaEliye : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 1);
    }
}

public class DiceCardSelfAbility_ChristashaLightPurge : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 3);
    }
}

public class DiceCardAbility_ChristashaGainTide1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 1);
    }
}

public class DiceCardAbility_ChristashaConvertTide1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        ChristashaAbilityHelper.ConvertTideToGolden(owner, 1);
    }
}

public class DiceCardAbility_ChristashaStrengthNextTurn2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        BattleUnitBuf_ChristashaStrengthNextTurn buf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_ChristashaStrengthNextTurn) as BattleUnitBuf_ChristashaStrengthNextTurn;
        if (buf == null)
        {
            buf = new BattleUnitBuf_ChristashaStrengthNextTurn { stack = 2 };
            owner.bufListDetail.AddBuf(buf);
        }
        else
        {
            buf.stack += 2;
        }
    }
}

public class BattleUnitBuf_ChristashaStrengthNextTurn : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        if (_owner == null)
        {
            this.Destroy();
            return;
        }

        if (stack > 0)
        {
            int bonus = ConsumeGoldenTideForStrength(_owner);
            int finalStack = 1 + bonus;
            // 若黄金之潮已触发，避免再次触发潮/黄金之潮逻辑
            if (bonus > 0)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, finalStack, null);
            }
            else
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, finalStack, _owner);
            }
            stack--;
        }

        if (stack <= 0)
        {
            this.Destroy();
        }
    }

    private static int ConsumeGoldenTideForStrength(BattleUnitModel owner)
    {
        if (owner == null) return 0;
        BattleUnitBuf_GoldenTide golden = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_GoldenTide) as BattleUnitBuf_GoldenTide;
        if (golden == null || golden.stack <= 0) return 0;

        int consume = 1;
        int bonus = 2;
        if (golden.stack >= 2)
        {
            consume = 2;
            bonus = 3;
        }

        golden.stack -= consume;
        if (golden.stack <= 0)
        {
            golden.Destroy();
        }

        Steria.HarmonyHelpers.NotifyPassivesOnTideConsumed(owner, consume);
        return bonus;
    }
}

public class DiceCardAbility_ChristashaBurn2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
    }
}

public class DiceCardAbility_ChristashaBurn1Moon1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
        ChristashaAbilityHelper.AddMoonGravity(target, 1);
    }
}

public class DiceCardAbility_ChristashaMoon2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        ChristashaAbilityHelper.AddMoonGravity(target, 2);
    }
}

public class DiceCardAbility_ChristashaGloryOnHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null || target == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 1);
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
    }

    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        if (owner == null || target == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 1);
        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
    }
}

public class DiceCardAbility_ChristashaBurstReroll : DiceCardAbilityBase
{
    private int _repeatCount;
    private const int MaxRepeat = 20;
    private static readonly Dictionary<BattleDiceBehavior, int> _pendingPowerDown = new Dictionary<BattleDiceBehavior, int>();

    public override void OnWinParrying()
    {
        if (_repeatCount >= MaxRepeat) return;
        AddPendingPowerDown(behavior);
        ActivateBonusAttackDice();
        _repeatCount++;
    }

    public override void BeforeRollDice()
    {
        if (ConsumePendingPowerDown(behavior))
        {
            behavior?.ApplyDiceStatBonus(new DiceStatBonus { power = -2 });
        }
    }

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
        PassiveAbility_9004001.AddTideStacks(owner, 2);
    }

    private static void AddPendingPowerDown(BattleDiceBehavior dice)
    {
        if (dice == null) return;
        if (_pendingPowerDown.TryGetValue(dice, out int count))
        {
            _pendingPowerDown[dice] = count + 1;
        }
        else
        {
            _pendingPowerDown[dice] = 1;
        }
    }

    private static bool ConsumePendingPowerDown(BattleDiceBehavior dice)
    {
        if (dice == null) return false;
        if (!_pendingPowerDown.TryGetValue(dice, out int count) || count <= 0) return false;
        count--;
        if (count <= 0)
        {
            _pendingPowerDown.Remove(dice);
        }
        else
        {
            _pendingPowerDown[dice] = count;
        }
        return true;
    }
}

public class DiceCardAbility_ChristashaCounterPowerDown : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        BattleDiceBehavior targetDice = behavior?.TargetDice;
        if (targetDice == null) return;
        targetDice.ApplyDiceStatBonus(new DiceStatBonus { dmg = -5 });
    }
}

/// <summary>
/// 汐音：光耀万海 - [渐快]
/// 使用后永久复制最后一颗骰子（至多2次），并使本书页骰子数值按 (X-1)/X 缩放
/// </summary>
public class DiceCardSelfAbility_ChristashaGlory : DiceCardSelfAbilityBase
{
    internal const int MaxGrow = 2;

    public override void OnUseCard()
    {
        if (card?.card == null) return;

        BattleDiceCardBuf_ChristashaGloryGrowth growth = card.card.GetBufList()
            .FirstOrDefault(b => b is BattleDiceCardBuf_ChristashaGloryGrowth) as BattleDiceCardBuf_ChristashaGloryGrowth;
        if (growth == null)
        {
            growth = new BattleDiceCardBuf_ChristashaGloryGrowth { StackCount = 0 };
            card.card.AddBufWithoutDuplication(growth);
        }

        EnsureUniqueDiceList(card.card, growth);
        if (growth.StackCount >= MaxGrow) return;
        // 延后复制：确保本次使用的点数不受新复制影响
        growth.PendingAdd = 1;
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null || card?.card == null) return;
        int count = card.card.XmlData?.DiceBehaviourList?.Count ?? 0;
        if (count <= 1) return;

        float factor = (count - 1f) / count;
        int baseMin = behavior.GetDiceVanillaMin();
        int baseMax = behavior.behaviourInCard.Dice;
        int scaledMin = Mathf.CeilToInt(baseMin * factor);
        int scaledMax = Mathf.CeilToInt(baseMax * factor);
        behavior.ApplyDiceStatBonus(new DiceStatBonus
        {
            min = scaledMin - baseMin,
            max = scaledMax - baseMax
        });
    }

    public override void OnEndBattle()
    {
        if (card?.card == null) return;
        BattleDiceCardBuf_ChristashaGloryGrowth growth = card.card.GetBufList()
            .FirstOrDefault(b => b is BattleDiceCardBuf_ChristashaGloryGrowth) as BattleDiceCardBuf_ChristashaGloryGrowth;
        TryApplyPendingGrowth(card.card, growth);
    }

    internal static void TryApplyPendingGrowth(BattleDiceCardModel model, BattleDiceCardBuf_ChristashaGloryGrowth growth)
    {
        if (model == null || growth == null) return;
        if (growth.PendingAdd <= 0 || growth.StackCount >= MaxGrow) return;

        EnsureUniqueDiceList(model, growth);
        growth.PendingAdd = 0;
        // 永久追加最后一颗骰子（在本次战斗结束后生效）
        DiceCardXmlInfo xml = model.XmlData;
        if (xml?.DiceBehaviourList != null && xml.DiceBehaviourList.Count > 0)
        {
            DiceBehaviour last = xml.DiceBehaviourList[xml.DiceBehaviourList.Count - 1];
            xml.DiceBehaviourList.Add(last.Copy());
            growth.StackCount += 1;
        }
    }

    private static void EnsureUniqueDiceList(BattleDiceCardModel model, BattleDiceCardBuf_ChristashaGloryGrowth growth)
    {
        if (model == null || growth == null || growth.HasClonedList) return;
        DiceCardXmlInfo xml = model.XmlData;
        if (xml?.DiceBehaviourList == null) return;

        List<DiceBehaviour> copied = new List<DiceBehaviour>();
        foreach (DiceBehaviour dice in xml.DiceBehaviourList)
        {
            copied.Add(dice.Copy());
        }

        xml.DiceBehaviourList = copied;
        growth.HasClonedList = true;
        growth.BaseDiceCount = copied.Count;
    }
}

public class BattleDiceCardBuf_ChristashaGloryGrowth : BattleDiceCardBuf
{
    protected override string keywordId => "ChristashaGloryGrowth";

    public int PendingAdd { get; set; }
    public bool HasClonedList { get; set; }
    public int BaseDiceCount { get; set; }

    public int StackCount
    {
        get { return _stack; }
        set { _stack = value; }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        if (_card == null) return;
        DiceCardSelfAbility_ChristashaGlory.TryApplyPendingGrowth(_card, this);
    }
}

/// <summary>
/// 汐火联星 - [延长]
/// </summary>
public class DiceCardSelfAbility_ChristashaTwinStar : DiceCardSelfAbilityBase
{
    private const int MinTide = 5;

    public override bool OnChooseCard(BattleUnitModel owner)
    {
        if (owner == null) return false;
        return ChristashaAbilityHelper.GetTotalTideStacks(owner) >= MinTide;
    }

    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        if (unit == null) return;
        ChristashaAbilityHelper.ConvertTideToGolden(unit, 2);
        ApplyExtension(unit, self, targetUnit);
    }

    public override void OnUseCard()
    {
        if (owner == null) return;
        ChristashaAbilityHelper.ConvertTideToGolden(owner, 2);
        ApplyExtension(owner, card?.card, card?.target);
    }

    private void ApplyExtension(BattleUnitModel unit, BattleDiceCardModel usedCard, BattleUnitModel target)
    {
        if (unit == null || usedCard == null) return;

        BattleUnitBuf_ChristashaExtend buf = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_ChristashaExtend) as BattleUnitBuf_ChristashaExtend;
        if (buf == null)
        {
            buf = new BattleUnitBuf_ChristashaExtend();
            unit.bufListDetail.AddBuf(buf);
        }

        int slot = card?.slotOrder ?? -1;
        int targetSlot = card?.targetSlotOrder ?? -1;
        buf.SetQueued(usedCard, target, slot, targetSlot);
    }
}

public class DiceCardAbility_ChristashaTwinStarBurn : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 3, owner);
    }
}

public class DiceCardAbility_ChristashaTwinStarMoon : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        ChristashaAbilityHelper.AddMoonGravity(target, 3);
    }
}

public class BattleUnitBuf_ChristashaExtend : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    private BattleDiceCardModel _card;
    private BattleUnitModel _target;
    private int _slotOrder;
    private int _targetSlot;

    public void SetQueued(BattleDiceCardModel card, BattleUnitModel target, int slotOrder, int targetSlot)
    {
        _card = card;
        _target = target;
        _slotOrder = slotOrder;
        _targetSlot = targetSlot;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner == null || _card == null)
        {
            this.Destroy();
            return;
        }

        BattleUnitModel target = _target;
        if (target == null || target.IsDead() || target.IsBreakLifeZero())
        {
            List<BattleUnitModel> enemies = BattleObjectManager.instance?.GetAliveList((_owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
            if (enemies != null && enemies.Count > 0)
            {
                target = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            }
        }

        if (target == null)
        {
            this.Destroy();
            return;
        }

        int targetSlot = _targetSlot;
        if (target.speedDiceResult != null && targetSlot >= 0 && targetSlot < target.speedDiceResult.Count)
        {
            // keep
        }
        else if (target.speedDiceResult != null && target.speedDiceResult.Count > 0)
        {
            targetSlot = UnityEngine.Random.Range(0, target.speedDiceResult.Count);
        }
        else
        {
            targetSlot = 0;
        }

        // 复制卡牌，设置0费
        BattleDiceCardModel tempCard = BattleDiceCardModel.CreatePlayingCard(_card.XmlData);
        tempCard.SetCurrentCost(0);

        // 强制放入指定速度骰子
        int slot = _slotOrder;
        if (slot < 0 || _owner.speedDiceResult == null || slot >= _owner.speedDiceResult.Count || _owner.speedDiceResult[slot].breaked)
        {
            slot = -1;
            if (_owner.speedDiceResult != null)
            {
                for (int i = 0; i < _owner.speedDiceResult.Count; i++)
                {
                    if (!_owner.speedDiceResult[i].breaked && _owner.cardSlotDetail.cardAry[i] == null)
                    {
                        slot = i;
                        break;
                    }
                }
            }
        }

        if (slot >= 0)
        {
            _owner.cardOrder = slot;
            _owner.cardSlotDetail.AddCard(tempCard, target, targetSlot, false);
        }

        this.Destroy();
    }
}
