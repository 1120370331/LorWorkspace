using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000042 RID: 66
	public class EmotionSetInfo_V2 : EmotionSetInfo
	{
		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00004D68 File Offset: 0x00002F68
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.originalEmotionId ?? this.emotionId);
			}
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00004D9F File Offset: 0x00002F9F
		public void InjectId(int injectedId)
		{
			if (this.originalEmotionId == null)
			{
				this.originalEmotionId = new int?(this.emotionId);
				this.emotionId = injectedId;
			}
		}

		// Token: 0x040000AF RID: 175
		[XmlIgnore]
		private int? originalEmotionId;

		// Token: 0x040000B0 RID: 176
		[XmlAttribute("Pid")]
		public string WorkshopId = "";
	}
}
