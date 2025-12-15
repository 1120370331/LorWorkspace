using System;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x0200001C RID: 28
	public class QuestXmlInfo_V2 : QuestXmlInfo
	{
		// Token: 0x06000051 RID: 81 RVA: 0x00003DBF File Offset: 0x00001FBF
		public void InitOldFields()
		{
			this.sephirah = (string.IsNullOrWhiteSpace(this.sephirahName) ? 0 : Tools.MakeEnum<SephirahType>(this.sephirahName));
		}

		// Token: 0x04000036 RID: 54
		[XmlElement("Sephirah")]
		public string sephirahName;
	}
}
