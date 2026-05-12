using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            DiceCardXmlInfo xml = cardModel?.XmlData;
            IList<DiceBehaviour> list = xml?.DiceBehaviourList;
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        /// <summary>卡组 / 卡面条 UI 使用的 DiceCardItemModel（ClassInfo 等同 Xml）。</summary>
        public static DiceBehaviour TryGetDiceBehaviourAt(DiceCardItemModel cardModel, int index)
        {
            DiceCardXmlInfo xml = cardModel?.ClassInfo;
            IList<DiceBehaviour> list = xml?.DiceBehaviourList;
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

    // ===== UI 视觉：蓝白渐变 =====

    /// <summary>
    /// 程序化生成（或从 Resource/ArtWork 加载）乐章型骰子的 UI 贴图：
    ///   - "music_dice_die_icon.png"（可选）：整张自定义骰面图（书页描述行专用）。
    ///     若存在则只用这一层，并隐藏原版斩击/打击/突刺线稿，避免叠两层。
    ///   - "music_dice_face.png"：放在图标后面的蓝色渐变骰子面板（无 die_icon 时使用）
    ///   - "music_dice_glow.png"：覆盖在面板上、骰子外缘亮起的软白光
    /// 如果硬盘上找到同名 PNG，则优先使用 PNG；face/glow 找不到则生成默认的程序贴图。
    /// 后续手绘 PNG 替换不需要重新编译。
    /// </summary>
    internal static class MusicDiceSpriteFactory
    {
        private const string FaceFileName = "music_dice_face";
        private const string GlowFileName = "music_dice_glow";
        private const string DieIconFileName = "music_dice_die_icon";

        private static Sprite _faceSprite;
        private static Sprite _glowSprite;
        private static Sprite _dieIconSprite;
        private static bool _dieIconResolved;
        private static bool _dieIconLoadLogged;
        private static string _artworkPath;

        public static Sprite GetFaceSprite()
        {
            if (_faceSprite == null)
            {
                _faceSprite = TryLoadPng(FaceFileName) ?? BuildProceduralFace();
            }
            return _faceSprite;
        }

        public static Sprite GetGlowSprite()
        {
            if (_glowSprite == null)
            {
                _glowSprite = TryLoadPng(GlowFileName) ?? BuildProceduralGlow();
            }
            return _glowSprite;
        }

        /// <summary>
        /// 可选整张骰面 PNG（无程序兜底）。存在时书页 UI 仅此一层 + 隐藏原版行为图标。
        /// </summary>
        public static Sprite GetDieIconSprite()
        {
            if (!_dieIconResolved)
            {
                _dieIconResolved = true;
                string dir = GetArtworkPath();
                string path = Path.Combine(dir, DieIconFileName + ".png");
                _dieIconSprite = TryLoadPng(DieIconFileName);
                if (!_dieIconLoadLogged)
                {
                    _dieIconLoadLogged = true;
                    if (_dieIconSprite == null)
                    {
                        SteriaLogger.Log($"MusicDice: die icon NOT loaded. Tried={path}, exists={File.Exists(path)}, artworkDir={dir}, dirExists={Directory.Exists(dir)}");
                    }
                    else
                    {
                        SteriaLogger.Log($"MusicDice: die icon loaded OK ({path})");
                    }
                }
            }

            return _dieIconSprite;
        }

        private static Sprite TryLoadPng(string baseName)
        {
            try
            {
                string path = Path.Combine(GetArtworkPath(), baseName + ".png");
                if (!File.Exists(path))
                {
                    return null;
                }

                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                if (!ImageConversion.LoadImage(tex, data))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                tex.name = baseName;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] MusicDiceSpriteFactory.TryLoadPng({baseName}) error: {ex}");
                return null;
            }
        }

        private static string GetArtworkPath()
        {
            if (_artworkPath != null)
            {
                return _artworkPath;
            }

            try
            {
                // 与 SteriaLogger 一致：优先 Assembly.Location（Windows 下 CodeBase 可能带 “/C:/” 前缀）
                string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dllDir))
                {
                    dllDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                }

                string modRoot = Directory.GetParent(dllDir)?.FullName ?? dllDir;
                string candidate = Path.Combine(modRoot, "Resource", "ArtWork");
                if (!Directory.Exists(candidate))
                {
                    string alt = Path.Combine(dllDir, "Resource", "ArtWork");
                    if (Directory.Exists(alt))
                    {
                        candidate = alt;
                    }
                }

                _artworkPath = candidate;
            }
            catch
            {
                _artworkPath = string.Empty;
            }

            return _artworkPath;
        }

        /// <summary>
        /// 程序生成的蓝色骰子面板：垂直蓝白渐变（顶部浅蓝、底部深蓝），周边圆角软化。
        /// </summary>
        private static Sprite BuildProceduralFace()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "music_dice_face_procedural";

            Color top = new Color(0.62f, 0.86f, 1.00f, 1f);   // 浅蓝白
            Color bottom = new Color(0.18f, 0.36f, 0.78f, 1f); // 深蓝

            // 圆角软化所用半径
            const float cornerRadius = 10f;
            for (int y = 0; y < size; y++)
            {
                float t = (float)y / (size - 1);
                Color baseColor = Color.Lerp(bottom, top, t);
                for (int x = 0; x < size; x++)
                {
                    float alpha = ComputeRoundedRectAlpha(x, y, size, size, cornerRadius);
                    tex.SetPixel(x, y, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 程序生成的白色软光：径向渐变，中心透明、外缘高亮，模仿 linearDodge 的"骰子白边"。
        /// </summary>
        private static Sprite BuildProceduralGlow()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "music_dice_glow_procedural";

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = size * 0.5f;
            const float cornerRadius = 10f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    // 外缘亮，靠近中心 t 减弱
                    float t = Mathf.Clamp01(d / maxDist);
                    // 边缘部分（最外 30%）亮度抬升
                    float edge = Mathf.Clamp01((t - 0.7f) / 0.3f);
                    edge = edge * edge;
                    float rectAlpha = ComputeRoundedRectAlpha(x, y, size, size, cornerRadius);
                    float alpha = edge * 0.85f * rectAlpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static float ComputeRoundedRectAlpha(int px, int py, int w, int h, float radius)
        {
            float left = radius;
            float right = w - radius - 1;
            float bottom = radius;
            float top = h - radius - 1;

            float cx = Mathf.Clamp(px, left, right);
            float cy = Mathf.Clamp(py, bottom, top);
            float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
            if (dist <= radius - 1f)
            {
                return 1f;
            }
            if (dist >= radius)
            {
                return 0f;
            }
            return 1f - (dist - (radius - 1f));
        }
    }

    public static class MusicDiceVisuals
    {
        private sealed class SteriaMusicDiceStyleTag : MonoBehaviour
        {
        }

        /// <summary>标记：因自定义整张骰面图而暂时隐藏了 img_detail。</summary>
        private sealed class SteriaMusicDiceSuppressedVanillaDetailTag : MonoBehaviour
        {
        }

        /// <summary>整张骰面模式下暂时关掉 linearDodge 高光层。</summary>
        private sealed class SteriaMusicDiceSuppressedLinearDodgeTag : MonoBehaviour
        {
            public bool WasEnabled = true;
        }

        // 蓝白渐变：面板使用淡蓝，边缘使用白色亮边，整体呈现冷色调白蓝感
        private static readonly Color MusicDiceFaceColor = new Color(0.48f, 0.74f, 1f, 1f);
        private static readonly Color MusicDiceEdgeColor = Color.white;
        private static readonly Color MusicDiceTextColor = Color.white;
        // 给原版小图标使用的轻微调色（仅在没贴自定义面板的回退场景里用）
        private static readonly Color MusicDiceIconTint = new Color(0.62f, 0.86f, 1f, 1f);
        // 书页 face 节点的相对放大倍率（相对原图标尺寸），用来让蓝色骰子面板比图标稍大
        private const float BookFaceScale = 1.35f;
        private const float BookGlowScale = 1.5f;
        private const float BookDieIconScale = 1.18f * 0.7f;

        public static void ApplyOnActionDice(BattleSimpleActionUI_Dice diceUi)
        {
            if (diceUi == null)
            {
                return;
            }

            if (diceUi.img_diceFace != null)
            {
                diceUi.img_diceFace.color = MusicDiceFaceColor;
            }
            if (diceUi.img_diceFaceLinearDodge != null)
            {
                diceUi.img_diceFaceLinearDodge.color = MusicDiceEdgeColor;
            }
            if (diceUi.img_diceFaceClone != null)
            {
                diceUi.img_diceFaceClone.color = MusicDiceFaceColor;
            }
            if (diceUi.img_diceFaceLinearDodgeClone != null)
            {
                diceUi.img_diceFaceLinearDodgeClone.color = MusicDiceEdgeColor;
            }

            AccessTools.Field(typeof(BattleSimpleActionUI_Dice), "originColor")?.SetValue(diceUi, MusicDiceFaceColor);
            diceUi.SetValueColor(BattleDiceValueColor.Normal);
        }

        public static void ApplyOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            bool isMusic = MusicDiceSystem.IsMusicCard(cardUi.CardModel);
            if (!isMusic)
            {
                ClearOnCardUI(cardUi);
                return;
            }

            try
            {
                if (cardUi.ui_behaviourDescList != null)
                {
                    for (int i = 0; i < cardUi.ui_behaviourDescList.Count; i++)
                    {
                        BattleDiceCard_BehaviourDescUI desc = cardUi.ui_behaviourDescList[i];
                        DiceBehaviour bh = MusicDiceSystem.TryGetDiceBehaviourAt(cardUi.CardModel, i);
                        LorId cid = cardUi.CardModel?.XmlData?.id ?? default;
                        ApplyOnDescUI(desc, cid, bh);
                    }
                }

                ApplyBattleDiceCardFaceStrip(cardUi);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Steria] Music dice card UI style error: {ex}");
            }
        }

        /// <summary>SetCard 等路径：由 CardModel 按行解析 DiceBehaviour。</summary>
        public static void ApplyOnDescUI(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc == null)
            {
                return;
            }

            BattleDiceCardUI cardUi = desc.GetComponentInParent<BattleDiceCardUI>();
            int idx = cardUi?.ui_behaviourDescList?.IndexOf(desc) ?? -1;
            DiceBehaviour bh = idx >= 0 ? MusicDiceSystem.TryGetDiceBehaviourAt(cardUi?.CardModel, idx) : null;
            LorId cid = cardUi?.CardModel?.XmlData?.id ?? default;
            ApplyOnDescUI(desc, cid, bh);
        }

        /// <summary>SetBehaviourInfo Postfix：带有可靠的 LorId / DiceBehaviour，不依赖 CardModel 时机。</summary>
        public static void ApplyOnDescUI(BattleDiceCard_BehaviourDescUI desc, LorId cardId, DiceBehaviour behaviour)
        {
            if (desc == null)
            {
                return;
            }

            BattleDiceCardUI cardUi = desc.GetComponentInParent<BattleDiceCardUI>();

            bool musicCard = MusicDiceSystem.IsMusicCard(cardUi?.CardModel) || MusicDiceSystem.IsMusicCard(cardId);
            if (!musicCard)
            {
                ClearOnDescUI(desc);
                return;
            }

            DiceBehaviour rowBehaviour = behaviour;
            if (rowBehaviour == null && cardUi != null)
            {
                int idx = cardUi.ui_behaviourDescList?.IndexOf(desc) ?? -1;
                if (idx >= 0)
                {
                    rowBehaviour = MusicDiceSystem.TryGetDiceBehaviourAt(cardUi.CardModel, idx);
                }
            }

            if (!MusicDiceSystem.IsMusicAttackDiceBehaviour(rowBehaviour))
            {
                ClearOnDescUI(desc);
                return;
            }

            if (!IsMusicDescriptor(desc))
            {
                ClearOnDescUI(desc);
                return;
            }

            if (desc.img_detail != null)
            {
                EnsureBookDiceBackdrop(desc.img_detail);
                if (desc.img_detail.enabled)
                {
                    desc.img_detail.color = Color.white;
                }
            }

            if (desc.txt_ability != null)
            {
                desc.txt_ability.color = MusicDiceTextColor;
            }

            if (desc.txt_range != null)
            {
                desc.txt_range.color = MusicDiceTextColor;
            }
        }

        public static void ApplyOnOriginSlot(UI.UIOriginCardSlot slot, DiceCardItemModel cardModel)
        {
            if (slot == null)
            {
                return;
            }

            if (!MusicDiceSystem.IsMusicCard(cardModel))
            {
                ClearOnTransform(slot.transform);
                return;
            }

            Image[] behaviourIcons = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_BehaviourIcons")
                ?.GetValue(slot) as Image[];
            Image[] linearDodge = AccessTools.Field(typeof(UI.UIOriginCardSlot), "img_linearDodge")
                ?.GetValue(slot) as Image[];

            if (behaviourIcons == null)
            {
                return;
            }

            bool useDieArt = MusicDiceSpriteFactory.GetDieIconSprite() != null;

            for (int i = 0; i < behaviourIcons.Length; i++)
            {
                Image img = behaviourIcons[i];
                if (img == null)
                {
                    continue;
                }

                DiceBehaviour bh = MusicDiceSystem.TryGetDiceBehaviourAt(cardModel, i);
                Image lin = linearDodge != null && i < linearDodge.Length ? linearDodge[i] : null;

                if (!MusicDiceSystem.IsMusicAttackDiceBehaviour(bh))
                {
                    ClearSingleBehaviourIconSlot(img, lin);
                    continue;
                }

                if (useDieArt)
                {
                    EnsureBookDiceBackdrop(img);
                    if (img.enabled)
                    {
                        img.color = Color.white;
                    }

                    SuppressLinearDodge(lin);
                }
                else
                {
                    RestoreLinearDodgeIfNeeded(lin);
                    EnsureBookDiceBackdrop(img);
                    ApplyBlueTintIcon(img);
                    if (lin != null)
                    {
                        lin.color = MusicDiceEdgeColor;
                        if (lin.GetComponent<SteriaMusicDiceStyleTag>() == null)
                        {
                            lin.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
                        }
                    }
                }
            }
        }

        public static void ApplyOnDetailDescSlot(UI.UIDetailCardDescSlot desc, LorId cardId, DiceBehaviour behaviour)
        {
            if (desc == null)
            {
                return;
            }

            if (!MusicDiceSystem.IsMusicCard(cardId))
            {
                ClearDetailDescSlot(desc);
                return;
            }

            if (!MusicDiceSystem.IsMusicAttackDiceBehaviour(behaviour))
            {
                ClearDetailDescSlot(desc);
                return;
            }

            if (desc.img_detail != null)
            {
                EnsureBookDiceBackdrop(desc.img_detail);
                if (desc.img_detail.enabled)
                {
                    desc.img_detail.color = Color.white;
                }
            }
        }

        private static void ClearDetailDescSlot(UI.UIDetailCardDescSlot desc)
        {
            if (desc == null)
            {
                return;
            }

            DestroyBackdropChildrenIfExists(desc.transform);
            RestoreVanillaDetailIfNeededOnImage(desc.img_detail);
            RestoreSuppressedLinearDodgesUnder(desc.transform);
            ClearTagsUnder(desc.transform);
        }

        private static bool IsMusicDescriptor(BattleDiceCard_BehaviourDescUI desc)
        {
            // 字段都是私有的，我们只能通过 sprite 名 + 字段位置来推断；
            // 简单起见，按描述文字开头识别（DiceBehaviour.Detail 文本里通常含 Slash/Hit/Penetrate 字样）。
            // 这里直接对全部 desc 应用蓝色背景影响不大，因此返回 true 保持简单。
            return true;
        }

        private static void ApplyBlueTintIcon(Image img)
        {
            if (img == null)
            {
                return;
            }

            // 直接把图标染成淡蓝；HSV 仅做轻微提亮，避免覆盖出整块色块
            img.color = MusicDiceIconTint;
            RefineHsv hsv = img.GetComponent<RefineHsv>();
            if (hsv == null)
            {
                hsv = img.gameObject.AddComponent<RefineHsv>();
            }
            if (img.GetComponent<SteriaMusicDiceStyleTag>() == null)
            {
                img.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
            }

            hsv.ActiveChange = true;
            hsv._HueShift = 0f;
            hsv._Saturation = 1f;
            hsv._ValueBrightness = 1.15f;
            hsv.CallUpdate();
        }

        private static string IconBackdropSuffix(Image icon)
        {
            return "_" + icon.GetInstanceID();
        }

        private static bool TryGetBattleDiceCardBehaviourIconArrays(BattleDiceCardUI cardUi, out Image[] icons, out Image[] linearDodge)
        {
            icons = AccessTools.Field(typeof(BattleDiceCardUI), "img_BehaviourIcons")?.GetValue(cardUi) as Image[];
            linearDodge = AccessTools.Field(typeof(BattleDiceCardUI), "img_linearDodge")?.GetValue(cardUi) as Image[];

            if (icons != null && icons.Length > 0)
            {
                return true;
            }

            foreach (FieldInfo fi in typeof(BattleDiceCardUI).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (fi.FieldType != typeof(Image[]))
                {
                    continue;
                }

                string n = fi.Name;
                if (!n.StartsWith("img_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (n.IndexOf("behaviour", StringComparison.OrdinalIgnoreCase) < 0 && n.IndexOf("Behaviour", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (n.IndexOf("Desc", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                icons = fi.GetValue(cardUi) as Image[];
                if (icons != null && icons.Length > 0)
                {
                    break;
                }
            }

            if (linearDodge == null)
            {
                linearDodge = AccessTools.Field(typeof(BattleDiceCardUI), "img_linearDodge")?.GetValue(cardUi) as Image[];
            }

            return icons != null && icons.Length > 0;
        }

        private static void ApplyBattleDiceCardFaceStrip(BattleDiceCardUI cardUi)
        {
            if (cardUi?.CardModel == null || !MusicDiceSystem.IsMusicCard(cardUi.CardModel))
            {
                ClearBattleDiceCardFaceStrip(cardUi);
                return;
            }

            if (!TryGetBattleDiceCardBehaviourIconArrays(cardUi, out Image[] icons, out Image[] linearDodge))
            {
                return;
            }

            BattleDiceCardModel model = cardUi.CardModel;
            bool useDieArt = MusicDiceSpriteFactory.GetDieIconSprite() != null;

            for (int i = 0; i < icons.Length; i++)
            {
                Image img = icons[i];
                if (img == null)
                {
                    continue;
                }

                DiceBehaviour bh = MusicDiceSystem.TryGetDiceBehaviourAt(model, i);
                Image lin = linearDodge != null && i < linearDodge.Length ? linearDodge[i] : null;

                if (!MusicDiceSystem.IsMusicAttackDiceBehaviour(bh))
                {
                    ClearSingleBehaviourIconSlot(img, lin);
                    continue;
                }

                if (useDieArt)
                {
                    EnsureBookDiceBackdrop(img);
                    if (img.enabled)
                    {
                        img.color = Color.white;
                    }

                    SuppressLinearDodge(lin);
                }
                else
                {
                    RestoreLinearDodgeIfNeeded(lin);
                    EnsureBookDiceBackdrop(img);
                    ApplyBlueTintIcon(img);
                    if (lin != null)
                    {
                        lin.color = MusicDiceEdgeColor;
                        if (lin.GetComponent<SteriaMusicDiceStyleTag>() == null)
                        {
                            lin.gameObject.AddComponent<SteriaMusicDiceStyleTag>();
                        }
                    }
                }
            }
        }

        private static void ClearBattleDiceCardFaceStrip(BattleDiceCardUI cardUi)
        {
            if (cardUi == null || !TryGetBattleDiceCardBehaviourIconArrays(cardUi, out Image[] icons, out Image[] linearDodge))
            {
                return;
            }

            for (int i = 0; i < icons.Length; i++)
            {
                Image lin = linearDodge != null && i < linearDodge.Length ? linearDodge[i] : null;
                ClearSingleBehaviourIconSlot(icons[i], lin);
            }
        }

        private static void ClearSingleBehaviourIconSlot(Image behaviourIcon, Image linearDodge)
        {
            DestroyBackdropForBehaviourIcon(behaviourIcon);
            RestoreVanillaDetailIfNeededOnImage(behaviourIcon);
            RestoreLinearDodgeIfNeeded(linearDodge);
            RemoveBlueTintFromBehaviourIcon(behaviourIcon);

            if (linearDodge != null)
            {
                SteriaMusicDiceStyleTag lt = linearDodge.GetComponent<SteriaMusicDiceStyleTag>();
                if (lt != null)
                {
                    linearDodge.color = Color.white;
                    UnityEngine.Object.Destroy(lt);
                }
            }
        }

        private static void RemoveBlueTintFromBehaviourIcon(Image img)
        {
            if (img == null)
            {
                return;
            }

            SteriaMusicDiceStyleTag tag = img.GetComponent<SteriaMusicDiceStyleTag>();
            if (tag == null)
            {
                return;
            }

            RefineHsv hsv = img.GetComponent<RefineHsv>();
            if (hsv != null)
            {
                hsv.ActiveChange = true;
                hsv._HueShift = 0f;
                hsv._Saturation = 1f;
                hsv._ValueBrightness = 1f;
                hsv.CallUpdate();
            }

            img.color = Color.white;
            UnityEngine.Object.Destroy(tag);
        }

        private static void SuppressLinearDodge(Image dodge)
        {
            if (dodge == null)
            {
                return;
            }

            SteriaMusicDiceSuppressedLinearDodgeTag tag = dodge.GetComponent<SteriaMusicDiceSuppressedLinearDodgeTag>();
            if (tag == null)
            {
                tag = dodge.gameObject.AddComponent<SteriaMusicDiceSuppressedLinearDodgeTag>();
                tag.WasEnabled = dodge.enabled;
            }

            dodge.enabled = false;
        }

        private static void RestoreLinearDodgeIfNeeded(Image dodge)
        {
            if (dodge == null)
            {
                return;
            }

            SteriaMusicDiceSuppressedLinearDodgeTag tag = dodge.GetComponent<SteriaMusicDiceSuppressedLinearDodgeTag>();
            if (tag != null)
            {
                dodge.enabled = tag.WasEnabled;
                UnityEngine.Object.Destroy(tag);
            }
        }

        private static void DestroyBackdropForBehaviourIcon(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            Transform parent = icon.transform.parent;
            if (parent == null)
            {
                return;
            }

            string sfx = IconBackdropSuffix(icon);
            DestroySteriaBackdropTriple(parent, sfx);
            DestroySteriaBackdropTriple(parent, string.Empty);
        }

        private static void DestroySteriaBackdropTriple(Transform parent, string sfx)
        {
            DestroyChildIfExists(parent, "SteriaMusicDiceDieIcon" + sfx);
            DestroyChildIfExists(parent, "SteriaMusicDiceFace" + sfx);
            DestroyChildIfExists(parent, "SteriaMusicDiceGlow" + sfx);
        }

        private static void DestroySteriaFaceGlowLayersUnderParent(Transform parent, string sfx)
        {
            DestroyChildIfExists(parent, "SteriaMusicDiceFace" + sfx);
            DestroyChildIfExists(parent, "SteriaMusicDiceGlow" + sfx);
            if (!string.IsNullOrEmpty(sfx))
            {
                DestroyChildIfExists(parent, "SteriaMusicDiceFace");
                DestroyChildIfExists(parent, "SteriaMusicDiceGlow");
            }
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return;
            }

            Transform t = parent.Find(childName);
            if (t != null)
            {
                UnityEngine.Object.Destroy(t.gameObject);
            }
        }

        private static void RestoreSuppressedLinearDodgesUnder(Transform root)
        {
            if (root == null)
            {
                return;
            }

            SteriaMusicDiceSuppressedLinearDodgeTag[] tags = root.GetComponentsInChildren<SteriaMusicDiceSuppressedLinearDodgeTag>(true);
            foreach (SteriaMusicDiceSuppressedLinearDodgeTag tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                Image dodge = tag.GetComponent<Image>();
                if (dodge != null)
                {
                    dodge.enabled = tag.WasEnabled;
                }

                UnityEngine.Object.Destroy(tag);
            }
        }

        /// <summary>
        /// 书页骰子视觉：
        /// - 若存在 music_dice_die_icon.png：仅铺一层整张骰面，并隐藏原版行为线稿图标；
        /// - 否则：蓝色面板 + 软光 + 白色线稿图标（face / glow）。
        /// </summary>
        private static void EnsureBookDiceBackdrop(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            Transform parent = icon.transform.parent;
            if (parent == null)
            {
                return;
            }

            string sfx = IconBackdropSuffix(icon);
            Sprite die = MusicDiceSpriteFactory.GetDieIconSprite();
            if (die != null)
            {
                DestroySteriaFaceGlowLayersUnderParent(parent, sfx);
                Transform existingDie = parent.Find("SteriaMusicDiceDieIcon" + sfx);
                if (existingDie != null)
                {
                    SuppressVanillaDetailIcon(icon);
                    return;
                }

                GameObject go = new GameObject("SteriaMusicDiceDieIcon" + sfx);
                go.transform.SetParent(parent, false);
                Image dieImg = go.AddComponent<Image>();
                dieImg.sprite = die;
                dieImg.color = Color.white;
                dieImg.raycastTarget = false;
                dieImg.type = Image.Type.Simple;
                dieImg.preserveAspect = true;
                CopyIconRect(go.GetComponent<RectTransform>(), icon.rectTransform, BookDieIconScale);
                go.AddComponent<SteriaMusicDiceStyleTag>();
                go.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());

                SuppressVanillaDetailIcon(icon);
                return;
            }

            Transform legacyDie = parent.Find("SteriaMusicDiceDieIcon" + sfx);
            if (legacyDie == null)
            {
                legacyDie = parent.Find("SteriaMusicDiceDieIcon");
            }

            if (legacyDie != null)
            {
                UnityEngine.Object.Destroy(legacyDie.gameObject);
            }

            RestoreVanillaDetailIfNeededOnImage(icon);

            if (parent.Find("SteriaMusicDiceFace" + sfx) != null)
            {
                return;
            }

            RectTransform iconRect = icon.rectTransform;

            GameObject face = new GameObject("SteriaMusicDiceFace" + sfx);
            face.transform.SetParent(parent, false);
            Image faceImg = face.AddComponent<Image>();
            faceImg.sprite = MusicDiceSpriteFactory.GetFaceSprite();
            faceImg.color = Color.white;
            faceImg.raycastTarget = false;
            faceImg.type = Image.Type.Simple;
            faceImg.preserveAspect = false;
            CopyIconRect(face.GetComponent<RectTransform>(), iconRect, BookFaceScale);
            face.AddComponent<SteriaMusicDiceStyleTag>();
            face.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());

            GameObject glow = new GameObject("SteriaMusicDiceGlow" + sfx);
            glow.transform.SetParent(parent, false);
            Image glowImg = glow.AddComponent<Image>();
            glowImg.sprite = MusicDiceSpriteFactory.GetGlowSprite();
            glowImg.color = Color.white;
            glowImg.raycastTarget = false;
            glowImg.type = Image.Type.Simple;
            glowImg.preserveAspect = false;
            CopyIconRect(glow.GetComponent<RectTransform>(), iconRect, BookGlowScale);
            glow.AddComponent<SteriaMusicDiceStyleTag>();
            glow.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());
        }

        private static void SuppressVanillaDetailIcon(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            icon.enabled = false;
            if (icon.GetComponent<SteriaMusicDiceSuppressedVanillaDetailTag>() == null)
            {
                icon.gameObject.AddComponent<SteriaMusicDiceSuppressedVanillaDetailTag>();
            }
        }

        private static void RestoreVanillaDetailIfNeededOnImage(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            SteriaMusicDiceSuppressedVanillaDetailTag tag = icon.GetComponent<SteriaMusicDiceSuppressedVanillaDetailTag>();
            if (tag != null)
            {
                icon.enabled = true;
                UnityEngine.Object.Destroy(tag);
            }
        }

        private static void RestoreVanillaDetailIfNeeded(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc?.img_detail != null)
            {
                RestoreVanillaDetailIfNeededOnImage(desc.img_detail);
            }
        }

        /// <summary>
        /// 把 src 的 RectTransform 锚定 / 位移 / 旋转复制到 dst，并把 sizeDelta 按 scale 放大。
        /// 若 src 是用 anchorMin/Max 撑满父对象（sizeDelta=0），改用 rect.size 推断真实大小。
        /// </summary>
        private static void CopyIconRect(RectTransform dst, RectTransform src, float scale)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;

            Vector2 size = src.sizeDelta;
            if (Mathf.Approximately(size.x, 0f) && Mathf.Approximately(size.y, 0f))
            {
                Rect r = src.rect;
                size = new Vector2(r.width, r.height);
            }
            dst.sizeDelta = size * scale;
            dst.localScale = src.localScale;
            dst.localRotation = src.localRotation;
        }

        private static void ClearOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            ClearBattleDiceCardFaceStrip(cardUi);

            if (cardUi.ui_behaviourDescList != null)
            {
                foreach (BattleDiceCard_BehaviourDescUI desc in cardUi.ui_behaviourDescList)
                {
                    ClearOnDescUI(desc);
                }
            }

            ClearTagsUnder(cardUi.transform);
        }

        private static void ClearOnDescUI(BattleDiceCard_BehaviourDescUI desc)
        {
            if (desc == null)
            {
                return;
            }

            // 兼容旧版的深蓝背景板残留
            Transform bg = desc.transform.Find("SteriaMusicDiceBg");
            if (bg != null)
            {
                UnityEngine.Object.Destroy(bg.gameObject);
            }

            DestroyBackdropChildrenIfExists(desc.transform);
            RestoreVanillaDetailIfNeeded(desc);
            ClearTagsUnder(desc.transform);
        }

        /// <summary>
        /// 递归销毁我们额外添加的所有装饰 GameObject：
        /// 旧版的 SteriaMusicDiceHalo / 新版 SteriaMusicDiceFace, SteriaMusicDiceGlow 都包含在内。
        /// </summary>
        private static void DestroyBackdropChildrenIfExists(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t == null)
                {
                    continue;
                }
                string n = t.name;
                if (n.StartsWith("SteriaMusicDiceDieIcon", StringComparison.Ordinal)
                    || n.StartsWith("SteriaMusicDiceFace", StringComparison.Ordinal)
                    || n.StartsWith("SteriaMusicDiceGlow", StringComparison.Ordinal)
                    || n == "SteriaMusicDiceHalo")
                {
                    UnityEngine.Object.Destroy(t.gameObject);
                }
            }
        }

        private static void ClearOnTransform(Transform root)
        {
            if (root == null)
            {
                return;
            }

            DestroyBackdropChildrenIfExists(root);
            RestoreSuppressedVanillaIconsUnder(root);
            RestoreSuppressedLinearDodgesUnder(root);
            ClearTagsUnder(root);
        }

        private static void RestoreSuppressedVanillaIconsUnder(Transform root)
        {
            if (root == null)
            {
                return;
            }

            SteriaMusicDiceSuppressedVanillaDetailTag[] suppressed = root.GetComponentsInChildren<SteriaMusicDiceSuppressedVanillaDetailTag>(true);
            foreach (SteriaMusicDiceSuppressedVanillaDetailTag s in suppressed)
            {
                if (s == null)
                {
                    continue;
                }

                Image img = s.GetComponent<Image>();
                RestoreVanillaDetailIfNeededOnImage(img);
            }
        }

        private static void ClearTagsUnder(Transform root)
        {
            SteriaMusicDiceStyleTag[] tags = root.GetComponentsInChildren<SteriaMusicDiceStyleTag>(true);
            foreach (SteriaMusicDiceStyleTag tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                RefineHsv hsv = tag.GetComponent<RefineHsv>();
                if (hsv != null)
                {
                    hsv._HueShift = 0f;
                    hsv._Saturation = 1f;
                    hsv._ValueBrightness = 1f;
                    hsv.CallUpdate();
                }

                Image img = tag.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                }
                RawImage raw = tag.GetComponent<RawImage>();
                if (raw != null)
                {
                    raw.color = Color.white;
                }

                UnityEngine.Object.Destroy(tag);
            }
        }
    }
}
