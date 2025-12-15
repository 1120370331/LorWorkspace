using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000040 RID: 64
	public class EnemyDropItemTable_V2 : EnemyDropItemTable
	{
		// Token: 0x040000AB RID: 171
		[XmlElement("DropItem")]
		public List<EnemyDropItem_New> dropItemListNew;
	}
}
