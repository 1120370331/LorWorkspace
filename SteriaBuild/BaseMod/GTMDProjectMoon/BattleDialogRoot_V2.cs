using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000030 RID: 48
	[XmlType("BattleDialogRoot")]
	public class BattleDialogRoot_V2 : XmlRoot
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000071 RID: 113 RVA: 0x00004758 File Offset: 0x00002958
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (BattleDialogRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(BattleDialogCharacter), "workshopId", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					BattleDialogRoot_V2._serializer = new XmlSerializer(typeof(BattleDialogRoot_V2), xmlAttributeOverrides);
				}
				return BattleDialogRoot_V2._serializer;
			}
		}

		// Token: 0x04000089 RID: 137
		[XmlElement("GroupName")]
		public string groupName;

		// Token: 0x0400008A RID: 138
		[XmlElement("Character")]
		public List<BattleDialogCharacter_V2> characterList = new List<BattleDialogCharacter_V2>();

		// Token: 0x0400008B RID: 139
		private static XmlSerializer _serializer;
	}
}
