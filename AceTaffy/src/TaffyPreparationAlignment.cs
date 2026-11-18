using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UI;
using UnityEngine;
using Workshop;

namespace AceTaffy
{
    // UICharacterRenderer renders every preparation portrait from a dedicated
    // camera. Move only AceTaffy's UI instance; battle/world pivots stay frozen.
    public static class TaffyPreparationAlignment
    {
        // Native UI slots anchor each appearance at x=10*index. Center Taffy's
        // baked Default head (+17.59px from root at 100 PPU) on that anchor.
        // Do not copy Bada/Malkuth's -33px pose lean: that pushed Taffy left.
        private const float ShapeCorrectionUnits = -0.1759f;
        private static bool _installed;
        private static readonly Dictionary<UICharacter, CharacterAppearance> ShiftedSlots =
            new Dictionary<UICharacter, CharacterAppearance>();

        public static void Install(Harmony harmony)
        {
            if (_installed)
                return;
            var original = AccessTools.Method(typeof(UICharacterRenderer), nameof(UICharacterRenderer.SetCharacter));
            var postfix = AccessTools.Method(typeof(TaffyPreparationAlignment), nameof(AfterSetCharacter));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            _installed = true;
        }

        private static void AfterSetCharacter(UICharacterRenderer __instance, UnitDataModel unit, int index)
        {
            if (unit == null || index < 0 || index >= 11)
                return;
            UICharacter character = __instance.GetUICharacterByIndex(index);
            if (character == null || character.unitModel != unit || character.unitAppearance == null)
                return;
            Transform transform = character.unitAppearance.transform;
            Vector3 position = transform.localPosition;
            if (IsAceTaffySkin(character.unitAppearance))
            {
                // SetCharacter assigns x=10*index. Absolute placement is
                // idempotent even when the renderer reuses its appearance.
                float uiScale = unit.customizeData != null
                    ? unit.customizeData.height * 0.005f : 1f;
                position.x = 10f * index + ShapeCorrectionUnits * uiScale;
                transform.localPosition = position;
                ShiftedSlots[character] = character.unitAppearance;
            }
            else
            {
                CharacterAppearance shifted;
                if (ShiftedSlots.TryGetValue(character, out shifted))
                {
                    if (ReferenceEquals(shifted, character.unitAppearance))
                    {
                        // Only undo our own prior shift if SetData swapped
                        // this same preview instance to another skin.
                        position.x = 10f * index;
                        transform.localPosition = position;
                    }
                    ShiftedSlots.Remove(character);
                }
            }
        }

        private static bool IsAceTaffySkin(CharacterAppearance appearance)
        {
            var setter = appearance.GetComponent<WorkshopSkinDataSetter>();
            if (setter == null || setter.dic == null)
                return false;
            ClothCustomizeData data;
            if (!setter.dic.TryGetValue(ActionDetail.Default, out data) || data == null ||
                string.IsNullOrEmpty(data.spritePath) || string.IsNullOrEmpty(AceTaffyMod.RootPath))
                return false;
            string actual;
            try { actual = Path.GetFullPath(data.spritePath); }
            catch (ArgumentException) { return false; }
            foreach (string baseDirectory in new[] { "Char", Path.Combine("Resource", "CharacterSkin") })
            {
                foreach (string skin in new[] { "AceTaffy", "AceTaffy_Projection" })
                {
                    string expected = Path.GetFullPath(Path.Combine(
                        AceTaffyMod.RootPath, baseDirectory, skin, "ClothCustom", "Default.png"));
                    if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
    }
}
