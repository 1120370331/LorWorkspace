using System;
using System.Collections.Generic;
using UnityEngine;
using Steria;

/// <summary>
/// 倾覆万千之流 - 海洋波浪特效组件
/// 三层蓝色波浪向外扩散，带波动美感
/// </summary>
public class OceanWaveEffectComponent : MonoBehaviour
{
    private float _duration = 0.8f;  // 更快
    private float _elapsed = 0f;
    private BattleUnitModel _owner;

    // 三层粒子
    private List<GameObject> _mainWave = new List<GameObject>();
    private List<GameObject> _innerWave = new List<GameObject>();
    private List<GameObject> _outerWave = new List<GameObject>();

    private const int PARTICLE_COUNT = 48;  // 更多粒子
    private const float MAX_RADIUS = 35f;   // 更大范围

    // 海蓝色系
    private readonly Color _deepBlue = new Color(0.1f, 0.5f, 1f, 1f);
    private readonly Color _lightBlue = new Color(0.4f, 0.8f, 1f, 1f);
    private readonly Color _cyan = new Color(0.2f, 0.9f, 1f, 1f);
    private readonly Color _foam = new Color(0.9f, 1f, 1f, 1f);

    public void Init(BattleUnitModel owner)
    {
        _owner = owner;
        _elapsed = 0f;

        CreateMainWave();
        CreateInnerWave();
        CreateOuterWave();
        AddShockWave();

        SteriaLogger.Log("OceanWaveEffect: Started");
    }

    private void AddShockWave()
    {
        try
        {
            var cam = SingletonBehavior<BattleCamManager>.Instance?.EffectCam;
            if (cam != null)
            {
                var filter = cam.gameObject.AddComponent<CameraFilterPack_Distortion_ShockWave>();
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                filter.PosX = vp.x;
                filter.PosY = vp.y;
                filter.Speed = 1.5f;
                filter.Size = 0.5f;

                var auto = cam.gameObject.AddComponent<AutoScriptDestruct>();
                auto.targetScript = filter;
                auto.time = _duration;
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"OceanWave ShockWave error: {ex.Message}");
        }
    }

    private void CreateMainWave()
    {
        // 使用纯色材质，不用贴图
        Material mat = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            float angle = (360f / PARTICLE_COUNT) * i;

            // 创建纯色矩形粒子
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Quad);
            p.name = $"Main_{i}";
            p.transform.parent = transform;
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            p.transform.localScale = new Vector3(2f, 0.8f, 1f);

