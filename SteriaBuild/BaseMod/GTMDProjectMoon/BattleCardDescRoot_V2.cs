using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200003A RID: 58
	[XmlType("BattleCardDescRoot")]
	public class BattleCardDescRoot_V2 : XmlRoot
	{
		// Token: 0x040000A1 RID: 161
		[XmlArrayItem("BattleCardDesc")]
		public List<BattleCardDesc_V2> cardDescList;
	}
}
