using System;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x02000015 RID: 21
	public class EmotionCardXmlInfo_V2 : EmotionCardXmlInfo, IIdInjectable
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000042 RID: 66 RVA: 0x00003B88 File Offset: 0x00001D88
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.originalId ?? this.id);
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003BBF File Offset: 0x00001DBF
		public void InjectId(int injectedId)
		{
			if (this.originalId == null)
			{
				this.originalId = new int?(this.id);
				this.id = injectedId;
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003BE6 File Offset: 0x00001DE6
		public void InitOldFields()
		{
			this.Sephirah = (string.IsNullOrWhiteSpace(this.SephirahName) ? 0 : Tools.MakeEnum<SephirahType>(this.SephirahName));
		}

		// Token: 0x0400002B RID: 43
		[XmlElement("Sephirah")]
		public string SephirahName;

		// Token: 0x0400002C RID: 44
		[XmlIgnore]
		private int? originalId;

		// Token: 0x0400002D RID: 45
		[XmlAttribute("Pid")]
		public string WorkshopId = "";
	}
}
