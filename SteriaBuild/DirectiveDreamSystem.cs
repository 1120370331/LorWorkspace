using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;
using Steria;

/// <summary>
/// 接收指令：梦境 系统辅助
/// </summary>
public static class DirectiveDreamHelper
{
    public static bool HasStephanieProxy(BattleUnitModel unit)
    {
        if (unit?.bufListDetail == null) return false;
        return unit.bufListDetail.GetActivatedBufList().Any(b => b is BattleUnitBuf_StephanieProxy);
    }

    public static BattleUnitBuf_Flow GetFlowBuf(BattleUnitModel unit)
    {
        return unit?.bufListDetail?.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
    }

    public static BattleUnitBuf_Dream GetDreamBuf(BattleUnitModel unit)
    {
        return unit?.bufListDetail?.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Dream) as BattleUnitBuf_Dream;
    }

    public static BattleUnitBuf_Tide GetTideBuf(BattleUnitModel unit)
    {
        return unit?.bufListDetail?.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
    }

    public static int GetFlowStacks(BattleUnitModel unit) => GetFlowBuf(unit)?.stack ?? 0;
    public static int GetDreamStacks(BattleUnitModel unit) => GetDreamBuf(unit)?.stack ?? 0;
    public static int GetTideStacks(BattleUnitModel unit) => GetTideBuf(unit)?.stack ?? 0;

    public static void AddOrStackCommandProtection(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;

        BattleUnitBuf_CommandProtection buf = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_CommandProtection) as BattleUnitBuf_CommandProtection;

        if (buf != null)
        {
            buf.stack = Mathf.Clamp(buf.stack + amount, 0, 10);
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_CommandProtection { stack = Mathf.Clamp(amount, 0, 10) });
        }
    }

    public static void AddOrStackKarma(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0) return;

        BattleUnitBuf_KarmaLachesis buf = unit.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_KarmaLachesis) as BattleUnitBuf_KarmaLachesis;

        if (buf != null)
        {
            buf.stack = Mathf.Clamp(buf.stack + amount, 0, 10);
        }
        else
        {
            unit.bufListDetail.AddBuf(new BattleUnitBuf_KarmaLachesis { stack = Mathf.Clamp(amount, 0, 10) });
        }
    }

    public static bool IsFlowTransferCard(BattleDiceCardModel card)
    {
        int id = card?.GetID().id ?? -1;
        if (id < 0) return false;
        return HarmonyHelpers.IsFlowTransferCard(id) ||
               card.XmlData?.Keywords?.Contains("SteriaFlowTransfer") == true;
    }

    public static bool HasDreamRivalMark(BattleUnitModel unit)
    {
        return unit?.bufListDetail?.GetActivatedBufList()?.Any(b => b is BattleUnitBuf_DreamRivalMark) == true;
    }

    public static bool HasDreamDirectiveCardMark(BattleDiceCardModel card)
    {
        return card?.GetBufList()?.Any(b => b is BattleDiceCardBuf_DreamDirectiveMark) == true;
    }
}

/// <summary>
/// 接收指令：梦境 的回合级协调器（按阵营共享）
/// </summary>
public static class DirectiveDreamRoundCoordinator
{
    private class FactionRoundState
    {
        public int Round = -1;
        public BattleUnitModel RivalTarget;
        public readonly HashSet<int> CardMarkedUnitIndexes = new HashSet<int>();
    }

    private static readonly Dictionary<Faction, FactionRoundState> _states = new Dictionary<Faction, FactionRoundState>
    {
        { Faction.Player, new FactionRoundState() },
        { Faction.Enemy, new FactionRoundState() }
    };

    private static FactionRoundState GetState(Faction faction)
    {
        if (!_states.TryGetValue(faction, out FactionRoundState state))
        {
            state = new FactionRoundState();
            _states[faction] = state;
        }
        return state;
    }

    private static int CurrentRound => Singleton<StageController>.Instance?.RoundTurn ?? 0;

    private static Faction OppositeFaction(Faction faction)
    {
        return faction == Faction.Player ? Faction.Enemy : Faction.Player;
    }

