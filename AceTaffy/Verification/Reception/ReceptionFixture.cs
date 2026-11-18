using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using AceTaffy;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEditor;
using UnityEngine;

public static class ReceptionFixture
{
    static readonly string Ace=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../.."));
    static string Output=Environment.GetEnvironmentVariable("TAFFY_RECEPTION_OUTPUT");
    static List<BattleUnitModel> Units;
    static List<BookXmlInfo> BookXml;
    static BattleUnitModel Workshop;
    static Harmony Patches;
    static int Checks, SpawnCount, UnitId;
    static StageWaveModel Wave;
    static void Check(bool result,string text)
    { if(!result)throw new Exception(text);Checks++;Debug.Log("RECEPTION_CHECK "+text); }
    static void Set(object obj,string name,object value) { AccessTools.Field(obj.GetType(),name).SetValue(obj,value); }
    static T Load<T>(string name)
    { using(var file=File.OpenRead(Path.Combine(Ace,"AceTaffyMod",name)))return (T)new XmlSerializer(typeof(T)).Deserialize(file); }
    static void Patch(Type type,string name,string prefix)
    { Patches.Patch(AccessTools.Method(type,name),prefix:new HarmonyMethod(typeof(ReceptionFixture),prefix)); }

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Output);
            AccessTools.Field(typeof(AceTaffyMod),"<RootPath>k__BackingField").SetValue(null,Path.Combine(Ace,"AceTaffyMod"));
            Patches=new Harmony("lor.acetaffy.invention.units.fixture");
            Patch(typeof(BattleUnitBreakDetail),"RecoverBreak",nameof(RecoverWithoutUI));
            Patch(typeof(BattleUnitModel),"RecoverHP",nameof(RecoverHpWithoutUI));
            Patch(typeof(BattleObjectManager),"InitUI",nameof(Skip));
            Patch(typeof(UI.UICharacterRenderer),"SetCharacter",nameof(PreviewCharacter));
            new GameObject("Native portrait slots").AddComponent<UI.UICharacterRenderer>();
            Patch(typeof(BattleUnitBufListDetail),"CheckGift",nameof(Skip));
            Patch(typeof(BattleUnitBufListDetail),"CheckAchievements",nameof(Skip));
            // Native quickness OnRoundStart only creates its view effect.
            Patch(typeof(BattleUnitBuf_quickness),"OnRoundStart",nameof(Skip));
            Patch(typeof(BattleUnitModel),"OnWaveStart",nameof(SkipSummonWave));
            Patch(typeof(BattleUnitModel),"OnRoundStartOnlyUI",nameof(SummonSlots));
            Patch(typeof(BattleUnitModel),"RollSpeedDice",nameof(RollWithoutUI));
            Patches.Patch(AccessTools.Method(typeof(BattleUnitModel),"Die",new[]{typeof(BattleUnitModel),typeof(bool)}),
                prefix:new HarmonyMethod(typeof(ReceptionFixture),nameof(DeathWithoutPresentation)));
            Patches.Patch(AccessTools.Method(typeof(StageController),"AddNewUnit",new[]{typeof(Faction),typeof(LorId),typeof(int),typeof(int)}),
                prefix:new HarmonyMethod(typeof(ReceptionFixture),nameof(Spawn)));
            Wave=new StageWaveModel();
            Set(Wave,"_formation",new FormationModel(new FormationXmlInfo { positionList=Enumerable.Range(0,5)
                .Select(i=>new FormationPositionXmlData { vector=new XmlVector2 {x=10+i*8,y=i*8},eventList=new List<FormationPositionEventXmlData>() }).ToList() }));
            // The installed BaseMod preallocates 100 enemy slots. Reproduce its
            // real expansion: the old count>=6 guard left slot 5 at (-23,18).
            var summonPatch=typeof(BaseMod.BaseModInitializer).Assembly.GetType("SummonLiberation.Harmony_Patch",true);
            AccessTools.Method(summonPatch,"AddFormationPositionForEnemy").Invoke(null,new object[]{Wave.GetFormation(),100});
            Check(Wave.GetFormationPosition(5).Pos==new Vector3Int(-23,18,0),"real BaseMod expansion reproduces the upper-right sixth-slot defect");
            Patch(typeof(StageController),"GetCurrentWaveModel",nameof(CurrentWave));
            Singleton<AssemblyManager>.Instance.Initialize();
            foreach(var type in typeof(PassiveAbility_1883202).Assembly.GetTypes())
            {
                string prefix=type.IsSubclassOf(typeof(DiceCardSelfAbilityBase))?"DiceCardSelfAbility_":
                    type.IsSubclassOf(typeof(DiceCardAbilityBase))?"DiceCardAbility_":null;
                if(prefix==null || !type.Name.StartsWith(prefix))continue;
                var dict=AccessTools.Field(typeof(AssemblyManager),prefix=="DiceCardSelfAbility_"?"_diceCardSelfAbilityDict":"_diceCardAbilityDict")
                    .GetValue(Singleton<AssemblyManager>.Instance);
                AccessTools.Method(dict.GetType(),"Add").Invoke(dict,new object[]{type.Name.Substring(prefix.Length),type});
            }
            var cards=Load<DiceCardXmlRoot>("Data/CardInfo.xml");
            foreach(var xml in cards.cardXmlList)xml.workshopID="AceTaffy";
            ItemXmlDataList.instance.InitCardInfo(new List<DiceCardXmlInfo>());
            ItemXmlDataList.instance.AddCardInfoByMod("AceTaffy",cards.cardXmlList);
            BookXml=Load<BookXmlRoot>("Data/EquipPage_Enemy.xml").bookXmlList;
            foreach(var book in BookXml)book.workshopID="AceTaffy";
            Units=(List<BattleUnitModel>)AccessTools.Field(typeof(BattleObjectManager),"_unitList").GetValue(BattleObjectManager.instance);
            Units.Clear();
            Set(Singleton<StageController>.Instance,"_stageType",StageType.Invitation);
            var owner=Unit(Faction.Enemy,0);
            var inventor=Passive(owner,new PassiveAbility_1883202());
            var pattern=Passive(owner,new PassiveAbility_1883203());
            Passive(owner,new PassiveAbility_1883201());
            Passive(owner,new PassiveAbility_1883206());
            var target=Unit(Faction.Player,0);
            Unit(Faction.Player,1);Unit(Faction.Player,4);
            inventor.OnWaveStart();inventor.OnWaveStart();
            Check(SpawnCount==1 && TaffyInventions.Workshop(owner)==Workshop,"workshop summons exactly once");
            Check(owner.Book.GetSpeedDiceRule(owner).diceNum==6,"Taffy still starts with six speed dice");
            var ward=owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyManufactureGuard>().Single();
            ward.OnRoundEnd();
            Check(ward.stack==8 && ward.GetStatBonus().dmgAdder==-8 && !ward.IsDestroyed(),"eight protection persists across scene end");
            owner.breakDetail.breakGauge=100;
            inventor.OnRoundStart();
            Workshop.passiveDetail.OnTakeDamageByAttack(new BattleDiceBehavior(),0);
            Check(owner.breakDetail.breakGauge==110 && inventor.Progress==2,"workshop hit restores ten stagger and adds progress");
            inventor.AddProgress(3);
            Check(inventor.Progress==0 && inventor.ScheduledCount==1 && TaffyInventions.AliveUnits(owner).Count==0,
                "five progress schedules a next-scene invention without spawning immediately");
            Check(!owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>().Any(x=>!x.IsDestroyed()),
                "scheduled inventions grant no aura before arrival");
            inventor.OnEndBattle(null);
            Check(TaffyInventions.AliveUnits(owner).Count==0,"page end does not summon scheduled inventions");
            inventor.OnRoundStart();
            Check(inventor.ScheduledCount==0 && TaffyInventions.AliveUnits(owner).Count==1,"scheduled invention arrives at next scene start");
            Check(owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>().Single(x=>!x.IsDestroyed()).stack==1,
                "arrival adds one aura layer without double scene-start growth");
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Odd,InventionKind.Mechanism));
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Weird,InventionKind.Chain));
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Fragile,InventionKind.Defense));
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Super,InventionKind.Energy));
            Check(TaffyInventions.AliveUnits(owner).Count==4 &&
                  Units.Where(x=>x.faction==Faction.Enemy&&!x.IsDead()).Select(x=>x.index).Distinct().Count()==6,
                  "six enemy slots hold Taffy, workshop and four inventions");
            Check(Wave.GetFormationPosition(5).index==5 && Wave.GetFormation().PostionList.Count==100,
                  "native wave resolves sixth formation position");
            TaffyReceptionFormation.Ensure(Wave);
            Check(Wave.GetFormation().PostionList.Count==100,"formation setup preserves BaseMod extra slots and is idempotent");
            CheckFormationLayout(owner);
            foreach(var ally in BattleObjectManager.instance.GetAliveList(Faction.Enemy))
            {
                var reward=ally.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyNextSceneSupport>().Single();
                Check(reward.stack==3 && new[]{KeywordBuf.Strength,KeywordBuf.Quickness,KeywordBuf.Protection}.Contains(reward.Reward),
                      "full-board invention grants exactly three random next-scene stacks to each ally");
                ally.bufListDetail.OnRoundStart();
                Check(ally.bufListDetail.GetActivatedBuf(reward.Reward)==null,"full-board reward cannot activate in the creation scene");
                reward.OnRoundEnd();reward.OnRoundEnd();
                Check(ally.bufListDetail.GetReadyBufList().Any(x=>x.bufType==reward.Reward&&x.stack==3),"reward queues only once at scene end");
                ally.bufListDetail.OnRoundStart();
                Check(ally.bufListDetail.GetActivatedBuf(reward.Reward).stack==3,"reward promotes on the next native scene");
            }
            CheckSixSlotUI(owner);
            var first=TaffyInventions.AliveUnits(owner)[0];int freed=first.index;
            first.Die();inventor.OnDieOtherUnit(first);
            Check(inventor.DestroyedCount==1 && TaffyInventions.AliveUnits(owner).Count==3,
                  "death counts once and never deploys an old queued invention");
            inventor.OnEndBattle(null);
            Check(TaffyInventions.AliveUnits(owner).Count==3,"page end does not refill the empty slot");
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Super,InventionKind.Energy));
            Check(TaffyInventions.AliveUnits(owner).Any(x=>x.index==freed&&x!=first),"new invention reuses the dead slot");
            Check(TaffyInventions.AliveUnits(owner).Single(x=>x.index==freed).formationCellPos==first.formationCellPos,
                "replacement invention returns to the same right-hand rectangle corner");
            while(inventor.DestroyedCount<9)
            {
                var victim=TaffyInventions.AliveUnits(owner).FirstOrDefault();
                if(victim==null){PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Odd,InventionKind.Mechanism));victim=TaffyInventions.AliveUnits(owner)[0];}
                victim.Die();inventor.OnDieOtherUnit(victim);
                int count=inventor.DestroyedCount;
                int guard=owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyManufactureGuard>().Where(x=>!x.IsDestroyed()).Sum(x=>x.stack);
                int vulnerable=owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyManufactureVulnerable>().Where(x=>!x.IsDestroyed()).Sum(x=>x.stack);
                Check(guard==Math.Max(0,8-count)&&vulnerable==Math.Max(0,count-8),"persistent ward/vulnerability after kill "+count);
            }
            ClearInventions(owner);
            CheckVariants(owner,target);
            CheckAuras(owner,target,inventor);
            CheckAuraIndependence(owner,inventor);
            CheckScheduledTiming(owner,inventor);
            CheckIntercept(owner,target,inventor);
            CheckImproviseSlots(owner,pattern);
            int deaths=inventor.DestroyedCount;
            inventor.CompleteInvention(new TaffyInvention(InventionQualifier.Super,InventionKind.Energy));
            Check(inventor.ScheduledCount==1,"workshop-loss scenario has an invention awaiting next scene");
            var queued=Page(owner,Workshop,TaffyInventions.InventCardId);
            owner.cardSlotDetail.cardAry[4]=queued;
            Workshop.Die();inventor.OnDieOtherUnit(Workshop);
            Check(queued.isDestroyed && inventor.ScheduledCount==0 && owner.speedDiceResult.Count(x=>x.breaked)==2,"workshop death cancels already queued invention and breaks two dice immediately");
            owner.RollSpeedDice();
            Check(owner.speedDiceResult.Count(x=>x.breaked)==2 && owner.speedDiceResult.Count(x=>!x.breaked)==4,"workshop loss permanently breaks exactly two speed dice");
            pattern.OnAfterRollSpeedDice();
            int progress=inventor.Progress;inventor.OnRoundStart();inventor.AddProgress(100);
            Check(inventor.DestroyedCount==deaths&&inventor.Progress==progress&&!owner.allyCardDetail.GetHand().Any(TaffyInventions.IsInventCard),
                "workshop death neither strips ward nor continues workshop production");
            int live=TaffyInventions.AliveUnits(owner).Count;
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Super,InventionKind.Energy));
            Check(TaffyInventions.AliveUnits(owner).Count==live,"workshop loss prohibits future invention summons");
            owner.Die();
            Check(inventor.DestroyedCount==deaths&&inventor.ScheduledCount==0&&TaffyInventions.AliveUnits(owner).Count==0,
                "Taffy death cleans summons without crediting extra kills or spawning replacements");
            RenderSprites();
            Debug.Log("RECEPTION_COVERAGE exact DLL and native XML/card factories/queues/buffs; spawn scene loading, portrait UI, death presentation, recovery text and achievement bookkeeping are isolated substitutes. Complete live reception remains external acceptance.");
            Debug.Log("RECEPTION_PASS invention-unit checks="+Checks);
            EditorApplication.Exit(0);
        }
        catch(Exception ex)
        {
            File.WriteAllText(Path.Combine(Output,"failure.txt"),ex.ToString());
            Debug.LogError("RECEPTION_FAIL "+ex);EditorApplication.Exit(1);
        }
    }

    static void CheckVariants(BattleUnitModel creator,BattleUnitModel target)
    {
        var signatures=new HashSet<string>();
        for(int q=0;q<5;q++)for(int k=0;k<5;k++)
        {
            int id=TaffyInventions.FirstUnitId+q*5+k;
            var unit=Unit(Faction.Enemy,4,id,false);
            var passive=Passive(unit,new PassiveAbility_1883205());passive.Creator=creator;passive.OnUnitCreated();
            var fx=unit.Book.ClassInfo.EquipEffect;
            int[] baseHp={150,140,180,145,130},baseBp={80,75,100,70,85};
            double[] hpMult={1,1.1,.7,.9,1.3},bpMult={1,1,.8,1.1,1.25};
            Check(fx.Hp==(int)((int)(baseHp[k]*hpMult[q]+.5)*.8+.5) && fx.Break==(int)((int)(baseBp[k]*bpMult[q]+.5)*.8+.5),
                "twenty percent HP and stagger reduction from original rounded variant "+id);
            signatures.Add(string.Join(",",new[]{fx.SResist,fx.PResist,fx.HResist,fx.SBResist,fx.PBResist,fx.HBResist}));
            Check(passive.Device.UnitId==id&&unit.Book.HP==fx.Hp&&unit.Book.Break==fx.Break,
                "native variant stats and descriptor "+id);
            foreach(int emotion in new[]{0,4})
            {
                Set(unit.emotionDetail,"_emotionLevel",emotion);
                Check(unit.Book.GetSpeedDiceRule(unit).diceNum==2,"two dice at emotion "+emotion+" for "+id);
            }
            unit.speedDiceResult=Enumerable.Range(0,2).Select(_=>new SpeedDice()).ToList();
            passive.OnAfterRollSpeedDice();
            Check(unit.allyCardDetail.GetHand().Select(x=>x.GetID().id).SequenceEqual(new[]{1883500+2*k,1883501+2*k}),
                "type-specific two-page deck "+id);
            foreach(var card in unit.allyCardDetail.GetHand())
            {
                Check(card.GetSpec().Ranged==(new[]{1883503,1883506,1883509}.Contains(card.GetID().id)?CardRange.Far:CardRange.Near),"native ranged page type "+card.GetID().id);
                Check(card.CreateDiceCardBehaviorList().All(d=>string.IsNullOrEmpty(d.behaviourInCard.Script)||d.abilityList.Count>0),
                    "native dice ability factory resolves "+card.GetID().id);
            }
            var hit=Page(unit,target,1883500+2*k).cardBehaviorQueue.FirstOrDefault(x=>x.Type==BehaviourType.Atk);
            if(hit!=null) foreach(var ability in hit.abilityList)ability.OnSucceedAttack(target);
            if(k==0)
            {
                passive.OnSucceedAttack(hit);passive.OnSucceedAttack(hit);passive.OnSucceedAttack(hit);
                Check(unit.bufListDetail.GetReadyBufList().Any(x=>x.bufType==KeywordBuf.Strength&&x.stack>=passive.Device.Scale),
                    "mechanism winds into next-scene strength "+id);
            }
            if(k==3)
            {
                passive.BeforeRollDice(hit);
                var bonus=(DiceStatBonus)AccessTools.Field(typeof(BattleDiceBehavior),"_statBonus").GetValue(hit);
                Check(bonus.dmgRate==20*passive.Device.Scale,"destruction damage multiplier "+id);
            }
            if(k==4)
            {
                passive.OnUseCard(Page(unit,target,1883508));
                Check(TaffyMechanics.Smoke(unit)==2*passive.Device.Scale,"energy creates own smoke "+id);
                Set(unit,"<hp>k__BackingField",unit.MaxHp-20f);float before=unit.hp;
                passive.OnRoundEnd();
                Check(unit.hp==before+5*passive.Device.Scale,"energy repairs itself "+id);
            }
        }
        Check(signatures.Count==25,"all twenty-five six-resistance profiles are distinct");
        Check(!creator.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInvention>().Any(),
            "Taffy no longer receives permanent page-upgrade device buffs");
        var chain=Unit(Faction.Enemy,4,1883421,false);Passive(chain,new PassiveAbility_1883205());
        var page=Page(chain,target,1883503);page.cardAbility.OnUseCard();
        var stage=Singleton<StageController>.Instance;stage.GetAllCards().Clear();stage.GetAllCards().Add(page);
        for(int i=0;i<2;i++)
        {
            page.cardAbility.OnEndBattle();
            AccessTools.Method(typeof(StageController),"RemoveUsedCardData").Invoke(stage,new object[]{page});
            Check(page.target!=target&&stage.GetAllCards().Contains(page),"super chain schedules distinct secondary target "+i);
        }
        page.cardAbility.OnEndBattle();
        AccessTools.Method(typeof(StageController),"RemoveUsedCardData").Invoke(stage,new object[]{page});
        Check(!stage.GetAllCards().Contains(page),"chain replay terminates");
    }
    static void CheckAuras(BattleUnitModel owner,BattleUnitModel target,PassiveAbility_1883202 inventor)
    {
        for(int q=0;q<5;q++)
        {
            ClearInventions(owner);
            foreach(var old in owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>())old.Destroy();
            owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=0;
            PrepareInvention(inventor,new TaffyInvention((InventionQualifier)q,InventionKind.Mechanism));
            var unit=TaffyInventions.AliveUnits(owner).Single();
            var aura=TaffyInventions.Aura(owner,unit);
            Check(aura!=null&&aura.stack==1&&(int)aura.Qualifier==q,"live qualifier aura "+q);
            var page=Page(owner,target,1883002);
            var die=page.cardBehaviorQueue.Peek();
            if(q==3)die.behaviourInCard=new DiceBehaviour {Type=BehaviourType.Def,Detail=BehaviourDetail.Guard,Min=3,Dice=6};
            aura.BeforeRollDice(die);
            var bonus=(DiceStatBonus)AccessTools.Field(typeof(BattleDiceBehavior),"_statBonus").GetValue(die);
            if(q==0)Check(aura.GetSpeedDiceAdder(3)==1,"odd aura adds speed");
            if(q==1)
            {
                int before=target.bufListDetail.GetReadyBufList().Where(x=>x.bufType==KeywordBuf.Weak).Sum(x=>x.stack);
                owner.bufListDetail.OnSuccessAttack(die);owner.bufListDetail.OnSuccessAttack(die);
                Check(target.bufListDetail.GetReadyBufList().Where(x=>x.bufType==KeywordBuf.Weak).Sum(x=>x.stack)==before+1,"weird aura weakens once per page");
            }
            if(q==2)Check(bonus.dmgRate==15&&bonus.breakRate==15,"fragile aura increases both damage kinds");
            if(q==3||q==4)Check(bonus.power==1,"qualifier aura adds proper dice power "+q);
            inventor.OnRoundStart();inventor.OnRoundStart();
            Check(aura.stack==3,"qualifier aura grows once per living invention per scene "+q);
            unit.Die();
            Check(aura.IsDestroyed()&&aura.stack==0&&TaffyInventions.Aura(owner,unit)==null,
                "death removes the defeated device's entire aura "+q);
            inventor.OnRoundStart();
            Check(TaffyInventions.Aura(owner,unit)==null,"defeated device aura never returns "+q);
        }
        ClearInventions(owner);
        PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Super,InventionKind.Defense));
        PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Odd,InventionKind.Energy));
        var defense=TaffyInventions.AliveUnits(owner).Find(x=>TaffyInventions.UnitPassive(x).Device.Kind==InventionKind.Defense);
        var other=TaffyInventions.AliveUnits(owner).Find(x=>x!=defense);
        Func<BattleUnitModel,BattleUnitBuf_AceTaffyDeviceSupport> support=u=>u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyDeviceSupport>().Single();
        Check(support(other).GetStatBonus().dmgAdder==-4 && support(owner).GetStatBonus().dmgAdder==-4 && support(Workshop).GetStatBonus().dmgAdder==-4,
            "super defense supplies four persistent protection to every other ally including Taffy and workshop");
        Check(support(defense).GetStatBonus().dmgAdder==0,"defense never protects itself");
        var hit=Page(defense,target,1883504).cardBehaviorQueue.First();
        support(defense).BeforeRollDice(hit);
        Check(((DiceStatBonus)AccessTools.Field(typeof(BattleDiceBehavior),"_statBonus").GetValue(hit)).power==1,"energy boosts other invention dice power by one");
        Check(support(owner).Support(InventionKind.Energy)==0 && support(other).Support(InventionKind.Energy)==0,"energy excludes Taffy and itself");
        defense.Die();
        Check(support(other).GetStatBonus().dmgAdder==0 && support(owner).GetStatBonus().dmgAdder==0,"defense protection disappears immediately on death");
        ClearInventions(owner);
        foreach(var kind in new[]{InventionKind.Mechanism,InventionKind.Chain,InventionKind.Destruction})
            PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Odd,kind));
        PrepareInvention(inventor,new TaffyInvention(InventionQualifier.Odd,InventionKind.Energy));
        var receiver=TaffyInventions.AliveUnits(owner).Last();
        hit=Page(receiver,target,1883509).cardBehaviorQueue.First();support(receiver).BeforeRollDice(hit);
        var supportBonus=(DiceStatBonus)AccessTools.Field(typeof(BattleDiceBehavior),"_statBonus").GetValue(hit);
        Check(supportBonus.dmgRate==15&&supportBonus.breakRate==15&&supportBonus.max==1,"mechanism chain and destruction support compose without self-buffs");
        ClearInventions(owner);
    }
    static void CheckAuraIndependence(BattleUnitModel owner,PassiveAbility_1883202 inventor)
    {
        ClearInventions(owner);
        foreach(var old in owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>())old.Destroy();
        owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=0;
        inventor.CompleteInvention(new TaffyInvention(InventionQualifier.Odd,InventionKind.Mechanism));
        inventor.CompleteInvention(new TaffyInvention(InventionQualifier.Odd,InventionKind.Chain));
        inventor.OnRoundStart();
        var units=TaffyInventions.AliveUnits(owner);
        Check(units.Count==2,"two same-qualifier devices coexist for independent aura check");
        var auras=units.Select(x=>TaffyInventions.Aura(owner,x)).ToList();
        Check(auras.All(x=>x!=null&&x.stack==1&&x.Source==units[auras.IndexOf(x)]),
            "each device starts with its own one-layer aura");
        Check(!ReferenceEquals(auras[0],auras[1]),"same-qualifier devices never share an aura instance");
        inventor.OnRoundStart();inventor.OnRoundStart();
        Check(auras.All(x=>x.stack==3),"each device aura grows independently to three");
        units[0].Die();
        Check(auras[0].IsDestroyed()&&TaffyInventions.Aura(owner,units[0])==null,
            "defeated device aura is removed in full");
        Check(TaffyInventions.Aura(owner,units[1])==auras[1]&&auras[1].stack==3,
            "sibling device keeps its independent three layers");
        inventor.OnRoundStart();
        Check(TaffyInventions.Aura(owner,units[1]).stack==4,"surviving device continues its own growth");
        Check(owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>()
            .Where(x=>!x.IsDestroyed()).Sum(x=>x.stack)==4,"only the surviving device layers remain");
        units[1].Die();
        Check(!owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionAura>().Any(x=>!x.IsDestroyed()),
            "all independent auras disappear with their owners");
    }
    static void CheckImproviseSlots(BattleUnitModel owner,PassiveAbility_1883203 pattern)
    {
        // Sorted exactly like native RollSpeedDice: high to low, so the last
        // two entries are the slowest dice. A tie for second-slowest must not
        // steal the unique slowest die's page.
        int[] speeds={10,8,6,5,5,2};
        for(int seed=0;seed<8;seed++)
        {
            UnityEngine.Random.InitState(seed);
            owner.cardSlotDetail.InitCardSlots(speeds.Length);
            Set(owner.cardSlotDetail,"_playPoint",seed%5);
            owner.speedDiceResult=speeds.Select(v=>new SpeedDice { value=v }).ToList();
            pattern.OnAfterRollSpeedDice();
            var hand=owner.allyCardDetail.GetHand();
            var assigned=owner.cardSlotDetail.cardAry;
            Check(hand.Count==4&&!hand.Any(TaffyInventions.IsInventCard),
                "two invention pages leave the hand and four random pages remain, seed="+seed);
            Check(assigned[4]!=null&&assigned[5]!=null&&
                  TaffyInventions.IsInventCard(assigned[4].card)&&TaffyInventions.IsInventCard(assigned[5].card),
                "two invention pages occupy the two slowest dice, seed="+seed);
            for(int i=0;i<4;i++)
                Check(assigned[i]==null||!TaffyInventions.IsInventCard(assigned[i].card),
                    "no invention page on a faster die "+i+", seed="+seed);
            for(int i=4;i<6;i++)
                Check(assigned[i].slotOrder==i&&assigned[i].speedDiceResultValue==speeds[i],
                    "invention page binds native slow die "+i+", seed="+seed);
            Check(hand.Sum(x=>x.GetCost())<=seed%5,"random pages stay within the light budget, seed="+seed);
        }
        owner.cardSlotDetail.InitCardSlots(6);
        owner.speedDiceResult=Enumerable.Range(0,6).Select(_=>new SpeedDice()).ToList();
    }
    static void CheckFormationLayout(BattleUnitModel owner)
    {
        var devices=TaffyInventions.AliveUnits(owner);
        Check(Workshop.formationCellPos.x<owner.formationCellPos.x && devices.All(x=>x.formationCellPos.x>owner.formationCellPos.x),
            "native enemy coordinates place workshop left, Taffy middle, all four devices right");
        Check(Workshop.formationCellPos.y==0 && owner.formationCellPos.y==0,
            "workshop and Taffy share the centre line");
        var corners=devices.Select(x=>x.formationCellPos).ToArray();
        Check(corners.Distinct().Count()==4 && corners.Select(x=>x.x).Distinct().Count()==2 && corners.Select(x=>x.y).Distinct().Count()==2,
            "four inventions occupy a two-by-two rectangle with no outlier");
        var original=owner.formation;
        var before=Enumerable.Range(0,6).Select(i=>Wave.GetFormationPosition(i).Pos).ToArray();
        foreach(var position in Wave.GetFormation().PostionList)position.ChangePosToDefault();
        TaffyReceptionFormation.Ensure(Wave);
        Check(ReferenceEquals(original,owner.formation) && before.SequenceEqual(Wave.GetFormation().PostionList.Take(6).Select(x=>x.Pos)),
            "native reset and repeated setup preserve layout and existing unit position references");
        Check(Wave.GetFormation().XmlInfo.positionList[0].vector.x==10 && Wave.GetFormation().XmlInfo.positionList.Count==5,
            "reception layout leaves shared formation XML unchanged");
    }
    static void CheckSixSlotUI(BattleUnitModel owner)
    {
        Patch(typeof(BattleCharacterProfile.BattleCharacterProfileUI),"Initialize",nameof(Skip));
        var ui=new GameObject("Native profile manager").AddComponent<BattleUnitInfoManagerUI>();
        ui.enemyProfileArray=Enumerable.Range(0,5).Select(i=> {
            var p=new GameObject("Enemy profile "+i).AddComponent<BattleCharacterProfile.BattleCharacterProfileUI>();
            p.transform.SetParent(ui.transform);p.transform.localPosition=new Vector3(0,i*100,0);return p;
        }).ToArray();
        ui.allyProfileArray=Enumerable.Range(0,5).Select(i=>new GameObject("Ally profile "+i).AddComponent<BattleCharacterProfile.BattleCharacterProfileUI>()).ToArray();
        Patch(typeof(BattleCharacterProfile.BattleCharacterProfileUI),"SetUnitModel",nameof(ProfileWithoutPresentation));
        TaffyReceptionFormation.Install(Patches);
        ui.Initialize(Units);
        Check(ui.enemyProfileArray[5].UnitModel==Units.Single(x=>x.faction==Faction.Enemy&&x.index==5),"native profile Initialize binds sixth enemy");
        TaffyReceptionFormation.PrepareProfiles(ui,Units);
        Check(ui.enemyProfileArray.Length==6 && ui.enemyProfileArray.Select(x=>x.transform.localPosition).Distinct().Count()==6,
              "native enemy profile array expands to six distinct positions");
        TaffyReceptionFormation.PrepareProfiles(ui,Units);
        Check(ui.enemyProfileArray.Length==6,"profile expansion is idempotent");
        TaffyReceptionFormation.PrepareProfiles(ui,new List<BattleUnitModel>());
        Check(ui.enemyProfileArray[4].transform.localPosition.y==400 && !ui.enemyProfileArray[5].gameObject.activeSelf,
              "other receptions restore original profile positions and hide extra slot");
        var coins=new GameObject("Native emotion coin manager").AddComponent<BattleEmotionCoinUI>();
        coins.enermy=Enumerable.Range(0,5).Select(i=>new BattleEmotionCoinUI.BattleEmotionCoinData {
            target=new GameObject("Coin target "+i,typeof(RectTransform)).GetComponent<RectTransform>() }).ToArray();
        coins.librarian=Enumerable.Range(0,5).Select(i=>new BattleEmotionCoinUI.BattleEmotionCoinData {
            target=new GameObject("Ally coin target "+i,typeof(RectTransform)).GetComponent<RectTransform>() }).ToArray();
        coins.Init();
        Check(((System.Collections.IDictionary)AccessTools.Field(typeof(BattleEmotionCoinUI),"_enemy_lib").GetValue(coins)).Count==6,
            "native emotion coin Init registers all six enemies");
        TaffyReceptionFormation.PrepareCoins(coins);TaffyReceptionFormation.PrepareCoins(coins);
        Check(coins.enermy.Length==6 && coins.enermy.Select(x=>x.target).Distinct().Count()==6,"emotion coin UI owns six independent targets");
    }
    static void PrepareInvention(PassiveAbility_1883202 inventor,TaffyInvention device)
    {
        inventor.CompleteInvention(device);
        if(inventor.ScheduledCount==0)return;
        // Other tests arrange their starting units through the production scene boundary.
        var owner=Units.Single(x=>TaffyInventions.Inventor(x)==inventor);
        owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=0;
        inventor.OnRoundStart();
        owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=0;
    }
    static void CheckScheduledTiming(BattleUnitModel owner,PassiveAbility_1883202 inventor)
    {
        ClearInventions(owner);
        owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=4;
        inventor.OnRoundStart();
        Check(inventor.ScheduledCount==1 && TaffyInventions.AliveUnits(owner).Count==0,
            "completion from scene-start free progress waits an additional scene");
        inventor.OnRoundStart();
        var arriving=TaffyInventions.AliveUnits(owner).Single();
        arriving.RollSpeedDice();TaffyInventions.UnitPassive(arriving).OnAfterRollSpeedDice();
        Check(inventor.ScheduledCount==0 && arriving.allyCardDetail.GetHand().Count==2,
            "next-scene arrival can select both pages in its arrival scene");
        for(int i=0;i<4;i++)inventor.CompleteInvention(new TaffyInvention(InventionQualifier.Odd,InventionKind.Mechanism));
        Check(inventor.ScheduledCount==3 && TaffyInventions.AliveUnits(owner).Count==1,
            "scheduled units reserve the four-device cap without overflow backlog");
        inventor.OnRoundStart();
        Check(inventor.ScheduledCount==0 && TaffyInventions.AliveUnits(owner).Count==4,
            "all reserved inventions arrive together once at next scene start");
        ClearInventions(owner);
        inventor.OnRoundStart();
        Check(TaffyInventions.AliveUnits(owner).Count==0,"overflow production does not refill dead devices next scene");
        owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyInventionProgress>().Single().stack=0;
    }
    static void CheckIntercept(BattleUnitModel owner,BattleUnitModel target,PassiveAbility_1883202 inventor)
    {
        for(int lost=0;lost<=3;lost++)
        {
            var page=Page(owner,target,TaffyInventions.InventCardId);
            var ability=(DiceCardSelfAbility_AceTaffy_Improvise)page.cardAbility;
            ability.OnUseCard();for(int i=0;i<lost;i++)ability.Lose(i);
            var stage=Singleton<StageController>.Instance;stage.GetAllCards().Clear();stage.GetAllCards().Add(page);
            TaffyInventions.EndImprovise(page);
            AccessTools.Method(typeof(StageController),"RemoveUsedCardData").Invoke(stage,new object[]{page});
            if(lost==3)Check(!stage.GetAllCards().Contains(page),"all defeated invention dice cancel continuation");
            else
            {
                Check(page.target==Workshop&&page.cardBehaviorQueue.Count==3-lost&&page.cardBehaviorQueue.All(x=>x.Index>=lost),"native invention continuation retains survivors "+lost);
                TaffyInventions.EndImprovise(page);
                AccessTools.Method(typeof(StageController),"RemoveUsedCardData").Invoke(stage,new object[]{page});
                Check(!stage.GetAllCards().Contains(page),"invention continuation does not loop");
            }
        }
    }
    static void ClearInventions(BattleUnitModel owner)
    { foreach(var unit in TaffyInventions.AliveUnits(owner))unit.Die(); }
    static BattlePlayingCardDataInUnitModel Page(BattleUnitModel owner,BattleUnitModel target,int id)
    {
        var model=BattleDiceCardModel.CreatePlayingCard(ItemXmlDataList.instance.GetCardItem(new LorId("AceTaffy",id),false));
        var page=new BattlePlayingCardDataInUnitModel{owner=owner,target=target,card=model};
        page.cardAbility=model.CreateDiceCardSelfAbilityScript();if(page.cardAbility!=null)page.cardAbility.card=page;
        page.ResetCardQueue();return page;
    }
    static T Passive<T>(BattleUnitModel unit,T passive) where T:PassiveAbilityBase
    { passive.Init(unit);unit.passiveDetail.PassiveList.Add(passive);return passive; }
    static BattleUnitModel Unit(Faction faction,int index,int bookId=1883002,bool register=true)
    {
        var unit=(BattleUnitModel)FormatterServices.GetUninitializedObject(typeof(BattleUnitModel));
        Set(unit,"_id",++UnitId);
        unit.faction=faction;unit.index=index;unit.SetActionable(true);unit.history=new UnitBattleDataHistory();
        if(faction==Faction.Enemy)unit.formation=Wave.GetFormationPosition(index);
        unit.passiveDetail=new BattleUnitPassiveDetail(unit);unit.bufListDetail=new BattleUnitBufListDetail(unit);
        unit.allyCardDetail=new BattleAllyCardDetail(unit);unit.allyCardDetail.Init(new List<DiceCardXmlInfo>());
        unit.cardSlotDetail=new BattlePlayingCardSlotDetail(unit);unit.cardSlotDetail.InitCardSlots(6);
        unit.breakDetail=new BattleUnitBreakDetail(unit);unit.breakDetail.breakLife=1;
        unit.speedDiceResult=Enumerable.Range(0,6).Select(_=>new SpeedDice()).ToList();
        var data=(UnitBattleDataModel)FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
        data.emotionDetail=new BattleUnitEmotionDetail(unit);Set(unit,"_unitData",data);
        var xml=BookXml.Single(x=>x.id.id==bookId);var effect=xml.EquipEffect;
        var book=new BookModel();Set(book,"_classInfo",xml);Set(book,"_maxHp",effect.Hp);Set(book,"_maxBp",effect.Break);
        Set(book,"_speedDiceNum",xml.speedDiceNumber);Set(book,"_speedMin",effect.SpeedMin);Set(book,"_speedMax",effect.Speed);
        Set(book,"_maxPlayPoint",effect.MaxPlayPoint);Set(unit,"<hp>k__BackingField",(float)effect.Hp);
        unit.equipment=new BattleUnitModelEquip(unit){book=book,customBook=book};
        var unitData=(UnitDataModel)FormatterServices.GetUninitializedObject(typeof(UnitDataModel));
        Set(unitData,"_bookItem",book);unitData.giftInventory=new GiftInventory(unitData);data.unitData=unitData;
        if(register)Units.Add(unit);return unit;
    }
    public static bool ProfileWithoutPresentation(BattleCharacterProfile.BattleCharacterProfileUI __instance,BattleUnitModel unitModel)
    { Set(__instance,"_unitModel",unitModel);return false; }
    public static bool CurrentWave(ref StageWaveModel __result) { __result=Wave;return false; }
    public static bool Skip(){return false;}
    public static bool PreviewCharacter(UnitDataModel unit,int index){unit.textureIndex=index;return false;}
    public static bool RecoverWithoutUI(BattleUnitBreakDetail __instance,int value){__instance.RecoverBreakWithoutEffect(value);return false;}
    public static bool RecoverHpWithoutUI(BattleUnitModel __instance,int v)
    {Set(__instance,"<hp>k__BackingField",Math.Min(__instance.MaxHp,__instance.hp+v));return false;}
    public static bool SkipSummonWave(BattleUnitModel __instance)
    {return !TaffyInventions.IsWorkshop(__instance)&&TaffyInventions.UnitPassive(__instance)==null;}
    public static bool SummonSlots(BattleUnitModel __instance)
    {__instance.cardSlotDetail.InitCardSlots(__instance.Book.GetSpeedDiceRule(__instance).diceNum);return false;}
    public static bool RollWithoutUI(BattleUnitModel __instance,ref List<SpeedDice> __result)
    {__result=__instance.Book.GetSpeedDiceRule(__instance).Roll(__instance);__instance.speedDiceResult=__result;return false;}
    public static bool DeathWithoutPresentation(BattleUnitModel __instance)
    {
        if(__instance.IsDead())return false;
        __instance.DieFake();__instance.passiveDetail.OnDie();
        foreach(var unit in BattleObjectManager.instance.GetAliveList(false))if(unit!=__instance)unit.OnDieOtherUnit(__instance);
        return false;
    }
    public static bool Spawn(Faction faction,LorId enemyUnitId,int index,ref BattleUnitModel __result)
    {
        if(enemyUnitId.packageId!="AceTaffy")throw new Exception("Wrong spawn package");
        if(Units.Any(u=>u.faction==faction&&u.index==index&&!u.IsDead()))throw new Exception("Spawn overwrote living unit");
        Units.RemoveAll(u=>u.faction==faction&&u.index==index);
        SpawnCount++;
        if(enemyUnitId.id==1884){Workshop=Unit(faction,index,1883003);Passive(Workshop,new PassiveAbility_1883204());__result=Workshop;}
        else{__result=Unit(faction,index,enemyUnitId.id);var passive=Passive(__result,new PassiveAbility_1883205());passive.OnUnitCreated();}
        return false;
    }
    public static void Preview()
    {
        try
        {
            Directory.CreateDirectory(Output);
            RenderSprites();
            InventionIconFixture.Run();
        }
        catch(Exception ex)
        {
            File.WriteAllText(Path.Combine(Output,"failure.txt"),ex.ToString());
            Debug.LogError("RECEPTION_FAIL "+ex);EditorApplication.Exit(1);
        }
    }
    static void RenderSprites()
    {
        var camera=new GameObject("Invention unit acceptance").AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5.6f;camera.transform.position=new Vector3(0,2,-10);
        camera.backgroundColor=new Color(.12f,.13f,.15f);camera.clearFlags=CameraClearFlags.SolidColor;
        var rt=new RenderTexture(1500,520,24);camera.targetTexture=rt;
        for(int q=0;q<5;q++)
        {
            var objects=new List<GameObject>();
            for(int k=0;k<5;k++)
            {
                string skin="AceTaffy_Invention_"+q+"_"+k;
                var go=new GameObject(skin);objects.Add(go);var renderer=go.AddComponent<SpriteRenderer>();
                renderer.sprite=SpriteUtil.LoadLargePivotSprite(Path.Combine(Ace,"AceTaffyMod/Char",skin,"ClothCustom/Default.png"),new Vector2(.5f,(512-280f)/1024));
                renderer.transform.position=new Vector3(-12+k*6f,0,0);
                Check(renderer.sprite!=null,"native sprite loader decoded "+skin);
            }
            camera.Render();RenderTexture.active=rt;var png=new Texture2D(1500,520,TextureFormat.RGBA32,false);
            png.ReadPixels(new Rect(0,0,1500,520),0,0);png.Apply();
            File.WriteAllBytes(Path.Combine(Output,"units-qualifier-"+q+".png"),png.EncodeToPNG());
            RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(png);
            foreach(var go in objects)UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
