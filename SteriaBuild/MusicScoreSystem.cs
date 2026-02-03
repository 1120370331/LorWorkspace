using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using LOR_DiceSystem;

namespace Steria
{
    public enum SeaVoiceTrack
    {
        NightTrace,
        MorningLight,
        SeaReturn
    }

    public static class MusicScoreSystem
    {
        private sealed class SteriaMusicDiceStyleTag : MonoBehaviour
        {
        }

        private sealed class SteriaMusicCardTag : MonoBehaviour
        {
        }

        private sealed class MusicScoreQueueRunner : MonoBehaviour
        {
            public void ResetTimer()
            {
                _nextProcessTime = 0f;
            }

            private void Update()
            {
                ProcessQueue();
            }
        }

        public const string MusicDiceKeyword = "SteriaMusicDice";
        public const string SeaVoiceNightTraceKeyword = "SteriaSeaVoice_NightTrace";
        public const string SeaVoiceMorningLightKeyword = "SteriaSeaVoice_MorningLight";
        public const string SeaVoiceSeaReturnKeyword = "SteriaSeaVoice_SeaReturn";
        public const string MusicAccentKeyword = "SteriaMusicAccent"; // Reserved for future [重音]

        private static readonly Dictionary<Faction, int> _score = new Dictionary<Faction, int>();
        private static readonly Dictionary<Faction, List<SeaVoiceTrack>> _tracks = new Dictionary<Faction, List<SeaVoiceTrack>>();
        private static readonly Dictionary<Faction, int> _trackIndex = new Dictionary<Faction, int>();
        private static readonly ConditionalWeakTable<BattleDiceBehavior, HashSet<int>> _rolledMusicDiceRegistry = new ConditionalWeakTable<BattleDiceBehavior, HashSet<int>>();
        private static readonly Queue<ScoreEvent> _pendingScoreEvents = new Queue<ScoreEvent>();
        private static MusicScoreQueueRunner _queueRunner;
        private static float _nextProcessTime;
        private const float ScoreStepInterval = 0.08f;
        private static bool _active;
        private static bool _hasXiyin;
        private static readonly HashSet<int> _xiyinCardIds = new HashSet<int>
        {
            9007001, 9007002, 9007003, 9007004, 9007005, 9007006
        };

        public static bool IsActive => HasXiyinPresence;
        public static bool HasXiyinPresence
        {
            get
            {
                if (!_hasXiyin && BattleObjectManager.instance != null)
                {
                    _hasXiyin = HasXiyinInBattle();
                }
                return _hasXiyin;
            }
        }

        public static void InitializeForBattle()
        {
            ResetAll();
            EnsureQueueRunner();
            RegisterTracksFromDecks();
            MusicScoreUI.UpdateAll();
        }

        public static void RefreshTracksFromDecks()
        {
            _tracks[Faction.Player] = new List<SeaVoiceTrack>();
            _tracks[Faction.Enemy] = new List<SeaVoiceTrack>();
            _trackIndex[Faction.Player] = 0;
            _trackIndex[Faction.Enemy] = 0;
            RegisterTracksFromDecks();
            MusicScoreUI.UpdateAll();
        }

        public static void ResetAll()
        {
            _score[Faction.Player] = 0;
            _score[Faction.Enemy] = 0;
            _tracks[Faction.Player] = new List<SeaVoiceTrack>();
            _tracks[Faction.Enemy] = new List<SeaVoiceTrack>();
            _trackIndex[Faction.Player] = 0;
            _trackIndex[Faction.Enemy] = 0;
            _active = false;
            _hasXiyin = false;
            _pendingScoreEvents.Clear();
            _queueRunner?.ResetTimer();
            MusicScoreUI.UpdateAll();
        }

        public static int GetScore(Faction faction)
        {
            return _score.ContainsKey(faction) ? _score[faction] : 0;
        }

        public static bool IsMusicCard(BattleDiceCardModel card)
        {
            return card?.XmlData?.Keywords != null && card.XmlData.Keywords.Contains(MusicDiceKeyword);
        }

