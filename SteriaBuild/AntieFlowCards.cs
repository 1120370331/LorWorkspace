using System;
using System.Linq;
using HarmonyLib;
using Steria;

public class DiceCardSelfAbility_AntieShapeSea : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Spend all remaining Light this scene. Next scene, gain Flow equal to spent Light x 3. If at least 2 Light was spent, draw 1 page.";

    public override void OnUseCard()
    {
        if (owner?.cardSlotDetail == null)
        {
            return;
        }

        int remainingLight = Math.Max(0, owner.cardSlotDetail.PlayPoint);
        if (remainingLight <= 0)
        {
            return;
        }

        owner.cardSlotDetail.LosePlayPoint(remainingLight);
        owner.bufListDetail.AddBuf(new BattleUnitBuf_AntieFlowNextRound { stack = remainingLight * 3 });

        if (remainingLight >= 2)
        {
            owner.allyCardDetail?.DrawCards(1);
        }
    }
}

public class DiceCardSelfAbility_AntieReverseFlow : DiceCardSelfAbilityBase
{
    public static string Desc = "[After Action] If Flow is at least 4, consume 4 Flow and recover 4 Light. Otherwise, if Flow is at least 2, consume 2 Flow and draw 1 page.";

    public override void AfterAction()
    {
        base.AfterAction();

        if (owner == null)
        {
            return;
        }

        int flow = AntieFlowCardHelper.GetFlowStacks(owner);
        if (flow >= 4)
        {
            if (AntieFlowCardHelper.TryConsumeFlow(owner, 4))
            {
                owner.cardSlotDetail?.RecoverPlayPoint(4);
            }

            return;
        }

        if (flow >= 2 && AntieFlowCardHelper.TryConsumeFlow(owner, 2))
        {
            owner.allyCardDetail?.DrawCards(1);
        }
    }
}

public class BattleUnitBuf_AntieFlowNextRound : BattleUnitBuf
{
    public override bool Hide => true;
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void OnRoundStart()
    {
        base.OnRoundStart();

        if (_owner != null && !_owner.IsDead() && stack > 0)
        {
            CardAbilityHelper.AddFlowStacks(_owner, stack);
        }

        Destroy();
    }
}

public class DiceCardSelfAbility_AntieForceDrawOnBaseMax : DiceCardSelfAbilityBase
{
    public static string Desc = "[Clash] If this page's dice has a higher base maximum than the opposing dice, force a draw when it would lose the clash.";
}

public class DiceCardAbility_AntieGainFlowByVanillaOnHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);
        if (owner == null)
        {
            return;
        }

        int amount = Math.Max(1, behavior?.DiceVanillaValue ?? behavior?.GetDiceVanillaMax() ?? 0);
        CardAbilityHelper.AddFlowStacks(owner, amount);
    }
}

public class DiceCardAbility_AntieBonusDamageByVanillaOnHit : DiceCardAbilityBase
{
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        base.OnSucceedAttack(target);
        if (owner == null || target == null || target.IsDead())
        {
            return;
        }

        int damage = Math.Max(1, behavior?.DiceVanillaValue ?? behavior?.GetDiceVanillaMax() ?? 0);
        target.TakeDamage(damage, DamageType.Card_Ability, owner);
    }
}

[HarmonyPatch(typeof(BattleParryingManager), "GetDecisionResult")]
public static class BattleParryingManager_GetDecisionResult_AntieForceDrawPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        BattleParryingManager.ParryingTeam teamA,
        BattleParryingManager.ParryingTeam teamB,
        ref BattleParryingManager.ParryingDecisionResult __result)
    {
        if (__result == BattleParryingManager.ParryingDecisionResult.Draw)
        {
            return;
        }

        BattleParryingManager.ParryingTeam loser =
            __result == BattleParryingManager.ParryingDecisionResult.WinEnemy ? teamB : teamA;
        BattleParryingManager.ParryingTeam winner =
            __result == BattleParryingManager.ParryingDecisionResult.WinEnemy ? teamA : teamB;

        if (ShouldForceDraw(loser, winner))
        {
            __result = BattleParryingManager.ParryingDecisionResult.Draw;
        }
    }

    private static bool ShouldForceDraw(BattleParryingManager.ParryingTeam loser, BattleParryingManager.ParryingTeam winner)
    {
        BattleDiceBehavior losingDice = loser?.playingCard?.currentBehavior;
        BattleDiceBehavior winningDice = winner?.playingCard?.currentBehavior;
        if (losingDice == null || winningDice == null)
        {
            return false;
        }

        if (!HasForceDrawEffect(losingDice))
        {
            return false;
        }

        return losingDice.GetDiceVanillaMax() > winningDice.GetDiceVanillaMax();
    }

    private static bool HasForceDrawEffect(BattleDiceBehavior behavior)
    {
        if (behavior?.card?.card == null)
        {
            return false;
        }

        int cardId = behavior.card.card.GetID().id;
        return cardId == 9011501 ||
               cardId == 9011403 ||
               behavior.card.cardAbility is DiceCardSelfAbility_AntieForceDrawOnBaseMax;
    }
}

internal static class AntieFlowCardHelper
{
    public static int GetFlowStacks(BattleUnitModel unit)
    {
        BattleUnitBuf_Flow flow = GetFlowBuf(unit);
        return Math.Max(0, flow?.stack ?? 0);
    }

    public static bool TryConsumeFlow(BattleUnitModel unit, int amount)
    {
        if (unit == null || amount <= 0)
        {
            return false;
        }

        BattleUnitBuf_Flow flow = GetFlowBuf(unit);
        if (flow == null || flow.stack < amount)
        {
            return false;
        }

        bool hasProxy = DirectiveDreamHelper.HasStephanieProxy(unit);
        if (!hasProxy)
        {
            flow.stack -= amount;
            if (flow.stack <= 0)
            {
                flow.Destroy();
            }
        }

        HarmonyHelpers.NotifyPassivesOnFlowConsumed(unit, amount);
        return true;
    }

    private static BattleUnitBuf_Flow GetFlowBuf(BattleUnitModel unit)
    {
        return unit?.bufListDetail?.GetActivatedBufList()
            ?.FirstOrDefault(buf => buf is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
    }
}
