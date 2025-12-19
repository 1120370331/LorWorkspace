using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine; // Needed for Debug.Log if used
// Assuming BaseMod is used for BattleObjectManager or other utilities if needed
using BaseMod; // Added for BattleObjectManager reference
using Steria; // Added to access HarmonyHelpers

// NOTE: BattleUnitBuf_Flow is defined in BattleUnitBuf_Flow.cs (MyDLL namespace)

// --- Slazeya Card Abilities ---

// SlazeyaGainFlow2NextTurn (Card Self Ability) - 逐梦随流
public class DiceCardSelfAbility_SlazeyaGainFlow2NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 2 Flow";

    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 2 });
    }
}

// SlazeyaGainFlow3NextTurn (Card Self Ability) - 川流不息
public class DiceCardSelfAbility_SlazeyaGainFlow3NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 3 Flow";

    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 3 });
    }
}

// SlazeyaOceanCommand (Card Self Ability) - 洋流，听我的号令
public class DiceCardSelfAbility_SlazeyaOceanCommand : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 3 Flow and draw 1 page";

    public override void OnUseCard()
    {
        // 下回合获得3层流
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 3 });
        // 抽取1张书页
        owner.allyCardDetail.DrawCards(1);
        Debug.Log($"[Steria] SlazeyaOceanCommand: Queued 3 Flow for next turn and drew 1 page");
    }
}

// SlazeyaClashLoseGainFlow3 (Dice Ability) - 拼点失败立刻获得3层流
public class DiceCardAbility_SlazeyaClashLoseGainFlow3 : DiceCardAbilityBase
{
    public static string Desc = "[Clash Lose] Immediately gain 3 Flow";

    public override void OnLoseParrying()
    {
        // 立即获得3层流（考虑流x2被动）
        Steria.FlowMultiplierHelper.AddFlowStacksWithMultiplier(owner, 3);
        Debug.Log($"[Steria] SlazeyaClashLoseGainFlow3: Gained 3 Flow on clash lose");
    }
}

// SlazeyaEndlessFlow (Card Self Ability) - 随我流向无尽的尽头
public class DiceCardSelfAbility_SlazeyaEndlessFlow : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] This round all allies don't consume Flow for bonuses. This page's dice gain power equal to Flow/2 (rounded up)";

    private int _calculatedPowerBonus = 0;

    public override void OnStartBattle()
    {
        // 本幕全体友方书页享受流加成时不消耗流
        Steria.HarmonyPatches.NoFlowConsumptionActiveThisRound = true;
        Debug.Log($"[Steria] SlazeyaEndlessFlow: Activated NoFlowConsumption for this round");

        // 给所有友方单位添加不消耗流的标记Buff
        if (BattleObjectManager.instance != null)
        {
            foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                ally.bufListDetail.AddBuf(new BattleUnitBuf_NoFlowConsumption());
            }
        }
    }

    public override void OnUseCard()
    {
        // 使用时计算威力加成（向上取整）
        BattleUnitBuf_Flow flowBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        int flowStacks = flowBuf?.stack ?? 0;
        _calculatedPowerBonus = (flowStacks + 1) / 2; // 向上取整
        Debug.Log($"[Steria] SlazeyaEndlessFlow: OnUseCard - Calculated power bonus = {_calculatedPowerBonus} (Flow: {flowStacks})");
    }

    // 在骰子投掷前应用威力加成（这样可以正确处理 Standby 骰子）
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (_calculatedPowerBonus > 0 && behavior != null)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = _calculatedPowerBonus });
            Debug.Log($"[Steria] SlazeyaEndlessFlow: Applied +{_calculatedPowerBonus} power to dice (Index: {behavior.Index})");
        }
    }
}

// SlazeyaGainFlow5OnRoundStart (Card Self Ability)
public class DiceCardSelfAbility_SlazeyaGainFlow5OnRoundStart : DiceCardSelfAbilityBase
{
     public static string Desc = "[Round Start] Gain 5 Flow";

