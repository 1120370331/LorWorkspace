using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000044 RID: 68
	[Obsolete]
	public class EnemyDropItemTable_New
	{
		// Token: 0x040000C3 RID: 195
		[XmlAttribute("Level")]
		public int emotionLevel;

		// Token: 0x040000C4 RID: 196
		[XmlElement("DropItem")]
		public List<EnemyDropItem_New> dropItemList;

		// Token: 0x040000C5 RID: 197
		[XmlIgnore]
		public List<EnemyDropItem_ReNew> dropList = new List<EnemyDropItem_ReNew>();
	}
}
