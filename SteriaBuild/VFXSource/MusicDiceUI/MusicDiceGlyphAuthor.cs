using UnityEngine;
namespace Steria.VisualAuthoring
{
    // Original editable glyph geometry; the default material flow uses its frozen approved v1 PNG.
    public static class MusicDiceGlyphAuthor
    {
        public static Texture2D Generate()
        {
            const int size=128;
            Color top=new Color32(226,235,240,255),bottom=new Color32(189,206,216,255);
            Color[] pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Color sum=Color.clear;
                for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++)
                {
                    float px=50f+(((x+(sx+.5f)/4f)*100f/size)-50f)/.80f;
                    float py=50f+((100f-(y+(sy+.5f)/4f)*100f/size)-50f)/.80f;
                    Color sample=GlyphDistance(new Vector2(px,py))<=0f?Color.Lerp(top,bottom,Mathf.Clamp01(py/100f)):Color.clear;
                    sum+=new Color(sample.r*sample.a,sample.g*sample.a,sample.b*sample.a,sample.a);
                }
                sum/=16f;if(sum.a>0f){sum.r/=sum.a;sum.g/=sum.a;sum.b/=sum.a;}pixels[y*size+x]=sum;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;texture.SetPixels(pixels);texture.Apply();return texture;
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
