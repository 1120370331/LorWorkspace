using System;
using System.Collections.Generic;
using System.Linq;
using LOR_DiceSystem;
using UnityEngine;
using Steria;

internal static class ChristashaXiyuanHelper
{
    private static readonly Dictionary<BattleUnitModel, BattleUnitModel> _lastAttackTargetMap = new Dictionary<BattleUnitModel, BattleUnitModel>();

    public static void SetLastTarget(BattleUnitModel owner, BattleUnitModel target)
    {
        if (owner == null || target == null || owner == target)
        {
            return;
        }

        _lastAttackTargetMap[owner] = target;
    }

    public static BattleUnitModel GetLastTarget(BattleUnitModel owner)
    {
        if (owner == null)
        {
            return null;
        }

        return _lastAttackTargetMap.TryGetValue(owner, out BattleUnitModel target) ? target : null;
    }

    public static void ClearLastTarget(BattleUnitModel owner)
    {
        if (owner == null)
        {
            return;
        }

        _lastAttackTargetMap.Remove(owner);
    }

    public static bool HasSunlight(BattleUnitModel unit)
    {
        if (unit == null || unit.bufListDetail == null)
        {
            return false;
        }

        BattleUnitBuf_Sunlight sunlight = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Sunlight) as BattleUnitBuf_Sunlight;
        return sunlight != null && sunlight.stack > 0;
    }

    public static int GetTotalEnemyBurnStacks(BattleUnitModel owner)
    {
        if (owner == null || BattleObjectManager.instance == null)
        {
            return 0;
        }

        Faction enemyFaction = owner.faction == Faction.Enemy ? Faction.Player : Faction.Enemy;
        int total = 0;
        List<BattleUnitModel> enemies = BattleObjectManager.instance.GetAliveList(enemyFaction);
        if (enemies == null)
        {
            return 0;
        }

        foreach (BattleUnitModel enemy in enemies)
        {
            if (enemy == null || enemy.bufListDetail == null)
            {
                continue;
            }

            total += enemy.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
        }

        return total;
    }
}

public class BattleUnitBuf_SunriseAbyss : BattleUnitBuf
{
    protected override string keywordId => "SteriaSunriseAbyss";
    protected override string keywordIconId => "Sunlight";
    public override BufPositiveType positiveType => BufPositiveType.Positive;
}

/// <summary>
/// 速战速决3 (曦渊版)
/// 速度骰子+1，情感等级达到3后额外+1
/// </summary>
public class PassiveAbility_9009010 : PassiveAbilityBase
{
    public override int SpeedDiceNumAdder()
    {
        if (owner?.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 3)
        {
            return 2;
        }

        return 1;
    }
}

/// <summary>
/// 日沐曦渊（接待奖励书）
/// </summary>
public class PassiveAbility_9009011 : PassiveAbilityBase
{
    private const string MOD_ID = "SteriaBuilding";
    private const int CARD_RISING_SUN = 9010104;
    private const int CARD_DAWN_LIGHT = 9010105;

    private bool _awakened;
    private bool _egoCardAdded;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _awakened = false;
        _egoCardAdded = false;
        ChristashaXiyuanHelper.ClearLastTarget(owner);
        DiceCardSelfAbility_ChristashaXiyuanDawnLight.ClearGrowth(owner);
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        ChristashaXiyuanHelper.ClearLastTarget(owner);

        if (_awakened)
        {
            EnsureSunriseBuf();
        }
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        if (owner == null || owner.IsDead())
        {
            return;
        }

        if (!_awakened && owner.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 2)
        {
            _awakened = true;
            EnsureSunriseBuf();
            TryAddEgoCards();
            SteriaLogger.Log($"日沐曦渊: EGO awakened for {owner.UnitData?.unitData?.name}");
        }

        if (!_awakened)
        {
            return;
        }

        BattleUnitModel target = ChristashaXiyuanHelper.GetLastTarget(owner);
        if (target != null && !target.IsDead())
        {
            ChristashaEgoBossHelper.AddSunlight(target, 1, null);
        }
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (!_awakened || owner == null || behavior == null)
        {
            return;
        }

        int power = 1;
        if (owner.emotionDetail != null && owner.emotionDetail.EmotionLevel >= 4)
        {
            BattleUnitModel target = behavior.card?.target;
            if (ChristashaXiyuanHelper.HasSunlight(target))
            {
                power += 1;
            }
        }

