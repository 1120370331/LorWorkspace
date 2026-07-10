using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ChristashaDawnCoreOnlyAnimator : MonoBehaviour
{
    public float duration = 0.84f;
    public float revealStartDelay = 0.02f;
    public float revealDuration = 0.34f;
    public float holdDuration = 0.14f;
    public float retreatDuration = 0.34f;
    public bool loopInEditor = true;
    public bool loopInPlay = true;

    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int RetreatId = Shader.PropertyToID("_Retreat");
    private static readonly int EmissionBoostId = Shader.PropertyToID("_EmissionBoost");

    private MaterialPropertyBlock propertyBlock;
    private Renderer[] targetRenderers;
    private float startTime;

    private void OnEnable()
    {
        RefreshRenderers();
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        startTime = Time.realtimeSinceStartup;
        ApplyAtElapsed(0f);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void OnDisable()
    {
        RefreshRenderers();
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }

    private void Update()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            RefreshRenderers();
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - startTime);
        if (Application.isPlaying && loopInPlay && duration > 0.001f)
        {
            elapsed = Mathf.Repeat(elapsed, duration);
        }
#if UNITY_EDITOR
        if (!Application.isPlaying && loopInEditor && duration > 0.001f)
        {
            elapsed = Mathf.Repeat(elapsed, duration);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        else if (!Application.isPlaying)
        {
            elapsed = Mathf.Min(elapsed, duration);
            if (elapsed < duration)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
#endif
        ApplyAtElapsed(elapsed);

        if (Application.isPlaying && !loopInPlay && elapsed > duration)
        {
            enabled = false;
        }
    }

    private void ApplyAtElapsed(float elapsed)
    {
        float revealT = Mathf.InverseLerp(revealStartDelay, revealStartDelay + revealDuration, elapsed);
        float holdEnd = revealStartDelay + revealDuration + holdDuration;
        float retreatT = Mathf.InverseLerp(holdEnd, holdEnd + retreatDuration, elapsed);

        float reveal = EaseOutCubic(revealT);
        float retreat = retreatT <= 0f ? -0.08f : EaseInOutCubic(retreatT);
        float peak = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
        float emission = Mathf.Lerp(1.02f, 1.16f, Mathf.Pow(Mathf.Clamp01(peak), 0.72f));

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(RevealId, reveal);
            propertyBlock.SetFloat(RetreatId, retreat);
            propertyBlock.SetFloat(EmissionBoostId, emission);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RefreshRenderers()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
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
