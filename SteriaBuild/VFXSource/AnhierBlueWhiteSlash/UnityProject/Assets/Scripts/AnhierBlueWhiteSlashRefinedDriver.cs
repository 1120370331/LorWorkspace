using UnityEngine;

public sealed class AnhierBlueWhiteSlashRefinedDriver : MonoBehaviour
{
    public float duration = 0.62f;
    public bool loop = false;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int RetreatId = Shader.PropertyToID("_Retreat");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int LengthId = Shader.PropertyToID("_Length");

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

    private void ApplyAt(float elapsed)
    {
        float revealT = Mathf.InverseLerp(0.02f, 0.22f, elapsed);
        float holdT = Mathf.InverseLerp(0.16f, 0.30f, elapsed);
        float retreatT = Mathf.InverseLerp(0.38f, duration, elapsed);
        float fadeT = Mathf.InverseLerp(0.34f, duration, elapsed);

        float reveal = Mathf.Lerp(-0.05f, 1.18f, EaseOutCubic(revealT));
        float retreat = Mathf.Lerp(-0.10f, 0.82f, EaseInOutCubic(retreatT));
        float alpha = 1f - EaseInOutCubic(fadeT);
        float contact = Mathf.Sin(Mathf.Clamp01(holdT) * Mathf.PI);
        float intensity = Mathf.Lerp(1.20f, 2.75f, Mathf.Pow(Mathf.Clamp01(contact), 0.55f));
        float length = Mathf.Lerp(0.58f, 1.22f, Smooth01(0.22f, 0.55f, elapsed));

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

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
            block.SetFloat(AlphaId, alpha);
            block.SetFloat(IntensityId, intensity);
            block.SetFloat(LengthId, length);
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
