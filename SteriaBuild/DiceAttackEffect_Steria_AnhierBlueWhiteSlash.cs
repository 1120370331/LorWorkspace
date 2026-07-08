using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using UnityEngine;
using Steria;

/// <summary>
/// 安希尔普通斩击专用特效：原创蓝白透明刀光。
/// 结构参考寒昼的自定义 DiceAttackEffect 接入方式，但美术资源全部运行时生成。
/// </summary>
public class DiceAttackEffect_Steria_AnhierBlueWhiteSlash : DiceAttackEffect
{
    private const float Duration = 1.18f;
    private const string BundleName = "steria_anhier_bluewhite_slash";
    private const string PrefabName = "AnhierBlueWhiteSlashPrefab";
    private const string PrefabAssetPath = "assets/prefabs/anhierbluewhiteslashprefab.prefab";
    private const string EffectVersion = "AB-align-swing-reveal-20260706";

    private static Texture2D _slashTexture;
    private static Texture2D _sparkTexture;
    private static Texture2D _ringTexture;
    private static Sprite _slashSprite;
    private static Sprite _sparkSprite;
    private static Sprite _ringSprite;
    private static Material _slashMaterial;
    private static Material _sparkMaterial;
    private static Material _ringMaterial;
    private static AssetBundle _effectBundle;
    private static bool _triedLoadBundle;

    private readonly List<GameObject> _objects = new List<GameObject>();
    private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();
    private readonly List<Renderer> _meshRenderers = new List<Renderer>();
    private readonly List<Vector3> _quadStartScale = new List<Vector3>();
    private readonly List<Vector3> _quadEndScale = new List<Vector3>();
    private readonly List<Vector3> _quadStartPos = new List<Vector3>();
    private readonly List<Vector3> _quadEndPos = new List<Vector3>();
    private readonly List<float> _quadStartAlpha = new List<float>();
    private readonly List<float> _quadDelay = new List<float>();
    private readonly List<float> _quadLife = new List<float>();

    private new float _elapsed;
    private Transform _selfRoot;
    private Transform _targetRoot;
    private Direction _atkDir;
    private bool _impactSpawned;
    private bool _usingAssetBundlePrefab;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            base._bHasDamagedEffect = false;
            base._self = self?.model;
            base._selfTransform = self?.atkEffectRoot;
            base._targetTransform = target?.atkEffectRoot;

            _selfRoot = GetSlashPivot(self);
            _targetRoot = target?.atkEffectRoot;
            _atkDir = Direction.RIGHT;
            if (self != null && target != null)
            {
                _atkDir = (Direction)((target.WorldPosition - self.WorldPosition).x > 0f ? 1 : 0);
            }

