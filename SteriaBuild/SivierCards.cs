using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LOR_DiceSystem;
using Steria;

// 希维尔卡牌能力 - 按照设计稿实现

// ========== 聚愿而行 (9008001) ==========
// 骰子1：防御6-12 - 拼点胜利：使敌人当前书页骰子威力-2
// 骰子2：攻击斩击3-7 - 拼点胜利：使自身下一颗骰子威力+2
// 骰子3：攻击打击2-5 - 命中时：施加2层麻痹
// 骰子4：反击斩击2-7

public class DiceCardAbility_SivierGatherWish1 : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        base.OnWinParrying();
        // 使敌人当前书页骰子威力-2
        var target = behavior?.card?.target;
        if (target?.currentDiceAction != null)
        {
            foreach (var dice in target.currentDiceAction.GetDiceBehaviorList())
            {
                dice.ApplyDiceStatBonus(new DiceStatBonus { power = -2 });
            }
        }
    }
}

public class DiceCardAbility_SivierGatherWish2 : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        base.OnWinParrying();
        // 使自身下一颗骰子威力+2
        var cardAction = owner?.currentDiceAction;
        if (cardAction != null)
        {
            var list = cardAction.GetDiceBehaviorList();
            int idx = list.IndexOf(behavior);
            if (idx >= 0 && idx + 1 < list.Count)
            {
                list[idx + 1].ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
            }
        }
    }
}

public class DiceCardAbility_SivierGatherWish3 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：施加2层麻痹
        behavior?.card?.target?.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Paralysis, 2, owner);
    }
}

// ========== 守望潮汐 (9008002) ==========
// 使用时：抽2张牌
// 骰子1：防御6-10 - 拼点胜利：获得2层梦
// 骰子2：攻击斩击3-6

public class DiceCardSelfAbility_SivierWatchTide : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        // 抽2张牌
        owner?.allyCardDetail?.DrawCards(2);
    }
}

public class DiceCardAbility_SivierWatchTide1 : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        base.OnWinParrying();
        // 拼点胜利：获得2层梦
        SivierCardHelper.AddDreamToUnit(owner, 2);
    }
}

// ========== 愿望之刺 (9008003) ==========
// 使用时：恢复2点光芒
// 骰子1：攻击突刺4-7 - 命中时：获得1层梦
// 骰子2：防御3-6

public class DiceCardSelfAbility_SivierWishThorn : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        // 恢复2点光芒
        owner?.cardSlotDetail?.RecoverPlayPoint(2);
    }
}

public class DiceCardAbility_SivierWishThorn1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：获得1层梦
        SivierCardHelper.AddDreamToUnit(owner, 1);
    }
}

// ========== 愿护佑我们 (9008004) ==========
// 使用时：消耗所有梦，为全队友方单位施加等量x2层"愿望之盾"
// 骰子1：防御5-10
// 骰子2：防御4-8

public class DiceCardSelfAbility_SivierWishProtect : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        // 消耗所有梦，为全队施加等量x2层愿望之盾
        int dreamCount = SivierCardHelper.GetDreamCount(owner);
        if (dreamCount > 0)
        {
            SivierCardHelper.ConsumeDream(owner, dreamCount);
            int shieldAmount = dreamCount * 2;
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                SivierCardHelper.AddWishShieldToUnit(ally, shieldAmount);
            }
        }
    }
}

// ========== 海愿斩 (9008005) ==========
// 乐章型骰子 [重音]
// 若自身梦数量不低于5则使本书页骰子威力+2
// 骰子1：攻击斩击5-8 - 命中时：消耗1层梦来追加5点混乱伤害
// 骰子2：攻击斩击4-7 - 命中时：消耗1层梦来恢复5点混乱抗性

public class DiceCardSelfAbility_SivierSeaWishSlash : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        // 若自身梦数量不低于5则使本书页骰子威力+2
        int dreamCount = SivierCardHelper.GetDreamCount(owner);
        if (dreamCount >= 5)
        {
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 2 });
        }
    }
}

