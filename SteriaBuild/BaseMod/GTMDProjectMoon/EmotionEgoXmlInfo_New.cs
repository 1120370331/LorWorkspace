using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200004A RID: 74
	[Obsolete]
	public class EmotionEgoXmlInfo_New
	{
		// Token: 0x040000D4 RID: 212
		[XmlElement("ID")]
		public int id;

		// Token: 0x040000D5 RID: 213
		[XmlElement("Sephirah")]
		public SephirahType Sephirah;

		// Token: 0x040000D6 RID: 214
		[XmlElement("Card")]
		public LorIdXml _CardId;

		// Token: 0x040000D7 RID: 215
		[XmlElement("LockInBattle")]
		public bool isLock;
	}
}
