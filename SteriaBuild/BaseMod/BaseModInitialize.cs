using System;
using System.Reflection;
using HarmonyLib;
using Mod;
using ModSettingTool;
using SummonLiberation;

namespace BaseMod
{
	// Token: 0x02000054 RID: 84
	public static class BaseModInitialize
	{
		// Token: 0x060000B5 RID: 181 RVA: 0x00006654 File Offset: 0x00004854
		public static void OnInitializeMod()
		{
			Harmony harmony = new Harmony("LOR.BaseMod");
			Harmony_Patch.Init();
			MethodInfo method = typeof(BaseModInitialize).GetMethod("NoReferenceError", AccessTools.all);
			harmony.Patch(typeof(EntryScene).GetMethod("CheckModError", AccessTools.all), new HarmonyMethod(method), null, null, null, null);
			harmony.PatchAll(typeof(Harmony_Patch));
			GlobalGameManager.Instance.ver = string.Join(Environment.NewLine, new string[]
			{
				GlobalGameManager.Instance.ver,
				"BaseMod for workshop 2.3.2 ver"
			});
			Harmony_Patch.LoadModFiles();
			Harmony_Patch.LoadAssemblyFiles();
			ModSaveTool.LoadFromSaveData();
			new Harmony("LOR.SummonLiberation").PatchAll(typeof(Harmony_Patch));
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x0000671A File Offset: 0x0000491A
		private static void NoReferenceError()
		{
			Singleton<ModContentManager>.Instance._logs.RemoveAll((string x) => x.Contains("The same assembly name already exists"));
		}
	}
}
