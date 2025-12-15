using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseMod;

namespace Steria
{
    // Custom Buff: Flow (流)
    public class BattleUnitBuf_Flow : BattleUnitBuf
    {
        // 使用 EffectTexts.xml 中定义的 ID
        protected override string keywordId => "SteriaFlow";
        protected override string keywordIconId => "SteriaFlow"; // 对应 Resource/ArtWork/SteriaFlow.png

        public override BufPositiveType positiveType => BufPositiveType.Positive;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            SteriaLogger.Log($"BattleUnitBuf_Flow: Init called, owner={owner?.UnitData?.unitData?.name}");
        }

        public override void OnAddBuf(int addedStack)
        {
            base.OnAddBuf(addedStack);
            SteriaLogger.Log($"BattleUnitBuf_Flow: OnAddBuf called, addedStack={addedStack}, total stack={this.stack}");
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            // Flow 不会自动消失，只在使用书页时消耗
        }
    }
} 
