using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using LOR_DiceSystem;
using UnityEngine;
using BaseMod;
using Mod;
using Sound;
using Steria;

// Ailierel passives and card abilities

public class PassiveAbility_9007001 : PassiveAbilityBase
{
    private const int Stage1Threshold = 10;
    private const int Stage2Threshold = 20;

    private int _flowConsumedTotal;
    private int _stage;

    public override void Init(BattleUnitModel self)
    {
        base.Init(self);
        _flowConsumedTotal = 0;
        _stage = 0;
    }

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _flowConsumedTotal = 0;
        _stage = 0;
    }

    public void OnFlowConsumed(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _flowConsumedTotal += amount;

        if (_flowConsumedTotal >= Stage2Threshold)
        {
            _stage = 2;
        }
        else if (_flowConsumedTotal >= Stage1Threshold)
        {
            _stage = 1;
        }
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null)
        {
            return;
        }

        if (_stage >= 2)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
            return;
        }

        if (_stage == 1 && behavior.Type == BehaviourType.Atk)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior)
    {
        if (behavior == null || behavior.Type != BehaviourType.Atk)
        {
            return;
        }

        if (_stage == 1)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 25 });
        }
        else if (_stage >= 2)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 50 });
        }
    }

    public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        if (_stage >= 2)
        {
            return 1.25f;
        }

        return base.DmgFactor(dmg, type, keyword);
    }
}

public class PassiveAbility_9007002 : PassiveAbilityBase
{
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null || behavior.card == null || owner == null)
        {
            return;
        }

        if (!behavior.IsParrying())
        {
            return;
        }

        int mySpeed = GetSpeedValue(behavior.card);
        int targetSpeed = GetTargetSpeedValue(behavior.card);
        if (mySpeed <= 0 || targetSpeed <= 0)
        {
            return;
        }

        if (mySpeed > targetSpeed)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        }
        else if (mySpeed < targetSpeed)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = -1 });
        }
    }

    private static int GetSpeedValue(BattlePlayingCardDataInUnitModel card)
    {
        if (card == null)
        {
            return -1;
        }

        if (card.speedDiceResultValue > 0)
        {
            return card.speedDiceResultValue;
        }

        BattleUnitModel unit = card.owner;
        if (unit?.speedDiceResult == null)
        {
            return -1;
        }

        int slot = card.slotOrder;
        if (slot < 0 || slot >= unit.speedDiceResult.Count)
        {
            return -1;
        }

        return unit.GetSpeedDiceResult(slot).value;
    }

    private static int GetTargetSpeedValue(BattlePlayingCardDataInUnitModel card)
    {
        if (card?.target == null)
        {
            return -1;
        }

        int targetSlotOrder = card.targetSlotOrder;
        if (targetSlotOrder < 0 || card.target.speedDiceResult == null || targetSlotOrder >= card.target.speedDiceResult.Count)
        {
            return -1;
        }

        BattlePlayingCardDataInUnitModel targetCard = null;
        if (card.target.cardSlotDetail?.cardAry != null && targetSlotOrder < card.target.cardSlotDetail.cardAry.Count)
        {
            targetCard = card.target.cardSlotDetail.cardAry[targetSlotOrder];
        }

        int targetSpeed = GetSpeedValue(targetCard);
        if (targetSpeed > 0)
        {
            return targetSpeed;
        }

        return card.target.GetSpeedDiceResult(targetSlotOrder).value;
    }
}

public class PassiveAbility_9007003 : PassiveAbilityBase
{
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null)
        {
            return;
        }

        if (behavior.Detail == BehaviourDetail.Evasion)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
        }
        else if (behavior.Detail == BehaviourDetail.Guard)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = -2 });
        }
    }
}

public class PassiveAbility_9007004 : PassiveAbilityBase
{
    public override void OnKill(BattleUnitModel target)
    {
        base.OnKill(target);
        if (owner == null)
        {
            return;
        }

        Steria.CardAbilityHelper.AddFlowStacks(owner, 5);
    }
}

public class PassiveAbility_9007005 : PassiveAbilityBase
{
    private const string MOD_ID = "SteriaBuilding";
    private const int SOUND_SEGMENT_CARD_ID = 9007099;
    private const string SOUND_SEGMENT_NAME = "音段接收";
    private const string SOUND_SEGMENT_ARTWORK = "Ailierel_SoundSegment.png";

    private bool _soundSegmentApplied;

    public override void OnWaveStart()
    {
        base.OnWaveStart();
        _soundSegmentApplied = false;
    }

