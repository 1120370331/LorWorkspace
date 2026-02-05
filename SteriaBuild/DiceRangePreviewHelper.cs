using System;
using LOR_DiceSystem;
using UnityEngine;

namespace Steria
{
    internal static class DiceRangePreviewHelper
    {
        private const int CardNightTrace = 9007006;
        private const int CardSeaReturn = 9008006;
        private const int CardChristashaGlory = 9009006;

        public static bool TryGetAdjustedRange(BattleDiceCardModel cardModel, DiceBehaviour behaviour, out int min, out int max)
        {
            min = behaviour?.Min ?? 0;
            max = behaviour?.Dice ?? 0;

            if (cardModel == null || behaviour == null)
            {
                return false;
            }

            DiceCardXmlInfo xml = cardModel.XmlData;
            int cardId = xml?.id.id ?? 0;

            switch (cardId)
            {
                case CardNightTrace:
                    return TryApplyNightTrace(cardModel, ref min, ref max);
                case CardSeaReturn:
                    return TryApplySeaReturn(cardModel, ref min, ref max);
                case CardChristashaGlory:
                    return TryApplyGloryScale(cardModel, ref min, ref max);
                default:
                    return false;
            }
        }

        private static bool TryApplyNightTrace(BattleDiceCardModel cardModel, ref int min, ref int max)
        {
            BattleUnitModel owner = cardModel.owner as BattleUnitModel;
            if (owner == null)
            {
                return false;
            }

            int growth = global::AilierelAbilityHelper.GetNightTraceGrowth(owner, 3);
            if (growth <= 0)
            {
                return false;
            }

            min = Math.Max(1, min + growth);
            max = Math.Max(1, max + growth * 2);
            return true;
        }

        private static bool TryApplySeaReturn(BattleDiceCardModel cardModel, ref int min, ref int max)
        {
            BattleUnitModel owner = cardModel.owner as BattleUnitModel;
            if (owner == null)
            {
                return false;
            }

            int count = global::DiceCardSelfAbility_SivierSeaReturn.GetUseCountForUnit(owner);
            if (count <= 0)
            {
                return false;
            }

            min = Math.Max(1, min - count);
            max = Math.Max(1, max - count * 2);
            return true;
        }

        private static bool TryApplyGloryScale(BattleDiceCardModel cardModel, ref int min, ref int max)
        {
            DiceCardXmlInfo xml = cardModel.XmlData;
            int diceCount = xml?.DiceBehaviourList?.Count ?? 0;
            if (diceCount <= 1)
            {
                return false;
            }

            float factor = (diceCount - 1f) / diceCount;
            int scaledMin = Mathf.CeilToInt(min * factor);
            int scaledMax = Mathf.CeilToInt(max * factor);

            min = Math.Max(1, scaledMin);
            max = Math.Max(1, scaledMax);
            return true;
        }
    }
}
