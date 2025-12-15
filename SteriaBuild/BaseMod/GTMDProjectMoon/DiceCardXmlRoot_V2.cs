using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_DiceSystem;

namespace GTMDProjectMoon
{
	// Token: 0x02000038 RID: 56
	[XmlType("DiceCardXmlRoot")]
	public class DiceCardXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600007C RID: 124 RVA: 0x0000487C File Offset: 0x00002A7C
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (DiceCardXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(DiceCardXmlInfo), "category", attributes);
					xmlAttributeOverrides.Add(typeof(DiceCardXmlInfo), "optionList", attributes);
					xmlAttributeOverrides.Add(typeof(DiceCardXmlInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					XmlAttributes xmlAttributes = new XmlAttributes
					{
						XmlIgnore = false
					};
					xmlAttributes.XmlElements.Add(new XmlElementAttribute("CustomCategory"));
					xmlAttributeOverrides.Add(typeof(DiceCardXmlInfo_V2), "customCategoryFallback", xmlAttributes);
					DiceCardXmlRoot_V2._serializer = new XmlSerializer(typeof(DiceCardXmlRoot_V2), xmlAttributeOverrides);
				}
				return DiceCardXmlRoot_V2._serializer;
			}
		}

		// Token: 0x0400009C RID: 156
		[XmlElement("Card")]
		public List<DiceCardXmlInfo_V2> cardXmlList = new List<DiceCardXmlInfo_V2>();

		// Token: 0x0400009D RID: 157
		private static XmlSerializer _serializer;
	}
}
