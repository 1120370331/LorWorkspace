using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;
using Sound;

/// <summary>
/// 清司风流 - 风系斩击特效
/// 在攻击者和目标身上创建风斩视觉效果
/// </summary>
public class DiceAttackEffect_Steria_WindSlash : DiceAttackEffect
{
    private List<GameObject> _effectQuads = new List<GameObject>();
    private List<MeshRenderer> _renderers = new List<MeshRenderer>();
    private List<Vector3> _startScales = new List<Vector3>();
    private List<Vector3> _endScales = new List<Vector3>();

    private float _duration = 0.5f;
    private new float _elapsed = 0f;
    private float _maxAlpha = 1.5f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            SteriaLogger.Log($"WindSlash: Initialize START, self={self?.name}, target={target?.name}");

            this._self = self.model;

            // 不挂载到任何父节点
            base.transform.parent = null;
            base.transform.position = Vector3.zero;
            base.transform.rotation = Quaternion.identity;
            base.transform.localScale = Vector3.one;

            this._destroyTime = _duration;
            this._elapsed = 0f;

            // 创建视觉效果
            CreateEffects(self, target);

            // 播放音效
            SoundEffectPlayer.PlaySound("Battle/Kali_Atk");

            // 屏幕震动
            SteriaEffectHelper.AddScreenShake(0.02f, 0.01f, 70f, 0.3f);

            SteriaLogger.Log($"WindSlash: Initialize END, created {_effectQuads.Count} quads");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash ERROR: {ex}");
        }
    }

    private void CreateEffects(BattleUnitView self, BattleUnitView target)
    {
        // 在攻击者身上创建1个纵向斩击
        if (self?.atkEffectRoot != null)
        {
            CreateSlashQuad(self.atkEffectRoot, new Vector3(0.5f, 0.5f, -0.3f), 90f, 1.8f);
        }

        // 在目标身上创建3个横向斩击
        if (target?.atkEffectRoot != null)
        {
            CreateSlashQuad(target.atkEffectRoot, new Vector3(0f, 0.8f, -0.5f), 15f, 2.5f);
            CreateSlashQuad(target.atkEffectRoot, new Vector3(-0.3f, 0.3f, -0.4f), -10f, 2.3f);
            CreateSlashQuad(target.atkEffectRoot, new Vector3(0.3f, 0.5f, -0.6f), 25f, 2.0f);
        }
    }

    private void CreateSlashQuad(Transform parent, Vector3 localPos, float rotationZ, float scale)
    {
        Material material = SteriaEffectSprites.GetEffectMaterial("wind_slash", true, 0.15f);
        if (material == null)
        {
            SteriaLogger.Log("WindSlash: Failed to get material for wind_slash");
            return;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WindSlash_" + _effectQuads.Count;

        // 移除碰撞体
        var collider = quad.GetComponent("MeshCollider") as Component;
        if (collider != null) UnityEngine.Object.Destroy(collider);

        // 设置材质
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = new Material(material);
        renderer.sortingOrder = 100 + _effectQuads.Count;

        // 设置变换
        quad.transform.SetParent(parent);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        // 计算缩放
        float aspectRatio = 16f / 9f;
        Vector3 baseScale = new Vector3(scale * aspectRatio, scale, 1f);
        Vector3 startScale = baseScale * 0.3f;
        Vector3 endScale = baseScale * 1.1f;

        quad.transform.localScale = startScale;

        _effectQuads.Add(quad);
        _renderers.Add(renderer);
        _startScales.Add(startScale);
        _endScales.Add(endScale);

        SteriaLogger.Log($"WindSlash: Created quad at {localPos}, rotation={rotationZ}, scale={scale}");
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        // 更新所有Quad
        for (int i = 0; i < _effectQuads.Count; i++)
        {
            if (_effectQuads[i] != null && _renderers[i] != null)
            {
                // 缩放动画
                float scaleProgress = SteriaEffectHelper.EaseOutQuad(progress);
                _effectQuads[i].transform.localScale = Vector3.Lerp(_startScales[i], _endScales[i], scaleProgress);

                // 透明度：前30%不变，后70%渐出
                float alphaProgress = progress < 0.3f ? 0f : (progress - 0.3f) / 0.7f;
                float currentAlpha = Mathf.Lerp(_maxAlpha, 0f, alphaProgress);

                if (_renderers[i].material != null)
                {
                    Color color = new Color(currentAlpha, currentAlpha, currentAlpha, currentAlpha);
                    _renderers[i].material.SetColor("_TintColor", color * 0.5f);
                }
            }
        }

        // 销毁检查
        if (_elapsed >= _duration)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
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
