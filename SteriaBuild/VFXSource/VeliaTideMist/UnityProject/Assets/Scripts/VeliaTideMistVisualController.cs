using System;
using UnityEngine;

/// <summary>Pure Unity event driver. One reveal per instance; no game settlement or render clock.</summary>
public sealed class VeliaTideMistVisualController
{
    public const string BundleName = "steria_velia_tide_mist";
    public const string MaterialName = "VeliaTideMistMaterial";
    public const float IntroDuration = 0.32f;
    public const float FadeDuration = 0.40f;
    public const int MaxPoints = 16;
    public const int MaxProtectionRects = 16;

    private readonly Vector4[] _points = new Vector4[MaxPoints];
    private readonly Vector4[] _rects = new Vector4[MaxProtectionRects];
    private int _pointCount, _rectCount, _ordinal = -1;
    private float _elapsed, _pulseAge, _fadeAge, _finishEnvelope;
    private bool _fired, _finishing, _complete;

    public float Elapsed { get { return _elapsed; } }
    public int DiceOrdinal { get { return _ordinal; } }
    public int PulseCount { get; private set; }
    public int SuccessfulPointCount { get { return _pointCount; } }
    public bool IsMirrored { get; private set; }
    public bool DiceReady { get { return _ordinal >= 0 && (_complete || _elapsed >= IntroDuration); } }
    public bool PulseFinished { get { return _fired && _pulseAge >= PulseDuration; } }
    public bool IsComplete { get { return _complete; } }
    public bool IsFinishing { get { return _finishing; } }
    public float PulseDuration { get { return _ordinal > 0 ? 0.77f : 0.72f; } }
    public float Envelope
    {
        get
        {
            if (_complete || _ordinal < 0) return 0f;
            return _finishing ? _finishEnvelope * (1f - Smooth(_fadeAge / FadeDuration)) : Smooth(_elapsed / IntroDuration);
        }
    }
    public float Pulse
    {
        get
        {
            if (!_fired || _complete) return 0f;
            // Preserve r7's exact first .18s, hold that light for .30s, then extend
            // the remaining original fall by .15s. The real callback clock keeps running.
            float baseDuration = _ordinal > 0 ? 0.32f : 0.27f;
            float age = _pulseAge;
            if (age > 0.18f)
                age = age <= 0.48f ? 0.18f : 0.18f + (age - 0.48f) * (baseDuration - 0.18f) / (PulseDuration - 0.48f);
            if (age < 0.04f) return Smooth(age / 0.04f);
            if (age <= 0.07f) return 1f;
            return 1f - Smooth((age - 0.07f) / (baseDuration - 0.07f));
        }
    }

    public void SetMirrored(bool mirrored) { IsMirrored = mirrored; }

    // Zero-based ordinal; duplicate BeginDice and callback calls cannot create another pulse.
    public void BeginDice(int ordinal)
    {
        if (_complete || _finishing || ordinal < 0 || ordinal <= _ordinal) return;
        _ordinal = ordinal;
        _fired = false;
        _pulseAge = 0f;
        _pointCount = 0;
        Array.Clear(_points, 0, _points.Length);
    }

    public void TriggerPulse(Vector2[] successfulViewportPoints)
    {
        if (_ordinal < 0 || _fired || _finishing || _complete) return;
        _fired = true;
        _pulseAge = 0f;
        PulseCount++;
        _pointCount = 0;
        if (successfulViewportPoints == null) return;
        foreach (Vector2 p in successfulViewportPoints)
        {
            if (!Finite(p.x) || !Finite(p.y) || p.x < 0f || p.x > 1f || p.y < 0f || p.y > 1f) continue;
            bool duplicate = false;
            for (int i = 0; i < _pointCount; i++)
                if (Vector2.SqrMagnitude(p - new Vector2(_points[i].x, _points[i].y)) < 0.000001f) duplicate = true;
            if (!duplicate) _points[_pointCount++] = new Vector4(p.x, p.y, 0f, 0f);
            if (_pointCount == MaxPoints) break;
        }
    }

    public void SetProtectionRects(Rect[] viewportRects)
    {
        _rectCount = 0;
        Array.Clear(_rects, 0, _rects.Length);
        if (viewportRects == null) return;
        foreach (Rect rect in viewportRects)
        {
            if (!Finite(rect.xMin) || !Finite(rect.yMin) || !Finite(rect.xMax) || !Finite(rect.yMax)) continue;
            if (rect.width <= 0f || rect.height <= 0f || rect.xMax < 0f || rect.xMin > 1f || rect.yMax < 0f || rect.yMin > 1f) continue;
            _rects[_rectCount++] = new Vector4(Mathf.Clamp01(rect.xMin), Mathf.Clamp01(rect.yMin), Mathf.Clamp01(rect.xMax), Mathf.Clamp01(rect.yMax));
            if (_rectCount == MaxProtectionRects) break;
        }
    }

    public void Finish()
    {
        if (_complete || _finishing) return;
        _finishEnvelope = Envelope;
        _finishing = true;
        _fadeAge = 0f;
    }

    public void Cancel()
    {
        _complete = true;
        _pointCount = 0;
    }

    public void Advance(float dt)
    {
        if (_complete || _ordinal < 0 || !Finite(dt) || dt <= 0f) return;
        _elapsed += dt;
        if (_fired) _pulseAge += dt;
        if (_finishing)
        {
            _fadeAge += dt;
            if (_fadeAge >= FadeDuration) Cancel();
        }
    }

    public void Apply(Material material, float aspect)
    {
        if (material == null) return;
        material.SetVector("_State", new Vector4(Envelope, Pulse, _elapsed, _ordinal > 0 ? 1f : 0f));
        material.SetFloat("_PulseAge", _fired && !_complete ? _pulseAge : -1f);
        material.SetFloat("_SunReveal", Smooth((_elapsed - 0.07f) / (IntroDuration - 0.07f)));
        material.SetFloat("_HitStrength", _fired && !_complete ? (1f - Smooth(_pulseAge / 0.14f)) * Envelope : 0f);
        material.SetFloat("_Aspect", Finite(aspect) && aspect > 0f ? aspect : 16f / 9f);
        material.SetFloat("_MirrorX", IsMirrored ? 1f : 0f);
        material.SetInt("_PointCount", _pointCount);
        material.SetVectorArray("_HitPoints", _points);
        material.SetInt("_ProtectionCount", _rectCount);
        material.SetVectorArray("_ProtectionRects", _rects);
    }

    private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
}
