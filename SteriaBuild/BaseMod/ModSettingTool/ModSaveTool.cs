using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BaseMod;
using GameSave;
using UnityEngine;

namespace ModSettingTool
{
	// Token: 0x02000003 RID: 3
	public static class ModSaveTool
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x00002058 File Offset: 0x00000258
		public static string ModSavePath
		{
			get
			{
				return Path.Combine(SaveManager.savePath, "ModSaveFiles.dat");
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000206C File Offset: 0x0000026C
		public static void LoadFromSaveData()
		{
			try
			{
				if (!File.Exists(ModSaveTool.ModSavePath))
				{
					using (FileStream fileStream = File.Create(ModSaveTool.ModSavePath))
					{
						new BinaryFormatter().Serialize(fileStream, ModSaveTool.ModSaveData.GetSerializedData());
					}
				}
				else
				{
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					object obj;
					using (FileStream serializationStream = File.Open(ModSaveTool.ModSavePath, FileMode.Open))
					{
						obj = binaryFormatter.Deserialize(serializationStream);
					}
					if (obj == null)
					{
						throw new Exception();
					}
					ModSaveTool.ModSaveData.LoadFromSerializedData(obj);
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadModSaveerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002150 File Offset: 0x00000350
		public static void SaveModSaveData()
		{
			try
			{
				using (FileStream fileStream = File.Create(ModSaveTool.ModSavePath))
				{
					new BinaryFormatter().Serialize(fileStream, ModSaveTool.ModSaveData.GetSerializedData());
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/ModSaveerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000021D4 File Offset: 0x000003D4
		public static void RemoveUnknownSaves()
		{
			List<string> removeSaves = new List<string>();
			foreach (KeyValuePair<string, SaveData> keyValuePair in ModSaveTool.ModSaveData.GetDictionarySelf())
			{
				if (!ModSaveTool.LoadedModsWorkshopId.Contains(keyValuePair.Key))
				{
					removeSaves.Add(keyValuePair.Key);
				}
			}
			if (removeSaves.Count > 0)
			{
				Tools.SetAlarmText("BaseMod_ReMoveUnloadSave", 1, delegate(bool flag)
				{
					if (flag)
					{
						foreach (string key in removeSaves)
						{
							ModSaveTool.ModSaveData.GetDictionarySelf().Remove(key);
						}
					}
				}, null);
			}
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002280 File Offset: 0x00000480
		public static SaveData GetModSaveData(string WorkshopId = "")
		{
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				WorkshopId = Tools.GetModId(Assembly.GetCallingAssembly());
			}
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				return null;
			}
			if (ModSaveTool.ModSaveData.GetData(WorkshopId) == null)
			{
				ModSaveTool.ModSaveData.AddData(WorkshopId, new SaveData(2));
			}
			return ModSaveTool.ModSaveData.GetData(WorkshopId);
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000022D4 File Offset: 0x000004D4
		public static void SaveString(string name, string value, string WorkshopId = "")
		{
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				WorkshopId = Tools.GetModId(Assembly.GetCallingAssembly());
			}
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				return;
			}
			ModSaveTool.GetModSaveData(WorkshopId).GetDictionarySelf()[name] = new SaveData(value);
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000230A File Offset: 0x0000050A
		public static void Saveint(string name, int value, string WorkshopId = "")
		{
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				WorkshopId = Tools.GetModId(Assembly.GetCallingAssembly());
			}
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				return;
			}
			ModSaveTool.GetModSaveData(WorkshopId).GetDictionarySelf()[name] = new SaveData(value);
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002340 File Offset: 0x00000540
		public static void Saveulong(string name, ulong value, string WorkshopId = "")
		{
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				WorkshopId = Tools.GetModId(Assembly.GetCallingAssembly());
			}
			if (string.IsNullOrWhiteSpace(WorkshopId))
			{
				return;
			}
			ModSaveTool.GetModSaveData(WorkshopId).GetDictionarySelf()[name] = new SaveData(value);
		}

		// Token: 0x04000001 RID: 1
		public static HashSet<string> LoadedModsWorkshopId = new HashSet<string>();

		// Token: 0x04000002 RID: 2
		public static SaveData ModSaveData = new SaveData(2);
	}
}
