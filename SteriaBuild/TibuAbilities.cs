using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;

// Tibu passives and card-buffs

public class PassiveAbility_9006001 : PassiveAbilityBase
{
    private static readonly string MOD_ID = "SteriaBuilding";
    private const int CARD_TIDE_AWAKENING = 9006007;

    private int _tideConsumedAccumulator;
    private int _strengthNextRound;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _tideConsumedAccumulator = 0;
        _strengthNextRound = 0;
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _tideConsumedAccumulator = 0;
        _strengthNextRound = 0;
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (owner == null || owner.IsDead())
        {
            return;
        }

        if (_strengthNextRound > 0)
        {
            // Apply stored strength for this round without consuming Tide.
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, _strengthNextRound, null);
            _strengthNextRound = 0;
        }

        TryAddTideAwakeningCard();
    }

    private void TryAddTideAwakeningCard()
    {
        try
        {
            LorId lorId = new LorId(MOD_ID, CARD_TIDE_AWAKENING);
            owner.personalEgoDetail.AddCard(lorId);
        }
        catch (Exception ex)
        {
            SteriaLogger.LogError($"TideAwakening: Failed to add EGO card: {ex.Message}");
        }
    }

    public void OnTideConsumed(int amount)
    {
        if (owner == null || amount <= 0)
        {
            return;
        }

        _tideConsumedAccumulator += amount;

        while (_tideConsumedAccumulator >= 3)
        {
            _tideConsumedAccumulator -= 3;
            owner.cardSlotDetail.RecoverPlayPoint(1);
            _strengthNextRound++;
        }
    }
}

public class PassiveAbility_9006002 : PassiveAbilityBase
{
    public override void OnRoundStartAfter()
    {
        base.OnRoundStartAfter();
        ClearTideRevelationFromAllies();
        ApplyTideRevelationToRandomCards();
        RefreshHandUi();
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        ClearTideRevelationFromAllies();
        RefreshHandUi();
    }

    private void ApplyTideRevelationToRandomCards()
    {
        if (owner == null || owner.IsDead() || BattleObjectManager.instance == null)
        {
            return;
        }

        List<BattleDiceCardModel> candidates = new List<BattleDiceCardModel>();
        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => !u.IsDead() && !u.IsBreakLifeZero())
            .ToList();

        foreach (BattleUnitModel ally in allies)
        {
            if (ally?.allyCardDetail == null) continue;
            List<BattleDiceCardModel> hand = ally.allyCardDetail.GetHand();
            if (hand == null || hand.Count == 0) continue;

            foreach (BattleDiceCardModel card in hand)
            {
                if (card?.XmlData == null)
                {
                    continue;
                }

                if (IsEquipmentCard(card))
                {
                    continue;
                }

                if (!Steria.TibuAbilityHelper.HasTideRevelation(card))
                {
                    candidates.Add(card);
                }
            }
        }

        int applyCount = Math.Min(2, candidates.Count);
        for (int i = 0; i < applyCount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            BattleDiceCardModel selected = candidates[index];
            candidates.RemoveAt(index);

            if (selected != null)
            {
                BattleDiceCardBuf_TideRevelation buf = new BattleDiceCardBuf_TideRevelation(owner, 1);
                selected.AddBufWithoutDuplication(buf);
            }
        }
    }

    private void ClearTideRevelationFromAllies()
    {
        if (owner == null || owner.IsDead() || BattleObjectManager.instance == null)
        {
            return;
        }

        List<BattleUnitModel> allies = BattleObjectManager.instance
            .GetAliveList(owner.faction)
            .Where(u => !u.IsDead() && !u.IsBreakLifeZero())
            .ToList();

        foreach (BattleUnitModel ally in allies)
        {
            if (ally?.allyCardDetail == null)
            {
                continue;
            }

            List<BattleDiceCardModel> hand = ally.allyCardDetail.GetHand();
            if (hand == null)
            {
                continue;
            }

            foreach (BattleDiceCardModel card in hand)
            {
                Steria.TibuAbilityHelper.ClearTideRevelation(card);
            }
        }
    }

    private static bool IsEquipmentCard(BattleDiceCardModel card)
    {
        if (card?.XmlData == null)
        {
            return true;
        }

        if (card.XmlData.Spec.Ranged == CardRange.Instance)
        {
            return true;
        }

        if (card.XmlData.IsEgo() || card.XmlData.IsPersonal())
        {
            return true;
        }

        return false;
    }

    private static void RefreshHandUi()
    {
        try
        {
            SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.UpdateCardList();
        }
        catch
        {
            // Ignore UI refresh errors (e.g. during enemy turns or scene unload).
        }
    }
}

