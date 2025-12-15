using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200002F RID: 47
	[Obsolete]
	public class BookEquipEffect_New
	{
		// Token: 0x04000074 RID: 116
		[XmlElement("HpReduction")]
		public int HpReduction;

		// Token: 0x04000075 RID: 117
		[XmlElement("HP")]
		public int Hp;

		// Token: 0x04000076 RID: 118
		[XmlElement("DeadLine")]
		public int DeadLine;

		// Token: 0x04000077 RID: 119
		[XmlElement]
		public int Break;

		// Token: 0x04000078 RID: 120
		[XmlElement("SpeedMin")]
		public int SpeedMin;

		// Token: 0x04000079 RID: 121
		[XmlElement]
		public int Speed;

		// Token: 0x0400007A RID: 122
		[XmlElement]
		public int SpeedDiceNum;

		// Token: 0x0400007B RID: 123
		[XmlElement]
		public AtkResist SResist = 3;

		// Token: 0x0400007C RID: 124
		[XmlElement]
		public AtkResist PResist = 3;

		// Token: 0x0400007D RID: 125
		[XmlElement]
		public AtkResist HResist = 3;

		// Token: 0x0400007E RID: 126
		[XmlElement]
		public AtkResist SBResist = 3;

		// Token: 0x0400007F RID: 127
		[XmlElement]
		public AtkResist PBResist = 3;

		// Token: 0x04000080 RID: 128
		[XmlElement]
		public AtkResist HBResist = 3;

		// Token: 0x04000081 RID: 129
		public int MaxPlayPoint = 3;

		// Token: 0x04000082 RID: 130
		[XmlElement("StartPlayPoint")]
		public int StartPlayPoint = 3;

		// Token: 0x04000083 RID: 131
		[XmlElement("AddedStartDraw")]
		public int AddedStartDraw;

		// Token: 0x04000084 RID: 132
		[XmlIgnore]
		public int PassiveCost = 10;

		// Token: 0x04000085 RID: 133
		[XmlElement("OnlyCard")]
		public List<LorIdXml> OnlyCard = new List<LorIdXml>();

		// Token: 0x04000086 RID: 134
		[XmlElement("Card")]
		public List<BookSoulCardInfo_New> CardList = new List<BookSoulCardInfo_New>();

		// Token: 0x04000087 RID: 135
		[XmlElement("Passive")]
		public List<LorIdXml> _PassiveList = new List<LorIdXml>();

		// Token: 0x04000088 RID: 136
		[XmlIgnore]
		public List<LorId> PassiveList = new List<LorId>();
	}
}
