using UnityEngine;

namespace Steria.VisualAuthoring
{
    // Music UI sprite factory: original vector geometry, independent of every attack image.
    public static class MusicDiceSpriteGenerator
    {
        private static Sprite _cardSprite;
        private static Sprite _glyphSprite;
        private static Sprite _blankFrameSprite;
        private static readonly Color GlyphColor = new Color32(215, 225, 230, 255);
        private static readonly Color FillTop = new Color32(41, 62, 75, 255);
        private static readonly Color FillBottom = new Color32(32, 49, 60, 255);
        private static readonly Color EdgeTop = new Color32(163, 189, 205, 255);
        private static readonly Color EdgeBottom = new Color32(111, 143, 163, 255);
        // Inset polygon expanded by a round 3-unit distance gives a continuous rounded hexagon.
        private static readonly Vector2[] FramePath = {
            new Vector2(50, 10.52f), new Vector2(85.72f, 30.26f), new Vector2(85.72f, 69.74f),
            new Vector2(50, 89.48f), new Vector2(14.28f, 69.74f), new Vector2(14.28f, 30.26f)
        };

        public static Sprite GetCardSprite()
        {
            return _cardSprite ?? (_cardSprite = Rasterize("SteriaCleanMusicCard", 256, true, true));
        }

        public static Sprite GetGlyphSprite()
        {
            return _glyphSprite ?? (_glyphSprite = Rasterize("SteriaOriginalMusicGlyph", 128, false, true));
        }

        // QA may inspect the complete unoccluded ground; the runtime card uses the same sampling function.
        public static Sprite GetBlankFrameSprite()
        {
            return _blankFrameSprite ?? (_blankFrameSprite = Rasterize("SteriaCleanMusicFrame", 256, true, false));
        }

        private static Sprite Rasterize(string name, int size, bool frame, bool glyph)
        {
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float px = (x + (sx + .5f) / 4f) * 100f / size;
                            float py = 100f - (y + (sy + .5f) / 4f) * 100f / size;
                            Color sample = Color.clear;
                            if (frame)
                            {
                                float distance = PolygonDistance(new Vector2(px, py), FramePath) - 3f;
                                if (distance <= 0f)
                                    sample = distance >= -3.3f
                                        ? Color.Lerp(EdgeTop, EdgeBottom, py / 100f)
                                        : Color.Lerp(FillTop, FillBottom, py / 100f);
                            }
                            // The card's center has a visible dark margin. Glyph-only keeps its full canvas.
                            float gx = frame ? (px - 20f) / .60f : px;
                            float gy = frame ? (py - 20f) / .60f : py;
                            if (glyph && GlyphDistance(new Vector2(gx, gy)) <= 0f) sample = GlyphColor;
                            sum += new Color(sample.r * sample.a, sample.g * sample.a, sample.b * sample.a, sample.a);
                        }
                    sum /= 16f;
                    if (sum.a > 0f)
                    { sum.r /= sum.a; sum.g /= sum.a; sum.b /= sum.a; }
                    pixels[y * size + x] = sum;
                }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static float PolygonDistance(Vector2 point, Vector2[] polygon)
        {
            float distance = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                distance = Mathf.Min(distance, SegmentDistance(point, polygon[j], polygon[i]));
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                        / (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
            }
            return inside ? -distance : distance;
        }

        private static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
            return (point - a - segment * t).magnitude;
        }

        private static float CurveDistance(Vector2 point, Vector2 a, Vector2 control, Vector2 b, float radius)
        {
            float distance = float.MaxValue;
            Vector2 previous = a;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                Vector2 current = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * control + t * t * b;
                distance = Mathf.Min(distance, SegmentDistance(point, previous, current));
                previous = current;
            }
            return distance - radius;
        }

        private static float EllipseDistance(Vector2 point, Vector2 center, Vector2 radius, float angle)
        {
            Vector2 delta = point - center;
            float u = delta.x * Mathf.Cos(angle) + delta.y * Mathf.Sin(angle);
            float v = -delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle);
            float k0 = Mathf.Sqrt(u * u / (radius.x * radius.x) + v * v / (radius.y * radius.y));
            float k1 = Mathf.Sqrt(u * u / Mathf.Pow(radius.x, 4) + v * v / Mathf.Pow(radius.y, 4));
            return k1 < .0001f ? -Mathf.Min(radius.x, radius.y) : k0 * (k0 - 1f) / k1;
        }

        private static float SmoothUnion(float a, float b)
        {
            const float rounding = 2f;
            float h = Mathf.Clamp01(.5f + .5f * (b - a) / rounding);
            return Mathf.Lerp(b, a, h) - rounding * h * (1f - h);
        }

        private static float GlyphDistance(Vector2 point)
        {
            float leftHead = EllipseDistance(point, new Vector2(29, 78), new Vector2(16, 10.5f), -.35f);
            float rightHead = EllipseDistance(point, new Vector2(73, 67), new Vector2(14.5f, 10), -.26f);
            float leftStem = CurveDistance(point, new Vector2(40, 77), new Vector2(39, 49), new Vector2(40, 29), 5.8f);
            float rightStem = CurveDistance(point, new Vector2(83, 66), new Vector2(82, 43), new Vector2(83, 17), 5.8f);
            float beam = CurveDistance(point, new Vector2(40, 29), new Vector2(61, 22), new Vector2(83, 17), 5.8f);
            float stems = SmoothUnion(leftStem, rightStem);
            return SmoothUnion(SmoothUnion(leftHead, rightHead), SmoothUnion(stems, beam));
        }
    }

}
