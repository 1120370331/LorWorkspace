using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq; // Needed for Linq operations like Random
using UnityEngine;
using MyDLL; // Added using for MyDLL namespace

namespace MyDLL
{
    // --- REMOVED Helper Functions ---
    // CardAbilityHelper is now defined in CardAbilities.cs within MyDLL namespace

    // --- Dice Card Abilities (Effects on Dice) ---

    #region 拙劣控流 (ID: 9001001) Dice 1
    // 骰子1：命中时：获得1层"流"
    public class DiceCardAbility_AnhierFlow1 : DiceCardAbilityBase
    {
        public static string Desc = "[命中时] 获得1层[流]";
        public override void OnSucceedAttack() // Use OnSucceedAttack for on hit effects
        {
            CardAbilityHelper.AddFlowStacks(this.owner, 1);
        }
    }
    #endregion

    #region 回忆侧斩 (ID: 9001002) Dice 1
    // 骰子1：命中时：抽取1张书页
    public class DiceCardAbility_AnhierDraw1 : DiceCardAbilityBase
    {
        public static string Desc = "[命中时] 抽取1张书页";
        public override void OnSucceedAttack()
        {
            this.owner.allyCardDetail.DrawCards(1);
        }
    }
    #endregion

    #region 奥塔尔的荣耀 (ID: 9001003) Dice 1 & 2
    // 骰子1：拼点胜利：抽取1张书页
    public class DiceCardAbility_AnhierClashWinDraw1 : DiceCardAbilityBase
    {
        public static string Desc = "[拼点胜利] 抽取1张书页";
        public override void OnWinParrying()
        {
            this.owner.allyCardDetail.DrawCards(1);
        }
    }

    // 骰子2：命中时：恢复2点光芒
    public class DiceCardAbility_AnhierGainLight2 : DiceCardAbilityBase
    {
        public static string Desc = "[命中时] 恢复2点光芒";
        public override void OnSucceedAttack()
        {
            this.owner.cardSlotDetail.RecoverPlayPoint(2);
        }
    }
    #endregion

    #region 攫取回忆 (ID: 9001005) Dice 1 & 2
    // 骰子1/2：命中时：抽取1张书页
    // 可以复用 DiceCardAbility_AnhierDraw1，或者创建一个新的以防万一需要区分
    public class DiceCardAbility_AnhierGrabDraw1 : DiceCardAbilityBase
    {
        public static string Desc = "[命中时] 抽取1张书页";
        public override void OnSucceedAttack()
        {
            this.owner.allyCardDetail.DrawCards(1);
        }
    }
    #endregion


    // --- Self Card Abilities (Effects On Use) ---

