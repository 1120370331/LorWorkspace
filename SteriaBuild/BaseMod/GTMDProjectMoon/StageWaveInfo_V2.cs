using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using BaseBridge;
using LorIdExtensions;

namespace GTMDProjectMoon
{
	// Token: 0x0200004F RID: 79
	public class StageWaveInfo_V2 : StageWaveInfo
	{
		// Token: 0x060000AF RID: 175 RVA: 0x0000645E File Offset: 0x0000465E
		public void InitOldFields(string uniqueId)
		{
			this.uniqueId = uniqueId;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00006468 File Offset: 0x00004668
		public void InitInjectedFields()
		{
			this.formationLorId = LorIdLegacy.MakeLorIdLegacy(this.formationLorIdXml, this.uniqueId);
			if (this.formationLorId.IsBasic())
			{
				this.formationId = this.formationLorId.id;
			}
			else
			{
				FormationXmlInfo entityByKey = Singleton<BridgeManager>.Instance.formationBridge.GetEntityByKey(this.formationLorId);
				if (entityByKey != null)
				{
					this.formationId = entityByKey.id;
				}
			}
			LorIdLegacy.InitializeLorIdsLegacy<LorIdXml>(this.emotionCardListXml, this.emotionCardListNew, this.uniqueId);
			foreach (LorId lorId in this.emotionCardListNew)
			{
				if (lorId.IsBasic())
				{
					this.emotionCardList.Add(lorId.id);
				}
				else
				{
					EmotionCardXmlInfo entityByKey2 = Singleton<BridgeManager>.Instance.emotionCardBridge.GetEntityByKey(lorId, 0);
					if (entityByKey2 != null)
					{
						this.emotionCardList.Add(entityByKey2.id);
					}
				}
			}
		}

		// Token: 0x040000F1 RID: 241
		[XmlElement("Formation")]
		public LorIdXml formationLorIdXml = new LorIdXml("", 1);

		// Token: 0x040000F2 RID: 242
		[XmlArray("emotionCardList")]
		[XmlArrayItem("int")]
		public List<LorIdXml> emotionCardListXml = new List<LorIdXml>();

		// Token: 0x040000F3 RID: 243
		[XmlIgnore]
		public LorId formationLorId;

		// Token: 0x040000F4 RID: 244
		[XmlIgnore]
		public List<LorId> emotionCardListNew = new List<LorId>();

		// Token: 0x040000F5 RID: 245
		public string uniqueId = "";
	}
}
