using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200000F RID: 15
	[XmlType("FloorLevelXmlRoot")]
	public class FloorLevelXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000037 RID: 55 RVA: 0x00003928 File Offset: 0x00001B28
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (FloorLevelXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(FloorLevelXmlInfo), "sephirahType", attributes);
					xmlAttributeOverrides.Add(typeof(FloorLevelXmlInfo), "stageId", attributes);
					FloorLevelXmlRoot_V2._serializer = new XmlSerializer(typeof(FloorLevelXmlRoot_V2), xmlAttributeOverrides);
				}
				return FloorLevelXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000018 RID: 24
		[XmlElement("FloorLevelXmlInfo")]
		public List<FloorLevelXmlInfo_V2> list;

		// Token: 0x04000019 RID: 25
		private static XmlSerializer _serializer;
	}
}