    #region 拙劣控流 (ID: 9001001) On Use
    // 使用时：恢复1点光芒
    public class DiceCardSelfAbility_AnhierRecoverLight1 : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时] 恢复1点光芒";
        public override void OnUseCard()
        {
            this.owner.cardSlotDetail.RecoverPlayPoint(1);
        }
    }
    #endregion

    #region 回忆侧斩 (ID: 9001002) On Use
    // 使用时：从手中随机丢弃1张书页
    public class DiceCardSelfAbility_AnhierDiscardRandom1 : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时] 从手中随机丢弃1张书页";
        public override void OnUseCard()
        {
            this.owner.allyCardDetail.DisCardACardRandom();
        }
    }
    #endregion

    #region 奥塔尔的荣耀 (ID: 9001003) On Use
    // 使用时：丢弃手中所有书页，并抽取X张书页（X=丢弃的书页数量）
    public class DiceCardSelfAbility_AnhierDiscardAllDraw : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时] 丢弃手中所有书页，并抽取等量的书页";
        public override void OnUseCard()
        {
            List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
            int discardedCount = 0;
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                BattleDiceCardModel cardToDiscard = hand[i];
                this.owner.allyCardDetail.DiscardACardByAbility(cardToDiscard);
                discardedCount++;
            }

            if (discardedCount > 0)
            {
                this.owner.allyCardDetail.DrawCards(discardedCount);
            }
        }
    }
    #endregion

    #region 回忆协奏之刃 (ID: 9001004) On Use
    // 使用时：丢弃手中所有书页，并使本书页骰子威力+X（X=丢弃的书页数量）
    public class DiceCardSelfAbility_AnhierDiscardAllPowerUp : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时] 丢弃手中所有书页，并使本书页所有骰子威力+{0}"; // {0} will be replaced by the count

        public override void OnUseCard() // Moved logic from OnStartBattle to OnUseCard
        {
            UnityEngine.Debug.Log($"[MyDLL] {GetType().Name}: OnUseCard triggered."); // Log entry
            List<BattleDiceCardModel> hand = this.owner.allyCardDetail.GetHand();
            int discardedCount = 0;
            // Use Clone() to avoid modifying the list while iterating if DiscardACardByAbility affects the original hand list immediately
            List<BattleDiceCardModel> handClone = new List<BattleDiceCardModel>(hand); 
            foreach (BattleDiceCardModel cardToDiscard in handClone) 
            {
                 // Check if the card being considered for discard is THIS card instance.
                 // We don't want the EGO page to discard itself during its own effect resolution.
                 // Compare instances directly. this.card.card should be the BattleDiceCardModel.
                 if (cardToDiscard == this.card.card) continue; // Use this.card.card

                this.owner.allyCardDetail.DiscardACardByAbility(cardToDiscard); 
                discardedCount++;
            }
            UnityEngine.Debug.Log($"[MyDLL] {GetType().Name}: Discarded {discardedCount} cards."); // Log discard count

            int powerBonus = discardedCount;
            if (powerBonus > 0)
            {
                UnityEngine.Debug.Log($"[MyDLL] {GetType().Name}: Applying power bonus: {powerBonus}"); // Log bonus application
                int behaviorIndex = 0;
                // IMPORTANT: Access dice behaviors via this.card.GetDiceBehaviorList()
                foreach (BattleDiceBehavior behavior in this.card.GetDiceBehaviorList())
                {
                    UnityEngine.Debug.Log($"[MyDLL] {GetType().Name}: Applying bonus to behavior {behaviorIndex}."); // Log bonus application per behavior
                    behavior.ApplyDiceStatBonus(new DiceStatBonus { power = powerBonus });
                    behaviorIndex++;
                }
                 // Dynamic description update might be less reliable here, consider removing or testing thoroughly
                 // card.card.SetCurrentDescFixed("[使用时] 丢弃手中所有书页，并使本书页所有骰子威力+" + powerBonus);
            }
            else
            {
                 UnityEngine.Debug.Log($"[MyDLL] {GetType().Name}: No cards discarded, no power bonus applied."); // Log zero discard case
            }
        }

        // Keep OnStartBattle empty or remove if not needed
        // public override void OnStartBattle()
        // {
        // }

         // Override GetDesc() maybe? Or handle formatting where text is displayed.
    }
    #endregion

    #region 内调之流 (ID: 9001007) On Use
    // 使用时：下回合获得5层"流", 抽取2张书页
    public class DiceCardSelfAbility_AnhierGainFlow5Draw2NextTurn : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时] 抽取2张书页，下一幕开始时获得5层[流]";
        private bool _effectTriggered = false; // Prevent duplicate calls if OnUseCard is called multiple times

        public override void OnUseCard()
        {
             if (_effectTriggered) return;
             _effectTriggered = true;

             this.owner.allyCardDetail.DrawCards(2);
             // Add a temporary buff that adds Flow on the next round start
             this.owner.bufListDetail.AddBuf(new BattleUnitBuf_AddFlowNextRound() { amount = 5 });
        }
    }

    // Helper Buff to add Flow next round
    public class BattleUnitBuf_AddFlowNextRound : BattleUnitBuf
    {
        public int amount = 0;
        public override void OnRoundStart()
        {
            CardAbilityHelper.AddFlowStacks(this._owner, amount);
            this.Destroy(); // Destroy self after applying flow
        }
         public override void Init(BattleUnitModel owner) { 
             base.Init(owner); 
         } 
    }
    #endregion

} 