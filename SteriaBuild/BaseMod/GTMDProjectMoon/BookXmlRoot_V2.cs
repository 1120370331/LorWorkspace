using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000027 RID: 39
	[XmlType("BookXmlRoot")]
	public class BookXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00004040 File Offset: 0x00002240
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (BookXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(BookEquipEffect), "OnlyCard", attributes);
					xmlAttributeOverrides.Add(typeof(BookEquipEffect), "CardList", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "optionList", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "categoryList", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "EquipEffect", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "episode", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "skinType", attributes);
					xmlAttributeOverrides.Add(typeof(BookXmlInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					XmlAttributes xmlAttributes = new XmlAttributes
					{
						XmlIgnore = false
					};
					xmlAttributes.XmlElements.Add(new XmlElementAttribute("CustomCategory"));
					xmlAttributeOverrides.Add(typeof(BookXmlInfo_V2), "customCategoryListFallback", xmlAttributes);
					BookXmlRoot_V2._serializer = new XmlSerializer(typeof(BookXmlRoot_V2), xmlAttributeOverrides);
				}
				return BookXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000048 RID: 72
		[XmlElement("Book")]
		public List<BookXmlInfo_V2> bookXmlList = new List<BookXmlInfo_V2>();

		// Token: 0x04000049 RID: 73
		private static XmlSerializer _serializer;
	}
}
