using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000034 RID: 52
	[Obsolete]
	public class BattleDialogRoot
	{
		// Token: 0x0400008F RID: 143
		[XmlElement("GroupName")]
		public string groupName;

		// Token: 0x04000090 RID: 144
		[XmlElement("Character")]
		public List<BattleDialogCharacter_New> characterList = new List<BattleDialogCharacter_New>();
	}
}
