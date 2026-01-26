using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;

/// <summary>
/// 薇莉亚荆棘施法特效 - 远程攻击
/// 同时在目标位置创建受击特效（荆棘囚笼）
/// </summary>
public class DiceAttackEffect_VeliaThorn_F : DiceAttackEffect
{
    private const float TOTAL_DURATION = 1.2f;
    private const float CAST_SCALE = 1.75f;      // 施法特效缩小50%
    private const float DAMAGED_SCALE = 2f;      // 命中特效缩小50%
    private const float CAST_OFFSET_X = -2.5f;   // 施法特效右移250px（负值因为翻转时localPosition也会翻转）
    private const float DAMAGED_DURATION = 0.6f; // 受击特效持续时间（更快）

    // 施法特效帧（从少到多）
    private static readonly string[] CastFrames = new string[]
    {
        "荆棘_施法_阈值40", "荆棘_施法_阈值35", "荆棘_施法_阈值30",
        "荆棘_施法_阈值25", "荆棘_施法_阈值20", "荆棘_施法_阈值15"
    };

    // 受击特效帧
    private static readonly string[] DamagedFrames = new string[]
    {
        "荆棘囚笼_帧1", "荆棘囚笼_帧2", "荆棘囚笼_帧3",
        "荆棘囚笼_帧4", "荆棘囚笼_帧5", "荆棘囚笼_帧6"
    };

    // 静态缓存避免重复加载导致卡顿
    private static List<Sprite> _cachedCastSprites = null;
    private static List<Sprite> _cachedDamagedSprites = null;
    private static bool _spritesLoaded = false;

    private SpriteRenderer _castRenderer;
    private SpriteRenderer _damagedRenderer;
    private GameObject _castObj;
    private GameObject _damagedObj;
    private float _localElapsed = 0f;
    private bool _isInitialized = false;

    protected override void Awake()
    {
        Steria.SteriaLogger.Log("VeliaThorn_F: Awake");
    }

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            Steria.SteriaLogger.Log("VeliaThorn_F: Initialize started");
            _bHasDamagedEffect = true;
            _destroyTime = TOTAL_DURATION;
            _localElapsed = 0f;

            // 设置位置
            Transform pivot = self.charAppearance.GetAtkEffectPivot(ActionDetail.Fire);
            if (pivot == null) pivot = self.charAppearance.atkEffectRoot;
            transform.parent = pivot;
            _selfTransform = pivot;
            _targetTransform = target.atkEffectRoot;
            transform.localPosition = new Vector3(0f, 1f, 0f);

            // 加载精灵
            LoadSprites();

            // 创建施法特效
            CreateCastEffect();

            // 创建受击特效（在目标位置）
            CreateDamagedEffect(target);

