using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 薇莉亚荆棘之潮群攻特效
/// 在每个受击目标位置显示荆棘囚笼
/// </summary>
public class FarAreaEffect_VeliaThorn : FarAreaEffect
{
    private const float DAMAGED_SCALE = 2f;
    private const float DAMAGED_DURATION = 0.6f;

    private static readonly string[] DamagedFrames = new string[]
    {
        "荆棘囚笼_帧1", "荆棘囚笼_帧2", "荆棘囚笼_帧3",
        "荆棘囚笼_帧4", "荆棘囚笼_帧5", "荆棘囚笼_帧6"
    };

    private static List<Sprite> _cachedSprites = null;
    private static bool _spritesLoaded = false;

    private List<DamagedEffectInstance> _activeEffects = new List<DamagedEffectInstance>();
    private bool _damageGiven = false;

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        LoadSprites();
        _isDoneEffect = false;
        _damageGiven = false;
        isRunning = false; // 不需要等待，立即开始
        Steria.SteriaLogger.Log("FarAreaEffect_VeliaThorn: Init");
    }

    public override void OnEffectStart()
    {
        base.OnEffectStart();
        Steria.SteriaLogger.Log("FarAreaEffect_VeliaThorn: OnEffectStart");
    }

    public override void OnGiveDamage()
    {
        base.OnGiveDamage();
        Steria.SteriaLogger.Log("FarAreaEffect_VeliaThorn: OnGiveDamage");
    }

    public override void OnEffectEnd()
    {
        _isDoneEffect = true;
        isRunning = false;
        Steria.SteriaLogger.Log("FarAreaEffect_VeliaThorn: OnEffectEnd");
        UnityEngine.Object.Destroy(gameObject);
    }

    private void LoadSprites()
    {
        if (_spritesLoaded) return;

        _cachedSprites = new List<Sprite>();
        foreach (string name in DamagedFrames)
        {
            var tex = Steria.SteriaEffectSprites.GetTexture(name, false, 0f);
            if (tex != null)
            {
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                _cachedSprites.Add(spr);
            }
        }
        _spritesLoaded = true;
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        Steria.SteriaLogger.Log($"FarAreaEffect_VeliaThorn: GiveDamage to {damagedUnitList?.Count ?? 0} units");
        _damageGiven = true;

        if (damagedUnitList == null || _cachedSprites == null || _cachedSprites.Count == 0)
        {
            // 没有特效需要播放，立即完成
            _isDoneEffect = true;
            return;
        }

        foreach (var unit in damagedUnitList)
        {
            if (unit?.view?.atkEffectRoot != null)
            {
                CreateDamagedEffect(unit.view);
            }
        }

        // 如果没有创建任何特效，立即完成
        if (_activeEffects.Count == 0)
        {
            _isDoneEffect = true;
        }
    }

    private void CreateDamagedEffect(BattleUnitView target)
    {
        var effectObj = new GameObject("ThornCageEffect");
        effectObj.transform.SetParent(target.atkEffectRoot);
        effectObj.transform.localPosition = Vector3.zero;
        effectObj.transform.localScale = Vector3.one * DAMAGED_SCALE;

        var sr = effectObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            sr.material = new Material(shader);
            sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }

        if (_cachedSprites.Count > 0)
            sr.sprite = _cachedSprites[0];

        _activeEffects.Add(new DamagedEffectInstance
        {
            obj = effectObj,
            renderer = sr,
            elapsed = 0f
        });
    }

    protected override void Update()
    {
        base.Update();

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.elapsed += Time.deltaTime;

            float progress = effect.elapsed / DAMAGED_DURATION;

            if (_cachedSprites != null && _cachedSprites.Count > 0 && effect.renderer != null)
            {
                // 蓄势效果：前30%时间停在第一帧，后70%时间播放剩余5帧
                int idx;
                if (progress < 0.3f)
                {
                    idx = 0; // 第一帧停顿蓄势
                }
                else
                {
                    // 剩余70%时间播放帧2-6
                    float remainProgress = (progress - 0.3f) / 0.7f;
                    idx = 1 + Mathf.Clamp(Mathf.FloorToInt(remainProgress * 5), 0, 4);
                }
                effect.renderer.sprite = _cachedSprites[idx];

                float alpha = progress < 0.7f ? 1f : 1f - (progress - 0.7f) / 0.3f;
                effect.renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }

            if (effect.elapsed >= DAMAGED_DURATION)
            {
                if (effect.obj != null)
                    UnityEngine.Object.Destroy(effect.obj);
                _activeEffects.RemoveAt(i);
            }
        }

        // 当伤害已给出且所有特效播放完成时，标记完成
        if (_damageGiven && _activeEffects.Count == 0)
        {
            _isDoneEffect = true;
        }
    }

    private class DamagedEffectInstance
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public float elapsed;
    }
}
