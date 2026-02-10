using System;
using UnityEngine;

/// <summary>
/// 克丽丝塔夏群攻行为脚本（金白色特效）
/// </summary>
public class BehaviourAction_ChristashaGoldenMass : BehaviourActionBase
{
    public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
    {
        this._self = self;

        GameObject effectObj = new GameObject("FarAreaEffect_ChristashaGoldenMass");
        FarAreaEffect_ChristashaGoldenMass effect = effectObj.AddComponent<FarAreaEffect_ChristashaGoldenMass>();

        if (effect != null)
        {
            effect.Init(self, Array.Empty<object>());
        }

        return effect;
    }
}

