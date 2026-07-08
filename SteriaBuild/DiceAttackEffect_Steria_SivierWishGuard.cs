using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using Steria;
using UnityEngine;

/// <summary>
/// 希维尔防御骰专用特效：蓝紫色格挡法阵。
/// 挂载在防御方(self)自身的Guard锚点上，不朝向目标移动。
/// </summary>
public class DiceAttackEffect_Steria_SivierWishGuard : DiceAttackEffect
{
    private const string BundleName = "steria_sivier_wish_guard";
    private const string PrefabName = "SivierWishGuardPrefab";
    private const string LogName = "SivierWishGuard";
    private const string EffectVersion = "AB-sivier-wish-guard-20260707";
    private const float Duration = 0.92f;

    private static readonly Color ColorVioletDeep = new Color(0.46f, 0.14f, 0.86f, 0.85f);
    private static readonly Color ColorBluePlasma = new Color(0.20f, 0.48f, 1f, 0.90f);
    private static readonly Color ColorWhiteCore = new Color(0.90f, 0.95f, 1f, 0.95f);

    private static AssetBundle _effectBundle;
    private static bool _triedLoadBundle;

    private readonly List<GameObject> _objects = new List<GameObject>();
    private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();

    private BattleUnitView _selfView;
    private Transform _selfRoot;
    private new float _elapsed;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            base._bHasDamagedEffect = false;
            base._self = self?.model;
            base._selfTransform = self?.atkEffectRoot;

            _selfView = self;
            _selfRoot = GetPivot(self) ?? self?.atkEffectRoot;
            base._targetTransform = target?.atkEffectRoot;
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
                PlayGuardSound();
                SteriaEffectHelper.AddScreenShake(0.016f, 0.012f, 66f, 0.16f);
                return;
            }

            CreateFallbackEffect();
            PlayGuardSound();
            SteriaEffectHelper.AddScreenShake(0.012f, 0.010f, 60f, 0.14f);
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

            Transform parent = _selfRoot ?? base._selfTransform;
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

            SetLayerAndSorting(main);
            _objects.Add(main);

            SteriaLogger.Log($"{LogName}: using AssetBundle prefab ({EffectVersion})");
            return true;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"{LogName} AB load failed, using fallback: {ex.Message}");
            return false;
        }
    }

    private void CreateFallbackEffect()
    {
        Transform parent = _selfRoot ?? base._selfTransform;
        if (parent == null)
        {
            return;
        }

        GameObject go = new GameObject(LogName + "_FallbackCircle");
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.15f, -0.05f);
        _objects.Add(go);

        CreateRingBurst(go.transform, "AB_Guard_Fallback_OuterViolet", ColorVioletDeep, 1.55f, 0f, 0.02f);
        CreateRingBurst(go.transform, "AB_Guard_Fallback_InnerBlue", ColorBluePlasma, 1.05f, 0.05f, 0.01f);
        CreateRingBurst(go.transform, "AB_Guard_Fallback_CoreWhite", ColorWhiteCore, 0.55f, 0.10f, 0f);

        GameObject spray = new GameObject(LogName + "_FallbackSpray");
        spray.layer = 8;
        spray.transform.SetParent(go.transform, false);
        _objects.Add(spray);

        ParticleSystem ps = spray.AddComponent<ParticleSystem>();
        _systems.Add(ps);
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.60f;
        main.startDelay = 0.26f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.40f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
        main.startColor = new ParticleSystem.MinMaxGradient(ColorVioletDeep, ColorWhiteCore);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.85f;
        shape.arc = 360f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateGuardFadeGradient(0.85f);

        ParticleSystemRenderer renderer = spray.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.10f;
        renderer.lengthScale = 2.0f;
        renderer.sortingOrder = 128;
    }

    private void CreateRingBurst(Transform parent, string name, Color color, float size, float delay, float zOffset)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, zOffset);
        _objects.Add(go);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        _systems.Add(ps);
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = delay + 0.52f;
        main.startDelay = delay;
        main.startLifetime = 0.42f;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(color, Mathf.Min(1f, color.a));

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.28f, 1.10f), new Keyframe(1f, 0.94f)));

        var rotOver = ps.rotationOverLifetime;
        rotOver.enabled = true;
        rotOver.z = new ParticleSystem.MinMaxCurve(2.4f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 120;
    }

    private void PlayGuardSound()
    {
        try
        {
            _selfView?.charAppearance?.soundInfo?.PlaySound(MotionDetail.G, true);
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
                    SteriaLogger.Log($"{LogName}: loaded AssetBundle {path}");
                    return _effectBundle;
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"{LogName}: failed bundle path {path}: {ex.Message}");
            }
        }

        SteriaLogger.Log($"{LogName}: AssetBundle not found, using fallback");
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

    private static Transform GetPivot(BattleUnitView view)
    {
        try
        {
            return view?.charAppearance?.GetAtkEffectPivot(ActionDetail.Guard) ?? view?.atkEffectRoot;
        }
        catch
        {
            return view?.atkEffectRoot;
        }
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color baseColor, float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(baseColor.r * 0.70f, baseColor.g * 0.70f, baseColor.b * 0.78f), 0f),
                new GradientColorKey(new Color(0.92f, 0.95f, 1f), 0.22f),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.12f),
                new GradientAlphaKey(peakAlpha * 0.50f, 0.52f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static ParticleSystem.MinMaxGradient CreateGuardFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.46f, 0.14f, 0.86f), 0f),
                new GradientColorKey(new Color(0.85f, 0.90f, 1f), 0.24f),
                new GradientColorKey(new Color(0.20f, 0.48f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.10f),
                new GradientAlphaKey(peakAlpha * 0.46f, 0.50f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
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