    // Per setting "回合开始时：获得5层"流"", assuming this means when the card action starts
    public override void OnStartBattle()
    {
         Steria.CardAbilityHelper.AddFlowStacks(owner, 5);
    }
}


// SlazeyaFlowBonusX2 (Card Self Ability) - Complex Interaction
public class DiceCardSelfAbility_SlazeyaFlowBonusX2 : DiceCardSelfAbilityBase
{
    public static string Desc = "Flow bonus for this page is doubled";
    // Needs a marker buff to signal the patch
    public class BattleUnitBuf_FlowBonusX2Marker : BattleUnitBuf {
        // public override bool IsContagious() => false; // Removed invalid override
        public override BufPositiveType positiveType => BufPositiveType.Positive;
        public override void OnRoundEnd() { this.Destroy(); } // Lasts one round
    }

    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_FlowBonusX2Marker());
    }
}

// SlazeyaMassAttackTeamLightGain (Card Self Ability) - 倾覆万千之流
// 使用时：消耗所有流（但不提供威力加成），每消耗5层流下回合为所有友方恢复1点光芒
public class DiceCardSelfAbility_SlazeyaMassAttackTeamLightGain : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Consume all Flow (no power bonus). For every 5 Flow spent, next turn all allies gain 1 Light";
    public override void OnUseCard()
    {
        // 使用新的群攻卡牌流消耗记录（在 RegisterCardUsage 中已消耗所有流）
        int flowConsumedByThisCard = HarmonyHelpers.GetMassAttackFlowConsumed(this.card);

        Debug.Log($"[Steria] SlazeyaMassAttackTeamLightGain: Flow consumed = {flowConsumedByThisCard}");

        if (flowConsumedByThisCard > 0) {
            int lightToGain = flowConsumedByThisCard / 5;
            if (lightToGain > 0)
            {
               if (BattleObjectManager.instance != null) {
                   foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction)) {
                       ally.bufListDetail.AddBuf(new BattleUnitBuf_GainLightNextTurn() { stack = lightToGain });
                   }
                   Debug.Log($"[Steria] SlazeyaMassAttackTeamLightGain: Granting {lightToGain} light next turn to allies due to {flowConsumedByThisCard} flow consumed.");
               } else {
                   Debug.LogError("[Steria] BattleObjectManager.instance is null!");
               }
            }
        } else {
             Debug.Log($"[Steria] SlazeyaMassAttackTeamLightGain: No Flow consumed for this card action.");
        }
    }
}

// --- Slazeya Dice Abilities ---

// SlazeyaGainLight1
public class DiceCardAbility_SlazeyaGainLight1 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Light";
    public override void OnSucceedAttack()
    {
        owner.cardSlotDetail.RecoverPlayPoint(1);
    }
}

// SlazeyaClashWinGainFlow2NextTurn
public class DiceCardAbility_SlazeyaClashWinGainFlow2NextTurn : DiceCardAbilityBase
{
    public static string Desc = "[Clash Win] Next turn gain 2 Flow";
    public override void OnWinParrying()
    {
         owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 2 });
    }
}

// SlazeyaClashLosePowerUpNextDice
public class DiceCardAbility_SlazeyaClashLosePowerUpNextDice : DiceCardAbilityBase
{
     public static string Desc = "[Clash Lose] Power up next dice by this dice's base power";
     // Implementation requires Harmony Patch on clash resolution or LoseParrying
     public override void OnLoseParrying()
     {
         // The logic to find the next dice and apply power needs a patch.
         // A patch on LoseParrying can get 'this' behavior (__instance)
         // and then iterate through this.card.GetDiceBehaviorList() to find the next one.
         Debug.LogWarning("SlazeyaClashLosePowerUpNextDice effect requires Harmony patching.");
     }
}

// SlazeyaDraw2
public class DiceCardAbility_SlazeyaDraw2 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Draw 2 pages";
    public override void OnSucceedAttack()
    {
        owner.allyCardDetail.DrawCards(2);
    }
}

// SlazeyaGainFlow1OnHit
public class DiceCardAbility_SlazeyaGainFlow1OnHit : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 1 Flow";
    public override void OnSucceedAttack()
    {
       Steria.CardAbilityHelper.AddFlowStacks(owner, 1);
    }
}

