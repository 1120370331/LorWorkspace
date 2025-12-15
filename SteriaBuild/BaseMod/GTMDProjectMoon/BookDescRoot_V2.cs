using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200002B RID: 43
	[XmlType("BookDescRoot")]
	public class BookDescRoot_V2 : XmlRoot
	{
		// Token: 0x04000059 RID: 89
		[XmlArrayItem("BookDesc")]
		public List<BookDesc_V2> bookDescList;
	}
}
