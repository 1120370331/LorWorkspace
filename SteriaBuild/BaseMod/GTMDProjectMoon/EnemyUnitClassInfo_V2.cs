using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using BaseBridge;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x0200003F RID: 63
	public class EnemyUnitClassInfo_V2 : EnemyUnitClassInfo
	{
		// Token: 0x06000087 RID: 135 RVA: 0x00004B3C File Offset: 0x00002D3C
		public void InitOldFields(string uniqueId)
		{
			this.workshopID = uniqueId;
			this.height = RandomUtil.Range(this.minHeight, this.maxHeight);
			foreach (EnemyDropItemTable_V2 enemyDropItemTable_V in this.dropTableListNew)
			{
				foreach (EnemyDropItem_New enemyDropItem_New in enemyDropItemTable_V.dropItemListNew)
				{
					enemyDropItem_New.workshopId = Tools.ClarifyWorkshopId("", enemyDropItem_New.workshopId, uniqueId);
				}
			}
			foreach (EmotionSetInfo_V2 emotionSetInfo_V in this.emotionCardListNew)
			{
				emotionSetInfo_V.WorkshopId = Tools.ClarifyWorkshopIdLegacy("", emotionSetInfo_V.WorkshopId, uniqueId);
			}
			LorId.InitializeLorIds<LorIdXml>(this.bookLorIdXml, this.bookLorId, uniqueId);
			this.bookId = new List<int>();
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004C68 File Offset: 0x00002E68
		public void InitInjectedFields()
		{
			foreach (EmotionSetInfo_V2 emotionSetInfo_V in this.emotionCardListNew)
			{
				if (emotionSetInfo_V.lorId.IsBasic())
				{
					this.emotionCardList.Add(emotionSetInfo_V);
				}
				else
				{
					EmotionCardXmlInfo entityByKey = Singleton<BridgeManager>.Instance.emotionCardBridge.GetEntityByKey(emotionSetInfo_V.lorId, 0);
					if (entityByKey != null)
					{
						emotionSetInfo_V.InjectId(entityByKey.id);
						this.emotionCardList.Add(emotionSetInfo_V);
					}
				}
			}
		}

		// Token: 0x040000A7 RID: 167
		[XmlElement("BookId")]
		public List<LorIdXml> bookLorIdXml = new List<LorIdXml>();

		// Token: 0x040000A8 RID: 168
		[XmlIgnore]
		public List<LorId> bookLorId = new List<LorId>();

		// Token: 0x040000A9 RID: 169
		[XmlElement("DropTable")]
		public List<EnemyDropItemTable_V2> dropTableListNew = new List<EnemyDropItemTable_V2>();

		// Token: 0x040000AA RID: 170
		[XmlElement("Emotion")]
		public List<EmotionSetInfo_V2> emotionCardListNew = new List<EmotionSetInfo_V2>();
	}
}
