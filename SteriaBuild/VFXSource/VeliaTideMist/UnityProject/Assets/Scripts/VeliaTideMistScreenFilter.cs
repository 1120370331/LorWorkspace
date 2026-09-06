using UnityEngine;

/// <summary>Appended to EffectCam's existing image-effect chain. Owns only its cloned material.</summary>
[ExecuteInEditMode]
public sealed class VeliaTideMistScreenFilter : MonoBehaviour
{
    private VeliaTideMistVisualController _driver;
    private Material _material;
    public bool IsInitialized { get { return _driver != null && _material != null; } }

    public void Initialize(VeliaTideMistVisualController driver, Material materialTemplate)
    {
        Release();
        if (driver == null || materialTemplate == null || materialTemplate.shader == null || !materialTemplate.shader.isSupported) return;
        _material = new Material(materialTemplate) { name = "VeliaTideMist owned instance", hideFlags = HideFlags.HideAndDontSave };
        _driver = driver;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_driver == null || _material == null || _driver.IsComplete || _driver.Envelope <= 0f)
        {
            Graphics.Blit(source, destination);
            return;
        }
        _driver.Apply(_material, (float)source.width / source.height);
        // No source blur, temporary camera, shared filter edits or clock work in this callback.
        Graphics.Blit(source, destination, _material, 0);
    }

    public void Release()
    {
        _driver = null;
        if (_material == null) return;
        Material owned = _material;
        _material = null;
        if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
    }

    private void OnDisable()
    {
        if (_driver != null) _driver.Cancel();
        Release();
    }
    private void OnDestroy() { Release(); }
}
