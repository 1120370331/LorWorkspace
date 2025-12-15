using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000051 RID: 81
	[Obsolete]
	public class StageClassInfo_New
	{
		// Token: 0x040000F7 RID: 247
		[XmlAttribute("id")]
		public int _id;

		// Token: 0x040000F8 RID: 248
		[XmlIgnore]
		public string workshopID = "";

		// Token: 0x040000F9 RID: 249
		[XmlElement("Wave")]
		public List<StageWaveInfo> waveList = new List<StageWaveInfo>();

		// Token: 0x040000FA RID: 250
		[XmlElement("StageType")]
		public StageType stageType;

		// Token: 0x040000FB RID: 251
		[XmlElement("MapInfo")]
		public List<string> mapInfo = new List<string>();

		// Token: 0x040000FC RID: 252
		[XmlElement("FloorNum")]
		public int floorNum = 1;

		// Token: 0x040000FD RID: 253
		[XmlElement("Name")]
		public string stageName;

		// Token: 0x040000FE RID: 254
		[XmlElement("Chapter")]
		public int chapter;

		// Token: 0x040000FF RID: 255
		[XmlElement("Invitation")]
		public StageInvitationInfo invitationInfo = new StageInvitationInfo();

		// Token: 0x04000100 RID: 256
		[XmlElement("Condition")]
		public StageExtraCondition_New extraCondition = new StageExtraCondition_New();

		// Token: 0x04000101 RID: 257
		[XmlElement("Story")]
		public List<StageStoryInfo> storyList = new List<StageStoryInfo>();

		// Token: 0x04000102 RID: 258
		[XmlElement("IsChapterLast")]
		public bool isChapterLast;

		// Token: 0x04000103 RID: 259
		[XmlElement("StoryType")]
		public string _storyType;

		// Token: 0x04000104 RID: 260
		[XmlElement("invitationtype")]
		public bool isStageFixedNormal;

		// Token: 0x04000105 RID: 261
		[XmlElement("FloorOnly")]
		public List<SephirahType> floorOnlyList = new List<SephirahType>();

		// Token: 0x04000106 RID: 262
		[XmlElement("ExceptFloor")]
		public List<SephirahType> exceptFloorList = new List<SephirahType>();

		// Token: 0x04000107 RID: 263
		[XmlElement("RewardItems")]
		public List<BookDropItemXmlInfo> _rewardList = new List<BookDropItemXmlInfo>();

		// Token: 0x04000108 RID: 264
		[XmlIgnore]
		public List<BookDropItemInfo> rewardList = new List<BookDropItemInfo>();
	}
}
