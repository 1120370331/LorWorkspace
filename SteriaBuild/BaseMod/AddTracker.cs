using System;

namespace BaseMod
{
	// Token: 0x02000056 RID: 86
	internal class AddTracker<T>
	{
		// Token: 0x060000BC RID: 188 RVA: 0x00006A63 File Offset: 0x00004C63
		public AddTracker(T element)
		{
			this.element = element;
		}

		// Token: 0x04000110 RID: 272
		public T element;

		// Token: 0x04000111 RID: 273
		public bool added;
	}
}
