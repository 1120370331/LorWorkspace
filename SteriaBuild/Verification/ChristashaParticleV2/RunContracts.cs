using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Battle.DiceAttackEffect;
using Steria.Vfx;
using UnityEngine;

internal static class RunContracts
{
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly List<string> Checks = new List<string>();

    private static void Main(string[] args)
    {
        Prefabs();
        Melee();
        bool meleeOnly = args.Length > 1 && args[1] == "melee";
        if (!meleeOnly)
        {
            Projectile();
            NativeMass();
            EmptyAndRepeatedMass();
            CancellationAndMissingAssets();
        }
        File.WriteAllLines(Path.Combine(args[0], "checks.txt"), Checks);
        Console.WriteLine("CHRISTASHA_V2_MANAGED_NATIVE_CONTRACT_PASS " + Checks.Count + " assertions; scope=" + (meleeOnly ? "melee" : "all") + "; source-linked native contracts and adapters; no GPU claims");
    }

    private static void Prefabs(bool missing = false)
    {
        Resources.Missing = missing;
        ChristashaNativeParticleLibrary.Unsupported.Clear();
        typeof(ChristashaParticleInstance).GetField("_library", Fields).SetValue(null, null);
        typeof(ChristashaParticleInstance).GetField("_loadAttempted", Fields).SetValue(null, false);
        // Recording Texture2D/ImageConversion boundary checks loader flags without
        // pretending these bytes are a decoded production brush or a GPU test.
        string directory = Path.GetDirectoryName(typeof(RunContracts).Assembly.Location);
        string brush = Path.Combine(Directory.GetParent(directory).FullName, "Resource", "Effects", "ChristashaNative", "tide-brush-imagegen-v1.png");
        Directory.CreateDirectory(Path.GetDirectoryName(brush));
        File.WriteAllBytes(brush, new byte[] { 1 });
    }

    private static BattleUnitView Actor(float x, float map = 3f, bool right = false)
    {
        var view = new GameObject("Actor").AddComponent<BattleUnitView>();
        view.transform.position = new Vector3(x, 0f, 0f);
        var rotation = new GameObject("Facing").transform;
        rotation.SetParent(view.transform, false);
        rotation.localScale = Vector3.one * map;
        rotation.localRotation = Quaternion.Euler(0f, right ? 180f : 0f, 0f);
        view.characterRotationCenter = rotation;
        var appearance = new GameObject("Appearance").AddComponent<CharacterAppearance>();
        appearance.transform.SetParent(rotation, false);
        var torso = new GameObject("Torso").transform;
        torso.SetParent(appearance.transform, false);
        torso.localPosition = new Vector3(0f, .5f, 0f);
        appearance.atkEffectRoot = view.atkEffectRoot = torso;
        foreach (string suffix in new[] { "J", "H", "Z", "F", "G" })
        {
            var pivot = new GameObject("Pivot_" + suffix).transform;
            pivot.SetParent(appearance.transform, false);
            pivot.localPosition = new Vector3(-.45f, .7f, 0f);
            pivot.localScale = new Vector3(1.15f, .9f, 1f);
            pivot.localRotation = Quaternion.Euler(0f, 0f, 17f);
            typeof(CharacterAppearance).GetField("atkEffectPivot_" + suffix).SetValue(appearance, pivot);
        }
        view.charAppearance = appearance;
        view.model = new BattleUnitModel { view = view, currentDiceAction = new BattlePlayingCardDataInUnitModel { currentBehavior = new BattleDiceBehavior() } };
        return view;
    }

    private static DiceAttackEffect Dice(string kind, BattleUnitView owner, BattleUnitView target, float roll = 0f)
    {
        Type type = typeof(DiceAttackEffect_Steria_ChristashaParticleV2).Assembly.GetType("DiceAttackEffect_Steria_ChristashaV2" + kind);
        var effect = (DiceAttackEffect)new GameObject("UntrustedContainsH").AddComponent(type);
        effect.Initialize(owner, target, 1f);
        effect.SetScale(roll);
        return effect;
    }

