using System;
using System.IO;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x02000061 RID: 97
	public class StorySerializer_new
	{
		// Token: 0x060001A6 RID: 422 RVA: 0x000104A8 File Offset: 0x0000E6A8
		public static void StoryTextExport(string path, string outpath, string outname)
		{
			string text = Resources.Load<TextAsset>(path).text;
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", text);
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x000104DF File Offset: 0x0000E6DF
		public static void StoryTextExport_str(string str, string outpath, string outname)
		{
			Directory.CreateDirectory(outpath);
			File.WriteAllText(outpath + "/" + outname + ".txt", str);
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x000104FF File Offset: 0x0000E6FF
		private static bool CheckStaticReExportLock()
		{
			return File.Exists(Harmony_Patch.StoryPath_Static + "/DeleteThisToExportStaticStoryAgain");
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x00010515 File Offset: 0x0000E715
		private static void CreateStaticReExportLock()
		{
			File.WriteAllText(Harmony_Patch.StoryPath_Static + "/DeleteThisToExportStaticStoryAgain", "yes");
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00010530 File Offset: 0x0000E730
		private static bool CheckLocalizeReExportLock()
		{
			return File.Exists(Harmony_Patch.StoryPath_Localize + "/DeleteThisToExportLocalizeStoryAgain");
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00010546 File Offset: 0x0000E746
		private static void CreateLocalizeReExportLock()
		{
			File.WriteAllText(Harmony_Patch.StoryPath_Localize + "/DeleteThisToExportLocalizeStoryAgain", "yes");
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00010564 File Offset: 0x0000E764
		public static void ExportStory()
		{
			try
			{
				if (!StorySerializer_new.CheckStaticReExportLock())
				{
					TextAsset[] array = Resources.LoadAll<TextAsset>("Xml/Story/StoryEffect/");
					for (int i = 0; i < array.Length; i++)
					{
						StorySerializer_new.StoryTextExport_str(array[i].text, Harmony_Patch.StoryPath_Static, array[i].name);
					}
					StorySerializer_new.CreateStaticReExportLock();
				}
				if (!StorySerializer_new.CheckLocalizeReExportLock())
				{
					TextAsset[] array2 = Resources.LoadAll<TextAsset>("Xml/Story/" + TextDataModel.CurrentLanguage);
					for (int j = 0; j < array2.Length; j++)
					{
						StorySerializer_new.StoryTextExport_str(array2[j].text, Harmony_Patch.StoryPath_Localize, array2[j].name);
					}
					StorySerializer_new.CreateLocalizeReExportLock();
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SSLSerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
	}
}
