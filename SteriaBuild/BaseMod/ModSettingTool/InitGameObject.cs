using System;
using System.Collections.Generic;
using BaseMod;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ModSettingTool
{
	// Token: 0x02000004 RID: 4
	public class InitGameObject : MonoBehaviour
	{
		// Token: 0x0600000B RID: 11 RVA: 0x00002390 File Offset: 0x00000590
		public static void SetAlarmText(string alarmtype, List<string> Options, OptionEvent confirmFunc = null, params object[] args)
		{
			if (UIAlarmPopup.instance.IsOpened())
			{
				UIAlarmPopup.instance.Close();
			}
			if (InitGameObject.OptionDropdown == null)
			{
				GameObject gameObject = Object.Instantiate<GameObject>(UtilTools.GetTransform("[Canvas][Script]PopupCanvas/[Script]PopupManager/[Prefab]UIOptionWindow/OptionRootPanel/[Layout]PanelLayout/RightPanel/[Prefab]OptionDropdown (1)/").gameObject);
				InitGameObject.OptionDropdown = gameObject.GetComponent<TMP_Dropdown>();
				gameObject.transform.SetParent(UIAlarmPopup.instance.transform, false);
				gameObject.transform.localPosition = new Vector3(-121.3409f, 72.2201f, 0f);
				gameObject.transform.localScale = new Vector3(1.1f, 1.15f, 1f);
				GameObject gameObject2 = Object.Instantiate<GameObject>(UtilTools.GetTransform("[Canvas][Script]PopupCanvas/[Prefab]PopupAlarm/[Rect]Normal/[Button]OK/").gameObject);
				gameObject2.transform.SetParent(UIAlarmPopup.instance.transform, false);
				gameObject2.transform.localPosition = new Vector3(140.7599f, 69.4f, 0f);
				InitGameObject.GetEvent = gameObject2.GetComponent<EventTrigger>();
				InitGameObject.GetEvent.triggers[0].callback.RemoveAllListeners();
				InitGameObject.GetEvent.triggers[0].callback.AddListener(new UnityAction<BaseEventData>(InitGameObject.OnClick));
				InitGameObject.GetEvent.triggers[2].callback.RemoveAllListeners();
				InitGameObject.GetEvent.triggers[2].callback.AddListener(new UnityAction<BaseEventData>(InitGameObject.OnClick));
			}
			InitGameObject.OptionDropdown.gameObject.SetActive(true);
			InitGameObject.GetEvent.gameObject.SetActive(true);
			InitGameObject.OptionDropdown.ClearOptions();
			InitGameObject.OptionDropdown.AddOptions(Options);
			UIAlarmPopup.instance.currentAnimState = 0;
			GameObject ob_blue = UIAlarmPopup.instance.ob_blue;
			GameObject ob_normal = UIAlarmPopup.instance.ob_normal;
			GameObject ob_Reward = UIAlarmPopup.instance.ob_Reward;
			GameObject ob_BlackBg = UIAlarmPopup.instance.ob_BlackBg;
			List<GameObject> buttonRoots = UIAlarmPopup.instance.ButtonRoots;
			ob_blue.SetActive(false);
			ob_normal.SetActive(true);
			ob_Reward.SetActive(false);
			ob_BlackBg.SetActive(false);
			foreach (GameObject gameObject3 in buttonRoots)
			{
				gameObject3.SetActive(false);
			}
			UIAlarmPopup.instance.currentAlarmType = 0;
			UIAlarmPopup.instance.buttonNumberType = 1;
			UIAlarmPopup.instance.currentmode = 0;
			UIAlarmPopup.instance.anim.updateMode = 0;
			TextMeshProUGUI txt_alarm = UIAlarmPopup.instance.txt_alarm;
			txt_alarm.text = TextDataModel.GetText(alarmtype, args);
			if (string.IsNullOrWhiteSpace(txt_alarm.text))
			{
				txt_alarm.text = string.Format(alarmtype, args);
			}
			InitGameObject.GetOptionEvent = confirmFunc;
			UIAlarmPopup.instance.Open();
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002650 File Offset: 0x00000850
		private static void OnClick(BaseEventData data)
		{
			UISoundManager.instance.PlayEffectSound(1);
			InitGameObject.OptionDropdown.gameObject.SetActive(false);
			InitGameObject.GetEvent.gameObject.SetActive(false);
			UIAlarmPopup.instance.Close();
			if (InitGameObject.GetOptionEvent != null)
			{
				InitGameObject.GetOptionEvent(InitGameObject.OptionDropdown.value);
				InitGameObject.GetOptionEvent = null;
			}
		}

		// Token: 0x04000003 RID: 3
		private static TMP_Dropdown OptionDropdown;

		// Token: 0x04000004 RID: 4
		private static OptionEvent GetOptionEvent;

		// Token: 0x04000005 RID: 5
		private static EventTrigger GetEvent;
	}
}