    private static void ResetStateIfNeeded(Faction sourceFaction)
    {
        FactionRoundState state = GetState(sourceFaction);
        int round = CurrentRound;
        if (state.Round == round) return;

        state.Round = round;
        state.RivalTarget = null;
        state.CardMarkedUnitIndexes.Clear();
    }

    private static List<BattleUnitModel> GetAliveEnemiesWithRivalMark(Faction sourceFaction)
    {
        if (BattleObjectManager.instance == null) return new List<BattleUnitModel>();

        Faction enemyFaction = OppositeFaction(sourceFaction);
        return BattleObjectManager.instance.GetAliveList(enemyFaction)
            .Where(u => u != null && !u.IsDead() && DirectiveDreamHelper.HasDreamRivalMark(u))
            .ToList();
    }

    private static void RemoveAllRivalMarks(BattleUnitModel unit)
    {
        if (unit?.bufListDetail == null) return;

        List<BattleUnitBuf> marks = unit.bufListDetail.GetActivatedBufList()
            .Where(b => b is BattleUnitBuf_DreamRivalMark)
            .ToList();

        for (int i = 0; i < marks.Count; i++)
        {
            marks[i].Destroy();
        }
    }

    public static void BeginRoundForFaction(Faction sourceFaction)
    {
        ResetStateIfNeeded(sourceFaction);
    }

    public static BattleUnitModel EnsureSingleRivalTargetForFaction(BattleUnitModel owner)
    {
        if (owner == null || owner.IsDead() || BattleObjectManager.instance == null)
        {
            return null;
        }

        Faction sourceFaction = owner.faction;
        ResetStateIfNeeded(sourceFaction);
        FactionRoundState state = GetState(sourceFaction);

        if (state.RivalTarget != null && !state.RivalTarget.IsDead())
        {
            if (!DirectiveDreamHelper.HasDreamRivalMark(state.RivalTarget))
            {
                state.RivalTarget.bufListDetail.AddBuf(new BattleUnitBuf_DreamRivalMark { stack = 1 });
            }
            return state.RivalTarget;
        }

        List<BattleUnitModel> markedEnemies = GetAliveEnemiesWithRivalMark(sourceFaction);
        if (markedEnemies.Count > 1)
        {
            BattleUnitModel keep = markedEnemies[0];
            for (int i = 1; i < markedEnemies.Count; i++)
            {
                RemoveAllRivalMarks(markedEnemies[i]);
            }
            state.RivalTarget = keep;
            return keep;
        }

        if (markedEnemies.Count == 1)
        {
            state.RivalTarget = markedEnemies[0];
            return state.RivalTarget;
        }

        Faction enemyFaction = OppositeFaction(sourceFaction);
        List<BattleUnitModel> enemies = BattleObjectManager.instance.GetAliveList(enemyFaction)
            .Where(u => u != null && !u.IsDead())
            .ToList();

        if (enemies.Count == 0)
        {
            return null;
        }

        BattleUnitModel selected = enemies[UnityEngine.Random.Range(0, enemies.Count)];
        selected.bufListDetail.AddBuf(new BattleUnitBuf_DreamRivalMark { stack = 1 });
        state.RivalTarget = selected;
        SteriaLogger.Log($"DirectiveDream: Created shared rival target={selected.UnitData?.unitData?.name}, sourceFaction={sourceFaction}");
        return selected;
    }

    public static BattleUnitModel GetUniqueMarkedEnemyForAttacker(BattleUnitModel attacker)
    {
        if (attacker == null || attacker.IsDead() || BattleObjectManager.instance == null)
        {
            return null;
        }

        List<BattleUnitModel> markedEnemies = GetAliveEnemiesWithRivalMark(attacker.faction);
        if (markedEnemies.Count == 1)
        {
            return markedEnemies[0];
        }

        if (markedEnemies.Count > 1)
        {
            BattleUnitModel keep = markedEnemies[0];
            for (int i = 1; i < markedEnemies.Count; i++)
            {
                RemoveAllRivalMarks(markedEnemies[i]);
            }
            return keep;
        }

        return null;
    }

