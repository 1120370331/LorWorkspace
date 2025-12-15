using System;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200002A RID: 42
	public class BookSoulCardInfo_New
	{
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000069 RID: 105 RVA: 0x000045C9 File Offset: 0x000027C9
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.WorkshopId, this.cardId);
			}
		}

		// Token: 0x04000055 RID: 85
		[XmlText]
		public int cardId;

		// Token: 0x04000056 RID: 86
		[XmlAttribute("Pid")]
		public string WorkshopId = "";

		// Token: 0x04000057 RID: 87
		[XmlAttribute("Level")]
		public int requireLevel;

		// Token: 0x04000058 RID: 88
		[XmlAttribute("Emotion")]
		public int emotionLevel = 1;
	}
}
