using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000032 RID: 50
	[XmlType("BattleDialogRelationRoot")]
	public class BattleDialogRelationRoot_V2 : XmlRoot
	{
		// Token: 0x0400008D RID: 141
		[XmlElement("Relation")]
		public List<BattleDialogRelationWithBookID_V2> list;
	}
}
