using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Steria;

public static class Program
{
    private static int _checks,_plays,_afterRoll;
    private static BattleUnitModel _actual;
    private static readonly List<string> Events=new List<string>();
    private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private static void Check(bool pass,string message)
    { if(!pass)throw new Exception(message);Console.WriteLine("PASS "+(++_checks)+": "+message); }
    public static int ControlledRange(int min,int max) { return min; }
    public static void NoLog(object value) { }
    public static bool NoCall() { return false; }
    public static bool RecordPlay(BattleUnitModel caster,BattleUnitModel target)
    { _actual=target;_plays++;Events.Add("player dispatch");return false; }
    public static bool RecordAfterRoll(BattleUnitModel caster,BattleUnitModel target)
    { _actual=target;_afterRoll++;Events.Add("after-roll dispatch");return false; }

    // Only native RNG and logging are substituted for deterministic managed
    // execution. Candidate passive/card IL, real game models/buffs and cooldown
    // manager remain authoritative. Render dispatch is observed at its boundary.
    public static IEnumerable<CodeInstruction> IsolateEngine(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var instruction in instructions)
        {
            var method=instruction.operand as MethodInfo;
            if(method!=null && method.DeclaringType==typeof(UnityEngine.Random) && method.Name=="Range" && method.ReturnType==typeof(int))
                instruction.operand=AccessTools.Method(typeof(Program),"ControlledRange");
            if(method!=null && method.DeclaringType==typeof(UnityEngine.Debug) && method.Name=="Log" && method.GetParameters().Length==1)
                instruction.operand=AccessTools.Method(typeof(Program),"NoLog");
            yield return instruction;
        }
    }
    private static BattleUnitModel Unit(int id,Faction faction)
    {
        var unit=new BattleUnitModel(id){faction=faction,index=id};unit.breakDetail.breakLife=1;
        unit.bufListDetail.GetActivatedBufList().Add(new BattleUnitBuf_quickness{stack=5});
        BattleObjectManager.instance.GetList().Add(unit);return unit;
    }
    private static int Buff(BattleUnitModel unit) { return unit.bufListDetail.GetActivatedBufList().Single().stack; }
    private static void Run()
    {
        var harmony=new Harmony("Steria.VeliaObservation.ManagedAcceptance");
        harmony.Patch(AccessTools.Method(typeof(SteriaLogger),"Log"),prefix:new HarmonyMethod(typeof(Program),"NoCall"));
        harmony.Patch(AccessTools.Method(typeof(VeliaPotentialObservationEffect),"Play"),prefix:new HarmonyMethod(typeof(Program),"RecordPlay"));
        harmony.Patch(AccessTools.Method(typeof(VeliaPotentialObservationEffect),"PlayAfterSpeedDice"),prefix:new HarmonyMethod(typeof(Program),"RecordAfterRoll"));
        harmony.Patch(AccessTools.Method(typeof(VeliaPotentialObservationEffect),"Cancel"),prefix:new HarmonyMethod(typeof(Program),"NoCall"));
        foreach(var method in new[]{AccessTools.Method(typeof(PassiveAbility_9004001),"ApplyPotentialObservationEffect"),
            AccessTools.Method(typeof(PassiveAbility_9004001),"AutoApplyPotentialObservation"),
            AccessTools.Method(typeof(DiceCardSelfAbility_VeliaPotentialObservation),"ApplyEffect"),
            AccessTools.Method(typeof(DiceCardSelfAbility_VeliaPotentialObservation),"OnUseInstance")})
            harmony.Patch(method,transpiler:new HarmonyMethod(typeof(Program),"IsolateEngine"));
        BattleObjectManager.instance.Init_only();
        var owner=Unit(0,Faction.Enemy);var target=Unit(1,Faction.Enemy);
        var passive=new PassiveAbility_9004001();passive.Init(owner);
        // Direct actual application exposes the settlement/feedback split and
        // proves the recipient remains the selected object, not a later draw.
        passive.ApplyPotentialObservationEffect(target);
        Check(Buff(target)==6 && _afterRoll==0,"target Quickness incremented before after-roll; no visual dispatch yet");
        passive.ApplyPotentialObservationEffect(owner);
        Check(Buff(owner)==5 && Buff(target)==6,"same-round duplicate application cannot settle twice");
        passive.OnAfterRollSpeedDice();passive.OnAfterRollSpeedDice();
        Check(_afterRoll==1 && ReferenceEquals(_actual,target),"after-roll consumes exact applied target once despite repeated callback");
        Check(Buff(target)==6,"visual callback does not reapply mechanics");
        // Empty live list isolates reset semantics from the next auto selection.
        BattleObjectManager.instance.GetList().Clear();passive.OnRoundStart();
        passive.ApplyPotentialObservationEffect(owner);passive.OnAfterRollSpeedDice();
        Check(_afterRoll==2 && ReferenceEquals(_actual,owner) && Buff(owner)==6,"next round self-target produces one feedback and one settlement");
        passive.OnRoundStart();passive.ApplyPotentialObservationEffect(target);passive.OnWaveStart();passive.OnAfterRollSpeedDice();
        Check(_afterRoll==2,"wave reset discards pending target without stale dispatch");
        SteriaEgoCooldownManager.ClearAllCooldowns();
        var player=Unit(2,Faction.Player);var ally=Unit(3,Faction.Player);
        var card=new DiceCardSelfAbility_VeliaPotentialObservation();
        card.OnUseInstance(player,null,target);card.OnUseInstance(player,null,null);
        Check(_plays==0 && Buff(target)==7,"invalid enemy/null targets emit no player feedback or buff");
        card.OnUseInstance(player,null,ally);
        Check(_plays==1 && ReferenceEquals(_actual,ally) && Buff(ally)==6,"player success dispatches actual ally after buff");
        card.OnUseInstance(player,null,ally);
        Check(_plays==1 && Buff(ally)==6,"cooldown rejects duplicate visuals and mechanics");
        // Enumerate the actual compiler-generated host coroutine; do not fake a
        // frame scheduler. Generation mismatch skips the engine entry at its end.
        var host=(VeliaPotentialObservationEffect)FormatterServices.GetUninitializedObject(typeof(VeliaPotentialObservationEffect));
        var iterator=(IEnumerator)typeof(VeliaPotentialObservationEffect).GetMethod("AfterDice",Flags).Invoke(host,new object[]{-1});
        Check(iterator.MoveNext() && iterator.Current==null,"post-roll host first yields one rendered-frame boundary");
        Check(iterator.MoveNext() && iterator.Current is UnityEngine.WaitForSeconds,"host then waits scaled native WaitForSeconds");
        float seconds=(float)typeof(UnityEngine.WaitForSeconds).GetField("m_Seconds",Flags).GetValue(iterator.Current);
        Check(Math.Abs(seconds-.22f)<1e-6f,"post-roll wait is .22s beyond next-frame native .20s roulette tail");
        Check(!iterator.MoveNext(),"stale generation finishes coroutine without dispatch");
        Console.WriteLine("SCOPE real game models + candidate DLL logic; controlled RNG/logging and observed render dispatch; no live battle or engine scheduling claim");
    }
    public static int Main()
    {
        try { Run();Console.WriteLine("VELIA_OBSERVATION_REAL_ASSEMBLY_PASS checks="+_checks);return 0; }
        catch(Exception ex) { Console.WriteLine(ex);return 1; }
    }
}