    public static bool FactionHasDirectiveDreamPassive(Faction faction)
    {
        if (BattleObjectManager.instance == null)
        {
            return false;
        }

        return BattleObjectManager.instance.GetAliveList(faction)
            .Any(u => u != null && !u.IsDead() && u.passiveDetail?.PassiveList?.Any(p => p is PassiveAbility_9002010) == true);
    }

    public static void MarkDirectiveCardForUnit(BattleUnitModel owner)
    {
        if (owner == null || owner.IsDead() || owner.allyCardDetail == null)
        {
            return;
        }

        Faction sourceFaction = owner.faction;
        ResetStateIfNeeded(sourceFaction);
        FactionRoundState state = GetState(sourceFaction);

        if (state.CardMarkedUnitIndexes.Contains(owner.index))
        {
            return;
        }

        List<BattleDiceCardModel> hand = owner.allyCardDetail.GetHand();
        if (hand == null || hand.Count == 0)
        {
            return;
        }

        List<BattleDiceCardModel> candidates = hand
            .Where(c => c != null && !DirectiveDreamHelper.HasDreamDirectiveCardMark(c))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        BattleDiceCardModel selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        selected.AddBufWithoutDuplication(new BattleDiceCardBuf_DreamDirectiveMark(owner));
        state.CardMarkedUnitIndexes.Add(owner.index);

        SteriaLogger.Log($"DirectiveDream: Marked directive card owner={owner.UnitData?.unitData?.name}, card={selected.GetName()}");
    }

    public static BattleDiceCardModel GetPrioritizedMarkedCard(BattleUnitModel owner, BattleDiceCardModel origin)
    {
        List<BattleDiceCardModel> hand = owner?.allyCardDetail?.GetHand();
        if (hand == null || hand.Count == 0)
        {
            return null;
        }

        if (origin != null && DirectiveDreamHelper.HasDreamDirectiveCardMark(origin))
        {
            return origin;
        }

        List<BattleDiceCardModel> markedCards = hand
            .Where(c => c != null && DirectiveDreamHelper.HasDreamDirectiveCardMark(c))
            .ToList();

        if (markedCards.Count == 0)
        {
            return null;
        }

        int originCost = origin?.GetCost() ?? int.MaxValue;

        BattleDiceCardModel preferred = markedCards
            .Where(c => c.GetCost() <= originCost)
            .OrderByDescending(c => c.GetCost())
            .FirstOrDefault();

        if (preferred != null)
        {
            return preferred;
        }

        return markedCards.OrderBy(c => c.GetCost()).FirstOrDefault();
    }
}

/// <summary>
/// 指令加护：最大层数10；受到伤害/混乱伤害 -3%*层数；10层时所有骰子威力+1
/// </summary>
public class BattleUnitBuf_CommandProtection : BattleUnitBuf
{
    protected override string keywordId => "SteriaCommandProtection";
    protected override string keywordIconId => "指令加护";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        stack = Mathf.Clamp(stack, 0, 10);
    }

    public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        int count = Mathf.Clamp(stack, 0, 10);
        return Mathf.Max(0.1f, 1f - (count * 0.03f));
    }

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        int count = Mathf.Clamp(stack, 0, 10);
        return Mathf.Max(0.1f, 1f - (count * 0.03f));
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null) return;

        if (Mathf.Clamp(stack, 0, 10) >= 10)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
    }
}

/// <summary>
/// 业-拉克西丝：最大层数10；受到伤害/混乱伤害 +3%*层数；
/// 5层时所有骰子最大值-2；10层时掷出最小值则摧毁该骰并获得1层虚弱
/// </summary>
public class BattleUnitBuf_KarmaLachesis : BattleUnitBuf
{
    protected override string keywordId => "SteriaKarmaLachesis";
    protected override string keywordIconId => "业-拉克西丝";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        stack = Mathf.Clamp(stack, 0, 10);
    }

    public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        int count = Mathf.Clamp(stack, 0, 10);
        return 1f + (count * 0.03f);
    }

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        int count = Mathf.Clamp(stack, 0, 10);
        return 1f + (count * 0.03f);
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior == null) return;

        if (Mathf.Clamp(stack, 0, 10) >= 5)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { max = -2 });
        }
    }

    public override void OnRollDice(BattleDiceBehavior behavior)
    {
        base.OnRollDice(behavior);
        if (_owner == null || behavior == null) return;
        if (Mathf.Clamp(stack, 0, 10) < 10) return;
        if (behavior.DiceDestroyed) return;

        int min = behavior.GetDiceMin();
        if (behavior.DiceResultValue <= min)
        {
            behavior.DestroyDice(DiceUITiming.Start);
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Weak, 1, _owner);
            SteriaLogger.Log($"KarmaLachesis: {_owner.UnitData?.unitData?.name} rolled min and destroyed own dice");
        }
    }
}

