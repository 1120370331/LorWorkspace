using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200001E RID: 30
	[XmlType("TitleXmlRoot")]
	public class TitleXmlRoot_V2 : XmlRoot
	{
		// Token: 0x04000038 RID: 56
		[XmlElement("Prefix")]
		public TitleListRoot_V2 prefixXmlList;

		// Token: 0x04000039 RID: 57
		[XmlElement("Postfix")]
		public TitleListRoot_V2 postfixXmlList;
	}
}
