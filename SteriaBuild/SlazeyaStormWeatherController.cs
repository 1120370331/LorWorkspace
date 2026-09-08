using System;
using UnityEngine;

/// <summary>Optional weather companion; all motion reads the original visual's clock.</summary>
public sealed class SlazeyaStormWeatherController : IDisposable
{
    public const string Version = "2026-09-08.weather.1";
    public const string MaterialName = "SlazeyaStormWeatherMaterial";
    public const string ShaderName = "Steria/SlazeyaStormWeather";
    private readonly SlazeyaStormVisualController _visual;
    private readonly Vector4[] _bodies = new Vector4[16];
    private Vector4 _ellipse;
    private Vector2 _core;
    private int _bodyCount;
    private bool _cancelled, _projectionValid;
    public float CloudWeight { get; private set; }
    public float GradeWeight { get; private set; }
    public float RainWeight { get; private set; }
    public bool IsComplete { get { return _cancelled || _visual == null || _visual.IsComplete || _visual.BurstAge >= 1.20f; } }
    public float Envelope
    {
        get
        {
            if (IsComplete) return 0;
            float b = _visual.BurstAge;
            // Early callbacks fade from their captured entrance level, without a second rise.
            float g = b < 0 ? _visual.Elapsed : Mathf.Max(0, _visual.Elapsed - b);
            return Smooth(g / .32f) * (b < 0 ? 1 : 1 - Smooth((b - .30f) / .80f));
        }
    }
    public SlazeyaStormWeatherController(SlazeyaStormVisualController visual)
    { _visual = visual; SetWeights(1, 1, 1); }
    public void SetWeights(float cloud, float grade, float rain)
    { CloudWeight = Weight(cloud); GradeWeight = Weight(grade); RainWeight = Weight(rain); }
    public void SetProjection(Vector4 cloudEllipse, Vector2 coreViewport, Rect[] bodyRects)
    {
        _projectionValid = Finite(cloudEllipse.x) && Finite(cloudEllipse.y) && Finite(cloudEllipse.z) && Finite(cloudEllipse.w)
            && cloudEllipse.z > .0001f && cloudEllipse.w > .0001f && Finite(coreViewport.x) && Finite(coreViewport.y);
        _ellipse = _projectionValid ? cloudEllipse : Vector4.zero;
        _core = _projectionValid ? coreViewport : Vector2.zero;
        _bodyCount = 0;
        Array.Clear(_bodies, 0, _bodies.Length);
        if (bodyRects == null) return;
        foreach (Rect r in bodyRects)
        {
            if (!Finite(r.xMin) || !Finite(r.xMax) || !Finite(r.yMin) || !Finite(r.yMax) || r.width <= 0 || r.height <= 0) continue;
            _bodies[_bodyCount++] = new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
            if (_bodyCount == _bodies.Length) break;
        }
    }
    // Host and native preview supply the same camera that will execute the image effect.
    public void Project(Camera camera, SlazeyaStormVisualController.Footprint footprint, Bounds[] bodies)
    {
        var pose = SlazeyaStormVisualController.InitialMacroPose(footprint, 0, 0);
        Rect cloud; Vector3 core = camera != null ? camera.WorldToViewportPoint(_visual.CoreCenter) : Vector3.zero;
        bool valid = TryProjectBounds(camera, new Bounds(pose.Center, pose.Radii * 2), out cloud) && core.z > 0;
        Rect[] rects = new Rect[bodies == null ? 0 : bodies.Length];
        for (int i = 0; i < rects.Length; i++) TryProjectBounds(camera, bodies[i], out rects[i]);
        SetProjection(valid ? new Vector4(cloud.center.x, cloud.center.y, cloud.width * .5f, cloud.height * .5f) : Vector4.zero, new Vector2(core.x, core.y), rects);
    }
    public static bool TryProjectBounds(Camera camera, Bounds bounds, out Rect rect)
    {
        rect = new Rect(); if (camera == null) return false;
        Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            p = camera.WorldToViewportPoint(p);
            if (!SlazeyaStormVisualController.IsFinite(p) || p.z <= 0) return false;
            lo = Vector2.Min(lo, new Vector2(p.x, p.y)); hi = Vector2.Max(hi, new Vector2(p.x, p.y));
        }
        rect = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y); return rect.width > 0 && rect.height > 0;
    }
    public void Apply(Material material, float aspect)
    {
        if (material == null) return;
        float b = _visual == null ? -1 : _visual.BurstAge, e = Envelope;
        float g = _visual == null ? 0 : _visual.Elapsed;
        float cloud = CloudWeight * e * (b < 0 ? 1 : Mathf.Lerp(1, .55f, Smooth(b / .12f)));
        float grade = GradeWeight * e * (b < 0 ? 1 : Mathf.Lerp(1, .72f, Smooth(b / .08f)));
        float rain = RainWeight * e * Smooth(((b < 0 ? g : g - b) - .12f) / .46f) * (b < 0 ? 1 : Mathf.Lerp(1, .60f, Smooth(b / .10f)));
        material.SetVector("_Weather", new Vector4(cloud, grade, rain, g));
        material.SetVector("_CloudEllipse", _ellipse);
        material.SetVector("_Core", new Vector4(_core.x, _core.y, _projectionValid ? 1 : 0, 0));
        material.SetFloat("_Aspect", Finite(aspect) && aspect > 0 ? aspect : 16f / 9f);
        material.SetInt("_BodyCount", _bodyCount); material.SetVectorArray("_Bodies", _bodies);
    }
    public void Cancel() { _cancelled = true; }
    public void Dispose() { Cancel(); }
    private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    private static float Weight(float f) { return Finite(f) ? Mathf.Clamp01(f) : 0; }
    private static float Smooth(float f) { f = Mathf.Clamp01(f); return f * f * (3 - 2 * f); }
}