/// <summary>
/// 福-拉克西丝：最大层数10；掷出最小值时消耗1层，重投，并在当幕获得1层强壮
/// </summary>
public class BattleUnitBuf_FortuneLachesis : BattleUnitBuf
{
    protected override string keywordId => "SteriaFortuneLachesis";
    protected override string keywordIconId => "福-拉克西斯";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        stack = Mathf.Clamp(stack, 0, 10);
    }

    public override void ChangeDiceResult(BattleDiceBehavior behavior, ref int diceResult)
    {
        base.ChangeDiceResult(behavior, ref diceResult);
        if (_owner == null || behavior == null) return;
        if (stack <= 0) return;

        int min = behavior.GetDiceMin();
        if (diceResult > min) return;

        int max = behavior.GetDiceMax();
        int rerolled = DiceStatCalculator.MakeDiceResult(min, max, 0);
        if (rerolled < min) rerolled = min;

        diceResult = rerolled;
        stack -= 1;
        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, _owner);

        SteriaLogger.Log($"FortuneLachesis: {_owner.UnitData?.unitData?.name} rerolled min value ({min}->{diceResult}), remaining={stack}");

        if (stack <= 0)
        {
            Destroy();
        }
    }
}

/// <summary>
/// 代行-斯蒂芬妮：
/// - 自身流/梦/潮层数不会减少
/// - 流转卡牌的返还效果失效（由 HarmonyHelpers.RegisterCardUsage 侧处理）
/// - 所有消耗流/梦/潮的书页骰子威力+1
/// </summary>
public class BattleUnitBuf_StephanieProxy : BattleUnitBuf
{
    protected override string keywordId => "SteriaStephanieProxy";
    protected override string keywordIconId => "代行-斯蒂芬妮";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    private int _flowSnapshot;
    private int _dreamSnapshot;
    private int _tideSnapshot;

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        CaptureSnapshots();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        RestoreSnapshots();
        CaptureSnapshots();
    }

    public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
    {
        base.OnUseCard(card);
        if (_owner == null || card == null) return;

        bool hasResource = DirectiveDreamHelper.GetFlowStacks(_owner) > 0
            || DirectiveDreamHelper.GetDreamStacks(_owner) > 0
            || DirectiveDreamHelper.GetTideStacks(_owner) > 0
            || ChristashaAbilityHelper.GetGoldenTideStacks(_owner) > 0;

        if (hasResource)
        {
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 1 });
        }
    }

    public override void OnEndBattle(BattlePlayingCardDataInUnitModel curCard)
    {
        base.OnEndBattle(curCard);
        RestoreSnapshots();
        CaptureSnapshots();
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        RestoreSnapshots();
        CaptureSnapshots();
    }

    private void CaptureSnapshots()
    {
        if (_owner == null) return;
        _flowSnapshot = DirectiveDreamHelper.GetFlowStacks(_owner);
        _dreamSnapshot = DirectiveDreamHelper.GetDreamStacks(_owner);
        _tideSnapshot = DirectiveDreamHelper.GetTideStacks(_owner);
    }

    private void RestoreSnapshots()
    {
        if (_owner == null) return;

        BattleUnitBuf_Flow flow = DirectiveDreamHelper.GetFlowBuf(_owner);
        if (_flowSnapshot > 0)
        {
            if (flow == null)
            {
                _owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = _flowSnapshot });
            }
            else if (flow.stack < _flowSnapshot)
            {
                flow.stack = _flowSnapshot;
            }
        }

        BattleUnitBuf_Dream dream = DirectiveDreamHelper.GetDreamBuf(_owner);
        if (_dreamSnapshot > 0)
        {
            if (dream == null)
            {
                _owner.bufListDetail.AddBuf(new BattleUnitBuf_Dream { stack = _dreamSnapshot });
            }
            else if (dream.stack < _dreamSnapshot)
            {
                dream.stack = _dreamSnapshot;
            }
        }

        BattleUnitBuf_Tide tide = DirectiveDreamHelper.GetTideBuf(_owner);
        if (_tideSnapshot > 0)
        {
            if (tide == null)
            {
                _owner.bufListDetail.AddBuf(new BattleUnitBuf_Tide { stack = _tideSnapshot });
            }
            else if (tide.stack < _tideSnapshot)
            {
                tide.stack = _tideSnapshot;
            }
        }
    }
}

