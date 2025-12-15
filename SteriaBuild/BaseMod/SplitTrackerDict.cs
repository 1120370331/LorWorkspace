using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseMod
{
	// Token: 0x02000058 RID: 88
	internal class SplitTrackerDict<TSplitter, TValue> : Dictionary<TSplitter, TrackerDict<TValue>>
	{
		// Token: 0x060000BF RID: 191 RVA: 0x00006AAB File Offset: 0x00004CAB
		public SplitTrackerDict(Comparison<TSplitter> splitterSorter)
		{
			this.splitterSorter = Comparer<TSplitter>.Create(splitterSorter);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00006ABF File Offset: 0x00004CBF
		public SplitTrackerDict(IComparer<TSplitter> splitterSorter)
		{
			this.splitterSorter = splitterSorter;
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000C1 RID: 193 RVA: 0x00006ACE File Offset: 0x00004CCE
		public IEnumerable<TSplitter> SortedKeys
		{
			get
			{
				return base.Keys.OrderBy((TSplitter x) => x, this.splitterSorter);
			}
		}

		// Token: 0x04000112 RID: 274
		public readonly IComparer<TSplitter> splitterSorter;
	}
}
