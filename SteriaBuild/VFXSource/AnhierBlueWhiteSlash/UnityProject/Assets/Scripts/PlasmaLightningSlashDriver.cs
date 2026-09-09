using UnityEngine;

public sealed class PlasmaLightningSlashDriver : MonoBehaviour
{
    public float duration = 0.50f;
    public bool loop;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int RetreatId = Shader.PropertyToID("_Retreat");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int PhaseId = Shader.PropertyToID("_Phase");

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
            ParticleSystem particle = particles[i];
            if (particle == null)
            {
                continue;
            }

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
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

        elapsed = Mathf.Clamp(elapsed, 0f, duration);
        float revealT = Mathf.InverseLerp(0.03f, 0.15f, elapsed);
        float retreatT = Mathf.InverseLerp(0.30f, 0.50f, elapsed);
        float preFlash = Mathf.Lerp(0.16f, 1f, Smooth01(0.00f, 0.03f, elapsed));
        float reveal = Mathf.Lerp(0.015f, 1.035f, EaseOutCubic(revealT));
        float retreat = Mathf.Lerp(-0.085f, 1.035f, EaseInOutCubic(retreatT));
        float bodyDrop = Mathf.Lerp(1f, 0.62f, Smooth01(0.22f, 0.30f, elapsed));
        float fade = 1f - EaseInOutCubic(retreatT);
        float contactPeak = Mathf.Exp(-Mathf.Pow((elapsed - 0.18f) / 0.043f, 2f));
        float intensity = (1.14f + contactPeak * 0.60f) * bodyDrop;
        float alpha = preFlash * fade;
        float phase = elapsed * 5.2f;

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
            block.SetFloat(PhaseId, phase);
            renderer.SetPropertyBlock(block);
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static float Smooth01(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}
