using System;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using HarmonyLib;
using LOR_DiceSystem;
using Steria.AntieVfx;
using UnityEngine;

namespace Steria.Vfx
{
    public static class AntieIceIndigoDefaults
    {
        public static string Resolve(BattleUnitView view, BattleCardBehaviourResult result, ActionDetail action, string customResource)
        {
            if (result == null || !result.hasBehaviour) return customResource;
            var range = result.cardModel == null ? result.diceBehaviourResult.range : result.cardModel.GetSpec().Ranged;
            return Resource(view == null ? null : view.model, result.behaviourRawData, range, action, customResource) ?? customResource;
        }

        static string Resource(BattleUnitModel unit, DiceBehaviour dice, CardRange range, ActionDetail action, string customResource)
        {
            var id = unit == null || unit.equipment == null || unit.Book == null ? null : unit.Book.BookId;
            if (id == null || id.packageId != "SteriaBuilding" || (id.id != 99000011 && id.id != 13) || dice == null ||
                (dice.Type != BehaviourType.Atk && dice.Type != BehaviourType.Standby) ||
                (range != CardRange.Near && range != CardRange.Far) || dice.MotionDetail == MotionDetail.F ||
                action == ActionDetail.Fire || customResource == "None" || dice.EffectRes == "None") return null;
            // Far cards can contain ordinary J/H/Z dice; only actual firing stays on the old path.
            switch (dice.Detail)
            {
                case BehaviourDetail.Hit: return "Steria_AntieIceIndigoHit";
                case BehaviourDetail.Slash: return "Steria_AntieIceIndigoSlash";
                case BehaviourDetail.Penetrate: return "Steria_AntieIceIndigoPierce";
                default: return null;
            }
        }

        public static bool TryCreateDirect(BattleUnitModel unit, float destroyTime, out DiceAttackEffect effect)
        {
            effect = null;
            var playing = unit == null ? null : unit.currentDiceAction;
            if (playing == null || playing.card == null || playing.currentBehavior == null || unit.view == null ||
                playing.target == null || playing.target.view == null) return false;
            var dice = playing.currentBehavior.behaviourInCard;
            var resource = Resource(unit, dice, playing.card.GetSpec().Ranged, ActionDetail.Default, null);
            if (resource == null) return false;
            float ratio = Mathf.Clamp01((float)playing.currentBehavior.DiceResultValue / Mathf.Max(1, dice.Dice));
            effect = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource, ratio, unit.view, playing.target.view, destroyTime);
            return effect != null;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), "PrintEffect")]
    public static class AntieCoreAttackPresentationPatch
    {
        static readonly FieldInfo Librarian = AccessTools.Field(typeof(RencounterManager), "_librarian"),
            Enemy = AccessTools.Field(typeof(RencounterManager), "_enemy"),
            LibrarianResult = AccessTools.Field(typeof(RencounterManager), "_currentLibrarianBehaviourResult"),
            EnemyResult = AccessTools.Field(typeof(RencounterManager), "_currentEnemyBehaviourResult");

        public static void Prefix(RencounterManager __instance, ActionDetail librarianActionDetail, ActionDetail enemyActionDetail,
            ref string liCustomRes, ref string enCustomRes)
        {
            liCustomRes = AntieIceIndigoDefaults.Resolve((BattleUnitView)Librarian.GetValue(__instance),
                (BattleCardBehaviourResult)LibrarianResult.GetValue(__instance), librarianActionDetail, liCustomRes);
            enCustomRes = AntieIceIndigoDefaults.Resolve((BattleUnitView)Enemy.GetValue(__instance),
                (BattleCardBehaviourResult)EnemyResult.GetValue(__instance), enemyActionDetail, enCustomRes);
        }
    }

    internal static class AntieIceIndigoAssets
    {
        internal const string BundleName = "steria_antie_ice_indigo_combat";
        static AssetBundle bundle;
        static readonly string[] Names = { "HitVertical", "SlashHorizontal", "PierceDual" };
        static readonly GameObject[] Prefabs = new GameObject[3];

        internal static AntieSlash Create(Transform parent, AntieAttackKind kind)
        {
            if (bundle == null)
            {
                string assembly = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                string path = Path.Combine(Directory.GetParent(assembly).FullName, "Assemblies", "AB", BundleName);
                if (!File.Exists(path)) throw new FileNotFoundException("Antie AssetBundle missing", path);
                bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null) throw new InvalidOperationException("Cannot load Antie AssetBundle: " + path);
            }
            int index = (int)kind;
            if (Prefabs[index] == null) Prefabs[index] = bundle.LoadAsset<GameObject>(Names[index]);
            if (Prefabs[index] == null) throw new InvalidOperationException("Missing Antie profile: " + Names[index]);
            var go = UnityEngine.Object.Instantiate(Prefabs[index], parent, false);
            go.SetActive(false);
            try
            {
                var visual = go.AddComponent<AntieSlash>();
                visual.kind = kind;
                visual.autoPlay = visual.animationDriven = false;
                // The authored arcs face +X. Native character-local forward is -X.
                visual.mirror = true;
                visual.BindNative();
                foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.isSupported)
                        throw new InvalidOperationException("Unsupported Antie material");
                    renderer.sortingLayerName = "Particle";
                }
                return visual;
            }
            catch { UnityEngine.Object.Destroy(go); throw; }
        }
    }
}

