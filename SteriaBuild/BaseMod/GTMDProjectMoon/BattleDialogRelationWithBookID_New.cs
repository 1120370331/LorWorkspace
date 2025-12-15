using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000037 RID: 55
	[Obsolete]
	public class BattleDialogRelationWithBookID_New
	{
		// Token: 0x04000097 RID: 151
		[XmlAttribute("Pid")]
		public string workshopId = "";

		// Token: 0x04000098 RID: 152
		[XmlAttribute("BookID")]
		public int bookID;

		// Token: 0x04000099 RID: 153
		[XmlAttribute("StoryID")]
		public int storyID;

		// Token: 0x0400009A RID: 154
		[XmlElement("GroupName")]
		public string groupName;

		// Token: 0x0400009B RID: 155
		[XmlElement("CharacterID")]
		public string characterID;
	}
}
