using System;

namespace BaseMod
{
	// Token: 0x0200005D RID: 93
	public class CustomMapManager : CreatureMapManager
	{
		// Token: 0x060000EB RID: 235 RVA: 0x00007A91 File Offset: 0x00005C91
		public virtual bool IsMapChangable()
		{
			return true;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00007A94 File Offset: 0x00005C94
		public virtual bool IsMapChangableByAssimilation()
		{
			return true;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00007A97 File Offset: 0x00005C97
		public virtual void CustomInit()
		{
		}
	}
}
