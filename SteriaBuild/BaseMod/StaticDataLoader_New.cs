using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using BaseBridge;
using GTMDProjectMoon;
using HarmonyLib;
using LOR_DiceSystem;
using Mod;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x02000060 RID: 96
	public static class StaticDataLoader_New
	{
		// Token: 0x06000133 RID: 307 RVA: 0x0000A6BA File Offset: 0x000088BA
		private static string GetModdingPath(DirectoryInfo dir, string type)
		{
			return dir.FullName + "/StaticInfo/" + type;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0000A6D0 File Offset: 0x000088D0
		private static bool ExistsModdingPath(DirectoryInfo dir, string type, out DirectoryInfo subDir)
		{
			string moddingPath = StaticDataLoader_New.GetModdingPath(dir, type);
			if (Directory.Exists(moddingPath))
			{
				subDir = new DirectoryInfo(moddingPath);
				return true;
			}
			subDir = null;
			return false;
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000A6FC File Offset: 0x000088FC
		public static void StaticDataExport(string path, string outpath, string outname)
		{
			string text = Resources.Load<TextAsset>(path).text;
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", text);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000A733 File Offset: 0x00008933
		public static void StaticDataExport_str(string str, string outpath, string outname)
		{
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", str);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000A753 File Offset: 0x00008953
		private static bool CheckReExportLock()
		{
			return File.Exists(Harmony_Patch.StaticPath + "/DeleteThisToExportStaticAgain");
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000A769 File Offset: 0x00008969
		private static void CreateReExportLock()
		{
			File.WriteAllText(Harmony_Patch.StaticPath + "/DeleteThisToExportStaticAgain", "yes");
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000A784 File Offset: 0x00008984
		public static void ExportOriginalFiles()
		{
			try
			{
				if (!StaticDataLoader_New.CheckReExportLock())
				{
					StaticDataLoader_New.ExportPassive();
					StaticDataLoader_New.ExportCard();
					StaticDataLoader_New.ExportDeck();
					StaticDataLoader_New.ExportBook();
					StaticDataLoader_New.ExportCardDropTable();
					StaticDataLoader_New.ExportDropBook();
					StaticDataLoader_New.ExportGift();
					StaticDataLoader_New.ExportEmotionCard();
					StaticDataLoader_New.ExportEmotionEgo();
					StaticDataLoader_New.ExportToolTip();
					StaticDataLoader_New.ExportTitle();
					StaticDataLoader_New.ExportFormation();
					StaticDataLoader_New.ExportQuest();
					StaticDataLoader_New.ExportEnemyUnit();
					StaticDataLoader_New.ExportStage();
					StaticDataLoader_New.ExportFloorInfo();
					StaticDataLoader_New.ExportFinalRewardInfo();
					StaticDataLoader_New.ExportCreditInfo();
					StaticDataLoader_New.ExportResourceInfo();
					StaticDataLoader_New.ExportAttackEffectInfo();
					StaticDataLoader_New.CreateReExportLock();
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
				File.WriteAllText(Application.dataPath + "/Mods/ESDerror.log", ex.ToString());
			}
		}

		// Token: 0x0600013A RID: 314 RVA: 0x0000A840 File Offset: 0x00008A40
		public static void ExportPassive()
		{
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_Creature", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_Creature");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Philip", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Philip");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Eileen", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Eileen");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Greta", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Greta");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Bremen", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Bremen");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Oswald", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Oswald");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Jaeheon", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Jaeheon");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Elena", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Elena");
			StaticDataLoader_New.StaticDataExport("Xml/PassiveList_ch7_Pluto", Harmony_Patch.StaticPath + "/PassiveList", "PassiveList_ch7_Pluto");
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000A97C File Offset: 0x00008B7C
		private static void ExportCard()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_Basic", Harmony_Patch.StaticPath + "/Card", "CardInfo_Basic");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch1", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch2", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch3", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch4", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch5", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch5_2", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch5_2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch6", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch6_2", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch6_2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch6_3", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch6_3");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_special", Harmony_Patch.StaticPath + "/Card", "CardInfo_special");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ego", Harmony_Patch.StaticPath + "/Card", "CardInfo_ego");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ego_whitenight", Harmony_Patch.StaticPath + "/Card", "CardInfo_ego_whitenight");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_hod", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_hod");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_netzach", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_netzach");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_binah", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_binah");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_hokma", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_gebura", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_gebura");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_tiphereth", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_tiphereth");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_gebura", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_gebura");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_chesed", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_chesed");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_chesed", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_chesed");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_binah", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_binah");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_hokma", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_creature_final_keter", Harmony_Patch.StaticPath + "/Card", "CardInfo_creature_final_keter");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Philip", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Philip");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Eileen", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Eileen");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Greta", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Greta");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Bremen", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Bremen");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Oswald", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Oswald");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Jaeheon", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Jaeheon");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Elena", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Elena");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Pluto", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Pluto");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Argalia", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Argalia");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Roland2Phase", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Roland2Phase");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_BlackSilence3Phase", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_BlackSilence3Phase");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_Roland4Phase", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_Roland4Phase");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_FinalBand", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_FinalBand");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_ch7_FinalBand_Middle", Harmony_Patch.StaticPath + "/Card", "CardInfo_ch7_FinalBand_Middle");
			StaticDataLoader_New.StaticDataExport("Xml/Card/CardInfo_final", Harmony_Patch.StaticPath + "/Card", "CardInfo_final");
		}

		// Token: 0x0600013C RID: 316 RVA: 0x0000AE94 File Offset: 0x00009094
		private static void ExportDeck()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_basic", Harmony_Patch.StaticPath + "/Deck", "Deck_basic");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_ch1", Harmony_Patch.StaticPath + "/Deck", "Deck_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_ch2", Harmony_Patch.StaticPath + "/Deck", "Deck_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_ch3", Harmony_Patch.StaticPath + "/Deck", "Deck_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_ch4", Harmony_Patch.StaticPath + "/Deck", "Deck_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_ch5", Harmony_Patch.StaticPath + "/Deck", "Deck_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch1", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch2", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch3", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch4", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch5", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch5_2", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch5_2");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch6", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_creature", Harmony_Patch.StaticPath + "/Deck", "Deck_creature");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_creature_final", Harmony_Patch.StaticPath + "/Deck", "Deck_creature_final");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_creature_hokma", Harmony_Patch.StaticPath + "/Deck", "Deck_creature_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Philip", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Philip");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Eileen", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Eileen");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Greta", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Greta");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Bremen", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Bremen");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Oswald", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Oswald");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Jaeheon", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Jaeheon");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Elena", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Elena");
			StaticDataLoader_New.StaticDataExport("Xml/Card/Deck_enemy_ch7_Pluto", Harmony_Patch.StaticPath + "/Deck", "Deck_enemy_ch7_Pluto");
		}

		// Token: 0x0600013D RID: 317 RVA: 0x0000B190 File Offset: 0x00009390
		public static void ExportBook()
		{
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_basic", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_basic");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch1", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch2", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch3", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch4", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch5", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch6", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_ch7", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_ch7");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch1", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch2", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch3", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch4", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch5", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch5_2", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch5_2");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch6", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch6_2", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch6_2");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch6_R", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch6_R");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_hokma", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_hod", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_hod");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_netzach", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_netzach");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_gebura", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_gebura");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_tiphereth", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_tiphereth");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_gebura", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_gebura");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_chesed", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_chesed");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_binah", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_binah");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_hokma", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_creature_final_keter", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_creature_final_keter");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Philip", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Philip");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Eileen", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Eileen");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Greta", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Greta");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Bremen", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Bremen");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Oswald", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Oswald");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Jaeheon", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Jaeheon");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Elena", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Elena");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_Pluto", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_Pluto");
			StaticDataLoader_New.StaticDataExport("Xml/EquipPage_enemy_ch7_BandFinal", Harmony_Patch.StaticPath + "/EquipPage", "EquipPage_enemy_ch7_BandFinal");
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000B630 File Offset: 0x00009830
		public static void ExportCardDropTable()
		{
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch1", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch2", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch3", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch4", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch5", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch6", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/CardDropTable_ch7", Harmony_Patch.StaticPath + "/CardDropTable", "CardDropTable_ch7");
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000B710 File Offset: 0x00009910
		public static void ExportDropBook()
		{
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch1", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch1");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch2", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch3", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch4", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch5", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch6", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/DropBook_ch7", Harmony_Patch.StaticPath + "/DropBook", "DropBook_ch7");
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000B7EF File Offset: 0x000099EF
		public static void ExportGift()
		{
			StaticDataLoader_New.StaticDataExport("Xml/GiftInfo", Harmony_Patch.StaticPath + "/GiftInfo", "GiftInfo");
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000B810 File Offset: 0x00009A10
		public static void ExportEmotionCard()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_keter", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_keter");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_malkuth", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_malkuth");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_yesod", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_yesod");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_hod", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_hod");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_netzach", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_netzach");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_tiphereth", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_tiphereth");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_geburah", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_geburah");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_chesed", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_chesed");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_enemy", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_enemy");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_hokma", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_hokma");
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionCard_binah", Harmony_Patch.StaticPath + "/EmotionCard", "EmotionCard_binah");
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000B967 File Offset: 0x00009B67
		public static void ExportEmotionEgo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Card/EmotionCard/EmotionEgo", Harmony_Patch.StaticPath + "/EmotionEgo", "EmotionEgo");
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000B987 File Offset: 0x00009B87
		public static void ExportToolTip()
		{
			StaticDataLoader_New.StaticDataExport("Xml/XmlToolTips", Harmony_Patch.StaticPath + "/XmlToolTips", "XmlToolTips");
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000B9A7 File Offset: 0x00009BA7
		public static void ExportTitle()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Titles", Harmony_Patch.StaticPath + "/Titles", "Titles");
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000B9C7 File Offset: 0x00009BC7
		public static void ExportFormation()
		{
			StaticDataLoader_New.StaticDataExport("Xml/FormationInfo", Harmony_Patch.StaticPath + "/FormationInfo", "FormationInfo");
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000B9E7 File Offset: 0x00009BE7
		public static void ExportQuest()
		{
			StaticDataLoader_New.StaticDataExport("Xml/QuestInfo", Harmony_Patch.StaticPath + "/QuestInfo", "QuestInfo");
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000BA08 File Offset: 0x00009C08
		public static void ExportEnemyUnit()
		{
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch2", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch2");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch3", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch3");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch4", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch4");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch5", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch5");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch5_2", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch5_2");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch6", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch6");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_creature", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_creature");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_creature_final", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_creature_final");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Philip", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Philip");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Eileen", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Eileen");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Greta", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Greta");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Bremen", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Bremen");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Oswald", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Oswald");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Jaeheon", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Jaeheon");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Elena", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Elena");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_Pluto", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_Pluto");
			StaticDataLoader_New.StaticDataExport("Xml/EnemyUnitInfo_ch7_BandFinal", Harmony_Patch.StaticPath + "/EnemyUnitInfo", "EnemyUnitInfo_ch7_BandFinal");
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000BC50 File Offset: 0x00009E50
		public static void ExportStage()
		{
			StaticDataLoader_New.StaticDataExport("Xml/StageInfo", Harmony_Patch.StaticPath + "/StageInfo", "StageInfo");
			StaticDataLoader_New.StaticDataExport("Xml/StageInfo_creature", Harmony_Patch.StaticPath + "/StageInfo", "StageInfo_creature");
			StaticDataLoader_New.StaticDataExport("Xml/StageInfo_normal", Harmony_Patch.StaticPath + "/StageInfo", "StageInfo_normal");
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000BCB7 File Offset: 0x00009EB7
		public static void ExportFloorInfo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/FloorLevelInfo", Harmony_Patch.StaticPath + "/FloorLevelInfo", "FloorLevelInfo");
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000BCD7 File Offset: 0x00009ED7
		public static void ExportFinalRewardInfo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/Card/FinalBandReward", Harmony_Patch.StaticPath + "/FinalBandReward", "FinalBandReward");
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000BCF7 File Offset: 0x00009EF7
		public static void ExportCreditInfo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/EndingCredit/CreditPerson", Harmony_Patch.StaticPath + "/EndingCredit", "CreditPerson");
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000BD17 File Offset: 0x00009F17
		public static void ExportResourceInfo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/ResourcesInfo", Harmony_Patch.StaticPath + "/ResourceInfo", "ResourceInfo");
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000BD37 File Offset: 0x00009F37
		public static void ExportAttackEffectInfo()
		{
			StaticDataLoader_New.StaticDataExport("Xml/AttackEffectPathInfo", Harmony_Patch.StaticPath + "/AttackEffectPathInfo", "AttackEffectPathInfo");
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000BD58 File Offset: 0x00009F58
		public static void LoadModFiles(List<ModContent> loadedContents)
		{
			try
			{
				List<ModContent> list = new List<ModContent>();
				foreach (ModContent modContent in loadedContents)
				{
					if (!BasemodConfig.FindBasemodConfig(modContent._itemUniqueId).IgnoreStaticFiles)
					{
						list.Add(modContent);
					}
				}
				StaticDataLoader_New.LoadAllPassive_MOD(list);
				StaticDataLoader_New.LoadAllCard_MOD(list);
				StaticDataLoader_New.LoadAllDeck_MOD(list);
				StaticDataLoader_New.LoadAllBook_MOD(list);
				StaticDataLoader_New.LoadAllCardDropTable_MOD(list);
				StaticDataLoader_New.LoadAllDropBook_MOD(list);
				StaticDataLoader_New.LoadAllGiftAndTitle_MOD(list);
				StaticDataLoader_New.LoadAllEmotionCard_MOD(list);
				StaticDataLoader_New.LoadAllEmotionEgo_MOD(list);
				StaticDataLoader_New.LoadAllToolTip_MOD(list);
				StaticDataLoader_New.LoadAllFormation_MOD(list);
				StaticDataLoader_New.LoadAllQuest_MOD(list);
				StaticDataLoader_New.LoadAllEnemyUnit_MOD(list);
				StaticDataLoader_New.LoadAllStage_MOD(list);
				StaticDataLoader_New.LoadAllFloorInfo_MOD(list);
				Singleton<BridgeManager>.Instance.bridgeLoadHandler.MarkBridgeReady("BaseMod");
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/LoadStaticInfoError.log", Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0000BE8C File Offset: 0x0000A08C
		private static void LoadAllPassive_MOD(List<ModContent> mods)
		{
			TrackerDict<PassiveXmlInfo_V2> dict = new TrackerDict<PassiveXmlInfo_V2>();
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "PassiveList", out dir))
					{
						StaticDataLoader_New.LoadPassive_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoPassiveError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddPassiveByMod(dict);
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000BF6C File Offset: 0x0000A16C
		private static void LoadPassive_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<PassiveXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadPassive_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadPassive_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000BFC8 File Offset: 0x0000A1C8
		private static void LoadPassive_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<PassiveXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewPassive(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000C0B0 File Offset: 0x0000A2B0
		private static void LoadNewPassive(string str, string uniqueId, TrackerDict<PassiveXmlInfo_V2> dict)
		{
			PassiveXmlRoot_V2 passiveXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				passiveXmlRoot_V = (PassiveXmlRoot_V2)PassiveXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (PassiveXmlInfo_V2 passiveXmlInfo_V in passiveXmlRoot_V.list)
			{
				string workshopID = Tools.ClarifyWorkshopId(passiveXmlInfo_V.workshopID, passiveXmlRoot_V.customPid, uniqueId);
				passiveXmlInfo_V.workshopID = workshopID;
				dict[passiveXmlInfo_V.id] = new AddTracker<PassiveXmlInfo_V2>(passiveXmlInfo_V);
				if (!string.IsNullOrWhiteSpace(passiveXmlInfo_V.CustomInnerType))
				{
					passiveXmlInfo_V.CustomInnerType = passiveXmlInfo_V.CustomInnerType.Trim();
				}
				passiveXmlInfo_V.CopyInnerType = LorId.MakeLorId(passiveXmlInfo_V.CopyInnerTypeXml, "");
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0000C190 File Offset: 0x0000A390
		private static void AddPassiveByMod(TrackerDict<PassiveXmlInfo_V2> dict)
		{
			OptimizedReplacer.AddOrReplace<PassiveXmlInfo_V2, PassiveXmlInfo>(dict, Singleton<PassiveXmlList>.Instance._list, (PassiveXmlInfo p) => p.id, null);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000C1C4 File Offset: 0x0000A3C4
		private static void LoadAllCard_MOD(List<ModContent> mods)
		{
			TrackerDict<DiceCardXmlInfo> dict = new TrackerDict<DiceCardXmlInfo>();
			SplitTrackerDict<string, DiceCardXmlInfo> splitDict = new SplitTrackerDict<string, DiceCardXmlInfo>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "Card", out dir))
					{
						StaticDataLoader_New.LoadCard_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoCardError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddCardInfoByMod(dict, splitDict);
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000C2BC File Offset: 0x0000A4BC
		private static void LoadCard_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<DiceCardXmlInfo> dict, SplitTrackerDict<string, DiceCardXmlInfo> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadCard_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadCard_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000C318 File Offset: 0x0000A518
		private static void LoadCard_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<DiceCardXmlInfo> dict, SplitTrackerDict<string, DiceCardXmlInfo> splitDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewCard(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0000C400 File Offset: 0x0000A600
		private static void LoadNewCard(string str, string uniqueId, TrackerDict<DiceCardXmlInfo> dict, SplitTrackerDict<string, DiceCardXmlInfo> splitDict)
		{
			DiceCardXmlRoot_V2 diceCardXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				diceCardXmlRoot_V = (DiceCardXmlRoot_V2)DiceCardXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			TrackerDict<DiceCardXmlInfo> trackerDict;
			if (!splitDict.TryGetValue(uniqueId, out trackerDict))
			{
				splitDict.Add(uniqueId, trackerDict = new TrackerDict<DiceCardXmlInfo>());
			}
			foreach (DiceCardXmlInfo_V2 diceCardXmlInfo_V in diceCardXmlRoot_V.cardXmlList)
			{
				string workshopID = Tools.ClarifyWorkshopId(diceCardXmlInfo_V.workshopID, diceCardXmlRoot_V.customPid, uniqueId);
				diceCardXmlInfo_V.workshopID = workshopID;
				diceCardXmlInfo_V.InitOldFields();
				dict[diceCardXmlInfo_V.id] = (trackerDict[diceCardXmlInfo_V.id] = new AddTracker<DiceCardXmlInfo>(diceCardXmlInfo_V));
			}
		}

		// Token: 0x06000158 RID: 344 RVA: 0x0000C4E4 File Offset: 0x0000A6E4
		private static void AddCardInfoByMod(TrackerDict<DiceCardXmlInfo> dict, SplitTrackerDict<string, DiceCardXmlInfo> splitDict)
		{
			if (ItemXmlDataList.instance._basicCardList == null)
			{
				ItemXmlDataList.instance._basicCardList = new List<DiceCardXmlInfo>();
			}
			OptimizedReplacer.AddOrReplace<DiceCardXmlInfo, DiceCardXmlInfo>(dict, ItemXmlDataList.instance._basicCardList, (DiceCardXmlInfo card) => card.id, (DiceCardXmlInfo card) => card.optionList.Contains(0));
			if (ItemXmlDataList.instance._cardInfoList == null)
			{
				ItemXmlDataList.instance._cardInfoList = new List<DiceCardXmlInfo>();
			}
			OptimizedReplacer.AddOrReplace<DiceCardXmlInfo, DiceCardXmlInfo>(dict, ItemXmlDataList.instance._cardInfoList, (DiceCardXmlInfo card) => card.id, null);
			if (ItemXmlDataList.instance._cardInfoTable == null)
			{
				ItemXmlDataList.instance._cardInfoTable = new Dictionary<LorId, DiceCardXmlInfo>();
			}
			OptimizedReplacer.AddOrReplace<DiceCardXmlInfo, DiceCardXmlInfo>(dict, ItemXmlDataList.instance._cardInfoTable, (DiceCardXmlInfo card) => card.id, null);
			OptimizedReplacer.AddOrReplace<string, DiceCardXmlInfo, DiceCardXmlInfo>(splitDict, ItemXmlDataList.instance._workshopDict, (DiceCardXmlInfo card) => card.id, null);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0000C620 File Offset: 0x0000A820
		private static void LoadAllDeck_MOD(List<ModContent> mods)
		{
			TrackerDict<DeckXmlInfo> dict = new TrackerDict<DeckXmlInfo>();
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "Deck", out dir))
					{
						StaticDataLoader_New.LoadDeck_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoDeckError.log", ex.ToString());
				}
			}
			if (Singleton<DeckXmlList>.Instance._list == null)
			{
				Singleton<DeckXmlList>.Instance._list = new List<DeckXmlInfo>();
			}
			OptimizedReplacer.AddOrReplace<DeckXmlInfo, DeckXmlInfo>(dict, Singleton<DeckXmlList>.Instance._list, (DeckXmlInfo d) => d.id, null);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000C744 File Offset: 0x0000A944
		private static void LoadDeck_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<DeckXmlInfo> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadDeck_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadDeck_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0000C7A0 File Offset: 0x0000A9A0
		private static void LoadDeck_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<DeckXmlInfo> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewDeck(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000C888 File Offset: 0x0000AA88
		private static void LoadNewDeck(string str, string uniqueId, TrackerDict<DeckXmlInfo> dict)
		{
			DeckXmlRoot_V2 deckXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				deckXmlRoot_V = (DeckXmlRoot_V2)DeckXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (DeckXmlInfo deckXmlInfo in deckXmlRoot_V.deckXmlList)
			{
				string text = Tools.ClarifyWorkshopId(deckXmlInfo.workshopId, deckXmlRoot_V.customPid, uniqueId);
				deckXmlInfo.workshopId = text;
				LorId.InitializeLorIds<LorIdXml>(deckXmlInfo._cardIdList, deckXmlInfo.cardIdList, text);
				dict[deckXmlInfo.id] = new AddTracker<DeckXmlInfo>(deckXmlInfo);
			}
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000C948 File Offset: 0x0000AB48
		private static void LoadAllBook_MOD(List<ModContent> mods)
		{
			TrackerDict<BookXmlInfo_V2> dict = new TrackerDict<BookXmlInfo_V2>();
			SplitTrackerDict<string, BookXmlInfo_V2> splitDict = new SplitTrackerDict<string, BookXmlInfo_V2>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "EquipPage", out dir))
					{
						StaticDataLoader_New.LoadBook_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoEquipPageError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddEquipPageByMod(dict, splitDict);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000CA40 File Offset: 0x0000AC40
		private static void LoadBook_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<BookXmlInfo_V2> dict, SplitTrackerDict<string, BookXmlInfo_V2> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadBook_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadBook_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000CA9C File Offset: 0x0000AC9C
		private static void LoadBook_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<BookXmlInfo_V2> dict, SplitTrackerDict<string, BookXmlInfo_V2> splitDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewCorePage(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0000CB84 File Offset: 0x0000AD84
		private static void LoadNewCorePage(string str, string uniqueId, TrackerDict<BookXmlInfo_V2> dict, SplitTrackerDict<string, BookXmlInfo_V2> splitDict)
		{
			BookXmlRoot_V2 bookXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				bookXmlRoot_V = (BookXmlRoot_V2)BookXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			TrackerDict<BookXmlInfo_V2> trackerDict;
			if (!splitDict.TryGetValue(uniqueId, out trackerDict))
			{
				splitDict.Add(uniqueId, trackerDict = new TrackerDict<BookXmlInfo_V2>());
			}
			foreach (BookXmlInfo_V2 bookXmlInfo_V in bookXmlRoot_V.bookXmlList)
			{
				string packageId = Tools.ClarifyWorkshopId(bookXmlInfo_V.workshopID, bookXmlRoot_V.customPid, uniqueId);
				bookXmlInfo_V.InitOldFields(packageId);
				trackerDict[bookXmlInfo_V.id] = (dict[bookXmlInfo_V.id] = new AddTracker<BookXmlInfo_V2>(bookXmlInfo_V));
			}
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000CC60 File Offset: 0x0000AE60
		private static void AddEquipPageByMod(TrackerDict<BookXmlInfo_V2> dict, SplitTrackerDict<string, BookXmlInfo_V2> splitDict)
		{
			OptimizedReplacer.AddOrReplace<BookXmlInfo_V2, BookXmlInfo>(dict, Singleton<BookXmlList>.Instance._list, (BookXmlInfo ep) => ep.id, null);
			OptimizedReplacer.AddOrReplace<BookXmlInfo_V2, BookXmlInfo>(dict, Singleton<BookXmlList>.Instance._dictionary, (BookXmlInfo ep) => ep.id, null);
			OptimizedReplacer.AddOrReplace<string, BookXmlInfo_V2, BookXmlInfo>(splitDict, Singleton<BookXmlList>.Instance._workshopBookDict, (BookXmlInfo ep) => ep.id, null);
			foreach (AddTracker<BookXmlInfo_V2> addTracker in dict.Values)
			{
				BookXmlInfo_V2 element = addTracker.element;
				if (element.LorEpisode != null)
				{
					OrcTools.EpisodeDic[element.id] = element.LorEpisode;
				}
			}
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000CD68 File Offset: 0x0000AF68
		private static void LoadAllCardDropTable_MOD(List<ModContent> mods)
		{
			TrackerDict<CardDropTableXmlInfo> dict = new TrackerDict<CardDropTableXmlInfo>();
			SplitTrackerDict<string, CardDropTableXmlInfo> splitDict = new SplitTrackerDict<string, CardDropTableXmlInfo>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "CardDropTable", out dir))
					{
						StaticDataLoader_New.LoadCardDropTable_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoCardDropTableError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddCardDropTableByMod(dict, splitDict);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000CE60 File Offset: 0x0000B060
		private static void LoadCardDropTable_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<CardDropTableXmlInfo> dict, SplitTrackerDict<string, CardDropTableXmlInfo> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadCardDropTable_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadCardDropTable_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000CEBC File Offset: 0x0000B0BC
		private static void LoadCardDropTable_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<CardDropTableXmlInfo> dict, SplitTrackerDict<string, CardDropTableXmlInfo> splitDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewCardDropTable(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000CFA4 File Offset: 0x0000B1A4
		private static void LoadNewCardDropTable(string str, string uniqueId, TrackerDict<CardDropTableXmlInfo> dict, SplitTrackerDict<string, CardDropTableXmlInfo> splitDict)
		{
			CardDropTableXmlRoot_V2 cardDropTableXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				cardDropTableXmlRoot_V = (CardDropTableXmlRoot_V2)CardDropTableXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			TrackerDict<CardDropTableXmlInfo> trackerDict;
			if (!splitDict.TryGetValue(uniqueId, out trackerDict))
			{
				splitDict.Add(uniqueId, trackerDict = new TrackerDict<CardDropTableXmlInfo>());
			}
			foreach (CardDropTableXmlInfo cardDropTableXmlInfo in cardDropTableXmlRoot_V.dropTableXmlList)
			{
				string text = Tools.ClarifyWorkshopId(cardDropTableXmlInfo.workshopId, cardDropTableXmlRoot_V.customPid, uniqueId);
				cardDropTableXmlInfo.workshopId = text;
				LorId.InitializeLorIds<LorIdXml>(cardDropTableXmlInfo._cardIdList, cardDropTableXmlInfo.cardIdList, text);
				trackerDict[cardDropTableXmlInfo.id] = (dict[cardDropTableXmlInfo.id] = new AddTracker<CardDropTableXmlInfo>(cardDropTableXmlInfo));
			}
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000D094 File Offset: 0x0000B294
		private static void AddCardDropTableByMod(TrackerDict<CardDropTableXmlInfo> dict, SplitTrackerDict<string, CardDropTableXmlInfo> splitDict)
		{
			if (Singleton<CardDropTableXmlList>.Instance._list == null)
			{
				Singleton<CardDropTableXmlList>.Instance._list = new List<CardDropTableXmlInfo>();
			}
			OptimizedReplacer.AddOrReplace<CardDropTableXmlInfo, CardDropTableXmlInfo>(dict, Singleton<CardDropTableXmlList>.Instance._list, (CardDropTableXmlInfo cdt) => cdt.id, null);
			OptimizedReplacer.AddOrReplace<string, CardDropTableXmlInfo, CardDropTableXmlInfo>(splitDict, Singleton<CardDropTableXmlList>.Instance._workshopDict, (CardDropTableXmlInfo cdt) => cdt.id, null);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000D11C File Offset: 0x0000B31C
		private static void LoadAllDropBook_MOD(List<ModContent> mods)
		{
			TrackerDict<DropBookXmlInfo> dict = new TrackerDict<DropBookXmlInfo>();
			SplitTrackerDict<string, DropBookXmlInfo> splitDict = new SplitTrackerDict<string, DropBookXmlInfo>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "DropBook", out dir))
					{
						StaticDataLoader_New.LoadDropBook_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoDropBookError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddDropBookByMod(dict, splitDict);
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000D214 File Offset: 0x0000B414
		private static void LoadDropBook_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<DropBookXmlInfo> dict, SplitTrackerDict<string, DropBookXmlInfo> splitDict)
		{
			StaticDataLoader_New.LoadDropBook_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadDropBook_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000D254 File Offset: 0x0000B454
		private static void LoadDropBook_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<DropBookXmlInfo> dict, SplitTrackerDict<string, DropBookXmlInfo> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewDropBook(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000D358 File Offset: 0x0000B558
		private static void LoadNewDropBook(string str, string uniqueId, TrackerDict<DropBookXmlInfo> dict, SplitTrackerDict<string, DropBookXmlInfo> splitDict)
		{
			BookUseXmlRoot_V2 bookUseXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				bookUseXmlRoot_V = (BookUseXmlRoot_V2)BookUseXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			TrackerDict<DropBookXmlInfo> trackerDict;
			if (!splitDict.TryGetValue(uniqueId, out trackerDict))
			{
				splitDict.Add(uniqueId, trackerDict = new TrackerDict<DropBookXmlInfo>());
			}
			foreach (DropBookXmlInfo dropBookXmlInfo in bookUseXmlRoot_V.bookXmlList)
			{
				string text = Tools.ClarifyWorkshopId(dropBookXmlInfo.workshopID, bookUseXmlRoot_V.customPid, uniqueId);
				dropBookXmlInfo.workshopID = text;
				dropBookXmlInfo.InitializeDropItemList(text);
				CardDropTableXmlInfo workshopData = Singleton<CardDropTableXmlList>.Instance.GetWorkshopData(text, dropBookXmlInfo.id.id);
				if (workshopData != null)
				{
					foreach (LorId lorId in workshopData.cardIdList)
					{
						dropBookXmlInfo.DropItemList.Add(new BookDropItemInfo(lorId)
						{
							itemType = 0
						});
					}
				}
				trackerDict[dropBookXmlInfo.id] = (dict[dropBookXmlInfo.id] = new AddTracker<DropBookXmlInfo>(dropBookXmlInfo));
			}
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000D4B8 File Offset: 0x0000B6B8
		private static void AddDropBookByMod(TrackerDict<DropBookXmlInfo> dict, SplitTrackerDict<string, DropBookXmlInfo> splitDict)
		{
			OptimizedReplacer.AddOrReplace<DropBookXmlInfo, DropBookXmlInfo>(dict, Singleton<DropBookXmlList>.Instance._list, (DropBookXmlInfo db) => db.id, null);
			OptimizedReplacer.AddOrReplace<DropBookXmlInfo, DropBookXmlInfo>(dict, Singleton<DropBookXmlList>.Instance._dict, (DropBookXmlInfo db) => db.id, null);
			if (Singleton<DropBookXmlList>.Instance._workshopDict == null)
			{
				Singleton<DropBookXmlList>.Instance._workshopDict = new Dictionary<string, List<DropBookXmlInfo>>();
			}
			OptimizedReplacer.AddOrReplace<string, DropBookXmlInfo, DropBookXmlInfo>(splitDict, Singleton<DropBookXmlList>.Instance._workshopDict, (DropBookXmlInfo db) => db.id, null);
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000D570 File Offset: 0x0000B770
		private static void LoadAllGiftAndTitle_MOD(List<ModContent> mods)
		{
			TrackerDict<GiftXmlInfo_V2> trackerDict = new TrackerDict<GiftXmlInfo_V2>();
			TrackerDict<TitleXmlInfo_V2> prefixDict = new TrackerDict<TitleXmlInfo_V2>();
			TrackerDict<TitleXmlInfo_V2> postfixDict = new TrackerDict<TitleXmlInfo_V2>();
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "GiftInfo", out dir))
					{
						StaticDataLoader_New.LoadGift_MOD(dir, text, trackerDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoGiftError.log", ex.ToString());
				}
				try
				{
					DirectoryInfo dir2;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "Titles", out dir2))
					{
						StaticDataLoader_New.LoadTitle_MOD(dir2, text, prefixDict, postfixDict);
					}
				}
				catch (Exception ex2)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex2.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoTitlesError.log", ex2.ToString());
				}
			}
			StaticDataLoader_New.AddGiftAndTitleByMod(trackerDict, prefixDict, postfixDict);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000D6CC File Offset: 0x0000B8CC
		private static void LoadGift_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<GiftXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadGift_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadGift_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000D728 File Offset: 0x0000B928
		private static void LoadGift_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<GiftXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewGift(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000D810 File Offset: 0x0000BA10
		private static void LoadNewGift(string str, string uniqueId, TrackerDict<GiftXmlInfo_V2> dict)
		{
			GiftXmlRoot_V2 giftXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				giftXmlRoot_V = (GiftXmlRoot_V2)GiftXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (GiftXmlInfo_V2 giftXmlInfo_V in giftXmlRoot_V.giftXmlList)
			{
				giftXmlInfo_V.WorkshopId = Tools.ClarifyWorkshopIdLegacy(giftXmlInfo_V.WorkshopId, giftXmlRoot_V.customPid, uniqueId);
				dict[giftXmlInfo_V.lorId] = new AddTracker<GiftXmlInfo_V2>(giftXmlInfo_V);
				OrcTools.CustomGifts[giftXmlInfo_V.lorId] = giftXmlInfo_V;
			}
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000D8C8 File Offset: 0x0000BAC8
		private static void LoadTitle_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<TitleXmlInfo_V2> prefixDict, TrackerDict<TitleXmlInfo_V2> postfixDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadTitle_MOD_Checking(dir, uniqueId, prefixDict, postfixDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadTitle_MOD(directories[i], uniqueId, prefixDict, postfixDict);
				}
			}
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000D924 File Offset: 0x0000BB24
		private static void LoadTitle_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<TitleXmlInfo_V2> prefixDict, TrackerDict<TitleXmlInfo_V2> postfixDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewTitle(File.ReadAllText(fileInfo.FullName), uniqueId, prefixDict, postfixDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000DA0C File Offset: 0x0000BC0C
		private static void LoadNewTitle(string str, string uniqueId, TrackerDict<TitleXmlInfo_V2> prefixDict, TrackerDict<TitleXmlInfo_V2> postfixDict)
		{
			TitleXmlRoot_V2 titleXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				titleXmlRoot_V = (TitleXmlRoot_V2)new XmlSerializer(typeof(TitleXmlRoot_V2)).Deserialize(stringReader);
			}
			StaticDataLoader_New.LoadHalfTitle(titleXmlRoot_V.prefixXmlList.List, uniqueId, titleXmlRoot_V.customPid, prefixDict);
			StaticDataLoader_New.LoadHalfTitle(titleXmlRoot_V.postfixXmlList.List, uniqueId, titleXmlRoot_V.customPid, postfixDict);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0000DA88 File Offset: 0x0000BC88
		private static void LoadHalfTitle(List<TitleXmlInfo_V2> list, string uniqueId, string rootId, TrackerDict<TitleXmlInfo_V2> dict)
		{
			foreach (TitleXmlInfo_V2 titleXmlInfo_V in list)
			{
				titleXmlInfo_V.WorkshopId = Tools.ClarifyWorkshopIdLegacy(titleXmlInfo_V.WorkshopId, rootId, uniqueId);
				dict[titleXmlInfo_V.lorId] = new AddTracker<TitleXmlInfo_V2>(titleXmlInfo_V);
			}
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000DAF4 File Offset: 0x0000BCF4
		private static void AddGiftAndTitleByMod(TrackerDict<GiftXmlInfo_V2> giftDict, TrackerDict<TitleXmlInfo_V2> prefixDict, TrackerDict<TitleXmlInfo_V2> postfixDict)
		{
			foreach (int num in Singleton<GiftXmlList>.Instance._giftDict.Keys)
			{
				AddTracker<GiftXmlInfo_V2> addTracker;
				if (giftDict.TryGetValue(new LorId(num), out addTracker))
				{
					addTracker.element.dontRemove = true;
				}
			}
			OptimizedReplacer.AddOrReplace<GiftXmlInfo_V2, GiftXmlInfo>(giftDict, Singleton<GiftXmlList>.Instance._list, (GiftXmlInfo gift) => gift.id, (GiftXmlInfo_V2 gift) => gift.lorId.IsBasic());
			OptimizedReplacer.AddOrReplace<GiftXmlInfo_V2, GiftXmlInfo>(giftDict, Singleton<GiftXmlList>.Instance._giftDict, (GiftXmlInfo gift) => gift.id, (GiftXmlInfo_V2 gift) => gift.lorId.IsBasic());
			OptimizedReplacer.AddOrReplace<TitleXmlInfo_V2, TitleXmlInfo>(prefixDict, Singleton<TitleXmlList>.Instance._prefixList, (TitleXmlInfo title) => title.ID, (TitleXmlInfo_V2 title) => title.lorId.IsBasic());
			OptimizedReplacer.AddOrReplace<TitleXmlInfo_V2, TitleXmlInfo>(postfixDict, Singleton<TitleXmlList>.Instance._postfixList, (TitleXmlInfo title) => title.ID, (TitleXmlInfo_V2 title) => title.lorId.IsBasic());
			HashSet<int> hashSet = new HashSet<int>();
			int num2 = StaticDataLoader_New.MinInjectedId;
			foreach (GiftXmlInfo giftXmlInfo in Singleton<GiftXmlList>.Instance._list)
			{
				hashSet.Add(giftXmlInfo.id);
			}
			foreach (TitleXmlInfo titleXmlInfo in Singleton<TitleXmlList>.Instance._prefixList)
			{
				hashSet.Add(titleXmlInfo.ID);
			}
			foreach (TitleXmlInfo titleXmlInfo2 in Singleton<TitleXmlList>.Instance._postfixList)
			{
				hashSet.Add(titleXmlInfo2.ID);
			}
			AddTracker<GiftXmlInfo_V2> addTracker2 = null;
			AddTracker<TitleXmlInfo_V2> addTracker3 = null;
			AddTracker<TitleXmlInfo_V2> addTracker4 = null;
			HashSet<LorId> hashSet2 = new HashSet<LorId>();
			hashSet2.UnionWith(giftDict.Keys);
			hashSet2.UnionWith(prefixDict.Keys);
			hashSet2.UnionWith(postfixDict.Keys);
			hashSet2.RemoveWhere((LorId x) => x.IsBasic());
			List<LorId> list = hashSet2.ToList<LorId>();
			list.Sort(OptimizedReplacer.packageSortedComp);
			foreach (LorId key in list)
			{
				if ((!giftDict.TryGetValue(key, out addTracker2) || addTracker2.added) && (!prefixDict.TryGetValue(key, out addTracker3) || addTracker3.added))
				{
					if (!postfixDict.TryGetValue(key, out addTracker4))
					{
						continue;
					}
					if (addTracker4.added)
					{
						continue;
					}
				}
				while (hashSet.Contains(num2))
				{
					num2++;
				}
				if (addTracker2 != null && !addTracker2.added)
				{
					addTracker2.element.InjectId(num2);
					Singleton<GiftXmlList>.Instance._list.Add(addTracker2.element);
					Singleton<GiftXmlList>.Instance._giftDict.Add(num2, addTracker2.element);
				}
				if (addTracker3 != null && !addTracker3.added)
				{
					addTracker3.element.InjectId(num2);
					Singleton<TitleXmlList>.Instance._prefixList.Add(addTracker3.element);
				}
				if (addTracker4 != null && !addTracker4.added)
				{
					addTracker4.element.InjectId(num2);
					Singleton<TitleXmlList>.Instance._postfixList.Add(addTracker4.element);
				}
				num2++;
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000DF70 File Offset: 0x0000C170
		private static void LoadAllEmotionCard_MOD(List<ModContent> mods)
		{
			SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2> dict = new SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2>(OptimizedReplacer.sephirahComp);
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "EmotionCard", out dir))
					{
						StaticDataLoader_New.LoadEmotionCard_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoEmotionCardError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddEmotionCardByMod(dict);
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0000E054 File Offset: 0x0000C254
		private static void LoadEmotionCard_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadEmotionCard_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadEmotionCard_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000E0B0 File Offset: 0x0000C2B0
		private static void LoadEmotionCard_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewEmotionCard(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0000E198 File Offset: 0x0000C398
		private static void LoadNewEmotionCard(string str, string uniqueId, SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2> dict)
		{
			EmotionCardXmlRoot_V2 emotionCardXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				emotionCardXmlRoot_V = (EmotionCardXmlRoot_V2)EmotionCardXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (EmotionCardXmlInfo_V2 emotionCardXmlInfo_V in emotionCardXmlRoot_V.emotionCardXmlList)
			{
				emotionCardXmlInfo_V.InitOldFields();
				emotionCardXmlInfo_V.WorkshopId = Tools.ClarifyWorkshopIdLegacy(emotionCardXmlInfo_V.WorkshopId, emotionCardXmlRoot_V.customPid, uniqueId);
				TrackerDict<EmotionCardXmlInfo_V2> trackerDict;
				if (!dict.TryGetValue(emotionCardXmlInfo_V.Sephirah, out trackerDict))
				{
					trackerDict = (dict[emotionCardXmlInfo_V.Sephirah] = new TrackerDict<EmotionCardXmlInfo_V2>());
				}
				trackerDict[emotionCardXmlInfo_V.lorId] = new AddTracker<EmotionCardXmlInfo_V2>(emotionCardXmlInfo_V);
				Dictionary<LorId, EmotionCardXmlInfo> dictionary;
				if (!OrcTools.CustomEmotionCards.TryGetValue(emotionCardXmlInfo_V.Sephirah, out dictionary))
				{
					dictionary = (OrcTools.CustomEmotionCards[emotionCardXmlInfo_V.Sephirah] = new Dictionary<LorId, EmotionCardXmlInfo>());
				}
				dictionary[emotionCardXmlInfo_V.lorId] = emotionCardXmlInfo_V;
			}
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000E2AC File Offset: 0x0000C4AC
		private static void AddEmotionCardByMod(SplitTrackerDict<SephirahType, EmotionCardXmlInfo_V2> dict)
		{
			Dictionary<SephirahType, HashSet<int>> usedIds = new Dictionary<SephirahType, HashSet<int>>();
			Dictionary<SephirahType, int> minCheckedIds = new Dictionary<SephirahType, int>();
			List<EmotionCardXmlInfo> list = Singleton<EmotionCardXmlList>.Instance._list;
			OptimizedReplacer.AddOrReplaceWithInject<SephirahType, EmotionCardXmlInfo_V2, EmotionCardXmlInfo>(dict, list, (EmotionCardXmlInfo card) => card.id, (EmotionCardXmlInfo card) => card.Sephirah, delegate(EmotionCardXmlInfo_V2 injCard)
			{
				int num;
				if (!minCheckedIds.TryGetValue(injCard.Sephirah, out num))
				{
					num = (minCheckedIds[injCard.Sephirah] = StaticDataLoader_New.MinInjectedId);
				}
				else
				{
					num++;
				}
				HashSet<int> hashSet;
				if (usedIds.TryGetValue(injCard.Sephirah, out hashSet))
				{
					while (hashSet.Contains(num))
					{
						num++;
					}
				}
				minCheckedIds[injCard.Sephirah] = num;
				return num;
			}, delegate(EmotionCardXmlInfo_V2 addCard)
			{
				HashSet<int> hashSet;
				if (!usedIds.TryGetValue(addCard.Sephirah, out hashSet))
				{
					hashSet = (usedIds[addCard.Sephirah] = new HashSet<int>());
				}
				hashSet.Add(addCard.id);
			}, null);
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000E340 File Offset: 0x0000C540
		private static void LoadAllEmotionEgo_MOD(List<ModContent> mods)
		{
			StaticDataLoader_New.FixEmotionEgoIds();
			SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2> dict = new SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2>(OptimizedReplacer.sephirahComp);
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "EmotionEgo", out dir))
					{
						StaticDataLoader_New.LoadEmotionEgo_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoEmotionEgoError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddEmotionEgoByMod(dict);
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000E42C File Offset: 0x0000C62C
		private static void LoadEmotionEgo_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadEmotionEgo_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadEmotionEgo_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000E488 File Offset: 0x0000C688
		private static void LoadEmotionEgo_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewEmotionEgo(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000E570 File Offset: 0x0000C770
		private static void LoadNewEmotionEgo(string str, string uniqueId, SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2> dict)
		{
			EmotionEgoXmlRoot_V2 emotionEgoXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				emotionEgoXmlRoot_V = (EmotionEgoXmlRoot_V2)EmotionEgoXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (EmotionEgoXmlInfo_V2 emotionEgoXmlInfo_V in emotionEgoXmlRoot_V.egoXmlList)
			{
				string packageId = Tools.ClarifyWorkshopId("", emotionEgoXmlRoot_V.customPid, uniqueId);
				emotionEgoXmlInfo_V.InitOldFields(packageId);
				TrackerDict<EmotionEgoXmlInfo_V2> trackerDict;
				if (!dict.TryGetValue(emotionEgoXmlInfo_V.Sephirah, out trackerDict))
				{
					trackerDict = (dict[emotionEgoXmlInfo_V.Sephirah] = new TrackerDict<EmotionEgoXmlInfo_V2>());
				}
				trackerDict[emotionEgoXmlInfo_V.lorId] = new AddTracker<EmotionEgoXmlInfo_V2>(emotionEgoXmlInfo_V);
				Dictionary<LorId, EmotionEgoXmlInfo> dictionary;
				if (!OrcTools.CustomEmotionEgo.TryGetValue(emotionEgoXmlInfo_V.Sephirah, out dictionary))
				{
					dictionary = (OrcTools.CustomEmotionEgo[emotionEgoXmlInfo_V.Sephirah] = new Dictionary<LorId, EmotionEgoXmlInfo>());
				}
				dictionary[emotionEgoXmlInfo_V.lorId] = emotionEgoXmlInfo_V;
			}
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000E680 File Offset: 0x0000C880
		private static void AddEmotionEgoByMod(SplitTrackerDict<SephirahType, EmotionEgoXmlInfo_V2> dict)
		{
			Dictionary<SephirahType, HashSet<int>> usedIds = new Dictionary<SephirahType, HashSet<int>>();
			Dictionary<SephirahType, int> minCheckedIds = new Dictionary<SephirahType, int>();
			List<EmotionEgoXmlInfo> list = Singleton<EmotionEgoXmlList>.Instance._list;
			OptimizedReplacer.AddOrReplaceWithInject<SephirahType, EmotionEgoXmlInfo_V2, EmotionEgoXmlInfo>(dict, list, (EmotionEgoXmlInfo card) => card.id, (EmotionEgoXmlInfo card) => card.Sephirah, delegate(EmotionEgoXmlInfo_V2 injCard)
			{
				int num;
				if (!minCheckedIds.TryGetValue(injCard.Sephirah, out num))
				{
					num = (minCheckedIds[injCard.Sephirah] = StaticDataLoader_New.MinInjectedId);
				}
				else
				{
					num++;
				}
				HashSet<int> hashSet;
				if (usedIds.TryGetValue(injCard.Sephirah, out hashSet))
				{
					while (hashSet.Contains(num))
					{
						num++;
					}
				}
				minCheckedIds[injCard.Sephirah] = num;
				return num;
			}, delegate(EmotionEgoXmlInfo_V2 addCard)
			{
				HashSet<int> hashSet;
				if (!usedIds.TryGetValue(addCard.Sephirah, out hashSet))
				{
					hashSet = (usedIds[addCard.Sephirah] = new HashSet<int>());
				}
				hashSet.Add(addCard.id);
			}, null);
		}

		// Token: 0x0600017F RID: 383 RVA: 0x0000E714 File Offset: 0x0000C914
		private static void FixEmotionEgoIds()
		{
			foreach (EmotionEgoXmlInfo emotionEgoXmlInfo in Singleton<EmotionEgoXmlList>.Instance._list)
			{
				if (emotionEgoXmlInfo.id == 0)
				{
					LorId cardId = emotionEgoXmlInfo.CardId;
					if (cardId.IsBasic() && cardId.id > 910000 && cardId.id < 920000)
					{
						emotionEgoXmlInfo.id = cardId.id - 910000;
					}
				}
			}
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0000E7A8 File Offset: 0x0000C9A8
		private static void LoadAllToolTip_MOD(List<ModContent> mods)
		{
			TrackerDict<ToolTipXmlInfo> dict = new TrackerDict<ToolTipXmlInfo>();
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "XmlToolTips", out dir))
					{
						StaticDataLoader_New.LoadToolTip_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoToolTipError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddToolTipByMod(dict);
		}

		// Token: 0x06000181 RID: 385 RVA: 0x0000E888 File Offset: 0x0000CA88
		private static void LoadToolTip_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<ToolTipXmlInfo> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadToolTip_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadToolTip_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000E8E4 File Offset: 0x0000CAE4
		private static void LoadToolTip_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<ToolTipXmlInfo> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewToolTip(File.ReadAllText(fileInfo.FullName), dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000183 RID: 387 RVA: 0x0000E9CC File Offset: 0x0000CBCC
		private static void LoadNewToolTip(string str, TrackerDict<ToolTipXmlInfo> dict)
		{
			ToolTipXmlRoot_V2 toolTipXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				toolTipXmlRoot_V = (ToolTipXmlRoot_V2)ToolTipXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (ToolTipXmlInfo_V2 toolTipXmlInfo_V in toolTipXmlRoot_V.toolTipXmlList)
			{
				toolTipXmlInfo_V.InitOldFields();
				dict[new LorId(toolTipXmlInfo_V.ID)] = new AddTracker<ToolTipXmlInfo>(toolTipXmlInfo_V);
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x0000EA68 File Offset: 0x0000CC68
		private static void AddToolTipByMod(TrackerDict<ToolTipXmlInfo> dict)
		{
			OptimizedReplacer.AddOrReplace<ToolTipXmlInfo, ToolTipXmlInfo>(dict, Singleton<ToolTipXmlList>.Instance._list, (ToolTipXmlInfo tt) => tt.ID, null);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000EA9C File Offset: 0x0000CC9C
		[Obsolete]
		public static TitleXmlRoot LoadNewTitle(string str)
		{
			TitleXmlRoot result;
			using (StringReader stringReader = new StringReader(str))
			{
				result = (TitleXmlRoot)new XmlSerializer(typeof(TitleXmlRoot)).Deserialize(stringReader);
			}
			return result;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x0000EAE8 File Offset: 0x0000CCE8
		private static void LoadAllFormation_MOD(List<ModContent> mods)
		{
			TrackerDict<FormationXmlInfo_V2> dict = new TrackerDict<FormationXmlInfo_V2>();
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "FormationInfo", out dir))
					{
						StaticDataLoader_New.LoadFormation_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoFormationError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddFormationByMod(dict);
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0000EBC8 File Offset: 0x0000CDC8
		private static void LoadFormation_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<FormationXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadFormation_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadFormation_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x06000188 RID: 392 RVA: 0x0000EC24 File Offset: 0x0000CE24
		private static void LoadFormation_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<FormationXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewFormation(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000189 RID: 393 RVA: 0x0000ED0C File Offset: 0x0000CF0C
		private static void LoadNewFormation(string str, string uniqueId, TrackerDict<FormationXmlInfo_V2> dict)
		{
			FormationXmlRoot_V2 formationXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				formationXmlRoot_V = (FormationXmlRoot_V2)new XmlSerializer(typeof(FormationXmlRoot_V2)).Deserialize(stringReader);
			}
			foreach (FormationXmlInfo_V2 formationXmlInfo_V in formationXmlRoot_V.list)
			{
				formationXmlInfo_V.WorkshopId = Tools.ClarifyWorkshopIdLegacy(formationXmlInfo_V.WorkshopId, formationXmlRoot_V.customPid, uniqueId);
				dict[formationXmlInfo_V.lorId] = new AddTracker<FormationXmlInfo_V2>(formationXmlInfo_V);
				OrcTools.CustomFormations[formationXmlInfo_V.lorId] = formationXmlInfo_V;
			}
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000EDD0 File Offset: 0x0000CFD0
		private static void AddFormationByMod(TrackerDict<FormationXmlInfo_V2> dict)
		{
			HashSet<int> usedIds = new HashSet<int>();
			int minCheckedId = StaticDataLoader_New.MinInjectedId - 1;
			List<FormationXmlInfo> list = Singleton<FormationXmlList>.Instance._list;
			OptimizedReplacer.AddOrReplaceWithInject<FormationXmlInfo_V2, FormationXmlInfo>(dict, list, (FormationXmlInfo form) => form.id, delegate(FormationXmlInfo_V2 injForm)
			{
				int minCheckedId = minCheckedId;
				minCheckedId++;
				while (usedIds.Contains(minCheckedId))
				{
					minCheckedId = minCheckedId;
					minCheckedId++;
				}
				return minCheckedId;
			}, delegate(FormationXmlInfo_V2 addForm)
			{
				usedIds.Add(addForm.id);
			}, null);
		}

		// Token: 0x0600018B RID: 395 RVA: 0x0000EE48 File Offset: 0x0000D048
		private static void LoadAllQuest_MOD(List<ModContent> mods)
		{
			SplitTrackerDict<SephirahType, QuestXmlInfo_V2> dict = new SplitTrackerDict<SephirahType, QuestXmlInfo_V2>(OptimizedReplacer.sephirahComp);
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "QuestInfo", out dir))
					{
						StaticDataLoader_New.LoadQuest_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoQuestError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddQuestByMod(dict);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0000EF2C File Offset: 0x0000D12C
		private static void LoadQuest_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, QuestXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadQuest_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadQuest_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x0600018D RID: 397 RVA: 0x0000EF88 File Offset: 0x0000D188
		private static void LoadQuest_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, QuestXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewQuest(File.ReadAllText(fileInfo.FullName), dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600018E RID: 398 RVA: 0x0000F070 File Offset: 0x0000D270
		private static void LoadNewQuest(string str, SplitTrackerDict<SephirahType, QuestXmlInfo_V2> dict)
		{
			QuestXmlRoot_V2 questXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				questXmlRoot_V = (QuestXmlRoot_V2)QuestXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (QuestXmlInfo_V2 questXmlInfo_V in questXmlRoot_V.list)
			{
				questXmlInfo_V.InitOldFields();
				TrackerDict<QuestXmlInfo_V2> trackerDict;
				if (!dict.TryGetValue(questXmlInfo_V.sephirah, out trackerDict))
				{
					trackerDict = (dict[questXmlInfo_V.sephirah] = new TrackerDict<QuestXmlInfo_V2>());
				}
				trackerDict[new LorId(questXmlInfo_V.level)] = new AddTracker<QuestXmlInfo_V2>(questXmlInfo_V);
			}
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000F130 File Offset: 0x0000D330
		private static void AddQuestByMod(SplitTrackerDict<SephirahType, QuestXmlInfo_V2> dict)
		{
			OptimizedReplacer.AddOrReplace<SephirahType, QuestXmlInfo_V2, QuestXmlInfo>(dict, Singleton<QuestXmlList>.Instance._list, (QuestXmlInfo quest) => quest.level, (QuestXmlInfo quest) => quest.sephirah, null);
		}

		// Token: 0x06000190 RID: 400 RVA: 0x0000F18C File Offset: 0x0000D38C
		private static void LoadAllEnemyUnit_MOD(List<ModContent> mods)
		{
			TrackerDict<EnemyUnitClassInfo_V2> dict = new TrackerDict<EnemyUnitClassInfo_V2>();
			SplitTrackerDict<string, EnemyUnitClassInfo_V2> splitDict = new SplitTrackerDict<string, EnemyUnitClassInfo_V2>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "EnemyUnitInfo", out dir))
					{
						StaticDataLoader_New.LoadEnemyUnit_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoUnitError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddEnemyUnitByMod(dict, splitDict);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x0000F284 File Offset: 0x0000D484
		private static void LoadEnemyUnit_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<EnemyUnitClassInfo_V2> dict, SplitTrackerDict<string, EnemyUnitClassInfo_V2> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadEnemyUnit_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadEnemyUnit_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x06000192 RID: 402 RVA: 0x0000F2E0 File Offset: 0x0000D4E0
		private static void LoadEnemyUnit_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<EnemyUnitClassInfo_V2> dict, SplitTrackerDict<string, EnemyUnitClassInfo_V2> splitDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewEnemyUnit(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000193 RID: 403 RVA: 0x0000F3C8 File Offset: 0x0000D5C8
		private static void LoadNewEnemyUnit(string str, string uniqueId, TrackerDict<EnemyUnitClassInfo_V2> dict, SplitTrackerDict<string, EnemyUnitClassInfo_V2> splitDict)
		{
			EnemyUnitClassRoot_V2 enemyUnitClassRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				enemyUnitClassRoot_V = (EnemyUnitClassRoot_V2)EnemyUnitClassRoot_V2.Serializer.Deserialize(stringReader);
			}
			TrackerDict<EnemyUnitClassInfo_V2> trackerDict;
			if (!splitDict.TryGetValue(uniqueId, out trackerDict))
			{
				trackerDict = (splitDict[uniqueId] = new TrackerDict<EnemyUnitClassInfo_V2>());
			}
			foreach (EnemyUnitClassInfo_V2 enemyUnitClassInfo_V in enemyUnitClassRoot_V.list)
			{
				string text = Tools.ClarifyWorkshopId(enemyUnitClassInfo_V.workshopID, enemyUnitClassRoot_V.customPid, uniqueId);
				enemyUnitClassInfo_V.workshopID = text;
				enemyUnitClassInfo_V.InitOldFields(text);
				OrcTools.DropItemDicV2[enemyUnitClassInfo_V.id] = enemyUnitClassInfo_V.dropTableListNew;
				trackerDict[enemyUnitClassInfo_V.id] = (dict[enemyUnitClassInfo_V.id] = new AddTracker<EnemyUnitClassInfo_V2>(enemyUnitClassInfo_V));
			}
		}

		// Token: 0x06000194 RID: 404 RVA: 0x0000F4C4 File Offset: 0x0000D6C4
		private static void AddEnemyUnitByMod(TrackerDict<EnemyUnitClassInfo_V2> dict, SplitTrackerDict<string, EnemyUnitClassInfo_V2> splitDict)
		{
			if (Singleton<EnemyUnitClassInfoList>.Instance._list == null)
			{
				Singleton<EnemyUnitClassInfoList>.Instance._list = new List<EnemyUnitClassInfo>();
			}
			OptimizedReplacer.AddOrReplace<EnemyUnitClassInfo_V2, EnemyUnitClassInfo>(dict, Singleton<EnemyUnitClassInfoList>.Instance._list, (EnemyUnitClassInfo eu) => eu.id, null);
			if (Singleton<EnemyUnitClassInfoList>.Instance._workshopEnemyDict == null)
			{
				Singleton<EnemyUnitClassInfoList>.Instance._workshopEnemyDict = new Dictionary<string, List<EnemyUnitClassInfo>>();
			}
			OptimizedReplacer.AddOrReplace<string, EnemyUnitClassInfo_V2, EnemyUnitClassInfo>(splitDict, Singleton<EnemyUnitClassInfoList>.Instance._workshopEnemyDict, (EnemyUnitClassInfo eu) => eu.id, null);
			Dictionary<string, HashSet<int>> dictionary = new Dictionary<string, HashSet<int>>();
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			foreach (LorId lorId in Singleton<BookXmlList>.Instance._dictionary.Keys)
			{
				HashSet<int> hashSet;
				if (!dictionary.TryGetValue(lorId.packageId, out hashSet))
				{
					hashSet = (dictionary[lorId.packageId] = new HashSet<int>());
				}
				hashSet.Add(lorId.id);
			}
			Dictionary<LorId, Dictionary<string, LorId>> dictionary3 = new Dictionary<LorId, Dictionary<string, LorId>>();
			foreach (AddTracker<EnemyUnitClassInfo_V2> addTracker in dict.Values)
			{
				EnemyUnitClassInfo_V2 element = addTracker.element;
				foreach (LorId lorId2 in element.bookLorId)
				{
					if (lorId2.packageId == element.workshopID)
					{
						element.bookId.Add(lorId2.id);
					}
					else
					{
						Dictionary<string, LorId> dictionary4;
						if (!dictionary3.TryGetValue(lorId2, out dictionary4))
						{
							dictionary4 = (dictionary3[lorId2] = new Dictionary<string, LorId>());
						}
						LorId lorId3;
						if (!dictionary4.TryGetValue(element.workshopID, out lorId3))
						{
							int num;
							if (!dictionary2.TryGetValue(element.workshopID, out num))
							{
								num = StaticDataLoader_New.MinInjectedId;
							}
							else
							{
								num++;
							}
							HashSet<int> hashSet2;
							if (dictionary.TryGetValue(element.workshopID, out hashSet2))
							{
								while (hashSet2.Contains(num))
								{
									num++;
								}
							}
							dictionary2[element.workshopID] = num;
							lorId3 = (dictionary4[element.workshopID] = new LorId(element.workshopID, num));
							OrcTools.UnitBookDic[lorId3] = lorId2;
						}
						element.bookId.Add(lorId3.id);
					}
				}
			}
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0000F79C File Offset: 0x0000D99C
		private static void LoadAllStage_MOD(List<ModContent> mods)
		{
			TrackerDict<StageClassInfo_V2> dict = new TrackerDict<StageClassInfo_V2>();
			SplitTrackerDict<string, StageClassInfo_V2> splitDict = new SplitTrackerDict<string, StageClassInfo_V2>(new Comparison<string>(string.Compare));
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "StageInfo", out dir))
					{
						StaticDataLoader_New.LoadStage_MOD(dir, text, dict, splitDict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoGiftError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddStageByMod(dict, splitDict);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000F894 File Offset: 0x0000DA94
		private static void LoadStage_MOD(DirectoryInfo dir, string uniqueId, TrackerDict<StageClassInfo_V2> dict, SplitTrackerDict<string, StageClassInfo_V2> splitDict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadStage_MOD_Checking(dir, uniqueId, dict, splitDict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadStage_MOD(directories[i], uniqueId, dict, splitDict);
				}
			}
		}

		// Token: 0x06000197 RID: 407 RVA: 0x0000F8F0 File Offset: 0x0000DAF0
		private static void LoadStage_MOD_Checking(DirectoryInfo dir, string uniqueId, TrackerDict<StageClassInfo_V2> dict, SplitTrackerDict<string, StageClassInfo_V2> splitDict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewStage(File.ReadAllText(fileInfo.FullName), uniqueId, dict, splitDict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0000F9D8 File Offset: 0x0000DBD8
		private static void AddStageByMod(TrackerDict<StageClassInfo_V2> dict, SplitTrackerDict<string, StageClassInfo_V2> splitDict)
		{
			if (Singleton<StageClassInfoList>.Instance._workshopStageDict == null)
			{
				Singleton<StageClassInfoList>.Instance._workshopStageDict = new Dictionary<string, List<StageClassInfo>>();
			}
			OptimizedReplacer.AddOrReplace<string, StageClassInfo_V2, StageClassInfo>(splitDict, Singleton<StageClassInfoList>.Instance._workshopStageDict, (StageClassInfo stage) => stage.id, null);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, Singleton<StageClassInfoList>.Instance._list, (StageClassInfo stage) => stage.id, null);
			StaticDataLoader_New.ClassifyWorkshopInvitation(dict);
		}

		// Token: 0x06000199 RID: 409 RVA: 0x0000FA68 File Offset: 0x0000DC68
		private static void ClassifyWorkshopInvitation(TrackerDict<StageClassInfo_V2> dict)
		{
			List<StageClassInfo> recipeCondList = Singleton<StageClassInfoList>.Instance._recipeCondList;
			Dictionary<int, List<StageClassInfo>> valueCondList = Singleton<StageClassInfoList>.Instance._valueCondList;
			List<StageClassInfo> workshopRecipeList = Singleton<StageClassInfoList>.Instance._workshopRecipeList;
			Dictionary<int, List<StageClassInfo>> workshopValueDict = Singleton<StageClassInfoList>.Instance._workshopValueDict;
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, recipeCondList, (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsBasic() && stage.invitationInfo.combine == 1);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, valueCondList[1], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsBasic() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 1);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, valueCondList[2], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsBasic() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 2);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, valueCondList[3], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsBasic() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 3);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, workshopRecipeList, (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsWorkshop() && stage.invitationInfo.combine == 1);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, workshopValueDict[1], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsWorkshop() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 1);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, workshopValueDict[2], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsWorkshop() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 2);
			OptimizedReplacer.AddOrReplace<StageClassInfo_V2, StageClassInfo>(dict, workshopValueDict[3], (StageClassInfo stage) => stage.id, (StageClassInfo_V2 stage) => stage.id.IsWorkshop() && stage.invitationInfo.combine == 2 && stage.invitationInfo.bookNum == 3);
			valueCondList[1].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
			valueCondList[2].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
			valueCondList[3].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
			workshopValueDict[1].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
			workshopValueDict[2].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
			workshopValueDict[3].Sort(new Comparison<StageClassInfo>(StaticDataLoader_New.<ClassifyWorkshopInvitation>g__comparison|102_16));
		}

		// Token: 0x0600019A RID: 410 RVA: 0x0000FD80 File Offset: 0x0000DF80
		private static void LoadNewStage(string str, string uniqueId, TrackerDict<StageClassInfo_V2> dict, SplitTrackerDict<string, StageClassInfo_V2> splitDict)
		{
			StageXmlRoot_V2 stageXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				stageXmlRoot_V = (StageXmlRoot_V2)StageXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (StageClassInfo_V2 stageClassInfo_V in stageXmlRoot_V.list)
			{
				string text = Tools.ClarifyWorkshopId(stageClassInfo_V.workshopID, stageXmlRoot_V.customPid, uniqueId);
				stageClassInfo_V.workshopID = text;
				stageClassInfo_V.InitializeIds(text);
				stageClassInfo_V.InitOldFields(text);
				if (text != "")
				{
					TrackerDict<StageClassInfo_V2> trackerDict;
					if (!splitDict.TryGetValue(uniqueId, out trackerDict))
					{
						trackerDict = (splitDict[uniqueId] = new TrackerDict<StageClassInfo_V2>());
					}
					trackerDict[stageClassInfo_V.id] = (dict[stageClassInfo_V.id] = new AddTracker<StageClassInfo_V2>(stageClassInfo_V));
				}
				else
				{
					dict[stageClassInfo_V.id] = new AddTracker<StageClassInfo_V2>(stageClassInfo_V);
				}
			}
		}

		// Token: 0x0600019B RID: 411 RVA: 0x0000FE90 File Offset: 0x0000E090
		private static void LoadAllFloorInfo_MOD(List<ModContent> mods)
		{
			SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2> dict = new SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2>(OptimizedReplacer.sephirahComp);
			foreach (ModContent modContent in mods)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				string text = modContent._itemUniqueId;
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				try
				{
					DirectoryInfo dir;
					if (StaticDataLoader_New.ExistsModdingPath(dirInfo, "FloorLevelInfo", out dir))
					{
						StaticDataLoader_New.LoadFloorInfo_MOD(dir, text, dict);
					}
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(Application.dataPath + "/Mods/" + dirInfo.Name + "_StaticInfoFloorInfoError.log", ex.ToString());
				}
			}
			StaticDataLoader_New.AddFloorInfoByMod(dict);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0000FF74 File Offset: 0x0000E174
		private static void LoadFloorInfo_MOD(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2> dict)
		{
			if (Harmony_Patch.IsBasemodDebugMode)
			{
				Debug.Log("Basemod: loading folder at " + dir.FullName);
			}
			StaticDataLoader_New.LoadFloorInfo_MOD_Checking(dir, uniqueId, dict);
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					StaticDataLoader_New.LoadFloorInfo_MOD(directories[i], uniqueId, dict);
				}
			}
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000FFD0 File Offset: 0x0000E1D0
		private static void LoadFloorInfo_MOD_Checking(DirectoryInfo dir, string uniqueId, SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2> dict)
		{
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				try
				{
					StaticDataLoader_New.LoadNewFloorInfo(File.ReadAllText(fileInfo.FullName), uniqueId, dict);
				}
				catch (Exception ex)
				{
					UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Error from  " + uniqueId + " " + fileInfo.Name, fileInfo.FullName, fileInfo.DirectoryName, "Error Xml Files", null);
					Singleton<ModContentManager>.Instance.AddErrorLog(ex.ToString());
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/Error in ",
						uniqueId,
						" ",
						fileInfo.Name,
						".log"
					}), ex.ToString());
				}
			}
		}

		// Token: 0x0600019E RID: 414 RVA: 0x000100B8 File Offset: 0x0000E2B8
		private static void LoadNewFloorInfo(string str, string uniqueId, SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2> dict)
		{
			FloorLevelXmlRoot_V2 floorLevelXmlRoot_V;
			using (StringReader stringReader = new StringReader(str))
			{
				floorLevelXmlRoot_V = (FloorLevelXmlRoot_V2)FloorLevelXmlRoot_V2.Serializer.Deserialize(stringReader);
			}
			foreach (FloorLevelXmlInfo_V2 floorLevelXmlInfo_V in floorLevelXmlRoot_V.list)
			{
				floorLevelXmlInfo_V.InitOldFields(uniqueId);
				TrackerDict<FloorLevelXmlInfo_V2> trackerDict;
				if (!dict.TryGetValue(floorLevelXmlInfo_V.sephirahType, out trackerDict))
				{
					trackerDict = (dict[floorLevelXmlInfo_V.sephirahType] = new TrackerDict<FloorLevelXmlInfo_V2>());
				}
				trackerDict[new LorId(floorLevelXmlInfo_V.level)] = new AddTracker<FloorLevelXmlInfo_V2>(floorLevelXmlInfo_V);
				OrcTools.FloorLevelStageDic[floorLevelXmlInfo_V] = floorLevelXmlInfo_V.stageLorId;
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00010188 File Offset: 0x0000E388
		private static void AddFloorInfoByMod(SplitTrackerDict<SephirahType, FloorLevelXmlInfo_V2> dict)
		{
			OptimizedReplacer.AddOrReplace<SephirahType, FloorLevelXmlInfo_V2, FloorLevelXmlInfo>(dict, Singleton<FloorLevelXmlList>.Instance._list, (FloorLevelXmlInfo fl) => fl.level, (FloorLevelXmlInfo fl) => fl.sephirahType, null);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000101E4 File Offset: 0x0000E3E4
		internal static void PrepareBridges()
		{
			Singleton<BridgeManager>.Instance.bridgeLoadHandler.RegisterBridge("BaseMod", new Action(StaticDataLoader_New.CompleteInjection), new Action(Tools.CallOnInjectIds));
			Singleton<BridgeManager>.Instance.giftBridge.AddBridge(delegate(GiftXmlInfo gift)
			{
				GiftXmlInfo_V2 giftXmlInfo_V = gift as GiftXmlInfo_V2;
				if (giftXmlInfo_V == null || giftXmlInfo_V.dontRemove)
				{
					return null;
				}
				return giftXmlInfo_V.lorId;
			}, (LorId id) => OrcTools.CustomGifts.GetValueSafe(id));
			Singleton<BridgeManager>.Instance.formationBridge.AddBridge(delegate(FormationXmlInfo formation)
			{
				FormationXmlInfo_V2 formationXmlInfo_V = formation as FormationXmlInfo_V2;
				if (formationXmlInfo_V == null)
				{
					return null;
				}
				return formationXmlInfo_V.lorId;
			}, (LorId id) => OrcTools.CustomFormations.GetValueSafe(id));
			Singleton<BridgeManager>.Instance.passiveToInnerTypeName.AddGenerator(delegate(PassiveXmlInfo passive)
			{
				PassiveXmlInfo_V2 passiveXmlInfo_V = passive as PassiveXmlInfo_V2;
				if (passiveXmlInfo_V == null || string.IsNullOrWhiteSpace(passiveXmlInfo_V.CustomInnerType))
				{
					return null;
				}
				return passiveXmlInfo_V.CustomInnerType.Trim();
			});
			Singleton<BridgeManager>.Instance.passiveToInnerTypeSource.AddGenerator(delegate(PassiveXmlInfo passive)
			{
				PassiveXmlInfo_V2 passiveXmlInfo_V = passive as PassiveXmlInfo_V2;
				if (passiveXmlInfo_V == null || !(passiveXmlInfo_V.CopyInnerType != LorId.None))
				{
					return null;
				}
				return passiveXmlInfo_V.CopyInnerType;
			});
			Singleton<BridgeManager>.Instance.emotionCardBridge.AddBridge(delegate(EmotionCardXmlInfo card)
			{
				EmotionCardXmlInfo_V2 emotionCardXmlInfo_V = card as EmotionCardXmlInfo_V2;
				if (emotionCardXmlInfo_V == null)
				{
					return null;
				}
				return emotionCardXmlInfo_V.lorId;
			}, delegate(LorId id, SephirahType sephirah)
			{
				Dictionary<LorId, EmotionCardXmlInfo> valueSafe = OrcTools.CustomEmotionCards.GetValueSafe(sephirah);
				if (valueSafe == null)
				{
					return null;
				}
				return valueSafe.GetValueSafe(id);
			});
			Singleton<BridgeManager>.Instance.emotionEgoBridge.AddBridge(delegate(EmotionEgoXmlInfo card)
			{
				EmotionEgoXmlInfo_V2 emotionEgoXmlInfo_V = card as EmotionEgoXmlInfo_V2;
				if (emotionEgoXmlInfo_V == null)
				{
					return null;
				}
				return emotionEgoXmlInfo_V.lorId;
			}, delegate(LorId id, SephirahType sephirah)
			{
				Dictionary<LorId, EmotionEgoXmlInfo> valueSafe = OrcTools.CustomEmotionEgo.GetValueSafe(sephirah);
				if (valueSafe == null)
				{
					return null;
				}
				return valueSafe.GetValueSafe(id);
			});
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x000103AD File Offset: 0x0000E5AD
		private static void CompleteInjection()
		{
			StaticDataLoader_New.CompleteEnemyInjection();
			StaticDataLoader_New.CompleteStageInjection();
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x000103BC File Offset: 0x0000E5BC
		private static void CompleteEnemyInjection()
		{
			foreach (EnemyUnitClassInfo enemyUnitClassInfo in Singleton<EnemyUnitClassInfoList>.Instance._list)
			{
				EnemyUnitClassInfo_V2 enemyUnitClassInfo_V = enemyUnitClassInfo as EnemyUnitClassInfo_V2;
				if (enemyUnitClassInfo_V != null)
				{
					enemyUnitClassInfo_V.InitInjectedFields();
				}
			}
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x0001041C File Offset: 0x0000E61C
		private static void CompleteStageInjection()
		{
			foreach (StageClassInfo stageClassInfo in Singleton<StageClassInfoList>.Instance._list)
			{
				StageClassInfo_V2 stageClassInfo_V = stageClassInfo as StageClassInfo_V2;
				if (stageClassInfo_V != null)
				{
					stageClassInfo_V.InitInjectedFields();
				}
			}
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00010488 File Offset: 0x0000E688
		[CompilerGenerated]
		internal static int <ClassifyWorkshopInvitation>g__comparison|102_16(StageClassInfo info1, StageClassInfo info2)
		{
			return (int)(10f * (info2.invitationInfo.bookValue - info1.invitationInfo.bookValue));
		}

		// Token: 0x04000124 RID: 292
		private static readonly int MinInjectedId = 1000000000;
	}
}
