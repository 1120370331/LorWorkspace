using HarmonyLib;
using System;
using System.Reflection;
using UI;
using UnityEngine;

namespace Steria
{
    public static class AntieDeckUiPatches
    {
        private const string MOD_ID = "SteriaBuilding";
        private const int ANTIE_BOOK_ID = 99000011;
        private static readonly string[] TAB_NAMES = { "司潮形态", "司梦形态", "司流形态" };
        private static readonly FieldInfo DeckTabsControllerField =
            AccessTools.Field(typeof(UIEquipDeckCardList), "deckTabsController");

        [HarmonyPatch(typeof(UIEquipDeckCardList), nameof(UIEquipDeckCardList.OpenInit))]
        public static class UIEquipDeckCardList_OpenInit_AntieDeckTabsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIEquipDeckCardList __instance)
            {
                ApplyAntieTabNames(__instance, __instance?.currentunit);
            }
        }

        [HarmonyPatch(typeof(UIEquipDeckCardList), nameof(UIEquipDeckCardList.SetData))]
        public static class UIEquipDeckCardList_SetData_AntieDeckTabsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIEquipDeckCardList __instance, UnitDataModel unitdata)
            {
                ApplyAntieTabNames(__instance, unitdata);
            }
        }

        [HarmonyPatch(typeof(UIEquipDeckCardList), nameof(UIEquipDeckCardList.OnChangeDeckTab))]
        public static class UIEquipDeckCardList_OnChangeDeckTab_AntieDeckTabsPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(UIEquipDeckCardList __instance)
            {
                if (!IsAntieBook(__instance?.currentunit))
                {
                    return true;
                }

                UICustomTabsController controller = GetDeckTabsController(__instance);
                if (controller == null || controller.GetCurrentIndex() < TAB_NAMES.Length)
                {
                    return true;
                }

                controller.OnSelectTab(0);
                return false;
            }
        }

        private static void ApplyAntieTabNames(UIEquipDeckCardList deckCardList, UnitDataModel unitdata)
        {
            try
            {
                if (!IsAntieBook(unitdata))
                {
                    return;
                }

                UICustomTabsController controller = GetDeckTabsController(deckCardList);
                UICustomTabButton[] tabs = controller?.CustomTabs;
                if (tabs == null || tabs.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < tabs.Length; i++)
                {
                    UICustomTabButton tab = tabs[i];
                    if (tab == null)
                    {
                        continue;
                    }

                    bool enabled = i < TAB_NAMES.Length;
                    tab.gameObject.SetActive(enabled);
                    if (enabled && tab.TabName != null)
                    {
                        tab.TabName.text = TAB_NAMES[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Error applying Antie deck tab names: {ex}");
            }
        }

        private static UICustomTabsController GetDeckTabsController(UIEquipDeckCardList deckCardList)
        {
            return DeckTabsControllerField?.GetValue(deckCardList) as UICustomTabsController;
        }

        private static bool IsAntieBook(UnitDataModel unitdata)
        {
            LorId id = unitdata?.bookItem?.BookId;
            return id != null &&
                id.id == ANTIE_BOOK_ID &&
                (id.packageId == MOD_ID || string.IsNullOrEmpty(id.packageId));
        }
    }
}
