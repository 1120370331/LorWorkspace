using System;
using System.Collections.Generic;
using UnityEngine;
using Steria;

/// <summary>
/// 倾覆万千之流 - 蓝白色粒子浪潮向外扩散
/// </summary>
public class FarAreaEffect_Steria_OceanWave : FarAreaEffect
{
    // 时间控制
    private float _dmgTime = 0.5f;
    private float _totalTime = 2.0f;
    private float _elapsedTime = 0f;
    private bool _damageGiven = false;

    // 粒子系统
    private List<GameObject> _waveParticles = new List<GameObject>();
    private const int PARTICLE_COUNT = 24;  // 粒子数量
    private const float WAVE_SPEED = 15f;   // 扩散速度
    private const float MAX_RADIUS = 20f;   // 最大扩散半径

    // 滤镜
    private CameraFilterPack_Distortion_ShockWave _shockWaveFilter;

    // 颜色
    private readonly Color _waveColorBlue = new Color(0.3f, 0.6f, 1f, 1f);
    private readonly Color _waveColorWhite = new Color(0.9f, 0.95f, 1f, 1f);

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        this.isRunning = true;
        _elapsedTime = 0f;
        _damageGiven = false;

        // 设置位置到施法者
        if (self?.view != null)
        {
            transform.position = self.view.WorldPosition;
        }

        // 添加冲击波滤镜
        AddShockWaveFilter();

        // 创建粒子浪潮
        CreateWaveParticles();

        // 屏幕震动
        SteriaEffectHelper.AddScreenShake(0.03f, 0.02f, 60f, 0.5f);

        SteriaLogger.Log("OceanWave: Initialized - 倾覆万千之流特效开始");
    }

    private void AddShockWaveFilter()
    {
        try
        {
            var effectCam = SingletonBehavior<BattleCamManager>.Instance?.EffectCam;
            if (effectCam != null)
            {
                _shockWaveFilter = effectCam.gameObject.AddComponent<CameraFilterPack_Distortion_ShockWave>();

                // 计算屏幕位置
                Vector3 viewportPos = effectCam.WorldToViewportPoint(transform.position);
                _shockWaveFilter.PosX = viewportPos.x;
                _shockWaveFilter.PosY = viewportPos.y;
                _shockWaveFilter.Speed = 1.5f;
                _shockWaveFilter.Size = 0.5f;

                // 自动销毁
                var autoDestruct = effectCam.gameObject.AddComponent<AutoScriptDestruct>();
                autoDestruct.targetScript = _shockWaveFilter;
                autoDestruct.time = _totalTime;
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"OceanWave: ShockWave filter error: {ex.Message}");
        }
    }

    private void CreateWaveParticles()
    {
        try
        {
            Material material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.2f);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
            }

            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                float angle = (360f / PARTICLE_COUNT) * i;
                CreateSingleParticle(angle, material);
            }
        }
        catch (Exception ex)
        {
            SteriaLogger.Log($"OceanWave: CreateWaveParticles error: {ex.Message}");
        }
    }

    private void CreateSingleParticle(float angle, Material baseMaterial)
    {
        GameObject particle = SteriaEffectHelper.CreateEffectQuad(
            $"OceanWave_Particle_{angle}",
            new Material(baseMaterial),
            transform,
            Vector3.zero,
            angle,
            new Vector3(1.5f, 0.8f, 1f),
            100
        );

        if (particle != null)
        {
            // 设置初始颜色（蓝白交替）
            var renderer = particle.GetComponent<MeshRenderer>();
            if (renderer?.material != null)
            {
                Color color = (angle % 30f < 15f) ? _waveColorBlue : _waveColorWhite;
                SteriaEffectHelper.SetAdditiveMaterialAlpha(renderer.material, 1.5f, color);
            }
            _waveParticles.Add(particle);
        }
    }

    protected override void Update()
    {
        if (!isRunning) return;

        _elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / _totalTime);

        // 更新粒子位置和大小
        UpdateParticles(progress);

        // 伤害时机
        if (!_damageGiven && _elapsedTime >= _dmgTime)
        {
            _damageGiven = true;
            SteriaEffectHelper.AddScreenShake(0.04f, 0.03f, 80f, 0.3f);
        }

        // 特效结束
        if (_elapsedTime >= _totalTime)
        {
            isRunning = false;
            _isDoneEffect = true;
            CleanupParticles();
            SteriaLogger.Log("OceanWave: Effect completed");
        }

        base.Update();
    }

    private void UpdateParticles(float progress)
    {
        float currentRadius = progress * MAX_RADIUS;
        float alpha = 1.5f * (1f - progress);  // 逐渐淡出
        float scale = 1f + progress * 2f;      // 逐渐变大

        for (int i = 0; i < _waveParticles.Count; i++)
        {
            var particle = _waveParticles[i];
            if (particle == null) continue;

            float angle = (360f / PARTICLE_COUNT) * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * currentRadius;
            float z = Mathf.Sin(angle) * currentRadius * 0.3f;  // Z轴压缩

            particle.transform.localPosition = new Vector3(x, 0.5f, z);
            particle.transform.localScale = new Vector3(scale * 1.5f, scale * 0.8f, 1f);

            // 更新透明度
            var renderer = particle.GetComponent<MeshRenderer>();
            if (renderer?.material != null)
            {
                Color color = (i % 2 == 0) ? _waveColorBlue : _waveColorWhite;
                SteriaEffectHelper.SetAdditiveMaterialAlpha(renderer.material, alpha, color);
            }
        }
    }

    private void CleanupParticles()
    {
        foreach (var particle in _waveParticles)
        {
            if (particle != null)
            {
                UnityEngine.Object.Destroy(particle);
            }
        }
        _waveParticles.Clear();
    }

    private void OnDestroy()
    {
        CleanupParticles();
        SteriaLogger.Log("OceanWave: Destroyed");
    }
}
