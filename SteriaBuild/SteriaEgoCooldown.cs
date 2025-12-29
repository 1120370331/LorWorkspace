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
    /// 使用"使用时的回合数"来计算冷却，而不是递减计数
    /// 这样可以避免重复调用导致的问题
    /// </summary>
    public static class SteriaEgoCooldownManager
    {
        // 存储每个单位每张卡牌的使用回合
        // Key: (unitIndex, cardId) -> 使用时的回合数
        private static Dictionary<(int unitIndex, int cardId), int> _usedAtRound = new Dictionary<(int, int), int>();

        // 注册的卡牌冷却配置
        // Key: cardId -> 冷却回合数
        private static readonly Dictionary<int, int> _cardCooldownConfig = new Dictionary<int, int>
        {
            { 9001011, 4 },  // 安希尔 - 自我之流 - 4幕冷却
            { 9004005, 1 },  // 薇莉亚 - 潜力观测 - 1幕冷却
        };

        /// <summary>
        /// 获取当前回合数
        /// </summary>
        private static int GetCurrentRound()
        {
            try
            {
                return Singleton<StageController>.Instance?.RoundTurn ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 注册一张卡牌的冷却配置（运行时添加）
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
            if (!_usedAtRound.TryGetValue(key, out int usedRound))
            {
                return false; // 从未使用过，不在冷却中
            }

            int maxCd = GetMaxCooldown(cardId);
            int currentRound = GetCurrentRound();
            int roundsSinceUse = currentRound - usedRound;

            // 如果经过的回合数 < 冷却回合数，则仍在冷却中
            bool onCooldown = roundsSinceUse < maxCd;

            SteriaLogger.Log($"IsOnCooldown: Card {cardId}, Unit {unit.index}, UsedAt {usedRound}, Current {currentRound}, MaxCd {maxCd}, OnCooldown {onCooldown}");

            return onCooldown;
        }

        /// <summary>
        /// 获取剩余冷却回合数
        /// </summary>
        public static int GetRemainingCooldown(BattleUnitModel unit, int cardId)
        {
            if (unit == null) return 0;

            var key = (unit.index, cardId);
            if (!_usedAtRound.TryGetValue(key, out int usedRound))
            {
                return 0; // 从未使用过
            }

            int maxCd = GetMaxCooldown(cardId);
            int currentRound = GetCurrentRound();
            int roundsSinceUse = currentRound - usedRound;
            int remaining = maxCd - roundsSinceUse;

            return Math.Max(0, remaining);
        }

        /// <summary>
        /// 获取冷却进度 (0.0 - 1.0)，1.0表示冷却完成可用
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
        /// 设置卡牌进入冷却（记录使用时的回合数）
        /// </summary>
        public static void StartCooldown(BattleUnitModel unit, int cardId)
        {
            if (unit == null) return;
            int maxCd = GetMaxCooldown(cardId);
            if (maxCd <= 0) return;

            var key = (unit.index, cardId);
            int currentRound = GetCurrentRound();
            _usedAtRound[key] = currentRound;

            SteriaLogger.Log($"SteriaEgoCooldown: Card {cardId} used at round {currentRound} for unit {unit.index} (cooldown {maxCd} rounds)");
        }

        /// <summary>
        /// 清除所有冷却（战斗结束时调用）
        /// </summary>
        public static void ClearAllCooldowns()
        {
            _usedAtRound.Clear();
            SteriaLogger.Log("SteriaEgoCooldown: Cleared all cooldowns");
        }

        /// <summary>
        /// 清除特定单位的所有冷却
        /// </summary>
        public static void ClearUnitCooldowns(BattleUnitModel unit)
        {
            if (unit == null) return;
            var keysToRemove = _usedAtRound.Keys.Where(k => k.unitIndex == unit.index).ToList();
            foreach (var key in keysToRemove)
            {
                _usedAtRound.Remove(key);
            }
        }

        /// <summary>
        /// 调试：输出当前所有冷却状态
        /// </summary>
        public static void DebugLogAllCooldowns()
        {
            int currentRound = GetCurrentRound();
            SteriaLogger.Log($"=== EGO Cooldown Debug (Round {currentRound}) ===");
            foreach (var kvp in _usedAtRound)
            {
                int maxCd = GetMaxCooldown(kvp.Value);
                int remaining = maxCd - (currentRound - kvp.Value);
                SteriaLogger.Log($"  Unit {kvp.Key.unitIndex}, Card {kvp.Key.cardId}: UsedAt {kvp.Value}, Remaining {remaining}");
            }
            SteriaLogger.Log("=== End Debug ===");
        }
    }
}

// ============================================================================
// 以下是Harmony补丁类 - 必须放在顶层（不能嵌套）才能被PatchAll()发现
// ============================================================================

/// <summary>
/// 补丁：在战斗开始时清除冷却（确保重新开始战斗时冷却被重置）
/// </summary>
[HarmonyPatch(typeof(StageController), "StartBattle")]
public static class SteriaEgoCooldown_StartBattle_Patch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        try
        {
            Steria.SteriaEgoCooldownManager.ClearAllCooldowns();
            Steria.SteriaLogger.Log("SteriaEgoCooldown: Cleared cooldowns on battle start");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] SteriaEgoCooldown_StartBattle_Patch error: {ex}");
        }
    }
}