    public void TryApplySoundSegmentDiceOnRoundStart()
    {
        if (_soundSegmentApplied)
        {
            return;
        }

        if (ApplySoundSegmentDice())
        {
            _soundSegmentApplied = true;
        }
    }

    public bool ApplySoundSegmentDice()
    {
        if (owner == null || owner.IsDead())
        {
            return false;
        }

        RemoveExistingSoundSegmentDice();
        bool added = AddSoundSegmentEvasionDice();
        try
        {
            owner.view?.keepUI?.Init(true);
        }
        catch
        {
            // ignore UI refresh errors
        }

        return added;
    }

    private void RemoveExistingSoundSegmentDice()
    {
        BattleKeepedCardDataInUnitModel keepCard = owner.cardSlotDetail?.keepCard;
        if (keepCard?.cardBehaviorQueue == null || keepCard.cardBehaviorQueue.Count == 0)
        {
            return;
        }

        Queue<BattleDiceBehavior> newQueue = new Queue<BattleDiceBehavior>();
        foreach (BattleDiceBehavior behavior in keepCard.cardBehaviorQueue)
        {
            if (!IsSoundSegmentDice(behavior))
            {
                newQueue.Enqueue(behavior);
            }
        }

        keepCard.cardBehaviorQueue = newQueue;
    }

    private static bool IsSoundSegmentDice(BattleDiceBehavior behavior)
    {
        if (behavior?.abilityList == null)
        {
            return false;
        }

        return behavior.abilityList.Any(a => a is DiceCardAbility_AilierelSoundSegmentReceive);
    }

    private bool AddSoundSegmentEvasionDice()
    {
        BattleKeepedCardDataInUnitModel keepCard = owner.cardSlotDetail?.keepCard;
        if (keepCard == null)
        {
            return false;
        }

        DiceCardXmlInfo cardItem = new DiceCardXmlInfo(new LorId(MOD_ID, SOUND_SEGMENT_CARD_ID))
        {
            workshopName = SOUND_SEGMENT_NAME,
            Artwork = SOUND_SEGMENT_ARTWORK,
            Rarity = Rarity.Common,
            Spec = new DiceCardSpec
            {
                Ranged = CardRange.Near,
                Cost = 0
            }
        };

        BattleDiceCardModel cardModel = BattleDiceCardModel.CreatePlayingCard(cardItem);
        if (cardModel == null)
        {
            return false;
        }

        BattleDiceBehavior evasionBehavior = new BattleDiceBehavior();
        evasionBehavior.behaviourInCard = new DiceBehaviour
        {
            Min = 1,
            Dice = 6,
            Type = BehaviourType.Def,
            Detail = BehaviourDetail.Evasion
        };

        evasionBehavior.SetIndex(0);
        evasionBehavior.AddAbility(new DiceCardAbility_AilierelSoundSegmentReceive());
        keepCard.AddBehaviourForOnlyDefense(cardModel, evasionBehavior);
        SteriaLogger.Log($"SoundSegment: Added 1-6 evasion die to {owner.UnitData?.unitData?.name} (name={SOUND_SEGMENT_NAME}, art={SOUND_SEGMENT_ARTWORK})");
        return true;
    }
}

public class DiceCardAbility_AilierelSoundSegmentReceive : DiceCardAbilityBase
{
    public override void BeforeRollDice()
    {
        if (behavior == null || owner == null)
        {
            return;
        }

        int targetCount = AilierelAbilityHelper.CountCardsTargeting(owner);
        if (targetCount > 0)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = targetCount });
        }
    }
}

public class DiceCardSelfAbility_AilierelDuskAmbush : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

public class DiceCardAbility_AilierelGainFlow1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (owner == null)
        {
            return;
        }

        Steria.CardAbilityHelper.AddFlowStacks(owner, 1);
    }
}

public class DiceCardSelfAbility_AilierelSideStep : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        owner.allyCardDetail.DrawCards(1);
    }
}

public class DiceCardAbility_AilierelSideStepLoseDestroySecond : DiceCardAbilityBase
{
    public override void OnLoseParrying()
    {
        if (behavior?.card == null)
        {
            return;
        }

        behavior.card.DestroyDice(match => match.index == 1);
    }
}

public class DiceCardAbility_AilierelDraw1 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.allyCardDetail.DrawCards(1);
    }
}

public class DiceCardSelfAbility_AilierelBigCatch : DiceCardSelfAbilityBase
{
    private int _damageDealt;

