using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000048 RID: 72
	[Obsolete]
	public class EgoCardDetail
	{
		// Token: 0x040000CF RID: 207
		[XmlElement("ID")]
		public int id;

		// Token: 0x040000D0 RID: 208
		[XmlElement("Sephirah")]
		public SephirahType Sephirah;

		// Token: 0x040000D1 RID: 209
		[XmlElement("Card")]
		public LorIdXml _CardId;

		// Token: 0x040000D2 RID: 210
		[XmlElement("LockInBattle")]
		public bool isLock;
	}
}