/// <summary>
/// 补丁：在战斗结束时清除冷却
/// </summary>
[HarmonyPatch(typeof(StageController), "EndBattle")]
public static class SteriaEgoCooldown_EndBattle_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            Steria.SteriaEgoCooldownManager.ClearAllCooldowns();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] SteriaEgoCooldown_EndBattle_Patch error: {ex}");
        }
    }
}

/// <summary>
/// 补丁：拦截EGO卡牌的冷却显示
/// </summary>
[HarmonyPatch(typeof(BattleDiceCardUI), "SetEgoCoolTimeGauge")]
public static class SteriaEgoCooldown_SetEgoCoolTimeGauge_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(BattleDiceCardUI __instance)
    {
        try
        {
            // 获取卡牌模型
            var cardModel = __instance.CardModel;
            if (cardModel == null) return true;

            int cardId = cardModel.GetID().id;

            // 检查是否是我们管理的卡牌
            if (!Steria.SteriaEgoCooldownManager.IsCardRegistered(cardId))
            {
                return true; // 不是我们的卡牌，使用原版逻辑
            }

            // 尝试获取当前单位
            BattleUnitModel selectedUnit = null;
            try
            {
                selectedUnit = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.SelectedModel;

                // 如果方式1失败，尝试从卡牌所属的personalEgoDetail获取
                if (selectedUnit == null)
                {
                    var aliveUnits = BattleObjectManager.instance?.GetAliveList(Faction.Player);
                    if (aliveUnits != null)
                    {
                        foreach (var unit in aliveUnits)
                        {
                            if (unit.personalEgoDetail != null)
                            {
                                var cards = unit.personalEgoDetail.GetHand();
                                if (cards != null && cards.Exists(c => c.GetID().id == cardId))
                                {
                                    selectedUnit = unit;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] SetEgoCoolTimeGauge error getting unit: {ex.Message}");
            }

            if (selectedUnit == null)
            {
                return true; // 找不到单位，使用原版逻辑
            }

            // 使用反射获取私有字段
            var ob_EgoCoolTime = HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "ob_EgoCoolTime")?.GetValue(__instance) as GameObject;
            var rect_Gauge = HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "rect_Gauge")?.GetValue(__instance) as RectTransform;
            var img_Bg = HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "img_Bg")?.GetValue(__instance) as Image;
            var img_BgGlow = HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "img_BgGlow")?.GetValue(__instance) as Image;
            var hsv_bgGlow = HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "hsv_bgGlow")?.GetValue(__instance) as RefineHsv;
            var gaugeLength = (float)(HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "gaugeLength")?.GetValue(__instance) ?? 950f);

            if (ob_EgoCoolTime == null)
            {
                return true;
            }

            // 检查自定义冷却状态
            float progress = Steria.SteriaEgoCooldownManager.GetCooldownProgress(selectedUnit, cardId);

            if (progress >= 1f)
            {
                // 冷却完成，可以使用
                ob_EgoCoolTime.SetActive(false);
                HarmonyLib.AccessTools.Field(typeof(BattleDiceCardUI), "isEgoCoolTimeLock")?.SetValue(__instance, false);

                // 调用SetEgoFrameLockColor(false)
                var setEgoFrameLockColor = HarmonyLib.AccessTools.Method(typeof(BattleDiceCardUI), "SetEgoFrameLockColor");
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
                var setEgoLock = HarmonyLib.AccessTools.Method(typeof(BattleDiceCardUI), "SetEgoLock");
                setEgoLock?.Invoke(__instance, null);
            }

            return false; // 跳过原版逻辑
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] SetEgoCoolTimeGauge patch error: {ex}");
            return true;
        }
    }
}

/// <summary>
/// 补丁：在GetClickableState中检查自定义冷却，确保冷却中的卡牌不可点击
/// </summary>
[HarmonyPatch(typeof(BattleDiceCardUI), "GetClickableState")]
public static class SteriaEgoCooldown_GetClickableState_Patch
{
    [HarmonyPostfix]
    public static void Postfix(BattleDiceCardUI __instance, ref BattleDiceCardUI.ClickableState __result)
    {
        try
        {
            // 如果已经是不可点击状态，不需要处理
            if (__result == BattleDiceCardUI.ClickableState.CannotClick)
            {
                return;
            }

            // 获取卡牌模型
            var cardModel = __instance.CardModel;
            if (cardModel == null) return;

            int cardId = cardModel.GetID().id;

            // 检查是否是我们管理的卡牌
            if (!Steria.SteriaEgoCooldownManager.IsCardRegistered(cardId))
            {
                return;
            }

            // 获取当前选中的单位
            BattleUnitModel selectedUnit = null;
            try
            {
                selectedUnit = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.SelectedModel;
            }
            catch { }

            if (selectedUnit == null) return;

            // 检查自定义冷却状态
            if (Steria.SteriaEgoCooldownManager.IsOnCooldown(selectedUnit, cardId))
            {
                __result = BattleDiceCardUI.ClickableState.CannotClick;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] GetClickableState patch error: {ex}");
        }
    }
}
