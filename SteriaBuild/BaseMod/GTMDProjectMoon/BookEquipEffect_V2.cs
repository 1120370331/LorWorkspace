using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x02000029 RID: 41
	public class BookEquipEffect_V2 : BookEquipEffect
	{
		// Token: 0x06000067 RID: 103 RVA: 0x000044C4 File Offset: 0x000026C4
		public void InitOldFields(string packageId)
		{
			LorId.InitializeLorIds<LorIdXml>(this._PassiveList, this.PassiveList, packageId);
			LorId.InitializeLorIds<LorIdXml>(this.OnlyCard, this.OnlyCards, packageId);
			foreach (LorId lorId in this.OnlyCards)
			{
				if (lorId.IsBasic())
				{
					this.OnlyCard.Add(lorId.id);
				}
			}
			foreach (BookSoulCardInfo_New bookSoulCardInfo_New in this.CardList)
			{
				bookSoulCardInfo_New.WorkshopId = Tools.ClarifyWorkshopId("", bookSoulCardInfo_New.WorkshopId, packageId);
			}
		}

		// Token: 0x04000052 RID: 82
		[XmlElement("OnlyCard")]
		public List<LorIdXml> OnlyCard = new List<LorIdXml>();

		// Token: 0x04000053 RID: 83
		[XmlIgnore]
		public List<LorId> OnlyCards = new List<LorId>();

		// Token: 0x04000054 RID: 84
		[XmlElement("SoulCard")]
		public List<BookSoulCardInfo_New> CardList = new List<BookSoulCardInfo_New>();
	}
}
