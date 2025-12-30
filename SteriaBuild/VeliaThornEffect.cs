using UnityEngine;
using Battle.DiceAttackEffect;

/// <summary>
/// 简单特效模板 - 一条线
/// </summary>
public class DiceAttackEffect_VeliaThorn_Z : DiceAttackEffect
{
    private LineRenderer _line;
    private float _progress = 0f;

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        base.Initialize(self, target, destroyTime);

        // 创建一条线
        GameObject obj = new GameObject("Line");
        obj.transform.SetParent(transform);

        _line = obj.AddComponent<LineRenderer>();
        _line.material = new Material(Shader.Find("Sprites/Default"));

        // === 你可以改这些参数 ===
        _line.startWidth = 0.3f;    // 起点粗细
        _line.endWidth = 0.1f;      // 终点粗细
        _line.startColor = Color.red;
        _line.endColor = Color.red;
        // ========================

        _line.positionCount = 2;
        _line.SetPosition(0, _selfTransform.position);
        _line.SetPosition(1, _selfTransform.position);
    }

    protected override void Update()
    {
        base.Update();

        // 进度：0到1
        _progress += Time.deltaTime / (_destroyTime * 0.5f);
        _progress = Mathf.Clamp01(_progress);

        // 线的终点逐渐延伸到目标
        Vector3 currentEnd = Vector3.Lerp(
            _selfTransform.position,
            _targetTransform.position,
            _progress
        );

        _line.SetPosition(1, currentEnd);
    }
}
