using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;

namespace Steria
{
    // Match the book/card identity, not the shared skin or reusable effect class.
    internal static class MemoryFlameAudio
    {
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> Failed = new HashSet<string>();
        private static readonly FieldInfo SoundPrefab = AccessTools.Field(typeof(CharacterSound), "_soundPrefab");

        internal static string Select(LorId id, DiceBehaviour raw, DiceBehaviourResultData result,
            DiceBehaviourResultData opponent, bool opponentHasBehaviour, ActionDetail motion)
        {
            if (id == null || id.packageId != "SteriaBuilding" || raw == null ||
                result.result != Result.Win || result.actionType != ActionType.Atk ||
                result.range != CardRange.Near || result.skip || raw.Type != BehaviourType.Atk)
                return null;
            switch (id.id)
            {
                case 9001002: case 9001003: case 9001004:
                case 9001005: case 9001009: case 9001010: break;
                default: return null;
            }
            // The native near-vs-ranged win is a projectile clash, not a hit sound.
            if (opponentHasBehaviour && opponent.range == CardRange.Far && opponent.actionType == ActionType.Atk)
                return null;
            string action = raw.ActionScript;
            if (!string.IsNullOrEmpty(action) && action != "Steria_MemoryConcerto_Long" &&
                action != "Steria_MemoryConcerto_Medium" && action != "Steria_MemoryConcerto_Final")
                return null;
            if (motion != result.actionDetail || result.behaviourDetail != raw.Detail) return null;
            string cue = null, profile = null;
            if (motion == ActionDetail.Slash && raw.Detail == BehaviourDetail.Slash) { cue = "slash"; profile = "MemorySlash"; }
            if (motion == ActionDetail.Hit && raw.Detail == BehaviourDetail.Hit) { cue = "hit"; profile = "MemoryHit"; }
            if (motion == ActionDetail.Penetrate && raw.Detail == BehaviourDetail.Penetrate) { cue = "pierce"; profile = "MemoryPierce"; }
            if (cue == null || (raw.EffectRes != "Steria_AnhierTexture" + profile && raw.EffectRes != "Steria_" + profile))
                return null;
            return cue;
        }

        private static AudioClip Load(string cue)
        {
            AudioClip clip;
            if (Clips.TryGetValue(cue, out clip) && clip != null) return clip;
            if (Failed.Contains(cue)) return null;
            clip = null;
            try
            {
                int frames = cue == "slash" ? 20286 : cue == "hit" ? 24255 : cue == "pierce" ? 15876 : 0;
                if (frames == 0) return null;
                string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(typeof(MemoryFlameAudio).Assembly.CodeBase).Path));
                string directory = Path.Combine(Directory.GetParent(assemblyDir).FullName, "Resource", "CustomAudio", "MemoryFlame");
                float[] samples = SkillPcmWave.ReadStereo44100(Path.Combine(directory, cue + ".wav"), frames);
                clip = AudioClip.Create("Steria_MemoryFlame_" + cue, frames, 2, 44100, false);
                if (clip == null || !clip.SetData(samples, 0)) throw new InvalidDataException("PCM upload failed");
                Clips[cue] = clip;
                return clip;
            }
            catch (Exception ex)
            {
                if (clip != null) UnityEngine.Object.Destroy(clip);
                Failed.Add(cue); // A broken optional file should not retry/log on every attack.
                Debug.LogWarning("[Steria] Memory flame " + cue + " unavailable; native sound retained: " + ex.Message);
                return null;
            }
        }

        internal static bool TryPlay(BattleUnitView view, string cue)
        {
            BattleEffectSound player = null;
            try
            {
                if (view == null || view.charAppearance == null || view.charAppearance.soundInfo == null || SoundPrefab == null)
                    return false;
                var prefab = SoundPrefab.GetValue(view.charAppearance.soundInfo) as BattleEffectSound;
                var manager = SingletonBehavior<BattleSoundManager>.Instance;
                if (prefab == null || manager == null) return false;
                AudioClip clip = Load(cue);
                if (clip == null) return false;
                // Same player, parent, FX volume and native lifecycle as CharacterSound.PlaySound.
                player = UnityEngine.Object.Instantiate<BattleEffectSound>(prefab, manager.transform);
                player.Init(clip, manager.VolumeFX, false);
                return true;
            }
            catch (Exception ex)
            {
                if (player != null) UnityEngine.Object.Destroy(player.gameObject);
                if (Failed.Add("play:" + cue)) Debug.LogWarning("[Steria] Memory flame playback fell back: " + ex.Message);
                return false;
            }
        }
    }

    // PrintEffect calls this at the real PRE/POST event. No OnUseCard timer,
    // extra damage callback, effect-layer audio or shared CharacterSound mutation.
    [HarmonyPatch(typeof(RencounterManager), "PrintSound")]
    internal static class MemoryFlamePrintSoundPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(BattleUnitView view, BattleCardBehaviourResult selfResult,
            BattleCardBehaviourResult opponentResult, ActionDetail actionDetail)
        {
            if (selfResult == null || !selfResult.hasBehaviour || selfResult.isBlockedDice ||
                selfResult.cardModel == null || opponentResult == null) return true;
            string cue = MemoryFlameAudio.Select(selfResult.cardModel.GetID(), selfResult.behaviourRawData,
                selfResult.diceBehaviourResult, opponentResult.diceBehaviourResult, opponentResult.hasBehaviour, actionDetail);
            return cue == null || !MemoryFlameAudio.TryPlay(view, cue);
        }
    }
}
