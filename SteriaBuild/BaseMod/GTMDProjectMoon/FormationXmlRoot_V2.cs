using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000018 RID: 24
	[XmlType("FormationXmlRoot")]
	public class FormationXmlRoot_V2 : XmlRoot
	{
		// Token: 0x04000031 RID: 49
		[XmlElement("Formation")]
		public List<FormationXmlInfo_V2> list;
	}
}
