using System;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x0200003B RID: 59
	public class BattleCardDesc_V2 : BattleCardDesc
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00004A2B File Offset: 0x00002C2B
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.workshopId, this.cardID);
			}
		}

		// Token: 0x040000A2 RID: 162
		[XmlAttribute("Pid")]
		public string workshopId;
	}
}