// SlazeyaBleed5OnFlow5 - 风暴分流的骰子效果
// 命中时：如果本骰子被流强化过至少5次，则施加5层流血
public class DiceCardAbility_SlazeyaBleed5OnFlow5 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] If this dice received at least 5 Flow enhancements, apply 5 Bleed";

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null || this.card == null || this.behavior == null) return;

        int flowEnhancementCount = HarmonyHelpers.GetFlowEnhancementCountForDice(this.card, this.behavior.Index);
        Debug.Log($"[Steria] SlazeyaBleed5OnFlow5: Flow enhancement count = {flowEnhancementCount}");

        if (flowEnhancementCount >= 5)
        {
            target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 5, owner);
            Debug.Log($"[Steria] SlazeyaBleed5OnFlow5: Applied 5 Bleed to {target.UnitData?.unitData?.name}");
        }
    }
}

// SlazeyaMassAttackBonusDamage - 倾覆万千之流的骰子效果
// 命中时：追加本书页消耗的流x1点伤害
public class DiceCardAbility_SlazeyaMassAttackBonusDamage : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Add bonus damage equal to Flow spent";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        // 使用新的群攻卡牌流消耗记录
        int flowConsumedByThisCard = HarmonyHelpers.GetMassAttackFlowConsumed(this.card);

        Debug.Log($"[Steria] SlazeyaMassAttackBonusDamage: Flow consumed = {flowConsumedByThisCard}");

        if (flowConsumedByThisCard > 0) {
             int bonusDamage = flowConsumedByThisCard; // x1 instead of x2
            target.TakeDamage(bonusDamage, DamageType.Card_Ability, owner);
             Debug.Log($"[Steria] SlazeyaMassAttackBonusDamage: Dealt {bonusDamage} bonus damage to {target.UnitData.unitData.name} due to {flowConsumedByThisCard} flow consumed.");
        } else {
             Debug.Log($"[Steria] SlazeyaMassAttackBonusDamage: No Flow consumed for this card action.");
        }
    }
}


// --- Cultist Card Abilities ---

// CultistGainFlow3NextTurn
public class DiceCardSelfAbility_CultistGainFlow3NextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain 3 Flow";
    public override void OnUseCard()
    {
        owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = 3 }); // Reusing the helper buff
    }
}

// CultistFlow2CheckGainLightDraw
public class DiceCardSelfAbility_CultistFlow2CheckGainLightDraw : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] If Flow spent >= 2, next turn gain 2 Light and draw 1 page";
    public override void OnUseCard()
    {
        // Corrected: Use HarmonyHelpers from MyDLL namespace
        int flowConsumedByThisCard = HarmonyHelpers.GetFlowConsumedByCard(this.card);

        if (flowConsumedByThisCard >= 2)
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_GainLightNextTurn() { stack = 2 });
            owner.bufListDetail.AddBuf(new BattleUnitBuf_DrawNextTurn() { stack = 1 });
            Debug.Log($"[MyDLL] CultistFlow2CheckGainLightDraw: Granting light/draw next turn due to {flowConsumedByThisCard} flow consumed.");
        } else {
             // Changed LogWarning to Debug
             Debug.Log($"[MyDLL] CultistFlow2CheckGainLightDraw: Flow consumed ({flowConsumedByThisCard}) < 2. No effect.");
        }
    }
}

// CultistGainFlowAllyCountNextTurn
public class DiceCardSelfAbility_CultistGainFlowAllyCountNextTurn : DiceCardSelfAbilityBase
{
    public static string Desc = "[On Use] Next turn gain Flow equal to number of surviving allies";
    public override void OnUseCard()
    {
         if (BattleObjectManager.instance != null) {
             int aliveAllies = BattleObjectManager.instance.GetAliveList(owner.faction).Count(x => x != owner);
             if (aliveAllies > 0) {
                  owner.bufListDetail.AddBuf(new BattleUnitBuf_SlazeyaFlowNextTurn() { stack = aliveAllies });
                  Debug.Log($"[MyDLL] CultistGainFlowAllyCountNextTurn: Granting {aliveAllies} flow next turn.");
             }
         } else {
              Debug.LogError("[MyDLL] BattleObjectManager.instance is null!");
         }
    }
}

