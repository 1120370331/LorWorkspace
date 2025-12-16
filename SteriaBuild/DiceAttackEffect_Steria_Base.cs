using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// Steria特效基类 - 封装通用的特效逻辑
/// </summary>
public abstract class DiceAttackEffect_Steria_Base : DiceAttackEffect
{
    // 特效配置
    protected SteriaEffectConfig _config;

    // Quad管理
    protected List<GameObject> _effectQuads = new List<GameObject>();
    protected List<MeshRenderer> _renderers = new List<MeshRenderer>();
    protected List<Vector3> _startScales = new List<Vector3>();
    protected List<Vector3> _endScales = new List<Vector3>();
    protected List<Vector3> _startPositions = new List<Vector3>();
    protected List<Vector3> _endPositions = new List<Vector3>();

    // 视图引用
    protected BattleUnitView _selfView;
    protected BattleUnitView _targetView;

    // 时间控制
    protected float _duration;
    protected new float _elapsed = 0f;

    /// <summary>
    /// 子类必须实现：返回特效配置
    /// </summary>
    protected abstract SteriaEffectConfig GetConfig();

    /// <summary>
    /// 子类可重写：自定义初始化逻辑
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// 子类可重写：自定义更新逻辑
    /// </summary>
    protected virtual void OnUpdate(float progress) { }

    /// <summary>
    /// 子类可重写：自定义清理逻辑
    /// </summary>
    protected virtual void OnCleanup() { }

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        _config = GetConfig();
        if (_config == null)
        {
            Debug.LogError("[Steria] Effect config is null!");
            return;
        }

        this._self = self.model;
        this._selfView = self;
        this._targetView = target;

        // 设置挂载点
        SetupTransform(self, target);

        // 设置时间
        _duration = _config.Duration;
        this._destroyTime = _duration;
        this._elapsed = 0f;

        // 创建特效
        CreateEffects();

        // 添加震动
        if (_config.Shake.IsValid)
        {
            SteriaEffectHelper.AddScreenShake(
                _config.Shake.X,
                _config.Shake.Y,
                _config.Shake.Speed,
                _config.Shake.Duration
            );
        }

        // 子类自定义初始化
        OnInitialize();

        SteriaLogger.Log($"{_config.EffectName}: Initialized with {_effectQuads.Count} quads");
    }

    protected virtual void SetupTransform(BattleUnitView self, BattleUnitView target)
    {
        Transform parent = null;

        switch (_config.Target)
        {
            case EffectTarget.Self:
                parent = self.atkEffectRoot;
                break;
            case EffectTarget.Target:
                parent = target?.atkEffectRoot;
                break;
            case EffectTarget.World:
                parent = null;
                break;
        }

        if (parent != null)
        {
            base.transform.parent = parent;
            base.transform.localPosition = _config.RootOffset;
            base.transform.localRotation = Quaternion.identity;
            base.transform.localScale = Vector3.one;
        }
        else
        {
            base.transform.parent = null;
            base.transform.position = _config.RootOffset;
            base.transform.rotation = Quaternion.identity;
            base.transform.localScale = Vector3.one;
        }
    }

    protected virtual void CreateEffects()
    {
        if (_config.Quads == null) return;

        for (int i = 0; i < _config.Quads.Length; i++)
        {
            CreateQuadEffect(_config.Quads[i], i);
        }
    }

    protected virtual void CreateQuadEffect(QuadEffectConfig quadConfig, int index)
    {
        try
        {
            Material material = SteriaEffectSprites.GetEffectMaterial(
                quadConfig.TextureName,
                true,
                quadConfig.BrightnessThreshold
            );

            if (material == null)
            {
                SteriaLogger.Log($"ERROR: Material not found for {quadConfig.TextureName}");
                return;
            }

            Vector3 startScale = quadConfig.GetStartScale();
            Vector3 endScale = quadConfig.GetEndScale();

            GameObject quad = SteriaEffectHelper.CreateEffectQuad(
                $"{_config.EffectName}_{index}",
                material,
                base.transform,
                quadConfig.LocalPosition,
                quadConfig.RotationZ,
                startScale,
                100 + index
            );

            if (quad != null)
            {
                var renderer = quad.GetComponent<MeshRenderer>();

                _effectQuads.Add(quad);
                _renderers.Add(renderer);
                _startScales.Add(startScale);
                _endScales.Add(endScale);
                _startPositions.Add(quadConfig.LocalPosition);
                _endPositions.Add(quadConfig.LocalPosition + quadConfig.PositionOffset);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] Error creating quad effect: {ex}");
        }
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        // 更新所有Quad
        for (int i = 0; i < _effectQuads.Count; i++)
        {
            UpdateQuad(i, progress);
        }

        // 子类自定义更新
        OnUpdate(progress);

        // 销毁检查
        if (_elapsed >= _duration)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    protected virtual void UpdateQuad(int index, float progress)
    {
        if (index >= _effectQuads.Count || _effectQuads[index] == null || _renderers[index] == null)
            return;

        var quad = _effectQuads[index];
        var renderer = _renderers[index];
        var quadConfig = _config.Quads[index];

        // 缩放动画
        if (quadConfig.AnimateScale)
        {
            float scaleProgress = SteriaEffectHelper.EaseOutQuad(progress);
            quad.transform.localScale = Vector3.Lerp(_startScales[index], _endScales[index], scaleProgress);
        }
        else
        {
            quad.transform.localScale = _endScales[index];
        }

        // 位置动画
        if (quadConfig.AnimatePosition)
        {
            float posProgress = SteriaEffectHelper.EaseOutQuad(progress);
            quad.transform.localPosition = Vector3.Lerp(_startPositions[index], _endPositions[index], posProgress);
        }

        // 透明度
        float alpha = SteriaEffectHelper.CalculateFadeAlpha(
            progress,
            _config.FadeInEnd,
            _config.FadeOutStart,
            _config.MaxAlpha
        );

        if (renderer.material != null)
        {
            SteriaEffectHelper.SetAdditiveMaterialAlpha(renderer.material, alpha, quadConfig.Tint);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 子类自定义清理
        OnCleanup();

        // 清理所有Quad
        foreach (var quad in _effectQuads)
        {
            if (quad != null) UnityEngine.Object.Destroy(quad);
        }
        _effectQuads.Clear();
        _renderers.Clear();
        _startScales.Clear();
        _endScales.Clear();
        _startPositions.Clear();
        _endPositions.Clear();
    }

    #region 辅助方法供子类使用

    /// <summary>
    /// 在指定位置创建额外的Quad
    /// </summary>
    protected GameObject CreateAdditionalQuad(QuadEffectConfig config, Transform parent, int sortingOrder)
    {
        Material material = SteriaEffectSprites.GetEffectMaterial(config.TextureName, true, config.BrightnessThreshold);
        if (material == null) return null;

        return SteriaEffectHelper.CreateEffectQuad(
            $"{_config.EffectName}_extra",
            material,
            parent,
            config.LocalPosition,
            config.RotationZ,
            config.GetStartScale(),
            sortingOrder
        );
    }

    /// <summary>
    /// 添加Quad到管理列表
    /// </summary>
    protected void RegisterQuad(GameObject quad, QuadEffectConfig config)
    {
        if (quad == null) return;

        var renderer = quad.GetComponent<MeshRenderer>();
        _effectQuads.Add(quad);
        _renderers.Add(renderer);
        _startScales.Add(config.GetStartScale());
        _endScales.Add(config.GetEndScale());
        _startPositions.Add(config.LocalPosition);
        _endPositions.Add(config.LocalPosition + config.PositionOffset);
    }

    #endregion
}
