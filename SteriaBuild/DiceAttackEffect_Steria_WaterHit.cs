using Steria;

/// <summary>
/// 水系打击特效
/// </summary>
public class DiceAttackEffect_Steria_WaterHit : DiceAttackEffect_Steria_Base
{
    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WaterHit;
    }
}
