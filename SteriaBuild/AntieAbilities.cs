using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseMod;
using Steria;

public class PassiveAbility_9011001 : PassiveAbilityBase
{
    public enum AntieForm
    {
        Tide = 0,
        Dream = 1,
        Flow = 2
    }

    private static readonly string MOD_ID = "SteriaBuilding";
    private static readonly int[] FORM_CARD_IDS = { 9011101, 9011102, 9011103 };
    private static readonly AntieForm[] FORMS = { AntieForm.Tide, AntieForm.Dream, AntieForm.Flow };
    private static readonly int[] TIDE_DECK = { 9011201, 9011201, 9011501, 9011501, 9011202, 9011202, 9006006, 9011203, 9006002 };
    private static readonly int[] DREAM_DECK = { 9011301, 9011301, 9011501, 9011501, 9011302, 9011302, 9008002, 9011303, 9008003 };
    private static readonly int[] FLOW_DECK = { 9011401, 9011401, 9011402, 9011402, 9011403, 9011403, 9002006, 9002007, 9002008 };
    private static readonly KeywordBuf[] TIDE_EXTRA_DEBUFFS =
    {
        KeywordBuf.Weak,
        KeywordBuf.Vulnerable,
        KeywordBuf.Paralysis,
        KeywordBuf.Binding
    };

    private AntieForm _currentForm;
    private int _roundCount;
    private int _formSwitchCooldown;
    private bool _formCardsAdded;

    public AntieForm CurrentForm => _currentForm;

    public override void OnUnitCreated()
    {
        base.OnUnitCreated();
        TryAddFormCards();
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _roundCount = 0;
        _formSwitchCooldown = 0;
        _formCardsAdded = false;
        TryAddFormCards();
        RemoveFormCooldownBuf();
        ActivateForm(RandomUtil.SelectOne(FORMS), true);
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (owner == null || owner.IsDead())
        {
            return;
        }

        TryAddFormCards();
        TickFormSwitchCooldown();
        _roundCount++;
        if (owner.faction != Faction.Enemy || _roundCount <= 1 || (_roundCount - 1) % 2 != 0)
        {
            return;
        }

        ActivateForm(GetRandomOtherForm(), true);
    }

    public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        if (_currentForm == AntieForm.Dream && type == DamageType.Attack)
        {
            return 0.7f;
        }