public class BattleDiceCardBuf_TideRevelation : BattleDiceCardBuf
{
    protected override string keywordId => "TideRevelation";
    protected override string keywordIconId => "TideRevelation";
    private readonly BattleUnitModel _sourceOwner;

    public BattleDiceCardBuf_TideRevelation(BattleUnitModel sourceOwner, int stack)
    {
        _sourceOwner = sourceOwner;
        _stack = Math.Max(1, stack);
    }

    public BattleDiceCardBuf_TideRevelation(int stack) : this(null, stack) { }

    public override void OnUseCard(BattleUnitModel owner)
    {
        BattleUnitModel tideOwner = _sourceOwner ?? owner;
        if (tideOwner != null && !tideOwner.IsDead())
        {
            int tideAmount = UnityEngine.Random.Range(1, 4);
            PassiveAbility_9004001.AddTideStacks(tideOwner, tideAmount);
        }

        this.Destroy();
    }

    public override void OnRoundEnd()
    {
        this.Destroy();
    }
}

public class BattleUnitBuf_TideBorrowedCard : BattleUnitBuf
{
    public override bool Hide => true;

    private class BorrowedCardInfo
    {
        public BattleDiceCardModel Card;
        public BattleUnitModel OriginalOwner;
        public int OriginalCost;
    }

    private readonly List<BorrowedCardInfo> _borrowed = new List<BorrowedCardInfo>();

    public void AddBorrowedCard(BattleDiceCardModel card, BattleUnitModel originalOwner, int originalCost)
    {
        if (card == null || originalOwner == null)
        {
            return;
        }

        if (_borrowed.Any(x => x.Card == card))
        {
            return;
        }

        _borrowed.Add(new BorrowedCardInfo
        {
            Card = card,
            OriginalOwner = originalOwner,
            OriginalCost = originalCost
        });
    }

    public bool ReturnBorrowedCard(BattleDiceCardModel card)
    {
        if (card == null)
        {
            return false;
        }

        BorrowedCardInfo info = _borrowed.FirstOrDefault(x => x.Card == card);
        if (info == null)
        {
            return false;
        }

        ReturnBorrowedCardInfo(info);
        _borrowed.Remove(info);
        return true;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        ReturnAllBorrowedCards();
        this.Destroy();
    }

    public override void OnDie()
    {
        base.OnDie();
        ReturnAllBorrowedCards();
    }

    private void ReturnAllBorrowedCards()
    {
        if (_borrowed.Count == 0)
        {
            return;
        }

        foreach (BorrowedCardInfo info in _borrowed.ToList())
        {
            ReturnBorrowedCardInfo(info);
        }

        _borrowed.Clear();
    }

    private void ReturnBorrowedCardInfo(BorrowedCardInfo info)
    {
        if (info?.Card == null || info.OriginalOwner == null)
        {
            return;
        }

        BattleUnitModel currentOwner = info.Card.owner as BattleUnitModel;
        if (currentOwner?.allyCardDetail != null)
        {
            currentOwner.allyCardDetail.ExhaustACardAnywhere(info.Card);
        }

        info.Card.owner = info.OriginalOwner;
        info.Card.SetCurrentCost(info.OriginalCost);

        if (info.OriginalOwner.allyCardDetail != null)
        {
            info.OriginalOwner.allyCardDetail.AddCardToDeck(new List<BattleDiceCardModel> { info.Card });
        }
    }
}

namespace Steria
{
    public static class TibuAbilityHelper
    {
        public static bool HasTideRevelation(BattleDiceCardModel card)
        {
            if (card == null)
            {
                return false;
            }

            List<BattleDiceCardBuf> bufs = card.GetBufList();
            if (bufs == null)
            {
                return false;
            }

            return bufs.Any(b => b is BattleDiceCardBuf_TideRevelation);
        }

