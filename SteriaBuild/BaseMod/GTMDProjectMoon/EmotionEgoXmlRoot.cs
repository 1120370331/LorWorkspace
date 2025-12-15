using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000049 RID: 73
	[Obsolete]
	public class EmotionEgoXmlRoot
	{
		// Token: 0x040000D3 RID: 211
		[XmlElement("EmotionEgo")]
		public List<EmotionEgoXmlInfo_New> egoXmlList;
	}
}