    private static void Melee()
    {
        foreach (bool right in new[] { false, true })
        foreach (float map in new[] { 3f, 2.25f })
        foreach (var item in new[] { new { kind = "SlashH", action = ActionDetail.Slash }, new { kind = "SlashV", action = ActionDetail.Slash }, new { kind = "Hit", action = ActionDetail.Hit } })
        {
            var owner = Actor(right ? -5 : 5, map, right);
            var target = Actor(right ? 8 : -8, map, !right);
            DiceAttackEffect effect = Dice(item.kind, owner, target, 1f);
            Transform pivot = owner.charAppearance.GetSpecialMotionPivot(item.action);
            Check(effect.transform.parent == pivot && Close(effect.transform.TransformPoint(Vector3.right), pivot.TransformPoint(Vector3.right)), item.kind + " native special/fallback pivot matrix at scale one, one mirror map=" + map + " right=" + right);
            Check(!effect.GetComponent<SpriteRenderer>().enabled && !effect.GetComponent<Animator>().enabled && !effect.HasDamagedEffect, item.kind + " native empty renderers disabled and result ownership preserved");
            var visual = ChristashaNativeParticlePlayback.Started.Last();
            float artScale = (item.action == ActionDetail.Slash || item.action == ActionDetail.Hit || item.action == ActionDetail.Penetrate) ? .35f : 1f;
            Check(Close(visual.Root.transform.TransformPoint(Vector3.right), pivot.TransformPoint(Vector3.right * artScale)),
                item.kind + " independent art normalization=" + artScale);
            effect.SetScale(4f);
            Check(Close(effect.transform.TransformPoint(Vector3.right), pivot.TransformPoint(Vector3.right)), "native SetScale remains a no-op across repeated roll inputs");
            Check(visual.Root.layer == 8 && visual.Root.GetComponent<ParticleSystemRenderer>().sortingLayerName == "Particle", "Effect object layer and separate Particle sorting layer");
            Tick(effect, .2f);
            effect.SetDestroyTime(2f);
            Tick(effect, .6f);
            Check(effect.gameObject != null && Math.Abs(visual.Age - .8f) < .0001f, "SetDestroyTime preserves independent visual clock");
            Tick(effect, 1.21f);
            Check(effect.gameObject == null && visual.Stopped && visual.Root == null, "native destruction releases owned particle root");
        }
        var unprepared = Dice("Hit", Actor(3), null);
        Check(unprepared.gameObject != null, "melee allows unprepared target");
        UnityEngine.Object.Destroy(unprepared.gameObject);
        foreach (bool right in new[] { false, true })
        {
            var owner = Actor(right ? -5f : 5f, 3f, right);
            var target = Actor(right ? 8f : -8f, 3f, !right);
            // The default branch above deliberately uses fallback atkEffectRoot,
            // not the different ordinary J pivot. Here prove explicit special pivots too.
            owner.charAppearance.specialPivots[ActionDetail.Slash] = owner.charAppearance.atkEffectPivot_J;
            var horizontal = Dice("SlashH", owner, target, 1f);
            var h = ChristashaNativeParticlePlayback.Started.Last().Root;
            var vertical = Dice("SlashV", owner, target, 1f);
            var v = ChristashaNativeParticlePlayback.Started.Last().Root;
            Check(h.name == v.name && new[] { Vector3.zero, Vector3.right, new Vector3(0f, 1f, 0f) }.All(point => Close(h.transform.TransformPoint(point), v.transform.TransformPoint(point))),
                "SlashH/V share accepted Slash posture with no extra rotation, right=" + right);
            Check(horizontal.transform.parent == owner.charAppearance.atkEffectPivot_J &&
                Close(h.transform.TransformPoint(Vector3.right), owner.charAppearance.atkEffectPivot_J.TransformPoint(Vector3.right * .35f)),
                "explicit special-motion pivot keeps full rotated/scaled parent matrix, right=" + right);
            UnityEngine.Object.Destroy(horizontal.gameObject);
            UnityEngine.Object.Destroy(vertical.gameObject);
        }
        var library = ChristashaNativeParticleLibrary.Created.Last();
        Check(!library.Disposed && library.Brush != null && library.Brush.linear && !library.Brush.mipChain && library.Brush.readable && library.Brush.wrapMode == TextureWrapMode.Clamp,
            "instance cleanup preserves cached library/brush; loader uses linear readable Clamp texture without mips");
        Check(ChristashaNativeParticleLibrary.Created.Count == 1 && Resources.Requests.Count > 0,
            "instances share one native library and resolve through game Resources");
        int started = ChristashaNativeParticlePlayback.Started.Count;
        ChristashaNativeParticleLibrary.Unsupported.Add("Pierce");
        var unsupported = Dice("PierceNear", Actor(5), Actor(-5));
        Check(ChristashaNativeParticlePlayback.Started.Count == started, "unsupported native recipe produces no substitute visual or old bundle fallback");
        Tick(unsupported, 1f);
        Check(unsupported.gameObject == null, "unsupported recipe retains caller-owned cleanup deadline");
        ChristashaNativeParticleLibrary.Unsupported.Clear();
    }

