using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200004C RID: 76
	[XmlType("StageXmlRoot")]
	public class StageXmlRoot_V2 : XmlRoot
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000A8 RID: 168 RVA: 0x00005F08 File Offset: 0x00004108
		[XmlIgnore]
		public static XmlSerializer Serializer
		{
			get
			{
				if (StageXmlRoot_V2._serializer == null)
				{
					XmlAttributes attributes = new XmlAttributes
					{
						XmlIgnore = true
					};
					XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
					xmlAttributeOverrides.Add(typeof(StageClassInfo), "floorOnlyList", attributes);
					xmlAttributeOverrides.Add(typeof(StageClassInfo), "exceptFloorList", attributes);
					xmlAttributeOverrides.Add(typeof(StageClassInfo), "workshopID", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					XmlAttributes xmlAttributes = new XmlAttributes();
					xmlAttributes.XmlElements.Add(new XmlElementAttribute("Wave", typeof(StageWaveInfo_V2)));
					xmlAttributeOverrides.Add(typeof(StageClassInfo), "waveList", xmlAttributes);
					XmlAttributes xmlAttributes2 = new XmlAttributes();
					xmlAttributes2.XmlElements.Add(new XmlElementAttribute("Condition", typeof(StageExtraCondition_V2)));
					xmlAttributeOverrides.Add(typeof(StageClassInfo), "extraCondition", xmlAttributes2);
					xmlAttributeOverrides.Add(typeof(StageWaveInfo), "formationId", attributes);
					xmlAttributeOverrides.Add(typeof(StageWaveInfo), "emotionCardList", attributes);
					xmlAttributeOverrides.Add(typeof(StageExtraCondition), "needClearStageList", attributes);
					xmlAttributeOverrides.Add(typeof(StageStoryInfo), "packageId", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Pid")
					});
					xmlAttributeOverrides.Add(typeof(StageStoryInfo), "chapter", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Chapter")
					});
					xmlAttributeOverrides.Add(typeof(StageStoryInfo), "group", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Group")
					});
					xmlAttributeOverrides.Add(typeof(StageStoryInfo), "episode", new XmlAttributes
					{
						XmlIgnore = false,
						XmlAttribute = new XmlAttributeAttribute("Episode")
					});
					StageXmlRoot_V2._serializer = new XmlSerializer(typeof(StageXmlRoot_V2), xmlAttributeOverrides);
				}
				return StageXmlRoot_V2._serializer;
			}
		}

		// Token: 0x040000EB RID: 235
		[XmlElement("Stage")]
		public List<StageClassInfo_V2> list;

		// Token: 0x040000EC RID: 236
		private static XmlSerializer _serializer;
	}
}
