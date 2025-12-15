using System;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x0200002C RID: 44
	public class BookDesc_V2 : BookDesc
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600006C RID: 108 RVA: 0x000045FE File Offset: 0x000027FE
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.workshopId, this.bookID);
			}
		}

		// Token: 0x0400005A RID: 90
		[XmlAttribute("Pid")]
		public string workshopId;
	}
}
