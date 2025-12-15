using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000012 RID: 18
	public class GiftXmlInfo_V2 : GiftXmlInfo
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600003D RID: 61 RVA: 0x00003A94 File Offset: 0x00001C94
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.originalId ?? this.id);
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003ACB File Offset: 0x00001CCB
		public void InjectId(int injectedId)
		{
			if (this.originalId == null)
			{
				this.originalId = new int?(this.id);
				this.id = injectedId;
				OrcTools.GiftAndTitleDic[injectedId] = this.lorId;
			}
		}

		// Token: 0x0400001F RID: 31
		[XmlIgnore]
		private int? originalId;

		// Token: 0x04000020 RID: 32
		[XmlAttribute("Pid")]
		public string WorkshopId = "";

		// Token: 0x04000021 RID: 33
		[XmlElement("Passive")]
		public List<string> CustomScriptList = new List<string>();

		// Token: 0x04000022 RID: 34
		[XmlElement("PriorityOrder")]
		public GiftPriorityOrder priority = GiftPriorityOrder.Guest;

		// Token: 0x04000023 RID: 35
		[XmlIgnore]
		internal bool dontRemove;
	}
}
