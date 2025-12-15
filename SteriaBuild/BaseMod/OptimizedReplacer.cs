using System;
using System.Collections.Generic;
using GTMDProjectMoon;

namespace BaseMod
{
	// Token: 0x02000059 RID: 89
	internal static class OptimizedReplacer
	{
		// Token: 0x060000C2 RID: 194 RVA: 0x00006B00 File Offset: 0x00004D00
		internal static void AddOrReplace<TElement, TBase>(TrackerDict<TElement> dict, List<TBase> originList, Func<TBase, LorId> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			for (int i = 0; i < originList.Count; i++)
			{
				LorId key = idSelector(originList[i]);
				AddTracker<TElement> addTracker;
				if (dict.TryGetValue(key, out addTracker) && (predicate == null || predicate(addTracker.element)))
				{
					originList[i] = (TBase)((object)addTracker.element);
					addTracker.added = true;
				}
			}
			foreach (LorId key2 in dict.SortedKeys)
			{
				AddTracker<TElement> addTracker2 = dict[key2];
				if (addTracker2.added)
				{
					addTracker2.added = false;
				}
				else if (predicate == null || predicate(addTracker2.element))
				{
					originList.Add((TBase)((object)addTracker2.element));
				}
			}
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00006BE8 File Offset: 0x00004DE8
		internal static void AddOrReplace<TElement, TBase>(TrackerDict<TElement> dict, List<TBase> originList, Func<TBase, int> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			for (int i = 0; i < originList.Count; i++)
			{
				LorId key = new LorId(idSelector(originList[i]));
				AddTracker<TElement> addTracker;
				if (dict.TryGetValue(key, out addTracker) && (predicate == null || predicate(addTracker.element)))
				{
					originList[i] = (TBase)((object)addTracker.element);
					addTracker.added = true;
				}
			}
			foreach (LorId key2 in dict.SortedKeys)
			{
				AddTracker<TElement> addTracker2 = dict[key2];
				if (addTracker2.added)
				{
					addTracker2.added = false;
				}
				else if (predicate == null || predicate(addTracker2.element))
				{
					originList.Add((TBase)((object)addTracker2.element));
				}
			}
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00006CD4 File Offset: 0x00004ED4
		internal static void AddOrReplace<TSplitter, TElement, TBase>(SplitTrackerDict<TSplitter, TElement> splitDict, Dictionary<TSplitter, List<TBase>> originSplitDict, Func<TBase, LorId> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			foreach (TSplitter key in splitDict.SortedKeys)
			{
				List<TBase> originList;
				if (!originSplitDict.TryGetValue(key, out originList))
				{
					originList = (originSplitDict[key] = new List<TBase>());
				}
				OptimizedReplacer.AddOrReplace<TElement, TBase>(splitDict[key], originList, idSelector, predicate);
			}
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00006D44 File Offset: 0x00004F44
		internal static void AddOrReplace<TSplitter, TElement, TBase>(SplitTrackerDict<TSplitter, TElement> splitDict, Dictionary<TSplitter, List<TBase>> originSplitDict, Func<TBase, int> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			foreach (TSplitter key in splitDict.SortedKeys)
			{
				List<TBase> originList;
				if (!originSplitDict.TryGetValue(key, out originList))
				{
					originList = (originSplitDict[key] = new List<TBase>());
				}
				OptimizedReplacer.AddOrReplace<TElement, TBase>(splitDict[key], originList, idSelector, predicate);
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00006DB4 File Offset: 0x00004FB4
		internal static void AddOrReplace<TSplitter, TElement, TBase>(SplitTrackerDict<TSplitter, TElement> splitDict, List<TBase> originSplitList, Func<TBase, int> idSelector, Func<TBase, TSplitter> splitSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			for (int i = 0; i < originSplitList.Count; i++)
			{
				LorId key = new LorId(idSelector(originSplitList[i]));
				TSplitter key2 = splitSelector(originSplitList[i]);
				TrackerDict<TElement> trackerDict;
				AddTracker<TElement> addTracker;
				if (splitDict.TryGetValue(key2, out trackerDict) && trackerDict.TryGetValue(key, out addTracker) && (predicate == null || predicate(addTracker.element)))
				{
					originSplitList[i] = (TBase)((object)addTracker.element);
					addTracker.added = true;
				}
			}
			foreach (TSplitter key3 in splitDict.SortedKeys)
			{
				TrackerDict<TElement> trackerDict2 = splitDict[key3];
				foreach (LorId key4 in trackerDict2.SortedKeys)
				{
					AddTracker<TElement> addTracker2 = trackerDict2[key4];
					if (addTracker2.added)
					{
						addTracker2.added = false;
					}
					else if (predicate == null || predicate(addTracker2.element))
					{
						originSplitList.Add((TBase)((object)addTracker2.element));
					}
				}
			}
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00006F14 File Offset: 0x00005114
		internal static void AddOrReplaceWithInject<TSplitter, TElement, TBase>(SplitTrackerDict<TSplitter, TElement> splitDict, List<TBase> originSplitList, Func<TBase, int> idSelector, Func<TBase, TSplitter> splitSelector, Func<TElement, int> injectIdFunc, Action<TElement> addAction = null, Predicate<TElement> predicate = null) where TElement : TBase, IIdInjectable
		{
			for (int i = 0; i < originSplitList.Count; i++)
			{
				LorId key = new LorId(idSelector(originSplitList[i]));
				TSplitter key2 = splitSelector(originSplitList[i]);
				TrackerDict<TElement> trackerDict;
				AddTracker<TElement> addTracker;
				if (splitDict.TryGetValue(key2, out trackerDict) && trackerDict.TryGetValue(key, out addTracker) && (predicate == null || predicate(addTracker.element)))
				{
					originSplitList[i] = (TBase)((object)addTracker.element);
					addTracker.added = true;
				}
			}
			foreach (TSplitter key3 in splitDict.SortedKeys)
			{
				TrackerDict<TElement> trackerDict2 = splitDict[key3];
				foreach (LorId lorId in trackerDict2.SortedKeys)
				{
					AddTracker<TElement> addTracker2 = trackerDict2[lorId];
					if (addTracker2.added)
					{
						addTracker2.added = false;
					}
					else
					{
						TElement element = addTracker2.element;
						if (predicate == null || predicate(element))
						{
							if (lorId.IsBasic())
							{
								element.InjectId(lorId.id);
								originSplitList.Add((TBase)((object)element));
								if (addAction != null)
								{
									addAction(element);
								}
							}
							else
							{
								ref TElement ptr = ref element;
								if (default(TElement) == null)
								{
									TElement telement = element;
									ptr = ref telement;
								}
								ptr.InjectId(injectIdFunc(element));
								originSplitList.Add((TBase)((object)element));
							}
						}
					}
				}
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00007104 File Offset: 0x00005304
		internal static void AddOrReplaceWithInject<TElement, TBase>(TrackerDict<TElement> dict, List<TBase> originList, Func<TBase, int> idSelector, Func<TElement, int> injectIdFunc, Action<TElement> addAction = null, Predicate<TElement> predicate = null) where TElement : TBase, IIdInjectable
		{
			for (int i = 0; i < originList.Count; i++)
			{
				LorId key = new LorId(idSelector(originList[i]));
				AddTracker<TElement> addTracker;
				if (dict.TryGetValue(key, out addTracker) && (predicate == null || predicate(addTracker.element)))
				{
					originList[i] = (TBase)((object)addTracker.element);
					addTracker.added = true;
				}
			}
			foreach (LorId lorId in dict.SortedKeys)
			{
				AddTracker<TElement> addTracker2 = dict[lorId];
				if (addTracker2.added)
				{
					addTracker2.added = false;
				}
				else
				{
					TElement element = addTracker2.element;
					if (predicate == null || predicate(element))
					{
						if (lorId.IsBasic())
						{
							element.InjectId(lorId.id);
							originList.Add((TBase)((object)element));
							if (addAction != null)
							{
								addAction(element);
							}
						}
						else
						{
							ref TElement ptr = ref element;
							if (default(TElement) == null)
							{
								TElement telement = element;
								ptr = ref telement;
							}
							ptr.InjectId(injectIdFunc(element));
							originList.Add((TBase)((object)element));
						}
					}
				}
			}
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00007268 File Offset: 0x00005468
		internal static void AddOrReplace<TElement, TBase>(TrackerDict<TElement> dict, Dictionary<LorId, TBase> originDict, Func<TBase, LorId> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			foreach (AddTracker<TElement> addTracker in dict.Values)
			{
				if (predicate == null || predicate(addTracker.element))
				{
					originDict[idSelector((TBase)((object)addTracker.element))] = (TBase)((object)addTracker.element);
				}
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x000072F4 File Offset: 0x000054F4
		internal static void AddOrReplace<TElement, TBase>(TrackerDict<TElement> dict, Dictionary<int, TBase> originDict, Func<TBase, int> idSelector, Predicate<TElement> predicate = null) where TElement : TBase
		{
			foreach (AddTracker<TElement> addTracker in dict.Values)
			{
				if (predicate == null || predicate(addTracker.element))
				{
					originDict[idSelector((TBase)((object)addTracker.element))] = (TBase)((object)addTracker.element);
				}
			}
		}

		// Token: 0x04000113 RID: 275
		public static readonly IComparer<LorId> packageSortedComp = Comparer<LorId>.Create(delegate(LorId x, LorId y)
		{
			int num = (x.packageId ?? "").CompareTo(y.packageId ?? "");
			if (num != 0)
			{
				return num;
			}
			return x.id - y.id;
		});

		// Token: 0x04000114 RID: 276
		public static readonly IComparer<SephirahType> sephirahComp = Comparer<SephirahType>.Create(delegate(SephirahType x, SephirahType y)
		{
			bool flag = x >= 0 && x <= 11;
			bool flag2 = y >= 0 && y <= 11;
			if (flag)
			{
				if (flag2)
				{
					return x.CompareTo(y);
				}
				return -1;
			}
			else
			{
				if (flag2)
				{
					return 1;
				}
				return x.ToString().CompareTo(y.ToString());
			}
		});
	}
}