    public override void OnUseCard()
    {
        _damageDealt = 0;
    }

    public override void AfterGiveDamage(int damage, BattleUnitModel target)
    {
        if (damage > 0)
        {
            _damageDealt += damage;
        }
    }

    public override void OnEndBattle()
    {
        if (_damageDealt >= 8 && owner != null)
        {
            Steria.CardAbilityHelper.AddFlowStacks(owner, 3);
        }

        _damageDealt = 0;
    }
}

public class DiceCardAbility_AilierelBleed2 : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null)
        {
            return;
        }

        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
    }
}

public class DiceCardAbility_AilierelBleed2DestroyDice : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null)
        {
            return;
        }

        target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);

        BattlePlayingCardDataInUnitModel currentCard = target.currentDiceAction;
        if (currentCard != null)
        {
            currentCard.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
        }
    }
}

public class DiceCardSelfAbility_AilierelWander : DiceCardSelfAbilityBase
{
    private const int FlowCost = 3;

    public override void OnUseCard()
    {
        if (!AilierelAbilityHelper.TryConsumeFlow(owner, FlowCost))
        {
            return;
        }

        owner.allyCardDetail.DrawCards(2);
    }
}

public class DiceCardAbility_AilierelGainQuickness : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
    }
}

public class DiceCardSelfAbility_AilierelWhereSwim : DiceCardSelfAbilityBase
{
    public override void OnUseCard()
    {
        Steria.CardAbilityHelper.AddFlowStacks(owner, 8);
    }
}

public class DiceCardAbility_AilierelRerollIfBleed : DiceCardAbilityBase
{
    private bool _repeatTriggered;

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (_repeatTriggered || target == null)
        {
            return;
        }

        BattleUnitBuf bleed = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding);
        if (bleed != null && bleed.stack > 5)
        {
            ActivateBonusAttackDice();
            _repeatTriggered = true;
        }
    }
}

public class BattleUnitBuf_AilierelNightTraceGrowth : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;
}

public class DiceCardSelfAbility_AilierelNightTrace : DiceCardSelfAbilityBase
{
    private const int MaxGrowth = 3;

    public override bool OnChooseCard(BattleUnitModel owner)
    {
        return true;
    }

    public override int GetCostAdder(BattleUnitModel unit, BattleDiceCardModel self)
    {
        return AilierelAbilityHelper.GetNightTraceGrowth(unit, MaxGrowth);
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        int growth = AilierelAbilityHelper.GetNightTraceGrowth(owner, MaxGrowth);
        if (growth <= 0)
        {
            return;
        }

        behavior.ApplyDiceStatBonus(new DiceStatBonus
        {
            min = growth,
            max = growth * 2
        });
    }

    public override void OnEndBattle()
    {
        AilierelAbilityHelper.AddNightTraceGrowth(owner, MaxGrowth);
    }
}

public class DiceCardAbility_AilierelNightTraceBleedAndFlow : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target != null)
        {
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }

        int cost = card?.card?.GetCost() ?? 0;
        if (cost > 0 && owner != null)
        {
            Steria.CardAbilityHelper.AddFlowStacks(owner, cost);
        }
    }
}

internal static class AilierelAbilityHelper
{
    public static int GetFlowStacks(BattleUnitModel owner)
    {
        if (owner == null)
        {
            return 0;
        }

        BattleUnitBuf_Flow flow = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        return flow?.stack ?? 0;
    }

    public static bool TryConsumeFlow(BattleUnitModel owner, int amount)
    {
        if (owner == null || amount <= 0)
        {
            return false;
        }

        BattleUnitBuf_Flow flow = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        if (flow == null || flow.stack < amount)
        {
            return false;
        }

        flow.stack -= amount;
        Steria.HarmonyHelpers.NotifyPassivesOnFlowConsumed(owner, amount);

        if (flow.stack <= 0)
        {
            flow.Destroy();
        }

        return true;
    }

    public static int GetNightTraceGrowth(BattleUnitModel owner, int maxGrowth)
    {
        if (owner == null)
        {
            return 0;
        }

        BattleUnitBuf_AilierelNightTraceGrowth buf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_AilierelNightTraceGrowth) as BattleUnitBuf_AilierelNightTraceGrowth;

