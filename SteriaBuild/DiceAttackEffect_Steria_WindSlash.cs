using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 风系斩击特效 - 攻击者手边1个纵向，目标周围3个横向
/// </summary>
public class DiceAttackEffect_Steria_WindSlash : DiceAttackEffect_Steria_Base
{
    // 目标身上的额外Quad配置
    private static readonly QuadEffectConfig[] TargetQuads = new[]
    {
        new QuadEffectConfig
        {
            TextureName = "wind_slash",
            LocalPosition = new Vector3(0f, 0.8f, -0.5f),
            RotationZ = 15f,
            BaseScale = 2.5f,
            ScaleMultiplierStart = 0.3f,
            ScaleMultiplierEnd = 1.1f,
            AnimateScale = true
        },
        new QuadEffectConfig
        {
            TextureName = "wind_slash",
            LocalPosition = new Vector3(-0.3f, 0.3f, -0.4f),
            RotationZ = -10f,
            BaseScale = 2.3f,
            ScaleMultiplierStart = 0.3f,
            ScaleMultiplierEnd = 1.1f,
            AnimateScale = true
        },
        new QuadEffectConfig
        {
            TextureName = "wind_slash",
            LocalPosition = new Vector3(0.3f, 0.5f, -0.6f),
            RotationZ = 25f,
            BaseScale = 2.0f,
            ScaleMultiplierStart = 0.3f,
            ScaleMultiplierEnd = 1.1f,
            AnimateScale = true
        }
    };

    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WindSlash;
    }

    protected override void SetupTransform(BattleUnitView self, BattleUnitView target)
    {
        // WindSlash不挂载到任何父节点，使用世界坐标
        base.transform.parent = null;
        base.transform.position = Vector3.zero;
        base.transform.rotation = Quaternion.identity;
        base.transform.localScale = Vector3.one;
    }

    protected override void CreateEffects()
    {
        // 在攻击者身上创建纵向斩击
        if (_selfView != null && _selfView.atkEffectRoot != null)
        {
            var selfConfig = _config.Quads[0];
            CreateQuadOnTarget(_selfView.atkEffectRoot, selfConfig, 0);
        }

        // 在目标身上创建3个横向斩击
        if (_targetView != null && _targetView.atkEffectRoot != null)
        {
            for (int i = 0; i < TargetQuads.Length; i++)
            {
                CreateQuadOnTarget(_targetView.atkEffectRoot, TargetQuads[i], i + 1);
            }
        }

        SteriaLogger.Log($"WindSlash: Created {_effectQuads.Count} slashes");
    }

    private void CreateQuadOnTarget(Transform parent, QuadEffectConfig config, int index)
    {
        Material material = SteriaEffectSprites.GetEffectMaterial(config.TextureName, true, config.BrightnessThreshold);
        if (material == null) return;

        GameObject quad = SteriaEffectHelper.CreateEffectQuad(
            $"WindSlash_{index}",
            material,
            parent,
            config.LocalPosition,
            config.RotationZ,
            config.GetStartScale(),
            100 + index
        );

        if (quad != null)
        {
            var renderer = quad.GetComponent<MeshRenderer>();
            _effectQuads.Add(quad);
            _renderers.Add(renderer);
            _startScales.Add(config.GetStartScale());
            _endScales.Add(config.GetEndScale());
            _startPositions.Add(config.LocalPosition);
            _endPositions.Add(config.LocalPosition);

            // 存储配置引用以便更新时使用
            _quadConfigs.Add(config);
        }
    }

    // 存储每个Quad的配置
    private List<QuadEffectConfig> _quadConfigs = new List<QuadEffectConfig>();

    protected override void UpdateQuad(int index, float progress)
    {
        if (index >= _effectQuads.Count || _effectQuads[index] == null || _renderers[index] == null)
            return;

        var quad = _effectQuads[index];
        var renderer = _renderers[index];
        var config = index < _quadConfigs.Count ? _quadConfigs[index] : _config.Quads[0];

        // 缩放动画
        float scaleProgress = SteriaEffectHelper.EaseOutQuad(progress);
        quad.transform.localScale = Vector3.Lerp(_startScales[index], _endScales[index], scaleProgress);

        // 透明度：前30%不变，后70%渐出
        float alphaProgress = progress < 0.3f ? 0f : (progress - 0.3f) / 0.7f;
        float currentAlpha = Mathf.Lerp(_config.MaxAlpha, 0f, alphaProgress);

        if (renderer.material != null)
        {
            Color color = new Color(currentAlpha, currentAlpha, currentAlpha, currentAlpha);
            renderer.material.SetColor("_TintColor", color * 0.5f);
        }
    }

    protected override void OnCleanup()
    {
        _quadConfigs.Clear();
    }
}
