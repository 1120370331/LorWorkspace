using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace BaseMod
{
	// Token: 0x02000065 RID: 101
	public static class UIPanelTool
	{
		// Token: 0x0600025D RID: 605 RVA: 0x0001A389 File Offset: 0x00018589
		public static T GetUIPanel<T>(UIPanelType type) where T : UIPanel
		{
			return UIController.Instance.GetUIPanel(type) as T;
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0001A3A0 File Offset: 0x000185A0
		public static UIMainPanel GetMainPanel()
		{
			UIPanelTool.这个是注译 = "主界面中间楼层和云朵UI";
			UIMainPanel uipanel = UIPanelTool.GetUIPanel<UIMainPanel>(1);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600025F RID: 607 RVA: 0x0001A3BD File Offset: 0x000185BD
		public static UIBattleResultPanel GetBattleResultPanel()
		{
			UIPanelTool.这个是注译 = "接待后奖励界面";
			UIBattleResultPanel uipanel = UIPanelTool.GetUIPanel<UIBattleResultPanel>(13);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000260 RID: 608 RVA: 0x0001A3DB File Offset: 0x000185DB
		public static UIBattleSettingPanel GetBattleSettingPanel()
		{
			UIPanelTool.这个是注译 = "接待前";
			UIBattleSettingPanel uipanel = UIPanelTool.GetUIPanel<UIBattleSettingPanel>(10);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0001A3F9 File Offset: 0x000185F9
		public static UIControlButtonPanel GetControlButtonPanel()
		{
			UIPanelTool.这个是注译 = "左侧栏按钮(不是敌人)";
			UIControlButtonPanel uipanel = UIPanelTool.GetUIPanel<UIControlButtonPanel>(11);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000262 RID: 610 RVA: 0x0001A417 File Offset: 0x00018617
		public static UICurtainPanel GetCurtainPanel()
		{
			UIPanelTool.这个是注译 = "没有使用";
			UICurtainPanel uipanel = UIPanelTool.GetUIPanel<UICurtainPanel>(5);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0001A434 File Offset: 0x00018634
		public static UIPanel GetDecorationsPanel()
		{
			UIPanelTool.这个是注译 = "UI05";
			UIPanel uipanel = UIPanelTool.GetUIPanel<UIPanel>(15);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000264 RID: 612 RVA: 0x0001A452 File Offset: 0x00018652
		public static UIPanel GetDUMMYPanel()
		{
			UIPanelTool.这个是注译 = "UI06";
			UIPanel uipanel = UIPanelTool.GetUIPanel<UIPanel>(18);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0001A470 File Offset: 0x00018670
		public static UIFilterPanel GetFilterPanel()
		{
			UIPanelTool.这个是注译 = "点击司书出现的界面最上方几个按钮(非接待前)";
			UIFilterPanel uipanel = UIPanelTool.GetUIPanel<UIFilterPanel>(17);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0001A48E File Offset: 0x0001868E
		public static UIFloorPanel GetFloorPanel()
		{
			UIPanelTool.这个是注译 = "主界面楼层信息";
			UIFloorPanel uipanel = UIPanelTool.GetUIPanel<UIFloorPanel>(2);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000267 RID: 615 RVA: 0x0001A4AB File Offset: 0x000186AB
		public static UIInvitationPanel GetInvitationPanel()
		{
			UIPanelTool.这个是注译 = "主线界面";
			UIInvitationPanel uipanel = UIPanelTool.GetUIPanel<UIInvitationPanel>(7);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000268 RID: 616 RVA: 0x0001A4C8 File Offset: 0x000186C8
		public static UILibrarianInfoPanel GetLibrarianInfoPanel()
		{
			UIPanelTool.这个是注译 = "司书信息";
			UILibrarianInfoPanel uipanel = UIPanelTool.GetUIPanel<UILibrarianInfoPanel>(3);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000269 RID: 617 RVA: 0x0001A4E5 File Offset: 0x000186E5
		public static UIStoryArchivesPanel GetStoryArchivesPanel()
		{
			UIPanelTool.这个是注译 = "书库";
			UIStoryArchivesPanel uipanel = UIPanelTool.GetUIPanel<UIStoryArchivesPanel>(12);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026A RID: 618 RVA: 0x0001A503 File Offset: 0x00018703
		public static UITitlePanel GetTitlePanel()
		{
			UIPanelTool.这个是注译 = "左上角标题";
			UITitlePanel uipanel = UIPanelTool.GetUIPanel<UITitlePanel>(16);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0001A521 File Offset: 0x00018721
		public static UIBookPanel GetBookPanel()
		{
			UIPanelTool.这个是注译 = "烧书界面";
			UIBookPanel uipanel = UIPanelTool.GetUIPanel<UIBookPanel>(4);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0001A53E File Offset: 0x0001873E
		public static UICardPanel GetCardPanel()
		{
			UIPanelTool.这个是注译 = "战斗外战斗书页列表";
			UICardPanel uipanel = UIPanelTool.GetUIPanel<UICardPanel>(8);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0001A55B File Offset: 0x0001875B
		public static UIEquipPageInventoryPanel GetEquipPageInventoryPanel()
		{
			UIPanelTool.这个是注译 = "战斗外核心书页列表";
			UIEquipPageInventoryPanel uipanel = UIPanelTool.GetUIPanel<UIEquipPageInventoryPanel>(14);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0001A579 File Offset: 0x00018779
		public static UILibrarianCharacterListPanel GetLibrarianCharacterListPanel()
		{
			UIPanelTool.这个是注译 = "右侧玩家UI";
			UILibrarianCharacterListPanel uipanel = UIPanelTool.GetUIPanel<UILibrarianCharacterListPanel>(9);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0001A597 File Offset: 0x00018797
		public static UIEnemyCharacterListPanel GetEnemyCharacterListPanel()
		{
			UIPanelTool.这个是注译 = "左侧敌人UI";
			UIEnemyCharacterListPanel uipanel = UIPanelTool.GetUIPanel<UIEnemyCharacterListPanel>(6);
			UIPanelTool.Debug(uipanel.gameObject);
			return uipanel;
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0001A5B4 File Offset: 0x000187B4
		public static UISephirahButton GetSephirahButton(SephirahType sephirahType)
		{
			UIPanelTool.这个是注译 = "战斗主界面楼层按钮";
			UISephirahButton uisephirahButton = UIPanelTool.GetBattleSettingPanel().FindSephirahButton(sephirahType);
			UIPanelTool.Debug(uisephirahButton.gameObject);
			return uisephirahButton;
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0001A5D6 File Offset: 0x000187D6
		public static UISettingEquipPageInvenPanel GetEquipInvenPanel()
		{
			UIPanelTool.这个是注译 = "UI16";
			UISettingEquipPageInvenPanel equipPagePanel = UIPanelTool.GetUIPanel<UIBattleSettingPanel>(10).EditPanel.EquipPagePanel;
			UIPanelTool.Debug(equipPagePanel.gameObject);
			return equipPagePanel;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0001A5FE File Offset: 0x000187FE
		public static UIBattleStoryPanel GetBattleStoryPanel()
		{
			UIPanelTool.这个是注译 = "UI17";
			UIBattleStoryPanel battleStoryPanel = UIPanelTool.GetUIPanel<UIStoryArchivesPanel>(12).battleStoryPanel;
			UIPanelTool.Debug(battleStoryPanel.gameObject);
			return battleStoryPanel;
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0001A624 File Offset: 0x00018824
		public static UILibrarianEquipDeckPanel GetCardDeckPanel()
		{
			UIPanelTool.这个是注译 = "玩家核心书页";
			if (UIController.Instance.CurrentUIPhase != 5)
			{
				UILibrarianEquipDeckPanel equipInfoDeckPanel = UIPanelTool.GetUIPanel<UICardPanel>(8).EquipInfoDeckPanel;
				UIPanelTool.Debug(equipInfoDeckPanel.gameObject);
				return equipInfoDeckPanel;
			}
			UILibrarianEquipDeckPanel equipInfoDeckPanel2 = UIPanelTool.GetUIPanel<UIBattleSettingPanel>(10).EditPanel.BattleCardPanel.EquipInfoDeckPanel;
			UIPanelTool.Debug(equipInfoDeckPanel2.gameObject);
			return equipInfoDeckPanel2;
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0001A680 File Offset: 0x00018880
		public static UICardEquipInfoPanel GetCardEquipInfoPanel()
		{
			UIPanelTool.这个是注译 = "战斗书页";
			if (UIController.Instance.CurrentUIPhase != 5)
			{
				UICardEquipInfoPanel cardEquipInfoPanel = UIPanelTool.GetUIPanel<UICardPanel>(8).CardEquipInfoPanel;
				UIPanelTool.Debug(cardEquipInfoPanel.gameObject);
				return cardEquipInfoPanel;
			}
			UICardEquipInfoPanel cardEquipInfoPanel2 = UIPanelTool.GetUIPanel<UIBattleSettingPanel>(10).EditPanel.BattleCardPanel.CardEquipInfoPanel;
			UIPanelTool.Debug(cardEquipInfoPanel2.gameObject);
			return cardEquipInfoPanel2;
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0001A6DC File Offset: 0x000188DC
		private static void Debug(GameObject UIPanel)
		{
			if (UIPanelTool.IsDebug)
			{
				try
				{
					Text[] componentsInChildren = UIPanel.GetComponentsInChildren<Text>();
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].text = UIPanelTool.这个是注译 + "_" + i.ToString();
						componentsInChildren[i].color = Color.red;
					}
					Image[] componentsInChildren2 = UIPanel.GetComponentsInChildren<Image>();
					for (int j = 0; j < componentsInChildren2.Length; j++)
					{
						componentsInChildren2[j].color = Color.red;
					}
				}
				catch
				{
				}
			}
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0001A768 File Offset: 0x00018968
		public static void DoSetParent(Transform Target, Transform transform)
		{
			if (Target.GetComponent<RectTransform>())
			{
				Target.transform.SetParent(transform, false);
				return;
			}
			Target.transform.parent = transform;
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0001A794 File Offset: 0x00018994
		public static void SetParent(Transform Target, string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				UIPanelTool.DoSetParent(Target, null);
				return;
			}
			Transform transform = UIPanelTool.GetTransform(input);
			if (transform)
			{
				UIPanelTool.DoSetParent(Target, transform);
			}
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0001A7C8 File Offset: 0x000189C8
		private static string GetTransformPath(Transform transform, bool includeSelf = false)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (includeSelf)
			{
				stringBuilder.Append(transform.name);
			}
			while (transform.parent)
			{
				transform = transform.parent;
				stringBuilder.Insert(0, '/');
				stringBuilder.Insert(0, transform.name);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0001A820 File Offset: 0x00018A20
		public static Transform GetTransform(string input)
		{
			Transform result = null;
			if (input.EndsWith("/"))
			{
				input = input.Remove(input.Length - 1);
			}
			GameObject gameObject = GameObject.Find(input);
			if (gameObject != null)
			{
				result = gameObject.transform;
			}
			else
			{
				string b = input.Split(new char[]
				{
					'/'
				}).Last<string>();
				Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
				List<GameObject> list = new List<GameObject>();
				foreach (Object @object in array)
				{
					if (@object.name == b)
					{
						list.Add((GameObject)@object);
					}
				}
				foreach (GameObject gameObject2 in list)
				{
					string text = UIPanelTool.GetTransformPath(gameObject2.transform, true);
					if (text.EndsWith("/"))
					{
						text = text.Remove(text.Length - 1);
					}
					if (text == input)
					{
						result = gameObject2.transform;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x0400014C RID: 332
		public static string 这个是注译 = string.Empty;

		// Token: 0x0400014D RID: 333
		public static bool IsDebug;
	}
}
