using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using HarmonyLib;
using LOR_DiceSystem;

class Program
{
    static string Root;
    static int Checks;
    static MethodInfo Select, Prefix;
    static readonly List<string> Played = new List<string>();
    static bool PlaybackSucceeds = true;
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); Checks++; }
    static bool FakePlayback(string cue, ref bool __result) { Played.Add(cue); __result = PlaybackSucceeds; return false; }
    static DiceBehaviour Raw(BehaviourDetail detail = BehaviourDetail.Slash)
    {
        return new DiceBehaviour { Type = BehaviourType.Atk, Detail = detail, EffectRes = "Steria_AnhierTextureMemory" + (detail == BehaviourDetail.Penetrate ? "Pierce" : detail.ToString()) };
    }
    static DiceBehaviourResultData ResultFor(BehaviourDetail detail = BehaviourDetail.Slash)
    {
        return new DiceBehaviourResultData { result = Result.Win, actionType = ActionType.Atk, range = CardRange.Near,
            behaviourDetail = detail, actionDetail = detail == BehaviourDetail.Slash ? ActionDetail.Slash : detail == BehaviourDetail.Hit ? ActionDetail.Hit : ActionDetail.Penetrate };
    }
    static string Choose(int id, DiceBehaviour raw, DiceBehaviourResultData result, string package = "SteriaBuilding",
        DiceBehaviourResultData opponent = default(DiceBehaviourResultData), bool opponentHas = false)
    {
        return (string)Select.Invoke(null, new object[] { new LorId(package, id), raw, result, opponent, opponentHas, result.actionDetail });
    }
    static BattleCardBehaviourResult CardResult(BehaviourDetail detail)
    {
        var model = new BattleDiceCardModel();
        typeof(BattleDiceCardModel).GetField("_xmlData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, new DiceCardXmlInfo(new LorId("SteriaBuilding", 9001004)));
        return new BattleCardBehaviourResult { cardModel = model, hasBehaviour = true, behaviourRawData = Raw(detail), diceBehaviourResult = ResultFor(detail) };
    }
    static void Main(string[] args)
    {
        Root = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, evt) => {
            string name = new AssemblyName(evt.Name).Name + ".dll";
            foreach (string dir in new[] { Path.Combine(Root,"SteriaBuild/bin/Release/net472"), Path.Combine(Root,"SteriaBuild/Libs"), @"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Managed" })
                if (File.Exists(Path.Combine(dir,name))) return Assembly.LoadFrom(Path.Combine(dir,name));
            return null;
        };
        try { Run(false); Console.WriteLine("MEMORY_FLAME_POLICY_PASS: " + Checks + " checks; real compiled mod/game types and production PCM reader; Unity dispatch checked separately"); }
        catch (Exception ex) { Console.WriteLine(ex); Environment.ExitCode = 1; }
    }
    public static void RunInUnity(string root)
    {
        Root = root;
        Run(true);
        UnityEngine.Debug.Log("MEMORY_FLAME_UNITY_PASS: " + Checks + " checks; real Harmony PrintSound dispatch, native player instances, exact clips and FX volume");
    }
    static void Run(bool unity)
    {
        Assembly mod = typeof(SkillPcmWave).Assembly;
        Type helper = mod.GetType("Steria.MemoryFlameAudio", true), patch = mod.GetType("Steria.MemoryFlamePrintSoundPatch", true);
        Select = AccessTools.Method(helper,"Select"); Prefix = AccessTools.Method(patch,"Prefix");
        var xml = XDocument.Load(Path.Combine(Root,"SteriaBuild/SteriaModFolder/Data/CardInfo.xml"));
        int routed = 0;
        foreach (var card in xml.Root.Elements("Card"))
        foreach (var die in card.Elements("BehaviourList").Elements("Behaviour"))
        {
            string effect = (string)die.Attribute("EffectRes");
            if (effect != "Steria_AnhierTextureMemorySlash" && effect != "Steria_AnhierTextureMemoryHit" && effect != "Steria_AnhierTextureMemoryPierce") continue;
            var detail = (BehaviourDetail)Enum.Parse(typeof(BehaviourDetail),(string)die.Attribute("Detail"));
            var raw = Raw(detail);raw.EffectRes = effect;raw.ActionScript = (string)die.Attribute("ActionScript");
            string expected = detail == BehaviourDetail.Penetrate ? "pierce" : detail.ToString().ToLowerInvariant();
            Check(Choose((int)card.Attribute("ID"), raw, ResultFor(detail)) == expected,"XML memory binding");routed++;
        }
        Check(routed == 11,"exact eleven approved memory attack dice");
        foreach (int id in new[] {9001001,9001006,9001007,9001008,9001011,9007001,9008001})
            Check(Choose(id,Raw(),ResultFor()) == null,"foreign/theme/guard/instance card excluded");
        foreach (string package in new[] {"","OtherMod"}) Check(Choose(9001004,Raw(),ResultFor(),package)==null,"package isolation");
        foreach (Result result in new[] {Result.Lose,Result.Draw}) { var r=ResultFor();r.result=result;Check(Choose(9001004,Raw(),r)==null,"loss/draw remains native"); }
        foreach (CardRange range in new[] {CardRange.Far,CardRange.FarArea,CardRange.Instance}) { var r=ResultFor();r.range=range;Check(Choose(9001004,Raw(),r)==null,"range exclusion"); }
        var state=ResultFor();state.skip=true;Check(Choose(9001004,Raw(),state)==null,"skipped die");
        state=ResultFor();state.actionType=ActionType.Def;Check(Choose(9001004,Raw(),state)==null,"defense");
        var data=Raw();data.Type=BehaviourType.Standby;Check(Choose(9001004,data,ResultFor())==null,"unapproved counter die");
        data=Raw();data.EffectRes="Steria_TideSlash";Check(Choose(9001004,data,ResultFor())==null,"wrong theme");
        data=Raw();data.ActionScript="ForeignSpecial";Check(Choose(9001004,data,ResultFor())==null,"foreign action script");
        var far=ResultFor();far.range=CardRange.Far;Check(Choose(9001004,Raw(),ResultFor(),"SteriaBuilding",far,true)==null,"native projectile clash");
        Check(Choose(9001004,Raw(),ResultFor(),"SteriaBuilding",far,false)=="slash","one-sided target does not create ranged clash");
        foreach (var cue in new[] {Tuple.Create("slash",20286),Tuple.Create("hit",24255),Tuple.Create("pierce",15876)})
        {
            string path=Path.Combine(Root,"SteriaBuild/SteriaModFolder/Resource/CustomAudio/MemoryFlame",cue.Item1+".wav");
            var pcm=SkillPcmWave.ReadStereo44100(path,cue.Item2);
            Check(pcm.Length==cue.Item2*2&&pcm.All(x=>Math.Abs(x)<1),"real production PCM reader accepts exact cue");
            bool rejected=false;try{SkillPcmWave.ReadStereo44100(path,cue.Item2+1);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"wrong duration rejected");
        }
        if (!unity) return;
        var harmony=new Harmony("Steria.MemoryFlame.Verification");
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            MethodInfo native=AccessTools.Method(typeof(RencounterManager),"PrintSound");
            Check(Harmony.GetPatchInfo(native).Prefixes.Any(p=>p.PatchMethod.DeclaringType==patch),"real private game hook installed");
            harmony.Patch(AccessTools.Method(helper,"TryPlay"),prefix:new HarmonyMethod(typeof(Program),nameof(FakePlayback)));
            var manager=FormatterServices.GetUninitializedObject(typeof(RencounterManager));
            var opponent=new BattleCardBehaviourResult { hasBehaviour=false };
            foreach (var detail in new[] {BehaviourDetail.Slash,BehaviourDetail.Hit,BehaviourDetail.Penetrate})
            {
                var result=CardResult(detail);
                native.Invoke(manager,new object[] {null,result,opponent,result.diceBehaviourResult.actionDetail});
            }
            Check(Played.SequenceEqual(new[] {"slash","hit","pierce"}),"real PrintSound dispatches one sound per attack and suppresses native success");
            PlaybackSucceeds=false;
            var valid=CardResult(BehaviourDetail.Slash);
            Check((bool)Prefix.Invoke(null,new object[] {null,valid,opponent,ActionDetail.Slash}),"failed playback retains native sound");
            valid.isBlockedDice=true;int before=Played.Count;
            Check((bool)Prefix.Invoke(null,new object[] {null,valid,opponent,ActionDetail.Slash})&&Played.Count==before,"blocked die adds no sound");
            valid.hasBehaviour=false;valid.isBlockedDice=false;
            native.Invoke(manager,new object[] {null,valid,opponent,ActionDetail.Slash});
            Check(Played.Count==before,"no-behavior original path remains callable");
            harmony.Unpatch(AccessTools.Method(helper,"TryPlay"), HarmonyPatchType.Prefix, harmony.Id);
            NativeAudioSmoke(native, manager, opponent);
        }
        finally { harmony.UnpatchSelf(); }
    }

    static void NativeAudioSmoke(MethodInfo native, object manager, BattleCardBehaviourResult opponent)
    {
        var root = new UnityEngine.GameObject("MemoryFlameNativeAudioSmoke");
        var soundManager = root.AddComponent<BattleSoundManager>();
        AccessTools.Field(typeof(BattleSoundManager), "volumeEffectSound").SetValue(soundManager, .4f);
        AccessTools.Field(typeof(BattleSoundManager), "volumeFxRatio").SetValue(soundManager, 1f);
        var listener = new UnityEngine.GameObject("FixtureListener"); listener.AddComponent<UnityEngine.AudioListener>();
        var actor = new UnityEngine.GameObject("FixtureView"); actor.SetActive(false);
        var view = actor.AddComponent<BattleUnitView>();
        var appearance = actor.AddComponent<CharacterAppearance>();
        var sound = actor.AddComponent<CharacterSound>();
        AccessTools.Field(typeof(CharacterAppearance), "_soundInfo").SetValue(appearance, sound);
        view.charAppearance = appearance;
        var prefabObject = new UnityEngine.GameObject("FixtureNativeSoundPrefab");
        var prefabSource = prefabObject.AddComponent<UnityEngine.AudioSource>();
        prefabSource.playOnAwake = false;
        var prefab = prefabObject.AddComponent<BattleEffectSound>();
        AccessTools.Field(typeof(CharacterSound), "_soundPrefab").SetValue(sound,prefab);
        try
        {
            var expected = new[] {20286,24255,15876}; int count=0;
            foreach (var detail in new[] {BehaviourDetail.Slash,BehaviourDetail.Hit,BehaviourDetail.Penetrate})
            {
                var result=CardResult(detail);
                native.Invoke(manager,new object[] {view,result,opponent,result.diceBehaviourResult.actionDetail});
                var sources=root.GetComponentsInChildren<UnityEngine.AudioSource>();
                Check(sources.Length==count+1,"one native player per real PrintSound event");
                var source=sources.Last();
                Check(source.clip!=null && source.clip.samples==expected[count] && source.clip.channels==2,"native AudioClip contains approved PCM");
                Check(Math.Abs(source.volume-.4f)<.0001f && !source.loop && source.pitch==1,"native FX volume once, no looping or pitch retiming");
                count++;
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(actor);
            UnityEngine.Object.DestroyImmediate(prefabObject);
            UnityEngine.Object.DestroyImmediate(listener);
        }
    }
}
