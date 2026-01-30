using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using LOR_DiceSystem;

namespace Steria
{
    public enum SeaVoiceTrack
    {
        NightTrace,
        MorningLight
    }

    public static class MusicScoreSystem
    {
        public const string MusicDiceKeyword = "SteriaMusicDice";
        public const string SeaVoiceNightTraceKeyword = "SteriaSeaVoice_NightTrace";
        public const string SeaVoiceMorningLightKeyword = "SteriaSeaVoice_MorningLight";
        public const string MusicAccentKeyword = "SteriaMusicAccent"; // Reserved for future [重音]

        private static readonly Dictionary<Faction, int> _score = new Dictionary<Faction, int>();
        private static readonly Dictionary<Faction, List<SeaVoiceTrack>> _tracks = new Dictionary<Faction, List<SeaVoiceTrack>>();
        private static readonly Dictionary<Faction, int> _trackIndex = new Dictionary<Faction, int>();
        private static bool _active;
        private static bool _hasXiyin;

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

        public static int GetMusicScoreMultiplier(BattleDiceCardModel card)
        {
            if (card?.XmlData?.Keywords != null && card.XmlData.Keywords.Contains(MusicAccentKeyword))
            {
                return 2;
            }

            return 1;
        }

        public static void AddScore(BattleUnitModel owner, int amount)
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

        public static int GetMaxScore(Faction faction)
        {
            if (BattleObjectManager.instance == null)
            {
                return 0;
            }

            int count = BattleObjectManager.instance.GetAliveList(faction)?.Count ?? 0;
            return Math.Max(0, count * 20);
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

        private static bool RegisterTracksForFaction(Faction faction)
        {
            bool hasNightTrace = false;
            bool hasMorningLight = false;
            bool hasMusicDice = false;

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

                    if (xml.Keywords.Contains(MusicDiceKeyword))
                    {
                        hasMusicDice = true;
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

            if (_tracks[faction].Count > 0)
            {
                string trackList = string.Join(",", _tracks[faction]);
                SteriaLogger.Log($"SeaVoice: Registered tracks for {faction}: {trackList}");
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
            }

            SteriaLogger.Log($"SeaVoice: Triggered {track} for {faction}");
            MusicScoreUI.UpdateAll();
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
            if (cardUi == null || !IsMusicCard(cardUi.CardModel))
            {
                return;
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
            if (cardUi == null || !IsMusicCard(cardUi.CardModel))
            {
                return;
            }

            ApplyMonochromeToDescImages(desc);

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

        private static void ApplyMonochromeToDescImages(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc == null)
            {
                return;
            }

            Image[] images = desc.GetComponentsInChildren<Image>(true);
            if (images != null)
            {
                foreach (Image img in images)
                {
                    if (IsIconLikeImage(img, desc))
                    {
                        ApplyMonochromeIcon(img);
                        continue;
                    }

                    if (IsBackgroundLikeImage(img.gameObject.name, img.sprite != null ? img.sprite.name : null))
                    {
                        img.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
                        RefineHsv hsv = img.GetComponent<RefineHsv>();
                        if (hsv != null)
                        {
                            hsv.ActiveChange = false;
                        }
                    }
                }
            }

            RawImage[] rawImages = desc.GetComponentsInChildren<RawImage>(true);
            if (rawImages != null)
            {
                foreach (RawImage raw in rawImages)
                {
                    if (IsIconLikeImage(raw.gameObject.name))
                    {
                        ApplyMonochromeIcon(raw);
                        continue;
                    }

                    if (IsBackgroundLikeImage(raw.gameObject.name, null))
                    {
                        raw.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
                        RefineHsv hsv = raw.GetComponent<RefineHsv>();
                        if (hsv != null)
                        {
                            hsv.ActiveChange = false;
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

            hsv.ActiveChange = true;
            hsv._HueShift = 0f;
            hsv._Saturation = 0f;
            hsv._ValueBrightness = 1.2f;
            hsv.CallUpdate();
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

            RectTransform rect = bg.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-6f, -2f);
            rect.offsetMax = new Vector2(6f, 2f);
        }
    }
}
