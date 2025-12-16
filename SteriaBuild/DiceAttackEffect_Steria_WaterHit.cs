using System;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 水系打击特效 - 使用water_slash.png图片，旋转90度
/// </summary>
public class DiceAttackEffect_Steria_WaterHit : DiceAttackEffect
{
    private GameObject _effectQuad;
    private MeshRenderer _renderer;
    private float _duration = 1.0f;  // 延长100%
    private new float _elapsed = 0f;

    private Vector3 _startScale;
    private Vector3 _endScale;
    private float _startAlpha = 1.2f;
    private float _endAlpha = 0f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        SteriaLogger.Log("WaterHit: Initialize called");
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
            Material material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.15f);
            if (material == null) return;

            _effectQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _effectQuad.name = "WaterHit";

            var collider = _effectQuad.GetComponent("MeshCollider") as Component;
            if (collider != null) UnityEngine.Object.Destroy(collider);

            _renderer = _effectQuad.GetComponent<MeshRenderer>();
            _renderer.material = new Material(material);
            _renderer.sortingOrder = 100;

            _effectQuad.transform.SetParent(base.transform);
            _effectQuad.transform.localPosition = new Vector3(-0.8f, 0f, 0f);  // 向前移动（负X方向）
            _effectQuad.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);  // 反向旋转

            float scale = 3.6f;  // 原3.0f增大20%
            float aspectRatio = 16f / 9f;
            Vector3 baseScale = new Vector3(scale * aspectRatio, scale, 1f);

            _startScale = baseScale * 0.3f;
            _endScale = baseScale * 1.1f;
            _effectQuad.transform.localScale = _startScale;

            SteriaLogger.Log("WaterHit effect created");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] Error creating WaterHit effect: {ex}");
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
                    shake.X = 0.012f;
                    shake.Y = 0.015f;
                    shake.Speed = 50f;

                    var autoDestroy = instance.EffectCam.gameObject.AddComponent<AutoScriptDestruct>();
                    if (autoDestroy != null)
                    {
                        autoDestroy.targetScript = shake;
                        autoDestroy.time = 0.25f;
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_effectQuad != null) UnityEngine.Object.Destroy(_effectQuad);
    }
}
