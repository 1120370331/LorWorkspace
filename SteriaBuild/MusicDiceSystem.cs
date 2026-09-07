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

    // Native silhouettes and white strokes are preserved; only chromatic pixels change.
    internal static class MusicDiceSpriteFactory
    {
        private static Sprite _cardSprite;
        private static Sprite _glyphSprite;

        public static Sprite GetCardSprite()
        {
            if (_cardSprite != null) return _cardSprite;
            // A single fixed native frame, independent of the former attack detail.
            Sprite source = UI.UISpriteDataManager.instance?._cardBehaviourDetailIcons[(int)BehaviourDetail.Slash];
            if (source == null) return null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D texture = null;
            Texture2D readableAtlas = null;
            try
            {
                // Readable copies also work for the game's non-readable atlas textures.
                // Non-packed native icons contain the complete rect, including transparent margins.
                Rect area = source.packed ? source.textureRect : source.rect;
                Vector2 offset = source.packed ? source.textureRectOffset : Vector2.zero;
                int width = Mathf.RoundToInt(source.rect.width);
                int height = Mathf.RoundToInt(source.rect.height);
                target = RenderTexture.GetTemporary(source.texture.width, source.texture.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(source.texture, target);
                RenderTexture.active = target;
                // Read the complete target before cropping. Unity 2019 D3D sub-rect ReadPixels
                // can address a different atlas row when the destination texture is smaller.
                readableAtlas = new Texture2D(source.texture.width, source.texture.height, TextureFormat.RGBA32, false);
                readableAtlas.ReadPixels(new Rect(0, 0, source.texture.width, source.texture.height), 0, 0, false);
                int areaWidth = Mathf.RoundToInt(area.width);
                int areaHeight = Mathf.RoundToInt(area.height);
                Color[] region = readableAtlas.GetPixels(Mathf.RoundToInt(area.x), Mathf.RoundToInt(area.y), areaWidth, areaHeight);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels(new Color[width * height]);
                texture.SetPixels(Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y), areaWidth, areaHeight, region);
                Color[] pixels = texture.GetPixels();
                ClearBakedAttackCenter(pixels, width, height);
                float targetHue, targetSaturation, targetValue;
                Color.RGBToHSV(MusicDiceVisuals.FaceColor, out targetHue, out targetSaturation, out targetValue);
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color original = pixels[i];
                    float h, s, v;
                    Color.RGBToHSV(original, out h, out s, out v);
                    // Neutral ink, white strokes and their antialiased edges keep their luminance.
                    float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.35f, s));
                    Color recolored = Color.HSVToRGB(targetHue, targetSaturation, v * targetValue);
                    Color result = Color.Lerp(original, recolored, weight);
                    result.a = original.a;
                    pixels[i] = result;
                }
                CompositeGlyph(pixels, width, height);
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                texture.name = source.name + "_SteriaMusic";
                texture.filterMode = source.texture.filterMode;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite variant = Sprite.Create(texture, new Rect(0, 0, width, height),
                    new Vector2(source.pivot.x / width, source.pivot.y / height), source.pixelsPerUnit,
                    0, SpriteMeshType.FullRect, source.border);
                variant.name = texture.name;
                _cardSprite = variant;
                return variant;
            }
            catch (Exception ex)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                // Unsupported atlas layouts retain the native icon rather than inventing a fallback.
                Debug.LogError("[Steria] Native music icon conversion failed: " + ex);
                return source;
            }
            finally
            {
                RenderTexture.active = previous;
                if (readableAtlas != null) UnityEngine.Object.Destroy(readableAtlas);
                if (target != null) RenderTexture.ReleaseTemporary(target);
            }
        }
        // Original hand-drawn paths in a 100 x 100 design space. No font or resource glyph.
        public static Sprite GetGlyphSprite()
        {
            if (_glyphSprite != null) return _glyphSprite;
            const int size = 128;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int coverage = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (InGlyph((x + (sx + .5f) / 4f) * 100f / size,
                                100f - (y + (sy + .5f) / 4f) * 100f / size)) coverage++;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, coverage / 16f);
                }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "SteriaOriginalMusicGlyph";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            _glyphSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            _glyphSprite.name = texture.name;
            return _glyphSprite;
        }

        private static readonly Vector2[] LeftStem = { new Vector2(34, 78), new Vector2(36, 28),
            new Vector2(39, 24), new Vector2(46, 26), new Vector2(44, 77) };
        private static readonly Vector2[] RightStem = { new Vector2(77, 67), new Vector2(78, 17),
            new Vector2(82, 12), new Vector2(88, 13), new Vector2(87, 65) };
        private static readonly Vector2[] Beam = { new Vector2(38, 25), new Vector2(84, 11),
            new Vector2(88, 13), new Vector2(86, 24), new Vector2(39, 37) };

        private static bool InGlyph(float x, float y)
        {
            return InEllipse(x, y, 29f, 78f, 16f, 10.5f, -.35f)
                || InEllipse(x, y, 73f, 67f, 14.5f, 10f, -.26f)
                || InPolygon(x, y, LeftStem) || InPolygon(x, y, RightStem) || InPolygon(x, y, Beam);
        }

        private static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry, float angle)
        {
            float dx = x - cx, dy = y - cy;
            float u = dx * Mathf.Cos(angle) + dy * Mathf.Sin(angle);
            float v = -dx * Mathf.Sin(angle) + dy * Mathf.Cos(angle);
            return u * u / (rx * rx) + v * v / (ry * ry) <= 1f;
        }

        private static bool InPolygon(float x, float y, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > y) != (polygon[j].y > y)
                    && x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y)
                        / (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
            return inside;
        }

        private static void ClearBakedAttackCenter(Color[] pixels, int width, int height)
        {
            // Locate only the largest neutral white connected component inside the frame.
            bool[] candidate = new bool[pixels.Length];
            for (int y = (int)(height * .18f); y < height * .84f; y++)
                for (int x = (int)(width * .18f); x < width * .82f; x++)
                {
                    Color c = pixels[y * width + x];
                    float low = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    float high = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    candidate[y * width + x] = c.a > .5f && low > .65f && high - low < .24f;
                }
            List<int> largest = new List<int>();
            int[] directions = { -1, 1, -width, width };
            for (int i = 0; i < candidate.Length; i++)
            {
                if (!candidate[i]) continue;
                List<int> component = new List<int> { i };
                candidate[i] = false;
                for (int n = 0; n < component.Count; n++)
                    foreach (int direction in directions)
                    {
                        int next = component[n] + direction;
                        if (next >= 0 && next < candidate.Length && candidate[next])
                        { candidate[next] = false; component.Add(next); }
                    }
                if (component.Count > largest.Count) largest = component;
            }
            if (largest.Count < width * height / 100)
                throw new InvalidOperationException("Native Slash center could not be isolated.");
            bool[] mask = new bool[pixels.Length];
            foreach (int index in largest)
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int x = index % width + dx, y = index / width + dy;
                        if (x > width * .18f && x < width * .82f && y > height * .18f && y < height * .84f)
                            mask[y * width + x] = true;
                    }
            // Propagate surrounding colored ground inward, then relax only the removed center.
            bool[] unresolved = (bool[])mask.Clone();
            List<int> frontier = new List<int>();
            for (int i = 0; i < mask.Length; i++)
                if (mask[i]) foreach (int direction in directions)
                    if (!mask[i + direction]) { frontier.Add(i); break; }
            for (int n = 0; n < frontier.Count; n++)
            {
                int i = frontier[n];
                if (!unresolved[i]) continue;
                Color sum = Color.clear; int count = 0;
                foreach (int direction in directions)
                    if (!unresolved[i + direction]) { sum += pixels[i + direction]; count++; }
                if (count == 0) continue;
                pixels[i] = sum / count; unresolved[i] = false;
                foreach (int direction in directions)
                    if (unresolved[i + direction]) frontier.Add(i + direction);
            }
            Color[] nextPixels = (Color[])pixels.Clone();
            for (int pass = 0; pass < 120; pass++)
            {
                for (int i = 0; i < mask.Length; i++)
                    if (mask[i]) nextPixels[i] = (pixels[i - 1] + pixels[i + 1] + pixels[i - width] + pixels[i + width]) / 4f;
                for (int i = 0; i < mask.Length; i++) if (mask[i]) pixels[i] = nextPixels[i];
            }
        }

        private static void CompositeGlyph(Color[] pixels, int width, int height)
        {
            Texture2D glyph = GetGlyphSprite().texture;
            // At 24px card size the occupied music center is about 13px high, with sturdy stems.
            float size = Mathf.Min(width, height) * .50f;
            float left = (width - size) * .5f - width * .01f, bottom = (height - size) * .5f - height * .015f;
            for (int y = Mathf.CeilToInt(bottom); y < bottom + size; y++)
                for (int x = Mathf.CeilToInt(left); x < left + size; x++)
                {
                    Color ink = glyph.GetPixelBilinear((x + .5f - left) / size, (y + .5f - bottom) / size);
                    int i = y * width + x;
                    Color ground = pixels[i];
                    float alpha = ink.a + ground.a * (1f - ink.a);
                    if (alpha <= 0f) continue;
                    Color blended = (ink * ink.a + ground * ground.a * (1f - ink.a)) / alpha;
                    blended.a = alpha;
                    pixels[i] = blended;
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