            _destroyTime = Duration;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash Initialize ERROR: {ex}");
        }
    }

    protected override void Start()
    {
        try
        {
            if (TryCreateAssetBundleEffect())
            {
                _usingAssetBundlePrefab = true;
                PlaySlashSound();
                SteriaEffectHelper.AddScreenShake(0.024f, 0.018f, 84f, 0.24f);
                return;
            }

            EnsureResources();
            SteriaLogger.Log($"AnhierBlueWhiteSlash Start fallback: selfRoot={FormatTransform(_selfRoot)}, targetRoot={FormatTransform(_targetRoot)}, dir={_atkDir}");
            CreateDrawSlash();
            CreateBladeStreakParticles();
            CreateColdMist();
            CreateOpeningCrossFlashes();
            CreateVisibilityBloom();
            PlaySlashSound();
            SteriaEffectHelper.AddScreenShake(0.022f, 0.016f, 80f, 0.22f);
            SteriaLogger.Log($"AnhierBlueWhiteSlash fallback spawned: objects={_objects.Count}, renderers={_meshRenderers.Count}, particles={_systems.Count}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash Start ERROR: {ex}");
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

            GameObject prefab = LoadBundlePrefab(bundle);
            if (prefab == null)
            {
                SteriaLogger.Log($"AnhierBlueWhiteSlash AB fallback: prefab not found. Tried '{PrefabName}' and '{PrefabAssetPath}'");
                return false;
            }

            Transform parent = _selfRoot ?? base._selfTransform;
            if (parent == null)
            {
                return false;
            }

            GameObject main = UnityEngine.Object.Instantiate(prefab);
            main.name = "AnhierBlueWhiteSlash_AB";
            main.transform.SetParent(parent, false);
            main.transform.localPosition = Vector3.zero;
            main.transform.localRotation = _atkDir == Direction.LEFT ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            main.transform.localScale = Vector3.one;

            AlignAssetBundleEffect(main);
            SetLayerAndSorting(main);
            _objects.Add(main);
            SteriaLogger.Log($"AnhierBlueWhiteSlash: using AssetBundle prefab ({EffectVersion})");
            return true;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash AB load failed, using fallback: {ex.Message}");
            return false;
        }
    }

    private static GameObject LoadBundlePrefab(AssetBundle bundle)
    {
        if (bundle == null)
        {
            return null;
        }

        GameObject prefab = bundle.LoadAsset<GameObject>(PrefabName);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = bundle.LoadAsset<GameObject>(PrefabAssetPath);
        if (prefab != null)
        {
            return prefab;
        }

        try
        {
            string[] names = bundle.GetAllAssetNames();
            SteriaLogger.Log("AnhierBlueWhiteSlash AB asset names: " + string.Join(", ", names));
            foreach (string name in names)
            {
                if (name.IndexOf("anhierbluewhiteslash", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    prefab = bundle.LoadAsset<GameObject>(name);
                    if (prefab != null)
                    {
                        return prefab;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash AB asset-name scan failed: {ex.Message}");
        }

        return null;
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
                    SteriaLogger.Log($"AnhierBlueWhiteSlash: loaded AssetBundle {path}");
                    return _effectBundle;
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"AnhierBlueWhiteSlash: failed bundle path {path}: {ex.Message}");
            }
        }

        SteriaLogger.Log("AnhierBlueWhiteSlash: AssetBundle not found, using generated fallback");
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

    private void AlignAssetBundleEffect(GameObject root)
    {
        if (root == null || _targetRoot == null)
        {
            return;
        }

        try
        {
            Vector3 targetLocal = root.transform.InverseTransformPoint(_targetRoot.position);
            float forwardDistance = Mathf.Max(1.2f, Mathf.Abs(targetLocal.x));
            float distanceScale = Mathf.Clamp(forwardDistance / 8.75f, 0.70f, 1.35f);

            Transform bladeRoot = FindChildRecursive(root.transform, "AB_BladeRoot");
            if (bladeRoot != null)
            {
                bladeRoot.localPosition = new Vector3(targetLocal.x * 0.5f, targetLocal.y * 0.30f + 0.04f, 0f);
                bladeRoot.localScale = new Vector3(distanceScale, 1f, 1f);
            }

            Transform impactRoot = FindChildRecursive(root.transform, "AB_ImpactRoot");
            if (impactRoot != null)
            {
                float side = targetLocal.x >= 0f ? 1f : -1f;
                impactRoot.localPosition = new Vector3(targetLocal.x - side * 0.22f, targetLocal.y + 0.12f, -0.02f);
                impactRoot.localScale = Vector3.one;
            }

            SteriaLogger.Log($"AnhierBlueWhiteSlash AB aligned: targetLocal=({targetLocal.x:0.00},{targetLocal.y:0.00},{targetLocal.z:0.00}), scale={distanceScale:0.00}, dir={_atkDir}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash AB align failed: {ex.Message}");
        }
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

    private static void EnsureResources()
    {
        if (_slashMaterial != null && _sparkMaterial != null && _ringMaterial != null)
        {
            return;
        }

        _slashTexture = CreateSlashTexture(768, 256);
        _sparkTexture = CreateSparkTexture(128, 128);
        _ringTexture = CreateRingTexture(256, 256);
        _slashSprite = Sprite.Create(_slashTexture, new Rect(0f, 0f, _slashTexture.width, _slashTexture.height), new Vector2(0.5f, 0.5f), 192f);
        _sparkSprite = Sprite.Create(_sparkTexture, new Rect(0f, 0f, _sparkTexture.width, _sparkTexture.height), new Vector2(0.5f, 0.5f), 128f);
        _ringSprite = Sprite.Create(_ringTexture, new Rect(0f, 0f, _ringTexture.width, _ringTexture.height), new Vector2(0.5f, 0.5f), 160f);

        _slashMaterial = CreateAdditiveMaterial("Steria_Anhier_BlueWhiteSlash_Mat", _slashTexture, new Color(0.62f, 0.9f, 1f, 0.95f));
        _sparkMaterial = CreateAdditiveMaterial("Steria_Anhier_Spark_Mat", _sparkTexture, new Color(0.78f, 0.97f, 1f, 0.95f));
        _ringMaterial = CreateAdditiveMaterial("Steria_Anhier_Ring_Mat", _ringTexture, new Color(0.56f, 0.92f, 1f, 0.82f));
        SteriaLogger.Log($"AnhierBlueWhiteSlash resources ready: slashShader={_slashMaterial.shader?.name}, sparkShader={_sparkMaterial.shader?.name}, ringShader={_ringMaterial.shader?.name}");
    }

    private static Material CreateAdditiveMaterial(string name, Texture2D texture, Color tint)
    {
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Diffuse");
        Material material = new Material(shader);
        material.name = name;
        material.mainTexture = texture;
        material.renderQueue = 3100;
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", tint);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }
        else
        {
            material.color = tint;
        }

        return material;
    }

    private static Texture2D CreateSlashTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Steria_Anhier_Slash_Texture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);

                float arc = 0.54f + Mathf.Sin((u - 0.1f) * Mathf.PI) * 0.23f - u * 0.05f;
                float belly = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                float bladeWidth = Mathf.Lerp(0.018f, 0.11f, belly) * Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0.02f, 0.25f, u));
                float dist = Mathf.Abs(v - arc);

                float core = Mathf.Pow(Mathf.Clamp01(1f - dist / Mathf.Max(0.001f, bladeWidth * 0.28f)), 1.6f);
                float body = Mathf.Pow(Mathf.Clamp01(1f - dist / Mathf.Max(0.001f, bladeWidth)), 2.1f);
                float haze = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v - arc + 0.09f) / Mathf.Max(0.001f, bladeWidth * 2.4f)), 2.6f);

                float taper = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI), 0.45f);
                float tipCut = 1f - Mathf.SmoothStep(0.92f, 1f, u) * 0.45f;
                float alpha = Mathf.Clamp01((core * 0.98f + body * 0.58f + haze * 0.2f) * taper * tipCut);

                float serration = Mathf.Sin(u * 90f + v * 18f) * 0.5f + 0.5f;
                alpha *= Mathf.Lerp(0.78f, 1.08f, serration * Mathf.SmoothStep(0.55f, 1f, u));

                if (alpha <= 0.01f)
                {
                    continue;
                }

                Color deepBlue = new Color(0.05f, 0.45f, 1f, alpha * 0.58f);
                Color iceBlue = new Color(0.22f, 0.82f, 1f, alpha * 0.84f);
                Color white = new Color(0.94f, 1f, 1f, Mathf.Min(1f, alpha * 1.15f));
                Color color = Color.Lerp(deepBlue, iceBlue, body);
                color = Color.Lerp(color, white, Mathf.Clamp01(core * 1.1f));
                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateSparkTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Steria_Anhier_Spark_Texture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float max = Mathf.Min(width, height) * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y) - center;
                float r = p.magnitude / max;
                float axisX = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(p.y) / (max * 0.08f)), 2.3f) * Mathf.Clamp01(1f - Mathf.Abs(p.x) / max);
                float axisY = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(p.x) / (max * 0.08f)), 2.3f) * Mathf.Clamp01(1f - Mathf.Abs(p.y) / max);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - r), 3.4f);
                float alpha = Mathf.Clamp01(axisX + axisY * 0.72f + glow * 0.75f);
                pixels[y * width + x] = new Color(0.72f, 0.95f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateRingTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Steria_Anhier_Ring_Texture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float max = Mathf.Min(width, height) * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = (new Vector2(x, y) - center).magnitude / max;
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.63f) / 0.045f, 2f));
                float inner = Mathf.Exp(-Mathf.Pow((r - 0.32f) / 0.075f, 2f)) * 0.28f;
                float alpha = Mathf.Clamp01((ring + inner) * (1f - Mathf.SmoothStep(0.92f, 1f, r)));
                pixels[y * width + x] = new Color(0.46f, 0.88f, 1f, alpha * 0.72f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private Transform GetSlashPivot(BattleUnitView view)
    {
        try
        {
            return view?.charAppearance?.GetAtkEffectPivot(ActionDetail.Slash) ?? view?.atkEffectRoot;
        }
        catch
        {
            return view?.atkEffectRoot;
        }
    }

    private static string FormatTransform(Transform transform)
    {
        if (transform == null)
        {
            return "null";
        }

        Vector3 world = transform.position;
        Vector3 local = transform.localPosition;
        return $"{transform.name} world=({world.x:0.00},{world.y:0.00},{world.z:0.00}) local=({local.x:0.00},{local.y:0.00},{local.z:0.00}) layer={transform.gameObject.layer}";
    }

    private void CreateDrawSlash()
    {
        if (_selfRoot == null)
        {
            return;
        }

        float side = _atkDir == Direction.RIGHT ? 1f : -1f;
        float yRot = _atkDir == Direction.RIGHT ? 0f : 180f;

        CreateSlashQuad(
            _selfRoot,
            "AnhierBlueWhiteSlash_MainArc",
            new Vector3(-0.32f * side, 0.08f, -0.52f),
            new Vector3(0.24f * side, -0.03f, -0.52f),
            new Vector3(2.05f, 0.68f, 1f),
            new Vector3(5.8f, 1.32f, 1f),
            new Vector3(0f, yRot, -12f * side),
            0f,
            0.38f,
            0.92f,
            168);

        CreateSlashQuad(
            _selfRoot,
            "AnhierBlueWhiteSlash_WhiteEdge",
            new Vector3(-0.18f * side, 0.08f, -0.56f),
            new Vector3(0.34f * side, -0.04f, -0.56f),
            new Vector3(1.28f, 0.24f, 1f),
            new Vector3(4.6f, 0.52f, 1f),
            new Vector3(0f, yRot, -12f * side),
            0.015f,
            0.28f,
            1f,
            176);

        CreateSlashQuad(
            _selfRoot,
            "AnhierBlueWhiteSlash_Backwash",
            new Vector3(-0.64f * side, 0.25f, -0.62f),
            new Vector3(-0.94f * side, 0.26f, -0.62f),
            new Vector3(1.7f, 0.42f, 1f),
            new Vector3(3.9f, 0.92f, 1f),
            new Vector3(0f, yRot, -25f * side),
            0.05f,
            0.36f,
            0.42f,
            150);
    }

    private void CreateOpeningCrossFlashes()
    {
        if (_targetRoot == null)
        {
            return;
        }

        float side = _atkDir == Direction.RIGHT ? 1f : -1f;
        float yRot = _atkDir == Direction.RIGHT ? 0f : 180f;

        CreateSlashQuad(
            _targetRoot,
            "AnhierBlueWhiteSlash_TargetCutA",
            new Vector3(-0.55f * side, 0.36f, -0.48f),
            new Vector3(0.12f * side, 0.26f, -0.48f),
            new Vector3(0.82f, 0.18f, 1f),
            new Vector3(3.2f, 0.48f, 1f),
            new Vector3(0f, yRot, 18f * side),
            0.11f,
            0.36f,
            0.64f,
            186);

        CreateSlashQuad(
            _targetRoot,
            "AnhierBlueWhiteSlash_TargetCutB",
            new Vector3(0.38f * side, 0.04f, -0.5f),
            new Vector3(-0.18f * side, 0.34f, -0.5f),
            new Vector3(0.72f, 0.16f, 1f),
            new Vector3(2.35f, 0.42f, 1f),
            new Vector3(0f, yRot, -38f * side),
            0.17f,
            0.28f,
            0.5f,
            188);
    }

    private void CreateVisibilityBloom()
    {
        Transform parent = _targetRoot ?? _selfRoot;
        if (parent == null)
        {
            return;
        }

        float side = _atkDir == Direction.RIGHT ? 1f : -1f;
        float yRot = _atkDir == Direction.RIGHT ? 0f : 180f;

        CreateSlashQuad(
            parent,
            "AnhierBlueWhiteSlash_FrontBloom",
            new Vector3(-0.18f * side, 0.22f, -0.28f),
            new Vector3(0.08f * side, 0.22f, -0.28f),
            new Vector3(1.6f, 0.44f, 1f),
            new Vector3(4.8f, 0.9f, 1f),
            new Vector3(0f, yRot, -16f * side),
            0.02f,
            0.56f,
            1f,
            420);
    }

    private void CreateSlashQuad(Transform parent, string name, Vector3 startPos, Vector3 endPos,
        Vector3 startScale, Vector3 endScale, Vector3 euler, float delay, float life, float alpha, int sortingOrder)
    {
        GameObject quad = new GameObject(name);
        quad.name = name;
        quad.layer = 8;

        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = startPos;
        quad.transform.localRotation = Quaternion.Euler(euler);
        quad.transform.localScale = startScale;

        SpriteRenderer renderer = quad.AddComponent<SpriteRenderer>();
        renderer.sprite = _slashSprite;
        renderer.material = new Material(_slashMaterial);
        renderer.color = new Color(0.72f, 0.94f, 1f, 0f);
        renderer.sortingOrder = sortingOrder + 300;
        SetRendererAlpha(renderer, 0f);

        _objects.Add(quad);
        _meshRenderers.Add(renderer);
        _quadStartScale.Add(startScale);
        _quadEndScale.Add(endScale);
        _quadStartPos.Add(startPos);
        _quadEndPos.Add(endPos);
        _quadStartAlpha.Add(alpha);
        _quadDelay.Add(delay);
        _quadLife.Add(life);
    }

    private void CreateBladeStreakParticles()
    {
        if (_selfRoot == null)
        {
            return;
        }

        GameObject edge = CreateParticleObject(_selfRoot, "AnhierBlueWhiteSlash_IceEdge");
        edge.transform.localPosition = new Vector3(0f, 0.16f, -0.58f);
        edge.transform.localRotation = Quaternion.Euler(0f, 0f, _atkDir == Direction.RIGHT ? -10f : 10f);
        ParticleSystem ps = edge.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startDelay = 0.03f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.46f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.8f, 8.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.72f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.86f, 1f, 0.76f), new Color(1f, 1f, 1f, 0.95f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 34) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.35f, 0.22f, 0.02f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        float side = _atkDir == Direction.RIGHT ? 1f : -1f;
        vel.x = new ParticleSystem.MinMaxCurve(8f * side, 18f * side);
        vel.y = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateBlueWhiteFadeGradient(1f);

        ParticleSystemRenderer renderer = edge.GetComponent<ParticleSystemRenderer>();
        renderer.material = _slashMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.18f;
        renderer.lengthScale = 4.4f;
        renderer.sortingOrder = 482;

        GameObject sparks = CreateParticleObject(_targetRoot ?? _selfRoot, "AnhierBlueWhiteSlash_StarShards");
        sparks.transform.localPosition = _targetRoot != null ? new Vector3(0f, 0.28f, -0.42f) : new Vector3(0.9f * side, 0.18f, -0.42f);
        ParticleSystem sparkPs = sparks.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule sparkMain = sparkPs.main;
        sparkMain.duration = 0.18f;
        sparkMain.loop = false;
        sparkMain.startDelay = 0.13f;
        sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.74f);
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 7.6f);
        sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
        sparkMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28318f);
        sparkMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.9f, 1f, 0.72f), new Color(1f, 1f, 1f, 0.95f));
        sparkMain.gravityModifier = 0.04f;
        sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule sparkEmission = sparkPs.emission;
        sparkEmission.rateOverTime = 0f;
        sparkEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule sparkShape = sparkPs.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 0.38f;

        ParticleSystem.ColorOverLifetimeModule sparkColor = sparkPs.colorOverLifetime;
        sparkColor.enabled = true;
        sparkColor.color = CreateBlueWhiteFadeGradient(0.88f);

        ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.material = _sparkMaterial;
        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkRenderer.sortingOrder = 496;
    }

    private void CreateColdMist()
    {
        Transform parent = _targetRoot ?? _selfRoot;
        if (parent == null)
        {
            return;
        }

        GameObject mist = CreateParticleObject(parent, "AnhierBlueWhiteSlash_ColdMist");
        mist.transform.localPosition = _targetRoot != null ? new Vector3(0f, 0.18f, -0.62f) : new Vector3(0.45f, 0.12f, -0.62f);
        ParticleSystem ps = mist.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startDelay = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.75f, 1.65f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28318f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.14f, 0.56f, 1f, 0.26f), new Color(0.88f, 1f, 1f, 0.38f));
        main.gravityModifier = -0.04f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 18) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.52f;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.55f;
        noise.frequency = 0.72f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = CreateBlueWhiteFadeGradient(0.36f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.18f);
        curve.AddKey(0.35f, 1f);
        curve.AddKey(1f, 1.25f);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer renderer = mist.GetComponent<ParticleSystemRenderer>();
        renderer.material = _slashMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 438;
    }

    private void CreateImpact()
    {
        if (_targetRoot == null)
        {
            return;
        }

        GameObject ring = new GameObject("AnhierBlueWhiteSlash_ImpactRing");
        ring.name = "AnhierBlueWhiteSlash_ImpactRing";
        ring.layer = 8;

        ring.transform.SetParent(_targetRoot, false);
        ring.transform.localPosition = new Vector3(0f, 0.28f, -0.24f);
        ring.transform.localRotation = Quaternion.Euler(0f, 0f, _atkDir == Direction.RIGHT ? -10f : 10f);
        ring.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
        renderer.sprite = _ringSprite;
        renderer.material = new Material(_ringMaterial);
        renderer.color = new Color(0.78f, 0.97f, 1f, 0.92f);
        renderer.sortingOrder = 500;
        SetRendererAlpha(renderer, 0.92f);

        _objects.Add(ring);
        _meshRenderers.Add(renderer);
        _quadStartScale.Add(new Vector3(0.7f, 0.7f, 1f));
        _quadEndScale.Add(new Vector3(2.4f, 1.34f, 1f));
        _quadStartPos.Add(ring.transform.localPosition);
        _quadEndPos.Add(ring.transform.localPosition);
        _quadStartAlpha.Add(0.72f);
        _quadDelay.Add(0.18f);
        _quadLife.Add(0.26f);

        SteriaEffectHelper.AddScreenShake(0.028f, 0.02f, 88f, 0.18f);
    }

    private GameObject CreateParticleObject(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.layer = 8;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = true;
        _objects.Add(go);
        _systems.Add(ps);
        return go;
    }

    private static ParticleSystem.MinMaxGradient CreateBlueWhiteFadeGradient(float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.2f, 0.66f, 1f), 0f),
                new GradientColorKey(new Color(0.9f, 1f, 1f), 0.28f),
                new GradientColorKey(new Color(0.42f, 0.78f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.12f),
                new GradientAlphaKey(peakAlpha * 0.48f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private void PlaySlashSound()
    {
        try
        {
            base._self?.view?.charAppearance?.soundInfo?.PlaySound((MotionDetail)0, true);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash sound error: {ex.Message}");
        }
    }

    protected override void Update()
    {
        try
        {
            _elapsed += Time.deltaTime;

            if (_usingAssetBundlePrefab)
            {
                if (_elapsed >= Duration)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
                return;
            }

            if (!_impactSpawned && _elapsed >= 0.18f)
            {
                _impactSpawned = true;
                CreateImpact();
            }

            UpdateSlashQuads();

            if (_elapsed >= Duration)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"AnhierBlueWhiteSlash Update ERROR: {ex.Message}");
        }
    }

    private void UpdateSlashQuads()
    {
        int count = Mathf.Min(_meshRenderers.Count, _quadLife.Count);
        for (int i = 0; i < count; i++)
        {
            Renderer renderer = _meshRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            GameObject obj = renderer.gameObject;
            float local = (_elapsed - _quadDelay[i]) / Mathf.Max(0.001f, _quadLife[i]);
            if (local < 0f)
            {
                SetRendererAlpha(renderer, 0f);
                continue;
            }

            float t = Mathf.Clamp01(local);
            float expand = EaseOutExpo(t);
            obj.transform.localScale = Vector3.Lerp(_quadStartScale[i], _quadEndScale[i], expand);
            obj.transform.localPosition = Vector3.Lerp(_quadStartPos[i], _quadEndPos[i], EaseOutCubic(t));

            float alpha;
            if (t < 0.16f)
            {
                alpha = Mathf.Lerp(0f, _quadStartAlpha[i], t / 0.16f);
            }
            else
            {
                alpha = Mathf.Lerp(_quadStartAlpha[i], 0f, Mathf.Pow((t - 0.16f) / 0.84f, 1.35f));
            }

            SetRendererAlpha(renderer, alpha);
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        t = 1f - t;
        return 1f - t * t * t;
    }

    private static float EaseOutExpo(float t)
    {
        t = Mathf.Clamp01(t);
        return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    }

    private static void SetRendererAlpha(Renderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            Color spriteColor = spriteRenderer.color;
            spriteColor.r = 0.72f;
            spriteColor.g = 0.94f;
            spriteColor.b = 1f;
            spriteColor.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = spriteColor;
        }

        SetMaterialAlpha(renderer.material, alpha);
    }

    private static void SetMaterialAlpha(Material material, float alpha)
    {
        if (material == null)
        {
            return;
        }

        Color color = new Color(0.56f, 0.88f, 1f, Mathf.Clamp01(alpha));
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        else
        {
            material.color = color;
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
        _meshRenderers.Clear();
        _quadStartScale.Clear();
        _quadEndScale.Clear();
        _quadStartPos.Clear();
        _quadEndPos.Clear();
        _quadStartAlpha.Clear();
        _quadDelay.Clear();
        _quadLife.Clear();
    }
}