public abstract class DiceAttackEffect_Steria_AntieIceIndigoBase : DiceAttackEffect
{
    protected abstract AntieAttackKind Profile { get; }
    AntieSlash visual;
    bool cleared;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        if (self == null || self.model == null || self.charAppearance == null || self.characterRotationCenter == null ||
            target == null || target.atkEffectRoot == null) throw new InvalidOperationException("Antie effect requires native actor/target views");
        gameObject.name = "AntieIceIndigo_" + (Profile == AntieAttackKind.HitVertical ? "H" : Profile == AntieAttackKind.SlashHorizontal ? "J" : "Z");
        additionalScale = Vector3.zero;
        offset = Vector2.zero;
        addedDestroyTime = 0;
        base.Initialize(self, target, Mathf.Max(.001f, destroyTime));
        _selfTransform = self.characterRotationCenter;
        transform.SetParent(_selfTransform, false);
        transform.localPosition = new Vector3(-1.3f, -.18f, 0);
        transform.localRotation = Quaternion.identity;
        spr.enabled = animator.enabled = false;
        try
        {
            visual = Steria.Vfx.AntieIceIndigoAssets.Create(transform, Profile);
            // Horizontal and dual curves have a negative-X start. Translate the art as a whole
            // so their near edge remains in front; never chase the target or rotate by its slope.
            float x = Profile == AntieAttackKind.SlashHorizontal ? -.98f : Profile == AntieAttackKind.PierceDual ? -.80f : 0;
            float y = Profile == AntieAttackKind.SlashHorizontal ? .14f : Profile == AntieAttackKind.PierceDual ? .06f : 0;
            visual.transform.localPosition = new Vector3(x, y, 0);
            visual.transform.localScale = new Vector3(-.5f, .5f, .5f);
            AlignHorizontalCameraSide();
            visual.ApplyAt(0);
            _destroyTime = Mathf.Max(_destroyTime, AntieSlash.Duration);
            SetLayer("Effect");
            visual.gameObject.SetActive(true);
        }
        catch { Clear(); UnityEngine.Object.Destroy(gameObject); throw; }
    }

    protected override void Update()
    {
        if (cleared || visual == null) return;
        AlignHorizontalCameraSide();
        visual.ApplyAt(_elapsed + Mathf.Max(0, Time.deltaTime));
        base.Update();
    }

    void AlignHorizontalCameraSide()
    {
        if (visual == null || Profile != AntieAttackKind.SlashHorizontal) return;
        // The game turns the actor 180 degrees around Y for the other facing.
        // Preserve the camera-facing convex edge without reversing attack X.
        var scale = visual.transform.localScale;
        scale.z = Mathf.Abs(scale.z) * (_selfTransform.TransformVector(Vector3.forward).z < 0 ? -1 : 1);
        visual.transform.localScale = scale;
    }

    public override void SetDestroyTime(float destroyTime)
    {
        base.SetDestroyTime(destroyTime);
        if (_elapsed >= _destroyTime) { Clear(); UnityEngine.Object.Destroy(gameObject); }
    }

    public override void SetLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) throw new InvalidOperationException("Native effect layer missing: " + layerName);
        foreach (var child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }

    void Clear()
    {
        if (cleared) return;
        cleared = true;
        if (visual == null) return;
        visual.ApplyAt(AntieSlash.Duration);
        visual.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(visual.gameObject);
        visual = null;
    }
    protected void OnDisable() { Clear(); }
    protected override void OnDestroy() { Clear(); base.OnDestroy(); }
}

public sealed class DiceAttackEffect_Steria_AntieIceIndigoHit : DiceAttackEffect_Steria_AntieIceIndigoBase
{ protected override AntieAttackKind Profile { get { return AntieAttackKind.HitVertical; } } }
public sealed class DiceAttackEffect_Steria_AntieIceIndigoSlash : DiceAttackEffect_Steria_AntieIceIndigoBase
{ protected override AntieAttackKind Profile { get { return AntieAttackKind.SlashHorizontal; } } }
public sealed class DiceAttackEffect_Steria_AntieIceIndigoPierce : DiceAttackEffect_Steria_AntieIceIndigoBase
{ protected override AntieAttackKind Profile { get { return AntieAttackKind.PierceDual; } } }
