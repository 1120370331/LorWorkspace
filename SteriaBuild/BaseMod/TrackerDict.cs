using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseMod
{
	// Token: 0x02000057 RID: 87
	internal class TrackerDict<TValue> : Dictionary<LorId, AddTracker<TValue>>
	{
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000BD RID: 189 RVA: 0x00006A72 File Offset: 0x00004C72
		public IEnumerable<LorId> SortedKeys
		{
			get
			{
				return base.Keys.OrderBy((LorId x) => x, OptimizedReplacer.packageSortedComp);
			}
		}
	}
}
