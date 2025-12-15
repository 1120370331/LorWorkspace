using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x0200004D RID: 77
	public class StageClassInfo_V2 : StageClassInfo
	{
		// Token: 0x060000AA RID: 170 RVA: 0x0000612F File Offset: 0x0000432F
		public StageClassInfo_V2()
		{
			this.extraCondition = new StageExtraCondition_V2();
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00006158 File Offset: 0x00004358
		public void InitOldFields(string uniqueId)
		{
			this.floorOnlyList = (from s in this.floorOnlyNamesList
			where !string.IsNullOrWhiteSpace(s)
			select Tools.MakeEnum<SephirahType>(s)).ToList<SephirahType>();
			this.exceptFloorList = (from s in this.exceptFloorNamesList
			where !string.IsNullOrWhiteSpace(s)
			select Tools.MakeEnum<SephirahType>(s)).ToList<SephirahType>();
			StageExtraCondition_V2 stageExtraCondition_V = this.extraCondition as StageExtraCondition_V2;
			if (stageExtraCondition_V != null)
			{
				stageExtraCondition_V.InitOldFields(uniqueId);
			}
			foreach (StageWaveInfo stageWaveInfo in this.waveList)
			{
				StageWaveInfo_V2 stageWaveInfo_V = stageWaveInfo as StageWaveInfo_V2;
				if (stageWaveInfo_V != null)
				{
					stageWaveInfo_V.InitOldFields(uniqueId);
				}
			}
			foreach (StageStoryInfo stageStoryInfo in this.storyList)
			{
				stageStoryInfo.packageId = Tools.ClarifyWorkshopId(stageStoryInfo.packageId, this.workshopID, uniqueId);
				if (string.IsNullOrWhiteSpace(stageStoryInfo.packageId) && !string.IsNullOrWhiteSpace(stageStoryInfo.story) && stageStoryInfo.episode == 0 && stageStoryInfo.group == 0 && stageStoryInfo.chapter == 0)
				{
					stageStoryInfo.chapter = this.chapter;
					stageStoryInfo.group = 1;
					stageStoryInfo.episode = this._id;
				}
				stageStoryInfo.valid = true;
			}
			if (this.invitationInfo.combine == 1)
			{
				this.invitationInfo.needsBooks.Sort();
				this.invitationInfo.needsBooks.Reverse();
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x0000635C File Offset: 0x0000455C
		public void InitInjectedFields()
		{
			foreach (StageWaveInfo stageWaveInfo in this.waveList)
			{
				StageWaveInfo_V2 stageWaveInfo_V = stageWaveInfo as StageWaveInfo_V2;
				if (stageWaveInfo_V != null)
				{
					stageWaveInfo_V.InitInjectedFields();
				}
			}
		}

		// Token: 0x040000ED RID: 237
		[XmlElement("FloorOnly")]
		public List<string> floorOnlyNamesList = new List<string>();

		// Token: 0x040000EE RID: 238
		[XmlElement("ExceptFloor")]
		public List<string> exceptFloorNamesList = new List<string>();
	}
}
