// CPU host for the unchanged Unity authoring algorithm. PNG encoding only; no GPU/game claims.
using System;
using System.Drawing;
using System.IO;
public static class AuthorHost
{
    public static void Main() { Steria.VisualAuthoring.MusicDamageIconAuthor.Run(); }
}
namespace UnityEditor { public static class EditorApplication { public static void Exit(int code) { Environment.Exit(code); } } }
namespace UnityEngine
{
    public class Object { public string name; public static void DestroyImmediate(Object value) { } public static void Destroy(Object value) { } }
    public static class Debug
    {
        public static void Log(object text) { Console.WriteLine(text); }
        public static void LogError(object text) { Console.Error.WriteLine(text); }
        public static void LogException(Exception error) { Console.Error.WriteLine(error); }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float n) { return new Vector2(a.x * n, a.y * n); }
        public static Vector2 operator *(float n, Vector2 a) { return a * n; }
        public static Vector2 operator /(Vector2 a, float n) { return a * (1f / n); }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color operator +(Color x, Color y) { return new Color(x.r + y.r, x.g + y.g, x.b + y.b, x.a + y.a); }
        public static Color operator *(Color x, float n) { return new Color(x.r * n, x.g * n, x.b * n, x.a * n); }
        public static Color operator /(Color x, float n) { return x * (1f / n); }
        public static bool operator ==(Color x, Color y) { return x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a; }
        public static bool operator !=(Color x, Color y) { return !(x == y); }
        public override bool Equals(object o) { return o is Color && this == (Color)o; }
        public override int GetHashCode() { return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode(); }
        public static Color Lerp(Color x, Color y, float t) { return x * (1f - Mathf.Clamp01(t)) + y * Mathf.Clamp01(t); }
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Abs(float n) { return Math.Abs(n); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static float Clamp01(float n) { return Math.Max(0, Math.Min(1, n)); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Sin(float n) { return (float)Math.Sin(n); }
        public static float Cos(float n) { return (float)Math.Cos(n); }
        public static float Sqrt(float n) { return (float)Math.Sqrt(n); }
        public static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
        public static float Exp(float n) { return (float)Math.Exp(n); }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Clamp }
    public class Texture2D : Object
    {
        public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode; public Color[] pixels;
        public Texture2D(int w, int h, TextureFormat format, bool mipmap) { width = w; height = h; }
        public void SetPixels(Color[] values) { pixels = values; }
        public void Apply() { }
        public byte[] EncodeToPNG()
        {
            using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var stream = new MemoryStream())
            {
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    Color c = pixels[(height - 1 - y) * width + x];
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(Byte(c.a), Byte(c.r), Byte(c.g), Byte(c.b)));
                }
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); return stream.ToArray();
            }
        }
        private static int Byte(float v) { return (int)Math.Round(Mathf.Clamp01(v) * 255f); }
    }
}
