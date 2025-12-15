using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200000C RID: 12
	[XmlType("DeckXmlRoot")]
	public class DeckXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000031 RID: 49 RVA: 0x000037D0 File Offset: 0x000019D0
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (DeckXmlRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(DeckXmlInfo), "workshopId", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					DeckXmlRoot_V2._serializer = new XmlSerializer(typeof(DeckXmlRoot_V2), xmlAttributeOverrides);
				}
				return DeckXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000012 RID: 18
		[XmlElement("Deck")]
		public List<DeckXmlInfo> deckXmlList = new List<DeckXmlInfo>();

		// Token: 0x04000013 RID: 19
		private static XmlSerializer _serializer;
	}
}
