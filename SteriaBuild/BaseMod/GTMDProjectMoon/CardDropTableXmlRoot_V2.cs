using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200000D RID: 13
	[XmlType("CardDropTableXmlRoot")]
	public class CardDropTableXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00003848 File Offset: 0x00001A48
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (CardDropTableXmlRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(CardDropTableXmlInfo), "workshopId", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					CardDropTableXmlRoot_V2._serializer = new XmlSerializer(typeof(CardDropTableXmlRoot_V2), xmlAttributeOverrides);
				}
				return CardDropTableXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000014 RID: 20
		[XmlElement("DropTable")]
		public List<CardDropTableXmlInfo> dropTableXmlList;

		// Token: 0x04000015 RID: 21
		private static XmlSerializer _serializer;
	}
}
