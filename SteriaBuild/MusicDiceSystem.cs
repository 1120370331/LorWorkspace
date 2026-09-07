using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using LOR_DiceSystem;

namespace Steria
{
    /// <summary>
    /// 乐章型骰子（Music-type dice）核心系统。
    ///
    /// 乐章型骰子是并列于"进攻、防御、反击"的独立骰子类型：
    ///   - 视觉：蓝白渐变外观
    ///   - 伤害：固定 1.0x 体力、1.25x 混乱，无视斩/打/突抗性
    ///   - 拼点：与防御骰拼点时跳过拼点，直接造成伤害，并把防御骰挪到书页末尾
    ///   - 抗性：独立的"乐章抗性"，默认 1.0
    ///   - 不受强壮 / 忍耐 / 进攻型骰子威力加成的影响
    ///   - 专属威力加成：强音（BattleUnitBuf_Forte）
    ///
    /// 识别：卡牌带有 SteriaMusicDice 关键词，且骰子为攻击型（Slash / Penetrate / Hit）。
    /// </summary>
    public static class MusicDiceSystem
    {
        public const string MusicDiceKeyword = "SteriaMusicDice";
        public const string ForteKeyword = "SteriaForte";

        public const float MusicHpMultiplier = 1.0f;
        public const float MusicStaggerMultiplier = 1.25f;

        // ===== Identification =====

        public static bool IsMusicCard(BattleDiceCardModel card)
        {
            return card?.XmlData?.Keywords != null && card.XmlData.Keywords.Contains(MusicDiceKeyword);
        }

        public static bool IsMusicCard(DiceCardItemModel card)
        {
            return card?.ClassInfo?.Keywords != null && card.ClassInfo.Keywords.Contains(MusicDiceKeyword);
        }

        public static bool IsMusicCard(LorId cardId)
        {
            if (ItemXmlDataList.instance == null)
            {
                return false;
            }

            DiceCardXmlInfo info = ItemXmlDataList.instance.GetCardItem(cardId, false);
            return info?.Keywords != null && info.Keywords.Contains(MusicDiceKeyword);
        }

        /// <summary>
        /// 一颗骰子是否属于乐章型：所在书页带 SteriaMusicDice 关键词，且骰子细节为攻击型。
        /// 防御骰 / 反击骰 / 闪避骰即便挂在乐章卡上，也不会成为乐章型骰子。
        /// </summary>
        public static bool IsMusicDiceBehaviour(BattleDiceBehavior behavior)
        {
            if (behavior == null)
            {
                return false;
            }

            if (!IsMusicCard(behavior.card?.card))
            {
                return false;
            }

            BehaviourDetail detail = behavior.Detail;
            return detail == BehaviourDetail.Slash
                || detail == BehaviourDetail.Penetrate
                || detail == BehaviourDetail.Hit;
        }

        /// <summary>书页 XML 上的单颗骰（用于 UI 行与 DiceBehaviourList 对齐）。</summary>
        public static bool IsMusicAttackDiceBehaviour(DiceBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            BehaviourDetail d = behaviour.Detail;
            return d == BehaviourDetail.Slash || d == BehaviourDetail.Penetrate || d == BehaviourDetail.Hit;
        }

