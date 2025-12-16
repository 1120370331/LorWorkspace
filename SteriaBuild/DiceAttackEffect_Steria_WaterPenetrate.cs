using System;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 水系突刺特效 - 使用water_slash.png图片，高度压缩50%
/// </summary>
public class DiceAttackEffect_Steria_WaterPenetrate : DiceAttackEffect
{
    private GameObject _effectQuad;
    private MeshRenderer _renderer;
    private float _duration = 0.9f;  // 延长100%
    private new float _elapsed = 0f;

    private Vector3 _startScale;
    private Vector3 _endScale;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _startAlpha = 1.3f;
    private float _endAlpha = 0f;

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
            Material material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.15f);
            if (material == null) return;

            _effectQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _effectQuad.name = "WaterPenetrate";

            var collider = _effectQuad.GetComponent("MeshCollider") as Component;
            if (collider != null) UnityEngine.Object.Destroy(collider);

            _renderer = _effectQuad.GetComponent<MeshRenderer>();
            _renderer.material = new Material(material);
            _renderer.sortingOrder = 100;

            _effectQuad.transform.SetParent(base.transform);
            _effectQuad.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);

            float scale = 3.96f;  // 原3.3f增大20%
            float aspectRatio = 16f / 9f;
            Vector3 baseScale = new Vector3(scale * aspectRatio, scale * 0.5f, 1f);

            _startScale = baseScale * 0.4f;
            _endScale = baseScale * 1.2f;
            _effectQuad.transform.localScale = _startScale;

            _startPos = new Vector3(-0.23f, 0f, 0f);  // 原-0.2f往外移动15%
            _endPos = new Vector3(-0.575f, 0f, 0f);  // 原-0.5f往外移动15%
            _effectQuad.transform.localPosition = _startPos;

            Debug.Log("[Steria] WaterPenetrate effect created");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] Error creating WaterPenetrate effect: {ex}");
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
                    shake.X = 0.018f;
                    shake.Y = 0.005f;
                    shake.Speed = 80f;

                    var autoDestroy = instance.EffectCam.gameObject.AddComponent<AutoScriptDestruct>();
                    if (autoDestroy != null)
                    {
                        autoDestroy.targetScript = shake;
                        autoDestroy.time = 0.2f;
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
            float posProgress = EaseOutCubic(progress);
            _effectQuad.transform.localPosition = Vector3.Lerp(_startPos, _endPos, posProgress);

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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_effectQuad != null) UnityEngine.Object.Destroy(_effectQuad);
    }
}