/// <summary>
/// 梦想宿敌 标记
/// </summary>
public class BattleUnitBuf_DreamRivalMark : BattleUnitBuf
{
    protected override string keywordId => "SteriaDreamRival";
    protected override string keywordIconId => "梦想宿敌";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        Destroy();
    }
}

/// <summary>
/// 梦想意旨（手牌标记）
/// </summary>
public class BattleDiceCardBuf_DreamDirectiveMark : BattleDiceCardBuf
{
    protected override string keywordId => "DreamDirectiveMark";
    protected override string keywordIconId => "DreamDirectiveMark";

    private readonly BattleUnitModel _sourceOwner;
    private bool _resolved;

    public BattleDiceCardBuf_DreamDirectiveMark(BattleUnitModel sourceOwner)
    {
        _sourceOwner = sourceOwner;
        _stack = 1;
    }

    public override void OnUseCard(BattleUnitModel owner, BattlePlayingCardDataInUnitModel playingCard)
    {
        if (_resolved) return;
        _resolved = true;

        if (playingCard != null)
        {
            playingCard.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 1 });
        }

        BattleUnitModel rewardOwner = _sourceOwner ?? owner;
        if (rewardOwner != null && !rewardOwner.IsDead())
        {
            rewardOwner.bufListDetail.AddBuf(new BattleUnitBuf_DirectiveMarkedCardUsedNextRound { stack = 1 });
        }

        Destroy();
    }

    public override void OnDiscard(BattleUnitModel owner, BattleDiceCardModel card)
    {
        ResolveUnused(owner);
    }

    public override void OnRoundEnd()
    {
        ResolveUnused(_sourceOwner ?? (_card?.owner as BattleUnitModel));
    }

    private void ResolveUnused(BattleUnitModel rewardOwner)
    {
        if (_resolved) return;
        _resolved = true;

        if (rewardOwner != null && !rewardOwner.IsDead())
        {
            rewardOwner.bufListDetail.AddBuf(new BattleUnitBuf_KarmaLachesisNextRound { stack = 1 });
        }

        Destroy();
    }
}

public class BattleUnitBuf_CommandProtectionNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner != null && !_owner.IsDead())
        {
            DirectiveDreamHelper.AddOrStackCommandProtection(_owner, Math.Max(1, stack));
        }
        Destroy();
    }
}

public class BattleUnitBuf_DirectiveMarkedCardUsedNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner != null && !_owner.IsDead())
        {
            _owner.cardSlotDetail?.RecoverPlayPoint(Math.Max(1, stack));
            DirectiveDreamHelper.AddOrStackCommandProtection(_owner, Math.Max(1, stack));
        }
        Destroy();
    }
}

public class BattleUnitBuf_KarmaLachesisNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        if (_owner != null && !_owner.IsDead())
        {
            DirectiveDreamHelper.AddOrStackKarma(_owner, Math.Max(1, stack));
        }
        Destroy();
    }
}

