using System.Collections.Generic;
using LOR_DiceSystem;

// XML ActionScript is the public type name without "BehaviourAction_".
// Each die owns its rhythm: no shared counter, owner state or coroutine.
public sealed class BehaviourAction_Steria_MemoryConcerto_Long : SteriaMemoryConcertoAction
{
    protected override ActionDetail AttackMotion { get { return ActionDetail.Slash; } }
    protected override BehaviourDetail AttackDetail { get { return BehaviourDetail.Slash; } }
    protected override float Preparation { get { return .30f; } }
    protected override float Recovery { get { return .35f; } }
}

public sealed class BehaviourAction_Steria_MemoryConcerto_Medium : SteriaMemoryConcertoAction
{
    protected override ActionDetail AttackMotion { get { return ActionDetail.Hit; } }
    protected override BehaviourDetail AttackDetail { get { return BehaviourDetail.Hit; } }
    protected override float Preparation { get { return .10f; } }
    protected override float Recovery { get { return .25f; } }
}

public sealed class BehaviourAction_Steria_MemoryConcerto_Final : SteriaMemoryConcertoAction
{
    protected override ActionDetail AttackMotion { get { return ActionDetail.Penetrate; } }
    protected override BehaviourDetail AttackDetail { get { return BehaviourDetail.Penetrate; } }
    protected override float Preparation { get { return .50f; } }
    protected override float Recovery { get { return .55f; } }
}

// Deliberately not named BehaviourAction_: AssemblyManager scans that prefix.
public abstract class SteriaMemoryConcertoAction : SteriaTimedMeleeAction { }

public abstract class SteriaTimedMeleeAction : BehaviourActionBase
{
    protected abstract ActionDetail AttackMotion { get; }
    protected abstract BehaviourDetail AttackDetail { get; }
    protected abstract float Preparation { get; }
    protected abstract float Recovery { get; }
    protected virtual ActionDetail PreparationMotion { get { return ActionDetail.Guard; } }
    protected virtual ActionDetail StrikeMotion { get { return AttackMotion; } }
    protected virtual bool SupportsAppearance(BattleUnitView view) { return true; }

    public override List<RencounterManager.MovingAction> GetMovingAction(
        ref RencounterManager.ActionAfterBehaviour self,
        ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var nativeSelf = base.GetMovingAction(ref self, ref opponent);
        if (self.result != Result.Win || opponent.result != Result.Lose ||
            self.data.range != CardRange.Near || opponent.data.range != CardRange.Near ||
            self.data.actionType != ActionType.Atk || self.data.actionDetail != AttackMotion ||
            self.data.behaviourDetail != AttackDetail || self.data.skip || opponent.data.skip ||
            self.behaviourResultData == null || opponent.behaviourResultData == null ||
            self.behaviourResultData.isBlockedDice ||
            (self.infoList != null && self.infoList.Count > 0) ||
            (opponent.infoList != null && opponent.infoList.Count > 0) ||
            self.startMoveRoutineEvent != null || self.endMoveRoutineEvent != null ||
            opponent.startMoveRoutineEvent != null || opponent.endMoveRoutineEvent != null ||
            !HasOrdinaryReaction(opponent) || !HasDefaultLoseScript(opponent) ||
            !SupportsAppearance(self.view))
            return nativeSelf;

        var nativeOpponent = base.GetMovingAction(ref opponent, ref self);
        // Preserve native movement parameters and reaction objects. Unknown queue
        // shapes are not truncated, and existing custom opponent queues are never cleared.
        if (nativeSelf.Count != 2 || nativeSelf[0].actionDetail != AttackMotion ||
            nativeSelf[0].moveState != CharMoveState.MoveForward ||
            nativeSelf[1].actionDetail != AttackMotion || nativeSelf[1].moveState != CharMoveState.Stop ||
            nativeOpponent.Count < 1 || nativeOpponent.Count > 2 ||
            nativeSelf.Exists(x => x.IsExistEffectTiming()) ||
            nativeOpponent.Exists(x => x.IsExistEffectTiming()))
            return nativeSelf;

        nativeSelf[0].delay = .15f;
        nativeSelf[1].delay = Recovery;
        nativeSelf[0].actionDetail = nativeSelf[1].actionDetail = StrikeMotion;
        // PRE is the START of this attack segment, not a delay before it.
        // One explicit winner event prevents MoveRoutine's all-NONE early print.
        nativeSelf[0].SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
        nativeSelf.Insert(0, Hold(PreparationMotion, Preparation));

        if (nativeOpponent.Count == 1)
        {
            // A single native Knockback must run once, never repeat at the tail.
            nativeOpponent.Add(Hold(nativeOpponent[0].actionDetail, Recovery));
        }
        nativeOpponent.Insert(0, Hold(ActionDetail.Default, Preparation));
        opponent.infoList = nativeOpponent;
        return nativeSelf;
    }

    private static RencounterManager.MovingAction Hold(ActionDetail motion, float delay)
    {
        var action = new RencounterManager.MovingAction(motion, CharMoveState.Stop, 0f, false, delay, 1f);
        action.SetEffectTiming(EffectTiming.NONE, EffectTiming.NONE, EffectTiming.NONE);
        return action;
    }

    private static bool HasDefaultLoseScript(RencounterManager.ActionAfterBehaviour opponent)
    {
        var raw = opponent.behaviourResultData.behaviourRawData;
        string script = raw == null ? null : raw.ActionScript;
        // These entries all preserve the native Lose queue.
        return string.IsNullOrEmpty(script) || script == "Steria_MemoryConcerto_Long" ||
            script == "Steria_MemoryConcerto_Medium" || script == "Steria_MemoryConcerto_Final" ||
            script == "Steria_ChristashaSweep" || script == "Steria_ChristashaReturnCut" ||
            script == "Steria_ChristashaDownCut" || script == "Steria_ChristashaThrust" ||
            script == "Steria_ChristashaBlow";
    }

    private static bool HasOrdinaryReaction(RencounterManager.ActionAfterBehaviour opponent)
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