            var r = p.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.material = new Material(mat);
                r.material.color = (i % 2 == 0) ? _deepBlue : _lightBlue;
                r.sortingOrder = 100;
            }
            _mainWave.Add(p);
        }
    }

    private void CreateInnerWave()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));

        int count = PARTICLE_COUNT / 2;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + 7.5f;

            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Quad);
            p.name = $"Inner_{i}";
            p.transform.parent = transform;
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            p.transform.localScale = new Vector3(1.5f, 0.5f, 1f);

            var r = p.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.material = new Material(mat);
                r.material.color = _cyan;
                r.sortingOrder = 110;
            }
            _innerWave.Add(p);
        }
    }

    private void CreateOuterWave()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            float angle = (360f / PARTICLE_COUNT) * i + 5f;

            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Quad);
            p.name = $"Outer_{i}";
            p.transform.parent = transform;
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            p.transform.localScale = new Vector3(1f, 0.3f, 1f);

            var r = p.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.material = new Material(mat);
                r.material.color = _foam;
                r.sortingOrder = 90;
            }
            _outerWave.Add(p);
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        float time = _elapsed;

        UpdateWaves(progress, time);

        if (_elapsed >= _duration)
        {
            Cleanup();
            Destroy(gameObject);
        }
    }

    private void UpdateWaves(float progress, float time)
    {
        float radius = progress * MAX_RADIUS;
        float alpha = 1f - progress;  // 淡出
        float scale = 1f + progress * 1.5f;

        // 主层 - 带波动
        for (int i = 0; i < _mainWave.Count; i++)
        {
            var p = _mainWave[i];
            if (p == null) continue;

            float baseAngle = (360f / PARTICLE_COUNT) * i * Mathf.Deg2Rad;
            float rWave = Mathf.Sin(time * 6f + i * 0.4f) * 1.5f;
            float aWave = Mathf.Sin(time * 4f + i * 0.2f) * 0.1f;
            float hWave = Mathf.Sin(time * 8f + i * 0.6f) * 0.5f;

            float r = radius + rWave;
            float a = baseAngle + aWave;
            float x = Mathf.Cos(a) * r;
            float z = Mathf.Sin(a) * r * 0.3f;

            p.transform.localPosition = new Vector3(x, 0.5f + hWave, z);
            float sWave = 1f + Mathf.Sin(time * 5f + i) * 0.2f;
            p.transform.localScale = new Vector3(scale * 1.5f * sWave, scale * 0.6f * sWave, 1f);

            var rend = p.GetComponent<MeshRenderer>();
            if (rend?.material != null)
            {
                Color c = (i % 2 == 0) ? _deepBlue : _lightBlue;
                c.a = alpha;
                rend.material.color = c;
            }
        }

        // 内层 - 更快
        float innerR = progress * MAX_RADIUS * 0.8f;
        int innerCount = PARTICLE_COUNT / 2;
        for (int i = 0; i < _innerWave.Count; i++)
        {
            var p = _innerWave[i];
            if (p == null) continue;

            float baseAngle = ((360f / innerCount) * i + 7.5f) * Mathf.Deg2Rad;
            float rWave = Mathf.Sin(time * 7f + i * 0.5f) * 1f;
            float aWave = Mathf.Cos(time * 5f + i * 0.3f) * 0.15f;
            float hWave = Mathf.Cos(time * 9f + i) * 0.4f;

            float r = innerR + rWave;
            float a = baseAngle + aWave;
            float x = Mathf.Cos(a) * r;
            float z = Mathf.Sin(a) * r * 0.3f;

            p.transform.localPosition = new Vector3(x, 0.3f + hWave, z);
            float sWave = 1f + Mathf.Sin(time * 6f + i * 0.8f) * 0.2f;
            p.transform.localScale = new Vector3(scale * 1.2f * sWave, scale * 0.4f * sWave, 1f);

            var rend = p.GetComponent<MeshRenderer>();
            if (rend?.material != null)
            {
                Color c = _cyan;
                c.a = alpha * 0.9f;
                rend.material.color = c;
            }
        }

        // 外层 - 最快
        float outerR = progress * MAX_RADIUS * 1.3f;
        for (int i = 0; i < _outerWave.Count; i++)
        {
            var p = _outerWave[i];
            if (p == null) continue;

            float baseAngle = ((360f / PARTICLE_COUNT) * i + 5f) * Mathf.Deg2Rad;
            float rWave = Mathf.Sin(time * 5f + i * 0.6f) * 2f;
            float aWave = Mathf.Sin(time * 3f + i * 0.4f) * 0.2f;
            float hWave = Mathf.Sin(time * 10f + i * 0.8f) * 0.4f;

            float r = outerR + rWave;
            float a = baseAngle + aWave;
            float x = Mathf.Cos(a) * r;
            float z = Mathf.Sin(a) * r * 0.3f;

            p.transform.localPosition = new Vector3(x, 0.7f + hWave, z);
            float sWave = 1f + Mathf.Cos(time * 4f + i * 0.5f) * 0.3f;
            p.transform.localScale = new Vector3(scale * 0.8f * sWave, scale * 0.3f * sWave, 1f);

            var rend = p.GetComponent<MeshRenderer>();
            if (rend?.material != null)
            {
                Color c = _foam;
                c.a = alpha * 0.7f;
                rend.material.color = c;
            }
        }
    }

    private void Cleanup()
    {
        foreach (var p in _mainWave) if (p != null) Destroy(p);
        foreach (var p in _innerWave) if (p != null) Destroy(p);
        foreach (var p in _outerWave) if (p != null) Destroy(p);
        _mainWave.Clear();
        _innerWave.Clear();
        _outerWave.Clear();
    }

    private void OnDestroy()
    {
        Cleanup();
        SteriaLogger.Log("OceanWaveEffect: Destroyed");
    }
}
