using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using LOR_DiceSystem;

namespace Steria
{
    /// <summary>
    /// Steria自定义EGO冷却管理器
    /// 用于管理装备书页的冷却，不依赖游戏内置的情感硬币系统
    /// </summary>
    public static class SteriaEgoCooldownManager
    {
        // 存储每个单位每张卡牌的冷却状态
        // Key: (unitIndex, cardId) -> 剩余冷却回合数
        private static Dictionary<(int unitIndex, int cardId), int> _cooldowns = new Dictionary<(int, int), int>();

        // 注册的卡牌冷却配置
        // Key: cardId -> 冷却回合数
        private static Dictionary<int, int> _cardCooldownConfig = new Dictionary<int, int>();

        /// <summary>
        /// 注册一张卡牌的冷却配置
        /// </summary>
        public static void RegisterCardCooldown(int cardId, int cooldownRounds)
        {
            _cardCooldownConfig[cardId] = cooldownRounds;
            SteriaLogger.Log($"SteriaEgoCooldown: Registered card {cardId} with {cooldownRounds} rounds cooldown");
        }

        /// <summary>
        /// 检查卡牌是否已注册冷却
        /// </summary>
        public static bool IsCardRegistered(int cardId)
        {
            return _cardCooldownConfig.ContainsKey(cardId);
        }

        /// <summary>
        /// 获取卡牌的最大冷却回合数
        /// </summary>
        public static int GetMaxCooldown(int cardId)
        {
            return _cardCooldownConfig.TryGetValue(cardId, out int cd) ? cd : 0;
        }

        /// <summary>
        /// 检查单位的卡牌是否在冷却中
        /// </summary>
        public static bool IsOnCooldown(BattleUnitModel unit, int cardId)
        {
            if (unit == null) return false;
            var key = (unit.index, cardId);
            return _cooldowns.TryGetValue(key, out int cd) && cd > 0;
        }

        /// <summary>
        /// 获取剩余冷却回合数
        /// </summary>
        public static int GetRemainingCooldown(BattleUnitModel unit, int cardId)
        {
            if (unit == null) return 0;
            var key = (unit.index, cardId);
            return _cooldowns.TryGetValue(key, out int cd) ? cd : 0;
        }

        /// <summary>
        /// 获取冷却进度 (0.0 - 1.0)
        /// </summary>
        public static float GetCooldownProgress(BattleUnitModel unit, int cardId)
        {
            int maxCd = GetMaxCooldown(cardId);
            if (maxCd <= 0) return 1f; // 无冷却，直接可用

            int remaining = GetRemainingCooldown(unit, cardId);
            if (remaining <= 0) return 1f; // 冷却完成

            // 进度 = (最大冷却 - 剩余冷却) / 最大冷却
            return (float)(maxCd - remaining) / maxCd;
        }

        /// <summary>
        /// 设置卡牌进入冷却
        /// </summary>
        public static void StartCooldown(BattleUnitModel unit, int cardId)
        {
            if (unit == null) return;
            int maxCd = GetMaxCooldown(cardId);
            if (maxCd <= 0) return;

            var key = (unit.index, cardId);
            _cooldowns[key] = maxCd;
            SteriaLogger.Log($"SteriaEgoCooldown: Card {cardId} entered cooldown ({maxCd} rounds) for unit {unit.index}");
        }

        /// <summary>
        /// 减少所有单位的冷却（每回合开始时调用）
        /// </summary>
        public static void ReduceAllCooldowns()
        {
            var keysToUpdate = _cooldowns.Keys.ToList();
            foreach (var key in keysToUpdate)
            {
                if (_cooldowns[key] > 0)
                {
                    _cooldowns[key]--;
                    SteriaLogger.Log($"SteriaEgoCooldown: Reduced cooldown for unit {key.unitIndex}, card {key.cardId} to {_cooldowns[key]}");
                }
            }
        }

        /// <summary>
        /// 清除所有冷却（战斗结束时调用）
        /// </summary>
        public static void ClearAllCooldowns()
        {
            _cooldowns.Clear();
            SteriaLogger.Log("SteriaEgoCooldown: Cleared all cooldowns");
        }

        /// <summary>
        /// 清除特定单位的所有冷却
        /// </summary>
        public static void ClearUnitCooldowns(BattleUnitModel unit)
        {
            if (unit == null) return;
            var keysToRemove = _cooldowns.Keys.Where(k => k.unitIndex == unit.index).ToList();
            foreach (var key in keysToRemove)
            {
                _cooldowns.Remove(key);
            }
        }
    }

