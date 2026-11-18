// Game data stand-ins expose only the native ActionPhase dependencies. The
// ActionPhase, DiceAttackEffect, and FarAreaEffect methods come from repository
// decompilation without behavioral edits; production adapters are source-linked.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ActionDetail { Default, Guard, Evade, Damaged, Slash, Penetrate, Hit, Move, Standing, NONE, Fire, Aim, Special, S1, S2, S3 }
namespace Workshop
{
    public class ClothCustomizeData { public string spritePath; }
    public class WorkshopSkinDataSetter : MonoBehaviour
    {
        public Dictionary<ActionDetail, ClothCustomizeData> dic = new Dictionary<ActionDetail, ClothCustomizeData>();
    }
}
public enum CardRange { FarArea, FarAreaEach }
public enum DialogType { DEATH }
public class BattleUnitModel
{
    public BattleUnitView view;
    public BattlePlayingCardDataInUnitModel currentDiceAction;
    public bool dead;
    public int faction, hp = 100, damageCalls;
    public BreakDetail breakDetail = new BreakDetail();
    public bool IsDead() { return dead; }
}
public class BreakDetail { public int breakGauge = 50; }
public class DiceEffectManager
{
    public Battle.DiceAttackEffect.DiceAttackEffect CreateBehaviourEffect() { return null; }
    public void CreateDamagedEffectByBehaviour() { }
}
public class BattleUnitView : MonoBehaviour
{
    public BattleUnitModel model;
    public CharacterAppearance charAppearance;
    public Transform atkEffectRoot, characterRotationCenter;
    public Vector3 WorldPosition { get { return transform.position; } }
    public void DisplayDlg(DialogType type, List<BattleUnitModel> units) { }
}
public class CharacterAppearance : MonoBehaviour
{
    public Transform atkEffectRoot, atkEffectPivot_G, atkEffectPivot_J, atkEffectPivot_Z, atkEffectPivot_H, atkEffectPivot_F;
    public readonly Dictionary<ActionDetail, Transform> specialPivots = new Dictionary<ActionDetail, Transform>();
    public Transform GetSpecialMotionPivot(ActionDetail action)
    {
        Transform pivot;
        return specialPivots.TryGetValue(action, out pivot) ? pivot : atkEffectRoot;
    }
    public Transform GetAtkEffectPivot(ActionDetail action)
    {
        Transform pivot = action == ActionDetail.Slash ? atkEffectPivot_J : action == ActionDetail.Hit ? atkEffectPivot_H :
            action == ActionDetail.Penetrate ? atkEffectPivot_Z : action == ActionDetail.Fire ? atkEffectPivot_F : action == ActionDetail.Guard ? atkEffectPivot_G : atkEffectRoot;
        return pivot ?? atkEffectRoot;
    }
    private ActionDetail current;
    public ActionDetail GetCurrentMotionDetail() { return current; }
    public object GetCharacterMotion(ActionDetail action) { return this; }
    public void ChangeMotion(ActionDetail action) { current = action; }
}
public class BattlePlayingCardDataInUnitModel
{
    public BattleDiceBehavior currentBehavior;
    public CardModel card = new CardModel();
    public List<BattleDiceBehavior> GetDiceBehaviorList() { return new List<BattleDiceBehavior> { currentBehavior }; }
}
public class CardModel { public Spec GetSpec() { return new Spec(); } }
public class Spec { public CardRange Ranged = CardRange.FarAreaEach; }
public class DiceDefinition { public ActionDetail MotionDetail = ActionDetail.Slash; }
public class BattleDiceBehavior
{
    public int DiceResultValue = 10, Index, endCalls;
    public DiceDefinition behaviourInCard = new DiceDefinition();
    public void GiveDamage(BattleUnitModel target) { target.damageCalls++; }
    public void OnEndFarAreaBehaviourAtk(List<BattleUnitModel> success, List<BattleUnitModel> fail) { endCalls++; }
}
public static class MotionConverter { public static ActionDetail MotionToAction(ActionDetail action) { return action; } }
public class BattleManagerUI { public UnitList ui_unitListInfoSummary = new UnitList(); }
public class UnitList { public void UpdateCharacterProfile(params object[] ignored) { } }
public static class Util { public static void DebugEditorLog(string text) { throw new Exception("Native manager timeout: " + text); } }
public class Singleton<T> where T : new() { public static T Instance = new T(); }
public class SingletonBehavior<T> where T : new() { public static T Instance = new T(); }
public class AutoDestruct : MonoBehaviour { public float time; public void DestroyWhenDisable() { } }
public class BehaviourActionBase { protected BattleUnitModel _self; public virtual float GetAreaAtkWaitActionDelay() { return .5f; } public virtual FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self) { return null; } }
public partial class BattleFarAreaPlayManager
{
    public enum Phase { Action, RollDice }
    public Phase _phase;
    public float _elapsed, _elapsedActionDelay;
    public float _actionPreDelay = 1f, _actionPostDelay = .5f;
    public FarAreaEffect _attackerEffect;
    public BattleUnitModel attacker;
    public List<VictimInfo> victims = new List<VictimInfo>(), defenseVictims = new List<VictimInfo>();
    public void Tick(float dt) { if (_phase == Phase.Action) ActionPhase(dt); }
    public class VictimInfo
    {
        public BattleUnitModel unitModel;
        public BattlePlayingCardDataInUnitModel playingCard;
        public bool cardDestroyed;
        public List<int> destroyedDicesIndex = new List<int>();
    }
}
namespace Steria
{
    public static class SteriaEffectHelper
    {
        public static int ShakeCalls;
        public static float LastX,LastY,LastSpeed,LastDuration;
        public static void AddScreenShake(float x,float y,float speed,float duration)
        { ShakeCalls++;LastX=x;LastY=y;LastSpeed=speed;LastDuration=duration; }
    }
    public static class SteriaLogger
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(string text) { Messages.Add(text); }
        public static void LogError(string text) { Messages.Add(text); }
    }
}
namespace Steria.Vfx
{
    // Record the V3 presentation boundary. The isolated game player tests actual atlases.
    internal sealed class ChristashaXiyinTide : IDisposable
    {
        internal const float ReleaseTime = .75f;
        internal static ChristashaXiyinTide Latest;
        internal readonly List<BattleUnitView> Targets = new List<BattleUnitView>();
        internal List<BattleUnitView> Subjects;
        internal bool Disposed;
        private float end = 1.67f;
        internal static void Prewarm() { }
        internal static ChristashaXiyinTide Create(Transform root, BattleUnitView caster, List<BattleUnitView> subjects)
        { return Latest = Resources.Missing ? null : new ChristashaXiyinTide { Subjects = subjects }; }
        internal void Present(float time) { }
        internal void Confirm(BattleUnitView view, float time) { Targets.Add(view); end = Math.Max(end, time + .635f); }
        internal bool Complete(float time) { return time >= end; }
        public void Dispose() { Disposed = true; }
    }
    internal sealed class ChristashaStarlance : IDisposable
    {
        // This managed fixture exercises the legacy-particle fallback. The installed
        // Unity player fixture verifies the real shader/atlas path independently.
        internal static ChristashaStarlance Create(float actorScale) { return null; }
        internal static bool Prepare() { return false; }
        internal bool Complete { get { return true; } }
        internal float Lifetime { get { return 0f; } }
        internal void Confirm() { }
        internal void SetLayer(string name) { }
        internal void Advance(float dt, Vector3 source, Vector3 contact, Vector3 exit, Camera camera) { }
        public void Dispose() { }
    }
    public static class ChristashaNativeMassRecipes { public const float AreaDesignWidth = 32f; }
    // A recording visual boundary is intentional: the tests exercise production
    // game adapters. Real particles are independently compiled and GPU-previewed.
    public sealed class ChristashaNativeParticleLibrary : IDisposable
    {
        public static readonly HashSet<string> Unsupported = new HashSet<string>();
        public static readonly List<ChristashaNativeParticleLibrary> Created = new List<ChristashaNativeParticleLibrary>();
        public readonly Texture2D Brush;
        private readonly Func<string, GameObject> resolver;
        public bool Disposed;
        public ChristashaNativeParticleLibrary(Func<string, GameObject> resolver, Texture2D originalBrush = null, Shader strikeShader = null)
        {
            this.resolver = resolver; Brush = originalBrush; Created.Add(this);
        }
        public GameObject CreatePrototype(string recipe, Transform parent = null)
        {
            if (Unsupported.Contains(recipe)) throw new NotSupportedException("Native recipe unavailable: " + recipe);
            if (resolver("prefabs/native-fixture/" + recipe) == null) throw new InvalidOperationException("Native source unavailable");
            var result = new GameObject("ChristashaNative_" + recipe);
            result.transform.SetParent(parent, false);
            result.AddComponent<ParticleSystemRenderer>();
            result.SetActive(false);
            return result;
        }
        public void Dispose() { Disposed = true; }
    }
    public sealed class ChristashaNativeParticlePlayback
    {
        public static readonly List<ChristashaNativeParticlePlayback> Started = new List<ChristashaNativeParticlePlayback>();
        public GameObject Root;
        public uint Seed;
        public float Age;
        public float Speed;
        public bool Stopped;
        public int Advances;
        public int ImpactConfirmations;
        public Vector3 Aim;
        public Vector3 Exit;
        public bool StrikeImpactOccurred { get { return !Stopped && ImpactConfirmations>0 && Age>=ChristashaProjectileArc.FlightTime; } }
        public float Duration
        {
            get
            {
                if (Root.name.EndsWith("Area")) return 1.16f / Speed;
                if (Root.name.EndsWith("Slash")) return 1.30f / Speed;
                if (Root.name.EndsWith("Hit")) return 1.08f / Speed;
                if (Root.name.EndsWith("Pierce")) return .72f / Speed;
                if (Root.name.EndsWith("Projectile")) return ChristashaProjectileArc.VisualDuration / Speed;
                return .65f / Speed;
            }
        }
        public static ChristashaNativeParticlePlayback Play(GameObject instance, uint seed, float playbackSpeed = 1f)
        {
            var visual = new ChristashaNativeParticlePlayback { Root = instance, Seed = seed, Speed = playbackSpeed };
            instance.SetActive(true);
            Started.Add(visual);
            return visual;
        }
        public void Advance(float dt) { Age += dt; Advances++; }
        public void StopAndClear() { Stopped = true; }
        public void AimStrike(Vector3 localTarget) { Aim = localTarget; }
        public void AimStrike(Vector3 localTarget,Vector3 localExit) { Aim=localTarget;Exit=localExit; }
        public void ConfirmStrikeImpact() { ImpactConfirmations++; }
    }
}