        return base.DmgFactor(dmg, type, keyword);
    }

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        if (_currentForm == AntieForm.Dream && type == DamageType.Attack)
        {
            return 0.7f;
        }

        return base.BreakDmgFactor(dmg, type, keyword);
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);
        TryApplyFlowFormBonusDamage(behavior, behavior?.TargetDice?.owner ?? behavior?.card?.target);
    }

    public override void OnSucceedAreaAttack(BattleDiceBehavior behavior, BattleUnitModel target)
    {
        base.OnSucceedAreaAttack(behavior, target);
        TryApplyFlowFormBonusDamage(behavior, target);
    }

    public void TryApplyFlowFormBonusDamage(BattleDiceBehavior behavior, BattleUnitModel target)
    {
        if (_currentForm != AntieForm.Flow || owner == null || behavior == null)
        {
            return;
        }

        if (target == null || target == owner || target.IsDead())
        {
            return;
        }

        if (HarmonyHelpers.GetFlowEnhancementCountForDice(behavior.card, behavior.Index) <= 0)
        {
            return;
        }

        int bonusDamage = Mathf.Max(1, behavior.GetDiceVanillaMax() / 2);
        target.TakeDamage(bonusDamage, DamageType.Passive, owner);
        target.TakeBreakDamage(bonusDamage, DamageType.Passive, owner, AtkResist.Normal);
    }

    public void OnTideConsumed(int amount, BattleUnitModel enhancedTarget = null, KeywordBuf enhancedBufType = KeywordBuf.None, bool enhancedByTide = false)
    {
        if (_currentForm != AntieForm.Tide || owner == null || amount <= 0)
        {
            return;
        }

        if (!enhancedByTide || !IsTideExtraDebuffTrigger(enhancedBufType))
        {
            return;
        }

        BattleUnitModel target = enhancedTarget ?? owner.currentDiceAction?.target;
        if (target == null || target == owner || target.faction == owner.faction || target.IsDead())
        {
            SteriaLogger.Log($"Antie Tide Form: skipped extra debuff, invalid target. enhancedBuf={enhancedBufType}");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            KeywordBuf selected = ApplyRandomMinorDebuff(target);
            SteriaLogger.Log($"Antie Tide Form: applied extra {selected} to {target.UnitData?.unitData?.name} after enhancing {enhancedBufType}");
        }
    }

    public void ChangeToTide()
    {
        ActivateForm(AntieForm.Tide, true);
    }

    public void ChangeToDream()
    {
        ActivateForm(AntieForm.Dream, true);
    }

    public void ChangeToFlow()
    {
        ActivateForm(AntieForm.Flow, true);
    }

    public bool CanChangeForm(AntieForm form)
    {
        return owner != null && _formSwitchCooldown <= 0 && _currentForm != form;
    }

    public void StartFormSwitchCooldown()
    {
        if (owner == null)
        {
            return;
        }

        _formSwitchCooldown = 2;
        RefreshFormCooldownBuf();
    }

    private void TryAddFormCards()
    {
        if (_formCardsAdded || owner == null || owner.faction != Faction.Player)
        {
            return;
        }

        foreach (int cardId in FORM_CARD_IDS)
        {
            owner.personalEgoDetail.AddCard(new LorId(MOD_ID, cardId));
        }

        _formCardsAdded = true;
    }

    private AntieForm GetRandomOtherForm()
    {
        List<AntieForm> forms = new List<AntieForm>
        {
            AntieForm.Tide,
            AntieForm.Dream,
            AntieForm.Flow
        };
        forms.Remove(_currentForm);
        return RandomUtil.SelectOne(forms);
    }

    private void ActivateForm(AntieForm form, bool replaceHand)
    {
        if (owner == null)
        {
            return;
        }

        int tideStack = TakeBuffStack<BattleUnitBuf_Tide>();
        int dreamStack = TakeBuffStack<BattleUnitBuf_Dream>();
        int flowStack = TakeBuffStack<BattleUnitBuf_Flow>();
        int converted = tideStack + dreamStack + flowStack;
        _currentForm = form;
        RefreshFormMarker(form);

        switch (form)
        {
            case AntieForm.Tide:
                AddBuff(new BattleUnitBuf_Tide(), converted + 5);
                break;
            case AntieForm.Dream:
                AddBuff(new BattleUnitBuf_Dream(), converted + 7);
                break;
            case AntieForm.Flow:
                AddBuff(new BattleUnitBuf_Flow(), flowStack);
                CardAbilityHelper.AddFlowStacks(owner, tideStack + dreamStack + 8);
                break;
        }

        if (replaceHand)
        {
            ReplaceHandWithFormDeck(form);
        }

        owner.view?.speedDiceSetterUI?.DeselectAll();
        SingletonBehavior<BattleManagerUI>.Instance?.ui_unitListInfoSummary?.UpdateCharacterProfileAll();
    }

    private void TickFormSwitchCooldown()
    {
        if (_formSwitchCooldown <= 0)
        {
            RemoveFormCooldownBuf();
            return;
        }

        _formSwitchCooldown--;
        if (_formSwitchCooldown <= 0)
        {
            RemoveFormCooldownBuf();
        }
        else
        {
            RefreshFormCooldownBuf();
        }
    }

    private void RefreshFormCooldownBuf()
    {
        RemoveFormCooldownBuf();
        if (owner == null || _formSwitchCooldown <= 0)
        {
            return;
        }

        owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieFormCooldown(_formSwitchCooldown));
    }

    private void RemoveFormCooldownBuf()
    {
        owner?.bufListDetail?.GetActivatedBufList()
            ?.Where(buf => buf is BattleUnitBuf_AntieFormCooldown)
            .ToList()
            .ForEach(buf => buf.Destroy());
    }

    private void RefreshFormMarker(AntieForm form)
    {
        if (owner?.bufListDetail == null)
        {
            return;
        }

        owner.bufListDetail.GetActivatedBufList()
            .Where(buf => buf is BattleUnitBuf_AntieFormBase)
            .ToList()
            .ForEach(buf => buf.Destroy());

        switch (form)
        {
            case AntieForm.Tide:
                owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieTideForm());
                break;
            case AntieForm.Dream:
                owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieDreamForm());
                break;
            case AntieForm.Flow:
                owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieFlowForm());
                break;
        }
    }

    private void ReplaceHandWithFormDeck(AntieForm form)
    {
        if (owner?.allyCardDetail == null)
        {
            return;
        }

        int handCount = Math.Max(0, owner.allyCardDetail.GetHand()?.Count ?? 0);
        List<DiceCardXmlInfo> deck = GetFormDeck(form);
        if (deck == null || deck.Count == 0)
        {
            return;
        }

        owner.ChangeBaseDeck(deck, handCount);
    }

    private List<DiceCardXmlInfo> GetFormDeck(AntieForm form)
    {
        int[] fallback = form == AntieForm.Tide ? TIDE_DECK : form == AntieForm.Dream ? DREAM_DECK : FLOW_DECK;
        List<DiceCardXmlInfo> result = new List<DiceCardXmlInfo>();
        foreach (int id in fallback)
        {
            DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(MOD_ID, id), false)
                ?? ItemXmlDataList.instance.GetCardItem(id, false);
            if (card != null)
            {
                result.Add(card);
            }
        }
        return result;
    }

    private int TakeBuffStack<T>() where T : BattleUnitBuf
    {
        BattleUnitBuf buf = owner?.bufListDetail?.GetActivatedBufList()
            .FirstOrDefault(x => x is T);
        if (buf == null)
        {
            return 0;
        }

        int stack = Math.Max(0, buf.stack);
        buf.Destroy();
        return stack;
    }

    private void AddBuff(BattleUnitBuf buf, int stack)
    {
        if (owner == null || buf == null || stack <= 0)
        {
            return;
        }

        buf.stack = stack;
        owner.bufListDetail.AddBuf(buf);
    }

    private KeywordBuf ApplyRandomMinorDebuff(BattleUnitModel target)
    {
        KeywordBuf selected = RandomUtil.SelectOne(TIDE_EXTRA_DEBUFFS);
        HarmonyHelpers.RunWithoutTideEnhancement(() =>
        {
            target.bufListDetail.AddKeywordBufByCard(selected, 1, owner);
        });
        return selected;
    }

    private bool IsTideExtraDebuffTrigger(KeywordBuf bufType)
    {
        return TIDE_EXTRA_DEBUFFS.Contains(bufType) ||
            bufType == KeywordBuf.Bleeding ||
            bufType == KeywordBuf.Burn ||
            bufType == KeywordBuf.Decay ||
            bufType == KeywordBuf.Smoke;
    }
}

