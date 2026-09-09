using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Steria;
using LOR_DiceSystem;
class Program {
 static int n;
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static void Check(bool ok,string label) {if(!ok)throw new Exception(label);Console.WriteLine("PASS "+(++n)+": "+label);}
 static BattleUnitModel Unit(int flow,bool slaz=false,Faction faction=Faction.Enemy){
  var u=new BattleUnitModel(n+1);u.faction=faction;u.breakDetail.breakLife=1;
  if(slaz){var p=new PassiveAbility_9002001();p.Init(u);u.passiveDetail.PassiveList.Add(p);}
  u.bufListDetail.AddBuf(new BattleUnitBuf_Flow{stack=flow});
  BattleObjectManager.instance.GetList().Add(u);return u;
 }
 static BattleDiceCardModel Card(int id,int count=2,string package="SteriaBuilding"){
  var xml=new DiceCardXmlInfo(new LorId(package,id));xml.workshopName="fixture";
  for(int i=0;i<count;i++)xml.DiceBehaviourList.Add(new DiceBehaviour{Min=3,Dice=7,Type=BehaviourType.Atk,Detail=BehaviourDetail.Slash});
  var c=new BattleDiceCardModel();typeof(BattleDiceCardModel).GetField("_xmlData",flags).SetValue(c,xml);return c;
 }
 static BattlePlayingCardDataInUnitModel Equip(BattleUnitModel u,BattleDiceCardModel c){var p=new BattlePlayingCardDataInUnitModel{owner=u,card=c};u.cardSlotDetail.cardAry.Add(p);return p;}
 static bool Enhance(BattleUnitModel u,BattleDiceCardModel c){string message;return FlowPlanning.TryEnhance(u,c,out message,true);}
 static int Field(object o,string f){return (int)o.GetType().GetField(f,flags).GetValue(o);}
 static void Fresh(){FlowPlanning.Reset();BattleObjectManager.instance.Init_only();}
 static void Main(){try{Run();Console.WriteLine("FLOW_REAL_ASSEMBLY_PASS "+n);}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
  Fresh();var u=Unit(2);var c=Card(9002001);
  Check(Enhance(u,c)&&FlowPlanning.Available(u)==0,"exact balance succeeds and spends the final Flow");
  Fresh();u=Unit(3);c=Card(9002001);
  Check(Enhance(u,c)&&FlowPlanning.Available(u)==1&&FlowPlanning.Get(c).Levels==1,"one click spends two and gives one level");
  Check(!Enhance(u,c)&&FlowPlanning.Available(u)==1,"normal limit blocks repeat");
  Fresh();u=Unit(20,true);c=Card(9002001);var other=Card(9002001);
  Check(Enhance(u,c)&&Enhance(u,c)&&!Enhance(u,c),"divine passive grants exactly one extra level");
  Check(FlowPlanning.Available(u)==16&&FlowPlanning.Get(other)==null,"same card id does not share enhancement");
  var action=Equip(u,c);var plain=Equip(u,other);var passive=u.passiveDetail.PassiveList.OfType<PassiveAbility_9002001>().Single();
  Check(Field(passive,"_flowConsumedAccumulator")==0,"planning does not farm consumption hooks");
  FlowPlanning.Commit();FlowPlanning.Commit();
  Check(Field(passive,"_flowConsumedAccumulator")==4,"commit notifies divine consumption once");
  HarmonyHelpers.RegisterCardUsage(plain);HarmonyHelpers.RegisterCardUsage(action);HarmonyHelpers.RegisterCardUsage(action);
  Check(FlowPlanning.Available(u)==16,"execution/rebinding never spends flow again");
  Check(HarmonyHelpers.GetFlowPowerBonusForDice(action,0)==2&&HarmonyHelpers.GetFlowPowerBonusForDice(action,1)==2,"equal per-die planned enhancement binds");
  Check(HarmonyHelpers.GetFlowPowerBonusForDice(plain,0)==0,"unenhanced page never receives automatic flow");
  Check(HarmonyHelpers.GetFlowConsumedByCard(action)==4&&HarmonyHelpers.GetFlowEnhancementCountForDice(action,1)==2,"existing consumer hooks read planned records");
  var refunds=u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_FlowTransferRefundNextRound>().ToList();
  Check(refunds.Count==1&&refunds[0].stack==4,"flow-transfer schedules actual spend once");
  CardAbilityHelper.AddFlowStacks(u,3);CardAbilityHelper.AddFlowStacks(u,2,false);
  Check(FlowPlanning.Available(u)==16&&u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().Single().stack==8,"mid-scene gain defers; divine multiplier applies once; external gain is excluded");
  FlowGainTiming.BeginRound();foreach(var refund in refunds)refund.OnRoundStart();
  Check(FlowPlanning.Available(u)==28,"next start settles gain plus refund without multiplying refund");
  CardAbilityHelper.AddFlowStacks(u,1);FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Available(u)==30&&!u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().Any(),"existing next-start producer is not delayed twice");
  Check(FlowPlanning.Get(c)==null&&HarmonyHelpers.GetFlowPowerBonusForDice(action,0)==0,"used card stays cleared and next round clears action maps");
  Fresh();u=Unit(10,true);c=Card(9002006,3);Enhance(u,c);action=Equip(u,c);FlowPlanning.Commit();
  u.passiveDetail.PassiveList[0].OnLoseParrying(null);
  HarmonyHelpers.RegisterCardUsage(action);
  Check(FlowPlanning.Available(u)==6&&HarmonyHelpers.GetFlowPowerBonusForDice(action,2)==1,"clash loss keeps divine penalty without revoking paid enhancement");
  Fresh();u=Unit(5);c=Card(123,2);Enhance(u,c);action=Equip(u,c);FlowPlanning.Commit();
  c.GetBehaviourList().Add(new DiceBehaviour{Min=1,Dice=2,Type=BehaviourType.Atk,Detail=BehaviourDetail.Slash});
  HarmonyHelpers.RegisterCardUsage(action);
  Check(HarmonyHelpers.GetFlowPowerBonusForDice(action,2)==0,"dice added after planning cannot gain unpaid enhancement");
  Fresh();u=Unit(9);c=Card(9002009,1);Enhance(u,c);Equip(u,c);FlowPlanning.Commit();CardAbilityHelper.AddFlowStacks(u,3);
  FlowGainTiming.BeginRound();FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Available(u)==3,"gain after spending all creates live flow instead of reviving destroyed buff");
  Fresh();u=Unit(30,true);c=Card(9002006,3);for(int i=0;i<5;i++)Check(Enhance(u,c),"ocean enhancement step "+i);
  Check(!Enhance(u,c)&&FlowPlanning.Get(c).Levels==5&&FlowPlanning.Available(u)==15,"ocean retains four plus divine one limit");
  Fresh();u=Unit(8,true);c=Card(9002001);Enhance(u,c);Enhance(u,c);Equip(u,c);
  var ally=Unit(3);var endless=Equip(ally,Card(9002007,0));FlowPlanning.Reconcile();
  Check(FlowPlanning.Available(u)==8&&FlowPlanning.Get(c).Paid==0,"planned endless refunds reservation without divine multiplication");
  var extra=Card(9002006,3);for(int i=0;i<5;i++)Enhance(u,extra);Equip(u,extra);
  Check(FlowPlanning.Get(extra).Levels==5&&FlowPlanning.Available(u)==8,"free enhancement still has finite levels");
  ally.cardSlotDetail.cardAry.Remove(endless);FlowPlanning.Reconcile();
  Check(FlowPlanning.Available(u)>=1&&FlowPlanning.Get(c).Paid==4&&FlowPlanning.Get(extra).Levels==1,"removing immunity reprices and retracts unaffordable levels");
  Check(Field(u.passiveDetail.PassiveList[0],"_flowConsumedAccumulator")==0,"immunity toggles cannot farm passive counters");
  Fresh();u=Unit(17,true);c=Card(9002009,1);Check(Enhance(u,c)&&FlowPlanning.Available(u)==0,"mass attack explicitly reserves all flow");
  action=Equip(u,c);FlowPlanning.Commit();HarmonyHelpers.RegisterCardUsage(action);
  Check(HarmonyHelpers.GetMassAttackFlowConsumed(action)==17&&HarmonyHelpers.GetFlowPowerBonusForDice(action,0)==0,"mass retains special spend and no ordinary bonus");
  Fresh();u=Unit(10);c=Card(9002001);Enhance(u,c);FlowPlanning.Commit();
  Check(FlowPlanning.Available(u)==8&&FlowPlanning.Get(c)?.Levels==1,"unused hand enhancement stays paid and committed");
  FlowGainTiming.BeginRound();FlowGainTiming.EndRoundStart();FlowPlanning.Commit();FlowGainTiming.BeginRound();FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Get(c)?.Levels==1&&FlowPlanning.Available(u)==8,"unused hand enhancement survives two rounds without refunding spend");
  action=Equip(u,c);FlowPlanning.Commit();HarmonyHelpers.RegisterCardUsage(action);
  Check(FlowPlanning.Get(c)==null&&HarmonyHelpers.GetFlowPowerBonusForDice(action,0)==1,"actual usage removes hand enhancement but preserves current action power");
  var repeated=new BattlePlayingCardDataInUnitModel{owner=u,card=c};HarmonyHelpers.InheritManualFlow(action,repeated);HarmonyHelpers.ClearAllFlowPowerBonusForCard(action);HarmonyHelpers.RegisterCardUsage(repeated);
  Check(HarmonyHelpers.GetFlowPowerBonusForDice(repeated,0)==1&&HarmonyHelpers.GetFlowEnhancementCountForDice(repeated,0)==1,"derived repeat action inherits an independent locked enhancement snapshot");
  var reused=Equip(u,c);HarmonyHelpers.RegisterCardUsage(reused);
  Check(HarmonyHelpers.GetFlowPowerBonusForDice(reused,0)==0,"later reuse of same card instance has no stale enhancement");
  Fresh();u=Unit(10);c=Card(123,1);c.GetBehaviourList()[0].Type=BehaviourType.Standby;
  Check(!Enhance(u,c)&&FlowPlanning.Available(u)==10,"counter-only cards preserve exclusion");
  Fresh();u=Unit(10,false,Faction.Enemy);c=Card(9002001);Equip(u,c);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(c)!=null,"enemy prepares through same entry before execution");
  Fresh();u=Unit(10);c=Card(9002006,3,"another-mod");Check(Enhance(u,c)&&!Enhance(u,c),"foreign mod id never acquires Slazeya card limit");
  Fresh();u=Unit(4);var low=Card(801,2);low.XmlData.Spec.Cost=1;var high=Card(802,3);high.XmlData.Spec.Cost=3;
  Equip(u,low);Equip(u,high);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(high)?.Levels==1&&FlowPlanning.Get(low)==null&&FlowPlanning.Available(u)==1,"AI funds high-cost page before earlier low-cost slot");
  FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(high)?.Levels==1&&FlowPlanning.Available(u)==1,"repeated preparation reprices without double spending");
  Fresh();u=Unit(3);var many=Card(803,3);many.XmlData.Spec.Cost=2;var few=Card(804,1);few.XmlData.Spec.Cost=2;
  Equip(u,many);Equip(u,few);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(few)?.Levels==1&&FlowPlanning.Get(many)==null&&FlowPlanning.Available(u)==2,"equal-cost AI pages prefer fewer dice and keep unusable remainder");
  Fresh();u=Unit(4);low=Card(805,2);low.XmlData.Spec.Cost=1;high=Card(806,5);high.XmlData.Spec.Cost=3;
  Equip(u,high);Equip(u,low);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(high)==null&&FlowPlanning.Get(low)?.Levels==1,"unaffordable high-cost page does not stop lower-cost candidates");
  Fresh();u=Unit(20,true);low=Card(807,2);low.XmlData.Spec.Cost=1;high=Card(9002006,3);high.XmlData.Spec.Cost=3;
  Equip(u,low);Equip(u,high);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(high)?.Levels==5&&FlowPlanning.Get(low)?.Levels==2&&FlowPlanning.Available(u)==1,"AI exhausts higher-priority enhancement cap then spends on next page");
  Fresh();u=Unit(4);low=Card(808,2);low.XmlData.Spec.Cost=1;high=Card(809,3);high.XmlData.Spec.Cost=4;
  low.SetCostToZero(true);high.SetCostToZero(true);Equip(u,low);Equip(u,high);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(high)!=null&&FlowPlanning.Get(low)==null,"enemy zero-cost override preserves printed light-cost priority");
  Fresh();u=Unit(1);many=Card(810,1);many.XmlData.Spec.Cost=2;many.GetBehaviourList().Add(new DiceBehaviour{Type=BehaviourType.Standby,Detail=BehaviourDetail.Guard,Min=2,Dice=4});
  few=Card(811,1);few.XmlData.Spec.Cost=2;Equip(u,many);Equip(u,few);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(few)!=null&&FlowPlanning.Get(many)==null,"same-cost priority counts printed standby dice too");
  Fresh();u=Unit(5,true);c=Card(9002001,2);Equip(u,c);FlowPlanning.PrepareEnemies();FlowPlanning.PrepareEnemies();FlowPlanning.Commit();FlowPlanning.Commit();
  refunds=u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_FlowTransferRefundNextRound>().Where(b=>!b.IsDestroyed()).ToList();
  Check(FlowPlanning.Available(u)==1&&refunds.Count==1&&refunds[0].stack==4,"AI Flow-transfer schedules actual preparation spend once");
  FlowGainTiming.BeginRound();foreach(var refund in refunds)refund.OnRoundStart();FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Available(u)==5,"AI keeps remainder and receives exact Flow-transfer refund next round");
  Fresh();u=Unit(4);c=Card(812,3);var removed=Equip(u,c);FlowPlanning.PrepareEnemies();u.cardSlotDetail.cardAry.Remove(removed);
  other=Card(813,2);Equip(u,other);FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(c)==null&&FlowPlanning.Get(other)?.Levels==1&&FlowPlanning.Available(u)==2,"changed AI loadout reuses released preparation budget");
  Fresh();u=Unit(12,true);c=Card(9002001);Enhance(u,c);FlowPlanning.Commit();
  passive=u.passiveDetail.PassiveList.OfType<PassiveAbility_9002001>().Single();
  refunds=u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_FlowTransferRefundNextRound>().Where(b=>!b.IsDestroyed()).ToList();
  FlowGainTiming.BeginRound();foreach(var refund in refunds)refund.OnRoundStart();FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Available(u)==12&&FlowPlanning.Get(c)?.Levels==1&&Field(passive,"_flowConsumedAccumulator")==2,"unplayed transfer refunds newly paid Flow next round without divine multiplication");
  Check(Enhance(u,c)&&FlowPlanning.Get(c).Levels==2&&FlowPlanning.Available(u)==10,"cross-round extra enhancement only pays for one new level");
  FlowPlanning.Commit();FlowPlanning.Commit();
  refunds=u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_FlowTransferRefundNextRound>().Where(b=>!b.IsDestroyed()).ToList();
  Check(Field(passive,"_flowConsumedAccumulator")==4&&refunds.Count==1&&refunds[0].stack==2,"cross-round commit notifies and refunds only new investment once");
  FlowGainTiming.BeginRound();foreach(var refund in refunds)refund.OnRoundStart();FlowGainTiming.EndRoundStart();FlowPlanning.Commit();
  Check(Field(passive,"_flowConsumedAccumulator")==4&&!u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_FlowTransferRefundNextRound>().Any(b=>!b.IsDestroyed()),"holding enhanced transfer page cannot repeatedly farm refunds or passive progress");
  Fresh();u=Unit(10,true);c=Card(9002006,2);Enhance(u,c);FlowPlanning.Commit();FlowGainTiming.BeginRound();FlowGainTiming.EndRoundStart();
  ally=Unit(3);endless=Equip(ally,Card(9002007,0));FlowPlanning.Reconcile();
  Check(FlowPlanning.Available(u)==8&&FlowPlanning.Get(c).Paid==2,"later immunity cannot refund historical paid levels");
  Enhance(u,c);Check(FlowPlanning.Get(c).Levels==2&&FlowPlanning.Get(c).Paid==2,"new immunity level remains separate from historical payment");
  ally.cardSlotDetail.cardAry.Remove(endless);FlowPlanning.Reconcile();
  Check(FlowPlanning.Available(u)==6&&FlowPlanning.Get(c).Levels==2&&FlowPlanning.Get(c).Paid==4,"removing immunity reprices only the new level");
  Equip(u,c);FlowPlanning.PrepareEnemies();FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(c).Levels==5&&FlowPlanning.Get(c).Paid==10&&FlowPlanning.Available(u)==0,"AI replan preserves historical enhancement and spends current budget exactly");
  u.cardSlotDetail.cardAry.Clear();FlowPlanning.PrepareEnemies();
  Check(FlowPlanning.Get(c).Levels==1&&FlowPlanning.Get(c).Paid==2&&FlowPlanning.Available(u)==8,"AI loadout removal releases only uncommitted additions and preserves historical payment");
  Fresh();u=Unit(0);CardAbilityHelper.AddFlowStacksThisRound(u,5);
  Check(FlowPlanning.Available(u)==5&&!u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().Any(),"Self Flow immediate API gives five usable Flow without pending income");
  CardAbilityHelper.AddFlowStacks(u,3);
  Check(FlowPlanning.Available(u)==5&&u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().Single().stack==3,"ordinary producers remain deferred after Self Flow immediate income");
  var explicitNext=new BattleUnitBuf_SlazeyaFlowNextTurn{stack=2};u.bufListDetail.AddBuf(explicitNext);
  Check(FlowPlanning.Available(u)==5,"explicit next-turn producer does not settle early");
  FlowGainTiming.BeginRound();explicitNext.OnRoundStart();FlowGainTiming.EndRoundStart();
  Check(FlowPlanning.Available(u)==10,"ordinary and explicit next-turn income still settles at next round start");
  Fresh();u=Unit(0,true);CardAbilityHelper.AddFlowStacksThisRound(u,5);
  Check(FlowPlanning.Available(u)==10&&!u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_PendingFlow>().Any(),"immediate Flow applies divine source multiplier exactly once");
  CardAbilityHelper.AddFlowStacksThisRound(u,3,false);
  Check(FlowPlanning.Available(u)==13,"immediate external Flow preserves source multiplier exclusion");
 }
}

