using System;
using UnityEngine;

/// <summary>
/// 倾覆万千之流 - 群攻特效行为脚本
/// </summary>
public class BehaviourAction_Steria_OceanWave : BehaviourActionBase
{
    public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
    {
        this._self = self;

        // 创建一个空的GameObject来承载FarAreaEffect
        GameObject effectObj = new GameObject("FarAreaEffect_Steria_OceanWave");
        FarAreaEffect_Steria_OceanWave effect = effectObj.AddComponent<FarAreaEffect_Steria_OceanWave>();

        if (effect != null)
        {
            effect.Init(self, Array.Empty<object>());
        }

        return effect;
    }
}
