using UnityEngine;

public sealed class OceanBladeSlashUsableDriver : MonoBehaviour
{
    public float duration = 0.72f;
    public bool loop;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int RetreatId = Shader.PropertyToID("_Retreat");
    private static readonly int LengthId = Shader.PropertyToID("_Length");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int DistortId = Shader.PropertyToID("_DistortAmount");

    private Renderer[] renderers;
    private ParticleSystem[] particles;
    private MaterialPropertyBlock block;
    private float startTime;

    private void Awake()
    {
        RefreshTargets();
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        RefreshTargets();
        startTime = Time.time;
        RestartParticles();
        ApplyAt(0f);
    }

    private void Update()
    {
        float elapsed = Mathf.Max(0f, Time.time - startTime);
        if (loop && duration > 0.001f)
        {
            elapsed = Mathf.Repeat(elapsed, duration);
        }
        else
        {
            elapsed = Mathf.Min(elapsed, duration);
        }

        ApplyAt(elapsed);
    }

    private void RefreshTargets()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void RestartParticles()
    {
        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem ps = particles[i];
            if (ps == null)
            {
                continue;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    public void ApplyAt(float elapsed)
    {
        if (renderers == null)
        {
            RefreshTargets();
        }
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        float revealT = Mathf.InverseLerp(0.015f, 0.24f, elapsed);
        float retreatT = Mathf.InverseLerp(0.38f, duration, elapsed);
        float fadeT = Mathf.InverseLerp(0.44f, duration, elapsed);
        float impactT = Mathf.InverseLerp(0.12f, 0.31f, elapsed);

        float reveal = Mathf.Lerp(-0.08f, 1.20f, EaseOutCubic(revealT));
        float retreat = Mathf.Lerp(-0.16f, 0.96f, EaseInOutCubic(retreatT));
        float length = Mathf.Lerp(0.42f, 1.18f, Smooth01(0.10f, 0.30f, elapsed));
        float alpha = 1f - EaseInOutCubic(fadeT);
        float impact = Mathf.Sin(Mathf.Clamp01(impactT) * Mathf.PI);
        float intensity = Mathf.Lerp(1.35f, 3.75f, Mathf.Pow(Mathf.Clamp01(impact), 0.42f));
        float distort = Mathf.Lerp(0.025f, 0.090f, Mathf.Clamp01(impact)) * alpha;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetFloat(RevealId, reveal);
            block.SetFloat(RetreatId, retreat);
            block.SetFloat(LengthId, length);
            block.SetFloat(AlphaId, alpha);
            block.SetFloat(IntensityId, intensity);
            block.SetFloat(DistortId, distort);
            renderer.SetPropertyBlock(block);
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static float Smooth01(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}
