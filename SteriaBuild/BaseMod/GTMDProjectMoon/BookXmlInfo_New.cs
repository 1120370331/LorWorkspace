using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CustomInvitation;

namespace GTMDProjectMoon
{
	// Token: 0x0200002E RID: 46
	[Obsolete]
	public class BookXmlInfo_New
	{
		// Token: 0x0400005D RID: 93
		[XmlAttribute("ID")]
		public int _id;

		// Token: 0x0400005E RID: 94
		[XmlIgnore]
		public bool isError;

		// Token: 0x0400005F RID: 95
		[XmlIgnore]
		public string workshopID = "";

		// Token: 0x04000060 RID: 96
		[XmlElement("Name")]
		public string InnerName = "";

		// Token: 0x04000061 RID: 97
		[XmlElement("TextId")]
		public int TextId = -1;

		// Token: 0x04000062 RID: 98
		[XmlElement("BookIcon")]
		public string _bookIcon = "";

		// Token: 0x04000063 RID: 99
		[XmlElement("Option")]
		public List<BookOption> optionList = new List<BookOption>();

		// Token: 0x04000064 RID: 100
		[XmlElement("Category")]
		public List<BookCategory> categoryList = new List<BookCategory>();

		// Token: 0x04000065 RID: 101
		[XmlElement("CustomCategory")]
		public List<string> customCategoryList = new List<string>();

		// Token: 0x04000066 RID: 102
		[XmlElement]
		public BookEquipEffect_New EquipEffect = new BookEquipEffect_New();

		// Token: 0x04000067 RID: 103
		[XmlElement("Rarity")]
		public Rarity Rarity;

		// Token: 0x04000068 RID: 104
		[XmlElement("CharacterSkin")]
		public List<string> CharacterSkin = new List<string>();

		// Token: 0x04000069 RID: 105
		[XmlElement("CharacterSkinType")]
		public string skinType = "";

		// Token: 0x0400006A RID: 106
		[XmlElement("SkinGender")]
		public Gender gender = 2;

		// Token: 0x0400006B RID: 107
		[XmlElement("Chapter")]
		public int Chapter = 1;

		// Token: 0x0400006C RID: 108
		[XmlElement("Episode")]
		public LorIdXml episode = new LorIdXml("", -2);

		// Token: 0x0400006D RID: 109
		[XmlElement("RangeType")]
		public EquipRangeType RangeType;

		// Token: 0x0400006E RID: 110
		[XmlElement("NotEquip")]
		public bool canNotEquip;

		// Token: 0x0400006F RID: 111
		[XmlElement("RandomFace")]
		public bool RandomFace;

		// Token: 0x04000070 RID: 112
		[XmlElement("SpeedDiceNum")]
		public int speedDiceNumber = 1;

		// Token: 0x04000071 RID: 113
		[XmlElement("SuccessionPossibleNumber")]
		public int SuccessionPossibleNumber = 9;

		// Token: 0x04000072 RID: 114
		[XmlElement("SoundInfo")]
		public List<BookSoundInfo> motionSoundList;

		// Token: 0x04000073 RID: 115
		[XmlIgnore]
		public int remainRewardValue;
	}
}
