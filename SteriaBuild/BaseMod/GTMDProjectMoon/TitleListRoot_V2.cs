using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200001F RID: 31
	public class TitleListRoot_V2
	{
		// Token: 0x0400003A RID: 58
		[XmlElement("Title")]
		public List<TitleXmlInfo_V2> List;
	}
}
