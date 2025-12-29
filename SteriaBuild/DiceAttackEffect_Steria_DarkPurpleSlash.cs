using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 忘却之梦专用特效 - 暗紫色黑洞吞噬效果
/// 画面变暗 -> 黑洞出现 -> 穿透敌人 -> 画面变亮
/// </summary>
public class DiceAttackEffect_Steria_DarkPurpleSlash : DiceAttackEffect
{
    // 视图引用
    private BattleUnitView _selfView;
    private BattleUnitView _targetView;

    // 时间控制
    private float _duration = 1.5f;  // 总持续时间（延长以便看清穿透效果）
    private new float _elapsed = 0f;

    // 特效阶段
    private enum Phase { DarkenScreen, BlackHole, Pierce, Brighten }
    private Phase _currentPhase = Phase.DarkenScreen;

    // 画面滤镜
    private GameObject _screenOverlay;
    private MeshRenderer _overlayRenderer;
    private Material _overlayMaterial;

    // 黑洞特效
    private GameObject _blackHoleQuad;
    private MeshRenderer _blackHoleRenderer;

    // 斩击特效
    private GameObject _slashQuad;
    private MeshRenderer _slashRenderer;

    // 颜色配置
    private readonly Color _darkPurple = new Color(0.15f, 0.05f, 0.25f, 0.85f);
    private readonly Color _brightPurple = new Color(0.6f, 0.3f, 0.9f, 1f);

    // 角色移动相关
    private Vector3 _selfStartPos;      // 安希尔初始位置
    private Vector3 _targetPos;         // 敌人位置
    private Vector3 _pierceEndPos;      // 穿透结束位置（敌人后方）
    private bool _hasCameraControl = false;
    private bool _pierceStarted = false;

    // 水流模糊滤镜
    private CameraFilterPack_Blur_Radial_Fast _blurFilter;
    private bool _hasBlurFilter = false;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        this._self = self.model;
        this._selfView = self;
        this._targetView = target;
        this._destroyTime = _duration;

        // 保存初始位置
        _selfStartPos = self.WorldPosition;
        _targetPos = target != null ? target.WorldPosition : _selfStartPos;

        // 计算穿透结束位置（敌人后方约1.5个单位，不要太远）
        Vector3 direction = (_targetPos - _selfStartPos).normalized;
        _pierceEndPos = _targetPos + direction * 1.5f;

        // 设置位置到目标身上
        if (target != null && target.atkEffectRoot != null)
        {
            transform.parent = target.atkEffectRoot;
            transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
        }

