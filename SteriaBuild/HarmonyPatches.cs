using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq; // Required for Linq
using LOR_DiceSystem; // Required for DiceStatBonus etc.
using BaseMod; // Required for PassiveAbilityBase and potentially Tools
using UnityEngine; // Required for Debug.Log and Singleton
using System.Reflection;

namespace Steria
{
    // --- Moved Harmony Helper Methods and Storage Here ---
    public static class HarmonyHelpers // Renamed from partial HarmonyPatches to avoid confusion
    {
        // Simple class to hold flow consumption data for a card instance during its action
        internal class FlowCardData
        {
            public bool ShouldConsumeFlow { get; set; } = false; // Does this card type consume flow?
            public int InitialFlowAvailable { get; set; } = 0; // How much flow was available *when the card was used*?
            public int CurrentFlowRemaining { get; set; } = 0; // How much flow is left for subsequent dice *in this action*?
        }

        // Storage for flow consumption *rules* per card instance (determined at card use start)
        internal static Dictionary<BattlePlayingCardDataInUnitModel, FlowCardData> _flowConsumingCards = new Dictionary<BattlePlayingCardDataInUnitModel, FlowCardData>();

        // Storage for flow consumed per card instance during an action
        internal static Dictionary<BattlePlayingCardDataInUnitModel, int> _flowConsumedByCardAction = new Dictionary<BattlePlayingCardDataInUnitModel, int>();
        // Storage for flow consumed per dice behavior instance during an action
        internal static Dictionary<BattleDiceBehavior, int> _flowConsumedByDiceAction = new Dictionary<BattleDiceBehavior, int>();
        // Storage for dice behaviors that triggered a repeat effect this action
        internal static HashSet<BattleDiceBehavior> _repeatTriggeredDice = new HashSet<BattleDiceBehavior>();

        public static int GetFlowConsumedByCard(BattlePlayingCardDataInUnitModel card) {
            // Manual implementation of GetValueOrDefault
            _flowConsumedByCardAction.TryGetValue(card, out int consumed);
            return consumed; // Returns 0 if key not found
        }

        public static int GetFlowConsumedByDice(DiceCardAbilityBase ability) {
            // Assuming 'ability.behavior' IS set by the game when calling ability methods:
            // Ensure ability and behavior are not null before accessing
            BattleDiceBehavior currentBehavior = ability?.behavior;
            if (currentBehavior != null) {
                 // Manual implementation of GetValueOrDefault
                 _flowConsumedByDiceAction.TryGetValue(currentBehavior, out int consumed);
                 return consumed;
            } else {
                 // Reduced log spam: Only log warning if ability itself wasn't null
                 if (ability != null) {
                     Debug.LogWarning($"[MyDLL] HarmonyHelpers.GetFlowConsumedByDice: Could not find behavior for ability {ability.GetType().Name}. This method might not work reliably.");
                 }
                 return 0; // Cannot determine consumption without behavior instance
            }
        }

         // Method to record consumption (called from patches)
         public static void RecordFlowConsumptionForCard(BattlePlayingCardDataInUnitModel card, int amount) {
                if (card != null) {
                     // Manual implementation of GetValueOrDefault + update
                     _flowConsumedByCardAction.TryGetValue(card, out int current);
                     _flowConsumedByCardAction[card] = current + amount;
                     Debug.Log($"[MyDLL] Recorded {amount} flow consumption for card {card.card.GetName()}. Total this action: {_flowConsumedByCardAction[card]}");
                }
         }
         // Method to record consumption (called from patches) - Added index parameter
         public static void RecordFlowConsumptionForDice(BattleDiceBehavior behavior, int index, int amount) { // Added index parameter
             if (behavior != null) {
                  // Manual implementation of GetValueOrDefault + update
                  _flowConsumedByDiceAction.TryGetValue(behavior, out int current);
                  _flowConsumedByDiceAction[behavior] = current + amount;
                  // Use the passed-in index for logging
                  Debug.Log($"[MyDLL] Recorded {amount} flow consumption for dice behavior (Index: {index}). Total this action: {_flowConsumedByDiceAction[behavior]}");
             }
         }
         public static void ClearFlowConsumptionTracking() { // Call this at appropriate times (e.g., end of action/round start)
                _flowConsumedByCardAction.Clear();
                _flowConsumedByDiceAction.Clear();
                _flowConsumingCards.Clear(); // Also clear the rules dictionary
                _repeatTriggeredDice.Clear(); // Clear the repeat set as well
                // 重置"本幕不消耗流"标志
                HarmonyPatches.NoFlowConsumptionActiveThisRound = false;
                Debug.Log("[MyDLL] Cleared Flow Consumption Tracking dictionaries (including repeat set and NoFlowConsumption flag).");
         }

        // 存储每颗骰子的威力加成（由流分配）- 使用卡牌实例+骰子索引作为键
        private static Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>> _flowPowerBonusPerCard =
            new Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>>();

        // 存储已经应用了流加成的骰子实例（用于防止重复应用，同时允许反击骰子获得加成）
        private static HashSet<BattleDiceBehavior> _flowBonusAppliedDice = new HashSet<BattleDiceBehavior>();
        // 存储已应用潮之启示加成的骰子实例
        private static HashSet<BattleDiceBehavior> _tideRevelationAppliedDice = new HashSet<BattleDiceBehavior>();

        // 检查骰子是否已经应用了流加成
        public static bool HasFlowBonusApplied(BattleDiceBehavior dice)
        {
            return dice != null && _flowBonusAppliedDice.Contains(dice);
        }

        // 标记骰子已经应用了流加成
        public static void MarkFlowBonusApplied(BattleDiceBehavior dice)
        {
            if (dice != null)
            {
                _flowBonusAppliedDice.Add(dice);
            }
        }

        // 清除已应用流加成的骰子记录
        public static void ClearFlowBonusAppliedDice()
        {
            _flowBonusAppliedDice.Clear();
        }

        public static bool HasTideRevelationApplied(BattleDiceBehavior dice)
        {
            return dice != null && _tideRevelationAppliedDice.Contains(dice);
        }

        public static void MarkTideRevelationApplied(BattleDiceBehavior dice)
        {
            if (dice != null)
            {
                _tideRevelationAppliedDice.Add(dice);
            }
        }

        public static void RemoveTideRevelationApplied(BattleDiceBehavior dice)
        {
            if (dice != null)
            {
                _tideRevelationAppliedDice.Remove(dice);
            }
        }

        // 不消耗流、只获得加成的卡牌ID集合（流转卡牌）
        // 添加新卡牌时只需在此集合中添加对应ID即可
        private static readonly HashSet<int> _flowBonusOnlyCardIds = new HashSet<int>
        {
            9001001,  // 拙劣控流
            9001007,  // 内调之流
            9001008,  // 清司风流
            // 斯拉泽雅流转卡牌
            9002001,  // 逐梦随流
            9002002,  // 川流不息
            9002003,  // 万物之流
            9002006,  // 洋流，听我的号令
            // 司流者教徒流转卡牌
            9003005,  // 顺流而为
            // 艾莉蕾尔流转卡牌
            9007001,  // 暮雾伏击
            9007002,  // 侧闪
            // 在此添加更多具有相同效果的卡牌ID...
        };

        // 消耗所有流但不提供威力加成的卡牌ID集合（群攻卡牌等）
        private static readonly HashSet<int> _consumeAllFlowNoBonus = new HashSet<int>
        {
            9002009,  // 倾覆万千之流
        };

        // 可受多次流强化的卡牌ID及其最大强化次数
        private static readonly Dictionary<int, int> _multiFlowBonusCards = new Dictionary<int, int>
        {
            { 9002004, 5 },  // 风暴分流 - 至多5次流强化
            { 9002008, 2 },  // 百川逐风 - 至多2次流强化
        };

        // [迅攻] 关键词ID
        private const string _rapidAssaultKeywordId = "SteriaRapidAssault";

