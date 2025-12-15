using System;
using System.Collections.Generic;
using BaseMod;
using LOR_DiceSystem;
using LOR_XML;

namespace GTMDProjectMoon
{
	// Token: 0x0200004B RID: 75
	public static class OrcTools
	{
		// Token: 0x0600009C RID: 156 RVA: 0x00004F6C File Offset: 0x0000316C
		[Obsolete]
		public static StageClassInfo CopyStageClassInfo(this StageClassInfo stageClassInfo, StageClassInfo_New newinfo, string uniqueId = "")
		{
			List<int> list = new List<int>();
			StageExtraCondition_New extraCondition = newinfo.extraCondition;
			if (extraCondition != null)
			{
				foreach (LorId lorId in extraCondition.stagecondition)
				{
					list.Add(lorId.id);
				}
				OrcTools.StageConditionDic[stageClassInfo.extraCondition] = extraCondition.stagecondition;
			}
			stageClassInfo._id = newinfo._id;
			stageClassInfo.workshopID = newinfo.workshopID;
			stageClassInfo.waveList = newinfo.waveList;
			stageClassInfo.stageType = newinfo.stageType;
			stageClassInfo.mapInfo = newinfo.mapInfo;
			stageClassInfo.floorNum = newinfo.floorNum;
			stageClassInfo.stageName = newinfo.stageName;
			stageClassInfo.chapter = newinfo.chapter;
			stageClassInfo.invitationInfo = newinfo.invitationInfo;
			stageClassInfo.extraCondition = new StageExtraCondition
			{
				needClearStageList = list,
				needLevel = newinfo.extraCondition.needLevel
			};
			stageClassInfo.storyList = newinfo.storyList;
			stageClassInfo.isChapterLast = newinfo.isChapterLast;
			stageClassInfo._storyType = newinfo._storyType;
			stageClassInfo.isStageFixedNormal = newinfo.isStageFixedNormal;
			stageClassInfo.floorOnlyList = newinfo.floorOnlyList;
			stageClassInfo.exceptFloorList = newinfo.exceptFloorList;
			stageClassInfo._rewardList = newinfo._rewardList;
			stageClassInfo.rewardList = newinfo.rewardList;
			return stageClassInfo;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000050DC File Offset: 0x000032DC
		[Obsolete]
		public static EnemyUnitClassInfo CopyEnemyUnitClassInfo(this EnemyUnitClassInfo enemyUnitClassInfo, EnemyUnitClassInfo_New newinfo, string uniqueId = "")
		{
			List<EnemyDropItemTable> list = new List<EnemyDropItemTable>();
			foreach (EnemyDropItemTable_New enemyDropItemTable_New in newinfo.dropTableList)
			{
				List<EnemyDropItem> list2 = new List<EnemyDropItem>();
				foreach (EnemyDropItem_New enemyDropItem_New in enemyDropItemTable_New.dropItemList)
				{
					list2.Add(new EnemyDropItem
					{
						prob = enemyDropItem_New.prob,
						bookId = enemyDropItem_New.bookId
					});
				}
				list.Add(new EnemyDropItemTable
				{
					emotionLevel = enemyDropItemTable_New.emotionLevel,
					dropItemList = list2
				});
			}
			enemyUnitClassInfo.AiScript = newinfo.AiScript;
			enemyUnitClassInfo.bodyId = newinfo.bodyId;
			enemyUnitClassInfo.bookId = newinfo.bookId;
			enemyUnitClassInfo.dropBonus = newinfo.dropBonus;
			enemyUnitClassInfo.dropTableList = list;
			enemyUnitClassInfo.emotionCardList = newinfo.emotionCardList;
			enemyUnitClassInfo.exp = newinfo.exp;
			enemyUnitClassInfo.faceType = newinfo.faceType;
			enemyUnitClassInfo.gender = newinfo.gender;
			enemyUnitClassInfo.height = newinfo.height;
			enemyUnitClassInfo.isUnknown = newinfo.isUnknown;
			enemyUnitClassInfo.maxHeight = newinfo.maxHeight;
			enemyUnitClassInfo.minHeight = newinfo.minHeight;
			enemyUnitClassInfo.name = newinfo.name;
			enemyUnitClassInfo.nameId = newinfo.nameId;
			enemyUnitClassInfo.retreat = newinfo.retreat;
			enemyUnitClassInfo.workshopID = newinfo.workshopID;
			enemyUnitClassInfo._id = newinfo._id;
			OrcTools.DropItemDic[new LorId(uniqueId, newinfo._id)] = newinfo.dropTableList;
			return enemyUnitClassInfo;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000052AC File Offset: 0x000034AC
		[Obsolete]
		public static BattleDialogRoot CopyBattleDialogRoot(this BattleDialogRoot battleDialogRoot, BattleDialogRoot newinfo, string uniqueId = "")
		{
			battleDialogRoot.characterList = new List<BattleDialogCharacter>();
			foreach (BattleDialogCharacter_New battleDialogCharacter_New in newinfo.characterList)
			{
				int num = -1;
				try
				{
					num = int.Parse(battleDialogCharacter_New.characterID);
				}
				catch
				{
				}
				battleDialogCharacter_New.workshopId = uniqueId;
				battleDialogCharacter_New.bookId = num;
				battleDialogRoot.groupName = newinfo.groupName;
				battleDialogRoot.characterList.Add(new BattleDialogCharacter
				{
					characterID = battleDialogCharacter_New.characterID,
					dialogTypeList = battleDialogCharacter_New.dialogTypeList,
					workshopId = uniqueId,
					bookId = num
				});
				if (string.IsNullOrWhiteSpace(battleDialogCharacter_New.characterName))
				{
					battleDialogCharacter_New.characterName = "";
				}
				if (num != -1)
				{
					OrcTools.DialogDetail dialogDetail = OrcTools.DialogDetail.FindDialogInCharacterID(new LorId(uniqueId, num));
					if (dialogDetail == null)
					{
						dialogDetail = new OrcTools.DialogDetail
						{
							GroupName = newinfo.groupName,
							BookId = new LorId(uniqueId, num),
							CharacterID = new LorId(uniqueId, num),
							CharacterName = battleDialogCharacter_New.characterName
						};
						OrcTools.dialogDetails.Add(dialogDetail);
					}
					else
					{
						dialogDetail.CharacterName = battleDialogCharacter_New.characterName;
					}
				}
			}
			return battleDialogRoot;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000053F8 File Offset: 0x000035F8
		public static BattleDialogRoot CopyBattleDialogRootNew(this BattleDialogRoot_V2 newinfo, string uniqueId = "")
		{
			BattleDialogRoot battleDialogRoot = new BattleDialogRoot();
			battleDialogRoot.characterList = new List<BattleDialogCharacter>();
			battleDialogRoot.groupName = newinfo.groupName;
			foreach (BattleDialogCharacter_V2 battleDialogCharacter_V in newinfo.characterList)
			{
				int num = -1;
				try
				{
					num = int.Parse(battleDialogCharacter_V.characterID);
				}
				catch
				{
				}
				string workshopId = Tools.ClarifyWorkshopId(battleDialogCharacter_V.workshopId, newinfo.customPid, uniqueId);
				battleDialogCharacter_V.workshopId = workshopId;
				battleDialogCharacter_V.bookId = num;
				battleDialogRoot.characterList.Add(new BattleDialogCharacter
				{
					characterID = battleDialogCharacter_V.characterID,
					dialogTypeList = battleDialogCharacter_V.dialogTypeList,
					workshopId = workshopId,
					bookId = num
				});
				BattleDialogCharacter_V2 battleDialogCharacter_V2 = battleDialogCharacter_V;
				string characterName = battleDialogCharacter_V.characterName;
				battleDialogCharacter_V2.characterName = (((characterName != null) ? characterName.Trim() : null) ?? string.Empty);
				if (num != -1)
				{
					OrcTools.DialogDetail dialogDetail = OrcTools.DialogDetail.FindDialogInCharacterID(new LorId(uniqueId, num));
					if (dialogDetail == null)
					{
						dialogDetail = new OrcTools.DialogDetail
						{
							GroupName = newinfo.groupName,
							BookId = new LorId(uniqueId, num),
							CharacterID = new LorId(uniqueId, num),
							CharacterName = battleDialogCharacter_V.characterName
						};
						OrcTools.dialogDetails.Add(dialogDetail);
					}
					else
					{
						dialogDetail.CharacterName = battleDialogCharacter_V.characterName;
					}
				}
			}
			return battleDialogRoot;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00005584 File Offset: 0x00003784
		[Obsolete]
		public static List<BattleDialogRelationWithBookID> CopyBattleDialogRelation(this List<BattleDialogRelationWithBookID> BattleDialogRelation, List<BattleDialogRelationWithBookID_New> newinfo, string uniqueId = "")
		{
			foreach (BattleDialogRelationWithBookID_New battleDialogRelationWithBookID_New in newinfo)
			{
				if (string.IsNullOrWhiteSpace(battleDialogRelationWithBookID_New.workshopId))
				{
					battleDialogRelationWithBookID_New.workshopId = uniqueId;
				}
				if (battleDialogRelationWithBookID_New.workshopId.ToLower() == "@origin")
				{
					battleDialogRelationWithBookID_New.workshopId = "";
				}
				int num = -1;
				try
				{
					num = int.Parse(battleDialogRelationWithBookID_New.characterID);
				}
				catch
				{
				}
				BattleDialogRelation.Add(new BattleDialogRelationWithBookID
				{
					bookID = battleDialogRelationWithBookID_New.bookID,
					storyID = battleDialogRelationWithBookID_New.storyID,
					groupName = battleDialogRelationWithBookID_New.groupName,
					characterID = battleDialogRelationWithBookID_New.characterID
				});
				if (string.IsNullOrWhiteSpace(battleDialogRelationWithBookID_New.groupName))
				{
					battleDialogRelationWithBookID_New.groupName = "";
				}
				if (num != -1)
				{
					OrcTools.DialogDetail dialogDetail = OrcTools.DialogDetail.FindDialogInBookID(new LorId(battleDialogRelationWithBookID_New.workshopId, battleDialogRelationWithBookID_New.bookID));
					if (dialogDetail == null)
					{
						dialogDetail = new OrcTools.DialogDetail
						{
							GroupName = battleDialogRelationWithBookID_New.groupName,
							BookId = new LorId(battleDialogRelationWithBookID_New.workshopId, battleDialogRelationWithBookID_New.bookID),
							CharacterID = new LorId(battleDialogRelationWithBookID_New.workshopId, num)
						};
						OrcTools.dialogDetails.Add(dialogDetail);
					}
					else
					{
						dialogDetail.GroupName = battleDialogRelationWithBookID_New.groupName;
						dialogDetail.CharacterID = new LorId(battleDialogRelationWithBookID_New.workshopId, num);
					}
				}
			}
			return BattleDialogRelation;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x0000571C File Offset: 0x0000391C
		public static List<BattleDialogRelationWithBookID> CopyBattleDialogRelationNew(this BattleDialogRelationRoot_V2 newinfo, string uniqueId = "")
		{
			List<BattleDialogRelationWithBookID> list = new List<BattleDialogRelationWithBookID>();
			foreach (BattleDialogRelationWithBookID_V2 battleDialogRelationWithBookID_V in newinfo.list)
			{
				battleDialogRelationWithBookID_V.workshopId = Tools.ClarifyWorkshopId(battleDialogRelationWithBookID_V.workshopId, newinfo.customPid, uniqueId);
				int num = -1;
				try
				{
					num = int.Parse(battleDialogRelationWithBookID_V.characterID);
				}
				catch
				{
				}
				list.Add(new BattleDialogRelationWithBookID
				{
					bookID = battleDialogRelationWithBookID_V.bookID,
					storyID = battleDialogRelationWithBookID_V.storyID,
					groupName = battleDialogRelationWithBookID_V.groupName,
					characterID = battleDialogRelationWithBookID_V.characterID
				});
				BattleDialogRelationWithBookID battleDialogRelationWithBookID = battleDialogRelationWithBookID_V;
				string groupName = battleDialogRelationWithBookID_V.groupName;
				battleDialogRelationWithBookID.groupName = (((groupName != null) ? groupName.Trim() : null) ?? string.Empty);
				if (num != -1)
				{
					OrcTools.DialogDetail dialogDetail = OrcTools.DialogDetail.FindDialogInBookID(battleDialogRelationWithBookID_V.bookLorId);
					if (dialogDetail == null)
					{
						dialogDetail = new OrcTools.DialogDetail
						{
							GroupName = battleDialogRelationWithBookID_V.groupName,
							BookId = battleDialogRelationWithBookID_V.bookLorId,
							CharacterID = new LorId(battleDialogRelationWithBookID_V.workshopId, num)
						};
						OrcTools.dialogDetails.Add(dialogDetail);
					}
					else
					{
						dialogDetail.GroupName = battleDialogRelationWithBookID_V.groupName;
						dialogDetail.CharacterID = new LorId(battleDialogRelationWithBookID_V.workshopId, num);
					}
				}
			}
			return list;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00005894 File Offset: 0x00003A94
		[Obsolete]
		public static BookXmlInfo CopyBookXmlInfo(this BookXmlInfo bookXml, BookXmlInfo_New newinfo, string uniqueId = "")
		{
			List<int> list = new List<int>();
			List<BookSoulCardInfo> list2 = new List<BookSoulCardInfo>();
			foreach (LorIdXml lorIdXml in newinfo.EquipEffect.OnlyCard)
			{
				list.Add(lorIdXml.xmlId);
			}
			foreach (BookSoulCardInfo_New bookSoulCardInfo_New in newinfo.EquipEffect.CardList)
			{
				bookSoulCardInfo_New.WorkshopId = Tools.ClarifyWorkshopId("", bookSoulCardInfo_New.WorkshopId, uniqueId);
				list2.Add(new BookSoulCardInfo
				{
					cardId = bookSoulCardInfo_New.cardId,
					requireLevel = bookSoulCardInfo_New.requireLevel,
					emotionLevel = bookSoulCardInfo_New.emotionLevel
				});
			}
			bookXml._id = newinfo._id;
			bookXml.isError = newinfo.isError;
			bookXml.workshopID = newinfo.workshopID;
			bookXml.InnerName = newinfo.InnerName;
			bookXml.TextId = newinfo.TextId;
			bookXml._bookIcon = newinfo._bookIcon;
			bookXml.optionList = newinfo.optionList;
			bookXml.categoryList = newinfo.categoryList;
			foreach (string category in newinfo.customCategoryList)
			{
				bookXml.categoryList.Add(OrcTools.GetBookCategory(category));
			}
			newinfo.customCategoryList.Clear();
			bookXml.EquipEffect = new BookEquipEffect
			{
				HpReduction = newinfo.EquipEffect.HpReduction,
				Hp = newinfo.EquipEffect.Hp,
				DeadLine = newinfo.EquipEffect.DeadLine,
				Break = newinfo.EquipEffect.Break,
				SpeedMin = newinfo.EquipEffect.SpeedMin,
				Speed = newinfo.EquipEffect.Speed,
				SpeedDiceNum = newinfo.EquipEffect.SpeedDiceNum,
				SResist = newinfo.EquipEffect.SResist,
				PResist = newinfo.EquipEffect.PResist,
				HResist = newinfo.EquipEffect.HResist,
				SBResist = newinfo.EquipEffect.SBResist,
				PBResist = newinfo.EquipEffect.PBResist,
				HBResist = newinfo.EquipEffect.HBResist,
				MaxPlayPoint = newinfo.EquipEffect.MaxPlayPoint,
				StartPlayPoint = newinfo.EquipEffect.StartPlayPoint,
				AddedStartDraw = newinfo.EquipEffect.AddedStartDraw,
				PassiveCost = newinfo.EquipEffect.PassiveCost,
				OnlyCard = list,
				CardList = list2,
				_PassiveList = newinfo.EquipEffect._PassiveList,
				PassiveList = newinfo.EquipEffect.PassiveList
			};
			bookXml.Rarity = newinfo.Rarity;
			bookXml.CharacterSkin = newinfo.CharacterSkin;
			bookXml.skinType = newinfo.skinType;
			bookXml.gender = newinfo.gender;
			bookXml.Chapter = newinfo.Chapter;
			try
			{
				bookXml.episode = newinfo.episode.xmlId;
			}
			catch
			{
				bookXml.episode = -1;
			}
			bookXml.RangeType = newinfo.RangeType;
			bookXml.canNotEquip = newinfo.canNotEquip;
			bookXml.RandomFace = newinfo.RandomFace;
			bookXml.speedDiceNumber = newinfo.speedDiceNumber;
			bookXml.SuccessionPossibleNumber = newinfo.SuccessionPossibleNumber;
			bookXml.motionSoundList = newinfo.motionSoundList;
			bookXml.remainRewardValue = newinfo.remainRewardValue;
			LorId key = new LorId(uniqueId, newinfo._id);
			List<LorId> list3 = new List<LorId>();
			LorId.InitializeLorIds<LorIdXml>(newinfo.EquipEffect.OnlyCard, list3, uniqueId);
			OrcTools.OnlyCardDic[key] = list3;
			OrcTools.SoulCardDic[key] = newinfo.EquipEffect.CardList;
			if (newinfo.episode.xmlId > 0)
			{
				OrcTools.EpisodeDic[key] = LorId.MakeLorId(newinfo.episode, uniqueId);
			}
			return bookXml;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00005CCC File Offset: 0x00003ECC
		[Obsolete]
		public static DiceCardXmlInfo CopyDiceCardXmlInfo(this DiceCardXmlInfo info, DiceCardXmlInfo_New newinfo)
		{
			info.Artwork = newinfo.Artwork;
			info.category = (string.IsNullOrWhiteSpace(newinfo.customCategory) ? newinfo.category : OrcTools.GetBookCategory(newinfo.customCategory));
			info.Chapter = newinfo.Chapter;
			info.DiceBehaviourList = newinfo.DiceBehaviourList;
			info.EgoMaxCooltimeValue = newinfo.EgoMaxCooltimeValue;
			info.Keywords = newinfo.Keywords;
			info.MapChange = newinfo.MapChange;
			info.MaxNum = newinfo.MaxNum;
			info.optionList = newinfo.optionList;
			info.Priority = newinfo.Priority;
			info.PriorityScript = newinfo.PriorityScript;
			info.Rarity = newinfo.Rarity;
			info.Script = newinfo.Script;
			info.ScriptDesc = newinfo.ScriptDesc;
			info.SkinChange = newinfo.SkinChange;
			info.SkinChangeType = newinfo.SkinChangeType;
			info.SkinHeight = newinfo.SkinHeight;
			info.Spec = newinfo.Spec;
			info.SpecialEffect = newinfo.SpecialEffect;
			info.workshopName = newinfo.workshopName;
			info._id = newinfo._id;
			info._textId = newinfo._textId;
			return info;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00005DFC File Offset: 0x00003FFC
		public static BookCategory GetBookCategory(string category)
		{
			if (!string.IsNullOrWhiteSpace(category))
			{
				return Tools.MakeEnum<BookCategory>(category);
			}
			return 0;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00005E0E File Offset: 0x0000400E
		public static BookOption GetBookOption(string option)
		{
			if (!string.IsNullOrWhiteSpace(option))
			{
				return Tools.MakeEnum<BookOption>(option);
			}
			return 0;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00005E20 File Offset: 0x00004020
		public static CardOption GetCardOption(string option)
		{
			if (!string.IsNullOrWhiteSpace(option))
			{
				return Tools.MakeEnum<CardOption>(option);
			}
			return Tools.MakeEnum<CardOption>("None");
		}

		// Token: 0x040000D8 RID: 216
		public static readonly List<OrcTools.DialogDetail> dialogDetails = new List<OrcTools.DialogDetail>();

		// Token: 0x040000D9 RID: 217
		public static Dictionary<LorId, List<LorId>> OnlyCardDic = new Dictionary<LorId, List<LorId>>();

		// Token: 0x040000DA RID: 218
		public static Dictionary<LorId, List<BookSoulCardInfo_New>> SoulCardDic = new Dictionary<LorId, List<BookSoulCardInfo_New>>();

		// Token: 0x040000DB RID: 219
		public static readonly Dictionary<LorId, string> StageNameDic = new Dictionary<LorId, string>();

		// Token: 0x040000DC RID: 220
		public static readonly Dictionary<StageExtraCondition, List<LorId>> StageConditionDic = new Dictionary<StageExtraCondition, List<LorId>>();

		// Token: 0x040000DD RID: 221
		public static readonly Dictionary<LorId, string> CharacterNameDic = new Dictionary<LorId, string>();

		// Token: 0x040000DE RID: 222
		public static readonly Dictionary<EmotionEgoXmlInfo, LorId> EgoDic = new Dictionary<EmotionEgoXmlInfo, LorId>();

		// Token: 0x040000DF RID: 223
		[Obsolete]
		public static readonly Dictionary<LorId, List<EnemyDropItemTable_New>> DropItemDic = new Dictionary<LorId, List<EnemyDropItemTable_New>>();

		// Token: 0x040000E0 RID: 224
		public static readonly Dictionary<LorId, List<EnemyDropItemTable_V2>> DropItemDicV2 = new Dictionary<LorId, List<EnemyDropItemTable_V2>>();

		// Token: 0x040000E1 RID: 225
		public static readonly Dictionary<SephirahType, Dictionary<LorId, EmotionCardXmlInfo>> CustomEmotionCards = new Dictionary<SephirahType, Dictionary<LorId, EmotionCardXmlInfo>>();

		// Token: 0x040000E2 RID: 226
		public static readonly Dictionary<SephirahType, Dictionary<LorId, EmotionEgoXmlInfo>> CustomEmotionEgo = new Dictionary<SephirahType, Dictionary<LorId, EmotionEgoXmlInfo>>();

		// Token: 0x040000E3 RID: 227
		public static readonly Dictionary<LorId, GiftXmlInfo> CustomGifts = new Dictionary<LorId, GiftXmlInfo>();

		// Token: 0x040000E4 RID: 228
		public static readonly Dictionary<LorId, TitleXmlInfo> CustomPrefixes = new Dictionary<LorId, TitleXmlInfo>();

		// Token: 0x040000E5 RID: 229
		public static readonly Dictionary<LorId, TitleXmlInfo> CustomPostfixes = new Dictionary<LorId, TitleXmlInfo>();

		// Token: 0x040000E6 RID: 230
		public static readonly Dictionary<int, LorId> GiftAndTitleDic = new Dictionary<int, LorId>();

		// Token: 0x040000E7 RID: 231
		public static readonly Dictionary<LorId, LorId> UnitBookDic = new Dictionary<LorId, LorId>();

		// Token: 0x040000E8 RID: 232
		public static readonly Dictionary<FloorLevelXmlInfo, LorId> FloorLevelStageDic = new Dictionary<FloorLevelXmlInfo, LorId>();

		// Token: 0x040000E9 RID: 233
		public static readonly Dictionary<LorId, FormationXmlInfo> CustomFormations = new Dictionary<LorId, FormationXmlInfo>();

		// Token: 0x040000EA RID: 234
		public static readonly Dictionary<LorId, LorId> EpisodeDic = new Dictionary<LorId, LorId>();

		// Token: 0x02000072 RID: 114
		public class DialogDetail
		{
			// Token: 0x060002DA RID: 730 RVA: 0x0001C544 File Offset: 0x0001A744
			public static OrcTools.DialogDetail FindDialogInBookID(LorId id)
			{
				if (id == null)
				{
					return null;
				}
				foreach (OrcTools.DialogDetail dialogDetail in OrcTools.dialogDetails)
				{
					if (dialogDetail.BookId == id)
					{
						return dialogDetail;
					}
				}
				return null;
			}

			// Token: 0x060002DB RID: 731 RVA: 0x0001C5B0 File Offset: 0x0001A7B0
			public static OrcTools.DialogDetail FindDialogInCharacterName(string name)
			{
				if (string.IsNullOrWhiteSpace(name))
				{
					return null;
				}
				name = name.Trim();
				foreach (OrcTools.DialogDetail dialogDetail in OrcTools.dialogDetails)
				{
					if (dialogDetail.CharacterName == name)
					{
						return dialogDetail;
					}
				}
				return null;
			}

			// Token: 0x060002DC RID: 732 RVA: 0x0001C624 File Offset: 0x0001A824
			public static OrcTools.DialogDetail FindDialogInGroupName(string name)
			{
				if (string.IsNullOrWhiteSpace(name))
				{
					return null;
				}
				name = name.Trim();
				foreach (OrcTools.DialogDetail dialogDetail in OrcTools.dialogDetails)
				{
					if (dialogDetail.GroupName == name)
					{
						return dialogDetail;
					}
				}
				return null;
			}

			// Token: 0x060002DD RID: 733 RVA: 0x0001C698 File Offset: 0x0001A898
			public static OrcTools.DialogDetail FindDialogInCharacterID(LorId id)
			{
				if (id == null)
				{
					return null;
				}
				foreach (OrcTools.DialogDetail dialogDetail in OrcTools.dialogDetails)
				{
					if (dialogDetail.CharacterID == id)
					{
						return dialogDetail;
					}
				}
				return null;
			}

			// Token: 0x04000167 RID: 359
			public string CharacterName = "";

			// Token: 0x04000168 RID: 360
			public string GroupName = "";

			// Token: 0x04000169 RID: 361
			public LorId CharacterID = LorId.None;

			// Token: 0x0400016A RID: 362
			public LorId BookId = LorId.None;
		}
	}
}
