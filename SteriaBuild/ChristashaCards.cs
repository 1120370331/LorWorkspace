using System;
using System.Collections.Generic;
using System.Linq;
using LOR_DiceSystem;
using UnityEngine;

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

        List<BattleDiceCardModel> before = owner.allyCardDetail.GetHand().ToList();
        owner.allyCardDetail.DrawCards(1);
        List<BattleDiceCardModel> after = owner.allyCardDetail.GetHand();

        BattleDiceCardModel drawn = null;
        foreach (var card in after)
        {
            if (!before.Contains(card))
            {
                drawn = card;
                break;
            }
        }

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
}

public class DiceCardSelfAbility_ChristashaEliye : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 2);
    }
}

public class DiceCardSelfAbility_ChristashaLightPurge : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        if (owner == null) return;
        PassiveAbility_9004001.AddTideStacks(owner, 2);
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
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, _owner);
            stack--;
        }

        if (stack <= 0)
        {
            this.Destroy();
        }
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
    private bool _repeatTriggered;

    public override void OnWinParrying()
    {
        if (_repeatTriggered) return;
        behavior?.ApplyDiceStatBonus(new DiceStatBonus { power = -2 });
        ActivateBonusAttackDice();
        _repeatTriggered = true;
    }

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null) return;
        target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
        PassiveAbility_9004001.AddTideStacks(owner, 1);
    }
}

public class DiceCardAbility_ChristashaCounterPowerDown : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        BattleDiceBehavior targetDice = behavior?.TargetDice;
        if (targetDice == null) return;
        targetDice.ApplyDiceStatBonus(new DiceStatBonus { power = -5 });
    }
}

/// <summary>
/// 汐音：光耀万海 - [渐快]
/// 使用后永久复制最后一颗骰子（至多2次），并使本书页骰子数值按 (X-1)/X 缩放
/// </summary>
public class DiceCardSelfAbility_ChristashaGlory : DiceCardSelfAbilityBase
{
    private const int MaxGrow = 2;

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
        if (growth == null || growth.PendingAdd <= 0 || growth.StackCount >= MaxGrow) return;

        growth.PendingAdd = 0;
        // 永久追加最后一颗骰子（在本次战斗结束后生效）
        DiceCardXmlInfo xml = card.card.XmlData;
        if (xml?.DiceBehaviourList != null && xml.DiceBehaviourList.Count > 0)
        {
            DiceBehaviour last = xml.DiceBehaviourList[xml.DiceBehaviourList.Count - 1];
            xml.DiceBehaviourList.Add(last.Copy());
            growth.StackCount += 1;
        }
    }
}

public class BattleDiceCardBuf_ChristashaGloryGrowth : BattleDiceCardBuf
{
    protected override string keywordId => "ChristashaGloryGrowth";

    public int PendingAdd { get; set; }

    public int StackCount
    {
        get { return _stack; }
        set { _stack = value; }
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
