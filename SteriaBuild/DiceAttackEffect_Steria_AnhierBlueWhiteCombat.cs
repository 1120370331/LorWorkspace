using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using Steria;
using UnityEngine;

public abstract class DiceAttackEffect_Steria_AnhierBlueWhiteCombatBase : DiceAttackEffect
{
    private const string BundleName = "steria_anhier_bluewhite_combat";
    private const string EffectVersion = "AB-combat-vfx-farhit-runtime-projectile-20260706";

    private static AssetBundle _effectBundle;
    private static bool _triedLoadBundle;

    private readonly List<GameObject> _objects = new List<GameObject>();
    private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();

    private BattleUnitView _selfView;
    private BattleUnitView _targetView;
    private Transform _selfRoot;
    private Transform _targetRoot;
    private Transform _travelRoot;
    private Transform _impactRoot;
    private GameObject _runtimeProjectileRoot;
    private readonly List<TrailRenderer> _runtimeTrails = new List<TrailRenderer>();
    private ParticleSystem _runtimeHeadParticles;
    private Vector3 _runtimeStartWorld;
    private Vector3 _runtimeImpactWorld;
    private Direction _atkDir;
    private new float _elapsed;
    private bool _runtimeProjectileReady;
    private bool _impactRootActivated;

    protected abstract string PrefabName { get; }
    protected abstract string LogName { get; }
    protected abstract ActionDetail PivotAction { get; }
    protected abstract bool AnchorOnTarget { get; }
    protected virtual float Duration => 1.05f;
    protected virtual float HitYOffset => 0.12f;
    protected virtual float HitForwardOffset => 0.18f;
    protected virtual bool UseRuntimeProjectile => false;
    protected virtual float RuntimeProjectileTravelTime => 0.36f;
    protected virtual Vector3 RuntimeProjectileStartLocalOffset => new Vector3(0.92f, 0.18f, -0.02f);

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            base._bHasDamagedEffect = false;
            base._self = self?.model;
            base._selfTransform = self?.atkEffectRoot;

            _selfView = self;
            _targetView = target;
            _selfRoot = GetPivot(self, PivotAction);
            _targetRoot = GetPivot(target, PivotAction);
            if (_targetRoot == null)
            {
                _targetRoot = target?.atkEffectRoot;
            }
            base._targetTransform = _targetRoot;
            _atkDir = Direction.RIGHT;
            if (self != null && target != null)
            {
                _atkDir = (Direction)((target.WorldPosition - self.WorldPosition).x > 0f ? 1 : 0);
            }