public class DiceCardAbility_SivierSeaWishSlash1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：消耗1层梦来追加5点混乱伤害
        if (SivierCardHelper.GetDreamCount(owner) >= 1)
        {
            SivierCardHelper.ConsumeDream(owner, 1);
            behavior?.card?.target?.TakeBreakDamage(5, DamageType.Card_Ability);
        }
    }
}

public class DiceCardAbility_SivierSeaWishSlash2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：消耗1层梦来恢复5点混乱抗性
        if (SivierCardHelper.GetDreamCount(owner) >= 1)
        {
            SivierCardHelper.ConsumeDream(owner, 1);
            owner?.breakDetail?.RecoverBreak(5);
        }
    }
}

// ========== 汐音：海之还愿 (9008006) ==========
// 乐章型骰子
// [渐弱]:每使用1次本书页使本书页光芒消耗-1,骰子最大值-2,最小值-1(至多触发4次)
// 使用时：使所有友方角色获得(等同本书页光芒消耗)点梦
// 骰子1：攻击斩击6-14 - 命中时：抽1张牌
// 骰子2：攻击斩击9-16 - 命中时：恢复1点光芒
// 骰子3：攻击突刺5-13 - 命中时：获得1层梦
// 骰子4：反击5-12

public class DiceCardSelfAbility_SivierSeaReturn : DiceCardSelfAbilityBase
{
    // 追踪每个角色使用此卡的次数（用于渐弱效果）
    private static Dictionary<int, int> _useCount = new Dictionary<int, int>();

    // 获取当前使用次数（用于计算减益）
    private static int GetUseCount(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        int id = unit.GetHashCode();
        return _useCount.ContainsKey(id) ? _useCount[id] : 0;
    }

    // 渐弱效果：减少光芒消耗
    public override int GetCostAdder(BattleUnitModel unit, BattleDiceCardModel self)
    {
        int count = GetUseCount(unit);
        return -count; // 每使用1次减少1点消耗
    }

    public override void OnUseCard()
    {
        base.OnUseCard();
        // 使所有友方角色获得(等同本书页光芒消耗)点梦
        int cost = card?.card?.GetCost() ?? 5;
        var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
        foreach (var ally in allies)
        {
            SivierCardHelper.AddDreamToUnit(ally, cost);
        }
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        // 渐弱效果：每使用1次本书页使骰子最大值-2,最小值-1
        int count = GetUseCount(owner);
        if (count > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { min = -count, max = -count * 2 });
        }
    }

    // 在卡牌使用完毕后增加使用次数
    public override void AfterAction()
    {
        base.AfterAction();
        // 渐弱效果：记录使用次数（至多4次）
        if (owner == null) return;
        int ownerId = owner.GetHashCode();
        if (!_useCount.ContainsKey(ownerId))
            _useCount[ownerId] = 0;
        if (_useCount[ownerId] < 4)
            _useCount[ownerId]++;
    }
}

public class DiceCardAbility_SivierSeaReturn1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：抽1张牌
        owner?.allyCardDetail?.DrawCards(1);
    }
}

public class DiceCardAbility_SivierSeaReturn2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：恢复1点光芒
        owner?.cardSlotDetail?.RecoverPlayPoint(1);
    }
}

public class DiceCardAbility_SivierSeaReturn3 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：获得1层梦
        SivierCardHelper.AddDreamToUnit(owner, 1);
    }
}

// ========== 愿望终将埋葬于深海 (9008007) ==========
// [佚亡]
// 本书页仅限消耗20层以上梦后使用
// 使用时：持续2幕，所有敌人无法恢复光芒
// 骰子1：攻击打击10-23 - 命中时：下回合开始时失去2点光芒

