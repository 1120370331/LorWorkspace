using System;
using UnityEngine;

/// <summary>
/// 薇莉亚荆棘之潮群攻行为脚本
/// </summary>
public class BehaviourAction_VeliaThorn_Area : BehaviourActionBase
{
    public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
    {
        Steria.SteriaLogger.Log("BehaviourAction_VeliaThorn_Area: Creating effect");
        this._self = self;

        // 创建GameObject并添加FarAreaEffect组件
        GameObject go = new GameObject("FarAreaEffect_VeliaThorn");
        FarAreaEffect_VeliaThorn effect = go.AddComponent<FarAreaEffect_VeliaThorn>();

        if (effect != null)
        {
            effect.Init(self, Array.Empty<object>());
        }

        return effect;
    }
}