        public static bool IsMusicCard(DiceCardItemModel card)
        {
            return card?.ClassInfo?.Keywords != null && card.ClassInfo.Keywords.Contains(MusicDiceKeyword);
        }

        public static bool IsMusicCard(LorId cardId)
        {
            if (ItemXmlDataList.instance == null)
            {
                return false;
            }

            DiceCardXmlInfo info = ItemXmlDataList.instance.GetCardItem(cardId, false);
            return info?.Keywords != null && info.Keywords.Contains(MusicDiceKeyword);
        }

        public static bool IsMusicDiceBehaviour(BattleDiceBehavior behavior)
        {
            if (behavior == null)
            {
                return false;
            }

            if (behavior.Type == BehaviourType.Standby)
            {
                return false;
            }

            switch (behavior.Detail)
            {
                case BehaviourDetail.Slash:
                case BehaviourDetail.Penetrate:
                case BehaviourDetail.Hit:
                case BehaviourDetail.Guard:
                case BehaviourDetail.Evasion:
                    return true;
                default:
                    return false;
            }
        }

        public static int GetMusicScoreMultiplier(BattleDiceCardModel card)
        {
            if (card?.XmlData?.Keywords != null && card.XmlData.Keywords.Contains(MusicAccentKeyword))
            {
                return 2;
            }

            return 1;
        }

        private static void AddScoreImmediate(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0)
            {
                return;
            }

            if (!_hasXiyin)
            {
                _hasXiyin = HasXiyinInBattle();
            }

            if (!_active && _hasXiyin)
            {
                _active = true;
            }

            Faction faction = owner.faction;
            int max = GetMaxScore(faction);
            if (max <= 0)
            {
                return;
            }

            if (!_score.ContainsKey(faction))
            {
                _score[faction] = 0;
            }

            _score[faction] += amount;
            SteriaLogger.Log($"MusicScore: {faction} +{amount} -> {_score[faction]}/{max}");

            while (_score[faction] >= max)
            {
                _score[faction] -= max;
                TriggerSeaVoice(faction, owner);
                max = GetMaxScore(faction);
                if (max <= 0)
                {
                    break;
                }
            }

            MusicScoreUI.UpdateAll();
        }

        private struct ScoreEvent
        {
            public BattleUnitModel Owner;
            public int Amount;
            public int NoteIndex;
            public int Growth;
            public int MaxGrowth;
        }

        private static void EnsureQueueRunner()
        {
            if (_queueRunner != null)
            {
                return;
            }

            GameObject go = new GameObject("SteriaMusicScoreQueue");
            BattleManagerUI ui = SingletonBehavior<BattleManagerUI>.Instance;
            if (ui != null)
            {
                go.transform.SetParent(ui.transform, false);
            }
            _queueRunner = go.AddComponent<MusicScoreQueueRunner>();
            _nextProcessTime = 0f;
        }

        private static void EnqueueScore(BattleUnitModel owner, int amount, int noteIndex, int growth, int maxGrowth)
        {
            if (owner == null || amount <= 0)
            {
                return;
            }

            EnsureQueueRunner();
            _pendingScoreEvents.Enqueue(new ScoreEvent
            {
                Owner = owner,
                Amount = amount,
                NoteIndex = noteIndex,
                Growth = growth,
                MaxGrowth = maxGrowth
            });

            // Try to process immediately if we're not throttled, so per-die hits update on time.
            if (Time.unscaledTime >= _nextProcessTime)
            {
                ProcessQueue();
            }
        }

        private static void ProcessQueue()
        {
            if (_pendingScoreEvents.Count == 0)
            {
                return;
            }

            if (Time.unscaledTime < _nextProcessTime)
            {
                return;
            }

            ScoreEvent evt = _pendingScoreEvents.Dequeue();
            AddScoreImmediate(evt.Owner, evt.Amount);
            if (evt.NoteIndex > 0)
            {
                AilierelSoundHelper.PlayNightTraceNote(evt.Owner, evt.NoteIndex, evt.Growth, evt.MaxGrowth);
            }

            _nextProcessTime = Time.unscaledTime + ScoreStepInterval;
        }

