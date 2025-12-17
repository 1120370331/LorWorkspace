using Steria;

/// <summary>
/// 水系远程打击特效（拙劣控流用）
/// </summary>
public class DiceAttackEffect_Steria_WaterFarHit : DiceAttackEffect_Steria_Base
{
    protected override SteriaEffectConfig GetConfig()
    {
        return SteriaEffectConfig.WaterFarHit;
    }
}