        return Math.Min(buf?.stack ?? 0, maxGrowth);
    }

    public static void AddNightTraceGrowth(BattleUnitModel owner, int maxGrowth)
    {
        if (owner == null)
        {
            return;
        }

        BattleUnitBuf_AilierelNightTraceGrowth buf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_AilierelNightTraceGrowth) as BattleUnitBuf_AilierelNightTraceGrowth;

        if (buf == null)
        {
            buf = new BattleUnitBuf_AilierelNightTraceGrowth { stack = 1 };
            owner.bufListDetail.AddBuf(buf);
            return;
        }

        if (buf.stack < maxGrowth)
        {
            buf.stack++;
        }
    }

    public static int CountCardsTargeting(BattleUnitModel target)
    {
        if (target == null || BattleObjectManager.instance == null)
        {
            return 0;
        }

        int count = 0;
        List<BattleUnitModel> units = BattleObjectManager.instance.GetAliveList();
        foreach (BattleUnitModel unit in units)
        {
            if (unit?.cardSlotDetail?.cardAry == null)
            {
                continue;
            }

            foreach (BattlePlayingCardDataInUnitModel card in unit.cardSlotDetail.cardAry)
            {
                if (card == null || card.isDestroyed)
                {
                    continue;
                }

                if (card.target == target)
                {
                    count++;
                }
            }
        }

        return count;
    }
}

internal static class AilierelSoundHelper
{
    private static readonly string[] NightTraceNotes = { "C", "D", "E", "F", "G", "A", "B" };
    private const string NightTraceAudioPrefix = "XiyinNightTrace_";
    private static readonly Dictionary<string, AudioClip> _noteClips = new Dictionary<string, AudioClip>();
    private static bool _loaded;
    private const float FastDurationSeconds = 0.9f;
    private const float MinVolume = 1.0f;
    private const float MaxVolume = 1.8f;
    private const float MinPitch = 0.98f;
    private const float MaxPitch = 1.04f;

    public static void PlayNightTraceNote(BattleUnitModel owner, int noteIndex, int growth, int maxGrowth)
    {
        if (noteIndex < 1)
        {
            noteIndex = 1;
        }
        if (noteIndex > NightTraceNotes.Length)
        {
            noteIndex = NightTraceNotes.Length;
        }

        EnsureLoaded();

        string note = NightTraceNotes[noteIndex - 1];
        if (!_noteClips.TryGetValue(note, out AudioClip clip) || clip == null)
        {
            SteriaLogger.Log($"NightTrace: Missing audio for note {note} (index {noteIndex}).");
            return;
        }

        GameObject sourceObject = new GameObject($"NightTrace_{note}");
        if (owner?.view?.transform != null)
        {
            sourceObject.transform.SetParent(owner.view.transform, false);
        }

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        ApplyMusicIndicators(source, growth, maxGrowth);
        source.clip = clip;
        source.Play();

        UnityEngine.Object.Destroy(sourceObject, FastDurationSeconds);
    }

    private static void ApplyMusicIndicators(AudioSource source, int growth, int maxGrowth)
    {
        if (source == null)
        {
            return;
        }

        float t = 0f;
        if (maxGrowth > 0)
        {
            t = Mathf.Clamp01((float)growth / maxGrowth);
        }

        float smooth = t * t * (3f - 2f * t);
        float volume = Mathf.Lerp(MinVolume, MaxVolume, smooth);
        float pitch = Mathf.Lerp(MinPitch, MaxPitch, smooth);

        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        string modId = Tools.GetModId(Assembly.GetExecutingAssembly());
        string modPath = Singleton<ModContentManager>.Instance?.GetModPath(modId);
        if (string.IsNullOrEmpty(modPath))
        {
            SteriaLogger.Log("NightTrace: Mod path not found; audio will not load.");
            return;
        }

        string folder = Path.Combine(modPath, "Resource", "CustomAudio");
        foreach (string note in NightTraceNotes)
        {
            AudioClip clip = TryLoadNoteClip(folder, note);
            if (clip != null)
            {
                _noteClips[note] = clip;
            }
        }
    }

    private static AudioClip TryLoadNoteClip(string folder, string note)
    {
        string[] baseNames =
        {
            NightTraceAudioPrefix + note,
            note
        };
        string[] exts = { ".mp3", ".wav", ".ogg" };

        foreach (string baseName in baseNames)
        {
            foreach (string ext in exts)
            {
                string path = Path.Combine(folder, baseName + ext);
                if (!File.Exists(path))
                {
                    continue;
                }

                AudioClip clip = Tools.GetAudio(path, baseName);
                if (clip != null)
                {
                    return clip;
                }
            }
        }

        return null;
    }
}
