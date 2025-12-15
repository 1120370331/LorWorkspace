using System;
using System.Collections.Generic;

namespace BaseMod
{
	// Token: 0x0200006A RID: 106
	public static class BuffExtension
	{
		// Token: 0x060002BB RID: 699 RVA: 0x0001C004 File Offset: 0x0001A204
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static BattleUnitBuf AddBufByCard(this BattleUnitBufListDetail unitBufListDetail, BattleUnitBuf buf, int stack, BattleUnitModel actor = null, BufReadyType readyType = 0)
		{
			if (buf == null)
			{
				return buf;
			}
			if (actor == null)
			{
				actor = unitBufListDetail._self;
			}
			buf._owner = unitBufListDetail._self;
			buf.stack = 0;
			BattleUnitBuf battleUnitBuf = buf.FindMatch(readyType);
			if (battleUnitBuf == null)
			{
				return battleUnitBuf;
			}
			battleUnitBuf.Modify(stack, actor, true);
			return battleUnitBuf;
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0001C050 File Offset: 0x0001A250
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static T AddBufByCard<T>(this BattleUnitBufListDetail unitBufListDetail, int stack, BattleUnitModel actor = null, BufReadyType readyType = 0) where T : BattleUnitBuf
		{
			if (actor == null)
			{
				actor = unitBufListDetail._self;
			}
			T t = Activator.CreateInstance<T>();
			t._owner = unitBufListDetail._self;
			t.stack = 0;
			BattleUnitBuf battleUnitBuf = t.FindMatch(readyType);
			if (battleUnitBuf == null)
			{
				return (T)((object)battleUnitBuf);
			}
			battleUnitBuf.Modify(stack, actor, true);
			return (T)((object)battleUnitBuf);
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0001C0B0 File Offset: 0x0001A2B0
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static BattleUnitBuf AddBufByEtc(this BattleUnitBufListDetail unitBufListDetail, BattleUnitBuf buf, int stack, BattleUnitModel actor = null, BufReadyType readyType = 0)
		{
			if (buf == null)
			{
				return buf;
			}
			if (actor == null)
			{
				actor = unitBufListDetail._self;
			}
			buf._owner = unitBufListDetail._self;
			buf.stack = 0;
			BattleUnitBuf battleUnitBuf = buf.FindMatch(readyType);
			if (battleUnitBuf == null)
			{
				return battleUnitBuf;
			}
			battleUnitBuf.Modify(stack, actor, false);
			return battleUnitBuf;
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0001C0FC File Offset: 0x0001A2FC
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static T AddBufByEtc<T>(this BattleUnitBufListDetail unitBufListDetail, int stack, BattleUnitModel actor = null, BufReadyType readyType = 0) where T : BattleUnitBuf
		{
			if (actor == null)
			{
				actor = unitBufListDetail._self;
			}
			T t = Activator.CreateInstance<T>();
			t._owner = unitBufListDetail._self;
			t.stack = 0;
			BattleUnitBuf battleUnitBuf = t.FindMatch(readyType);
			if (battleUnitBuf == null)
			{
				return (T)((object)battleUnitBuf);
			}
			battleUnitBuf.Modify(stack, actor, false);
			return (T)((object)battleUnitBuf);
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0001C15C File Offset: 0x0001A35C
		public static TResult FindBuf<TResult>(this BattleUnitBufListDetail unitBufListDetail, BufReadyType readyType = 0) where TResult : BattleUnitBuf
		{
			List<BattleUnitBuf> list = unitBufListDetail.FindList(readyType);
			for (int i = 0; i < list.Count; i++)
			{
				TResult tresult = list[i] as TResult;
				if (tresult != null && !list[i].IsDestroyed())
				{
					return tresult;
				}
			}
			return default(TResult);
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0001C1B8 File Offset: 0x0001A3B8
		public static List<TResult> FindAllBuf<TResult>(this BattleUnitBufListDetail unitBufListDetail, BufReadyType readyType = 0) where TResult : BattleUnitBuf
		{
			List<TResult> list = new List<TResult>();
			List<BattleUnitBuf> list2 = unitBufListDetail.FindList(readyType);
			for (int i = 0; i < list2.Count; i++)
			{
				TResult tresult = list[i];
				if (tresult != null && !list[i].IsDestroyed())
				{
					list.Add(tresult);
				}
			}
			return list;
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x0001C20F File Offset: 0x0001A40F
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static int AddBufStack(this BattleUnitBuf buf, int stack, BattleUnitModel actor = null, bool byCard = true)
		{
			buf.Modify(stack, actor, byCard);
			return buf.stack;
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x0001C224 File Offset: 0x0001A424
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		public static int SetBufStack<TResult>(this BattleUnitBufListDetail unitBufListDetail, int stack, BufReadyType readyType = 0) where TResult : BattleUnitBuf
		{
			BattleUnitBuf battleUnitBuf = unitBufListDetail.FindBuf(readyType);
			if (battleUnitBuf != null)
			{
				battleUnitBuf.stack = stack;
				battleUnitBuf.OnAddBuf(stack);
				return battleUnitBuf.stack;
			}
			return 0;
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x0001C258 File Offset: 0x0001A458
		public static BattleUnitBuf FindMatch(this BattleUnitBuf buf, BufReadyType readyType)
		{
			if (buf._owner == null || !buf._owner.bufListDetail.CanAddBuf(buf))
			{
				return null;
			}
			List<BattleUnitBuf> list = buf._owner.bufListDetail.FindList(readyType);
			BattleUnitBuf battleUnitBuf = list.Find((BattleUnitBuf targetBuf) => targetBuf.GetType() == buf.GetType() && !targetBuf.IsDestroyed());
			if (battleUnitBuf == null || battleUnitBuf.independentBufIcon)
			{
				buf.Init(buf._owner);
				battleUnitBuf = buf;
				list.Add(battleUnitBuf);
			}
			return battleUnitBuf;
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x0001C2F8 File Offset: 0x0001A4F8
		[Obsolete("Use EnumExtenderV2 and KeywordUtil for native compatibility with original KeywordBuf system", false)]
		private static BattleUnitBuf Modify(this BattleUnitBuf buf, int stack, BattleUnitModel actor, bool byCard = true)
		{
			if (byCard)
			{
				int num = 0;
				num += actor.OnGiveKeywordBufByCard(buf, stack, buf._owner);
				num += buf._owner.OnAddKeywordBufByCard(buf, stack);
				stack += num;
				stack *= actor.GetMultiplierOnGiveKeywordBufByCard(buf, buf._owner);
			}
			buf._owner.bufListDetail.ModifyStack(buf, stack);
			int stack2 = buf.stack;
			buf.stack += stack;
			buf.OnAddBuf(stack);
			if (byCard)
			{
				buf._owner.OnAddKeywordBufByCardForEvent(buf.bufType, stack, 1);
			}
			if (buf.bufType == 23 && buf.stack > stack2)
			{
				buf._owner.OnGainChargeStack();
			}
			buf._owner.bufListDetail.CheckGift(buf.bufType, stack, actor);
			return buf;
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x0001C3C0 File Offset: 0x0001A5C0
		public static List<BattleUnitBuf> FindList(this BattleUnitBufListDetail unitBufListDetail, BufReadyType readyType = 0)
		{
			List<BattleUnitBuf> result = unitBufListDetail.GetActivatedBufList();
			if (readyType != 1)
			{
				if (readyType == 2)
				{
					result = unitBufListDetail.GetReadyReadyBufList();
				}
			}
			else
			{
				result = unitBufListDetail.GetReadyBufList();
			}
			return result;
		}
	}
}
