using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000043 RID: 67
	[Obsolete]
	public class EnemyUnitClassInfo_New
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000090 RID: 144 RVA: 0x00004DD9 File Offset: 0x00002FD9
		[XmlIgnore]
		public LorId id
		{
			get
			{
				return new LorId(this.workshopID, this._id);
			}
		}

		// Token: 0x040000B1 RID: 177
		[XmlAttribute("ID")]
		public int _id;

		// Token: 0x040000B2 RID: 178
		[XmlIgnore]
		public string workshopID = "";

		// Token: 0x040000B3 RID: 179
		[XmlElement("Name")]
		public string name = string.Empty;

		// Token: 0x040000B4 RID: 180
		[XmlElement("FaceType")]
		public UnitFaceType faceType;

		// Token: 0x040000B5 RID: 181
		[XmlElement("NameID")]
		public int nameId;

		// Token: 0x040000B6 RID: 182
		[XmlElement("MinHeight")]
		public int minHeight;

		// Token: 0x040000B7 RID: 183
		[XmlElement("MaxHeight")]
		public int maxHeight;

		// Token: 0x040000B8 RID: 184
		[XmlElement("Unknown")]
		public bool isUnknown;

		// Token: 0x040000B9 RID: 185
		[XmlElement("Gender")]
		public Gender gender;

		// Token: 0x040000BA RID: 186
		[XmlElement("Retreat")]
		public bool retreat;

		// Token: 0x040000BB RID: 187
		[XmlIgnore]
		public int height;

		// Token: 0x040000BC RID: 188
		[XmlElement("BookId")]
		public List<int> bookId;

		// Token: 0x040000BD RID: 189
		[XmlElement("BodyId")]
		public int bodyId;

		// Token: 0x040000BE RID: 190
		[XmlElement("Exp")]
		public int exp;

		// Token: 0x040000BF RID: 191
		[XmlElement("DropBonus")]
		public float dropBonus;

		// Token: 0x040000C0 RID: 192
		[XmlElement("DropTable")]
		public List<EnemyDropItemTable_New> dropTableList = new List<EnemyDropItemTable_New>();

		// Token: 0x040000C1 RID: 193
		[XmlElement("Emotion")]
		public List<EmotionSetInfo> emotionCardList = new List<EmotionSetInfo>();

		// Token: 0x040000C2 RID: 194
		[XmlElement("AiScript")]
		public string AiScript = "";
	}
}
