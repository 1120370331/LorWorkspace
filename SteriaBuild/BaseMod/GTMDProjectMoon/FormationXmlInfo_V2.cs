using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000019 RID: 25
	public class FormationXmlInfo_V2 : FormationXmlInfo, IIdInjectable
	{
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00003CB0 File Offset: 0x00001EB0
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.originalId ?? this.id);
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003CE7 File Offset: 0x00001EE7
		public void InjectId(int injectedId)
		{
			if (this.originalId == null)
			{
				this.originalId = new int?(this.id);
				this.id = injectedId;
			}
		}

		// Token: 0x04000032 RID: 50
		[XmlIgnore]
		private int? originalId;

		// Token: 0x04000033 RID: 51
		[XmlAttribute("Pid")]
		public string WorkshopId = "";
	}
}
