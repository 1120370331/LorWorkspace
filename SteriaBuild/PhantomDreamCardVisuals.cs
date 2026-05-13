using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDiceCardBuf_PhantomDreamTransferred : BattleDiceCardBuf
{
    protected override string keywordId => "PhantomDreamTransferred";
    protected override string keywordIconId => "SteriaPhantomDream";
}

namespace Steria
{
    public static class PhantomDreamCardVisuals
    {
        private const string RootName = "SteriaPhantomDreamCardGlow";
        private static readonly Color InnerGlowColor = new Color(1f, 0.34f, 0.92f, 0.9f);
        private static readonly Color OuterGlowColor = new Color(0.72f, 0.42f, 1f, 0.42f);
        private static readonly Color DotPink = new Color(1f, 0.42f, 0.96f, 0.9f);
        private static readonly Color DotLavender = new Color(0.78f, 0.58f, 1f, 0.78f);

        private static Sprite _solidSprite;
        private static Sprite _dotSprite;

        private sealed class PhantomDreamCardGlowAnimator : MonoBehaviour
        {
            public CanvasGroup Group;
            public Image[] Dots = new Image[0];
            public float[] DotBaseAlpha = new float[0];

            private void Update()
            {
                float time = Time.unscaledTime;
                if (Group != null)
                {
                    Group.alpha = 0.74f + Mathf.Sin(time * 2.2f) * 0.14f;
                }

                for (int i = 0; i < Dots.Length; i++)
                {
                    Image dot = Dots[i];
                    if (dot == null)
                    {
                        continue;
                    }

                    float baseAlpha = i < DotBaseAlpha.Length ? DotBaseAlpha[i] : 0.8f;
                    Color color = dot.color;
                    color.a = baseAlpha * (0.46f + 0.54f * Mathf.Abs(Mathf.Sin(time * (1.45f + i * 0.07f) + i * 0.83f)));
                    dot.color = color;
                }
            }
        }

        public static void ApplyOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            if (!HasTransferredMarker(cardUi.CardModel))
            {
                ClearOnCardUI(cardUi);
                return;
            }

            EnsureVisual(cardUi);
        }

        private static bool HasTransferredMarker(BattleDiceCardModel card)
        {
            List<BattleDiceCardBuf> bufs = card?.GetBufList();
            if (bufs == null)
            {
                return false;
            }

            for (int i = 0; i < bufs.Count; i++)
            {
                BattleDiceCardBuf buf = bufs[i];
                if (buf is global::BattleDiceCardBuf_PhantomDreamTransferred && !buf.IsDestroyed())
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureVisual(BattleDiceCardUI cardUi)
        {
            Transform existing = cardUi.transform.Find(RootName);
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return;
            }

            GameObject rootObj = new GameObject(RootName, typeof(RectTransform));
            rootObj.transform.SetParent(cardUi.transform, false);
            rootObj.transform.SetAsLastSibling();

            RectTransform root = rootObj.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(-9f, -12f);
            root.offsetMax = new Vector2(9f, 12f);

            CanvasGroup group = rootObj.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            CreateBorderLayer(root, "Outer", OuterGlowColor, 8f, -3f);
            CreateBorderLayer(root, "Inner", InnerGlowColor, 3.2f, 3f);
            Image[] dots = CreateDots(root);

            PhantomDreamCardGlowAnimator animator = rootObj.AddComponent<PhantomDreamCardGlowAnimator>();
            animator.Group = group;
            animator.Dots = dots;
            animator.DotBaseAlpha = BuildBaseAlphas(dots.Length);
        }

        private static void ClearOnCardUI(BattleDiceCardUI cardUi)
        {
            Transform root = cardUi?.transform?.Find(RootName);
            if (root != null)
            {
                UnityEngine.Object.Destroy(root.gameObject);
            }
        }

        private static void CreateBorderLayer(RectTransform root, string suffix, Color color, float thickness, float inset)
        {
            CreateBar(root, "Top" + suffix, color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness), new Vector2(0f, -inset));
            CreateBar(root, "Bottom" + suffix, color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness), new Vector2(0f, inset));
            CreateBar(root, "Left" + suffix, color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f), new Vector2(inset, 0f));
            CreateBar(root, "Right" + suffix, color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f), new Vector2(-inset, 0f));
        }

        private static void CreateBar(RectTransform root, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(root, false);

            Image image = obj.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private static Image[] CreateDots(RectTransform root)
        {
            Vector2[] anchors =
            {
                new Vector2(0.07f, 1.02f), new Vector2(0.18f, 0.99f), new Vector2(0.33f, 1.04f), new Vector2(0.49f, 1.01f),
                new Vector2(0.66f, 1.03f), new Vector2(0.82f, 0.98f), new Vector2(0.94f, 1.02f),
                new Vector2(0.06f, -0.01f), new Vector2(0.21f, 0.02f), new Vector2(0.39f, -0.03f), new Vector2(0.58f, 0.01f),
                new Vector2(0.75f, -0.02f), new Vector2(0.92f, 0.02f),
                new Vector2(-0.02f, 0.13f), new Vector2(0.02f, 0.31f), new Vector2(-0.03f, 0.52f), new Vector2(0.01f, 0.74f),
                new Vector2(1.02f, 0.16f), new Vector2(0.99f, 0.35f), new Vector2(1.03f, 0.55f), new Vector2(1.01f, 0.78f)
            };

            Image[] dots = new Image[anchors.Length];
            for (int i = 0; i < anchors.Length; i++)
            {
                GameObject obj = new GameObject("Dot" + i, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(root, false);

                Image image = obj.GetComponent<Image>();
                image.sprite = GetDotSprite();
                image.color = i % 3 == 0 ? DotPink : DotLavender;
                image.raycastTarget = false;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = anchors[i];
                rect.anchorMax = anchors[i];
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = 5f + (i % 5) * 1.8f;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(Mathf.Sin(i * 2.31f) * 5.5f, Mathf.Cos(i * 1.73f) * 5.5f);

                dots[i] = image;
            }

            return dots;
        }

        private static float[] BuildBaseAlphas(int count)
        {
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = 0.58f + (i % 4) * 0.1f;
            }

            return result;
        }

        private static Sprite GetSolidSprite()
        {
            if (_solidSprite != null)
            {
                return _solidSprite;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _solidSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            _solidSprite.hideFlags = HideFlags.HideAndDontSave;
            return _solidSprite;
        }

        private static Sprite GetDotSprite()
        {
            if (_dotSprite != null)
            {
                return _dotSprite;
            }

            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / radius);
                    float alpha = Mathf.Pow(1f - normalized, 1.7f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _dotSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _dotSprite.hideFlags = HideFlags.HideAndDontSave;
            return _dotSprite;
        }
    }
}
