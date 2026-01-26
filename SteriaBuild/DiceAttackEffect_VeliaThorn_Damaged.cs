using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;

/// <summary>
/// 薇莉亚荆棘囚笼受击特效 - 群攻敌人受击动画
/// 6帧动画循环播放
/// </summary>
public class DiceAttackEffect_VeliaThorn_Damaged : DiceAttackEffect
{
    private const float TOTAL_DURATION = 0.8f;
    private const float EFFECT_SCALE = 4f;
    private const int FRAME_COUNT = 6;

    private static readonly string[] FrameNames = new string[]
    {
        "荆棘囚笼_帧1", "荆棘囚笼_帧2", "荆棘囚笼_帧3",
        "荆棘囚笼_帧4", "荆棘囚笼_帧5", "荆棘囚笼_帧6"
    };

    private List<Sprite> _sprites = new List<Sprite>();
    private SpriteRenderer _spriteRenderer;
    private GameObject _effectObj;
    private float _localElapsed = 0f;
    private int _currentFrame = -1;
    private bool _isInitialized = false;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            Steria.SteriaLogger.Log("VeliaThorn_Damaged: Initialize");
            _bHasDamagedEffect = false;
            _destroyTime = TOTAL_DURATION;
            _localElapsed = 0f;

            // 特效显示在目标位置
            _targetTransform = target.atkEffectRoot;
            transform.position = _targetTransform.position;

            LoadSprites();
            CreateEffectObject();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Steria.SteriaLogger.Log($"VeliaThorn_Damaged ERROR: {ex}");
        }
    }

    private void LoadSprites()
    {
        _sprites.Clear();
        foreach (string name in FrameNames)
        {
            var texture = Steria.SteriaEffectSprites.GetTexture(name, false, 0f);
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                _sprites.Add(sprite);
            }
        }
        Steria.SteriaLogger.Log($"VeliaThorn_Damaged: Loaded {_sprites.Count} frames");
    }

    private void CreateEffectObject()
    {
        _effectObj = new GameObject("ThornCageEffect");
        _effectObj.transform.SetParent(transform);
        _effectObj.transform.localPosition = Vector3.zero;
        _effectObj.transform.localScale = Vector3.one * EFFECT_SCALE;

        _spriteRenderer = _effectObj.AddComponent<SpriteRenderer>();
        _spriteRenderer.sortingOrder = 100;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _spriteRenderer.material = new Material(shader);
            _spriteRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _spriteRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }

        if (_sprites.Count > 0)
        {
            _spriteRenderer.sprite = _sprites[0];
        }
    }

    protected override void Update()
    {
        if (!_isInitialized || _sprites.Count == 0)
        {
            base.Update();
            return;
        }

        _localElapsed += Time.deltaTime;
        _elapsed = _localElapsed;

        // 计算当前帧
        float frameTime = TOTAL_DURATION / FRAME_COUNT;
        int frameIndex = Mathf.FloorToInt(_localElapsed / frameTime);
        frameIndex = Mathf.Clamp(frameIndex, 0, _sprites.Count - 1);

        if (frameIndex != _currentFrame)
        {
            _currentFrame = frameIndex;
            _spriteRenderer.sprite = _sprites[_currentFrame];
        }

        // 淡出效果
        float progress = _localElapsed / TOTAL_DURATION;
        float alpha = progress < 0.7f ? 1f : 1f - (progress - 0.7f) / 0.3f;
        _spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

        if (_localElapsed >= _destroyTime)
        {
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_effectObj != null)
            UnityEngine.Object.Destroy(_effectObj);
    }
}
