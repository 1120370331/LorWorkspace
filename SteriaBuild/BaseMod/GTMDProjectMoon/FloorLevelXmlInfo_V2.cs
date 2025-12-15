using System;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x02000010 RID: 16
	public class FloorLevelXmlInfo_V2 : FloorLevelXmlInfo
	{
		// Token: 0x06000039 RID: 57 RVA: 0x000039A0 File Offset: 0x00001BA0
		public void InitOldFields(string packageId)
		{
			this.sephirahType = (string.IsNullOrWhiteSpace(this.sephirahName) ? 0 : Tools.MakeEnum<SephirahType>(this.sephirahName));
			this.stageLorIdXml.pid = Tools.ClarifyWorkshopIdLegacy("", this.stageLorIdXml.pid, packageId);
			this.stageLorId = LorId.MakeLorId(this.stageLorIdXml, "");
			if (this.stageLorId.IsBasic())
			{
				this.stageId = this.stageLorId.id;
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003A23 File Offset: 0x00001C23
		public FloorLevelXmlInfo_V2()
		{
			this.stageId = -1;
		}

		// Token: 0x0400001A RID: 26
		[XmlElement("Sephirah")]
		public string sephirahName;

		// Token: 0x0400001B RID: 27
		[XmlElement("Stage")]
		public LorIdXml stageLorIdXml;

		// Token: 0x0400001C RID: 28
		[XmlIgnore]
		public LorId stageLorId;
	}
}