public class BattleUnitBuf_AntieFormCooldown : BattleUnitBuf
{
    protected override string keywordId => "AntieFormCooldown";
    protected override string keywordIconId => "AntieFormCooldown";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public BattleUnitBuf_AntieFormCooldown(int count)
    {
        stack = Math.Max(1, count);
    }

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        stack = Math.Max(1, stack);
    }
}

public abstract class BattleUnitBuf_AntieFormBase : BattleUnitBuf
{
    public override BufPositiveType positiveType => BufPositiveType.Positive;
}

public class BattleUnitBuf_AntieTideForm : BattleUnitBuf_AntieFormBase
{
    protected override string keywordId => "AntieTideForm";
    protected override string keywordIconId => "SeaTides";
}

public class BattleUnitBuf_AntieDreamForm : BattleUnitBuf_AntieFormBase
{
    protected override string keywordId => "AntieDreamForm";
    protected override string keywordIconId => "SteriaDream";
}

public class BattleUnitBuf_AntieFlowForm : BattleUnitBuf_AntieFormBase
{
    protected override string keywordId => "AntieFlowForm";
    protected override string keywordIconId => "SteriaFlow";
}

public class PassiveAbility_9011002 : PassiveAbilityBase
{
    private BattleUnitModel _markedTarget;
    private readonly HashSet<BattleDiceBehavior> _baseMaxBoostedDice = new HashSet<BattleDiceBehavior>();

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _markedTarget = null;
        _baseMaxBoostedDice.Clear();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        RefreshMarkedTarget();
        _baseMaxBoostedDice.Clear();
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        base.BeforeRollDice(behavior);

