using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using LOR_DiceSystem;
using Steria;

public static class Program
{
    private static int _checks;
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        Console.WriteLine("PASS " + (++_checks) + ": " + label);
    }

    private static BattleUnitModel Unit(Faction faction = Faction.Enemy)
    {
        var unit = new BattleUnitModel(1) { faction = faction };
        unit.equipment.book = new BookModel(new BookXmlInfo { _id = 9000001 });
        unit.equipment.book.SetMaxPlayPoint(4);
        var data = (UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        data.emotionDetail = new BattleUnitEmotionDetail(unit);
        AccessTools.Field(typeof(BattleUnitModel), "_unitData").SetValue(unit, data);
        unit.breakDetail.breakLife = 1;
        return unit;
    }

    private static BattleDiceCardModel Card(BattleUnitModel owner, int cost = 4,
        string package = "SteriaBuilding")
    {
        var xml = new DiceCardXmlInfo(new LorId(package, 9002009));
        xml.Spec.Cost = cost;
        var card = new BattleDiceCardModel { owner = owner };
        AccessTools.Field(typeof(BattleDiceCardModel), "_xmlData").SetValue(card, xml);
        card.SetCurrentCost(cost);
        return card;
    }

    public static int Main()
    {
        try { Run(); Console.WriteLine("ENEMY_PATTERN_COST_PASS " + _checks); return 0; }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    public static void Run()
    {
        BattleObjectManager.instance.Init_only();
        var enemy = Unit();
        var card = Card(enemy);
        card.SetPriorityAdder(100);
        EnemyPatternCardCost.MakeFree(card);
        Check(card.GetCost() == 0 && EnemyPatternCardCost.GetDisplayCost(card) == 4,
            "scripted enemy page displays printed 4 while native cost stays zero");
        Check(card.GetOriginCost() == 4 && card.GetPriorityAdder() == 100,
            "shared XML cost and scripted priority are preserved");

        enemy.cardSlotDetail.SetPlayPoint(0);
        Check(enemy.cardSlotDetail.OnApplyCard(card) && enemy.cardSlotDetail.ReservedPlayPoint == 0,
            "native reservation succeeds at zero Light without reserving printed cost");
        Check(enemy.cardSlotDetail.SpendCost(card.GetCost()) && enemy.PlayPoint == 0 &&
            enemy.history.costAtOneRound == 0, "native spending consumes no Light or spend history");

        var normal = Card(enemy);
        Check(EnemyPatternCardCost.GetDisplayCost(normal) == 4,
            "same-ID unmarked instance retains normal cost");
        normal.SetCostToZero(true);
        Check(EnemyPatternCardCost.GetDisplayCost(normal) == 0,
            "ordinary zero-cost effect remains visibly zero");
        normal.SetCostToZero(false);
        normal.SetCurrentCost(2);
        Check(EnemyPatternCardCost.GetDisplayCost(normal) == 2,
            "ordinary partial discount remains visible");

        var player = Card(Unit(Faction.Player));
        EnemyPatternCardCost.MakeFree(player);
        Check(player.GetCost() == 0 && EnemyPatternCardCost.GetDisplayCost(player) == 0,
            "player discount remains visibly zero");
        var foreign = Card(enemy, 4, "AnotherMod");
        EnemyPatternCardCost.MakeFree(foreign);
        Check(EnemyPatternCardCost.GetDisplayCost(foreign) == 0,
            "foreign-mod pages do not acquire presentation exemption");

        EnemyPatternCardCost.MakeFree(card);
        Check(EnemyPatternCardCost.GetDisplayCost(card) == 4, "repeat grant is idempotent");
        card.owner = Unit();
        Check(EnemyPatternCardCost.GetDisplayCost(card) == 0,
            "transferring an instance does not transfer the old enemy's display exemption");
        card.owner = enemy;
        card.SetCostToZero(false);
        Check(EnemyPatternCardCost.GetDisplayCost(card) == card.GetCost(),
            "removing free cost restores native display");
        var zero = Card(enemy, 0);
        EnemyPatternCardCost.MakeFree(zero);
        Check(EnemyPatternCardCost.GetDisplayCost(zero) == 0, "printed-zero page remains zero");
        EnemyPatternCardCost.MakeFree(null);

        MethodInfo bind = AccessTools.Method(typeof(BattleDiceCardUI), "SetCard");
        MethodInfo getCost = AccessTools.Method(typeof(BattleDiceCardModel), "GetCost");
        MethodInfo display = AccessTools.Method(typeof(EnemyPatternCardCost), "GetDisplayCost");
        List<CodeInstruction> original = PatchProcessor.GetOriginalInstructions(bind);
        int nativeReads = original.Count(x => x.Calls(getCost));
        Check(nativeReads == 2, "real game SetCard has two cost reads: style cache and digits");
        var rewritten = EnemyPatternCardCostUiPatch.Transpiler(original).ToList();
        Check(rewritten.Count(x => x.Calls(display)) == nativeReads && !rewritten.Any(x => x.Calls(getCost)),
            "both native UI cost reads use the display value, avoiding discount styling");
        foreach (var method in new[] {
            AccessTools.Method(typeof(BattleUnitModel), "CheckCardAvailable"),
            AccessTools.Method(typeof(BattleUnitModel), "SpendCardAndCost"),
            AccessTools.Method(typeof(BattlePlayingCardSlotDetail), "OnApplyCard") })
            Check(PatchProcessor.GetOriginalInstructions(method).Any(x => x.Calls(getCost)),
                method.Name + " still reads the actual payable cost");

        MethodInfo makeFree = AccessTools.Method(typeof(EnemyPatternCardCost), "MakeFree");
        foreach (var method in new[] {
            AccessTools.Method(typeof(PassiveAbility_9002006), "AddNewCard"),
            AccessTools.Method(typeof(PassiveAbility_9009007), "AddNewCard"),
            AccessTools.Method(typeof(PassiveAbility_9005001), "AddNewCard"),
            AccessTools.Method(typeof(PassiveAbility_9005006), "OnRoundStart"),
            AccessTools.Method(typeof(PassiveAbility_9010001), "OnRoundStart") })
            Check(PatchProcessor.GetOriginalInstructions(method).Any(x => x.Calls(makeFree)),
                method.DeclaringType.Name + " registers its real generated/drawn instances");
    }
}