        public static int ConsumeTideStacks(BattleUnitModel owner, int amount, bool requireFull = false)
        {
            if (owner == null || amount <= 0)
            {
                return 0;
            }

            BattleUnitBuf_Tide tideBuf = owner.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
            if (tideBuf == null || tideBuf.stack <= 0)
            {
                return 0;
            }

            if (requireFull && tideBuf.stack < amount)
            {
                return 0;
            }

            int toConsume = Math.Min(amount, tideBuf.stack);
            int consumed = 0;
            while (consumed < toConsume)
            {
                tideBuf.ConsumeTideForBonus();
                consumed++;
            }

            return consumed;
        }

        public static void ClearTideRevelation(BattleDiceCardModel card)
        {
            if (card == null)
            {
                return;
            }

            List<BattleDiceCardBuf> bufs = card.GetBufList();
            if (bufs == null || bufs.Count == 0)
            {
                return;
            }

            for (int i = bufs.Count - 1; i >= 0; i--)
            {
                if (bufs[i] is BattleDiceCardBuf_TideRevelation)
                {
                    card.RemoveBuf(bufs[i]);
                }
            }
        }

        public static int CountNegativeStacks(BattleUnitModel target)
        {
            if (target == null)
            {
                return 0;
            }

            int total = 0;
            foreach (BattleUnitBuf buf in target.bufListDetail.GetActivatedBufList())
            {
                if (buf != null && buf.stack > 0 && buf.positiveType == BufPositiveType.Negative)
                {
                    total += buf.stack;
                }
            }

            return total;
        }

        public static void UpgradeBuffStacks(BattleUnitModel target, BufPositiveType type, int amount, bool includeTide)
        {
            if (target == null || amount <= 0)
            {
                return;
            }

            ApplyBuffStackUpgrade(target.bufListDetail.GetActivatedBufList(), type, amount, includeTide);
            ApplyBuffStackUpgrade(target.bufListDetail.GetReadyBufList(), type, amount, includeTide);
            ApplyBuffStackUpgrade(target.bufListDetail.GetReadyReadyBufList(), type, amount, includeTide);
        }

        public static void ExtendNegativeBuffsOneRound(BattleUnitModel target)
        {
            if (target == null)
            {
                return;
            }

            BattleUnitBufListDetail bufListDetail = target.bufListDetail;
            if (bufListDetail == null)
            {
                return;
            }

            // Extend active negatives to next round.
            List<BattleUnitBuf> activeBufs = bufListDetail.GetActivatedBufList();
            if (activeBufs != null)
            {
                foreach (BattleUnitBuf buf in activeBufs.ToList())
                {
                    if (!IsExtendableNegative(buf))
                    {
                        continue;
                    }

                    AddReadyClone(bufListDetail, buf, false);
                }
            }

            // Extend next-round negatives to the round after.
            List<BattleUnitBuf> readyBufs = bufListDetail.GetReadyBufList();
            if (readyBufs != null)
            {
                foreach (BattleUnitBuf buf in readyBufs.ToList())
                {
                    if (!IsExtendableNegative(buf))
                    {
                        continue;
                    }

                    AddReadyClone(bufListDetail, buf, true);
                }
            }
        }

        private static void ApplyBuffStackUpgrade(List<BattleUnitBuf> bufList, BufPositiveType type, int amount, bool includeTide)
        {
            if (bufList == null)
            {
                return;
            }

            foreach (BattleUnitBuf buf in bufList)
            {
                if (buf == null || buf.IsDestroyed() || buf.stack <= 0)
                {
                    continue;
                }

                if (buf.positiveType != type)
                {
                    continue;
                }

                if (!includeTide && buf is BattleUnitBuf_Tide)
                {
                    continue;
                }

                buf.stack += amount;
            }
        }

        private static bool IsExtendableNegative(BattleUnitBuf buf)
        {
            return buf != null && !buf.IsDestroyed() && buf.stack > 0 && buf.positiveType == BufPositiveType.Negative;
        }

        private static void AddReadyClone(BattleUnitBufListDetail bufListDetail, BattleUnitBuf source, bool toNextNext)
        {
            if (bufListDetail == null || source == null)
            {
                return;
            }

            BattleUnitBuf clone;
            try
            {
                clone = Activator.CreateInstance(source.GetType()) as BattleUnitBuf;
            }
            catch
            {
                return;
            }

            if (clone == null)
            {
                return;
            }

            clone.stack = source.stack;

            if (toNextNext)
            {
                bufListDetail.AddReadyReadyBuf(clone);
            }
            else
            {
                bufListDetail.AddReadyBuf(clone);
            }
        }
    }
}