        internal static bool HasRapidAssaultKeyword(BattlePlayingCardDataInUnitModel card)
        {
            if (card?.card?.XmlData?.Keywords != null && card.card.XmlData.Keywords.Contains(_rapidAssaultKeywordId))
            {
                return true;
            }

            string[] abilityKeywords = card?.cardAbility?.Keywords;
            if (abilityKeywords != null)
            {
                for (int i = 0; i < abilityKeywords.Length; i++)
                {
                    if (abilityKeywords[i] == _rapidAssaultKeywordId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsRapidAssaultFaster(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.target == null)
            {
                return false;
            }

            if (!HasRapidAssaultKeyword(card))
            {
                return false;
            }

            int mySpeed = GetSpeedValue(card);
            int targetSpeed = GetTargetSpeedValue(card);
            if (mySpeed <= 0 || targetSpeed <= 0)
            {
                return false;
            }

            return mySpeed > targetSpeed;
        }

        internal static bool IsRapidAssaultFaster(BattlePlayingCardDataInUnitModel card, BattlePlayingCardDataInUnitModel opponent)
        {
            if (card == null || opponent == null)
            {
                return false;
            }

            if (!HasRapidAssaultKeyword(card))
            {
                return false;
            }

            int mySpeed = GetSpeedValue(card);
            int opponentSpeed = GetSpeedValue(opponent);
            if (mySpeed <= 0 || opponentSpeed <= 0)
            {
                return false;
            }

            return mySpeed > opponentSpeed;
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

        // 存储每颗骰子的原始流强化次数（未乘以倍率，用于计算debuff层数）
        private static Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>> _flowEnhancementCountPerCard =
            new Dictionary<BattlePlayingCardDataInUnitModel, Dictionary<int, int>>();

        /// <summary>
        /// 获取骰子的原始流强化次数（未乘以倍率）
        /// </summary>
        public static int GetFlowEnhancementCountForDice(BattlePlayingCardDataInUnitModel card, int diceIndex)
        {
            if (card != null && _flowEnhancementCountPerCard.TryGetValue(card, out var countMap))
            {
                if (countMap.TryGetValue(diceIndex, out int count))
                {
                    return count;
                }
            }
            return 0;
        }

        // 存储群攻卡牌消耗的流数量（用于额外伤害计算）
        private static Dictionary<BattlePlayingCardDataInUnitModel, int> _massAttackFlowConsumed =
            new Dictionary<BattlePlayingCardDataInUnitModel, int>();

        /// <summary>
        /// 获取群攻卡牌消耗的流数量
        /// </summary>
        public static int GetMassAttackFlowConsumed(BattlePlayingCardDataInUnitModel card)
        {
            if (card != null && _massAttackFlowConsumed.TryGetValue(card, out int consumed))
            {
                return consumed;
            }
            return 0;
        }

        /// <summary>
        /// 检查并消耗潮来增强荆棘效果
        /// 荆棘是被潮影响的负面效果，施加时消耗1层潮并增加1层荆棘
        /// </summary>
        /// <returns>潮加成的层数（0或1）</returns>
        public static int CheckAndConsumeTideForThorn(BattleUnitModel giver)
        {
            if (giver == null) return 0;

            BattleUnitBuf_Tide tideBuf = giver.bufListDetail?.GetActivatedBufList()
                ?.FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;

            if (tideBuf == null || tideBuf.stack <= 0) return 0;

            // 消耗1层潮，增加1层荆棘
            tideBuf.stack--;
            SteriaLogger.Log($"Tide: Giver {giver.UnitData?.unitData?.name} consumed 1 Tide for Thorn, remaining = {tideBuf.stack}");
            NotifyPassivesOnTideConsumed(giver, 1);

            if (tideBuf.stack <= 0)
            {
                tideBuf.Destroy();
            }

            return 1;
        }

        /// <summary>
        /// 清除群攻卡牌消耗记录
        /// </summary>
        public static void ClearMassAttackFlowConsumed(BattlePlayingCardDataInUnitModel card)
        {
            if (card != null)
            {
                _massAttackFlowConsumed.Remove(card);
            }
        }

        /// <summary>
        /// 检查卡牌是否具有"只获得流加成而不消耗流"的效果
        /// </summary>
        public static bool IsFlowBonusOnlyCard(int cardId)
        {
            return _flowBonusOnlyCardIds.Contains(cardId);
        }

        /// <summary>
        /// 通知所有相关被动流被消耗
        /// </summary>
        public static void NotifyPassivesOnFlowConsumed(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0) return;

            // 通知 PassiveAbility_9000005 (不会忘记的那个梦想)
            var passive9000005 = owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9000005) as PassiveAbility_9000005;
            passive9000005?.OnFlowConsumed(amount);

            // 通知 PassiveAbility_9002003 (御风司流)
            var passive9002003 = owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9002003) as PassiveAbility_9002003;
            passive9002003?.OnFlowConsumed(amount);

            // 通知 PassiveAbility_9002004 (司流者)
            var passive9002004 = owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9002004) as PassiveAbility_9002004;
            passive9002004?.OnFlowConsumed(amount);

            // 通知 PassiveAbility_9007001 (汐音共振)
            var passive9007001 = owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9007001) as PassiveAbility_9007001;
            passive9007001?.OnFlowConsumed(amount);

            SteriaLogger.Log($"NotifyPassivesOnFlowConsumed: Notified passives of {amount} flow consumed");
        }

        /// <summary>
        /// 通知所有相关被动潮被消耗
        /// </summary>
        public static void NotifyPassivesOnTideConsumed(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0) return;