public class DiceCardSelfAbility_SivierWishBuried : DiceCardSelfAbilityBase
{
    // 追踪累计消耗的梦层数
    private static Dictionary<int, int> _totalDreamConsumed = new Dictionary<int, int>();

    public static void OnDreamConsumed(BattleUnitModel unit, int amount)
    {
        if (unit == null) return;
        int id = unit.GetHashCode();
        if (!_totalDreamConsumed.ContainsKey(id))
            _totalDreamConsumed[id] = 0;
        _totalDreamConsumed[id] += amount;
        Steria.SteriaLogger.Log($"SivierWishBuried: Dream consumed {amount}, total: {_totalDreamConsumed[id]} for unit {unit.UnitData?.unitData?.name}");
    }

    public static int GetTotalDreamConsumed(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        int id = unit.GetHashCode();
        int total = _totalDreamConsumed.ContainsKey(id) ? _totalDreamConsumed[id] : 0;
        return total;
    }

    // 使用OnChooseCard来限制卡牌选择，这样AI也会遵守这个限制
    public override bool OnChooseCard(BattleUnitModel owner)
    {
        // 本书页仅限消耗20层以上梦后使用
        int consumed = GetTotalDreamConsumed(owner);
        bool canUse = consumed >= 20;
        Steria.SteriaLogger.Log($"SivierWishBuried OnChooseCard: consumed={consumed}, canUse={canUse}");
        return canUse && base.OnChooseCard(owner);
    }

    public override void OnUseCard()
    {
        base.OnUseCard();
        // 使用时：持续2幕，所有敌人无法恢复光芒
        var enemies = BattleObjectManager.instance.GetAliveList(
            owner.faction == Faction.Player ? Faction.Enemy : Faction.Player);
        foreach (var enemy in enemies)
        {
            enemy.bufListDetail.AddBuf(new BattleUnitBuf_SivierNoLightRecover());
        }
    }
}

public class DiceCardAbility_SivierWishBuried1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack()
    {
        base.OnSucceedAttack();
        // 命中时：下回合开始时失去2点光芒
        behavior?.card?.target?.bufListDetail.AddBuf(new BattleUnitBuf_SivierLoseLightNextTurn());
    }
}

// ========== 辅助类 ==========

public static class SivierCardHelper
{
    public static int GetDreamCount(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        var buf = unit.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
        return buf?.stack ?? 0;
    }

    public static void AddDreamToUnit(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        var buf = unit.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
        if (buf != null)
        {
            buf.stack += amount;
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_Dream { stack = amount });
        }
    }

    public static void ConsumeDream(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        var buf = unit.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
        if (buf != null && buf.stack >= amount)
        {
            buf.stack -= amount;
            // 通知被动和追踪系统
            DiceCardSelfAbility_SivierWishBuried.OnDreamConsumed(unit, amount);
            if (buf.stack <= 0)
            {
                buf.Destroy();
            }
        }
    }

    public static void AddWishShieldToUnit(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        var buf = unit.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_WishShield) as BattleUnitBuf_WishShield;
        if (buf != null)
        {
            buf.stack += amount;
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_WishShield { stack = amount });
        }
    }
}

// ========== 额外Buff类 ==========

/// <summary>
/// 无法恢复光芒（持续2幕）
/// </summary>
public class BattleUnitBuf_SivierNoLightRecover : BattleUnitBuf
{
    private int _duration = 2;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        this.stack = 1;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        // 阻止光芒恢复：将恢复点设为0
        _owner?.cardSlotDetail?.SetRecoverPoint(0);
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        _duration--;
        if (_duration <= 0)
        {
            Destroy();
        }
    }
}

/// <summary>
/// 下回合开始时失去2点光芒
/// </summary>
public class BattleUnitBuf_SivierLoseLightNextTurn : BattleUnitBuf
{
    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        this.stack = 1;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _owner?.cardSlotDetail?.LosePlayPoint(2);
        Destroy();
    }
}