            _isInitialized = true;
            Steria.SteriaLogger.Log($"VeliaThorn_F: Init done, cast={_cachedCastSprites?.Count ?? 0}, damaged={_cachedDamagedSprites?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Steria.SteriaLogger.Log($"VeliaThorn_F ERROR: {ex}");
        }
    }

    private void LoadSprites()
    {
        // 使用静态缓存，只加载一次
        if (_spritesLoaded) return;

        _cachedCastSprites = new List<Sprite>();
        foreach (string name in CastFrames)
        {
            var tex = Steria.SteriaEffectSprites.GetTexture(name, false, 0f);
            if (tex != null)
            {
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                _cachedCastSprites.Add(spr);
            }
        }

        _cachedDamagedSprites = new List<Sprite>();
        foreach (string name in DamagedFrames)
        {
            var tex = Steria.SteriaEffectSprites.GetTexture(name, false, 0f);
            if (tex != null)
            {
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                _cachedDamagedSprites.Add(spr);
            }
        }

        _spritesLoaded = true;
        Steria.SteriaLogger.Log($"Sprites cached: cast={_cachedCastSprites.Count}, damaged={_cachedDamagedSprites.Count}");
    }

    private void CreateCastEffect()
    {
        _castObj = new GameObject("CastEffect");
        _castObj.transform.SetParent(transform);
        _castObj.transform.localPosition = new Vector3(CAST_OFFSET_X, 0f, 0f);
        _castObj.transform.localScale = Vector3.one * CAST_SCALE;

        _castRenderer = _castObj.AddComponent<SpriteRenderer>();
        _castRenderer.sortingOrder = 100;
        SetupAdditiveMaterial(_castRenderer);
        _castRenderer.color = new Color(1f, 1f, 1f, 0f); // 初始透明

        if (_cachedCastSprites != null && _cachedCastSprites.Count > 0)
            _castRenderer.sprite = _cachedCastSprites[0];
    }

    private void CreateDamagedEffect(BattleUnitView target)
    {
        _damagedObj = new GameObject("DamagedEffect");
        _damagedObj.transform.SetParent(target.atkEffectRoot);
        _damagedObj.transform.localPosition = Vector3.zero;
        _damagedObj.transform.localScale = Vector3.one * DAMAGED_SCALE;

        _damagedRenderer = _damagedObj.AddComponent<SpriteRenderer>();
        _damagedRenderer.sortingOrder = 100;
        SetupAdditiveMaterial(_damagedRenderer);

        if (_cachedDamagedSprites != null && _cachedDamagedSprites.Count > 0)
            _damagedRenderer.sprite = _cachedDamagedSprites[0];

        // 添加自动销毁
        var destruct = _damagedObj.AddComponent<AutoDestruct>();
        destruct.time = TOTAL_DURATION + 0.5f;
    }

    private void SetupAdditiveMaterial(SpriteRenderer sr)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            sr.material = new Material(shader);
            sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
    }

    protected override void Update()
    {
        if (!_isInitialized) { base.Update(); return; }

        _localElapsed += Time.deltaTime;
        _elapsed = _localElapsed;
        float progress = _localElapsed / TOTAL_DURATION;

        // 更新施法特效 - 从左到右淡入，从左到右淡出
        if (_cachedCastSprites != null && _cachedCastSprites.Count > 0 && _castRenderer != null)
        {
            int idx = Mathf.Clamp(Mathf.FloorToInt(progress * _cachedCastSprites.Count * 2), 0, _cachedCastSprites.Count - 1);
            _castRenderer.sprite = _cachedCastSprites[idx];

            if (progress < 0.25f)
            {
                // 淡入阶段：从左到右显示（通过UV裁剪）
                float fadeIn = progress / 0.25f; // 0 to 1
                _castRenderer.material.mainTextureOffset = new Vector2(1f - fadeIn, 0f);
                _castRenderer.material.mainTextureScale = new Vector2(fadeIn, 1f);
                _castRenderer.color = Color.white;
            }
            else if (progress < 0.7f)
            {
                // 保持阶段：完全显示
                _castRenderer.material.mainTextureOffset = Vector2.zero;
                _castRenderer.material.mainTextureScale = Vector2.one;
                _castRenderer.color = Color.white;
            }
            else
            {
                // 淡出阶段：从左到右消失（通过UV裁剪）
                float fadeOut = (progress - 0.7f) / 0.3f; // 0 to 1
                _castRenderer.material.mainTextureOffset = new Vector2(fadeOut, 0f);
                _castRenderer.material.mainTextureScale = new Vector2(1f - fadeOut, 1f);
                _castRenderer.color = Color.white;
            }
        }

        // 更新受击特效 - 蓄势效果：前30%停在第一帧，后70%播放剩余5帧
        if (_cachedDamagedSprites != null && _cachedDamagedSprites.Count > 0 && _damagedRenderer != null)
        {
            float damagedProgress = _localElapsed / DAMAGED_DURATION;

            // 蓄势效果
            int idx;
            if (damagedProgress < 0.3f)
            {
                idx = 0; // 第一帧停顿蓄势
            }
            else
            {
                float remainProgress = (damagedProgress - 0.3f) / 0.7f;
                idx = 1 + Mathf.Clamp(Mathf.FloorToInt(remainProgress * 5), 0, 4);
            }
            _damagedRenderer.sprite = _cachedDamagedSprites[idx];

            // 立即显示，后面淡出
            float alpha = damagedProgress < 0.7f ? 1f : 1f - (damagedProgress - 0.7f) / 0.3f;
            _damagedRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        // 朝向（反转）
        if (_selfTransform != null && _targetTransform != null)
        {
            transform.localScale = _selfTransform.position.x < _targetTransform.position.x
                ? new Vector3(-1f, 1f, 1f) : Vector3.one;
        }

        if (_localElapsed >= _destroyTime)
            UnityEngine.Object.Destroy(gameObject);
    }

    protected override void OnDestroy()
    {
        if (_castObj != null) UnityEngine.Object.Destroy(_castObj);
    }
}
