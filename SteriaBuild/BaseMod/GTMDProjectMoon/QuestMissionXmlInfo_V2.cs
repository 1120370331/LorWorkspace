using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200001D RID: 29
	public class QuestMissionXmlInfo_V2 : QuestMissionXmlInfo
	{
		// Token: 0x04000037 RID: 55
		[XmlAttribute("Script")]
		public string scriptName = string.Empty;
	}
}
