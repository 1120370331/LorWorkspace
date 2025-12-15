using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using Battle.DiceAttackEffect;
using EnumExtenderV2;
using ExtendedLoader;
using GameSave;
using GTMDProjectMoon;
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using Mod;
using ModSettingTool;
using StoryScene;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Workshop;

namespace BaseMod
{
	// Token: 0x02000063 RID: 99
	public static class Harmony_Patch
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00013E1D File Offset: 0x0001201D
		public static string StaticPath
		{
			get
			{
				Harmony_Patch.Staticpath = Directory.CreateDirectory(Application.dataPath + "/Managed/BaseMod/StaticInfo").FullName;
				return Harmony_Patch.Staticpath;
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00013E42 File Offset: 0x00012042
		public static string LocalizePath
		{
			get
			{
				Harmony_Patch.Localizepath = Directory.CreateDirectory(Application.dataPath + "/Managed/BaseMod/Localize/" + TextDataModel.CurrentLanguage).FullName;
				return Harmony_Patch.Localizepath;
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060001BF RID: 447 RVA: 0x00013E6C File Offset: 0x0001206C
		public static string StoryPath_Static
		{
			get
			{
				Harmony_Patch.StoryStaticpath = Directory.CreateDirectory(Application.dataPath + "/Managed/BaseMod/Story/EffectInfo").FullName;
				return Harmony_Patch.StoryStaticpath;
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00013E91 File Offset: 0x00012091
		public static string StoryPath_Localize
		{
			get
			{
				Harmony_Patch.Storylocalizepath = Directory.CreateDirectory(Application.dataPath + "/Managed/BaseMod/Story/Localize/" + TextDataModel.CurrentLanguage).FullName;
				return Harmony_Patch.Storylocalizepath;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060001C1 RID: 449 RVA: 0x00013EBB File Offset: 0x000120BB
		public static List<ModContent> LoadedModContents
		{
			get
			{
				return Singleton<ModContentManager>.Instance._loadedContents;
			}
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x00013EC8 File Offset: 0x000120C8
		private static bool VoidPre()
		{
			try
			{
				return false;
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/error.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00013F3C File Offset: 0x0001213C
		public static void Init()
		{
			try
			{
				Harmony_Patch.path = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
				Harmony_Patch.AssemList = new List<Assembly>();
				Harmony_Patch.LoadedAssembly = new List<string>();
				Harmony_Patch.ArtWorks = null;
				Harmony_Patch.BookThumb = null;
				Harmony_Patch.AudioClips = null;
				Harmony_Patch.CustomEffects = new Dictionary<string, Type>();
				Harmony_Patch.CustomMapManager = new Dictionary<string, Type>();
				Harmony_Patch.CustomBattleDialogModel = new Dictionary<string, Type>();
				Harmony_Patch.CustomGiftPassive = new Dictionary<string, Type>();
				Harmony_Patch.CustomEmotionCardAbility = new Dictionary<string, Type>();
				Harmony_Patch.ModStoryCG = new Dictionary<LorId, Harmony_Patch.ModStroyCG>();
				Harmony_Patch.ModWorkShopId = new Dictionary<Assembly, string>();
				Harmony_Patch.IsModStorySelected = false;
				try
				{
					Harmony_Patch.CreateShortcuts();
					Harmony_Patch.ExportDocuments();
				}
				catch
				{
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/error.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00014058 File Offset: 0x00012258
		private static void CreateShortcuts()
		{
			string modPath = Singleton<ModContentManager>.Instance.GetModPath("BaseMod");
			UtilTools.CreateShortcut(Application.dataPath + "/Managed/BaseMod/", "BaseMod for Workshop", modPath, modPath, "Way to BaseMod Files", null);
			UtilTools.CreateShortcut(Application.dataPath + "/Mods/", "Player.log", SaveManager.savePath + "/Player.log", SaveManager.savePath, "Way to Player.log", null);
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x000140CC File Offset: 0x000122CC
		private static void ExportDocuments()
		{
			string modPath = Singleton<ModContentManager>.Instance.GetModPath("BaseMod");
			try
			{
				if (File.Exists(modPath + "/SteamworkUploader.rar"))
				{
					File.Copy(modPath + "/SteamworkUploader.rar", Application.dataPath + "/Managed/BaseMod/SteamworkUploader.rar", true);
				}
			}
			catch
			{
			}
			if (Directory.Exists(modPath + "/Documents/"))
			{
				UtilTools.CopyDir(modPath + "/Documents", Application.dataPath + "/Managed/BaseMod/Documents");
			}
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x00014164 File Offset: 0x00012364
		public static void LoadAssemblyFiles()
		{
			ModSaveTool.LoadedModsWorkshopId.Add("BaseMod");
			foreach (ModContent modContent in Harmony_Patch.LoadedModContents)
			{
				Singleton<ModContentManager>.Instance._currentPid = modContent._itemUniqueId;
				DirectoryInfo dirInfo = modContent._dirInfo;
				if (!BasemodConfig.FindBasemodConfig(modContent._itemUniqueId).IgnoreStaticFiles)
				{
					foreach (FileInfo fileInfo in dirInfo.GetFiles())
					{
						string text = "unknown";
						try
						{
							if (fileInfo.Name.Contains(".dll") && !Harmony_Patch.LoadedAssembly.Contains(fileInfo.FullName))
							{
								Harmony_Patch.LoadedAssembly.Add(fileInfo.Directory.FullName);
								text = "LoadAssembly";
								if (Harmony_Patch.IsBasemodDebugMode)
								{
									Debug.Log("Basemod load : " + fileInfo.FullName);
								}
								Assembly assembly = Assembly.LoadFile(fileInfo.FullName);
								text = "GetAssemblyTypes";
								IEnumerable<Type> enumerable;
								try
								{
									enumerable = assembly.GetTypes();
								}
								catch (ReflectionTypeLoadException ex)
								{
									Singleton<ModContentManager>.Instance.AddErrorLog(string.Concat(new string[]
									{
										"Load_",
										fileInfo.Name,
										"_",
										text,
										"_Error"
									}));
									File.WriteAllText(string.Concat(new string[]
									{
										Application.dataPath,
										"/Mods/Load_",
										fileInfo.Name,
										"_",
										text,
										"_Error.log"
									}), string.Join("\n", from e in ex.LoaderExceptions
									select e.ToString()));
									enumerable = from t in ex.Types
									where t != null
									select t;
								}
								foreach (Type type in enumerable)
								{
									text = "LoadCustomTypes";
									string name = type.Name;
									if (type.IsSubclassOf(typeof(DiceAttackEffect)) && name.StartsWith("DiceAttackEffect_"))
									{
										Harmony_Patch.CustomEffects[name.Substring("DiceAttackEffect_".Length).Trim()] = type;
									}
									if (type.IsSubclassOf(typeof(CustomMapManager)) && name.EndsWith("MapManager"))
									{
										Harmony_Patch.CustomMapManager[name.Trim()] = type;
									}
									if (type.IsSubclassOf(typeof(BattleDialogueModel)) && name.StartsWith("BattleDialogueModel_"))
									{
										Harmony_Patch.CustomBattleDialogModel[name.Substring("BattleDialogueModel_".Length).Trim()] = type;
									}
									if (type.IsSubclassOf(typeof(PassiveAbilityBase)) && name.StartsWith("GiftPassiveAbility_"))
									{
										Harmony_Patch.CustomGiftPassive[name.Substring("GiftPassiveAbility_".Length).Trim()] = type;
									}
									if (type.IsSubclassOf(typeof(EmotionCardAbilityBase)) && name.StartsWith("EmotionCardAbility_"))
									{
										Harmony_Patch.CustomEmotionCardAbility[name.Substring("EmotionCardAbility_".Length).Trim()] = type;
									}
									if (type.IsSubclassOf(typeof(QuestMissionScriptBase)) && name.StartsWith("QuestMissionScript_"))
									{
										Harmony_Patch.CustomQuest[name.Substring("QuestMissionScript_".Length).Trim()] = type;
									}
									text = "LoadHarmonyPatch";
									if (name == "Harmony_Patch" || (type != null && type.BaseType != null && type.BaseType.Name == "Harmony_Patch"))
									{
										Activator.CreateInstance(type);
									}
								}
								text = "LoadOtherTypes";
								Harmony_Patch.LoadTypesFromAssembly(enumerable, fileInfo.Name);
								ModSaveTool.LoadedModsWorkshopId.Add(Tools.GetModId(assembly));
								Harmony_Patch.AssemList.Add(assembly);
							}
						}
						catch (Exception ex2)
						{
							Singleton<ModContentManager>.Instance.AddErrorLog(string.Concat(new string[]
							{
								"Load_",
								fileInfo.Name,
								"_",
								text,
								"_Error"
							}));
							File.WriteAllText(string.Concat(new string[]
							{
								Application.dataPath,
								"/Mods/Load_",
								fileInfo.Name,
								"_",
								text,
								"_Error.log"
							}), ex2.ToString());
						}
					}
					Singleton<ModContentManager>.Instance._currentPid = "";
				}
			}
			Harmony_Patch.CallAllInitializer();
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x000146C0 File Offset: 0x000128C0
		private static void CallAllInitializer()
		{
			foreach (ValueTuple<string, string, ModInitializer> valueTuple in Harmony_Patch.allInitializers)
			{
				string item = valueTuple.Item1;
				string item2 = valueTuple.Item2;
				ModInitializer item3 = valueTuple.Item3;
				Singleton<ModContentManager>.Instance._currentPid = item;
				try
				{
					item3.OnInitializeMod();
				}
				catch (Exception ex)
				{
					Singleton<ModContentManager>.Instance.AddErrorLog(string.Concat(new string[]
					{
						"OnInitialize_",
						item2,
						"_",
						item3.GetType().Name,
						"_Error"
					}));
					File.WriteAllText(string.Concat(new string[]
					{
						Application.dataPath,
						"/Mods/OnInitialize_",
						item2,
						"_",
						item3.GetType().Name,
						"_Error.log"
					}), ex.ToString());
				}
				Singleton<ModContentManager>.Instance._currentPid = "";
			}
			Harmony_Patch.allInitializers.Clear();
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x000147EC File Offset: 0x000129EC
		private static void LoadTypesFromAssembly(IEnumerable<Type> types, string filename)
		{
			AssemblyManager instance = Singleton<AssemblyManager>.Instance;
			foreach (Type type in types)
			{
				string name = type.Name;
				if (type.IsSubclassOf(typeof(DiceCardSelfAbilityBase)) && name.StartsWith("DiceCardSelfAbility_"))
				{
					instance._diceCardSelfAbilityDict.Add(name.Substring("DiceCardSelfAbility_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(DiceCardAbilityBase)) && name.StartsWith("DiceCardAbility_"))
				{
					instance._diceCardAbilityDict.Add(name.Substring("DiceCardAbility_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(BehaviourActionBase)) && name.StartsWith("BehaviourAction_"))
				{
					instance._behaviourActionDict.Add(name.Substring("BehaviourAction_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(PassiveAbilityBase)) && name.StartsWith("PassiveAbility_"))
				{
					instance._passiveAbilityDict.Add(name.Substring("PassiveAbility_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(DiceCardPriorityBase)) && name.StartsWith("DiceCardPriority_"))
				{
					instance._diceCardPriorityDict.Add(name.Substring("DiceCardPriority_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(EnemyUnitAggroSetter)) && name.StartsWith("EnemyUnitAggroSetter_"))
				{
					instance._enemyUnitAggroSetterDict.Add(name.Substring("EnemyUnitAggroSetter_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(EnemyTeamStageManager)) && name.StartsWith("EnemyTeamStageManager_"))
				{
					instance._enemyTeamStageManagerDict.Add(name.Substring("EnemyTeamStageManager_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(EnemyUnitTargetSetter)) && name.StartsWith("EnemyUnitTargetSetter_"))
				{
					instance._enemyUnitTargetSetterDict.Add(name.Substring("EnemyUnitTargetSetter_".Length), type);
				}
				else if (type.IsSubclassOf(typeof(ModInitializer)))
				{
					Harmony_Patch.allInitializers.Add(new ValueTuple<string, string, ModInitializer>(Singleton<ModContentManager>.Instance._currentPid, filename, Activator.CreateInstance(type) as ModInitializer));
				}
			}
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00014A88 File Offset: 0x00012C88
		public static void LoadModFiles()
		{
			Dictionary<string, List<WorkshopSkinData>> bookSkinData = Singleton<CustomizingBookSkinLoader>.Instance._bookSkinData;
			StaticDataLoader_New.ExportOriginalFiles();
			StaticDataLoader_New.LoadModFiles(Harmony_Patch.LoadedModContents);
			LocalizedTextLoader_New.ExportOriginalFiles();
			LocalizedTextLoader_New.LoadModFiles(Harmony_Patch.LoadedModContents);
			StorySerializer_new.ExportStory();
			Harmony_Patch.LoadBookSkins(bookSkinData);
			Harmony_Patch.GetArtWorks();
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00014AC4 File Offset: 0x00012CC4
		private static void ReloadModFiles()
		{
			GlobalGameManager.Instance.LoadStaticData();
			Singleton<StageClassInfoList>.Instance.GetAllWorkshopData().Clear();
			Singleton<EnemyUnitClassInfoList>.Instance.GetAllWorkshopData().Clear();
			Singleton<BookXmlList>.Instance.GetAllWorkshopData().Clear();
			Singleton<CardDropTableXmlList>.Instance.GetAllWorkshopData().Clear();
			Singleton<DropBookXmlList>.Instance.GetAllWorkshopData().Clear();
			ItemXmlDataList.instance.GetAllWorkshopData().Clear();
			Singleton<BookDescXmlList>.Instance._dictionaryWorkshop.Clear();
			Singleton<CustomizingCardArtworkLoader>.Instance._artworkData.Clear();
			Singleton<CustomizingBookSkinLoader>.Instance._bookSkinData.Clear();
			Harmony_Patch.ArtWorks = null;
			Harmony_Patch.CustomEffects.Clear();
			Harmony_Patch.CustomMapManager.Clear();
			Harmony_Patch.CustomBattleDialogModel.Clear();
			Harmony_Patch.CustomGiftPassive.Clear();
			Harmony_Patch.CustomEmotionCardAbility.Clear();
			Harmony_Patch.LoadedAssembly.Clear();
			Harmony_Patch.ArtWorks.Clear();
			Harmony_Patch.BookThumb.Clear();
			Harmony_Patch.AudioClips.Clear();
			Harmony_Patch.ModEpMatch.Clear();
			Harmony_Patch.ModStoryCG = null;
			Harmony_Patch.ModWorkShopId.Clear();
			OrcTools.StageNameDic.Clear();
			OrcTools.StageConditionDic.Clear();
			OrcTools.CharacterNameDic.Clear();
			OrcTools.EgoDic.Clear();
			OrcTools.DropItemDic.Clear();
			OrcTools.DropItemDicV2.Clear();
			OrcTools.dialogDetails.Clear();
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00014C22 File Offset: 0x00012E22
		private static void CopyLoaderThumbsForCompat()
		{
			Harmony_Patch.BookThumb = XLRoot.BookThumb;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x00014C30 File Offset: 0x00012E30
		private static void LoadBookSkins(Dictionary<string, List<WorkshopSkinData>> _bookSkinData)
		{
			CustomizingBookSkinLoader instance = Singleton<CustomizingBookSkinLoader>.Instance;
			foreach (ModContent modContent in Harmony_Patch.LoadedModContents)
			{
				if (!BasemodConfig.FindBasemodConfig(modContent._itemUniqueId).IgnoreStaticFiles)
				{
					string fullName = modContent._dirInfo.FullName;
					string itemUniqueId = modContent._itemUniqueId;
					string text = "";
					if (!itemUniqueId.ToLower().EndsWith("@origin"))
					{
						text = itemUniqueId;
					}
					int num = 0;
					List<WorkshopSkinData> list;
					if (instance._bookSkinData.TryGetValue(itemUniqueId, out list))
					{
						num = list.Count;
					}
					string text2 = Path.Combine(fullName, "Char");
					List<WorkshopSkinData> list2 = new List<WorkshopSkinData>();
					if (Directory.Exists(text2))
					{
						string[] directories = Directory.GetDirectories(text2);
						for (int i = 0; i < directories.Length; i++)
						{
							try
							{
								WorkshopAppearanceInfo workshopAppearanceInfo = WorkshopAppearanceItemLoader.LoadCustomAppearance(directories[i], false);
								if (workshopAppearanceInfo != null)
								{
									string[] array = directories[i].Split(new char[]
									{
										'\\'
									});
									string text3 = array[array.Length - 1];
									workshopAppearanceInfo.path = directories[i];
									workshopAppearanceInfo.uniqueId = text;
									workshopAppearanceInfo.bookName = "Custom_" + text3;
									if (workshopAppearanceInfo.isClothCustom)
									{
										list2.Add(new WorkshopSkinData
										{
											dic = workshopAppearanceInfo.clothCustomInfo,
											dataName = workshopAppearanceInfo.bookName,
											contentFolderIdx = workshopAppearanceInfo.uniqueId,
											id = num + i
										});
									}
									if (workshopAppearanceInfo.faceCustomInfo != null && workshopAppearanceInfo.faceCustomInfo.Count > 0 && FaceData.GetExtraData(workshopAppearanceInfo.faceCustomInfo) == null)
									{
										XLRoot.LoadFaceCustom(workshopAppearanceInfo.faceCustomInfo, "mod_" + itemUniqueId + ":" + text3);
									}
								}
							}
							catch (Exception ex)
							{
								Debug.LogError("BaseMod: error loading skin at " + directories[i]);
								Debug.LogError(ex);
							}
						}
						List<WorkshopSkinData> list3;
						if (_bookSkinData.TryGetValue(text, out list3) && list3 != null)
						{
							list3.AddRange(list2);
						}
						else
						{
							_bookSkinData[text] = list2;
						}
					}
				}
			}
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00014E7C File Offset: 0x0001307C
		public static WorkshopAppearanceInfo LoadCustomAppearance(string path)
		{
			return WorkshopAppearanceItemLoader.LoadCustomAppearance(path, false);
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00014E88 File Offset: 0x00013088
		public static GameObject CreateCustomCharacter_new(WorkshopSkinData workshopSkinData, out string resourceName, Transform characterRotationCenter = null)
		{
			GameObject result = null;
			resourceName = "";
			try
			{
				if (workshopSkinData != null)
				{
					GameObject customAppearancePrefab = XLRoot.CustomAppearancePrefab;
					if (characterRotationCenter != null)
					{
						result = Object.Instantiate<GameObject>(customAppearancePrefab, characterRotationCenter);
					}
					else
					{
						result = Object.Instantiate<GameObject>(customAppearancePrefab);
					}
					resourceName = workshopSkinData.dataName;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CCCnewerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
				result = null;
			}
			return result;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00014F0C File Offset: 0x0001310C
		public static CharacterMotion CopyCharacterMotion(CharacterAppearance apprearance, ActionDetail detail)
		{
			Harmony_Patch.<>c__DisplayClass24_0 CS$<>8__locals1;
			CS$<>8__locals1.characterMotion = Object.Instantiate<CharacterMotion>(apprearance._motionList[0]);
			CS$<>8__locals1.characterMotion.transform.parent = apprearance._motionList[0].transform.parent;
			CS$<>8__locals1.characterMotion.transform.position = apprearance._motionList[0].transform.position;
			CS$<>8__locals1.characterMotion.transform.localPosition = apprearance._motionList[0].transform.localPosition;
			CS$<>8__locals1.characterMotion.transform.localScale = apprearance._motionList[0].transform.localScale;
			CS$<>8__locals1.characterMotion.transform.name = "Custom_" + detail.ToString();
			CS$<>8__locals1.characterMotion.actionDetail = detail;
			CS$<>8__locals1.characterMotion.motionSpriteSet.Clear();
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer", 2, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("CustomizePivot/DummyHead", 5, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Front", 2, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Back", 2, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Back_Skin", 3, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Skin", 3, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Front_Skin", 3, ref CS$<>8__locals1);
			Harmony_Patch.<CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0("Customize_Renderer_Effect", 9, ref CS$<>8__locals1);
			return CS$<>8__locals1.characterMotion;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0001507C File Offset: 0x0001327C
		[HarmonyPatch(typeof(CustomCoreBookInventoryModel), "GetBookIdList_CustomCoreBook")]
		[HarmonyPostfix]
		private static List<int> CustomCoreBookInventoryModel_GetBookIdList_CustomCoreBook_Post(List<int> idList)
		{
			try
			{
				BookInventoryModel bookInventoryModel = Singleton<BookInventoryModel>.Instance;
				BookXmlList bookXmlList = Singleton<BookXmlList>.Instance;
				idList.RemoveAll(delegate(int x)
				{
					if (bookInventoryModel.GetBookCount(x) < 1)
					{
						BookXmlInfo data = bookXmlList.GetData(x);
						return data == null || data.isError || data.optionList == null || !data.optionList.Contains(1);
					}
					return false;
				});
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			return idList;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x000150D4 File Offset: 0x000132D4
		[HarmonyPatch(typeof(UnitDataModel), "LoadFromSaveData")]
		[HarmonyPostfix]
		private static void UnitDataModel_LoadFromSaveData_Post(UnitDataModel __instance, SaveData data)
		{
			try
			{
				SaveData data2 = data.GetData("BasemodPrefixID");
				if (data2 != null)
				{
					LorId key = LorId.LoadFromSaveData(data2);
					GiftXmlInfo giftXmlInfo;
					if (OrcTools.CustomGifts.TryGetValue(key, out giftXmlInfo))
					{
						__instance.prefixID = giftXmlInfo.id;
					}
				}
				SaveData data3 = data.GetData("BasemodPostfixID");
				if (data3 != null)
				{
					LorId key2 = LorId.LoadFromSaveData(data3);
					GiftXmlInfo giftXmlInfo2;
					if (OrcTools.CustomGifts.TryGetValue(key2, out giftXmlInfo2))
					{
						__instance.postfixID = giftXmlInfo2.id;
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadCustomTitleerror.txt", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00015188 File Offset: 0x00013388
		[HarmonyPatch(typeof(BookModel), "bookIcon", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool BookModel_get_bookIcon_Pre(BookModel __instance, ref Sprite __result)
		{
			try
			{
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(__instance.ClassInfo.BookIcon, out sprite))
				{
					__result = sprite;
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/BookIconerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x00015204 File Offset: 0x00013404
		[HarmonyPatch(typeof(BookXmlInfo), "Name", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool BookXmlInfo_get_Name_Pre(BookXmlInfo __instance, ref string __result)
		{
			try
			{
				string bookName = Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(__instance.workshopID, __instance.TextId));
				if (!string.IsNullOrWhiteSpace(bookName))
				{
					__result = bookName;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x00015254 File Offset: 0x00013454
		[HarmonyPatch(typeof(BookXmlInfo), "Desc", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool BookXmlInfo_get_Desc_Pre(BookXmlInfo __instance, ref List<string> __result)
		{
			try
			{
				List<string> bookText = Singleton<BookDescXmlList>.Instance.GetBookText(new LorId(__instance.workshopID, __instance.TextId));
				if (bookText.Count > 0)
				{
					__result = bookText;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x000152A8 File Offset: 0x000134A8
		[HarmonyPatch(typeof(BookModel), "SetXmlInfo")]
		[HarmonyPostfix]
		private static void BookModel_SetXmlInfo_Post(BookModel __instance, BookXmlInfo classInfo)
		{
			try
			{
				List<LorId> list;
				if (OrcTools.OnlyCardDic.TryGetValue(classInfo.id, out list))
				{
					__instance._onlyCards = new List<DiceCardXmlInfo>();
					foreach (LorId lorId in list)
					{
						DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(lorId, false);
						if (cardItem == null || cardItem.isError)
						{
							Debug.LogError("onlycard not found");
						}
						else
						{
							__instance._onlyCards.Add(cardItem);
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SetOnlyCardserror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x00015384 File Offset: 0x00013584
		[HarmonyPatch(typeof(BookModel), "CreateSoulCard")]
		[HarmonyPrefix]
		private static bool BookModel_CreateSoulCard_Pre(BookModel __instance, int emotionLevel, ref BattleDiceCardModel __result)
		{
			try
			{
				if (__instance._classInfo == null)
				{
					Debug.LogError("BookXmlInfo is null");
					return false;
				}
				List<BookSoulCardInfo_New> list;
				if (!OrcTools.SoulCardDic.TryGetValue(__instance._classInfo.id, out list))
				{
					return true;
				}
				BookSoulCardInfo_New bookSoulCardInfo_New = list.Find((BookSoulCardInfo_New x) => x.emotionLevel == emotionLevel);
				if (bookSoulCardInfo_New == null)
				{
					__result = null;
					return false;
				}
				DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(bookSoulCardInfo_New.lorId, false);
				if (cardItem == null || cardItem.isError)
				{
					__result = null;
					return false;
				}
				__result = BattleDiceCardModel.CreatePlayingCard(cardItem);
				return false;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CreateSoulCarderror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x00015470 File Offset: 0x00013670
		[HarmonyPatch(typeof(StageNameXmlList), "GetName", new Type[]
		{
			typeof(StageClassInfo)
		})]
		[HarmonyPrefix]
		private static bool StageNameXmlList_GetName_Pre(StageClassInfo stageInfo, ref string __result)
		{
			try
			{
				if (stageInfo == null || stageInfo.id == null)
				{
					__result = "Not Found";
					return false;
				}
				string text;
				if (OrcTools.StageNameDic.TryGetValue(stageInfo.id, out text))
				{
					__result = text;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x000154CC File Offset: 0x000136CC
		[HarmonyPatch(typeof(CharactersNameXmlList), "GetName", new Type[]
		{
			typeof(LorId)
		})]
		[HarmonyPrefix]
		private static bool CharactersNameXmlList_GetName_Pre(LorId id, ref string __result)
		{
			try
			{
				string text;
				if (OrcTools.CharacterNameDic.TryGetValue(id, out text))
				{
					__result = text;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x00015508 File Offset: 0x00013708
		[HarmonyPatch(typeof(DropBookXmlInfo), "Name", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool DropBookXmlInfo_get_Name_Pre(DropBookXmlInfo __instance, ref string __result)
		{
			try
			{
				string text = TextDataModel.GetText(__instance._targetText, Array.Empty<object>());
				if (!string.IsNullOrWhiteSpace(text))
				{
					__result = text;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001DA RID: 474 RVA: 0x00015550 File Offset: 0x00013750
		[HarmonyPatch(typeof(UnitDataModel), MethodType.Constructor, new Type[]
		{
			typeof(LorId),
			typeof(SephirahType),
			typeof(bool)
		})]
		[HarmonyPrefix]
		private static void UnitDataModel_ctor_Pre(ref LorId defaultBook)
		{
			LorId lorId;
			if (defaultBook != null && OrcTools.UnitBookDic.TryGetValue(defaultBook, out lorId))
			{
				defaultBook = lorId;
			}
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0001557C File Offset: 0x0001377C
		[HarmonyPatch(typeof(BookXmlList), "GetData", new Type[]
		{
			typeof(LorId),
			typeof(bool)
		})]
		[HarmonyPrefix]
		private static void BookXmlList_GetData_Pre(ref LorId id)
		{
			LorId lorId;
			if (id != null && OrcTools.UnitBookDic.TryGetValue(id, out lorId))
			{
				id = lorId;
			}
		}

		// Token: 0x060001DC RID: 476 RVA: 0x000155A8 File Offset: 0x000137A8
		[HarmonyPatch(typeof(UnitDataModel), "SetByEnemyUnitClassInfo")]
		[HarmonyPostfix]
		private static void UnitDataModel_SetByEnemyUnitClassInfo_Post(UnitDataModel __instance, EnemyUnitClassInfo classInfo)
		{
			try
			{
				string name;
				if (OrcTools.CharacterNameDic.TryGetValue(new LorId(classInfo.workshopID, classInfo.nameId), out name))
				{
					__instance._name = name;
				}
			}
			catch
			{
			}
		}

		// Token: 0x060001DD RID: 477 RVA: 0x000155F0 File Offset: 0x000137F0
		[HarmonyPatch(typeof(UnitDataModel), "SetEnemyDropTable")]
		[HarmonyPrefix]
		private static bool UnitDataModel_SetEnemyDropTable_Pre(UnitDataModel __instance, EnemyUnitClassInfo classInfo)
		{
			try
			{
				if (classInfo == null)
				{
					return false;
				}
				List<EnemyDropItemTable_V2> list;
				List<EnemyDropItemTable_New> list2;
				if (!OrcTools.DropItemDicV2.TryGetValue(classInfo.id, out list) & !OrcTools.DropItemDic.TryGetValue(classInfo.id, out list2))
				{
					return true;
				}
				__instance._dropTable.Clear();
				if (list != null)
				{
					foreach (EnemyDropItemTable_V2 enemyDropItemTable_V in list)
					{
						DropTable dropTable2;
						DropTable dropTable = __instance._dropTable.TryGetValue(enemyDropItemTable_V.emotionLevel, out dropTable2) ? dropTable2 : new DropTable();
						foreach (EnemyDropItem_New enemyDropItem_New in enemyDropItemTable_V.dropItemListNew)
						{
							dropTable.Add(enemyDropItem_New.prob, enemyDropItem_New.bookLorId);
						}
						__instance._dropTable[enemyDropItemTable_V.emotionLevel] = dropTable;
					}
				}
				if (list2 != null)
				{
					foreach (EnemyDropItemTable_New enemyDropItemTable_New in list2)
					{
						DropTable dropTable4;
						DropTable dropTable3 = __instance._dropTable.TryGetValue(enemyDropItemTable_New.emotionLevel, out dropTable4) ? dropTable4 : new DropTable();
						foreach (EnemyDropItem_ReNew enemyDropItem_ReNew in enemyDropItemTable_New.dropList)
						{
							dropTable3.Add(enemyDropItem_ReNew.prob, enemyDropItem_ReNew.bookId);
						}
						__instance._dropTable[enemyDropItemTable_New.emotionLevel] = dropTable3;
					}
				}
				__instance._dropBonus = classInfo.dropBonus;
				__instance._expDrop = classInfo.exp;
				return false;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SetEnemyDropTableerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00015878 File Offset: 0x00013A78
		[HarmonyPatch(typeof(UIInvitationStageInfoPanel), "SetData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIInvitationStageInfoPanel_SetData_In(IEnumerable<CodeInstruction> instructions)
		{
			bool ready = true;
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				int num;
				if (ready && instruction.IsStloc(null) && Harmony_Patch.TryGetIntValue(instruction.operand, out num) && num == 5)
				{
					ready = false;
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Ldarg_1, null);
					yield return new CodeInstruction(OpCodes.Ldarg_2, null);
					yield return new CodeInstruction(OpCodes.Ldloc, 5);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIInvitationStageInfoPanel_SetData_CheckCustomDrop", null, null));
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00015888 File Offset: 0x00013A88
		internal static bool TryGetIntValue(object operand, out int value)
		{
			IConvertible convertible = operand as IConvertible;
			if (convertible != null)
			{
				try
				{
					value = convertible.ToInt32(null);
					return true;
				}
				catch
				{
					goto IL_2E;
				}
			}
			LocalBuilder localBuilder = operand as LocalBuilder;
			if (localBuilder != null)
			{
				value = localBuilder.LocalIndex;
				return true;
			}
			IL_2E:
			value = 0;
			return false;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x000158DC File Offset: 0x00013ADC
		private static void UIInvitationStageInfoPanel_SetData_CheckCustomDrop(UIInvitationStageInfoPanel panel, StageClassInfo stage, UIStoryLine story, List<LorId> dropBookIds)
		{
			CanvasGroup cg = panel.rewardBookList.cg;
			EnemyUnitClassInfoList instance = Singleton<EnemyUnitClassInfoList>.Instance;
			if (story == null && cg.interactable)
			{
				foreach (StageWaveInfo stageWaveInfo in stage.waveList)
				{
					foreach (LorId lorId in stageWaveInfo.enemyUnitIdList)
					{
						EnemyUnitClassInfo data = instance.GetData(lorId);
						List<EnemyDropItemTable_V2> list;
						if (OrcTools.DropItemDicV2.TryGetValue(data.id, out list))
						{
							foreach (EnemyDropItemTable_V2 enemyDropItemTable_V in list)
							{
								foreach (EnemyDropItem_New enemyDropItem_New in enemyDropItemTable_V.dropItemListNew)
								{
									if (!dropBookIds.Contains(enemyDropItem_New.bookLorId))
									{
										dropBookIds.Add(enemyDropItem_New.bookLorId);
									}
								}
							}
						}
						List<EnemyDropItemTable_New> list2;
						if (OrcTools.DropItemDic.TryGetValue(data.id, out list2))
						{
							foreach (EnemyDropItemTable_New enemyDropItemTable_New in list2)
							{
								foreach (EnemyDropItem_ReNew enemyDropItem_ReNew in enemyDropItemTable_New.dropList)
								{
									if (!dropBookIds.Contains(enemyDropItem_ReNew.bookId))
									{
										dropBookIds.Add(enemyDropItem_ReNew.bookId);
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x00015B2C File Offset: 0x00013D2C
		[HarmonyPatch(typeof(UnitDataModel), "InitBattleDialogByDefaultBook")]
		[HarmonyPrefix]
		private static bool UnitDataModel_InitBattleDialogByDefaultBook_Pre(UnitDataModel __instance, LorId lorId)
		{
			try
			{
				if (lorId.id <= 0)
				{
					return true;
				}
				OrcTools.DialogDetail dialogDetail = OrcTools.DialogDetail.FindDialogInBookID(lorId) ?? OrcTools.DialogDetail.FindDialogInCharacterID(lorId);
				if (dialogDetail == null)
				{
					return true;
				}
				BattleDialogCharacter battleDialogCharacter = null;
				if (!string.IsNullOrWhiteSpace(dialogDetail.GroupName))
				{
					BattleDialogXmlList instance = Singleton<BattleDialogXmlList>.Instance;
					string groupName = dialogDetail.GroupName;
					int id = dialogDetail.CharacterID.id;
					battleDialogCharacter = instance.GetCharacterData(groupName, id.ToString());
				}
				if (battleDialogCharacter == null)
				{
					battleDialogCharacter = Singleton<BattleDialogXmlList>.Instance.GetCharacterData_Mod(lorId.packageId, dialogDetail.CharacterID.id);
				}
				Type type = Harmony_Patch.FindBattleDialogueModel(dialogDetail.CharacterName, (battleDialogCharacter != null) ? battleDialogCharacter.characterID : null);
				if (type == null)
				{
					__instance.battleDialogModel = new BattleDialogueModel(battleDialogCharacter);
					return false;
				}
				__instance.battleDialogModel = (Activator.CreateInstance(type, new object[]
				{
					battleDialogCharacter
				}) as BattleDialogueModel);
				return false;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/InitBattleDialogByDefaultBookerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00015C4C File Offset: 0x00013E4C
		public static Type FindBattleDialogueModel(string name, string id)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = null;
			}
			else
			{
				name = name.Trim();
				Type result;
				if (Harmony_Patch.CustomBattleDialogModel.TryGetValue(name, out result))
				{
					return result;
				}
			}
			if (string.IsNullOrWhiteSpace(id))
			{
				id = null;
			}
			else
			{
				id = id.Trim();
				Type result2;
				if (Harmony_Patch.CustomBattleDialogModel.TryGetValue(id, out result2))
				{
					return result2;
				}
			}
			Type type = null;
			if (!Harmony_Patch.CoreDialogsLoaded)
			{
				Type typeFromHandle = typeof(BattleDialogueModel);
				foreach (Type type2 in Assembly.Load("Assembly-CSharp").GetTypes())
				{
					if (typeFromHandle.IsAssignableFrom(type2) && type2.Name.StartsWith("BattleDialogueModel_"))
					{
						string text = type2.Name.Substring("BattleDialogueModel_".Length);
						if (!Harmony_Patch.CustomBattleDialogModel.ContainsKey(text))
						{
							Harmony_Patch.CustomBattleDialogModel[text] = type2;
							if (text == name || (text == id && type == null))
							{
								type = type2;
							}
						}
					}
				}
				Harmony_Patch.CoreDialogsLoaded = true;
			}
			return type;
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00015D64 File Offset: 0x00013F64
		[HarmonyPatch(typeof(BattleDiceCardModel), "GetName")]
		[HarmonyPrefix]
		private static bool BattleDiceCardModel_GetName_Pre(BattleDiceCardModel __instance, ref string __result)
		{
			try
			{
				string cardName;
				if (__instance.XmlData._textId <= 0)
				{
					cardName = Harmony_Patch.GetCardName(__instance.GetID());
				}
				else
				{
					cardName = Harmony_Patch.GetCardName(new LorId(__instance.XmlData.workshopID, __instance.XmlData._textId));
				}
				if (cardName != "Not Found")
				{
					__result = cardName;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x00015DDC File Offset: 0x00013FDC
		[HarmonyPatch(typeof(DiceCardItemModel), "GetName")]
		[HarmonyPrefix]
		public static bool DiceCardItemModel_GetName_Pre(DiceCardItemModel __instance, ref string __result)
		{
			try
			{
				string cardName;
				if (__instance.ClassInfo._textId <= 0)
				{
					cardName = Harmony_Patch.GetCardName(__instance.GetID());
				}
				else
				{
					cardName = Harmony_Patch.GetCardName(new LorId(__instance.ClassInfo.workshopID, __instance.ClassInfo._textId));
				}
				if (cardName != "Not Found")
				{
					__result = cardName;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x00015E54 File Offset: 0x00014054
		[HarmonyPatch(typeof(DiceCardXmlInfo), "Name", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool DiceCardXmlInfo_get_Name_Pre(DiceCardXmlInfo __instance, ref string __result)
		{
			try
			{
				string cardName;
				if (__instance._textId <= 0)
				{
					cardName = Harmony_Patch.GetCardName(__instance.id);
				}
				else
				{
					cardName = Harmony_Patch.GetCardName(new LorId(__instance.workshopID, __instance._textId));
				}
				if (cardName != "Not Found")
				{
					__result = cardName;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x00015EBC File Offset: 0x000140BC
		[HarmonyPatch(typeof(BattleDiceCardUI), "SetCard")]
		[HarmonyPostfix]
		[HarmonyPriority(600)]
		private static void BattleDiceCardUI_SetCard_Post(BattleDiceCardUI __instance, BattleDiceCardModel cardModel)
		{
			try
			{
				string cardName;
				if (cardModel.XmlData._textId <= 0)
				{
					cardName = Harmony_Patch.GetCardName(cardModel.GetID());
				}
				else
				{
					cardName = Harmony_Patch.GetCardName(new LorId(cardModel.XmlData.workshopID, cardModel.XmlData._textId));
				}
				if (cardName != "Not Found")
				{
					__instance.txt_cardName.text = cardName;
				}
			}
			catch
			{
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x00015F34 File Offset: 0x00014134
		[HarmonyPatch(typeof(AssetBundleManagerRemake), "LoadCardSprite")]
		[HarmonyPrefix]
		private static bool AssetBundleManagerRemake_LoadCardSprite_Pre(string name, ref Sprite __result)
		{
			try
			{
				if (name == null)
				{
					return true;
				}
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(name, out sprite))
				{
					__result = sprite;
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadCardSpriteerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00015FAC File Offset: 0x000141AC
		[HarmonyPatch(typeof(CustomizingCardArtworkLoader), "GetSpecificArtworkSprite")]
		[HarmonyPostfix]
		private static Sprite CustomizingCardArtworkLoader_GetSpecificArtworkSprite_Post(Sprite cardArtwork, string name)
		{
			try
			{
				if (cardArtwork == null)
				{
					cardArtwork = Singleton<AssetBundleManagerRemake>.Instance.LoadCardSprite(name);
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadCardSprite2error.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return cardArtwork;
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00016010 File Offset: 0x00014210
		[HarmonyPatch(typeof(BattleDiceCardBuf), "GetBufIcon")]
		[HarmonyPrefix]
		private static bool BattleDiceCardBuf_GetBufIcon_Pre(BattleDiceCardBuf __instance, ref Sprite __result)
		{
			if (Harmony_Patch.ArtWorks == null)
			{
				Harmony_Patch.GetArtWorks();
			}
			if (!__instance._iconInit)
			{
				try
				{
					string keywordIconId = __instance.keywordIconId;
					Sprite sprite;
					if (!string.IsNullOrWhiteSpace(keywordIconId) && Harmony_Patch.ArtWorks.TryGetValue("CardBuf_" + keywordIconId, out sprite) && sprite != null)
					{
						__instance._iconInit = true;
						__instance._bufIcon = sprite;
						__result = sprite;
						return false;
					}
				}
				catch
				{
				}
				return true;
			}
			return true;
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00016090 File Offset: 0x00014290
		[HarmonyPatch(typeof(BookPassiveInfo), "name", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool BookPassiveInfo_get_name_Pre(BookPassiveInfo __instance, ref string __result)
		{
			try
			{
				string name = Singleton<PassiveDescXmlList>.Instance.GetName(__instance.passive.id);
				if (!string.IsNullOrWhiteSpace(name))
				{
					__result = name;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001EB RID: 491 RVA: 0x000160DC File Offset: 0x000142DC
		[HarmonyPatch(typeof(BookPassiveInfo), "desc", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool BookPassiveInfo_get_desc_Pre(BookPassiveInfo __instance, ref string __result)
		{
			try
			{
				string desc = Singleton<PassiveDescXmlList>.Instance.GetDesc(__instance.passive.id);
				if (!string.IsNullOrWhiteSpace(desc))
				{
					__result = desc;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x060001EC RID: 492 RVA: 0x00016128 File Offset: 0x00014328
		[HarmonyPatch(typeof(BattleUnitBufListDetail), "OnRoundStart")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> BattleUnitBufListDetail_OnRoundStart_In(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
		{
			LocalBuilder newBufLocal = ilgen.DeclareLocal(typeof(BattleUnitBuf));
			LocalBuilder oldBufListLocal = ilgen.DeclareLocal(typeof(BattleUnitBuf));
			LocalBuilder bufPredicateLocal = ilgen.DeclareLocal(typeof(Predicate<BattleUnitBuf>));
			MethodInfo currentBufProperty = AccessTools.PropertyGetter(typeof(List<BattleUnitBuf>.Enumerator), "Current");
			MethodInfo independentIconProperty = AccessTools.PropertyGetter(typeof(BattleUnitBuf), "independentBufIcon");
			MethodInfo iconGetterMethod = AccessTools.Method(typeof(BattleUnitBuf), "GetBufIcon", null, null);
			MethodInfo unityEqualityMethod = AccessTools.Method(typeof(Object), "op_Equality", new Type[]
			{
				typeof(Object),
				typeof(Object)
			}, null);
			MethodInfo bufFindMethod = AccessTools.Method(typeof(List<BattleUnitBuf>), "Find", null, null);
			MethodInfo helper = AccessTools.Method(typeof(Harmony_Patch), "BattleUnitBufListDetail_OnRoundStart_FindSameTypeBuf", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				bool patchingFind = instruction.Is(OpCodes.Callvirt, bufFindMethod);
				if (patchingFind)
				{
					yield return new CodeInstruction(OpCodes.Stloc, bufPredicateLocal);
					yield return new CodeInstruction(OpCodes.Dup, null);
					yield return new CodeInstruction(OpCodes.Stloc, oldBufListLocal);
					yield return new CodeInstruction(OpCodes.Ldloc, bufPredicateLocal);
				}
				yield return instruction;
				if (patchingFind)
				{
					yield return new CodeInstruction(OpCodes.Ldloc, newBufLocal);
					yield return new CodeInstruction(OpCodes.Ldloc, oldBufListLocal);
					yield return new CodeInstruction(OpCodes.Call, helper);
				}
				else if (instruction.Is(OpCodes.Call, currentBufProperty))
				{
					yield return new CodeInstruction(OpCodes.Dup, null);
					yield return new CodeInstruction(OpCodes.Stloc, newBufLocal);
				}
				else if (instruction.Is(OpCodes.Callvirt, independentIconProperty))
				{
					yield return new CodeInstruction(OpCodes.Ldloc, newBufLocal);
					yield return new CodeInstruction(OpCodes.Callvirt, iconGetterMethod);
					yield return new CodeInstruction(OpCodes.Ldnull, null);
					yield return new CodeInstruction(OpCodes.Call, unityEqualityMethod);
					yield return new CodeInstruction(OpCodes.Or, null);
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060001ED RID: 493 RVA: 0x00016140 File Offset: 0x00014340
		private static BattleUnitBuf BattleUnitBufListDetail_OnRoundStart_FindSameTypeBuf(BattleUnitBuf oldBuf, BattleUnitBuf newBuf, List<BattleUnitBuf> list)
		{
			if (oldBuf != null)
			{
				return oldBuf;
			}
			return list.Find((BattleUnitBuf x) => x.GetType() == newBuf.GetType() && !x.IsDestroyed());
		}

		// Token: 0x060001EE RID: 494 RVA: 0x00016171 File Offset: 0x00014371
		[HarmonyPatch(typeof(BattleUnitBufListDetail), "CanAddBuf")]
		[HarmonyPrefix]
		private static void BattleUnitBufListDetail_CanAddBuf_Pre(BattleUnitBufListDetail __instance, BattleUnitBuf buf, ref BattleUnitModel __state)
		{
			if (buf == null || __instance._self == null || buf._owner == __instance._self)
			{
				return;
			}
			if (buf._owner != null)
			{
				__state = buf._owner;
			}
			buf._owner = __instance._self;
		}

		// Token: 0x060001EF RID: 495 RVA: 0x000161A9 File Offset: 0x000143A9
		[HarmonyPatch(typeof(BattleUnitBufListDetail), "CanAddBuf")]
		[HarmonyPostfix]
		private static void BattleUnitBufListDetail_CanAddBuf_Post(BattleUnitBuf buf, BattleUnitModel __state)
		{
			if (buf == null || __state == null)
			{
				return;
			}
			buf._owner = __state;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x000161BC File Offset: 0x000143BC
		[HarmonyPatch(typeof(BattleUnitBuf), "GetBufIcon")]
		[HarmonyPrefix]
		private static void BattleUnitBuf_GetBufIcon_Pre(BattleUnitBuf __instance)
		{
			if (Harmony_Patch.ArtWorks == null)
			{
				Harmony_Patch.GetArtWorks();
			}
			if (BattleUnitBuf._bufIconDictionary == null)
			{
				BattleUnitBuf._bufIconDictionary = new Dictionary<string, Sprite>();
			}
			if (BattleUnitBuf._bufIconDictionary.Count == 0)
			{
				Sprite[] array = Resources.LoadAll<Sprite>("Sprites/BufIconSheet/");
				if (array != null && array.Length != 0)
				{
					for (int i = 0; i < array.Length; i++)
					{
						BattleUnitBuf._bufIconDictionary[array[i].name] = array[i];
					}
				}
			}
			string keywordIconId = __instance.keywordIconId;
			Sprite value;
			if (!string.IsNullOrWhiteSpace(keywordIconId) && !BattleUnitBuf._bufIconDictionary.ContainsKey(keywordIconId) && Harmony_Patch.ArtWorks.TryGetValue(keywordIconId, out value))
			{
				BattleUnitBuf._bufIconDictionary[keywordIconId] = value;
			}
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0001625E File Offset: 0x0001445E
		[HarmonyPatch(typeof(BattleEmotionCardModel), MethodType.Constructor, new Type[]
		{
			typeof(EmotionCardXmlInfo),
			typeof(BattleUnitModel)
		})]
		[HarmonyPriority(200)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> BattleEmotionCardModel_ctor_In(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
		{
			List<CodeInstruction> codes = instructions.ToList<CodeInstruction>();
			Label trueJumpLabel = ilgen.DefineLabel();
			Label falseJumpLabel = ilgen.DefineLabel();
			MethodInfo moveNextMethod = AccessTools.Method(typeof(List<string>.Enumerator), "MoveNext", null, null);
			MethodInfo createInstanceMethod = AccessTools.Method(typeof(Activator), "CreateInstance", new Type[]
			{
				typeof(Type)
			}, null);
			MethodInfo getTypeMethod = AccessTools.Method(typeof(Type), "GetType", new Type[]
			{
				typeof(string)
			}, null);
			int num2;
			for (int i = 0; i < codes.Count; i = num2 + 1)
			{
				int num;
				if ((codes[i].opcode == OpCodes.Ldloca || codes[i].opcode == OpCodes.Ldloca_S) && Harmony_Patch.TryGetIntValue(codes[i].operand, out num) && num == 0)
				{
					if (i < codes.Count - 1 && codes[i + 1].Is(OpCodes.Call, moveNextMethod))
					{
						yield return new CodeInstruction(OpCodes.Nop, null).WithLabels(new Label[]
						{
							falseJumpLabel
						});
					}
				}
				else if (codes[i].Is(OpCodes.Call, createInstanceMethod))
				{
					yield return new CodeInstruction(OpCodes.Dup, null);
					yield return new CodeInstruction(OpCodes.Brtrue, trueJumpLabel);
					yield return new CodeInstruction(OpCodes.Pop, null);
					yield return new CodeInstruction(OpCodes.Br, falseJumpLabel);
					yield return new CodeInstruction(OpCodes.Nop, null).WithLabels(new Label[]
					{
						trueJumpLabel
					});
				}
				yield return codes[i];
				if (codes[i].Is(OpCodes.Call, getTypeMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1, null);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "BattleEmotionCardModel_ctor_CheckCustomAbility", null, null));
				}
				num2 = i;
			}
			yield break;
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00016275 File Offset: 0x00014475
		private static Type BattleEmotionCardModel_ctor_CheckCustomAbility(Type oldType, string name)
		{
			return Harmony_Patch.FindEmotionCardAbilityType(name.Trim()) ?? oldType;
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00016288 File Offset: 0x00014488
		public static EmotionCardAbilityBase FindEmotionCardAbility(string name)
		{
			EmotionCardAbilityBase result;
			try
			{
				result = (Activator.CreateInstance(Harmony_Patch.FindEmotionCardAbilityType(name)) as EmotionCardAbilityBase);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x000162C0 File Offset: 0x000144C0
		public static Type FindEmotionCardAbilityType(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = null;
			}
			else
			{
				name = name.Trim();
				Type result;
				if (Harmony_Patch.CustomEmotionCardAbility.TryGetValue(name, out result))
				{
					return result;
				}
			}
			Type result2 = null;
			if (!Harmony_Patch.CoreEmotionCardsLoaded)
			{
				Type typeFromHandle = typeof(EmotionCardAbilityBase);
				foreach (Type type in Assembly.Load("Assembly-CSharp").GetTypes())
				{
					if (typeFromHandle.IsAssignableFrom(type) && type.Name.StartsWith("EmotionCardAbility_"))
					{
						string text = type.Name.Substring("EmotionCardAbility_".Length);
						if (!Harmony_Patch.CustomEmotionCardAbility.ContainsKey(text))
						{
							Harmony_Patch.CustomEmotionCardAbility[text] = type;
							if (text == name)
							{
								result2 = type;
							}
						}
					}
				}
				Harmony_Patch.CoreEmotionCardsLoaded = true;
			}
			return result2;
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x00016398 File Offset: 0x00014598
		[HarmonyPatch(typeof(UIAbnormalityCardPreviewSlot), "Init")]
		[HarmonyPostfix]
		private static void UIAbnormalityCardPreviewSlot_Init_Post(UIAbnormalityCardPreviewSlot __instance, EmotionCardXmlInfo card)
		{
			try
			{
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(card.Artwork, out sprite))
				{
					__instance.artwork.sprite = sprite;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/EmotionCardPreviewArtworkerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x00016410 File Offset: 0x00014610
		[HarmonyPatch(typeof(BattleUnitDiceActionUI_EmotionCard), "Init")]
		[HarmonyPostfix]
		private static void BattleUnitDiceActionUI_EmotionCard_Init_Post(BattleUnitDiceActionUI_EmotionCard __instance, BattleEmotionCardModel card)
		{
			try
			{
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				string artwork = card.XmlInfo.Artwork;
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(artwork, out sprite))
				{
					__instance.artwork.sprite = sprite;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/EmotionCardDiceActionArtworkerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x00016490 File Offset: 0x00014690
		[HarmonyPatch(typeof(UIEmotionPassiveCardInven), "SetSprites")]
		[HarmonyPostfix]
		private static void UIEmotionPassiveCardInven_SetSprites_Post(UIEmotionPassiveCardInven __instance)
		{
			try
			{
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				string artwork = __instance._card.Artwork;
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(artwork, out sprite))
				{
					__instance._artwork.sprite = sprite;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/EmotionCardInvenPassiveArtworkerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x00016510 File Offset: 0x00014710
		[HarmonyPatch(typeof(EmotionPassiveCardUI), "SetSprites")]
		[HarmonyPostfix]
		private static void EmotionPassiveCardUI_SetSprites_Post(EmotionPassiveCardUI __instance)
		{
			try
			{
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				string artwork = __instance._card.Artwork;
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(artwork, out sprite))
				{
					__instance._artwork.sprite = sprite;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/EmotionCardPassiveArtworkerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x00016590 File Offset: 0x00014790
		[HarmonyPatch(typeof(BattleDiceCardUI), "SetEgoCardForPopup")]
		[HarmonyPostfix]
		private static void BattleDiceCardUI_SetEgoCardForPopup_Post(BattleDiceCardUI __instance, EmotionEgoXmlInfo egoxmlinfo)
		{
			try
			{
				DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(egoxmlinfo.CardId, false);
				string cardName;
				if (cardItem._textId <= 0)
				{
					cardName = Harmony_Patch.GetCardName(cardItem.id);
				}
				else
				{
					cardName = Harmony_Patch.GetCardName(new LorId(cardItem.workshopID, cardItem._textId));
				}
				if (cardName != "Not Found")
				{
					__instance.txt_cardName.text = cardName;
				}
			}
			catch
			{
			}
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0001660C File Offset: 0x0001480C
		[HarmonyPatch(typeof(EmotionEgoXmlInfo), "CardId", MethodType.Getter)]
		[HarmonyPrefix]
		private static bool EmotionEgoXmlInfo_get_CardId_Pre(EmotionEgoXmlInfo __instance, ref LorId __result)
		{
			try
			{
				LorId lorId;
				if (OrcTools.EgoDic.TryGetValue(__instance, out lorId))
				{
					__result = lorId;
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/EgoCardIderror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00016674 File Offset: 0x00014874
		[HarmonyPatch(typeof(QuestMissionModel), MethodType.Constructor, new Type[]
		{
			typeof(QuestModel),
			typeof(QuestMissionXmlInfo)
		})]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> QuestMissionModel_ctor_In(IEnumerable<CodeInstruction> instructions)
		{
			instructions.ToList<CodeInstruction>();
			MethodInfo concatMethod = AccessTools.Method(typeof(string), "Concat", new Type[]
			{
				typeof(object[])
			}, null);
			MethodInfo getTypeMethod = AccessTools.Method(typeof(Type), "GetType", new Type[]
			{
				typeof(string)
			}, null);
			MethodInfo customQuestTextMethod = AccessTools.Method(typeof(Harmony_Patch), "QuestMissionModel_ctor_CheckCustomName", null, null);
			MethodInfo customQuestTypeMethod = AccessTools.Method(typeof(Harmony_Patch), "QuestMissionModel_ctor_CheckCustomType", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(getTypeMethod))
				{
					yield return new CodeInstruction(OpCodes.Dup, null);
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Ldarg_2, null);
					yield return new CodeInstruction(OpCodes.Call, customQuestTypeMethod);
				}
				else
				{
					yield return instruction;
					if (instruction.Calls(concatMethod))
					{
						yield return new CodeInstruction(OpCodes.Ldarg_2, null);
						yield return new CodeInstruction(OpCodes.Call, customQuestTextMethod);
					}
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00016684 File Offset: 0x00014884
		private static string QuestMissionModel_ctor_CheckCustomName(string oldName, QuestMissionXmlInfo questInfo)
		{
			QuestMissionXmlInfo_V2 questMissionXmlInfo_V = questInfo as QuestMissionXmlInfo_V2;
			if (questMissionXmlInfo_V != null && !string.IsNullOrWhiteSpace(questMissionXmlInfo_V.scriptName))
			{
				return "QuestMissionScript_" + questMissionXmlInfo_V.scriptName.Trim();
			}
			return oldName;
		}

		// Token: 0x060001FD RID: 509 RVA: 0x000166C0 File Offset: 0x000148C0
		private static Type QuestMissionModel_ctor_CheckCustomType(string typeName, Type oldType, QuestMissionXmlInfo questInfo)
		{
			QuestMissionXmlInfo_V2 questMissionXmlInfo_V = questInfo as QuestMissionXmlInfo_V2;
			if (questMissionXmlInfo_V != null && !string.IsNullOrWhiteSpace(questMissionXmlInfo_V.scriptName))
			{
				Type type = Harmony_Patch.FindCustomQuestScriptType(questMissionXmlInfo_V.scriptName);
				if (type != null)
				{
					return type;
				}
			}
			if (oldType == null && typeName.StartsWith("QuestMissionScript_"))
			{
				oldType = Harmony_Patch.FindCustomQuestScriptType(typeName.Substring("QuestMissionScript_".Length));
			}
			return oldType;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0001672C File Offset: 0x0001492C
		public static QuestMissionScriptBase FindCustomQuestScript(string name)
		{
			QuestMissionScriptBase result;
			try
			{
				result = (Activator.CreateInstance(Harmony_Patch.FindCustomQuestScriptType(name)) as QuestMissionScriptBase);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060001FF RID: 511 RVA: 0x00016764 File Offset: 0x00014964
		public static Type FindCustomQuestScriptType(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = null;
			}
			else
			{
				name = name.Trim();
				Type result;
				if (Harmony_Patch.CustomQuest.TryGetValue(name, out result))
				{
					return result;
				}
			}
			Type result2 = null;
			if (!Harmony_Patch.CoreQuestsLoaded)
			{
				Type typeFromHandle = typeof(QuestMissionScriptBase);
				foreach (Type type in Assembly.Load("Assembly-CSharp").GetTypes())
				{
					if (typeFromHandle.IsAssignableFrom(type) && type.Name.StartsWith("QuestMissionScript_"))
					{
						string text = type.Name.Substring("QuestMissionScript_".Length);
						if (!Harmony_Patch.CustomQuest.ContainsKey(text))
						{
							Harmony_Patch.CustomQuest[text] = type;
							if (text == name)
							{
								result2 = type;
							}
						}
					}
				}
				Harmony_Patch.CoreQuestsLoaded = true;
			}
			return result2;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0001683C File Offset: 0x00014A3C
		[HarmonyPatch(typeof(UISpriteDataManager), "GetStoryIcon")]
		[HarmonyPrefix]
		private static bool UISpriteDataManager_GetStoryIcon_Pre(string story, ref UIIconManager.IconSet __result)
		{
			try
			{
				if (story == null)
				{
					return true;
				}
				if (Harmony_Patch.ArtWorks == null)
				{
					Harmony_Patch.GetArtWorks();
				}
				Sprite sprite;
				if (Harmony_Patch.ArtWorks.TryGetValue(story, out sprite))
				{
					__result = new UIIconManager.IconSet
					{
						type = story,
						icon = sprite,
						iconGlow = sprite
					};
					Sprite iconGlow;
					if (Harmony_Patch.ArtWorks.TryGetValue(story + "_Glow", out iconGlow))
					{
						__result.iconGlow = iconGlow;
					}
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/StroyIconerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x06000201 RID: 513 RVA: 0x000168F4 File Offset: 0x00014AF4
		[HarmonyPatch(typeof(UIStoryArchivesPanel), "InitData")]
		[HarmonyPostfix]
		private static void UIStoryArchivesPanel_InitData_Post(UIStoryArchivesPanel __instance)
		{
			try
			{
				if (__instance.episodeBooksData.Count == 0 && __instance.chapterBooksData.Count > 0)
				{
					__instance.bookStoryPanel.SetData();
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/StoryArchivesIniterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00016968 File Offset: 0x00014B68
		[HarmonyPatch(typeof(UIBookStoryPanel), "SetData")]
		[HarmonyPrefix]
		[HarmonyPriority(600)]
		private static void UIBookStoryPanel_SetData_Pre(UIBookStoryPanel __instance)
		{
			try
			{
				UIStoryArchivesPanel panel = __instance.panel;
				int i;
				int num = i = 0;
				while (i < panel.chapterBooksData.Count)
				{
					BookXmlInfo bookXmlInfo = panel.chapterBooksData[i];
					if (bookXmlInfo.id.IsWorkshop() && OrcTools.EpisodeDic.ContainsKey(bookXmlInfo.id))
					{
						panel.episodeBooksData.Add(bookXmlInfo);
					}
					else
					{
						panel.chapterBooksData[num] = panel.chapterBooksData[i];
						num++;
					}
					i++;
				}
				panel.chapterBooksData.RemoveRange(num, i - num);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/BookStorySetDataerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00016A3C File Offset: 0x00014C3C
		[HarmonyPatch(typeof(UIBookStoryPanel), "SetData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIBookStoryPanel_SetData_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo method = AccessTools.Method(typeof(Type), "GetTypeFromHandle", null, null);
			MethodInfo method2 = AccessTools.Method(typeof(Enum), "GetValues", null, null);
			MethodInfo method3 = AccessTools.PropertyGetter(typeof(Array), "Length");
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
			for (int i = 0; i < list.Count - 3; i++)
			{
				if (list[i].Is(OpCodes.Ldtoken, typeof(UIStoryLine)) && list[i + 1].Calls(method) && list[i + 2].Calls(method2) && list[i + 3].Calls(method3))
				{
					list.RemoveRange(i + 1, 3);
					UIStoryLine[] originalValues = EnumExtender.GetOriginalValues<UIStoryLine>();
					int num = Array.BinarySearch<UIStoryLine>(originalValues, 100) - 1;
					list[i] = new CodeInstruction(OpCodes.Ldc_I4, originalValues[num]);
					break;
				}
			}
			return list;
		}

		// Token: 0x06000204 RID: 516 RVA: 0x00016B44 File Offset: 0x00014D44
		[HarmonyPatch(typeof(StoryTotal), "SetData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> StoryTotal_SetData_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo method = AccessTools.Method(typeof(Type), "GetTypeFromHandle", null, null);
			MethodInfo method2 = AccessTools.Method(typeof(Enum), "GetValues", null, null);
			MethodInfo method3 = AccessTools.PropertyGetter(typeof(Array), "Length");
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
			for (int i = 0; i < list.Count - 3; i++)
			{
				if (list[i].Is(OpCodes.Ldtoken, typeof(UIStoryLine)) && list[i + 1].Calls(method) && list[i + 2].Calls(method2) && list[i + 3].Calls(method3))
				{
					list.RemoveRange(i + 1, 3);
					UIStoryLine[] originalValues = EnumExtender.GetOriginalValues<UIStoryLine>();
					int num = Array.BinarySearch<UIStoryLine>(originalValues, 100) - 1;
					list[i] = new CodeInstruction(OpCodes.Ldc_I4, originalValues[num] + 1);
					break;
				}
			}
			return list;
		}

		// Token: 0x06000205 RID: 517 RVA: 0x00016C4C File Offset: 0x00014E4C
		[HarmonyPatch(typeof(UIBookStoryChapterSlot), "SetEpisodeSlots")]
		[HarmonyPostfix]
		private static void UIBookStoryChapterSlot_SetEpisodeSlots_Post(UIBookStoryChapterSlot __instance)
		{
			try
			{
				bool flag = false;
				List<BookXmlInfo> list = null;
				int i = __instance.EpisodeSlots.Count - 1;
				while (i >= 0)
				{
					if (__instance.EpisodeSlots[i].gameObject.activeSelf)
					{
						if (__instance.EpisodeSlots[i].isEtc)
						{
							flag = true;
							list = __instance.EpisodeSlots[i].books;
							break;
						}
						break;
					}
					else
					{
						i--;
					}
				}
				Dictionary<LorId, List<BookXmlInfo>> dictionary = new Dictionary<LorId, List<BookXmlInfo>>();
				StageClassInfoList instance = Singleton<StageClassInfoList>.Instance;
				foreach (BookXmlInfo bookXmlInfo in __instance.panel.panel.GetEpisodeBooksDataAll())
				{
					LorId lorId;
					if (OrcTools.EpisodeDic.TryGetValue(bookXmlInfo.id, out lorId))
					{
						StageClassInfo data = instance.GetData(lorId);
						if (data != null && data.chapter == __instance.chapter && (!EnumExtender.IsValidEnumName(bookXmlInfo.BookIcon) || !EnumExtender.IsOriginalName<UIStoryLine>(bookXmlInfo.BookIcon)))
						{
							List<BookXmlInfo> list2;
							if (!dictionary.TryGetValue(lorId, out list2))
							{
								list2 = (dictionary[lorId] = new List<BookXmlInfo>());
							}
							list2.Add(bookXmlInfo);
						}
					}
				}
				foreach (KeyValuePair<LorId, List<BookXmlInfo>> keyValuePair in dictionary)
				{
					i++;
					if (__instance.EpisodeSlots.Count <= i)
					{
						__instance.InstatiateAdditionalSlot();
					}
					__instance.EpisodeSlots[i].Init(keyValuePair.Value, __instance);
					string text;
					if (OrcTools.StageNameDic.TryGetValue(keyValuePair.Key, out text))
					{
						__instance.EpisodeSlots[i].episodeText.text = text;
					}
				}
				if (flag && list.Count > 0 && !__instance.EpisodeSlots[i].isEtc)
				{
					i++;
					if (__instance.EpisodeSlots.Count <= i)
					{
						__instance.InstatiateAdditionalSlot();
					}
					__instance.EpisodeSlots[i].Init(__instance.chapter, list, __instance);
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/UBSCSSESerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000206 RID: 518 RVA: 0x00016ED0 File Offset: 0x000150D0
		[HarmonyPatch(typeof(UIBookStoryPanel), "OnSelectEpisodeSlot")]
		[HarmonyPostfix]
		private static void UIBookStoryPanel_OnSelectEpisodeSlot_Post(UIBookStoryPanel __instance, UIBookStoryEpisodeSlot slot)
		{
			try
			{
				LorId key;
				string text;
				if (((slot != null) ? slot.books : null) != null && slot.books.Count > 0 && OrcTools.EpisodeDic.TryGetValue(slot.books[0].id, out key) && OrcTools.StageNameDic.TryGetValue(key, out text))
				{
					__instance.selectedEpisodeText.text = text;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/OnSelectEpisodeSloterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000207 RID: 519 RVA: 0x00016F74 File Offset: 0x00015174
		[HarmonyPatch(typeof(StorySerializer), "HasEffectFile")]
		[HarmonyPrefix]
		private static bool StorySerializer_HasEffectFile_Pre(StageStoryInfo stageStoryInfo, ref bool __result)
		{
			try
			{
				if (stageStoryInfo == null)
				{
					return true;
				}
				if (stageStoryInfo.IsMod)
				{
					BasemodConfig basemodConfig = BasemodConfig.FindBasemodConfig(stageStoryInfo.packageId);
					if (basemodConfig.IgnoreStory)
					{
						return true;
					}
					string path = Path.Combine(Singleton<ModContentManager>.Instance.GetModPath(stageStoryInfo.packageId), "Data", "StoryText");
					string[] array = stageStoryInfo.story.Split(new char[]
					{
						'.'
					});
					if (Directory.Exists(path))
					{
						FileInfo[] files = new DirectoryInfo(path).GetFiles();
						for (int i = 0; i < files.Length; i++)
						{
							if (Path.GetFileNameWithoutExtension(files[i].FullName) == array[0])
							{
								__result = true;
								return false;
							}
						}
						string text = Path.Combine(path, TextDataModel.CurrentLanguage);
						if (!Directory.Exists(text))
						{
							text = Path.Combine(path, basemodConfig.DefaultLocale);
						}
						if (Directory.Exists(text))
						{
							files = new DirectoryInfo(text).GetFiles();
							for (int i = 0; i < files.Length; i++)
							{
								if (Path.GetFileNameWithoutExtension(files[i].FullName) == array[0])
								{
									__result = true;
									return false;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/HasEffectFileerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x06000208 RID: 520 RVA: 0x000170F8 File Offset: 0x000152F8
		[HarmonyPatch(typeof(ModContentManager), "GetModPath")]
		[HarmonyPrefix]
		private static bool ModContentManager_GetModPath_Pre(ModContentManager __instance, string packageId, ref string __result)
		{
			try
			{
				if (__instance._loadedContents != null)
				{
					ModContent modContent = __instance._loadedContents.Find((ModContent mod) => mod._modInfo.invInfo.workshopInfo.uniqueId == packageId);
					if (modContent != null)
					{
						string fullName = modContent._dirInfo.FullName;
						if (fullName != null && !string.IsNullOrWhiteSpace(fullName))
						{
							__result = fullName;
							return false;
						}
					}
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0001716C File Offset: 0x0001536C
		[HarmonyPatch(typeof(StorySerializer), "LoadStageStory")]
		[HarmonyPrefix]
		[HarmonyPriority(600)]
		private static bool StorySerializer_LoadStageStory_Pre(StageStoryInfo stageStoryInfo, ref bool __result)
		{
			try
			{
				if (stageStoryInfo == null)
				{
					__result = false;
					return false;
				}
				if (stageStoryInfo.IsMod)
				{
					BasemodConfig basemodConfig = BasemodConfig.FindBasemodConfig(stageStoryInfo.packageId);
					if (basemodConfig.IgnoreStory)
					{
						return true;
					}
					string modPath = Singleton<ModContentManager>.Instance.GetModPath(stageStoryInfo.packageId);
					string path = Path.Combine(modPath, "Data", "StoryText");
					string path2 = Path.Combine(modPath, "Data", "StoryEffect");
					string text = Path.Combine(path, stageStoryInfo.story);
					string text2 = Path.Combine(path2, stageStoryInfo.story);
					string[] array = stageStoryInfo.story.Split(new char[]
					{
						'.'
					});
					if (Directory.Exists(path2))
					{
						foreach (FileInfo fileInfo in new DirectoryInfo(path2).GetFiles())
						{
							if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == array[0])
							{
								text2 = fileInfo.FullName;
							}
						}
					}
					if (Directory.Exists(path))
					{
						string text3 = Path.Combine(path, TextDataModel.CurrentLanguage);
						if (!Directory.Exists(text3))
						{
							text3 = Path.Combine(path, basemodConfig.DefaultLocale);
						}
						if (Directory.Exists(text3))
						{
							foreach (FileInfo fileInfo2 in new DirectoryInfo(text3).GetFiles())
							{
								if (Path.GetFileNameWithoutExtension(fileInfo2.FullName) == array[0])
								{
									text = fileInfo2.FullName;
								}
							}
						}
						else
						{
							foreach (FileInfo fileInfo3 in new DirectoryInfo(path).GetFiles())
							{
								if (Path.GetFileNameWithoutExtension(fileInfo3.FullName) == array[0])
								{
									text = fileInfo3.FullName;
								}
							}
						}
					}
					if (StorySerializer.LoadStoryFile(text, text2, modPath))
					{
						__result = true;
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadStageStoryerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x0600020A RID: 522 RVA: 0x00017394 File Offset: 0x00015594
		[HarmonyPatch(typeof(UIBattleStoryInfoPanel), "SetData")]
		[HarmonyPrefix]
		private static void UIBattleStoryInfoPanel_SetData_Pre(UIBattleStoryInfoPanel __instance, StageClassInfo stage)
		{
			try
			{
				if (stage != null && stage.id.IsWorkshop())
				{
					if (!BasemodConfig.FindBasemodConfig(stage.workshopID).IgnoreStory)
					{
						string text = Path.Combine(Singleton<ModContentManager>.Instance.GetModPath(stage.workshopID), "Resource", "StoryBgSprite", StorySerializer.effectDefinition.cg.src);
						Sprite modStoryCG = Harmony_Patch.GetModStoryCG(stage.id, text);
						if (modStoryCG != null)
						{
							__instance.CG.sprite = modStoryCG;
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/UIStoryInfo_SDerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600020B RID: 523 RVA: 0x00017458 File Offset: 0x00015658
		[HarmonyPatch(typeof(StoryManager), "LoadBackgroundSprite")]
		[HarmonyPrefix]
		private static bool StoryManager_LoadBackgroundSprite_Pre(StoryManager __instance, string src, ref Sprite __result)
		{
			if (string.IsNullOrWhiteSpace(src))
			{
				return true;
			}
			try
			{
				string text = src;
				Sprite sprite;
				if (!__instance._loadedCustomSprites.TryGetValue("bgsprite:" + src, out sprite) && !Harmony_Patch.CheckedCustomSprites.Contains(src))
				{
					if (!File.Exists(Path.Combine(ModUtil.GetModBgSpritePath(StorySerializer.curModPath), src)))
					{
						text += ".png";
					}
					sprite = SpriteUtil.LoadSprite(Path.Combine(ModUtil.GetModBgSpritePath(StorySerializer.curModPath), text), new Vector2(0.5f, 0.5f));
					if (sprite != null)
					{
						__instance._loadedCustomSprites.Add("bgsprite:" + src, sprite);
					}
					else
					{
						Harmony_Patch.CheckedCustomSprites.Add(src);
					}
				}
				if (sprite != null)
				{
					__result = sprite;
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LoadBackgroundSpriteerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x0600020C RID: 524 RVA: 0x00017564 File Offset: 0x00015764
		public static Sprite GetModStoryCG(LorId StageId, string Path)
		{
			if (StageId == null)
			{
				return null;
			}
			Harmony_Patch.ModStroyCG modStroyCG;
			if (Harmony_Patch.ModStoryCG.TryGetValue(StageId, out modStroyCG))
			{
				return modStroyCG.sprite;
			}
			if (Harmony_Patch.CheckedModStoryCG.Contains(StageId))
			{
				return null;
			}
			try
			{
				if (File.Exists(Path))
				{
					Texture2D texture2D = new Texture2D(2, 2);
					ImageConversion.LoadImage(texture2D, File.ReadAllBytes(Path));
					Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
					Harmony_Patch.ModStoryCG[StageId] = new Harmony_Patch.ModStroyCG
					{
						path = Path,
						sprite = sprite
					};
					return sprite;
				}
				if (File.Exists(Path + ".png"))
				{
					Texture2D texture2D2 = new Texture2D(2, 2);
					ImageConversion.LoadImage(texture2D2, File.ReadAllBytes(Path + ".png"));
					Sprite sprite2 = Sprite.Create(texture2D2, new Rect(0f, 0f, (float)texture2D2.width, (float)texture2D2.height), new Vector2(0.5f, 0.5f));
					Harmony_Patch.ModStoryCG[StageId] = new Harmony_Patch.ModStroyCG
					{
						path = Path,
						sprite = sprite2
					};
					return sprite2;
				}
			}
			catch
			{
			}
			Harmony_Patch.CheckedModStoryCG.Add(StageId);
			return null;
		}

		// Token: 0x0600020D RID: 525 RVA: 0x000176D0 File Offset: 0x000158D0
		[HarmonyPatch(typeof(StageController), "GameOver")]
		[HarmonyPostfix]
		private static void StageController_GameOver_Post(StageController __instance)
		{
			try
			{
				Harmony_Patch.ModStroyCG modStroyCG;
				if (Harmony_Patch.ModStoryCG.TryGetValue(__instance._stageModel.ClassInfo.id, out modStroyCG))
				{
					ModSaveTool.SaveString("ModLastStroyCG", modStroyCG.path, "BaseMod");
				}
				else if (Harmony_Patch.TryAddModStoryCG(__instance._stageModel.ClassInfo))
				{
					ModSaveTool.SaveString("ModLastStroyCG", Harmony_Patch.ModStoryCG[__instance._stageModel.ClassInfo.id].path, "BaseMod");
				}
				else
				{
					ModSaveTool.SaveString("ModLastStroyCG", "", "BaseMod");
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SaveModCGerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600020E RID: 526 RVA: 0x000177A8 File Offset: 0x000159A8
		private static bool TryAddModStoryCG(StageClassInfo stageClassInfo)
		{
			StageStoryInfo stageStoryInfo = (stageClassInfo != null) ? stageClassInfo.GetStartStory() : null;
			if (stageStoryInfo == null || string.IsNullOrWhiteSpace(stageStoryInfo.story))
			{
				return false;
			}
			if (!stageClassInfo.id.IsWorkshop())
			{
				return false;
			}
			if (BasemodConfig.FindBasemodConfig(stageClassInfo.id.packageId).IgnoreStory)
			{
				return false;
			}
			string modPath = Singleton<ModContentManager>.Instance.GetModPath(stageStoryInfo.packageId);
			if (string.IsNullOrWhiteSpace(modPath))
			{
				return false;
			}
			string text = Path.Combine(modPath, "Data", "StoryEffect", stageStoryInfo.story);
			string[] array = stageClassInfo.GetStartStory().story.Split(new char[]
			{
				'.'
			});
			string text2 = string.Empty;
			if (File.Exists(Path.Combine(modPath, "Data", "StoryEffect", array[0] + ".xml")))
			{
				text = Path.Combine(modPath, "Data", "StoryEffect", array[0] + ".xml");
			}
			if (File.Exists(Path.Combine(modPath, "Data", "StoryEffect", array[0] + ".txt")))
			{
				text = Path.Combine(modPath, "Data", "StoryEffect", array[0] + ".txt");
			}
			if (File.Exists(text))
			{
				using (StreamReader streamReader = new StreamReader(text))
				{
					text2 = ((SceneEffect)new XmlSerializer(typeof(SceneEffect)).Deserialize(streamReader)).cg.src;
				}
			}
			if (string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			string text3 = Path.Combine(modPath, "Resource", "StoryBgSprite", text2);
			return Harmony_Patch.GetModStoryCG(stageClassInfo.id, text3) != null;
		}

		// Token: 0x0600020F RID: 527 RVA: 0x0001795C File Offset: 0x00015B5C
		[HarmonyPatch(typeof(EntryScene), "SetCG")]
		[HarmonyPostfix]
		[HarmonyPriority(600)]
		private static void EntryScene_SetCG_Post(EntryScene __instance)
		{
			try
			{
				string @string = ModSaveTool.GetModSaveData("BaseMod").GetString("ModLastStroyCG");
				if (!string.IsNullOrWhiteSpace(@string) && File.Exists(@string))
				{
					Texture2D texture2D = new Texture2D(1, 1);
					ImageConversion.LoadImage(texture2D, File.ReadAllBytes(@string));
					Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
					__instance.CGImage.sprite = sprite;
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/SetEntrySceneCGerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000210 RID: 528 RVA: 0x00017A44 File Offset: 0x00015C44
		[HarmonyPatch(typeof(StageExtraCondition), "IsUnlocked")]
		[HarmonyPrefix]
		private static bool StageExtraCondition_IsUnlocked_Pre(StageExtraCondition __instance, ref bool __result)
		{
			try
			{
				List<LorId> list;
				if (OrcTools.StageConditionDic.TryGetValue(__instance, out list))
				{
					__result = true;
					foreach (LorId lorId in list)
					{
						if (LibraryModel.Instance.ClearInfo.GetClearCount(lorId) <= 0)
						{
							__result = false;
							break;
						}
					}
					return false;
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/StageConditionerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x06000211 RID: 529 RVA: 0x00017B1C File Offset: 0x00015D1C
		[HarmonyPatch(typeof(UIInvitationRightMainPanel), "SendInvitation")]
		[HarmonyTranspiler]
		[HarmonyPriority(200)]
		private static IEnumerable<CodeInstruction> UIInvitationRightMainPanel_SendInvitation_In(IEnumerable<CodeInstruction> instructions)
		{
			bool waiting = true;
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Stloc_1 && waiting)
				{
					waiting = false;
					yield return new CodeInstruction(OpCodes.Ldloc_0, null);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIInvitationRightMainPanel_SendInvitation_CheckCustomCondition", null, null));
					yield return new CodeInstruction(OpCodes.And, null);
				}
				yield return instruction;
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000212 RID: 530 RVA: 0x00017B2C File Offset: 0x00015D2C
		private static bool UIInvitationRightMainPanel_SendInvitation_CheckCustomCondition(StageClassInfo bookRecipe)
		{
			return bookRecipe.currentState != 3;
		}

		// Token: 0x06000213 RID: 531 RVA: 0x00017B3C File Offset: 0x00015D3C
		[HarmonyPatch(typeof(DebugConsoleScript), "EnterCreatureBattle")]
		[HarmonyPatch(typeof(UIController), "OnClickStartCreatureStage")]
		[HarmonyPatch(typeof(UICreatureRebattleNumberSlot), "OnPointerClick")]
		[HarmonyPatch(typeof(UICreatureRebattleNumberSlot), "SetData")]
		[HarmonyPatch(typeof(UIMainPanel), "OnClickLevelUp")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> FloorLevelXmlInfo_CustomStageId_In(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
		{
			FieldInfo field = AccessTools.Field(typeof(FloorLevelXmlInfo), "stageId");
			MethodInfo method = AccessTools.Method(typeof(StageClassInfoList), "GetData", new Type[]
			{
				typeof(int)
			}, null);
			MethodInfo operand = AccessTools.Method(typeof(Harmony_Patch), "FloorLevelXmlInfo_TryFixStageInfo", null, null);
			MethodInfo method2 = AccessTools.Method(typeof(StageNameXmlList), "GetName", new Type[]
			{
				typeof(int)
			}, null);
			MethodInfo operand2 = AccessTools.Method(typeof(Harmony_Patch), "FloorLevelXmlInfo_TryFixStageName", null, null);
			LocalBuilder localBuilder = null;
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
			for (int i = 0; i < list.Count - 1; i++)
			{
				if (list[i].LoadsField(field, false))
				{
					if (list[i + 1].Calls(method))
					{
						if (localBuilder == null)
						{
							localBuilder = ilgen.DeclareLocal(typeof(FloorLevelXmlInfo));
						}
						list.InsertRange(i, new CodeInstruction[]
						{
							new CodeInstruction(OpCodes.Dup, null),
							new CodeInstruction(OpCodes.Stloc, localBuilder)
						});
						list.InsertRange(i + 4, new CodeInstruction[]
						{
							new CodeInstruction(OpCodes.Ldloc, localBuilder),
							new CodeInstruction(OpCodes.Call, operand)
						});
						i += 4;
					}
					else if (list[i + 1].Calls(method2))
					{
						if (localBuilder == null)
						{
							localBuilder = ilgen.DeclareLocal(typeof(FloorLevelXmlInfo));
						}
						list.InsertRange(i, new CodeInstruction[]
						{
							new CodeInstruction(OpCodes.Dup, null),
							new CodeInstruction(OpCodes.Stloc, localBuilder)
						});
						list.InsertRange(i + 4, new CodeInstruction[]
						{
							new CodeInstruction(OpCodes.Ldloc, localBuilder),
							new CodeInstruction(OpCodes.Call, operand2)
						});
						i += 4;
					}
				}
			}
			return list;
		}

		// Token: 0x06000214 RID: 532 RVA: 0x00017D38 File Offset: 0x00015F38
		private static string FloorLevelXmlInfo_TryFixStageName(string name, FloorLevelXmlInfo info)
		{
			LorId lorId;
			if (OrcTools.FloorLevelStageDic.TryGetValue(info, out lorId))
			{
				StageClassInfo data = Singleton<StageClassInfoList>.Instance.GetData(lorId);
				if (data != null)
				{
					string name2 = Singleton<StageNameXmlList>.Instance.GetName(data);
					if (name2 != null && !string.IsNullOrWhiteSpace(name2) && name2 != "Not Found")
					{
						return name2;
					}
				}
			}
			return name;
		}

		// Token: 0x06000215 RID: 533 RVA: 0x00017D8C File Offset: 0x00015F8C
		private static StageClassInfo FloorLevelXmlInfo_TryFixStageInfo(StageClassInfo stage, FloorLevelXmlInfo info)
		{
			LorId lorId;
			if (OrcTools.FloorLevelStageDic.TryGetValue(info, out lorId))
			{
				StageClassInfo data = Singleton<StageClassInfoList>.Instance.GetData(lorId);
				if (data != null)
				{
					return data;
				}
			}
			return stage;
		}

		// Token: 0x06000216 RID: 534 RVA: 0x00017DBA File Offset: 0x00015FBA
		[HarmonyPatch(typeof(UIOriginEquipPageList), "UpdateEquipPageList")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIOriginEquipPageList_UpdateEquipPageList_In(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction codeInstruction in instructions)
			{
				if (codeInstruction.opcode == OpCodes.Ldc_I4_5)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIOriginEquipPageList), "currentScreenBookModelList"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<BookModel>), "Count"));
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIOriginEquipPageList), "equipPageSlotList"));
					yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<UIOriginEquipPageSlot>), "Count"));
					yield return new CodeInstruction(OpCodes.Sub, null);
				}
				else
				{
					yield return codeInstruction;
				}
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x00017DCA File Offset: 0x00015FCA
		[HarmonyPatch(typeof(UISettingEquipPageScrollList), "SetData")]
		[HarmonyPatch(typeof(UIEquipPageScrollList), "SetData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIEquipPageScrollList_SetData_In(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen, MethodBase original)
		{
			bool firstFind = true;
			MethodInfo findingMethod = AccessTools.Method(typeof(List<UIStoryKeyData>), "Find", null, null);
			MethodInfo isWorkshopProperty = AccessTools.PropertyGetter(typeof(BookModel), "IsWorkshop");
			MethodInfo currentBookProperty = AccessTools.PropertyGetter(typeof(List<BookModel>.Enumerator), "Current");
			LocalBuilder currentBookLocal = ilgen.DeclareLocal(typeof(BookModel));
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (firstFind && instruction.Is(OpCodes.Callvirt, findingMethod))
				{
					firstFind = false;
					yield return new CodeInstruction(OpCodes.Ldloc, currentBookLocal);
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(original.DeclaringType, "totalkeysdata"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIEquipPageScrollList_SetData_FixCustomStory", null, null));
				}
				else if (instruction.Is(OpCodes.Call, currentBookProperty))
				{
					yield return new CodeInstruction(OpCodes.Dup, null);
					yield return new CodeInstruction(OpCodes.Stloc, currentBookLocal);
				}
				else if (instruction.Is(OpCodes.Callvirt, isWorkshopProperty))
				{
					yield return new CodeInstruction(OpCodes.Ldloc, currentBookLocal);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIEquipPageScrollList_SetData_IsCustomStory", null, null));
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000218 RID: 536 RVA: 0x00017DE8 File Offset: 0x00015FE8
		private static UIStoryKeyData UIEquipPageScrollList_SetData_FixCustomStory(UIStoryKeyData oldKey, BookModel bookModel, List<UIStoryKeyData> allKeys)
		{
			LorId episodeId;
			if (!OrcTools.EpisodeDic.TryGetValue(bookModel.BookId, out episodeId))
			{
				return oldKey;
			}
			UIStoryLine storyline = Harmony_Patch.GetModEpMatch(episodeId);
			UIStoryKeyData uistoryKeyData = allKeys.Find((UIStoryKeyData x) => x.workshopId == bookModel.ClassInfo.id.packageId && x.chapter == bookModel.ClassInfo.Chapter && x.StoryLine == storyline);
			if (uistoryKeyData == null)
			{
				uistoryKeyData = new UIStoryKeyData(bookModel.ClassInfo.Chapter, bookModel.ClassInfo.id.packageId)
				{
					StoryLine = storyline
				};
				allKeys.Add(uistoryKeyData);
			}
			return uistoryKeyData;
		}

		// Token: 0x06000219 RID: 537 RVA: 0x00017E7E File Offset: 0x0001607E
		private static bool UIEquipPageScrollList_SetData_IsCustomStory(bool isWorkshop, BookModel book)
		{
			return isWorkshop || !EnumExtender.IsValidEnumName(book.ClassInfo.BookIcon) || !EnumExtender.IsOriginalName<UIStoryLine>(book.ClassInfo.BookIcon);
		}

		// Token: 0x0600021A RID: 538 RVA: 0x00017EAC File Offset: 0x000160AC
		public static UIStoryLine GetModEpMatch(LorId episodeId)
		{
			if (!Harmony_Patch.ModEpMatch.ContainsKey(episodeId))
			{
				UIStoryLine uistoryLine;
				EnumExtender.TryFindUnnamedValue<UIStoryLine>(new UIStoryLine?(Harmony_Patch.ModEpMin), null, false, ref uistoryLine);
				EnumExtender.TryAddName<UIStoryLine>(string.Format("BaseMod{0}", uistoryLine), uistoryLine, false);
				Harmony_Patch.ModEpMatch.Add(episodeId, uistoryLine);
			}
			return Harmony_Patch.ModEpMatch[episodeId];
		}

		// Token: 0x0600021B RID: 539 RVA: 0x00017F14 File Offset: 0x00016114
		[HarmonyPatch(typeof(UISettingInvenEquipPageListSlot), "SetBooksData")]
		[HarmonyPostfix]
		[HarmonyPriority(600)]
		private static void UISettingInvenEquipPageListSlot_SetBooksData_Pre(UISettingInvenEquipPageListSlot __instance, List<BookModel> books, UIStoryKeyData storyKey)
		{
			try
			{
				if (books.Count > 0)
				{
					if (!EnumExtender.IsOriginalValue<UIStoryLine>(storyKey.StoryLine))
					{
						LorId lorId;
						if (OrcTools.EpisodeDic.TryGetValue(books[0].BookId, out lorId))
						{
							StageClassInfo data = Singleton<StageClassInfoList>.Instance.GetData(lorId);
							if (data != null)
							{
								UIIconManager.IconSet storyIcon = UISpriteDataManager.instance.GetStoryIcon(data.storyType);
								if (storyIcon != null)
								{
									__instance.img_IconGlow.enabled = true;
									__instance.img_Icon.enabled = true;
									__instance.img_Icon.sprite = storyIcon.icon;
									__instance.img_IconGlow.sprite = storyIcon.iconGlow;
								}
								string str;
								if (OrcTools.StageNameDic.TryGetValue(data.id, out str))
								{
									__instance.txt_StoryName.text = "workshop " + str;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SIEPLSSBDerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600021C RID: 540 RVA: 0x00018030 File Offset: 0x00016230
		[HarmonyPatch(typeof(UIInvenEquipPageListSlot), "SetBooksData")]
		[HarmonyPostfix]
		[HarmonyPriority(600)]
		private static void UIInvenEquipPageListSlot_SetBooksData_Post(UIInvenEquipPageListSlot __instance, List<BookModel> books, UIStoryKeyData storyKey)
		{
			try
			{
				if (books.Count >= 0)
				{
					if (!EnumExtender.IsOriginalValue<UIStoryLine>(storyKey.StoryLine))
					{
						LorId lorId;
						if (OrcTools.EpisodeDic.TryGetValue(books[0].BookId, out lorId))
						{
							StageClassInfo data = Singleton<StageClassInfoList>.Instance.GetData(lorId);
							if (data != null)
							{
								UIIconManager.IconSet storyIcon = UISpriteDataManager.instance.GetStoryIcon(data.storyType);
								if (storyIcon != null)
								{
									__instance.img_IconGlow.enabled = true;
									__instance.img_Icon.enabled = true;
									__instance.img_Icon.sprite = storyIcon.icon;
									__instance.img_IconGlow.sprite = storyIcon.iconGlow;
								}
								string str;
								if (OrcTools.StageNameDic.TryGetValue(data.id, out str))
								{
									__instance.txt_StoryName.text = "workshop " + str;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/IEPLSSBDerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0001814C File Offset: 0x0001634C
		[HarmonyPatch(typeof(StageController), "InitializeMap")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> StageController_InitializeMap_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo loadPrefabMethod = AccessTools.Method(typeof(Util), "LoadPrefab", new Type[]
			{
				typeof(string),
				typeof(Transform)
			}, null);
			MethodInfo getComponentMethod = AccessTools.Method(typeof(GameObject), "GetComponent", Array.Empty<Type>(), new Type[]
			{
				typeof(MapManager)
			});
			MethodInfo getHelperMethod = AccessTools.Method(typeof(Harmony_Patch), "GetMapComponentFixed", null, null);
			MethodInfo currentMapNameProperty = AccessTools.PropertyGetter(typeof(List<string>.Enumerator), "Current");
			MethodInfo helperMethod = AccessTools.Method(typeof(Harmony_Patch), "StageController_InitializeMap_CheckCustomMap", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Is(OpCodes.Callvirt, getComponentMethod))
				{
					yield return new CodeInstruction(OpCodes.Callvirt, getHelperMethod);
				}
				else
				{
					yield return instruction;
				}
				if (instruction.Is(OpCodes.Call, loadPrefabMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldloca, 1);
					yield return new CodeInstruction(OpCodes.Call, currentMapNameProperty);
					yield return new CodeInstruction(OpCodes.Call, helperMethod);
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0001815C File Offset: 0x0001635C
		private static GameObject StageController_InitializeMap_CheckCustomMap(GameObject originalMapObject, string mapName)
		{
			try
			{
				if (!mapName.ToLower().StartsWith("custom_"))
				{
					return originalMapObject;
				}
				string text = mapName.Substring("custom_".Length).Trim();
				Type type;
				if (Harmony_Patch.CustomMapManager.TryGetValue(text + "MapManager", out type))
				{
					Debug.Log("Find MapManager:" + text);
					if (type == null)
					{
						return originalMapObject;
					}
					GameObject gameObject = Util.LoadPrefab("LibraryMaps/KETHER_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
					MapManager component = gameObject.GetComponent<MapManager>();
					MapManager mapManager = (MapManager)gameObject.AddComponent(type);
					mapManager.borderFrame = component.borderFrame;
					mapManager.backgroundRoot = component.backgroundRoot;
					mapManager._obstacleRoot = component._obstacleRoot;
					mapManager._obstacles = component._obstacles;
					mapManager._roots = component._roots;
					component.enabled = false;
					Object.Destroy(component);
					gameObject.name = "InvitationMap_" + mapName;
					if (mapManager is CustomMapManager)
					{
						(mapManager as CustomMapManager).CustomInit();
					}
					return gameObject;
				}
				else
				{
					StageModel stageModel = Singleton<StageController>.Instance.GetStageModel();
					string text2 = (stageModel != null) ? stageModel.ClassInfo.workshopID : null;
					if (!string.IsNullOrEmpty(text2))
					{
						string str = Path.Combine(Singleton<ModContentManager>.Instance.GetModPath(text2), "CustomMap_" + text);
						if (Directory.Exists(str))
						{
							Debug.Log("Find SimpleMap:" + str);
							GameObject gameObject2 = Util.LoadPrefab("LibraryMaps/KETHER_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
							MapManager component2 = gameObject2.GetComponent<MapManager>();
							SimpleMapManager simpleMapManager = gameObject2.AddComponent<SimpleMapManager>();
							simpleMapManager.borderFrame = component2.borderFrame;
							simpleMapManager.backgroundRoot = component2.backgroundRoot;
							simpleMapManager._obstacleRoot = component2._obstacleRoot;
							simpleMapManager._obstacles = component2._obstacles;
							simpleMapManager._roots = component2._roots;
							component2.enabled = false;
							Object.Destroy(component2);
							gameObject2.name = "InvitationMap_" + mapName;
							simpleMapManager.SimpleInit(str, text);
							simpleMapManager.CustomInit();
							return gameObject2;
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/InitializeMaperror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return originalMapObject;
		}

		// Token: 0x0600021F RID: 543 RVA: 0x000183CC File Offset: 0x000165CC
		private static MapManager GetMapComponentFixed(GameObject gameObject)
		{
			return gameObject.GetComponents<MapManager>().LastOrDefault<MapManager>();
		}

		// Token: 0x06000220 RID: 544 RVA: 0x000183D9 File Offset: 0x000165D9
		[HarmonyPatch(typeof(BattleSceneRoot), "ChangeToEgoMap")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> BattleSceneRoot_ChangeToEgoMap_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo loadPrefabMethod = AccessTools.Method(typeof(Util), "LoadPrefab", new Type[]
			{
				typeof(string),
				typeof(Transform)
			}, null);
			MethodInfo getComponentMethod = AccessTools.Method(typeof(GameObject), "GetComponent", Array.Empty<Type>(), new Type[]
			{
				typeof(MapManager)
			});
			MethodInfo getHelperMethod = AccessTools.Method(typeof(Harmony_Patch), "GetMapComponentFixed", null, null);
			MethodInfo helperMethod = AccessTools.Method(typeof(Harmony_Patch), "BattleSceneRoot_ChangeToEgoMap_CheckCustomMap", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Is(OpCodes.Callvirt, getComponentMethod))
				{
					yield return new CodeInstruction(OpCodes.Callvirt, getHelperMethod);
				}
				else
				{
					yield return instruction;
				}
				if (instruction.Is(OpCodes.Call, loadPrefabMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_1, null);
					yield return new CodeInstruction(OpCodes.Call, helperMethod);
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000221 RID: 545 RVA: 0x000183EC File Offset: 0x000165EC
		private static GameObject BattleSceneRoot_ChangeToEgoMap_CheckCustomMap(GameObject originalMapObject, string mapName)
		{
			try
			{
				if (!mapName.ToLower().StartsWith("custom_"))
				{
					return originalMapObject;
				}
				string text = mapName.Substring("custom_".Length).Trim();
				Type type;
				if (Harmony_Patch.CustomMapManager.TryGetValue(text + "MapManager", out type))
				{
					Debug.Log("Find EGO MapManager:" + text);
					if (type == null)
					{
						return originalMapObject;
					}
					GameObject gameObject = Util.LoadPrefab("LibraryMaps/KETHER_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
					MapManager component = gameObject.GetComponent<MapManager>();
					MapManager mapManager = (MapManager)gameObject.AddComponent(type);
					mapManager.borderFrame = component.borderFrame;
					mapManager.backgroundRoot = component.backgroundRoot;
					mapManager._obstacleRoot = component._obstacleRoot;
					mapManager._obstacles = component._obstacles;
					mapManager._roots = component._roots;
					component.enabled = false;
					Object.Destroy(component);
					gameObject.name = "EGO_CardMap_" + mapName;
					if (mapManager is CustomMapManager)
					{
						(mapManager as CustomMapManager).CustomInit();
					}
					return gameObject;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/ChangeToEgoMaperror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return originalMapObject;
		}

		// Token: 0x06000222 RID: 546 RVA: 0x00018558 File Offset: 0x00016758
		[HarmonyPatch(typeof(StageController), "CanChangeMap")]
		[HarmonyPostfix]
		private static void StageController_CanChangeMap_Post(ref bool __result)
		{
			CustomMapManager customMapManager = SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject as CustomMapManager;
			if (customMapManager != null)
			{
				__result &= customMapManager.IsMapChangable();
			}
		}

		// Token: 0x06000223 RID: 547 RVA: 0x00018584 File Offset: 0x00016784
		[HarmonyPatch(typeof(StageController), "IsTwistedArgaliaBattleEnd")]
		[HarmonyPostfix]
		private static void StageController_IsTwistedArgaliaBattleEnd_Post(ref bool __result)
		{
			CustomMapManager customMapManager = SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject as CustomMapManager;
			if (customMapManager != null && new StackFrame(2).GetMethod().Name.Contains("MapByAssimilation"))
			{
				__result |= !customMapManager.IsMapChangableByAssimilation();
			}
		}

		// Token: 0x06000224 RID: 548 RVA: 0x000185D0 File Offset: 0x000167D0
		public static GameObject FindBaseMap(string name)
		{
			GameObject gameObject = null;
			if (name == "malkuth")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/MALKUTH_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "yesod")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/YESOD_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "hod")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/HOD_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "netzach")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/NETZACH_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "tiphereth")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/TIPHERETH_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "gebura")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/GEBURAH_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "chesed")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/CHESED_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "keter")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/KETHER_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "hokma")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/HOKMA_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (name == "binah")
			{
				gameObject = Util.LoadPrefab("LibraryMaps/BINAH_Map", SingletonBehavior<BattleSceneRoot>.Instance.transform);
			}
			if (gameObject == null)
			{
				try
				{
					gameObject = Util.LoadPrefab("InvitationMaps/InvitationMap_" + name, SingletonBehavior<BattleSceneRoot>.Instance.transform);
				}
				catch (Exception)
				{
					gameObject = null;
				}
			}
			if (gameObject == null)
			{
				try
				{
					gameObject = Util.LoadPrefab("CreatureMaps/CreatureMap_" + name, SingletonBehavior<BattleSceneRoot>.Instance.transform);
				}
				catch (Exception)
				{
					gameObject = null;
				}
			}
			GameObject result;
			if (gameObject != null)
			{
				result = gameObject;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000225 RID: 549 RVA: 0x000187B8 File Offset: 0x000169B8
		[HarmonyPatch(typeof(UIStoryProgressPanel), "SelectedSlot")]
		[HarmonyPrefix]
		private static void UIStoryProgressPanel_SelectedSlot_Pre(UIStoryProgressIconSlot slot)
		{
			try
			{
				string value;
				if (slot == null)
				{
					value = null;
				}
				else
				{
					List<StageClassInfo> storyData = slot._storyData;
					if (storyData == null)
					{
						value = null;
					}
					else
					{
						StageClassInfo stageClassInfo = storyData.FirstOrDefault<StageClassInfo>();
						value = ((stageClassInfo != null) ? stageClassInfo.workshopID : null);
					}
				}
				Harmony_Patch.IsModStorySelected = !string.IsNullOrWhiteSpace(value);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CheckSelectedSloterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000226 RID: 550 RVA: 0x00018838 File Offset: 0x00016A38
		[HarmonyPatch(typeof(UIInvitationRightMainPanel), "SetCustomInvToggle")]
		[HarmonyPrefix]
		private static bool UIInvitationRightMainPanel_SetCustomInvToggle_Pre(UIInvitationRightMainPanel __instance, ref bool ison)
		{
			ison |= Harmony_Patch.IsModStorySelected;
			__instance._workshopInvitationToggle.SetIsOnWithoutNotify(ison);
			__instance.customInvPanel.Close();
			__instance.currentSelectedNormalstage = null;
			return false;
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00018864 File Offset: 0x00016A64
		[HarmonyPatch(typeof(UIInvitationDropBookSlot), "SetData_DropBook")]
		[HarmonyPostfix]
		private static void UIInvitationDropBookSlot_SetData_DropBook_Post(UIInvitationDropBookSlot __instance, LorId bookId)
		{
			try
			{
				if (Singleton<DropBookInventoryModel>.Instance.GetBookCount(bookId) == 0)
				{
					__instance.txt_bookNum.text = "∞";
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000228 RID: 552 RVA: 0x000188A4 File Offset: 0x00016AA4
		[HarmonyPatch(typeof(TextDataModel), "InitTextData")]
		[HarmonyPostfix]
		private static void TextDataModel_InitTextData_Post()
		{
			try
			{
				LocalizedTextLoader_New.ExportOriginalFiles();
				LocalizedTextLoader_New.LoadModFiles(Singleton<ModContentManager>.Instance._loadedContents);
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/InitTextDataerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000229 RID: 553 RVA: 0x00018924 File Offset: 0x00016B24
		[HarmonyPatch(typeof(StorySerializer), "LoadStory", new Type[]
		{
			typeof(bool)
		})]
		[HarmonyPostfix]
		private static void StorySerializer_LoadStory_Post()
		{
			try
			{
				StorySerializer_new.ExportStory();
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/LoadStoryerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600022A RID: 554 RVA: 0x00018998 File Offset: 0x00016B98
		[HarmonyPatch(typeof(DiceEffectManager), "CreateBehaviourEffect")]
		[HarmonyPrefix]
		private static bool DiceEffectManager_CreateBehaviourEffect_Pre(ref DiceAttackEffect __result, string resource, float scaleFactor, BattleUnitView self, BattleUnitView target, float time = 1f)
		{
			try
			{
				if (resource == null || string.IsNullOrWhiteSpace(resource))
				{
					__result = null;
					return false;
				}
				Type type;
				if (Harmony_Patch.CustomEffects.TryGetValue(resource, out type))
				{
					DiceAttackEffect diceAttackEffect = new GameObject(resource).AddComponent(type) as DiceAttackEffect;
					diceAttackEffect.Initialize(self, target, time);
					diceAttackEffect.SetScale(scaleFactor);
					__result = diceAttackEffect;
					return false;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CreateBehaviourEffecterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x0600022B RID: 555 RVA: 0x00018A34 File Offset: 0x00016C34
		[HarmonyPatch(typeof(BattleSimpleActionUI_Dice), "SetDiceValue")]
		[HarmonyPrefix]
		private static bool BattleSimpleActionUI_Dice_SetDiceValue_Pre(BattleSimpleActionUI_Dice __instance, bool enable, int diceValue)
		{
			try
			{
				int num = 0;
				int num2 = diceValue;
				List<GameObject> list = new List<GameObject>();
				List<GameObject> list2 = new List<GameObject>();
				for (int i = 0; i < __instance.layout_numbers.childCount; i++)
				{
					list.Add(__instance.layout_numbers.GetChild(i).gameObject);
					list2.Add(__instance.layout_numberbgs.GetChild(i).gameObject);
					__instance.layout_numbers.GetChild(i).gameObject.SetActive(false);
					__instance.layout_numberbgs.GetChild(i).gameObject.SetActive(false);
				}
				bool flag;
				do
				{
					num++;
					num2 /= 10;
					flag = (num2 == 0);
				}
				while (!flag);
				int num3 = num - __instance.layout_numbers.childCount;
				for (int j = 0; j < num3; j++)
				{
					GameObject gameObject = Object.Instantiate<GameObject>(__instance.layout_numbers.GetChild(0).gameObject, __instance.layout_numbers);
					list.Add(gameObject);
					gameObject.gameObject.SetActive(false);
					GameObject gameObject2 = Object.Instantiate<GameObject>(__instance.layout_numberbgs.GetChild(0).gameObject, __instance.layout_numberbgs);
					list2.Add(gameObject2);
					gameObject2.gameObject.SetActive(false);
				}
				if (enable)
				{
					List<Sprite> battleDice_NumberAutoSlice = UISpriteDataManager.instance.BattleDice_NumberAutoSlice;
					List<Sprite> battleDice_numberAutoSliceBg = UISpriteDataManager.instance.BattleDice_numberAutoSliceBg;
					for (int k = 0; k < num; k++)
					{
						int index = diceValue % 10;
						Sprite sprite = battleDice_NumberAutoSlice[index];
						Image component = list[list.Count - k - 1].GetComponent<Image>();
						component.sprite = sprite;
						component.SetNativeSize();
						component.gameObject.SetActive(true);
						Sprite sprite2 = battleDice_numberAutoSliceBg[index];
						Image component2 = list2[list.Count - k - 1].GetComponent<Image>();
						component2.sprite = sprite2;
						component2.SetNativeSize();
						component2.gameObject.SetActive(true);
						component2.rectTransform.anchoredPosition = component.rectTransform.anchoredPosition;
						diceValue /= 10;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SetDiceValueerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00018C8C File Offset: 0x00016E8C
		[HarmonyPatch(typeof(VersionViewer), "Start")]
		[HarmonyPrefix]
		private static void VersionViewer_Start_Pre(VersionViewer __instance)
		{
			__instance.GetComponent<Text>().fontSize = 30;
			__instance.gameObject.transform.localPosition = new Vector3(-830f, -460f);
		}

		// Token: 0x0600022D RID: 557 RVA: 0x00018CBC File Offset: 0x00016EBC
		[HarmonyPatch(typeof(UIInvitationDropBookList), "ApplyFilterAll")]
		[HarmonyPostfix]
		private static void UIInvitationDropBookList_ApplyFilterAll_Post(UIInvitationDropBookList __instance)
		{
			try
			{
				LorId[] collection = __instance._currentBookIdList.OrderBy(delegate(LorId x)
				{
					if (!string.IsNullOrWhiteSpace(x.packageId) && !x.packageId.EndsWith("@origin"))
					{
						return x.packageId;
					}
					return "";
				}).ToArray<LorId>();
				__instance._currentBookIdList.Clear();
				__instance._currentBookIdList.AddRange(collection);
				__instance.SelectablePanel.ChildSelectable = __instance.BookSlotList[0].selectable;
				__instance.UpdateBookListPage(false);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/ModBookSort_Invi.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00018D74 File Offset: 0x00016F74
		[HarmonyPatch(typeof(UIInvenFeedBookList), "ApplyFilterAll")]
		[HarmonyPostfix]
		private static void UIInvenFeedBookList_ApplyFilterAll_Post(UIInvenFeedBookList __instance)
		{
			try
			{
				LorId[] collection = __instance._currentBookIdList.OrderBy(delegate(LorId x)
				{
					if (!string.IsNullOrWhiteSpace(x.packageId) && !x.packageId.EndsWith("@origin"))
					{
						return x.packageId;
					}
					return "";
				}).ToArray<LorId>();
				__instance._currentBookIdList.Clear();
				__instance._currentBookIdList.AddRange(collection);
				__instance.UpdateBookListPage(false);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/ModBookSort_Feed.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600022F RID: 559 RVA: 0x00018E10 File Offset: 0x00017010
		[HarmonyPatch(typeof(UIInvenCardListScroll), "ApplyFilterAll")]
		[HarmonyPrefix]
		private static bool UIInvenCardListScroll_ApplyFilterAll_Pre(UIInvenCardListScroll __instance)
		{
			try
			{
				__instance._currentCardListForFilter.Clear();
				List<DiceCardItemModel> cardsByDetailFilterUI = __instance.GetCardsByDetailFilterUI(__instance.GetCardBySearchFilterUI(__instance.GetCardsByCostFilterUI(__instance.GetCardsByGradeFilterUI(__instance._originCardList))));
				cardsByDetailFilterUI.Sort(new Comparison<DiceCardItemModel>(Harmony_Patch.ModCardItemSort));
				if (__instance._unitdata != null)
				{
					Predicate<DiceCardItemModel> cond1 = (DiceCardItemModel x) => true;
					switch (__instance._unitdata.bookItem.ClassInfo.RangeType)
					{
					case 0:
						cond1 = ((DiceCardItemModel x) => x.GetSpec().Ranged != 1);
						break;
					case 1:
						cond1 = ((DiceCardItemModel x) => x.GetSpec().Ranged > 0);
						break;
					case 2:
						cond1 = ((DiceCardItemModel x) => true);
						break;
					}
					List<DiceCardXmlInfo> onlyCards = __instance._unitdata.bookItem.GetOnlyCards();
					Predicate<DiceCardItemModel> cond2 = (DiceCardItemModel x) => onlyCards.Exists((DiceCardXmlInfo y) => y.id == x.GetID());
					__instance._currentCardListForFilter.AddRange(cardsByDetailFilterUI.FindAll(delegate(DiceCardItemModel x)
					{
						if (!x.ClassInfo.optionList.Contains(1))
						{
							return cond1(x);
						}
						return cond2(x);
					}));
					__instance._currentCardListForFilter.AddRange(cardsByDetailFilterUI.FindAll((DiceCardItemModel x) => x.ClassInfo.optionList.Contains(1) && !cond1(x)));
				}
				else
				{
					__instance._currentCardListForFilter.AddRange(cardsByDetailFilterUI);
				}
				int maxRow = __instance.GetMaxRow();
				__instance.scrollBar.SetScrollRectSize((float)__instance.column * __instance.slotWidth, ((float)maxRow + (float)__instance.row - 1f) * __instance.slotHeight);
				__instance.scrollBar.SetWindowPosition(0f, 0f);
				__instance.selectablePanel.ChildSelectable = __instance.slotList[0].selectable;
				__instance.SetCardsData(__instance.GetCurrentPageList());
				return false;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/ModCardSort.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00019068 File Offset: 0x00017268
		private static int ModCardItemSort(DiceCardItemModel a, DiceCardItemModel b)
		{
			int num = (b.ClassInfo.optionList.Contains(1) > false) ? 1 : 0;
			int num2 = (a.ClassInfo.optionList.Contains(1) > false) ? 1 : 0;
			int num3 = (b.ClassInfo.isError ? -1 : num) - (a.ClassInfo.isError ? -1 : num2);
			int result;
			if (num3 != 0)
			{
				result = num3;
			}
			else
			{
				num3 = a.GetSpec().Cost - b.GetSpec().Cost;
				if (num3 != 0)
				{
					result = num3;
				}
				else
				{
					num3 = a.ClassInfo.workshopID.CompareTo(b.ClassInfo.workshopID);
					result = ((num3 != 0) ? num3 : (a.GetID().id - b.GetID().id));
				}
			}
			return result;
		}

		// Token: 0x06000231 RID: 561 RVA: 0x00019124 File Offset: 0x00017324
		[HarmonyPatch(typeof(SaveManager), "SavePlayData")]
		[HarmonyPrefix]
		private static void SaveManager_SavePlayData_Pre()
		{
			try
			{
				ModSaveTool.SaveModSaveData();
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SaveFailed.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00019178 File Offset: 0x00017378
		[HarmonyPatch(typeof(CharacterAppearance), "CreateGiftData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> CharacterAppearance_CreateGiftData_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo loadMethod = Harmony_Patch.GenericMethod(typeof(Resources), "Load", new Type[]
			{
				typeof(GameObject)
			});
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Is(OpCodes.Call, loadMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_2, null);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "CharacterAppearance_CreateGiftData_CheckCustomGift", null, null));
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000233 RID: 563 RVA: 0x00019188 File Offset: 0x00017388
		private static MethodInfo GenericMethod(Type type, string name, Type[] generics)
		{
			return AccessTools.FirstMethod(type, (MethodInfo method) => method.Name == name && method.IsGenericMethod).MakeGenericMethod(generics);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x000191BC File Offset: 0x000173BC
		private static GameObject CharacterAppearance_CreateGiftData_CheckCustomGift(GameObject originalGift, string resPath)
		{
			try
			{
				string[] array = resPath.Split(new char[]
				{
					'/'
				});
				string[] array2 = array[array.Length - 1].Split(new char[]
				{
					'_'
				});
				if (array2.Length < 3 || array2[1].ToLower() != "custom")
				{
					return originalGift;
				}
				GiftAppearance customGiftAppearancePrefabObject = Harmony_Patch.CustomGiftAppearancePrefabObject;
				CustomGiftAppearance.SetGiftArtWork(customGiftAppearancePrefabObject, array2[2]);
				return customGiftAppearancePrefabObject.gameObject;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CACGDerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return originalGift;
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000235 RID: 565 RVA: 0x00019268 File Offset: 0x00017468
		internal static GiftAppearance CustomGiftAppearancePrefabObject
		{
			get
			{
				if (Harmony_Patch._giftAppearance == null)
				{
					Harmony_Patch._giftAppearance = Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Gifts/Gifts_NeedRename/Gift_Challenger"), XLRoot.persistentRoot.transform).GetComponent<GiftAppearance>();
					Harmony_Patch._giftAppearance._frontSpriteRenderer.gameObject.transform.localScale = Vector2.one;
					Harmony_Patch._giftAppearance._sideSpriteRenderer.gameObject.transform.localScale = Vector2.one;
					Harmony_Patch._giftAppearance._frontBackSpriteRenderer.gameObject.transform.localScale = Vector2.one;
					Harmony_Patch._giftAppearance._sideBackSpriteRenderer.gameObject.transform.localScale = Vector2.one;
				}
				return Harmony_Patch._giftAppearance;
			}
		}

		// Token: 0x06000236 RID: 566 RVA: 0x00019339 File Offset: 0x00017539
		[HarmonyPatch(typeof(GiftModel), "CreateScripts")]
		[HarmonyPriority(200)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> GiftModel_CreateScripts_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo getTypeMethod = AccessTools.Method(typeof(Type), "GetType", new Type[]
			{
				typeof(string)
			}, null);
			MethodInfo hideMethod = AccessTools.Method(typeof(PassiveAbilityBase), "Hide", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Is(OpCodes.Callvirt, hideMethod))
				{
					yield return new CodeInstruction(OpCodes.Pop, null);
				}
				else
				{
					yield return instruction;
					if (instruction.Is(OpCodes.Call, getTypeMethod))
					{
						yield return new CodeInstruction(OpCodes.Ldloc_2, null);
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "GiftModel_CreateScripts_CheckCustomGiftAbility", null, null));
					}
					instruction = null;
				}
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x0001934C File Offset: 0x0001754C
		[HarmonyPatch(typeof(GiftModel), "CreateScripts")]
		[HarmonyPostfix]
		private static void GiftModel_CreateScripts_Post(GiftModel __instance, List<PassiveAbilityBase> __result)
		{
			GiftXmlInfo_V2 giftXmlInfo_V = __instance.ClassInfo as GiftXmlInfo_V2;
			if (giftXmlInfo_V != null)
			{
				for (int i = 0; i < giftXmlInfo_V.CustomScriptList.Count; i++)
				{
					try
					{
						Type type = Harmony_Patch.FindGiftPassiveAbilityType(giftXmlInfo_V.CustomScriptList[i]);
						PassiveAbilityBase passiveAbilityBase = null;
						if (type != null)
						{
							try
							{
								passiveAbilityBase = (PassiveAbilityBase)Activator.CreateInstance(type);
							}
							catch
							{
							}
						}
						if (passiveAbilityBase == null)
						{
							passiveAbilityBase = new PassiveAbilityBase();
						}
						passiveAbilityBase.name = __instance.GetName();
						passiveAbilityBase.desc = __instance.GiftDesc;
						__result.Add(passiveAbilityBase);
					}
					catch (Exception)
					{
					}
				}
			}
		}

		// Token: 0x06000238 RID: 568 RVA: 0x000193F8 File Offset: 0x000175F8
		private static Type GiftModel_CreateScripts_CheckCustomGiftAbility(Type oldType, int num)
		{
			return Harmony_Patch.FindGiftPassiveAbilityType(num.ToString().Trim()) ?? oldType;
		}

		// Token: 0x06000239 RID: 569 RVA: 0x00019410 File Offset: 0x00017610
		public static PassiveAbilityBase FindGiftPassiveAbility(string name)
		{
			PassiveAbilityBase result;
			try
			{
				result = (Activator.CreateInstance(Harmony_Patch.FindGiftPassiveAbilityType(name)) as PassiveAbilityBase);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x0600023A RID: 570 RVA: 0x00019448 File Offset: 0x00017648
		public static Type FindGiftPassiveAbilityType(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = null;
			}
			else
			{
				name = name.Trim();
				Type result;
				if (Harmony_Patch.CustomGiftPassive.TryGetValue(name, out result))
				{
					return result;
				}
			}
			Type result2 = null;
			if (!Harmony_Patch.CoreGiftPassivesLoaded)
			{
				Type typeFromHandle = typeof(PassiveAbilityBase);
				foreach (Type type in Assembly.Load("Assembly-CSharp").GetTypes())
				{
					if (typeFromHandle.IsAssignableFrom(type) && type.Name.StartsWith("GiftPassiveAbility_"))
					{
						string text = type.Name.Substring("GiftPassiveAbility_".Length);
						if (!Harmony_Patch.CustomGiftPassive.ContainsKey(text))
						{
							Harmony_Patch.CustomGiftPassive[text] = type;
							if (text == name)
							{
								result2 = type;
							}
						}
					}
				}
				Harmony_Patch.CoreGiftPassivesLoaded = true;
			}
			return result2;
		}

		// Token: 0x0600023B RID: 571 RVA: 0x0001951F File Offset: 0x0001771F
		[HarmonyPatch(typeof(BattleUnitPassiveDetail), "Init")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> BattleUnitPassiveDetail_Init_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo addRangeMethod = AccessTools.Method(typeof(List<PassiveAbilityBase>), "AddRange", null, null);
			MethodInfo firstRangeMethod = AccessTools.Method(typeof(Harmony_Patch), "BattleUnitPassiveDetail_Init_InsertFirst", null, null);
			MethodInfo bookPassivesMethod = AccessTools.Method(typeof(BookModel), "CreatePassiveList", null, null);
			bool book = false;
			foreach (CodeInstruction instruction in instructions)
			{
				if (book && instruction.Is(OpCodes.Callvirt, addRangeMethod))
				{
					yield return new CodeInstruction(OpCodes.Callvirt, firstRangeMethod);
				}
				else
				{
					yield return instruction;
					if (instruction.Is(OpCodes.Callvirt, bookPassivesMethod))
					{
						book = true;
					}
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0001952F File Offset: 0x0001772F
		private static void BattleUnitPassiveDetail_Init_InsertFirst(List<PassiveAbilityBase> passiveList, List<PassiveAbilityBase> bookPassiveList)
		{
			passiveList.InsertRange(0, bookPassiveList);
		}

		// Token: 0x0600023D RID: 573 RVA: 0x00019539 File Offset: 0x00017739
		[HarmonyPatch(typeof(UIGiftInvenSlot), "SetData")]
		[HarmonyPatch(typeof(UIGiftDataSlot), "SetData")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIGiftSlot_SetData_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo loadGiftMethod = Harmony_Patch.GenericMethod(typeof(Resources), "Load", new Type[]
			{
				typeof(GiftAppearance)
			});
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Is(OpCodes.Call, loadGiftMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_1, null);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIGiftSlot_CheckCustomGift", null, null));
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600023E RID: 574 RVA: 0x0001954C File Offset: 0x0001774C
		private static GiftAppearance UIGiftSlot_CheckCustomGift(GiftAppearance originalAppearance, GiftModel data)
		{
			try
			{
				if (data != null)
				{
					string[] array = data.GetResourcePath().Split(new char[]
					{
						'/'
					});
					string[] array2 = array[array.Length - 1].Split(new char[]
					{
						'_'
					});
					if (array2.Length < 3 || array2[1].ToLower() != "custom")
					{
						return originalAppearance;
					}
					GiftAppearance customGiftAppearancePrefabObject = Harmony_Patch.CustomGiftAppearancePrefabObject;
					CustomGiftAppearance.SetGiftArtWork(customGiftAppearancePrefabObject, array2[2]);
					if (customGiftAppearancePrefabObject._frontSpriteRenderer != null && customGiftAppearancePrefabObject._frontSpriteRenderer.sprite == null)
					{
						customGiftAppearancePrefabObject._frontSpriteRenderer.sprite = customGiftAppearancePrefabObject._frontBackSpriteRenderer.sprite;
					}
					return customGiftAppearancePrefabObject;
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/GiftSetDataerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return originalAppearance;
		}

		// Token: 0x0600023F RID: 575 RVA: 0x00019638 File Offset: 0x00017838
		[HarmonyPatch(typeof(UIGiftPreviewSlot), "UpdateSlot")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIGiftPreviewSlot_UpdateSlot_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo loadGiftMethod = Harmony_Patch.GenericMethod(typeof(Resources), "Load", new Type[]
			{
				typeof(GiftAppearance)
			});
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Is(OpCodes.Call, loadGiftMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIGiftPreviewSlot), "Gift"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Patch), "UIGiftSlot_CheckCustomGift", null, null));
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000240 RID: 576 RVA: 0x00019648 File Offset: 0x00017848
		[HarmonyPatch(typeof(GiftInventory), "GetAllGiftsListForTitle")]
		[HarmonyPostfix]
		private static void GiftInventory_GetAllGiftsListForTitle_Post(List<GiftModel> __result)
		{
			try
			{
				List<GiftModel> sephirahGifts = null;
				List<GiftModel> librarianGifts = null;
				List<GiftModel> guestGifts = null;
				__result.RemoveAll(delegate(GiftModel x)
				{
					GiftXmlInfo_V2 giftXmlInfo_V = x.ClassInfo as GiftXmlInfo_V2;
					if (giftXmlInfo_V != null && giftXmlInfo_V.priority != GiftPriorityOrder.Creature)
					{
						switch (giftXmlInfo_V.priority)
						{
						case GiftPriorityOrder.Sephirah:
							(sephirahGifts = (sephirahGifts ?? new List<GiftModel>())).Add(x);
							break;
						case GiftPriorityOrder.Librarian:
							(librarianGifts = (librarianGifts ?? new List<GiftModel>())).Add(x);
							break;
						case GiftPriorityOrder.Guest:
							(guestGifts = (guestGifts ?? new List<GiftModel>())).Add(x);
							break;
						}
						return true;
					}
					return false;
				});
				for (int i = 0; i < __result.Count; i++)
				{
					int giftClassInfoId = __result[i].GetGiftClassInfoId();
					if (giftClassInfoId != 6)
					{
						if (giftClassInfoId != 12)
						{
							if (giftClassInfoId == 140)
							{
								if (guestGifts != null)
								{
									__result.InsertRange(i, guestGifts);
									i += guestGifts.Count;
									guestGifts = null;
								}
							}
						}
						else if (librarianGifts != null)
						{
							__result.InsertRange(i, librarianGifts);
							i += librarianGifts.Count;
							librarianGifts = null;
						}
					}
					else if (sephirahGifts != null)
					{
						__result.InsertRange(i, sephirahGifts);
						i += sephirahGifts.Count;
						sephirahGifts = null;
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/GiftListForTitleSorterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000241 RID: 577 RVA: 0x00019780 File Offset: 0x00017980
		[HarmonyPatch(typeof(UIGiftInventory), "SetGiftData", new Type[]
		{
			typeof(GiftPosition)
		})]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> UIGiftInventory_SetGiftData_In(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo sortMethod = AccessTools.Method(typeof(List<GiftModel>), "Sort", new Type[]
			{
				typeof(Comparison<GiftModel>)
			}, null);
			MethodInfo helperMain = AccessTools.Method(typeof(Harmony_Patch), "UIGiftInventory_SetGiftData_Helper", null, null);
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Calls(sortMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0, null);
					yield return new CodeInstruction(OpCodes.Call, helperMain);
				}
				instruction = null;
			}
			IEnumerator<CodeInstruction> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000242 RID: 578 RVA: 0x00019790 File Offset: 0x00017990
		private static void UIGiftInventory_SetGiftData_Helper(UIGiftInventory inventory)
		{
			try
			{
				List<GiftModel> sephirahGifts = null;
				List<GiftModel> librarianGifts = null;
				List<GiftModel> guestGifts = null;
				inventory.giftListData.RemoveAll(delegate(GiftModel x)
				{
					GiftXmlInfo_V2 giftXmlInfo_V = x.ClassInfo as GiftXmlInfo_V2;
					if (giftXmlInfo_V != null && giftXmlInfo_V.priority != GiftPriorityOrder.Creature)
					{
						switch (giftXmlInfo_V.priority)
						{
						case GiftPriorityOrder.Sephirah:
							(sephirahGifts = (sephirahGifts ?? new List<GiftModel>())).Add(x);
							break;
						case GiftPriorityOrder.Librarian:
							(librarianGifts = (librarianGifts ?? new List<GiftModel>())).Add(x);
							break;
						case GiftPriorityOrder.Guest:
							(guestGifts = (guestGifts ?? new List<GiftModel>())).Add(x);
							break;
						}
						return true;
					}
					return false;
				});
				for (int i = 0; i < inventory.giftListData.Count; i++)
				{
					int giftClassInfoId = inventory.giftListData[i].GetGiftClassInfoId();
					if (giftClassInfoId != 6)
					{
						if (giftClassInfoId != 12)
						{
							if (giftClassInfoId == 140)
							{
								if (guestGifts != null)
								{
									inventory.giftListData.InsertRange(i, guestGifts);
									i += guestGifts.Count;
									guestGifts = null;
								}
							}
						}
						else if (librarianGifts != null)
						{
							inventory.giftListData.InsertRange(i, librarianGifts);
							i += librarianGifts.Count;
							librarianGifts = null;
						}
					}
					else if (sephirahGifts != null)
					{
						inventory.giftListData.InsertRange(i, sephirahGifts);
						i += sephirahGifts.Count;
						sephirahGifts = null;
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/GiftInventorySorterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000243 RID: 579 RVA: 0x000198F4 File Offset: 0x00017AF4
		[HarmonyPatch(typeof(GiftInventory), "LoadFromSaveData")]
		[HarmonyPostfix]
		private static void GiftInventory_LoadFromSaveData_Post(GiftInventory __instance)
		{
			UnitDataModel owner = __instance._owner;
			if (owner == null)
			{
				return;
			}
			LibraryFloorModel floor = LibraryModel.Instance.GetFloor(owner.OwnerSephirah);
			if (floor == null)
			{
				return;
			}
			int num = floor._unitDataList.FindIndex((UnitDataModel unit) => unit == owner);
			if (num < 0)
			{
				return;
			}
			SaveData stageStorageData = LibraryModel.Instance.CustomStorage.GetStageStorageData("BasemodGift");
			if (stageStorageData == null)
			{
				return;
			}
			SaveData data = stageStorageData.GetData(owner.OwnerSephirah.ToString());
			if (data == null)
			{
				return;
			}
			SaveData data2 = data.GetData(num.ToString());
			if (data2 == null)
			{
				return;
			}
			SaveData data3 = data2.GetData("equipList");
			SaveData data4 = data2.GetData("unequipList");
			SaveData data5 = data2.GetData("offList");
			if (data3 != null)
			{
				foreach (SaveData saveData in data3)
				{
					LorId key = LorId.LoadFromSaveData(saveData);
					GiftXmlInfo giftXmlInfo;
					if (OrcTools.CustomGifts.TryGetValue(key, out giftXmlInfo))
					{
						GiftModel giftModel = new GiftModel(giftXmlInfo);
						__instance.AddGift(giftModel);
						__instance.Equip(giftModel);
					}
				}
			}
			if (data4 != null)
			{
				foreach (SaveData saveData2 in data4)
				{
					LorId key2 = LorId.LoadFromSaveData(saveData2);
					GiftXmlInfo giftXmlInfo2;
					if (OrcTools.CustomGifts.TryGetValue(key2, out giftXmlInfo2))
					{
						GiftModel giftModel2 = new GiftModel(giftXmlInfo2);
						__instance.AddGift(giftModel2);
					}
				}
			}
			if (data5 != null)
			{
				foreach (SaveData saveData3 in data5)
				{
					LorId key3 = LorId.LoadFromSaveData(saveData3);
					GiftXmlInfo gift;
					if (OrcTools.CustomGifts.TryGetValue(key3, out gift))
					{
						GiftModel giftModel3 = __instance._equippedList.Find((GiftModel model) => model.ClassInfo == gift);
						if (giftModel3 != null)
						{
							giftModel3.isShowEquipGift = false;
						}
					}
				}
			}
		}

		// Token: 0x06000244 RID: 580 RVA: 0x00019B28 File Offset: 0x00017D28
		[HarmonyPatch(typeof(CustomSaveStorageModel), "GetSaveData")]
		[HarmonyPrefix]
		private static void CustomSaveStorageModel_GetSaveData_Pre(CustomSaveStorageModel __instance)
		{
			__instance._storage.Remove("BasemodGift");
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00019B3C File Offset: 0x00017D3C
		[HarmonyPatch(typeof(BattleCardBehaviourResult), "GetAbilityDataAfterRoll")]
		[HarmonyPostfix]
		private static List<EffectTypoData> BattleCardBehaviourResult_GetAbilityDataAfterRoll_Post(List<EffectTypoData> list, BattleCardBehaviourResult __instance)
		{
			try
			{
				List<EffectTypoData> collection;
				if (Harmony_Patch.CustomEffectTypoData.TryGetValue(__instance, out collection))
				{
					Harmony_Patch.CustomEffectTypoData.Remove(__instance);
					list.AddRange(collection);
				}
			}
			catch
			{
			}
			return list;
		}

		// Token: 0x06000246 RID: 582 RVA: 0x00019B84 File Offset: 0x00017D84
		[HarmonyPatch(typeof(BattleActionTypoSlot), "SetData")]
		[HarmonyPostfix]
		private static void BattleActionTypoSlot_SetData_Post(BattleActionTypoSlot __instance, EffectTypoData data)
		{
			try
			{
				EffectTypoData_New effectTypoData_New = data as EffectTypoData_New;
				if (effectTypoData_New != null)
				{
					if (effectTypoData_New.battleUIPassiveSet != null)
					{
						EffectTypoData_New.BattleUIPassiveSet battleUIPassiveSet = effectTypoData_New.battleUIPassiveSet;
						BattleUIPassiveSet battleUIPassiveSet2 = default(BattleUIPassiveSet);
						battleUIPassiveSet2.type = effectTypoData_New.type;
						battleUIPassiveSet2.frame = battleUIPassiveSet.frame;
						battleUIPassiveSet2.Icon = battleUIPassiveSet.Icon;
						battleUIPassiveSet2.IconGlow = battleUIPassiveSet.IconGlow;
						battleUIPassiveSet2.textColor = battleUIPassiveSet.textColor;
						battleUIPassiveSet2.IconColor = battleUIPassiveSet.IconColor;
						battleUIPassiveSet2.IconGlowColor = battleUIPassiveSet.IconGlowColor;
						BattleUIPassiveSet battleUIPassiveSet3 = battleUIPassiveSet2;
						UISpriteDataManager.instance.BattleUIEffectSetDic[battleUIPassiveSet3.type] = battleUIPassiveSet3;
					}
					BattleUIPassiveSet battleUIPassiveSet4;
					if (UISpriteDataManager.instance.BattleUIEffectSetDic.TryGetValue(effectTypoData_New.type, out battleUIPassiveSet4))
					{
						__instance.img_Icon.sprite = battleUIPassiveSet4.Icon;
						__instance.img_Icon.color = battleUIPassiveSet4.IconColor;
						if (battleUIPassiveSet4.IconGlow != null)
						{
							__instance.img_IconGlow.enabled = true;
							__instance.img_IconGlow.sprite = battleUIPassiveSet4.IconGlow;
							__instance.img_IconGlow.color = battleUIPassiveSet4.IconGlowColor;
						}
						else
						{
							__instance.img_IconGlow.enabled = false;
						}
						__instance.img_Frame.sprite = battleUIPassiveSet4.frame;
						__instance.txt_desc.color = battleUIPassiveSet4.textColor;
						__instance.txt_title.color = battleUIPassiveSet4.textColor;
						Color underlayColor = SingletonBehavior<DirectingDataSetter>.Instance.OnGrayScale ? (battleUIPassiveSet4.textColor * battleUIPassiveSet4.textColor.grayscale) : (battleUIPassiveSet4.textColor * SingletonBehavior<DirectingDataSetter>.Instance.graycolor);
						__instance.msetter_title.underlayColor = underlayColor;
						Vector2 sizeDelta = __instance.img_Frame.rectTransform.sizeDelta;
						sizeDelta.y = ((data.Title != "") ? __instance.TitleFrameHeight : __instance.defaultFrameHeight);
						__instance.img_Frame.rectTransform.sizeDelta = sizeDelta;
					}
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/CustomEffectUISeterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000247 RID: 583 RVA: 0x00019DCC File Offset: 0x00017FCC
		private static string GetCardName(LorId cardID)
		{
			return Singleton<BattleCardDescXmlList>.Instance.GetCardName(cardID);
		}

		// Token: 0x06000248 RID: 584 RVA: 0x00019DDC File Offset: 0x00017FDC
		private static void GetArtWorks()
		{
			Harmony_Patch.ArtWorks = new Dictionary<string, Sprite>();
			foreach (ModContent modContent in Harmony_Patch.LoadedModContents)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				if (Directory.Exists(dirInfo.FullName + "/ArtWork"))
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(dirInfo.FullName + "/ArtWork");
					if (directoryInfo.GetDirectories().Length != 0)
					{
						DirectoryInfo[] directories = directoryInfo.GetDirectories();
						for (int i = 0; i < directories.Length; i++)
						{
							Harmony_Patch.GetArtWorks(directories[i]);
						}
					}
					foreach (FileInfo fileInfo in directoryInfo.GetFiles())
					{
						Texture2D texture2D = new Texture2D(2, 2);
						ImageConversion.LoadImage(texture2D, File.ReadAllBytes(fileInfo.FullName));
						Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
						Harmony_Patch.ArtWorks[fileNameWithoutExtension] = value;
					}
				}
			}
		}

		// Token: 0x06000249 RID: 585 RVA: 0x00019F38 File Offset: 0x00018138
		private static void GetArtWorks(DirectoryInfo dir)
		{
			if (dir.GetDirectories().Length != 0)
			{
				DirectoryInfo[] directories = dir.GetDirectories();
				for (int i = 0; i < directories.Length; i++)
				{
					Harmony_Patch.GetArtWorks(directories[i]);
				}
			}
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				Texture2D texture2D = new Texture2D(2, 2);
				ImageConversion.LoadImage(texture2D, File.ReadAllBytes(fileInfo.FullName));
				Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
				Harmony_Patch.ArtWorks[fileNameWithoutExtension] = value;
			}
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00019FF8 File Offset: 0x000181F8
		public static string BuildPath(params string[] paths)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Xml/");
			foreach (string value in paths)
			{
				stringBuilder.Append(value);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0001A108 File Offset: 0x00018308
		[CompilerGenerated]
		internal static void <CopyCharacterMotion>g__TryAddMotionSpriteSet|24_0(string name, CharacterAppearanceType type, ref Harmony_Patch.<>c__DisplayClass24_0 A_2)
		{
			Transform transform = A_2.characterMotion.transform.Find(name);
			if (transform != null)
			{
				SpriteRenderer component = transform.GetComponent<SpriteRenderer>();
				if (component != null)
				{
					A_2.characterMotion.motionSpriteSet.Add(new SpriteSet(component, type));
				}
			}
		}

		// Token: 0x04000126 RID: 294
		private const string QUEST_TYPE_NAME_PREFIX = "QuestMissionScript_";

		// Token: 0x04000127 RID: 295
		private static string path = string.Empty;

		// Token: 0x04000128 RID: 296
		private static string Staticpath;

		// Token: 0x04000129 RID: 297
		private static string StoryStaticpath;

		// Token: 0x0400012A RID: 298
		private static string Storylocalizepath;

		// Token: 0x0400012B RID: 299
		private static string Localizepath;

		// Token: 0x0400012C RID: 300
		private static List<Assembly> AssemList;

		// Token: 0x0400012D RID: 301
		private static List<string> LoadedAssembly;

		// Token: 0x0400012E RID: 302
		public static Dictionary<string, Type> CustomEffects = new Dictionary<string, Type>();

		// Token: 0x0400012F RID: 303
		public static Dictionary<string, Type> CustomMapManager = new Dictionary<string, Type>();

		// Token: 0x04000130 RID: 304
		public static Dictionary<string, Type> CustomBattleDialogModel = new Dictionary<string, Type>();

		// Token: 0x04000131 RID: 305
		public static Dictionary<string, Type> CustomQuest = new Dictionary<string, Type>();

		// Token: 0x04000132 RID: 306
		private static bool CoreDialogsLoaded = false;

		// Token: 0x04000133 RID: 307
		public static Dictionary<string, Type> CustomGiftPassive = new Dictionary<string, Type>();

		// Token: 0x04000134 RID: 308
		public static Dictionary<string, Type> CustomEmotionCardAbility = new Dictionary<string, Type>();

		// Token: 0x04000135 RID: 309
		[Obsolete("Core thumbs are now handled by UnitRenderUtil", true)]
		public static Dictionary<string, int> CoreThumbDic = new Dictionary<string, int>();

		// Token: 0x04000136 RID: 310
		public static Dictionary<BattleCardBehaviourResult, List<EffectTypoData>> CustomEffectTypoData = new Dictionary<BattleCardBehaviourResult, List<EffectTypoData>>();

		// Token: 0x04000137 RID: 311
		public static Dictionary<string, Sprite> ArtWorks = null;

		// Token: 0x04000138 RID: 312
		public static Dictionary<LorId, Sprite> BookThumb;

		// Token: 0x04000139 RID: 313
		public static Dictionary<string, AudioClip> AudioClips = null;

		// Token: 0x0400013A RID: 314
		public static bool IsModStorySelected;

		// Token: 0x0400013B RID: 315
		public static Dictionary<LorId, UIStoryLine> ModEpMatch = new Dictionary<LorId, UIStoryLine>();

		// Token: 0x0400013C RID: 316
		private static readonly int ModEpMin = 200;

		// Token: 0x0400013D RID: 317
		private static readonly HashSet<string> CheckedCustomSprites = new HashSet<string>();

		// Token: 0x0400013E RID: 318
		public static Dictionary<LorId, Harmony_Patch.ModStroyCG> ModStoryCG = null;

		// Token: 0x0400013F RID: 319
		private static readonly HashSet<LorId> CheckedModStoryCG = new HashSet<LorId>();

		// Token: 0x04000140 RID: 320
		public static Dictionary<Assembly, string> ModWorkShopId;

		// Token: 0x04000141 RID: 321
		[TupleElementNames(new string[]
		{
			"pid",
			"filename",
			"initializer"
		})]
		private static readonly List<ValueTuple<string, string, ModInitializer>> allInitializers = new List<ValueTuple<string, string, ModInitializer>>();

		// Token: 0x04000142 RID: 322
		private static bool CoreEmotionCardsLoaded = false;

		// Token: 0x04000143 RID: 323
		private static bool CoreQuestsLoaded = false;

		// Token: 0x04000144 RID: 324
		private static GiftAppearance _giftAppearance;

		// Token: 0x04000145 RID: 325
		private static bool CoreGiftPassivesLoaded = false;

		// Token: 0x04000146 RID: 326
		public static bool IsBasemodDebugMode = true;

		// Token: 0x02000087 RID: 135
		public class ModStroyCG
		{
			// Token: 0x040001FA RID: 506
			public string path;

			// Token: 0x040001FB RID: 507
			public Sprite sprite;
		}
	}
}
