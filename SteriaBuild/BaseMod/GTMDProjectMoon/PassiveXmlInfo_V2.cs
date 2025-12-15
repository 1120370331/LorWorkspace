using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000025 RID: 37
	public class PassiveXmlInfo_V2 : PassiveXmlInfo
	{
		// Token: 0x04000043 RID: 67
		[XmlElement("CopyInnerType")]
		public LorIdXml CopyInnerTypeXml = new LorIdXml("", -1);

		// Token: 0x04000044 RID: 68
		[XmlIgnore]
		public LorId CopyInnerType = LorId.None;

		// Token: 0x04000045 RID: 69
		[XmlElement("CustomInnerType")]
		public string CustomInnerType = "";
	}
}
