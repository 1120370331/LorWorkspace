using System;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000033 RID: 51
	public class BattleDialogRelationWithBookID_V2 : BattleDialogRelationWithBookID
	{
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000075 RID: 117 RVA: 0x000047EB File Offset: 0x000029EB
		[XmlIgnore]
		public LorId bookLorId
		{
			get
			{
				return new LorId(this.workshopId, this.bookID);
			}
		}

		// Token: 0x0400008E RID: 142
		[XmlAttribute("Pid")]
		public string workshopId = "";
	}
}
