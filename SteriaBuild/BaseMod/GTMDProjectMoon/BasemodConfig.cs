using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Mod;

namespace GTMDProjectMoon
{
	// Token: 0x02000009 RID: 9
	public class BasemodConfig
	{
		// Token: 0x0600002A RID: 42 RVA: 0x00003680 File Offset: 0x00001880
		public static BasemodConfig FindBasemodConfig(string modId)
		{
			if (string.IsNullOrEmpty(modId))
			{
				return BasemodConfig.NonBasemodDefault;
			}
			BasemodConfig basemodConfig;
			if (BasemodConfig.LoadedConfigs.TryGetValue(modId, out basemodConfig) && basemodConfig != null)
			{
				return BasemodConfig.LoadedConfigs[modId];
			}
			string modPath = Singleton<ModContentManager>.Instance.GetModPath(modId);
			if (string.IsNullOrEmpty(modPath))
			{
				return null;
			}
			string path = Path.Combine(modPath, "BasemodConfig.xml");
			basemodConfig = null;
			if (File.Exists(path))
			{
				try
				{
					using (StringReader stringReader = new StringReader(File.ReadAllText(path)))
					{
						basemodConfig = (new XmlSerializer(typeof(BasemodConfig)).Deserialize(stringReader) as BasemodConfig);
					}
				}
				catch
				{
				}
			}
			if (basemodConfig == null)
			{
				if (BasemodConfig.CheckBasemodLoading(modPath))
				{
					basemodConfig = BasemodConfig.BasemodDefault;
				}
				else
				{
					basemodConfig = BasemodConfig.NonBasemodDefault;
				}
			}
			BasemodConfig.LoadedConfigs[modId] = basemodConfig;
			return basemodConfig;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003760 File Offset: 0x00001960
		private static bool CheckBasemodLoading(string modFolder)
		{
			return true;
		}

		// Token: 0x04000009 RID: 9
		[XmlElement]
		public string DefaultLocale = "default";

		// Token: 0x0400000A RID: 10
		[XmlElement]
		public bool IgnoreLocalize;

		// Token: 0x0400000B RID: 11
		[XmlElement]
		public bool IgnoreStory;

		// Token: 0x0400000C RID: 12
		[XmlElement]
		public bool IgnoreStaticFiles;

		// Token: 0x0400000D RID: 13
		[XmlIgnore]
		public static readonly BasemodConfig BasemodDefault = new BasemodConfig();

		// Token: 0x0400000E RID: 14
		[XmlIgnore]
		public static readonly BasemodConfig NonBasemodDefault = new BasemodConfig
		{
			IgnoreLocalize = true,
			IgnoreStory = true,
			IgnoreStaticFiles = true
		};

		// Token: 0x0400000F RID: 15
		[XmlIgnore]
		public static readonly Dictionary<string, BasemodConfig> LoadedConfigs = new Dictionary<string, BasemodConfig>();
	}
}
