using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000020 RID: 32
	public class TitleXmlInfo_V2 : TitleXmlInfo
	{
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00003E10 File Offset: 0x00002010
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.originalId ?? this.ID);
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003E47 File Offset: 0x00002047
		public void InjectId(int injectedId)
		{
			if (this.originalId == null)
			{
				this.originalId = new int?(this.ID);
				this.ID = injectedId;
				OrcTools.GiftAndTitleDic[injectedId] = this.lorId;
			}
		}

		// Token: 0x0400003B RID: 59
		[XmlIgnore]
		private int? originalId;

		// Token: 0x0400003C RID: 60
		[XmlAttribute("Pid")]
		public string WorkshopId = "";
	}
}
