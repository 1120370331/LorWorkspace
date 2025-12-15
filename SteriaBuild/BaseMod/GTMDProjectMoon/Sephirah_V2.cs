using System;
using System.Xml.Serialization;
using BaseMod;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000017 RID: 23
	public class Sephirah_V2 : Sephirah
	{
		// Token: 0x06000048 RID: 72 RVA: 0x00003C7B File Offset: 0x00001E7B
		public void InitOldFields()
		{
			this.sephirahType = (string.IsNullOrWhiteSpace(this.sephirahName) ? 0 : Tools.MakeEnum<SephirahType>(this.sephirahName));
		}

		// Token: 0x04000030 RID: 48
		[XmlAttribute("SephirahType")]
		public string sephirahName;
	}
}
