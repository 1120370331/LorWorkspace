using System;
using System.Collections.Generic;
using System.Linq;

namespace Steria
{
    internal static class TideConsumptionTracker
    {
        private static readonly Dictionary<BattleUnitModel, int> _lastTideStacks = new Dictionary<BattleUnitModel, int>();

        public static void Reset()
        {
            _lastTideStacks.Clear();
        }

        public static void CaptureAllUnits()
        {
            _lastTideStacks.Clear();
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            List<BattleUnitModel> units = BattleObjectManager.instance.GetAliveList(false);
            if (units == null)
            {
                return;
            }

            foreach (BattleUnitModel unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                _lastTideStacks[unit] = GetTideStacks(unit);
            }
        }

        public static void NotifyConsumed(BattleUnitModel owner, bool isGolden)
        {
            if (owner == null)
            {
                return;
            }

            if (!_lastTideStacks.ContainsKey(owner))
            {
                _lastTideStacks[owner] = GetTideStacks(owner);
                return;
            }

            if (!isGolden)
            {
                _lastTideStacks[owner] = GetTideStacks(owner);
            }
        }

        public static void FlushUnnotified()
        {
            if (BattleObjectManager.instance == null)
            {
                return;
            }

            List<BattleUnitModel> units = BattleObjectManager.instance.GetAliveList(false);
            if (units == null)
            {
                return;
            }

            foreach (BattleUnitModel unit in units)
            {
                if (unit == null || unit.IsDead())
                {
                    continue;
                }

                int current = GetTideStacks(unit);
                if (_lastTideStacks.TryGetValue(unit, out int last))
                {
                    int diff = Math.Max(0, last - current);
                    if (diff > 0)
                    {
                        HarmonyHelpers.NotifyPassivesOnTideConsumed(unit, diff);
                    }
                }

                _lastTideStacks[unit] = current;
            }
        }

        private static int GetTideStacks(BattleUnitModel unit)
        {
            if (unit == null)
            {
                return 0;
            }

            BattleUnitBuf_Tide tideBuf = unit.bufListDetail?.GetActivatedBufList()
                ?.FirstOrDefault(b => b is BattleUnitBuf_Tide) as BattleUnitBuf_Tide;
            return tideBuf?.stack ?? 0;
        }
    }
}
