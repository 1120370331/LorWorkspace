using System;
using System.Collections;
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
// 骰子1：防御6-14 - 拼点胜利：获得5层梦
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
        // 拼点胜利：获得5层梦
        SivierCardHelper.AddDreamToUnit(owner, 5);
    }
}

// ========== 愿望之刺 (9008003) ==========
// 使用时：恢复2点光芒，将一张“愿露”置入手牌
// 骰子1：攻击突刺4-7 - 命中时：获得1层梦
// 骰子2：防御3-6

public class DiceCardSelfAbility_SivierWishThorn : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        // 恢复2点光芒
        owner?.cardSlotDetail?.RecoverPlayPoint(2);
        SivierCardHelper.AddSivierCardToHand(owner, SivierCardHelper.CardWishDew);
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

// ========== 集愿之盾 (9008004) ==========
// 使用时：消耗所有梦，为全队友方单位施加等量x2层"愿望之盾"
// 下回合将两张“梦之庇护”置入手牌
// 骰子1：防御5-9
// 骰子2：反击防御4-8

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

        if (owner != null)
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_SivierAddCardsNextRound
            {
                cardId = SivierCardHelper.CardDreamShelter,
                amount = 2
            });
        }
    }
}

// ========== 愿露 (9008008) ==========
// 使用时：抽1张牌，获得1层梦

public class DiceCardSelfAbility_SivierWishDew : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        ApplyWishDew(owner);
    }

    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        SteriaLogger.Log($"愿露: OnUseInstance triggered for {unit?.UnitData?.unitData?.name}, target={targetUnit?.UnitData?.unitData?.name}");
        ApplyWishDew(unit);
    }

    private static void ApplyWishDew(BattleUnitModel unit)
    {
        unit?.allyCardDetail?.DrawCards(1);
        SivierCardHelper.AddDreamToUnit(unit, 1);
    }
}

// ========== 梦之庇护 (9008009) ==========
// 使用时：恢复1点光芒，抽2张牌

public class DiceCardSelfAbility_SivierDreamShelter : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        base.OnUseCard();
        owner?.cardSlotDetail?.RecoverPlayPoint(1);
        owner?.allyCardDetail?.DrawCards(2);
    }
}

// ========== 海愿斩 (9008005) ==========
// 乐章型骰子 [重音]
// 若自身梦数量不低于5则使本书页骰子威力+2
// 若本书页消耗了2层梦，则将2张愿露置入手牌
// 骰子1：攻击斩击5-8 - 命中时：消耗1层梦来追加5点混乱伤害
// 骰子2：攻击斩击4-7 - 命中时：消耗1层梦来恢复5点混乱抗性

public class DiceCardSelfAbility_SivierSeaWishSlash : DiceCardSelfAbilityBase
{
    private static readonly Dictionary<BattlePlayingCardDataInUnitModel, int> _dreamConsumedByCard =
        new Dictionary<BattlePlayingCardDataInUnitModel, int>();
    private bool _addedWishDew;

    internal static void RecordDreamConsumed(BattlePlayingCardDataInUnitModel cardAction, int amount)
    {
        if (cardAction == null || amount <= 0) return;
        int current;
        _dreamConsumedByCard.TryGetValue(cardAction, out current);
        _dreamConsumedByCard[cardAction] = current + amount;
    }

    public override void OnUseCard()
    {
        base.OnUseCard();
        _addedWishDew = false;
        if (card != null)
        {
            _dreamConsumedByCard[card] = 0;
        }

        // 若自身梦数量不低于5则使本书页骰子威力+2
        int dreamCount = SivierCardHelper.GetDreamCount(owner);
        if (dreamCount >= 5)
        {
            // 海愿斩本身是乐章型卡牌，必须通过乐章白名单 + 原初之潮白名单下发威力。
            MusicDicePowerScope.RunWithAllowance(() =>
                global::PrimalTidePowerScope.RunWithAllowance(() =>
                    card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 2 })));
        }
    }

    public override void AfterAction()
    {
        base.AfterAction();

        if (_addedWishDew || card == null)
        {
            return;
        }

        _addedWishDew = true;
        int consumed;
        if (_dreamConsumedByCard.TryGetValue(card, out consumed) && consumed >= 2)
        {
            SivierCardHelper.AddSivierCardToHand(owner, SivierCardHelper.CardWishDew);
            SivierCardHelper.AddSivierCardToHand(owner, SivierCardHelper.CardWishDew);
        }
        _dreamConsumedByCard.Remove(card);
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
            DiceCardSelfAbility_SivierSeaWishSlash.RecordDreamConsumed(behavior?.card, 1);
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
            DiceCardSelfAbility_SivierSeaWishSlash.RecordDreamConsumed(behavior?.card, 1);
            owner?.breakDetail?.RecoverBreak(5);
        }
    }
}

