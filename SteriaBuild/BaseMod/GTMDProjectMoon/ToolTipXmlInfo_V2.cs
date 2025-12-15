using System;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x02000022 RID: 34
	public class ToolTipXmlInfo_V2 : ToolTipXmlInfo
	{
		// Token: 0x0600005B RID: 91 RVA: 0x00003EF3 File Offset: 0x000020F3
		public void InitOldFields()
		{
			this.ID = (string.IsNullOrWhiteSpace(this.IDname) ? 0 : Tools.MakeEnum<ToolTipTarget>(this.IDname));
		}

		// Token: 0x0400003F RID: 63
		[XmlAttribute("ID")]
		public string IDname;
	}
}
