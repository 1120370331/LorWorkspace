using System;
using System.Collections.Generic;
using LOR_DiceSystem;
using Sound;

/// <summary>
/// 清司风流 - 风系斩击攻击动作
/// 参考 BehaviourAction_Apostle_StaffFarAtk
/// </summary>
public class BehaviourAction_Steria_Hurricane : BehaviourActionBase
{
    public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        bool flag = self.behaviourResultData.behaviourRawData.Type != BehaviourType.Standby;
        if (self.result == Result.Win && flag)
        {
            List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
            List<RencounterManager.MovingAction> infoList = opponent.infoList;
            if (infoList != null && infoList.Count > 0)
            {
                opponent.infoList.Clear();
            }

            // 第一段攻击 - 风斩特效
            RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(
                ActionDetail.Fire,
                CharMoveState.Stop,
                0f,
                true,
                0.25f,
                1f
            );
            movingAction.customEffectRes = "Steria_WindSlash";
            movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.NONE, EffectTiming.NONE);
            list.Add(movingAction);

            // 对手受击
            RencounterManager.MovingAction item = new RencounterManager.MovingAction(
                ActionDetail.Damaged,
                CharMoveState.Stop,
                0f,
                true,
                0.25f,
                1f
            );
            opponent.infoList.Add(item);

            // 第二段攻击
            RencounterManager.MovingAction movingAction2 = new RencounterManager.MovingAction(
                ActionDetail.Fire,
                CharMoveState.Stop,
                0f,
                true,
                1f,
                1f
            );
            movingAction2.SetEffectTiming(EffectTiming.NONE, EffectTiming.PRE, EffectTiming.PRE);
            list.Add(movingAction2);

            // 对手受击
            RencounterManager.MovingAction item2 = new RencounterManager.MovingAction(
                ActionDetail.Damaged,
                CharMoveState.Stop,
                0f,
                true,
                1f,
                1f
            );
            opponent.infoList.Add(item2);

            // 播放音效
            SoundEffectPlayer.PlaySound("Battle/Kali_Atk");

            return list;
        }
        return base.GetMovingAction(ref self, ref opponent);
    }

    public override bool IsMovable()
    {
        return false;
    }

    public override bool IsOpponentMovable()
    {
        return false;
    }
}
