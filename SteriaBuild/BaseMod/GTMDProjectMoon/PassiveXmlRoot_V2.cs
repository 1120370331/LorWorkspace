using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000024 RID: 36
	[XmlType("PassiveXmlRoot")]
	public class PassiveXmlRoot_V2 : XmlRoot
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00003F34 File Offset: 0x00002134
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (PassiveXmlRoot_V2._serializer == null)
				{
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(PassiveXmlInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					PassiveXmlRoot_V2._serializer = new XmlSerializer(typeof(PassiveXmlRoot_V2), xmlAttributeOverrides);
				}
				return PassiveXmlRoot_V2._serializer;
			}
		}

		// Token: 0x04000041 RID: 65
		[XmlElement("Passive")]
		public List<PassiveXmlInfo_V2> list;

		// Token: 0x04000042 RID: 66
		private static XmlSerializer _serializer;
	}
}
