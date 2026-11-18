using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using LOR_DiceSystem;
using MA = RencounterManager.MovingAction;
using AAB = RencounterManager.ActionAfterBehaviour;

internal static class Program
{
    static int checks;
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    static void Close(float a, float b, string message) => Check(Math.Abs(a-b) < .00001f, message);
    static object Snapshot(MA x) => new {
        motion = x.actionDetail.ToString(), movement = x.moveState.ToString(), delay = x.delay,
        distanceRatio = x.dstRatio, rotation = x.dstRotation, distance = x.dstDistance,
        updateDirection = x.updateDirectionAfterAction, speed = x.speed, knockbackPower = x.knockbackPower,
        attackEvent = x.atkEffectTiming.ToString(), recoveryEvent = x.recoverEffectTiming.ToString(),
        damageEvent = x.damagedEffectTiming.ToString(), customEffect = x.customEffectRes,
        callAttackEffect = x.callAtkEffectEvent, forceEffect = x.printEffectForcely
    };
    static string Dump(IEnumerable<MA> q) => JsonSerializer.Serialize(q.Select(Snapshot), JsonOptions);
    static AAB Actor(ActionDetail motion, BehaviourDetail detail, ActionType type, Result result, string script = "") => new() {
        data = new DiceBehaviourResultData { actionDetail = motion, behaviourDetail = detail, actionType = type, range = CardRange.Near, result = result },
        result = result, view = new BattleUnitView(), infoList = new(),
        behaviourResultData = new BattleCardBehaviourResult { behaviourRawData = new DiceBehaviour { ActionScript = script } }
    };
    static AAB Attacker(int i) => Actor(new[] {ActionDetail.Slash, ActionDetail.Hit, ActionDetail.Penetrate}[i],
        new[] {BehaviourDetail.Slash, BehaviourDetail.Hit, BehaviourDetail.Penetrate}[i], ActionType.Atk, Result.Win);
    static AAB Target() {
        var a = Actor(ActionDetail.Hit, BehaviourDetail.Hit, ActionType.Atk, Result.Lose);
        a.behaviourResultData.dmgListTaken.Add(new DmgTakenData { dmg = 7 });
        a.behaviourResultData.breakListTaken.Add(new DmgTakenData { dmg = 3 });
        a.view.WorldPosition = new UnityEngine.Vector3(3, 0, 0);
        return a;
    }
    static BehaviourActionBase Action(int i) => (BehaviourActionBase)Activator.CreateInstance(Type.GetType(
        "BehaviourAction_Steria_MemoryConcerto_" + new[] {"Long", "Medium", "Final"}[i])!);
    static void Fallback(string name, BehaviourActionBase action, AAB self, AAB opp) {
        var oldSelfList = self.infoList;
        var oldOppList = opp.infoList;
        string oldOpp = Dump(opp.infoList);
        var expected = SingletonBehavior<RencounterManager>.Instance.GetMovingAction(self, opp);
        var actual = action.GetMovingAction(ref self, ref opp);
        Check(Dump(expected) == Dump(actual), name + ": exact base result");
        Check(ReferenceEquals(oldSelfList, self.infoList) && ReferenceEquals(oldOppList, opp.infoList) && Dump(opp.infoList) == oldOpp,
            name + ": no existing queue mutation");
    }
    static object VerifyWin(int i, string label, AAB target) {
        var self = Attacker(i);
        var original = SingletonBehavior<RencounterManager>.Instance.GetMovingAction(self, target);
        var targetOriginal = SingletonBehavior<RencounterManager>.Instance.GetMovingAction(target, self);
        var result = Action(i).GetMovingAction(ref self, ref target);
        Check(result.Count == 3 && target.infoList.Count == 3, label + ": aligned queues");
        Check(result.Distinct().Count() == 3 && target.infoList.Distinct().Count() == 3 && !result.Intersect(target.infoList).Any(), label + ": no aliases");
        Check(result[0].actionDetail == ActionDetail.Guard && result[0].moveState == CharMoveState.Stop, label + ": visible preparation");
        Check(target.infoList[0].actionDetail == ActionDetail.Default && target.infoList[0].moveState == CharMoveState.Stop, label + ": no early damage");
        foreach (var q in new[] {result, target.infoList}) Check(!q[0].IsExistEffectTiming() && !q[2].IsExistEffectTiming(), label + ": event-free preparation/recovery");
        Check(result.Count(x => x.atkEffectTiming == EffectTiming.PRE) == 1 && result.Count(x => x.recoverEffectTiming == EffectTiming.PRE) == 1 &&
            result.Count(x => x.damagedEffectTiming == EffectTiming.PRE) == 1 && result[1].IsExistEffectTiming(), label + ": unique attack event");
        for (int j = 0; j < 2; j++) {
            float configuredDelay = result[j+1].delay;
            result[j+1].delay = original[j].delay;
            result[j+1].SetEffectTiming(EffectTiming.NONE, EffectTiming.NONE, EffectTiming.NONE);
            Check(Dump(new[] {result[j+1]}) == Dump(new[] {original[j]}), label + ": exact native self movement " + j);
            result[j+1].delay = configuredDelay;
        }
        result[1].SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
        for (int j = 0; j < targetOriginal.Count; j++)
            Check(Dump(new[] {target.infoList[j+1]}) == Dump(new[] {targetOriginal[j]}), label + ": exact native reaction " + j);
        Check(target.infoList.Count(x => x.moveState == CharMoveState.Knockback) == targetOriginal.Count(x => x.moveState == CharMoveState.Knockback), label + ": no extra knockback");
        Close(result.Sum(x => x.delay), new[] {.80f, .50f, 1.20f}[i], label + ": minimum rhythm");
        Close(result[0].delay, new[] {.30f, .10f, .50f}[i], label + ": preparation interval");
        Close(result[0].delay, target.infoList[0].delay, label + ": synchronized preparation");
        Check(result[1].customEffectRes == null && result[1].callAtkEffectEvent && !result[1].printEffectForcely, label + ": existing effect routing");
        // Declarative event trace, not a substitute for running Unity MoveRoutine.
        float elapsed = 0;
        var segments = new List<object>();
        for (int j = 0; j < result.Count; j++) {
            var duration = Math.Max(result[j].delay, target.infoList[j].delay);
            segments.Add(new { index = j, start = elapsed, minimumDuration = duration, end = elapsed + duration,
                phase = new[] {"preparation", "attack", "recovery"}[j], self = Snapshot(result[j]), opponent = Snapshot(target.infoList[j]) });
            elapsed += duration;
        }
        return new { die = i+1, label, effectProfile = new[] {"MemorySlash", "MemoryHit", "MemoryPierce"}[i],
            minimumDuration = elapsed, segments };
    }
    static void VerifyChristasha(XDocument xml) {
        var entries = new[] {
            (9009002, 0, ActionDetail.Slash, BehaviourDetail.Slash, ActionDetail.Guard, ActionDetail.Slash, .14f, .18f),
            (9009002, 1, ActionDetail.Slash, BehaviourDetail.Slash, ActionDetail.Move, ActionDetail.Slash2, .09f, .16f),
            (9009005, 0, ActionDetail.Slash, BehaviourDetail.Slash, ActionDetail.S1, ActionDetail.Slash2, .22f, .22f),
            (9009005, 1, ActionDetail.Penetrate, BehaviourDetail.Penetrate, ActionDetail.Guard, ActionDetail.Penetrate, .18f, .21f),
            (9009007, 1, ActionDetail.Hit, BehaviourDetail.Hit, ActionDetail.S1, ActionDetail.Hit, .23f, .23f)
        };
        foreach (var (card, index, input, detail, prep, strike, windup, recovery) in entries) {
            var die = xml.Root!.Elements("Card").Single(c => (int)c.Attribute("ID") == card).Descendants("Behaviour").ElementAt(index);
            var action = (BehaviourActionBase)Activator.CreateInstance(Type.GetType("BehaviourAction_" + (string)die.Attribute("ActionScript"), true)!)!;
            foreach (var skin in new[]{"Christasha_Normal", "Christasha_Texture_v2_Projection", "Christasha"}) {
                var self = Actor(input, detail, ActionType.Atk, Result.Win);
                var target = Target();
                var appearance = self.view.charAppearance = new CharacterAppearance();
                var cloth = new Workshop.ClothCustomizeData { spritePath = "skin/" + skin + "/ClothCustom/Default.png" };
                foreach (var pose in new[]{ActionDetail.Default, prep, strike}) appearance.Outfit.dic[pose] = cloth;
                if (skin == "Christasha") {
                    Fallback("borrowed page", action, self, target);
                    continue;
                }
                var queue = action.GetMovingAction(ref self, ref target);
                Check(queue.Select(m => m.actionDetail).SequenceEqual(new[]{prep, strike, strike}), "cast queue " + skin + "/" + card);
                Check(queue.Count(m => m.IsExistEffectTiming()) == 1 && queue[1].atkEffectTiming == EffectTiming.PRE &&
                    queue[1].damagedEffectTiming == EffectTiming.PRE && queue[1].recoverEffectTiming == EffectTiming.PRE &&
                    queue[1].customEffectRes == null, "single native EffectRes and damage at strike");
                Check(target.infoList.Count == 3 && target.infoList[0].actionDetail == ActionDetail.Default, "synchronized reaction");
                Close(queue[0].delay, windup, "windup"); Close(queue[1].delay, .15f, "strike"); Close(queue[2].delay, recovery, "recovery");
                self.result = Result.Lose;
                Fallback("lost clash", action, self, Target());
                self.result = Result.Win;
                var foreign = Target(); foreign.behaviourResultData.behaviourRawData.ActionScript = "Foreign_CustomLose";
                Fallback("foreign reaction", action, self, foreign);
            }
        }
    }

