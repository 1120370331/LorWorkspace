using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000016 RID: 22
	[XmlType("AbnormalityCardsRoot")]
	public class AbnormalityCardsRoot_V2
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00003C1C File Offset: 0x00001E1C
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (AbnormalityCardsRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(Sephirah), "sephirahType", attributes);
					AbnormalityCardsRoot_V2._serializer = new XmlSerializer(typeof(AbnormalityCardsRoot_V2), xmlAttributeOverrides);
				}
				return AbnormalityCardsRoot_V2._serializer;
			}
		}

		// Token: 0x0400002E RID: 46
		[XmlElement("Sephirah")]
		public List<Sephirah_V2> sephirahList;

		// Token: 0x0400002F RID: 47
		private static XmlSerializer _serializer;
	}
}