        // 让相机跟随安希尔
        try
        {
            SingletonBehavior<BattleCamManager>.Instance?.FollowOneUnit(self.model, true);
            _hasCameraControl = true;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"DarkPurpleSlash: Camera control error: {ex.Message}");
        }

        // 添加水流模糊滤镜
        try
        {
            var effectCam = SingletonBehavior<BattleCamManager>.Instance?.EffectCam;
            if (effectCam != null)
            {
                _blurFilter = effectCam.gameObject.AddComponent<CameraFilterPack_Blur_Radial_Fast>();
                _blurFilter.Intensity = 0f;  // 初始无模糊
                _blurFilter.MovX = 0.5f;
                _blurFilter.MovY = 0.5f;
                _hasBlurFilter = true;
                SteriaLogger.Log("DarkPurpleSlash: Blur filter added");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"DarkPurpleSlash: Blur filter error: {ex.Message}");
        }

        // 创建画面滤镜覆盖层
        CreateScreenOverlay();

        // 创建黑洞特效
        CreateBlackHole();

        // 创建斩击特效
        CreateSlashEffect();

        // 添加强烈震动
        SteriaEffectHelper.AddScreenShake(0.03f, 0.025f, 70f, 0.5f);

        SteriaLogger.Log("DarkPurpleSlash: Initialized - 忘却之梦特效开始");
    }

    private void CreateScreenOverlay()
    {
        try
        {
            _screenOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _screenOverlay.name = "ForgottenDream_ScreenOverlay";

            // 设置为全屏覆盖
            _screenOverlay.transform.parent = Camera.main?.transform;
            _screenOverlay.transform.localPosition = new Vector3(0f, 0f, 2f);
            _screenOverlay.transform.localRotation = Quaternion.identity;
            _screenOverlay.transform.localScale = new Vector3(10f, 10f, 1f);

            _overlayRenderer = _screenOverlay.GetComponent<MeshRenderer>();

            // 创建半透明材质
            _overlayMaterial = new Material(Shader.Find("Sprites/Default"));
            _overlayMaterial.color = new Color(0f, 0f, 0f, 0f);
            _overlayRenderer.material = _overlayMaterial;
            _overlayRenderer.sortingOrder = 500;
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"CreateScreenOverlay error: {ex.Message}");
        }
    }

    private void CreateBlackHole()
    {
        try
        {
            Material material = SteriaEffectSprites.GetEffectMaterial("water_surround", true, 0.1f);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
            }

            _blackHoleQuad = SteriaEffectHelper.CreateEffectQuad(
                "ForgottenDream_BlackHole",
                material,
                transform,
                Vector3.zero,
                0f,
                Vector3.zero,  // 初始大小为0
                150
            );

            if (_blackHoleQuad != null)
            {
                _blackHoleRenderer = _blackHoleQuad.GetComponent<MeshRenderer>();
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"CreateBlackHole error: {ex.Message}");
        }
    }

    private void CreateSlashEffect()
    {
        try
        {
            Material material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.15f);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
            }

            _slashQuad = SteriaEffectHelper.CreateEffectQuad(
                "ForgottenDream_Slash",
                material,
                transform,
                new Vector3(-2f, 0f, 0f),
                15f,
                new Vector3(0.5f, 0.3f, 1f),
                160
            );

            if (_slashQuad != null)
            {
                _slashRenderer = _slashQuad.GetComponent<MeshRenderer>();
                _slashQuad.SetActive(false);  // 初始隐藏
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"CreateSlashEffect error: {ex.Message}");
        }
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        // 阶段控制
        if (progress < 0.2f)
        {
            // 阶段1: 画面变暗 (0-20%)
            UpdateDarkenPhase(progress / 0.2f);
        }
        else if (progress < 0.5f)
        {
            // 阶段2: 黑洞出现并扩大 (20-50%)
            UpdateBlackHolePhase((progress - 0.2f) / 0.3f);
        }
        else if (progress < 0.7f)
        {
            // 阶段3: 穿透斩击 (50-70%)
            UpdatePiercePhase((progress - 0.5f) / 0.2f);
        }
        else
        {
            // 阶段4: 画面变亮，特效消散 (70-100%)
            UpdateBrightenPhase((progress - 0.7f) / 0.3f);
        }

        // 销毁检查
        if (_elapsed >= _duration)
        {
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private void UpdateDarkenPhase(float phaseProgress)
    {
        // 画面逐渐变暗
        if (_overlayMaterial != null)
        {
            float alpha = Mathf.Lerp(0f, _darkPurple.a, SteriaEffectHelper.EaseOutQuad(phaseProgress));
            _overlayMaterial.color = new Color(_darkPurple.r, _darkPurple.g, _darkPurple.b, alpha);
        }
    }

    private void UpdateBlackHolePhase(float phaseProgress)
    {
        // 保持画面暗
        if (_overlayMaterial != null)
        {
            _overlayMaterial.color = _darkPurple;
        }

        // 黑洞从小变大，旋转
        if (_blackHoleQuad != null)
        {
            float scale = Mathf.Lerp(0f, 4f, SteriaEffectHelper.EaseOutQuad(phaseProgress));
            _blackHoleQuad.transform.localScale = new Vector3(scale * 1.5f, scale, 1f);
            _blackHoleQuad.transform.Rotate(0f, 0f, -Time.deltaTime * 200f);

            // 设置暗紫色
            if (_blackHoleRenderer?.material != null)
            {
                float alpha = Mathf.Lerp(0f, 1.5f, phaseProgress);
                SteriaEffectHelper.SetAdditiveMaterialAlpha(_blackHoleRenderer.material, alpha, _brightPurple);
            }
        }
    }

    private void UpdatePiercePhase(float phaseProgress)
    {
        // 开始穿透时的初始化
        if (!_pierceStarted)
        {
            _pierceStarted = true;
            // 添加额外震动
            SteriaEffectHelper.AddScreenShake(0.04f, 0.03f, 100f, 0.3f);
            SteriaLogger.Log("DarkPurpleSlash: Pierce phase started - 开始穿透");
        }

        // 更新水流模糊滤镜 - 穿透时增强模糊
        if (_hasBlurFilter && _blurFilter != null)
        {
            _blurFilter.Intensity = Mathf.Lerp(0f, 0.25f, phaseProgress);
        }

        // 显示斩击特效
        if (_slashQuad != null && !_slashQuad.activeSelf)
        {
            _slashQuad.SetActive(true);
        }

        // 移动安希尔穿透敌人
        if (_selfView != null)
        {
            // 使用缓动函数让移动更流畅
            float moveProgress = SteriaEffectHelper.EaseOutQuad(phaseProgress);
            Vector3 newPos = Vector3.Lerp(_selfStartPos, _pierceEndPos, moveProgress);
            _selfView.WorldPosition = newPos;
        }

        // 斩击从左到右穿透
        if (_slashQuad != null)
        {
            float xPos = Mathf.Lerp(-2f, 2f, SteriaEffectHelper.EaseOutQuad(phaseProgress));
            _slashQuad.transform.localPosition = new Vector3(xPos, 0f, -0.1f);

            float scale = Mathf.Lerp(0.5f, 3f, phaseProgress);
            _slashQuad.transform.localScale = new Vector3(scale * 2f, scale, 1f);

            if (_slashRenderer?.material != null)
            {
                SteriaEffectHelper.SetAdditiveMaterialAlpha(_slashRenderer.material, 2f, _brightPurple);
            }
        }

        // 黑洞开始收缩
        if (_blackHoleQuad != null)
        {
            float scale = Mathf.Lerp(4f, 2f, phaseProgress);
            _blackHoleQuad.transform.localScale = new Vector3(scale * 1.5f, scale, 1f);
            _blackHoleQuad.transform.Rotate(0f, 0f, -Time.deltaTime * 300f);
        }
    }

    private void UpdateBrightenPhase(float phaseProgress)
    {
        // 水流模糊滤镜淡出
        if (_hasBlurFilter && _blurFilter != null)
        {
            _blurFilter.Intensity = Mathf.Lerp(0.25f, 0f, phaseProgress);
        }

        // 画面逐渐变亮（先闪白再恢复）
        if (_overlayMaterial != null)
        {
            if (phaseProgress < 0.3f)
            {
                // 闪白
                float flashProgress = phaseProgress / 0.3f;
                float alpha = Mathf.Lerp(_darkPurple.a, 0.5f, flashProgress);
                _overlayMaterial.color = new Color(0.8f, 0.7f, 1f, alpha);
            }
            else
            {
                // 恢复正常
                float fadeProgress = (phaseProgress - 0.3f) / 0.7f;
                float alpha = Mathf.Lerp(0.5f, 0f, SteriaEffectHelper.EaseOutQuad(fadeProgress));
                _overlayMaterial.color = new Color(0.8f, 0.7f, 1f, alpha);
            }
        }

        // 黑洞消散
        if (_blackHoleQuad != null && _blackHoleRenderer?.material != null)
        {
            float alpha = Mathf.Lerp(1.5f, 0f, phaseProgress);
            SteriaEffectHelper.SetAdditiveMaterialAlpha(_blackHoleRenderer.material, alpha, _brightPurple);

            float scale = Mathf.Lerp(2f, 0f, phaseProgress);
            _blackHoleQuad.transform.localScale = new Vector3(scale * 1.5f, scale, 1f);
        }

        // 斩击消散
        if (_slashQuad != null && _slashRenderer?.material != null)
        {
            float alpha = Mathf.Lerp(2f, 0f, phaseProgress);
            SteriaEffectHelper.SetAdditiveMaterialAlpha(_slashRenderer.material, alpha, _brightPurple);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 释放相机控制
        if (_hasCameraControl)
        {
            try
            {
                SingletonBehavior<BattleCamManager>.Instance?.FollowOneUnit(null, false);
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"DarkPurpleSlash: Camera release error: {ex.Message}");
            }
        }

        // 清理水流模糊滤镜
        if (_hasBlurFilter && _blurFilter != null)
        {
            UnityEngine.Object.Destroy(_blurFilter);
        }

        // 清理画面滤镜
        if (_screenOverlay != null)
        {
            UnityEngine.Object.Destroy(_screenOverlay);
        }
        if (_overlayMaterial != null)
        {
            UnityEngine.Object.Destroy(_overlayMaterial);
        }

        // 清理黑洞
        if (_blackHoleQuad != null)
        {
            UnityEngine.Object.Destroy(_blackHoleQuad);
        }

        // 清理斩击
        if (_slashQuad != null)
        {
            UnityEngine.Object.Destroy(_slashQuad);
        }

        SteriaLogger.Log("DarkPurpleSlash: Destroyed - 忘却之梦特效结束");
    }
}