// ========== 汐音：海之还愿 (9008006) ==========
// 乐章型骰子
// [渐弱]:每使用1次本书页使本书页光芒消耗-1,骰子最大值-2,最小值-1(至多触发4次)
// 使用时：自身获得(等同本书页光芒消耗)x2层梦
// 骰子1：攻击斩击6-14 - 命中时：抽1张牌
// 骰子2：攻击斩击9-16 - 命中时：恢复1点光芒
// 骰子3：攻击突刺5-13 - 命中时：获得1层梦
// 骰子4/5：防御8-13 - 拼点胜利：施加1层麻痹

public class DiceCardSelfAbility_SivierSeaReturn : DiceCardSelfAbilityBase
{
    // 追踪每个角色使用此卡的次数（用于渐弱效果）
    private static Dictionary<int, int> _useCount = new Dictionary<int, int>();
    private bool _countedThisUse;

    // 获取当前使用次数（用于计算减益）
    private static int GetUseCount(BattleUnitModel unit)
    {
        if (unit == null) return 0;
        int id = unit.GetHashCode();
        return _useCount.ContainsKey(id) ? _useCount[id] : 0;
    }

    internal static int GetUseCountForUnit(BattleUnitModel unit)
    {
        return GetUseCount(unit);
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
        _countedThisUse = false;

        // 自身获得(等同本书页光芒消耗)x2点梦
        int cost = card?.card?.GetCost() ?? 5;
        int gain = Math.Max(0, cost) * 2;
        SivierCardHelper.AddDreamToUnit(owner, gain);
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

        if (_countedThisUse)
            return;

        _countedThisUse = true;

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

public class DiceCardAbility_SivierSeaReturn4 : DiceCardAbilityBase
{
    public override void OnWinParrying()
    {
        base.OnWinParrying();
        // 拼点胜利：施加1层麻痹
        var target = behavior?.card?.target;
        if (target != null)
        {
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Paralysis, 1, owner);
        }
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
    public const string ModId = "SteriaBuilding";
    public const int CardWishDew = 9008008;
    public const int CardDreamShelter = 9008009;

    public static BattleUnitBuf GetDreamBuf(BattleUnitModel unit)
    {
        if (unit?.bufListDetail == null) return null;

        var activeBufList = unit.bufListDetail.GetActivatedBufList();
        if (activeBufList == null || activeBufList.Count == 0) return null;

        BattleUnitBuf typedBuf = activeBufList.FirstOrDefault(x => x is BattleUnitBuf_Dream);
        if (typedBuf != null && !typedBuf.IsDestroyed())
        {
            return typedBuf;
        }

        // 兜底：兼容同名类型来自不同程序集导致的类型不匹配
        return activeBufList.FirstOrDefault(x =>
            x != null &&
            !x.IsDestroyed() &&
            x.GetType().Name == nameof(BattleUnitBuf_Dream));
    }

    public static int GetDreamCount(BattleUnitModel unit)
    {
        BattleUnitBuf buf = GetDreamBuf(unit);
        return buf?.stack ?? 0;
    }

    public static void AddDreamToUnit(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;
        BattleUnitBuf buf = GetDreamBuf(unit);
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
        BattleUnitBuf buf = GetDreamBuf(unit);
        if (buf != null && buf.stack >= amount)
        {
            bool hasProxy = DirectiveDreamHelper.HasStephanieProxy(unit);
            if (!hasProxy)
            {
                buf.stack -= amount;
            }
            // 通知被动和追踪系统
            DiceCardSelfAbility_SivierWishBuried.OnDreamConsumed(unit, amount);
            if (!hasProxy && buf.stack <= 0)
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

    public static BattleDiceCardModel AddSivierCardToHand(BattleUnitModel unit, int cardId)
    {
        if (unit?.allyCardDetail == null) return null;

        try
        {
            LorId lorId = new LorId(ModId, cardId);
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(lorId, true)
                ?? ItemXmlDataList.instance.GetCardItem(lorId, false)
                ?? ItemXmlDataList.instance.GetCardItem(cardId, false);
            if (cardItem == null)
            {
                SteriaLogger.LogWarning($"SivierCardHelper: card {cardId} not found");
                return null;
            }

            BattleDiceCardModel cardModel = BattleDiceCardModel.CreatePlayingCard(cardItem);
            if (cardModel == null) return null;
            cardModel.owner = unit;
            EnsureExhaustOnUse(cardModel);
            int before = unit.allyCardDetail.GetHand()?.Count ?? -1;
            unit.allyCardDetail.AddCardToHand(cardModel, false);
            int after = unit.allyCardDetail.GetHand()?.Count ?? -1;
            SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.UpdateCardList();
            SteriaLogger.Log($"SivierCardHelper: added derivative card {cardId} to {unit.UnitData?.unitData?.name}, hand {before}->{after}");
            return cardModel;
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"SivierCardHelper: failed to add card {cardId}: {ex.Message}");
            return null;
        }
    }

    private static void EnsureExhaustOnUse(BattleDiceCardModel cardModel)
    {
        if (cardModel?.XmlData == null)
        {
            return;
        }

        if (cardModel.XmlData.optionList == null)
        {
            cardModel.XmlData.optionList = new List<CardOption>();
        }

        if (!cardModel.XmlData.optionList.Contains(CardOption.ExhaustOnUse))
        {
            cardModel.XmlData.optionList.Add(CardOption.ExhaustOnUse);
        }
    }

    public static void PlayPhantomDreamSpecialMotion(BattleUnitModel unit)
    {
        CharacterAppearance appearance = unit?.view?.charAppearance;
        if (appearance == null) return;

        appearance.ChangeMotion(ActionDetail.Special);
        unit.view.StartCoroutine(ResetMotionAfter(appearance, 1.0f));
    }

    private static IEnumerator ResetMotionAfter(CharacterAppearance appearance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (appearance != null)
        {
            appearance.ChangeMotion(ActionDetail.Standing);
        }
    }

    public static void TryAutoUsePhantomDreamForEnemy(BattleUnitModel unit)
    {
        if (unit == null || unit.faction != Faction.Enemy || unit.IsDead() || unit.allyCardDetail == null)
        {
            return;
        }

        List<BattleDiceCardModel> hand = unit.allyCardDetail.GetHand();
        if (hand == null || hand.Count == 0)
        {
            return;
        }

        int dream = GetDreamCount(unit);
        List<BattleDiceCardModel> phantomCards = hand
            .Where(c => c != null
                && Steria.HarmonyHelpers.IsPhantomDreamCard(c)
                && dream >= Steria.HarmonyHelpers.GetPhantomDreamCost(c))
            .ToList();
        if (phantomCards.Count == 0)
        {
            return;
        }

        int actionSlots = GetUsableActionSlotCount(unit);
        bool guaranteed = hand.Count > actionSlots;
        if (!guaranteed && UnityEngine.Random.value >= 0.5f)
        {
            return;
        }

        List<BattleUnitModel> allies = BattleObjectManager.instance?.GetAliveList(unit.faction)
            ?.Where(ally => ally != null && ally != unit && !ally.IsDead() && !ally.IsBreakLifeZero())
            .ToList();
        if (allies == null || allies.Count == 0)
        {
            return;
        }

        List<BattleUnitModel> shuffledAllies = allies.OrderBy(_ => UnityEngine.Random.value).ToList();
        List<BattleDiceCardModel> shuffledCards = phantomCards.OrderBy(_ => UnityEngine.Random.value).ToList();

        foreach (BattleDiceCardModel sourceCard in shuffledCards)
        {
            foreach (BattleUnitModel ally in shuffledAllies)
            {
                int targetSlot = GetRandomUsableSpeedSlot(ally);
                if (Steria.HarmonyHelpers.TryResolvePhantomDreamTransfer(unit, sourceCard, ally, targetSlot))
                {
                    SteriaLogger.Log($"Sivier AI: auto-used Phantom Dream card {sourceCard.GetName()} on {ally.UnitData?.unitData?.name}");
                    return;
                }
            }
        }
    }

    private static int GetUsableActionSlotCount(BattleUnitModel unit)
    {
        if (unit?.speedDiceResult != null && unit.speedDiceResult.Count > 0)
        {
            return Math.Max(1, unit.speedDiceResult.Count(x => !x.breaked));
        }

        return Math.Max(1, unit?.cardSlotDetail?.cardAry?.Count ?? 1);
    }

    private static int GetRandomUsableSpeedSlot(BattleUnitModel unit)
    {
        if (unit?.speedDiceResult == null || unit.speedDiceResult.Count == 0)
        {
            return -1;
        }

        List<int> slots = new List<int>();
        for (int i = 0; i < unit.speedDiceResult.Count; i++)
        {
            if (!unit.speedDiceResult[i].breaked)
            {
                slots.Add(i);
            }
        }

        return slots.Count > 0 ? slots[UnityEngine.Random.Range(0, slots.Count)] : -1;
    }
}

// ========== 额外Buff类 ==========

/// <summary>
/// 下回合将指定希维尔衍生书页置入手牌。
/// </summary>
public class BattleUnitBuf_SivierAddCardsNextRound : BattleUnitBuf
{
    public int cardId;
    public int amount;

    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        int count = Math.Max(0, amount);
        for (int i = 0; i < count; i++)
        {
            SivierCardHelper.AddSivierCardToHand(_owner, cardId);
        }

        Destroy();
    }
}

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
