using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using Steria;
using UnityEngine;

public abstract class DiceAttackEffect_Steria_ChristashaDawnCombatBase : DiceAttackEffect
{
    private const string BundleName = "steria_christasha_dawn_combat";
    private const string EffectVersion = "AB-christasha-dawn-combat-20260706";

    private static readonly Color ColorDawnWhite = new Color(1f, 0.98f, 0.78f, 1f);
    private static readonly Color ColorDawnGold = new Color(1f, 0.78f, 0.10f, 0.95f);
    private static readonly Color ColorDawnAmber = new Color(1f, 0.47f, 0.02f, 0.76f);
    private static readonly Color ColorDawnOrange = new Color(1f, 0.38f, 0.08f, 0.88f);
    private static readonly Color ColorDeepBlack = new Color(0.02f, 0.015f, 0.02f, 0.62f);

    private static AssetBundle _effectBundle;
    private static bool _triedLoadBundle;

    private readonly List<GameObject> _objects = new List<GameObject>();
    private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();
    private readonly List<TrailRenderer> _runtimeTrails = new List<TrailRenderer>();
    private readonly List<Material> _crescentRevealMaterials = new List<Material>();

    private BattleUnitView _selfView;
    private Transform _selfRoot;
    private Transform _targetRoot;
    private Transform _travelRoot;
    private Transform _impactRoot;
    private GameObject _runtimeProjectileRoot;
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
    protected virtual float Duration => 1.12f;
    protected virtual float HitYOffset => 0.16f;
    protected virtual float HitForwardOffset => 0.14f;
    protected virtual bool UseRuntimeProjectile => false;
    protected virtual float RuntimeProjectileTravelTime => 0.34f;
    protected virtual Vector3 RuntimeProjectileStartLocalOffset => new Vector3(0.95f, 0.22f, -0.04f);
    protected virtual bool CenterSlashTravelOnCaster => false;
    protected virtual float CenterSlashYOffset => 0f;
    protected virtual bool CenterSlashTravelUsesTargetSide => false;
    protected virtual float SlashTravelDistanceDivisor => 8.5f;
    protected virtual float SlashDistanceScaleMin => 0.78f;
    protected virtual float SlashDistanceScaleMax => 1.48f;
    protected virtual float SlashShakeStrength => 0.020f;
    protected virtual float SlashShakeSpeed => 0.014f;
    protected virtual float SlashShakeFrequency => 80f;
    protected virtual float SlashShakeDuration => 0.18f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            base._bHasDamagedEffect = false;
            base._self = self?.model;
            base._selfTransform = self?.atkEffectRoot;

