using System;
using System.Xml.Serialization;
using BaseMod;
using LorIdExtensions;

namespace GTMDProjectMoon
{
	// Token: 0x02000047 RID: 71
	public class EmotionEgoXmlInfo_V2 : EmotionEgoXmlInfo, IIdInjectable
	{
		// Token: 0x06000096 RID: 150 RVA: 0x00004ED3 File Offset: 0x000030D3
		public void InjectId(int injectedId)
		{
			if (this.id == 0)
			{
				this.id = injectedId;
				OrcTools.EgoDic[this] = this.lorCardId;
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00004EF8 File Offset: 0x000030F8
		public void InitOldFields(string packageId)
		{
			this.Sephirah = (string.IsNullOrWhiteSpace(this.SephirahName) ? 0 : Tools.MakeEnum<SephirahType>(this.SephirahName));
			this.lorId = LorIdLegacy.MakeLorIdLegacy(this.lorIdXml, packageId);
			this.lorCardId = LorId.MakeLorId(this.lorCardIdXml, packageId);
		}

		// Token: 0x040000CA RID: 202
		[XmlElement("ID")]
		public LorIdXml lorIdXml;

		// Token: 0x040000CB RID: 203
		[XmlIgnore]
		public LorId lorId;

		// Token: 0x040000CC RID: 204
		[XmlElement("Sephirah")]
		public string SephirahName;

		// Token: 0x040000CD RID: 205
		[XmlElement("Card")]
		public LorIdXml lorCardIdXml;

		// Token: 0x040000CE RID: 206
		[XmlIgnore]
		public LorId lorCardId;
	}
}
