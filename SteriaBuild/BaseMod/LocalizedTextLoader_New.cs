using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using GTMDProjectMoon;
using LOR_XML;
using Mod;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x0200005F RID: 95
	public static class LocalizedTextLoader_New
	{
		// Token: 0x060000FB RID: 251 RVA: 0x00008110 File Offset: 0x00006310
		private static string GetModdingPath(DirectoryInfo dir, string type, string defaultLocale = "default")
		{
			string text = Path.Combine(dir.FullName, "Localize", TextDataModel.CurrentLanguage, type);
			if (!Directory.Exists(text))
			{
				text = Path.Combine(dir.FullName, "Localize", defaultLocale, type);
			}
			return text;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00008150 File Offset: 0x00006350
		public static void LocalizeTextExport(string path, string outpath, string outname)
		{
			string text = Resources.Load<TextAsset>(path).text;
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", text);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00008187 File Offset: 0x00006387
		public static void LocalizeTextExport_str(string str, string outpath, string outname)
		{
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", str);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000081A7 File Offset: 0x000063A7
		private static bool CheckReExportLock()
		{
			return File.Exists(Harmony_Patch.LocalizePath + "/DeleteThisToExportLocalizeAgain");
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000081BD File Offset: 0x000063BD
		private static void CreateReExportLock()
		{
			File.WriteAllText(Harmony_Patch.LocalizePath + "/DeleteThisToExportLocalizeAgain", "yes");
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000081D8 File Offset: 0x000063D8
		public static void ExportOriginalFiles()
		{
			try
			{
				if (!LocalizedTextLoader_New.CheckReExportLock())
				{
					string text = TextDataModel.CurrentLanguage ?? "cn";
					TextAsset textAsset = Resources.Load<TextAsset>(Harmony_Patch.BuildPath(new string[]
					{
						"LocalizeList"
					}));
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(textAsset.text);
					XmlNode xmlNode = xmlDocument.SelectSingleNode("localize_list");
					XmlNodeList xmlNodeList = xmlNode.SelectNodes("language");
					List<string> list = new List<string>();
					foreach (object obj in xmlNodeList)
					{
						string innerText = ((XmlNode)obj).InnerText;
						list.Add(innerText);
					}
					XmlNodeList xmlNodeList2 = xmlNode.SelectNodes("localize");
					List<string> list2 = new List<string>();
					foreach (object obj2 in xmlNodeList2)
					{
						string innerText2 = ((XmlNode)obj2).InnerText;
						list2.Add(innerText2);
					}
					if (!list.Contains(text))
					{
						Debug.LogError(string.Format("Not supported language {0}", text));
					}
					else
					{
						foreach (string text2 in list2)
						{
							LocalizedTextLoader_New.ExportOringalLocalizeFile(Harmony_Patch.BuildPath(new string[]
							{
								"Localize/",
								text,
								"/",
								text,
								"_",
								text2
							}));
						}
						LocalizedTextLoader_New.ExportOthers(text);
						LocalizedTextLoader_New.CreateReExportLock();
					}
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/ELerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00008424 File Offset: 0x00006624
		public static void ExportOringalLocalizeFile(string path)
		{
			TextAsset textAsset = Resources.Load<TextAsset>(path);
			LocalizedTextLoader_New.LocalizeTextExport_str(textAsset.text, Harmony_Patch.LocalizePath + "/etc", textAsset.name);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00008458 File Offset: 0x00006658
		public static void ExportOthers(string language)
		{
			LocalizedTextLoader_New.ExportBattleDialogues(language);
			LocalizedTextLoader_New.ExportBattleDialoguesRelations();
			LocalizedTextLoader_New.ExportCharactersName(language);
			LocalizedTextLoader_New.ExportLibrariansName(language);
			LocalizedTextLoader_New.ExportStageName(language);
			LocalizedTextLoader_New.ExportPassiveDesc(language);
			LocalizedTextLoader_New.ExportGiftDesc(language);
			LocalizedTextLoader_New.ExportBattleCardDescriptions(language);
			LocalizedTextLoader_New.ExportBattleCardAbilityDescriptions(language);
			LocalizedTextLoader_New.ExportBattleEffectTexts(language);
			LocalizedTextLoader_New.ExportAbnormalityCardDescriptions(language);
			LocalizedTextLoader_New.ExportAbnormalityAbilityDescription(language);
			LocalizedTextLoader_New.ExportBookDescriptions(language);
			LocalizedTextLoader_New.ExportOpeningLyrics(language);
			LocalizedTextLoader_New.ExportEndingLyrics(language);
			LocalizedTextLoader_New.ExportBossBirdText(language);
			LocalizedTextLoader_New.ExportWhiteNightText(language);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000084CC File Offset: 0x000066CC
		private static void ExportBattleDialogues(string language)
		{
			TextAsset[] array = Resources.LoadAll<TextAsset>("Xml/BattleDialogues/" + language + "/");
			for (int i = 0; i < array.Length; i++)
			{
				LocalizedTextLoader_New.LocalizeTextExport_str(array[i].text, Harmony_Patch.LocalizePath + "/BattleDialogues", array[i].name);
			}
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00008521 File Offset: 0x00006721
		private static void ExportBattleDialoguesRelations()
		{
			LocalizedTextLoader_New.LocalizeTextExport("Xml/BattleDialogues/Book_BattleDlg_Relations", Harmony_Patch.LocalizePath + "/Book_BattleDlg_Relations", "Book_BattleDlg_Relations");
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00008544 File Offset: 0x00006744
		private static void ExportCharactersName(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_CharactersName", language)).text, Harmony_Patch.LocalizePath + "/CharactersName", "CharactersName");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_CreatureName", language), Harmony_Patch.LocalizePath + "/CharactersName", "CreatureName");
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000085A3 File Offset: 0x000067A3
		private static void ExportLibrariansName(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_NormalLibrariansNamePreset", language)).text, Harmony_Patch.LocalizePath + "/NormalLibrariansNamePreset", "NormalLibrariansNamePreset");
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000085D3 File Offset: 0x000067D3
		private static void ExportStageName(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_StageName", language)).text, Harmony_Patch.LocalizePath + "/StageName", "StageName");
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00008604 File Offset: 0x00006804
		private static void ExportPassiveDesc(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_PassiveDesc", language)).text, Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_CreaturePassive", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "CreaturePassive");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_CreaturePassive_Final", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "CreaturePassive_Final");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Eileen", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Eileen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Jaeheon", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Jaeheon");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Oswald", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Oswald");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Elena", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Elena");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Argalia", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Argalia");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Tanya", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Tanya");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Pluto", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Pluto");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Greta", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Greta");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Philip", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Philip");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_Bremen", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_Bremen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_PassiveDesc_Ch7_BandFinal", language), Harmony_Patch.LocalizePath + "/PassiveDesc", "PassiveDesc_Ch7_BandFinal");
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00008813 File Offset: 0x00006A13
		private static void ExportGiftDesc(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_GiftTexts", language)).text, Harmony_Patch.LocalizePath + "/GiftTexts", "GiftTexts");
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00008844 File Offset: 0x00006A44
		private static void ExportBattleCardDescriptions(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{1}_BattleCards", language, language)).text, Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Creature", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Creature");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Creature_Final", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Creature_Final");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Eileen", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Eileen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Jaeheon", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Jaeheon");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Elena", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Elena");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Argalia", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Argalia");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Tanya", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Tanya");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Oswald", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Oswald");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Pluto", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Pluto");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Greta", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Greta");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Philip", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Philip");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_Bremen", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_Bremen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCards_Ch7_BandFinal", language, language), Harmony_Patch.LocalizePath + "/BattlesCards", "BattleCards_Ch7_BandFinal");
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00008A64 File Offset: 0x00006C64
		private static void ExportBattleCardAbilityDescriptions(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities_elena", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities_elena");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities_Eileen", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities_Eileen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities_Argalia", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities_Argalia");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities_Tanya", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities_Tanya");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{1}_BattleCardAbilities_BandFinal", language, language), Harmony_Patch.LocalizePath + "/BattleCardAbilities", "BattleCardAbilities_BandFinal");
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00008B50 File Offset: 0x00006D50
		private static void ExportBattleEffectTexts(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_EffectTexts", language)).text, Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Jaeheon", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Jaeheon");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Oswald", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Oswald");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Pluto", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Pluto");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Greta", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Greta");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Philip", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Philip");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_Bremen", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_Bremen");
			LocalizedTextLoader_New.LocalizeTextExport(string.Format("Xml/Localize/{0}/{0}_EffectTexts_Ch7_BandFinal", language, language), Harmony_Patch.LocalizePath + "/EffectTexts", "EffectTexts_Ch7_BandFinal");
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00008C8E File Offset: 0x00006E8E
		private static void ExportAbnormalityCardDescriptions(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_AbnormalityCards", language)).text, Harmony_Patch.LocalizePath + "/AbnormalityCards", "AbnormalityCards");
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00008CBE File Offset: 0x00006EBE
		private static void ExportAbnormalityAbilityDescription(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_AbnormalityAbilities", language)).text, Harmony_Patch.LocalizePath + "/AbnormalityAbilities", "AbnormalityAbilities");
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00008CEE File Offset: 0x00006EEE
		private static void ExportBookDescriptions(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_Books", language)).text, Harmony_Patch.LocalizePath + "/Books", "Books");
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00008D1E File Offset: 0x00006F1E
		private static void ExportOpeningLyrics(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_OpeningLyrics", language)).text, Harmony_Patch.LocalizePath + "/OpeningLyrics", "_OpeningLyrics");
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00008D4E File Offset: 0x00006F4E
		private static void ExportEndingLyrics(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_EndingLyrics", language)).text, Harmony_Patch.LocalizePath + "/EndingLyrics", "EndingLyrics");
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00008D7E File Offset: 0x00006F7E
		private static void ExportBossBirdText(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_Bossbird", language)).text, Harmony_Patch.LocalizePath + "/BossBirdText", "BossBirdText");
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00008DAE File Offset: 0x00006FAE
		private static void ExportWhiteNightText(string language)
		{
			LocalizedTextLoader_New.LocalizeTextExport_str(Resources.Load<TextAsset>(string.Format("Xml/Localize/{0}/{0}_WhiteNight", language)).text, Harmony_Patch.LocalizePath + "/WhiteNightText", "WhiteNightText");
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00008DE0 File Offset: 0x00006FE0
		public static void LoadModFiles(List<ModContent> loadedContents)
		{
			try
			{
				SplitTrackerDict<string, BookDesc_V2> dict = new SplitTrackerDict<string, BookDesc_V2>(new Comparison<string>(string.Compare));
				SplitTrackerDict<string, BattleDialogRelationWithBookID> dict2 = new SplitTrackerDict<string, BattleDialogRelationWithBookID>(new Comparison<string>(string.Compare));
				foreach (ModContent modContent in loadedContents)
				{
					BasemodConfig basemodConfig = BasemodConfig.FindBasemodConfig(modContent._itemUniqueId);
					if (!basemodConfig.IgnoreLocalize)
					{
						string defaultLocale = basemodConfig.DefaultLocale;
						DirectoryInfo dirInfo = modContent._dirInfo;
						string text = modContent._modInfo.invInfo.workshopInfo.uniqueId;
						if (text.ToLower().EndsWith("@origin"))
						{
							text = "";
						}
						try
						{
							string moddingPath = LocalizedTextLoader_New.GetModdingPath(dirInfo, "etc", defaultLocale);
							DirectoryInfo dir = new DirectoryInfo(moddingPath);
							if (Directory.Exists(moddingPath))
							{
								LocalizedTextLoader_New.LoadLocalizeFile_MOD(dir);
							}
							string moddingPath2 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "Book_BattleDlg_Relations", defaultLocale);
							dir = new DirectoryInfo(moddingPath2);
							if (Directory.Exists(moddingPath2))
							{
								LocalizedTextLoader_New.LoadBattleDialogues_Relations_MOD(dir, text, dict2);
							}
							string moddingPath3 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "BattleDialogues", defaultLocale);
							dir = new DirectoryInfo(moddingPath3);
							if (Directory.Exists(moddingPath3))
							{
								LocalizedTextLoader_New.LoadBattleDialogues_MOD(dir, text);
							}
							string moddingPath4 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "CharactersName", defaultLocale);
							dir = new DirectoryInfo(moddingPath4);
							if (Directory.Exists(moddingPath4))
							{
								LocalizedTextLoader_New.LoadCharactersName_MOD(dir, text);
							}
							string moddingPath5 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "NormalLibrariansNamePreset", defaultLocale);
							dir = new DirectoryInfo(moddingPath5);
							if (Directory.Exists(moddingPath5))
							{
								LocalizedTextLoader_New.LoadLibrariansName_MOD(dir, text);
							}
							string moddingPath6 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "StageName", defaultLocale);
							dir = new DirectoryInfo(moddingPath6);
							if (Directory.Exists(moddingPath6))
							{
								LocalizedTextLoader_New.LoadStageName_MOD(dir, text);
							}
							string moddingPath7 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "PassiveDesc", defaultLocale);
							dir = new DirectoryInfo(moddingPath7);
							if (Directory.Exists(moddingPath7))
							{
								LocalizedTextLoader_New.LoadPassiveDesc_MOD(dir, text);
							}
							string moddingPath8 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "GiftTexts", defaultLocale);
							dir = new DirectoryInfo(moddingPath8);
							if (Directory.Exists(moddingPath8))
							{
								LocalizedTextLoader_New.LoadGiftDesc_MOD(dir);
							}
							string moddingPath9 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "BattlesCards", defaultLocale);
							dir = new DirectoryInfo(moddingPath9);
							if (Directory.Exists(moddingPath9))
							{
								LocalizedTextLoader_New.LoadBattleCardDescriptions_MOD(dir, text);
							}
							string moddingPath10 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "BattleCardAbilities", defaultLocale);
							dir = new DirectoryInfo(moddingPath10);
							if (Directory.Exists(moddingPath10))
							{
								LocalizedTextLoader_New.LoadBattleCardAbilityDescriptions_MOD(dir);
							}
							string moddingPath11 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "EffectTexts", defaultLocale);
							dir = new DirectoryInfo(moddingPath11);
							if (Directory.Exists(moddingPath11))
							{
								LocalizedTextLoader_New.LoadBattleEffectTexts_MOD(dir);
							}
							string moddingPath12 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "AbnormalityCards", defaultLocale);
							dir = new DirectoryInfo(moddingPath12);
							if (Directory.Exists(moddingPath12))
							{
								LocalizedTextLoader_New.LoadAbnormalityCardDescriptions_MOD(dir);
							}
							string moddingPath13 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "AbnormalityAbilities", defaultLocale);
							dir = new DirectoryInfo(moddingPath13);
							if (Directory.Exists(moddingPath13))
							{
								LocalizedTextLoader_New.LoadAbnormalityAbilityDescription_MOD(dir);
							}
							string moddingPath14 = LocalizedTextLoader_New.GetModdingPath(dirInfo, "Books", defaultLocale);
							dir = new DirectoryInfo(moddingPath14);
							if (Directory.Exists(moddingPath14))
							{
								LocalizedTextLoader_New.LoadBookDescriptions_MOD(dir, text, dict);
							}
						}
						catch (Exception ex)
						{
							Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
							File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_LTLerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
						}
					}
				}
				LocalizedTextLoader_New.AddBookDescriptions_MOD(dict);
				LocalizedTextLoader_New.AddDialogRelations_MOD(dict2);
				Tools.CallOnLoadLocalize(TextDataModel.CurrentLanguage);
			}
			catch (Exception ex2)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex2.Message + Environment.NewLine + ex2.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/LTLerror.log", ex2.Message + Environment.NewLine + ex2.StackTrace);
			}
		}

		// Token: 0x06000115 RID: 277 RVA: 0x000091C0 File Offset: 0x000073C0
		public static void LoadLocalizeFile_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadLocalizeFile_MOD_Checking(dir, TextDataModel.textDic);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadLocalizeFile_MOD(directories[i]);
				}
			}
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000921C File Offset: 0x0000741C
		public static void LoadLocalizeFile_MOD_Checking(DirectoryInfo dir, Dictionary<string, string> dic)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(File.ReadAllText(fileInfo.FullName));
					foreach (object obj in xmlDocument.SelectNodes("localize/text"))
					{
						XmlNode xmlNode = (XmlNode)obj;
						XmlNode namedItem = xmlNode.Attributes.GetNamedItem("id");
						string key = ((namedItem != null) ? namedItem.InnerText : null) ?? string.Empty;
						dic[key] = xmlNode.InnerText;
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000930C File Offset: 0x0000750C
		private static void LoadBattleDialogues_Relations_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<string, BattleDialogRelationWithBookID> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBattleDialogues_Relations_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBattleDialogues_Relations_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00009368 File Offset: 0x00007568
		private static void LoadBattleDialogues_Relations_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<string, BattleDialogRelationWithBookID> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (BattleDialogRelationWithBookID battleDialogRelationWithBookID in ((BattleDialogRelationRoot_V2)new XmlSerializer(typeof(BattleDialogRelationRoot_V2)).Deserialize(stringReader)).CopyBattleDialogRelationNew(uniqueId))
						{
							if (!string.IsNullOrWhiteSpace(battleDialogRelationWithBookID.groupName))
							{
								TrackerDict<BattleDialogRelationWithBookID> trackerDict;
								if (!dict.TryGetValue(battleDialogRelationWithBookID.groupName, out trackerDict))
								{
									trackerDict = (dict[battleDialogRelationWithBookID.groupName] = new TrackerDict<BattleDialogRelationWithBookID>());
								}
								trackerDict[new LorId(battleDialogRelationWithBookID.bookID)] = new AddTracker<BattleDialogRelationWithBookID>(battleDialogRelationWithBookID);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00009490 File Offset: 0x00007690
		private static void AddDialogRelations_MOD(SplitTrackerDict<string, BattleDialogRelationWithBookID> dict)
		{
			OptimizedReplacer.AddOrReplace<string, BattleDialogRelationWithBookID, BattleDialogRelationWithBookID>(dict, Singleton<BattleDialogXmlList>.Instance._relationList, (BattleDialogRelationWithBookID relation) => relation.bookID, (BattleDialogRelationWithBookID relation) => relation.groupName, null);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x000094EC File Offset: 0x000076EC
		private static void LoadBattleDialogues_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBattleDialogues_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBattleDialogues_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00009544 File Offset: 0x00007744
		private static void LoadBattleDialogues_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<string, BattleDialogRoot> dictionary = Singleton<BattleDialogXmlList>.Instance._dictionary;
			List<BattleDialogCharacter> list = new List<BattleDialogCharacter>();
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						BattleDialogRoot_V2 battleDialogRoot_V = (BattleDialogRoot_V2)BattleDialogRoot_V2.Serializer.Deserialize(stringReader);
						if (!string.IsNullOrWhiteSpace(battleDialogRoot_V.groupName))
						{
							BattleDialogRoot battleDialogRoot = battleDialogRoot_V.CopyBattleDialogRootNew(uniqueId);
							dictionary[battleDialogRoot_V.groupName] = battleDialogRoot;
							list.AddRange(battleDialogRoot.characterList);
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
			Singleton<BattleDialogXmlList>.Instance.AddDialogByMod(list);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00009630 File Offset: 0x00007830
		private static void LoadCharactersName_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadCharactersName_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadCharactersName_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00009688 File Offset: 0x00007888
		private static void LoadCharactersName_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<int, string> dictionary = Singleton<CharactersNameXmlList>.Instance._dictionary;
			Dictionary<LorId, string> characterNameDic = OrcTools.CharacterNameDic;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						CharactersNameRoot_V2 charactersNameRoot_V = (CharactersNameRoot_V2)new XmlSerializer(typeof(CharactersNameRoot_V2)).Deserialize(stringReader);
						foreach (CharacterName_V2 characterName_V in charactersNameRoot_V.nameList)
						{
							characterName_V.workshopId = Tools.ClarifyWorkshopId(characterName_V.workshopId, charactersNameRoot_V.customPid, uniqueId);
							LorId lorId = characterName_V.lorId;
							if (lorId.IsBasic())
							{
								dictionary[characterName_V.ID] = characterName_V.name;
							}
							else
							{
								characterNameDic[lorId] = characterName_V.name;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x0600011E RID: 286 RVA: 0x000097D0 File Offset: 0x000079D0
		private static void LoadLibrariansName_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadLibrariansName_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadLibrariansName_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00009828 File Offset: 0x00007A28
		private static void LoadLibrariansName_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<int, string> dictionary = Singleton<CharactersNameXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						CharactersNameRoot_V2 charactersNameRoot_V = (CharactersNameRoot_V2)new XmlSerializer(typeof(CharactersNameRoot_V2)).Deserialize(stringReader);
						foreach (CharacterName_V2 characterName_V in charactersNameRoot_V.nameList)
						{
							characterName_V.workshopId = Tools.ClarifyWorkshopIdLegacy(characterName_V.workshopId, charactersNameRoot_V.customPid, uniqueId);
							if (characterName_V.lorId.IsBasic())
							{
								dictionary[characterName_V.ID] = characterName_V.name;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00009954 File Offset: 0x00007B54
		private static void LoadStageName_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadStageName_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadStageName_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x06000121 RID: 289 RVA: 0x000099AC File Offset: 0x00007BAC
		private static void LoadStageName_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<int, string> dictionary = Singleton<StageNameXmlList>.Instance._dictionary;
			Dictionary<LorId, string> stageNameDic = OrcTools.StageNameDic;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						CharactersNameRoot_V2 charactersNameRoot_V = (CharactersNameRoot_V2)new XmlSerializer(typeof(CharactersNameRoot_V2)).Deserialize(stringReader);
						foreach (CharacterName_V2 characterName_V in charactersNameRoot_V.nameList)
						{
							characterName_V.workshopId = Tools.ClarifyWorkshopId(characterName_V.workshopId, charactersNameRoot_V.customPid, uniqueId);
							LorId lorId = characterName_V.lorId;
							if (lorId.IsBasic())
							{
								dictionary[characterName_V.ID] = characterName_V.name;
							}
							else
							{
								stageNameDic[lorId] = characterName_V.name;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00009AF4 File Offset: 0x00007CF4
		private static void LoadPassiveDesc_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadPassiveDesc_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadPassiveDesc_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00009B4C File Offset: 0x00007D4C
		private static void LoadPassiveDesc_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<LorId, PassiveDesc> dictionary = Singleton<PassiveDescXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						PassiveDescRoot_V2 passiveDescRoot_V = (PassiveDescRoot_V2)PassiveDescRoot_V2.Serializer.Deserialize(stringReader);
						foreach (PassiveDesc passiveDesc in passiveDescRoot_V.descList)
						{
							passiveDesc.workshopID = Tools.ClarifyWorkshopId(passiveDesc.workshopID, passiveDescRoot_V.customPid, uniqueId);
							dictionary[passiveDesc.ID] = passiveDesc;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00009C58 File Offset: 0x00007E58
		private static void LoadGiftDesc_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadGiftDesc_MOD_Checking(dir);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadGiftDesc_MOD(directories[i]);
				}
			}
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00009CB0 File Offset: 0x00007EB0
		private static void LoadGiftDesc_MOD_Checking(DirectoryInfo dir)
		{
			Dictionary<string, GiftText> dictionary = Singleton<GiftDescXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (GiftText giftText in ((GiftTextRoot)new XmlSerializer(typeof(GiftTextRoot)).Deserialize(stringReader)).giftList)
						{
							dictionary[giftText.id] = giftText;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00009DA8 File Offset: 0x00007FA8
		private static void LoadBattleCardDescriptions_MOD(DirectoryInfo dir, string uniqueId)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBattleCardDescriptions_MOD_Checking(dir, uniqueId);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBattleCardDescriptions_MOD(directories[i], uniqueId);
				}
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00009E00 File Offset: 0x00008000
		private static void LoadBattleCardDescriptions_MOD_Checking(DirectoryInfo dir, string uniqueId)
		{
			Dictionary<LorId, BattleCardDesc> dictionary = Singleton<BattleCardDescXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						BattleCardDescRoot_V2 battleCardDescRoot_V = (BattleCardDescRoot_V2)new XmlSerializer(typeof(BattleCardDescRoot_V2)).Deserialize(stringReader);
						foreach (BattleCardDesc_V2 battleCardDesc_V in battleCardDescRoot_V.cardDescList)
						{
							battleCardDesc_V.workshopId = Tools.ClarifyWorkshopId(battleCardDesc_V.workshopId, battleCardDescRoot_V.customPid, uniqueId);
							dictionary[battleCardDesc_V.lorId] = battleCardDesc_V;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00009F18 File Offset: 0x00008118
		private static void LoadBattleCardAbilityDescriptions_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBattleCardAbilityDescriptions_MOD_Checking(dir);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBattleCardAbilityDescriptions_MOD(directories[i]);
				}
			}
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00009F70 File Offset: 0x00008170
		private static void LoadBattleCardAbilityDescriptions_MOD_Checking(DirectoryInfo dir)
		{
			Dictionary<string, BattleCardAbilityDesc> dictionary = Singleton<BattleCardAbilityDescXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (BattleCardAbilityDesc battleCardAbilityDesc in ((BattleCardAbilityDescRoot)new XmlSerializer(typeof(BattleCardAbilityDescRoot)).Deserialize(stringReader)).cardDescList)
						{
							dictionary[battleCardAbilityDesc.id] = battleCardAbilityDesc;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000A068 File Offset: 0x00008268
		private static void LoadBattleEffectTexts_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBattleEffectTexts_MOD_Checking(dir);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBattleEffectTexts_MOD(directories[i]);
				}
			}
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000A0C0 File Offset: 0x000082C0
		private static void LoadBattleEffectTexts_MOD_Checking(DirectoryInfo dir)
		{
			Dictionary<string, BattleEffectText> dictionary = Singleton<BattleEffectTextsXmlList>._instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (BattleEffectText battleEffectText in ((BattleEffectTextRoot)new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stringReader)).effectTextList)
						{
							dictionary[battleEffectText.ID] = battleEffectText;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000A1B8 File Offset: 0x000083B8
		private static void LoadAbnormalityCardDescriptions_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadAbnormalityCardDescriptions_MOD_Checking(dir);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadAbnormalityCardDescriptions_MOD(directories[i]);
				}
			}
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000A210 File Offset: 0x00008410
		private static void LoadAbnormalityCardDescriptions_MOD_Checking(DirectoryInfo dir)
		{
			Dictionary<string, AbnormalityCard> dictionary = Singleton<AbnormalityCardDescXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (Sephirah_V2 sephirah_V in ((AbnormalityCardsRoot_V2)AbnormalityCardsRoot_V2.Serializer.Deserialize(stringReader)).sephirahList)
						{
							sephirah_V.InitOldFields();
							foreach (AbnormalityCard abnormalityCard in sephirah_V.list)
							{
								dictionary[abnormalityCard.id] = abnormalityCard;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000A340 File Offset: 0x00008540
		private static void LoadAbnormalityAbilityDescription_MOD(DirectoryInfo dir)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadAbnormalityAbilityDescription_MOD_Checking(dir);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadAbnormalityAbilityDescription_MOD(directories[i]);
				}
			}
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000A398 File Offset: 0x00008598
		private static void LoadAbnormalityAbilityDescription_MOD_Checking(DirectoryInfo dir)
		{
			Dictionary<string, AbnormalityAbilityText> dictionary = Singleton<AbnormalityAbilityTextXmlList>.Instance._dictionary;
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						foreach (AbnormalityAbilityText abnormalityAbilityText in ((AbnormalityAbilityRoot)new XmlSerializer(typeof(AbnormalityAbilityRoot)).Deserialize(stringReader)).abnormalityList)
						{
							dictionary[abnormalityAbilityText.id] = abnormalityAbilityText;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x0000A490 File Offset: 0x00008690
		private static void LoadBookDescriptions_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<string, BookDesc_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("BasemodLocalize: loading folder at " + dir.FullName);
			}
			LocalizedTextLoader_New.LoadBookDescriptions_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					LocalizedTextLoader_New.LoadBookDescriptions_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000A4EC File Offset: 0x000086EC
		private static void LoadBookDescriptions_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<string, BookDesc_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(fileInfo.FullName)))
					{
						BookDescRoot_V2 bookDescRoot_V = (BookDescRoot_V2)new XmlSerializer(typeof(BookDescRoot_V2)).Deserialize(stringReader);
						foreach (BookDesc_V2 bookDesc_V in bookDescRoot_V.bookDescList)
						{
							bookDesc_V.workshopId = Tools.ClarifyWorkshopId(bookDesc_V.workshopId, bookDescRoot_V.customPid, uniqueId);
							TrackerDict<BookDesc_V2> trackerDict;
							if (!dict.TryGetValue(bookDesc_V.workshopId, out trackerDict))
							{
								trackerDict = (dict[bookDesc_V.workshopId] = new TrackerDict<BookDesc_V2>());
							}
							trackerDict[bookDesc_V.lorId] = new AddTracker<BookDesc_V2>(bookDesc_V);
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("BasemodLocalize: error loading file at " + fileInfo.FullName);
					Debug.LogException(ex);
				}
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000A620 File Offset: 0x00008820
		private static void AddBookDescriptions_MOD(SplitTrackerDict<string, BookDesc_V2> dict)
		{
			TrackerDict<BookDesc_V2> dict2;
			if (dict.TryGetValue("", out dict2))
			{
				OptimizedReplacer.AddOrReplace<BookDesc_V2, BookDesc>(dict2, Singleton<BookDescXmlList>.Instance._dictionaryOrigin, (BookDesc book) => book.bookID, null);
			}
			OptimizedReplacer.AddOrReplace<string, BookDesc_V2, BookDesc>(dict, Singleton<BookDescXmlList>.Instance._dictionaryWorkshop, (BookDesc book) => book.bookID, (BookDesc_V2 book) => book.lorId.IsWorkshop());
		}
	}
}