        if (IsClashingWithNemesis(behavior))
        {
            ApplyNemesisBaseMaxBonus(behavior);
        }
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior)
    {
        base.BeforeGiveDamage(behavior);

        if (IsWinningClashAgainstNemesis(behavior))
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 50, breakRate = 50 });
        }
    }

    public override void OnSucceedAttack(BattleDiceBehavior behavior)
    {
        base.OnSucceedAttack(behavior);
        TryMarkOnHit(behavior?.TargetDice?.owner ?? behavior?.card?.target);
    }

    public override void OnSucceedAreaAttack(BattleDiceBehavior behavior, BattleUnitModel target)
    {
        base.OnSucceedAreaAttack(behavior, target);
        TryMarkOnHit(target);
    }

    public override void OnDrawParrying(BattleDiceBehavior behavior)
    {
        base.OnDrawParrying(behavior);

        if (!IsClashingWithNemesis(behavior))
        {
            return;
        }

        BattlePlayingCardDataInUnitModel card = behavior?.card;
        if (card == null || behavior.DiceDestroyed)
        {
            return;
        }

        BattleDiceBehavior replayDice = card.CopyDiceBehaviour(behavior);
        replayDice.forbiddenBonusDice = behavior.forbiddenBonusDice;
        if (_baseMaxBoostedDice.Contains(behavior))
        {
            _baseMaxBoostedDice.Add(replayDice);
        }
        card.AddDice(replayDice);
    }

    public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        if (IsTakingLossPenaltyDamage(type))
        {
            return 1.5f;
        }

        return base.DmgFactor(dmg, type, keyword);
    }

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        if (IsTakingLossPenaltyDamage(type))
        {
            return 1.5f;
        }

        return base.BreakDmgFactor(dmg, type, keyword);
    }

    internal bool IsMarkedTarget(BattleUnitModel target)
    {
        RefreshMarkedTarget();
        return target != null && target == _markedTarget;
    }

    internal bool IsWinningClashAgainstNemesis(BattleDiceBehavior behavior)
    {
        return IsClashingWithNemesis(behavior) && behavior.DiceResultValue > behavior.TargetDice.DiceResultValue;
    }

    internal bool ShouldBoostDeflectDamage(BattleDiceBehavior behavior, BattleDiceBehavior targetDice)
    {
        return behavior != null &&
               targetDice != null &&
               targetDice.owner != null &&
               IsMarkedTarget(targetDice.owner) &&
               behavior.DiceResultValue > targetDice.DiceResultValue;
    }

    internal bool ShouldBoostEvasionRecovery(BattleDiceBehavior behavior)
    {
        return behavior != null &&
               behavior.Detail == BehaviourDetail.Evasion &&
               IsWinningClashAgainstNemesis(behavior);
    }

    internal static PassiveAbility_9011002 GetPassive(BattleUnitModel unit)
    {
        return unit?.passiveDetail?.PassiveList?
            .FirstOrDefault(x => x is PassiveAbility_9011002) as PassiveAbility_9011002;
    }

    private bool IsClashingWithNemesis(BattleDiceBehavior behavior)
    {
        return behavior?.TargetDice?.owner != null && IsMarkedTarget(behavior.TargetDice.owner);
    }

    private void ApplyNemesisBaseMaxBonus(BattleDiceBehavior behavior)
    {
        if (behavior?.behaviourInCard == null || !_baseMaxBoostedDice.Add(behavior))
        {
            return;
        }

        behavior.behaviourInCard.Dice = Math.Max(1, behavior.behaviourInCard.Dice + 2);
    }

    private bool IsTakingLossPenaltyDamage(DamageType type)
    {
        if (type != DamageType.Attack && type != DamageType.Rebound)
        {
            return false;
        }

        BattleDiceBehavior behavior = owner?.currentDiceAction?.currentBehavior;
        return IsClashingWithNemesis(behavior) && behavior.DiceResultValue < behavior.TargetDice.DiceResultValue;
    }

    private void TryMarkOnHit(BattleUnitModel target)
    {
        RefreshMarkedTarget();
        if (_markedTarget != null || owner == null || target == null || target == owner)
        {
            return;
        }

        if (target.faction == owner.faction || !IsValidMarkedTarget(target))
        {
            return;
        }

        SetMarkedTarget(target);
    }

    private void SetMarkedTarget(BattleUnitModel target)
    {
        RemoveOwnedMarks();
        _markedTarget = target;

        if (target?.bufListDetail == null)
        {
            return;
        }

        target.bufListDetail.AddBuf(new BattleUnitBuf_TidalNemesisMark(owner) { stack = 1 });
    }

    private void RefreshMarkedTarget()
    {
        if (_markedTarget == null)
        {
            return;
        }

        if (IsValidMarkedTarget(_markedTarget))
        {
            EnsureMarkBuf(_markedTarget);
            return;
        }

        RemoveOwnedMarks();
        _markedTarget = null;
    }

    private bool IsValidMarkedTarget(BattleUnitModel target)
    {
        if (target == null || target.IsDead() || target.IsKnockout())
        {
            return false;
        }

        if (BattleObjectManager.instance == null)
        {
            return true;
        }

        return BattleObjectManager.instance.GetAliveList(target.faction).Contains(target);
    }

    private void EnsureMarkBuf(BattleUnitModel target)
    {
        if (target?.bufListDetail == null)
        {
            return;
        }

        if (!target.bufListDetail.GetActivatedBufList()
            .Any(x => x is BattleUnitBuf_TidalNemesisMark mark && mark.SourceOwner == owner))
        {
            target.bufListDetail.AddBuf(new BattleUnitBuf_TidalNemesisMark(owner) { stack = 1 });
        }
    }

    private void RemoveOwnedMarks()
    {
        if (BattleObjectManager.instance == null || owner == null)
        {
            return;
        }

        foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList())
        {
            if (unit?.bufListDetail == null)
            {
                continue;
            }

            List<BattleUnitBuf> marks = unit.bufListDetail.GetActivatedBufList()
                .Where(x => x is BattleUnitBuf_TidalNemesisMark mark && mark.SourceOwner == owner)
                .ToList();

            foreach (BattleUnitBuf mark in marks)
            {
                mark.Destroy();
            }
        }
    }
}

