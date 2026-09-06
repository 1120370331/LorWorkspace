/// <summary>Each dice entry rejoins the same visual-only card session.</summary>
public class BehaviourAction_Steria_VeliaTideMist : BehaviourActionBase
{
    public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
    {
        _self = self;
        return FarAreaEffect_Steria_VeliaTideMist.GetOrCreate(self);
    }
}
