using System;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x02000068 RID: 104
	public class EffectTypoData_New : EffectTypoData
	{
		// Token: 0x04000154 RID: 340
		public string type;

		// Token: 0x04000155 RID: 341
		public EffectTypoData_New.BattleUIPassiveSet battleUIPassiveSet;

		// Token: 0x020000AA RID: 170
		public class BattleUIPassiveSet
		{
			// Token: 0x040002BE RID: 702
			public Sprite frame;

			// Token: 0x040002BF RID: 703
			public Sprite Icon;

			// Token: 0x040002C0 RID: 704
			public Sprite IconGlow;

			// Token: 0x040002C1 RID: 705
			public Color textColor;

			// Token: 0x040002C2 RID: 706
			public Color IconColor;

			// Token: 0x040002C3 RID: 707
			public Color IconGlowColor;
		}
	}
}
