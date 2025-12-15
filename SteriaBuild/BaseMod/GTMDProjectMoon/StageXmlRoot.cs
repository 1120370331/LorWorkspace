using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000050 RID: 80
	[Obsolete]
	public class StageXmlRoot
	{
		// Token: 0x040000F6 RID: 246
		[XmlElement("Stage")]
		public List<StageClassInfo_New> list;
	}
}
