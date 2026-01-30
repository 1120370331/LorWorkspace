using UnityEngine;
using UnityEngine.UI;
using BaseMod;

namespace Steria
{
    public static class MusicScoreUI
    {
        private static GameObject _root;
        private static Text _leftText;
        private static Text _rightText;

        public static void EnsureUI()
        {
            if (_root != null)
            {
                return;
            }

            BattleManagerUI ui = SingletonBehavior<BattleManagerUI>.Instance;
            if (ui == null)
            {
                return;
            }

            _root = new GameObject("SteriaMusicScoreUI");
            Transform parent = FindCanvasParent(ui);
            if (parent == null)
            {
                return;
            }
            _root.transform.SetParent(parent, false);
            _root.layer = parent.gameObject.layer;

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Font font = UtilTools._DefFont;

            _leftText = CreateText("MusicScore_Left", _root.transform, new Vector2(120f, 320f), new Vector2(0f, 0.5f), TextAnchor.MiddleLeft, font);
            _rightText = CreateText("MusicScore_Right", _root.transform, new Vector2(-120f, 320f), new Vector2(1f, 0.5f), TextAnchor.MiddleRight, font);
        }

        private static Transform FindCanvasParent(BattleManagerUI ui)
        {
            if (ui != null)
            {
                Canvas[] uiCanvases = ui.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas canvas in uiCanvases)
                {
                    if (canvas == null)
                    {
                        continue;
                    }

                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.gameObject.activeInHierarchy)
                    {
                        return canvas.transform;
                    }
                }

                if (uiCanvases.Length > 0)
                {
                    return uiCanvases[0].transform;
                }
            }

            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                if (canvas.gameObject.activeInHierarchy)
                {
                    return canvas.transform;
                }
            }

            if (ui.ui_unitListInfoSummary != null)
            {
                return ui.ui_unitListInfoSummary.transform;
            }

            return ui.transform;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 anchor, TextAnchor alignment, Font font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(260f, 44f);
            text.fontSize = 24;
            text.color = new Color(1f, 0.95f, 0.7f, 1f);
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }
            text.text = "Score 0/0";
            CreateBackground($"{name}_Bg", parent, rect);
            return text;
        }

        private static void CreateBackground(string name, Transform parent, RectTransform target)
        {
            GameObject bg = new GameObject(name);
            bg.transform.SetParent(parent, false);
            bg.transform.SetSiblingIndex(target.GetSiblingIndex());
            UnityEngine.UI.Image img = bg.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            RectTransform rect = bg.GetComponent<RectTransform>();
            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.anchoredPosition = target.anchoredPosition;
            rect.sizeDelta = new Vector2(target.sizeDelta.x + 16f, target.sizeDelta.y + 10f);
        }

        public static void UpdateAll()
        {
            EnsureUI();
            if (_root == null || _leftText == null || _rightText == null)
            {
                return;
            }

            bool active = MusicScoreSystem.IsActive;
            _root.SetActive(active);
            if (!active)
            {
                return;
            }

            int leftMax = MusicScoreSystem.GetMaxScore(Faction.Player);
            int rightMax = MusicScoreSystem.GetMaxScore(Faction.Enemy);
            _leftText.text = $"P {MusicScoreSystem.GetScore(Faction.Player)}/{leftMax}";
            _rightText.text = $"E {MusicScoreSystem.GetScore(Faction.Enemy)}/{rightMax}";
        }

        public static void DestroyUI()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _leftText = null;
                _rightText = null;
            }
        }
    }
}
