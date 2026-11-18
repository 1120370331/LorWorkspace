using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using AceTaffy;
using HarmonyLib;
using LOR_DiceSystem;

// Focused executable checks against the real referenced game assembly.
internal static class MechanicsProbe
{
    private static int _checks;
    private static readonly FieldInfo HpField = typeof(BattleUnitModel).GetField("<hp>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static BattleDiceBehavior _countedDie;
    private static int _rolls;
    private static int _drawnCards;

    public static bool FakeDrawCards(int count)
    {
        _drawnCards += count;
        return false;
    }

    public static void CountRoll(BattleDiceBehavior __instance)
    {
        if (Object.ReferenceEquals(__instance, _countedDie)) _rolls++;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool FakeWin(BattleUnitModel __instance, BattleDiceBehavior behavior)
    {
        __instance.passiveDetail.OnWinParrying(behavior);
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool FakeSucceed(BattleUnitModel __instance, BattleDiceBehavior behavior)
    {
        __instance.passiveDetail.OnSucceedAttack(behavior);
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool SkipRollCallback() { return false; }

    [HarmonyPriority(Priority.Last)]
    public static bool FakeUnbroken(ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool DeterministicDice(int diceMin, ref int __result)
    {
        __result = diceMin;
        return false;
    }

    public static bool StubLocalizedName(BattleUnitBuf __instance, ref string __result)
    {
        __result = "烟气 " + __instance.stack;
        return false;
    }

    public static bool StubNativeSmokeDescription(ref string __result)
    {
        __result = "原版烟气描述";
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool FakeLoseHp(BattleUnitModel __instance, int value, ref int __result)
    {
        int paid = Math.Min(value, Math.Max(0, (int)__instance.hp - 1));
        HpField.SetValue(__instance, __instance.hp - paid);
        __result = paid;
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    public static bool FakeTakeDamage(BattleUnitModel __instance, int v, ref int __result)
    {
        int paid = Math.Min(v, Math.Max(0, (int)__instance.hp - 1));
        HpField.SetValue(__instance, __instance.hp - paid);
        __result = paid;
        return false;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        _checks++;
        Console.WriteLine("[CASE] " + name + " result=PASS");
    }

    private static BattleUnitModel Unit(int ordinary, int pressure)
    {
        var unit = (BattleUnitModel)FormatterServices.GetUninitializedObject(typeof(BattleUnitModel));
        unit.bufListDetail = new BattleUnitBufListDetail(unit);
        unit.passiveDetail = new BattleUnitPassiveDetail(unit);
        unit.allyCardDetail = new BattleAllyCardDetail(unit);
        var data = (UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        data.emotionDetail = new BattleUnitEmotionDetail(unit);
        typeof(BattleUnitModel).GetField("_unitData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(unit, data);
        var smoke = new BattleUnitBuf_smoke { stack = ordinary + pressure };
        smoke.Init(unit);
        unit.bufListDetail.GetActivatedBufList().Add(smoke);
        if (pressure > 0)
        {
            var buf = new BattleUnitBuf_AceTaffyPressure { stack = pressure };
            buf.Init(unit);
            unit.bufListDetail.GetActivatedBufList().Add(buf);
        }
        return unit;
    }

    private static BattleUnitBuf_smoke SmokeBuf(BattleUnitModel unit)
    {
        return (BattleUnitBuf_smoke)unit.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke);
    }

    private static BattleUnitBuf_AceTaffyPressure PressureBuf(BattleUnitModel unit)
    {
        return unit.bufListDetail.GetActivatedBufList().Find(
            buf => buf is BattleUnitBuf_AceTaffyPressure) as BattleUnitBuf_AceTaffyPressure;
    }

    private static void RoundEnd(int ordinary, int pressure)
    {
        var unit = Unit(ordinary, pressure);
        TaffyMechanics.BeforeSmokeRoundEnd(SmokeBuf(unit), unit);
        PressureBuf(unit).OnRoundEnd();
        Check(TaffyMechanics.Pressure(unit) == pressure - 1 &&
              TaffyMechanics.Smoke(unit) == Math.Max(0, ordinary - 1), "smoke-first decay");

        unit = Unit(ordinary, pressure);
        PressureBuf(unit).OnRoundEnd();
        if (TaffyMechanics.BeforeSmokeRoundEnd(SmokeBuf(unit), unit)) SmokeBuf(unit).OnRoundEnd();
        Check(TaffyMechanics.Pressure(unit) == pressure - 1 &&
              TaffyMechanics.Smoke(unit) == Math.Max(0, ordinary - 1), "pressure-first decay");
    }

    private static void Run()
    {
        foreach (int pressure in new[] { 0, 5, 6, 9, 10 })
        {
            var unit = Unit(3, pressure);
            Check(TaffyMechanics.Pressure(unit) == pressure && TaffyMechanics.Smoke(unit) == 3 &&
                  TaffyMechanics.EffectiveSmoke(unit) == pressure + 3, "aggregate counts");
            Check(unit.bufListDetail.GetKewordBufStack(KeywordBuf.Smoke) == pressure + 3,
                "native smoke keyword count includes pressure exactly once");
            int rate = 15; // Native rate before the postfix in this focused probe.
            TaffyMechanics.SmokeDamageRate(SmokeBuf(unit), unit, ref rate);
            Check(rate == (3 + pressure) * 5 + (pressure == 10 ? 20 : pressure >= 6 ? 10 : 0),
                "combined smoke incoming rate plus pressure tier");
            if (pressure == 10)
            {
                var die = new BattleDiceBehavior
                {
                    behaviourInCard = new DiceBehaviour { Type = BehaviourType.Standby, Detail = BehaviourDetail.Hit }
                };
                PressureBuf(unit).BeforeRollDice(die);
                Check(die.PowerAdder == 1, "attack counter gets overpressure power");
            }
        }

        var puffy = Unit(3, 6);
        puffy.passiveDetail.PassiveList.Add(new PassiveAbility_240026());
        int puffyRate = 0;
        TaffyMechanics.SmokeDamageRate(SmokeBuf(puffy), puffy, ref puffyRate);
        Check(puffyRate == 10, "Puffy Brume suppresses ordinary smoke penalty");
        var puffyDie = new BattleDiceBehavior { behaviourInCard = new DiceBehaviour { Type = BehaviourType.Atk } };
        SmokeBuf(puffy).BeforeGiveDamage(puffyDie);
        var statField = typeof(BattleDiceBehavior).GetField("_statBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(((DiceStatBonus)statField.GetValue(puffyDie)).dmgRate == 45,
            "Puffy Brume outgoing bonus sees nine combined smoke layers");
        var plainDie = new BattleDiceBehavior { behaviourInCard = new DiceBehaviour { Type = BehaviourType.Atk } };
        var plain = Unit(3, 6);
        SmokeBuf(plain).BeforeGiveDamage(plainDie);
        Check(((DiceStatBonus)statField.GetValue(plainDie)).dmgRate == 0,
            "pressure alone does not grant Puffy Brume outgoing bonus");
        var heart = Unit(3, 6);
        heart.passiveDetail.PassiveList.Add(new PassiveAbility_1883101());
        int heartRate = 0;
        TaffyMechanics.SmokeDamageRate(SmokeBuf(heart), heart, ref heartRate);
        Check(heartRate == 10, "SteamHeart suppresses ordinary smoke penalty");

        var spender = Unit(2, 4);
        bool result = false;
        Check(!TaffyMechanics.UseSmoke(SmokeBuf(spender), 3, spender, ref result) && result &&
              TaffyMechanics.Smoke(spender) == 0 && TaffyMechanics.Pressure(spender) == 3,
              "ordinary smoke spends first");
        Check(!TaffyMechanics.UseSmoke(SmokeBuf(spender), 6, spender, ref result) && !result &&
              TaffyMechanics.Pressure(spender) == 3, "insufficient spend is atomic");
        var onlyPressure = Unit(0, 5);
        Check(!TaffyMechanics.SpendOrdinarySmoke(onlyPressure, 3) &&
              TaffyMechanics.Pressure(onlyPressure) == 5, "ordinary-only spend rejects pressure-only state");
        var twoOrdinary = Unit(2, 5);
        Check(!TaffyMechanics.SpendOrdinarySmoke(twoOrdinary, 3) &&
              TaffyMechanics.Smoke(twoOrdinary) == 2 && TaffyMechanics.Pressure(twoOrdinary) == 5,
              "ordinary-only spend is atomic below three ordinary layers");
        var mixedOrdinary = Unit(3, 4);
        Check(TaffyMechanics.SpendOrdinarySmoke(mixedOrdinary, 3) &&
              TaffyMechanics.Smoke(mixedOrdinary) == 0 && TaffyMechanics.Pressure(mixedOrdinary) == 4,
              "ordinary-only spend leaves pressure untouched");
        var nativeOnly = Unit(3, 0);
        Check(TaffyMechanics.SpendOrdinarySmoke(nativeOnly, 3) &&
              TaffyMechanics.EffectiveSmoke(nativeOnly) == 0,
              "ordinary-only spend works on a unit without pressure");
        RoundEnd(0, 3);
        RoundEnd(4, 3);

        var harmed = Unit(0, 0);
        var hp = typeof(BattleUnitModel).GetField("<hp>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        hp.SetValue(harmed, 100f);
        float before;
        TaffyMechanics.BeforeHpLoss(harmed, out before);
        hp.SetValue(harmed, 97f);
        TaffyMechanics.AfterHpLoss(harmed, before);
        Check(TaffyMechanics.LostHpThisRound(harmed), "any actual HP loss is recorded");
        TaffyMechanics.ClearHpLoss(harmed);
        Check(!TaffyMechanics.LostHpThisRound(harmed), "HP marker resets");

        var dieToRepeat = new BattleDiceBehavior
        {
            behaviourInCard = new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit }
        };
        dieToRepeat.ApplyDiceStatBonus(new DiceStatBonus { power = 2 });
        var baseline = typeof(BattleDiceBehavior).GetField("_firstStatBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);
        baseline.SetValue(dieToRepeat, new DiceStatBonus { power = 2 });
        dieToRepeat.ApplyDiceStatBonus(new DiceStatBonus { power = 1 });
        var repeated = new BattlePlayingCardDataInUnitModel().CopyDiceBehaviour(dieToRepeat);
        Check(repeated.PowerAdder == 3 && ((DiceStatBonus)baseline.GetValue(repeated)).power == 2,
            "copied die retains clean pre-roll baseline");

        var xml = new DiceCardXmlInfo();
        xml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Def, Detail = BehaviourDetail.Guard });
        xml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        xml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        xml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Standby, Detail = BehaviourDetail.Hit });
        xml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        var model = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(model, xml);
        var page = new BattlePlayingCardDataInUnitModel { card = model };
        var third = new BattleDiceBehavior { card = page, behaviourInCard = xml.DiceBehaviourList[4] };
        third.SetIndex(4);
        Check(TaffyMechanics.IsThirdAttack(third), "third attack skips defense and counter");
        third.SetIndex(3);
        Check(!TaffyMechanics.IsThirdAttack(third), "counter is never third ordinary attack");
        var harmony = new Harmony("acetaffy.mechanics.probe");
        TaffyMechanics.Install(harmony);
        Check(Harmony.GetPatchInfo(AccessTools.PropertyGetter(typeof(BattleUnitBuf_smoke),
                  "bufActivatedText")) == null, "native Smoke description getter remains unpatched");
        Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(BattleUnitModel), "LoseHp")) != null,
            "Harmony HP loss hook installed");
        Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(BattleDiceBehavior), "OnEventDiceAbility")) != null,
            "Harmony dice ability scope installed");

        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "LoseHp"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeLoseHp)));
        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "TakeDamage"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeTakeDamage)));
        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "OnWinParrying"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeWin)));
        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "OnSucceedAttack",
                new[] { typeof(BattleDiceBehavior) }),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeSucceed)));
        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "OnRollDice"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(SkipRollCallback)));
        harmony.Patch(AccessTools.Method(typeof(BattleUnitModel), "IsBreakLifeZero"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeUnbroken)));
        harmony.Patch(AccessTools.Method(typeof(BattleDiceBehavior), "RollDice"),
            postfix: new HarmonyMethod(typeof(MechanicsProbe), nameof(CountRoll)));
        harmony.Patch(AccessTools.Method(typeof(DiceStatCalculator), "MakeDiceResult"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(DeterministicDice)));
        harmony.Patch(AccessTools.Method(typeof(BattleAllyCardDetail), "DrawCards"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(FakeDrawCards)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(BattleUnitBuf), "bufActivatedNameWithStack"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(StubLocalizedName)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(BattleUnitBuf_smoke), "bufActivatedText"),
            prefix: new HarmonyMethod(typeof(MechanicsProbe), nameof(StubNativeSmokeDescription)));

        var originalCardOwner = Unit(0, 0);
        originalCardOwner.faction = Faction.Enemy;
        originalCardOwner.passiveDetail.PassiveList.Add(new PassiveAbility_1883101());
        var crystal = new PassiveAbility_1883103();
        crystal.Init(originalCardOwner);
        originalCardOwner.passiveDetail.PassiveList.Add(crystal);
        originalCardOwner.cardSlotDetail = new BattlePlayingCardSlotDetail(originalCardOwner);
        HpField.SetValue(originalCardOwner, 100f);
        var originalXml = new DiceCardXmlInfo();
        var originalModel = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(originalModel, originalXml);
        originalCardOwner.cardSlotDetail.keepCard.card = originalModel;
        var originalPage = new BattlePlayingCardDataInUnitModel { owner = originalCardOwner, card = originalModel };
        var ember = new DiceCardSelfAbility_scorchedgirl_ember { card = originalPage };
        originalPage.cardAbility = ember;
        originalPage.OnUseCard(); // Real native page effect invokes owner.LoseHp(5).
        Check(originalCardOwner.hp == 95f && TaffyMechanics.Pressure(originalCardOwner) == 1,
            "native Ember page self-harm grants SteamHeart pressure once: HP " + originalCardOwner.hp +
            ", pressure " + TaffyMechanics.Pressure(originalCardOwner));
        Check(originalCardOwner.bufListDetail.GetActivatedBufList().Exists(b => b is BattleUnitBuf_AceTaffyGuard) &&
              originalCardOwner.cardSlotDetail.keepCard.GetDiceBehaviorList().Count == 1,
              "native Ember page activates one Crystal Guard counter");

        var helperOwner = Unit(0, 0);
        helperOwner.faction = Faction.Enemy;
        helperOwner.passiveDetail.PassiveList.Add(new PassiveAbility_1883101());
        HpField.SetValue(helperOwner, 100f);
        TaffyMechanics.PayHp(helperOwner, 4);
        Check(helperOwner.hp == 96f && TaffyMechanics.Pressure(helperOwner) == 1,
            "PayHp uses the same once-per-payment event");

        var enemy = Unit(0, 0);
        var victim = Unit(0, 0);
        victim.faction = Faction.Enemy;
        HpField.SetValue(victim, 100f);
        victim.passiveDetail.PassiveList.Add(new PassiveAbility_1883103());
        var enemyPage = new BattlePlayingCardDataInUnitModel { owner = enemy };
        BattleUnitModel previous;
        TaffyMechanics.EnterCardScope(enemyPage, out previous);
        victim.TakeDamage(5, DamageType.Card_Ability, enemy, KeywordBuf.None);
        TaffyMechanics.ExitCardScope(previous);
        Check(TaffyMechanics.LostHpThisRound(victim) &&
              !victim.bufListDetail.GetActivatedBufList().Exists(b => b is BattleUnitBuf_AceTaffyGuard),
              "enemy page damage records HP loss without Crystal Guard");

        var shown = Unit(3, 4);
        var visibleSmoke = SmokeBuf(shown).GetUIData();
        Check(visibleSmoke.stack == 3 && visibleSmoke.bufActivatedNameWithStack == "烟气 3" &&
              visibleSmoke.bufActivatedText == "原版烟气描述" &&
              SmokeBuf(shown).paramInBufDesc == 35,
              "native BattleBufUIData shows ordinary count without changing native description");
        Check(SmokeBuf(shown).GetDamageIncreaseRate() == 35,
            "real native smoke damage rate reads combined stack");
        var shownHigh = Unit(3, 6);
        Check(SmokeBuf(shownHigh).GetDamageIncreaseRate() == 55,
            "real native smoke damage rate includes combined stack and high-pressure tier");

        CheckExhaustRecycle();

        CheckRerollDispatch();
        CheckOneSidedDispatch();
        Console.WriteLine("PASS " + _checks + " mechanics boundaries");
    }

    private static void CheckExhaustRecycle()
    {
        foreach (int ordinary in new[] { 0, 2 })
        {
            var owner = Unit(ordinary, 5);
            var page = new BattlePlayingCardDataInUnitModel { owner = owner };
            _drawnCards = 0;
            new DiceCardSelfAbility_AceTaffy_ExhaustRecycle { card = page }.OnUseCard();
            Check(_drawnCards == 0 && TaffyMechanics.Smoke(owner) == ordinary &&
                  TaffyMechanics.Pressure(owner) == 5,
                  "ExhaustRecycle rejects insufficient ordinary smoke without a reward");
        }
        var mixed = Unit(3, 4);
        _drawnCards = 0;
        new DiceCardSelfAbility_AceTaffy_ExhaustRecycle
            { card = new BattlePlayingCardDataInUnitModel { owner = mixed } }.OnUseCard();
        Check(_drawnCards == 2 && TaffyMechanics.Smoke(mixed) == 0 &&
              TaffyMechanics.Pressure(mixed) == 7,
              "ExhaustRecycle pays three ordinary smoke, draws two and gains three pressure");
        var native = Unit(3, 0);
        _drawnCards = 0;
        new DiceCardSelfAbility_AceTaffy_ExhaustRecycle
            { card = new BattlePlayingCardDataInUnitModel { owner = native } }.OnUseCard();
        Check(_drawnCards == 2 && TaffyMechanics.Smoke(native) == 0 &&
              TaffyMechanics.Pressure(native) == 3,
              "ExhaustRecycle works from ordinary smoke without pre-existing pressure");
    }

    private static void CheckOneSidedDispatch()
    {
        var owner = Unit(0, 6);
        var data = (UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        data.emotionDetail = new BattleUnitEmotionDetail(owner);
        typeof(BattleUnitModel).GetField("_unitData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, data);
        var compound = new PassiveAbility_1883102();
        compound.Init(owner);
        owner.passiveDetail.PassiveList.Add(compound);
        var xml = new DiceCardXmlInfo();
        for (int i = 0; i < 3; i++)
            xml.DiceBehaviourList.Add(new DiceBehaviour
                { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit, Min = 4, Dice = 4 });
        var model = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(model, xml);
        var page = new BattlePlayingCardDataInUnitModel { owner = owner, card = model };
        var third = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2], card = page };
        third.SetIndex(2);
        page.currentBehavior = third;
        _countedDie = third;
        _rolls = 0;
        third.RollDice();
        third.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(third.isBonusAttack && TaffyMechanics.Pressure(owner) == 3,
            "one-sided hit dispatch requests same-die bonus roll and costs three pressure");
        if (third.isBonusAttack) third.RollDice();
        third.isBonusAttack = false;
        Check(_rolls == 2, "one-sided hit resolves a second real RollDice on the same die");
        third.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(TaffyMechanics.Pressure(owner) == 3 && !third.isBonusAttack,
            "bonus hit cannot trigger an infinite repeat");

        TaffyMechanics.AddPressure(owner, 3);
        var secondPage = new BattlePlayingCardDataInUnitModel { owner = owner, card = model };
        var anotherThird = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2], card = secondPage };
        anotherThird.SetIndex(2);
        secondPage.currentBehavior = anotherThird;
        anotherThird.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!anotherThird.isBonusAttack && TaffyMechanics.Pressure(owner) == 6,
            "a second one-sided page shares the round latch");
        compound.OnRoundStart();
        anotherThird.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(anotherThird.isBonusAttack && TaffyMechanics.Pressure(owner) == 3,
            "next round restores one-sided repeat allowance");

        var counterPage = new BattlePlayingCardDataInUnitModel { owner = owner, card = model };
        var counter = new BattleDiceBehavior
        {
            behaviourInCard = new DiceBehaviour { Type = BehaviourType.Standby, Detail = BehaviourDetail.Hit },
            card = counterPage
        };
        counter.SetIndex(2);
        compound.OnRoundStart();
        TaffyMechanics.AddPressure(owner, 3);
        counter.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!counter.isBonusAttack, "counterattack hit is excluded");

        var shortXml = new DiceCardXmlInfo();
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Def, Detail = BehaviourDetail.Guard });
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        var shortModel = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(shortModel, shortXml);
        var shortPage = new BattlePlayingCardDataInUnitModel { owner = owner, card = shortModel };
        var shortDie = new BattleDiceBehavior { behaviourInCard = shortXml.DiceBehaviourList[2], card = shortPage };
        shortDie.SetIndex(2);
        shortDie.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!shortDie.isBonusAttack, "two attacks and a defense die do not repeat on hit");

        TaffyMechanics.SpendPressure(owner, TaffyMechanics.Pressure(owner) - 5);
        compound.OnRoundStart();
        var lowPage = new BattlePlayingCardDataInUnitModel { owner = owner, card = model };
        var lowThird = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2], card = lowPage };
        lowThird.SetIndex(2);
        lowThird.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!lowThird.isBonusAttack && TaffyMechanics.Pressure(owner) == 5,
            "insufficient pressure does not repeat one-sided hit");
        TaffyMechanics.AddPressure(owner, 1);
        var opposingDie = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[0] };
        typeof(BattleDiceBehavior).GetField("_targetDice", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(lowThird, opposingDie);
        lowThird.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!lowThird.isBonusAttack && TaffyMechanics.Pressure(owner) == 6,
            "a clash hit is never reclassified as a one-sided hit after pressure rises");
    }

    private static void CheckRerollDispatch()
    {
        var unit = Unit(0, 6);
        var data = (UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        data.emotionDetail = new BattleUnitEmotionDetail(unit);
        typeof(BattleUnitModel).GetField("_unitData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(unit, data);
        var compound = new PassiveAbility_1883102();
        compound.Init(unit);
        unit.passiveDetail.PassiveList.Add(compound);
        var xml = new DiceCardXmlInfo();
        for (int i = 0; i < 3; i++)
            xml.DiceBehaviourList.Add(new DiceBehaviour
                { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit, Min = 4, Dice = 4 });
        var model = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(model, xml);
        var page = new BattlePlayingCardDataInUnitModel { owner = unit, card = model };
        var third = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2] };
        third.SetIndex(2);
        third.card = page;
        var next = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[0] };
        next.SetIndex(0);
        page.currentBehavior = third;
        page.AddDice(next);

        _countedDie = third;
        _rolls = 0;
        third.RollDice();
        var opposed = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[0] };
        typeof(BattleDiceBehavior).GetField("_targetDice", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(third, opposed);
        third.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(!third.isBonusAttack && TaffyMechanics.Pressure(unit) == 6,
            "clash hit callback does not spend before clash-win callback");
        page.OnWinParryingAttack(); // Native dispatch: die ability -> owner -> passive.
        Check(third.isBonusAttack && TaffyMechanics.Pressure(unit) == 3 &&
              page.currentBehavior == third && page.cardBehaviorQueue.Count == 1,
              "third attack clash win requests same-die bonus roll, costs three pressure");
        third.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.SuccessAtk, Unit(0, 0));
        Check(TaffyMechanics.Pressure(unit) == 3,
            "clash hit and win callbacks cannot double-spend");
        // The managers keep currentBehavior when isBonusAttack; replay its actual
        // RollDice method, then advance through the native page queue.
        if (third.isBonusAttack) third.RollDice();
        third.isBonusAttack = false;
        Check(_rolls == 2 && page.currentBehavior == third,
            "same BattleDiceBehavior rolls twice, not a queued copy");
        page.NextDice();
        Check(page.currentBehavior == next, "native page queue advances after the bonus roll");

        TaffyMechanics.AddPressure(unit, 3);
        var otherPage = new BattlePlayingCardDataInUnitModel { owner = unit, card = model };
        var otherThird = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2], card = otherPage };
        otherThird.SetIndex(2);
        otherPage.currentBehavior = otherThird;
        otherPage.OnWinParryingAttack();
        Check(!otherThird.isBonusAttack && TaffyMechanics.Pressure(unit) == 6,
            "another page cannot repeat in the same round");

        compound.OnRoundStart();
        otherPage.OnWinParryingAttack();
        Check(otherThird.isBonusAttack && TaffyMechanics.Pressure(unit) == 3,
            "next round restores the one-repeat allowance");

        var shortXml = new DiceCardXmlInfo();
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Def, Detail = BehaviourDetail.Guard });
        shortXml.DiceBehaviourList.Add(new DiceBehaviour { Type = BehaviourType.Atk, Detail = BehaviourDetail.Hit });
        var shortModel = (BattleDiceCardModel)FormatterServices.GetUninitializedObject(typeof(BattleDiceCardModel));
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(shortModel, shortXml);
        var shortPage = new BattlePlayingCardDataInUnitModel { owner = unit, card = shortModel };
        var secondAttack = new BattleDiceBehavior { behaviourInCard = shortXml.DiceBehaviourList[2], card = shortPage };
        secondAttack.SetIndex(2);
        shortPage.currentBehavior = secondAttack;
        compound.OnRoundStart();
        TaffyMechanics.AddPressure(unit, 3);
        shortPage.OnWinParryingAttack();
        Check(!secondAttack.isBonusAttack, "two attacks with a defense die do not trigger");

        TaffyMechanics.SpendPressure(unit, TaffyMechanics.Pressure(unit) - 5);
        compound.OnRoundStart();
        var lowPage = new BattlePlayingCardDataInUnitModel { owner = unit, card = model };
        var lowThird = new BattleDiceBehavior { behaviourInCard = xml.DiceBehaviourList[2], card = lowPage };
        lowThird.SetIndex(2);
        lowPage.currentBehavior = lowThird;
        lowPage.OnWinParryingAttack();
        Check(!lowThird.isBonusAttack && TaffyMechanics.Pressure(unit) == 5,
            "five pressure cannot trigger the repeat");
    }

    private static void Main()
    {
        try { Run(); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
                Console.Error.WriteLine(ex.InnerException.GetType().FullName + ": " + ex.InnerException.Message);
            Environment.Exit(1);
        }
    }
}
