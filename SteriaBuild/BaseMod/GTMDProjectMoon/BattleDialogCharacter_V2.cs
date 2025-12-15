using System;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000031 RID: 49
	public class BattleDialogCharacter_V2 : BattleDialogCharacter
	{
		// Token: 0x0400008C RID: 140
		[XmlAttribute("Name")]
		public string characterName = "";
	}
}
