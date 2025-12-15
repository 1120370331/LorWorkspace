using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000041 RID: 65
	public class EnemyDropItem_New
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00004D40 File Offset: 0x00002F40
		[XmlIgnore]
		public LorId bookLorId
		{
			get
			{
				return new LorId(this.workshopId, this.bookId);
			}
		}

		// Token: 0x040000AC RID: 172
		[XmlText]
		public int bookId;

		// Token: 0x040000AD RID: 173
		[XmlAttribute("Prob")]
		public float prob;

		// Token: 0x040000AE RID: 174
		[XmlAttribute("Pid")]
		public string workshopId = "";
	}
}
