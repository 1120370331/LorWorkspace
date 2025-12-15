using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using BaseMod;

namespace GTMDProjectMoon
{
	// Token: 0x02000028 RID: 40
	public class BookXmlInfo_V2 : BookXmlInfo
	{
		// Token: 0x06000065 RID: 101 RVA: 0x0000419C File Offset: 0x0000239C
		public void InitOldFields(string packageId)
		{
			this.workshopID = packageId;
			this.EquipEffectNew.InitOldFields(packageId);
			this.EquipEffect = this.EquipEffectNew;
			if (this.EquipEffectNew.OnlyCards.Count > 0)
			{
				OrcTools.OnlyCardDic[base.id] = this.EquipEffectNew.OnlyCards;
			}
			if (this.EquipEffectNew.CardList.Count > 0)
			{
				OrcTools.SoulCardDic[base.id] = this.EquipEffectNew.CardList;
			}
			if (this.episodeXml.xmlId > 0)
			{
				this.LorEpisode = LorId.MakeLorId(this.episodeXml, packageId);
			}
			this.episode = this.LorEpisode.id;
			if (!string.IsNullOrWhiteSpace(this.legacySkinType))
			{
				this.newSkinType = this.legacySkinType;
			}
			if (!string.IsNullOrWhiteSpace(this.newSkinType))
			{
				if (this.newSkinType == "UNKNOWN")
				{
					this.newSkinType = "Lor";
				}
				else if (this.newSkinType == "CUSTOM")
				{
					this.newSkinType = "Custom";
				}
				else if (this.newSkinType == "LOR")
				{
					this.newSkinType = "Lor";
				}
			}
			else if (this.CharacterSkin[0].StartsWith("Custom"))
			{
				this.newSkinType = "Custom";
			}
			else
			{
				this.newSkinType = "Lor";
			}
			this.skinType = this.newSkinType;
			List<string> list = this.customOptionList;
			List<BookOption> list2;
			if (list == null)
			{
				list2 = null;
			}
			else
			{
				list2 = (from x in list
				where !string.IsNullOrWhiteSpace(x)
				select Tools.MakeEnum<BookOption>(x)).ToList<BookOption>();
			}
			this.optionList = (list2 ?? new List<BookOption>());
			List<string> list3 = this.customCategoryList;
			List<BookCategory> categoryList;
			if (list3 == null)
			{
				categoryList = null;
			}
			else
			{
				categoryList = (from x in list3
				where !string.IsNullOrWhiteSpace(x)
				select Tools.MakeEnum<BookCategory>(x)).ToList<BookCategory>();
			}
			this.categoryList = categoryList;
			if (this.categoryList == null || this.categoryList.Count == 0)
			{
				List<string> list4 = this.customCategoryListFallback;
				List<BookCategory> list5;
				if (list4 == null)
				{
					list5 = null;
				}
				else
				{
					list5 = (from x in list4
					where !string.IsNullOrWhiteSpace(x)
					select Tools.MakeEnum<BookCategory>(x)).ToList<BookCategory>();
				}
				this.categoryList = (list5 ?? new List<BookCategory>());
			}
		}

		// Token: 0x0400004A RID: 74
		[XmlElement("Option")]
		public List<string> customOptionList = new List<string>();

		// Token: 0x0400004B RID: 75
		[XmlElement("Category")]
		public List<string> customCategoryList = new List<string>();

		// Token: 0x0400004C RID: 76
		[XmlElement("CustomCategory")]
		[Obsolete("Just use the Category tag", false)]
		public List<string> customCategoryListFallback;

		// Token: 0x0400004D RID: 77
		[XmlElement("EquipEffect")]
		public BookEquipEffect_V2 EquipEffectNew = new BookEquipEffect_V2();

		// Token: 0x0400004E RID: 78
		[XmlElement("Episode")]
		public LorIdXml episodeXml = new LorIdXml("", -2);

		// Token: 0x0400004F RID: 79
		[XmlIgnore]
		public LorId LorEpisode = LorId.None;

		// Token: 0x04000050 RID: 80
		[XmlElement("SkinType")]
		public string newSkinType = "";

		// Token: 0x04000051 RID: 81
		[XmlElement("CharacterSkinType")]
		public string legacySkinType = "";
	}
}