        public static DiceBehaviour TryGetDiceBehaviourAt(BattleDiceCardModel cardModel, int index)
        {
            IList<DiceBehaviour> list = cardModel?.GetBehaviourList();
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        /// <summary>卡组 / 卡面条 UI 使用的 DiceCardItemModel（ClassInfo 等同 Xml）。</summary>
        public static DiceBehaviour TryGetDiceBehaviourAt(DiceCardItemModel cardModel, int index)
        {
            IList<DiceBehaviour> list = cardModel?.GetBehaviourList();
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        // ===== Resistance =====

        /// <summary>
        /// 目标的"乐章抗性"乘数，默认 1.0。
        /// 后续可通过 buff / passive 修改（例如减免伤害的乐章护盾等）。
        /// </summary>
        public static float GetMusicResistance(BattleUnitModel target)
        {
            return 1.0f;
        }
    }

    /// <summary>
    /// 乐章型骰子造成伤害时进入的线程局部上下文。
    /// 用于让 BookModel.GetResistRate 知道当前正在结算乐章型骰子伤害，
    /// 并按"第 1 次 = 体力"、"第 2 次 = 混乱"的顺序返回固定乘数。
    /// </summary>
    public static class MusicDamageContext
    {
        [ThreadStatic] private static int _depth;
        [ThreadStatic] private static int _callIndex;
        [ThreadStatic] private static BattleUnitModel _currentTarget;

        public static bool IsActive => _depth > 0;

        public static void Enter(BattleUnitModel target)
        {
            if (_depth == 0)
            {
                _callIndex = 0;
                _currentTarget = target;
            }
            _depth++;
        }

        public static void Exit()
        {
            if (_depth > 0)
            {
                _depth--;
            }
            if (_depth == 0)
            {
                _callIndex = 0;
                _currentTarget = null;
            }
        }

        /// <summary>
        /// 在乐章型骰子伤害结算路径中，按调用顺序返回应替换 BookModel.GetResistRate 的结果。
        /// 第 1 次调用 -> 体力倍率：MusicHpMultiplier * 乐章抗性 * 混乱倍率
        /// 第 2 次调用 -> 混乱倍率：MusicStaggerMultiplier * 乐章抗性 * 混乱倍率
        /// 其余情况返回 null，表示不替换。
        ///
        /// 混乱倍率：当目标已陷入混乱（IsBreakLifeZero）且其被动未禁止
        /// "陷入混乱时抗性变化"，与原版一致额外 ×2，模拟 vanilla 把抗性
        /// 直接读成 AtkResist.Weak 的效果。
        /// </summary>
        public static float? NextResistRate()
        {
            if (_depth <= 0)
            {
                return null;
            }

            _callIndex++;
            float resistance = MusicDiceSystem.GetMusicResistance(_currentTarget);
            float breakMultiplier = GetBreakMultiplier(_currentTarget);

            if (_callIndex == 1)
            {
                return MusicDiceSystem.MusicHpMultiplier * resistance * breakMultiplier;
            }
            if (_callIndex == 2)
            {
                return MusicDiceSystem.MusicStaggerMultiplier * resistance * breakMultiplier;
            }
            return null;
        }

        /// <summary>
        /// 与原版 BattleUnitModel.GetResistHP / GetResistBP 起头处的逻辑一致：
        /// 目标陷入混乱 (IsBreakLifeZero) 且被动允许时，抗性视作 Weak (2.0×)。
        /// 我们直接把这个倍率乘到乐章固定倍率上，让"陷入混乱受 2× 伤害"对乐章一样生效。
        /// </summary>
        private static float GetBreakMultiplier(BattleUnitModel target)
        {
            if (target == null)
            {
                return 1f;
            }

            try
            {
                if (target.IsBreakLifeZero() && !target.passiveDetail.DontChangeResistByBreak())
                {
                    return 2f;
                }
            }
            catch
            {
                // 防御性兜底，万一字段不存在不影响主路径
            }
            return 1f;
        }
    }

    /// <summary>
    /// 允许向乐章型骰子下发威力 / 伤害类加成的临时白名单。
    /// 默认情况下我们会拦截原版"强壮 / 忍耐 / 进攻型骰子威力提升"等效果，
    /// 但乐章专属来源（强音 buff、海愿斩等卡牌能力）必须能通过这条通道生效。
    ///
    /// 参考 ChristashaEgoBoss.cs 中的 PrimalTidePowerScope 实现。
    /// </summary>
    public static class MusicDicePowerScope
    {
        [ThreadStatic] private static int _depth;

        public static bool IsAllowanceActive => _depth > 0;

        public static void RunWithAllowance(Action action)
        {
            _depth++;
            try
            {
                action?.Invoke();
            }
            finally
            {
                if (_depth > 0)
                {
                    _depth--;
                }
            }
        }
    }

    // Music UI sprite factory: baked original artwork embedded in this assembly.
    internal static class MusicDiceSpriteFactory
    {
        private static Sprite _cardSprite;
        private static Sprite _glyphSprite;
        private static Sprite _blankFrameSprite;
        private static bool _cardResolved, _glyphResolved, _blankFrameResolved;

        public static Sprite GetCardSprite()
        {
            return GetCached(ref _cardSprite, ref _cardResolved,
                "Steria.VisualAssets.MusicDice.Card.png", "SteriaCleanMusicCard");
        }

        public static Sprite GetGlyphSprite()
        {
            return GetCached(ref _glyphSprite, ref _glyphResolved,
                "Steria.VisualAssets.MusicDice.Glyph.png", "SteriaOriginalMusicGlyph");
        }

        public static Sprite GetBlankFrameSprite()
        {
            return GetCached(ref _blankFrameSprite, ref _blankFrameResolved,
                "Steria.VisualAssets.MusicDice.BlankFrame.png", "SteriaCleanMusicFrame");
        }

        private static Sprite GetCached(ref Sprite sprite, ref bool resolved, string resourceName, string name)
        {
            if (resolved) return sprite;
            resolved = true;
            Texture2D texture = null;
            try
            {
                byte[] bytes;
                using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null) throw new InvalidOperationException("Missing embedded music artwork: " + resourceName);
                    using (var buffer = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(buffer);
                        bytes = buffer.ToArray();
                    }
                }
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, bytes, true))
                    throw new InvalidOperationException("Invalid embedded music artwork: " + resourceName);
                texture.name = name;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                sprite.name = name;
                return sprite;
            }
            catch (Exception ex)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                Debug.LogError("[Steria] Music artwork load failed: " + ex);
                // Never rasterize the expensive authoring geometry on a game UI path.
                return null;
            }
        }
    }

    public static class MusicDiceVisuals
    {
        public static readonly Color FaceColor = new Color32(143, 175, 196, 255);
        public static readonly Color EdgeColor = new Color32(201, 216, 225, 255);

        // State belongs only to explicitly changed Graphics. Never restore frame, material or enabled state.
        private sealed class GraphicState
        {
            public Graphic Graphic;
            public Sprite OriginalSprite, AppliedSprite;
            public Color OriginalColor, AppliedColor;
            public bool HasSprite, HasColor;
        }

        private sealed class MusicStyleState : MonoBehaviour
        {
            public readonly List<GraphicState> Graphics = new List<GraphicState>();
            public bool ActionIsMusic;
            public bool HasOriginColor;
            public Color OriginColor;
            public bool DamageIsMusic;
        }

        public static bool IsMusicUiBehaviour(DiceBehaviour behaviour)
        {
            return behaviour != null && behaviour.Type == BehaviourType.Atk
                && (behaviour.Detail == BehaviourDetail.Slash || behaviour.Detail == BehaviourDetail.Penetrate
                    || behaviour.Detail == BehaviourDetail.Hit);
        }

        private static MusicStyleState State(Component owner)
        {
            return owner.GetComponent<MusicStyleState>() ?? owner.gameObject.AddComponent<MusicStyleState>();
        }

        private static GraphicState Track(Component owner, Graphic graphic)
        {
            MusicStyleState state = State(owner);
            GraphicState entry = state.Graphics.Find(x => x.Graphic == graphic);
            if (entry == null)
            {
                entry = new GraphicState { Graphic = graphic };
                state.Graphics.Add(entry);
            }
            return entry;
        }

        private static void StyleIcon(Component owner, Image image, bool glyphOnly = false)
        {
            if (image == null || image.sprite == null) return;
            GraphicState entry = Track(owner, image);
            // A refresh must not consume another mod's sprite or compound our conversion.
            if (entry.HasSprite) return;
            entry.OriginalSprite = image.sprite;
            entry.AppliedSprite = glyphOnly ? MusicDiceSpriteFactory.GetGlyphSprite() : MusicDiceSpriteFactory.GetCardSprite();
            if (entry.AppliedSprite == null) return;
            entry.HasSprite = true;
            image.sprite = entry.AppliedSprite;
        }

        private static void StyleColor(Component owner, Graphic graphic, Color color)
        {
            if (graphic == null) return;
            GraphicState entry = Track(owner, graphic);
            if (entry.HasColor) return;
            entry.OriginalColor = graphic.color;
            color.a = graphic.color.a;
            entry.AppliedColor = color;
            entry.HasColor = true;
            graphic.color = color;
        }

        public static void RestoreBeforeBind(Component owner)
        {
            if (owner == null) return;
            MusicStyleState state = owner.GetComponent<MusicStyleState>();
            if (state == null) return;
            foreach (GraphicState entry in state.Graphics)
            {
                if (entry.Graphic == null) continue;
                Image image = entry.Graphic as Image;
                if (entry.HasSprite && image != null && image.sprite == entry.AppliedSprite)
                    image.sprite = entry.OriginalSprite;
                // Respect any later external color or alpha updates instead of restoring stale values.
                if (entry.HasColor && entry.Graphic.color == entry.AppliedColor)
                    entry.Graphic.color = entry.OriginalColor;
            }
            state.Graphics.Clear();
            BattleSimpleActionUI_Dice action = owner as BattleSimpleActionUI_Dice;
            if (action != null && state.HasOriginColor)
                AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "originColor").SetValue(action, state.OriginColor);
            state.HasOriginColor = false;
            state.ActionIsMusic = false;
            state.DamageIsMusic = false;
        }

        public static void RestoreCardBeforeBind(BattleDiceCardUI cardUi)
        {
            if (cardUi == null) return;
            RestoreBeforeBind(cardUi);
            if (cardUi.ui_behaviourDescList != null)
                foreach (BattleDiceCard_BehaviourDescUI desc in cardUi.ui_behaviourDescList)
                    RestoreBeforeBind(desc);
        }

        public static void PrepareAction(BattleSimpleActionUI_Dice diceUi, BattleDiceCardModel card, DiceBehaviour behaviour)
        {
            if (diceUi == null) return;
            RestoreBeforeBind(diceUi);
            State(diceUi).ActionIsMusic = MusicDiceSystem.IsMusicCard(card) && IsMusicUiBehaviour(behaviour);
        }

        // Called only after vanilla SetColors succeeded, including the three PrepareDice overloads.
        public static void ApplyOnActionDice(BattleSimpleActionUI_Dice diceUi)
        {
            if (diceUi == null) return;
            MusicStyleState state = diceUi.GetComponent<MusicStyleState>();
            if (state == null || !state.ActionIsMusic) return;
            StyleIcon(diceUi, diceUi.imgIcon, true);
            StyleIcon(diceUi, diceUi.imgDetailIcon_Center, true);
            StyleColor(diceUi, diceUi.img_diceFace, FaceColor);
            StyleColor(diceUi, diceUi.img_diceFaceClone, FaceColor);
            StyleColor(diceUi, diceUi.img_diceFaceLinearDodge, EdgeColor);
            StyleColor(diceUi, diceUi.img_diceFaceLinearDodgeClone, EdgeColor);
            StyleColor(diceUi, AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "img_ActionDefeatDestoryed")?.GetValue(diceUi) as Graphic, FaceColor);
            StyleColor(diceUi, AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "img_ActionDefeatDestoryedLinear")?.GetValue(diceUi) as Graphic, EdgeColor);
            StyleColor(diceUi, diceUi.txt_diceRange, FaceColor);
            StyleColor(diceUi, diceUi.img_hundredsPlace, FaceColor);
            StyleColor(diceUi, diceUi.img_tensPlace, FaceColor);
            StyleColor(diceUi, diceUi.img_unitsPlace, FaceColor);
            var field = AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "originColor");
            if (!state.HasOriginColor)
            {
                state.OriginColor = (Color)field.GetValue(diceUi);
                state.HasOriginColor = true;
            }
            field.SetValue(diceUi, FaceColor);
            // Do not call SetValueColor: vanilla owns Increase / Decrease and number HSV states.
        }

        public static bool IsMusicDamageText(DamageTextEffect effect)
        {
            return effect != null && effect.GetComponent<MusicStyleState>()?.DamageIsMusic == true;
        }

        public static void ApplyOnDamageText(DamageTextEffect effect)
        {
            if (effect == null) return;
            // Every existing layer uses the same original music glyph; vanilla retains layer and fade ownership.
            StyleIcon(effect, effect.img_resistIcon, true);
            StyleIcon(effect, effect.img_resistIconBg, true);
            StyleIcon(effect, effect.img_resistIconFg, true);
            State(effect).DamageIsMusic = true;
        }

        public static void ApplyOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null || !MusicDiceSystem.IsMusicCard(cardUi.CardModel)) return;
            if (cardUi.img_behaviourDetatilList != null)
                for (int i = 0; i < cardUi.img_behaviourDetatilList.Count; i++)
                    if (IsMusicUiBehaviour(MusicDiceSystem.TryGetDiceBehaviourAt(cardUi.CardModel, i)))
                        StyleIcon(cardUi, cardUi.img_behaviourDetatilList[i]);
        }

        public static void ApplyOnDescUI(BattleDiceCard_BehaviourDescUI desc, LorId cardId, DiceBehaviour behaviour)
        {
            if (desc == null || !MusicDiceSystem.IsMusicCard(cardId) || !IsMusicUiBehaviour(behaviour)) return;
            StyleIcon(desc, desc.img_detail);
            StyleColor(desc, desc.txt_range, FaceColor);
        }

        public static void ApplyOnOriginSlot(UI.UIOriginCardSlot slot, DiceCardItemModel cardModel)
        {
            if (slot == null || !MusicDiceSystem.IsMusicCard(cardModel)) return;
            Image[] icons = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_BehaviourIcons")?.GetValue(slot) as Image[];
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
                if (IsMusicUiBehaviour(MusicDiceSystem.TryGetDiceBehaviourAt(cardModel, i)))
                    StyleIcon(slot, icons[i]);
        }

        public static void ApplyOnDetailDescSlot(UI.UIDetailCardDescSlot desc, LorId cardId, DiceBehaviour behaviour)
        {
            if (desc == null || !MusicDiceSystem.IsMusicCard(cardId) || !IsMusicUiBehaviour(behaviour)) return;
            StyleIcon(desc, desc.img_detail);
            StyleColor(desc, desc.txt_range, FaceColor);
        }
    }

}
