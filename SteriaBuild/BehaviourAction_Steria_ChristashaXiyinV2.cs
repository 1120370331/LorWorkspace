using UnityEngine;

/// <summary>Each mass dice, including cloned dice, receives a new effect and a new lifecycle.</summary>
public sealed class BehaviourAction_Steria_ChristashaXiyinV2 : BehaviourActionBase
{
    public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
    {
        _self = self;
        // Native Start/EndFarAreaPlay restore defaults for other cards. This
        // effect's own .5-second release and visual tail own this dice's timing.
        Singleton<BattleFarAreaPlayManager>.Instance.SetActionDelay(0f, 0f);
        GameObject root = new GameObject("FarAreaEffect_Steria_ChristashaXiyinV2");
        FarAreaEffect_Steria_ChristashaXiyinV2 effect = root.AddComponent<FarAreaEffect_Steria_ChristashaXiyinV2>();
        effect.Init(self);
        return effect;
    }
}
