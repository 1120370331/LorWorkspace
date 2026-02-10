using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 克丽丝塔夏群攻金白特效
/// </summary>
public class FarAreaEffect_ChristashaGoldenMass : FarAreaEffect
{
    private const float Duration = 0.95f;
    private const float FadeOutStart = 0.65f;
    private const float BaseScale = 7.2f;

    private readonly List<VisualInstance> _instances = new List<VisualInstance>();
    private float _elapsed;
    private bool _damageGiven;

    private static readonly string[] TextureCandidates =
    {
        "Sunlight",
        "GoldenTide",
        "water_slash"
    };

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        _elapsed = 0f;
        _damageGiven = false;
        _isDoneEffect = false;
        isRunning = true;

        if (self?.view != null)
        {
            transform.position = self.view.WorldPosition;
        }
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        _damageGiven = true;
        if (damagedUnitList == null || damagedUnitList.Count == 0)
        {
            return;
        }

        foreach (BattleUnitModel unit in damagedUnitList)
        {
            if (unit?.view?.atkEffectRoot == null)
            {
                continue;
            }

            CreateGoldenFlash(unit.view);
        }
    }

    private void CreateGoldenFlash(BattleUnitView target)
    {
        Material material = ResolveMaterial();
        if (material == null)
        {
            return;
        }

        GameObject flash = new GameObject("ChristashaGoldenMass_Flash");
        flash.transform.SetParent(target.atkEffectRoot, false);
        flash.transform.localPosition = Vector3.zero;
        flash.transform.localScale = Vector3.one * BaseScale;

        SpriteRenderer renderer = flash.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 120;
        renderer.material = new Material(material);
        renderer.color = new Color(1f, 0.95f, 0.72f, 0.95f);

        _instances.Add(new VisualInstance
        {
            obj = flash,
            renderer = renderer,
            pulseSeed = UnityEngine.Random.Range(0f, 10f)
        });
    }

    private Material ResolveMaterial()
    {
        foreach (string textureName in TextureCandidates)
        {
            Material material = Steria.SteriaEffectSprites.GetEffectMaterial(textureName, true, 0.12f);
            if (material != null)
            {
                return material;
            }
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return null;
        }

        return new Material(shader);
    }

    protected override void Update()
    {
        base.Update();

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Duration);

        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            VisualInstance instance = _instances[i];
            if (instance.obj == null || instance.renderer == null)
            {
                _instances.RemoveAt(i);
                continue;
            }

            float pulse = 1f + Mathf.Sin((_elapsed * 17f) + instance.pulseSeed) * 0.08f;
            instance.obj.transform.localScale = Vector3.one * (BaseScale * pulse * (1f + t * 0.35f));

            float alpha = t < FadeOutStart
                ? 0.92f
                : Mathf.Lerp(0.92f, 0f, (t - FadeOutStart) / (1f - FadeOutStart));

            instance.renderer.color = new Color(1f, 0.95f, 0.72f, Mathf.Clamp01(alpha));

            if (t >= 1f)
            {
                UnityEngine.Object.Destroy(instance.obj);
                _instances.RemoveAt(i);
            }
        }

        if (_damageGiven && _elapsed >= Duration && _instances.Count == 0)
        {
            _isDoneEffect = true;
            isRunning = false;
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private class VisualInstance
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public float pulseSeed;
    }
}

