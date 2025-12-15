using LOR_DiceSystem;
using UnityEngine;
using Steria; // For SteriaLogger

// 珍贵的回忆弃置效果：下一幕开始时获得1层强壮
// 注意：Buff 类必须在全局命名空间中，游戏才能正确调用其方法
public class BattleUnitBuf_PreciousMemoryStrength : BattleUnitBuf
{
    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        SteriaLogger.Log($"BattleUnitBuf_PreciousMemoryStrength: Init called, owner={owner?.UnitData?.unitData?.name}");
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        SteriaLogger.Log($"BattleUnitBuf_PreciousMemoryStrength: OnRoundStart, owner={this._owner?.UnitData?.unitData?.name}");

        if (this._owner != null)
        {
            try
            {
                this._owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1, this._owner);
                SteriaLogger.Log($"BattleUnitBuf_PreciousMemoryStrength: 成功添加1层强壮");
            }
            catch (System.Exception ex)
            {
                SteriaLogger.LogError($"BattleUnitBuf_PreciousMemoryStrength: 添加强壮失败: {ex.Message}");
            }
        }

        // 效果触发后销毁自身
        this.Destroy();
        SteriaLogger.Log($"BattleUnitBuf_PreciousMemoryStrength: 已销毁");
    }
}
