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
    private bool _impactShakePlayed;
    private BattleUnitView _targetView;
    private float _visualTime;
    private float _visualLifetime;
    private string _layerName = "Effect";

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        Cleanup();
        try
        {
            if (self == null || self.model == null || self.charAppearance == null)
                throw new InvalidOperationException("missing native self/target appearance");
            // FX_Script_AttackBasic uses the special-motion pivot and native
            // scale one. Ordinary DiceAttackEffect.Initialize would instead
            // select H/J/Z by name and add an unwanted initial 2x multiplier.
            _self = self.model;
            additionalScale = Vector3.zero;
            offset = Vector2.zero;
            addedDestroyTime = 0f;
            _selfTransform = self.charAppearance.GetSpecialMotionPivot(PivotAction) ?? self.atkEffectRoot;
            if (_selfTransform == null) throw new InvalidOperationException("missing native special-motion/fallback pivot");
            gameObject.name = "ChristashaNative_" + Recipe;
            transform.SetParent(_selfTransform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            _initializedPos = transform.position;
            _destroyTime = Mathf.Max(.001f, destroyTime);
            _elapsed = 0f;
            _targetTransform = target == null ? null : target.atkEffectRoot;
            _targetView = target;
            spr.enabled = false;
            animator.enabled = false;
            _bHasDamagedEffect = false; // Native result processing owns damage sparks and all damage.
            _arrived = false;
            _impactShakePlayed = false;
            _visualTime = 0f;
            _visualLifetime = 0f;
            if (IsProjectile)
            {
                // A projectile uses live Fire world coordinates and a neutral
                // root. Neither actor/map scaling nor roll power is duplicated.
                transform.SetParent(null, false);
                transform.position = _selfTransform.position;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
            _visual = ChristashaParticleInstance.CreateLocal(Recipe, transform, Quaternion.identity);
            if (_visual != null)
            {
                if (!IsProjectile && Recipe == "Pierce" && _targetTransform != null)
                    AimPierce();
                _visualLifetime = _visual.Duration + .02f;
                _destroyTime = Mathf.Max(_destroyTime, _visualLifetime);
            }
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
        // Matches FX_Script_AttackBasic.SetScale: native particles do not receive
        // the ordinary sprite effect's (1 + roll) size multiplier.
    }

    public override void SetDestroyTime(float destroyTime)
    {
        // Retiming the native destruction deadline must not replay or stretch the visual.
        base.SetDestroyTime(Mathf.Max(Mathf.Max(.001f, destroyTime), _visualLifetime));
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
            if (!IsProjectile && Recipe == "Pierce" && _visual != null && _targetTransform != null)
                AimPierce();
            if (_visual != null) _visual.Advance(Time.deltaTime);
            if (IsProjectile && !_impactShakePlayed && _visual != null && _visual.HasConfirmedImpact)
            {
                _impactShakePlayed = true;
                SteriaEffectHelper.AddScreenShake(.012f,.009f,70f,.14f);
            }
            if (_visual != null && _visual.Complete)
            {
                _visual.Dispose();
                _visual = null;
            }
        }
        catch (Exception error)
        {
            SteriaLogger.LogError("ChristashaParticleV2 playback: " + error);
            Cleanup();
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private void AimPierce()
    {
        if (_visual == null || _targetView == null || _targetTransform == null) return;
        float targetScale = _targetView.characterRotationCenter == null ? 1f : Mathf.Abs(_targetView.characterRotationCenter.lossyScale.x);
        Vector3 exit = ChristashaThrustEnvelope.Exit(_visual.Root.transform.position, _targetTransform.position, _targetView.WorldPosition, targetScale);
        _visual.AimAt(_targetTransform.position, exit);
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
        float progress = Mathf.Clamp01(time / ChristashaProjectileArc.FlightTime);
        if (_visual != null)
        {
            // Both endpoints remain live. Neither prefab-local distances nor the
            // attacker's facing/scale can move this contact away from the target.
            _visual.Root.transform.position = ChristashaProjectileArc.Position(start, end, progress);
            _visual.Root.transform.rotation = ChristashaParticleInstance.AimLeft(ChristashaProjectileArc.Tangent(start, end, progress));
        }
        if (progress < 1f) return;
        _arrived = true;
        if (_visual != null) _visual.Root.transform.position = end;
        // Arrival alone is not a hit. The native result presentation confirms
        // impact through ChristashaStrikeImpactRouter before the contact gate.
    }

    internal bool TryConfirmImpact(BattleUnitView target)
    {
        if (_visual == null || target == null || target.atkEffectRoot != _targetTransform ||
            (!IsProjectile && Recipe != "Pierce")) return false;
        _visual.ConfirmImpact();
        return true;
    }

    private void Cleanup()
    {
        if (_visual != null) _visual.Dispose();
        _visual = null;
        _targetView = null;
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

/// <summary>Native recipe instances shared by dice/mass adapters; templates and brush stay cached.</summary>
internal sealed class ChristashaParticleInstance : IDisposable
{
    private static ChristashaNativeParticleLibrary _library;
    private static Texture2D _originalBrush;
    private static AssetBundle _strikeBundle;
    private static bool _loadAttempted;
    private static uint _seed = 73013u;
    private static readonly HashSet<string> ReportedFailures = new HashSet<string>();
    private ChristashaNativeParticlePlayback _driver;
    private const float SlashArtScale = .45f;
    // These recipes need independent art calibration; one is an explicit
    // provisional normalization, not approval to deploy the unfinished recipes.
    private const float HitArtScale = .55f;
    private const float PierceArtScale = .35f;
    private const float ProjectileArtScale = 1f;
    private const float CastArtScale = 1f;
    private const float AreaArtScale = 1f;
    private const float PointArtScale = 2.2f;
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
            ChristashaNativeParticleLibrary library = GetLibrary();
            if (library == null) return null;
            instance = new ChristashaParticleInstance { Root = library.CreatePrototype(recipe, parent) };
            instance.Root.name = "ChristashaNative_" + recipe;
            instance.Root.transform.localPosition = position;
            instance.Root.transform.localRotation = rotation;
            instance.Root.transform.localScale = scale * ArtScale(recipe);
            instance.SetLayer("Effect");
            // Playback activates the independently owned root after placement.
            // Instance disposal never destroys the shared library or its templates.
            instance._driver = ChristashaNativeParticlePlayback.Play(instance.Root, NextSeed(), PlaybackSpeed(recipe));
            return instance;
        }
        catch (Exception error)
        {
            if (instance != null) instance.Dispose();
            if (ReportedFailures.Add(recipe)) SteriaLogger.LogError("Christasha native instance " + recipe + ": " + error);
            return null;
        }
    }

    internal float Duration { get { return _driver == null ? 0f : _driver.Duration; } }

    private static float ArtScale(string recipe)
    {
        switch (recipe)
        {
            case "Slash": return SlashArtScale;
            case "Hit": return HitArtScale;
            case "Pierce": return PierceArtScale;
            case "Projectile": return ProjectileArtScale;
            case "Cast": return CastArtScale;
            case "Area": return AreaArtScale;
            case "Point": return PointArtScale;
            default: throw new ArgumentException("Unknown native recipe: " + recipe, "recipe");
        }
    }

    private static float PlaybackSpeed(string recipe)
    {
        switch (recipe)
        {
            case "Slash": return 1f; // Native library applies its internal .92 recipe timing.
            case "Hit": return 1f; // Native library applies its internal .93 recipe timing.
            case "Pierce": return 1f; // Pierce hook supplies its own .68s duration.
            case "Projectile": return 1f; // Projectile hook supplies its own .32s duration.
            default: return 1f; // Mass owner controls Cast/Area/Point timing through its own hooks.
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

    internal void AimAt(Vector3 worldTarget,Vector3 worldExit)
    {
        if (Root != null && _driver != null) _driver.AimStrike(Root.transform.InverseTransformPoint(worldTarget),Root.transform.InverseTransformPoint(worldExit));
    }

    internal bool HasConfirmedImpact { get { return _driver != null && _driver.StrikeImpactOccurred; } }

    internal void ConfirmImpact()
    {
        if (_driver != null) _driver.ConfirmStrikeImpact();
    }

    public void Dispose()
    {
        if (_driver != null) _driver.StopAndClear();
        _driver = null;
        if (Root != null) UnityEngine.Object.Destroy(Root);
        Root = null;
    }

    private static ChristashaNativeParticleLibrary GetLibrary()
    {
        if (_loadAttempted) return _library;
        _loadAttempted = true;
        Texture2D brush = null;
        try
        {
            string directory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            string modRoot = Directory.GetParent(directory).FullName;
            string path = Path.Combine(modRoot, "Resource", "Effects", "ChristashaNative", "tide-brush-imagegen-v1.png");
            if (!File.Exists(path)) throw new FileNotFoundException("Christasha native original brush is missing", path);
            brush = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                name = "ChristashaNative_OriginalBrush",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!ImageConversion.LoadImage(brush, File.ReadAllBytes(path), false)) throw new InvalidDataException("Cannot decode Christasha native original brush: " + path);
            Shader strikeShader = null;
            string shaderPath = Path.Combine(directory, "AB", "steria_christasha_light_strike");
            if (_strikeBundle == null && File.Exists(shaderPath)) _strikeBundle = AssetBundle.LoadFromFile(shaderPath);
            if (_strikeBundle != null) strikeShader = _strikeBundle.LoadAsset<Shader>("assets/nativeruntime/platinumstrike.shader");
            if (strikeShader == null) SteriaLogger.LogError("Christasha strike shader unavailable: " + shaderPath);
            _library = new ChristashaNativeParticleLibrary(resource => Resources.Load<GameObject>(resource), brush, strikeShader);
            _originalBrush = brush;
            SteriaLogger.Log("Christasha native library initialized from installed game resources and original brush");
        }
        catch (Exception error)
        {
            if (brush != null) UnityEngine.Object.Destroy(brush);
            SteriaLogger.LogError("Christasha native library unavailable; combat continues without visual: " + error);
        }
        return _library;
    }
}