    private static void Projectile()
    {
        foreach (bool right in new[] { false, true })
        {
            var owner = Actor(right ? -12 : 12, 3, right);
            var target = Actor(right ? 10 : -10, 2.25f, !right);
            int first = ChristashaNativeParticlePlayback.Started.Count;
            var effect = Dice("PierceFar", owner, target, 1f);
            var visual = ChristashaNativeParticlePlayback.Started.Last();
            Check(effect.transform.parent == null && Close(visual.Root.transform.position, owner.charAppearance.GetSpecialMotionPivot(ActionDetail.Fire).position), "F pivot explicitly chosen; neutral projectile root");
            Check(Math.Abs(Math.Abs(visual.Root.transform.lossyScale.x) - 1f) < .0001f, "world projectile excludes actor map scale and ignores roll");
            owner.transform.position += new Vector3(2, 1, 0);
            target.transform.position += new Vector3(-3, 4, 0);
            Tick(effect, .13f);
            Check(Close(visual.Root.transform.position, Vector3.Lerp(owner.charAppearance.GetSpecialMotionPivot(ActionDetail.Fire).position, target.atkEffectRoot.position, .5f)), "projectile uses both live world endpoints including moved/sloped target");
            Tick(effect, .13f);
            Check(visual.Stopped && ChristashaNativeParticlePlayback.Started.Count == first + 1, "arrival cleans projectile without inventing confirmed-hit Point");
            UnityEngine.Object.Destroy(effect.gameObject);
        }
        var removedTarget = Actor(-5);
        var interrupted = Dice("PierceFar", Actor(5), removedTarget);
        var interruptedVisual = ChristashaNativeParticlePlayback.Started.Last();
        UnityEngine.Object.Destroy(removedTarget.gameObject);
        Tick(interrupted, .05f);
        Check(interruptedVisual.Stopped, "destroyed projectile target clears owned projectile");
        UnityEngine.Object.Destroy(interrupted.gameObject);
    }

    private static BattleFarAreaPlayManager Manager(BattleUnitView owner, params BattleUnitView[] targets)
    {
        var manager = new BattleFarAreaPlayManager { attacker = owner.model };
        Singleton<BattleFarAreaPlayManager>.Instance = manager;
        foreach (var target in targets) manager.victims.Add(new BattleFarAreaPlayManager.VictimInfo { unitModel = target.model, playingCard = target.model.currentDiceAction });
        return manager;
    }

    private static FarAreaEffect StartMass(BattleFarAreaPlayManager manager)
    {
        var effect = new BehaviourAction_Steria_ChristashaXiyinV2().SetFarAreaAtkEffect(manager.attacker);
        manager._attackerEffect = effect;
        return effect;
    }

    private static void NativeMass()
    {
        foreach (bool right in new[] { false, true })
        {
            int first = ChristashaNativeParticlePlayback.Started.Count;
            var owner = Actor(right ? -22 : 22, 3, right);
            var success = Actor(right ? 4 : -4, 2.25f, !right);
            var defense = Actor(right ? 20 : -20, 3, !right);
            success.model.currentDiceAction.currentBehavior.DiceResultValue = 3;
            defense.model.currentDiceAction.currentBehavior.DiceResultValue = 20;
            var manager = Manager(owner, success, defense);
            var effect = StartMass(manager);
            Check(effect.isRunning && !effect.HasIndependentAction, "mass starts native gate, native manager owns combat");
            Tick(effect, .31f); manager.Tick(.31f);
            var area = ChristashaNativeParticlePlayback.Started.Last();
            Check(area.Root.name == "ChristashaNative_Area" && effect.isRunning, "one Area begins at .3 before .5 release");
            Check(Close(area.Root.transform.position, new Vector3(right ? -1f : 1f, 0f, 0f)) && Math.Abs(area.Root.transform.localScale.x - 44f / ChristashaNativeMassRecipes.AreaDesignWidth) < .0001f, "area world extent covers caster and every native victim including defenses");
            Check(area.Root.transform.TransformPoint(new Vector3(0, 1, 0)).y > area.Root.transform.position.y, "area facing mirror preserves upward axis");
            success.transform.position += new Vector3(1f, 0f, 0f);
            Tick(effect, .20f); manager.Tick(.20f);
            Check(!effect.isRunning && success.model.damageCalls == 1 && manager._actionPreDelay == 0f, "own .5 release gate controls native callback without default one-second delay");
            var points = ChristashaNativeParticlePlayback.Started.Skip(first).Where(x => x.Root.name == "ChristashaNative_Point").ToArray();
            Check(success.model.damageCalls == 1 && defense.model.damageCalls == 0 && points.Length == 1, "real native ActionPhase settles success once and suppresses defended Point");
            Check(Close(points[0].Root.transform.position, success.atkEffectRoot.position) && Close(points[0].Root.transform.localScale, Vector3.one), "Point uses successful target's callback-time world point without negative/map scale");
            effect.GiveDamageFromManager(new List<BattleUnitModel> { success.model, success.model });
            Check(ChristashaNativeParticlePlayback.Started.Count == first + 3, "duplicate callback does not spawn or replay points");
            Tick(effect, .7f); manager.Tick(.7f);
            Check(effect.IsDoneEffect && manager._phase == BattleFarAreaPlayManager.Phase.RollDice && owner.model.currentDiceAction.currentBehavior.endCalls == 1, "native manager reaches RollDice after visual tail without timeout");
            Check(ChristashaNativeParticlePlayback.Started.Skip(first).All(x => x.Stopped && x.Root == null), "mass releases all cast/area/point instance ownership");
            manager.NextCardNativeResetFixture();
            Check(manager._actionPreDelay == 1f && manager._actionPostDelay == .5f, "native Start/End reset assignments restore next-card default timing");
        }
    }

