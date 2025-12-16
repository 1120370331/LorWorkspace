using Steria;

/// <summary>
/// 水系突刺特效
/// </summary>
public class DiceAttackEffect_Steria_WaterPenetrate : DiceAttackEffect_Steria_Base
{
    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WaterPenetrate;
    }
}
