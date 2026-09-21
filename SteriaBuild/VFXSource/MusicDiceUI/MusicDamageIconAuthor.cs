using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Steria.VisualAuthoring
{
    // Offline extension of the existing code-authored music glyph. No game-time rasterization.
    public static class MusicDamageIconAuthor
    {
        private static readonly Func<Vector2, float> GlyphDistance =
            (Func<Vector2, float>)Delegate.CreateDelegate(typeof(Func<Vector2, float>),
                typeof(MusicDiceGlyphAuthor).GetMethod("GlyphDistance", BindingFlags.Static | BindingFlags.NonPublic));
        private static readonly Vector2[] Shield = BuildShield();

        public static void Run()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                int arg = Array.IndexOf(args, "-musicDamageOutput");
                if (arg < 0) throw new ArgumentException("Missing -musicDamageOutput");
                string output = args[arg + 1];
                Directory.CreateDirectory(output);
                string[] states = { "Weak", "Vulnerable", "Normal", "Endure", "Resist" };
                for (int family = 0; family < 2; family++)
                    for (int state = 0; state < states.Length; state++)
                    {
                        Texture2D texture = Generate(family == 1, state);
                        File.WriteAllBytes(Path.Combine(output, (family == 0 ? "Hp" : "Bp") + states[state] + ".png"), texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                Debug.Log("MUSIC_DAMAGE_AUTHOR_PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static Vector2[] BuildShield()
        {
            var points = new System.Collections.Generic.List<Vector2>();
            // Broad, softly arched crown and pointed foot of the native 128px resistance badge.
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                points.Add(new Vector2(Mathf.Lerp(20f, 105f, t), 23f - 10f * Mathf.Sin(t * Mathf.PI)));
            }
            points.Add(new Vector2(102f, 83f));
            points.Add(new Vector2(64f, 113f));
            points.Add(new Vector2(21f, 83f));
            return points.ToArray();
        }

        private static float ShieldDistance(Vector2 p)
        {
            float distance = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = Shield.Length - 1; i < Shield.Length; j = i++)
            {
                Vector2 a = Shield[j], b = Shield[i], segment = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude);
                distance = Mathf.Min(distance, (p - a - t * segment).magnitude);
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside ? -distance : distance;
        }

        private static Color Over(Color under, Color color, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            float a = alpha + under.a * (1f - alpha);
            if (a <= 0f) return Color.clear;
            return new Color((color.r * alpha + under.r * under.a * (1f - alpha)) / a,
                (color.g * alpha + under.g * under.a * (1f - alpha)) / a,
                (color.b * alpha + under.b * under.a * (1f - alpha)) / a, a);
        }

        private static Color Sample(Vector2 p, bool stagger, int state)
        {
            float grain = .5f + .5f * Mathf.Sin(p.x * 2.71f + p.y * 1.63f) * Mathf.Sin(p.y * 3.13f - p.x * .73f);
            float brush = .23f * Mathf.Sin(p.x * .57f + p.y * .21f) + .13f * Mathf.Sin(p.y * 1.7f);
            float shield = ShieldDistance(p) + brush;
            Vector2 glyphPoint = (p - new Vector2(64f, 60f)) / .63f + new Vector2(50f, 50f);
            float glyph = GlyphDistance(glyphPoint) * .63f;
            float edge = Mathf.Min(Mathf.Abs(shield), Mathf.Abs(glyph) + .1f);
            Color hue = stagger ? new Color(1f, .70f, .17f) : new Color(1f, .13f, .22f);
            Color core = stagger ? new Color(1f, .97f, .71f) : new Color(1f, .91f, .92f);
            Color dark = stagger ? new Color(.13f, .081f, .02f) : new Color(.15f, .025f, .035f);
            if (state == 4)
            {
                Color result = Over(Color.clear, new Color(.015f, .015f, .018f), Mathf.Exp(-Mathf.Max(shield, 0) * Mathf.Max(shield, 0) / 36f) * .8f);
                Color fill = new Color(.19f + grain * .025f, .19f + grain * .025f, .20f + grain * .025f);
                result = Over(result, fill, Mathf.Clamp01(-shield) * .95f);
                result = Over(result, new Color(.10f, .10f, .10f), Mathf.Clamp01(-glyph));
                return Over(result, new Color(.32f, .31f, .30f), Mathf.Clamp01(2f - Mathf.Abs(shield + 2f)) * (.12f + grain * .24f));
            }
            float energy = state == 0 ? 1f : state == 1 ? .82f : state == 2 ? .60f : .17f;
            Color pixel = Over(Color.clear, new Color(.012f, .009f, .012f), Mathf.Exp(-Mathf.Max(shield, 0) * Mathf.Max(shield, 0) / 42f) * .7f);
            pixel = Over(pixel, dark, Mathf.Clamp01(-shield) * .8f);
            // Vanilla weakness flashes a pale edge; vulnerable has the saturated filled interior.
            if (state == 1) pixel = Over(pixel, stagger ? new Color(1f, .78f, .015f) : hue, Mathf.Clamp01(-shield) * .91f);
            if (state == 3)
            {
                Color muted = stagger ? new Color(.29f, .22f, .10f) : new Color(.30f, .10f, .10f);
                pixel = Over(pixel, muted, Mathf.Clamp01(-shield) * (.8f + grain * .14f));
                pixel = Over(pixel, dark * .65f, Mathf.Clamp01(-glyph));
            }
            pixel = Over(pixel, hue, Mathf.Exp(-edge * edge / (state < 2 ? 42f : 18f)) * energy * (.43f + grain * .18f));
            pixel = Over(pixel, hue, Mathf.Clamp01(4.2f - edge) * (state == 3 ? .25f : .92f) * (.8f + grain * .2f));
            float whiteEdge = Mathf.Min(Mathf.Abs(shield) / (state == 0 ? 2.4f : 1.7f), Mathf.Abs(glyph) / 1.1f);
            pixel = Over(pixel, core, Mathf.Clamp01(1.55f - whiteEdge) * (state == 3 ? .10f : .98f) * (.80f + grain * .2f));
            // A broken inner brush line makes the shield read as the game's painted badge, not a flat vector border.
            float inner = Mathf.Abs(shield + 4f);
            pixel = Over(pixel, hue, Mathf.Clamp01(1.2f - inner) * energy * (.4f + grain * .4f));
            return pixel;
        }

        private static Texture2D Generate(bool stagger, int state)
        {
            const int size = 128, samples = 2;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Color sum = Color.clear;
                for (int sy = 0; sy < samples; sy++) for (int sx = 0; sx < samples; sx++)
                {
                    Color c = Sample(new Vector2(x + (sx + .5f) / samples, y + (sy + .5f) / samples), stagger, state);
                    sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                }
                sum /= samples * samples;
                if (sum.a > 0) { sum.r /= sum.a; sum.g /= sum.a; sum.b /= sum.a; }
                pixels[(size - 1 - y) * size + x] = sum;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }
    }
}
