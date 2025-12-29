using System;
using System.Collections.Generic;
using UnityEngine;
using Steria;

/// <summary>
/// 倾覆万千之流 - 原神水系风格极致重制 (Genshin Hydro Burst Ultimate)
/// 核心理念：拒绝几何体，使用 "Slash" 贴图构建有机的水流形态 (Lotus/Whirlpool)
/// </summary>
public class FarAreaEffect_Steria_OceanWave : FarAreaEffect
{
    // 时间轴
    private const float TIME_GATHER = 0.5f;     // 聚怪/漩涡时间
    private const float TIME_BURST = 0.5f;      // 爆发时刻
    private const float TOTAL_DURATION = 3.5f;  // 总时长

    private float _elapsed = 0f;
    private bool _hasBurst = false;

    // 资源引用
    private List<ParticleSystem> _systems = new List<ParticleSystem>();
    private CameraFilterPack_Distortion_ShockWave _shockWave;

    // 极致水系色板 (Genshin Style)
    // 核心：极亮的青白色 (HDR感)
    private readonly Color _hydroBright = new Color(0.4f, 1.0f, 1.0f, 1f); 
    // 中层：高饱和海蓝
    private readonly Color _hydroMid = new Color(0.0f, 0.6f, 1.0f, 1f);
    // 深层：深邃蓝紫 (增加体积感)
    private readonly Color _hydroDeep = new Color(0.1f, 0.2f, 0.7f, 1f);

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        this.isRunning = true;
        _elapsed = 0f;
        _hasBurst = false;

        if (self?.view != null)
        {
            transform.position = self.view.WorldPosition;
        }

        GameObject root = new GameObject("Steria_Hydro_Ultimate");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        // 1. 漩涡聚气 (Whirlpool) - 使用 Slash 贴图构建旋转水流
        CreateHydroWhirlpool(root);

        // 2. 爆发：水之莲华 (Hydro Lotus) - 巨大的绽放效果
        CreateHydroLotus(root);

        // 3. 爆发：高速水激流 (Jet Streams)
        CreateHydroJets(root);

        // 4. 爆发：泡沫炸裂 (Foam Burst)
        CreateHydroFoam(root);

        // 5. 环境：地面法阵 (Ground Sigil)
        CreateGroundSigil(root);

