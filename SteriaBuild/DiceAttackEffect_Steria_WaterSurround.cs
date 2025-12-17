using Steria;

/// <summary>
/// 水系环绕特效（自我之流用）
/// </summary>
public class DiceAttackEffect_Steria_WaterSurround : DiceAttackEffect_Steria_Base
{
    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WaterSurround;
    }
}
