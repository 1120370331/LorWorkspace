using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x02000021 RID: 33
	[XmlType("ToolTipXmlRoot")]
	public class ToolTipXmlRoot_V2
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00003E94 File Offset: 0x00002094
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (ToolTipXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(ToolTipXmlInfo), "ID", attributes);
					ToolTipXmlRoot_V2._serializer = new XmlSerializer(typeof(ToolTipXmlRoot_V2), xmlAttributeOverrides);
				}
				return ToolTipXmlRoot_V2._serializer;
			}
		}

		// Token: 0x0400003D RID: 61
		[XmlElement("ToolTip")]
		public List<ToolTipXmlInfo_V2> toolTipXmlList;

		// Token: 0x0400003E RID: 62
		private static XmlSerializer _serializer;
	}
}