    private static void EmptyAndRepeatedMass()
    {
        var owner = Actor(12);
        var defense = Actor(-16);
        defense.model.currentDiceAction.currentBehavior.DiceResultValue = 20;
        for (int dice = 0; dice < 3; dice++)
        {
            owner.model.currentDiceAction.currentBehavior = new BattleDiceBehavior();
            int first = ChristashaNativeParticlePlayback.Started.Count;
            var manager = Manager(owner, defense);
            var effect = StartMass(manager);
            for (int frame = 0; frame < 105; frame++) { Tick(effect, 1f / 60f); manager.Tick(1f / 60f); }
            Check(effect.IsDoneEffect && manager._phase == BattleFarAreaPlayManager.Phase.RollDice, "copied mass dice " + dice + " has independent releasable lifetime");
            Check(ChristashaNativeParticlePlayback.Started.Skip(first).Count() == 2 && defense.model.damageCalls == 0, "all defended: cast and one area remain; zero success points; finishes");
        }
        var units = Enumerable.Range(0, 20).Select(i => Actor(-i - 1).model).ToList();
        var many = StartMass(Manager(owner));
        int start = ChristashaNativeParticlePlayback.Started.Count;
        units.Insert(0, units[0]);
        many.GiveDamageFromManager(units);
        Check(ChristashaNativeParticlePlayback.Started.Skip(start).Count(x => x.Root.name.EndsWith("Point")) == 16, "unique successful targets capped at sixteen despite duplicated entries");
        Tick(many, 1.5f);
        var empty = StartMass(Manager(owner));
        empty.GiveDamageFromManager(null);
        Tick(empty, 1.5f);
        Check(empty.IsDoneEffect, "null callback list also terminates");
    }

    private static void CancellationAndMissingAssets()
    {
        foreach (string mode in new[] { "owner-dead", "owner-hidden", "card-changed", "dice-changed", "effect-disabled", "effect-destroyed", "no-callback" })
        {
            var owner = Actor(5);
            int first = ChristashaNativeParticlePlayback.Started.Count;
            var effect = StartMass(Manager(owner));
            if (mode == "owner-dead") owner.model.dead = true;
            if (mode == "owner-hidden") owner.gameObject.SetActive(false);
            if (mode == "card-changed") owner.model.currentDiceAction = new BattlePlayingCardDataInUnitModel();
            if (mode == "dice-changed") owner.model.currentDiceAction.currentBehavior = new BattleDiceBehavior();
            if (mode == "effect-disabled") effect.gameObject.SetActive(false);
            if (mode == "effect-destroyed") UnityEngine.Object.Destroy(effect.gameObject);
            Tick(effect, mode == "no-callback" ? 1.81f : .01f);
            Check(effect.IsDoneEffect && !effect.isRunning && ChristashaNativeParticlePlayback.Started.Skip(first).All(x => x.Stopped), mode + " cannot block native manager and clears all owned instances");
        }
        Prefabs(true);
        var missingOwner = Actor(5);
        var missing = StartMass(Manager(missingOwner));
        Check(!missing.isRunning, "missing prefab releases native gate immediately");
        missing.GiveDamageFromManager(new List<BattleUnitModel>());
        Check(missing.IsDoneEffect, "missing resources and empty callback finish without native timeout");
        var diceMissing = Dice("SlashH", missingOwner, null);
        Tick(diceMissing, 1f);
        Check(diceMissing.gameObject == null, "missing melee resource preserves native cleanup deadline");
        Prefabs();
    }

    private static void Tick(MonoBehaviour effect, float dt)
    {
        if (effect == null || effect.gameObject == null || !effect.gameObject.activeInHierarchy) return;
        Time.deltaTime = dt;
        effect.GetType().GetMethod("Update", Fields).Invoke(effect, null);
    }
    private static bool Close(Vector3 a, Vector3 b) { return Vector3.Distance(a, b) < .0001f; }
    private static void Check(bool pass, string label) { if (!pass) throw new Exception("FAIL: " + label); Checks.Add("PASS: " + label); }
}
