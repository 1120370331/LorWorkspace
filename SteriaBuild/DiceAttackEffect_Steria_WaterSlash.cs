using System;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 水系斩击特效 - 使用water_slash.png图片
/// </summary>
public class DiceAttackEffect_Steria_WaterSlash : DiceAttackEffect
{
    private GameObject _effectQuad;
    private MeshRenderer _renderer;
    private float _duration = 0.98f;  // 原1.4f减少30%
    private new float _elapsed = 0f;

    private Vector3 _startScale;
    private Vector3 _endScale;
    private float _startAlpha = 1.0f;
    private float _endAlpha = 0f;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        this._self = self.model;

        // 挂载到攻击者身上
        base.transform.parent = self.atkEffectRoot;
        base.transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
        base.transform.localRotation = Quaternion.identity;
        base.transform.localScale = Vector3.one;

        this._destroyTime = _duration;
        this._elapsed = 0f;

        CreateEffect();
        AddScreenShake();
    }

    private void CreateEffect()
    {
        try
        {
            _effectQuad = SteriaEffectSprites.CreateEffectQuad("water_slash", base.transform, 1.875f);  // 原3.75f降低50%
            if (_effectQuad != null)
            {
                _effectQuad.transform.localPosition = new Vector3(-0.8f, 0f, 0f);  // 向前移动（负X方向）
            }

            if (_effectQuad != null)
            {
                _renderer = _effectQuad.GetComponent<MeshRenderer>();

                _startScale = _effectQuad.transform.localScale * 0.5f;
                _endScale = _effectQuad.transform.localScale * 1.3f;
                _effectQuad.transform.localScale = _startScale;

                _startPosition = _effectQuad.transform.localPosition;
                _endPosition = _startPosition + new Vector3(0f, -0.3f, 0f);

                _effectQuad.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-10f, 10f));

                Debug.Log("[Steria] WaterSlash effect created successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] Error creating WaterSlash effect: {ex}");
        }
    }

    private void AddScreenShake()
    {
        try
        {
            BattleCamManager instance = SingletonBehavior<BattleCamManager>.Instance;
            if (instance != null && instance.EffectCam != null)
            {
                var shake = instance.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>();
                if (shake != null)
                {
                    shake.X = 0.01f;
                    shake.Y = 0.012f;
                    shake.Speed = 40f;

                    var autoDestroy = instance.EffectCam.gameObject.AddComponent<AutoScriptDestruct>();
                    if (autoDestroy != null)
                    {
                        autoDestroy.targetScript = shake;
                        autoDestroy.time = 0.3f;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Steria] Could not add screen shake: {ex.Message}");
        }
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        if (_effectQuad != null && _renderer != null)
        {
            // 固定大小，不放大
            _effectQuad.transform.localScale = _endScale;

            // 位置动画保留
            float posProgress = EaseOutQuad(progress);
            _effectQuad.transform.localPosition = Vector3.Lerp(_startPosition, _endPosition, posProgress);

            // 快速渐入渐出：前15%渐入，后25%渐出
            float currentAlpha;
            if (progress < 0.15f)
            {
                currentAlpha = Mathf.Lerp(0f, _startAlpha, progress / 0.15f);
            }
            else if (progress > 0.75f)
            {
                currentAlpha = Mathf.Lerp(_startAlpha, 0f, (progress - 0.75f) / 0.25f);
            }
            else
            {
                currentAlpha = _startAlpha;
            }

            if (_renderer.material != null)
            {
                Color color = new Color(currentAlpha * 0.9f, currentAlpha, currentAlpha * 1.1f, currentAlpha);
                _renderer.material.SetColor("_TintColor", color * 0.5f);
            }
        }

        if (_elapsed >= _duration)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInQuad(float t)
    {
        return t * t;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_effectQuad != null) UnityEngine.Object.Destroy(_effectQuad);
    }
}
