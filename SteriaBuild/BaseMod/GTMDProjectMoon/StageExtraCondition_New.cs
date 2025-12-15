using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000052 RID: 82
	[Obsolete]
	public class StageExtraCondition_New
	{
		// Token: 0x04000109 RID: 265
		[XmlElement("Stage")]
		public List<LorIdXml> needClearStageList = new List<LorIdXml>();

		// Token: 0x0400010A RID: 266
		[XmlIgnore]
		public List<LorId> stagecondition = new List<LorId>();

		// Token: 0x0400010B RID: 267
		[XmlElement("Level")]
		public int needLevel;
	}
}
