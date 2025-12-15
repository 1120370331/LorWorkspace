using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000036 RID: 54
	[Obsolete]
	public class BattleDialogRelationRoot
	{
		// Token: 0x04000096 RID: 150
		[XmlElement("Relation")]
		public List<BattleDialogRelationWithBookID_New> list;
	}
}
