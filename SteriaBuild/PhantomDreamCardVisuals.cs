using System;
using System.Collections.Generic;
using HarmonyLib;
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

        private static Sprite _glowFrameSprite;
        private static Sprite _dotSprite;

        private sealed class PhantomDreamCardGlowAnimator : MonoBehaviour
        {
            public CanvasGroup Group;
            public Image[] GlowLayers = new Image[0];
            public Image[] Particles = new Image[0];
            public RectTransform[] ParticleRects = new RectTransform[0];
            public float[] ParticleBaseAlpha = new float[0];
            public Vector2[] ParticleBasePosition = new Vector2[0];
            public Vector2[] ParticleDrift = new Vector2[0];
            public float[] ParticleSpeed = new float[0];
            public float[] ParticlePhase = new float[0];

            private void Update()
            {
                float time = Time.unscaledTime;
                if (Group != null)
                {
                    Group.alpha = 0.78f + Mathf.Sin(time * 1.9f) * 0.12f;
                }

                for (int i = 0; i < GlowLayers.Length; i++)
                {
                    Image glow = GlowLayers[i];
                    if (glow == null)
                    {
                        continue;
                    }

                    Color color = glow.color;
                    color.a = (i == 0 ? OuterGlowColor.a : InnerGlowColor.a) * (0.72f + Mathf.Sin(time * (1.35f + i * 0.3f)) * 0.18f);
                    glow.color = color;
                }

                for (int i = 0; i < Particles.Length; i++)
                {
                    Image particle = Particles[i];
                    RectTransform rect = i < ParticleRects.Length ? ParticleRects[i] : null;
                    if (particle == null || rect == null)
                    {
                        continue;
                    }

                    float speed = i < ParticleSpeed.Length ? ParticleSpeed[i] : 1.4f;
                    float phase = i < ParticlePhase.Length ? ParticlePhase[i] : i * 0.41f;
                    Vector2 basePosition = i < ParticleBasePosition.Length ? ParticleBasePosition[i] : Vector2.zero;
                    Vector2 drift = i < ParticleDrift.Length ? ParticleDrift[i] : new Vector2(5f, 5f);
                    float wave = Mathf.Sin(time * speed + phase);
                    float crossWave = Mathf.Cos(time * (speed * 0.73f) + phase * 1.31f);
                    rect.anchoredPosition = basePosition + new Vector2(wave * drift.x, crossWave * drift.y);
                    float scale = 0.82f + Mathf.Abs(wave) * 0.34f;
                    rect.localScale = new Vector3(scale, scale, 1f);

                    float baseAlpha = i < ParticleBaseAlpha.Length ? ParticleBaseAlpha[i] : 0.75f;
                    Color color = particle.color;
                    color.a = baseAlpha * (0.32f + 0.68f * Mathf.Abs(Mathf.Sin(time * (speed + 0.2f) + phase)));
                    particle.color = color;
                }
            }
        }

        public static void ApplyOnCardUI(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return;
            }

            if (!IsHandCardUi(cardUi))
            {
                ClearOnCardUI(cardUi);
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
            RectTransform faceRoot = ResolveCardFaceRoot(cardUi);
            if (faceRoot == null)
            {
                return;
            }

            Transform existing = FindVisualRoot(cardUi);
            if (existing != null)
            {
                if (existing.parent != faceRoot)
                {
                    UnityEngine.Object.Destroy(existing.gameObject);
                }
                else
                {
                    existing.SetAsLastSibling();
                    return;
                }
            }

            Transform existingOnTarget = faceRoot.Find(RootName);
            if (existingOnTarget != null)
            {
                existingOnTarget.SetAsLastSibling();
                return;
            }

            GameObject rootObj = new GameObject(RootName, typeof(RectTransform));
            rootObj.transform.SetParent(faceRoot, false);
            rootObj.transform.SetAsLastSibling();

            RectTransform root = rootObj.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(-16f, -18f);
            root.offsetMax = new Vector2(16f, 18f);

            CanvasGroup group = rootObj.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Image[] glowLayers =
            {
                CreateGlowLayer(root, "OuterGlow", OuterGlowColor, new Vector2(-10f, -12f), new Vector2(10f, 12f)),
                CreateGlowLayer(root, "InnerGlow", InnerGlowColor, new Vector2(-2f, -3f), new Vector2(2f, 3f))
            };
            Image[] particles = CreateParticles(root, out RectTransform[] particleRects, out Vector2[] basePositions, out Vector2[] drifts, out float[] speeds, out float[] phases);

            PhantomDreamCardGlowAnimator animator = rootObj.AddComponent<PhantomDreamCardGlowAnimator>();
            animator.Group = group;
            animator.GlowLayers = glowLayers;
            animator.Particles = particles;
            animator.ParticleRects = particleRects;
            animator.ParticleBaseAlpha = BuildBaseAlphas(particles.Length);
            animator.ParticleBasePosition = basePositions;
            animator.ParticleDrift = drifts;
            animator.ParticleSpeed = speeds;
            animator.ParticlePhase = phases;
        }

        private static bool IsHandCardUi(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return false;
            }

            BattleUnitCardsInHandUI handUi = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand;
            if (handUi == null)
            {
                return false;
            }

            List<BattleDiceCardUI> cardList = handUi.GetCardUIList();
            if (cardList != null && cardList.Contains(cardUi))
            {
                return true;
            }

            GameObject handRoot = handUi.rootobj;
            return handRoot != null && cardUi.transform != null && cardUi.transform.IsChildOf(handRoot.transform);
        }

        private static RectTransform ResolveCardFaceRoot(BattleDiceCardUI cardUi)
        {
            RectTransform vibeRect = AccessTools.Field(typeof(BattleDiceCardUI), "vibeRect")?.GetValue(cardUi) as RectTransform;
            if (vibeRect != null)
            {
                return vibeRect;
            }

            Image artwork = AccessTools.Field(typeof(BattleDiceCardUI), "img_artwork")?.GetValue(cardUi) as Image;
            RectTransform artworkParent = artwork?.transform?.parent as RectTransform;
            if (artworkParent != null)
            {
                return artworkParent;
            }

            return cardUi.transform as RectTransform;
        }

        private static Transform FindVisualRoot(BattleDiceCardUI cardUi)
        {
            if (cardUi == null)
            {
                return null;
            }

            Transform direct = cardUi.transform.Find(RootName);
            if (direct != null)
            {
                return direct;
            }

            RectTransform faceRoot = ResolveCardFaceRoot(cardUi);
            return faceRoot != null ? faceRoot.Find(RootName) : null;
        }

        private static void ClearOnCardUI(BattleDiceCardUI cardUi)
        {
            Transform root = FindVisualRoot(cardUi);
            if (root != null)
            {
                UnityEngine.Object.Destroy(root.gameObject);
            }
        }

        private static Image CreateGlowLayer(RectTransform root, string name, Color color, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(root, false);

            Image image = obj.GetComponent<Image>();
            image.sprite = GetGlowFrameSprite();
            image.color = color;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return image;
        }

        private static Image[] CreateParticles(
            RectTransform root,
            out RectTransform[] particleRects,
            out Vector2[] basePositions,
            out Vector2[] drifts,
            out float[] speeds,
            out float[] phases)
        {
            const int count = 44;
            Image[] particles = new Image[count];
            particleRects = new RectTransform[count];
            basePositions = new Vector2[count];
            drifts = new Vector2[count];
            speeds = new float[count];
            phases = new float[count];

            for (int i = 0; i < count; i++)
            {
                GameObject obj = new GameObject("Particle" + i, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(root, false);

                Image image = obj.GetComponent<Image>();
                image.sprite = GetDotSprite();
                image.color = i % 3 == 0 ? DotPink : DotLavender;
                image.raycastTarget = false;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;

                RectTransform rect = image.rectTransform;
                float t = Mathf.Repeat(0.11f + i * 0.381966f, 1f);
                Vector2 anchor;
                Vector2 outward;
                switch (i % 4)
                {
                    case 0:
                        anchor = new Vector2(t, 1.02f);
                        outward = new Vector2(0f, 1f);
                        break;
                    case 1:
                        anchor = new Vector2(1.02f, t);
                        outward = new Vector2(1f, 0f);
                        break;
                    case 2:
                        anchor = new Vector2(t, -0.02f);
                        outward = new Vector2(0f, -1f);
                        break;
                    default:
                        anchor = new Vector2(-0.02f, t);
                        outward = new Vector2(-1f, 0f);
                        break;
                }

                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = 4.5f + (i % 6) * 1.6f;
                rect.sizeDelta = new Vector2(size, size);

                Vector2 tangential = new Vector2(-outward.y, outward.x);
                Vector2 basePosition = outward * (3.5f + (i % 5) * 1.4f) + tangential * Mathf.Sin(i * 1.37f) * 8f;
                rect.anchoredPosition = basePosition;

                particles[i] = image;
                particleRects[i] = rect;
                basePositions[i] = basePosition;
                drifts[i] = new Vector2(3.5f + (i % 4) * 1.2f, 4.5f + (i % 5) * 1.1f);
                speeds[i] = 1.15f + (i % 7) * 0.18f;
                phases[i] = i * 0.73f;
            }

            return particles;
        }

        private static float[] BuildBaseAlphas(int count)
        {
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = 0.56f + (i % 5) * 0.08f;
            }

            return result;
        }

        private static Sprite GetGlowFrameSprite()
        {
            if (_glowFrameSprite != null)
            {
                return _glowFrameSprite;
            }

            const int width = 96;
            const int height = 136;
            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Min(x, width - 1 - x) / (width * 0.5f);
                    float dy = Mathf.Min(y, height - 1 - y) / (height * 0.5f);
                    float edge = Mathf.Min(dx, dy);
                    float alpha = Mathf.Clamp01(1f - edge / 0.34f);
                    alpha = Mathf.Pow(alpha, 1.65f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _glowFrameSprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _glowFrameSprite.hideFlags = HideFlags.HideAndDontSave;
            return _glowFrameSprite;
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
