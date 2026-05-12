using LOR_DiceSystem;
using Steria;

/// <summary>
/// 强音 (Forte) - 乐章型骰子专属威力提升 buff。
///
/// - 乐章型骰子的威力 +X（X = 当前层数）
/// - 回合结束时清空全部层数（参考原版"强壮"）
/// - 通过 MusicDicePowerScope 进入白名单，绕过乐章型骰子默认的威力屏蔽逻辑。
///   同时在 PrimalTidePowerScope 内调用，避免被"原初之潮"威力屏蔽干扰。
/// </summary>
public class BattleUnitBuf_Forte : BattleUnitBuf
{
    protected override string keywordId => "SteriaForte";
    protected override string keywordIconId => "SteriaForte";

    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        if (behavior == null || _owner == null || stack <= 0)
        {
            return;
        }

        if (!MusicDiceSystem.IsMusicDiceBehaviour(behavior))
        {
            return;
        }

        MusicDicePowerScope.RunWithAllowance(() =>
            global::PrimalTidePowerScope.RunWithAllowance(() =>
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = stack })));
    }

    public override void OnRoundEnd()
    {
        // 与"强壮"一致，回合结束时清空全部层数。
        this.Destroy();
    }
}
