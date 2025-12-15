using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200004E RID: 78
	public class StageExtraCondition_V2 : StageExtraCondition
	{
		// Token: 0x060000AD RID: 173 RVA: 0x000063B8 File Offset: 0x000045B8
		public void InitOldFields(string uniqueId)
		{
			LorId.InitializeLorIds<LorIdXml>(this.needClearStageListNew, this.stagecondition, uniqueId);
			foreach (LorId lorId in this.stagecondition)
			{
				if (lorId.IsBasic())
				{
					this.needClearStageList.Add(lorId.id);
				}
			}
			OrcTools.StageConditionDic[this] = this.stagecondition;
		}

		// Token: 0x040000EF RID: 239
		[XmlElement("Stage")]
		public List<LorIdXml> needClearStageListNew = new List<LorIdXml>();

		// Token: 0x040000F0 RID: 240
		[XmlIgnore]
		public List<LorId> stagecondition = new List<LorId>();
	}
}