    /// <summary>
    /// Harmony补丁：拦截EGO卡牌的冷却显示和可用性检查
    /// </summary>
    public static class SteriaEgoCooldownPatches
    {
        /// <summary>
        /// 补丁：在SetEgoCoolTimeGauge之前检查自定义冷却
        /// </summary>
        [HarmonyPatch(typeof(BattleDiceCardUI), "SetEgoCoolTimeGauge")]
        public static class BattleDiceCardUI_SetEgoCoolTimeGauge_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(BattleDiceCardUI __instance)
            {
                // 获取卡牌模型
                var cardModel = __instance.CardModel;
                if (cardModel == null) return true;

                int cardId = cardModel.GetID().IsBasic() ? cardModel.GetID().id : cardModel.GetID().id;

                // 检查是否是我们管理的卡牌
                if (!SteriaEgoCooldownManager.IsCardRegistered(cardId))
                {
                    return true; // 不是我们的卡牌，使用原版逻辑
                }

                // 获取当前选中的单位
                BattleUnitModel selectedUnit = null;
                try
                {
                    selectedUnit = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.SelectedModel;
                }
                catch { }

                if (selectedUnit == null) return true;

                // 使用反射获取私有字段
                var ob_EgoCoolTime = AccessTools.Field(typeof(BattleDiceCardUI), "ob_EgoCoolTime")?.GetValue(__instance) as GameObject;
                var rect_Gauge = AccessTools.Field(typeof(BattleDiceCardUI), "rect_Gauge")?.GetValue(__instance) as RectTransform;
                var img_Bg = AccessTools.Field(typeof(BattleDiceCardUI), "img_Bg")?.GetValue(__instance) as Image;
                var img_BgGlow = AccessTools.Field(typeof(BattleDiceCardUI), "img_BgGlow")?.GetValue(__instance) as Image;
                var hsv_bgGlow = AccessTools.Field(typeof(BattleDiceCardUI), "hsv_bgGlow")?.GetValue(__instance) as RefineHsv;
                var gaugeLength = (float)(AccessTools.Field(typeof(BattleDiceCardUI), "gaugeLength")?.GetValue(__instance) ?? 950f);

                if (ob_EgoCoolTime == null) return true;

                // 检查自定义冷却状态
                float progress = SteriaEgoCooldownManager.GetCooldownProgress(selectedUnit, cardId);

                if (progress >= 1f)
                {
                    // 冷却完成，可以使用
                    ob_EgoCoolTime.SetActive(false);
                    AccessTools.Field(typeof(BattleDiceCardUI), "isEgoCoolTimeLock")?.SetValue(__instance, false);

                    // 调用SetEgoFrameLockColor(false)
                    var setEgoFrameLockColor = AccessTools.Method(typeof(BattleDiceCardUI), "SetEgoFrameLockColor");
                    setEgoFrameLockColor?.Invoke(__instance, new object[] { false });
                }
                else
                {
                    // 还在冷却中
                    ob_EgoCoolTime.SetActive(true);

                    // 更新冷却条位置
                    if (rect_Gauge != null)
                    {
                        rect_Gauge.anchoredPosition = new Vector2(gaugeLength * progress, 0f);
                    }

                    // 更新视觉效果
                    if (hsv_bgGlow != null)
                    {
                        hsv_bgGlow.CallUpdate();
                    }

                    if (progress < 0.7f)
                    {
                        if (img_BgGlow != null)
                        {
                            Color color = img_BgGlow.color;
                            color.a = 1f;
                            img_BgGlow.color = color;
                        }
                        if (hsv_bgGlow != null)
                        {
                            hsv_bgGlow._ValueBrightness = 0.3f;
                        }
                        if (img_Bg != null)
                        {
                            Color white = Color.white;
                            white.a = 0.3f;
                            img_Bg.color = white;
                        }
                    }
                    else
                    {
                        if (hsv_bgGlow != null)
                        {
                            hsv_bgGlow._ValueBrightness = progress * 1.2f;
                        }
                        if (img_Bg != null)
                        {
                            Color white = Color.white;
                            white.a = 0.7f;
                            img_Bg.color = white;
                        }
                    }

                    // 调用SetEgoLock()
                    var setEgoLock = AccessTools.Method(typeof(BattleDiceCardUI), "SetEgoLock");
                    setEgoLock?.Invoke(__instance, null);
                }

                return false; // 跳过原版逻辑
            }
        }

        /// <summary>
        /// 补丁：在回合开始时减少冷却
        /// </summary>
        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
        public static class StageController_RoundStartPhase_ReduceCooldown_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                SteriaEgoCooldownManager.ReduceAllCooldowns();
            }
        }

        /// <summary>
        /// 补丁：在战斗结束时清除冷却
        /// </summary>
        [HarmonyPatch(typeof(StageController), "EndBattle")]
        public static class StageController_EndBattle_ClearCooldown_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                SteriaEgoCooldownManager.ClearAllCooldowns();
            }
        }
    }
}
