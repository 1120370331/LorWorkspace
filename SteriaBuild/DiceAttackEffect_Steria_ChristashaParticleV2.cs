using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using Steria;
using Steria.Vfx;
using UnityEngine;

/// <summary>Native dice attachment and lifetime; all particle recipes live in the shared visual driver.</summary>
public abstract class DiceAttackEffect_Steria_ChristashaParticleV2 : DiceAttackEffect
{
    protected abstract string Recipe { get; }
    protected abstract ActionDetail PivotAction { get; }
    private bool IsProjectile { get { return PivotAction == ActionDetail.Fire; } }
    private ChristashaParticleInstance _visual;
    private bool _arrived;
    private float _visualTime;
    private string _layerName = "Effect";
    private const float ProjectileTravelTime = .26f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        Cleanup();
        try
        {
            if (self == null || self.model == null || self.charAppearance == null)
                throw new InvalidOperationException("missing native self/target appearance");
            // Native code tests Contains(H/J/Z/G) in the final name segment. The
            // character name itself contains H; only an isolated suffix is safe.
            string suffix = PivotAction == ActionDetail.Slash ? "J" : PivotAction == ActionDetail.Hit ? "H" : IsProjectile ? "F" : "Z";
            gameObject.name = "ChristashaParticleV2_" + suffix;
            additionalScale = Vector3.zero;
            offset = Vector2.zero;
            addedDestroyTime = 0f;
            // Native initialization dereferences target even though melee never
            // uses it. The self fallback only supplies that required native field.
            base.Initialize(self, target ?? self, Mathf.Max(.001f, destroyTime));
            _targetTransform = target == null ? null : target.atkEffectRoot;
            spr.enabled = false;
            animator.enabled = false;
            _bHasDamagedEffect = false; // Native result processing owns damage sparks and all damage.
            _arrived = false;
            _visualTime = 0f;
            if (IsProjectile)
            {
                // F has no native name branch. The detached root deliberately
                // excludes actor/map scaling and receives the native roll scale once.
                _selfTransform = self.charAppearance.GetAtkEffectPivot(ActionDetail.Fire) ?? self.atkEffectRoot;
                transform.SetParent(null, false);
                transform.position = _selfTransform.position;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one * 2f;
            }
            _visual = ChristashaParticleInstance.CreateLocal(Recipe, transform, Quaternion.identity);
            // World-authored projectile retains the caller's roll multiplier,
            // while compensating the native root's initial 2x exactly once.
            if (IsProjectile && _visual != null) _visual.Root.transform.localScale = Vector3.one * .5f;
            SetLayer(_layerName);
            if (IsProjectile) PositionProjectile(0f);
        }
        catch (Exception error)
        {
            SteriaLogger.LogError("ChristashaParticleV2 " + Recipe + ": " + error);
            Cleanup();
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    public override void SetScale(float scaleFactor)
    {
        base.SetScale(scaleFactor);
        if (IsProjectile && !_arrived) PositionProjectile(_visualTime);
    }

    public override void SetDestroyTime(float destroyTime)
    {
        // Retiming the native destruction deadline must not replay or stretch the visual.
        base.SetDestroyTime(Mathf.Max(.001f, destroyTime));
    }

    public override void SetLayer(string layerName)
    {
        _layerName = layerName;
        base.SetLayer(layerName);
        if (_visual != null) _visual.SetLayer(layerName);
    }

    protected override void Update()
    {
        base.Update(); // Includes caller SetDestroyTime and the default one-second lifetime.
        try
        {
            _visualTime += Time.deltaTime;
            if (IsProjectile && !_arrived) PositionProjectile(_visualTime);
            if (_visual != null) _visual.Advance(Time.deltaTime);
        }
        catch (Exception error)
        {
            SteriaLogger.LogError("ChristashaParticleV2 playback: " + error);
            Cleanup();
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private void PositionProjectile(float time)
    {
        if (_selfTransform == null || _targetTransform == null)
        {
            _arrived = true;
            if (_visual != null) _visual.Dispose();
            _visual = null;
            return;
        }
        Vector3 start = _selfTransform.position;
        Vector3 end = _targetTransform.position;
        Vector3 direction = end - start;
        float progress = Mathf.Clamp01(time / ProjectileTravelTime);
        if (_visual != null)
        {
            // Both endpoints remain live. Neither prefab-local distances nor the
            // attacker's facing/scale can move this contact away from the target.
            _visual.Root.transform.position = Vector3.Lerp(start, end, progress);
            _visual.Root.transform.rotation = ChristashaParticleInstance.AimLeft(direction);
        }
        if (progress < 1f) return;
        _arrived = true;
        if (_visual != null) _visual.Dispose();
        _visual = null;
        // Initialize supplies no success information. Do not manufacture a
        // confirmed-hit Point here; the native dice result owns that feedback.
    }

    private void Cleanup()
    {
        if (_visual != null) _visual.Dispose();
        _visual = null;
    }

    protected virtual void OnDisable() { Cleanup(); UnityEngine.Object.Destroy(gameObject); }
    protected override void OnDestroy() { Cleanup(); base.OnDestroy(); }
}

public sealed class DiceAttackEffect_Steria_ChristashaV2SlashH : DiceAttackEffect_Steria_ChristashaParticleV2
{ protected override string Recipe { get { return "Slash"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Slash; } } }
public sealed class DiceAttackEffect_Steria_ChristashaV2SlashV : DiceAttackEffect_Steria_ChristashaParticleV2
{ protected override string Recipe { get { return "Slash"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Slash; } } }
public sealed class DiceAttackEffect_Steria_ChristashaV2Hit : DiceAttackEffect_Steria_ChristashaParticleV2
{ protected override string Recipe { get { return "Hit"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Hit; } } }
public sealed class DiceAttackEffect_Steria_ChristashaV2PierceNear : DiceAttackEffect_Steria_ChristashaParticleV2
{ protected override string Recipe { get { return "Pierce"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Penetrate; } } }
public sealed class DiceAttackEffect_Steria_ChristashaV2PierceFar : DiceAttackEffect_Steria_ChristashaParticleV2
{ protected override string Recipe { get { return "Projectile"; } } protected override ActionDetail PivotAction { get { return ActionDetail.Fire; } } }

/// <summary>Instance ownership shared by dice and mass adapters; cached bundle assets stay shared.</summary>
internal sealed class ChristashaParticleInstance : IDisposable
{
    internal const string BundleName = "steria_christasha_particle_v2";
    private static AssetBundle _bundle;
    private static bool _loadAttempted;
    private static uint _seed = 73013u;
    private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    private ChristashaParticleVisual _driver;
    internal GameObject Root { get; private set; }
    internal float Age { get; private set; }
    internal bool Complete { get { return Root == null || _driver == null || Age >= _driver.Duration; } }

    internal static ChristashaParticleInstance CreateLocal(string recipe, Transform parent, Quaternion rotation)
    {
        return Create(recipe, parent, Vector3.zero, rotation, Vector3.one);
    }

    internal static ChristashaParticleInstance CreateWorld(string recipe, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Create(recipe, null, position, rotation, scale);
    }

    private static ChristashaParticleInstance Create(string recipe, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        ChristashaParticleInstance instance = null;
        try
        {
            GameObject prefab = GetPrefab(recipe);
            if (prefab == null) return null;
            instance = new ChristashaParticleInstance { Root = UnityEngine.Object.Instantiate(prefab) };
            instance.Root.name = "ChristashaV2_" + recipe;
            instance.Root.transform.SetParent(parent, false);
            instance.Root.transform.localPosition = position;
            instance.Root.transform.localRotation = rotation;
            instance.Root.transform.localScale = scale;
            // The visual driver owns any per-instance material state; the adapter
            // owns the cloned GameObject and invokes StopAndClear before destruction.
            instance._driver = ChristashaParticleVisual.Play(instance.Root, NextSeed());
            instance.SetLayer("Effect");
            instance.Root.SetActive(true);
            return instance;
        }
        catch (Exception error)
        {
            if (instance != null) instance.Dispose();
            SteriaLogger.LogError("ChristashaParticleV2 instance " + recipe + ": " + error);
            return null;
        }
    }

    private static uint NextSeed()
    {
        unchecked { _seed = _seed * 1664525u + 1013904223u; }
        return _seed == 0u ? 1u : _seed;
    }

    internal static Quaternion AimLeft(Vector3 direction)
    {
        return direction.sqrMagnitude < .000001f ? Quaternion.identity :
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180f);
    }

    internal void SetLayer(string layerName)
    {
        if (Root == null) return;
        int layer = LayerMask.NameToLayer(layerName);
        foreach (Transform child in Root.GetComponentsInChildren<Transform>(true))
            if (layer >= 0) child.gameObject.layer = layer;
        foreach (Renderer renderer in Root.GetComponentsInChildren<Renderer>(true))
            renderer.sortingLayerName = "Particle";
    }

    internal void Advance(float deltaTime)
    {
        if (Root == null || _driver == null) return;
        float dt = Mathf.Max(0f, deltaTime);
        Age += dt;
        _driver.Advance(dt);
    }

    public void Dispose()
    {
        if (_driver != null) _driver.StopAndClear();
        _driver = null;
        if (Root != null) UnityEngine.Object.Destroy(Root);
        Root = null;
    }

    private static GameObject GetPrefab(string recipe)
    {
        GameObject prefab;
        if (Prefabs.TryGetValue(recipe, out prefab)) return prefab;
        if (!_loadAttempted)
        {
            _loadAttempted = true;
            string directory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            string modRoot = Directory.GetParent(directory).FullName;
            foreach (string relative in new[] { "Resource/AssetBundle/", "Assemblies/AB/" })
            {
                foreach (string extension in new[] { "", ".ab" })
                {
                    string path = Path.Combine(modRoot, relative + BundleName + extension);
                    if (!File.Exists(path)) continue;
                    try { _bundle = AssetBundle.LoadFromFile(path); }
                    catch (Exception error) { SteriaLogger.LogError("ChristashaParticleV2 bundle: " + error.Message); }
                    if (_bundle != null) break;
                }
                if (_bundle != null) break;
            }
            SteriaLogger.Log(_bundle != null ? "ChristashaParticleV2 bundle loaded" : "ChristashaParticleV2 bundle unavailable; combat proceeds without visuals");
        }
        if (_bundle != null) prefab = _bundle.LoadAsset<GameObject>("ChristashaV2_" + recipe);
        Prefabs[recipe] = prefab;
        if (prefab == null) SteriaLogger.LogError("ChristashaParticleV2 missing prefab: " + recipe);
        return prefab;
    }
}
