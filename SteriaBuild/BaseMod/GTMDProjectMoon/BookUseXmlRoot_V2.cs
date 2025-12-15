using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200000E RID: 14
	[XmlType("BookUseXmlRoot")]
	public class BookUseXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000035 RID: 53 RVA: 0x000038B8 File Offset: 0x00001AB8
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (BookUseXmlRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(DropBookXmlInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					BookUseXmlRoot_V2._serializer = new XmlSerializer(typeof(BookUseXmlRoot_V2), xmlAttributeOverrides);
				}
				return BookUseXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000016 RID: 22
		[XmlElement("BookUse")]
		public List<DropBookXmlInfo> bookXmlList;

		// Token: 0x04000017 RID: 23
		private static XmlSerializer _serializer;
	}
}
