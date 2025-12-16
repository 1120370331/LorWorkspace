using Steria;

/// <summary>
/// 水系斩击特效
/// </summary>
public class DiceAttackEffect_Steria_WaterSlash : DiceAttackEffect_Steria_Base
{
    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WaterSlash;
    }
}
