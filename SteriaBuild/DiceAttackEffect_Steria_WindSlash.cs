using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;
using Sound;
using LOR_DiceSystem;

/// <summary>
/// 清司风流 - 风系斩击特效
/// </summary>
public class DiceAttackEffect_Steria_WindSlash : DiceAttackEffect
{
    public Direction atkdir;
    public GameObject Main;
    private float time;
    public BattleUnitModel _target;

    private List<GameObject> _effectObjects = new List<GameObject>();
    private List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    private List<Vector3> _startScales = new List<Vector3>();
    private List<Vector3> _endScales = new List<Vector3>();
    private float _duration = 0.5f;
    private float _maxAlpha = 1.5f;
    private static Sprite _windSlashSprite = null;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            SteriaLogger.Log($"WindSlash Initialize: self={self?.name}, target={target?.name}");

            base._bHasDamagedEffect = false;
            base._self = self.model;
            _target = target.model;
            base._selfTransform = self.atkEffectRoot;
            base._targetTransform = target.atkEffectRoot;
            atkdir = (Direction)((double)(target.WorldPosition - self.WorldPosition).x > 0.0 ? 1 : 0);

            SteriaLogger.Log($"WindSlash Initialize done, atkdir={atkdir}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash Initialize ERROR: {ex}");
        }
    }

    protected override void Start()
    {
        try
        {
            SteriaLogger.Log("WindSlash Start() called");

            // 加载Sprite
            LoadSprite();

            // 创建视觉效果
            if (_windSlashSprite != null)
            {
                // 在攻击者身上创建1个纵向斩击
                if (base._selfTransform != null)
                {
                    CreateSlashSprite(base._selfTransform, new Vector3(0.5f, 0.5f, 0f), 90f, 0.8f);
                }

                // 在目标身上创建4个斩击，左右左右交替，方向相反
                if (base._targetTransform != null)
                {
                    // 左1 - 向右上斜
                    CreateSlashSprite(base._targetTransform, new Vector3(-0.4f, 0.9f, 0f), 25f, 1.2f);
                    // 右1 - 向左上斜（反方向）
                    CreateSlashSprite(base._targetTransform, new Vector3(0.4f, 0.7f, 0f), -25f, 1.1f);
                    // 左2 - 向右上斜
                    CreateSlashSprite(base._targetTransform, new Vector3(-0.3f, 0.4f, 0f), 20f, 1.0f);
                    // 右2 - 向左上斜（反方向）
                    CreateSlashSprite(base._targetTransform, new Vector3(0.3f, 0.2f, 0f), -20f, 0.9f);
                }

                SteriaLogger.Log($"WindSlash: Created {_effectObjects.Count} sprites");
            }
            else
            {
                SteriaLogger.Log("WindSlash: No sprite available");
            }

            // 播放斩击音效 - 使用角色的soundInfo
            try
            {
                // MotionDetail: 0=Slash, 1=Penetrate, 2=Hit
                base._self?.view?.charAppearance?.soundInfo?.PlaySound((MotionDetail)0, true);
            }
            catch (Exception soundEx)
            {
                SteriaLogger.Log($"WindSlash: Sound error: {soundEx.Message}");
            }

            // 屏幕震动移到骰子能力的OnSucceedAttack中，避免在拼点时触发

            SteriaLogger.Log("WindSlash Start() completed");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash Start ERROR: {ex}");
        }
    }

    private void LoadSprite()
    {
        if (_windSlashSprite != null) return;

        try
        {
            Texture2D texture = SteriaEffectSprites.GetTexture("wind_slash", true, 0.1f);
            if (texture != null)
            {
                _windSlashSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                _windSlashSprite.name = "wind_slash_sprite";
                SteriaLogger.Log($"WindSlash: Created sprite ({texture.width}x{texture.height})");
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash LoadSprite ERROR: {ex}");
        }
    }

    private void CreateSlashSprite(Transform parent, Vector3 localPos, float rotationZ, float scale)
    {
        try
        {
            GameObject spriteObj = new GameObject("WindSlash_" + _effectObjects.Count);
            spriteObj.transform.parent = parent;
            spriteObj.transform.localPosition = localPos;
            spriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            spriteObj.layer = 8; // 和寒昼事务所一样使用 layer 8

            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = _windSlashSprite;
            sr.sortingOrder = 100 + _effectObjects.Count;

            // 设置加法混合材质
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

            sr.color = new Color(1f, 1f, 1f, _maxAlpha);

            Vector3 startScale = new Vector3(scale * 0.3f, scale * 0.3f, 1f);
            Vector3 endScale = new Vector3(scale * 1.2f, scale * 1.2f, 1f);
            spriteObj.transform.localScale = startScale;

            _effectObjects.Add(spriteObj);
            _renderers.Add(sr);
            _startScales.Add(startScale);
            _endScales.Add(endScale);

            SteriaLogger.Log($"WindSlash: Created sprite at {localPos}, layer={spriteObj.layer}");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash CreateSlashSprite ERROR: {ex}");
        }
    }

    protected override void Update()
    {
        try
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / _duration);

            // 更新所有Sprite
            for (int i = 0; i < _effectObjects.Count; i++)
            {
                if (_effectObjects[i] != null && _renderers[i] != null)
                {
                    float scaleProgress = 1f - (1f - progress) * (1f - progress);
                    _effectObjects[i].transform.localScale = Vector3.Lerp(_startScales[i], _endScales[i], scaleProgress);

                    float alphaProgress = progress < 0.3f ? 0f : (progress - 0.3f) / 0.7f;
                    float currentAlpha = Mathf.Lerp(_maxAlpha, 0f, alphaProgress);
                    _renderers[i].color = new Color(1f, 1f, 1f, currentAlpha);
                }
            }

            // 销毁
            if (time >= _duration)
            {
                foreach (var obj in _effectObjects)
                {
                    if (obj != null) UnityEngine.Object.Destroy(obj);
                }
                _effectObjects.Clear();
                _renderers.Clear();
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }
        catch
        {
        }
    }
}
