using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BattleCharacterProfile;
using HarmonyLib;
using UnityEngine;

namespace AceTaffy
{
    // Extend only this reception's runtime formation; never mutate shared formation XML.
    public static class TaffyReceptionFormation
    {
        public static void Ensure(StageWaveModel wave)
        {
            var positions = wave.GetFormation().PostionList;
            // Native enemy formationCellPos mirrors X. Larger authored X is
            // farther left: workshop (slot 1), Taffy (slot 0), then a 2x2 block.
            var layout = new[] {
                new Vector2Int(16, 0), new Vector2Int(24, 0),
                new Vector2Int(8, 6), new Vector2Int(2, 6),
                new Vector2Int(8, -6), new Vector2Int(2, -6)
            };
            for (int i = 0; i < layout.Length; i++)
            {
                var data = new FormationPositionXmlData {
                    name = "AceTaffy_Formation_" + i,
                    vector = new XmlVector2 { x = layout[i].x, y = layout[i].y },
                    eventList = new List<FormationPositionEventXmlData>()
                };
                if (i >= positions.Count)
                    positions.Add(new FormationPosition(data) { index = i, eventList = new List<FormationPositionEvent>() });
                else
                {
                    // Keep the position object already referenced by Taffy.
                    // Own a new default so native resets cannot restore formation 41,
                    // without changing the shared FormationXmlList entry.
                    positions[i]._xmlInfo = data;
                    positions[i].ChangePos(layout[i]);
                }
            }
        }

        private sealed class ProfileLayout
        {
            public BattleCharacterProfileUI[] Original;
            public Vector3[] Positions;
            public Vector3[] Scales;
            public BattleCharacterProfileUI[] Expanded;
        }
        private static readonly ConditionalWeakTable<BattleCharacterProfileUI[], ProfileLayout> Layouts =
            new ConditionalWeakTable<BattleCharacterProfileUI[], ProfileLayout>();

        public static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(BattleUnitInfoManagerUI), "Initialize"),
                prefix: new HarmonyMethod(typeof(TaffyReceptionFormation), nameof(PrepareProfiles)));
            harmony.Patch(AccessTools.Method(typeof(BattleEmotionCoinUI), "Init"),
                prefix: new HarmonyMethod(typeof(TaffyReceptionFormation), nameof(PrepareCoins)));
        }

        public static void PrepareCoins(BattleEmotionCoinUI __instance)
        {
            if (!BattleObjectManager.instance.GetList(Faction.Enemy).Any(u => TaffyInventions.Inventor(u) != null)) return;
            bool right = Singleton<StageController>.Instance.AllyFormationDirection == Direction.RIGHT;
            var coins = right ? __instance.enermy : __instance.librarian;
            if (coins.Length >= TaffyInventions.EnemySlots) return;
            var last = coins[coins.Length - 1];
            var copy = Object.Instantiate(last.target, last.target.parent);
            copy.name = "AceTaffy_SixthEnemyCoinTarget";
            copy.position = last.target.position + last.target.position - coins[coins.Length - 2].target.position;
            var expanded = coins.Concat(new[] { new BattleEmotionCoinUI.BattleEmotionCoinData {
                cosFactor = last.cosFactor, sinFactor = last.sinFactor, target = copy
            } }).ToArray();
            if (right) __instance.enermy = expanded;
            else __instance.librarian = expanded;
        }

        public static void PrepareProfiles(BattleUnitInfoManagerUI __instance, IList<BattleUnitModel> unitList)
        {
            bool active = unitList.Any(u => u.faction == Faction.Enemy && TaffyInventions.Inventor(u) != null);
            if (!active)
            {
                RestoreProfiles(__instance.enemyProfileArray);
                RestoreProfiles(__instance.allyProfileArray);
                return;
            }
            bool right = Singleton<StageController>.Instance.AllyFormationDirection == Direction.RIGHT;
            var profiles = right ? __instance.enemyProfileArray : __instance.allyProfileArray;
            ProfileLayout layout;
            if (!Layouts.TryGetValue(profiles, out layout))
            {
                if (!active || profiles.Length >= TaffyInventions.EnemySlots) return;
                layout = new ProfileLayout { Original = profiles,
                    Positions = profiles.Select(p => p.transform.localPosition).ToArray(),
                    Scales = profiles.Select(p => p.transform.localScale).ToArray() };
                var copy = Object.Instantiate(profiles[profiles.Length - 1], profiles[0].transform.parent);
                copy.name = "AceTaffy_SixthEnemyProfile";
                layout.Expanded = profiles.Concat(new[] { copy }).ToArray();
                Layouts.Add(profiles, layout);
                Layouts.Add(layout.Expanded, layout);
            }
            Vector3 step = layout.Positions[1] - layout.Positions[0];
            float ratio = (layout.Original.Length - 1f) / (layout.Expanded.Length - 1f);
            for (int i = 0; i < layout.Expanded.Length; i++)
            {
                layout.Expanded[i].transform.localPosition = layout.Positions[0] + step * (i * ratio);
                layout.Expanded[i].transform.localScale = layout.Scales[0] * ratio;
            }
            if (right) __instance.enemyProfileArray = layout.Expanded;
            else __instance.allyProfileArray = layout.Expanded;
        }

        private static void RestoreProfiles(BattleCharacterProfileUI[] profiles)
        {
            ProfileLayout layout;
            if (profiles == null || !Layouts.TryGetValue(profiles, out layout)) return;
            for (int i = 0; i < layout.Original.Length; i++)
            {
                layout.Original[i].transform.localPosition = layout.Positions[i];
                layout.Original[i].transform.localScale = layout.Scales[i];
            }
            layout.Expanded[layout.Expanded.Length - 1].gameObject.SetActive(false);
        }
    }
}
