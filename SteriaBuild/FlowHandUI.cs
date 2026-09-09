using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LOR_DiceSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Steria
{
    // Faceted translucent circular prism, drawn as UI geometry; the center uses the existing Flow icon.
    public sealed class FlowPrismGraphic : MaskableGraphic
    {
        public bool Armed;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2 center = r.center;
            float radius = Math.Min(r.width, r.height) * .48f;
            const int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                float b = (i + 1) * Mathf.PI * 2 / segments;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Color fill = Armed ? new Color(.16f, .55f, .66f, .66f) : new Color(.12f, .25f, .30f, .57f);
                Color edge = Color.Lerp(new Color(.28f, .48f, .55f, .65f), new Color(.75f, .90f, .94f, .86f), (p.y + 1) / 2);
                int n = vh.currentVertCount;
                vh.AddVert(center, fill, Vector2.zero);
                vh.AddVert(center + p * radius * .84f, fill, Vector2.zero);
                vh.AddVert(center + q * radius * .84f, fill, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                n = vh.currentVertCount;
                vh.AddVert(center + p * radius * .84f, edge, Vector2.zero);
                vh.AddVert(center + q * radius * .84f, edge, Vector2.zero);
                edge.a *= i % 2 == 0 ? .72f : 1;
                vh.AddVert(center + q * radius, edge, Vector2.zero);
                vh.AddVert(center + p * radius, edge, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
            if (Armed)
            {
                AddRing(vh, center, radius - 1.5f, radius, new Color(.70f, .98f, 1, .95f), new Color(.70f, .98f, 1, .95f));
                AddRing(vh, center, radius, radius + 2, new Color(.22f, .85f, 1, .40f), new Color(.22f, .85f, 1, .18f));
                AddRing(vh, center, radius + 2, radius + 5, new Color(.22f, .85f, 1, .18f), new Color(.22f, .85f, 1, 0));
            }
        }

        private static void AddRing(VertexHelper vh, Vector2 center, float inner, float outer, Color innerColor, Color outerColor)
        {
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                float b = (i + 1) * Mathf.PI * 2 / segments;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = vh.currentVertCount;
                vh.AddVert(center + p * inner, innerColor, Vector2.zero);
                vh.AddVert(center + q * inner, innerColor, Vector2.zero);
                vh.AddVert(center + q * outer, outerColor, Vector2.zero);
                vh.AddVert(center + p * outer, outerColor, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }

    public sealed class FlowHandController : MonoBehaviour
    {
        private BattleUnitCardsInHandUI _hand;
        private BattleUnitModel _unit;
        private GameObject _buttonObject;
        private Button _button;
        private FlowPrismGraphic _prism;
        private TextMeshProUGUI _count;
        private TextMeshProUGUI _hint;
        private bool _armed;
        private int _revision = -1;
        private float _messageUntil;
        private string _message;
        private string _lastNotice;
        public bool Armed => _armed;

        public void Initialize(BattleUnitCardsInHandUI hand)
        {
            _hand = hand;
            _buttonObject = new GameObject("SteriaFlowButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(FlowPrismGraphic), typeof(Button));
            RectTransform rect = _buttonObject.GetComponent<RectTransform>();
            rect.SetParent(hand.rootobj.transform, false);
            rect.sizeDelta = new Vector2(62, 62);
            _prism = _buttonObject.GetComponent<FlowPrismGraphic>();
            _button = _buttonObject.GetComponent<Button>();
            _button.targetGraphic = _prism;
            _button.onClick.AddListener(Toggle);
            var iconObject = new GameObject("FlowIcon", typeof(RectTransform), typeof(Image));
            var icon = iconObject.GetComponent<Image>();
            icon.transform.SetParent(rect, false);
            icon.rectTransform.sizeDelta = new Vector2(34, 34);
            icon.raycastTarget = false;
            Sprite sprite;
            if (BattleUnitBuf._bufIconDictionary.TryGetValue("SteriaFlow", out sprite)) icon.sprite = sprite;
            else icon.enabled = false;
            _count = CreateText(rect, "AvailableFlow", new Vector2(0, -49), new Vector2(150, 44), 18);
            _hint = CreateText(rect, "FlowHint", new Vector2(0, 65), new Vector2(420, 46), 15);
            _buttonObject.SetActive(false);
            RefreshNow();
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition = pos;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.78f, .90f, .94f);
            text.raycastTarget = false;
            if (SingletonBehavior<LocalizedFontSetter>.Instance != null)
                SingletonBehavior<LocalizedFontSetter>.Instance.SetLocalizedFont(text, FontType.FONT_BODY);
            return text;
        }

        public void Toggle()
        {
            if (!CanInteract()) return;
            _hand.SetSelectedCardUI(null);
            _armed = !_armed;
            _messageUntil = 0;
            RefreshState();
        }

        // Match the hand panel's own selected/hovered model resolution.
        private BattleUnitModel DisplayedUnit => _hand?.SelectedModel ?? _hand?.HOveredModel;
        private int PendingFlow => DisplayedUnit?.bufListDetail?.GetActivatedBufList()
            .OfType<BattleUnitBuf_PendingFlow>().Where(b => !b.IsDestroyed()).Sum(b => Math.Max(0, b.stack)) ?? 0;
        private bool CanShow() => _hand != null && _hand.rootobj != null && _hand.IsActivated() &&
            DisplayedUnit != null && (FlowPlanning.Available(DisplayedUnit) > 0 || PendingFlow > 0);
        private bool CanInteract() => CanShow() && FlowPlanning.Available(DisplayedUnit) > 0 && FlowPlanning.IsPlanning &&
            _hand.SelectedModel != null && _hand.SelectedModel.faction == Faction.Player && _hand.SelectedModel.IsControlable() &&
            !_hand.SelectedModel.IsDead() && !_hand.SelectedModel.IsBreakLifeZero();

        public bool HandleCardClick(BattleDiceCardUI cardUI, PointerEventData data)
        {
            if (!_armed || !_hand.GetCardUIList().Contains(cardUI)) return false;
            if (data != null && data.button != PointerEventData.InputButton.Left)
            {
                Cancel();
                return true;
            }
            if (!CanInteract())
            {
                _message = "当前无法进行流强化";
                _messageUntil = Time.unscaledTime + 3;
                Cancel();
                cardUI.PlayVibeCard();
                return true;
            }
            string message;
            bool success = FlowPlanning.TryEnhance(_hand.SelectedModel, cardUI.CardModel, out message);
            _armed = success && FlowPlanning.Available(_hand.SelectedModel) > 0;
            _message = message ?? "请在布置阶段选择当前角色的手牌";
            if (_armed) _message += "\n点击手牌继续强化 · 右键取消";
            _messageUntil = Time.unscaledTime + 3;
            RefreshCards();
            RefreshNow();
            if (success)
            {
                SingletonBehavior<BattleSoundManager>.Instance?.PlaySound(EffectSoundType.CARD_APPLY, false);
                FlowCardPreview.Flash(cardUI);
            }
            else cardUI.PlayVibeCard();
            return true;
        }

        public void Cancel() { _armed = false; RefreshState(); }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) _armed = false;
            if (FlowPlanning.IsPlanning) FlowPlanning.Reconcile();
            RefreshNow();
        }

        // Binding and Flow-gain callbacks call this synchronously; Update is only a fallback.
        public void RefreshNow()
        {
            if (_hand == null || _buttonObject == null) return;
            if (_unit != DisplayedUnit) { _unit = DisplayedUnit; _armed = false; _messageUntil = 0; }
            bool show = CanShow();
            _buttonObject.SetActive(show);
            bool canInteract = CanInteract();
            _button.interactable = canInteract;
            if (!canInteract) _armed = false;
            if (!show) { _armed = false; RefreshState(); return; }
            if (_lastNotice != FlowPlanning.Notice)
            {
                _lastNotice = FlowPlanning.Notice;
                _message = _lastNotice;
                _messageUntil = Time.unscaledTime + 4;
            }
            if (_revision != FlowPlanning.Revision) RefreshCards();
            // Measure the actual hand bounds so the button cannot overlap the last page.
            var cards = (_hand.GetCardUIList() ?? new List<BattleDiceCardUI>()).Where(c => c != null && c.gameObject.activeSelf).ToList();
            if (cards.Count > 0)
            {
                Transform parent = _buttonObject.transform.parent;
                float right = float.MinValue;
                float left = float.MaxValue;
                float top = float.MinValue;
                var corners = new Vector3[4];
                foreach (var card in cards)
                {
                    var cardRect = card.transform as RectTransform;
                    if (cardRect == null) continue;
                    cardRect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 point = parent.InverseTransformPoint(corner);
                        right = Mathf.Max(right, point.x);
                        left = Mathf.Min(left, point.x);
                        top = Mathf.Max(top, point.y);
                    }
                }
                float bottom = cards.Min(c => parent.InverseTransformPoint(c.transform.position).y);
                if (right > float.MinValue) _buttonObject.transform.localPosition = new Vector3(right + 44, bottom + 32, 0);
                if (top > float.MinValue) _hint.transform.position = parent.TransformPoint(new Vector3((left + right) / 2, top + 32, 0));
            }
            else
            {
                var panel = _hand.rootobj.transform as RectTransform;
                if (panel != null)
                    _buttonObject.transform.localPosition = new Vector3(panel.rect.xMax - 44, panel.rect.center.y, 0);
            }
            _buttonObject.transform.SetAsLastSibling();
            RefreshState();
        }

        private void RefreshState()
        {
            if (_prism == null) return;
            if (_prism.Armed != _armed) { _prism.Armed = _armed; _prism.SetVerticesDirty(); }
            int available = FlowPlanning.Available(DisplayedUnit);
            int pending = PendingFlow;
            _count.text = pending > 0 ? "可用 " + available + "\n下幕 +" + pending : "流 " + available;
            _hint.text = Time.unscaledTime < _messageUntil ? _message : (_armed ? "选择一张手牌进行流强化\n右键取消" : "");
        }

        private void RefreshCards()
        {
            _revision = FlowPlanning.Revision;
            var cards = _hand.GetCardUIList();
            if (cards == null) return;
            foreach (var cardUI in cards.Where(c => c != null && c.gameObject.activeSelf)) FlowCardPreview.Apply(cardUI);
        }

        private void OnDisable() { _armed = false; RefreshState(); if (_buttonObject != null) _buttonObject.SetActive(false); }
        private void OnDestroy() { if (_buttonObject != null) Destroy(_buttonObject); }
    }

    public sealed class FlowRangePresentation
    {
        public string Original, Written;
        public Vector2 Size;
        public Vector2 Position;
        public TextAlignmentOptions Alignment;
        public float FontSize, LineSpacing;
        public bool AutoSize, WordWrapping;
        public TextOverflowModes Overflow;
    }

    public sealed class FlowCardPreviewStamp : MonoBehaviour
    {
        public readonly Dictionary<TextMeshProUGUI, FlowRangePresentation> Texts = new Dictionary<TextMeshProUGUI, FlowRangePresentation>();
        public readonly Dictionary<Transform, Vector3> RowPositions = new Dictionary<Transform, Vector3>();
        public BattleDiceCardModel BoundCard;
        public readonly List<Image> FlashImages = new List<Image>();
        public float FlashStart = -1;
        public void StopFlash()
        {
            FlashStart = -1;
            foreach (var image in FlashImages) if (image != null) image.gameObject.SetActive(false);
        }
        private void Update() { UpdateFlash(Time.unscaledTime); }
        private void UpdateFlash(float now)
        {
            if (FlashStart < 0) return;
            float age = now - FlashStart;
            if (age >= .3f) { StopFlash(); return; }
            float alpha = age < .05f ? Mathf.Lerp(.12f, .95f, age / .05f) : .95f * (1 - (age - .05f) / .25f);
            foreach (var image in FlashImages) if (image != null) image.color = new Color(.70f, .98f, 1, alpha);
        }
        private void OnEnable() { Canvas.willRenderCanvases += KeepRangeSpacing; }
        private void KeepRangeSpacing()
        {
            if (Texts.Count > 0) FlowCardPreview.SpaceVisibleRanges(GetComponent<BattleDiceCardUI>(), false);
        }
        private void OnDisable() { Canvas.willRenderCanvases -= KeepRangeSpacing; StopFlash(); }
        private void OnDestroy() { Canvas.willRenderCanvases -= KeepRangeSpacing; }
    }

    public static class FlowCardPreview
    {
        private static readonly System.Reflection.MethodInfo RefreshIcons = AccessTools.Method(typeof(BattleDiceCardUI), "EnableAddedIcons");

        public static void Restore(BattleDiceCardUI ui) { Restore(ui, true); }
        private static void Restore(BattleDiceCardUI ui, bool stopFlash)
        {
            var stamp = ui?.GetComponent<FlowCardPreviewStamp>();
            if (stamp == null) return;
            foreach (var row in stamp.RowPositions)
                if (row.Key != null) row.Key.localPosition = row.Value;
            stamp.RowPositions.Clear();
            foreach (var pair in stamp.Texts)
            {
                if (pair.Key == null) continue;
                var text = pair.Key; var saved = pair.Value;
                if (text.text == saved.Written) text.text = saved.Original;
                text.rectTransform.sizeDelta = saved.Size;
                text.rectTransform.anchoredPosition = saved.Position;
                text.alignment = saved.Alignment;
                text.fontSize = saved.FontSize; text.lineSpacing = saved.LineSpacing;
                text.enableAutoSizing = saved.AutoSize; text.enableWordWrapping = saved.WordWrapping;
                text.overflowMode = saved.Overflow;
            }
            stamp.Texts.Clear();
            if (stopFlash || stamp.BoundCard != ui.CardModel) stamp.StopFlash();
        }

        public static void Apply(BattleDiceCardUI ui)
        {
            Restore(ui, false);
            if (ui?.CardModel == null) return;
            // Always refresh the real card-buff area, including when a pooled card loses its Flow plan.
            if (ui.bufIconListUI != null) RefreshIcons.Invoke(ui, null);
            var plan = FlowPlanning.Get(ui.CardModel);
            if (plan == null) return;
            var stamp = ui.GetComponent<FlowCardPreviewStamp>() ?? ui.gameObject.AddComponent<FlowCardPreviewStamp>();
            stamp.BoundCard = ui.CardModel;
            var cardRect = ui.transform as RectTransform;
            if (cardRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            var dice = ui.CardModel.GetBehaviourList();
            for (int i = 0; i < dice.Count && i < ui.ui_behaviourDescList.Count; i++)
            {
                var row = ui.ui_behaviourDescList[i];
                if (row?.txt_range == null) continue;
                int bonus = plan.Mass ? plan.Amount / 8 :
                    (dice[i].Type == BehaviourType.Standby || !plan.DiceIndices.Contains(i) ? 0 : plan.Levels + (plan.Proxy ? 1 : 0));
                if (bonus <= 0) continue;
                var text = row.txt_range;
                var saved = new FlowRangePresentation { Original = text.text, Size = text.rectTransform.sizeDelta,
                    Position = text.rectTransform.anchoredPosition, Alignment = text.alignment,
                    FontSize = text.fontSize, LineSpacing = text.lineSpacing, AutoSize = text.enableAutoSizing,
                    WordWrapping = text.enableWordWrapping, Overflow = text.overflowMode };
                saved.Written = (dice[i].Min + bonus) + "-" + (dice[i].Dice + bonus) + "\n(+" + bonus + ")";
                stamp.Texts[text] = saved;
                text.text = saved.Written;
                text.enableAutoSizing = false; text.enableWordWrapping = false;
                // Positive natural leading: a bonus line must never move above its range.
                text.lineSpacing = 0;
                // Preserve horizontal alignment while anchoring the two-line block at its old top edge.
                text.alignment = (TextAlignmentOptions)(((int)saved.Alignment & 0xFF) | ((int)TextAlignmentOptions.TopLeft & ~0xFF));
                float height = text.GetPreferredValues(saved.Written).y;
                Vector3 oldTop = text.transform.TransformPoint(new Vector3(0, text.rectTransform.rect.yMax, 0));
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(text.rectTransform.rect.height, height));
                Vector3 newTop = text.transform.TransformPoint(new Vector3(0, text.rectTransform.rect.yMax, 0));
                text.transform.position += oldTop - newTop;
                text.overflowMode = TextOverflowModes.Overflow;
            }
            SpaceVisibleRanges(ui, true);
        }

        // Reflow visible dice rows using glyph bounds, not hidden prefab rows or guessed line spacing.
        // Run again just before rendering because native layout groups can reposition pooled rows.
        public static void SpaceVisibleRanges(BattleDiceCardUI ui, bool updateMeshes)
        {
            var stamp = ui?.GetComponent<FlowCardPreviewStamp>();
            if (stamp == null || stamp.Texts.Count == 0 || ui.CardModel == null || ui.ui_behaviourDescList == null) return;
            float previousBottom = float.PositiveInfinity;
            int count = Math.Min(ui.CardModel.GetBehaviourList().Count, ui.ui_behaviourDescList.Count);
            for (int i = 0; i < count; i++)
            {
                var row = ui.ui_behaviourDescList[i];
                var text = row?.txt_range;
                if (text == null || !row.gameObject.activeSelf || !text.gameObject.activeSelf) continue;
                if (updateMeshes) text.ForceMeshUpdate(true);
                Bounds bounds = text.textBounds;
                if (bounds.size.y <= 0) continue;
                Vector3 a = ui.transform.InverseTransformPoint(text.transform.TransformPoint(bounds.min));
                Vector3 b = ui.transform.InverseTransformPoint(text.transform.TransformPoint(bounds.max));
                float top = Mathf.Max(a.y, b.y);
                float bottom = Mathf.Min(a.y, b.y);
                float padding = ui.transform.InverseTransformVector(text.transform.TransformVector(Vector3.up * text.fontSize * .15f)).magnitude;
                float overlap = top - (previousBottom - padding);
                if (overlap > .01f)
                {
                    if (!stamp.RowPositions.ContainsKey(row.transform)) stamp.RowPositions.Add(row.transform, row.transform.localPosition);
                    row.transform.position += ui.transform.TransformVector(Vector3.down * overlap);
                    bottom -= overlap;
                }
                previousBottom = bottom;
            }
        }

        public static void Flash(BattleDiceCardUI ui)
        {
            if (ui == null) return;
            var stamp = ui.GetComponent<FlowCardPreviewStamp>() ?? ui.gameObject.AddComponent<FlowCardPreviewStamp>();
            if (stamp.FlashImages.Count == 0 && ui.img_linearDodges != null)
                foreach (var source in ui.img_linearDodges)
                {
                    if (source == null) continue;
                    var go = new GameObject("SteriaFlowSuccessFlash", typeof(RectTransform), typeof(Image));
                    var image = go.GetComponent<Image>();
                    image.transform.SetParent(source.transform, false);
                    var rect = image.rectTransform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    image.sprite = source.sprite; image.material = source.material;
                    image.type = source.type; image.preserveAspect = source.preserveAspect;
                    image.fillCenter = source.fillCenter; image.raycastTarget = false;
                    stamp.FlashImages.Add(image);
                }
            stamp.BoundCard = ui.CardModel;
            stamp.FlashStart = Time.unscaledTime;
            foreach (var image in stamp.FlashImages)
            {
                image.color = new Color(.70f, .98f, 1, .12f);
                image.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(BattleDiceCardBuf), "GetBufIcon")]
    public static class FlowNativeIconPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleDiceCardBuf __instance, ref Sprite __result)
        {
            var flow = __instance as BattleDiceCardBuf_ManualFlow;
            if (flow == null) return true;
            __result = null;
            if (!flow.IsDestroyed() && flow.Levels > 0)
                BattleUnitBuf._bufIconDictionary.TryGetValue("SteriaFlow", out __result);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleDiceCardBufUI), "SetBufIcon")]
    public static class FlowNativeCountPatch
    {
        [HarmonyPrefix]
        public static void Prefix(BattleDiceCardBufUI __instance)
        {
            var stamp = __instance.GetComponent<FlowNativeSlotStamp>();
            if (stamp == null || !stamp.Applied) return;
            __instance.img_bufIcon.raycastTarget = stamp.IconRaycast;
            __instance.txt_bufIconStack.raycastTarget = stamp.TextRaycast;
            if (__instance.overlay != null) __instance.overlay.enabled = stamp.OverlayEnabled;
            stamp.Applied = false;
        }

        [HarmonyPostfix]
        public static void Postfix(BattleDiceCardBufUI __instance, BattleDiceCardBuf cardBuf)
        {
            var flow = cardBuf as BattleDiceCardBuf_ManualFlow;
            if (flow == null) return;
            var stamp = __instance.GetComponent<FlowNativeSlotStamp>() ?? __instance.gameObject.AddComponent<FlowNativeSlotStamp>();
            stamp.IconRaycast = __instance.img_bufIcon.raycastTarget;
            stamp.TextRaycast = __instance.txt_bufIconStack.raycastTarget;
            stamp.OverlayEnabled = __instance.overlay != null && __instance.overlay.enabled;
            stamp.Applied = true;
            __instance.txt_bufIconStack.text = "×" + flow.Levels;
            __instance.txt_bufIconStack.raycastTarget = false;
            __instance.img_bufIcon.raycastTarget = false;
            if (__instance.overlay != null) __instance.overlay.enabled = false;
        }
    }

    public sealed class FlowNativeSlotStamp : MonoBehaviour
    {
        public bool Applied, IconRaycast, TextRaycast, OverlayEnabled;
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "EnableAddedIcons")]
    public static class FlowNativeSlotsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(BattleDiceCardUI __instance)
        {
            if (__instance.CardModel == null || FlowPlanning.Get(__instance.CardModel) == null) return;
            var slots = __instance.bufIconListUI;
            if (slots == null || slots.Length == 0) return;
            int needed = __instance.CardModel.GetBufList().Count(b => b.GetBufIcon() != null);
            if (needed <= slots.Length) return;
            var expanded = slots.ToList();
            while (expanded.Count < needed)
            {
                var template = expanded[expanded.Count - 1];
                var copy = UnityEngine.Object.Instantiate(template, template.transform.parent);
                copy.name = "SteriaFlowExtraNativeSlot";
                var rect = copy.transform as RectTransform;
                var previous = template.transform as RectTransform;
                if (rect != null && previous != null && template.transform.parent.GetComponent<LayoutGroup>() == null)
                {
                    Vector2 step = new Vector2(previous.rect.width * previous.localScale.x * 1.1f, 0);
                    if (expanded.Count > 1)
                        step = previous.anchoredPosition - ((RectTransform)expanded[expanded.Count - 2].transform).anchoredPosition;
                    if (step.sqrMagnitude < 1) step = new Vector2(previous.rect.width * previous.localScale.x * 1.1f, 0);
                    rect.anchoredPosition = previous.anchoredPosition + step;
                    // A full fixed native row wraps its extra slot toward the card interior.
                    var parent = rect.parent as RectTransform;
                    if (parent != null && parent.rect.width > rect.rect.width &&
                        Mathf.Abs(rect.localPosition.x - parent.rect.center.x) + rect.rect.width * rect.localScale.x / 2 > parent.rect.width / 2)
                    {
                        var first = (RectTransform)expanded[0].transform;
                        rect.anchoredPosition = new Vector2(first.anchoredPosition.x,
                            previous.anchoredPosition.y + previous.rect.height * previous.localScale.y * 1.1f);
                    }
                }
                foreach (var graphic in copy.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                expanded.Add(copy);
            }
            __instance.bufIconListUI = expanded.ToArray();
        }
    }

    [HarmonyPatch]
    public static class FlowHandButtonPatch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (string name in new[] { "Initialize", "SetCardsObject", "UpdateCardList", "Deactivate" })
                yield return AccessTools.Method(typeof(BattleUnitCardsInHandUI), name);
        }

        [HarmonyPostfix]
        public static void Postfix(BattleUnitCardsInHandUI __instance)
        {
            if (__instance == null || __instance.rootobj == null) return;
            var controller = __instance.GetComponent<FlowHandController>();
            if (controller == null)
            {
                controller = __instance.gameObject.AddComponent<FlowHandController>();
                controller.Initialize(__instance);
            }
            else controller.RefreshNow();
        }
    }

    [HarmonyPatch]
    public static class FlowHandGainRefreshPatch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BattleUnitBuf_Flow), "Init");
            yield return AccessTools.Method(typeof(CardAbilityHelper), "ApplyFlowStacksNow");
            yield return AccessTools.Method(typeof(FlowGainTiming), "Gain");
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            var hand = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand;
            if (hand != null) FlowHandButtonPatch.Postfix(hand);
        }
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "OnPdSubmit")]
    public static class FlowHandClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleDiceCardUI __instance, BaseEventData data)
        {
            var controller = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand?.GetComponent<FlowHandController>();
            return controller == null || !controller.HandleCardClick(__instance, data as PointerEventData);
        }
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "SetCard", new Type[] { typeof(BattleDiceCardModel), typeof(BattleDiceCardUI.Option[]) })]
    public static class FlowCardPreviewPatch
    {
        [HarmonyPrefix]
        public static void Prefix(BattleDiceCardUI __instance) { FlowCardPreview.Restore(__instance); }
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        public static void Postfix(BattleDiceCardUI __instance) { FlowCardPreview.Apply(__instance); }
    }
}
