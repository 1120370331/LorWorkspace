using System.Collections.Generic;
using LOR_DiceSystem;

// Exact XML suffixes; each die carries its own action, with no shared ordinal.
public sealed class BehaviourAction_AceTaffy_SteamDetonation : TaffySteamAction
{
    protected override float Preparation { get { return .30f; } }
    protected override float Recovery { get { return .40f; } }
}

public sealed class BehaviourAction_AceTaffy_SteamFollowThrough : TaffySteamAction
{
    protected override float Preparation { get { return .10f; } }
    protected override float Recovery { get { return .25f; } }
}

// Mirrors the repository's bounded MemoryConcerto queue adapter. Any unknown
// opponent script, native queue, event or result delegates back to native play.
public abstract class TaffySteamAction : BehaviourActionBase
{
    protected abstract float Preparation { get; }
    protected abstract float Recovery { get; }

    public override List<RencounterManager.MovingAction> GetMovingAction(
        ref RencounterManager.ActionAfterBehaviour self,
        ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var nativeSelf = base.GetMovingAction(ref self, ref opponent);
        if (self.result != Result.Win || opponent.result != Result.Lose ||
            self.data.range != CardRange.Near || opponent.data.range != CardRange.Near ||
            self.data.actionType != ActionType.Atk || self.data.actionDetail != ActionDetail.Hit ||
            self.data.behaviourDetail != BehaviourDetail.Hit || self.data.skip || opponent.data.skip ||
            self.behaviourResultData == null || opponent.behaviourResultData == null ||
            self.behaviourResultData.isBlockedDice ||
            (self.infoList != null && self.infoList.Count > 0) ||
            (opponent.infoList != null && opponent.infoList.Count > 0) ||
            self.startMoveRoutineEvent != null || self.endMoveRoutineEvent != null ||
            opponent.startMoveRoutineEvent != null || opponent.endMoveRoutineEvent != null ||
            !OrdinaryReaction(opponent) || !DefaultLose(opponent))
            return nativeSelf;

        var nativeOpponent = base.GetMovingAction(ref opponent, ref self);
        if (nativeSelf.Count != 2 || nativeSelf[0].actionDetail != ActionDetail.Hit ||
            nativeSelf[0].moveState != CharMoveState.MoveForward ||
            nativeSelf[1].actionDetail != ActionDetail.Hit || nativeSelf[1].moveState != CharMoveState.Stop ||
            nativeOpponent.Count < 1 || nativeOpponent.Count > 2 ||
            nativeSelf.Exists(x => x.IsExistEffectTiming()) || nativeOpponent.Exists(x => x.IsExistEffectTiming()))
            return nativeSelf;

        nativeSelf[0].delay = .15f;
        nativeSelf[1].delay = Recovery;
        nativeSelf[0].SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
        nativeSelf.Insert(0, Hold(ActionDetail.Guard, Preparation));
        if (nativeOpponent.Count == 1)
            nativeOpponent.Add(Hold(nativeOpponent[0].actionDetail, Recovery));
        nativeOpponent.Insert(0, Hold(ActionDetail.Default, Preparation));
        opponent.infoList = nativeOpponent;
        return nativeSelf;
    }

    private static RencounterManager.MovingAction Hold(ActionDetail motion, float delay)
    {
        var result = new RencounterManager.MovingAction(motion, CharMoveState.Stop, 0f, false, delay, 1f);
        result.SetEffectTiming(EffectTiming.NONE, EffectTiming.NONE, EffectTiming.NONE);
        return result;
    }

    private static bool DefaultLose(RencounterManager.ActionAfterBehaviour opponent)
    {
        var raw = opponent.behaviourResultData.behaviourRawData;
        string script = raw == null ? null : raw.ActionScript;
        return string.IsNullOrEmpty(script) || script == "AceTaffy_SteamDetonation" ||
            script == "AceTaffy_SteamFollowThrough";
    }

    private static bool OrdinaryReaction(RencounterManager.ActionAfterBehaviour opponent)
    {
        var data = opponent.data;
        if (data.actionType == ActionType.None)
            return data.actionDetail == ActionDetail.Default || data.actionDetail == ActionDetail.Damaged ||
                data.actionDetail == ActionDetail.NONE;
        if (data.actionType == ActionType.Def)
            return (data.actionDetail == ActionDetail.Guard && data.behaviourDetail == BehaviourDetail.Guard) ||
                (data.actionDetail == ActionDetail.Evade && data.behaviourDetail == BehaviourDetail.Evasion);
        if (data.actionType == ActionType.Atk)
            return (data.actionDetail == ActionDetail.Slash && data.behaviourDetail == BehaviourDetail.Slash) ||
                (data.actionDetail == ActionDetail.Hit && data.behaviourDetail == BehaviourDetail.Hit) ||
                (data.actionDetail == ActionDetail.Penetrate && data.behaviourDetail == BehaviourDetail.Penetrate);
        return false;
    }
}