            _destroyTime = Duration;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} Initialize ERROR: {ex}");
        }
    }

    protected override void Start()
    {
        try
        {
            if (TryCreateAssetBundleEffect())
            {
                PlayImpactSound();
                SteriaEffectHelper.AddScreenShake(0.018f, 0.014f, 76f, 0.18f);
                return;
            }

            CreateFallbackEffect();
            PlayImpactSound();
            SteriaEffectHelper.AddScreenShake(0.014f, 0.010f, 70f, 0.14f);
            SteriaLogger.Log($"{LogName}: fallback spawned particles={_systems.Count}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} Start ERROR: {ex}");
        }
    }

    private bool TryCreateAssetBundleEffect()
    {
        try
        {
            AssetBundle bundle = GetEffectBundle();
            if (bundle == null)
            {
                return false;
            }

            SteriaLogger.Log($"{LogName}: loading AB prefab '{PrefabName}'");
            GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (prefab == null)
            {
                SteriaLogger.Log($"{LogName} AB fallback: prefab not found '{PrefabName}'");
                return false;
            }
            SteriaLogger.Log($"{LogName}: AB prefab loaded");

            Transform parent = AnchorOnTarget ? (_targetRoot ?? _selfRoot) : (_selfRoot ?? base._selfTransform);
            if (parent == null)
            {
                return false;
            }

            GameObject main = UnityEngine.Object.Instantiate(prefab);
            SteriaLogger.Log($"{LogName}: AB prefab instantiated");
            main.name = LogName + "_AB";
            main.transform.SetParent(parent, false);
            main.transform.localPosition = Vector3.zero;
            main.transform.localRotation = _atkDir == Direction.LEFT ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            main.transform.localScale = Vector3.one;

            AlignAssetBundleEffect(main);
            SetLayerAndSorting(main);
            _objects.Add(main);
            if (UseRuntimeProjectile)
            {
                CreateRuntimeProjectileTrail(main);
            }
            SteriaLogger.Log($"{LogName}: using AssetBundle prefab ({EffectVersion})");
            return true;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} AB load failed, using fallback: {ex.Message}");
            return false;
        }
    }

    private void AlignAssetBundleEffect(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        try
        {
            Vector3 targetLocal = Vector3.zero;
            if (!AnchorOnTarget && _targetRoot != null)
            {
                targetLocal = root.transform.InverseTransformPoint(_targetRoot.position);
            }

            _travelRoot = FindChildRecursive(root.transform, "AB_TravelRoot");
            _impactRoot = FindChildRecursive(root.transform, "AB_ImpactRoot");

            if (!AnchorOnTarget && _targetRoot != null)
            {
                if (UseRuntimeProjectile)
                {
                    RefreshRuntimeImpactWorld();
                    if (_travelRoot != null)
                    {
                        _travelRoot.gameObject.SetActive(false);
                    }
                    if (_impactRoot != null)
                    {
                        _impactRoot.position = _runtimeImpactWorld;
                        _impactRoot.localScale = Vector3.one;
                        _impactRoot.gameObject.SetActive(false);
                        _impactRootActivated = false;
                    }

                    SteriaLogger.Log($"{LogName} AB runtime aligned: targetLocal=({targetLocal.x:0.00},{targetLocal.y:0.00},{targetLocal.z:0.00}), impactWorld=({_runtimeImpactWorld.x:0.00},{_runtimeImpactWorld.y:0.00},{_runtimeImpactWorld.z:0.00}), dir={_atkDir}");
                    return;
                }

                float forwardDistance = Mathf.Max(1.15f, Mathf.Abs(targetLocal.x));
                float distanceScale = Mathf.Clamp(forwardDistance / 8.5f, 0.72f, 1.38f);
                if (_travelRoot != null)
                {
                    _travelRoot.localPosition = new Vector3(targetLocal.x * 0.5f, targetLocal.y * 0.22f, targetLocal.z * 0.5f);
                    _travelRoot.localScale = new Vector3(distanceScale, 1f, 1f);
                }
                if (_impactRoot != null)
                {
                    float side = targetLocal.x >= 0f ? 1f : -1f;
                    _impactRoot.localPosition = new Vector3(targetLocal.x - side * HitForwardOffset, targetLocal.y + HitYOffset, targetLocal.z - 0.02f);
                    _impactRoot.localScale = Vector3.one;
                }

                SteriaLogger.Log($"{LogName} AB aligned: targetLocal=({targetLocal.x:0.00},{targetLocal.y:0.00},{targetLocal.z:0.00}), dir={_atkDir}");
            }
            else
            {
                if (_travelRoot != null)
                {
                    _travelRoot.localPosition = Vector3.zero;
                    _travelRoot.localScale = Vector3.one;
                }
                if (_impactRoot != null)
                {
                    _impactRoot.localPosition = new Vector3(0f, HitYOffset, -0.02f);
                    _impactRoot.localScale = Vector3.one;
                }

                SteriaLogger.Log($"{LogName} AB aligned on target: dir={_atkDir}");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} AB align failed: {ex.Message}");
        }
    }

    private static Transform GetPivot(BattleUnitView view, ActionDetail detail)
    {
        try
        {
            return view?.charAppearance?.GetAtkEffectPivot(detail) ?? view?.atkEffectRoot;
        }
        catch
        {
            return view?.atkEffectRoot;
        }
    }

    private static AssetBundle GetEffectBundle()
    {
        if (_effectBundle != null)
        {
            return _effectBundle;
        }

        if (_triedLoadBundle)
        {
            return null;
        }

        _triedLoadBundle = true;
        foreach (string path in GetBundleCandidatePaths())
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                _effectBundle = AssetBundle.LoadFromFile(path);
                if (_effectBundle != null)
                {
                    SteriaLogger.Log($"AnhierBlueWhiteCombat: loaded AssetBundle {path}");
                    return _effectBundle;
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"AnhierBlueWhiteCombat: failed bundle path {path}: {ex.Message}");
            }
        }

        SteriaLogger.Log("AnhierBlueWhiteCombat: AssetBundle not found, using fallback");
        return null;
    }

    private static IEnumerable<string> GetBundleCandidatePaths()
    {
        string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        string modRoot = Directory.GetParent(assemblyDir)?.FullName ?? assemblyDir;

        yield return Path.Combine(modRoot, "Resource", "AssetBundle", BundleName);
        yield return Path.Combine(modRoot, "Resource", "AssetBundle", BundleName + ".ab");
        yield return Path.Combine(modRoot, "Assemblies", "AB", BundleName);
        yield return Path.Combine(modRoot, "Assemblies", "AB", BundleName + ".ab");
    }

    private void CreateFallbackEffect()
    {
        Transform parent = AnchorOnTarget ? (_targetRoot ?? _selfRoot) : (_selfRoot ?? base._selfTransform);
        if (parent == null)
        {
            return;
        }

        GameObject go = new GameObject(LogName + "_FallbackParticles");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = AnchorOnTarget ? new Vector3(0f, HitYOffset, -0.03f) : Vector3.zero;
        go.transform.localRotation = _atkDir == Direction.LEFT ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        _objects.Add(go);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        _systems.Add(ps);
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.85f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.82f, 1f, 0.85f), new Color(1f, 1f, 1f, 0.95f));
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = AnchorOnTarget ? 55f : 18f;
        shape.radius = AnchorOnTarget ? 0.6f : 0.25f;
        shape.rotation = new Vector3(0f, 90f, 0f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateBlueWhiteFadeGradient(0.9f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.14f;
        renderer.lengthScale = AnchorOnTarget ? 1.8f : 3.4f;
        renderer.sortingOrder = 110;
    }

    private void CreateRuntimeProjectileTrail(GameObject root)
    {
        if (!UseRuntimeProjectile || root == null || _targetRoot == null)
        {
            return;
        }

        try
        {
            _runtimeStartWorld = root.transform.TransformPoint(RuntimeProjectileStartLocalOffset);
            RefreshRuntimeImpactWorld();

            _runtimeProjectileRoot = new GameObject(LogName + "_RuntimeProjectileTrail");
            _runtimeProjectileRoot.layer = 8;
            _runtimeProjectileRoot.transform.position = _runtimeStartWorld;
            _objects.Add(_runtimeProjectileRoot);

            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_FarHit_RuntimeTrail_Core", new Vector3(0f, 0.02f, -0.08f), 0.46f, 0.04f, 0.48f, new Color(0.92f, 1f, 1f, 0.96f), new Color(0.20f, 0.66f, 1f, 0f), 130);
            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_FarHit_RuntimeTrail_BlueWake", new Vector3(0f, -0.08f, -0.12f), 0.72f, 0.08f, 0.56f, new Color(0.12f, 0.56f, 1f, 0.68f), new Color(0.06f, 0.26f, 1f, 0f), 126);
            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_FarHit_RuntimeTrail_ThinSpark", new Vector3(0f, 0.16f, -0.16f), 0.18f, 0.02f, 0.34f, new Color(0.74f, 0.94f, 1f, 0.82f), new Color(0.22f, 0.72f, 1f, 0f), 134);
            CreateRuntimeProjectileHead(_runtimeProjectileRoot.transform);

            _runtimeProjectileReady = true;
            SteriaLogger.Log($"{LogName}: runtime projectile created start=({_runtimeStartWorld.x:0.00},{_runtimeStartWorld.y:0.00},{_runtimeStartWorld.z:0.00}) impact=({_runtimeImpactWorld.x:0.00},{_runtimeImpactWorld.y:0.00},{_runtimeImpactWorld.z:0.00}) trailRenderers={_runtimeTrails.Count}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} runtime projectile create failed: {ex.Message}");
        }
    }

    private void CreateRuntimeTrailLayer(Transform parent, string name, Vector3 localOffset, float startWidth, float endWidth, float trailTime, Color startColor, Color endColor, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localOffset;

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.minVertexDistance = 0.04f;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.autodestruct = false;
        trail.emitting = true;
        trail.startColor = startColor;
        trail.endColor = endColor;
        trail.material = CreateRuntimeMaterial(startColor);
        trail.sortingOrder = sortingOrder;
        _runtimeTrails.Add(trail);
    }

    private void CreateRuntimeProjectileHead(Transform parent)
    {
        GameObject go = new GameObject("AB_FarHit_RuntimeProjectileHead");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        _runtimeHeadParticles = ps;
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.duration = 0.24f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.48f, 1.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.84f, 0.98f, 1f, 0.95f), new Color(0.12f, 0.56f, 1f, 0.66f));
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 38f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateBlueWhiteFadeGradient(0.92f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = CreateRuntimeMaterial(new Color(0.84f, 0.98f, 1f, 0.95f));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 136;
    }

    private static Material CreateRuntimeMaterial(Color tint)
    {
        Shader shader = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", tint);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }
        material.color = tint;
        material.renderQueue = 3150;
        return material;
    }

    private void RefreshRuntimeImpactWorld()
    {
        if (_targetRoot != null)
        {
            _runtimeImpactWorld = _targetRoot.position + new Vector3(0f, HitYOffset, -0.02f);
        }
    }

    private void UpdateRuntimeProjectile()
    {
        if (!_runtimeProjectileReady || _runtimeProjectileRoot == null)
        {
            return;
        }

        RefreshRuntimeImpactWorld();
        float raw = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, RuntimeProjectileTravelTime));
        float eased = 1f - Mathf.Pow(1f - raw, 3f);
        _runtimeProjectileRoot.transform.position = Vector3.Lerp(_runtimeStartWorld, _runtimeImpactWorld, eased);

        Transform impactRoot = _impactRoot;
        if (impactRoot != null)
        {
            impactRoot.position = _runtimeImpactWorld;
            if (!_impactRootActivated && raw >= 0.82f)
            {
                impactRoot.gameObject.SetActive(true);
                ParticleSystem[] systems = impactRoot.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem system in systems)
                {
                    system.Clear(true);
                    system.Play(true);
                }
                _impactRootActivated = true;
            }
        }

        bool stillFlying = raw < 0.98f;
        foreach (TrailRenderer trail in _runtimeTrails)
        {
            if (trail != null)
            {
                trail.emitting = stillFlying;
            }
        }

        if (_runtimeHeadParticles != null)
        {
            var emission = _runtimeHeadParticles.emission;
            emission.enabled = stillFlying;
        }
    }

    private void PlayImpactSound()
    {
        try
        {
            MotionDetail motion = MotionDetail.H;
            if (PivotAction == ActionDetail.Penetrate)
            {
                motion = MotionDetail.Z;
            }
            else if (PivotAction == ActionDetail.Slash)
            {
                motion = MotionDetail.J;
            }

            _selfView?.charAppearance?.soundInfo?.PlaySound(motion, true);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} sound error: {ex.Message}");
        }
    }

    protected override void Update()
    {
        try
        {
            _elapsed += Time.deltaTime;
            if (UseRuntimeProjectile)
            {
                UpdateRuntimeProjectile();
            }
            if (_elapsed >= Duration)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} Update ERROR: {ex.Message}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (GameObject obj in _objects)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
        _objects.Clear();
        _systems.Clear();
        _runtimeTrails.Clear();
    }

    private static ParticleSystem.MinMaxGradient CreateBlueWhiteFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.10f, 0.52f, 1f), 0f),
                new GradientColorKey(new Color(0.92f, 1f, 1f), 0.2f),
                new GradientColorKey(new Color(0.28f, 0.72f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.12f),
                new GradientAlphaKey(peakAlpha * 0.45f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetLayerAndSorting(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            GameObject go = child.gameObject;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sortingOrder < 0)
            {
                go.layer = 20;
            }
            else
            {
                go.layer = 8;
            }
        }
    }
}

