using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UI;
using UnityEngine;

namespace SummonLiberation
{
	// Token: 0x02000008 RID: 8
	public class Harmony_Patch
	{
		// Token: 0x06000020 RID: 32 RVA: 0x0000271C File Offset: 0x0000091C
		[HarmonyPatch(typeof(LibraryFloorModel), "Init")]
		[HarmonyPostfix]
		[HarmonyPriority(500)]
		private static void LibraryFloorModel_Init_Post(LibraryFloorModel __instance)
		{
			try
			{
				Harmony_Patch.AddFormationPosition(__instance._defaultFormation, 99);
				if (__instance._formation == null)
				{
					__instance._formation = __instance._defaultFormation;
				}
				else
				{
					Harmony_Patch.AddFormationPosition(__instance._formation, 99);
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/LFIerror.txt", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002798 File Offset: 0x00000998
		[HarmonyPatch(typeof(StageWaveModel), "Init")]
		[HarmonyPostfix]
		[HarmonyPriority(500)]
		private static void StageWaveModel_Init_Post(StageWaveModel __instance)
		{
			try
			{
				Harmony_Patch.AddFormationPositionForEnemy(__instance._formation, 100);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SWMIerror.txt", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000027F4 File Offset: 0x000009F4
		private static void AddFormationPosition(FormationModel Formation, int targetCount)
		{
			List<FormationPosition> postionList = Formation._postionList;
			for (int i = postionList.Count; i < targetCount; i++)
			{
				FormationPosition item = new FormationPosition(new FormationPositionXmlData
				{
					name = "E" + i.ToString(),
					vector = new XmlVector2
					{
						x = Harmony_Patch.GetVector2X(i - 4),
						y = Harmony_Patch.GetVector2Y(i - 4)
					},
					eventList = new List<FormationPositionEventXmlData>()
				})
				{
					eventList = new List<FormationPositionEvent>(),
					index = i
				};
				postionList.Add(item);
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002888 File Offset: 0x00000A88
		private static int GetVector2X(int i)
		{
			switch (i)
			{
			case 1:
				return 12;
			case 2:
				return 12;
			case 3:
				return 9;
			case 4:
				return 9;
			case 5:
				return 8;
			case 6:
				return 8;
			case 7:
				return 21;
			case 8:
				return 21;
			case 9:
				return 20;
			case 10:
				return 20;
			case 11:
				return 2;
			case 12:
				return 2;
			case 13:
				return 22;
			case 14:
				return 22;
			case 15:
				return 22;
			default:
				return 12;
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002908 File Offset: 0x00000B08
		private static int GetVector2Y(int i)
		{
			switch (i)
			{
			case 1:
				return 7;
			case 2:
				return -9;
			case 3:
				return -5;
			case 4:
				return -15;
			case 5:
				return 19;
			case 6:
				return 9;
			case 7:
				return 19;
			case 8:
				return 9;
			case 9:
				return -5;
			case 10:
				return -15;
			case 11:
				return -14;
			case 12:
				return 14;
			case 13:
				return -16;
			case 14:
				return 0;
			case 15:
				return 16;
			default:
				return 0;
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002988 File Offset: 0x00000B88
		private static void AddFormationPositionForEnemy(FormationModel Formation, int targetCount)
		{
			List<FormationPosition> postionList = Formation._postionList;
			int num = -23;
			int num2 = 18;
			for (int i = postionList.Count; i < targetCount; i++)
			{
				FormationPositionXmlData formationPositionXmlData = new FormationPositionXmlData();
				formationPositionXmlData.name = "E" + i.ToString();
				formationPositionXmlData.vector = new XmlVector2
				{
					x = num,
					y = num2
				};
				formationPositionXmlData.eventList = null;
				num += 5;
				if (num > -3)
				{
					num2 -= 7;
					num = -23;
				}
				if (num2 < -17)
				{
					num = -12;
					num2 = 0;
				}
				FormationPosition item = new FormationPosition(formationPositionXmlData)
				{
					eventList = new List<FormationPositionEvent>(),
					index = i
				};
				postionList.Add(item);
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002A2C File Offset: 0x00000C2C
		public static BattleUnitModel SummonUnit(Faction Faction, LorId EnemyUnitID, LorId BookID, int Index = -1, string PlayerUnitName = "Null", bool ForcedlySummon = false)
		{
			try
			{
				if (EnemyUnitID == null)
				{
					EnemyUnitID = LorId.None;
				}
				BattleUnitModel battleUnitModel = null;
				if (BattleObjectManager.instance.ExistsUnit((BattleUnitModel x) => x.faction == Faction && x.index == Index) && !ForcedlySummon)
				{
					return battleUnitModel;
				}
				if (Faction == null)
				{
					StageModel stageModel = Singleton<StageController>.Instance._stageModel;
					BattleTeamModel enemyTeam = Singleton<StageController>.Instance._enemyTeam;
					UnitBattleDataModel unitBattleDataModel = UnitBattleDataModel.CreateUnitBattleDataByEnemyUnitId(stageModel, EnemyUnitID);
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					StageWaveModel currentWaveModel = Singleton<StageController>.Instance.GetCurrentWaveModel();
					UnitDataModel unitData = unitBattleDataModel.unitData;
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(0);
					if (Index == -1)
					{
						battleUnitModel.index = 0;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel2 = enumerator.Current;
								if (battleUnitModel2.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel2.index + 1;
								}
							}
							goto IL_119;
						}
					}
					battleUnitModel.index = Index;
					IL_119:
					battleUnitModel.formation = currentWaveModel.GetFormationPosition(battleUnitModel.index);
					if (unitBattleDataModel.isDead)
					{
						return battleUnitModel;
					}
					battleUnitModel.grade = unitData.grade;
					battleUnitModel.SetUnitData(unitBattleDataModel);
					battleUnitModel.OnCreated();
					enemyTeam.AddUnit(battleUnitModel);
				}
				else
				{
					StageLibraryFloorModel currentStageFloorModel = Singleton<StageController>.Instance.GetCurrentStageFloorModel();
					BattleTeamModel librarianTeam = Singleton<StageController>.Instance._librarianTeam;
					UnitDataModel unitDataModel = new UnitDataModel(BookID, currentStageFloorModel.Sephirah, false);
					UnitBattleDataModel unitBattleDataModel2 = new UnitBattleDataModel(Singleton<StageController>.Instance.GetStageModel(), unitDataModel);
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					if (Singleton<EnemyUnitClassInfoList>.Instance.GetData(EnemyUnitID) != null)
					{
						unitDataModel.SetByEnemyUnitClassInfo(Singleton<EnemyUnitClassInfoList>.Instance.GetData(EnemyUnitID));
					}
					else
					{
						unitDataModel.SetTemporaryPlayerUnitByBook(BookID);
						unitDataModel.isSephirah = false;
						unitDataModel.customizeData.height = 175;
						unitDataModel.gender = 2;
						unitDataModel.appearanceType = unitDataModel.gender;
						unitDataModel.SetCustomName(PlayerUnitName);
						unitDataModel.forceItemChangeLock = true;
					}
					if (PlayerUnitName != "Null")
					{
						unitDataModel.SetTempName(PlayerUnitName);
					}
					unitBattleDataModel2.Init();
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(1);
					if (Index == -1)
					{
						battleUnitModel.index = -1;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel3 = enumerator.Current;
								if (battleUnitModel3.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel3.index + 1;
								}
							}
							goto IL_2B8;
						}
					}
					battleUnitModel.index = Index;
					IL_2B8:
					battleUnitModel.grade = unitDataModel.grade;
					battleUnitModel.formation = currentStageFloorModel.GetFormationPosition(battleUnitModel.index);
					battleUnitModel.SetUnitData(unitBattleDataModel2);
					unitDataModel._enemyUnitId = EnemyUnitID;
					battleUnitModel.OnCreated();
					librarianTeam.AddUnit(battleUnitModel);
				}
				BattleObjectManager.instance.RegisterUnit(battleUnitModel);
				battleUnitModel.passiveDetail.OnUnitCreated();
				if (battleUnitModel.allyCardDetail.GetAllDeck().Count > 0)
				{
					battleUnitModel.allyCardDetail.ReturnAllToDeck();
					battleUnitModel.allyCardDetail.DrawCards(battleUnitModel.UnitData.unitData.GetStartDraw());
				}
				if (Singleton<StageController>.Instance.Phase <= 5)
				{
					battleUnitModel.OnRoundStartOnlyUI();
					battleUnitModel.RollSpeedDice();
				}
				SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
				int num = 0;
				foreach (BattleUnitModel battleUnitModel4 in BattleObjectManager.instance.GetList())
				{
					SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(battleUnitModel4.UnitData.unitData, num++, true, false);
				}
				BattleObjectManager.instance.InitUI();
				return battleUnitModel;
			}
			catch (Exception ex)
			{
				Debug.LogError("召唤错误" + ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return null;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002E94 File Offset: 0x00001094
		public static BattleUnitModel SummonUnitByUnitBattleData(Faction Faction, UnitBattleDataModel unitBattleData, LorId EnemyUnitID, int Index = -1, string PlayerUnitName = "Null", bool ForcedlySummon = false)
		{
			try
			{
				if (EnemyUnitID == null)
				{
					EnemyUnitID = LorId.None;
				}
				BattleUnitModel battleUnitModel = null;
				if (BattleObjectManager.instance.ExistsUnit((BattleUnitModel x) => x.faction == Faction && x.index == Index) && !ForcedlySummon)
				{
					return battleUnitModel;
				}
				if (Faction == null)
				{
					StageModel stageModel = Singleton<StageController>.Instance._stageModel;
					BattleTeamModel enemyTeam = Singleton<StageController>.Instance._enemyTeam;
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					StageWaveModel currentWaveModel = Singleton<StageController>.Instance.GetCurrentWaveModel();
					UnitDataModel unitData = unitBattleData.unitData;
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(0);
					if (Index == -1)
					{
						battleUnitModel.index = 0;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel2 = enumerator.Current;
								if (battleUnitModel2.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel2.index + 1;
								}
							}
							goto IL_111;
						}
					}
					battleUnitModel.index = Index;
					IL_111:
					battleUnitModel.formation = currentWaveModel.GetFormationPosition(battleUnitModel.index);
					if (unitBattleData.isDead)
					{
						return battleUnitModel;
					}
					battleUnitModel.grade = unitData.grade;
					battleUnitModel.SetUnitData(unitBattleData);
					battleUnitModel.OnCreated();
					enemyTeam.AddUnit(battleUnitModel);
				}
				else
				{
					StageLibraryFloorModel currentStageFloorModel = Singleton<StageController>.Instance.GetCurrentStageFloorModel();
					BattleTeamModel librarianTeam = Singleton<StageController>.Instance._librarianTeam;
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					if (PlayerUnitName != "Null")
					{
						unitBattleData.unitData.SetTempName(PlayerUnitName);
					}
					unitBattleData.Init();
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(1);
					if (Index == -1)
					{
						battleUnitModel.index = -1;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel3 = enumerator.Current;
								if (battleUnitModel3.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel3.index + 1;
								}
							}
							goto IL_225;
						}
					}
					battleUnitModel.index = Index;
					IL_225:
					battleUnitModel.grade = unitBattleData.unitData.grade;
					battleUnitModel.formation = currentStageFloorModel.GetFormationPosition(battleUnitModel.index);
					battleUnitModel.SetUnitData(unitBattleData);
					unitBattleData.unitData._enemyUnitId = EnemyUnitID;
					battleUnitModel.OnCreated();
					librarianTeam.AddUnit(battleUnitModel);
				}
				BattleObjectManager.instance.RegisterUnit(battleUnitModel);
				battleUnitModel.passiveDetail.OnUnitCreated();
				if (battleUnitModel.allyCardDetail.GetAllDeck().Count > 0)
				{
					battleUnitModel.allyCardDetail.ReturnAllToDeck();
					battleUnitModel.allyCardDetail.DrawCards(battleUnitModel.UnitData.unitData.GetStartDraw());
				}
				if (Singleton<StageController>.Instance.Phase <= 5)
				{
					battleUnitModel.OnRoundStartOnlyUI();
					battleUnitModel.RollSpeedDice();
				}
				SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
				int num = 0;
				foreach (BattleUnitModel battleUnitModel4 in BattleObjectManager.instance.GetList())
				{
					SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(battleUnitModel4.UnitData.unitData, num++, true, false);
				}
				BattleObjectManager.instance.InitUI();
				return battleUnitModel;
			}
			catch (Exception ex)
			{
				Debug.LogError("召唤错误" + ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return null;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003270 File Offset: 0x00001470
		public static BattleUnitModel SummonUnitByUnitData(Faction Faction, UnitDataModel unitData, LorId EnemyUnitID, LorId BookID, int Index = -1, string PlayerUnitName = "Null", bool ForcedlySummon = false)
		{
			try
			{
				if (EnemyUnitID == null)
				{
					EnemyUnitID = LorId.None;
				}
				BattleUnitModel battleUnitModel = null;
				if (BattleObjectManager.instance.ExistsUnit((BattleUnitModel x) => x.faction == Faction && x.index == Index) && !ForcedlySummon)
				{
					return battleUnitModel;
				}
				if (Faction == null)
				{
					StageModel stageModel = Singleton<StageController>.Instance._stageModel;
					BattleTeamModel enemyTeam = Singleton<StageController>.Instance._enemyTeam;
					UnitBattleDataModel unitBattleDataModel = new UnitBattleDataModel(stageModel, unitData);
					unitBattleDataModel.Init();
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					StageWaveModel currentWaveModel = Singleton<StageController>.Instance.GetCurrentWaveModel();
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(0);
					if (Index == -1)
					{
						battleUnitModel.index = 0;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel2 = enumerator.Current;
								if (battleUnitModel2.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel2.index + 1;
								}
							}
							goto IL_118;
						}
					}
					battleUnitModel.index = Index;
					IL_118:
					battleUnitModel.formation = currentWaveModel.GetFormationPosition(battleUnitModel.index);
					if (unitBattleDataModel.isDead)
					{
						return battleUnitModel;
					}
					battleUnitModel.grade = unitData.grade;
					battleUnitModel.SetUnitData(unitBattleDataModel);
					battleUnitModel.OnCreated();
					enemyTeam.AddUnit(battleUnitModel);
				}
				else
				{
					StageLibraryFloorModel currentStageFloorModel = Singleton<StageController>.Instance.GetCurrentStageFloorModel();
					BattleTeamModel librarianTeam = Singleton<StageController>.Instance._librarianTeam;
					UnitBattleDataModel unitBattleDataModel2 = new UnitBattleDataModel(Singleton<StageController>.Instance.GetStageModel(), unitData);
					BattleObjectManager.instance.UnregisterUnitByIndex(Faction, Index);
					if (Singleton<EnemyUnitClassInfoList>.Instance.GetData(EnemyUnitID) != null)
					{
						unitData.SetByEnemyUnitClassInfo(Singleton<EnemyUnitClassInfoList>.Instance.GetData(EnemyUnitID));
					}
					if (PlayerUnitName != "Null")
					{
						unitData.SetTempName(PlayerUnitName);
					}
					unitBattleDataModel2.Init();
					battleUnitModel = BattleObjectManager.CreateDefaultUnit(1);
					if (Index == -1)
					{
						battleUnitModel.index = -1;
						using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetAliveList(Faction).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								BattleUnitModel battleUnitModel3 = enumerator.Current;
								if (battleUnitModel3.index >= battleUnitModel.index)
								{
									battleUnitModel.index = battleUnitModel3.index + 1;
								}
							}
							goto IL_259;
						}
					}
					battleUnitModel.index = Index;
					IL_259:
					battleUnitModel.grade = unitData.grade;
					battleUnitModel.formation = currentStageFloorModel.GetFormationPosition(battleUnitModel.index);
					battleUnitModel.SetUnitData(unitBattleDataModel2);
					unitData._enemyUnitId = EnemyUnitID;
					battleUnitModel.OnCreated();
					librarianTeam.AddUnit(battleUnitModel);
				}
				BattleObjectManager.instance.RegisterUnit(battleUnitModel);
				battleUnitModel.passiveDetail.OnUnitCreated();
				if (battleUnitModel.allyCardDetail.GetAllDeck().Count > 0)
				{
					battleUnitModel.allyCardDetail.ReturnAllToDeck();
					battleUnitModel.allyCardDetail.DrawCards(battleUnitModel.UnitData.unitData.GetStartDraw());
				}
				if (Singleton<StageController>.Instance.Phase <= 5)
				{
					battleUnitModel.OnRoundStartOnlyUI();
					battleUnitModel.RollSpeedDice();
				}
				SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
				int num = 0;
				foreach (BattleUnitModel battleUnitModel4 in BattleObjectManager.instance.GetList())
				{
					SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(battleUnitModel4.UnitData.unitData, num++, true, false);
				}
				BattleObjectManager.instance.InitUI();
				return battleUnitModel;
			}
			catch (Exception ex)
			{
				Debug.LogError("召唤错误" + ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return null;
		}
	}
}
