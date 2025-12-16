using System;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;
using Sound;

/// <summary>
/// 清司风流 - 风系斩击特效
/// 简化版：使用游戏内置特效系统
/// </summary>
public class DiceAttackEffect_Steria_WindSlash : DiceAttackEffect
{
    private float _duration = 0.8f;
    private new float _elapsed = 0f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            SteriaLogger.Log($"WindSlash: Initialize START");

            this._self = self.model;

            // 挂载到攻击者的斩击pivot
            Transform pivot = self.charAppearance?.GetAtkEffectPivot(ActionDetail.Slash);
            if (pivot == null) pivot = self.atkEffectRoot;

            base.transform.parent = pivot;
            base.transform.localPosition = Vector3.zero;
            base.transform.localRotation = Quaternion.identity;
            base.transform.localScale = Vector3.one;

            this._destroyTime = _duration;
            this._elapsed = 0f;

            // 播放音效
            SoundEffectPlayer.PlaySound("Battle/Kali_Atk");

            // 屏幕震动
            SteriaEffectHelper.AddScreenShake(0.02f, 0.01f, 70f, 0.3f);

            // 在目标身上创建一个内置特效
            if (target != null)
            {
                SingletonBehavior<DiceEffectManager>.Instance?.CreateBehaviourEffect(
                    "Kali_J", 0.8f, self, target, 0.5f);
            }

            SteriaLogger.Log($"WindSlash: Initialize END");
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"WindSlash ERROR: {ex}");
        }
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }
}