public class DiceAttackEffect_Steria_AnhierBlueWhitePierce : DiceAttackEffect_Steria_AnhierBlueWhiteCombatBase
{
    protected override string PrefabName => "AnhierBlueWhitePiercePrefab";
    protected override string LogName => "AnhierBlueWhitePierce";
    protected override ActionDetail PivotAction => ActionDetail.Penetrate;
    protected override bool AnchorOnTarget => false;
    protected override float HitYOffset => 0.08f;
    protected override float HitForwardOffset => 0.16f;
}

public class DiceAttackEffect_Steria_AnhierBlueWhiteHit : DiceAttackEffect_Steria_AnhierBlueWhiteCombatBase
{
    protected override string PrefabName => "AnhierBlueWhiteHitPrefab";
    protected override string LogName => "AnhierBlueWhiteHit";
    protected override ActionDetail PivotAction => ActionDetail.Hit;
    protected override bool AnchorOnTarget => false;
    protected override float HitYOffset => 0.16f;
    protected override float HitForwardOffset => 0.12f;
}

public class DiceAttackEffect_Steria_AnhierBlueWhiteFarHit : DiceAttackEffect_Steria_AnhierBlueWhiteCombatBase
{
    protected override string PrefabName => "AnhierBlueWhiteFarHitPrefab";
    protected override string LogName => "AnhierBlueWhiteFarHit";
    protected override ActionDetail PivotAction => ActionDetail.Hit;
    protected override bool AnchorOnTarget => false;
    protected override bool UseRuntimeProjectile => true;
    protected override float HitYOffset => 0.18f;
    protected override float HitForwardOffset => 0f;
    protected override float RuntimeProjectileTravelTime => 0.38f;
}
