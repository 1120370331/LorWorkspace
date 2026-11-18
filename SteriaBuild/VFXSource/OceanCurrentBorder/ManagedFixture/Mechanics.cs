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
 static void Main(){try{Run();Console.WriteLine("BORDER_REAL_MECHANICS_PASS "+n);}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
  Fresh();var u=Unit(20,true);var c=Card(9002002,3);var same=Card(9002002,3);
  var trace=new List<string>(); Action<string> save=label=>trace.Add(label+","+(FlowPlanning.Get(c)?.Levels??0)+","+(FlowPlanning.Get(same)?.Levels??0));
  save("plain");Check(Enhance(u,c),"actual card gains Flow enhancement");save("enhanced");
  Check(FlowPlanning.Get(c).Levels==1&&FlowPlanning.Get(same)==null,"same XML ID cards are independent instances");
  Check(Enhance(u,c),"actual second enhancement");save("twice");
  FlowPlanning.Commit();FlowPlanning.BeginRound();Check(FlowPlanning.Get(c).Levels==2,"unused card survives next round");save("next-round");
  var action=Equip(u,c);FlowPlanning.Commit();HarmonyHelpers.RegisterCardUsage(action);
  Check(FlowPlanning.Get(c)==null&&HarmonyHelpers.GetFlowPowerBonusForDice(action,0)==2,"consumption clears hand enhancement and retains action snapshot");save("consumed");
  Fresh();save("reset");System.IO.File.WriteAllLines("output/ocean-current-vfx/ui/managed/real-mechanics-trace.csv",trace);
 }
}