        SteriaLogger.Log("OceanWave: Genshin Ultimate Style Initialized");
    }

    /// <summary>
    /// 漩涡：使用弯曲的 Slash 贴图旋转汇聚
    /// </summary>
    private void CreateHydroWhirlpool(GameObject parent)
    {
        var go = CreateParticleObject(parent, "Whirlpool");
        var ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = TIME_GATHER;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f; // 由旋转驱动
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startColor = _hydroBright;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = ps.emission;
        emission.rateOverTime = 60;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 8f;
        shape.radiusThickness = 0f;

        // 关键：速度与旋转
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial = -20f; // 向心吸入
        // vel.orbital 在旧版Unity中不可用，使用旋转模块代替

        // 关键：使用 Slash 贴图并旋转，使其看起来像水流
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.1f);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // 让粒子朝向旋转方向
        var rot = ps.rotationBySpeed;
        rot.enabled = true;
        rot.range = new Vector2(0.8f, 1f); // 根据速度调整角度

        // 颜色渐变
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(_hydroMid, 0f), new GradientColorKey(_hydroBright, 1f) }
        };
    }

    /// <summary>
    /// 水之莲华：爆发时产生的巨大的、花瓣状的水幕
    /// </summary>
    private void CreateHydroLotus(GameObject parent)
    {
        var go = CreateParticleObject(parent, "HydroLotus");
        var ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.startDelay = TIME_BURST;
        main.startLifetime = 0.6f;
        main.startSpeed = 5f; 
        main.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startColor = _hydroMid;
        main.startRotation3D = true; // 开启3D旋转

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) }); // 16瓣莲花

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;
        shape.arc = 360f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.1f);
        renderer.renderMode = ParticleSystemRenderMode.Billboard; // 使用 Billboard 配合 3D Rotation

        // 关键：通过 Initial Rotation 让 Slash 贴图竖起来并围成一圈
        // 这里需要随机化一点，让它看起来有机
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = 0.5f; // 随着时间轻微旋转

        // 尺寸随时间变大
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.2f);
        curve.AddKey(0.4f, 1.0f); // 快速展开
        curve.AddKey(1.0f, 1.2f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(_hydroBright, 0f), new GradientColorKey(_hydroDeep, 1f) }
        };
    }

    /// <summary>
    /// 高速水激流：向四周射出的尖锐水刺 (拉伸渲染)
    /// </summary>
    private void CreateHydroJets(GameObject parent)
    {
        var go = CreateParticleObject(parent, "HydroJets");
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0); // 平铺
        var ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.startDelay = TIME_BURST;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 35f); // 极快
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startColor = _hydroBright;
        
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        // 关键：拉伸模式
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.cameraVelocityScale = 0f;
        renderer.velocityScale = 0.15f; // 拉伸程度
        renderer.lengthScale = 4f; // 基础长度
        renderer.material = SteriaEffectSprites.GetEffectMaterial("water_slash", true, 0.15f); // 使用 Slash 增加锐利感

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(_hydroBright, 0f), new GradientColorKey(_hydroMid, 1f) }
        };
    }

    /// <summary>
    /// 泡沫炸裂：大量细小的水珠，增加丰富度
    /// </summary>
    private void CreateHydroFoam(GameObject parent)
    {
        var go = CreateParticleObject(parent, "HydroFoam");
        var ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.startDelay = TIME_BURST;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(1f, 1f, 1f, 0.8f); // 纯白泡沫
        main.gravityModifier = 0.5f; // 受重力影响

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SteriaEffectSprites.GetEffectMaterial("water_hit", true, 0.05f); // 圆点

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.0f; // 湍流运动

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(_hydroBright, 1f) }
        };
    }

    /// <summary>
    /// 地面法阵：巨大的、旋转的水纹，作为背景
    /// </summary>
    private void CreateGroundSigil(GameObject parent)
    {
        var go = CreateParticleObject(parent, "GroundSigil");
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var ps = go.GetComponent<ParticleSystem>();

        var main = ps.main;
        main.startDelay = TIME_BURST - 0.1f; // 稍微提前一点
        main.startLifetime = 2.0f;
        main.startSize = 25f; // 覆盖全场
        main.startColor = _hydroDeep;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SteriaEffectSprites.GetEffectMaterial("water_surround", true, 0.1f);

        // 旋转
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = 0.2f;

        // 颜色：从亮到暗
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.1f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(_hydroMid, 0f), new GradientColorKey(_hydroDeep, 1f) }
        };
    }

    // --- Core System ---

    private GameObject CreateParticleObject(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        _systems.Add(ps);
        var main = ps.main;
        main.playOnAwake = true;
        return go;
    }

    private void AddBurstShockWave()
    {
        if (SingletonBehavior<BattleCamManager>.Instance?.EffectCam == null) return;
        
        var cam = SingletonBehavior<BattleCamManager>.Instance.EffectCam;
        _shockWave = cam.gameObject.AddComponent<CameraFilterPack_Distortion_ShockWave>();
        
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        _shockWave.PosX = viewportPos.x;
        _shockWave.PosY = viewportPos.y;
        _shockWave.Size = 0f;
        _shockWave.Speed = 3.0f; 
        
        var autoDestruct = cam.gameObject.AddComponent<AutoScriptDestruct>();
        autoDestruct.targetScript = _shockWave;
        autoDestruct.time = 0.8f;
    }

    protected override void Update()
    {
        if (!isRunning) return;
        _elapsed += Time.deltaTime;

        if (!_hasBurst && _elapsed >= TIME_BURST)
        {
            _hasBurst = true;
            SteriaEffectHelper.AddScreenShake(0.1f, 0.1f, 90f, 0.4f); // 强烈的瞬间震动
            AddBurstShockWave();
        }

        if (_shockWave != null)
        {
            _shockWave.Size += Time.deltaTime * 2f;
        }

        if (_elapsed >= TOTAL_DURATION)
        {
            isRunning = false;
            _isDoneEffect = true;
            Destroy(gameObject);
        }

        base.Update();
    }

    private void OnDestroy()
    {
        foreach (var ps in _systems)
        {
            if (ps != null) Destroy(ps.gameObject);
        }
        _systems.Clear();
    }
}