    public static void Main(string[] args) {
        bool christashaOnly = args.Contains("--christasha-only");
        string root = Path.GetFullPath(args.SingleOrDefault(arg => arg != "--christasha-only") ?? ".");
        string output = Path.Combine(root, "output/memory-concerto-rhythm");
        Directory.CreateDirectory(output);
        var xml = XDocument.Load(Path.Combine(root, "SteriaBuild/SteriaModFolder/Data/CardInfo.xml"));
        if (christashaOnly) {
            VerifyChristasha(xml);
            Console.WriteLine($"CHRISTASHA_RHYTHM_PASS: {checks} assertions; five XML actions on native queues");
            return;
        }
        var dice = xml.Root!.Elements("Card").Single(c => (string)c.Attribute("ID") == "9001004").Element("BehaviourList")!.Elements("Behaviour").ToList();
        var entryTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Name.StartsWith("BehaviourAction_Steria_MemoryConcerto_") && t.IsSubclassOf(typeof(BehaviourActionBase))).ToArray();
        Check(entryTypes.Length == 3 && entryTypes.All(t => !t.IsAbstract && t.IsPublic && t.GetConstructor(Type.EmptyTypes) != null), "native registration: three public parameterless entries");
        for (int i = 0; i < 3; i++) Check("BehaviourAction_" + (string)dice[i].Attribute("ActionScript") == Action(i).GetType().Name, "XML entry resolves " + i);
        Check((string)dice[3].Attribute("ActionScript") == "" && (string)dice[3].Attribute("Motion") == "E", "fourth evasion stays native");
        var sequence = Enumerable.Range(0, 3).Select(i => VerifyWin(i, new[] {"Long", "Medium", "Final"}[i], Target())).ToList();
        for (int i = 0; i < 3; i++) {
            VerifyWin(i, "one-sided", Actor(ActionDetail.Damaged, BehaviourDetail.None, ActionType.None, Result.Lose));
            VerifyWin(i, "vs guard", Actor(ActionDetail.Guard, BehaviourDetail.Guard, ActionType.Def, Result.Lose));
            VerifyWin(i, "vs evasion", Actor(ActionDetail.Evade, BehaviourDetail.Evasion, ActionType.Def, Result.Lose));
            var mirror = Target(); mirror.behaviourResultData.behaviourRawData.ActionScript = "Steria_MemoryConcerto_Final";
            VerifyWin(i, "mirrored page", mirror);
            var actor = Attacker(i); var opp = Target();
            actor.result = Result.Lose; opp.result = Result.Win; Fallback("lose", Action(i), actor, opp);
            actor.result = opp.result = Result.Draw; Fallback("draw", Action(i), actor, opp);
            actor = Attacker(i); opp = Target(); opp.data.range = CardRange.Far; Fallback("near-vs-far", Action(i), actor, opp);
            actor.data.range = CardRange.Far; Fallback("far", Action(i), actor, opp);
            actor = Attacker(i); actor.data.actionDetail = ActionDetail.S1; Fallback("unknown motion", Action(i), actor, Target());
            actor = Attacker(i); actor.data.actionType = ActionType.Def; Fallback("wrong type", Action(i), actor, Target());
            actor = Attacker(i); actor.data.range = CardRange.FarArea; Fallback("mass", Action(i), actor, Target());
            actor = Attacker(i); actor.behaviourResultData.isBlockedDice = true; Fallback("blocked", Action(i), actor, Target());
            actor = Attacker(i); actor.data.skip = true; Fallback("skip", Action(i), actor, Target());
            opp = Target(); opp.behaviourResultData.behaviourRawData.ActionScript = "Foreign_CustomLose"; Fallback("custom script", Action(i), Attacker(i), opp);
            opp = Target(); opp.infoList.Add(new MA(ActionDetail.S1, CharMoveState.Custom)); Fallback("existing opponent queue", Action(i), Attacker(i), opp);
            opp = Target(); opp.startMoveRoutineEvent = () => {}; Fallback("foreign event", Action(i), Attacker(i), opp);
            actor = Attacker(i); opp = Target(); var sharedAction = Action(i);
            var once = sharedAction.GetMovingAction(ref actor, ref opp); actor.infoList = once;
            Fallback("reentry", sharedAction, actor, opp);
            actor = Attacker(i); opp = Target(); var twice = sharedAction.GetMovingAction(ref actor, ref opp);
            Check(Dump(once) == Dump(twice) && !once.Intersect(twice).Any(), "reroll/second owner independently reuses same entry");
        }
        Parallel.For(0, 12, i => { var a = Attacker(i % 3); var b = Target(); var q = Action(i % 3).GetMovingAction(ref a, ref b); if (q.Count != 3) throw new Exception("concurrent instances"); });
        var evade = Actor(ActionDetail.Evade, BehaviourDetail.Evasion, ActionType.Def, Result.Win);
        var evadeOpp = Target();
        Fallback("evade accidentally assigned final entry", Action(2), evade, evadeOpp);
        var evadeSelfQueue = new BehaviourActionBase().GetMovingAction(ref evade, ref evadeOpp);
        var evadeOppQueue = new BehaviourActionBase().GetMovingAction(ref evadeOpp, ref evade);
        File.WriteAllText(Path.Combine(output, "queues.json"), JsonSerializer.Serialize(new {
            schema = 1, source = "linked production GetMovingAction + exact extracted native queue methods",
            boundary = "Nominal game-time minima; not recorded gameplay. Movement completion, frame yields, dice UI gaps, camera and sounds are not simulated.",
            sequence, nativeFourthEvasion = new { self = evadeSelfQueue.Select(Snapshot), opponent = evadeOppQueue.Select(Snapshot) },
            checks
        }, JsonOptions));
        Console.WriteLine($"RHYTHM_PASS: {checks} assertions; real production classes/native queues; XML registration, events, movement, fallback, reroll/concurrency; queues.json written");
    }
}
