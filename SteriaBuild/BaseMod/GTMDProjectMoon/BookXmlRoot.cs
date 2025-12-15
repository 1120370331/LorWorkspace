using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200002D RID: 45
	[Obsolete]
	public class BookXmlRoot
	{
		// Token: 0x0400005B RID: 91
		[XmlElement("Version")]
		public string version = "1.1";

		// Token: 0x0400005C RID: 92
		[XmlElement("Book")]
		public List<BookXmlInfo_New> bookXmlList;
	}
}
