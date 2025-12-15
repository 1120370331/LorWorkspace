using System;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x0200000B RID: 11
	public class CharacterName_V2 : CharacterName
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600002F RID: 47 RVA: 0x000037B3 File Offset: 0x000019B3
		[XmlIgnore]
		public LorId lorId
		{
			get
			{
				return new LorId(this.workshopId, this.ID);
			}
		}

		// Token: 0x04000011 RID: 17
		[XmlAttribute("Pid")]
		public string workshopId;
	}
}
