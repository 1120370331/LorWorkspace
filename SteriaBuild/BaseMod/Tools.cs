using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using EnumExtenderV2;
using GTMDProjectMoon;
using LorIdExtensions;
using LOR_DiceSystem;
using MyJsonTool;
using NAudio.Wave;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Workshop;

namespace BaseMod
{
	// Token: 0x02000067 RID: 103
	public static class Tools
	{
		// Token: 0x06000292 RID: 658 RVA: 0x0001B2A8 File Offset: 0x000194A8
		public static LorId MakeLorId(int id)
		{
			return new LorId(Tools.FindModId(Assembly.GetCallingAssembly()), id);
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0001B2BA File Offset: 0x000194BA
		public static LorName MakeLorName(string name)
		{
			return new LorName(Tools.FindModId(Assembly.GetCallingAssembly()), name);
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0001B2CC File Offset: 0x000194CC
		private static string FindModId(Assembly callingAssembly)
		{
			string text;
			if (!Harmony_Patch.ModWorkShopId.TryGetValue(callingAssembly, out text))
			{
				string directoryName = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(callingAssembly.CodeBase).Path));
				DirectoryInfo directoryInfo = new DirectoryInfo(directoryName);
				if (File.Exists(Path.Combine(directoryName, "StageModInfo.xml")))
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(directoryName + "/StageModInfo.xml")))
					{
						text = ((NormalInvitation)new XmlSerializer(typeof(NormalInvitation)).Deserialize(stringReader)).workshopInfo.uniqueId;
						goto IL_107;
					}
				}
				if (File.Exists(Path.Combine(directoryInfo.Parent.FullName, "StageModInfo.xml")))
				{
					using (StringReader stringReader2 = new StringReader(File.ReadAllText(directoryInfo.Parent.FullName + "/StageModInfo.xml")))
					{
						text = ((NormalInvitation)new XmlSerializer(typeof(NormalInvitation)).Deserialize(stringReader2)).workshopInfo.uniqueId;
						goto IL_107;
					}
				}
				text = "";
				IL_107:
				if (text.ToLower().EndsWith("@origin"))
				{
					text = "";
				}
				Harmony_Patch.ModWorkShopId[callingAssembly] = text;
			}
			return text;
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0001B424 File Offset: 0x00019624
		public static void ExhaustCardAnyWhere(this BattleAllyCardDetail cardDetail, BattleDiceCardModel card)
		{
			cardDetail._cardInReserved.Remove(card);
			cardDetail._cardInUse.Remove(card);
			cardDetail._cardInHand.Remove(card);
			cardDetail._cardInDiscarded.Remove(card);
			cardDetail._cardInDeck.Remove(card);
		}

		// Token: 0x06000296 RID: 662 RVA: 0x0001B474 File Offset: 0x00019674
		public static BattleDiceCardModel DrawCardSpecified(this BattleAllyCardDetail cardDetail, Predicate<BattleDiceCardModel> match)
		{
			if (cardDetail.GetHand().Count < cardDetail._maxDrawHand)
			{
				try
				{
					List<BattleDiceCardModel> cardInDeck = cardDetail._cardInDeck;
					List<BattleDiceCardModel> cardInDiscarded = cardDetail._cardInDiscarded;
					cardInDeck.AddRange(cardInDiscarded);
					cardInDiscarded.Clear();
					BattleDiceCardModel battleDiceCardModel = cardInDeck.Find(match);
					if (battleDiceCardModel != null)
					{
						cardDetail.AddCardToHand(battleDiceCardModel, false);
						cardInDeck.Remove(battleDiceCardModel);
						return battleDiceCardModel;
					}
				}
				catch (Exception ex)
				{
					File.WriteAllText(Application.dataPath + "/Mods/DrawCardSpecifiederror.log", ex.Message + Environment.NewLine + ex.StackTrace);
				}
				return null;
			}
			return null;
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0001B51C File Offset: 0x0001971C
		public static void AddCustomIcon(this BattleDiceCardModel cardModel, string resName, int priority = 0)
		{
			Sprite spr;
			if (Harmony_Patch.ArtWorks.TryGetValue(resName, out spr))
			{
				cardModel._iconAdder = resName;
				List<BattleDiceCardModel.CardIcon> addedIcons = cardModel._addedIcons;
				if (!addedIcons.Exists((BattleDiceCardModel.CardIcon x) => x.Icon == spr))
				{
					BattleDiceCardModel.CardIcon item = new BattleDiceCardModel.CardIcon(spr, priority);
					addedIcons.Add(item);
					addedIcons.Sort((BattleDiceCardModel.CardIcon x, BattleDiceCardModel.CardIcon y) => y.Priority - x.Priority);
				}
			}
		}

		// Token: 0x06000298 RID: 664 RVA: 0x0001B59D File Offset: 0x0001979D
		public static T GetScript<T>(this BattleDiceCardModel cardModel) where T : DiceCardSelfAbilityBase
		{
			return cardModel._script as T;
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0001B5AF File Offset: 0x000197AF
		public static bool ContainsCategory(this BookModel book, string category)
		{
			return book._classInfo.ContainsCategory(category);
		}

		// Token: 0x0600029A RID: 666 RVA: 0x0001B5BD File Offset: 0x000197BD
		public static bool ContainsOption(this BookModel book, string option)
		{
			return book._classInfo.ContainsOption(option);
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0001B5CB File Offset: 0x000197CB
		public static bool ContainsCategory(this BookXmlInfo book, string category)
		{
			return book.categoryList.Contains(OrcTools.GetBookCategory(category));
		}

		// Token: 0x0600029C RID: 668 RVA: 0x0001B5DE File Offset: 0x000197DE
		public static bool ContainsOption(this BookXmlInfo book, string option)
		{
			return book.optionList.Contains(OrcTools.GetBookOption(option));
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0001B5F1 File Offset: 0x000197F1
		public static bool ContainsCategory(this BattleDiceCardModel card, string category)
		{
			return card._xmlData.ContainsCategory(category);
		}

		// Token: 0x0600029E RID: 670 RVA: 0x0001B5FF File Offset: 0x000197FF
		public static bool ContainsOption(this BattleDiceCardModel card, string option)
		{
			return card._xmlData.ContainsOption(option);
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0001B60D File Offset: 0x0001980D
		public static bool ContainsCategory(this DiceCardXmlInfo card, string category)
		{
			return card.category == OrcTools.GetBookCategory(category);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0001B61D File Offset: 0x0001981D
		public static bool ContainsOption(this DiceCardXmlInfo card, string option)
		{
			return card.optionList.Contains(OrcTools.GetCardOption(option));
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0001B630 File Offset: 0x00019830
		public static void SetAbilityData(this BattleCardBehaviourResult battleCardBehaviourResult, EffectTypoData effectTypoData)
		{
			if (Harmony_Patch.CustomEffectTypoData == null)
			{
				Harmony_Patch.CustomEffectTypoData = new Dictionary<BattleCardBehaviourResult, List<EffectTypoData>>();
			}
			if (battleCardBehaviourResult == null || effectTypoData == null)
			{
				return;
			}
			List<EffectTypoData> list;
			if (!Harmony_Patch.CustomEffectTypoData.TryGetValue(battleCardBehaviourResult, out list))
			{
				list = (Harmony_Patch.CustomEffectTypoData[battleCardBehaviourResult] = new List<EffectTypoData>());
			}
			list.Add(effectTypoData);
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0001B680 File Offset: 0x00019880
		public static void SetAbilityData(this BattleCardBehaviourResult battleCardBehaviourResult, EffectTypoData_New effectTypoData_New)
		{
			if (Harmony_Patch.CustomEffectTypoData == null)
			{
				Harmony_Patch.CustomEffectTypoData = new Dictionary<BattleCardBehaviourResult, List<EffectTypoData>>();
			}
			if (battleCardBehaviourResult == null || effectTypoData_New == null)
			{
				return;
			}
			List<EffectTypoData> list;
			if (!Harmony_Patch.CustomEffectTypoData.TryGetValue(battleCardBehaviourResult, out list))
			{
				list = (Harmony_Patch.CustomEffectTypoData[battleCardBehaviourResult] = new List<EffectTypoData>());
			}
			list.Add(effectTypoData_New);
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0001B6D0 File Offset: 0x000198D0
		public static void SetAlarmText(string alarmtype, UIAlarmButtonType btnType = 0, ConfirmEvent confirmFunc = null, params object[] args)
		{
			if (UIAlarmPopup.instance.IsOpened())
			{
				UIAlarmPopup.instance.Close();
			}
			UIAlarmPopup.instance.currentAnimState = 0;
			GameObject ob_blue = UIAlarmPopup.instance.ob_blue;
			GameObject ob_normal = UIAlarmPopup.instance.ob_normal;
			GameObject ob_Reward = UIAlarmPopup.instance.ob_Reward;
			GameObject ob_BlackBg = UIAlarmPopup.instance.ob_BlackBg;
			List<GameObject> buttonRoots = UIAlarmPopup.instance.ButtonRoots;
			if (ob_blue.activeSelf)
			{
				ob_blue.gameObject.SetActive(false);
			}
			if (!ob_normal.activeSelf)
			{
				ob_normal.gameObject.SetActive(true);
			}
			if (ob_Reward.activeSelf)
			{
				ob_Reward.SetActive(false);
			}
			if (ob_BlackBg.activeSelf)
			{
				ob_BlackBg.SetActive(false);
			}
			foreach (GameObject gameObject in buttonRoots)
			{
				gameObject.gameObject.SetActive(false);
			}
			UIAlarmPopup.instance.currentAlarmType = 0;
			UIAlarmPopup.instance.buttonNumberType = btnType;
			UIAlarmPopup.instance.currentmode = 0;
			UIAlarmPopup.instance.anim.updateMode = 0;
			TextMeshProUGUI txt_alarm = UIAlarmPopup.instance.txt_alarm;
			if (args == null)
			{
				txt_alarm.text = TextDataModel.GetText(alarmtype, Array.Empty<object>());
			}
			else
			{
				txt_alarm.text = TextDataModel.GetText(alarmtype, args);
			}
			UIAlarmPopup.instance._confirmEvent = confirmFunc;
			buttonRoots[btnType].gameObject.SetActive(true);
			UIAlarmPopup.instance.Open();
			if (btnType == null)
			{
				UIControlManager.Instance.SelectSelectableForcely(UIAlarmPopup.instance.OkButton, false);
				return;
			}
			if (btnType == 1)
			{
				UIControlManager.Instance.SelectSelectableForcely(UIAlarmPopup.instance.yesButton, false);
			}
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x0001B880 File Offset: 0x00019A80
		public static AudioClip GetAudio(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
			return Tools.GetAudio(path, fileNameWithoutExtension);
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x0001B8A8 File Offset: 0x00019AA8
		public static AudioClip GetAudio(string path, string Name = "")
		{
			if (Harmony_Patch.AudioClips == null)
			{
				Harmony_Patch.AudioClips = new Dictionary<string, AudioClip>();
			}
			AudioClip result;
			if (!string.IsNullOrWhiteSpace(Name) && Harmony_Patch.AudioClips.TryGetValue(Name, out result))
			{
				return result;
			}
			if (!File.Exists(path))
			{
				return null;
			}
			string text;
			AudioType audioType;
			if (path.EndsWith(".wav"))
			{
				text = path;
				audioType = 20;
			}
			else if (path.EndsWith(".ogg"))
			{
				text = path;
				audioType = 14;
			}
			else
			{
				if (!path.EndsWith(".mp3"))
				{
					return null;
				}
				text = path.Replace(".mp3", ".wav");
				Mp3FileReader sourceProvider = new Mp3FileReader(path);
				WaveFileWriter.CreateWaveFile(text, sourceProvider);
				audioType = 20;
			}
			UnityWebRequest audioClip = UnityWebRequestMultimedia.GetAudioClip("file://" + text, audioType);
			audioClip.SendWebRequest();
			while (!audioClip.isDone)
			{
			}
			if (audioClip.isHttpError || audioClip.isNetworkError)
			{
				return null;
			}
			AudioClip content = DownloadHandlerAudioClip.GetContent(audioClip);
			if (path.EndsWith(".mp3"))
			{
				File.Delete(text);
			}
			if (!string.IsNullOrWhiteSpace(Name))
			{
				content.name = Name;
				Harmony_Patch.AudioClips[Name] = content;
			}
			return content;
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x0001B9B4 File Offset: 0x00019BB4
		public static void Save<T>(this T value, string key)
		{
			if (string.IsNullOrWhiteSpace(Tools.GetModId(Assembly.GetCallingAssembly())))
			{
				return;
			}
			string path = string.Concat(new string[]
			{
				Application.dataPath,
				"/ModSaves/",
				Tools.GetModId(Assembly.GetCallingAssembly()) + "/",
				key,
				".json"
			});
			Directory.CreateDirectory(Application.dataPath + "/ModSaves/" + Tools.GetModId(Assembly.GetCallingAssembly()));
			File.WriteAllText(path, LitJsonRegiter.ToJson<Tools.Test<T>>(new Tools.Test<T>
			{
				value = value
			}));
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0001BA48 File Offset: 0x00019C48
		public static T Load<T>(string key)
		{
			if (string.IsNullOrWhiteSpace(Tools.GetModId(Assembly.GetCallingAssembly())))
			{
				return default(T);
			}
			string path = string.Concat(new string[]
			{
				Application.dataPath,
				"/ModSaves/",
				Tools.GetModId(Assembly.GetCallingAssembly()) + "/",
				key,
				".json"
			});
			if (!File.Exists(path))
			{
				return default(T);
			}
			Tools.Test<T> test = LitJsonRegiter.ToObject<Tools.Test<T>>(File.ReadAllText(path));
			if (test == null)
			{
				return default(T);
			}
			return test.value;
		}

		// Token: 0x060002A8 RID: 680 RVA: 0x0001BAE0 File Offset: 0x00019CE0
		public static string GetModId(Assembly callingAssembly)
		{
			string value;
			if (!Harmony_Patch.ModWorkShopId.TryGetValue(callingAssembly, out value))
			{
				string directoryName = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(callingAssembly.CodeBase).Path));
				DirectoryInfo directoryInfo = new DirectoryInfo(directoryName);
				if (File.Exists(directoryName + "/StageModInfo.xml"))
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(directoryName + "/StageModInfo.xml")))
					{
						value = ((NormalInvitation)new XmlSerializer(typeof(NormalInvitation)).Deserialize(stringReader)).workshopInfo.uniqueId;
						goto IL_107;
					}
				}
				if (File.Exists(directoryInfo.Parent.FullName + "/StageModInfo.xml"))
				{
					using (StringReader stringReader2 = new StringReader(File.ReadAllText(directoryInfo.Parent.FullName + "/StageModInfo.xml")))
					{
						value = ((NormalInvitation)new XmlSerializer(typeof(NormalInvitation)).Deserialize(stringReader2)).workshopInfo.uniqueId;
						goto IL_107;
					}
				}
				value = "";
				IL_107:
				Harmony_Patch.ModWorkShopId[callingAssembly] = value;
			}
			return Harmony_Patch.ModWorkShopId[callingAssembly];
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x0001BC28 File Offset: 0x00019E28
		public static float ParseFloatSafe(string s)
		{
			float result;
			try
			{
				result = float.Parse(s.Replace(',', '.').Replace('/', '.'), Tools.invariant);
			}
			catch
			{
				Debug.Log("BaseMod: could not parse float (" + s + "), using 0 as fallback");
				result = 0f;
			}
			return result;
		}

		// Token: 0x060002AA RID: 682 RVA: 0x0001BC84 File Offset: 0x00019E84
		public static TEnum MakeEnum<TEnum>(string name) where TEnum : struct, Enum
		{
			name = name.Trim();
			TEnum tenum;
			if (EnumExtender.TryGetValueOf<TEnum>(name, ref tenum))
			{
				return tenum;
			}
			TEnum[] originalValues = EnumExtender.GetOriginalValues<TEnum>();
			if (EnumExtender.TryFindUnnamedValue<TEnum>(new TEnum?((originalValues.Length != 0) ? originalValues[0] : default(TEnum)), null, false, ref tenum) && EnumExtender.TryAddName<TEnum>(name, tenum, false))
			{
				return tenum;
			}
			throw new Exception("Could not find or add enum value");
		}

		// Token: 0x060002AB RID: 683 RVA: 0x0001BCF0 File Offset: 0x00019EF0
		public static string ClarifyWorkshopIdLegacy(string customId, string rootId, string modId)
		{
			if (string.IsNullOrWhiteSpace(customId))
			{
				customId = (rootId ?? "");
			}
			customId = customId.Trim();
			if (customId.ToLower() == "@origin")
			{
				return "";
			}
			if (customId.ToLower() == "@this")
			{
				return (modId ?? "").Trim();
			}
			return customId;
		}

		// Token: 0x060002AC RID: 684 RVA: 0x0001BD54 File Offset: 0x00019F54
		public static string ClarifyWorkshopId(string customId, string rootId, string modId)
		{
			if (string.IsNullOrWhiteSpace(customId))
			{
				customId = (rootId ?? "");
			}
			customId = customId.Trim();
			if (customId.ToLower() == "@origin")
			{
				return "";
			}
			if (customId.ToLower() == "@this" || customId == "")
			{
				return (modId ?? "").Trim();
			}
			return customId;
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0001BDC5 File Offset: 0x00019FC5
		public static void AddOnLocalizeAction(Action<string> action)
		{
			Tools.OnLoadLocalize += delegate(string x)
			{
				try
				{
					action(x);
				}
				catch (Exception ex)
				{
					File.WriteAllText(Application.dataPath + "/Mods/OnLocalizeError.log", ex.Message + Environment.NewLine + ex.StackTrace);
				}
			};
		}

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060002AE RID: 686 RVA: 0x0001BDE4 File Offset: 0x00019FE4
		// (remove) Token: 0x060002AF RID: 687 RVA: 0x0001BE18 File Offset: 0x0001A018
		private static event Action<string> OnLoadLocalize;

		// Token: 0x060002B0 RID: 688 RVA: 0x0001BE4B File Offset: 0x0001A04B
		internal static void CallOnLoadLocalize(string language)
		{
			Action<string> onLoadLocalize = Tools.OnLoadLocalize;
			if (onLoadLocalize == null)
			{
				return;
			}
			onLoadLocalize(language);
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x0001BE5D File Offset: 0x0001A05D
		public static void AddOnInjectIdsAction(Action action)
		{
			Tools.OnInjectIds += delegate()
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					File.WriteAllText(Application.dataPath + "/Mods/OnInjectError.log", ex.Message + Environment.NewLine + ex.StackTrace);
				}
			};
		}

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x060002B2 RID: 690 RVA: 0x0001BE7C File Offset: 0x0001A07C
		// (remove) Token: 0x060002B3 RID: 691 RVA: 0x0001BEB0 File Offset: 0x0001A0B0
		private static event Action OnInjectIds;

		// Token: 0x060002B4 RID: 692 RVA: 0x0001BEE3 File Offset: 0x0001A0E3
		internal static void CallOnInjectIds()
		{
			Action onInjectIds = Tools.OnInjectIds;
			if (onInjectIds == null)
			{
				return;
			}
			onInjectIds();
		}

		// Token: 0x04000151 RID: 337
		private static readonly NumberFormatInfo invariant = CultureInfo.InvariantCulture.NumberFormat;

		// Token: 0x020000A5 RID: 165
		public class Test<T>
		{
			// Token: 0x040002B8 RID: 696
			public T value;
		}
	}
}
