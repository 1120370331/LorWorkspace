using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameSave;
using Mod;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace BaseMod
{
	// Token: 0x02000066 RID: 102
	public class UtilTools
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x0600027B RID: 635 RVA: 0x0001A958 File Offset: 0x00018B58
		public static Font _DefFont
		{
			get
			{
				if (UtilTools.DefFont == null)
				{
					UtilTools.DefFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
					UtilTools.DefFontColor = UIColorManager.Manager.GetUIColor(0);
				}
				return UtilTools.DefFont;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x0600027C RID: 636 RVA: 0x0001A98B File Offset: 0x00018B8B
		public static Color _DefFontColor
		{
			get
			{
				return UtilTools.DefFontColor;
			}
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0001A994 File Offset: 0x00018B94
		public static InputField CreateInputField(Transform parent, string Imagepath, Vector2 position, TextAnchor tanchor, int fsize, Color tcolor, Font font)
		{
			GameObject gameObject = UtilTools.CreateImage(parent, Imagepath, new Vector2(1f, 1f), position).gameObject;
			Text text = UtilTools.CreateText(gameObject.transform, new Vector2(0f, 0f), fsize, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), tanchor, tcolor, font);
			text.text = "";
			InputField inputField = gameObject.AddComponent<InputField>();
			inputField.targetGraphic = gameObject.GetComponent<Image>();
			inputField.textComponent = text;
			return inputField;
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0001AA34 File Offset: 0x00018C34
		public static void DeepCopyGameObject(Transform original, Transform copyed)
		{
			copyed.localPosition = original.localPosition;
			copyed.localRotation = original.localRotation;
			copyed.localScale = original.localScale;
			copyed.gameObject.layer = original.gameObject.layer;
			for (int i = 0; i < copyed.childCount; i++)
			{
				UtilTools.DeepCopyGameObject(original.GetChild(i), copyed.GetChild(i));
			}
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0001AA9F File Offset: 0x00018C9F
		public static IEnumerator RenderCam_2(int index, UICharacterRenderer renderer)
		{
			return renderer.RenderCam(index);
		}

		// Token: 0x06000280 RID: 640 RVA: 0x0001AAA8 File Offset: 0x00018CA8
		public static Button AddButton(Image target)
		{
			Button button = target.gameObject.AddComponent<Button>();
			button.targetGraphic = target;
			return button;
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0001AABC File Offset: 0x00018CBC
		public static Image CreateImage(Transform parent, string Imagepath, Vector2 scale, Vector2 position)
		{
			GameObject gameObject = new GameObject("Image");
			Image image = gameObject.AddComponent<Image>();
			image.transform.SetParent(parent);
			Texture2D texture2D = new Texture2D(2, 2);
			ImageConversion.LoadImage(texture2D, File.ReadAllBytes(Imagepath));
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
			image.sprite = sprite;
			image.rectTransform.sizeDelta = new Vector2((float)texture2D.width, (float)texture2D.height);
			gameObject.SetActive(true);
			gameObject.transform.localScale = scale;
			gameObject.transform.localPosition = position;
			return image;
		}

		// Token: 0x06000282 RID: 642 RVA: 0x0001AB7C File Offset: 0x00018D7C
		public static Image CreateImage(Transform parent, Sprite Image, Vector2 scale, Vector2 position)
		{
			GameObject gameObject = new GameObject("Image");
			Image image = gameObject.AddComponent<Image>();
			image.transform.SetParent(parent);
			image.sprite = Image;
			image.rectTransform.sizeDelta = new Vector2((float)Image.texture.width, (float)Image.texture.height);
			gameObject.SetActive(true);
			gameObject.transform.localScale = scale;
			gameObject.transform.localPosition = position;
			return image;
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0001AC00 File Offset: 0x00018E00
		public static Text CreateText(Transform target, Vector2 position, int fsize, Vector2 anchormin, Vector2 anchormax, Vector2 anchorposition, TextAnchor anchor, Color tcolor, Font font)
		{
			GameObject gameObject = new GameObject("Text");
			Text text = gameObject.AddComponent<Text>();
			gameObject.transform.SetParent(target);
			text.rectTransform.sizeDelta = Vector2.zero;
			text.rectTransform.anchorMin = anchormin;
			text.rectTransform.anchorMax = anchormax;
			text.rectTransform.anchoredPosition = anchorposition;
			text.text = " ";
			text.font = font;
			text.fontSize = fsize;
			text.color = tcolor;
			text.alignment = anchor;
			gameObject.transform.localScale = new Vector3(1f, 1f);
			gameObject.transform.localPosition = position;
			gameObject.SetActive(true);
			return text;
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0001ACBC File Offset: 0x00018EBC
		public static TextMeshProUGUI CreateText_TMP(Transform target, Vector2 position, int fsize, Vector2 anchormin, Vector2 anchormax, Vector2 anchorposition, TextAlignmentOptions anchor, Color tcolor, TMP_FontAsset font)
		{
			GameObject gameObject = new GameObject("Text");
			TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
			gameObject.transform.SetParent(target);
			textMeshProUGUI.rectTransform.sizeDelta = Vector2.zero;
			textMeshProUGUI.rectTransform.anchorMin = anchormin;
			textMeshProUGUI.rectTransform.anchorMax = anchormax;
			textMeshProUGUI.rectTransform.anchoredPosition = anchorposition;
			textMeshProUGUI.text = " ";
			textMeshProUGUI.font = font;
			textMeshProUGUI.fontSize = (float)fsize;
			textMeshProUGUI.color = tcolor;
			textMeshProUGUI.alignment = anchor;
			gameObject.transform.localScale = new Vector3(1f, 1f);
			gameObject.transform.localPosition = position;
			gameObject.SetActive(true);
			return textMeshProUGUI;
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0001AD7C File Offset: 0x00018F7C
		public static Text CreateText(Transform target)
		{
			return UtilTools.CreateText(target, new Vector2(0f, 0f), 10, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), 0, Color.black, UtilTools.DefFont);
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0001ADD8 File Offset: 0x00018FD8
		public static Button CreateButton(Transform parent, string Imagepath, Vector2 scale, Vector2 position)
		{
			Image image = UtilTools.CreateImage(parent, Imagepath, scale, position);
			Button button = image.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			return button;
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0001AE04 File Offset: 0x00019004
		public static Button CreateButton(Transform parent, Sprite Image, Vector2 scale, Vector2 position)
		{
			Image image = UtilTools.CreateImage(parent, Image, scale, position);
			Button button = image.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			return button;
		}

		// Token: 0x06000288 RID: 648 RVA: 0x0001AE2D File Offset: 0x0001902D
		public static Button CreateButton(Transform parent, string Imagepath)
		{
			return UtilTools.CreateButton(parent, Imagepath, new Vector2(1f, 1f), new Vector2(0f, 0f));
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0001AE54 File Offset: 0x00019054
		public static UIOriginEquipPageSlot DuplicateEquipPageSlot(UIOriginEquipPageSlot origin, UIOriginEquipPageList parent)
		{
			UISettingInvenEquipPageSlot uisettingInvenEquipPageSlot = origin as UISettingInvenEquipPageSlot;
			if (uisettingInvenEquipPageSlot != null)
			{
				return Object.Instantiate<UISettingInvenEquipPageSlot>(uisettingInvenEquipPageSlot, origin.transform.parent);
			}
			return Object.Instantiate<UIInvenEquipPageSlot>((UIInvenEquipPageSlot)origin, origin.transform.parent);
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0001AE94 File Offset: 0x00019094
		public static void LoadFromSaveData_GiftInventory(GiftInventory __instance, SaveData data)
		{
			try
			{
				__instance.LoadFromSaveData(data);
			}
			catch
			{
			}
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0001AEC0 File Offset: 0x000190C0
		public static void SpriteTrace(string path, Sprite sprite)
		{
			string text = sprite.name + Environment.NewLine;
			text = text + sprite.rect.ToString() + Environment.NewLine;
			text = text + sprite.border.ToString() + Environment.NewLine;
			text = text + sprite.pivot.ToString() + Environment.NewLine;
			File.WriteAllText(path, text);
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0001AF48 File Offset: 0x00019148
		public static DirectoryInfo FindExistDir(string path)
		{
			foreach (ModContent modContent in Harmony_Patch.LoadedModContents)
			{
				DirectoryInfo dirInfo = modContent._dirInfo;
				if (Directory.Exists(dirInfo.FullName + "/" + path))
				{
					return new DirectoryInfo(dirInfo.FullName + "/" + path);
				}
			}
			return null;
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0001AFCC File Offset: 0x000191CC
		public static void CopyDir(string srcPath, string aimPath)
		{
			try
			{
				if (aimPath[aimPath.Length - 1] != Path.DirectorySeparatorChar)
				{
					aimPath += Path.DirectorySeparatorChar.ToString();
				}
				if (!Directory.Exists(aimPath))
				{
					Directory.CreateDirectory(aimPath);
				}
				foreach (string text in Directory.GetFileSystemEntries(srcPath))
				{
					if (Directory.Exists(text))
					{
						UtilTools.CopyDir(text, aimPath + Path.GetFileName(text));
					}
					else
					{
						File.Copy(text, aimPath + Path.GetFileName(text), true);
					}
				}
			}
			catch (Exception ex)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message + Environment.NewLine + ex.StackTrace);
				File.WriteAllText(Application.dataPath + "/Mods/CopyDirerror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0001B0C0 File Offset: 0x000192C0
		public static Shortcut CreateShortcut(string shortcutDirectory, string shortcutName, string targetPath, string targetDirectory, string description = null, string iconLocation = null)
		{
			if (!Directory.Exists(shortcutDirectory))
			{
				Directory.CreateDirectory(shortcutDirectory);
			}
			string fileName = Path.Combine(shortcutDirectory, string.Format("{0}.lnk", shortcutName));
			Shortcut shortcut = new Shortcut();
			shortcut.Path = targetPath;
			shortcut.Arguments = "";
			shortcut.WorkingDirectory = targetDirectory;
			shortcut.Description = description;
			shortcut.Save(fileName);
			return shortcut;
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0001B11C File Offset: 0x0001931C
		public static string GetTransformPath(Transform transform, bool includeSelf = false)
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

		// Token: 0x06000290 RID: 656 RVA: 0x0001B174 File Offset: 0x00019374
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
					string text = UtilTools.GetTransformPath(gameObject2.transform, true);
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

		// Token: 0x0400014E RID: 334
		public static Font DefFont;

		// Token: 0x0400014F RID: 335
		public static TMP_FontAsset DefFont_TMP;

		// Token: 0x04000150 RID: 336
		public static Color DefFontColor;
	}
}
