using System;
using System.IO;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x0200005E RID: 94
	public class SimpleMapManager : CustomMapManager
	{
		// Token: 0x060000EF RID: 239 RVA: 0x00007AA1 File Offset: 0x00005CA1
		public bool SimpleInit(string Path, string MapName)
		{
			if (Directory.Exists(Path))
			{
				this.resourcePath = Path + "/";
				this.mapName = MapName;
				return base.transform;
			}
			return false;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00007AD0 File Offset: 0x00005CD0
		public override bool IsMapChangable()
		{
			return false;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00007AD3 File Offset: 0x00005CD3
		public override bool IsMapChangableByAssimilation()
		{
			return false;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00007AD8 File Offset: 0x00005CD8
		public override void CustomInit()
		{
			try
			{
				this.Retextualize();
				AudioClip[] array = new AudioClip[3];
				foreach (FileInfo fileInfo in new DirectoryInfo(this.resourcePath).GetFiles())
				{
					if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == "BGM")
					{
						array[0] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM");
						array[1] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM");
						array[2] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM");
						break;
					}
					if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == "BGM1")
					{
						array[0] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM1");
					}
					if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == "BGM2")
					{
						array[1] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM2");
					}
					if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == "BGM3")
					{
						array[2] = Tools.GetAudio(fileInfo.FullName, this.mapName + "BGM3");
					}
				}
				this.mapBgm = array;
				this.mapSize = 2;
				this._bMapInitialized = true;
				Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = 100;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/Mods/SimpleMaperror.log", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00007CAC File Offset: 0x00005EAC
		public override void EnableMap(bool b)
		{
			this.isEnabled = b;
			base.gameObject.SetActive(b);
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00007CC4 File Offset: 0x00005EC4
		public override void ResetMap()
		{
			StageWaveModel currentWaveModel = Singleton<StageController>.Instance.GetCurrentWaveModel();
			if (currentWaveModel != null)
			{
				FormationModel formation = currentWaveModel.GetFormation();
				if (formation != null)
				{
					formation.PostionList.ForEach(delegate(FormationPosition x)
					{
						x.ChangePosToDefault();
					});
				}
			}
			StageLibraryFloorModel currentStageFloorModel = Singleton<StageController>.Instance.GetCurrentStageFloorModel();
			if (currentStageFloorModel != null)
			{
				FormationModel formation2 = currentStageFloorModel.GetFormation();
				if (formation2 == null)
				{
					return;
				}
				formation2.PostionList.ForEach(delegate(FormationPosition x)
				{
					x.ChangePosToDefault();
				});
			}
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00007D58 File Offset: 0x00005F58
		public override void OnRoundStart()
		{
			base.OnRoundStart();
			SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(this.mapBgm);
			int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
			Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 100;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00007DA9 File Offset: 0x00005FA9
		public override GameObject GetWallCrater()
		{
			return null;
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00007DAC File Offset: 0x00005FAC
		public override GameObject GetScratch(int lv, Transform parent)
		{
			return null;
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00007DB0 File Offset: 0x00005FB0
		private void Retextualize()
		{
			Transform transform = base.transform.Find("[Sprite]Foregrounds_BlackFrames_Act5 (1)");
			transform.gameObject.SetActive(true);
			transform.localScale = new Vector3(6.5f, 6.8f, 1f);
			Transform transform2 = base.transform.Find("[Transform]BackgroundRootTransform (1)");
			Transform transform3 = transform2.Find("GameObject (1)");
			Transform transform4 = transform3.Find("BG (1)");
			this.DuplicateSprite(transform4, this.TextureBackground, 0.5f);
			foreach (object obj in transform3)
			{
				Transform transform5 = (Transform)obj;
				if (transform5 != transform4)
				{
					transform5.gameObject.SetActive(false);
				}
			}
			foreach (object obj2 in transform2)
			{
				Transform transform6 = (Transform)obj2;
				if (transform6 != transform3)
				{
					transform6.gameObject.SetActive(false);
				}
			}
			Transform transform7 = base.transform.Find("[Transform]GroundSprites (1)");
			Transform transform8 = transform7.Find("Road");
			this.DuplicateSprite(transform8, this.TextureFloor, 0.5f);
			Transform transform9 = transform7.Find("RoadUnder");
			this.DuplicateSprite(transform9, this.TextureFloorUnder, 0.5f);
			foreach (object obj3 in transform7)
			{
				Transform transform10 = (Transform)obj3;
				if (transform10 != transform8 && transform10 != transform9)
				{
					transform10.gameObject.SetActive(false);
				}
			}
			foreach (object obj4 in base.transform)
			{
				Transform transform11 = (Transform)obj4;
				if (transform11 != transform && transform11 != transform2 && transform11 != transform7)
				{
					transform11.gameObject.SetActive(false);
				}
			}
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000801C File Offset: 0x0000621C
		private void DuplicateSprite(Transform transform, string path, float YMiddle = 0.5f)
		{
			string path2 = this.resourcePath + path + ".png";
			if (!File.Exists(path2))
			{
				transform.gameObject.SetActive(false);
				return;
			}
			Texture2D texture2D = new Texture2D(1, 1);
			ImageConversion.LoadImage(texture2D, File.ReadAllBytes(path2));
			Sprite sprite = transform.GetComponent<SpriteRenderer>().sprite;
			transform.GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, YMiddle), sprite.pixelsPerUnit, 0U, 0);
			transform.transform.localPosition = Vector3.zero;
			transform.gameObject.SetActive(true);
		}

		// Token: 0x0400011F RID: 287
		private readonly string TextureBackground = "BackGround";

		// Token: 0x04000120 RID: 288
		private readonly string TextureFloor = "Floor";

		// Token: 0x04000121 RID: 289
		private readonly string TextureFloorUnder = "FloorUnder";

		// Token: 0x04000122 RID: 290
		private string resourcePath = "";

		// Token: 0x04000123 RID: 291
		private string mapName = "";
	}
}
