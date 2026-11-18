using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.Serialization;
using HarmonyLib;
using LOR_DiceSystem;
using Steria;
using UnityEngine;
using UnityEngine.UI;

public static class AntieOverflowScenario
{
    private const string Package = "SteriaBuilding";
    private static readonly int[] CupIds = { 9011601, 9011602, 9011603 };
    private static int _nextUnit;
    private static int _checks;
    private static bool _managed;
    private static BattleManagerUI _summary;

    public static int Main(string[] args)
    {
        try
        {
            string gameManaged = @"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Managed";
            AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
            {
                string path = Path.Combine(gameManaged, new AssemblyName(request.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            _managed = true;
            Run(args[0]);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    public static void Run(string repository)
    {
        var harmony = new Harmony("steria.antie.overflow.fixture");
        var product = typeof(PassiveAbility_9011003).Assembly;
        foreach (var type in product.GetTypes().Where(type => type.Namespace == "Steria" &&
            type.Name.StartsWith("Antie", StringComparison.Ordinal) && type.IsDefined(typeof(HarmonyPatch), false)))
            harmony.CreateClassProcessor(type).Patch();

        // Keep native roll, decision, damage and equipment logic. Bypass platform achievements and presentation continuations.
        var skip = new HarmonyMethod(typeof(AntieOverflowScenario), nameof(Skip));
        harmony.Patch(AccessTools.Method(typeof(BattleDiceBehavior), "SaveBeforeRollDiceStateForAchievements"), prefix: skip);
        harmony.Patch(AccessTools.Method(typeof(BattleParryingManager), "ActionPhase"), prefix: skip);
        harmony.Patch(AccessTools.Method(typeof(BattleOneSidePlayManager), "Action"), prefix: skip);
        harmony.Patch(AccessTools.Method(typeof(BattleUnitInfoManagerUI), nameof(BattleUnitInfoManagerUI.UpdateCharacterProfileAll)), prefix: skip);
        if (_managed)
        {
            _summary = Uninitialized<BattleManagerUI>();
            _summary.ui_unitListInfoSummary = Uninitialized<BattleUnitInfoManagerUI>();
            harmony.Patch(AccessTools.PropertyGetter(typeof(SingletonBehavior<BattleManagerUI>), "Instance"),
                prefix: new HarmonyMethod(typeof(AntieOverflowScenario), nameof(ManagedSummary)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(BattleUnitView), nameof(BattleUnitView.WorldPosition)),
                prefix: new HarmonyMethod(typeof(AntieOverflowScenario), nameof(ManagedPosition)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(RandomUtil), "valueForProb"),
                prefix: new HarmonyMethod(typeof(AntieOverflowScenario), nameof(ManagedRandom)));
        }
        else InitializeUnitySummary();

        LoadCardsAndCheckBindings(Path.Combine(repository, "SteriaBuild", "SteriaModFolder"), product);
        TestTeamAndCups();
        TestFormConversion();
        TestCombat();
        Print("ANTIE_OVERFLOW_FIXTURE_PASS " + _checks);
    }

    private static void TestFormConversion()
    {
        BattleObjectManager.instance.Init_only();
        var antie = Unit(Faction.Enemy, 100, 100, 100, 100);
        var form = new PassiveAbility_9011001();
        form.Init(antie);
        antie.passiveDetail.PassiveList.Add(form);
        var cup = new PassiveAbility_9011003();
        cup.Init(antie);
        antie.passiveDetail.PassiveList.Add(cup);
        cup.OnWaveStart();

        antie.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 3 });
        antie.bufListDetail.AddBuf(new BattleUnitBuf_Dream { stack = 4 });
        antie.bufListDetail.AddBuf(new BattleUnitBuf_Tide { stack = 5 });
        antie.bufListDetail.AddBuf(new BattleUnitBuf_StrongFlow { stack = 2 });
        Invoke(form, "ActivateForm", PassiveAbility_9011001.AntieForm.Tide, false);
        Check(Stacks<BattleUnitBuf_OverflowFlow>(antie) == 3 && Stacks<BattleUnitBuf_OverflowDream>(antie) == 4 &&
            Stacks<BattleUnitBuf_OverflowTide>(antie) == 0 && Stacks<BattleUnitBuf_Tide>(antie) == 17 &&
            Stacks<BattleUnitBuf_StrongFlow>(antie) == 2 && antie.personalEgoDetail.GetHand().Count == 3,
            "native form entry records actual other-resource conversion, keeps special resources and preserves all cups");
    }

    private static bool Skip() => false;
    private static bool ManagedSummary(ref BattleManagerUI __result) { __result = _summary; return false; }
    private static bool ManagedPosition(ref Vector3 __result) { __result = Vector3.zero; return false; }
    private static bool ManagedRandom(ref float __result) { __result = 0.5f; return false; }

    private static T Uninitialized<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitializeUnitySummary()
    {
        var ui = new GameObject("Fixture battle summary").AddComponent<BattleManagerUI>();
        ui.enabled = false;
        ui.ui_unitListInfoSummary = new GameObject("Fixture unit summary").AddComponent<BattleUnitInfoManagerUI>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BattleDiceCardUI CreateUnityCardUi()
    {
        var ui = new GameObject("Fixture E.G.O. card").AddComponent<BattleDiceCardUI>();
        Set(ui, "ob_EgoCoolTime", new GameObject("Fixture cooldown gauge"));
        return ui;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GaugeHidden(BattleDiceCardUI ui) => !((GameObject)Read(ui, "ob_EgoCoolTime")).activeSelf;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BattleUnitView CreateUnityView(int id)
    {
        var view = new GameObject("Unit " + id).AddComponent<BattleUnitView>();
        view.enabled = false;
        return view;
    }

    private static void LoadCardsAndCheckBindings(string folder, Assembly product)
    {
        var manager = Singleton<AssemblyManager>.Instance;
        // The suite registers the same script names without invoking BaseMod's global initializer.
        var scripts = Read(manager, "_diceCardSelfAbilityDict");
        foreach (string name in new[] { "AntieFlowCup", "AntieDreamCup", "AntieTideCup" })
            Invoke(scripts, "Add", name, product.GetType("DiceCardSelfAbility_" + name, true));
        var cards = XDocument.Load(Path.Combine(folder, "Data", "CardInfo.xml"));
        var serializer = new XmlSerializer(typeof(DiceCardXmlInfo), new XmlRootAttribute("Card"));
        var cupXml = cards.Root.Elements("Card").Where(card => CupIds.Contains((int)card.Attribute("ID"))).Select(card =>
        {
            using (var reader = card.CreateReader()) return (DiceCardXmlInfo)serializer.Deserialize(reader);
        }).ToList();
        foreach (var card in cupXml)
        {
            card.workshopID = Package;
            Check(card.IsPersonal() && card.IsEgo() && card.Rarity == Rarity.Unique && card.Spec.Ranged == CardRange.Instance &&
                card.Spec.Cost == 0 && !card.IsExhaustOnUse() && card.DiceBehaviourList.Count == 0,
                $"Cup {card.id.id} deserializes as a reusable 0-Light Unique E.G.O. equipment page");
            Check(File.Exists(Path.Combine(folder, "Resource", "CombatPageArtwork", card.Artwork)), $"Cup {card.id.id} artwork exists");
        }

        ItemXmlDataList.instance.InitCardInfo(cupXml);
        string localize = Path.Combine(folder, "Localize", "cn");
        var effects = XDocument.Load(Path.Combine(localize, "EffectTexts", "EffectTexts.xml"));
        var effectTexts = effects.Descendants("BattleEffectText").ToDictionary(node => (string)node.Attribute("ID"), node =>
            new LOR_XML.BattleEffectText { Name = (string)node.Element("Name"), Desc = (string)node.Element("Desc") });
        Singleton<BattleEffectTextsXmlList>.Instance.Init(effectTexts);
        var abilities = XDocument.Load(Path.Combine(localize, "BattleCardAbilities", "BattleCardAbilities.xml"));
        var abilityTexts = abilities.Descendants("BattleCardAbility").Where(node => cupXml.Any(card => card.Script == (string)node.Attribute("ID")))
            .ToDictionary(node => (string)node.Attribute("ID"), node => new LOR_XML.BattleCardAbilityDesc { desc = node.Elements("Desc").Select(desc => (string)desc).ToList() });
        Singleton<BattleCardAbilityDescXmlList>.Instance.Init(abilityTexts);
        var names = XDocument.Load(Path.Combine(localize, "BattlesCards", "BattleCardDesc_AntieOverflow.xml"));
        var nameTexts = names.Descendants("BattleCardDesc").ToDictionary(
            node => new LorId(Package, (int)node.Attribute("ID")), node => new LOR_XML.BattleCardDesc
            {
                cardName = (string)node.Element("LocalizedName"),
                ability = (string)node.Element("Ability"),
                behaviourDescList = new List<LOR_XML.CardBehaviourDesc>()
            });
        Singleton<BattleCardDescXmlList>.Instance.Init(nameTexts);
        Check(cupXml.All(card => abilityTexts.ContainsKey(card.Script) &&
            Singleton<BattleCardDescXmlList>.Instance.GetCardName(card.id) == card.workshopName &&
            card.Keywords.All(key => !string.IsNullOrEmpty(Singleton<BattleEffectTextsXmlList>.Instance.GetEffectTextName(key)))),
            "Chinese cup scripts and all resource tooltip IDs resolve from the package data");
        foreach (var binding in new[] { ("Enemy", 13), ("Librarian", 99000011) })
        {
            var core = XDocument.Load(Path.Combine(folder, "Data", "EquipPage_" + binding.Item1 + ".xml"))
                .Root.Elements("Book").Single(book => (int)book.Attribute("ID") == binding.Item2);
            var ids = core.Element("EquipEffect").Elements("Passive").Select(node => (int)node).ToArray();
            Check(ids.Contains(9011001) && ids.Contains(9011002) && ids.Count(id => id == 9011003) == 1,
                binding.Item1 + " Antie keeps both existing passives and gains one cup passive");
        }
    }

    private static void TestTeamAndCups()
    {
        BattleObjectManager.instance.Init_only();
        Set(Singleton<StageController>.Instance, "_phase", StageController.StagePhase.ExecuteParrying);
        var antie = Unit(Faction.Player, 100, 100, 25, 10);
        var ally = Unit(Faction.Player, 50, 100, 25, 30);
        var fullAlly = Unit(Faction.Player, 100, 100, 100, 100);
        var enemy = Unit(Faction.Enemy, 99, 100, 90, 90);
        var antiePassive = new PassiveAbility_9011003();
        antiePassive.Init(antie);
        antie.passiveDetail.PassiveList.Add(antiePassive);
        var enemyPassive = new PassiveAbility_9011003();
        enemyPassive.Init(enemy);
        enemy.passiveDetail.PassiveList.Add(enemyPassive);
        antiePassive.OnUnitCreated();
        antiePassive.OnWaveStart();
        antiePassive.OnRoundStartAfter();
        Check(antie.personalEgoDetail.GetHand().Count == 3, "repeated creation, wave and scene callbacks register each Cup exactly once");
        var flowCup = Cup(antie, 0);
        var dreamCup = Cup(antie, 1);
        var tideCup = Cup(antie, 2);
        Check(flowCup.MaxCooltimeValue == 0 && flowCup.CanAddedEgoCard(), "personal E.G.O. copy preserves usable zero cooldown");
        Check(flowCup.XmlData.Keywords.Count == 2 && dreamCup.XmlData.Keywords.Count == 3 && tideCup.XmlData.Keywords.Count == 2,
            "personal E.G.O. copies retain all tooltip IDs");
        var cardUi = _managed ? Uninitialized<BattleDiceCardUI>() : CreateUnityCardUi();
        Set(cardUi, "_cardModel", flowCup);
        Set(cardUi, "isEgoCoolTimeLock", true);
        Set(cardUi, "graphics_EgoLockFrames", Array.Empty<Graphic>());
        Set(cardUi, "hsv_EgoLockFrames", Array.Empty<RefineHsv>());
        Set(cardUi, "hsv_behaviourIcons", new List<RefineHsv>());
        cardUi.SetEgoCoolTimeGauge();
        Check(!(bool)Read(cardUi, "isEgoCoolTimeLock") && (_managed || GaugeHidden(cardUi)),
            "Cup uses the resource check without the vanilla zero-cooldown UI lock");

        ally.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 35 });
        var paid = Attack(ally, enemy, 5);
        Check(FlowPlanning.TryEnhance(ally, paid.card, out _, true), "ordinary Flow reservation succeeds");
        Check(Stacks<BattleUnitBuf_OverflowFlow>(antie) == 0, "reservation does not create overflow");
        FlowPlanning.Commit();
        FlowPlanning.Commit();
        Check(Stacks<BattleUnitBuf_OverflowFlow>(antie) == 1 && Stacks<BattleUnitBuf_OverflowFlow>(enemy) == 0 &&
            Stacks<BattleUnitBuf_OverflowFlow>(ally) == 0, "only the same-side active passive records committed spend exactly once");

        HarmonyHelpers.NotifyPassivesOnFlowConsumed(ally, 28);
        SivierCardHelper.AddDreamToUnit(ally, 30);
        SivierCardHelper.ConsumeDream(ally, 19);
        ally.bufListDetail.AddBuf(new BattleUnitBuf_Tide { stack = 20 });
        ChristashaAbilityHelper.ConsumeTideStacks(ally, 9);
        Check(!antie.CheckCardAvailable(flowCup) && !antie.CheckCardAvailable(dreamCup) && !antie.CheckCardAvailable(tideCup),
            "all three cups reject one layer below their thresholds");
        antie.cardSlotDetail.AddCard(flowCup, antie, -1);
        Check(antie.personalEgoDetail.GetHand().Contains(flowCup) && Stacks<BattleUnitBuf_OverflowFlow>(antie) == 29,
            "direct submission below threshold leaves the equipped page and pool intact");

        HarmonyHelpers.NotifyPassivesOnFlowConsumed(antie, 1);
        SivierCardHelper.ConsumeDream(ally, 1);
        ChristashaAbilityHelper.ConsumeTideStacks(ally, 1);
        Check(antie.CheckCardAvailable(flowCup) && antie.CheckCardAvailable(dreamCup) && antie.CheckCardAvailable(tideCup),
            "all thresholds open at exactly 30/20/10");
        HarmonyHelpers.NotifyPassivesOnFlowConsumed(ally, 1);
        SivierCardHelper.ConsumeDream(ally, 5);
        ChristashaAbilityHelper.ConsumeTideStacks(ally, 2);

        antie.cardSlotDetail.AddCard(flowCup, antie, -1);
        antie.cardSlotDetail.AddCard(dreamCup, antie, -1);
        antie.cardSlotDetail.AddCard(tideCup, antie, -1);
        Check(Stacks<BattleUnitBuf_OverflowFlow>(antie) + Stacks<BattleUnitBuf_OverflowDream>(antie) + Stacks<BattleUnitBuf_OverflowTide>(antie) == 0,
            "three native equipment uses clear their own pools without crediting the cup costs");
        Check(antie.breakDetail.breakGauge == 27 && ally.breakDetail.breakGauge == 44 && fullAlly.breakDetail.breakGauge == 100,
            $"31 Stagger recovery is distributed 17/14/0 by missing Stagger ratio, gauges are {antie.breakDetail.breakGauge}/{ally.breakDetail.breakGauge}/{fullAlly.breakDetail.breakGauge}");
        Check(Stacks<BattleUnitBuf_WishShield>(antie) == 6 && Stacks<BattleUnitBuf_WishShield>(ally) == 13 && Stacks<BattleUnitBuf_WishShield>(fullAlly) == 6,
            "25 Wish Shield is distributed 6/13/6 by inverse maximum HP");
        Check(antie.hp == 32 && ally.hp == 30 && fullAlly.hp == 100, "12 HP recovery is distributed 7/5/0 by missing HP ratio");
        Check(new[] { antie, ally, fullAlly }.All(unit => Stacks<BattleUnitBuf_StrongFlow>(unit) == 3 &&
            Stacks<BattleUnitBuf_DeepDream>(unit) == 2 && Stacks<BattleUnitBuf_HolyTide>(unit) == 1),
            "each living ally gains the full tens quotient of each special resource");
        Check(antie.personalEgoDetail.GetHand().Count == 0 && antie.personalEgoDetail.GetCardAll().Count == 3 && antie.PlayPoint == 6,
            "cups are set aside without exhausting them or spending Light");
        antie.personalEgoDetail.ReturnCardsInUse();
        Check(antie.personalEgoDetail.GetHand().Count == 3 && !antie.CheckCardAvailable(flowCup), "native scene-end return restores each page and rechecks the pool");
        Check(DirectiveDreamHelper.GetFlowStacks(ally) == FlowPlanning.Available(ally) + 3 &&
            SivierCardHelper.GetDreamCount(ally) == SivierCardHelper.GetSpendableDreamCount(ally) + 2 &&
            ChristashaAbilityHelper.GetTotalTideStacks(ally) == ChristashaAbilityHelper.GetTideStacks(ally) + 1,
            "ownership includes specials and ordinary affordability excludes them");

        FlowPlanning.BeginRound();
        Check(!FlowPlanning.TryEnhance(fullAlly, Attack(fullAlly, enemy, 5).card, out _, true), "only Strong Flow cannot fund ordinary enhancement");
        SivierCardHelper.ConsumeDream(fullAlly, 1);
        Check(Stacks<BattleUnitBuf_DeepDream>(fullAlly) == 2 && ChristashaAbilityHelper.ConsumeTideStacks(fullAlly, 1) == 0 &&
            Stacks<BattleUnitBuf_HolyTide>(fullAlly) == 1, "ordinary Dream and Tide costs cannot take special-only holdings");

        int flowBeforeProxy = FlowPlanning.Available(ally);
        ally.bufListDetail.AddBuf(new BattleUnitBuf_StephanieProxy { stack = 1 });
        var mass = Attack(ally, enemy, 5, range: CardRange.FarAreaEach, cardId: 9002009);
        Check(FlowPlanning.TryEnhance(ally, mass.card, out _, true), "Stephanie proxy can still assign a mass enhancement");
        FlowPlanning.Commit();
        SivierCardHelper.ConsumeDream(ally, 1);
        ChristashaAbilityHelper.ConsumeTideStacks(ally, 1);
        Check(FlowPlanning.Available(ally) == flowBeforeProxy && Stacks<BattleUnitBuf_OverflowFlow>(antie) == 0 &&
            Stacks<BattleUnitBuf_OverflowDream>(antie) == 0 && Stacks<BattleUnitBuf_OverflowTide>(antie) == 0,
            "Stephanie proxy effects without a deduction create no overflow, including mass planning");

        enemyPassive.OnWaveStart();
        enemy.bufListDetail.AddBuf(new BattleUnitBuf_Tide { stack = 10 });
        ChristashaAbilityHelper.ConsumeTideStacks(enemy, 10);
        enemyPassive.OnRoundStartAfter();
        Check(enemy.hp == 99 && Stacks<BattleUnitBuf_HolyTide>(enemy) == 1 && Stacks<BattleUnitBuf_OverflowTide>(enemy) == 0 &&
            enemy.personalEgoDetail.GetHand().Count == 2 && Stacks<BattleUnitBuf_OverflowTide>(antie) == 0,
            "guest Antie uses a ready cup through the normal equip entry and only spends her own pool");

        int[] split = (int[])typeof(PassiveAbility_9011003).Assembly.GetType("Steria.AntieOverflow")
            .GetMethod("Allocate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { 10, new[] { .75, .5, .5 }, new[] { 1, 10, 10 } });
        Check(split.SequenceEqual(new[] { 1, 5, 4 }), "recovery cap redistributes capped share and ties differ by at most one");
    }

    private static void TestCombat()
    {
        BattleObjectManager.instance.Init_only();
        var a = Unit(Faction.Enemy, 100, 100, 100, 100);
        var b = Unit(Faction.Player, 100, 100, 100, 100);
        var passive = new PassiveAbility_9011003();
        passive.Init(a);
        a.passiveDetail.PassiveList.Add(passive);
        a.bufListDetail.AddBuf(new BattleUnitBuf_StrongFlow { stack = 2 });
        var left = Attack(a, b, 5);
        var right = Attack(b, a, 7);
        var tracker = new RollCounter();
        left.currentBehavior.AddAbility(tracker);
        var decision = Clash(left, right);
        Check(decision == BattleParryingManager.ParryingDecisionResult.WinEnemy && left.currentBehavior.DiceResultValue == 9 && right.currentBehavior.DiceResultValue == 7 && tracker.Rolls == 2 && tracker.BeforeRolls == 1 &&
            Stacks<BattleUnitBuf_StrongFlow>(a) == 1, "native clash uses the same losing die, reruns RollDice events and resolves to the new winner");

        left = Attack(a, b, 5);
        right = Attack(b, a, 12);
        tracker = new RollCounter();
        left.currentBehavior.AddAbility(tracker);
        decision = Clash(left, right);
        Check(decision == BattleParryingManager.ParryingDecisionResult.WinLibrarian && tracker.Rolls == 2 && Stacks<BattleUnitBuf_StrongFlow>(a) == 0,
            "an unsuccessful reroll spends exactly one Strong Flow and remains a loss");
        tracker.Rolls = 0;
        decision = Clash(left, right);
        Check(decision == BattleParryingManager.ParryingDecisionResult.WinLibrarian && tracker.Rolls == 1 && left.currentBehavior.DiceResultValue == 5,
            "without Strong Flow the same die settles normally with no leftover power");
        Check(Stacks<BattleUnitBuf_OverflowFlow>(a) == 2, "each genuinely spent Strong Flow contributes once to same-side overflow");

        b.bufListDetail.AddBuf(new BattleUnitBuf_DeepDream { stack = 3 });
        left = Attack(a, b, 10);
        right = Attack(b, a, 4, BehaviourType.Def);
        Clash(left, right);
        Check(left.currentBehavior.DiceResultValue == 6 && left.currentBehavior.DamageAdder == -4 && Stacks<BattleUnitBuf_DeepDream>(b) == 2,
            "defensive clash applies the penalty to the opposing die only");
        Clash(left, right);
        Check(left.currentBehavior.DiceResultValue == 6 && Stacks<BattleUnitBuf_DeepDream>(b) == 2, "replaying the same enemy die does not consume another Deep Dream");
        left = Attack(a, b, 15);
        a.battleCardResultLog = new BattleCardTotalResult(left);
        b.battleCardResultLog = new BattleCardTotalResult(null);
        var oneSide = new BattleOneSidePlayManager { victim = b, attacker = a };
        Set(oneSide, "_playingCard", left);
        Set(oneSide, "_attackerCardBehavioursResult", a.battleCardResultLog);
        Set(oneSide, "_victimCardBehavioursResult", b.battleCardResultLog);
        Invoke(oneSide, "Decision");
        left.currentBehavior.GiveDamage(b);
        Check(left.currentBehavior.DiceResultValue == 11 && left.currentBehavior.DiceResultDamage == 7 && Stacks<BattleUnitBuf_DeepDream>(b) == 1,
            "one-sided decision with no defender die reduces real roll and HP damage by four each");

        a.bufListDetail.AddBuf(new BattleUnitBuf_HolyTide { stack = 2 });
        var costly = Attack(a, b, 8, cost: 4, range: CardRange.FarAreaEach);
        a.allyCardDetail.UseCard(costly.card);
        a.OnUseCard(costly);
        a.OnUseCard(costly);
        Roll(costly.currentBehavior);
        var extraDie = Attack(a, b, 9).currentBehavior;
        extraDie.card = costly;
        Roll(extraDie);
        Check(a.PlayPoint == 2 && costly.currentBehavior.DiceResultValue == 12 && extraDie.DiceResultValue == 13 &&
            Stacks<BattleUnitBuf_HolyTide>(a) == 1, "one real four-Light mass page use buffs all its dice, including a newly added die, and spends once");
        var anotherAction = Attack(a, b, 8, cost: 3);
        a.cardSlotDetail.SetPlayPoint(6);
        a.allyCardDetail.UseCard(anotherAction.card);
        a.OnUseCard(anotherAction);
        Roll(anotherAction.currentBehavior);
        Check(a.PlayPoint == 3 && anotherAction.currentBehavior.DiceResultValue == 8 && Stacks<BattleUnitBuf_HolyTide>(a) == 1,
            "a real three-Light page use neither spends Holy Tide nor inherits power");
        var waived = Attack(a, b, 8, cost: 4);
        waived.card.SetCostToZero(true);
        a.allyCardDetail.UseCard(waived.card);
        a.OnUseCard(waived);
        Roll(waived.currentBehavior);
        Check(waived.currentBehavior.DiceResultValue == 8 && Stacks<BattleUnitBuf_HolyTide>(a) == 1,
            "a printed four-Light page made free does not spend Holy Tide");
    }

    private static BattleUnitModel Unit(Faction faction, int maxHp, int maxBreak, int hp, int bp)
    {
        var unit = new BattleUnitModel(++_nextUnit) { faction = faction, index = _nextUnit };
        var book = new BookModel(new BookXmlInfo { EquipEffect = new BookEquipEffect { Hp = maxHp, Break = maxBreak, MaxPlayPoint = 6 } });
        unit.equipment.book = book;
        var data = (UnitDataModel)FormatterServices.GetUninitializedObject(typeof(UnitDataModel));
        Set(data, "_bookItem", book);
        Set(data, "_history", new UnitHistoryModel(data));
        data.giftInventory = new GiftInventory(data);
        var battle = (UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        battle.unitData = data;
        battle.emotionDetail = new BattleUnitEmotionDetail(unit);
        battle.historyInStage = new UnitBattleStageHistory { stageInfo = new StageClassInfo() };
        battle.historyInWave = new UnitBattleWaveHistory { stageInfo = battle.historyInStage.stageInfo };
        Set(unit, "_unitData", battle);
        unit.view = _managed ? Uninitialized<BattleUnitView>() : CreateUnityView(_nextUnit);
        unit.SetCurrentOrder(-1);
        unit.SetHp(hp);
        unit.breakDetail.breakLife = 1;
        unit.breakDetail.breakGauge = bp;
        unit.battleCardResultLog = new BattleCardTotalResult(null);
        unit.allyCardDetail.Init(new List<DiceCardXmlInfo>());
        unit.personalEgoDetail.Init();
        unit.cardSlotDetail.SetPlayPoint(6);
        BattleObjectManager.instance.GetList().Add(unit);
        return unit;
    }

    private static BattleDiceCardModel Cup(BattleUnitModel unit, int index) =>
        unit.personalEgoDetail.GetHand().Single(card => card.GetID() == new LorId(Package, CupIds[index]));

    private static BattlePlayingCardDataInUnitModel Attack(BattleUnitModel owner, BattleUnitModel target, int roll,
        BehaviourType type = BehaviourType.Atk, int cost = 0, CardRange range = CardRange.Near, int cardId = 84999)
    {
        var xml = new DiceCardXmlInfo(new LorId(Package, cardId));
        xml.Spec.Cost = cost;
        xml.Spec.Ranged = range;
        var page = new BattlePlayingCardDataInUnitModel { owner = owner, target = target, card = BattleDiceCardModel.CreatePlayingCard(xml) };
        var die = new DiceBehaviour { Min = roll, Dice = roll, Type = type, Detail = type == BehaviourType.Def ? BehaviourDetail.Guard : BehaviourDetail.Slash };
        page.currentBehavior = new BattleDiceBehavior { card = page, behaviourInCard = die };
        page.card.GetBehaviourList().Add(die);
        return page;
    }

    private static BattleParryingManager.ParryingDecisionResult Clash(BattlePlayingCardDataInUnitModel enemy, BattlePlayingCardDataInUnitModel player)
    {
        var manager = new BattleParryingManager();
        var left = (BattleParryingManager.ParryingTeam)Read(manager, "_teamEnemy");
        var right = (BattleParryingManager.ParryingTeam)Read(manager, "_teamLibrarian");
        left.Init(enemy.owner, enemy);
        right.Init(player.owner, player);
        left.SetOpponent(right);
        right.SetOpponent(left);
        Set(manager, "_enemyCardBehavioursResult", new BattleCardTotalResult(enemy));
        Set(manager, "_librarianCardBehavioursResult", new BattleCardTotalResult(player));
        Invoke(manager, "Decision");
        return (BattleParryingManager.ParryingDecisionResult)Read(manager, "_decisionResult");
    }

    private static void Roll(BattleDiceBehavior die)
    {
        die.BeforeRollDice(null);
        die.RollDice();
        die.UpdateDiceFinalValue();
    }

    private static int Stacks<T>(BattleUnitModel unit) where T : BattleUnitBuf =>
        unit.bufListDetail.GetActivatedBufList().OfType<T>().Where(buf => !buf.IsDestroyed()).Sum(buf => buf.stack);

    private static object Invoke(object target, string method, params object[] args) => AccessTools.Method(target.GetType(), method).Invoke(target, args);
    private static object Read(object target, string field) => AccessTools.Field(target.GetType(), field).GetValue(target);
    private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Print($"PASS {++_checks}: {label}");
    }

    private static void Print(string message)
    {
        if (_managed) Console.WriteLine(message);
        else Debug.Log(message);
    }

    private sealed class RollCounter : DiceCardAbilityBase
    {
        internal int Rolls;
        internal int BeforeRolls;
        public override void OnRollDice() => Rolls++;
        public override void BeforeRollDice() => BeforeRolls++;
    }
}
