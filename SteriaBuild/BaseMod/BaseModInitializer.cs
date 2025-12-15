using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameSave;
using Mod;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x02000055 RID: 85
	public class BaseModInitializer : ModInitializer
	{
		// Token: 0x060000B7 RID: 183 RVA: 0x0000674B File Offset: 0x0000494B
		public BaseModInitializer()
		{
			BaseModInitializer.SaveSelection();
			BaseModInitializer.ClearReference();
			StaticDataLoader_New.PrepareBridges();
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00006762 File Offset: 0x00004962
		public override void OnInitializeMod()
		{
			BaseModInitialize.OnInitializeMod();
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000676C File Offset: 0x0000496C
		private static void SaveSelection()
		{
			try
			{
				List<string> names = new List<string>();
				List<ModContentInfo> allMods = Singleton<ModContentManager>.Instance._allMods;
				List<ModContentInfo> uexList = new List<ModContentInfo>();
				List<ModContentInfo> bmList = new List<ModContentInfo>();
				DirectoryInfo thisModDirectory = new FileInfo(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)).Directory.Parent;
				allMods.RemoveAll(delegate(ModContentInfo modContentInfo)
				{
					names.Add(modContentInfo.invInfo.workshopInfo.uniqueId);
					string uniqueId = modContentInfo.invInfo.workshopInfo.uniqueId;
					if (uniqueId == "BaseMod")
					{
						if (modContentInfo.dirInfo == thisModDirectory)
						{
							bmList.Insert(0, modContentInfo);
						}
						else
						{
							bmList.Add(modContentInfo);
						}
						return true;
					}
					if (!(uniqueId == "UnityExplorer"))
					{
						return false;
					}
					if (modContentInfo.activated)
					{
						uexList.Insert(0, modContentInfo);
					}
					else
					{
						uexList.Add(modContentInfo);
					}
					return true;
				});
				uexList.AddRange(bmList);
				allMods.InsertRange(0, uexList);
				int uexCount = 1;
				int bmCount = 1;
				names.RemoveAll(delegate(string name)
				{
					if (name == "BaseMod")
					{
						int num = bmCount + 1;
						bmCount = num;
						if (num > 0)
						{
							return true;
						}
					}
					if (name == "UnityExplorer")
					{
						int num = uexCount + 1;
						uexCount = num;
						return num > 0;
					}
					return false;
				});
				List<string> list = new List<string>(bmCount + uexCount + names.Count);
				list.AddRange(Enumerable.Repeat<string>("UnityExplorer", uexCount));
				list.AddRange(Enumerable.Repeat<string>("BaseMod", bmCount));
				list.AddRange(names);
				names = list;
				SaveData saveData = new SaveData();
				SaveData saveData2 = new SaveData(1);
				foreach (string text in names)
				{
					saveData2.AddToList(new SaveData(text));
				}
				saveData.AddData("orders", saveData2);
				SaveData saveData3 = Singleton<SaveManager>.Instance.LoadData(Singleton<ModContentManager>.Instance.savePath);
				if (saveData3 != null && saveData3.GetData("lastActivated") != null)
				{
					saveData.AddData("lastActivated", saveData3.GetData("lastActivated"));
				}
				Singleton<SaveManager>.Instance.SaveData(Singleton<ModContentManager>.Instance.savePath, saveData);
			}
			catch
			{
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00006974 File Offset: 0x00004B74
		private static void ClearReference()
		{
			foreach (string str in BaseModInitializer.References)
			{
				string path = Application.dataPath + "/Managed/" + str + ".dll";
				if (File.Exists(path))
				{
					try
					{
						File.Delete(path);
					}
					catch
					{
					}
				}
			}
		}

		// Token: 0x0400010F RID: 271
		internal static readonly List<string> References = new List<string>
		{
			"0Harmony",
			"Mono.Cecil",
			"Mono.Cecil.Mdb",
			"Mono.Cecil.Pdb",
			"Mono.Cecil.Rocks",
			"MonoMod.Common",
			"MonoMod.RuntimeDetour",
			"MonoMod.Utils"
		};
	}
}
