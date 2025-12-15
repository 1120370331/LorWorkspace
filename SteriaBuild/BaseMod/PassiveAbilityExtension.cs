using System;
using System.Collections.Generic;

namespace BaseMod
{
	// Token: 0x02000069 RID: 105
	public static class PassiveAbilityExtension
	{
		// Token: 0x060002B7 RID: 695 RVA: 0x0001BF0D File Offset: 0x0001A10D
		public static T FindPassive<T>(this BattleUnitPassiveDetail passiveDetail) where T : PassiveAbilityBase
		{
			return (T)((object)passiveDetail.PassiveList.Find((PassiveAbilityBase x) => x is T));
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x0001BF40 File Offset: 0x0001A140
		public static List<T> FindPassives<T>(this BattleUnitPassiveDetail passiveDetail) where T : PassiveAbilityBase
		{
			List<T> list = new List<T>();
			List<PassiveAbilityBase> passiveList = passiveDetail.PassiveList;
			for (int i = 0; i < passiveList.Count; i++)
			{
				T t = list[i];
				if (t != null)
				{
					list.Add(t);
				}
			}
			return list;
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0001BF83 File Offset: 0x0001A183
		public static T FindActivatedPassive<T>(this BattleUnitPassiveDetail passiveDetail) where T : PassiveAbilityBase
		{
			return (T)((object)passiveDetail.PassiveList.Find((PassiveAbilityBase x) => x is T && x.isActiavted));
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0001BFB4 File Offset: 0x0001A1B4
		public static List<T> FindActivatedPassives<T>(this BattleUnitPassiveDetail passiveDetail) where T : PassiveAbilityBase
		{
			List<T> list = new List<T>();
			List<PassiveAbilityBase> passiveList = passiveDetail.PassiveList;
			for (int i = 0; i < passiveList.Count; i++)
			{
				T t = list[i];
				if (t != null && t.isActiavted)
				{
					list.Add(t);
				}
			}
			return list;
		}
	}
}