            var passive9006001 = owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9006001) as PassiveAbility_9006001;
            passive9006001?.OnTideConsumed(amount);

            SteriaLogger.Log($"NotifyPassivesOnTideConsumed: Notified passives of {amount} tide consumed");
        }

        /// <summary>
        /// 注册新的"只获得流加成而不消耗流"的卡牌ID（运行时动态添加）
        /// </summary>
        public static void RegisterFlowBonusOnlyCard(int cardId)
        {
            _flowBonusOnlyCardIds.Add(cardId);
            SteriaLogger.Log($"Registered card ID {cardId} as FlowBonusOnly card");
        }

        // Method called when a card is about to be used
        // 新逻辑：所有书页都会消耗流，在使用时一次性分配威力加成
        // 特殊：_flowBonusOnlyCardIds 中的卡牌只获得加成而不消耗流
        // 注意：Standby 骰子被忽略，不参与流加成分配
        public static void RegisterCardUsage(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.owner == null || card.card == null) return;

            // 获取当前流的层数
            BattleUnitBuf_Flow flowBuf = card.owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
            int flowStacks = flowBuf?.stack ?? 0;

            SteriaLogger.Log($"RegisterCardUsage: Card={card.card.GetID()}, Owner={card.owner.UnitData.unitData.name}, Flow={flowStacks}, CardHash={card.GetHashCode()}");

            if (flowStacks <= 0)
            {
                SteriaLogger.Log("RegisterCardUsage: No flow to consume");
                return;
            }

            // 获取书页的骰子列表，排除 Standby 骰子
            var allDice = card.card.XmlData.DiceBehaviourList;
            if (allDice == null || allDice.Count == 0)
            {
                SteriaLogger.Log("RegisterCardUsage: No dice in card XML data");
                return;
            }

            // 筛选出非 Standby 骰子的索引
            List<int> nonStandbyIndices = new List<int>();
            for (int i = 0; i < allDice.Count; i++)
            {
                if (allDice[i].Type != BehaviourType.Standby)
                {
                    nonStandbyIndices.Add(i);
                }
            }

            int diceCount = nonStandbyIndices.Count;
            if (diceCount == 0)
            {
                SteriaLogger.Log("RegisterCardUsage: No non-Standby dice in card");
                return;
            }

            int cardId = card.card.GetID().id;

            // 检查是否是"流转"卡牌（不受流影响：不获得加成也不消耗流）
            bool isFlowTransfer = IsFlowBonusOnlyCard(cardId);
            if (isFlowTransfer)
            {
                SteriaLogger.Log($"RegisterCardUsage: [流转] card detected (ID: {cardId}) - not affected by flow");
                return; // 流转卡牌完全不受流影响，直接返回
            }

            // 检查是否是"消耗所有流但不提供威力加成"的卡牌（如群攻）
            bool isConsumeAllNoBonus = _consumeAllFlowNoBonus.Contains(cardId);
            if (isConsumeAllNoBonus)
            {
                SteriaLogger.Log($"RegisterCardUsage: [消耗所有流] card detected (ID: {cardId}) - consuming all {flowStacks} flow without bonus");
                // 记录消耗的流数量（用于额外伤害计算）
                _massAttackFlowConsumed[card] = flowStacks;
                // 消耗所有流
                flowBuf.stack = 0;
                flowBuf.Destroy();
                // 通知被动
                NotifyPassivesOnFlowConsumed(card.owner, flowStacks);
                return;
            }

            // 检查是否是"可受多次流强化"的卡牌
            int maxFlowPerDice = 1; // 默认每颗骰子最多+1
            if (_multiFlowBonusCards.TryGetValue(cardId, out int maxBonus))
            {
                maxFlowPerDice = maxBonus;
                SteriaLogger.Log($"RegisterCardUsage: [多次流强化] card detected (ID: {cardId}) - max {maxFlowPerDice} per dice");
            }

            // 检查角色是否有斯拉泽雅被动（流威力加成x2）
            int flowPowerMultiplier = 1;
            if (PassiveAbility_9002001.HasFlowPowerBonus(card.owner))
            {
                flowPowerMultiplier = 2;
                SteriaLogger.Log($"RegisterCardUsage: Owner has 斯拉泽雅被动 - flow power bonus x2");
            }

            // 计算流分配
            int flowToUse;
            Dictionary<int, int> powerBonusMap = new Dictionary<int, int>();
            Dictionary<int, int> enhancementCountMap = new Dictionary<int, int>(); // 原始流强化次数（未乘以倍率）

            if (maxFlowPerDice > 1)
            {
                // 多次流强化卡牌：每颗骰子可获得多次加成
                flowToUse = Math.Min(flowStacks, diceCount * maxFlowPerDice);
                int flowRemaining = flowToUse;
                for (int i = 0; i < nonStandbyIndices.Count && flowRemaining > 0; i++)
                {
                    int targetIndex = nonStandbyIndices[i];
                    int bonusForThisDice = Math.Min(flowRemaining, maxFlowPerDice);
                    powerBonusMap[targetIndex] = bonusForThisDice * flowPowerMultiplier;
                    enhancementCountMap[targetIndex] = bonusForThisDice; // 存储原始次数
                    flowRemaining -= bonusForThisDice;
                }
            }
            else
            {
                // 普通卡牌：每颗骰子最多+1威力（乘以倍率）
                flowToUse = Math.Min(flowStacks, diceCount);
                for (int i = 0; i < flowToUse; i++)
                {
                    int targetIndex = nonStandbyIndices[i];
                    powerBonusMap[targetIndex] = 1 * flowPowerMultiplier;
                    enhancementCountMap[targetIndex] = 1; // 存储原始次数
                }
            }

            SteriaLogger.Log($"RegisterCardUsage: Distributing {flowToUse} flow to {diceCount} non-Standby dice (max {maxFlowPerDice} per dice, multiplier {flowPowerMultiplier})");

            // 存储威力加成映射
            _flowPowerBonusPerCard[card] = powerBonusMap;
            // 存储原始流强化次数映射
            _flowEnhancementCountPerCard[card] = enhancementCountMap;
            foreach (var kvp in powerBonusMap)
            {
                int rawCount = enhancementCountMap.ContainsKey(kvp.Key) ? enhancementCountMap[kvp.Key] : 0;
                SteriaLogger.Log($"RegisterCardUsage: Dice index {kvp.Key} will get +{kvp.Value} power from flow (raw enhancement count: {rawCount})");
            }

            // 检查是否有"本幕不消耗流"效果
            bool noConsumption = HarmonyPatches.NoFlowConsumptionActiveThisRound ||
                card.owner.bufListDetail.GetActivatedBufList().Any(b => b is BattleUnitBuf_NoFlowConsumption);

            // 计算"视为消耗"的流数量（用于触发被动和卡牌效果）
            int totalConsumed = flowToUse;

            if (noConsumption)
            {
                SteriaLogger.Log($"RegisterCardUsage: NoFlowConsumption active, not actually consuming flow but treating as {totalConsumed} consumed");
                // 不实际消耗流，但仍然视为消耗（用于触发效果）
            }
            else
            {
                // 实际消耗流
                flowBuf.stack -= totalConsumed;
                if (flowBuf.stack <= 0)
                {
                    flowBuf.Destroy();
                }
                SteriaLogger.Log($"RegisterCardUsage: Consumed {totalConsumed} flow, remaining: {flowBuf?.stack ?? 0}");
            }

            // 记录流消耗（供卡牌能力查询）
            RecordFlowConsumptionForCard(card, totalConsumed);

            // 通知 PassiveAbility_9000005 (不会忘记的那个梦想)
            var passive9000005 = card.owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9000005) as PassiveAbility_9000005;
            if (passive9000005 != null)
            {
                SteriaLogger.Log($"RegisterCardUsage: Notifying PassiveAbility_9000005 of {totalConsumed} flow consumed");
                passive9000005.OnFlowConsumed(totalConsumed);
            }

            // 通知 PassiveAbility_9002003 (御风司流)
            var passive9002003 = card.owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9002003) as PassiveAbility_9002003;
            if (passive9002003 != null)
            {
                SteriaLogger.Log($"RegisterCardUsage: Notifying PassiveAbility_9002003 of {totalConsumed} flow consumed");
                passive9002003.OnFlowConsumed(totalConsumed);
            }

            // 通知 PassiveAbility_9002004 (司流者)
            var passive9002004 = card.owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9002004) as PassiveAbility_9002004;
            if (passive9002004 != null)
            {
                SteriaLogger.Log($"RegisterCardUsage: Notifying PassiveAbility_9002004 of {totalConsumed} flow consumed");
                passive9002004.OnFlowConsumed(totalConsumed);
            }

            // 通知 PassiveAbility_9007001 (汐音共振)
            var passive9007001 = card.owner.passiveDetail.PassiveList?.FirstOrDefault(p => p is PassiveAbility_9007001) as PassiveAbility_9007001;
            if (passive9007001 != null)
            {
                SteriaLogger.Log($"RegisterCardUsage: Notifying PassiveAbility_9007001 of {totalConsumed} flow consumed");
                passive9007001.OnFlowConsumed(totalConsumed);
            }
        }

        // 获取骰子的流威力加成（通过卡牌和骰子索引）
        public static int GetFlowPowerBonusForDice(BattlePlayingCardDataInUnitModel card, int diceIndex)
        {
            if (card != null && _flowPowerBonusPerCard.TryGetValue(card, out var bonusMap))
            {
                if (bonusMap.TryGetValue(diceIndex, out int bonus))
                {
                    return bonus;
                }
            }
            return 0;
        }

        // 清除骰子的流威力加成记录
        public static void ClearFlowPowerBonusForDice(BattlePlayingCardDataInUnitModel card, int diceIndex)
        {
            if (card != null && _flowPowerBonusPerCard.TryGetValue(card, out var bonusMap))
            {
                bonusMap.Remove(diceIndex);
                if (bonusMap.Count == 0)
                {
                    _flowPowerBonusPerCard.Remove(card);
                }
            }
        }

        // 清除卡牌的所有流威力加成记录
        public static void ClearAllFlowPowerBonusForCard(BattlePlayingCardDataInUnitModel card)
        {
            if (card != null)
            {
                _flowPowerBonusPerCard.Remove(card);
            }
        }

        // 检查卡牌是否有流威力加成记录
        public static bool HasFlowBonusForCard(BattlePlayingCardDataInUnitModel card)
        {
            return card != null && _flowPowerBonusPerCard.ContainsKey(card);
        }

        // 调试用：输出当前所有流威力加成记录
        public static void DebugLogAllFlowBonuses()
        {
            SteriaLogger.Log($"=== Flow Bonus Dictionary Debug ===");
            SteriaLogger.Log($"Total cards in dictionary: {_flowPowerBonusPerCard.Count}");
            foreach (var kvp in _flowPowerBonusPerCard)
            {
                SteriaLogger.Log($"  Card Hash={kvp.Key.GetHashCode()}, CardID={kvp.Key.card?.GetID()}");
                foreach (var diceKvp in kvp.Value)
                {
                    SteriaLogger.Log($"    Dice {diceKvp.Key}: +{diceKvp.Value} power");
                }
            }
            SteriaLogger.Log($"=== End Debug ===");
        }

        // Method called after a card has finished its action
        public static void CleanupCardUsage(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null) return;
            bool removedRule = _flowConsumingCards.Remove(card);
            bool removedConsumption = _flowConsumedByCardAction.Remove(card); // Also clear the total consumed for this card
            Debug.Log($"[MyDLL] Cleaned up Card Usage: ID={card.card?.GetID()}, Hash={card.GetHashCode()}. Removed Rule: {removedRule}, Removed Consumption: {removedConsumption}");

            // 清除流加成记录（包括反击骰子的加成）
            ClearAllFlowPowerBonusForCard(card);
            // 清除流强化次数记录
            _flowEnhancementCountPerCard.Remove(card);
            // 清除已应用流加成的骰子记录
            ClearFlowBonusAppliedDice();
            SteriaLogger.Log($"CleanupCardUsage: Cleared flow bonus records for card {card.card?.GetID()}");

            // Clear dice consumption for behaviors associated with this card - IMPORTANT
             List<BattleDiceBehavior> behaviors = card.GetDiceBehaviorList();
             if (behaviors != null) {
                foreach(var behavior in behaviors) {
                 if (behavior != null) {
                     _flowConsumedByDiceAction.Remove(behavior);
                     // Also remove from repeat set if the action ends prematurely
                     _repeatTriggeredDice.Remove(behavior);
                     RemoveTideRevelationApplied(behavior);
                 }
             }
             }
        }
    }


    // Contains all Harmony Patches for the mod
    // BaseMod will automatically discover and apply patches from classes
    // marked with [HarmonyPatch] if the assembly is loaded correctly.
    // However, base PatchAll only applies patches defined in this file or similar structure in the DLL.
    // Individual mods need their own PatchAll call in their Initializer.
    [HarmonyPatch] 
    public static class HarmonyPatches 
    {
        // Removed _harmonyInstance and _patched fields
        // Removed Initialize() and Unpatch() methods - ModInitializer handles PatchAll now

        // --- REMOVED TEST PATCH --- 
        // [HarmonyPatch(typeof(PassiveAbilityBase), nameof(PassiveAbilityBase.OnRoundStart))]
        // public static class PassiveAbilityBase_OnRoundStart_TestPatch { ... } // Removed
        // --- END OF REMOVED TEST PATCH ---

        // --- Helper to notify Passive 9000003 about discard events ---
        private static void NotifyPassive9000003(BattleUnitModel owner)
        {
            if (owner?.passiveDetail?.PassiveList == null) return;
            var passive = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000003) as PassiveAbility_9000003;
            passive?.Notify_CardDiscarded();
        }

        // --- Helper to increment discard count for 以执为攻 (按角色计数) ---
        private static void IncrementDiscardCountForUnit(BattleUnitModel unit)
        {
            DiceCardSelfAbility_AnhierDiscardPowerUp.IncrementDiscardCount(unit);
        }

        // --- Helper to handle 珍贵的回忆 被弃置时的效果 ---
        private const int PRECIOUS_MEMORY_NUMERIC_ID = 9001006;
        private const string MOD_PACKAGE_ID = "SteriaBuilding";

        private static void HandlePreciousMemoryDiscard(BattleUnitModel owner, BattleDiceCardModel card)
        {
            if (owner == null || card == null || card.GetID() == null)
            {
                SteriaLogger.LogWarning("HandlePreciousMemoryDiscard: null owner/card/ID");
                return;
            }

            LorId cardId = card.GetID();
            SteriaLogger.Log($"HandlePreciousMemoryDiscard: Card='{card.GetName()}', ID={cardId.id}, PkgId='{cardId.packageId}'");

            // 检查是否是珍贵的回忆 (同时检查 packageId 和 id)
            bool isPreciousMemory =
                cardId.id == PRECIOUS_MEMORY_NUMERIC_ID &&
                (string.IsNullOrEmpty(cardId.packageId) || cardId.packageId == MOD_PACKAGE_ID);

            if (!isPreciousMemory)
                return;

            SteriaLogger.Log("珍贵的回忆 被弃置! 触发效果: 抽2张牌 + 下一幕获得1层[强壮]");

            // 抽2张牌
            owner.allyCardDetail.DrawCards(2);
            SteriaLogger.Log("抽取了2张牌");

            // AddKeywordBufByEtc 默认下一幕生效，直接赋予即可
            try
            {
                owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, owner);
                SteriaLogger.Log("赋予了1层强壮（下一幕生效）");
            }
            catch (Exception ex)
            {
                SteriaLogger.LogError($"赋予强壮失败: {ex.Message}");
            }

            // 消耗这张牌（只消耗当前这一张，不影响其他同ID卡牌）
            try
            {
                owner.allyCardDetail.ExhaustACardAnywhere(card);
                SteriaLogger.Log("消耗了珍贵的回忆");
            }
            catch (Exception ex)
            {
                SteriaLogger.LogError($"消耗珍贵的回忆失败: {ex.Message}");
            }

            // 通知 PassiveAbility_9000005 (不会忘记的那个梦想) 珍贵的回忆被弃置
            var passive9000005 = owner.passiveDetail.PassiveList?
                .FirstOrDefault(p => p is PassiveAbility_9000005) as PassiveAbility_9000005;
            if (passive9000005 != null)
            {
                passive9000005.OnPreciousMemoryDiscarded();
                SteriaLogger.Log("通知了 PassiveAbility_9000005 珍贵的回忆被弃置");
            }
        }


        // --- Patches for Passive 9000001 (神脉：职业等级) --- 
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeDamage))]
        public static class PassiveAbility_9000001_Patch_TakeDamage
        {
            public static void Prefix(BattleUnitModel __instance, ref int v, DamageType type, BattleUnitModel attacker)
            {
                if (attacker?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && v > 0 && type == DamageType.Attack)
                {
                    int originalDmg = v;
                    v = (int)Math.Round(v * 1.15f);
                    Debug.Log($"[MyDLL] Passive 9000001 (Attacker: {attacker.UnitData.unitData.name}): Increasing damage to {__instance.UnitData.unitData.name} from {originalDmg} to {v} (+15%)");
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeBreakDamage))]
        public static class PassiveAbility_9000001_Patch_TakeBreakDamage
        {
            public static void Prefix(BattleUnitModel __instance, ref int damage, DamageType type, BattleUnitModel attacker, AtkResist atkResist)
            {
                if (attacker?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && damage > 0)
                {
                    int originalBreakDmg = damage;
                    damage = (int)Math.Round(damage * 1.15f);
                    Debug.Log($"[MyDLL] Passive 9000001 (Attacker: {attacker.UnitData.unitData.name}): Increasing break damage to {__instance.UnitData.unitData.name} from {originalBreakDmg} to {damage} (+15%)");
                }
            }
        }

        [HarmonyPatch(typeof(SpeedDiceRule), nameof(SpeedDiceRule.Roll))]
        public static class PassiveAbility_9000001_Patch_SpeedDiceRoll
        {
            public static void Postfix(SpeedDiceRule __instance, ref List<SpeedDice> __result, BattleUnitModel unitModel)
            {
                if (unitModel?.passiveDetail?.PassiveList.Any(p => p is PassiveAbility_9000001) == true && __result != null)
                {
                    foreach (SpeedDice dice in __result)
                    {
                        dice.faces += 1;
                    }
                }
            }
        }


        // --- Patches for Passive 9000002 (神脉：汐与梦) + 倾诉梦想：远望 + 薇莉亚被动 ---
        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), nameof(BattlePlayingCardSlotDetail.SpendCost))]
        public static class BattlePlayingCardSlotDetail_SpendCost_Patch
        {
            public static void Postfix(BattlePlayingCardSlotDetail __instance, int value)
            {
                // 字段名是 _self 而不是 _owner
                var selfField = AccessTools.Field(typeof(BattlePlayingCardSlotDetail), "_self");
                BattleUnitModel owner = selfField?.GetValue(__instance) as BattleUnitModel;
                if (owner != null && value > 0)
                {
                    // 触发 PassiveAbility_9000002 (神脉：汐与梦)
                    var passive = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9000002) as PassiveAbility_9000002;
                    passive?.OnActualLightSpend(value);

                    // 触发 PassiveAbility_9004001 (神脉：梦之汐-司潮-潜力观测) - 薇莉亚耗光给潮
                    var passive9004001 = owner.passiveDetail.PassiveList.FirstOrDefault(p => p is PassiveAbility_9004001) as PassiveAbility_9004001;
                    passive9004001?.OnLightSpent(value);

                    // 触发 倾诉梦想：远望 Buff
                    var dreamVisionBuf = owner.bufListDetail.GetActivatedBufList()
                        .FirstOrDefault(b => b is BattleUnitBuf_DreamVision) as BattleUnitBuf_DreamVision;
                    dreamVisionBuf?.OnLightSpent(value);
                }
            }
        }

        // --- Patches for Passive 9000003 (回忆燃烧) 以及 珍贵的回忆 弃置触发 ---

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnDiscardByAbility))]
        public static class BattleUnitModel_OnDiscardByAbility_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleUnitModel __instance, List<BattleDiceCardModel> cards)
            {
                try
                {
                    if (__instance == null || cards == null || cards.Count == 0)
                        return;

                    foreach (BattleDiceCardModel card in cards)
                    {
                        HandlePreciousMemoryDiscard(__instance, card);
                    }

                    // 通知 回忆燃烧 被动本幕有弃牌发生
                    NotifyPassive9000003(__instance);
                    // 增加弃牌计数（以执为攻，按角色计数）
                    IncrementDiscardCountForUnit(__instance);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MyDLL] Error in BattleUnitModel_OnDiscardByAbility_Patch Postfix: {ex}");
                }
            }
        }

        // --- Patch for BattleAllyCardDetail.DiscardACardByAbility (单张弃牌) ---
        // 指定参数类型以避免重载歧义
        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DiscardACardByAbility), new Type[] { typeof(BattleDiceCardModel) })]
        public static class BattleAllyCardDetail_DiscardACardByAbility_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleAllyCardDetail __instance, BattleDiceCardModel card)
            {
                try
                {
                    if (__instance == null || card == null)
                        return;

                    // 获取 owner
                    var ownerField = AccessTools.Field(typeof(BattleAllyCardDetail), "_owner");
                    BattleUnitModel owner = ownerField?.GetValue(__instance) as BattleUnitModel;

                    if (owner == null)
                    {
                        SteriaLogger.LogWarning("DiscardACardByAbility_Patch: Could not get owner");
                        return;
                    }

                    SteriaLogger.Log($"DiscardACardByAbility_Patch: Card='{card.GetName()}', Owner={owner.UnitData?.unitData?.name}");
                    HandlePreciousMemoryDiscard(owner, card);
                    NotifyPassive9000003(owner);
                    IncrementDiscardCountForUnit(owner);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in BattleAllyCardDetail_DiscardACardByAbility_Patch Postfix: {ex}");
                }
            }
        }

        // --- Patch for BattleAllyCardDetail.DisCardACardRandom (随机弃牌) ---
        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.DisCardACardRandom))]
        public static class BattleAllyCardDetail_DisCardACardRandom_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleAllyCardDetail __instance, BattleDiceCardModel __result)
            {
                try
                {
                    if (__instance == null || __result == null)
                        return;

                    // 获取 owner
                    var ownerField = AccessTools.Field(typeof(BattleAllyCardDetail), "_owner");
                    BattleUnitModel owner = ownerField?.GetValue(__instance) as BattleUnitModel;

                    if (owner == null)
                    {
                        SteriaLogger.LogWarning("DisCardACardRandom_Patch: Could not get owner");
                        return;
                    }

                    SteriaLogger.Log($"DisCardACardRandom_Patch: Card='{__result.GetName()}', Owner={owner.UnitData?.unitData?.name}");
                    HandlePreciousMemoryDiscard(owner, __result);
                    NotifyPassive9000003(owner);
                    IncrementDiscardCountForUnit(owner);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in BattleAllyCardDetail_DisCardACardRandom_Patch Postfix: {ex}");
                }
            }
        }

        // --- REMOVED PATCH for Passive 9000004 (回忆结晶) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.SetCurrentWave))]
        // public static class StageController_SetCurrentWave_Patch { ... } // Removed
        // --- END OF REMOVED PATCH for Passive 9000004 ---

        // --- REMOVED BattleUnitModel_OnWaveStart_Patch Class ---

        // --- REMOVED Patch for Battle Start (No longer needed as logic is in passive Init/OnWaveStart) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        // public static class StageController_StartBattle_Patch { ... }

        // --- REMOVED Patch for Scene Start (Logic moved to Passive OnWaveStart) ---
        // [HarmonyPatch(typeof(StageController), nameof(StageController.NextWave))]
        // public static class StageController_NextWave_Patch { ... } 
        
        // --- Patch for Battle Start to Reset Discard Counts ---
        [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        public static class StageController_StartBattle_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // PassiveAbility_9000004 现在使用实例变量，Init时自动重置，不需要手动重置
                DiceCardSelfAbility_AnhierDiscardPowerUp.ResetAllDiscardCounts(); // Reset all discard counts for 以执为攻
                Debug.Log("[Steria] StartBattle: Reset discard counts");
            }
        }

        // --- Patch for Battle End to Reset Counters ---
        [HarmonyPatch(typeof(StageController), nameof(StageController.EndBattle))]
        public static class StageController_EndBattle_ResetCounters_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // PassiveAbility_9000004 现在使用实例变量，不需要手动重置
                DiceCardSelfAbility_AnhierDiscardPowerUp.ResetAllDiscardCounts(); // Reset discard counts
                Debug.Log("[Steria] EndBattle: Reset discard counts");
            }
        }

        // --- Patch for CloseBattleScene to Reset Counters (backup) ---
        [HarmonyPatch(typeof(StageController), nameof(StageController.CloseBattleScene))]
        public static class StageController_CloseBattleScene_ResetCounters_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // PassiveAbility_9000004 现在使用实例变量，不需要手动重置
                DiceCardSelfAbility_AnhierDiscardPowerUp.ResetAllDiscardCounts(); // Reset discard counts
                Debug.Log("[Steria] CloseBattleScene: Reset discard counts");
            }
        }

        // --- Patch for SetCurrentWave to Reset Discard Counter for 以执为攻 ---
        [HarmonyPatch(typeof(StageController), nameof(StageController.SetCurrentWave))]
        public static class StageController_SetCurrentWave_ResetDiscardCount_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                DiceCardSelfAbility_AnhierDiscardPowerUp.ResetAllDiscardCounts(); // Reset all discard counts at wave start
                Debug.Log("[Steria] SetCurrentWave: Reset discard counts for 以执为攻");
            }
        }

        // --- RoundStartPhase_System Patch 已移除 ---
        // 回忆结晶 (PassiveAbility_9000004) 的逻辑现在在 OnRoundStart 中处理

        // --- Patch to Clear Flow Consumption Tracking at Round Start ---
        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
        [HarmonyPrefix] // Run before the original method
        public static void StageController_RoundStartPhase_System_ClearTracking_Prefix()
        {
            try
            {
                // Call helper from the dedicated helper class
                HarmonyHelpers.ClearFlowConsumptionTracking(); 
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MyDLL] Error in StageController_RoundStartPhase_System_ClearTracking_Prefix: {ex}");
            }
        }

        // --- NEW PATCHES FOR FLOW MECHANICS ---

        // Patch for Flow consumption and dice power bonus application
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnUseCard")]
        [HarmonyPrefix]
        public static void BattlePlayingCardDataInUnitModel_OnUseCard_Prefix(BattlePlayingCardDataInUnitModel __instance)
        {
            Debug.Log($"[Steria] Card Usage Start: ID={__instance.card?.GetID()}, Hash={__instance.GetHashCode()}, Owner={__instance.owner?.UnitData.unitData.name}");
            HarmonyHelpers.RegisterCardUsage(__instance);
        }

        // 注意：不要在 OnUseCard Postfix 中清除流加成记录
        // 因为 Standby 骰子是在 OnUseCard 之后才被创建和使用的
        // 流加成记录应该在 OnEndBattle 中清除

        // Patch for OnEndBattle to cleanup flow bonus records
        // 在卡牌行动完全结束后清除流加成记录（包括 Standby 骰子使用后）
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnEndBattle")]
        [HarmonyPostfix]
        public static void BattlePlayingCardDataInUnitModel_OnEndBattle_Postfix(BattlePlayingCardDataInUnitModel __instance)
        {
            Debug.Log($"[Steria] Card OnEndBattle Postfix: ID={__instance.card?.GetID()}, Hash={__instance.GetHashCode()}");
            HarmonyHelpers.CleanupCardUsage(__instance);

            if (__instance?.card != null)
            {
                TibuAbilityHelper.ClearTideRevelation(__instance.card);
            }

            if (__instance?.owner != null)
            {
                var borrowedBuf = __instance.owner.bufListDetail.GetActivatedBufList()
                    .FirstOrDefault(b => b is global::BattleUnitBuf_TideBorrowedCard) as global::BattleUnitBuf_TideBorrowedCard;
                borrowedBuf?.ReturnBorrowedCard(__instance.card);
            }
        }

        // 新的简化流消耗逻辑：在 RollDice 时应用预先计算好的威力加成
        // 修改：使用 HashSet 跟踪已应用加成的骰子，而不是立即清除记录
        // 这样反击骰子（复制的骰子）也能获得相同的加成
        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.RollDice))]
        [HarmonyPrefix]
        public static void BattleDiceBehavior_RollDice_FlowPatch(BattleDiceBehavior __instance)
        {
            // 添加调试日志 - 每次 RollDice 都输出
            SteriaLogger.Log($"RollDice Patch TRIGGERED: Card={__instance.card?.card?.GetID()}, DiceIndex={__instance.Index}, CardHash={__instance.card?.GetHashCode()}, DiceHash={__instance.GetHashCode()}");

            if (__instance.card?.card == null || __instance.owner == null)
            {
                SteriaLogger.Log("RollDice Patch: card or owner is null, skipping");
                return;
            }

            // 检查这个骰子实例是否已经应用了流加成（防止重复应用）
            if (HarmonyHelpers.HasFlowBonusApplied(__instance))
            {
                SteriaLogger.Log($"RollDice Patch: Dice already has flow bonus applied, skipping");
                return;
            }

            // 检查字典中是否有这个卡牌的记录
            bool hasCardInDict = HarmonyHelpers.HasFlowBonusForCard(__instance.card);
            SteriaLogger.Log($"RollDice Patch: HasFlowBonusForCard={hasCardInDict}");

            // 使用卡牌实例和骰子索引来查找威力加成
            int diceIndex = __instance.Index;
            int flowBonus = HarmonyHelpers.GetFlowPowerBonusForDice(__instance.card, diceIndex);
            SteriaLogger.Log($"RollDice Patch: DiceIndex={diceIndex}, FlowBonus={flowBonus}");

            if (flowBonus > 0)
            {
                SteriaLogger.Log($"RollDice FlowPatch: Applying +{flowBonus} power from flow to dice index {diceIndex}");
                __instance.ApplyDiceStatBonus(new DiceStatBonus { power = flowBonus });
                // 标记这个骰子实例已经应用了加成（防止重复应用）
                // 不清除记录，让反击骰子（复制的骰子）也能获得相同的加成
                HarmonyHelpers.MarkFlowBonusApplied(__instance);
            }
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.RollDice))]
        public static class BattleDiceBehavior_RollDice_TideRevelationPatch
        {
            [HarmonyPrefix]
            public static void Prefix(BattleDiceBehavior __instance)
            {
                try
                {
                    if (__instance == null || __instance.card?.card == null)
                    {
                        return;
                    }

                    if (HarmonyHelpers.HasTideRevelationApplied(__instance))
                    {
                        return;
                    }

                    if (TibuAbilityHelper.HasTideRevelation(__instance.card.card))
                    {
                        __instance.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
                        HarmonyHelpers.MarkTideRevelationApplied(__instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] TideRevelation RollDice patch error: {ex}");
                }
            }
        }


        // Patch for SlazeyaClashLosePowerUpNextDice
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnLoseParrying))]
        public static class BattleUnitModel_OnLoseParrying_Patch
        {
             [HarmonyPostfix]
             public static void Postfix(BattleUnitModel __instance, BattleDiceBehavior behavior)
             {
                 try
                 {
                     if (behavior == null) return;

                     // 检查是否有 SlazeyaClashLosePowerUpNextDice 能力
                     bool hasAbility = behavior.abilityList != null &&
                                       behavior.abilityList.Exists(a => a is DiceCardAbility_SlazeyaClashLosePowerUpNextDice);

                     if (!hasAbility) return;

                     Debug.Log($"[Steria] SlazeyaClashLosePowerUpNextDice: Triggered on dice {behavior.Index}");

                     BattlePlayingCardDataInUnitModel currentCard = behavior.card;
                     if (currentCard == null) return;

                     List<BattleDiceBehavior> behaviorList = currentCard.GetDiceBehaviorList();
                     if (behaviorList == null || behaviorList.Count == 0) return;

                     int currentDiceIdx = -1;
                     for (int i = 0; i < behaviorList.Count; i++)
                     {
                         if (behaviorList[i] == behavior)
                         {
                             currentDiceIdx = i;
                             break;
                         }
                     }

                     if (currentDiceIdx != -1 && currentDiceIdx + 1 < behaviorList.Count)
                     {
                         BattleDiceBehavior nextBehaviour = behaviorList[currentDiceIdx + 1];
                         // 使用骰子的最大值作为基础威力
                         int basePower = behavior.GetDiceMax();
                         if (basePower > 0)
                         {
                             DiceStatBonus bonus = new DiceStatBonus { power = basePower };
                             nextBehaviour.ApplyDiceStatBonus(bonus);
                             Debug.Log($"[Steria] SlazeyaClashLosePowerUpNextDice: Powered up dice {currentDiceIdx + 1} by {basePower}");
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     Debug.LogError($"[Steria] Error in OnLoseParrying_Patch: {ex}");
                 }
             }
        }

        // --- Patch for SlazeyaRepeatOnFlow5 Dice Repeat --- 
        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.GiveDamage))] // Patch after damage is dealt
        public static class BattleDiceBehavior_GiveDamage_RepeatPatch
        {
             [HarmonyPostfix]
             public static void Postfix(BattleDiceBehavior __instance) // __instance is the BattleDiceBehavior that just finished GiveDamage
             {
                 try
                 {
                     // Check if this behavior instance was marked for repeat by the ability
                     if (HarmonyHelpers._repeatTriggeredDice.Contains(__instance))
                     {
                         HarmonyHelpers._repeatTriggeredDice.Remove(__instance); // Remove immediately to prevent potential infinite loops

                         // --- Trigger the Repeat by setting isBonusAttack flag ---
                         // This marks the dice for a bonus attack.
                         // The _triggered flag within the ability instance *should* prevent the bonus attack
                         // from re-triggering the repeat marking if OnSucceedAttack runs again for it.
                         // The removal from _repeatTriggeredDice is the main safeguard.
                         Debug.Log($"[MyDLL] BattleDiceBehavior_GiveDamage_RepeatPatch: Repeating dice behavior (Hash: {__instance.GetHashCode()}, Index: {__instance.Index}) via isBonusAttack.");
                         __instance.isBonusAttack = true;
                     }
                 }
                 catch (Exception ex)
                 {
                      Debug.LogError($"[MyDLL] Error in BattleDiceBehavior_GiveDamage_RepeatPatch Postfix: {ex}");
                 }
             }
        }

        // --- Patch for Custom DiceAttackEffect (like 寒昼事务所) ---
        // 自定义特效字典
        public static Dictionary<string, Type> SteriaCustomEffects = new Dictionary<string, Type>();

        // 初始化自定义特效
        public static void InitCustomEffects()
        {
            SteriaCustomEffects.Clear();

            // 注册所有自定义特效
            SteriaCustomEffects["Steria_WindSlash"] = typeof(DiceAttackEffect_Steria_WindSlash);
            SteriaCustomEffects["Steria_WaterSlash"] = typeof(DiceAttackEffect_Steria_WaterSlash);
            SteriaCustomEffects["Steria_WaterHit"] = typeof(DiceAttackEffect_Steria_WaterHit);
            SteriaCustomEffects["Steria_WaterPenetrate"] = typeof(DiceAttackEffect_Steria_WaterPenetrate);
            SteriaCustomEffects["Steria_WaterFarHit"] = typeof(DiceAttackEffect_Steria_WaterFarHit);
            SteriaCustomEffects["Steria_WaterPierce"] = typeof(DiceAttackEffect_Steria_WaterPierce);
            SteriaCustomEffects["Steria_WaterSurround"] = typeof(DiceAttackEffect_Steria_WaterSurround);
            SteriaCustomEffects["Steria_DarkPurpleSlash"] = typeof(DiceAttackEffect_Steria_DarkPurpleSlash);
            SteriaCustomEffects["VeliaThorn_Z"] = typeof(DiceAttackEffect_VeliaThorn_Z);

            SteriaLogger.Log($"Initialized {SteriaCustomEffects.Count} custom effects");
            foreach (var kvp in SteriaCustomEffects)
            {
                SteriaLogger.Log($"  - {kvp.Key} => {kvp.Value.Name}");
            }
        }

        [HarmonyPatch(typeof(DiceEffectManager), "CreateBehaviourEffect")]
        [HarmonyPrefix]
        public static bool DiceEffectManager_CreateBehaviourEffect_Prefix(
            DiceEffectManager __instance,
            ref Battle.DiceAttackEffect.DiceAttackEffect __result,
            string resource,
            float scaleFactor,
            BattleUnitView self,
            BattleUnitView target,
            float time = 1f)
        {
            try
            {
                if (string.IsNullOrEmpty(resource))
                {
                    __result = null;
                    return false;
                }

                // 检查是否是我们的自定义特效
                if (SteriaCustomEffects.TryGetValue(resource, out Type effectType))
                {
                    SteriaLogger.Log($"CreateBehaviourEffect: Creating custom effect '{resource}'");

                    GameObject effectObj = new GameObject(resource);
                    var effect = effectObj.AddComponent(effectType) as Battle.DiceAttackEffect.DiceAttackEffect;

                    if (effect != null)
                    {
                        effect.Initialize(self, target, time);
                        effect.SetScale(scaleFactor);
                        __result = effect;
                        SteriaLogger.Log($"CreateBehaviourEffect: Successfully created '{resource}'");
                        return false; // 跳过原方法
                    }
                    else
                    {
                        SteriaLogger.Log($"CreateBehaviourEffect: Failed to create effect component for '{resource}'");
                        UnityEngine.Object.Destroy(effectObj);
                    }
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"CreateBehaviourEffect ERROR: {ex}");
            }

            return true; // 继续执行原方法
        }

        // --- Patch for 倾诉梦想：远望 (光芒消耗触发) ---
        // 已经有 BattlePlayingCardSlotDetail_SpendCost_Patch，在那里添加触发逻辑

        // --- Patch for 倾诉梦想：幻梦 (书页使用触发) ---
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnUseCard")]
        [HarmonyPostfix]
        public static void BattlePlayingCardDataInUnitModel_OnUseCard_DreamIllusion_Postfix(BattlePlayingCardDataInUnitModel __instance)
        {
            try
            {
                if (__instance?.owner == null) return;

                // 检查是否有倾诉梦想：幻梦 Buff
                var dreamIllusionBuf = __instance.owner.bufListDetail.GetActivatedBufList()
                    .FirstOrDefault(b => b is BattleUnitBuf_DreamIllusion) as BattleUnitBuf_DreamIllusion;

                if (dreamIllusionBuf != null)
                {
                    dreamIllusionBuf.OnCardUsed();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Error in DreamIllusion OnUseCard Postfix: {ex}");
            }
        }

        // --- Patch for 倾诉梦想：奉行 (造成伤害触发) 和 神脉：梦之汐 (造成伤害获得流) ---
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeDamage))]
        public static class BattleUnitModel_TakeDamage_DreamExecution_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleUnitModel __instance, int v, DamageType type, BattleUnitModel attacker)
            {
                try
                {
                    // 只处理攻击伤害且伤害大于0
                    if (attacker == null || v <= 0 || type != DamageType.Attack) return;

                    // 检查攻击者是否有倾诉梦想：奉行 Buff
                    var dreamExecutionBuf = attacker.bufListDetail.GetActivatedBufList()
                        .FirstOrDefault(b => b is BattleUnitBuf_DreamExecution) as BattleUnitBuf_DreamExecution;

                    if (dreamExecutionBuf != null)
                    {
                        dreamExecutionBuf.OnDamageDealt();
                    }

                    // 检查攻击者是否有 神脉：梦之汐 被动 (每造成10点伤害获得1层流)
                    var passive9002001 = attacker.passiveDetail.PassiveList?
                        .FirstOrDefault(p => p is PassiveAbility_9002001) as PassiveAbility_9002001;

                    if (passive9002001 != null)
                    {
                        passive9002001.OnDamageDealt(v);
                    }

                    // 检查攻击者是否有 斯拉泽雅司流者 被动 (每造成10点伤害获得1层流)
                    var passive9002005 = attacker.passiveDetail.PassiveList?
                        .FirstOrDefault(p => p is PassiveAbility_9002005) as PassiveAbility_9002005;

                    if (passive9002005 != null)
                    {
                        passive9002005.OnDamageDealt(v);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in DreamExecution TakeDamage Postfix: {ex}");
                }
            }
        }

        // --- Patch for 潮 (Tide) 自动触发机制 ---
        // 简化版：每当施加者有潮时，自动扣1层潮并额外赋予1层buff

        // 认可的buff类型列表（只有这些类型会触发潮加成）
        private static readonly HashSet<KeywordBuf> _tideValidBuffTypes = new HashSet<KeywordBuf>
        {
            // 正面效果
            KeywordBuf.Strength,      // 强壮
            KeywordBuf.Protection,    // 守护
            KeywordBuf.Quickness,     // 迅捷
            KeywordBuf.Endurance,     // 忍耐（加防御骰子威力）
            KeywordBuf.BreakProtection, // 振奋（降低混乱伤害）
            KeywordBuf.DmgUp,         // 威力提升
            KeywordBuf.SlashPowerUp,  // 斩击威力提升
            KeywordBuf.PenetratePowerUp, // 突刺威力提升
            KeywordBuf.HitPowerUp,    // 打击威力提升
            KeywordBuf.DefensePowerUp, // 防御威力提升
            // 负面效果
            KeywordBuf.Bleeding,      // 流血
            KeywordBuf.Burn,          // 烧伤
            KeywordBuf.Weak,          // 虚弱
            KeywordBuf.Vulnerable,    // 易伤
            KeywordBuf.Binding,       // 束缚
            KeywordBuf.Paralysis,     // 麻痹
            KeywordBuf.Decay,         // 腐蚀
            KeywordBuf.Smoke,         // 烟气
        };

        /// <summary>
        /// 简化版潮加成：只要施加者有潮，就扣1层并额外赋予1层
        /// </summary>
        private static int CheckAndConsumeTideSimple(BattleUnitModel giver, KeywordBuf bufType)
        {
            if (giver == null) return 0;

            // 只处理认可的buff类型
            if (!_tideValidBuffTypes.Contains(bufType)) return 0;

            // 获取施加者的潮buff
            BattleUnitBuf_Tide tideBuf = giver.bufListDetail.GetActivatedBufList()
                .FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;

            if (tideBuf == null || tideBuf.stack <= 0) return 0;

            // 扣1层潮，额外赋予1层buff
            tideBuf.stack -= 1;
            SteriaLogger.Log($"Tide: Giver {giver.UnitData?.unitData?.name} consumed 1 Tide for {bufType}, remaining = {tideBuf.stack}");
            HarmonyHelpers.NotifyPassivesOnTideConsumed(giver, 1);

            if (tideBuf.stack <= 0)
            {
                tideBuf.Destroy();
            }

            return 1; // 固定额外赋予1层
        }

        /// <summary>
        /// 刷新目标的速度骰子（用于迅捷等需要立即生效的buff）
        /// </summary>
        private static void RefreshSpeedDice(BattleUnitModel target, int addedQuickness)
        {
            if (target == null || target.speedDiceResult == null || addedQuickness <= 0) return;

            try
            {
                // 为每个未破坏的速度骰子增加迅捷加成
                foreach (var speedDice in target.speedDiceResult)
                {
                    if (!speedDice.breaked)
                    {
                        speedDice.value = Mathf.Clamp(speedDice.value + addedQuickness, 1, 999);
                    }
                }

                // 重新排序速度骰子（按值从高到低）
                target.speedDiceResult.Sort((d1, d2) =>
                {
                    if (d1.breaked && d2.breaked) return d2.value.CompareTo(d1.value);
                    if (d1.breaked) return -1;
                    if (d2.breaked) return 1;
                    return d2.value.CompareTo(d1.value);
                });

                // 更新UI
                if (target.view?.speedDiceSetterUI != null)
                {
                    target.view.speedDiceSetterUI.SetSpeedDicesAfterRoll(target.speedDiceResult);
                }

                SteriaLogger.Log($"Tide: Refreshed speed dice for {target.UnitData?.unitData?.name}, added {addedQuickness} Quickness");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Error refreshing speed dice: {ex}");
            }
        }

        // Patch for AddKeywordBufByEtc (下回合生效的buff)
        [HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.AddKeywordBufByEtc))]
        public static class BattleUnitBufListDetail_AddKeywordBufByEtc_TidePatch
        {
            [HarmonyPrefix]
            public static void Prefix(BattleUnitBufListDetail __instance, KeywordBuf bufType, ref int stack, BattleUnitModel actor)
            {
                try
                {
                    // 调试日志
                    SteriaLogger.Log($"Tide DEBUG: AddKeywordBufByEtc called - bufType={bufType}, stack={stack}, actor={(actor != null ? actor.UnitData?.unitData?.name : "NULL")}");

                    if (stack <= 0 || actor == null) return;

                    int tideBonus = CheckAndConsumeTideSimple(actor, bufType);
                    if (tideBonus > 0)
                    {
                        stack += tideBonus;
                        SteriaLogger.Log($"Tide: Enhanced {bufType} by {tideBonus}, new stack = {stack}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufByEtc TidePatch: {ex}");
                }
            }
        }

        // Patch for AddKeywordBufThisRoundByEtc (本回合生效的buff)
        [HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.AddKeywordBufThisRoundByEtc))]
        public static class BattleUnitBufListDetail_AddKeywordBufThisRoundByEtc_TidePatch
        {
            // 存储潮加成信息，用于Postfix
            private static int _lastTideBonus = 0;
            private static KeywordBuf _lastBufType = KeywordBuf.None;

            [HarmonyPrefix]
            public static void Prefix(BattleUnitBufListDetail __instance, KeywordBuf bufType, ref int stack, BattleUnitModel actor)
            {
                _lastTideBonus = 0;
                _lastBufType = bufType;

                try
                {
                    // 调试日志
                    SteriaLogger.Log($"Tide DEBUG: AddKeywordBufThisRoundByEtc called - bufType={bufType}, stack={stack}, actor={(actor != null ? actor.UnitData?.unitData?.name : "NULL")}");

                    if (stack <= 0 || actor == null) return;

                    int tideBonus = CheckAndConsumeTideSimple(actor, bufType);
                    if (tideBonus > 0)
                    {
                        stack += tideBonus;
                        _lastTideBonus = tideBonus;
                        SteriaLogger.Log($"Tide: Enhanced {bufType} (this round) by {tideBonus}, new stack = {stack}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufThisRoundByEtc TidePatch Prefix: {ex}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(BattleUnitBufListDetail __instance, KeywordBuf bufType, int stack)
            {
                try
                {
                    // 如果赋予的是迅捷，刷新速度骰子
                    if (_lastBufType == KeywordBuf.Quickness && stack > 0)
                    {
                        var ownerField = AccessTools.Field(typeof(BattleUnitBufListDetail), "_self");
                        BattleUnitModel target = ownerField?.GetValue(__instance) as BattleUnitModel;
                        if (target != null)
                        {
                            RefreshSpeedDice(target, stack);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufThisRoundByEtc TidePatch Postfix: {ex}");
                }
            }
        }

        // Patch for AddKeywordBufByCard (通过卡牌添加的buff，下回合生效)
        [HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.AddKeywordBufByCard))]
        public static class BattleUnitBufListDetail_AddKeywordBufByCard_TidePatch
        {
            [HarmonyPrefix]
            public static void Prefix(BattleUnitBufListDetail __instance, KeywordBuf bufType, ref int stack, BattleUnitModel actor)
            {
                try
                {
                    if (stack <= 0 || actor == null) return;

                    int tideBonus = CheckAndConsumeTideSimple(actor, bufType);
                    if (tideBonus > 0)
                    {
                        stack += tideBonus;
                        SteriaLogger.Log($"Tide: Enhanced {bufType} (by card) by {tideBonus}, new stack = {stack}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufByCard TidePatch: {ex}");
                }
            }
        }

        // Patch for AddKeywordBufThisRoundByCard (通过卡牌添加的buff，本回合生效)
        [HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.AddKeywordBufThisRoundByCard))]
        public static class BattleUnitBufListDetail_AddKeywordBufThisRoundByCard_TidePatch
        {
            private static KeywordBuf _lastBufType = KeywordBuf.None;

            [HarmonyPrefix]
            public static void Prefix(BattleUnitBufListDetail __instance, KeywordBuf bufType, ref int stack, BattleUnitModel actor)
            {
                _lastBufType = bufType;

                try
                {
                    if (stack <= 0 || actor == null) return;

                    int tideBonus = CheckAndConsumeTideSimple(actor, bufType);
                    if (tideBonus > 0)
                    {
                        stack += tideBonus;
                        SteriaLogger.Log($"Tide: Enhanced {bufType} (this round by card) by {tideBonus}, new stack = {stack}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufThisRoundByCard TidePatch Prefix: {ex}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(BattleUnitBufListDetail __instance, KeywordBuf bufType, int stack)
            {
                try
                {
                    // 如果赋予的是迅捷，刷新速度骰子
                    if (_lastBufType == KeywordBuf.Quickness && stack > 0)
                    {
                        var ownerField = AccessTools.Field(typeof(BattleUnitBufListDetail), "_self");
                        BattleUnitModel target = ownerField?.GetValue(__instance) as BattleUnitModel;
                        if (target != null)
                        {
                            RefreshSpeedDice(target, stack);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in AddKeywordBufThisRoundByCard TidePatch Postfix: {ex}");
                }
            }
        }

        // --- Patch for 全体友方本幕不消耗流 (随我流向无尽的尽头) ---
        // 存储本幕是否激活了不消耗流效果
        public static bool NoFlowConsumptionActiveThisRound = false;

        // 在幕开始时重置
        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
        public static class StageController_RoundStartPhase_ResetNoFlowConsumption_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                NoFlowConsumptionActiveThisRound = false;
                // 重置百川逐风的每幕触发记录
                DiceCardSelfAbility_SlazeyaHundredRiversRepeat.ResetTriggeredOwners();

                // 注意：冷却减少现在由 SteriaEgoCooldownPatches 处理

                SteriaLogger.Log("Reset NoFlowConsumptionActiveThisRound and HundredRiversRepeat triggers");
            }
        }

        // --- Patch for 音段接收：在速度骰初始化后重新添加反击闪避骰 ---
        [HarmonyPatch(typeof(BattleUnitModel), "OnRoundStart_speedDice")]
        public static class BattleUnitModel_OnRoundStartSpeedDice_SoundSegmentPatch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleUnitModel __instance)
            {
                try
                {
                    if (__instance == null || __instance.IsDead())
                    {
                        return;
                    }

                    PassiveAbility_9007005 passive = __instance.passiveDetail?.PassiveList
                        ?.FirstOrDefault(p => p is PassiveAbility_9007005) as PassiveAbility_9007005;
                    if (passive == null)
                    {
                        return;
                    }

                    passive.ApplySoundSegmentDice();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error applying SoundSegment dice after speed dice: {ex}");
                }
            }
        }

        // 在战斗结束时清除所有冷却状态和临时数据
        [HarmonyPatch(typeof(StageController), "EndBattle")]
        public static class StageController_EndBattle_ClearCooldowns_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    // 注意：冷却清除现在由 SteriaEgoCooldownPatches 处理
                    // 清除随我流向无尽的尽头的威力加成存储
                    DiceCardSelfAbility_SlazeyaEndlessFlow.ClearPowerBonuses();
                    SteriaLogger.Log("Battle ended: Cleared power bonuses");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error clearing data on battle end: {ex}");
                }
            }
        }

        // --- Patch for [迅攻]：速度更快时不改变目标并避免拼点 ---
        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.CanChangeAttackTarget))]
        public static class BattleUnitModel_CanChangeAttackTarget_RapidAssaultPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(BattleUnitModel __instance, BattleUnitModel target, int myIndex, int targetIndex, ref bool __result)
            {
                try
                {
                    if (__instance?.cardSlotDetail?.cardAry == null || target == null)
                    {
                        return true;
                    }

                    if (myIndex < 0 || myIndex >= __instance.cardSlotDetail.cardAry.Count)
                    {
                        return true;
                    }

                    BattlePlayingCardDataInUnitModel myCard = __instance.cardSlotDetail.cardAry[myIndex];
                    if (myCard == null || !HarmonyHelpers.HasRapidAssaultKeyword(myCard))
                    {
                        return true;
                    }

                    if (target.cardSlotDetail?.cardAry == null || targetIndex < 0 || targetIndex >= target.cardSlotDetail.cardAry.Count)
                    {
                        return true;
                    }

                    int mySpeed = myCard.speedDiceResultValue > 0
                        ? myCard.speedDiceResultValue
                        : __instance.GetSpeedDiceResult(myIndex).value;

                    int targetSpeed = -1;
                    BattlePlayingCardDataInUnitModel targetCard = target.cardSlotDetail.cardAry[targetIndex];
                    if (targetCard != null && targetCard.speedDiceResultValue > 0)
                    {
                        targetSpeed = targetCard.speedDiceResultValue;
                    }
                    else if (target.speedDiceResult != null && targetIndex < target.speedDiceResult.Count)
                    {
                        targetSpeed = target.GetSpeedDiceResult(targetIndex).value;
                    }

                    if (mySpeed <= 0 || targetSpeed <= 0)
                    {
                        return true;
                    }

                    if (mySpeed > targetSpeed)
                    {
                        __result = false;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] RapidAssault CanChangeAttackTarget patch error: {ex}");
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(StageController), "StartParrying")]
        public static class StageController_StartParrying_RapidAssaultPatch
        {
            private static readonly MethodInfo _startActionMethod =
                AccessTools.Method(typeof(StageController), "StartAction", new Type[] { typeof(BattlePlayingCardDataInUnitModel) });

            [HarmonyPrefix]
            public static bool Prefix(StageController __instance, BattlePlayingCardDataInUnitModel cardA, BattlePlayingCardDataInUnitModel cardB)
            {
                try
                {
                    BattlePlayingCardDataInUnitModel rapidCard = null;
                    if (HarmonyHelpers.IsRapidAssaultFaster(cardA, cardB))
                    {
                        rapidCard = cardA;
                    }
                    else if (HarmonyHelpers.IsRapidAssaultFaster(cardB, cardA))
                    {
                        rapidCard = cardB;
                    }

                    if (rapidCard == null || rapidCard.target == null)
                    {
                        return true;
                    }

                    if (_startActionMethod == null)
                    {
                        SteriaLogger.LogWarning("RapidAssault: StartAction method not found, falling back to parrying.");
                        return true;
                    }

                    _startActionMethod.Invoke(__instance, new object[] { rapidCard });
                    SteriaLogger.Log($"RapidAssault: One-side action by {rapidCard.owner?.UnitData?.unitData?.name}");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] RapidAssault StartParrying patch error: {ex}");
                }

                return true;
            }
        }

        // --- Patch for 荆棘攻击移除机制 ---
        // 当拥有荆棘的单位进行单方面攻击时，移除1层荆棘
        [HarmonyPatch(typeof(BattleOneSidePlayManager), "StartOneSidePlay")]
        public static class BattleOneSidePlayManager_StartOneSidePlay_ThornPatch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleOneSidePlayManager __instance, BattlePlayingCardDataInUnitModel card)
            {
                try
                {
                    if (card?.owner == null) return;

                    BattleUnitModel attacker = card.owner;

                    // 检查攻击者是否有荆棘
                    BattleUnitBuf_Thorn thornBuf = attacker.bufListDetail?.GetActivatedBufList()
                        ?.FirstOrDefault(b => b is BattleUnitBuf_Thorn) as BattleUnitBuf_Thorn;

                    if (thornBuf != null && thornBuf.stack > 0)
                    {
                        thornBuf.OnOneSidedAttack();
                        SteriaLogger.Log($"Thorn: {attacker.UnitData?.unitData?.name} attacked, Thorn reduced");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Steria] Error in BattleOneSidePlayManager Thorn patch: {ex}");
                }
            }
        }

    } // End of HarmonyPatches class

    /// <summary>
    /// Harmony补丁：修复mod核心书页的OnlyCard无法正确加载mod卡牌的问题
    /// 原版游戏使用int类型的ID，无法识别mod卡牌（需要完整的LorId包含workshopID）
    /// </summary>
    [HarmonyPatch(typeof(BookModel), "SetXmlInfo")]
    public static class BookModel_SetXmlInfo_OnlyCardFix_Patch
    {
        // Steria mod的workshopID
        private const string STERIA_WORKSHOP_ID = "NormalInvitation";

        // Steria mod的核心书页ID列表
        private static readonly HashSet<int> _steriaBookIds = new HashSet<int>
        {
            10000001, // 安希尔
            10000002, // 斯拉泽雅
            10000003, // 司流者教徒
            10000004, // 薇莉亚
        };

        [HarmonyPostfix]
        public static void Postfix(BookModel __instance, BookXmlInfo classInfo)
        {
            try
            {
                // 检查是否是Steria mod的核心书页
                if (classInfo == null || !_steriaBookIds.Contains(classInfo.id.id))
                {
                    return;
                }

                // 只处理mod书页（有workshopID的）
                if (!classInfo.id.IsWorkshop())
                {
                    return;
                }

                // 获取_onlyCards字段
                var onlyCardsField = AccessTools.Field(typeof(BookModel), "_onlyCards");
                if (onlyCardsField == null) return;

                var onlyCards = onlyCardsField.GetValue(__instance) as List<DiceCardXmlInfo>;
                if (onlyCards == null)
                {
                    onlyCards = new List<DiceCardXmlInfo>();
                    onlyCardsField.SetValue(__instance, onlyCards);
                }

                // 遍历OnlyCard列表，尝试使用mod的workshopID加载卡牌
                foreach (int cardId in classInfo.EquipEffect.OnlyCard)
                {
                    // 检查是否已经加载了这张卡牌
                    bool alreadyLoaded = onlyCards.Exists(x => x.id.IsBasic() ? x.id.id == cardId : x.id.id == cardId);
                    if (alreadyLoaded) continue;

                    // 尝试使用mod的workshopID加载卡牌
                    LorId modCardId = new LorId(STERIA_WORKSHOP_ID, cardId);
                    DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(modCardId, true);

                    if (cardItem != null && cardItem.id.IsWorkshop())
                    {
                        onlyCards.Add(cardItem);
                        SteriaLogger.Log($"OnlyCardFix: Added mod card {modCardId} to book {classInfo.id}");
                    }
                    else
                    {
                        SteriaLogger.Log($"OnlyCardFix: Failed to find mod card {modCardId} for book {classInfo.id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] OnlyCardFix error: {ex}");
            }
        }
    }

    // --- 扩展 CardAbilityHelper 以支持流x2 ---
    public static class FlowMultiplierHelper
    {
        /// <summary>
        /// 添加流层数，考虑流x2被动
        /// </summary>
        public static void AddFlowStacksWithMultiplier(BattleUnitModel owner, int amount)
        {
            if (owner == null || amount <= 0) return;

            // 检查是否有流x2被动 (PassiveAbility_9002001)
            if (PassiveAbility_9002001.HasPassive(owner))
            {
                int originalAmount = amount;
                amount *= 2;
                SteriaLogger.Log($"FlowMultiplier: {owner.UnitData?.unitData?.name} has 流x2 passive, {originalAmount} -> {amount}");
            }

            // 调用原始的添加流方法
            CardAbilityHelper.AddFlowStacks(owner, amount);
        }
    }
} // End of Steria namespace