        behavior.ApplyDiceStatBonus(new DiceStatBonus { power = power });
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);
        if (owner == null || behavior == null)
        {
            return;
        }

        BattleUnitModel target = behavior.card?.target;
        if (target == null || target == owner || target.IsDead())
        {
            return;
        }

        ChristashaXiyuanHelper.SetLastTarget(owner, target);

        if (!_awakened || owner.emotionDetail == null || owner.emotionDetail.EmotionLevel < 4)
        {
            return;
        }

        if (ChristashaXiyuanHelper.HasSunlight(target))
        {
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
        }
    }

    private void EnsureSunriseBuf()
    {
        if (owner == null || owner.bufListDetail == null)
        {
            return;
        }

        BattleUnitBuf_SunriseAbyss buf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_SunriseAbyss) as BattleUnitBuf_SunriseAbyss;

        if (buf == null)
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_SunriseAbyss { stack = 1 });
        }
        else if (buf.stack < 1)
        {
            buf.stack = 1;
        }
    }

    private void TryAddEgoCards()
    {
        if (_egoCardAdded || owner == null || owner.faction != Faction.Player)
        {
            return;
        }

        try
        {
            owner.personalEgoDetail?.AddCard(new LorId(MOD_ID, CARD_RISING_SUN));
            owner.personalEgoDetail?.AddCard(new LorId(MOD_ID, CARD_DAWN_LIGHT));
            _egoCardAdded = true;
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"日沐曦渊: failed to add EGO cards: {ex.Message}");
        }
    }
}

/// <summary>
/// 斯蒂芬妮之冠（曦渊版）
/// 每幕开始时随机：强壮+流 / 坚韧+梦 / 守护+振奋+潮
/// </summary>
public class PassiveAbility_9009012 : PassiveAbilityBase
{
    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (owner == null)
        {
            return;
        }

        int roll = UnityEngine.Random.Range(0, 3);
        switch (roll)
        {
            case 0:
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, owner);
                AddFlow(owner, 3);
                break;
            case 1:
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 1, owner);
                AddDream(owner, 3);
                break;
            default:
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 1, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.BreakProtection, 1, owner);
                PassiveAbility_9004001.AddTideStacks(owner, 3);
                break;
        }
    }

    private static void AddFlow(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0)
        {
            return;
        }

        BattleUnitBuf_Flow flow = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        if (flow != null)
        {
            flow.stack += amount;
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = amount });
        }
    }

    private static void AddDream(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0)
        {
            return;
        }

        BattleUnitBuf_Dream dream = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
        if (dream != null)
        {
            dream.stack += amount;
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_Dream { stack = amount });
        }
    }
}

/// <summary>
/// 飞星连刺
/// 使用后若3颗进攻型骰子全部命中，下回合对目标施加1层易损
/// </summary>
public class DiceCardSelfAbility_ChristashaXiyuanFeixing : DiceCardSelfAbilityBase
{
    private int _hitCount;
    private BattleUnitModel _target;

    public override void OnUseCard()
    {
        base.OnUseCard();
        _hitCount = 0;
        _target = card?.target;
    }

    internal void RegisterHit(BattleUnitModel target)
    {
        _hitCount++;
        if (_target == null && target != null)
        {
            _target = target;
        }
    }

    public override void AfterAction()
    {
        base.AfterAction();
        if (owner == null)
        {
            return;
        }

        if (_hitCount >= 3)
        {
            BattleUnitModel finalTarget = _target ?? card?.target;
            if (finalTarget != null && !finalTarget.IsDead())
            {
                finalTarget.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Vulnerable, 1, owner);
            }
        }
    }
}

public class DiceCardAbility_ChristashaXiyuanFeixingHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);
        DiceCardSelfAbility_ChristashaXiyuanFeixing selfAbility = card?.cardAbility as DiceCardSelfAbility_ChristashaXiyuanFeixing;
        selfAbility?.RegisterHit(target);
        if (target != null && owner != null)
        {
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
        }
    }
}

public class DiceCardSelfAbility_ChristashaXiyuanDawnSun : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        if (owner == null)
        {
            return;
        }

        BattleUnitModel target = card?.target;
        if (target == null || target.IsDead())
        {
            return;
        }

        int burn = target.bufListDetail?.GetKewordBufStack(KeywordBuf.Burn) ?? 0;
        if (burn < 5)
        {
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 3, owner);
        }
        else
        {
            owner.allyCardDetail?.DrawCards(2);
        }
    }
}

public class DiceCardSelfAbility_ChristashaXiyuanGoldenCrow : DiceCardSelfAbilityBase
{
    private bool _recoverPending;

    public override void OnUseCard()
    {
        base.OnUseCard();
        _recoverPending = false;
    }

    internal void MarkRecoverPending()
    {
        _recoverPending = true;
    }

    public override void AfterGiveDamage(int damage, BattleUnitModel target)
    {
        base.AfterGiveDamage(damage, target);
        if (!_recoverPending || owner == null || damage <= 0)
        {
            return;
        }

        int recover = Mathf.CeilToInt(damage * 0.2f);
        if (recover > 0)
        {
            owner.RecoverHP(recover);
        }

        _recoverPending = false;
    }
}