        public static bool TryMarkRolledDice(BattleDiceBehavior behavior)
        {
            if (behavior == null)
            {
                return false;
            }

            lock (_rolledMusicDiceRegistry)
            {
                if (!_rolledMusicDiceRegistry.TryGetValue(behavior, out HashSet<int> indexes))
                {
                    indexes = new HashSet<int>();
                    _rolledMusicDiceRegistry.Add(behavior, indexes);
                }

                int index = behavior.Index;
                if (!indexes.Add(index))
                {
                    return false;
                }

                return true;
            }
        }

        public static int GetMaxScore(Faction faction)
        {
            if (BattleObjectManager.instance == null)
            {
                return 0;
            }

            int count = BattleObjectManager.instance.GetAliveList(faction)?.Count ?? 0;
            return Math.Max(0, count * 20);
        }

        public static void AddScoreFromTide(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0)
            {
                return;
            }

            if (!_hasXiyin)
            {
                _hasXiyin = HasXiyinInBattle();
            }

            if (!_active && _hasXiyin)
            {
                _active = true;
            }

            int max = GetMaxScore(owner.faction);
            if (max <= 0)
            {
                return;
            }

            EnqueueScore(owner, amount, 0, 0, 0);
        }

        public static void TryAddScoreFromBehavior(BattleDiceBehavior behavior)
        {
            if (behavior == null)
            {
                return;
            }

            BattleDiceCardModel cardModel = behavior.card?.card;
            if (!IsMusicCard(cardModel))
            {
                return;
            }

            if (!IsMusicDiceBehaviour(behavior))
            {
                return;
            }

            if (!TryMarkRolledDice(behavior))
            {
                return;
            }

            int baseValue = behavior.DiceVanillaValue;
            if (baseValue <= 0)
            {
                baseValue = behavior.DiceResultValue;
            }
            int gain = baseValue % 7 + 1;
            if (gain <= 0)
            {
                return;
            }

            int noteIndex = 0;
            int growth = 0;
            int maxGrowth = 0;
            DiceCardXmlInfo xml = cardModel?.XmlData;
            if (xml != null && xml.id.id == 9007006)
            {
                noteIndex = baseValue % 7 + 1;
                growth = AilierelAbilityHelper.GetNightTraceGrowth(behavior.owner, 3);
                maxGrowth = 3;
            }

            EnqueueScore(behavior.owner, gain, noteIndex, growth, maxGrowth);
            SteriaLogger.Log($"MusicScore gain: card={cardModel?.XmlData?.id} detail={behavior.Detail} vanilla={behavior.DiceVanillaValue} result={behavior.DiceResultValue} gain={gain}");
        }


        private static void RegisterTracksFromDecks()
        {
            if (BattleObjectManager.instance == null)
            {
                _active = false;
                _hasXiyin = false;
                return;
            }

            _hasXiyin = HasXiyinInBattle();

            bool active = false;
            active |= RegisterTracksForFaction(Faction.Player);
            active |= RegisterTracksForFaction(Faction.Enemy);
            _active = active;
        }

        private static bool HasXiyinInBattle()
        {
            IList<BattleUnitModel> units = BattleObjectManager.instance.GetList();
            if (units == null)
            {
                return false;
            }

            foreach (BattleUnitModel unit in units)
            {
                if (IsXiyinUnit(unit))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsXiyinUnit(BattleUnitModel unit)
        {
            if (unit?.passiveDetail?.PassiveList == null)
            {
                return false;
            }

            if (HasXiyinDeckCard(unit))
            {
                return true;
            }

            try
            {
                if (unit.UnitData?.unitData != null)
                {
                    object data = unit.UnitData.unitData;
                    var type = data.GetType();
                    var bookProp = AccessTools.Property(type, "bookId")
                                   ?? AccessTools.Property(type, "bookID")
                                   ?? AccessTools.Property(type, "BookId")
                                   ?? AccessTools.Property(type, "BookID");
                    if (bookProp != null)
                    {
                        object value = bookProp.GetValue(data, null);
                        if (value is int bookIdInt && bookIdInt == 99000007)
                        {
                            return true;
                        }
                        if (value is LorId bookId && bookId.id == 99000007)
                        {
                            return true;
                        }
                    }

                    var nameProp = AccessTools.Property(type, "name") ?? AccessTools.Property(type, "Name");
                    string unitName = nameProp?.GetValue(data, null) as string;
                    if (!string.IsNullOrEmpty(unitName) &&
                        (unitName.Contains("艾莉蕾尔") || unitName.IndexOf("Ailierel", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignore lookup errors
            }

            foreach (PassiveAbilityBase passive in unit.passiveDetail.PassiveList)
            {
                if (passive is global::PassiveAbility_9007001 ||
                    passive is global::PassiveAbility_9007002 ||
                    passive is global::PassiveAbility_9007003 ||
                    passive is global::PassiveAbility_9007004 ||
                    passive is global::PassiveAbility_9007005)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasXiyinDeckCard(BattleUnitModel unit)
        {
            if (unit?.allyCardDetail == null)
            {
                return false;
            }

            List<BattleDiceCardModel> deck = unit.allyCardDetail.GetAllDeck();
            if (deck == null || deck.Count == 0)
            {
                return false;
            }

            foreach (BattleDiceCardModel card in deck)
            {
                if (card == null)
                {
                    continue;
                }

                LorId cardId = card.GetID();
                if (_xiyinCardIds.Contains(cardId.id))
                {
                    return true;
                }

                DiceCardXmlInfo xml = card.XmlData;
                if (xml?.Keywords == null || xml.Keywords.Count == 0)
                {
                    continue;
                }

                if (xml.Keywords.Contains(MusicDiceKeyword) ||
                    xml.Keywords.Contains(SeaVoiceNightTraceKeyword) ||
                    xml.Keywords.Contains(SeaVoiceMorningLightKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RegisterTracksForFaction(Faction faction)
        {
            bool hasNightTrace = false;
            bool hasMorningLight = false;
            bool hasSeaReturn = false;
            bool hasMusicDice = false;
            bool hasXiyinCards = false;

            List<BattleUnitModel> units = BattleObjectManager.instance.GetAliveList(faction);
            if (units == null)
            {
                return false;
            }

            foreach (BattleUnitModel unit in units)
            {
                if (unit?.allyCardDetail == null)
                {
                    continue;
                }

                foreach (BattleDiceCardModel card in unit.allyCardDetail.GetAllDeck())
                {
                    DiceCardXmlInfo xml = card?.XmlData;
                    if (xml?.Keywords == null || xml.Keywords.Count == 0)
                    {
                        if (card != null)
                        {
                            LorId cardId = card.GetID();
                            if (_xiyinCardIds.Contains(cardId.id))
                            {
                                hasXiyinCards = true;
                            }
                        }
                        continue;
                    }

                    if (xml.Keywords.Contains(SeaVoiceNightTraceKeyword))
                    {
                        hasNightTrace = true;
                    }

                    if (xml.Keywords.Contains(SeaVoiceMorningLightKeyword))
                    {
                        hasMorningLight = true;
                    }

                    if (xml.Keywords.Contains(SeaVoiceSeaReturnKeyword))
                    {
                        hasSeaReturn = true;
                    }

                    if (xml.Keywords.Contains(MusicDiceKeyword))
                    {
                        hasMusicDice = true;
                    }

                    if (card != null)
                    {
                        LorId cardId = card.GetID();
                        if (_xiyinCardIds.Contains(cardId.id))
                        {
                            hasXiyinCards = true;
                        }
                    }

                    if (xml.Keywords.Contains(MusicDiceKeyword) ||
                        xml.Keywords.Contains(SeaVoiceNightTraceKeyword) ||
                        xml.Keywords.Contains(SeaVoiceMorningLightKeyword) ||
                        xml.Keywords.Contains(SeaVoiceSeaReturnKeyword))
                    {
                        hasXiyinCards = true;
                    }
                }
            }

            if (!_tracks.ContainsKey(faction))
            {
                _tracks[faction] = new List<SeaVoiceTrack>();
            }

            if (hasNightTrace)
            {
                _tracks[faction].Add(SeaVoiceTrack.NightTrace);
            }

            if (hasMorningLight)
            {
                _tracks[faction].Add(SeaVoiceTrack.MorningLight);
            }

            if (hasSeaReturn)
            {
                _tracks[faction].Add(SeaVoiceTrack.SeaReturn);
            }

            if (_tracks[faction].Count > 0)
            {
                string trackList = string.Join(",", _tracks[faction]);
                SteriaLogger.Log($"SeaVoice: Registered tracks for {faction}: {trackList}");
            }

            if (hasXiyinCards)
            {
                _hasXiyin = true;
            }

            return hasMusicDice || _tracks[faction].Count > 0;
        }

        private static void TriggerSeaVoice(Faction faction, BattleUnitModel source)
        {
            if (!_tracks.ContainsKey(faction) || _tracks[faction].Count == 0)
            {
                SteriaLogger.Log($"SeaVoice: No tracks registered for {faction}; progress reset.");
                MusicScoreUI.UpdateAll();
                return;
            }

            int index = _trackIndex.ContainsKey(faction) ? _trackIndex[faction] : 0;
            if (index < 0 || index >= _tracks[faction].Count)
            {
                index = 0;
            }

            SeaVoiceTrack track = _tracks[faction][index];
            _trackIndex[faction] = (index + 1) % _tracks[faction].Count;

            BattleUnitModel actor = source;
            if (actor == null || actor.IsDead())
            {
                actor = BattleObjectManager.instance?.GetAliveList(faction)?.FirstOrDefault();
            }

            switch (track)
            {
                case SeaVoiceTrack.NightTrace:
                    TriggerNightTrace(faction, actor);
                    break;
                case SeaVoiceTrack.MorningLight:
                    TriggerMorningLight(faction, actor);
                    break;
                case SeaVoiceTrack.SeaReturn:
                    TriggerSeaReturn(faction, actor);
                    break;
            }

            NotifySeaVoiceTriggered(faction);
            SteriaLogger.Log($"SeaVoice: Triggered {track} for {faction}");
            MusicScoreUI.UpdateAll();
        }

        private static void NotifySeaVoiceTriggered(Faction faction)
        {
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(faction);
            if (allies == null || allies.Count == 0)
            {
                return;
            }

            foreach (BattleUnitModel unit in allies)
            {
                if (unit == null || unit.IsDead())
                {
                    continue;
                }

                var passive = unit.passiveDetail?.PassiveList?.FirstOrDefault(p => p is global::PassiveAbility_9009002) as global::PassiveAbility_9009002;
                passive?.OnSeaVoiceTriggered();
            }
        }

        private static void TriggerNightTrace(Faction faction, BattleUnitModel actor)
        {
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            Faction enemyFaction = (faction == Faction.Player) ? Faction.Enemy : Faction.Player;
            List<BattleUnitModel> enemies = BattleObjectManager.instance.GetAliveList(enemyFaction);
            if (enemies == null || enemies.Count == 0)
            {
                return;
            }

            int totalUnits = BattleObjectManager.instance.GetAliveList()?.Count ?? 0;
            int bleedStacks = Math.Max(0, 20 - totalUnits * 2);

            foreach (BattleUnitModel target in enemies)
            {
                if (target == null || target.IsDead())
                {
                    continue;
                }

                int breakMax = target.breakDetail.GetDefaultBreakGauge();
                int damage = (int)Math.Ceiling(breakMax * 0.1f);
                if (damage > 0)
                {
                    target.TakeBreakDamage(damage, DamageType.Passive, actor, AtkResist.Normal);
                }

                if (bleedStacks > 0)
                {
                    target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, bleedStacks, actor);
                }
            }
        }

        private static void TriggerMorningLight(Faction faction, BattleUnitModel actor)
        {
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(faction);
            if (allies == null || allies.Count == 0)
            {
                return;
            }

            foreach (BattleUnitModel unit in allies)
            {
                if (unit == null || unit.IsDead())
                {
                    continue;
                }

                unit.cardSlotDetail?.RecoverPlayPoint(2);
                unit.RecoverHP(15);
                unit.breakDetail?.RecoverBreak(15);
            }
        }

        private static void TriggerSeaReturn(Faction faction, BattleUnitModel actor)
        {
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(faction);
            if (allies == null || allies.Count == 0)
            {
                return;
            }

            foreach (BattleUnitModel unit in allies)
            {
                if (unit == null || unit.IsDead())
                {
                    continue;
                }

                unit.allyCardDetail?.DrawCards(2);
                unit.cardSlotDetail?.RecoverPlayPoint(2);
            }
        }

        public static void ApplyMusicDiceStyle(BattleSimpleActionUI_Dice diceUi)
        {
            if (diceUi == null)
            {
                return;
            }

            Color face = new Color(0.05f, 0.05f, 0.05f, 1f);
            Color edge = Color.white;

            if (diceUi.img_diceFace != null)
            {
                diceUi.img_diceFace.color = face;
            }
            if (diceUi.img_diceFaceLinearDodge != null)
            {
                diceUi.img_diceFaceLinearDodge.color = edge;
            }
            if (diceUi.img_diceFaceClone != null)
            {
                diceUi.img_diceFaceClone.color = face;
            }
            if (diceUi.img_diceFaceLinearDodgeClone != null)
            {
                diceUi.img_diceFaceLinearDodgeClone.color = edge;
            }

            AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "originColor")?.SetValue(diceUi, edge);
            diceUi.SetValueColor(BattleDiceValueColor.Normal);
        }

        public static void ApplyMusicDiceStyleOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            bool isMusic = IsMusicCard(cardUi.CardModel);
            SteriaMusicCardTag cardTag = cardUi.GetComponent<SteriaMusicCardTag>();
            if (!isMusic)
            {
                if (cardTag != null)
                {
                    UnityEngine.Object.Destroy(cardTag);
                }
                ClearMusicDiceStyle(cardUi);
                return;
            }
            else if (cardTag == null)
            {
                cardUi.gameObject.AddComponent<SteriaMusicCardTag>();
            }

            try
            {
                if (cardUi.img_behaviourDetatilList != null)
                {
                    foreach (UnityEngine.UI.Image img in cardUi.img_behaviourDetatilList)
                    {
                        ApplyMonochromeIcon(img);
                    }
                }

                if (cardUi.ui_behaviourDescList != null)
                {
                    foreach (BattleDiceCard_BehaviourDescUI desc in cardUi.ui_behaviourDescList)
                    {
                        ApplyMusicDiceStyleOnDescUI(desc);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Music dice card UI style error: {ex}");
            }
        }

        public static void ApplyMusicDiceStyleOnDescUI(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc == null)
            {
                return;
            }

            BattleDiceCardUI cardUi = desc.GetComponentInParent<BattleDiceCardUI>();
            if (cardUi == null)
            {
                return;
            }

            if (!IsMusicCard(cardUi.CardModel))
            {
                ClearMusicDiceStyleOnDesc(desc);
                return;
            }

            if (desc.img_detail != null)
            {
                ApplyMonochromeIcon(desc.img_detail);
            }

            if (desc.txt_ability != null)
            {
                desc.txt_ability.color = Color.white;
            }

            if (desc.txt_range != null)
            {
                desc.txt_range.color = Color.white;
            }

            EnsureBehaviourBackground(desc);
        }

        public static void ApplyMusicDiceStyleOnOriginSlot(UI.UIOriginCardSlot slot, DiceCardItemModel cardModel)
        {
            if (slot == null)
            {
                return;
            }

            bool isMusic = IsMusicCard(cardModel);
            if (!isMusic)
            {
                ClearMusicDiceStyleOnSlot(slot.transform);
                return;
            }

            ApplyMusicDiceStyleOnOriginSlotInternal(slot);
        }

        public static void ApplyMusicDiceStyleOnDetailDescSlot(UI.UIDetailCardDescSlot desc, LorId cardId)
        {
            if (desc == null)
            {
                return;
            }

            if (!IsMusicCard(cardId))
            {
                ClearMusicDiceStyleOnSlot(desc.transform);
                return;
            }

            if (desc.img_detail != null)
            {
                ApplyMonochromeIcon(desc.img_detail);
            }
        }

        private static void ApplyMusicDiceStyleOnOriginSlotInternal(UI.UIOriginCardSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            Image[] behaviourIcons = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_BehaviourIcons")
                ?.GetValue(slot) as Image[];
            if (behaviourIcons != null)
            {
                foreach (Image img in behaviourIcons)
                {
                    ApplyMonochromeIcon(img);
                }
            }

            Image rangeIcon = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_RangeIcon")
                ?.GetValue(slot) as Image;
            if (rangeIcon != null)
            {
                ApplyMonochromeIcon(rangeIcon);
            }

            Image[] linearDodge = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_linearDodge")
                ?.GetValue(slot) as Image[];
            if (linearDodge != null)
            {
                foreach (Image img in linearDodge)
                {
                    if (img != null)
                    {
                        img.color = Color.white;
                        if (img.GetComponent<SteriaMusicDiceStyleTag>() == null)
                        {
                            img.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
                        }
                    }
                }
            }
        }

        private static bool IsIconLikeImage(Image img, BattleDiceCard_BehaviourDescUI desc)
        {
            if (img == null)
            {
                return false;
            }

            if (desc != null && img == desc.img_detail)
            {
                return true;
            }

            return IsIconLikeImage(img.gameObject.name, img.sprite != null ? img.sprite.name : null);
        }

        private static bool IsIconLikeImage(string name, string spriteName = null)
        {
            string combined = $"{name} {spriteName}".ToLowerInvariant();
            return combined.Contains("icon") || combined.Contains("detail") || combined.Contains("dice") ||
                   combined.Contains("type") || combined.Contains("behav") || combined.Contains("attack") ||
                   combined.Contains("defense") || combined.Contains("evade");
        }

        private static bool IsLikelyIconSize(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }

            float width = rect.rect.width;
            float height = rect.rect.height;
            return width > 0f && height > 0f && width <= 80f && height <= 80f;
        }

        private static bool IsBackgroundLikeImage(string name, string spriteName)
        {
            string combined = $"{name} {spriteName}".ToLowerInvariant();
            return combined.Contains("bg") || combined.Contains("back") || combined.Contains("base") ||
                   combined.Contains("frame") || combined.Contains("line") || combined.Contains("mask") ||
                   combined.Contains("shadow") || combined.Contains("plate") || combined.Contains("panel") ||
                   combined.Contains("steriamusicdicebg");
        }

        private static void ApplyMonochromeIcon(Image img)
        {
            if (img == null)
            {
                return;
            }

            img.color = Color.white;
            RefineHsv hsv = img.GetComponent<RefineHsv>();
            if (hsv == null)
            {
                hsv = img.gameObject.AddComponent<RefineHsv>();
            }
            if (img.GetComponent<SteriaMusicDiceStyleTag>() == null)
            {
                img.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
            }

            hsv.ActiveChange = true;
            hsv._HueShift = 0f;
            hsv._Saturation = 0f;
            hsv._ValueBrightness = 1.2f;
            hsv.CallUpdate();
        }

        private static void ApplyMonochromeIcon(RawImage img)
        {
            if (img == null)
            {
                return;
            }

            img.color = Color.white;
            RefineHsv hsv = img.GetComponent<RefineHsv>();
            if (hsv == null)
            {
                hsv = img.gameObject.AddComponent<RefineHsv>();
            }
            if (img.GetComponent<SteriaMusicDiceStyleTag>() == null)
            {
                img.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
            }

            hsv.ActiveChange = true;
            hsv._HueShift = 0f;
            hsv._Saturation = 0f;
            hsv._ValueBrightness = 1.2f;
            hsv.CallUpdate();
        }

        private static void ClearMusicDiceStyle(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            if (cardUi.ui_behaviourDescList != null)
            {
                foreach (BattleDiceCard_BehaviourDescUI desc in cardUi.ui_behaviourDescList)
                {
                    if (desc == null)
                    {
                        continue;
                    }

                    Transform bg = desc.transform.Find("SteriaMusicDiceBg");
                    if (bg != null)
                    {
                        UnityEngine.Object.Destroy(bg.gameObject);
                    }
                }
            }

            SteriaMusicDiceStyleTag[] tags = cardUi.GetComponentsInChildren<SteriaMusicDiceStyleTag>(true);
            foreach (SteriaMusicDiceStyleTag tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                RefineHsv hsv = tag.GetComponent<RefineHsv>();
                if (hsv != null)
                {
                    hsv._HueShift = 0f;
                    hsv._Saturation = 1f;
                    hsv._ValueBrightness = 1f;
                    hsv.CallUpdate();
                }

                Image img = tag.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                }

                RawImage raw = tag.GetComponent<RawImage>();
                if (raw != null)
                {
                    raw.color = Color.white;
                }

                UnityEngine.Object.Destroy(tag);
            }
        }

        private static void ClearMusicDiceStyleOnSlot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            SteriaMusicDiceStyleTag[] tags = root.GetComponentsInChildren<SteriaMusicDiceStyleTag>(true);
            foreach (SteriaMusicDiceStyleTag tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                RefineHsv hsv = tag.GetComponent<RefineHsv>();
                if (hsv != null)
                {
                    hsv._HueShift = 0f;
                    hsv._Saturation = 1f;
                    hsv._ValueBrightness = 1f;
                    hsv.CallUpdate();
                }

                Image img = tag.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                }

                RawImage raw = tag.GetComponent<RawImage>();
                if (raw != null)
                {
                    raw.color = Color.white;
                }

                UnityEngine.Object.Destroy(tag);
            }
        }

        private static void ClearMusicDiceStyleOnDesc(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc == null)
            {
                return;
            }

            Transform bg = desc.transform.Find("SteriaMusicDiceBg");
            if (bg != null)
            {
                UnityEngine.Object.Destroy(bg.gameObject);
            }

            SteriaMusicDiceStyleTag[] tags = desc.GetComponentsInChildren<SteriaMusicDiceStyleTag>(true);
            foreach (SteriaMusicDiceStyleTag tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                RefineHsv hsv = tag.GetComponent<RefineHsv>();
                if (hsv != null)
                {
                    hsv._HueShift = 0f;
                    hsv._Saturation = 1f;
                    hsv._ValueBrightness = 1f;
                    hsv.CallUpdate();
                }

                Image img = tag.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                }

                RawImage raw = tag.GetComponent<RawImage>();
                if (raw != null)
                {
                    raw.color = Color.white;
                }

                UnityEngine.Object.Destroy(tag);
            }
        }

        private static void EnsureBehaviourBackground(BattleDiceCard_BehaviourDescUI desc)
        {
            Transform root = desc.transform;
            Transform existing = root.Find("SteriaMusicDiceBg");
            if (existing != null)
            {
                return;
            }

            GameObject bg = new GameObject("SteriaMusicDiceBg");
            bg.transform.SetParent(root, false);
            bg.transform.SetAsFirstSibling();

            UnityEngine.UI.Image img = bg.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            img.raycastTarget = false;

            RectTransform rect = bg.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-6f, -2f);
            rect.offsetMax = new Vector2(6f, 2f);
        }
    }
}
