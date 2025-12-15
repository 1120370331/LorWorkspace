using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000011 RID: 17
	[XmlType("GiftXmlRoot")]
	public class GiftXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600003B RID: 59 RVA: 0x00003A34 File Offset: 0x00001C34
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (GiftXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(GiftXmlInfo), "ScriptList", attributes);
					GiftXmlRoot_V2._serializer = new XmlSerializer(typeof(GiftXmlRoot_V2), xmlAttributeOverrides);
				}
				return GiftXmlRoot_V2._serializer;
			}
		}

		// Token: 0x0400001D RID: 29
		[XmlElement("Gift")]
		public List<GiftXmlInfo_V2> giftXmlList;

		// Token: 0x0400001E RID: 30
		private static XmlSerializer _serializer;
	}
}
