using UnityEngine;

/// <summary>Appended image effect. Owns one cloned material and never advances simulation.</summary>
[ExecuteInEditMode]
public sealed class SlazeyaStormWeatherScreenFilter : MonoBehaviour
{
    private SlazeyaStormWeatherController _driver;
    private Material _material;
    public bool IsInitialized { get { return _driver != null && _material != null; } }
    public void Initialize(SlazeyaStormWeatherController driver, Material template)
    {
        Release();
        if (driver == null || template == null || template.shader == null || !template.shader.isSupported) return;
        _material = new Material(template) { name = "Slazeya weather owned instance", hideFlags = HideFlags.HideAndDontSave };
        _driver = driver;
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_driver == null || _material == null || _driver.IsComplete || _driver.Envelope <= 0)
        { Graphics.Blit(source, destination); return; }
        _driver.Apply(_material, (float)source.width / source.height);
        Graphics.Blit(source, destination, _material, 0);
    }
    public void Release()
    {
        _driver = null;
        if (_material == null) return;
        Material owned = _material; _material = null;
        if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
    }
    private void OnDisable() { if (_driver != null) _driver.Cancel(); Release(); }
    private void OnDestroy() { Release(); }
}