/// <summary>
/// 接收指令：梦境 (ID: 9002010)
/// - 每回合开始：随机1名敌人获得[梦想宿敌]，回合结束移除
/// - 与[梦想宿敌]拼点时，骰子威力+1
/// - 每回合首次命中[梦想宿敌]后：下回合获得1层[指令加护]
/// - 每回合开始：随机手中1张书页获得[梦想意旨]，回合结束移除
///   - 被标记书页骰子威力+1
///   - 使用后：下回合恢复1点光芒并获得1层[指令加护]
///   - 未使用：下回合获得1层[业-拉克西丝]
/// </summary>
public class PassiveAbility_9002010 : PassiveAbilityBase
{
    private bool _rivalRewardTriggered;

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        _rivalRewardTriggered = false;
        if (owner != null)
        {
            DirectiveDreamRoundCoordinator.BeginRoundForFaction(owner.faction);
        }
    }

    public override void OnRoundStartAfter()
    {
        base.OnRoundStartAfter();
        AssignRoundMarks();
        RefreshHandUi();
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);
        if (behavior?.TargetDice?.owner == null) return;

        if (behavior.TargetDice.owner.bufListDetail.GetActivatedBufList().Any(b => b is BattleUnitBuf_DreamRivalMark))
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);

        if (_rivalRewardTriggered || owner == null || behavior == null)
        {
            return;
        }

        BattleUnitModel target = behavior.card?.target ?? behavior.TargetDice?.owner;
        if (target == null)
        {
            return;
        }

        bool hitRival = target.bufListDetail.GetActivatedBufList().Any(b => b is BattleUnitBuf_DreamRivalMark);
        if (!hitRival)
        {
            return;
        }

        owner.bufListDetail.AddBuf(new BattleUnitBuf_CommandProtectionNextRound { stack = 1 });
        _rivalRewardTriggered = true;
    }

    public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
    {
        BattleUnitModel marked = DirectiveDreamRoundCoordinator.GetUniqueMarkedEnemyForAttacker(owner);
        if (marked != null)
        {
            return marked;
        }

        return base.ChangeAttackTarget(card, idx);
    }

    public override BattleDiceCardModel OnSelectCardAuto(BattleDiceCardModel origin, int currentDiceSlotIdx)
    {
        if (owner == null || owner.IsDead())
        {
            return base.OnSelectCardAuto(origin, currentDiceSlotIdx);
        }

        BattleDiceCardModel prioritized = DirectiveDreamRoundCoordinator.GetPrioritizedMarkedCard(owner, origin);
        if (prioritized != null && prioritized != origin)
        {
            SteriaLogger.Log($"DirectiveDream: Prioritized marked card for {owner.UnitData?.unitData?.name} -> {prioritized.GetName()}");
            return prioritized;
        }

        return base.OnSelectCardAuto(origin, currentDiceSlotIdx);
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        RefreshHandUi();
    }

    public override void OnDie()
    {
        base.OnDie();
    }

    private void AssignRoundMarks()
    {
        if (owner == null || owner.IsDead() || BattleObjectManager.instance == null)
        {
            return;
        }

        DirectiveDreamRoundCoordinator.EnsureSingleRivalTargetForFaction(owner);
        DirectiveDreamRoundCoordinator.MarkDirectiveCardForUnit(owner);
    }

    private static void RefreshHandUi()
    {
        try
        {
            SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.UpdateCardList();
        }
        catch
        {
        }
    }

}

[HarmonyPatch(typeof(BattleUnitModel), "ChangeAttackTarget")]
public static class HarmonyPatch_DirectiveDream_GlobalTargetPriority
{
    [HarmonyPostfix]
    public static void BattleUnitModel_ChangeAttackTarget_Postfix(BattleUnitModel __instance, ref BattleUnitModel __result)
    {
        if (__instance == null || __instance.IsDead())
        {
            return;
        }

        if (__instance.faction != Faction.Enemy)
        {
            return;
        }

        if (!DirectiveDreamRoundCoordinator.FactionHasDirectiveDreamPassive(__instance.faction))
        {
            return;
        }

        BattleUnitModel marked = DirectiveDreamRoundCoordinator.GetUniqueMarkedEnemyForAttacker(__instance);
        if (marked != null && !marked.IsDead())
        {
            __result = marked;
        }
    }
}
