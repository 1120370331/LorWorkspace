using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200001B RID: 27
	[XmlType("QuestXmlRoot")]
	public class QuestXmlRoot_V2
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00003D24 File Offset: 0x00001F24
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (QuestXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(QuestXmlInfo), "sephirah", attributes);
					XmlAttributes xmlAttributes = new XmlAttributes();
					xmlAttributes.XmlElements.Add(new XmlElementAttribute("Mission", typeof(QuestMissionXmlInfo_V2)));
					xmlAttributeOverrides.Add(typeof(QuestXmlInfo), "missionList", xmlAttributes);
					QuestXmlRoot_V2._serializer = new XmlSerializer(typeof(QuestXmlRoot_V2), xmlAttributeOverrides);
				}
				return QuestXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000034 RID: 52
		[XmlElement("Quest")]
		public List<QuestXmlInfo_V2> list;

		// Token: 0x04000035 RID: 53
		private static XmlSerializer _serializer;
	}
}
