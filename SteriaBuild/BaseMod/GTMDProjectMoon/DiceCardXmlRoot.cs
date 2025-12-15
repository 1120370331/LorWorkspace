using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200003C RID: 60
	[Obsolete]
	public class DiceCardXmlRoot
	{
		// Token: 0x040000A3 RID: 163
		[XmlElement("Card")]
		public List<DiceCardXmlInfo_New> cardXmlList;
	}
}
