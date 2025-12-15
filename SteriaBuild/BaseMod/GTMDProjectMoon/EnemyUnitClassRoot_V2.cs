using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200003E RID: 62
	[XmlType("EnemyUnitClassRoot")]
	public class EnemyUnitClassRoot_V2 : XmlRoot
	{
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000085 RID: 133 RVA: 0x00004A64 File Offset: 0x00002C64
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (EnemyUnitClassRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(EnemyUnitClassInfo), "bookId", attributes);
					xmlAttributeOverrides.Add(typeof(EnemyUnitClassInfo), "dropTableList", attributes);
					xmlAttributeOverrides.Add(typeof(EnemyUnitClassInfo), "emotionCardList", attributes);
					xmlAttributeOverrides.Add(typeof(EnemyDropItemTable), "dropItemList", attributes);
					xmlAttributeOverrides.Add(typeof(EnemyUnitClassInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					EnemyUnitClassRoot_V2._serializer = new XmlSerializer(typeof(EnemyUnitClassRoot_V2), xmlAttributeOverrides);
				}
				return EnemyUnitClassRoot_V2._serializer;
			}
		}

		// Token: 0x040000A5 RID: 165
		[XmlElement("Enemy")]
		public List<EnemyUnitClassInfo_V2> list;

		// Token: 0x040000A6 RID: 166
		private static XmlSerializer _serializer;
	}
}
