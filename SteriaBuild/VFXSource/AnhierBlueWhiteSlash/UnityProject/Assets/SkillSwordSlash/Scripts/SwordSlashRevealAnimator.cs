using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SwordSlashRevealAnimator : MonoBehaviour
{
    public float duration = 0.78f;
    public float revealDelay = 0.03f;
    public float revealDuration = 0.30f;
    public float holdDuration = 0.08f;
    public float fadeDuration = 0.34f;
    public bool loopInEditor = true;
    public bool loopInPlay = true;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private Renderer[] targetRenderers;
    private ParticleSystem[] targetParticles;
    private MaterialPropertyBlock propertyBlock;
    private float startTime;

    private void OnEnable()
    {
        RefreshTargets();
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        startTime = Time.realtimeSinceStartup;
        RestartParticles();
        ApplyAtElapsed(0f);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void Update()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            RefreshTargets();
        }

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - startTime);
        bool shouldLoop = Application.isPlaying ? loopInPlay : loopInEditor;
        if (shouldLoop && duration > 0.001f)
        {
            elapsed = Mathf.Repeat(elapsed, duration);
        }
        else
        {
            elapsed = Mathf.Min(elapsed, duration);
        }

        ApplyAtElapsed(elapsed);
#if UNITY_EDITOR
        if (!Application.isPlaying && (shouldLoop || elapsed < duration))
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void ApplyAtElapsed(float elapsed)
    {
        float revealT = Mathf.InverseLerp(revealDelay, revealDelay + revealDuration, elapsed);
        float fadeStart = revealDelay + revealDuration + holdDuration;
        float fadeT = Mathf.InverseLerp(fadeStart, fadeStart + fadeDuration, elapsed);

        float reveal = EaseOutCubic(revealT);
        float alpha = 1f - EaseInOutCubic(fadeT);
        float contact = Mathf.Sin(Mathf.Clamp01((elapsed - revealDelay) / Mathf.Max(revealDuration + holdDuration, 0.001f)) * Mathf.PI);
        float intensity = Mathf.Lerp(1.4f, 3.2f, Mathf.Pow(Mathf.Clamp01(contact), 0.65f));

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(RevealId, reveal);
            propertyBlock.SetFloat(AlphaId, alpha);
            propertyBlock.SetFloat(IntensityId, intensity);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RefreshTargets()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
        targetParticles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void RestartParticles()
    {
        if (targetParticles == null)
        {
            return;
        }

        for (int i = 0; i < targetParticles.Length; i++)
        {
            ParticleSystem ps = targetParticles[i];
            if (ps == null)
            {
                continue;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
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
}
