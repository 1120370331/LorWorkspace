using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x02000026 RID: 38
	[XmlType("PassiveDescRoot")]
	public class PassiveDescRoot_V2 : XmlRoot
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00003FD0 File Offset: 0x000021D0
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (PassiveDescRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(PassiveDesc), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					PassiveDescRoot_V2._serializer = new XmlSerializer(typeof(PassiveDescRoot_V2), xmlAttributeOverrides);
				}
				return PassiveDescRoot_V2._serializer;
			}
		}

		// Token: 0x04000046 RID: 70
		[XmlElement("PassiveDesc")]
		public List<PassiveDesc> descList;

		// Token: 0x04000047 RID: 71
		private static XmlSerializer _serializer;
	}
}
