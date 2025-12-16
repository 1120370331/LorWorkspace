using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;

/// <summary>
/// 清司风流 - 飓风聚集特效
/// 在敌人身上显示风暴聚集效果，持续约1.5秒
/// </summary>
public class DiceAttackEffect_Steria_Hurricane_F : DiceAttackEffect
{
    // 风线数量
    private const int WIND_LINE_COUNT = 8;
    // 初始半径
    private const float INITIAL_RADIUS = 3.5f;
    // 最终半径
    private const float FINAL_RADIUS = 0.2f;
    // 旋转速度（度/秒）
    private const float ROTATION_SPEED = 720f;

    private List<LineRenderer> _windLines = new List<LineRenderer>();
    private List<float> _lineAngles = new List<float>();
    private float _progress = 0f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        // 参考原版 QoHFarAtk 的实现方式
        this._self = self.model;

        // 特效挂载到目标身上
        base.transform.parent = target.atkEffectRoot;
        base.transform.localPosition = new Vector3(0f, 1.5f, 0f); // 稍微抬高
        base.transform.localRotation = Quaternion.identity;
        base.transform.localScale = Vector3.one * 2f;

        // 设置持续时间为1.5秒
        this._destroyTime = 1.5f;
        this._elapsed = 0f;

        // 创建风线
        CreateWindLines();
    }

    private void CreateWindLines()
    {
        for (int i = 0; i < WIND_LINE_COUNT; i++)
        {
            GameObject lineObj = new GameObject("WindLine_" + i);
            lineObj.transform.SetParent(base.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();

            // 设置材质和颜色
            line.material = new Material(Shader.Find("Sprites/Default"));

            // 青色到白色的渐变，模拟风的颜色
            Color startColor = new Color(0.6f, 0.9f, 1f, 0.9f);  // 浅青色
            Color endColor = new Color(1f, 1f, 1f, 0.3f);        // 半透明白色
            line.startColor = startColor;
            line.endColor = endColor;

            // 线条宽度
            line.startWidth = 0.15f;
            line.endWidth = 0.05f;

            // 设置曲线点数
            line.positionCount = 20;
            line.useWorldSpace = false;

            _windLines.Add(line);

            // 每条线的初始角度均匀分布
            float angle = (360f / WIND_LINE_COUNT) * i;
            _lineAngles.Add(angle);
        }
    }

    protected override void Update()
    {
        this._elapsed += Time.deltaTime;

        // 计算进度 (0 -> 1)
        _progress = Mathf.Clamp01(this._elapsed / this._destroyTime);

        // 更新每条风线
        for (int i = 0; i < _windLines.Count; i++)
        {
            UpdateWindLine(i);
        }

        // 到时间后销毁
        if (this._elapsed >= this._destroyTime)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    private void UpdateWindLine(int index)
    {
        LineRenderer line = _windLines[index];
        if (line == null) return;

        // 更新角度（旋转）
        _lineAngles[index] += ROTATION_SPEED * Time.deltaTime;
        float baseAngle = _lineAngles[index];

        // 当前半径（从外向内收缩）
        float currentRadius = Mathf.Lerp(INITIAL_RADIUS, FINAL_RADIUS, EaseInQuad(_progress));

        // 绘制螺旋线
        int pointCount = line.positionCount;
        for (int j = 0; j < pointCount; j++)
        {
            float t = (float)j / (pointCount - 1);

            // 螺旋效果：角度随着点的位置增加
            float spiralAngle = baseAngle + t * 180f; // 半圈螺旋
            float rad = spiralAngle * Mathf.Deg2Rad;

            // 半径从外到内
            float r = currentRadius * (1f - t * 0.7f);

            // 添加一些垂直方向的变化，让风看起来更立体
            float height = t * 1.5f - 0.5f + Mathf.Sin(t * Mathf.PI * 2f + _elapsed * 5f) * 0.2f;

            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * r,
                height,
                Mathf.Sin(rad) * r * 0.3f // Z轴压缩，因为是2D游戏
            );

            line.SetPosition(j, pos);
        }

        // 透明度随进度变化：先增强后减弱
        float alpha;
        if (_progress < 0.7f)
        {
            alpha = Mathf.Lerp(0.3f, 1f, _progress / 0.7f);
        }
        else
        {
            alpha = Mathf.Lerp(1f, 0f, (_progress - 0.7f) / 0.3f);
        }

        Color startColor = new Color(0.6f, 0.9f, 1f, alpha * 0.9f);
        Color endColor = new Color(1f, 1f, 1f, alpha * 0.3f);
        line.startColor = startColor;
        line.endColor = endColor;

        // 线条宽度也随进度变化
        float widthMultiplier = 1f + (1f - _progress) * 0.5f;
        line.startWidth = 0.15f * widthMultiplier;
        line.endWidth = 0.05f * widthMultiplier;
    }

    // 缓动函数：加速收缩
    private float EaseInQuad(float t)
    {
        return t * t;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 清理风线
        foreach (var line in _windLines)
        {
            if (line != null && line.gameObject != null)
            {
                UnityEngine.Object.Destroy(line.gameObject);
            }
        }
        _windLines.Clear();
    }
}
