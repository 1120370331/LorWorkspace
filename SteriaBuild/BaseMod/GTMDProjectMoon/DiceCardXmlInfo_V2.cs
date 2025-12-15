using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using BaseMod;
using LOR_DiceSystem;

namespace GTMDProjectMoon
{
	// Token: 0x02000039 RID: 57
	public class DiceCardXmlInfo_V2 : DiceCardXmlInfo
	{
		// Token: 0x0600007E RID: 126 RVA: 0x0000496C File Offset: 0x00002B6C
		public void InitOldFields()
		{
			if (!string.IsNullOrWhiteSpace(this.customCategory))
			{
				this.category = Tools.MakeEnum<BookCategory>(this.customCategory);
			}
			else if (!string.IsNullOrWhiteSpace(this.customCategoryFallback))
			{
				this.category = Tools.MakeEnum<BookCategory>(this.customCategoryFallback);
			}
			this.optionList = (from x in this.customOptionList
			where !string.IsNullOrWhiteSpace(x)
			select Tools.MakeEnum<CardOption>(x)).ToList<CardOption>();
		}

		// Token: 0x0400009E RID: 158
		[XmlElement("Category")]
		public string customCategory;

		// Token: 0x0400009F RID: 159
		[Obsolete("Just use the Category tag; if multiple entries are desired, use the Option tag instead (for cardOptions)", false)]
		[XmlElement("CustomCategory")]
		public string customCategoryFallback;

		// Token: 0x040000A0 RID: 160
		[XmlElement("Option")]
		public List<string> customOptionList = new List<string>();
	}
}