// CultistFlowBonusX2
public class DiceCardSelfAbility_CultistFlowBonusX2 : DiceCardSelfAbilityBase
{
     public static string Desc = "Flow bonus for this page is doubled";
     // Needs a marker buff to signal the patch (can reuse Slazeya's marker)
     public override void OnUseCard() {
          owner.bufListDetail.AddBuf(new DiceCardSelfAbility_SlazeyaFlowBonusX2.BattleUnitBuf_FlowBonusX2Marker());
     }
}


// --- Cultist Dice Abilities ---

// CultistDraw1
public class DiceCardAbility_CultistDraw1 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Draw 1 page";
    public override void OnSucceedAttack()
    {
        owner.allyCardDetail.DrawCards(1);
    }
}

// CultistGainFlow3
public class DiceCardAbility_CultistGainFlow3 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Gain 3 Flow";
    public override void OnSucceedAttack()
    {
       Steria.CardAbilityHelper.AddFlowStacks(owner, 3);
    }
}

// CultistBleed2 - 命中时施加2层流血
public class DiceCardAbility_CultistBleed2 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 2 Bleed";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 2, owner);
    }
}

// CultistBleed1 - 命中时施加1层流血
public class DiceCardAbility_CultistBleed1 : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply 1 Bleed";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;
        target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 1, owner);
    }
}

// CultistFlowPowerBonus - 顺流而为：流转 + 流>=5时威力+1
public class DiceCardSelfAbility_CultistFlowPowerBonus : DiceCardSelfAbilityBase
{
    public static string Desc = "[Flow Transfer] If Flow >= 5, this page's dice gain +1 power";
    private int _powerBonus = 0;

    public override void OnUseCard()
    {
        // 检查流层数是否>=5
        BattleUnitBuf_Flow flowBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        int flowStacks = flowBuf?.stack ?? 0;

        if (flowStacks >= 5)
        {
            _powerBonus = 1;
            Debug.Log($"[Steria] CultistFlowPowerBonus: Flow >= 5 ({flowStacks}), dice will get +1 power");
        }
        else
        {
            _powerBonus = 0;
        }
    }

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (_powerBonus > 0 && behavior != null)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = _powerBonus });
            Debug.Log($"[Steria] CultistFlowPowerBonus: Applied +{_powerBonus} power to dice");
        }
    }
}

// --- 百川逐风 Card Self Ability ---

// SlazeyaMultiFlowBonus2 - 本书页骰子至多可受2次流强化（标记能力，实际逻辑在HarmonyPatches中）
public class DiceCardSelfAbility_SlazeyaMultiFlowBonus2 : DiceCardSelfAbilityBase
{
    public static string Desc = "This page's dice can receive up to 2 Flow enhancements";
    // 实际逻辑由 HarmonyPatches._multiFlowBonusCards 字典处理
    // 此能力仅作为标记存在
}

// SlazeyaHundredRiversRepeat - 百川逐风：2次流强化 + 流>10时消耗10流重复使用（每幕一次）
public class DiceCardSelfAbility_SlazeyaHundredRiversRepeat : DiceCardSelfAbilityBase
{
    public static string Desc = "This page's dice can receive up to 2 Flow enhancements. If Flow > 10, consume 10 Flow and repeat on another random enemy (max once per round)";
    private static HashSet<BattleUnitModel> _triggeredThisRoundOwners = new HashSet<BattleUnitModel>(); // 每幕每角色最多触发一次
    private BattleUnitModel _repeatTarget = null;

    // 在幕开始时重置触发记录（由HarmonyPatches调用）
    public static void ResetTriggeredOwners()
    {
        _triggeredThisRoundOwners.Clear();
    }

