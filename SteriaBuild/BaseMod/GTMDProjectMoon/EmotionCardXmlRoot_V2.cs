using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000014 RID: 20
	[XmlType("EmotionCardXmlRoot")]
	public class EmotionCardXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000040 RID: 64 RVA: 0x00003B28 File Offset: 0x00001D28
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (EmotionCardXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(EmotionCardXmlInfo), "Sephirah", attributes);
					EmotionCardXmlRoot_V2._serializer = new XmlSerializer(typeof(EmotionCardXmlRoot_V2), xmlAttributeOverrides);
				}
				return EmotionCardXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000029 RID: 41
		[XmlElement("EmotionCard")]
		public List<EmotionCardXmlInfo_V2> emotionCardXmlList;

		// Token: 0x0400002A RID: 42
		private static XmlSerializer _serializer;
	}
}