public class BattleUnitBuf_TidalNemesisMark : BattleUnitBuf
{
    protected override string keywordId => "SteriaTidalNemesis";
    protected override string keywordIconId => "梦想宿敌";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    public BattleUnitModel SourceOwner { get; private set; }

    public BattleUnitBuf_TidalNemesisMark(BattleUnitModel sourceOwner)
    {
        SourceOwner = sourceOwner;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
    }
}

public class BattleUnitBuf_TidalNemesisTempBreakBoost : BattleUnitBuf
{
    public BattleUnitBuf_TidalNemesisTempBreakBoost()
    {
        hide = true;
    }

    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        hide = true;
    }

    public override float BreakDmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        return type == DamageType.Rebound ? 1.5f : base.BreakDmgFactor(dmg, type, keyword);
    }
}

public abstract class DiceCardSelfAbility_AntieChangeFormBase : DiceCardSelfAbilityBase
{
    protected abstract PassiveAbility_9011001.AntieForm TargetForm { get; }

    public override bool OnChooseCard(BattleUnitModel owner)
    {
        PassiveAbility_9011001 passive = GetPassive(owner);
        return passive != null && passive.CanChangeForm(TargetForm);
    }

    public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
    {
        PassiveAbility_9011001 passive = GetPassive(unit);
        if (passive == null)
        {
            return;
        }

        switch (TargetForm)
        {
            case PassiveAbility_9011001.AntieForm.Tide:
                passive.ChangeToTide();
                break;
            case PassiveAbility_9011001.AntieForm.Dream:
                passive.ChangeToDream();
                break;
            case PassiveAbility_9011001.AntieForm.Flow:
                passive.ChangeToFlow();
                break;
        }

        passive.StartFormSwitchCooldown();
    }

    private static PassiveAbility_9011001 GetPassive(BattleUnitModel unit)
    {
        return unit?.passiveDetail?.PassiveList?.FirstOrDefault(x => x is PassiveAbility_9011001) as PassiveAbility_9011001;
    }
}

public class DiceCardSelfAbility_AntieChangeTideForm : DiceCardSelfAbility_AntieChangeFormBase
{
    protected override PassiveAbility_9011001.AntieForm TargetForm => PassiveAbility_9011001.AntieForm.Tide;
}

public class DiceCardSelfAbility_AntieChangeDreamForm : DiceCardSelfAbility_AntieChangeFormBase
{
    protected override PassiveAbility_9011001.AntieForm TargetForm => PassiveAbility_9011001.AntieForm.Dream;
}

public class DiceCardSelfAbility_AntieChangeFlowForm : DiceCardSelfAbility_AntieChangeFormBase
{
    protected override PassiveAbility_9011001.AntieForm TargetForm => PassiveAbility_9011001.AntieForm.Flow;
}

