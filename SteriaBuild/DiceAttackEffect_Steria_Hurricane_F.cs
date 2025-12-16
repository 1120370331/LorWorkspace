using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;

/// <summary>
/// 飓风聚集特效 - 在目标身上显示风暴聚集效果
/// 使用LineRenderer绘制螺旋风线，不继承基类
/// </summary>
public class DiceAttackEffect_Steria_Hurricane_F : DiceAttackEffect
{
    private const int WIND_LINE_COUNT = 8;
    private const float INITIAL_RADIUS = 3.5f;
    private const float FINAL_RADIUS = 0.2f;
    private const float ROTATION_SPEED = 720f;
    private const float DURATION = 1.5f;

    private List<LineRenderer> _windLines = new List<LineRenderer>();
    private List<float> _lineAngles = new List<float>();
    private float _progress = 0f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        this._self = self.model;

        // 挂载到目标身上
        base.transform.parent = target.atkEffectRoot;
        base.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        base.transform.localRotation = Quaternion.identity;
        base.transform.localScale = Vector3.one * 2f;

        this._destroyTime = DURATION;
        this._elapsed = 0f;

        CreateWindLines();

        SteriaLogger.Log($"Hurricane_F: Initialized with {WIND_LINE_COUNT} wind lines");
    }

    private void CreateWindLines()
    {
        for (int i = 0; i < WIND_LINE_COUNT; i++)
        {
            GameObject lineObj = new GameObject($"WindLine_{i}");
            lineObj.transform.SetParent(base.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));

            // 青色到白色渐变
            line.startColor = new Color(0.6f, 0.9f, 1f, 0.9f);
            line.endColor = new Color(1f, 1f, 1f, 0.3f);
            line.startWidth = 0.15f;
            line.endWidth = 0.05f;
            line.positionCount = 20;
            line.useWorldSpace = false;

            _windLines.Add(line);
            _lineAngles.Add((360f / WIND_LINE_COUNT) * i);
        }
    }

    protected override void Update()
    {
        this._elapsed += Time.deltaTime;
        _progress = Mathf.Clamp01(this._elapsed / this._destroyTime);

        for (int i = 0; i < _windLines.Count; i++)
        {
            UpdateWindLine(i);
        }

        if (this._elapsed >= this._destroyTime)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    private void UpdateWindLine(int index)
    {
        LineRenderer line = _windLines[index];
        if (line == null) return;

        // 更新角度
        _lineAngles[index] += ROTATION_SPEED * Time.deltaTime;
        float baseAngle = _lineAngles[index];

        // 当前半径（从外向内收缩）
        float currentRadius = Mathf.Lerp(INITIAL_RADIUS, FINAL_RADIUS, SteriaEffectHelper.EaseInQuad(_progress));

        // 绘制螺旋线
        int pointCount = line.positionCount;
        for (int j = 0; j < pointCount; j++)
        {
            float t = (float)j / (pointCount - 1);
            float spiralAngle = baseAngle + t * 180f;
            float rad = spiralAngle * Mathf.Deg2Rad;
            float r = currentRadius * (1f - t * 0.7f);
            float height = t * 1.5f - 0.5f + Mathf.Sin(t * Mathf.PI * 2f + _elapsed * 5f) * 0.2f;

            line.SetPosition(j, new Vector3(
                Mathf.Cos(rad) * r,
                height,
                Mathf.Sin(rad) * r * 0.3f
            ));
        }

        // 透明度变化
        float alpha = _progress < 0.7f
            ? Mathf.Lerp(0.3f, 1f, _progress / 0.7f)
            : Mathf.Lerp(1f, 0f, (_progress - 0.7f) / 0.3f);

        line.startColor = new Color(0.6f, 0.9f, 1f, alpha * 0.9f);
        line.endColor = new Color(1f, 1f, 1f, alpha * 0.3f);

        float widthMultiplier = 1f + (1f - _progress) * 0.5f;
        line.startWidth = 0.15f * widthMultiplier;
        line.endWidth = 0.05f * widthMultiplier;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

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
