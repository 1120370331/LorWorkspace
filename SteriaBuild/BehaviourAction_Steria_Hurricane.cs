using System;
using System.Collections.Generic;

/// <summary>
/// 清司风流 - 风系斩击攻击动作
/// 无延迟，立即造成伤害
/// </summary>
public class BehaviourAction_Steria_Hurricane : BehaviourActionBase
{
    public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();

        // 创建攻击动作 - 无延迟
        RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(
            ActionDetail.Fire,           // 远程攻击动作
            CharMoveState.Stop,          // 不移动
            1f,                          // 速度
            false,
            0f,                          // 无延迟
            0.6f                         // 持续时间
        );

        // 设置特效时机为 PRE（在攻击前播放）
        movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);

        list.Add(movingAction);

        // 对手的受击动作 - 无延迟
        opponent.infoList.Add(new RencounterManager.MovingAction(
            ActionDetail.Damaged,
            CharMoveState.Stop,
            1f,
            false,
            0f,                          // 无延迟
            0.6f
        ));

        return list;
    }

    // 不移动
    public override bool IsMovable()
    {
        return false;
    }

    public override bool IsOpponentMovable()
    {
        return false;
    }
}
