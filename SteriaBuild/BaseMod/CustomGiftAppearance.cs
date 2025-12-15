using System;
using System.Collections.Generic;
using System.IO;
using ExtendedLoader;
using HarmonyLib;
using Mod;
using UnityEngine;
using UnityEngine.Rendering;

namespace BaseMod
{
	// Token: 0x0200005C RID: 92
	public class CustomGiftAppearance : GiftAppearance
	{
		// Token: 0x060000E0 RID: 224 RVA: 0x000075E5 File Offset: 0x000057E5
		public static GiftAppearance CreateCustomGift(string giftName)
		{
			return CustomGiftAppearance.CreateCustomGift(giftName.Split(new char[]
			{
				'_'
			}));
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00007600 File Offset: 0x00005800
		public static GiftAppearance CreateCustomGift(string[] array2)
		{
			if (array2.Length < 3 || array2[1].ToLower() != "custom")
			{
				return null;
			}
			GiftAppearance giftAppearance;
			GiftAppearance result;
			if (CustomGiftAppearance.CreatedGifts.TryGetValue(array2[2], out giftAppearance))
			{
				result = giftAppearance;
			}
			else
			{
				GiftAppearance component = Object.Instantiate<GiftAppearance>(Harmony_Patch.CustomGiftAppearancePrefabObject, XLRoot.persistentRoot.transform).GetComponent<GiftAppearance>();
				CustomGiftAppearance.SetGiftArtWork(component, array2[2]);
				CustomGiftAppearance.CreatedGifts[array2[2]] = component;
				result = component;
			}
			return result;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00007674 File Offset: 0x00005874
		public static void GetGiftArtWork(bool forcedly = false)
		{
			if (CustomGiftAppearance.GiftArtWorkLoaded && !forcedly)
			{
				return;
			}
			CustomGiftAppearance.GiftArtWork = new Dictionary<string, Sprite>();
			foreach (ModContent modContent in Harmony_Patch.LoadedModContents)
			{
				string path = Path.Combine(modContent._dirInfo.FullName, "GiftArtWork");
				if (Directory.Exists(path))
				{
					foreach (FileInfo fileInfo in new DirectoryInfo(path).GetFiles())
					{
						Texture2D texture2D = new Texture2D(2, 2);
						ImageConversion.LoadImage(texture2D, File.ReadAllBytes(fileInfo.FullName));
						Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
						CustomGiftAppearance.GiftArtWork[fileNameWithoutExtension] = value;
					}
				}
			}
			CustomGiftAppearance.GiftArtWorkLoaded = true;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00007790 File Offset: 0x00005990
		public static void GetGiftArtWork()
		{
			CustomGiftAppearance.GetGiftArtWork(false);
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00007798 File Offset: 0x00005998
		public static Sprite GetGiftArtWork(string name)
		{
			CustomGiftAppearance.GetGiftArtWork();
			return CustomGiftAppearance.GiftArtWork.GetValueSafe(name);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x000077AC File Offset: 0x000059AC
		public void Awake()
		{
			if (!this.inited)
			{
				if (this._frontSpriteRenderer == null)
				{
					GameObject gameObject = new GameObject("new");
					gameObject.transform.SetParent(base.gameObject.transform);
					gameObject.transform.localPosition = new Vector2(0f, 0f);
					gameObject.transform.localScale = new Vector2(1f, 1f);
					this._frontSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
				}
				if (this._frontBackSpriteRenderer == null)
				{
					GameObject gameObject2 = new GameObject("new");
					gameObject2.transform.SetParent(base.gameObject.transform);
					gameObject2.transform.localPosition = new Vector2(0f, 0f);
					gameObject2.transform.localScale = new Vector2(1f, 1f);
					this._frontBackSpriteRenderer = gameObject2.AddComponent<SpriteRenderer>();
				}
				if (this._sideSpriteRenderer == null)
				{
					GameObject gameObject3 = new GameObject("new");
					gameObject3.transform.SetParent(base.gameObject.transform);
					gameObject3.transform.localPosition = new Vector2(0f, 0f);
					gameObject3.transform.localScale = new Vector2(1f, 1f);
					this._sideSpriteRenderer = gameObject3.AddComponent<SpriteRenderer>();
				}
				if (this._sideBackSpriteRenderer == null)
				{
					GameObject gameObject4 = new GameObject("new");
					gameObject4.transform.SetParent(base.gameObject.transform);
					gameObject4.transform.localPosition = new Vector2(0f, 0f);
					gameObject4.transform.localScale = new Vector2(1f, 1f);
					this._sideBackSpriteRenderer = gameObject4.AddComponent<SpriteRenderer>();
				}
				this.inited = true;
			}
			SortingGroup component = base.GetComponent<SortingGroup>();
			if (component != null)
			{
				Object.Destroy(component);
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000079CA File Offset: 0x00005BCA
		public void CustomInit(string name)
		{
			CustomGiftAppearance.SetGiftArtWork(this, name);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x000079D4 File Offset: 0x00005BD4
		internal static void SetGiftArtWork(GiftAppearance giftAppearance, string name)
		{
			CustomGiftAppearance.GetGiftArtWork();
			CustomGiftAppearance.SetGiftRendererSprite(giftAppearance._frontSpriteRenderer, name + "_front");
			CustomGiftAppearance.SetGiftRendererSprite(giftAppearance._sideSpriteRenderer, name + "_side");
			CustomGiftAppearance.SetGiftRendererSprite(giftAppearance._frontBackSpriteRenderer, name + "_frontBack");
			CustomGiftAppearance.SetGiftRendererSprite(giftAppearance._sideBackSpriteRenderer, name + "_sideBack");
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00007A3E File Offset: 0x00005C3E
		private static void SetGiftRendererSprite(SpriteRenderer renderer, string name)
		{
			if (!renderer)
			{
				return;
			}
			if (CustomGiftAppearance.GiftArtWork.ContainsKey(name))
			{
				renderer.enabled = true;
				renderer.sprite = CustomGiftAppearance.GiftArtWork[name];
				return;
			}
			renderer.enabled = false;
			renderer.sprite = null;
		}

		// Token: 0x0400011B RID: 283
		public static Dictionary<string, GiftAppearance> CreatedGifts = new Dictionary<string, GiftAppearance>();

		// Token: 0x0400011C RID: 284
		public static Dictionary<string, Sprite> GiftArtWork;

		// Token: 0x0400011D RID: 285
		public bool inited;

		// Token: 0x0400011E RID: 286
		public static bool GiftArtWorkLoaded;
	}
}
