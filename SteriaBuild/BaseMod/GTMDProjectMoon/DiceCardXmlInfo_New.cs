using System;
using System.Xml.Serialization;
using LOR_DiceSystem;

namespace GTMDProjectMoon
{
	// Token: 0x0200003D RID: 61
	[Obsolete]
	public class DiceCardXmlInfo_New : DiceCardXmlInfo
	{
		// Token: 0x040000A4 RID: 164
		[XmlElement("CustomCategory")]
		public string customCategory = "";
	}
}