    public override void OnUseCard()
    {
        // 检查流层数是否大于10
        BattleUnitBuf_Flow flowBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
        int flowStacks = flowBuf?.stack ?? 0;

        Debug.Log($"[Steria] SlazeyaHundredRiversRepeat: Flow stacks = {flowStacks}");

        // 检查是否本幕已触发过
        if (flowStacks > 10 && !_triggeredThisRoundOwners.Contains(owner))
        {
            _triggeredThisRoundOwners.Add(owner);

            // 消耗10层流
            flowBuf.stack -= 10;
            if (flowBuf.stack <= 0)
            {
                flowBuf.Destroy();
            }
            Debug.Log($"[Steria] SlazeyaHundredRiversRepeat: Consumed 10 Flow, remaining: {flowBuf?.stack ?? 0}");

            // 获取除当前目标外的其他敌人
            List<BattleUnitModel> enemies = BattleObjectManager.instance.GetAliveList(
                owner.faction == Faction.Player ? Faction.Enemy : Faction.Player);

            // 排除当前目标
            BattleUnitModel currentTarget = card.target;
            List<BattleUnitModel> otherEnemies = enemies.Where(e => e != currentTarget && !e.IsBreakLifeZero() && !e.IsDead()).ToList();

            if (otherEnemies.Count > 0)
            {
                // 随机选择一个敌人作为重复攻击目标
                _repeatTarget = otherEnemies[UnityEngine.Random.Range(0, otherEnemies.Count)];
                Debug.Log($"[Steria] SlazeyaHundredRiversRepeat: Will repeat on {_repeatTarget.UnitData?.unitData?.name}");
            }
        }
    }

    public override void OnEndBattle()
    {
        // 如果有重复攻击目标，使用原版游戏的方式添加重复攻击
        if (_repeatTarget != null && !_repeatTarget.IsDead() && !_repeatTarget.IsBreakLifeZero() && !owner.IsDead() && !owner.IsBreakLifeZero())
        {
            try
            {
                Debug.Log($"[Steria] SlazeyaHundredRiversRepeat: Adding repeat attack on {_repeatTarget.UnitData?.unitData?.name}");

                // 创建新的卡牌行动（针对新目标）
                BattlePlayingCardDataInUnitModel repeatCard = new BattlePlayingCardDataInUnitModel();
                repeatCard.owner = owner;
                repeatCard.card = card.card;
                repeatCard.target = _repeatTarget;
                int targetSlot = 0;
                if (_repeatTarget.speedDiceResult != null && _repeatTarget.speedDiceResult.Count > 0)
                {
                    targetSlot = UnityEngine.Random.Range(0, _repeatTarget.speedDiceResult.Count);
                }
                repeatCard.targetSlotOrder = targetSlot;
                repeatCard.earlyTarget = _repeatTarget;
                repeatCard.earlyTargetOrder = targetSlot;
                repeatCard.slotOrder = card.slotOrder;
                repeatCard.speedDiceResultValue = card.speedDiceResultValue;
                repeatCard.cardAbility = null; // 不设置能力，避免再次触发重复
                repeatCard.ResetCardQueue();

                // 使用原版游戏的方式添加重复攻击（像陷阵之志一样）
                Singleton<StageController>.Instance.AddAllCardListInBattle(repeatCard, _repeatTarget, targetSlot);

                Debug.Log($"[Steria] SlazeyaHundredRiversRepeat: Successfully added repeat attack");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] SlazeyaHundredRiversRepeat: Error adding repeat attack: {ex.Message}");
            }
        }
        _repeatTarget = null;
    }
}

// --- 百川逐风 Dice Abilities ---

// SlazeyaApplyWeakByFlowBonus - 命中时施加本骰子受流强化层虚弱
public class DiceCardAbility_SlazeyaApplyWeakByFlowBonus : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Apply Weak equal to this dice's flow enhancement count";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null || this.card == null || this.behavior == null) return;

        int flowEnhancementCount = HarmonyHelpers.GetFlowEnhancementCountForDice(this.card, this.behavior.Index);
        if (flowEnhancementCount > 0)
        {
            target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Weak, flowEnhancementCount, owner);
            Debug.Log($"[Steria] SlazeyaApplyWeakByFlowBonus: Applied {flowEnhancementCount} Weak to {target.UnitData?.unitData?.name}");
        }
    }
}