            _selfView = self;
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
                SteriaEffectHelper.AddScreenShake(SlashShakeStrength, SlashShakeSpeed, SlashShakeFrequency, SlashShakeDuration);
                return;
            }

            CreateFallbackEffect();
            PlayImpactSound();
            SteriaEffectHelper.AddScreenShake(0.014f, 0.010f, 72f, 0.14f);
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

            GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (prefab == null)
            {
                SteriaLogger.Log($"{LogName} AB fallback: prefab not found '{PrefabName}'");
                return false;
            }

            Transform parent = AnchorOnTarget ? (_targetRoot ?? _selfRoot) : (_selfRoot ?? base._selfTransform);
            if (parent == null)
            {
                return false;
            }

            GameObject main = UnityEngine.Object.Instantiate(prefab);
            main.name = LogName + "_AB";
            main.transform.SetParent(parent, false);
            main.transform.localPosition = Vector3.zero;
            main.transform.localRotation = Quaternion.identity;
            main.transform.localScale = Vector3.one;

            AlignAssetBundleEffect(main);
            SetLayerAndSorting(main);
            CacheCrescentRevealMaterials(main);
            AnimateCrescentRevealMaterials();
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

                float forwardDistance = Mathf.Max(1.25f, Mathf.Abs(targetLocal.x));
                float distanceScale = Mathf.Clamp(forwardDistance / SlashTravelDistanceDivisor, SlashDistanceScaleMin, SlashDistanceScaleMax);
                if (_travelRoot != null)
                {
                    float slashSide = CenterSlashTravelUsesTargetSide && targetLocal.x < 0f ? -1f : 1f;
                    Vector3 authoredTravelOffset = _travelRoot.localPosition;
                    _travelRoot.localPosition = CenterSlashTravelOnCaster
                        ? new Vector3(Mathf.Abs(authoredTravelOffset.x) * slashSide, CenterSlashYOffset + authoredTravelOffset.y, authoredTravelOffset.z)
                        : new Vector3(targetLocal.x * 0.5f, targetLocal.y * 0.22f, targetLocal.z * 0.5f);
                    _travelRoot.localScale = new Vector3(distanceScale * slashSide, 1f, 1f);
                }
                if (_impactRoot != null)
                {
                    float side = targetLocal.x >= 0f ? 1f : -1f;
                    _impactRoot.localPosition = new Vector3(targetLocal.x - side * HitForwardOffset, targetLocal.y + HitYOffset, targetLocal.z - 0.03f);
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
                    _impactRoot.localPosition = new Vector3(0f, HitYOffset, -0.03f);
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
        main.duration = 0.72f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 8.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 1.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(ColorDawnAmber, ColorDawnWhite);
        main.maxParticles = 45;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = AnchorOnTarget ? 60f : 20f;
        shape.radius = AnchorOnTarget ? 0.8f : 0.32f;
        shape.rotation = new Vector3(0f, 90f, 0f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateDawnFadeGradient(0.9f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.14f;
        renderer.lengthScale = AnchorOnTarget ? 2.1f : 3.8f;
        renderer.sortingOrder = 118;

        GameObject shade = new GameObject(LogName + "_FallbackBlackBackplate");
        shade.layer = 8;
        shade.transform.SetParent(go.transform, false);
        shade.transform.localPosition = new Vector3(0f, -0.05f, 0.08f);
        ParticleSystem shadePs = shade.AddComponent<ParticleSystem>();
        _systems.Add(shadePs);
        var shadeMain = shadePs.main;
        shadeMain.playOnAwake = true;
        shadeMain.loop = false;
        shadeMain.duration = 0.62f;
        shadeMain.startLifetime = 0.38f;
        shadeMain.startSpeed = 0f;
        shadeMain.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        shadeMain.startColor = ColorDeepBlack;
        shadeMain.maxParticles = 3;
        var shadeEmission = shadePs.emission;
        shadeEmission.rateOverTime = 0f;
        shadeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)2) });
        ParticleSystemRenderer shadeRenderer = shade.GetComponent<ParticleSystemRenderer>();
        shadeRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        shadeRenderer.sortingOrder = 82;
    }

    private void CacheCrescentRevealMaterials(GameObject root)
    {
        _crescentRevealMaterials.Clear();
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null || !source.HasProperty("_Reveal") || !source.HasProperty("_Retreat"))
                {
                    continue;
                }

                Material runtimeMaterial = new Material(source);
                runtimeMaterial.name = source.name + "_RuntimeReveal";
                runtimeMaterial.SetFloat("_Reveal", 0f);
                runtimeMaterial.SetFloat("_Retreat", -0.10f);
                if (runtimeMaterial.HasProperty("_RevealEdgeWidth"))
                {
                    runtimeMaterial.SetFloat("_RevealEdgeWidth", 0.065f);
                }
                materials[i] = runtimeMaterial;
                _crescentRevealMaterials.Add(runtimeMaterial);
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }

        if (_crescentRevealMaterials.Count > 0)
        {
            SteriaLogger.Log($"{LogName}: cached crescent reveal materials={_crescentRevealMaterials.Count}");
        }
    }

    private void AnimateCrescentRevealMaterials()
    {
        if (_crescentRevealMaterials.Count == 0)
        {
            return;
        }

        float revealRaw = Mathf.Clamp01((_elapsed - 0.02f) / 0.48f);
        float reveal = Mathf.Clamp(Mathf.Pow(revealRaw, 1.65f) * 1.06f, 0f, 1.06f);
        float fifoRetreat = reveal - 0.30f;
        float retreatRaw = Mathf.Clamp01((_elapsed - 0.54f) / 0.36f);
        float flushRetreat = -0.10f + Mathf.Pow(retreatRaw, 1.35f) * 1.22f;
        float retreat = Mathf.Clamp(Mathf.Max(fifoRetreat, flushRetreat), -0.10f, 1.12f);

        foreach (Material material in _crescentRevealMaterials)
        {
            if (material == null)
            {
                continue;
            }

            material.SetFloat("_Reveal", reveal);
            material.SetFloat("_Retreat", retreat);
        }
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

            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_ChristashaDawn_RuntimeTrail_WhiteCore", new Vector3(0f, 0.02f, -0.09f), 0.54f, 0.04f, 0.50f, ColorDawnWhite, new Color(1f, 0.78f, 0.10f, 0f), 138);
            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_ChristashaDawn_RuntimeTrail_GoldWake", new Vector3(0f, -0.10f, -0.13f), 0.92f, 0.09f, 0.58f, ColorDawnGold, new Color(1f, 0.48f, 0.02f, 0f), 132);
            CreateRuntimeTrailLayer(_runtimeProjectileRoot.transform, "AB_ChristashaDawn_RuntimeTrail_AmberSpark", new Vector3(0f, 0.17f, -0.16f), 0.20f, 0.02f, 0.36f, ColorDawnAmber, new Color(1f, 0.86f, 0.22f, 0f), 142);
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
        GameObject go = new GameObject("AB_ChristashaDawn_RuntimeProjectileHead");
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
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.52f, 1.20f);
        main.startColor = new ParticleSystem.MinMaxGradient(ColorDawnWhite, ColorDawnGold);
        main.maxParticles = 22;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 42f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.13f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateDawnFadeGradient(0.94f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = CreateRuntimeMaterial(ColorDawnWhite);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 144;
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
            _runtimeImpactWorld = _targetRoot.position + new Vector3(0f, HitYOffset, -0.03f);
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

        if (_impactRoot != null)
        {
            _impactRoot.position = _runtimeImpactWorld;
            if (!_impactRootActivated && raw >= 0.80f)
            {
                _impactRoot.gameObject.SetActive(true);
                ParticleSystem[] systems = _impactRoot.GetComponentsInChildren<ParticleSystem>(true);
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
            AnimateCrescentRevealMaterials();
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
        foreach (Material material in _crescentRevealMaterials)
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
        }
        _crescentRevealMaterials.Clear();
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
                    SteriaLogger.Log($"ChristashaDawnCombat: loaded AssetBundle {path}");
                    return _effectBundle;
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"ChristashaDawnCombat: failed bundle path {path}: {ex.Message}");
            }
        }

        SteriaLogger.Log("ChristashaDawnCombat: AssetBundle not found, using fallback");
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

    private static ParticleSystem.MinMaxGradient CreateDawnFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.58f, 0.05f), 0f),
                new GradientColorKey(new Color(1f, 0.98f, 0.78f), 0.20f),
                new GradientColorKey(new Color(1f, 0.78f, 0.10f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.10f),
                new GradientAlphaKey(peakAlpha * 0.46f, 0.52f),
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

public class DiceAttackEffect_Steria_ChristashaDawnSlashH : DiceAttackEffect_Steria_ChristashaDawnCombatBase
{
    protected override string PrefabName => "ChristashaDawnSlashVerticalPrefab";
    protected override string LogName => "ChristashaDawnSlashHAsV";
    protected override ActionDetail PivotAction => ActionDetail.Slash;
    protected override bool AnchorOnTarget => false;
    protected override bool CenterSlashTravelOnCaster => true;
    protected override bool CenterSlashTravelUsesTargetSide => true;
    protected override float CenterSlashYOffset => -0.06f;
    protected override float Duration => 2.37f;
    protected override float HitYOffset => 0.18f;
    protected override float HitForwardOffset => 0.08f;
    protected override float SlashTravelDistanceDivisor => 7.0f;
    protected override float SlashDistanceScaleMin => 0.95f;
    protected override float SlashDistanceScaleMax => 1.66f;
    protected override float SlashShakeStrength => 0.052f;
    protected override float SlashShakeSpeed => 0.024f;
    protected override float SlashShakeFrequency => 115f;
    protected override float SlashShakeDuration => 0.32f;
}

public class DiceAttackEffect_Steria_ChristashaDawnSlashV : DiceAttackEffect_Steria_ChristashaDawnCombatBase
{
    protected override string PrefabName => "ChristashaDawnSlashVerticalPrefab";
    protected override string LogName => "ChristashaDawnSlashV";
    protected override ActionDetail PivotAction => ActionDetail.Slash;
    protected override bool AnchorOnTarget => false;
    protected override bool CenterSlashTravelOnCaster => true;
    protected override bool CenterSlashTravelUsesTargetSide => true;
    protected override float CenterSlashYOffset => -0.06f;
    protected override float Duration => 2.37f;
    protected override float HitYOffset => 0.18f;
    protected override float HitForwardOffset => 0.08f;
    protected override float SlashTravelDistanceDivisor => 7.0f;
    protected override float SlashDistanceScaleMin => 0.95f;
    protected override float SlashDistanceScaleMax => 1.66f;
    protected override float SlashShakeStrength => 0.052f;
    protected override float SlashShakeSpeed => 0.024f;
    protected override float SlashShakeFrequency => 115f;
    protected override float SlashShakeDuration => 0.32f;
}

public class DiceAttackEffect_Steria_ChristashaDawnPierceNear : DiceAttackEffect_Steria_ChristashaDawnCombatBase
{
    protected override string PrefabName => "ChristashaDawnPierceNearPrefab";
    protected override string LogName => "ChristashaDawnPierceNear";
    protected override ActionDetail PivotAction => ActionDetail.Penetrate;
    protected override bool AnchorOnTarget => false;
    protected override float Duration => 1.02f;
    protected override float HitYOffset => 0.08f;
    protected override float HitForwardOffset => 0.10f;
    protected override float SlashShakeStrength => 0.038f;
    protected override float SlashShakeSpeed => 0.018f;
    protected override float SlashShakeFrequency => 98f;
    protected override float SlashShakeDuration => 0.24f;
}

public class DiceAttackEffect_Steria_ChristashaDawnPierceFar : DiceAttackEffect_Steria_ChristashaDawnCombatBase
{
    protected override string PrefabName => "ChristashaDawnPierceFarPrefab";
    protected override string LogName => "ChristashaDawnPierceFar";
    protected override ActionDetail PivotAction => ActionDetail.Penetrate;
    protected override bool AnchorOnTarget => false;
    protected override bool UseRuntimeProjectile => true;
    protected override float Duration => 1.16f;
    protected override float HitYOffset => 0.10f;
    protected override float HitForwardOffset => 0f;
    protected override float RuntimeProjectileTravelTime => 0.32f;
    protected override float SlashShakeStrength => 0.042f;
    protected override float SlashShakeSpeed => 0.020f;
    protected override float SlashShakeFrequency => 102f;
    protected override float SlashShakeDuration => 0.26f;
}

public class DiceAttackEffect_Steria_ChristashaDawnPierce : DiceAttackEffect_Steria_ChristashaDawnPierceFar
{
}

public class DiceAttackEffect_Steria_ChristashaDawnHit : DiceAttackEffect_Steria_ChristashaDawnCombatBase
{
    protected override string PrefabName => "ChristashaDawnHitPrefab";
    protected override string LogName => "ChristashaDawnHit";
    protected override ActionDetail PivotAction => ActionDetail.Hit;
    protected override bool AnchorOnTarget => true;
    protected override float Duration => 1.10f;
    protected override float HitYOffset => 0.18f;
    protected override float HitForwardOffset => 0.04f;
    protected override float SlashShakeStrength => 0.048f;
    protected override float SlashShakeSpeed => 0.022f;
    protected override float SlashShakeFrequency => 105f;
    protected override float SlashShakeDuration => 0.28f;
}
