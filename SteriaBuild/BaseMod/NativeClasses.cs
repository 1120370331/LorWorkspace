using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BaseMod
{
	// Token: 0x0200005A RID: 90
	internal static class NativeClasses
	{
		// Token: 0x060000CC RID: 204 RVA: 0x000073B6 File Offset: 0x000055B6
		internal static NativeClasses.IShellLinkW CreateShellLink()
		{
			return (NativeClasses.IShellLinkW)new NativeClasses.CShellLink();
		}

		// Token: 0x04000115 RID: 277
		internal const uint SLGP_SHORTPATH = 1U;

		// Token: 0x04000116 RID: 278
		internal const uint SLGP_UNCPRIORITY = 2U;

		// Token: 0x04000117 RID: 279
		internal const uint SLGP_RAWPATH = 4U;

		// Token: 0x02000079 RID: 121
		[Flags]
		internal enum SLR_MODE : uint
		{
			// Token: 0x0400017E RID: 382
			SLR_INVOKE_MSI = 128U,
			// Token: 0x0400017F RID: 383
			SLR_NOLINKINFO = 64U,
			// Token: 0x04000180 RID: 384
			SLR_NO_UI = 1U,
			// Token: 0x04000181 RID: 385
			SLR_NOUPDATE = 8U,
			// Token: 0x04000182 RID: 386
			SLR_NOSEARCH = 16U,
			// Token: 0x04000183 RID: 387
			SLR_NOTRACK = 32U,
			// Token: 0x04000184 RID: 388
			SLR_UPDATE = 4U,
			// Token: 0x04000185 RID: 389
			SLR_NO_UI_WITH_MSG_PUMP = 257U
		}

		// Token: 0x0200007A RID: 122
		[Flags]
		internal enum STGM_ACCESS : uint
		{
			// Token: 0x04000187 RID: 391
			STGM_READ = 0U,
			// Token: 0x04000188 RID: 392
			STGM_WRITE = 1U,
			// Token: 0x04000189 RID: 393
			STGM_READWRITE = 2U,
			// Token: 0x0400018A RID: 394
			STGM_SHARE_DENY_NONE = 64U,
			// Token: 0x0400018B RID: 395
			STGM_SHARE_DENY_READ = 48U,
			// Token: 0x0400018C RID: 396
			STGM_SHARE_DENY_WRITE = 32U,
			// Token: 0x0400018D RID: 397
			STGM_SHARE_EXCLUSIVE = 16U,
			// Token: 0x0400018E RID: 398
			STGM_PRIORITY = 262144U,
			// Token: 0x0400018F RID: 399
			STGM_CREATE = 4096U,
			// Token: 0x04000190 RID: 400
			STGM_CONVERT = 131072U,
			// Token: 0x04000191 RID: 401
			STGM_FAILIFTHERE = 0U,
			// Token: 0x04000192 RID: 402
			STGM_DIRECT = 0U,
			// Token: 0x04000193 RID: 403
			STGM_TRANSACTED = 65536U,
			// Token: 0x04000194 RID: 404
			STGM_NOSCRATCH = 1048576U,
			// Token: 0x04000195 RID: 405
			STGM_NOSNAPSHOT = 2097152U,
			// Token: 0x04000196 RID: 406
			STGM_SIMPLE = 134217728U,
			// Token: 0x04000197 RID: 407
			STGM_DIRECT_SWMR = 4194304U,
			// Token: 0x04000198 RID: 408
			STGM_DELETEONRELEASE = 67108864U
		}

		// Token: 0x0200007B RID: 123
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct _FILETIME
		{
			// Token: 0x04000199 RID: 409
			public uint dwLowDateTime;

			// Token: 0x0400019A RID: 410
			public uint dwHighDateTime;
		}

		// Token: 0x0200007C RID: 124
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
		internal struct _WIN32_FIND_DATAW
		{
			// Token: 0x0400019B RID: 411
			public uint dwFileAttributes;

			// Token: 0x0400019C RID: 412
			public NativeClasses._FILETIME ftCreationTime;

			// Token: 0x0400019D RID: 413
			public NativeClasses._FILETIME ftLastAccessTime;

			// Token: 0x0400019E RID: 414
			public NativeClasses._FILETIME ftLastWriteTime;

			// Token: 0x0400019F RID: 415
			public uint nFileSizeHigh;

			// Token: 0x040001A0 RID: 416
			public uint nFileSizeLow;

			// Token: 0x040001A1 RID: 417
			public uint dwReserved0;

			// Token: 0x040001A2 RID: 418
			public uint dwReserved1;

			// Token: 0x040001A3 RID: 419
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string cFileName;

			// Token: 0x040001A4 RID: 420
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}

		// Token: 0x0200007D RID: 125
		[Guid("000214F9-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		internal interface IShellLinkW
		{
			// Token: 0x060002F5 RID: 757
			[PreserveSig]
			int GetPath([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszFile, int cchMaxPath, ref NativeClasses._WIN32_FIND_DATAW pfd, uint fFlags);

			// Token: 0x060002F6 RID: 758
			[PreserveSig]
			int GetIDList(out IntPtr ppidl);

			// Token: 0x060002F7 RID: 759
			[PreserveSig]
			int SetIDList(IntPtr pidl);

			// Token: 0x060002F8 RID: 760
			[PreserveSig]
			int GetDescription([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszFile, int cchMaxName);

			// Token: 0x060002F9 RID: 761
			[PreserveSig]
			int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

			// Token: 0x060002FA RID: 762
			[PreserveSig]
			int GetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszDir, int cchMaxPath);

			// Token: 0x060002FB RID: 763
			[PreserveSig]
			int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

			// Token: 0x060002FC RID: 764
			[PreserveSig]
			int GetArguments([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszArgs, int cchMaxPath);

			// Token: 0x060002FD RID: 765
			[PreserveSig]
			int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

			// Token: 0x060002FE RID: 766
			[PreserveSig]
			int GetHotkey(out ushort pwHotkey);

			// Token: 0x060002FF RID: 767
			[PreserveSig]
			int SetHotkey(ushort pwHotkey);

			// Token: 0x06000300 RID: 768
			[PreserveSig]
			int GetShowCmd(out uint piShowCmd);

			// Token: 0x06000301 RID: 769
			[PreserveSig]
			int SetShowCmd(uint piShowCmd);

			// Token: 0x06000302 RID: 770
			[PreserveSig]
			int GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

			// Token: 0x06000303 RID: 771
			[PreserveSig]
			int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

			// Token: 0x06000304 RID: 772
			[PreserveSig]
			int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

			// Token: 0x06000305 RID: 773
			[PreserveSig]
			int Resolve(IntPtr hWnd, uint fFlags);

			// Token: 0x06000306 RID: 774
			[PreserveSig]
			int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}

		// Token: 0x0200007E RID: 126
		[Guid("0000010B-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		internal interface IPersistFile
		{
			// Token: 0x06000307 RID: 775
			[PreserveSig]
			int GetClassID(out Guid pClassID);

			// Token: 0x06000308 RID: 776
			[PreserveSig]
			int IsDirty();

			// Token: 0x06000309 RID: 777
			[PreserveSig]
			int Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

			// Token: 0x0600030A RID: 778
			[PreserveSig]
			int Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

			// Token: 0x0600030B RID: 779
			[PreserveSig]
			int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

			// Token: 0x0600030C RID: 780
			[PreserveSig]
			int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pszIconPath);
		}

		// Token: 0x0200007F RID: 127
		[Guid("00021401-0000-0000-C000-000000000046")]
		[ClassInterface(ClassInterfaceType.None)]
		[ComImport]
		private class CShellLink
		{
			// Token: 0x0600030D RID: 781
			[MethodImpl(MethodImplOptions.InternalCall)]
			public extern CShellLink();
		}
	}
}