// SlazeyaApplyBleedAndDamageByFlowBonus - 命中时追加5点伤害和本骰子受流强化层流血
public class DiceCardAbility_SlazeyaApplyBleedAndDamageByFlowBonus : DiceCardAbilityBase
{
    public static string Desc = "[On Hit] Deal 5 bonus damage and apply Bleed equal to this dice's flow enhancement count";
    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;

        // 追加5点伤害
        target.TakeDamage(5, DamageType.Card_Ability, owner);
        Debug.Log($"[Steria] SlazeyaApplyBleedAndDamageByFlowBonus: Dealt 5 bonus damage to {target.UnitData?.unitData?.name}");

        // 施加流强化层流血
        if (this.card == null || this.behavior == null) return;
        int flowEnhancementCount = HarmonyHelpers.GetFlowEnhancementCountForDice(this.card, this.behavior.Index);
        if (flowEnhancementCount > 0)
        {
            target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, flowEnhancementCount, owner);
            Debug.Log($"[Steria] SlazeyaApplyBleedAndDamageByFlowBonus: Applied {flowEnhancementCount} Bleed to {target.UnitData?.unitData?.name}");
        }
    }
}


// --- Helper Buffs (Implementations + Fixes) ---

// Buff to gain Flow next turn
public class BattleUnitBuf_SlazeyaFlowNextTurn : BattleUnitBuf
{
    protected override string keywordId => "MyMod_FlowNextTurn"; // Needs registration via Localize/EffectTexts
    protected override string keywordIconId => "FlowIcon"; // Needs an icon resource
    // Removed keywordTemplateId

    public override void OnRoundStart()
    {
        if (_owner == null) { this.Destroy(); return; }
        Steria.CardAbilityHelper.AddFlowStacks(_owner, this.stack);
        _owner.bufListDetail.RemoveBuf(this);
    }
}

// Buff to gain Light next turn
public class BattleUnitBuf_GainLightNextTurn : BattleUnitBuf
{
     public override void OnRoundStart()
     {
        if (_owner == null) { this.Destroy(); return; }
        _owner.cardSlotDetail.RecoverPlayPoint(this.stack);
        _owner.bufListDetail.RemoveBuf(this);
     }
}

// Buff to draw cards next turn
public class BattleUnitBuf_DrawNextTurn : BattleUnitBuf
{
     public override void OnRoundStart()
     {
         if (_owner == null) { this.Destroy(); return; }
        _owner.allyCardDetail.DrawCards(this.stack);
         _owner.bufListDetail.RemoveBuf(this);
     }
}

// --- CardAbilityHelper Class (Copied from AnhierCards.cs) ---
// Ensure this class is defined within the scope or accessible
// Added namespace Steria for consistency and potential future conflicts avoidance
namespace Steria 
{
    public static class CardAbilityHelper
    {
        // Helper to add Flow stacks safely
        public static void AddFlowStacks(BattleUnitModel owner, int amount)
        {
            SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks called: owner={owner?.UnitData?.unitData?.name}, amount={amount}");

            if (owner == null || amount <= 0)
            {
                SteriaLogger.LogWarning($"CardAbilityHelper.AddFlowStacks: Early return (owner null or amount <= 0)");
                return;
            }

            BattleUnitBuf_Flow existingFlow = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;
            SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: existingFlow = {(existingFlow != null ? "found" : "null")}");

            if (existingFlow != null)
            {
                existingFlow.stack += amount;
                SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: Updated existing Flow. New stack: {existingFlow.stack}");
            }
            else
            {
                BattleUnitBuf_Flow newFlow = new BattleUnitBuf_Flow();
                newFlow.stack = amount;
                owner.bufListDetail.AddBuf(newFlow);
                SteriaLogger.Log($"CardAbilityHelper.AddFlowStacks: Created new Flow buff with {amount} stacks");
            }
        }
    }
} 