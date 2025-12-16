using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 清司风流 - 风系斩击特效
/// 攻击者手边触发1个纵向的，目标周围触发3个角度不一样的横向的
/// </summary>
public class DiceAttackEffect_Steria_WindSlash : DiceAttackEffect
{
    private List<GameObject> _effectQuads = new List<GameObject>();
    private List<MeshRenderer> _renderers = new List<MeshRenderer>();
    private List<Vector3> _startScales = new List<Vector3>();
    private List<Vector3> _endScales = new List<Vector3>();

    private float _duration = 0.5f;
    private new float _elapsed = 0f;
    private float _startAlpha = 1.5f;
    private float _endAlpha = 0f;

    private BattleUnitView _selfView;
    private BattleUnitView _targetView;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        SteriaLogger.Log($"WindSlash: Initialize called, self={self?.name}, target={target?.name}");
        this._self = self.model;
        this._selfView = self;
        this._targetView = target;

        base.transform.parent = null;
        base.transform.position = Vector3.zero;
        base.transform.rotation = Quaternion.identity;
        base.transform.localScale = Vector3.one;

        this._destroyTime = _duration;
        this._elapsed = 0f;

        CreateEffects();
        AddScreenShake();
    }

    private void CreateEffects()
    {
        try
        {
            if (_selfView != null && _selfView.atkEffectRoot != null)
            {
                CreateSlashEffect(_selfView.atkEffectRoot, new Vector3(0.5f, 0.5f, -0.3f), 90f, 1.8f);
            }

            if (_targetView != null && _targetView.atkEffectRoot != null)
            {
                CreateSlashEffect(_targetView.atkEffectRoot, new Vector3(0f, 0.8f, -0.5f), 15f, 2.5f);
                CreateSlashEffect(_targetView.atkEffectRoot, new Vector3(-0.3f, 0.3f, -0.4f), -10f, 2.3f);
                CreateSlashEffect(_targetView.atkEffectRoot, new Vector3(0.3f, 0.5f, -0.6f), 25f, 2.0f);
            }

            Debug.Log($"[Steria] WindSlash effect created: {_effectQuads.Count} slashes");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Steria] Error creating WindSlash effects: {ex}");
        }
    }

    private void CreateSlashEffect(Transform parent, Vector3 localPos, float rotationZ, float scale)
    {
        Material material = SteriaEffectSprites.GetEffectMaterial("wind_slash", true, 0.15f);
        if (material == null) return;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WindSlash_" + _effectQuads.Count;

        var collider = quad.GetComponent("MeshCollider") as Component;
        if (collider != null) UnityEngine.Object.Destroy(collider);

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = new Material(material);
        renderer.sortingOrder = 100 + _effectQuads.Count;

        quad.transform.SetParent(parent);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        float aspectRatio = 16f / 9f;
        Vector3 baseScale = new Vector3(scale * aspectRatio, scale, 1f);
        Vector3 startScale = baseScale * 0.3f;
        Vector3 endScale = baseScale * 1.1f;

        quad.transform.localScale = startScale;

        _effectQuads.Add(quad);
        _renderers.Add(renderer);
        _startScales.Add(startScale);
        _endScales.Add(endScale);
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
                    shake.X = 0.02f;
                    shake.Y = 0.01f;
                    shake.Speed = 70f;

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

        for (int i = 0; i < _effectQuads.Count; i++)
        {
            if (_effectQuads[i] != null && _renderers[i] != null)
            {
                float scaleProgress = EaseOutQuad(progress);
                _effectQuads[i].transform.localScale = Vector3.Lerp(_startScales[i], _endScales[i], scaleProgress);

                float alphaProgress = progress < 0.3f ? 0f : (progress - 0.3f) / 0.7f;
                float currentAlpha = Mathf.Lerp(_startAlpha, _endAlpha, alphaProgress);

                if (_renderers[i].material != null)
                {
                    Color color = new Color(currentAlpha, currentAlpha, currentAlpha, currentAlpha);
                    _renderers[i].material.SetColor("_TintColor", color * 0.5f);
                }
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
        foreach (var quad in _effectQuads)
        {
            if (quad != null) UnityEngine.Object.Destroy(quad);
        }
        _effectQuads.Clear();
        _renderers.Clear();
    }
}
