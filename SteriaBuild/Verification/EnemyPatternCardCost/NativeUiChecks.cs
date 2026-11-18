using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LOR_DiceSystem;
using Steria;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

// Run inside Unity. Exercise the original game's SetCard through its cost binding,
// including NumbersData sprite selection. The unrelated artwork/description tail is
// omitted in this fixture; this is not a complete in-game card screen acceptance.
public static class NativeUiChecks
{
    private static UISpriteDataManager _sprites;

    private static void Set(object target, string name, object value)
    {
        AccessTools.Field(target.GetType(), name).SetValue(target, value);
    }

    private static IEnumerable<CodeInstruction> CostBindingOnly(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo end = AccessTools.Method(typeof(NumbersData), "SetOneValue");
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(end)) { yield return new CodeInstruction(OpCodes.Ret); yield break; }
        }
    }

    private static bool SpriteManager(ref UISpriteDataManager __result)
    {
        __result = _sprites;
        return false;
    }

    public static void Run()
    {
        Program.Run();
        var root = new GameObject("Native cost bind fixture");
        root.SetActive(false);
        var ui = root.AddComponent<BattleDiceCardUI>();
        ui.enabled = false;
        Set(ui, "ob_NormalFrames", new GameObject[0]);
        Set(ui, "ob_EgoFrames", new GameObject[0]);
        Set(ui, "txt_cardName", root.AddComponent<TextMeshProUGUI>());
        Set(ui, "txt_Resist", new List<TextMeshProUGUI>());
        Set(ui, "txt_bpResist", new List<TextMeshProUGUI>());
        Set(ui, "hsv_behaviourIcons", new List<RefineHsv>());
        var imageObject = new GameObject("Native cost digit", typeof(RectTransform));
        imageObject.transform.SetParent(root.transform, false);
        var digit = imageObject.AddComponent<Image>();
        var numbers = new NumbersData { firstNumbers = new NumbersData.NumbersImageData { content = digit } };
        Set(ui, "costNumbers", numbers);
        var normal = new Sprite[10];
        var discounted = new Sprite[10];
        var texture = new Texture2D(10, 2);
        for (int i = 0; i < 10; i++)
        {
            normal[i] = Sprite.Create(texture, new Rect(i, 0, 1, 1), Vector2.zero);
            discounted[i] = Sprite.Create(texture, new Rect(i, 1, 1, 1), Vector2.zero);
        }
        Set(ui, "costNumberSprite", normal);
        _sprites = root.AddComponent<UISpriteDataManager>();
        // Resolve the actual serialized backing field without relying on its spelling.
        var getter = AccessTools.PropertyGetter(typeof(UISpriteDataManager), "CardCostAddGlow");
        foreach (var instruction in PatchProcessor.GetOriginalInstructions(getter))
            if (instruction.opcode == OpCodes.Ldfld)
                ((FieldInfo)instruction.operand).SetValue(_sprites, discounted);

        var harmony = new Harmony("steria.enemy-pattern-cost.fixture");
        harmony.CreateClassProcessor(typeof(EnemyPatternCardCostUiPatch)).Patch();
        harmony.Patch(AccessTools.PropertyGetter(typeof(UISpriteDataManager), "instance"),
            prefix: new HarmonyMethod(typeof(NativeUiChecks), "SpriteManager"));
        harmony.Patch(AccessTools.Method(typeof(BattleDiceCardUI), "SetCard"),
            transpiler: new HarmonyMethod(typeof(NativeUiChecks), "CostBindingOnly") { priority = Priority.Last });
        try
        {
            var unitFactory = AccessTools.Method(typeof(Program), "Unit");
            var cardFactory = AccessTools.Method(typeof(Program), "Card");
            var enemy = (BattleUnitModel)unitFactory.Invoke(null, new object[] { Faction.Enemy });
            var free = (BattleDiceCardModel)cardFactory.Invoke(null, new object[] { enemy, 4, "SteriaBuilding" });
            free.XmlData.workshopName = "Scripted page";
            EnemyPatternCardCost.MakeFree(free);
            ui.SetCard(free);
            if (digit.sprite != normal[4] || (int)AccessTools.Field(typeof(BattleDiceCardUI), "_cost").GetValue(ui) != 4)
                throw new Exception("Native SetCard did not select ordinary digit 4 and matching style cache.");
            Debug.Log("PASS native SetCard: printed 4 / ordinary sprite / payable 0");
            var ordinary = (BattleDiceCardModel)cardFactory.Invoke(null, new object[] { enemy, 4, "SteriaBuilding" });
            ordinary.XmlData.workshopName = "Ordinary discount";
            ordinary.SetCostToZero(true);
            ui.SetCard(ordinary);
            if (digit.sprite != discounted[0] || (int)AccessTools.Field(typeof(BattleDiceCardUI), "_cost").GetValue(ui) != 0)
                throw new Exception("Native SetCard reuse leaked the scripted-page cost into an ordinary discount.");
            ui.SetCard(free);
            if (digit.sprite != normal[4] || free.GetCost() != 0)
                throw new Exception("Native SetCard rebind did not restore printed cost without changing payment.");
            Debug.Log("ENEMY_PATTERN_NATIVE_UI_PASS: scripted -> ordinary discount -> scripted");
        }
        finally { harmony.UnpatchSelf(); UnityEngine.Object.DestroyImmediate(root); }
    }
}
