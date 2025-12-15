using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000046 RID: 70
	[XmlType("EmotionEgoXmlRoot")]
	public class EmotionEgoXmlRoot_V2 : XmlRoot
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00004E48 File Offset: 0x00003048
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (EmotionEgoXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(EmotionEgoXmlInfo), "Sephirah", attributes);
					xmlAttributeOverrides.Add(typeof(EmotionEgoXmlInfo), "_CardId", attributes);
					xmlAttributeOverrides.Add(typeof(EmotionEgoXmlInfo), "id", attributes);
					EmotionEgoXmlRoot_V2._serializer = new XmlSerializer(typeof(EmotionEgoXmlRoot_V2), xmlAttributeOverrides);
				}
				return EmotionEgoXmlRoot_V2._serializer;
			}
		}

		// Token: 0x040000C8 RID: 200
		[XmlElement("EmotionEgo")]
		public List<EmotionEgoXmlInfo_V2> egoXmlList;

		// Token: 0x040000C9 RID: 201
		private static XmlSerializer _serializer;
	}
}