public class DiceCardAbility_ChristashaXiyuanGoldenCrowHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);

        if (target == null || owner == null)
        {
            return;
        }

        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 4, owner);
        ChristashaEgoBossHelper.AddSunlight(target, 1, owner);

        if (target.IsBreakLifeZero())
        {
            DiceCardSelfAbility_ChristashaXiyuanGoldenCrow selfAbility = card?.cardAbility as DiceCardSelfAbility_ChristashaXiyuanGoldenCrow;
            selfAbility?.MarkRecoverPending();
        }
    }
}

public class DiceCardAbility_ChristashaXiyuanApplySunlight1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);
        ChristashaEgoBossHelper.AddSunlight(target, 1, null);
    }

    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        base.OnSucceedAreaAttack(target);
        ChristashaEgoBossHelper.AddSunlight(target, 1, null);
    }
}

public class DiceCardAbility_ChristashaXiyuanBurn2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null || owner == null)
        {
            return;
        }

        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 2, owner);
    }

    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        OnSucceedAttack(target);
    }
}

public class DiceCardAbility_ChristashaXiyuanRecoverBreak5OnLose : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        base.OnLoseParrying();
        owner?.breakDetail?.RecoverBreak(5);
    }
}

/// <summary>
/// 擢升新阳：敌人每有7层烧伤，威力+1
/// </summary>
public class DiceCardSelfAbility_ChristashaXiyuanRisingSun : DiceCardSelfAbilityBase
{
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (owner == null || behavior == null)
        {
            return;
        }

        int totalBurn = ChristashaXiyuanHelper.GetTotalEnemyBurnStacks(owner);
        int bonus = totalBurn / 7;
        if (bonus > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = bonus });
        }
    }
}

/// <summary>
/// 黎明之光（曦渊版）
/// 渐强：每次使用后骰子威力+2、光芒消耗+1（至多3次）
/// 本书页不造成伤害
/// </summary>
public class DiceCardSelfAbility_ChristashaXiyuanDawnLight : DiceCardSelfAbilityBase
{
    private static readonly Dictionary<BattleUnitModel, int> _growthCountByOwner = new Dictionary<BattleUnitModel, int>();
    private bool _countedThisUse;

    public override void OnUseCard()
    {
        base.OnUseCard();
        _countedThisUse = false;
        EnsureEntry(owner);
    }

    public override int GetCostAdder(BattleUnitModel unit, BattleDiceCardModel self)
    {
        return GetGrowthCount(unit);
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null || owner == null)
        {
            return;
        }

        int growthCount = GetGrowthCount(owner);
        if (growthCount > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = growthCount * 2 });
        }
    }

    public override void OnEndAreaAttack()
    {
        base.OnEndAreaAttack();
        TryIncreaseGrowth();
    }

    public override void AfterAction()
    {
        base.AfterAction();
        TryIncreaseGrowth();
    }

    private void TryIncreaseGrowth()
    {
        if (_countedThisUse || owner == null)
        {
            return;
        }

        _countedThisUse = true;
        EnsureEntry(owner);

        if (_growthCountByOwner[owner] < 3)
        {
            _growthCountByOwner[owner]++;
        }
    }

    private static void EnsureEntry(BattleUnitModel unit)
    {
        if (unit == null)
        {
            return;
        }

        if (!_growthCountByOwner.ContainsKey(unit))
        {
            _growthCountByOwner[unit] = 0;
        }
    }

    private static int GetGrowthCount(BattleUnitModel unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return _growthCountByOwner.TryGetValue(unit, out int count) ? count : 0;
    }

    public static void ClearGrowth(BattleUnitModel owner = null)
    {
        if (owner == null)
        {
            _growthCountByOwner.Clear();
            return;
        }

        _growthCountByOwner.Remove(owner);
    }
}

public class DiceCardAbility_ChristashaXiyuanDawnLightBurnByCost : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);
        ApplyBurnByCost(target);
        ChristashaEgoBossHelper.AddSunlight(target, 1, owner);
    }

    public override void OnSucceedAreaAttack(BattleUnitModel target)
    {
        base.OnSucceedAreaAttack(target);
        ApplyBurnByCost(target);
        ChristashaEgoBossHelper.AddSunlight(target, 1, owner);
    }

    private void ApplyBurnByCost(BattleUnitModel target)
    {
        if (target == null || owner == null)
        {
            return;
        }

        int cost = card?.card?.GetCost() ?? 0;
        if (cost <= 0)
        {
            return;
        }

        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, cost, owner);
    }
}
