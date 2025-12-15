using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000035 RID: 53
	[Obsolete]
	public class BattleDialogCharacter_New
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00004824 File Offset: 0x00002A24
		[XmlIgnore]
		public LorId id
		{
			get
			{
				return new LorId(this.workshopId, this.bookId);
			}
		}

		// Token: 0x04000091 RID: 145
		[XmlAttribute("Name")]
		public string characterName = "";

		// Token: 0x04000092 RID: 146
		[XmlAttribute("ID")]
		public string characterID;

		// Token: 0x04000093 RID: 147
		[XmlElement("Type")]
		public List<BattleDialogType> dialogTypeList = new List<BattleDialogType>();

		// Token: 0x04000094 RID: 148
		[XmlIgnore]
		public string workshopId = "";

		// Token: 0x04000095 RID: 149
		[XmlIgnore]
		public int bookId;
	}
}
